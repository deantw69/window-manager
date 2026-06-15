using System.Windows.Interop;

namespace WindowManager;

/// <summary>
/// 隱藏的訊息視窗，用於承載全域熱鍵的 WM_HOTKEY 訊息。
/// </summary>
public sealed class MessageWindow : IDisposable
{
    private const int HwndMessage = -3;

    private readonly HwndSource _source;

    public IntPtr Handle => _source.Handle;

    /// <summary>處理訊息；回傳 true 表示已處理。</summary>
    public Func<int, IntPtr, bool>? OnMessage { get; set; }

    public MessageWindow()
    {
        var parameters = new HwndSourceParameters("WindowManager.MessageWindow")
        {
            ParentWindow = new IntPtr(HwndMessage),
            WindowStyle = 0
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (OnMessage is not null && OnMessage(msg, wParam))
            handled = true;
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
