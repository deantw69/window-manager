using System.Windows.Forms;
using WindowManager.Interop;
using WindowManager.Persistence;

namespace WindowManager.Triggers;

/// <summary>
/// 註冊/解除全域快捷鍵，並將 WM_HOTKEY 轉為事件。需傳入承載訊息的視窗 HWND。
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private const int IdSave = 1;
    private const int IdRestore = 2;

    private readonly IntPtr _hwnd;
    private bool _saveRegistered;
    private bool _restoreRegistered;

    public event Action? SaveRequested;
    public event Action? RestoreRequested;

    public HotkeyManager(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    /// <summary>
    /// 依設定重新註冊快捷鍵；回傳失敗訊息清單（空代表全部成功）。
    /// 失敗時該鍵維持未註冊，不影響另一鍵。
    /// </summary>
    public List<string> Apply(AppSettings settings)
    {
        UnregisterAll();
        var errors = new List<string>();

        if (!settings.HotkeysEnabled)
            return errors;

        _saveRegistered = TryRegister(IdSave, settings.SaveHotkey, "儲存", errors);
        _restoreRegistered = TryRegister(IdRestore, settings.RestoreHotkey, "還原", errors);
        return errors;
    }

    private bool TryRegister(int id, HotkeyConfig config, string label, List<string> errors)
    {
        if (!TryParse(config, out uint mods, out uint vk))
        {
            errors.Add($"{label}快捷鍵設定無效：{config}");
            return false;
        }

        if (!NativeMethods.RegisterHotKey(_hwnd, id, mods | NativeMethods.MOD_NOREPEAT, vk))
        {
            errors.Add($"{label}快捷鍵註冊失敗（可能已被其他程式佔用）：{config}");
            return false;
        }

        return true;
    }

    /// <summary>處理視窗訊息；若為已註冊的熱鍵則觸發對應事件並回傳 true。</summary>
    public bool ProcessMessage(int msg, IntPtr wParam)
    {
        if (msg != NativeMethods.WM_HOTKEY)
            return false;

        int id = wParam.ToInt32();
        switch (id)
        {
            case IdSave:
                SaveRequested?.Invoke();
                return true;
            case IdRestore:
                RestoreRequested?.Invoke();
                return true;
            default:
                return false;
        }
    }

    private void UnregisterAll()
    {
        if (_saveRegistered)
        {
            NativeMethods.UnregisterHotKey(_hwnd, IdSave);
            _saveRegistered = false;
        }
        if (_restoreRegistered)
        {
            NativeMethods.UnregisterHotKey(_hwnd, IdRestore);
            _restoreRegistered = false;
        }
    }

    private static bool TryParse(HotkeyConfig config, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;

        if (config.Ctrl) modifiers |= NativeMethods.MOD_CONTROL;
        if (config.Alt) modifiers |= NativeMethods.MOD_ALT;
        if (config.Shift) modifiers |= NativeMethods.MOD_SHIFT;
        if (config.Win) modifiers |= NativeMethods.MOD_WIN;

        if (string.IsNullOrWhiteSpace(config.Key))
            return false;

        if (!Enum.TryParse<Keys>(config.Key, ignoreCase: true, out var key))
            return false;

        vk = (uint)key;
        return modifiers != 0 && vk != 0;
    }

    public void Dispose() => UnregisterAll();
}
