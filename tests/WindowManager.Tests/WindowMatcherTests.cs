using WindowManager.Core;
using WindowManager.Persistence;
using Xunit;

namespace WindowManager.Tests;

public class WindowMatcherTests
{
    private static ScreenSignature Sig(int count = 1)
        => new() { Count = count, Bounds = { "0,0,1920,1080" } };

    private static LiveWindow Live(string path, string title, string cls)
        => new()
        {
            Handle = new IntPtr(1),
            ProcessId = 100,
            ExecutablePath = path,
            Title = title,
            ClassName = cls,
            State = ShowState.Normal,
            NormalBounds = new WindowRect(0, 0, 800, 600)
        };

    private static WindowLayout Layout(string path, string title, string cls)
        => new() { ExecutablePath = path, Title = title, ClassName = cls, ScreenSignature = Sig() };

    [Fact]
    public void TitleSimilarity_Identical_IsOne()
        => Assert.Equal(1.0, WindowMatcher.TitleSimilarity("Document.txt", "Document.txt"), 3);

    [Fact]
    public void TitleSimilarity_Substring_IsHigh()
        => Assert.True(WindowMatcher.TitleSimilarity("file - Notepad", "Notepad") >= 0.8);

    [Fact]
    public void TitleSimilarity_Different_IsLow()
        => Assert.True(WindowMatcher.TitleSimilarity("abcdef", "zyxwvu") < 0.3);

    [Fact]
    public void Score_FullMatch_IsHigh()
    {
        var layout = Layout(@"C:\app.exe", "Doc - App", "AppClass");
        var win = Live(@"C:\app.exe", "Doc - App", "AppClass");
        Assert.True(WindowMatcher.Score(layout, win, Sig()) >= 0.95);
    }

    [Fact]
    public void Score_DifferentPath_IsZero()
    {
        var layout = Layout(@"C:\a.exe", "X", "C");
        var win = Live(@"C:\b.exe", "X", "C");
        Assert.Equal(0, WindowMatcher.Score(layout, win, Sig()));
    }

    [Fact]
    public void Score_SamePathDifferentTitle_StillMeetsDefaultThreshold()
    {
        // 同程式同類別、標題不同（如同編輯器不同檔案）仍應達預設門檻 0.6
        var layout = Layout(@"C:\editor.exe", "alpha.cs - Editor", "EditorClass");
        var win = Live(@"C:\editor.exe", "beta.cs - Editor", "EditorClass");
        Assert.True(WindowMatcher.Score(layout, win, Sig()) >= 0.6);
    }

    [Fact]
    public void FindBestMatch_PicksHighestTitleAmongSameProgram()
    {
        var layout = Layout(@"C:\editor.exe", "alpha.cs - Editor", "EditorClass");
        var a = Live(@"C:\editor.exe", "alpha.cs - Editor", "EditorClass");
        var b = Live(@"C:\editor.exe", "gamma.cs - Editor", "EditorClass");

        var best = WindowMatcher.FindBestMatch(layout, new[] { b, a }, Sig(), 0.6);
        Assert.Same(a, best);
    }

    [Fact]
    public void FindBestMatch_NoneAboveThreshold_ReturnsNull()
    {
        var layout = Layout(@"C:\a.exe", "X", "C");
        var win = Live(@"C:\b.exe", "Y", "D");
        Assert.Null(WindowMatcher.FindBestMatch(layout, new[] { win }, Sig(), 0.6));
    }

    [Fact]
    public void IsExcluded_ByExecutableSubstring()
    {
        var settings = new AppSettings { ExcludedExecutables = { "discord" } };
        var win = Live(@"C:\Users\me\AppData\Discord.exe", "X", "C");
        Assert.True(WindowMatcher.IsExcluded(win, settings));
    }

    [Fact]
    public void IsExcluded_ByClassName()
    {
        var settings = new AppSettings { ExcludedClassNames = { "Shell_TrayWnd" } };
        var win = Live(@"C:\x.exe", "X", "Shell_TrayWnd");
        Assert.True(WindowMatcher.IsExcluded(win, settings));
    }

    [Fact]
    public void IsExcluded_NotInList_IsFalse()
    {
        var settings = new AppSettings();
        var win = Live(@"C:\x.exe", "X", "C");
        Assert.False(WindowMatcher.IsExcluded(win, settings));
    }
}
