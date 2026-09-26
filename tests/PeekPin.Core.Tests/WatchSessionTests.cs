using PeekPin;

namespace PeekPin.Core.Tests;

public class WatchSessionTests
{
    private static WatchSession NewSession() => new(new TargetConfig { Title = "Fixture", ProcessName = "fixture", ClassName = "Fixture" });

    [Fact]
    public void Select_SetsVisibleAndTopmostWithoutIcon()
    {
        var session = NewSession();
        var commands = session.Select(new WindowId(1));
        Assert.Equal(SessionPhase.Visible, session.Phase);
        Assert.Contains(SessionCommand.SetTopmost, commands);
        Assert.DoesNotContain(SessionCommand.ShowFloatIcon, commands);
    }

    [Fact]
    public void Minimize_FromVisible_DocksAndShowsIcon()
    {
        var session = NewSession();
        session.Select(new WindowId(1));
        var commands = session.NotifyMinimized();
        Assert.Equal(SessionPhase.Docked, session.Phase);
        Assert.Contains(SessionCommand.MinimizeNoActivate, commands);
        Assert.Contains(SessionCommand.ShowFloatIcon, commands);
        Assert.Contains(SessionCommand.ClearTopmost, commands);
    }

    [Fact]
    public void Pin_FromPeek_ActivatesAndHidesIcon()
    {
        var session = Docked();
        session.BeginPeek();
        var commands = session.Pin();
        Assert.Equal(SessionPhase.Visible, session.Phase);
        Assert.Contains(SessionCommand.Activate, commands);
        Assert.Contains(SessionCommand.HideFloatIcon, commands);
    }

    [Fact]
    public void Visible_DoesNotMinimizeFromPin()
    {
        var session = NewSession();
        session.Select(new WindowId(1));
        Assert.Empty(session.EndPeek());
        Assert.Equal(SessionPhase.Visible, session.Phase);
    }

    [Fact]
    public void Destroyed_ClosesIconWithoutActivate()
    {
        var session = Docked();
        var commands = session.Destroyed();
        Assert.Equal(SessionPhase.Unbound, session.Phase);
        Assert.Contains(SessionCommand.CloseFloatIcon, commands);
        Assert.DoesNotContain(SessionCommand.Activate, commands);
    }

    [Fact]
    public void AccessDenied_IsInaccessibleWithoutIcon()
    {
        var session = NewSession();
        session.Select(new WindowId(1));
        var commands = session.AccessDenied();
        Assert.Equal(SessionPhase.Inaccessible, session.Phase);
        Assert.Contains(SessionCommand.HideFloatIcon, commands);
        Assert.DoesNotContain(SessionCommand.ShowFloatIcon, commands);
    }

    [Fact]
    public void Pause_SuppressesShowAndTopmost()
    {
        var session = Docked();
        session.Pause();
        Assert.Empty(session.BeginPeek());
        Assert.Equal(SessionPhase.Docked, session.Phase);
        Assert.True(session.IsPaused);
    }

    [Fact]
    public void Quit_PeekingMinimizes_DockedAndVisibleOnlyClearTopmost()
    {
        var peeking = Docked();
        peeking.BeginPeek();
        var peekQuit = peeking.Quit();
        Assert.Contains(SessionCommand.MinimizeNoActivate, peekQuit);
        Assert.Contains(SessionCommand.ClearTopmost, peekQuit);
        Assert.DoesNotContain(SessionCommand.Activate, peekQuit);

        var docked = Docked();
        var dockQuit = docked.Quit();
        Assert.DoesNotContain(SessionCommand.MinimizeNoActivate, dockQuit);
        Assert.Contains(SessionCommand.ClearTopmost, dockQuit);

        var visible = NewSession();
        visible.Select(new WindowId(1));
        var visibleQuit = visible.Quit();
        Assert.DoesNotContain(SessionCommand.MinimizeNoActivate, visibleQuit);
        Assert.Contains(SessionCommand.ClearTopmost, visibleQuit);
    }

    [Fact]
    public void StopTracking_KeepsWindowSoTopmostCanBeCleared()
    {
        var session = NewSession();
        var window = new WindowId(42);
        session.Select(window);
        var commands = session.StopTracking();
        Assert.Equal(window, session.Window);
        Assert.Contains(SessionCommand.ClearTopmost, commands);
        Assert.Contains(SessionCommand.CloseFloatIcon, commands);
        Assert.DoesNotContain(SessionCommand.SetTopmost, commands);
    }

    private static WatchSession Docked()
    {
        var session = NewSession();
        session.Select(new WindowId(1));
        session.NotifyMinimized();
        return session;
    }
}
