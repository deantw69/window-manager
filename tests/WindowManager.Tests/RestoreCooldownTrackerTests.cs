using WindowManager.Services;
using Xunit;

namespace WindowManager.Tests;

public class RestoreCooldownTrackerTests
{
    private static IntPtr H(int v) => new(v);

    [Fact]
    public void Mark_ThenWithinCooldown_IsCoolingDown()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0);
        var t = new RestoreCooldownTracker(TimeSpan.FromSeconds(3), () => now);

        t.Mark(H(100));
        now = now.AddMilliseconds(500); // 模擬還原觸發的 SHOW 事件在去抖後回來
        Assert.True(t.IsCoolingDown(H(100)));
    }

    [Fact]
    public void AfterCooldownElapsed_NotCoolingDown()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0);
        var t = new RestoreCooldownTracker(TimeSpan.FromSeconds(3), () => now);

        t.Mark(H(100));
        now = now.AddSeconds(3); // 達到冷卻時間（>= 視為過期）
        Assert.False(t.IsCoolingDown(H(100)));
    }

    [Fact]
    public void UnmarkedHandle_IsNotCoolingDown()
    {
        var t = new RestoreCooldownTracker(TimeSpan.FromSeconds(3));
        Assert.False(t.IsCoolingDown(H(999)));
    }

    [Fact]
    public void ZeroHandle_IsIgnored()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0);
        var t = new RestoreCooldownTracker(TimeSpan.FromSeconds(3), () => now);

        t.Mark(IntPtr.Zero);
        Assert.False(t.IsCoolingDown(IntPtr.Zero));
    }

    [Fact]
    public void Mark_RefreshesCooldownWindow()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0);
        var t = new RestoreCooldownTracker(TimeSpan.FromSeconds(3), () => now);

        t.Mark(H(100));
        now = now.AddSeconds(2);
        t.Mark(H(100));        // 再次還原，冷卻重新計時
        now = now.AddSeconds(2); // 距首次 4s，但距最近一次僅 2s
        Assert.True(t.IsCoolingDown(H(100)));
    }
}
