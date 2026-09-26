using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Media = System.Windows.Media;
using WpfButton = System.Windows.Controls.Button;

namespace PeekPin;

public partial class MainWindow : Window
{
    private readonly AppController _controller;
    private bool _showingCatalog;
    private bool _applyingSettings;

    public MainWindow(AppController controller)
    {
        _controller = controller;
        InitializeComponent();
        SizeBox.ItemsSource = new[] { 32, 48, 64 };
        LanguageBox.ItemsSource = new[]
        {
            new LanguageChoice(UiLanguage.Vietnamese, "Tiếng Việt"),
            new LanguageChoice(UiLanguage.English, "English")
        };
        LanguageBox.DisplayMemberPath = nameof(LanguageChoice.Label);
        ApplyTexts();
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            NativeWindow.AddToolStyles(hwnd);
        };
        Closed += (_, _) => _controller.Panel = null;
        _controller.SessionsChanged += Refresh;
        _controller.LanguageChanged += OnLanguageApplied;
        LoadSettings();
        Refresh();
    }

    private void ApplyTexts()
    {
        Title = Strings.AppTitle;
        DeveloperText.Text = Strings.DeveloperName;
        SettingsHeader.Text = Strings.Settings;
        LanguageLabel.Text = Strings.Language;
        HoverLabel.Text = Strings.HoverDelay;
        HideLabel.Text = Strings.HideDelay;
        SizeLabel.Text = Strings.IconSize;
        RememberBox.Content = Strings.RememberPosition;
        StartupBox.Content = Strings.StartWithWindows;
        FullscreenBox.Content = Strings.HideIconWhenFullscreen;
        AddButton.Content = Strings.AddWindow;
        BackButton.Content = Strings.Back;
        SaveButton.Content = Strings.Save;
        HeaderText.Text = _showingCatalog ? Strings.CatalogTitle : Strings.WatchedTitle;
    }

    private void OnLanguageApplied()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(OnLanguageApplied);
            return;
        }

        ApplyTexts();
        if (_showingCatalog)
        {
            OnAdd(this, new RoutedEventArgs());
        }
        else
        {
            ShowWatched();
        }
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_applyingSettings || LanguageBox.SelectedItem is not LanguageChoice choice)
        {
            return;
        }

        if (string.Equals(choice.Code, _controller.Config.Language, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _controller.SetLanguage(choice.Code);
    }

    private sealed record LanguageChoice(string Code, string Label);

    public void Refresh()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(Refresh);
            return;
        }

        if (_showingCatalog)
        {
            return;
        }

        ShowWatched();
    }

    private void LoadSettings()
    {
        _applyingSettings = true;
        HoverBox.Text = _controller.Config.HoverDelayMs.ToString();
        HideBox.Text = _controller.Config.HideDelayMs.ToString();
        SizeBox.SelectedItem = _controller.Config.IconSize;
        RememberBox.IsChecked = _controller.Config.RememberIconPosition;
        StartupBox.IsChecked = _controller.Config.StartWithWindows;
        FullscreenBox.IsChecked = _controller.Config.HideIconWhenFullscreen;
        var code = _controller.Config.Language == UiLanguage.English ? UiLanguage.English : UiLanguage.Vietnamese;
        LanguageBox.SelectedItem = LanguageBox.Items.OfType<LanguageChoice>().FirstOrDefault(item => item.Code == code);
        _applyingSettings = false;
    }

    private void ShowWatched()
    {
        _showingCatalog = false;
        HeaderText.Text = Strings.WatchedTitle;
        AddButton.Visibility = Visibility.Visible;
        BackButton.Visibility = Visibility.Collapsed;
        WatchedList.Items.Clear();
        foreach (var host in _controller.Sessions)
        {
            WatchedList.Items.Add(BuildWatchedRow(host));
        }

        EmptyText.Text = Strings.SelectWindowPrompt;
        EmptyText.Visibility = _controller.Sessions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private UIElement BuildWatchedRow(SessionHost host)
    {
        var grid = new Grid { MinHeight = 36 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 0 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 8, 0) };
        text.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(host.Title) ? host.Session.Target.ProcessName : host.Title,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            FontSize = 14,
            Foreground = new Media.SolidColorBrush(Media.Color.FromRgb(17, 24, 39))
        });
        var status = host.Phase == SessionPhase.Inaccessible
            ? Strings.InaccessibleStatus
            : Strings.PhaseName(host.Phase);
        text.Children.Add(new TextBlock
        {
            Text = status,
            FontSize = 12,
            Foreground = new Media.SolidColorBrush(Media.Color.FromRgb(107, 114, 128)),
            Margin = new Thickness(0, 1, 0, 0)
        });
        Grid.SetColumn(text, 0);
        grid.Children.Add(text);

        var buttons = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var dock = new WpfButton { Content = Strings.Dock, Style = (Style)FindResource("QuietButton"), Tag = host };
        dock.Click += (_, _) => host.Apply(host.Session.NotifyMinimized());
        var remove = new WpfButton { Content = Strings.Unwatch, Style = (Style)FindResource("DangerButton"), Tag = host };
        remove.Click += (_, _) => _controller.Unwatch(host);
        buttons.Children.Add(dock);
        buttons.Children.Add(remove);
        Grid.SetColumn(buttons, 1);
        grid.Children.Add(buttons);
        return grid;
    }

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        _showingCatalog = true;
        HeaderText.Text = Strings.CatalogTitle;
        AddButton.Visibility = Visibility.Collapsed;
        BackButton.Visibility = Visibility.Visible;
        WatchedList.Items.Clear();
        var windows = _controller.Gateway.ListTopLevel(Environment.ProcessId);
        foreach (var window in windows)
        {
            var row = new WpfButton
            {
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                Style = (Style)FindResource("QuietButton"),
                Margin = new Thickness(0, 2, 0, 2),
                Tag = window
            };
            var label = new TextBlock
            {
                Text = $"{window.Title}  ·  {window.ProcessName}",
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap
            };
            row.Content = label;
            row.Click += (_, _) =>
            {
                _controller.Watch(window);
                ShowWatched();
            };
            WatchedList.Items.Add(row);
        }

        EmptyText.Text = Strings.NoWindows;
        EmptyText.Visibility = windows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnBack(object sender, RoutedEventArgs e) => ShowWatched();

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(HoverBox.Text, out var hover))
        {
            _controller.Config.HoverDelayMs = hover;
        }

        if (int.TryParse(HideBox.Text, out var hide))
        {
            _controller.Config.HideDelayMs = hide;
        }

        if (SizeBox.SelectedItem is int size)
        {
            _controller.Config.IconSize = size;
        }

        _controller.Config.RememberIconPosition = RememberBox.IsChecked == true;
        _controller.Config.StartWithWindows = StartupBox.IsChecked == true;
        _controller.Config.HideIconWhenFullscreen = FullscreenBox.IsChecked == true;
        _controller.Save();
        LoadSettings();
    }
}
