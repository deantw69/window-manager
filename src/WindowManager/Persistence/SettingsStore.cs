using System.IO;

namespace WindowManager.Persistence;

/// <summary>設定檔（settings.json）的讀寫，含毀損降級為預設值。</summary>
public sealed class SettingsStore
{
    private readonly string _path;

    public SettingsStore(string? path = null)
    {
        _path = path ?? System.IO.Path.Combine(JsonFileStore.AppDataDirectory, "settings.json");
    }

    public string FilePath => _path;

    public void Save(AppSettings settings)
    {
        settings.SchemaVersion = AppSettings.CurrentSchemaVersion;
        JsonFileStore.WriteAtomic(_path, settings);
    }

    /// <summary>讀取設定；不存在、版本不符或毀損時回傳預設值。</summary>
    public AppSettings Load()
    {
        var settings = JsonFileStore.ReadOrDefault<AppSettings>(_path);
        if (settings is null || settings.SchemaVersion != AppSettings.CurrentSchemaVersion)
            return new AppSettings();

        settings.ExcludedExecutables ??= new List<string>();
        settings.ExcludedClassNames ??= new List<string>();
        settings.SaveHotkey ??= new HotkeyConfig();
        settings.RestoreHotkey ??= new HotkeyConfig();
        return settings;
    }
}
