using WindowManager.Persistence;

namespace WindowManager.Services;

/// <summary>
/// 累加記憶：以「執行檔路徑 + 視窗類別名 + 視窗標題」為鍵，將目前開著的視窗 upsert 進既有佈局，
/// 保留目前沒開著（既有記錄中）的視窗，避免一次儲存就遺失未開啟視窗的位置。
/// </summary>
public static class LayoutMerger
{
    private const char Sep = '';

    public static LayoutSet Merge(
        LayoutSet existing,
        IEnumerable<WindowLayout> current,
        string savedAt,
        int maxRecords)
    {
        var map = new Dictionary<string, WindowLayout>(StringComparer.OrdinalIgnoreCase);

        // 先放既有記錄（含目前沒開著的，如關掉的 4 號）
        foreach (var w in existing.Windows)
            map[Key(w)] = w;

        // 再以目前開著的視窗覆蓋／新增，並更新時間戳
        foreach (var w in current)
        {
            w.SavedAt = savedAt;
            map[Key(w)] = w;
        }

        // 依儲存時間新到舊排序；超過上限則淘汰最舊
        var list = map.Values
            .OrderByDescending(w => w.SavedAt, StringComparer.Ordinal)
            .ToList();

        if (maxRecords > 0 && list.Count > maxRecords)
            list = list.Take(maxRecords).ToList();

        return new LayoutSet
        {
            CapturedAt = savedAt,
            Windows = list
        };
    }

    private static string Key(WindowLayout w)
        => string.Concat(
            w.ExecutablePath.ToLowerInvariant(), Sep,
            w.ClassName, Sep,
            w.Title.ToLowerInvariant());
}
