using System.Text;
using WindowManager.Core;

namespace WindowManager.Interop;

/// <summary>
/// 列舉並過濾受管理的頂層視窗，組成 <see cref="LiveWindow"/>。
/// </summary>
public static class WindowEnumerator
{
    /// <summary>列舉目前所有「受管理」的頂層視窗。</summary>
    public static List<LiveWindow> Enumerate(IntPtr? excludeHandle = null)
    {
        var result = new List<LiveWindow>();

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (excludeHandle.HasValue && hWnd == excludeHandle.Value)
                return true;

            if (!IsManageable(hWnd))
                return true;

            var win = Describe(hWnd);
            if (win is not null)
                result.Add(win);

            return true;
        }, IntPtr.Zero);

        return result;
    }

    /// <summary>判斷視窗是否值得管理：可見、具標題列、非工具視窗、有標題文字。</summary>
    public static bool IsManageable(IntPtr hWnd)
    {
        if (!NativeMethods.IsWindowVisible(hWnd))
            return false;

        long style = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_STYLE);
        long exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);

        // 工具視窗除非明確標記為 app window，否則略過
        bool isToolWindow = (exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0;
        bool isAppWindow = (exStyle & NativeMethods.WS_EX_APPWINDOW) != 0;
        if (isToolWindow && !isAppWindow)
            return false;

        // 必須有標題列
        if ((style & NativeMethods.WS_CAPTION) == 0)
            return false;

        // 必須有標題文字
        if (NativeMethods.GetWindowTextLength(hWnd) == 0)
            return false;

        return true;
    }

    /// <summary>擷取單一視窗的識別與位置資訊。</summary>
    public static LiveWindow? Describe(IntPtr hWnd)
    {
        if (!NativeMethods.IsWindow(hWnd))
            return null;

        NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);

        var placement = WINDOWPLACEMENT.Create();
        if (!NativeMethods.GetWindowPlacement(hWnd, ref placement))
            return null;

        var rc = placement.rcNormalPosition;
        var bounds = new WindowRect(rc.Left, rc.Top, rc.Right - rc.Left, rc.Bottom - rc.Top);

        return new LiveWindow
        {
            Handle = hWnd,
            ProcessId = pid,
            Title = GetWindowText(hWnd),
            ClassName = GetClassName(hWnd),
            ExecutablePath = GetExecutablePath(pid),
            State = ToShowState(placement.showCmd),
            NormalBounds = bounds
        };
    }

    private static ShowState ToShowState(int showCmd) => showCmd switch
    {
        NativeMethods.SW_SHOWMINIMIZED => ShowState.Minimized,
        NativeMethods.SW_SHOWMAXIMIZED => ShowState.Maximized,
        _ => ShowState.Normal
    };

    private static string GetWindowText(IntPtr hWnd)
    {
        int len = NativeMethods.GetWindowTextLength(hWnd);
        if (len <= 0) return string.Empty;
        var sb = new StringBuilder(len + 1);
        NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string GetClassName(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        NativeMethods.GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    /// <summary>透過 PID 取執行檔路徑；權限不足時回傳空字串（不丟例外）。</summary>
    public static string GetExecutablePath(uint pid)
    {
        if (pid == 0) return string.Empty;

        IntPtr hProcess = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (hProcess == IntPtr.Zero)
            return string.Empty;

        try
        {
            int capacity = 1024;
            var sb = new StringBuilder(capacity);
            if (NativeMethods.QueryFullProcessImageName(hProcess, 0, sb, ref capacity))
                return sb.ToString();
            return string.Empty;
        }
        finally
        {
            NativeMethods.CloseHandle(hProcess);
        }
    }
}
