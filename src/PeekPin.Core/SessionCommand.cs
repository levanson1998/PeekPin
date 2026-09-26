namespace PeekPin;

public enum SessionCommandKind
{
    ShowNoActivate,
    MinimizeNoActivate,
    Activate,
    SetTopmost,
    ClearTopmost,
    ShowFloatIcon,
    HideFloatIcon,
    CloseFloatIcon,
    MarkInaccessible
}

public readonly record struct SessionCommand(SessionCommandKind Kind)
{
    public static SessionCommand ShowNoActivate { get; } = new(SessionCommandKind.ShowNoActivate);
    public static SessionCommand MinimizeNoActivate { get; } = new(SessionCommandKind.MinimizeNoActivate);
    public static SessionCommand Activate { get; } = new(SessionCommandKind.Activate);
    public static SessionCommand SetTopmost { get; } = new(SessionCommandKind.SetTopmost);
    public static SessionCommand ClearTopmost { get; } = new(SessionCommandKind.ClearTopmost);
    public static SessionCommand ShowFloatIcon { get; } = new(SessionCommandKind.ShowFloatIcon);
    public static SessionCommand HideFloatIcon { get; } = new(SessionCommandKind.HideFloatIcon);
    public static SessionCommand CloseFloatIcon { get; } = new(SessionCommandKind.CloseFloatIcon);
    public static SessionCommand MarkInaccessible { get; } = new(SessionCommandKind.MarkInaccessible);
}
