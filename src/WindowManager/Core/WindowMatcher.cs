using WindowManager.Persistence;

namespace WindowManager.Core;

/// <summary>
/// 視窗比對與評分：以執行檔路徑 + 類別名為主鍵、標題模糊比對為加權、螢幕簽章一致加分。
/// </summary>
public static class WindowMatcher
{
    // 各因子權重，全部加總（含螢幕加分）恰為 1.0，避免飽和夾平使標題差異消失
    private const double PathWeight = 0.45;
    private const double ClassWeight = 0.2;
    private const double TitleWeight = 0.25;
    private const double ScreenBonus = 0.1;

    /// <summary>
    /// 計算佈局與目前視窗的匹配分數（0~1+，已夾限 1）。
    /// 兩者路徑皆非空且不同 → 視為不同程式，回傳 0。
    /// </summary>
    public static double Score(WindowLayout layout, LiveWindow win, ScreenSignature currentSignature)
    {
        bool bothHavePath = !string.IsNullOrEmpty(layout.ExecutablePath)
                            && !string.IsNullOrEmpty(win.ExecutablePath);

        if (bothHavePath &&
            !string.Equals(layout.ExecutablePath, win.ExecutablePath, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        double score = 0;

        if (bothHavePath)
            score += PathWeight; // 已知路徑相同

        if (!string.IsNullOrEmpty(layout.ClassName) &&
            string.Equals(layout.ClassName, win.ClassName, StringComparison.Ordinal))
        {
            score += ClassWeight;
        }

        score += TitleWeight * TitleSimilarity(layout.Title, win.Title);

        // 螢幕配置一致給予小幅加分，協助在多候選中偏好同環境記錄
        if (layout.ScreenSignature.Equals(currentSignature))
            score += ScreenBonus;

        return Math.Min(1.0, score);
    }

    /// <summary>從候選視窗中找出分數最高且達門檻者；找不到回傳 null。</summary>
    public static LiveWindow? FindBestMatch(
        WindowLayout layout,
        IEnumerable<LiveWindow> candidates,
        ScreenSignature currentSignature,
        double threshold)
    {
        LiveWindow? best = null;
        double bestScore = 0;

        foreach (var win in candidates)
        {
            double s = Score(layout, win, currentSignature);
            if (s >= threshold && s > bestScore)
            {
                bestScore = s;
                best = win;
            }
        }

        return best;
    }

    /// <summary>標題相似度（0~1）：忽略大小寫，子字串視為高相似，否則用 Levenshtein 比例。</summary>
    public static double TitleSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1.0;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;

        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();

        if (a == b) return 1.0;
        if (a.Contains(b) || b.Contains(a)) return 0.85;

        int distance = Levenshtein(a, b);
        int maxLen = Math.Max(a.Length, b.Length);
        return maxLen == 0 ? 1.0 : 1.0 - (double)distance / maxLen;
    }

    private static int Levenshtein(string s, string t)
    {
        int n = s.Length, m = t.Length;
        if (n == 0) return m;
        if (m == 0) return n;

        var prev = new int[m + 1];
        var curr = new int[m + 1];

        for (int j = 0; j <= m; j++) prev[j] = j;

        for (int i = 1; i <= n; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= m; j++)
            {
                int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }

        return prev[m];
    }

    /// <summary>判斷視窗是否在排除清單內（依執行檔路徑子字串或類別名完全相符，皆忽略大小寫）。</summary>
    public static bool IsExcluded(LiveWindow win, AppSettings settings)
        => IsExcluded(win.ExecutablePath, win.ClassName, settings);

    public static bool IsExcluded(string executablePath, string className, AppSettings settings)
    {
        if (!string.IsNullOrEmpty(executablePath))
        {
            foreach (var ex in settings.ExcludedExecutables)
            {
                if (!string.IsNullOrWhiteSpace(ex) &&
                    executablePath.Contains(ex, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        if (!string.IsNullOrEmpty(className))
        {
            foreach (var ex in settings.ExcludedClassNames)
            {
                if (string.Equals(ex, className, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
