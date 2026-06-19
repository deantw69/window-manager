using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace WindowManager.UI;

/// <summary>系統列圖示與右鍵選單。</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly Icon? _appIcon;

    public event Action? SaveClicked;
    public event Action? RestoreClicked;
    public event Action? ClearClicked;
    public event Action? SettingsClicked;
    public event Action? ExitClicked;

    public TrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("立即儲存佈局", null, (_, _) => SaveClicked?.Invoke());
        menu.Items.Add("立即還原佈局", null, (_, _) => RestoreClicked?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("清除所有記錄", null, (_, _) => ClearClicked?.Invoke());
        menu.Items.Add("設定…", null, (_, _) => SettingsClicked?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("結束", null, (_, _) => ExitClicked?.Invoke());

        _appIcon = LoadAppIcon();
        _icon = new NotifyIcon
        {
            Icon = _appIcon ?? SystemIcons.Application,
            Text = "視窗管理員",
            Visible = true,
            ContextMenuStrip = menu
        };

        _icon.DoubleClick += (_, _) => SettingsClicked?.Invoke();
    }

    /// <summary>載入內嵌的應用程式圖示，失敗則回傳 null 改用系統預設。</summary>
    private static Icon? LoadAppIcon()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("WindowManager.Resources.app.ico");
        return stream is null ? null : new Icon(stream);
    }

    public void ShowInfo(string title, string message)
    {
        _icon.BalloonTipTitle = title;
        _icon.BalloonTipText = message;
        _icon.ShowBalloonTip(3000);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _appIcon?.Dispose();
    }
}
