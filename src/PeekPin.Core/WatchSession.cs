namespace PeekPin;

public sealed class WatchSession
{
    private static readonly IReadOnlyList<SessionCommand> None = Array.Empty<SessionCommand>();

    public WatchSession(TargetConfig target)
    {
        Target = target;
    }

    public TargetConfig Target { get; }

    public SessionPhase Phase { get; private set; } = SessionPhase.Unbound;

    public bool TopmostApplied { get; private set; }

    public bool IsPaused { get; private set; }

    public WindowId Window { get; private set; }

    public IReadOnlyList<SessionCommand> Select(WindowId window)
    {
        Window = window;
        Phase = SessionPhase.Visible;
        if (IsPaused)
        {
            TopmostApplied = false;
            return None;
        }

        TopmostApplied = true;
        return new[] { SessionCommand.SetTopmost };
    }

    public IReadOnlyList<SessionCommand> Bind(WindowId window)
    {
        Window = window;
        if (Phase == SessionPhase.Unbound)
        {
            Phase = SessionPhase.Visible;
        }

        return CommandsForCurrentPhase();
    }

    public IReadOnlyList<SessionCommand> NotifyMinimized()
    {
        if (Phase is not (SessionPhase.Visible or SessionPhase.Peeking))
        {
            return None;
        }

        Phase = SessionPhase.Docked;
        TopmostApplied = false;
        if (IsPaused)
        {
            return new[] { SessionCommand.ClearTopmost };
        }

        return new[]
        {
            SessionCommand.MinimizeNoActivate,
            SessionCommand.ShowFloatIcon,
            SessionCommand.ClearTopmost
        };
    }

    public IReadOnlyList<SessionCommand> BeginPeek()
    {
        if (Phase != SessionPhase.Docked || IsPaused)
        {
            return None;
        }

        Phase = SessionPhase.Peeking;
        TopmostApplied = true;
        return new[] { SessionCommand.ShowNoActivate, SessionCommand.SetTopmost };
    }

    public IReadOnlyList<SessionCommand> EndPeek()
    {
        if (Phase != SessionPhase.Peeking || IsPaused)
        {
            return None;
        }

        Phase = SessionPhase.Docked;
        TopmostApplied = false;
        return new[] { SessionCommand.MinimizeNoActivate };
    }

    public IReadOnlyList<SessionCommand> Pin()
    {
        if (Phase is not (SessionPhase.Peeking or SessionPhase.Docked) || IsPaused)
        {
            return None;
        }

        Phase = SessionPhase.Visible;
        TopmostApplied = true;
        return new[] { SessionCommand.Activate, SessionCommand.SetTopmost, SessionCommand.HideFloatIcon };
    }

    public IReadOnlyList<SessionCommand> Destroyed()
    {
        Phase = SessionPhase.Unbound;
        Window = default;
        TopmostApplied = false;
        return new[] { SessionCommand.CloseFloatIcon };
    }

    public IReadOnlyList<SessionCommand> AccessDenied()
    {
        Phase = SessionPhase.Inaccessible;
        TopmostApplied = false;
        return new[] { SessionCommand.ClearTopmost, SessionCommand.HideFloatIcon, SessionCommand.MarkInaccessible };
    }

    public IReadOnlyList<SessionCommand> Pause()
    {
        IsPaused = true;
        TopmostApplied = false;
        return new[] { SessionCommand.HideFloatIcon, SessionCommand.ClearTopmost };
    }

    public IReadOnlyList<SessionCommand> Resume()
    {
        IsPaused = false;
        return CommandsForCurrentPhase();
    }

    public IReadOnlyList<SessionCommand> StopTracking()
    {
        Phase = SessionPhase.Unbound;
        TopmostApplied = false;
        return new[] { SessionCommand.ClearTopmost, SessionCommand.CloseFloatIcon };
    }

    public IReadOnlyList<SessionCommand> Reapply() => CommandsForCurrentPhase();

    public IReadOnlyList<SessionCommand> Quit()
    {
        var commands = new List<SessionCommand>();
        if (Phase == SessionPhase.Peeking)
        {
            commands.Add(SessionCommand.MinimizeNoActivate);
        }

        commands.Add(SessionCommand.ClearTopmost);
        TopmostApplied = false;
        return commands;
    }

    private IReadOnlyList<SessionCommand> CommandsForCurrentPhase()
    {
        if (IsPaused)
        {
            TopmostApplied = false;
            return None;
        }

        switch (Phase)
        {
            case SessionPhase.Visible:
                TopmostApplied = true;
                return new[] { SessionCommand.SetTopmost, SessionCommand.HideFloatIcon };
            case SessionPhase.Docked:
                TopmostApplied = false;
                return new[] { SessionCommand.ShowFloatIcon, SessionCommand.ClearTopmost };
            case SessionPhase.Peeking:
                TopmostApplied = true;
                return new[] { SessionCommand.ShowNoActivate, SessionCommand.SetTopmost, SessionCommand.ShowFloatIcon };
            default:
                TopmostApplied = false;
                return new[] { SessionCommand.HideFloatIcon };
        }
    }
}
