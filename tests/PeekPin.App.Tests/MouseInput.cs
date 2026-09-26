using System.Runtime.InteropServices;

namespace PeekPin.App.Tests;

internal static class MouseInput
{
    private const uint LeftDown = 0x0002;
    private const uint LeftUp = 0x0004;

    public static void Move(int x, int y) => NativeWindow.MoveCursor(new PixelPoint(x, y));

    public static void Click(int x, int y)
    {
        Move(x, y);
        mouse_event(LeftDown, 0, 0, 0, 0);
        mouse_event(LeftUp, 0, 0, 0, 0);
    }

    public static void Drag(int x1, int y1, int x2, int y2)
    {
        Move(x1, y1);
        mouse_event(LeftDown, 0, 0, 0, 0);
        Move(x2, y2);
        mouse_event(LeftUp, 0, 0, 0, 0);
    }

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, nint dwExtraInfo);
}
