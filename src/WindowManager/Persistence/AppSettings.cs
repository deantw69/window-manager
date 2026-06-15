namespace WindowManager.Persistence;

/// <summary>使用者設定（觸發機制開關、快捷鍵、間隔、排除清單等）。</summary>
public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    // --- 觸發機制開關 ---
    public bool HotkeysEnabled { get; set; } = true;
    public bool AutoSaveEnabled { get; set; } = false;
    public bool AutoRestoreNewWindowsEnabled { get; set; } = false;
    public bool RestoreOnStartupEnabled { get; set; } = false;

    /// <summary>開機自動啟動本程式。</summary>
    public bool RunAtLogin { get; set; } = false;

    // --- 快捷鍵（以字串描述，例如 "Ctrl+Alt+S"） ---
    public HotkeyConfig SaveHotkey { get; set; } = new() { Ctrl = true, Alt = true, Key = "S" };
    // 還原預設用 D：實測 Ctrl+Alt+R 常被其他程式佔用，改用較不易衝突的組合（可於設定變更）
    public HotkeyConfig RestoreHotkey { get; set; } = new() { Ctrl = true, Alt = true, Key = "D" };

    /// <summary>自動定時儲存間隔（秒）。</summary>
    public int AutoSaveIntervalSeconds { get; set; } = 300;

    /// <summary>還原最小化視窗時是否保留最小化狀態（否則還原為一般可見）。</summary>
    public bool RestoreMinimizedState { get; set; } = true;

    /// <summary>視窗比對門檻（0~1），達到才視為匹配。</summary>
    public double MatchThreshold { get; set; } = 0.6;

    /// <summary>累加記憶的最大保留筆數，超過則淘汰最舊（0 表示不限制）。</summary>
    public int MaxStoredLayouts { get; set; } = 500;

    /// <summary>排除清單：執行檔路徑（不分大小寫，子字串比對）。</summary>
    public List<string> ExcludedExecutables { get; set; } = new();

    /// <summary>排除清單：視窗類別名。</summary>
    public List<string> ExcludedClassNames { get; set; } = new();
}

/// <summary>單一快捷鍵設定。</summary>
public sealed class HotkeyConfig
{
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public bool Win { get; set; }

    /// <summary>主鍵，例如 "S"、"F9"。對應 <see cref="System.Windows.Forms.Keys"/> 名稱。</summary>
    public string Key { get; set; } = string.Empty;

    public override string ToString()
    {
        var parts = new List<string>();
        if (Ctrl) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        if (Win) parts.Add("Win");
        if (!string.IsNullOrEmpty(Key)) parts.Add(Key);
        return parts.Count == 0 ? "(無)" : string.Join("+", parts);
    }
}
