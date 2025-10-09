using UnityEngine;
using System.Collections.Generic;

public class MazeGenerator : MonoBehaviour
{
    public GenerateLib generateLib;
    public int width = 10;
    public int height = 10;
    public int depth = 10;
    public float cellSize = 5f; // 每个格子的大小

    private int[,,] maze;

    void Start()
    {
        GenerateMaze();
    }

    public void GenerateMaze()
    {
        maze = new int[width, height, depth];
        if (generateLib != null)
        {
            generateLib.RunGeneration(maze, width, height, depth);
        }
    }

    public int[,,] GetMaze()
    {
        return maze;
    }

    public Vector3 GetRandomVoidPosition()
    {
        List<Vector3> supportedVoids = new List<Vector3>();
        List<Vector3> anyVoids = new List<Vector3>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (maze[x, y, z] == (int)MazeData.CellType.Void)
                    {
                        anyVoids.Add(new Vector3(x, y, z));
                        // 支撑条件：y==0(地面)或下方存在Solid
                        if (y == 0 || maze[x, y - 1, z] == (int)MazeData.CellType.Solid)
                        {
                            supportedVoids.Add(new Vector3(x, y, z));
                        }
                    }
                }
            }
        }
        Vector3 chosen;
        if (supportedVoids.Count > 0)
        {
            chosen = supportedVoids[Random.Range(0, supportedVoids.Count)];
        }
        else if (anyVoids.Count > 0)
        {
            chosen = anyVoids[Random.Range(0, anyVoids.Count)];
        }
        else
        {
            chosen = Vector3.zero; // 如果没有Void，返回原点
        }
        // 转换为世界坐标（乘 cellSize）并把玩家放在格子中心
        return (chosen + Vector3.one * 0.5f) * cellSize;
    }

    // 可以添加其他方法，如获取特定位置等
}