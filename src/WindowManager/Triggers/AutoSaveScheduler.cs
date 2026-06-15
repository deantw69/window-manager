using System.Windows.Threading;
using WindowManager.Persistence;

namespace WindowManager.Triggers;

/// <summary>可開關的自動定時儲存排程（依設定間隔觸發）。</summary>
public sealed class AutoSaveScheduler : IDisposable
{
    private readonly DispatcherTimer _timer;

    public event Action? Tick;

    public AutoSaveScheduler()
    {
        _timer = new DispatcherTimer();
        _timer.Tick += (_, _) => Tick?.Invoke();
    }

    /// <summary>依設定啟停與套用間隔。</summary>
    public void Apply(AppSettings settings)
    {
        _timer.Stop();

        if (!settings.AutoSaveEnabled)
            return;

        int seconds = Math.Max(10, settings.AutoSaveIntervalSeconds);
        _timer.Interval = TimeSpan.FromSeconds(seconds);
        _timer.Start();
    }

    public void Dispose() => _timer.Stop();
}
