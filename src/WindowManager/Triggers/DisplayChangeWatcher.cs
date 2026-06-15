using Microsoft.Win32;
using System.Windows.Threading;
using WindowManager.Core;

namespace WindowManager.Triggers;

/// <summary>
/// 監聽螢幕配置變更（<see cref="SystemEvents.DisplaySettingsChanged"/>），去抖並比對簽章後，
/// 於配置確實改變時回報 <see cref="DisplayChanged"/>（供自動還原）。
/// </summary>
public sealed class DisplayChangeWatcher : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly Func<ScreenSignature> _signatureProvider;
    private readonly DisplayChangeDetector _detector = new();
    private readonly DispatcherTimer _debounce;

    private bool _enabled;
    private bool _subscribed;

    /// <summary>去抖延遲：螢幕插拔過程會連發多次事件，等配置穩定再判定。</summary>
    public TimeSpan Debounce
    {
        get => _debounce.Interval;
        set => _debounce.Interval = value;
    }

    public event Action? DisplayChanged;

    public DisplayChangeWatcher(Dispatcher dispatcher, Func<ScreenSignature>? signatureProvider = null)
    {
        _dispatcher = dispatcher;
        _signatureProvider = signatureProvider ?? ScreenSignature.Capture;
        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        _debounce.Tick += OnDebounceTick;
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled == _enabled) return;

        if (enabled)
        {
            _detector.Reset(_signatureProvider());
            if (!_subscribed)
            {
                SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
                _subscribed = true;
            }
            _enabled = true;
        }
        else
        {
            if (_subscribed)
            {
                SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
                _subscribed = false;
            }
            _debounce.Stop();
            _enabled = false;
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        // 事件可能在背景執行緒觸發；切回 UI 執行緒重啟去抖計時器。
        _dispatcher.BeginInvoke(() =>
        {
            _debounce.Stop();
            _debounce.Start();
        });
    }

    private void OnDebounceTick(object? sender, EventArgs e)
    {
        _debounce.Stop();
        if (!_enabled) return;

        // 去抖後比對簽章：確實改變才回報，避免事件誤報造成不必要的搬窗。
        if (_detector.IsChanged(_signatureProvider()))
            DisplayChanged?.Invoke();
    }

    public void Dispose() => SetEnabled(false);
}
