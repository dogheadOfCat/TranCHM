using System.Windows;

namespace CHMConverter;

public partial class App : System.Windows.Application
{
    /// <summary>当前主题名称</summary>
    public static string CurrentTheme { get; private set; } = "DarkPurple";

    /// <summary>可用的主题列表</summary>
    public static readonly string[] Themes = { "DarkPurple", "DarkBlue", "LightModern" };

    /// <summary>切换应用程序主题</summary>
    public static void SwitchTheme(string themeName)
    {
        if (CurrentTheme == themeName) return;

        var newDict = new ResourceDictionary
        {
            Source = new Uri($"Themes/{themeName}.xaml", UriKind.Relative)
        };

        var merged = Current.Resources.MergedDictionaries;

        // 移除旧主题（第一个 MergedDictionary）
        for (int i = 0; i < merged.Count; i++)
        {
            var src = merged[i].Source?.OriginalString ?? "";
            if (src.StartsWith("Themes/"))
            {
                merged.RemoveAt(i);
                break;
            }
        }

        // 插入新主题到最前面
        merged.Insert(0, newDict);
        CurrentTheme = themeName;
    }

    /// <summary>切换到下一个主题（循环）</summary>
    public static void CycleTheme()
    {
        var idx = (Array.IndexOf(Themes, CurrentTheme) + 1) % Themes.Length;
        SwitchTheme(Themes[idx]);
    }
}
