using PeekPin;

namespace PeekPin.Core.Tests;

public class HoverPolicyTests
{
    private static readonly HoverOptions Options = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(250));
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Hover_At499_DoesNotPeek_At500_PeeksOnce()
    {
        var state = new HoverState(T0, null, false, false);
        var early = HoverPolicy.Evaluate(state, Sample(SessionPhase.Docked, true, false, false, T0.AddMilliseconds(499)), Options);
        Assert.False(early.RequestPeek);
        var fired = HoverPolicy.Evaluate(early.State, Sample(SessionPhase.Docked, true, false, false, T0.AddMilliseconds(500)), Options);
        Assert.True(fired.RequestPeek);
        var again = HoverPolicy.Evaluate(fired.State, Sample(SessionPhase.Docked, true, false, false, T0.AddMilliseconds(800)), Options);
        Assert.False(again.RequestPeek);
    }

    [Fact]
    public void LeaveBeforeDelay_ResetsTimer()
    {
        var state = new HoverState(T0, null, false, false);
        var left = HoverPolicy.Evaluate(state, Sample(SessionPhase.Docked, false, false, false, T0.AddMilliseconds(400)), Options);
        Assert.Null(left.State.HoverStartedUtc);
        var back = HoverPolicy.Evaluate(left.State, Sample(SessionPhase.Docked, true, false, false, T0.AddMilliseconds(400)), Options);
        var tooSoon = HoverPolicy.Evaluate(back.State, Sample(SessionPhase.Docked, true, false, false, T0.AddMilliseconds(800)), Options);
        Assert.False(tooSoon.RequestPeek);
    }

    [Fact]
    public void Drag_CancelsHover()
    {
        var state = new HoverState(T0, null, false, false);
        var dragged = HoverPolicy.Evaluate(state, Sample(SessionPhase.Docked, true, false, true, T0.AddMilliseconds(600)), Options);
        Assert.False(dragged.RequestPeek);
    }

    [Fact]
    public void Peeking_OverWindow_DoesNotHide()
    {
        var state = HoverState.Initial;
        var result = HoverPolicy.Evaluate(state, Sample(SessionPhase.Peeking, false, true, false, T0.AddMilliseconds(1000)), Options);
        Assert.False(result.RequestHide);
    }

    [Fact]
    public void Peeking_Outside_HidesAt250Not249()
    {
        var state = new HoverState(null, T0, false, false);
        var early = HoverPolicy.Evaluate(state, Sample(SessionPhase.Peeking, false, false, false, T0.AddMilliseconds(249)), Options);
        Assert.False(early.RequestHide);
        var fired = HoverPolicy.Evaluate(early.State, Sample(SessionPhase.Peeking, false, false, false, T0.AddMilliseconds(250)), Options);
        Assert.True(fired.RequestHide);
        var again = HoverPolicy.Evaluate(fired.State, Sample(SessionPhase.Peeking, false, false, false, T0.AddMilliseconds(400)), Options);
        Assert.False(again.RequestHide);
    }

    [Fact]
    public void Visible_DoesNotHide()
    {
        var result = HoverPolicy.Evaluate(HoverState.Initial, Sample(SessionPhase.Visible, false, false, false, T0), Options);
        Assert.False(result.RequestHide);
        Assert.False(result.RequestPeek);
    }

    private static HoverSample Sample(SessionPhase phase, bool icon, bool window, bool dragging, DateTime now)
        => new(phase, icon, window, dragging, now);
}
