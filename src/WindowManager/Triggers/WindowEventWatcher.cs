using System.Windows.Threading;
using WindowManager.Interop;

namespace WindowManager.Triggers;

/// <summary>
/// 以 SetWinEventHook 監聽新視窗顯示事件，去抖後回報候選 HWND（供自動還原）。
/// </summary>
public sealed class WindowEventWatcher : IDisposable
{
    private readonly NativeMethods.WinEventDelegate _callback;
    private readonly Dispatcher _dispatcher;
    private readonly Dictionary<IntPtr, DispatcherTimer> _pending = new();

    private IntPtr _hook = IntPtr.Zero;
    private bool _enabled;

    /// <summary>去抖延遲：視窗顯示後稍待，避免初始化期間的位置抖動。</summary>
    public TimeSpan Debounce { get; set; } = TimeSpan.FromMilliseconds(400);

    public event Action<IntPtr>? WindowShown;

    public WindowEventWatcher(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _callback = OnWinEvent;
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled == _enabled) return;

        if (enabled)
        {
            _hook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_OBJECT_SHOW,
                NativeMethods.EVENT_OBJECT_SHOW,
                IntPtr.Zero,
                _callback,
                0, 0,
                NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);
            _enabled = _hook != IntPtr.Zero;
        }
        else
        {
            if (_hook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }
            ClearPending();
            _enabled = false;
        }
    }

    private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hWnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        // 僅處理視窗本身（非子物件）的顯示事件
        if (idObject != NativeMethods.OBJID_WINDOW || hWnd == IntPtr.Zero)
            return;

        if (!WindowEnumerator.IsManageable(hWnd))
            return;

        // 切回 UI 執行緒做去抖
        _dispatcher.BeginInvoke(() => Schedule(hWnd));
    }

    private void Schedule(IntPtr hWnd)
    {
        if (_pending.TryGetValue(hWnd, out var existing))
            existing.Stop();

        var timer = new DispatcherTimer { Interval = Debounce };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            _pending.Remove(hWnd);
            if (NativeMethods.IsWindow(hWnd) && WindowEnumerator.IsManageable(hWnd))
                WindowShown?.Invoke(hWnd);
        };
        _pending[hWnd] = timer;
        timer.Start();
    }

    private void ClearPending()
    {
        foreach (var t in _pending.Values) t.Stop();
        _pending.Clear();
    }

    public void Dispose() => SetEnabled(false);
}
