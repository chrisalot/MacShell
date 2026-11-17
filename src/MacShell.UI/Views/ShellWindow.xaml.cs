using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.System;
using Windows.UI.Core;
using MacShell.Core.Interfaces;
using MacShell.Core.Models;
using MacShell.Core.Services;

namespace MacShell.UI.Views;

/// <summary>
/// Main shell window - full-screen transparent overlay hosting Dock, Top Bar, and Launcher.
/// </summary>
public sealed partial class ShellWindow : Window
{
    private readonly DockManager _dockManager;
    private readonly IAppLauncher _appLauncher;
    private readonly ISystemService _systemService;
    private readonly IWindowManager _windowManager;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly GlobalHotkeyManager _hotkeyManager;

    private ObservableCollection<DockItem> _dockItems = new();
    private bool _isLauncherOpen;
    private IntPtr _hwnd;

    public ShellWindow(
        DockManager dockManager,
        IAppLauncher appLauncher,
        ISystemService systemService,
        IWindowManager windowManager)
    {
        _dockManager = dockManager;
        _appLauncher = appLauncher;
        _systemService = systemService;
        _windowManager = windowManager;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _hotkeyManager = new GlobalHotkeyManager();

        InitializeComponent();

        // Bind data
        DockItems = _dockItems;
        SystemService = _systemService;
        AppLauncher = _appLauncher;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #region Public Properties

    public ObservableCollection<DockItem> DockItems
    {
        get => _dockItems;
        private set => _dockItems = value;
    }

    public ISystemService SystemService { get; }
    public IAppLauncher AppLauncher { get; }

    public bool IsLauncherOpen
    {
        get => _isLauncherOpen;
        set
        {
            _isLauncherOpen = value;
            // Property changed notification would go here if we implemented INotifyPropertyChanged
        }
    }

    #endregion

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get window handle
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // Configure window for desktop overlay behavior
            ConfigureWindow();

            // Initialize services
            await _dockManager.LoadAsync();
            _systemService.StartMonitoring();
            _windowManager.StartMonitoring();

            // Sync dock items
            SyncDockItems();

            // Subscribe to dock changes
            _dockManager.ItemsChanged += OnDockItemsChanged;
            _dockManager.ItemRunningStateChanged += OnItemRunningStateChanged;

            // Register global hotkeys
            RegisterHotkeys();

            // Start app launcher index
            _ = _appLauncher.RefreshIndexAsync();

            System.Diagnostics.Debug.WriteLine("MacShell loaded successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading MacShell: {ex.Message}");
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _dockManager.ItemsChanged -= OnDockItemsChanged;
        _dockManager.ItemRunningStateChanged -= OnItemRunningStateChanged;
        _hotkeyManager.UnregisterAll();
        _systemService.StopMonitoring();
        _windowManager.StopMonitoring();
    }

    private void ConfigureWindow()
    {
        if (_hwnd == IntPtr.Zero) return;

        // Get AppWindow for advanced configuration
        var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        if (appWindow != null)
        {
            // Set as overlay window (always on bottom, click-through except for interactive elements)
            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.SetBorderAndTitleBar(false, false);
            }

            // Position to cover entire screen
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
            if (displayArea != null)
            {
                var workArea = displayArea.WorkArea;
                appWindow.MoveAndResize(workArea);
            }

            // Set Z-order to bottom (behind other windows but above desktop)
            SetWindowPos(_hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

            // Enable click-through for non-interactive areas
            // This is handled by IsHitTestVisible=False on ContentArea in XAML
        }
    }

    private void SyncDockItems()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            _dockItems.Clear();
            foreach (var item in _dockManager.Items.OrderBy(i => i.Order))
            {
                _dockItems.Add(item);
            }
        });
    }

    private void OnDockItemsChanged(object? sender, EventArgs e)
    {
        SyncDockItems();
    }

    private void OnItemRunningStateChanged(object? sender, MacShell.Core.Services.DockItemRunningStateChangedEventArgs e)
    {
        // The UI will update automatically via data binding
        // But we might want to animate the running indicator appearance
        _dispatcherQueue.TryEnqueue(() =>
        {
            // Find the DockItemControl and trigger animation
            // This would require walking the visual tree
        });
    }

    private void RegisterHotkeys()
    {
        // Win+Space for launcher
        _hotkeyManager.RegisterHotkey(HotkeyId.Launcher, VirtualKey.Space, VirtualKeyModifiers.Windows);

        // Win+, for settings
        _hotkeyManager.RegisterHotkey(HotkeyId.Settings, VirtualKey.Comma, VirtualKeyModifiers.Windows);

        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
    }

    private void OnHotkeyPressed(object? sender, HotkeyPressedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            switch (e.HotkeyId)
            {
                case HotkeyId.Launcher:
                    ToggleLauncher();
                    break;
                case HotkeyId.Settings:
                    OpenSettings();
                    break;
            }
        });
    }

    private void ToggleLauncher()
    {
        IsLauncherOpen = !IsLauncherOpen;
        LauncherWindow.IsOpen = IsLauncherOpen;
    }

    private void OnLauncherOpened(object? sender, EventArgs e)
    {
        IsLauncherOpen = true;
    }

    private void OnLauncherClosed(object? sender, EventArgs e)
    {
        IsLauncherOpen = false;
    }

    private void OnAppLaunched(object? sender, AppInfo e)
    {
        // App launched successfully
    }

    private void OnDockItemClick(object? sender, DockItem e)
    {
        if (e.IsRunning && e.WindowHandle != IntPtr.Zero)
        {
            // Activate existing window
            _ = _windowManager.ActivateWindowAsync(e.WindowHandle);
        }
        else
        {
            // Launch new instance
            _ = _dockManager.LaunchItemAsync(e.Id);
        }
    }

    private void OnDockItemRightClick(object? sender, DockItem e)
    {
        // Context menu is handled by DockItemControl
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        OpenSettings();
    }

    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow(_dockManager, _appLauncher, _systemService, _windowManager);
        settingsWindow.Activate();
    }

    // Win32 Interop
    private const int HWND_BOTTOM = 1;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static class Win32Interop
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowIdFromWindow(IntPtr hWnd);
    }
}

/// <summary>
/// Global hotkey manager using RegisterHotKey.
/// </summary>
internal sealed class GlobalHotkeyManager
{
    private const int WM_HOTKEY = 0x0312;
    private readonly IntPtr _hwnd;
    private readonly Dictionary<HotkeyId, int> _hotkeys = new();
    private int _nextId = 1;

    public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    public GlobalHotkeyManager()
    {
        // Create a hidden message window for hotkey messages
        _hwnd = CreateMessageWindow();
    }

    public void RegisterHotkey(HotkeyId id, VirtualKey key, VirtualKeyModifiers modifiers)
    {
        var hotkeyId = _nextId++;
        var vk = (uint)key;
        var mod = (uint)modifiers;

        if (RegisterHotKey(_hwnd, hotkeyId, mod, vk))
        {
            _hotkeys[id] = hotkeyId;
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _hotkeys.Values)
        {
            UnregisterHotKey(_hwnd, id);
        }
        _hotkeys.Clear();
        DestroyWindow(_hwnd);
    }

    private IntPtr CreateMessageWindow()
    {
        var wc = new WNDCLASS
        {
            lpfnWndProc = WndProc,
            hInstance = GetModuleHandle(null),
            lpszClassName = "MacShellHotkeyWindow"
        };
        RegisterClass(ref wc);
        return CreateWindowEx(0, "MacShellHotkeyWindow", "", 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);
    }

    private static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            // Find which hotkey this is
            // Note: In a real implementation, we'd need a static reference to the manager
        }
        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    // Win32 P/Invoke
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASS
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}

internal enum HotkeyId
{
    Launcher,
    Settings
}

internal sealed class HotkeyPressedEventArgs : EventArgs
{
    public required HotkeyId HotkeyId { get; init; }
}