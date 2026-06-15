using WindowManager.Core;
using WindowManager.Interop;
using WindowManager.Persistence;

namespace WindowManager.Services;

/// <summary>擷取目前所有受管理視窗，組成可持久化的佈局集合。</summary>
public sealed class LayoutCaptureService
{
    private readonly IntPtr _selfHandleToExclude;

    public LayoutCaptureService(IntPtr selfHandleToExclude = default)
    {
        _selfHandleToExclude = selfHandleToExclude;
    }

    public LayoutSet Capture(AppSettings settings)
    {
        var signature = ScreenSignature.Capture();
        var windows = WindowEnumerator.Enumerate(
            _selfHandleToExclude == IntPtr.Zero ? null : _selfHandleToExclude);

        var layouts = new List<WindowLayout>();
        foreach (var win in windows)
        {
            if (WindowMatcher.IsExcluded(win, settings))
                continue;

            layouts.Add(WindowLayout.FromLiveWindow(win, signature));
        }

        return new LayoutSet
        {
            CapturedAt = DateTime.Now.ToString("o"),
            Windows = layouts
        };
    }
}
