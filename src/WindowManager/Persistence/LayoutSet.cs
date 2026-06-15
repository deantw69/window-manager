namespace WindowManager.Persistence;

/// <summary>一整套視窗佈局集合（持久化的最上層物件）。</summary>
public sealed class LayoutSet
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>擷取時間（ISO 8601 字串，避免時區問題）。</summary>
    public string CapturedAt { get; set; } = string.Empty;

    public List<WindowLayout> Windows { get; set; } = new();
}
