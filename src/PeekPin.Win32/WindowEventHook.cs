namespace PeekPin;

public sealed class WindowEventHook : IDisposable
{
    private readonly NativeMethods.WinEventDelegate _callback;
    private readonly List<IntPtr> _hooks = [];
    private bool _disposed;

    public WindowEventHook()
    {
        _callback = OnEvent;
        Hook(NativeMethods.EventSystemForeground);
        Hook(NativeMethods.EventSystemMinimizeStart);
        Hook(NativeMethods.EventSystemMinimizeEnd);
        Hook(NativeMethods.EventObjectDestroy);
        Hook(NativeMethods.EventObjectShow);
    }

    public event Action<WindowId>? Minimized;

    public event Action<WindowId>? Restored;

    public event Action<WindowId>? Destroyed;

    public event Action<WindowId>? ForegroundChanged;

    public event Action<WindowId>? Shown;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var hook in _hooks)
        {
            if (hook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(hook);
            }
        }

        _hooks.Clear();
        GC.KeepAlive(_callback);
    }

    private void Hook(uint eventType)
    {
        var hook = NativeMethods.SetWinEventHook(
            eventType,
            eventType,
            IntPtr.Zero,
            _callback,
            0,
            0,
            NativeMethods.WineventOutOfContext | NativeMethods.WineventSkipOwnProcess);
        _hooks.Add(hook);
    }

    private void OnEvent(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
    {
        if (idObject != NativeMethods.ObjidWindow || hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
        {
            if (eventType == NativeMethods.EventObjectDestroy && hwnd != IntPtr.Zero && idObject == NativeMethods.ObjidWindow)
            {
                Destroyed?.Invoke(new WindowId(hwnd));
            }

            return;
        }

        var id = new WindowId(hwnd);
        switch (eventType)
        {
            case NativeMethods.EventSystemMinimizeStart:
                Minimized?.Invoke(id);
                break;
            case NativeMethods.EventSystemMinimizeEnd:
                Restored?.Invoke(id);
                break;
            case NativeMethods.EventObjectDestroy:
                Destroyed?.Invoke(id);
                break;
            case NativeMethods.EventSystemForeground:
                ForegroundChanged?.Invoke(id);
                break;
            case NativeMethods.EventObjectShow:
                Shown?.Invoke(id);
                break;
        }
    }
}
