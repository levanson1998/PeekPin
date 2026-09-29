using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PeekPin;

public partial class FloatIconWindow : Window
{
    private PixelPoint _dragOriginCursor;
    private PixelRect _dragOriginRect;
    private bool _dragging;
    private bool _pressed;

    public FloatIconWindow()
    {
        InitializeComponent();
        BuildMenu();
        SourceInitialized += OnSourceInitialized;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
    }

    public event Action? Clicked;

    public event Action? DragStarted;

    public event Action<PixelPoint>? DragCompleted;

    public bool IsDragging => _dragging;

    public ContextMenu IconContextMenu => (ContextMenu)IconRoot.ContextMenu;

    public nint Handle => new WindowInteropHelper(this).Handle;

    public void SetIcon(byte[]? png, int size)
    {
        Width = size;
        Height = size;
        var inner = Math.Max(8, size - 8);
        HitPad.Width = inner;
        HitPad.Height = inner;
        IconImage.Width = inner;
        IconImage.Height = inner;
        if (png is null || png.Length == 0)
        {
            IconImage.Source = null;
            return;
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = new MemoryStream(png);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        IconImage.Source = image;
    }

    public void Place(PixelPoint screenTopLeft, int size)
    {
        var hwnd = Handle;
        if (hwnd == 0)
        {
            Left = screenTopLeft.X;
            Top = screenTopLeft.Y;
            Width = size;
            Height = size;
            return;
        }

        NativeWindow.MoveTopmostNoActivate(hwnd, screenTopLeft.X, screenTopLeft.Y, size, size);
    }

    public void KeepAboveOthers()
    {
        if (!IsVisible)
        {
            return;
        }

        NativeWindow.KeepTopmost(Handle);
    }

    public PixelRect ScreenRect => NativeWindow.GetRect(Handle);

    private void BuildMenu()
    {
        var menu = new ContextMenu();
        menu.Items.Add(Item(Strings.ShowAndKeep, () => Clicked?.Invoke()));
        menu.Items.Add(Item(Strings.Minimize, () => MinimizeRequested?.Invoke()));
        menu.Items.Add(Item(Strings.Unwatch, () => UnwatchRequested?.Invoke()));
        menu.Items.Add(Item(Strings.Settings, () => SettingsRequested?.Invoke()));
        IconRoot.ContextMenu = menu;
    }

    public event Action? MinimizeRequested;

    public event Action? UnwatchRequested;

    public event Action? SettingsRequested;

    private static MenuItem Item(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = Handle;
        NativeWindow.AddToolStyles(hwnd);
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _pressed = true;
        _dragging = false;
        _dragOriginCursor = NativeWindow.Cursor();
        _dragOriginRect = ScreenRect;
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_pressed || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var cursor = NativeWindow.Cursor();
        var dx = cursor.X - _dragOriginCursor.X;
        var dy = cursor.Y - _dragOriginCursor.Y;
        var threshold = SystemParameters.MinimumHorizontalDragDistance;
        if (!_dragging && Math.Abs(dx) < threshold && Math.Abs(dy) < threshold)
        {
            return;
        }

        if (!_dragging)
        {
            _dragging = true;
            DragStarted?.Invoke();
        }

        NativeWindow.MoveTopmostNoActivate(
            Handle,
            _dragOriginRect.X + dx,
            _dragOriginRect.Y + dy,
            _dragOriginRect.Width,
            _dragOriginRect.Height);
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_pressed)
        {
            return;
        }

        _pressed = false;
        ReleaseMouseCapture();
        if (_dragging)
        {
            var rect = ScreenRect;
            DragCompleted?.Invoke(new PixelPoint(rect.X, rect.Y));
            _dragging = false;
        }
        else
        {
            Clicked?.Invoke();
        }

        e.Handled = true;
    }
}
