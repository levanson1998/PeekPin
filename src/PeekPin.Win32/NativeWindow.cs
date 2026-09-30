namespace PeekPin;

public static class NativeWindow
{
    public static void KeepTopmost(nint hwnd)
    {
        if (hwnd == 0)
        {
            return;
        }

        var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlExStyle);
        if ((style & NativeMethods.WsExTopmost) == 0)
        {
            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GwlExStyle, style | NativeMethods.WsExTopmost);
        }

        NativeMethods.SetWindowPos(
            hwnd,
            NativeMethods.HwndTopmost,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate | NativeMethods.SwpNoOwnerZOrder);
    }

    public static void MoveTopmostNoActivate(nint hwnd, int x, int y, int width, int height)
    {
        NativeMethods.SetWindowPos(
            hwnd,
            NativeMethods.HwndTopmost,
            x,
            y,
            width,
            height,
            NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
    }

    public static void AddToolStyles(nint hwnd)
    {
        var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlExStyle);
        style |= NativeMethods.WsExNoActivate | NativeMethods.WsExToolWindow;
        style &= ~NativeMethods.WsExAppWindow;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GwlExStyle, style);
    }

    public static PixelRect GetRect(nint hwnd)
    {
        if (!NativeMethods.GetWindowRect(hwnd, out var rect))
        {
            return default;
        }

        return new PixelRect(rect.Left, rect.Top, Math.Max(0, rect.Width), Math.Max(0, rect.Height));
    }

    public static PixelPoint Cursor() => MonitorLayout.Cursor();

    public static void MoveCursor(PixelPoint point) => NativeMethods.SetCursorPos(point.X, point.Y);

    public static nint WindowFromPoint(PixelPoint point)
    {
        return NativeMethods.WindowFromPoint(new NativeMethods.WinPoint { X = point.X, Y = point.Y });
    }

    public static bool IsTopmost(WindowId id) => WindowGateway.HasTopmostStyle(id);
}
