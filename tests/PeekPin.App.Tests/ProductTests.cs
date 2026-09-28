using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Forms;

namespace PeekPin.App.Tests;

public class ProductTests
{
    [Fact]
    public void WatchedFixtures_StayTopmostUntilDocked()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }

        StaPump.Run(() =>
        {
            using var scope = new ProductScope();
            var first = scope.Watch("PeekPin Fixture A");
            var second = scope.Watch("PeekPin Fixture B");
            Assert.True(NativeWindow.IsTopmost(first.Id));
            Assert.True(NativeWindow.IsTopmost(second.Id));
            var width = scope.Gateway.GetRect(first.Id).Width;
            scope.Dock(first.Host);
            StaPump.Wait(TimeSpan.FromMilliseconds(300));
            Assert.Equal(SessionPhase.Docked, first.Host.Phase);
            Assert.True(first.Host.Icon.IsVisible);
            Assert.InRange(width, 280, 340);
        });
    }

    [Fact]
    public void HoverAndClick_FollowTheSpec()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }

        StaPump.Run(() =>
        {
            using var scope = new ProductScope();
            var watched = scope.Watch("PeekPin Hover");
            var anchor = scope.Watch("PeekPin Anchor");
            scope.Gateway.Activate(anchor.Id);
            MouseInput.Move(5, 5);
            scope.Dock(watched.Host);
            StaPump.Wait(TimeSpan.FromMilliseconds(200));
            var center = Center(watched.Host);
            MouseInput.Move(center.X, center.Y);
            StaPump.Wait(TimeSpan.FromMilliseconds(400));
            MouseInput.Move(5, 5);
            StaPump.Wait(TimeSpan.FromMilliseconds(200));
            Assert.True(scope.Gateway.IsMinimized(watched.Id));

            MouseInput.Move(center.X, center.Y);
            StaPump.Wait(TimeSpan.FromMilliseconds(1000));
            Assert.False(scope.Gateway.IsMinimized(watched.Id));
            Assert.NotEqual(watched.Id, scope.Gateway.GetForeground());

            var window = scope.Gateway.GetRect(watched.Id);
            MouseInput.Move(window.X + 20, window.Y + 20);
            StaPump.Wait(TimeSpan.FromMilliseconds(400));
            Assert.False(scope.Gateway.IsMinimized(watched.Id));

            MouseInput.Move(5, 5);
            StaPump.Wait(TimeSpan.FromMilliseconds(500));
            Assert.True(scope.Gateway.IsMinimized(watched.Id));

            MouseInput.Click(center.X, center.Y);
            StaPump.Wait(TimeSpan.FromMilliseconds(400));
            Assert.Equal(SessionPhase.Visible, watched.Host.Phase);
            Assert.False(watched.Host.Icon.IsVisible);
            scope.Dock(watched.Host);
            StaPump.Wait(TimeSpan.FromMilliseconds(200));
            Assert.Equal(center.X, Center(watched.Host).X);
        });
    }

    [Fact]
    public void Drag_DoesNotPeek_AndSavesPosition()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }

        StaPump.Run(() =>
        {
            using var scope = new ProductScope();
            var watched = scope.Watch("PeekPin Drag");
            scope.Dock(watched.Host);
            StaPump.Wait(TimeSpan.FromMilliseconds(200));
            var center = Center(watched.Host);
            var beforeX = watched.Host.Session.Target.IconX;
            MouseInput.Drag(center.X, center.Y, center.X + 80, center.Y + 40);
            StaPump.Wait(TimeSpan.FromMilliseconds(300));
            Assert.True(scope.Gateway.IsMinimized(watched.Id));
            Assert.NotEqual(beforeX, watched.Host.Session.Target.IconX);
            Assert.False(File.Exists(Path.Combine(scope.Directory, "config.json")));
            Assert.False(File.Exists(Path.Combine(scope.Directory, "config.json.tmp")));
        });
    }

    [Fact]
    public void TransparentCorner_DoesNotHitIcon()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }

        StaPump.Run(() =>
        {
            using var scope = new ProductScope();
            var watched = scope.Watch("PeekPin HitTest");
            scope.Dock(watched.Host);
            StaPump.Wait(TimeSpan.FromMilliseconds(300));
            var rect = watched.Host.Icon.ScreenRect;
            var corner = NativeWindow.WindowFromPoint(new PixelPoint(rect.X, rect.Y));
            var center = NativeWindow.WindowFromPoint(new PixelPoint(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));
            Assert.NotEqual(watched.Host.Icon.Handle, corner);
            Assert.Equal(watched.Host.Icon.Handle, center);
        });
    }

    [Fact]
    public void ClickPeekedWindow_PinsIt()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }

        StaPump.Run(() =>
        {
            using var scope = new ProductScope();
            var watched = scope.Watch("PeekPin Pin");
            scope.Dock(watched.Host);
            StaPump.Wait(TimeSpan.FromMilliseconds(200));
            var center = Center(watched.Host);
            MouseInput.Move(center.X, center.Y);
            StaPump.Wait(TimeSpan.FromMilliseconds(1000));
            var window = scope.Gateway.GetRect(watched.Id);
            MouseInput.Click(window.X + window.Width / 2, window.Y + window.Height / 2);
            StaPump.Wait(TimeSpan.FromMilliseconds(400));
            Assert.Equal(SessionPhase.Visible, watched.Host.Phase);
            StaPump.Wait(TimeSpan.FromMilliseconds(400));
            Assert.Equal(SessionPhase.Visible, watched.Host.Phase);
        });
    }

    [Fact]
    public void Unwatch_ClearsTopmostAfterDock()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }

        StaPump.Run(() =>
        {
            using var scope = new ProductScope();
            var watched = scope.Watch("PeekPin Unwatch");
            scope.Dock(watched.Host);
            StaPump.Wait(TimeSpan.FromMilliseconds(300));
            scope.Controller.Unwatch(watched.Host);
            Assert.False(NativeWindow.IsTopmost(watched.Id));
            Assert.True(scope.Gateway.IsAlive(watched.Id));
        });
    }

    [Fact]
    public void MinimizeFromOutside_ShowsIconWithoutExtraClick()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }

        StaPump.Run(() =>
        {
            using var scope = new ProductScope();
            var watched = scope.Watch("PeekPin ExternalMin");
            scope.Gateway.MinimizeNoActivate(watched.Id);
            StaPump.Wait(TimeSpan.FromMilliseconds(800));
            Assert.Equal(SessionPhase.Docked, watched.Host.Phase);
            Assert.True(watched.Host.Icon.IsVisible);
        });
    }

    [Fact]
    public void IconMenu_HasNoQuit_PauseAndQuitCleanUp()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }

        StaPump.Run(() =>
        {
            using var scope = new ProductScope();
            var watched = scope.Watch("PeekPin Menu");
            scope.Dock(watched.Host);
            var headers = watched.Host.Icon.IconContextMenu.Items.OfType<MenuItem>().Select(item => item.Header?.ToString()).ToList();
            Assert.Contains(Strings.ShowAndKeep, headers);
            Assert.Contains(Strings.Minimize, headers);
            Assert.Contains(Strings.Unwatch, headers);
            Assert.DoesNotContain(Strings.Quit, headers);

            scope.Controller.SetPaused(true);
            Assert.False(watched.Host.Icon.IsVisible);
            Assert.False(NativeWindow.IsTopmost(watched.Id));
            scope.Controller.SetPaused(false);
            Assert.Equal(SessionPhase.Docked, watched.Host.Phase);

            scope.Controller.Quit();
            Assert.True(scope.Gateway.IsAlive(watched.Id));
            Assert.False(NativeWindow.IsTopmost(watched.Id));
        });
    }

    [Fact]
    public void SecondHost_RebindsSavedIcon()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }

        StaPump.Run(() =>
        {
            string directory;
            int x;
            int y;
            using (var scope = new ProductScope())
            {
                var watched = scope.Watch("PeekPin Rebind");
                scope.Dock(watched.Host);
                StaPump.Wait(TimeSpan.FromMilliseconds(200));
                var center = Center(watched.Host);
                MouseInput.Drag(center.X, center.Y, center.X + 60, center.Y + 30);
                StaPump.Wait(TimeSpan.FromMilliseconds(200));
                if (watched.Host.Phase != SessionPhase.Docked)
                {
                    scope.Dock(watched.Host);
                }
                directory = scope.Directory;
                x = watched.Host.Session.Target.IconX;
                y = watched.Host.Session.Target.IconY;
                scope.KeepFixture = true;
            }

            var controller = new AppController(directory, installHook: true, excludedProcessId: 0) { UpdateStartupRegistry = false };
            try
            {
                StaPump.Wait(TimeSpan.FromMilliseconds(400));
                var host = Assert.Single(controller.Sessions);
                Assert.Equal(SessionPhase.Docked, host.Phase);
                Assert.Equal(x, host.Session.Target.IconX);
                Assert.Equal(y, host.Session.Target.IconY);
            }
            finally
            {
                controller.Dispose();
            }
        });
    }

    [Fact]
    public void SecondProcess_Exits()
    {
        if (!Environment.UserInteractive)
        {
            return;
        }

        var exe = FindAppExe();
        var first = Process.Start(new ProcessStartInfo(exe) { UseShellExecute = false });
        Assert.NotNull(first);
        try
        {
            Thread.Sleep(1500);
            var second = Process.Start(new ProcessStartInfo(exe) { UseShellExecute = false });
            Assert.NotNull(second);
            Assert.True(second!.WaitForExit(5000));
            Assert.False(first.HasExited);
            Assert.Single(Process.GetProcessesByName("PeekPin"));
        }
        finally
        {
            if (!first.HasExited)
            {
                first.Kill(true);
            }
        }
    }

    private static PixelPoint Center(SessionHost host)
    {
        var rect = host.Icon.ScreenRect;
        return new PixelPoint(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
    }

    private static string FindAppExe()
    {
        var candidate = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "PeekPin.App", "bin", "Release", "net8.0-windows", "PeekPin.exe"));
        if (!File.Exists(candidate))
        {
            candidate = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",
                "src", "PeekPin.App", "bin", "Debug", "net8.0-windows", "PeekPin.exe"));
        }

        return candidate;
    }
}

internal sealed class ProductScope : IDisposable
{
    private readonly List<Form> _forms = [];
    public string Directory { get; } = Path.Combine(Path.GetTempPath(), "peekpin-product", Guid.NewGuid().ToString("N"));
    public WindowGateway Gateway { get; } = new();
    public AppController Controller { get; }
    public bool KeepFixture { get; set; }

    public ProductScope()
    {
        Controller = new AppController(Directory, Gateway, installHook: true, excludedProcessId: 0) { UpdateStartupRegistry = false };
    }

    public Watched Watch(string title)
    {
        var handle = StartForm(title);
        var snapshot = Gateway.ListTopLevel(0).Single(window => window.Id.Value == handle);
        var host = Controller.Watch(snapshot) ?? throw new InvalidOperationException("Watch failed.");
        return new Watched(host, new WindowId(handle));
    }

    public void Dock(SessionHost host) => host.Apply(host.Session.NotifyMinimized());

    public void Dispose()
    {
        Controller.Dispose();
        if (!KeepFixture)
        {
            foreach (var form in _forms)
            {
                if (!form.IsDisposed)
                {
                    form.Invoke(() => form.Close());
                }
            }
        }
    }

    private nint StartForm(string title)
    {
        var ready = new ManualResetEventSlim(false);
        Form? form = null;
        nint hwnd = 0;
        var thread = new Thread(() =>
        {
            form = new Form
            {
                Text = title,
                Width = 300,
                Height = 180,
                StartPosition = FormStartPosition.Manual,
                Location = new System.Drawing.Point(120, 120)
            };
            form.Shown += (_, _) =>
            {
                hwnd = form.Handle;
                ready.Set();
            };
            System.Windows.Forms.Application.Run(form);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        ready.Wait(TimeSpan.FromSeconds(5));
        _forms.Add(form!);
        return hwnd;
    }
}

internal readonly record struct Watched(SessionHost Host, WindowId Id);
