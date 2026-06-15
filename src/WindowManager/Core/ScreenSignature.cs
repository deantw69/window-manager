using System.Windows.Forms;

namespace WindowManager.Core;

/// <summary>
/// 螢幕配置簽章：記錄當時螢幕數量與各螢幕工作區，用於判斷還原時環境是否一致。
/// </summary>
public sealed class ScreenSignature : IEquatable<ScreenSignature>
{
    public int Count { get; init; }

    /// <summary>各螢幕工作區，格式 "X,Y,W,H"，依 X 再 Y 排序以保持穩定。</summary>
    public List<string> Bounds { get; init; } = new();

    public static ScreenSignature Capture()
    {
        var bounds = Screen.AllScreens
            .Select(s => s.Bounds)
            .Select(b => $"{b.X},{b.Y},{b.Width},{b.Height}")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        return new ScreenSignature
        {
            Count = Screen.AllScreens.Length,
            Bounds = bounds
        };
    }

    public bool Equals(ScreenSignature? other)
    {
        if (other is null) return false;
        if (Count != other.Count) return false;
        return Bounds.SequenceEqual(other.Bounds);
    }

    public override bool Equals(object? obj) => Equals(obj as ScreenSignature);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Count);
        foreach (var b in Bounds) hash.Add(b);
        return hash.ToHashCode();
    }
}
