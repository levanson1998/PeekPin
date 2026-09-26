namespace PeekPin;

public sealed class AppController : IDisposable
{
    private readonly AppConfigStore _store;
    private readonly List<SessionHost> _sessions = [];
    private readonly WindowEventHook? _hook;
    private readonly HashSet<nint> _known = [];
    private readonly System.Windows.Threading.DispatcherTimer _watchdog;
    private readonly System.Windows.Threading.Dispatcher _dispatcher;
    private bool _disposed;

    public AppController(string configDirectory, IWindowGateway? gateway = null, bool installHook = true, int? excludedProcessId = null)
    {
        ConfigDirectory = configDirectory;
        ExcludedProcessId = excludedProcessId ?? Environment.ProcessId;
        _store = new AppConfigStore(configDirectory);
        Config = _store.Load();
        UiLanguage.Apply(Config.Language);
        Gateway = gateway ?? new WindowGateway();
        Log = new FileLogger(Path.Combine(configDirectory, "logs"));
        _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        if (installHook)
        {
            _hook = new WindowEventHook();
            _hook.Minimized += id => Post(() => OnMinimized(id));
            _hook.ForegroundChanged += id => Post(() => OnForeground(id));
            _hook.Destroyed += id => Post(() => OnDestroyed(id));
            _hook.Shown += id => Post(() => OnShown(id));
        }

        _watchdog = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Background, _dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _watchdog.Tick += (_, _) =>
        {
            foreach (var host in _sessions.ToArray())
            {
                host.SyncWindowState();
            }
        };
        _watchdog.Start();
        RebindFromConfig();
    }

    public string ConfigDirectory { get; }

    public int ExcludedProcessId { get; }

    public AppConfig Config { get; private set; }

    public IWindowGateway Gateway { get; }

    public FileLogger Log { get; }

    public bool IsPaused { get; private set; }

    public bool IsFullscreenSuppressed { get; private set; }

    public IReadOnlyList<SessionHost> Sessions => _sessions;

    public MainWindow? Panel { get; set; }

    public bool UpdateStartupRegistry { get; set; } = true;

    public event Action? SessionsChanged;

    public event Action? LanguageChanged;

    public void SetLanguage(string code)
    {
        Config.Language = code;
        UiLanguage.Apply(code);
        Save();
        LanguageChanged?.Invoke();
    }

    public SessionHost? Watch(WindowSnapshot snapshot)
    {
        if (!AppConfigNormalizer.TryAddTarget(Config, new TargetConfig()))
        {
            return null;
        }

        Config.Targets.RemoveAt(Config.Targets.Count - 1);
        var cursor = NativeWindow.Cursor();
        var monitor = MonitorLayout.FromPoint(cursor);
        var relative = IconPlacement.DefaultNearCursor(Relative(monitor), Config.IconSize);
        var target = new TargetConfig
        {
            ProcessName = snapshot.ProcessName,
            ClassName = snapshot.ClassName,
            Title = snapshot.Title,
            MonitorDevice = monitor.DeviceName,
            IconX = relative.X,
            IconY = relative.Y
        };
        if (!AppConfigNormalizer.TryAddTarget(Config, target))
        {
            return null;
        }

        var host = CreateHost(target);
        _sessions.Add(host);
        if (Gateway.TryGetIntegrityBlocked(snapshot.Id))
        {
            host.Session.Select(snapshot.Id);
            host.Apply(host.Session.AccessDenied());
        }
        else
        {
            host.Apply(host.Session.Select(snapshot.Id));
            if (Gateway.IsMinimized(snapshot.Id))
            {
                host.Apply(host.Session.NotifyMinimized());
            }
        }

        Save();
        SessionsChanged?.Invoke();
        return host;
    }

    public void Unwatch(SessionHost host)
    {
        host.Apply(host.Session.StopTracking());
        _sessions.Remove(host);
        Config.Targets.Remove(host.Session.Target);
        host.Dispose();
        Save();
        SessionsChanged?.Invoke();
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        foreach (var host in _sessions)
        {
            host.Apply(paused ? host.Session.Pause() : host.Session.Resume());
        }

        SessionsChanged?.Invoke();
    }

    public void ShowPanel()
    {
        Panel ??= new MainWindow(this);
        Panel.Refresh();
        Panel.Show();
        Panel.Activate();
    }

    public void Save()
    {
        Config.Targets = _sessions.Select(host => host.Session.Target).ToList();
        Config = AppConfigNormalizer.Normalize(Config);
        _store.Save(Config);
        foreach (var host in _sessions)
        {
            host.RefreshDelays();
        }

        if (UpdateStartupRegistry)
        {
            StartupRegistration.Apply(Config.StartWithWindows, Environment.ProcessPath ?? "");
        }
    }

    public void Quit()
    {
        foreach (var host in _sessions.ToArray())
        {
            host.Apply(host.Session.Quit());
        }

        Save();
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _watchdog.Stop();
        _hook?.Dispose();
        foreach (var host in _sessions.ToArray())
        {
            host.Dispose();
        }

        _sessions.Clear();
        Panel?.Close();
    }

    private void RebindFromConfig()
    {
        foreach (var target in Config.Targets)
        {
            _sessions.Add(CreateHost(target));
        }

        TryBindUnbound();
    }

    private SessionHost CreateHost(TargetConfig target)
    {
        var session = new WatchSession(target);
        return new SessionHost(this, Gateway, session, System.Windows.Threading.Dispatcher.CurrentDispatcher);
    }

    private void OnMinimized(WindowId id)
    {
        Find(id)?.OnUserMinimized();
        SessionsChanged?.Invoke();
    }

    private void OnForeground(WindowId id)
    {
        Find(id)?.OnTargetForeground();
        UpdateFullscreen();
        if (!IsPaused && !IsFullscreenSuppressed)
        {
            foreach (var host in _sessions)
            {
                if (host.Phase is SessionPhase.Visible or SessionPhase.Peeking)
                {
                    Gateway.TrySetTopmost(host.Session.Window, true);
                }
            }
        }

        SessionsChanged?.Invoke();
    }

    private void OnDestroyed(WindowId id)
    {
        var host = Find(id);
        host?.OnDestroyed();
        SessionsChanged?.Invoke();
    }

    private void OnShown(WindowId id) => TryBindUnbound();

    private void UpdateFullscreen()
    {
        var suppressed = Config.HideIconWhenFullscreen && Gateway.IsFullscreenForeground();
        if (suppressed == IsFullscreenSuppressed)
        {
            return;
        }

        IsFullscreenSuppressed = suppressed;
        foreach (var host in _sessions)
        {
            if (suppressed)
            {
                host.Icon.Hide();
            }
            else
            {
                host.Apply(host.Session.Reapply());
            }
        }
    }

    private void TryBindUnbound()
    {
        var windows = Gateway.ListTopLevel(ExcludedProcessId);
        var candidates = windows.Select(window => new WindowCandidate(
            window.Id,
            new WindowIdentity(window.ProcessName, window.ClassName, window.Title))).ToList();
        var matches = WindowIdentityMatcher.Match(Config.Targets, candidates);
        for (var i = 0; i < _sessions.Count && i < matches.Length; i++)
        {
            var match = matches[i];
            if (match is null || _sessions[i].Phase != SessionPhase.Unbound)
            {
                continue;
            }

            var host = _sessions[i];
            if (Gateway.TryGetIntegrityBlocked(match.Value))
            {
                host.Session.Select(match.Value);
                host.Apply(host.Session.AccessDenied());
                continue;
            }

            host.Apply(host.Session.Select(match.Value));
            if (Gateway.IsMinimized(match.Value))
            {
                host.Apply(host.Session.NotifyMinimized());
            }
        }

        SessionsChanged?.Invoke();
    }

    private void Post(Action action)
    {
        _dispatcher.BeginInvoke(action);
    }

    private SessionHost? Find(WindowId id)
    {
        return _sessions.FirstOrDefault(host => host.Session.Window == id);
    }

    private static PixelRect Relative(DisplayMonitor monitor)
    {
        return new PixelRect(0, 0, monitor.WorkArea.Width, monitor.WorkArea.Height);
    }
}
