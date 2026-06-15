namespace WindowManager.Core;

/// <summary>目前畫面上的一個頂層視窗（擷取自系統列舉）。</summary>
public sealed class LiveWindow
{
    public required IntPtr Handle { get; init; }
    public required uint ProcessId { get; init; }

    /// <summary>執行檔完整路徑；取不到時為空字串。</summary>
    public string ExecutablePath { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;

    public ShowState State { get; init; }

    /// <summary>還原（一般）狀態下的位置與大小，螢幕座標。</summary>
    public WindowRect NormalBounds { get; init; }
}

/// <summary>螢幕座標的矩形（左、上、寬、高）。</summary>
public readonly record struct WindowRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
}
