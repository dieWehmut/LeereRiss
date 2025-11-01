using UnityEngine;

/// <summary>
/// 全局保存迷宫尺寸设置（简单静态配置）
/// MainMenuController 在开始游戏前会把用户输入写入这里，迷宫生成器可以读取这些值。
/// </summary>
public static class MazeSettings
{
    // 默认值，可根据需求调整
    public const int MinWidth = 8;
    public const int MinDepth = 8;
    public const int MinHeight = 5;

    // 最大限制，防止分配过大导致内存崩溃
    public const int MaxWidth = 64;
    public const int MaxDepth = 64;
    public const int MaxHeight = 32;

    public static int Width { get; private set; } = 10;
    public static int Height { get; private set; } = 10;
    public static int Depth { get; private set; } = 10;

    /// <summary>
    /// 应用新的尺寸（会保证最小为1）。
    /// </summary>
    public static void Apply(int width, int height, int depth)
    {
        // 强制最小/最大范围，防止运行时因过大分配导致崩溃
        Width = Mathf.Clamp(width, MinWidth, MaxWidth);
        Height = Mathf.Clamp(height, MinHeight, MaxHeight);
        Depth = Mathf.Clamp(depth, MinDepth, MaxDepth);

        // 保存到 PlayerPrefs，确保切场景后仍可读取（以及用于调试验证）
        SaveToPlayerPrefs();
    }

    /// <summary>
    /// 恢复到默认尺寸（可在此修改默认值）。
    /// </summary>
    public static void ResetToDefaults()
    {
        Width = 10;
        Height = 10;
        Depth = 10;
    }

    // PlayerPrefs keys
    private const string KeyWidth = "MazeWidth";
    private const string KeyHeight = "MazeHeight";
    private const string KeyDepth = "MazeDepth";

    static MazeSettings()
    {
        // 类首次被引用时尝试从 PlayerPrefs 加载先前保存的设置
        LoadFromPlayerPrefs();
    }

    private static void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetInt(KeyWidth, Width);
        PlayerPrefs.SetInt(KeyHeight, Height);
        PlayerPrefs.SetInt(KeyDepth, Depth);
        PlayerPrefs.Save();
    }

    private static void LoadFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey(KeyWidth) && PlayerPrefs.HasKey(KeyHeight) && PlayerPrefs.HasKey(KeyDepth))
        {
            Width = Mathf.Max(1, PlayerPrefs.GetInt(KeyWidth, Width));
            Height = Mathf.Max(1, PlayerPrefs.GetInt(KeyHeight, Height));
            Depth = Mathf.Max(1, PlayerPrefs.GetInt(KeyDepth, Depth));
        }
    }
}
