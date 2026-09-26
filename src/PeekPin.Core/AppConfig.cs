namespace PeekPin;

public sealed class AppConfig
{
    public const int MaxTargets = 8;
    public const int MinHoverDelayMs = 100;
    public const int MaxHoverDelayMs = 3000;
    public const int MinHideDelayMs = 50;
    public const int MaxHideDelayMs = 2000;

    public int Version { get; set; } = 1;

    public int HoverDelayMs { get; set; } = 500;

    public int HideDelayMs { get; set; } = 250;

    public int IconSize { get; set; } = 48;

    public bool StartWithWindows { get; set; }

    public bool HideIconWhenFullscreen { get; set; } = true;

    public bool RememberIconPosition { get; set; } = true;

    public string Language { get; set; } = "vi";

    public List<TargetConfig> Targets { get; set; } = [];

    public static AppConfig CreateDefault() => new();
}
