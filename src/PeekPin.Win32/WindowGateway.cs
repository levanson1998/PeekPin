using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace PeekPin;

public sealed class WindowGateway : IWindowGateway
{
    public IReadOnlyList<WindowSnapshot> ListTopLevel(int ownProcessId)
    {
        var result = new List<WindowSnapshot>();
        NativeMethods.EnumWindowsProc callback = (hwnd, _) =>
        {
            var snapshot = TrySnapshot(hwnd);
            if (snapshot is not null && WindowCatalogFilter.Include(snapshot, ownProcessId))
            {
                result.Add(snapshot);
            }

            return true;
        };

        NativeMethods.EnumWindows(callback, IntPtr.Zero);
        GC.KeepAlive(callback);
        return result;
    }

    public bool TrySetTopmost(WindowId id, bool enabled)
    {
        var hwnd = id.Value;
        if (hwnd == 0 || !NativeMethods.IsWindow(hwnd))
        {
            return false;
        }

        if (!enabled)
        {
            var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlExStyle);
            if ((style & NativeMethods.WsExTopmost) != 0)
            {
                NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GwlExStyle, style & ~NativeMethods.WsExTopmost);
            }
        }

        var after = enabled ? NativeMethods.HwndTopmost : NativeMethods.HwndNotTopmost;
        var ok = NativeMethods.SetWindowPos(
            hwnd,
            after,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate | NativeMethods.SwpFrameChanged);
        return ok || (!enabled && !HasTopmostStyle(id));
    }

    public void ShowNoActivate(WindowId id)
    {
        var hwnd = id.Value;
        if (!NativeMethods.IsWindow(hwnd))
        {
            return;
        }

        var placement = CreatePlacement();
        if (!NativeMethods.GetWindowPlacement(hwnd, ref placement))
        {
            return;
        }

        var previous = NativeMethods.GetForegroundWindow();
        placement.ShowCmd = NativeMethods.SwShowNoActivate;
        NativeMethods.SetWindowPlacement(hwnd, ref placement);
        NativeMethods.SetWindowPos(
            hwnd,
            NativeMethods.HwndTopmost,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
        if (previous != IntPtr.Zero && previous != hwnd && NativeMethods.GetForegroundWindow() == hwnd)
        {
            ForceForeground(previous);
        }
    }

    public void MinimizeNoActivate(WindowId id)
    {
        var hwnd = id.Value;
        if (!NativeMethods.IsWindow(hwnd))
        {
            return;
        }

        NativeMethods.ShowWindow(hwnd, NativeMethods.SwShowMinNoActive);
    }

    public void Activate(WindowId id)
    {
        var hwnd = id.Value;
        if (!NativeMethods.IsWindow(hwnd))
        {
            return;
        }

        var placement = CreatePlacement();
        if (NativeMethods.GetWindowPlacement(hwnd, ref placement))
        {
            placement.ShowCmd = NativeMethods.SwRestore;
            NativeMethods.SetWindowPlacement(hwnd, ref placement);
        }

        ForceForeground(hwnd);
    }

    private static void ForceForeground(IntPtr hwnd)
    {
        if (NativeMethods.SetForegroundWindow(hwnd))
        {
            return;
        }

        var foreground = NativeMethods.GetForegroundWindow();
        var foregroundThread = foreground == 0 ? 0 : NativeMethods.GetWindowThreadProcessId(foreground, out _);
        var targetThread = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
        var currentThread = NativeMethods.GetCurrentThreadId();
        try
        {
            if (targetThread != 0)
            {
                NativeMethods.AttachThreadInput(currentThread, targetThread, true);
            }

            if (foregroundThread != 0 && foregroundThread != currentThread)
            {
                NativeMethods.AttachThreadInput(foregroundThread, currentThread, true);
            }

            NativeMethods.SetForegroundWindow(hwnd);
        }
        finally
        {
            if (targetThread != 0)
            {
                NativeMethods.AttachThreadInput(currentThread, targetThread, false);
            }

            if (foregroundThread != 0 && foregroundThread != currentThread)
            {
                NativeMethods.AttachThreadInput(foregroundThread, currentThread, false);
            }
        }
    }

    public PixelRect GetRect(WindowId id)
    {
        if (!NativeMethods.GetWindowRect(id.Value, out var rect))
        {
            return default;
        }

        return new PixelRect(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    public bool IsMinimized(WindowId id) => NativeMethods.IsWindow(id.Value) && NativeMethods.IsIconic(id.Value);

    public bool IsAlive(WindowId id) => id.Value != 0 && NativeMethods.IsWindow(id.Value);

    public WindowId GetForeground() => new(NativeMethods.GetForegroundWindow());

    public bool IsFullscreenForeground()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == 0 || !NativeMethods.IsWindow(hwnd))
        {
            return false;
        }

        var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlStyle);
        var hasCaption = (style & NativeMethods.WsCaption) == NativeMethods.WsCaption;
        if (!NativeMethods.GetWindowRect(hwnd, out var rect))
        {
            return false;
        }

        var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MonitorDefaultToNearest);
        var info = CreateMonitorInfo();
        if (monitor == 0 || !NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        var window = new PixelRect(rect.Left, rect.Top, rect.Width, rect.Height);
        var bounds = new PixelRect(info.Monitor.Left, info.Monitor.Top, info.Monitor.Width, info.Monitor.Height);
        return FullscreenClassifier.IsBorderlessFullscreen(window, bounds, hasCaption);
    }

    public bool TryGetIntegrityBlocked(WindowId id)
    {
        if (!NativeMethods.IsWindow(id.Value))
        {
            return true;
        }

        NativeMethods.GetWindowThreadProcessId(id.Value, out var pid);
        if (pid == 0)
        {
            return true;
        }

        var target = ReadIntegrity(pid);
        if (target is null)
        {
            return true;
        }

        var current = ReadIntegrity((uint)Environment.ProcessId);
        if (current is null)
        {
            return false;
        }

        return target.Value > current.Value;
    }

    public byte[]? TryGetIconPng(WindowId id)
    {
        var path = TryGetProcessPath(id.Value);
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var iconHandle = NativeMethods.ExtractIcon(IntPtr.Zero, path, 0);
        if (iconHandle == IntPtr.Zero || iconHandle == new IntPtr(1))
        {
            return null;
        }

        try
        {
            using var icon = Icon.FromHandle(iconHandle);
            using var bitmap = icon.ToBitmap();
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
        catch (ArgumentException)
        {
            return null;
        }
        finally
        {
            NativeMethods.DestroyIcon(iconHandle);
        }
    }

    public static bool HasTopmostStyle(WindowId id)
    {
        var style = NativeMethods.GetWindowLongPtr(id.Value, NativeMethods.GwlExStyle);
        return (style & NativeMethods.WsExTopmost) != 0;
    }

    public static WindowSnapshot? Describe(IntPtr hwnd) => TrySnapshot(hwnd);

    private static WindowSnapshot? TrySnapshot(IntPtr hwnd)
    {
        if (!NativeMethods.IsWindow(hwnd))
        {
            return null;
        }

        var title = ReadText(hwnd);
        var className = ReadClass(hwnd);
        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        var root = NativeMethods.GetAncestor(hwnd, NativeMethods.GaRoot);
        var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlExStyle);
        var cloaked = 0;
        NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DwmwaCloaked, out cloaked, sizeof(int));
        return new WindowSnapshot(
            new WindowId(hwnd),
            (int)pid,
            Path.GetFileNameWithoutExtension(TryGetProcessPath(hwnd) ?? ""),
            className,
            title,
            NativeMethods.IsWindowVisible(hwnd),
            cloaked != 0,
            (exStyle & NativeMethods.WsExToolWindow) != 0,
            root != hwnd && root != IntPtr.Zero);
    }

    private static string ReadText(IntPtr hwnd)
    {
        var buffer = new StringBuilder(512);
        NativeMethods.GetWindowText(hwnd, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    private static string ReadClass(IntPtr hwnd)
    {
        var buffer = new StringBuilder(256);
        NativeMethods.GetClassName(hwnd, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    private static string? TryGetProcessPath(IntPtr hwnd)
    {
        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0)
        {
            return null;
        }

        var process = NativeMethods.OpenProcess(NativeMethods.ProcessQueryLimitedInformation, false, pid);
        if (process == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var capacity = 1024;
            var buffer = new StringBuilder(capacity);
            return NativeMethods.QueryFullProcessImageName(process, 0, buffer, ref capacity)
                ? buffer.ToString()
                : null;
        }
        finally
        {
            NativeMethods.CloseHandle(process);
        }
    }

    private static int? ReadIntegrity(uint pid)
    {
        var process = NativeMethods.OpenProcess(NativeMethods.ProcessQueryLimitedInformation, false, pid);
        if (process == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            if (!NativeMethods.OpenProcessToken(process, NativeMethods.TokenQuery, out var token))
            {
                return null;
            }

            try
            {
                NativeMethods.GetTokenInformation(token, NativeMethods.TokenIntegrityLevel, IntPtr.Zero, 0, out var length);
                if (length <= 0)
                {
                    return null;
                }

                var buffer = Marshal.AllocHGlobal(length);
                try
                {
                    if (!NativeMethods.GetTokenInformation(token, NativeMethods.TokenIntegrityLevel, buffer, length, out _))
                    {
                        return null;
                    }

                    var label = Marshal.PtrToStructure<NativeMethods.TokenMandatoryLabel>(buffer);
                    var countPtr = NativeMethods.GetSidSubAuthorityCount(label.Label.Sid);
                    var count = Marshal.ReadByte(countPtr);
                    if (count == 0)
                    {
                        return null;
                    }

                    var ridPtr = NativeMethods.GetSidSubAuthority(label.Label.Sid, (uint)(count - 1));
                    return Marshal.ReadInt32(ridPtr);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            finally
            {
                NativeMethods.CloseHandle(token);
            }
        }
        finally
        {
            NativeMethods.CloseHandle(process);
        }
    }

    private static NativeMethods.WindowPlacement CreatePlacement()
    {
        return new NativeMethods.WindowPlacement
        {
            Length = Marshal.SizeOf<NativeMethods.WindowPlacement>()
        };
    }

    private static NativeMethods.MonitorInfoEx CreateMonitorInfo()
    {
        return new NativeMethods.MonitorInfoEx
        {
            Size = Marshal.SizeOf<NativeMethods.MonitorInfoEx>()
        };
    }
}
