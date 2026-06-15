using System.Windows.Forms;
using WindowManager.Core;
using WindowManager.Persistence;
using WindowManager.Services;
using Xunit;

namespace WindowManager.Tests;

public class ScreenAndModelTests
{
    [Fact]
    public void ClampToVisible_OnScreenRect_Unchanged()
    {
        var wa = (Screen.PrimaryScreen ?? Screen.AllScreens[0]).WorkingArea;
        var rect = new WindowRect(wa.X + 50, wa.Y + 50, 400, 300);

        Assert.True(ScreenClamp.IsVisibleEnough(rect));
        Assert.Equal(rect, ScreenClamp.ClampToVisible(rect));
    }

    [Fact]
    public void ClampToVisible_OffScreenRect_MovedIntoVisibleArea()
    {
        var rect = new WindowRect(-50000, -50000, 800, 600);

        Assert.False(ScreenClamp.IsVisibleEnough(rect));

        var clamped = ScreenClamp.ClampToVisible(rect);
        Assert.True(ScreenClamp.IsVisibleEnough(clamped), "夾限後應落在可見區域");
    }

    [Fact]
    public void ScreenSignature_Equality()
    {
        var a = new ScreenSignature { Count = 2, Bounds = { "0,0,1920,1080", "1920,0,1080,1920" } };
        var b = new ScreenSignature { Count = 2, Bounds = { "0,0,1920,1080", "1920,0,1080,1920" } };
        var c = new ScreenSignature { Count = 1, Bounds = { "0,0,1920,1080" } };

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void WindowLayout_FromLiveWindow_MapsFields()
    {
        var sig = new ScreenSignature { Count = 1, Bounds = { "0,0,1920,1080" } };
        var win = new LiveWindow
        {
            Handle = new IntPtr(1),
            ProcessId = 42,
            ExecutablePath = @"C:\app.exe",
            Title = "Title",
            ClassName = "Class",
            State = ShowState.Maximized,
            NormalBounds = new WindowRect(10, 20, 300, 400)
        };

        var layout = WindowLayout.FromLiveWindow(win, sig);

        Assert.Equal(@"C:\app.exe", layout.ExecutablePath);
        Assert.Equal("Title", layout.Title);
        Assert.Equal("Class", layout.ClassName);
        Assert.Equal(ShowState.Maximized, layout.State);
        Assert.Equal(10, layout.X);
        Assert.Equal(20, layout.Y);
        Assert.Equal(300, layout.Width);
        Assert.Equal(400, layout.Height);
        Assert.Equal(sig, layout.ScreenSignature);
    }

    [Fact]
    public void HotkeyConfig_ToString_Formats()
    {
        var hk = new HotkeyConfig { Ctrl = true, Alt = true, Key = "S" };
        Assert.Equal("Ctrl+Alt+S", hk.ToString());
        Assert.Equal("(無)", new HotkeyConfig().ToString());
    }
}
