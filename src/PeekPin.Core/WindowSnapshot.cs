namespace PeekPin;

public sealed record WindowSnapshot(
    WindowId Id,
    int ProcessId,
    string ProcessName,
    string ClassName,
    string Title,
    bool IsVisible,
    bool IsCloaked,
    bool IsToolWindow,
    bool IsChild);

public static class WindowCatalogFilter
{
    public static bool Include(WindowSnapshot window, int ownProcessId)
    {
        if (window.ProcessId == ownProcessId)
        {
            return false;
        }

        if (!window.IsVisible || window.IsCloaked || window.IsToolWindow || window.IsChild)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(window.Title);
    }
}
