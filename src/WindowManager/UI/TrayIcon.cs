using System.Drawing;
using System.Windows.Forms;

namespace WindowManager.UI;

/// <summary>系統列圖示與右鍵選單。</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;

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

        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "視窗管理員",
            Visible = true,
            ContextMenuStrip = menu
        };

        _icon.DoubleClick += (_, _) => SettingsClicked?.Invoke();
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
    }
}
