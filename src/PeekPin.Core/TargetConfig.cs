namespace PeekPin;

public sealed class TargetConfig
{
    public string ProcessName { get; set; } = "";

    public string ClassName { get; set; } = "";

    public string Title { get; set; } = "";

    public string MonitorDevice { get; set; } = "";

    public int IconX { get; set; }

    public int IconY { get; set; }

    public WindowIdentity Identity => new(ProcessName, ClassName, Title);
}
