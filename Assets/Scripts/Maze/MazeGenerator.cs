using UnityEngine;
using System.Collections.Generic;

public class MazeGenerator : MonoBehaviour
{
    public GenerateLib generateLib;
    public int width = 10;
    public int height = 10;
    public int depth = 10;
    public float cellSize = 1f; // 每个格子的大小

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
        List<Vector3> voids = new List<Vector3>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (maze[x, y, z] == (int)MazeData.CellType.Void)
                    {
                        voids.Add(new Vector3(x, y, z));
                    }
                }
            }
        }
        if (voids.Count > 0)
        {
            return voids[Random.Range(0, voids.Count)] * cellSize;
        }
        return Vector3.zero; // 如果没有Void，返回原点
    }

    // 可以添加其他方法，如获取特定位置等
}