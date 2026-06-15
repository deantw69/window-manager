using WindowManager.Core;
using WindowManager.Triggers;
using Xunit;

namespace WindowManager.Tests;

public class DisplayChangeDetectorTests
{
    private static ScreenSignature Sig(int count, params string[] bounds)
        => new() { Count = count, Bounds = bounds.ToList() };

    private static ScreenSignature Single1080p => Sig(1, "0,0,1920,1080");
    private static ScreenSignature Dual => Sig(2, "0,0,1920,1080", "1920,0,1920,1080");

    [Fact]
    public void IsChanged_FirstCallWithoutReset_ReturnsTrue()
    {
        var d = new DisplayChangeDetector();
        Assert.True(d.IsChanged(Single1080p)); // 尚未初始化視為變更
    }

    [Fact]
    public void AfterReset_SameSignature_ReturnsFalse()
    {
        var d = new DisplayChangeDetector();
        d.Reset(Single1080p);
        Assert.False(d.IsChanged(Single1080p));
    }

    [Fact]
    public void AfterReset_DifferentSignature_ReturnsTrue()
    {
        var d = new DisplayChangeDetector();
        d.Reset(Single1080p);
        Assert.True(d.IsChanged(Dual)); // 插上第二螢幕
    }

    [Fact]
    public void IsChanged_UpdatesBaseline_SoRepeatIsFalse()
    {
        var d = new DisplayChangeDetector();
        d.Reset(Single1080p);

        Assert.True(d.IsChanged(Dual));   // 第一次變更
        Assert.False(d.IsChanged(Dual));  // 同配置再次事件 → 不視為變更
    }

    [Fact]
    public void IsChanged_ResolutionChangeSameCount_ReturnsTrue()
    {
        var d = new DisplayChangeDetector();
        d.Reset(Single1080p);
        Assert.True(d.IsChanged(Sig(1, "0,0,1280,720"))); // 螢幕數相同但解析度改變
    }
}
