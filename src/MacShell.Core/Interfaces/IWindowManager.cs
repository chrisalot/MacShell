using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MacShell.Core.Interfaces;

/// <summary>
/// Window management service for interacting with Windows windows.
/// </summary>
public interface IWindowManager
{
    /// <summary>
    /// Gets all visible windows.
    /// </summary>
    Task<IReadOnlyList<WindowInfo>> GetVisibleWindowsAsync();

    /// <summary>
    /// Gets the window info for a specific process ID.
    /// </summary>
    Task<WindowInfo?> GetWindowByProcessIdAsync(int processId);

    /// <summary>
    /// Gets the window info for a specific window handle.
    /// </summary>
    Task<WindowInfo?> GetWindowByHandleAsync(nint handle);

    /// <summary>
    /// Activates (brings to front) a window.
    /// </summary>
    Task<bool> ActivateWindowAsync(nint handle);

    /// <summary>
    /// Minimizes a window.
    /// </summary>
    Task<bool> MinimizeWindowAsync(nint handle);

    /// <summary>
    /// Maximizes a window.
    /// </summary>
    Task<bool> MaximizeWindowAsync(nint handle);

    /// <summary>
    /// Restores a minimized/maximized window.
    /// </summary>
    Task<bool> RestoreWindowAsync(nint handle);

    /// <summary>
    /// Closes a window gracefully.
    /// </summary>
    Task<bool> CloseWindowAsync(nint handle);

    /// <summary>
    /// Gets the process ID for a window handle.
    /// </summary>
    Task<int?> GetProcessIdAsync(nint handle);

    /// <summary>
    /// Event raised when a window is created.
    /// </summary>
    event EventHandler<WindowInfo> WindowCreated;

    /// <summary>
    /// Event raised when a window is destroyed.
    /// </summary>
    event EventHandler<WindowInfo> WindowDestroyed;

    /// <summary>
    /// Event raised when a window's visibility changes.
    /// </summary>
    event EventHandler<WindowVisibilityChangedEventArgs> WindowVisibilityChanged;

    /// <summary>
    /// Event raised when the active window changes.
    /// </summary>
    event EventHandler<WindowInfo?> ActiveWindowChanged;

    /// <summary>
    /// Starts monitoring window events.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops monitoring window events.
    /// </summary>
    void StopMonitoring();
}

/// <summary>
/// Information about a window.
/// </summary>
public sealed record WindowInfo
{
    /// <summary>
    /// Window handle (HWND).
    /// </summary>
    public required nint Handle { get; init; }

    /// <summary>
    /// Process ID that owns this window.
    /// </summary>
    public required int ProcessId { get; init; }

    /// <summary>
    /// Process name.
    /// </summary>
    public required string ProcessName { get; init; }

    /// <summary>
    /// Window title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Window class name.
    /// </summary>
    public required string ClassName { get; init; }

    /// <summary>
    /// Window rectangle in screen coordinates.
    /// </summary>
    public required Rect Bounds { get; init; }

    /// <summary>
    /// Whether the window is visible.
    /// </summary>
    public required bool IsVisible { get; init; }

    /// <summary>
    /// Whether the window is minimized.
    /// </summary>
    public required bool IsMinimized { get; init; }

    /// <summary>
    /// Whether the window is maximized.
    /// </summary>
    public required bool IsMaximized { get; init; }

    /// <summary>
    /// Whether the window is the active/foreground window.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Whether this is a Store/UWP app window.
    /// </summary>
    public required bool IsStoreApp { get; init; }

    /// <summary>
    /// Application User Model ID (for Store apps).
    /// </summary>
    public string? AppUserModelId { get; init; }

    /// <summary>
    /// Window styles.
    /// </summary>
    public required WindowStyles Styles { get; init; }

    /// <summary>
    /// Extended window styles.
    /// </summary>
    public required ExtendedWindowStyles ExtendedStyles { get; init; }
}

/// <summary>
/// Window styles (GWL_STYLE).
/// </summary>
[Flags]
public enum WindowStyles : uint
{
    None = 0,
    Overlapped = 0x00000000,
    Popup = 0x80000000,
    Child = 0x40000000,
    Minimize = 0x20000000,
    Visible = 0x10000000,
    Disabled = 0x08000000,
    ClipSiblings = 0x04000000,
    ClipChildren = 0x02000000,
    Maximize = 0x01000000,
    Caption = 0x00C00000,
    Border = 0x00800000,
    DlgFrame = 0x00400000,
    VScroll = 0x00200000,
    HScroll = 0x00100000,
    SysMenu = 0x00080000,
    ThickFrame = 0x00040000,
    Group = 0x00020000,
    TabStop = 0x00010000,
    MinimizeBox = 0x00020000,
    MaximizeBox = 0x00010000,
}

/// <summary>
/// Extended window styles (GWL_EXSTYLE).
/// </summary>
[Flags]
public enum ExtendedWindowStyles : uint
{
    None = 0,
    DlgModalFrame = 0x00000001,
    NoParentNotify = 0x00000004,
    TopMost = 0x00000008,
    AcceptFiles = 0x00000010,
    Transparent = 0x00000020,
    MdiChild = 0x00000040,
    ToolWindow = 0x00000080,
    WindowEdge = 0x00000100,
    ClientEdge = 0x00000200,
    ContextHelp = 0x00000400,
    Right = 0x00001000,
    Left = 0x00000000,
    RtlReading = 0x00002000,
    LtrReading = 0x00000000,
    LeftScrollbar = 0x00004000,
    RightScrollbar = 0x00000000,
    ControlParent = 0x00010000,
    StaticEdge = 0x00020000,
    AppWindow = 0x00040000,
    OverlappedWindow = 0x00000000,
    PaletteWindow = 0x00000188,
    Layered = 0x00080000,
    NoInheritLayout = 0x00100000,
    NoRedirectionBitmap = 0x00200000,
    LayoutRtl = 0x00400000,
    Composited = 0x02000000,
    NoActivate = 0x08000000,
}

/// <summary>
/// Rectangle structure for window bounds.
/// </summary>
public readonly record struct Rect
{
    public readonly int X;
    public readonly int Y;
    public readonly int Width;
    public readonly int Height;

    public Rect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public override string ToString() => $"{X},{Y} {Width}x{Height}";
}

/// <summary>
/// Event arguments for window visibility changes.
/// </summary>
public sealed class WindowVisibilityChangedEventArgs : EventArgs
{
    public required WindowInfo Window { get; init; }
    public required bool IsVisible { get; init; }
}