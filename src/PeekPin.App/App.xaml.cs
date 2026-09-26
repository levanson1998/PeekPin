using System.Windows;
using System.Windows.Threading;

namespace PeekPin;

public partial class App : System.Windows.Application
{
    private SingleInstance? _instance;
    private AppController? _controller;
    private TrayController? _tray;
    private FileLogger? _log;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;

        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PeekPin");
        _log = new FileLogger(Path.Combine(directory, "logs"));
        _instance = SingleInstance.Acquire();
        if (!_instance.Acquired)
        {
            Shutdown(0);
            return;
        }

        _controller = new AppController(directory);
        _tray = new TrayController(_controller);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _controller?.Dispose();
        _instance?.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _log?.Error(e.Exception.ToString());
        _tray?.Icon.ShowBalloonTip(3000, Strings.AppTitle, e.Exception.Message, System.Windows.Forms.ToolTipIcon.Error);
        e.Handled = true;
    }

    private void OnDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _log?.Error(ex.ToString());
        }
    }
}
