using UnityEngine;

public class Maze : MonoBehaviour
{
    [Header("References")]
    public MazeData mazeData;
    public MazeGenerator mazeGenerator;
    public MazeUpdater mazeUpdater;
    public MazeVisualizer mazeVisualizer;
    public Player player;

    void Awake()
    {
        if (mazeData == null) mazeData = GetComponent<MazeData>();
        if (mazeGenerator == null) mazeGenerator = GetComponent<MazeGenerator>();
        if (mazeUpdater == null) mazeUpdater = GetComponent<MazeUpdater>();
        if (mazeVisualizer == null) mazeVisualizer = GetComponent<MazeVisualizer>();
        if (player == null) player = FindObjectOfType<Player>();

        // 初始化迷宫生成
        if (mazeGenerator != null)
        {
            mazeGenerator.GenerateMaze();
        }

        // 将Player放置在随机Void位置
        if (player != null && mazeGenerator != null)
        {
            player.transform.position = mazeGenerator.GetRandomVoidPosition();
        }

        // 初始化可视化
        if (mazeVisualizer != null)
        {
            mazeVisualizer.VisualizeMaze();
        }
    }

    void Update()
    {
        // 处理更新逻辑 - 暂时禁用，后续实现
        // if (mazeUpdater != null)
        // {
        //     mazeUpdater.UpdateMaze();
        //     // 如果更新了，通知可视化更新
        //     if (mazeVisualizer != null)
        //     {
        //         mazeVisualizer.UpdateVisualization();
        //     }
        // }
    }

    // 可以添加其他通讯方法
}