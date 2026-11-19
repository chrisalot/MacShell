using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Windows.System;
using Windows.UI.Input;
using WinRT.Interop;
using MacShell.Core.Interfaces;
using MacShell.Core.Models;
using MacShell.Core.Services;
using MacShell.UI.Controls;
using MacShell.UI.Helpers;

namespace MacShell.UI.Views;

/// <summary>
/// Main shell window - full-screen transparent overlay hosting Dock, Top Bar, and Launcher.
/// </summary>
public sealed class ShellWindow : Window
{
    private readonly DockManager _dockManager;
    private readonly IAppLauncher _appLauncher;
    private readonly ISystemService _systemService;
    private readonly IWindowManager _windowManager;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly GlobalHotkeyManager _hotkeyManager;

    private ObservableCollection<IDockItem> _dockItems = new();
    private bool _isLauncherOpen;
    private IntPtr _hwnd;

    // Named XAML elements
    private Grid? _rootGrid;
    private Grid? _contentArea;
    private TopBar? _topBar;
    private Grid? _desktopArea;
    private DockPanel? _dockPanel;
    private LauncherWindow? _launcherWindow;
    private Button? _settingsButton;

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
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _hotkeyManager = new GlobalHotkeyManager();

        LoadXaml();
        SetupBindings();
        SetupEventHandlers();
    }

    #region Public Properties

    public ObservableCollection<IDockItem> DockItems
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
            if (_launcherWindow != null)
            {
                _launcherWindow.IsOpen = value;
            }
        }
    }

    #endregion

    private void LoadXaml()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? "";
        var xamlPath = Path.Combine(assemblyDir, "Views", "ShellWindow.xaml");

        if (!File.Exists(xamlPath))
        {
            throw new FileNotFoundException($"ShellWindow.xaml not found at {xamlPath}");
        }

        var xamlContent = File.ReadAllText(xamlPath);
        var window = (Window)XamlReader.Load(xamlContent);

        // Copy content from loaded window to this instance
        // Note: WinUI 3 Window doesn't have Background, Width, Height, etc. - those are on the root element
        this.Content = window.Content;

        // Find named elements from the loaded content
        if (this.Content is FrameworkElement root)
        {
            _rootGrid = root.FindName("RootGrid") as Grid;
            _contentArea = root.FindName("ContentArea") as Grid;
            _topBar = root.FindName("TopBar") as TopBar;
            _desktopArea = root.FindName("DesktopArea") as Grid;
            _dockPanel = root.FindName("DockPanel") as DockPanel;
            _launcherWindow = root.FindName("LauncherWindow") as LauncherWindow;
            _settingsButton = root.FindName("SettingsButton") as Button;

            // Subscribe to events on root element (Window doesn't have Loaded/Unloaded in WinUI 3)
            root.Loaded += OnLoaded;
            root.Unloaded += OnUnloaded;
        }
    }

    private void SetupBindings()
    {
        if (_topBar != null)
        {
            _topBar.SystemService = _systemService;
        }

        if (_dockPanel != null)
        {
            _dockPanel.ItemsSource = _dockItems;
        }

        if (_launcherWindow != null)
        {
            _launcherWindow.AppLauncher = _appLauncher;
        }
    }

    private void SetupEventHandlers()
    {
        if (_dockPanel != null)
        {
            _dockPanel.ItemClick += OnDockItemClick;
            _dockPanel.ItemRightClick += OnDockItemRightClick;
        }

        if (_launcherWindow != null)
        {
            _launcherWindow.Opened += OnLauncherOpened;
            _launcherWindow.Closed += OnLauncherClosed;
            _launcherWindow.AppLaunched += OnAppLaunched;
        }

        if (_settingsButton != null)
        {
            _settingsButton.Click += OnSettingsClick;
        }
    }

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
        var windowIdPtr = Win32Interop.GetWindowIdFromWindow(_hwnd);
        var windowId = new Microsoft.UI.WindowId((ulong)windowIdPtr);
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
    }

    private void RegisterHotkeys()
    {
        // Win+Space for launcher
        _hotkeyManager.RegisterHotkey(HotkeyId.Launcher, VirtualKey.Space, VirtualKeyModifiers.Windows);

        // Win+, for settings (VK_OEM_COMMA = 0xBC)
        _hotkeyManager.RegisterHotkey(HotkeyId.Settings, (VirtualKey)0xBC, VirtualKeyModifiers.Windows);

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

    private void OnDockItemClick(object? sender, IDockItem e)
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

    private void OnDockItemRightClick(object? sender, IDockItem e)
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
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
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

    // Static delegate for WndProc to avoid GC issues
    private static readonly WndProcDelegate _wndProcDelegate = WndProc;

    private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

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