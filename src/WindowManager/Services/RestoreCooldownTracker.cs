namespace WindowManager.Services;

/// <summary>
/// 追蹤剛被本程式還原過的視窗 handle，於冷卻時間內視為「應略過」。
/// 用來打斷「還原 → SetWindowPlacement 觸發 EVENT_OBJECT_SHOW → 再次還原」的自我回授迴圈
/// （Windows 11 對視窗顯示/狀態變化發 SHOW 事件特別積極，會放大此問題）。
/// </summary>
public sealed class RestoreCooldownTracker
{
    private const int CleanupThreshold = 256;

    private readonly Dictionary<IntPtr, DateTime> _recent = new();
    private readonly TimeSpan _cooldown;
    private readonly Func<DateTime> _now;

    public RestoreCooldownTracker(TimeSpan cooldown, Func<DateTime>? now = null)
    {
        _cooldown = cooldown;
        _now = now ?? (() => DateTime.Now);
    }

    /// <summary>是否仍在冷卻期內（期內應略過自動還原）。過期項目會順手清除。</summary>
    public bool IsCoolingDown(IntPtr hWnd)
    {
        if (_recent.TryGetValue(hWnd, out var when))
        {
            if (_now() - when < _cooldown)
                return true;
            _recent.Remove(hWnd);
        }
        return false;
    }

    /// <summary>標記 handle 剛被本程式還原。</summary>
    public void Mark(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return;

        var now = _now();
        _recent[hWnd] = now;

        // 視窗關閉後其 handle 不會主動移除，超過門檻時清掉已過期項目避免無限增長。
        if (_recent.Count > CleanupThreshold)
            foreach (var stale in _recent
                         .Where(kv => now - kv.Value >= _cooldown)
                         .Select(kv => kv.Key).ToList())
                _recent.Remove(stale);
    }
}
