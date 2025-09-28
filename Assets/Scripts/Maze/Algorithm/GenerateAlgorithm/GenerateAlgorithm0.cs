using UnityEngine;
using System.Collections.Generic;

public class GenerateAlgorithm0 : IGenerateAlgorithm
{
    private int digCount = 0;
    private const int maxDig = 100; // 限制挖的格子数，生成更迷宫化的路径

    public void Generate(int[,,] maze, int width, int height, int depth)
    {
        digCount = 0;
        // 初始化所有为Solid
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    maze[x, y, z] = (int)MazeData.CellType.Solid;
                }
            }
        }

        // 底部（y=0）保持Solid

        // 随机选择一个出口位置（表面，y>0）
        Vector3Int exitPos = GetRandomExitPosition(width, height, depth);
        maze[exitPos.x, exitPos.y, exitPos.z] = (int)MazeData.CellType.Void;

        // 使用递归回溯生成迷宫路径，从出口开始向内部挖
        bool[,,] visited = new bool[width, height, depth];
        DigMaze(maze, exitPos, visited, width, height, depth);
    }

    private Vector3Int GetRandomExitPosition(int width, int height, int depth)
    {
        // 随机选择面：0=x=0, 1=x=width-1, 2=z=0, 3=z=depth-1, 4=y=height-1
        int face = Random.Range(0, 5);
        int x, y, z;
        switch (face)
        {
            case 0: // x=0, y随机1到height-2, z随机1到depth-2
                x = 0;
                y = Random.Range(1, height - 1);
                z = Random.Range(1, depth - 1);
                break;
            case 1: // x=width-1
                x = width - 1;
                y = Random.Range(1, height - 1);
                z = Random.Range(1, depth - 1);
                break;
            case 2: // z=0
                x = Random.Range(1, width - 1);
                y = Random.Range(1, height - 1);
                z = 0;
                break;
            case 3: // z=depth-1
                x = Random.Range(1, width - 1);
                y = Random.Range(1, height - 1);
                z = depth - 1;
                break;
            case 4: // y=height-1
                x = Random.Range(1, width - 1);
                y = height - 1;
                z = Random.Range(1, depth - 1);
                break;
            default:
                x = 0; y = 1; z = 1;
                break;
        }
        return new Vector3Int(x, y, z);
    }

    private void DigMaze(int[,,] maze, Vector3Int pos, bool[,,] visited, int width, int height, int depth)
    {
        if (digCount >= maxDig) return; // 限制挖的数量

        visited[pos.x, pos.y, pos.z] = true;
        maze[pos.x, pos.y, pos.z] = (int)MazeData.CellType.Void;
        digCount++;

        // 获取邻居（6个方向）
        List<Vector3Int> neighbors = GetNeighbors(pos, width, height, depth);

        // 随机打乱邻居顺序
        for (int i = neighbors.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector3Int temp = neighbors[i];
            neighbors[i] = neighbors[j];
            neighbors[j] = temp;
        }

        // 递归挖未访问的邻居
        foreach (Vector3Int neighbor in neighbors)
        {
            if (!visited[neighbor.x, neighbor.y, neighbor.z])
            {
                DigMaze(maze, neighbor, visited, width, height, depth);
            }
        }
    }

    private List<Vector3Int> GetNeighbors(Vector3Int pos, int width, int height, int depth)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        // 6个方向
        if (pos.x > 1) neighbors.Add(new Vector3Int(pos.x - 1, pos.y, pos.z)); // 左
        if (pos.x < width - 2) neighbors.Add(new Vector3Int(pos.x + 1, pos.y, pos.z)); // 右
        if (pos.y > 1) neighbors.Add(new Vector3Int(pos.x, pos.y - 1, pos.z)); // 下
        if (pos.y < height - 2) neighbors.Add(new Vector3Int(pos.x, pos.y + 1, pos.z)); // 上
        if (pos.z > 1) neighbors.Add(new Vector3Int(pos.x, pos.y, pos.z - 1)); // 前
        if (pos.z < depth - 2) neighbors.Add(new Vector3Int(pos.x, pos.y, pos.z + 1)); // 后
        return neighbors;
    }
}