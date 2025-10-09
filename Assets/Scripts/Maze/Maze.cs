using UnityEngine;

public class Maze : MonoBehaviour
{
    [Header("References")]
    public MazeData mazeData;
    public MazeGenerator mazeGenerator;
    public MazeVisualizer mazeVisualizer;
    public Player player;

    void Awake()
    {
        if (mazeData == null) mazeData = GetComponent<MazeData>();
    if (mazeGenerator == null) mazeGenerator = GetComponent<MazeGenerator>();
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

}