namespace PeekPin;

public interface IWindowGateway
{
    IReadOnlyList<WindowSnapshot> ListTopLevel(int ownProcessId);

    bool TrySetTopmost(WindowId id, bool enabled);

    void ShowNoActivate(WindowId id);

    void MinimizeNoActivate(WindowId id);

    void Activate(WindowId id);

    PixelRect GetRect(WindowId id);

    bool IsMinimized(WindowId id);

    bool IsAlive(WindowId id);

    WindowId GetForeground();

    bool IsFullscreenForeground();

    bool TryGetIntegrityBlocked(WindowId id);

    byte[]? TryGetIconPng(WindowId id);
}
