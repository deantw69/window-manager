using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WindowManager.Persistence;

namespace WindowManager;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _original;
    private HotkeyConfig _saveHotkey;
    private HotkeyConfig _restoreHotkey;

    public event Action<AppSettings>? SettingsSaved;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();

        _original = settings;
        _saveHotkey = Clone(settings.SaveHotkey);
        _restoreHotkey = Clone(settings.RestoreHotkey);

        LoadFrom(settings);
    }

    private void LoadFrom(AppSettings s)
    {
        HotkeysEnabledBox.IsChecked = s.HotkeysEnabled;
        AutoSaveEnabledBox.IsChecked = s.AutoSaveEnabled;
        AutoRestoreNewBox.IsChecked = s.AutoRestoreNewWindowsEnabled;
        RestoreOnStartupBox.IsChecked = s.RestoreOnStartupEnabled;
        RestoreMinimizedBox.IsChecked = s.RestoreMinimizedState;
        RunAtLoginBox.IsChecked = s.RunAtLogin;

        IntervalBox.Text = s.AutoSaveIntervalSeconds.ToString(CultureInfo.InvariantCulture);
        ThresholdBox.Text = s.MatchThreshold.ToString(CultureInfo.InvariantCulture);
        MaxStoredBox.Text = s.MaxStoredLayouts.ToString(CultureInfo.InvariantCulture);

        ExcludedExeBox.Text = string.Join(Environment.NewLine, s.ExcludedExecutables);
        ExcludedClassBox.Text = string.Join(Environment.NewLine, s.ExcludedClassNames);

        SaveHotkeyBox.Text = _saveHotkey.ToString();
        RestoreHotkeyBox.Text = _restoreHotkey.ToString();
    }

    private void HotkeyBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;

        // 忽略單獨的修飾鍵
        if (actualKey is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.System)
            return;

        var config = new HotkeyConfig
        {
            Ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control),
            Alt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt),
            Shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
            Win = Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin),
            Key = actualKey.ToString()
        };

        if ((string?)((System.Windows.Controls.TextBox)sender).Tag == "Save")
        {
            _saveHotkey = config;
            SaveHotkeyBox.Text = config.ToString();
        }
        else
        {
            _restoreHotkey = config;
            RestoreHotkeyBox.Text = config.ToString();
        }
    }

    private void ClearHotkey_Click(object sender, RoutedEventArgs e)
    {
        var empty = new HotkeyConfig();
        if ((string?)((FrameworkElement)sender).Tag == "Save")
        {
            _saveHotkey = empty;
            SaveHotkeyBox.Text = empty.ToString();
        }
        else
        {
            _restoreHotkey = empty;
            RestoreHotkeyBox.Text = empty.ToString();
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(IntervalBox.Text, out int interval) || interval < 10)
        {
            System.Windows.MessageBox.Show("自動儲存間隔需為至少 10 的整數（秒）。", "設定錯誤",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(ThresholdBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double threshold)
            || threshold is < 0 or > 1)
        {
            System.Windows.MessageBox.Show("比對門檻需為 0 到 1 之間的數值。", "設定錯誤",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(MaxStoredBox.Text, out int maxStored) || maxStored < 0)
        {
            System.Windows.MessageBox.Show("最大保留筆數需為 0 或正整數。", "設定錯誤",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var s = new AppSettings
        {
            HotkeysEnabled = HotkeysEnabledBox.IsChecked == true,
            AutoSaveEnabled = AutoSaveEnabledBox.IsChecked == true,
            AutoRestoreNewWindowsEnabled = AutoRestoreNewBox.IsChecked == true,
            RestoreOnStartupEnabled = RestoreOnStartupBox.IsChecked == true,
            RestoreMinimizedState = RestoreMinimizedBox.IsChecked == true,
            RunAtLogin = RunAtLoginBox.IsChecked == true,
            AutoSaveIntervalSeconds = interval,
            MatchThreshold = threshold,
            MaxStoredLayouts = maxStored,
            SaveHotkey = _saveHotkey,
            RestoreHotkey = _restoreHotkey,
            ExcludedExecutables = SplitLines(ExcludedExeBox.Text),
            ExcludedClassNames = SplitLines(ExcludedClassBox.Text)
        };

        SettingsSaved?.Invoke(s);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private static List<string> SplitLines(string text)
        => text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
               .Select(l => l.Trim())
               .Where(l => l.Length > 0)
               .ToList();

    private static HotkeyConfig Clone(HotkeyConfig c) => new()
    {
        Ctrl = c.Ctrl, Alt = c.Alt, Shift = c.Shift, Win = c.Win, Key = c.Key
    };
}
