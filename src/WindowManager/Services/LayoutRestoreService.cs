using WindowManager.Core;
using WindowManager.Interop;
using WindowManager.Persistence;

namespace WindowManager.Services;

public enum RestoreOutcome { Restored, NoMatch, Failed, Skipped }

public sealed record RestoreItemResult(WindowLayout Layout, RestoreOutcome Outcome, string? Note = null, IntPtr Handle = default);

public sealed record RestoreSummary(IReadOnlyList<RestoreItemResult> Items)
{
    public int Restored => Items.Count(i => i.Outcome == RestoreOutcome.Restored);
    public int NoMatch => Items.Count(i => i.Outcome == RestoreOutcome.NoMatch);
    public int Failed => Items.Count(i => i.Outcome == RestoreOutcome.Failed);
}

/// <summary>將已儲存佈局套用到目前匹配的視窗（含多螢幕夾限與失敗略過）。</summary>
public sealed class LayoutRestoreService
{
    public RestoreSummary RestoreAll(LayoutSet set, AppSettings settings)
    {
        var signature = ScreenSignature.Capture();
        var candidates = WindowEnumerator.Enumerate();
        var usedHandles = new HashSet<IntPtr>();
        var results = new List<RestoreItemResult>();

        foreach (var layout in set.Windows)
        {
            var pool = candidates.Where(c => !usedHandles.Contains(c.Handle));
            var match = WindowMatcher.FindBestMatch(layout, pool, signature, settings.MatchThreshold);

            if (match is null)
            {
                results.Add(new RestoreItemResult(layout, RestoreOutcome.NoMatch));
                continue;
            }

            usedHandles.Add(match.Handle);
            results.Add(ApplyTo(match.Handle, layout));
        }

        return new RestoreSummary(results);
    }

    /// <summary>對單一視窗套用最佳匹配佈局（供新視窗偵測使用）。</summary>
    public RestoreItemResult? RestoreSingleWindow(IntPtr hWnd, LayoutSet set, AppSettings settings)
    {
        var live = WindowEnumerator.Describe(hWnd);
        if (live is null || WindowMatcher.IsExcluded(live, settings))
            return null;

        var signature = ScreenSignature.Capture();

        WindowLayout? bestLayout = null;
        double bestScore = 0;
        foreach (var layout in set.Windows)
        {
            double s = WindowMatcher.Score(layout, live, signature);
            if (s >= settings.MatchThreshold && s > bestScore)
            {
                bestScore = s;
                bestLayout = layout;
            }
        }

        if (bestLayout is null)
            return null;

        return ApplyTo(hWnd, bestLayout);
    }

    private RestoreItemResult ApplyTo(IntPtr hWnd, WindowLayout layout)
    {
        if (!NativeMethods.IsWindow(hWnd))
            return new RestoreItemResult(layout, RestoreOutcome.Skipped, "視窗已關閉");

        var clamped = ScreenClamp.ClampToVisible(layout.Bounds);

        var placement = WINDOWPLACEMENT.Create();
        if (!NativeMethods.GetWindowPlacement(hWnd, ref placement))
            return new RestoreItemResult(layout, RestoreOutcome.Failed, "取得 placement 失敗");

        // 只更新「一般位置」矩形；依視窗「現況」維持其最大化/最小化狀態：
        // 目前最大化的保持最大化、最小化的保持最小化（這些情況下此矩形即還原後的一般位置），
        // 僅一般視窗會直接套用到此位置。還原不改變視窗的最大化/最小化狀態。
        placement.rcNormalPosition = new RECT
        {
            Left = clamped.X,
            Top = clamped.Y,
            Right = clamped.X + clamped.Width,
            Bottom = clamped.Y + clamped.Height
        };

        placement.showCmd = placement.showCmd switch
        {
            NativeMethods.SW_SHOWMAXIMIZED => NativeMethods.SW_SHOWMAXIMIZED,
            NativeMethods.SW_SHOWMINIMIZED => NativeMethods.SW_SHOWMINIMIZED,
            _ => NativeMethods.SW_SHOWNOACTIVATE
        };

        if (!NativeMethods.SetWindowPlacement(hWnd, ref placement))
            return new RestoreItemResult(layout, RestoreOutcome.Failed, "SetWindowPlacement 失敗");

        return new RestoreItemResult(layout, RestoreOutcome.Restored, Handle: hWnd);
    }
}
