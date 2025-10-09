using UnityEngine;
using System.Collections.Generic;

public class GenerateAlgorithm0 : IGenerateAlgorithm
{
    private int digCount = 0;
    private int maxDig; // 动态计算

    public void Generate(int[,,] maze, int width, int height, int depth)
    {
        // 动态计算maxDig：内部体积的20%
        int internalVolume = (width - 2) * (height - 2) * (depth - 2);
        maxDig = (int)(internalVolume * 0.4f);
        if (maxDig < 10) maxDig = 10; // 最小值

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

    private void DigMaze(int[,,] maze, Vector3Int start, bool[,,] visited, int width, int height, int depth)
    {
        // 使用显式栈的随机深度优先搜索（生成树），但在选定方向上延伸多格形成走廊
        var stack = new System.Collections.Generic.Stack<Vector3Int>();
        stack.Push(start);

        // 标记起点
        visited[start.x, start.y, start.z] = true;
        maze[start.x, start.y, start.z] = (int)MazeData.CellType.Void;
        digCount++;

        var rng = new System.Random();

        // corridor 参数：最长走廊长度（基于地图尺寸）
        int maxCorridor = Mathf.Clamp((width + depth) / 6, 1, 6);

        while (stack.Count > 0 && digCount < maxDig)
        {
            var current = stack.Pop();

            // 找到所有未访问的邻居
            var neighbors = GetNeighbors(current, width, height, depth);
            var unvisited = new System.Collections.Generic.List<Vector3Int>();
            foreach (var n in neighbors)
            {
                if (!visited[n.x, n.y, n.z]) unvisited.Add(n);
            }

            if (unvisited.Count == 0)
            {
                // dead end, 回溯
                continue;
            }

            // 随机打乱未访问邻居
            for (int i = unvisited.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = unvisited[i];
                unvisited[i] = unvisited[j];
                unvisited[j] = tmp;
            }

            // 选择第一个作为方向，并尝试在该方向延伸多格形成走廊
            var chosen = unvisited[0];
            Vector3Int dir = new Vector3Int(chosen.x - current.x, chosen.y - current.y, chosen.z - current.z);

            // 计算走廊长度（至少1）
            int corridorLen = UnityEngine.Random.Range(1, maxCorridor + 1);

            Vector3Int carvePos = current;
            int carvedSteps = 0;
            for (int step = 0; step < corridorLen && digCount < maxDig; step++)
            {
                Vector3Int candidate = new Vector3Int(carvePos.x + dir.x, carvePos.y + dir.y, carvePos.z + dir.z);

                // 检查边界和是否已访问或是外壳（保持表面和底部）
                if (candidate.x <= 0 || candidate.x >= width - 1 || candidate.z <= 0 || candidate.z >= depth - 1 || candidate.y <= 0 || candidate.y >= height - 1)
                {
                    break; // 到达边界或表面，不继续延伸
                }
                if (visited[candidate.x, candidate.y, candidate.z]) break; // 避免穿越已挖通道

                // 挖掘此格
                visited[candidate.x, candidate.y, candidate.z] = true;
                maze[candidate.x, candidate.y, candidate.z] = (int)MazeData.CellType.Void;
                digCount++;
                carvedSteps++;
                carvePos = candidate;
            }

            // 如果至少挖出一步，压入新位置以继续
            if (carvedSteps > 0)
            {
                // 如果当前还有其他未访问邻居，保留回溯点
                if (unvisited.Count > 1)
                {
                    stack.Push(current);
                }
                stack.Push(carvePos);
            }
            else
            {
                // 无法在该方向延伸，尝试下一个未访问邻居（将其放回栈以继续探索）
                for (int i = 1; i < unvisited.Count; i++)
                {
                    stack.Push(unvisited[i]);
                }
            }
        }
    }

    private List<Vector3Int> GetNeighbors(Vector3Int pos, int width, int height, int depth)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        // 水平和前后方向总是添加
        if (pos.x > 1) neighbors.Add(new Vector3Int(pos.x - 1, pos.y, pos.z)); // 左
        if (pos.x < width - 2) neighbors.Add(new Vector3Int(pos.x + 1, pos.y, pos.z)); // 右
        if (pos.z > 1) neighbors.Add(new Vector3Int(pos.x, pos.y, pos.z - 1)); // 前
        if (pos.z < depth - 2) neighbors.Add(new Vector3Int(pos.x, pos.y, pos.z + 1)); // 后
        // 垂直方向随机添加，减少垂直连接
        if (pos.y > 1 && Random.value < 0.2f) neighbors.Add(new Vector3Int(pos.x, pos.y - 1, pos.z)); // 下
        if (pos.y < height - 2 && Random.value < 0.2f) neighbors.Add(new Vector3Int(pos.x, pos.y + 1, pos.z)); // 上
        return neighbors;
    }
}