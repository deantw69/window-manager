using WindowManager.Persistence;
using WindowManager.Services;

namespace WindowManager;

/// <summary>
/// 命令列一次性模式：WindowManager.exe --save / --restore，執行單次擷取或還原後結束，
/// 不啟動系統列常駐，方便腳本化與驗證。
/// </summary>
public static class HeadlessRunner
{
    /// <summary>若參數含已知模式則執行並回傳 true（呼叫端隨即結束程式）。</summary>
    public static bool TryRun(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            return false;

        var settings = new SettingsStore().Load();

        switch (args[0].ToLowerInvariant())
        {
            case "--save":
                {
                    var store = new LayoutStore();
                    var current = new LayoutCaptureService().Capture(settings);
                    var merged = LayoutMerger.Merge(
                        store.Load(), current.Windows, DateTime.Now.ToString("o"), settings.MaxStoredLayouts);
                    store.Save(merged);
                    return true;
                }
            case "--restore":
                {
                    var set = new LayoutStore().Load();
                    new LayoutRestoreService().RestoreAll(set, settings);
                    return true;
                }
            case "--clear":
                {
                    new LayoutStore().Save(new LayoutSet());
                    return true;
                }
            default:
                return false;
        }
    }
}
