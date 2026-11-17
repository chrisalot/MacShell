using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using MacShell.Core.Interfaces;
using MacShell.Core.Models;
using Microsoft.Extensions.Logging;

namespace MacShell.Core.Services;

/// <summary>
/// Factory for creating dock items from various sources.
/// </summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public sealed class DockItemFactory : IDockItemFactory
{
    private readonly ILogger<DockItemFactory> _logger;
    private readonly IconExtractor _iconExtractor;

    public DockItemFactory(ILogger<DockItemFactory> logger)
    {
        _logger = logger;
        _iconExtractor = new IconExtractor();
    }

    public IDockItem CreateFromExecutable(string executablePath, string? arguments = null)
    {
        var name = GetAppName(executablePath);
        var iconPath = GetIconPath(executablePath);
        var id = $"exe:{GetStableHash(executablePath)}";
        var workingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty;

        return new DockItem
        {
            Id = id,
            Name = name,
            ExecutablePath = executablePath,
            Arguments = arguments ?? string.Empty,
            WorkingDirectory = workingDirectory,
            IconPath = iconPath,
            IsPinned = false,
            IsRunning = false,
            IsStoreApp = false
        };
    }

    public IDockItem CreateFromAppUserModelId(string appUserModelId)
    {
        // For Store apps, we need to get info from the package
        var name = GetStoreAppName(appUserModelId);
        var iconPath = $"shell:AppsFolder\\{appUserModelId}";
        var id = $"store:{appUserModelId}";

        return new DockItem
        {
            Id = id,
            Name = name,
            ExecutablePath = $"shell:AppsFolder\\{appUserModelId}",
            Arguments = string.Empty,
            WorkingDirectory = string.Empty,
            IconPath = iconPath,
            IsPinned = false,
            IsRunning = false,
            AppUserModelId = appUserModelId,
            IsStoreApp = true
        };
    }

    public IDockItem CreateFromProcess(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            var mainModule = process.MainModule;
            var executablePath = mainModule?.FileName ?? string.Empty;
            var name = process.ProcessName;
            var iconPath = !string.IsNullOrEmpty(executablePath) ? GetIconPath(executablePath) : string.Empty;
            var id = $"proc:{processId}";

            // Get window handle
            var handle = process.MainWindowHandle;

            // Try to get AppUserModelId for Store apps
            string? appUserModelId = null;
            bool isStoreApp = false;
            if (handle != 0)
            {
                appUserModelId = GetAppUserModelId(handle);
                isStoreApp = !string.IsNullOrEmpty(appUserModelId);
                if (isStoreApp)
                {
                    name = GetStoreAppName(appUserModelId);
                }
            }

            return new DockItem
            {
                Id = id,
                Name = name,
                ExecutablePath = executablePath,
                Arguments = string.Empty,
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty,
                IconPath = iconPath,
                IsPinned = false,
                IsRunning = true,
                ProcessId = processId,
                WindowHandle = handle,
                AppUserModelId = appUserModelId,
                IsStoreApp = isStoreApp
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create dock item for process {ProcessId}", processId);
            throw;
        }
    }

    private string GetAppName(string executablePath)
    {
        try
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
            return versionInfo.FileDescription ?? Path.GetFileNameWithoutExtension(executablePath);
        }
        catch
        {
            return Path.GetFileNameWithoutExtension(executablePath);
        }
    }

    private string GetIconPath(string executablePath)
    {
        // Return the executable path - WinUI can extract icons from it
        return executablePath;
    }

    private string GetStoreAppName(string appUserModelId)
    {
        try
        {
            // Query registry for display name
            var keyPath = $@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages\{appUserModelId}";
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath);
            if (key != null)
            {
                return key.GetValue("DisplayName") as string ?? appUserModelId;
            }
        }
        catch
        {
            // Ignore
        }
        return appUserModelId;
    }

    private string? GetAppUserModelId(nint hWnd)
    {
        try
        {
            var propertyStore = GetPropertyStore(hWnd);
            if (propertyStore != 0)
            {
                var propVariant = new PROPVARIANT();
                var hr = IPropertyStore_GetValue(propertyStore, ref PKEY_AppUserModel_ID, ref propVariant);
                if (hr == 0 && propVariant.vt == 31) // VT_LPWSTR
                {
                    return Marshal.PtrToStringUni(propVariant.pwszVal);
                }
            }
        }
        catch
        {
            // Ignore
        }
        return null;
    }

    private nint GetPropertyStore(nint hWnd)
    {
        var guid = typeof(IPropertyStore).GUID;
        SHGetPropertyStoreForWindow(hWnd, ref guid, out var ppv);
        return ppv;
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

    // Win32 P/Invoke for AppUserModelId
    private static Guid PKEY_AppUserModel_ID = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");

    [StructLayout(LayoutKind.Sequential)]
    private struct PROPVARIANT
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public nint pwszVal;
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    private interface IPropertyStore
    {
        int GetCount(out int cProps);
        int GetAt(int iProp, out PropertyKey pkey);
        int GetValue(ref PropertyKey key, ref PROPVARIANT pv);
        int SetValue(ref PropertyKey key, ref PROPVARIANT pv);
        int Commit();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid fmtid;
        public int pid;
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetPropertyStoreForWindow(nint hwnd, ref Guid riid, out nint ppv);

    [DllImport("shell32.dll", PreserveSig = true)]
    private static extern int IPropertyStore_GetValue(nint pps, ref Guid key, ref PROPVARIANT pv);

    // Icon extractor for getting icons from executables
    private sealed class IconExtractor : IDisposable
    {
        public string? ExtractIcon(string filePath, int size = 32)
        {
            // For now, just return the file path - WinUI's BitmapImage can handle .exe/.ico files
            return filePath;
        }

        public void Dispose() { }
    }
}