namespace PeekPin;

public static class FullscreenClassifier
{
    public static bool IsShellDesktop(string? className)
    {
        return className is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd";
    }

    public static bool IsBorderlessFullscreen(PixelRect window, PixelRect monitor, bool hasCaption, int tolerancePx = 2)
    {
        if (hasCaption || window.Width <= 0 || window.Height <= 0)
        {
            return false;
        }

        return Math.Abs(window.X - monitor.X) <= tolerancePx
            && Math.Abs(window.Y - monitor.Y) <= tolerancePx
            && Math.Abs(window.Right - monitor.Right) <= tolerancePx
            && Math.Abs(window.Bottom - monitor.Bottom) <= tolerancePx;
    }
}
