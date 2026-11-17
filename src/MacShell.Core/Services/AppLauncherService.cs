using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MacShell.Core.Interfaces;
using MacShell.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace MacShell.Core.Services;

/// <summary>
/// Application launcher service - indexes Start Menu and launches apps.
/// </summary>
public sealed class AppLauncherService : IAppLauncher
{
    private readonly ILogger<AppLauncherService> _logger;
    private readonly List<AppInfo> _appIndex = new();
    private readonly object _indexLock = new();
    private bool _indexed;
    private string? _indexCachePath;

    public AppLauncherService(ILogger<AppLauncherService> logger, string? cachePath = null)
    {
        _logger = logger;
        _indexCachePath = cachePath ?? GetDefaultCachePath();
    }

    public event EventHandler? IndexUpdated;

    public async Task<IReadOnlyList<AppInfo>> SearchAsync(string query, int maxResults = 20)
    {
        await EnsureIndexedAsync();

        if (string.IsNullOrWhiteSpace(query))
        {
            lock (_indexLock)
            {
                return _appIndex.Take(maxResults).ToList();
            }
        }

        var lowerQuery = query.ToLowerInvariant();
        var words = lowerQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        lock (_indexLock)
        {
            var results = _appIndex
                .Where(app => MatchesQuery(app, lowerQuery, words))
                .OrderByDescending(app => GetMatchScore(app, lowerQuery, words))
                .Take(maxResults)
                .ToList();

            return results;
        }
    }

    public async Task<bool> LaunchAsync(AppInfo app, string? arguments = null)
    {
        try
        {
            if (app.IsStoreApp && !string.IsNullOrEmpty(app.AppUserModelId))
            {
                // Launch Store app via AppUserModelId
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"shell:AppsFolder\\{app.AppUserModelId}",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            }
            else
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = app.ExecutablePath,
                    Arguments = arguments ?? app.DefaultArguments ?? string.Empty,
                    WorkingDirectory = app.WorkingDirectory,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            }

            _logger.LogDebug("Launched {AppName}", app.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch {AppName}", app.Name);
            return false;
        }
    }

    public async Task<bool> LaunchByPathAsync(string executablePath, string? arguments = null)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = Path.GetDirectoryName(executablePath),
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(startInfo);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch by path {Path}", executablePath);
            return false;
        }
    }

    public async Task RefreshIndexAsync()
    {
        _indexed = false;
        await EnsureIndexedAsync();
    }

    private async Task EnsureIndexedAsync()
    {
        if (_indexed)
            return;

        // Try loading from cache first
        if (await TryLoadFromCacheAsync())
        {
            _indexed = true;
            return;
        }

        await BuildIndexAsync();
        await SaveToCacheAsync();
        _indexed = true;
        IndexUpdated?.Invoke(this, EventArgs.Empty);
    }

    private async Task BuildIndexAsync()
    {
        var apps = new List<AppInfo>();

        // 1. Start Menu shortcuts
        apps.AddRange(GetStartMenuApps());

        // 2. Store/UWP apps
        apps.AddRange(GetStoreApps());

        // 3. Common system locations
        apps.AddRange(GetSystemApps());

        // Deduplicate by ID
        var uniqueApps = apps
            .GroupBy(a => a.Id)
            .Select(g => g.First())
            .OrderBy(a => a.Name)
            .ToList();

        lock (_indexLock)
        {
            _appIndex.Clear();
            _appIndex.AddRange(uniqueApps);
        }

        _logger.LogInformation("Built app index with {Count} applications", _appIndex.Count);
    }

    private IEnumerable<AppInfo> GetStartMenuApps()
    {
        var paths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)
        };

        foreach (var path in paths)
        {
            if (!Directory.Exists(path)) continue;

            foreach (var file in Directory.EnumerateFiles(path, "*.lnk", SearchOption.AllDirectories))
            {
                AppInfo? app = null;
                try
                {
                    app = ParseShortcut(file);
                }
                catch
                {
                    // Ignore parse errors
                }

                if (app != null)
                    yield return app;
            }
        }
    }

    private AppInfo? ParseShortcut(string shortcutPath)
    {
        try
        {
            // Use Windows Shell to resolve shortcut via late binding
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType == null)
                return null;

            dynamic shell = Activator.CreateInstance(shellType)!;
            var folder = shell.NameSpace(Path.GetDirectoryName(shortcutPath));
            var item = folder.ParseName(Path.GetFileName(shortcutPath));
            var link = item.GetLink;

            var targetPath = link.Path;
            var arguments = link.Arguments;
            var workingDir = link.WorkingDirectory;

            if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
                return null;

            var name = Path.GetFileNameWithoutExtension(shortcutPath);
            var id = $"lnk:{GetStableHash(shortcutPath)}";

            return new AppInfo
            {
                Id = id,
                Name = name,
                ExecutablePath = targetPath,
                DefaultArguments = arguments,
                WorkingDirectory = workingDir,
                IconPath = targetPath,
                IsStoreApp = false,
                Keywords = ExtractKeywords(name)
            };
        }
        catch
        {
            return null;
        }
    }

    private IEnumerable<AppInfo> GetStoreApps()
    {
        // Query registry for installed Store apps
        var userPackagesPath = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages";
        using var key = Registry.CurrentUser.OpenSubKey(userPackagesPath);
        if (key == null) yield break;

        foreach (var subKeyName in key.GetSubKeyNames())
        {
            using var pkgKey = key.OpenSubKey(subKeyName);
            if (pkgKey == null) continue;

            var displayName = pkgKey.GetValue("DisplayName") as string;
            var appUserModelId = pkgKey.GetValue("AppUserModelId") as string;

            if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(appUserModelId))
            {
                var id = $"store:{appUserModelId}";
                yield return new AppInfo
                {
                    Id = id,
                    Name = displayName,
                    ExecutablePath = $"shell:AppsFolder\\{appUserModelId}",
                    AppUserModelId = appUserModelId,
                    IsStoreApp = true,
                    IconPath = $"shell:AppsFolder\\{appUserModelId}",
                    Keywords = ExtractKeywords(displayName)
                };
            }
        }
    }

    private IEnumerable<AppInfo> GetSystemApps()
    {
        var systemApps = new[]
        {
            new { Name = "File Explorer", Path = @"C:\Windows\explorer.exe", Id = "sys:explorer" },
            new { Name = "Settings", Path = "ms-settings:", Id = "sys:settings" },
            new { Name = "Command Prompt", Path = @"C:\Windows\System32\cmd.exe", Id = "sys:cmd" },
            new { Name = "PowerShell", Path = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", Id = "sys:powershell" },
            new { Name = "Terminal", Path = @"C:\Windows\System32\WindowsTerminal.exe", Id = "sys:terminal" },
            new { Name = "Task Manager", Path = @"C:\Windows\System32\Taskmgr.exe", Id = "sys:taskmgr" },
            new { Name = "Run", Path = "shell:::{2559a1f3-21d7-11d4-bdaf-00c04f60b9f0}", Id = "sys:run" },
        };

        foreach (var app in systemApps)
        {
            if (File.Exists(app.Path) || app.Path.StartsWith("ms-") || app.Path.StartsWith("shell:"))
            {
                yield return new AppInfo
                {
                    Id = app.Id,
                    Name = app.Name,
                    ExecutablePath = app.Path,
                    IsStoreApp = false,
                    IconPath = app.Path,
                    Keywords = ExtractKeywords(app.Name)
                };
            }
        }
    }

    private static bool MatchesQuery(AppInfo app, string lowerQuery, string[] words)
    {
        var name = app.Name.ToLowerInvariant();
        var keywords = app.Keywords.Select(k => k.ToLowerInvariant());

        // All words must match somewhere
        return words.All(word =>
            name.Contains(word) ||
            keywords.Any(k => k.Contains(word)) ||
            (app.ExecutablePath?.ToLowerInvariant().Contains(word) ?? false)
        );
    }

    private static int GetMatchScore(AppInfo app, string lowerQuery, string[] words)
    {
        var name = app.Name.ToLowerInvariant();
        int score = 0;

        // Exact name match
        if (name == lowerQuery) score += 1000;
        // Starts with query
        else if (name.StartsWith(lowerQuery)) score += 500;
        // Contains query
        else if (name.Contains(lowerQuery)) score += 200;

        // Word matches
        foreach (var word in words)
        {
            if (name.StartsWith(word)) score += 50;
            else if (name.Contains(word)) score += 20;
        }

        // Prefer non-Store apps slightly
        if (!app.IsStoreApp) score += 10;

        return score;
    }

    private static string[] ExtractKeywords(string name)
    {
        return name.Split([' ', '-', '_', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 1)
            .Distinct()
            .ToArray();
    }

    private static int GetStableHash(string input)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in input)
            {
                hash = hash * 31 + c;
            }
            return Math.Abs(hash);
        }
    }

    private async Task<bool> TryLoadFromCacheAsync()
    {
        try
        {
            if (!File.Exists(_indexCachePath))
                return false;

            var json = await File.ReadAllTextAsync(_indexCachePath);
            var cached = JsonSerializer.Deserialize<List<AppInfo>>(json, AppJsonContext.Default.ListAppInfo);

            if (cached != null && cached.Count > 0)
            {
                // Check cache age (rebuild if older than 24 hours)
                var fileInfo = new FileInfo(_indexCachePath);
                if (DateTime.Now - fileInfo.LastWriteTime < TimeSpan.FromHours(24))
                {
                    lock (_indexLock)
                    {
                        _appIndex.Clear();
                        _appIndex.AddRange(cached);
                    }
                    _logger.LogDebug("Loaded {Count} apps from cache", cached.Count);
                    return true;
                }
            }
        }
        catch
        {
            // Ignore cache errors
        }
        return false;
    }

    private async Task SaveToCacheAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_indexCachePath!);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            List<AppInfo> copy;
            lock (_indexLock)
            {
                copy = _appIndex.ToList();
            }

            var json = JsonSerializer.Serialize(copy, AppJsonContext.Default.ListAppInfo);
            await File.WriteAllTextAsync(_indexCachePath!, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save app index cache");
        }
    }

    private static string GetDefaultCachePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "MacShell", "appindex.json");
    }
}

/// <summary>
/// JSON serialization context for AppInfo.
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(List<AppInfo>))]
[System.Text.Json.Serialization.JsonSerializable(typeof(AppInfo))]
internal partial class AppJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}