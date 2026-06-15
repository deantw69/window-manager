using WindowManager.Core;

namespace WindowManager.Triggers;

/// <summary>
/// 螢幕配置變更的純判定：保存上次簽章，判斷本次是否「確實改變」。
/// 抽出以便單元測試（不含 UI / SystemEvents 相依）。
/// </summary>
public sealed class DisplayChangeDetector
{
    private ScreenSignature? _last;

    /// <summary>設定目前簽章為基準（啟用監聽時呼叫，避免首次事件被誤判為變更）。</summary>
    public void Reset(ScreenSignature current) => _last = current;

    /// <summary>
    /// 傳入目前簽章；若與上次不同（或尚未初始化）視為變更，更新記錄並回傳 true。
    /// </summary>
    public bool IsChanged(ScreenSignature current)
    {
        bool changed = _last is null || !_last.Equals(current);
        _last = current;
        return changed;
    }
}
