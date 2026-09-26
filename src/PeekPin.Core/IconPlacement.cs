namespace PeekPin;

public static class IconPlacement
{
    public const int EdgeInset = 24;

    public static PixelPoint Clamp(PixelPoint point, PixelRect workArea, int iconSize)
    {
        var maxX = Math.Max(0, workArea.Width - iconSize);
        var maxY = Math.Max(0, workArea.Height - iconSize);
        return new PixelPoint(Math.Clamp(point.X, 0, maxX), Math.Clamp(point.Y, 0, maxY));
    }

    public static PixelPoint DefaultNearCursor(PixelRect workArea, int iconSize)
    {
        var point = new PixelPoint(workArea.Width - iconSize - EdgeInset, workArea.Height - iconSize - EdgeInset);
        return Clamp(point, workArea, iconSize);
    }

    public static PixelPoint Resolve(
        TargetConfig target,
        string currentMonitorDevice,
        PixelRect workArea,
        int iconSize)
    {
        if (!string.Equals(target.MonitorDevice, currentMonitorDevice, StringComparison.OrdinalIgnoreCase))
        {
            return DefaultNearCursor(workArea, iconSize);
        }

        return Clamp(new PixelPoint(target.IconX, target.IconY), workArea, iconSize);
    }
}
