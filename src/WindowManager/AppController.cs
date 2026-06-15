using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using WindowManager.Persistence;
using WindowManager.Services;
using WindowManager.Triggers;
using WindowManager.UI;

namespace WindowManager;

/// <summary>
/// 集中協調：載入設定/佈局、連接觸發機制與系統列、執行儲存與還原。
/// </summary>
public sealed class AppController : IDisposable
{
    private readonly SettingsStore _settingsStore = new();
    private readonly LayoutStore _layoutStore = new();

    private MessageWindow _messageWindow = null!;
    private HotkeyManager _hotkeys = null!;
    private AutoSaveScheduler _autoSave = null!;
    private WindowEventWatcher _watcher = null!;
    private TrayIcon _tray = null!;
    private LayoutCaptureService _capture = null!;
    private readonly LayoutRestoreService _restore = new();

    private AppSettings _settings = new();

    public AppSettings Settings => _settings;

    public void Start()
    {
        _settings = _settingsStore.Load();

        _messageWindow = new MessageWindow();
        _capture = new LayoutCaptureService(_messageWindow.Handle);

        _hotkeys = new HotkeyManager(_messageWindow.Handle);
        _hotkeys.SaveRequested += SaveNow;
        _hotkeys.RestoreRequested += RestoreNow;
        _messageWindow.OnMessage = _hotkeys.ProcessMessage;

        _autoSave = new AutoSaveScheduler();
        _autoSave.Tick += () => SaveNow(silent: true);

        _watcher = new WindowEventWatcher(Dispatcher.CurrentDispatcher);
        _watcher.WindowShown += OnNewWindowShown;

        _tray = new TrayIcon();
        _tray.SaveClicked += SaveNow;
        _tray.RestoreClicked += RestoreNow;
        _tray.ClearClicked += ClearLayouts;
        _tray.SettingsClicked += OpenSettings;
        _tray.ExitClicked += () => System.Windows.Application.Current.Shutdown();

        ApplySettings(_settings);

        if (_settings.RestoreOnStartupEnabled)
        {
            // 啟動後稍候再還原，等使用者既有視窗大致就緒
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            t.Tick += (_, _) => { t.Stop(); RestoreNow(silent: true); };
            t.Start();
        }
    }

    /// <summary>套用設定到各觸發機制（重新註冊熱鍵、啟停定時器與監聽、同步開機啟動）。</summary>
    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;

        var errors = _hotkeys.Apply(settings);
        _autoSave.Apply(settings);
        _watcher.SetEnabled(settings.AutoRestoreNewWindowsEnabled);

        SyncStartup(settings);

        if (errors.Count > 0)
            _tray.ShowInfo("快捷鍵警告", string.Join("\n", errors));
    }

    public void SaveSettings(AppSettings settings)
    {
        _settingsStore.Save(settings);
        ApplySettings(settings);
    }

    private void SyncStartup(AppSettings settings)
    {
        try
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exe))
                StartupManager.SetEnabled(settings.RunAtLogin, exe);
        }
        catch
        {
            // 開機啟動設定失敗不阻斷其他功能
        }
    }

    public void SaveNow() => SaveNow(silent: false);

    private void SaveNow(bool silent)
    {
        try
        {
            var current = _capture.Capture(_settings);
            var existing = _layoutStore.Load();
            var merged = LayoutMerger.Merge(
                existing, current.Windows, DateTime.Now.ToString("o"), _settings.MaxStoredLayouts);
            _layoutStore.Save(merged);
            if (!silent)
                _tray.ShowInfo("已儲存",
                    $"本次記錄 {current.Windows.Count} 個視窗；累計保留 {merged.Windows.Count} 筆");
        }
        catch (Exception ex)
        {
            _tray.ShowInfo("儲存失敗", ex.Message);
        }
    }

    private void ClearLayouts()
    {
        var answer = System.Windows.MessageBox.Show(
            "確定要清除所有已記憶的視窗佈局嗎？此動作無法復原。",
            "清除所有記錄", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
            return;

        _layoutStore.Save(new Persistence.LayoutSet());
        _tray.ShowInfo("已清除", "所有視窗佈局記錄已清空");
    }

    public void RestoreNow() => RestoreNow(silent: false);

    private void RestoreNow(bool silent)
    {
        try
        {
            var set = _layoutStore.Load();
            var summary = _restore.RestoreAll(set, _settings);
            if (!silent)
                _tray.ShowInfo("已還原",
                    $"還原 {summary.Restored} 個，未匹配 {summary.NoMatch} 個，失敗 {summary.Failed} 個");
        }
        catch (Exception ex)
        {
            _tray.ShowInfo("還原失敗", ex.Message);
        }
    }

    private void OnNewWindowShown(IntPtr hWnd)
    {
        try
        {
            var set = _layoutStore.Load();
            _restore.RestoreSingleWindow(hWnd, set, _settings);
        }
        catch
        {
            // 單一視窗還原失敗不影響整體
        }
    }

    private SettingsWindow? _settingsWindow;

    private void OpenSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings);
        _settingsWindow.SettingsSaved += SaveSettings;
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    public void Dispose()
    {
        _hotkeys?.Dispose();
        _autoSave?.Dispose();
        _watcher?.Dispose();
        _tray?.Dispose();
        _messageWindow?.Dispose();
    }
}
