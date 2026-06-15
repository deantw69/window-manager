using WindowManager.Core;

namespace WindowManager.Persistence;

/// <summary>一筆已儲存的視窗佈局記錄（識別資料 + 位置 + 狀態）。</summary>
public sealed class WindowLayout
{
    public string ExecutablePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;

    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public ShowState State { get; set; }

    /// <summary>擷取當時的螢幕配置簽章。</summary>
    public ScreenSignature ScreenSignature { get; set; } = new();

    /// <summary>此筆記錄最後一次被儲存/更新的時間（ISO 8601），供累加合併淘汰最舊用。</summary>
    public string SavedAt { get; set; } = string.Empty;

    public WindowRect Bounds => new(X, Y, Width, Height);

    public static WindowLayout FromLiveWindow(LiveWindow win, ScreenSignature signature)
        => new()
        {
            ExecutablePath = win.ExecutablePath,
            Title = win.Title,
            ClassName = win.ClassName,
            X = win.NormalBounds.X,
            Y = win.NormalBounds.Y,
            Width = win.NormalBounds.Width,
            Height = win.NormalBounds.Height,
            State = win.State,
            ScreenSignature = signature
        };
}
