using System.Drawing;
using System.Windows.Forms;
using WindowManager.Core;

namespace WindowManager.Services;

/// <summary>多螢幕安全：判斷座標是否可見，必要時夾限到最接近的螢幕工作區。</summary>
public static class ScreenClamp
{
    /// <summary>矩形是否與任一螢幕工作區有足夠重疊（標題列可見）。</summary>
    public static bool IsVisibleEnough(WindowRect rect)
    {
        var r = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        foreach (var screen in Screen.AllScreens)
        {
            var inter = Rectangle.Intersect(screen.WorkingArea, r);
            // 至少標題列高度與部分寬度落在工作區內才算可見
            if (inter.Width >= Math.Min(rect.Width, 100) && inter.Height >= 20)
                return true;
        }
        return false;
    }

    /// <summary>若離屏則夾限到最接近的螢幕工作區，確保完整可見；否則原樣回傳。</summary>
    public static WindowRect ClampToVisible(WindowRect rect)
    {
        if (IsVisibleEnough(rect))
            return rect;

        var target = NearestWorkingArea(rect);

        int width = Math.Min(rect.Width, target.Width);
        int height = Math.Min(rect.Height, target.Height);

        int x = Math.Clamp(rect.X, target.Left, Math.Max(target.Left, target.Right - width));
        int y = Math.Clamp(rect.Y, target.Top, Math.Max(target.Top, target.Bottom - height));

        return new WindowRect(x, y, width, height);
    }

    private static Rectangle NearestWorkingArea(WindowRect rect)
    {
        var center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

        Screen best = Screen.PrimaryScreen ?? Screen.AllScreens[0];
        long bestDist = long.MaxValue;

        foreach (var screen in Screen.AllScreens)
        {
            var wa = screen.WorkingArea;
            var waCenter = new Point(wa.X + wa.Width / 2, wa.Y + wa.Height / 2);
            long dx = waCenter.X - center.X;
            long dy = waCenter.Y - center.Y;
            long dist = dx * dx + dy * dy;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = screen;
            }
        }

        return best.WorkingArea;
    }
}
