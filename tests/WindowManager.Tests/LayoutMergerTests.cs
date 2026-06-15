using WindowManager.Core;
using WindowManager.Persistence;
using WindowManager.Services;
using Xunit;

namespace WindowManager.Tests;

public class LayoutMergerTests
{
    private static WindowLayout L(string app, int x, string savedAt = "")
        => new()
        {
            ExecutablePath = $@"C:\{app}.exe",
            ClassName = $"{app}Class",
            Title = app,
            X = x,
            Y = 0,
            Width = 800,
            Height = 600,
            SavedAt = savedAt
        };

    private static LayoutSet Set(params WindowLayout[] w) => new() { Windows = w.ToList() };

    [Fact]
    public void Merge_KeepsWindowsNotCurrentlyOpen()
    {
        // 第一次：1,2,3,4,5
        var first = Set(L("1", 10), L("2", 20), L("3", 30), L("4", 40), L("5", 50));

        // 第二次只開著 1,2,3,5,6（4 已關、6 新開），且 1,2,3,5 位置改變
        var second = new[] { L("1", 110), L("2", 120), L("3", 130), L("5", 150), L("6", 160) };

        var merged = LayoutMerger.Merge(first, second, "2026-01-01T00:00:01", 500);

        var byApp = merged.Windows.ToDictionary(w => w.Title);

        // 全部 1~6 都保留
        Assert.Equal(6, merged.Windows.Count);
        Assert.True(byApp.ContainsKey("4"));

        // 1,2,3,5 為第二次的新位置
        Assert.Equal(110, byApp["1"].X);
        Assert.Equal(150, byApp["5"].X);
        // 6 為新增
        Assert.Equal(160, byApp["6"].X);
        // 4 維持第一次的位置（沒被覆蓋）
        Assert.Equal(40, byApp["4"].X);
    }

    [Fact]
    public void Merge_UpsertByPathClassTitle()
    {
        var existing = Set(L("app", 10, "2026-01-01T00:00:00"));
        var current = new[] { L("app", 99) };

        var merged = LayoutMerger.Merge(existing, current, "2026-01-02T00:00:00", 500);

        Assert.Single(merged.Windows);
        Assert.Equal(99, merged.Windows[0].X); // 同鍵被更新
    }

    [Fact]
    public void Merge_DifferentTitleSameApp_AreSeparateRecords()
    {
        var existing = new LayoutSet
        {
            Windows = { new WindowLayout { ExecutablePath = @"C:\editor.exe", ClassName = "E", Title = "a.cs", X = 1 } }
        };
        var current = new[]
        {
            new WindowLayout { ExecutablePath = @"C:\editor.exe", ClassName = "E", Title = "b.cs", X = 2 }
        };

        var merged = LayoutMerger.Merge(existing, current, "t", 500);
        Assert.Equal(2, merged.Windows.Count); // 不同標題視為不同視窗
    }

    [Fact]
    public void Merge_EvictsOldestWhenExceedingCap()
    {
        var existing = Set(
            L("old1", 1, "2026-01-01T00:00:01"),
            L("old2", 2, "2026-01-01T00:00:02"));
        var current = new[] { L("new", 3) };

        var merged = LayoutMerger.Merge(existing, current, "2026-01-01T00:00:09", maxRecords: 2);

        Assert.Equal(2, merged.Windows.Count);
        Assert.DoesNotContain(merged.Windows, w => w.Title == "old1"); // 最舊被淘汰
        Assert.Contains(merged.Windows, w => w.Title == "new");
    }

    [Fact]
    public void Merge_ZeroCap_NoLimit()
    {
        var existing = Set(L("a", 1, "t1"), L("b", 2, "t2"), L("c", 3, "t3"));
        var current = new[] { L("d", 4) };

        var merged = LayoutMerger.Merge(existing, current, "t9", maxRecords: 0);
        Assert.Equal(4, merged.Windows.Count);
    }

    [Fact]
    public void Merge_StampsCurrentWindowsWithSavedAt()
    {
        var merged = LayoutMerger.Merge(new LayoutSet(), new[] { L("a", 1) }, "2026-06-15T10:00:00", 500);
        Assert.Equal("2026-06-15T10:00:00", merged.Windows[0].SavedAt);
    }
}
