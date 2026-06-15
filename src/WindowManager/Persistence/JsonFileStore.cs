using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindowManager.Persistence;

/// <summary>
/// 提供原子寫入（tmp → 置換）與安全讀取的 JSON 檔案存取工具。
/// </summary>
public static class JsonFileStore
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    /// <summary>應用程式設定目錄：%AppData%\WindowManager。</summary>
    public static string AppDataDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WindowManager");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>原子寫入：先寫 .tmp 再置換，避免中途中斷毀損既有檔案。</summary>
    public static void WriteAtomic<T>(string path, T value)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var tmp = path + ".tmp";
        var json = JsonSerializer.Serialize(value, Options);
        File.WriteAllText(tmp, json);

        if (File.Exists(path))
            File.Replace(tmp, path, null);
        else
            File.Move(tmp, path);
    }

    /// <summary>
    /// 讀取並反序列化。檔案不存在回傳 default；解析失敗時將毀損檔備份為 .corrupt 後回傳 default。
    /// </summary>
    public static T? ReadOrDefault<T>(string path)
    {
        if (!File.Exists(path))
            return default;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (Exception)
        {
            BackupCorrupt(path);
            return default;
        }
    }

    private static void BackupCorrupt(string path)
    {
        try
        {
            var backup = path + ".corrupt";
            File.Copy(path, backup, overwrite: true);
        }
        catch
        {
            // 備份失敗不影響主流程
        }
    }
}
