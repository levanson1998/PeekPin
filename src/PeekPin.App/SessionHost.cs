using System.Windows.Threading;

namespace PeekPin;

public sealed class SessionHost : IDisposable
{
    private readonly AppController _controller;
    private readonly IWindowGateway _gateway;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _hoverTimer;
    private readonly DispatcherTimer _hideTimer;
    private readonly DispatcherTimer _pollTimer;
    private HoverState _hover = HoverState.Initial;
    private bool _pointerOverIcon;
    private bool _selfMinimizing;
    private bool _sawIconicWhileDocked;
    private bool _disposed;

    public SessionHost(AppController controller, IWindowGateway gateway, WatchSession session, Dispatcher dispatcher)
    {
        _controller = controller;
        Session = session;
        _gateway = gateway;
        _dispatcher = dispatcher;
        _hoverTimer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(controller.Config.HoverDelayMs)
        };
        _hideTimer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(controller.Config.HideDelayMs)
        };
        _pollTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _hoverTimer.Tick += OnHoverTick;
        _hideTimer.Tick += OnHideTick;
        _pollTimer.Tick += OnPollTick;
        Icon = new FloatIconWindow();
        Icon.Clicked += () => Apply(Session.Pin());
        Icon.DragStarted += () =>
        {
            _hoverTimer.Stop();
            _hover = HoverState.Initial;
        };
        Icon.DragCompleted += OnDragCompleted;
        Icon.MinimizeRequested += () => Apply(Session.NotifyMinimized());
        Icon.UnwatchRequested += () => _controller.Unwatch(this);
        Icon.SettingsRequested += () => _controller.ShowPanel();
        Icon.MouseEnter += (_, _) => OnIconEnter();
        Icon.MouseLeave += (_, _) => OnIconLeave();
    }

    public WatchSession Session { get; }

    public FloatIconWindow Icon { get; }

    public SessionPhase Phase => Session.Phase;

    public string Title => Session.Target.Title;

    public void Apply(IReadOnlyList<SessionCommand> commands)
    {
        var id = Session.Window;
        foreach (var command in commands)
        {
            Execute(command, id);
        }
    }

    public void SyncWindowState()
    {
        if (_disposed || Session.Window.IsEmpty || !_gateway.IsAlive(Session.Window))
        {
            return;
        }

        if (_controller.IsPaused || _controller.IsFullscreenSuppressed)
        {
            if (Icon.IsVisible)
            {
                Icon.Hide();
            }

            return;
        }

        var minimized = _gateway.IsMinimized(Session.Window);
        if (Session.Phase == SessionPhase.Visible && minimized)
        {
            _controller.Log.Info($"window minimized hwnd={Session.Window} phase={Session.Phase}");
            OnUserMinimized();
            return;
        }

        if (Session.Phase == SessionPhase.Docked)
        {
            if (minimized)
            {
                _sawIconicWhileDocked = true;
            }
            else if (_sawIconicWhileDocked)
            {
                _sawIconicWhileDocked = false;
                _controller.Log.Info($"window restored hwnd={Session.Window}");
                Apply(Session.Pin());
                return;
            }

            if (!Icon.IsVisible)
            {
                _controller.Log.Info($"show icon hwnd={Session.Window}");
                ShowIcon();
            }
        }
        else if (Session.Phase == SessionPhase.Peeking && !Icon.IsVisible)
        {
            ShowIcon();
        }
        else if (Icon.IsVisible && Session.Phase is SessionPhase.Visible or SessionPhase.Unbound or SessionPhase.Inaccessible)
        {
            Icon.Hide();
        }
    }

    public void OnUserMinimized()
    {
        if (_selfMinimizing)
        {
            return;
        }

        Apply(Session.NotifyMinimized());
    }

    public void OnTargetForeground()
    {
        if (Session.Phase == SessionPhase.Peeking)
        {
            Apply(Session.Pin());
        }
    }

    public void OnDestroyed() => Apply(Session.Destroyed());

    public void RefreshDelays()
    {
        _hoverTimer.Interval = TimeSpan.FromMilliseconds(_controller.Config.HoverDelayMs);
        _hideTimer.Interval = TimeSpan.FromMilliseconds(_controller.Config.HideDelayMs);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _hoverTimer.Stop();
        _hideTimer.Stop();
        _pollTimer.Stop();
        Icon.Close();
    }

    private void Execute(SessionCommand command, WindowId id)
    {
        try
        {
            switch (command.Kind)
            {
                case SessionCommandKind.ShowNoActivate:
                    _gateway.ShowNoActivate(id);
                    break;
                case SessionCommandKind.MinimizeNoActivate:
                    _selfMinimizing = true;
                    _sawIconicWhileDocked = false;
                    try
                    {
                        _gateway.MinimizeNoActivate(id);
                    }
                    finally
                    {
                        _selfMinimizing = false;
                    }
                    break;
                case SessionCommandKind.Activate:
                    _gateway.Activate(id);
                    break;
                case SessionCommandKind.SetTopmost:
                    if (!_controller.IsPaused && !_controller.IsFullscreenSuppressed)
                    {
                        if (!_gateway.TrySetTopmost(id, true))
                        {
                            Apply(Session.AccessDenied());
                        }
                    }
                    break;
                case SessionCommandKind.ClearTopmost:
                    var cleared = _gateway.TrySetTopmost(id, false);
                    _controller.Log.Info($"clear topmost hwnd={id} ok={cleared}");
                    break;
                case SessionCommandKind.ShowFloatIcon:
                    ShowIcon();
                    break;
                case SessionCommandKind.HideFloatIcon:
                case SessionCommandKind.CloseFloatIcon:
                    _hoverTimer.Stop();
                    _hideTimer.Stop();
                    _pollTimer.Stop();
                    Icon.Hide();
                    break;
                case SessionCommandKind.MarkInaccessible:
                    break;
            }
        }
        catch (Exception ex)
        {
            _controller.Log.Error($"command {command.Kind} hwnd={id} {ex.GetType().Name}");
        }

        if (Session.Phase is SessionPhase.Peeking or SessionPhase.Docked)
        {
            _pollTimer.Start();
        }
        else
        {
            _pollTimer.Stop();
            _hideTimer.Stop();
        }
    }

    private void ShowIcon()
    {
        if (_controller.IsPaused || _controller.IsFullscreenSuppressed)
        {
            Icon.Hide();
            return;
        }

        var size = _controller.Config.IconSize;
        Icon.SetIcon(_gateway.TryGetIconPng(Session.Window), size);
        var cursor = NativeWindow.Cursor();
        var monitor = MonitorLayout.FromDeviceOrPoint(Session.Target.MonitorDevice, cursor);
        var relative = IconPlacement.Resolve(Session.Target, monitor.DeviceName, RelativeWork(monitor), size);
        if (!string.Equals(Session.Target.MonitorDevice, monitor.DeviceName, StringComparison.OrdinalIgnoreCase))
        {
            Session.Target.MonitorDevice = monitor.DeviceName;
            Session.Target.IconX = relative.X;
            Session.Target.IconY = relative.Y;
        }

        if (!Icon.IsVisible)
        {
            Icon.Show();
        }

        Icon.Place(new PixelPoint(monitor.WorkArea.X + relative.X, monitor.WorkArea.Y + relative.Y), size);
        _pollTimer.Start();
    }

    private void OnDragCompleted(PixelPoint screenTopLeft)
    {
        if (!_controller.Config.RememberIconPosition)
        {
            return;
        }

        var monitor = MonitorLayout.FromPoint(screenTopLeft);
        Session.Target.MonitorDevice = monitor.DeviceName;
        var relative = IconPlacement.Clamp(
            new PixelPoint(screenTopLeft.X - monitor.WorkArea.X, screenTopLeft.Y - monitor.WorkArea.Y),
            RelativeWork(monitor),
            _controller.Config.IconSize);
        Session.Target.IconX = relative.X;
        Session.Target.IconY = relative.Y;
        _controller.Save();
    }

    private void OnIconEnter()
    {
        _pointerOverIcon = true;
        if (Session.Phase != SessionPhase.Docked || Icon.IsDragging)
        {
            return;
        }

        _hover = new HoverState(DateTime.UtcNow, null, false, false);
        _hoverTimer.Stop();
        _hoverTimer.Start();
    }

    private void OnIconLeave()
    {
        _pointerOverIcon = false;
        _hoverTimer.Stop();
        _hover = HoverState.Initial;
    }

    private void OnHoverTick(object? sender, EventArgs e)
    {
        _hoverTimer.Stop();
        var sample = new HoverSample(Session.Phase, _pointerOverIcon && !Icon.IsDragging, false, Icon.IsDragging, DateTime.UtcNow);
        var result = HoverPolicy.Evaluate(_hover, sample, Options());
        _hover = result.State;
        if (result.RequestPeek)
        {
            Apply(Session.BeginPeek());
        }
    }

    private void OnHideTick(object? sender, EventArgs e)
    {
        _hideTimer.Stop();
        Apply(Session.EndPeek());
    }

    private void OnPollTick(object? sender, EventArgs e)
    {
        var cursor = NativeWindow.Cursor();
        if (Session.Phase == SessionPhase.Docked)
        {
            var overDockIcon = Icon.IsVisible && !Icon.IsDragging && Icon.ScreenRect.Contains(cursor);
            var docked = HoverPolicy.Evaluate(
                _hover,
                new HoverSample(Session.Phase, overDockIcon, false, Icon.IsDragging, DateTime.UtcNow),
                Options());
            _hover = docked.State;
            if (docked.RequestPeek)
            {
                Apply(Session.BeginPeek());
            }

            return;
        }

        if (Session.Phase != SessionPhase.Peeking)
        {
            return;
        }

        if (_gateway.GetForeground() == Session.Window)
        {
            Apply(Session.Pin());
            return;
        }

        var overIcon = Icon.IsVisible && Icon.ScreenRect.Contains(cursor);
        var overWindow = _gateway.IsAlive(Session.Window)
            && _gateway.GetRect(Session.Window).Inflate(12).Contains(cursor);
        var sample = new HoverSample(Session.Phase, overIcon, overWindow, false, DateTime.UtcNow);
        var result = HoverPolicy.Evaluate(_hover, sample, Options());
        _hover = result.State;
        _hideTimer.Stop();
        if (result.RequestHide)
        {
            Apply(Session.EndPeek());
        }
    }

    private HoverOptions Options()
    {
        return new HoverOptions(
            TimeSpan.FromMilliseconds(_controller.Config.HoverDelayMs),
            TimeSpan.FromMilliseconds(_controller.Config.HideDelayMs));
    }

    private static PixelRect RelativeWork(DisplayMonitor monitor)
    {
        return new PixelRect(0, 0, monitor.WorkArea.Width, monitor.WorkArea.Height);
    }
}
