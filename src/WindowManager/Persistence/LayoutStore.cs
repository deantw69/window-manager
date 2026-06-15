using System.IO;

namespace WindowManager.Persistence;

/// <summary>佈局檔案（layouts.json）的讀寫，含版本檢查與毀損降級。</summary>
public sealed class LayoutStore
{
    private readonly string _path;

    public LayoutStore(string? path = null)
    {
        _path = path ?? System.IO.Path.Combine(JsonFileStore.AppDataDirectory, "layouts.json");
    }

    public string FilePath => _path;

    public void Save(LayoutSet set)
    {
        set.SchemaVersion = LayoutSet.CurrentSchemaVersion;
        JsonFileStore.WriteAtomic(_path, set);
    }

    /// <summary>讀取佈局；不存在、版本不符或毀損時回傳空佈局。</summary>
    public LayoutSet Load()
    {
        var set = JsonFileStore.ReadOrDefault<LayoutSet>(_path);
        if (set is null)
            return new LayoutSet();

        if (set.SchemaVersion != LayoutSet.CurrentSchemaVersion)
        {
            // 版本不符：目前僅支援 v1，保留原檔不覆寫，以空佈局繼續
            return new LayoutSet();
        }

        set.Windows ??= new List<WindowLayout>();
        return set;
    }
}
