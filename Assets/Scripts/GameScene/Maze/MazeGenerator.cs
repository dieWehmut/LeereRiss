using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public class MazeGenerator : MonoBehaviour
{
    public GenerateLib generateLib;
    [Min(3)] public int width = 8;
    [Min(3)] public int depth = 8;
    [Min(3)] public int height = 5;
    public float cellSize = 5f; // 每个格子的大小
    public float layerHeight = 3f; // 层间距

    private int[,,] maze;
    private Vector3Int? exitCell; // 迷宫出口（边界上的一个 Void 单元）

    void Awake()
    {
        // 在任何其他对象的 Awake 之前应用 MainMenu 的设置，保证当其他脚本（例如 Maze）在 Awake 中调用 GenerateMaze 时使用正确尺寸
        width = Mathf.Max(8, MazeSettings.Width);
        depth = Mathf.Max(8, MazeSettings.Depth);
        height = Mathf.Max(5, MazeSettings.Height);

        // 为了避免每次运行都生成完全相同的迷宫，可根据时间初始化随机种子（可选）
        // 你可以注释掉下面这行以使用 Unity 的默认随机行为
        Random.InitState(System.Environment.TickCount);
    }

    void Start()
    {
        // 在生成迷宫前再次确认尺寸（Awake 已设置，但这里做最终保护）
        width = Mathf.Clamp(MazeSettings.Width, MazeSettings.MinWidth, MazeSettings.MaxWidth);
        depth = Mathf.Clamp(MazeSettings.Depth, MazeSettings.MinDepth, MazeSettings.MaxDepth);
        height = Mathf.Clamp(MazeSettings.Height, MazeSettings.MinHeight, MazeSettings.MaxHeight);

        // 检查总体单元数量，防止异常过大造成内存问题
        long totalCells = (long)width * (long)height * (long)depth;
        long maxCells = (long)MazeSettings.MaxWidth * (long)MazeSettings.MaxHeight * (long)MazeSettings.MaxDepth;
        if (totalCells > maxCells)
        {
            Debug.LogWarning($"Requested maze size too large ({totalCells} cells). Clamping to safe maximum.");
            // 简单策略：先尝试缩小高度，再缩小宽/深
            height = MazeSettings.MaxHeight;
            if ((long)width * (long)height * (long)depth > maxCells)
            {
                width = MazeSettings.MaxWidth;
                depth = MazeSettings.MaxDepth;
            }
        }

        Debug.Log($"MazeGenerator.Start: using width={width}, height={height}, depth={depth}");

        GenerateMaze();
    }

    public void GenerateMaze()
    {
        width = Mathf.Max(3, width);
        depth = Mathf.Max(3, depth);
        height = Mathf.Max(3, height);
        maze = new int[width, height, depth];
        if (generateLib != null)
        {
            generateLib.RunGeneration(maze, width, height, depth);
        }

        // 在生成后尝试寻找出口（边界上的 Void）
        exitCell = FindExitCell();
    }

    public int[,,] GetMaze()
    {
        return maze;
    }

    public bool TryGetRandomVoidCell(out Vector3Int cell)
    {
        cell = Vector3Int.zero;
        if (maze == null || maze.Length == 0)
        {
            return false;
        }

        List<Vector3Int> voidCells = new List<Vector3Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (maze[x, y, z] == (int)MazeData.CellType.Void)
                    {
                        voidCells.Add(new Vector3Int(x, y, z));
                    }
                }
            }
        }

        if (voidCells.Count == 0)
        {
            return false;
        }

        cell = voidCells[Random.Range(0, voidCells.Count)];
        return true;
    }

    public Vector3 GetRandomVoidPosition()
    {
        if (TryGetRandomVoidCell(out Vector3Int cell))
        {
            Vector3 position = CellToWorldCenter(cell);
            position.y = cell.y * LayerHeight;
            return position;
        }

        return transform.position;
    }

    public Vector3 CellToWorldCenter(Vector3Int cell)
    {
        float x = (cell.x + 0.5f) * cellSize;
        float y = (cell.y + 0.5f) * LayerHeight;
        float z = (cell.z + 0.5f) * cellSize;
        return new Vector3(x, y, z);
    }

    public bool IsInsideBounds(Vector3Int cell)
    {
        if (maze == null || maze.Length == 0)
        {
            return false;
        }

        return cell.x >= 0 && cell.x < width &&
               cell.y >= 0 && cell.y < height &&
               cell.z >= 0 && cell.z < depth;
    }

    public bool IsVoid(Vector3Int cell)
    {
        if (!IsInsideBounds(cell))
        {
            return false;
        }

        return maze[cell.x, cell.y, cell.z] == (int)MazeData.CellType.Void;
    }

    public float LayerHeight => Mathf.Max(layerHeight, 0.0001f);

    // ===== 出口定位与访问 =====
    private Vector3Int? FindExitCell()
    {
        if (maze == null) return null;

        // 扫描四周边界，寻找第一个 Void 作为出口
        // x 边界
        for (int y = 1; y < height - 1; y++)
        {
            for (int z = 1; z < depth - 1; z++)
            {
                if (maze[0, y, z] == (int)MazeData.CellType.Void) return new Vector3Int(0, y, z);
                if (maze[width - 1, y, z] == (int)MazeData.CellType.Void) return new Vector3Int(width - 1, y, z);
            }
        }
        // z 边界
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                if (maze[x, y, 0] == (int)MazeData.CellType.Void) return new Vector3Int(x, y, 0);
                if (maze[x, y, depth - 1] == (int)MazeData.CellType.Void) return new Vector3Int(x, y, depth - 1);
            }
        }
        return null;
    }

    public bool TryGetExitWorldPosition(out Vector3 position)
    {
        position = default;
        if (exitCell.HasValue)
        {
            position = CellToWorldCenter(exitCell.Value);
            return true;
        }
        return false;
    }
}