using System.Runtime.InteropServices;

namespace PeekPin;

public sealed record DisplayMonitor(string DeviceName, PixelRect Bounds, PixelRect WorkArea, int DpiX);

public static class MonitorLayout
{
    public static IReadOnlyList<DisplayMonitor> All()
    {
        var list = new List<DisplayMonitor>();
        NativeMethods.EnumWindowsProc? unused = null;
        GC.KeepAlive(unused);
        MonitorEnum((hMonitor, _, _, _) =>
        {
            var info = new NativeMethods.MonitorInfoEx
            {
                Size = Marshal.SizeOf<NativeMethods.MonitorInfoEx>()
            };
            if (!NativeMethods.GetMonitorInfo(hMonitor, ref info))
            {
                return true;
            }

            _ = NativeMethods.GetDpiForMonitor(hMonitor, 0, out var dpiX, out _);
            if (dpiX == 0)
            {
                dpiX = 96;
            }

            list.Add(new DisplayMonitor(
                info.DeviceName,
                ToRect(info.Monitor),
                ToRect(info.Work),
                (int)dpiX));
            return true;
        });
        return list;
    }

    public static DisplayMonitor FromPoint(PixelPoint screenPoint)
    {
        var monitors = All();
        foreach (var monitor in monitors)
        {
            if (monitor.Bounds.Contains(screenPoint) || monitor.WorkArea.Contains(screenPoint))
            {
                return monitor;
            }
        }

        return monitors.Count > 0
            ? monitors[0]
            : new DisplayMonitor(@"\\.\DISPLAY1", new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), 96);
    }

    public static DisplayMonitor FromDeviceOrPoint(string deviceName, PixelPoint screenPoint)
    {
        var match = All().FirstOrDefault(monitor =>
            string.Equals(monitor.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
        return match ?? FromPoint(screenPoint);
    }

    public static PixelPoint Cursor()
    {
        return NativeMethods.GetCursorPos(out var point)
            ? new PixelPoint(point.X, point.Y)
            : default;
    }

    private static PixelRect ToRect(NativeMethods.WinRect rect)
    {
        return new PixelRect(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr dwData);

    private static void MonitorEnum(MonitorEnumProc proc)
    {
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero);
        GC.KeepAlive(proc);
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
}
