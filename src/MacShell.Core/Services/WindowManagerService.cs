using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using MacShell.Core.Interfaces;
using MacShell.Core.Models;
using Microsoft.Extensions.Logging;

namespace MacShell.Core.Services;

/// <summary>
/// Window management service using Win32 APIs.
/// </summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public sealed class WindowManagerService : IWindowManager
{
    private readonly ILogger<WindowManagerService> _logger;
    private HashSet<nint> _trackedWindows = new();
    private readonly System.Threading.Timer? _pollTimer;
    private bool _disposed;
    private nint _shellHookHandle;
    private bool _useShellHook;

    public WindowManagerService(ILogger<WindowManagerService> logger)
    {
        _logger = logger;

        // Try to register shell hook for real-time window notifications
        _useShellHook = RegisterShellHook();

        if (!_useShellHook)
        {
            // Fallback to polling (every 2 seconds)
            _pollTimer = new System.Threading.Timer(PollWindows, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }

        // Initial scan
        _ = Task.Run(ScanWindowsAsync);
    }

    public event EventHandler<WindowInfo>? WindowCreated;
    public event EventHandler<WindowInfo>? WindowDestroyed;
    public event EventHandler<WindowVisibilityChangedEventArgs>? WindowVisibilityChanged;
    public event EventHandler<WindowInfo?>? ActiveWindowChanged;

    public async Task<IReadOnlyList<WindowInfo>> GetVisibleWindowsAsync()
    {
        return await Task.Run(ScanWindowsAsync);
    }

    public async Task<WindowInfo?> GetWindowByProcessIdAsync(int processId)
    {
        return await Task.Run(() =>
        {
            var windows = ScanWindowsAsync();
            return windows.FirstOrDefault(w => w.ProcessId == processId);
        });
    }

    public async Task<WindowInfo?> GetWindowByHandleAsync(nint handle)
    {
        return await Task.Run(() =>
        {
            if (IsWindow(handle) && IsWindowVisible(handle))
            {
                return GetWindowInfo(handle);
            }
            return null;
        });
    }

    public async Task<bool> ActivateWindowAsync(nint handle)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (IsIconic(handle))
                {
                    ShowWindow(handle, SW_RESTORE);
                }
                SetForegroundWindow(handle);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<bool> MinimizeWindowAsync(nint handle)
    {
        return await Task.Run(() =>
        {
            try
            {
                ShowWindow(handle, SW_MINIMIZE);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<bool> MaximizeWindowAsync(nint handle)
    {
        return await Task.Run(() =>
        {
            try
            {
                ShowWindow(handle, SW_MAXIMIZE);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<bool> RestoreWindowAsync(nint handle)
    {
        return await Task.Run(() =>
        {
            try
            {
                ShowWindow(handle, SW_RESTORE);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<bool> CloseWindowAsync(nint handle)
    {
        return await Task.Run(() =>
        {
            try
            {
                PostMessage(handle, WM_CLOSE, 0, 0);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<int?> GetProcessIdAsync(nint handle)
    {
        return await Task.Run(() =>
        {
            GetWindowThreadProcessId(handle, out var processId);
            return processId > 0 ? processId : (int?)null;
        });
    }

    public void StartMonitoring()
    {
        // Already monitoring via shell hook or polling
    }

    public void StopMonitoring()
    {
        _pollTimer?.Dispose();
        if (_useShellHook && _shellHookHandle != 0)
        {
            DeregisterShellHookWindow(_shellHookHandle);
        }
    }

    private List<WindowInfo> ScanWindowsAsync()
    {
        var windows = new List<WindowInfo>();

        EnumWindows((hWnd, lParam) =>
        {
            try
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                var info = GetWindowInfo(hWnd);
                if (info != null && ShouldTrackWindow(info))
                {
                    windows.Add(info);
                }
            }
            catch
            {
                // Ignore individual window errors
            }
            return true;
        }, 0);

        // Track new/destroyed windows
        var currentHandles = new HashSet<nint>(windows.Select(w => w.Handle));
        var newHandles = currentHandles.Except(_trackedWindows).ToList();
        var removedHandles = _trackedWindows.Except(currentHandles).ToList();

        foreach (var handle in newHandles)
        {
            var info = windows.First(w => w.Handle == handle);
            WindowCreated?.Invoke(this, info);
        }

        foreach (var handle in removedHandles)
        {
            var info = new WindowInfo
            {
                Handle = handle,
                ProcessId = 0,
                ProcessName = "",
                Title = "",
                ClassName = "",
                Bounds = new Rect(0, 0, 0, 0),
                IsVisible = false,
                IsMinimized = false,
                IsMaximized = false,
                IsActive = false,
                IsStoreApp = false,
                Styles = WindowStyles.None,
                ExtendedStyles = ExtendedWindowStyles.None
            };
            WindowDestroyed?.Invoke(this, info);
        }

        _trackedWindows = currentHandles;

        return windows;
    }

    private WindowInfo? GetWindowInfo(nint hWnd)
    {
        try
        {
            if (!IsWindow(hWnd) || !IsWindowVisible(hWnd))
                return null;

            // Get title
            var titleLength = GetWindowTextLength(hWnd);
            var title = titleLength > 0 ? new string('\0', titleLength) : "";
            if (titleLength > 0)
            {
                GetWindowText(hWnd, title, titleLength + 1);
                title = title.TrimEnd('\0');
            }

            // Get class name
            var className = new string('\0', 256);
            GetClassName(hWnd, className, 256);
            className = className.TrimEnd('\0');

            // Get process ID
            GetWindowThreadProcessId(hWnd, out var processId);
            var processName = "";
            try
            {
                var process = Process.GetProcessById(processId);
                processName = process.ProcessName;
            }
            catch
            {
                // Process might have exited
            }

            // Get window rect
            GetWindowRect(hWnd, out var rect);
            var bounds = new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

            // Get styles
            var style = (WindowStyles)GetWindowLong(hWnd, GWL_STYLE);
            var exStyle = (ExtendedWindowStyles)GetWindowLong(hWnd, GWL_EXSTYLE);

            // Check if minimized/maximized
            var placement = new WINDOWPLACEMENT { length = Marshal.SizeOf<WINDOWPLACEMENT>() };
            GetWindowPlacement(hWnd, ref placement);
            var isMinimized = placement.showCmd == SW_SHOWMINIMIZED;
            var isMaximized = placement.showCmd == SW_SHOWMAXIMIZED;

            // Check if active
            var isActive = GetForegroundWindow() == hWnd;

            // Check if Store app
            var isStoreApp = IsStoreApp(hWnd, className);
            var appUserModelId = isStoreApp ? GetAppUserModelId(hWnd) : null;

            return new WindowInfo
            {
                Handle = hWnd,
                ProcessId = processId,
                ProcessName = processName,
                Title = title,
                ClassName = className,
                Bounds = bounds,
                IsVisible = true,
                IsMinimized = isMinimized,
                IsMaximized = isMaximized,
                IsActive = isActive,
                IsStoreApp = isStoreApp,
                AppUserModelId = appUserModelId,
                Styles = style,
                ExtendedStyles = exStyle
            };
        }
        catch
        {
            return null;
        }
    }

    private bool ShouldTrackWindow(WindowInfo info)
    {
        // Skip tiny windows, tooltips, etc.
        if (info.Bounds.Width < 50 || info.Bounds.Height < 50)
            return false;

        // Skip known system window classes
        var excludedClasses = new[]
        {
            "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "Button", "Static",
            "Edit", "ComboBox", "ListBox", "ScrollBar", "msctls_trackbar32",
            "msctls_progress32", "SysTreeView32", "SysListView32", "SysTabControl32",
            "ToolbarWindow32", "RebarWindow32", "MSTaskSwWClass", "TaskSwitcherWnd",
            "Windows.UI.Core.CoreWindow", "ApplicationFrameWindow"
        };

        if (excludedClasses.Contains(info.ClassName))
            return false;

        // Skip windows with no title and not store apps
        if (string.IsNullOrEmpty(info.Title) && !info.IsStoreApp)
            return false;

        return true;
    }

    private bool IsStoreApp(nint hWnd, string className)
    {
        if (className == "ApplicationFrameWindow" || className == "Windows.UI.Core.CoreWindow")
            return true;

        // Check for AppUserModelId
        var appUserModelId = GetAppUserModelId(hWnd);
        return !string.IsNullOrEmpty(appUserModelId);
    }

    private string? GetAppUserModelId(nint hWnd)
    {
        try
        {
            var propertyStore = GetPropertyStore(hWnd);
            if (propertyStore != null)
            {
                var propVariant = new PROPVARIANT();
                var propertyKey = new PropertyKey { fmtid = PKEY_AppUserModel_ID, pid = 5 };
                var hr = propertyStore.GetValue(ref propertyKey, ref propVariant);
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

    private void PollWindows(object? state)
    {
        try
        {
            var windows = ScanWindowsAsync();
            // Check for visibility changes
            foreach (var window in windows)
            {
                // Could track visibility changes here
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error polling windows");
        }
    }

    private bool RegisterShellHook()
    {
        try
        {
            _shellHookHandle = RegisterShellHookWindow(IntPtr.Zero);
            return _shellHookHandle != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    // Win32 P/Invoke
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int SW_MINIMIZE = 6;
    private const int SW_MAXIMIZE = 3;
    private const int SW_RESTORE = 9;
    private const int SW_SHOWMINIMIZED = 2;
    private const int SW_SHOWMAXIMIZED = 3;
    private const int WM_CLOSE = 0x0010;

    private static Guid PKEY_AppUserModel_ID = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public int showCmd;
        public System.Drawing.Point ptMinPosition;
        public System.Drawing.Point ptMaxPosition;
        public RECT rcNormalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROPVARIANT
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public nint pwszVal;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, string lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint hWnd, string lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern nint GetWindowThreadProcessId(nint hWnd, out int lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern nint GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool GetWindowPlacement(nint hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, int Msg, nint wParam, nint lParam);

    [DllImport("shell32.dll")]
    private static extern nint RegisterShellHookWindow(nint hWnd);

    [DllImport("shell32.dll")]
    private static extern bool DeregisterShellHookWindow(nint hWnd);

    [DllImport("shell32.dll")]
    private static extern int SHGetPropertyStoreForWindow(nint hwnd, ref Guid riid, out nint ppv);

    private IPropertyStore? GetPropertyStore(nint hWnd)
    {
        var guid = typeof(IPropertyStore).GUID;
        var hr = SHGetPropertyStoreForWindow(hWnd, ref guid, out var ppv);
        if (hr == 0 && ppv != 0)
        {
            return Marshal.GetObjectForIUnknown(ppv) as IPropertyStore;
        }
        return null;
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

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _pollTimer?.Dispose();
        if (_useShellHook && _shellHookHandle != 0)
        {
            DeregisterShellHookWindow(_shellHookHandle);
        }
    }
}