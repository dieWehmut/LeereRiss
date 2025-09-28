using UnityEngine;

public class MazeUpdater : MonoBehaviour
{
    public UpdateLib updateLib;
    public MazeGenerator mazeGenerator;

    void Update()
    {
        // 每帧或定时更新迷宫
        if (Input.GetKeyDown(KeyCode.U)) // 按U键更新，作为示例
        {
            UpdateMaze();
        }
    }

    public void UpdateMaze()
    {
        if (updateLib != null && mazeGenerator != null)
        {
            int[,,] maze = mazeGenerator.GetMaze();
            if (maze != null)
            {
                updateLib.RunUpdate(maze, mazeGenerator.width, mazeGenerator.height, mazeGenerator.depth);
            }
        }
    }

    // 可以添加其他更新逻辑
}