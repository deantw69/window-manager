using System.Threading;
using System.Windows;

namespace WindowManager;

public partial class App : System.Windows.Application
{
    private static Mutex? _singleInstanceMutex;
    private AppController? _controller;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 命令列一次性模式（--save / --restore），執行後結束、不常駐
        if (HeadlessRunner.TryRun(e.Args))
        {
            Shutdown();
            return;
        }

        // 單一實例：已有實例執行時直接結束
        _singleInstanceMutex = new Mutex(initiallyOwned: true, "WindowManager.SingleInstance", out bool created);
        if (!created)
        {
            System.Windows.MessageBox.Show("視窗管理員已在執行中。", "視窗管理員",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _controller = new AppController();
        _controller.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _controller?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
