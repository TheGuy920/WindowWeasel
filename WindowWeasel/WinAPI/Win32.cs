using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowWeasel.WinAPI;

internal class Win32
{
    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;
    public const int WS_BORDER = 0x00800000;
    public const int WS_CAPTION = 0x00C00000;
    public const int WS_THICKFRAME = 0x00040000;
    public const int WS_EX_APPWINDOW = 0x00040000;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_LAYERED = 0x00080000;
    public const byte AC_SRC_OVER = 0x00;
    public const byte AC_SRC_ALPHA = 0x01;

    public static bool IsMainWindow(IntPtr hWnd) =>
        Win32.IsTargetWindow(hWnd, IntPtr.Zero);

    public static bool IsTargetWindow(IntPtr hWnd, IntPtr parent) =>
        Win32.GetWindow(hWnd, Win32.GetWindow_Cmd.GW_OWNER) == parent
        && Win32.IsWindowVisible(hWnd)
        ;//&& (Win32.GetWindowLong(hWnd, Win32.GWL_EXSTYLE) & Win32.WS_EX_TOOLWINDOW) == Win32.WS_EX_TOOLWINDOW;

    [DllImport("user32.dll")]
    public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetWindow(IntPtr hWnd, GetWindow_Cmd uCmd);

    internal enum GetWindow_Cmd : uint
    {
        GW_HWNDFIRST = 0,
        GW_HWNDLAST = 1,
        GW_HWNDNEXT = 2,
        GW_HWNDPREV = 3,
        GW_OWNER = 4,
        GW_CHILD = 5,
        GW_ENABLEDPOPUP = 6
    }

    public static List<IntPtr> EnumerateProcessWindows(int processId) =>
        EnumerateProcessWindows((uint)processId);

    public static List<IntPtr> EnumerateProcessWindows(uint processId)
    {
        List<IntPtr> windows = [];

        Win32.EnumChildWindows(IntPtr.Zero, (hWnd, lParam) =>
        {
            _ = Win32.GetWindowThreadProcessId(hWnd, out uint windowProcessId);
            if (windowProcessId == processId)
            {
                //Debug.WriteLine($"[E] Found window: {hWnd} = \"{Win32.GetWindowTitle(hWnd)}\"");
                //Debug.WriteLine($"[ Visible: {Win32.IsWindowVisible(hWnd)}, Parent: {Win32.GetWindow(hWnd, Win32.GetWindow_Cmd.GW_OWNER)} ]");
                windows.Add(hWnd);
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public static IEnumerable<IntPtr> GetChildWindowHandles(IntPtr parent)
    {
        _ = Win32.GetWindowThreadProcessId(parent, out uint processId);
        return Win32.EnumerateProcessWindows(processId).Where(wh => Win32.IsTargetWindow(wh, parent));
    }

    public static string GetWindowClassName(IntPtr hWnd)
    {
        const int nChars = 256;
        StringBuilder Buff = new(nChars);

        if (Win32.GetClassName(hWnd, Buff, nChars) > 0)
            return Buff.ToString();

        return string.Empty;
    }

    public static string GetWindowTitle(IntPtr hWnd)
    {
        const int nChars = 256;
        StringBuilder Buff = new(nChars);

        if (Win32.GetWindowText(hWnd, Buff, nChars) > 0)
            return Buff.ToString();

        return string.Empty;
    }

    public static void RemoveTitleBar(IntPtr hWnd)
    {
        int style = Win32.GetWindowLong(hWnd, GWL_STYLE);
        style = style & ~WS_CAPTION & ~WS_THICKFRAME;
        _ = Win32.SetWindowLong(hWnd, GWL_STYLE, style);
    }
}
