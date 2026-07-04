using System.Runtime.InteropServices;
using System.Text;

namespace Winotch;

public static class ForegroundWindowService
{
    public static ShellMode DetectShellMode()
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            return ShellMode.Mini;
        }

        GetWindowThreadProcessId(foreground, out var processId);
        var isOwnWindow = processId == Environment.ProcessId;
        var className = GetClassName(foreground);
        var isShell = className is "Progman" or "WorkerW" or "Shell_TrayWnd";

        if (!GetWindowRect(foreground, out var windowRect) ||
            !TryGetMonitorRect(foreground, out var monitorRect))
        {
            return ShellMode.Mini;
        }

        var placement = new WindowPlacement { Length = Marshal.SizeOf<WindowPlacement>() };
        var isMaximized = GetWindowPlacement(foreground, ref placement) && placement.ShowCmd == 3;
        return DecideMode(isOwnWindow, isShell, isMaximized, windowRect, monitorRect);
    }

    public static ShellMode DecideMode(bool isOwnWindow, bool isShell, bool isMaximized, NativeRect windowRect, NativeRect monitorRect)
    {
        if (isOwnWindow || isShell)
        {
            return ShellMode.Mini;
        }

        var widthCoverage = (double)windowRect.Width / Math.Max(1, monitorRect.Width);
        var heightCoverage = (double)windowRect.Height / Math.Max(1, monitorRect.Height);
        var coversTop = windowRect.Top <= monitorRect.Top + 8;
        var fillsScreen = widthCoverage >= 0.9 && heightCoverage >= 0.78 && coversTop;

        return isMaximized || fillsScreen ? ShellMode.FullBar : ShellMode.Mini;
    }

    private static bool TryGetMonitorRect(IntPtr window, out NativeRect rect)
    {
        var monitor = MonitorFromWindow(window, 2);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info))
        {
            rect = default;
            return false;
        }

        rect = info.Monitor;
        return true;
    }

    private static string GetClassName(IntPtr window)
    {
        var builder = new StringBuilder(256);
        var length = GetClassName(window, builder, builder.Capacity);
        return length == 0 ? "" : builder.ToString();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder className, int maxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll")]
    private static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement placement);
}

[StructLayout(LayoutKind.Sequential)]
public struct NativeRect
{
    public NativeRect(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MonitorInfo
{
    public int Size;
    public NativeRect Monitor;
    public NativeRect WorkArea;
    public uint Flags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WindowPlacement
{
    public int Length;
    public int Flags;
    public int ShowCmd;
    public System.Drawing.Point MinPosition;
    public System.Drawing.Point MaxPosition;
    public NativeRect NormalPosition;
}
