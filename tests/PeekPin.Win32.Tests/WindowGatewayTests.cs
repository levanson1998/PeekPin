using PeekPin;

namespace PeekPin.Win32.Tests;

public class WindowGatewayTests
{
    private readonly WindowGateway _gateway = new();

    public WindowGatewayTests()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }
    }

    [Fact]
    public void Catalog_IncludesFixture_AndSkipsToolWindow()
    {
        var fixture = FixtureWindow.Start("PeekPin Fixture");
        var tool = FixtureWindow.Start("PeekPin Tool", tool: true);
        try
        {
            var list = _gateway.ListTopLevel(0);
            Assert.Contains(list, window => window.Id.Value == fixture.Hwnd);
            Assert.DoesNotContain(list, window => window.Id.Value == tool.Hwnd);
        }
        finally
        {
            FixtureWindow.Close(fixture.Form);
            FixtureWindow.Close(tool.Form);
        }
    }

    [Fact]
    public void Topmost_CanBeSetAndCleared()
    {
        var fixture = FixtureWindow.Start("PeekPin Topmost");
        try
        {
            var id = new WindowId(fixture.Hwnd);
            Assert.True(_gateway.TrySetTopmost(id, true));
            Assert.True(WindowGateway.HasTopmostStyle(id));
            Assert.True(_gateway.TrySetTopmost(id, false));
            Assert.False(WindowGateway.HasTopmostStyle(id));
        }
        finally
        {
            FixtureWindow.Close(fixture.Form);
        }
    }

    [Fact]
    public void MinimizeNoActivate_DoesNotStealForeground()
    {
        var fixture = FixtureWindow.Start("PeekPin Minimize");
        try
        {
            var id = new WindowId(fixture.Hwnd);
            _gateway.MinimizeNoActivate(id);
            Thread.Sleep(200);
            Assert.True(_gateway.IsMinimized(id));
            Assert.NotEqual(id, _gateway.GetForeground());
        }
        finally
        {
            FixtureWindow.Close(fixture.Form);
        }
    }

    [Fact]
    public void ShowNoActivate_RestoresWithoutForeground()
    {
        var fixture = FixtureWindow.Start("PeekPin Show");
        try
        {
            var id = new WindowId(fixture.Hwnd);
            var beforeRect = _gateway.GetRect(id);
            _gateway.MinimizeNoActivate(id);
            Thread.Sleep(150);
            var foreground = _gateway.GetForeground();
            _gateway.ShowNoActivate(id);
            Thread.Sleep(200);
            Assert.False(_gateway.IsMinimized(id));
            Assert.NotEqual(id, _gateway.GetForeground());
            Assert.Equal(foreground, _gateway.GetForeground());
            var after = _gateway.GetRect(id);
            Assert.InRange(after.Width, beforeRect.Width - 8, beforeRect.Width + 8);
        }
        finally
        {
            FixtureWindow.Close(fixture.Form);
        }
    }

    [Fact]
    public void Activate_MakesFixtureForeground()
    {
        var fixture = FixtureWindow.Start("PeekPin Activate");
        try
        {
            var id = new WindowId(fixture.Hwnd);
            _gateway.Activate(id);
            Thread.Sleep(200);
            Assert.Equal(id, _gateway.GetForeground());
        }
        finally
        {
            FixtureWindow.Close(fixture.Form);
        }
    }

    [Fact]
    public void ClearTopmost_WorksWhileMinimized()
    {
        var fixture = FixtureWindow.Start("PeekPin ClearTopmost");
        try
        {
            var id = new WindowId(fixture.Hwnd);
            Assert.True(_gateway.TrySetTopmost(id, true));
            _gateway.MinimizeNoActivate(id);
            Thread.Sleep(200);
            Assert.True(_gateway.TrySetTopmost(id, false));
            Assert.False(WindowGateway.HasTopmostStyle(id));
        }
        finally
        {
            FixtureWindow.Close(fixture.Form);
        }
    }

    [Fact]
    public void ClosedWindow_TrySetTopmostReturnsFalse()
    {
        var fixture = FixtureWindow.Start("PeekPin Closed");
        var id = new WindowId(fixture.Hwnd);
        FixtureWindow.Close(fixture.Form);
        Thread.Sleep(200);
        Assert.False(_gateway.TrySetTopmost(id, true));
        _gateway.ShowNoActivate(id);
    }

    [Fact]
    public void SameIntegrity_IsNotBlocked()
    {
        var fixture = FixtureWindow.Start("PeekPin Integrity");
        try
        {
            Assert.False(_gateway.TryGetIntegrityBlocked(new WindowId(fixture.Hwnd)));
        }
        finally
        {
            FixtureWindow.Close(fixture.Form);
        }
    }

    [Fact]
    public void LayeredCorner_IsClickThrough_CenterIsNot()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }

        nint hwnd = 0;
        Exception? error = null;
        var ready = new ManualResetEventSlim(false);
        Form? form = null;
        var thread = new Thread(() =>
        {
            try
            {
                hwnd = LayeredProbe.Create(out form);
                ready.Set();
                System.Windows.Forms.Application.Run(form);
            }
            catch (Exception ex)
            {
                error = ex;
                ready.Set();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        ready.Wait(TimeSpan.FromSeconds(5));
        if (error is not null)
        {
            throw error;
        }

        try
        {
            Thread.Sleep(200);
            var corner = NativeWindow.WindowFromPoint(new PixelPoint(200, 200));
            var center = NativeWindow.WindowFromPoint(new PixelPoint(232, 232));
            Assert.NotEqual(hwnd, corner);
            Assert.Equal(hwnd, center);
        }
        finally
        {
            form?.Invoke(() => form.Close());
        }
    }

    [Fact]
    public void MaximizedWindow_IsNotFullscreen()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }

        var fixture = FixtureWindow.Start("PeekPin Maximized");
        try
        {
            fixture.Form.Invoke(() => fixture.Form.WindowState = FormWindowState.Maximized);
            Thread.Sleep(200);
            Assert.False(_gateway.IsFullscreen(new WindowId(fixture.Hwnd)));
        }
        finally
        {
            FixtureWindow.Close(fixture.Form);
        }
    }

    [Fact]
    public void BorderlessMonitorWindow_IsFullscreen()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }

        var fixture = FixtureWindow.Start("PeekPin Borderless");
        try
        {
            fixture.Form.Invoke(() =>
            {
                var screen = Screen.FromHandle(fixture.Form.Handle);
                fixture.Form.FormBorderStyle = FormBorderStyle.None;
                fixture.Form.WindowState = FormWindowState.Normal;
                fixture.Form.Bounds = screen.Bounds;
            });
            Thread.Sleep(200);
            Assert.True(_gateway.IsFullscreen(new WindowId(fixture.Hwnd)));
        }
        finally
        {
            FixtureWindow.Close(fixture.Form);
        }
    }

}
