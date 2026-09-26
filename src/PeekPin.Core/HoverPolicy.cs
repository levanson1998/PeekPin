namespace PeekPin;

public readonly record struct HoverState(DateTime? HoverStartedUtc, DateTime? OutsideStartedUtc, bool PeekRequested, bool HideRequested)
{
    public static HoverState Initial { get; } = new(null, null, false, false);
}

public readonly record struct HoverSample(
    SessionPhase Phase,
    bool PointerOverIcon,
    bool PointerOverWindow,
    bool IsDragging,
    DateTime NowUtc);

public readonly record struct HoverOptions(TimeSpan HoverDelay, TimeSpan HideDelay);

public readonly record struct HoverResult(HoverState State, bool RequestPeek, bool RequestHide);

public static class HoverPolicy
{
    public static HoverResult Evaluate(HoverState state, HoverSample sample, HoverOptions options)
    {
        if (sample.Phase == SessionPhase.Docked)
        {
            if (sample.IsDragging || !sample.PointerOverIcon)
            {
                return new HoverResult(HoverState.Initial, false, false);
            }

            var started = state.HoverStartedUtc ?? sample.NowUtc;
            var request = !state.PeekRequested && sample.NowUtc - started >= options.HoverDelay;
            var next = new HoverState(started, null, state.PeekRequested || request, false);
            return new HoverResult(next, request, false);
        }

        if (sample.Phase == SessionPhase.Peeking)
        {
            if (sample.PointerOverIcon || sample.PointerOverWindow)
            {
                return new HoverResult(new HoverState(null, null, false, false), false, false);
            }

            var started = state.OutsideStartedUtc ?? sample.NowUtc;
            var request = !state.HideRequested && sample.NowUtc - started >= options.HideDelay;
            var next = new HoverState(null, started, false, state.HideRequested || request);
            return new HoverResult(next, false, request);
        }

        return new HoverResult(HoverState.Initial, false, false);
    }
}
