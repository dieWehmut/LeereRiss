using UnityEngine;
using System.Collections.Generic;

public class MazeGenerator : MonoBehaviour
{
    public GenerateLib generateLib;
    [Min(3)] public int width = 11;
    [Min(3)] public int depth = 11;
    [Min(3)] public int height = 3;
    public float cellSize = 5f; // 每个格子的大小
    public float layerHeight = 3f; // 层间距

    private int[,,] maze;

    void Start()
    {
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
}