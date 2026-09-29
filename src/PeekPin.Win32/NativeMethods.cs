using System.Runtime.InteropServices;
using System.Text;

namespace PeekPin;

internal static class NativeMethods
{
    public const int GwlStyle = -16;
    public const int GwlExStyle = -20;
    public const int WsCaption = 0x00C00000;
    public const int WsExToolWindow = 0x00000080;
    public const int WsExTopmost = 0x00000008;
    public const int WsExLayered = 0x00080000;
    public const int WsExTransparent = 0x00000020;
    public const int WsExNoActivate = 0x08000000;
    public const int WsExAppWindow = 0x00040000;
    public const int GaRoot = 2;
    public const int SwShowMinimized = 2;
    public const int SwShowNoActivate = 4;
    public const int SwShowMinNoActive = 7;
    public const int SwRestore = 9;
    public const int SwpNoSize = 0x0001;
    public const int SwpNoMove = 0x0002;
    public const int SwpNoActivate = 0x0010;
    public const int SwpShowWindow = 0x0040;
    public const int SwpFrameChanged = 0x0020;
    public const int HWND_TOPMOST = -1;
    public const int HWND_NOTOPMOST = -2;
    public const uint EventSystemForeground = 0x0003;
    public const uint EventSystemMinimizeStart = 0x0016;
    public const uint EventSystemMinimizeEnd = 0x0017;
    public const uint EventObjectDestroy = 0x8001;
    public const uint EventObjectShow = 0x8002;
    public const uint WineventOutOfContext = 0x0000;
    public const uint WineventSkipOwnProcess = 0x0002;
    public const int ObjidWindow = 0;
    public const int DwmwaCloaked = 14;
    public const uint MonitorDefaultToNearest = 2;
    public const int ErrorAccessDenied = 5;
    public const uint ProcessQueryLimitedInformation = 0x1000;
    public const uint TokenQuery = 0x0008;
    public const int TokenIntegrityLevel = 25;
    public const uint LwaAlpha = 0x2;
    public const uint UlwAlpha = 0x2;
    public const int MonitorinfofPrimary = 1;

    public static readonly IntPtr HwndTopmost = new(-1);
    public static readonly IntPtr HwndNotTopmost = new(-2);

    public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    public delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);

    [StructLayout(LayoutKind.Sequential)]
    public struct WinPoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WinRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;

        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPlacement
    {
        public int Length;
        public int Flags;
        public int ShowCmd;
        public WinPoint MinPosition;
        public WinPoint MaxPosition;
        public WinRect NormalPosition;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MonitorInfoEx
    {
        public int Size;
        public WinRect Monitor;
        public WinRect Work;
        public int Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SidAndAttributes
    {
        public IntPtr Sid;
        public int Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TokenMandatoryLabel
    {
        public SidAndAttributes Label;
    }

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool IsWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll")]
    public static extern bool IsZoomed(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr hwnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll")]
    public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    public static extern nint GetWindowLongPtr(IntPtr hwnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    public static extern nint SetWindowLongPtr(IntPtr hwnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hwnd, out WinRect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowPlacement(IntPtr hwnd, ref WindowPlacement lpwndpl);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPlacement(IntPtr hwnd, ref WindowPlacement lpwndpl);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromPoint(WinPoint pt, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

    [DllImport("shcore.dll")]
    public static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool GetTokenInformation(IntPtr tokenHandle, int tokenInformationClass, IntPtr tokenInformation, int tokenInformationLength, out int returnLength);

    [DllImport("advapi32.dll")]
    public static extern IntPtr GetSidSubAuthority(IntPtr sid, uint subAuthority);

    [DllImport("advapi32.dll")]
    public static extern IntPtr GetSidSubAuthorityCount(IntPtr sid);

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out WinPoint lpPoint);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(WinPoint point);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

    public const int WmGetIcon = 0x007F;
    public const int IconBig = 1;
    public const int IconSmall = 0;
    public const int GclpHicon = -14;
}
