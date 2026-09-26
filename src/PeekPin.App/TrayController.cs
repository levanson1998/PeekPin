using System.Windows.Forms;

namespace PeekPin;

public sealed class TrayController : IDisposable
{
    private readonly AppController _controller;
    private readonly NotifyIcon _icon;

    public TrayController(AppController controller)
    {
        _controller = controller;
        _icon = new NotifyIcon
        {
            Text = Strings.AppTitle,
            Icon = LoadAppIcon(),
            Visible = true
        };
        _icon.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left)
            {
                _controller.ShowPanel();
            }
        };
        RebuildMenu();
        _controller.SessionsChanged += RebuildMenu;
        _controller.LanguageChanged += RebuildMenu;
    }

    public NotifyIcon Icon => _icon;

    public ContextMenuStrip? Menu => _icon.ContextMenuStrip;

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }

    private static System.Drawing.Icon LoadAppIcon()
    {
        try
        {
            if (!string.IsNullOrEmpty(Environment.ProcessPath))
            {
                var extracted = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath);
                if (extracted is not null)
                {
                    return extracted;
                }
            }
        }
        catch (ArgumentException)
        {
        }

        return System.Drawing.SystemIcons.Application;
    }

    private void RebuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(Strings.AddWindow, null, (_, _) => _controller.ShowPanel());
        var pauseLabel = _controller.IsPaused ? Strings.Resume : Strings.Pause;
        menu.Items.Add(pauseLabel, null, (_, _) => _controller.SetPaused(!_controller.IsPaused));
        menu.Items.Add(Strings.Settings, null, (_, _) => _controller.ShowPanel());
        menu.Items.Add(Strings.Quit, null, (_, _) =>
        {
            _controller.Quit();
            System.Windows.Application.Current?.Shutdown();
        });
        _icon.ContextMenuStrip = menu;
    }
}
