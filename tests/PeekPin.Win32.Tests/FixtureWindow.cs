using System.Runtime.InteropServices;

namespace PeekPin.Win32.Tests;

internal sealed class FixtureHandle
{
    public required Form Form { get; init; }
    public nint Hwnd { get; init; }
}

internal static class FixtureWindow
{
    public static FixtureHandle Start(string title, bool tool = false)
    {
        FixtureHandle? handle = null;
        var ready = new ManualResetEventSlim(false);
        var thread = new Thread(() =>
        {
            var form = new Form
            {
                Text = title,
                Width = 320,
                Height = 200,
                StartPosition = FormStartPosition.Manual,
                Location = new System.Drawing.Point(80, 80),
                ShowInTaskbar = !tool
            };
            if (tool)
            {
                form.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            }

            form.Shown += (_, _) =>
            {
                handle = new FixtureHandle { Form = form, Hwnd = form.Handle };
                ready.Set();
            };
            Application.Run(form);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        if (!ready.Wait(TimeSpan.FromSeconds(5)) || handle is null)
        {
            throw new InvalidOperationException("Fixture did not start.");
        }

        return handle;
    }

    public static void Close(Form form)
    {
        if (form.IsDisposed)
        {
            return;
        }

        form.Invoke(() => form.Close());
    }
}

internal static class LayeredProbe
{
    public static IntPtr Create(out Form form)
    {
        form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new System.Drawing.Point(200, 200),
            Size = new System.Drawing.Size(64, 64),
            BackColor = System.Drawing.Color.Magenta,
            TransparencyKey = System.Drawing.Color.Magenta
        };
        form.Paint += (_, e) =>
        {
            e.Graphics.FillEllipse(System.Drawing.Brushes.Red, 16, 16, 32, 32);
        };
        form.Show();
        return form.Handle;
    }
}

internal static class NativeProbe
{
    [DllImport("user32.dll")]
    public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
}
