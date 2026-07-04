using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Winotch;

public static class WindowChromeInterop
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;

    public static void SetMouseTransparent(Window window, bool enabled)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var style = GetWindowLong(handle, GwlExStyle);
        var next = enabled ? style | WsExTransparent : style & ~WsExTransparent;
        if (next != style)
        {
            SetWindowLong(handle, GwlExStyle, next);
        }
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int index, int newStyle);
}
