using UnityEngine;
using System.Collections.Generic;

public class GenerateAlgorithm0 : IGenerateAlgorithm
{
    private const int MaxAttempts = 6;

    private static readonly Vector3Int[] FaceDirections =
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1)
    };

    private static readonly Vector3Int[] CarveDirections =
    {
        new Vector3Int(2, 0, 0),
        new Vector3Int(-2, 0, 0),
        new Vector3Int(0, 2, 0),
        new Vector3Int(0, -2, 0),
        new Vector3Int(0, 0, 2),
        new Vector3Int(0, 0, -2)
    };

    private static readonly Vector3Int[] SideDirections =
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1)
    };

    private readonly List<Vector3Int> carvedBuffer = new List<Vector3Int>();
    private readonly List<Vector3Int> neighborBuffer = new List<Vector3Int>();
    private readonly List<Vector3Int> candidateBuffer = new List<Vector3Int>();
    private readonly Queue<Vector3Int> floodQueue = new Queue<Vector3Int>();

    public void Generate(int[,,] maze, int width, int height, int depth)
    {
        if (maze == null || width <= 0 || height <= 0 || depth <= 0)
        {
            return;
        }

        width = Mathf.Max(3, width);
        height = Mathf.Max(3, height);
        depth = Mathf.Max(3, depth);

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            FillWithSolids(maze, width, height, depth);

            if (TryGenerateMaze(maze, width, height, depth))
            {
                return;
            }
        }

        Debug.LogWarning("Maze generation failed to converge to a connected 3D layout. Falling back to a simple corridor.");
        FallbackGeneration(maze, width, height, depth);
    }

    private bool TryGenerateMaze(int[,,] maze, int width, int height, int depth)
    {
        bool[,,] visited = new bool[width, height, depth];
        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        carvedBuffer.Clear();

        Vector3Int start = PickRandomCarveCell(width, height, depth);
        MarkVisitedAndCarve(maze, visited, start);
        carvedBuffer.Add(start);
        stack.Push(start);

        while (stack.Count > 0)
        {
            Vector3Int current = stack.Peek();
            List<Vector3Int> neighbors = GetUnvisitedCarveNeighbors(current, width, height, depth, visited);

            if (neighbors.Count == 0)
            {
                stack.Pop();
                continue;
            }

            Vector3Int next = neighbors[Random.Range(0, neighbors.Count)];
            CarveBetween(maze, current, next);
            if (MarkVisitedAndCarve(maze, visited, next))
            {
                carvedBuffer.Add(next);
            }
            stack.Push(next);
        }

        AddLoops(maze, width, height, depth, carvedBuffer);
        Vector3Int exitCell = CreateExit(maze, width, height, depth, carvedBuffer);

        return ValidateConnectivity(maze, width, height, depth, exitCell);
    }

    private Vector3Int PickRandomCarveCell(int width, int height, int depth)
    {
        int x = RandomOdd(width);
        int y = RandomOdd(height);
        int z = RandomOdd(depth);
        return new Vector3Int(x, y, z);
    }

    private int RandomOdd(int size)
    {
        int interiorCount = Mathf.Max(1, (size - 1) / 2);
        int index = Random.Range(0, interiorCount);
        return 1 + index * 2;
    }

    private bool MarkVisitedAndCarve(int[,,] maze, bool[,,] visited, Vector3Int cell)
    {
        if (visited[cell.x, cell.y, cell.z])
        {
            return false;
        }

        visited[cell.x, cell.y, cell.z] = true;
        maze[cell.x, cell.y, cell.z] = (int)MazeData.CellType.Void;
        return true;
    }

    private void CarveBetween(int[,,] maze, Vector3Int from, Vector3Int to)
    {
        Vector3Int mid = new Vector3Int((from.x + to.x) / 2, (from.y + to.y) / 2, (from.z + to.z) / 2);
        maze[mid.x, mid.y, mid.z] = (int)MazeData.CellType.Void;
        maze[to.x, to.y, to.z] = (int)MazeData.CellType.Void;
    }

    private List<Vector3Int> GetUnvisitedCarveNeighbors(Vector3Int current, int width, int height, int depth, bool[,,] visited)
    {
        neighborBuffer.Clear();
        for (int i = 0; i < CarveDirections.Length; i++)
        {
            Vector3Int candidate = current + CarveDirections[i];
            if (!IsValidCarveCell(candidate, width, height, depth))
            {
                continue;
            }

            if (!visited[candidate.x, candidate.y, candidate.z])
            {
                neighborBuffer.Add(candidate);
            }
        }
        return neighborBuffer;
    }

    private bool IsValidCarveCell(Vector3Int cell, int width, int height, int depth)
    {
        return cell.x > 0 && cell.x < width - 1 &&
               cell.y > 0 && cell.y < height - 1 &&
               cell.z > 0 && cell.z < depth - 1 &&
               (cell.x & 1) == 1 &&
               (cell.y & 1) == 1 &&
               (cell.z & 1) == 1;
    }

    private void AddLoops(int[,,] maze, int width, int height, int depth, List<Vector3Int> carvedCells)
    {
        if (carvedCells == null || carvedCells.Count < 8)
        {
            return;
        }

        int loopCount = Mathf.Clamp(carvedCells.Count / 24, 1, carvedCells.Count / 6);

        for (int i = 0; i < loopCount; i++)
        {
            Vector3Int anchor = carvedCells[Random.Range(0, carvedCells.Count)];

            for (int j = 0; j < CarveDirections.Length; j++)
            {
                Vector3Int target = anchor + CarveDirections[j];
                if (!IsValidCarveCell(target, width, height, depth))
                {
                    continue;
                }

                Vector3Int mid = new Vector3Int((anchor.x + target.x) / 2, (anchor.y + target.y) / 2, (anchor.z + target.z) / 2);
                if (IsSolidLike(maze[target.x, target.y, target.z]) &&
                    IsSolidLike(maze[mid.x, mid.y, mid.z]))
                {
                    maze[mid.x, mid.y, mid.z] = (int)MazeData.CellType.Void;
                    maze[target.x, target.y, target.z] = (int)MazeData.CellType.Void;
                    carvedCells.Add(target);
                    break;
                }
            }
        }
    }

    private Vector3Int CreateExit(int[,,] maze, int width, int height, int depth, List<Vector3Int> carvedCells)
    {
        candidateBuffer.Clear();

        if (carvedCells != null)
        {
            for (int i = 0; i < carvedCells.Count; i++)
            {
                Vector3Int cell = carvedCells[i];
                for (int d = 0; d < SideDirections.Length; d++)
                {
                    Vector3Int boundary = cell + SideDirections[d];
                    if (IsSideBoundaryCell(boundary, width, height, depth))
                    {
                        candidateBuffer.Add(boundary);
                    }
                }
            }
        }

        Vector3Int exitCell = candidateBuffer.Count > 0
            ? candidateBuffer[Random.Range(0, candidateBuffer.Count)]
            : GetFallbackExit(width, height, depth);

    maze[exitCell.x, exitCell.y, exitCell.z] = (int)MazeData.CellType.Void;

        Vector3Int interior = GetInteriorAdjacent(exitCell, width, height, depth);
        if (IsInsideBounds(interior, width, height, depth) &&
            IsSolidLike(maze[interior.x, interior.y, interior.z]))
        {
            maze[interior.x, interior.y, interior.z] = (int)MazeData.CellType.Void;
            carvedCells?.Add(interior);
        }

        return exitCell;
    }

    private bool ValidateConnectivity(int[,,] maze, int width, int height, int depth, Vector3Int exitCell)
    {
        if (!IsInsideBounds(exitCell, width, height, depth) ||
            maze[exitCell.x, exitCell.y, exitCell.z] != (int)MazeData.CellType.Void)
        {
            return false;
        }

        floodQueue.Clear();
        bool[,,] visited = new bool[width, height, depth];
        floodQueue.Enqueue(exitCell);
        visited[exitCell.x, exitCell.y, exitCell.z] = true;

        int visitedCount = 0;
        while (floodQueue.Count > 0)
        {
            Vector3Int current = floodQueue.Dequeue();
            visitedCount++;

            for (int i = 0; i < FaceDirections.Length; i++)
            {
                Vector3Int neighbor = current + FaceDirections[i];
                if (!IsInsideBounds(neighbor, width, height, depth))
                {
                    continue;
                }

                if (visited[neighbor.x, neighbor.y, neighbor.z])
                {
                    continue;
                }

                if (maze[neighbor.x, neighbor.y, neighbor.z] == (int)MazeData.CellType.Void)
                {
                    visited[neighbor.x, neighbor.y, neighbor.z] = true;
                    floodQueue.Enqueue(neighbor);
                }
            }
        }

        int totalVoids = CountTotalVoids(maze, width, height, depth);
        return visitedCount == totalVoids;
    }

    private int CountTotalVoids(int[,,] maze, int width, int height, int depth)
    {
        int total = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (maze[x, y, z] == (int)MazeData.CellType.Void)
                    {
                        total++;
                    }
                }
            }
        }

        return total;
    }

    private void FillWithSolids(int[,,] maze, int width, int height, int depth)
    {
        // 初始将所有格子设置为 Solid
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

        // 现在从内部（非表面）随机选取一部分格子设为 Ethereal。
        // 要求：Ethereal 数量为原始 Solid 数的 2%（并使 Solid 数变为原来的 98%）
        int originalSolidCount = width * height * depth;
        int etherealCount = Mathf.RoundToInt(originalSolidCount * 0.02f);

        // 收集可放置 Ethereal 的候选内格（排除任何表面格子）
        List<Vector3Int> candidates = new List<Vector3Int>();
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                for (int z = 1; z < depth - 1; z++)
                {
                    candidates.Add(new Vector3Int(x, y, z));
                }
            }
        }

        // 打乱并取前 etherealCount 个位置，若候选数量不足，则尽量多取
        int maxPlace = Mathf.Min(etherealCount, candidates.Count);
        for (int i = 0; i < maxPlace; i++)
        {
            int idx = Random.Range(i, candidates.Count);
            // 交换 i 与 idx
            var tmp = candidates[i];
            candidates[i] = candidates[idx];
            candidates[idx] = tmp;

            Vector3Int pos = candidates[i];
            // 仅当当前位置仍为 Solid 时替换为 Ethereal
            if (maze[pos.x, pos.y, pos.z] == (int)MazeData.CellType.Solid)
            {
                maze[pos.x, pos.y, pos.z] = (int)MazeData.CellType.Ethereal;
            }
        }
    }

    // helper: treat Ethereal the same as Solid for carving/loop logic
    private bool IsSolidLike(int cellValue)
    {
        return cellValue == (int)MazeData.CellType.Solid || cellValue == (int)MazeData.CellType.Ethereal;
    }

    private void FallbackGeneration(int[,,] maze, int width, int height, int depth)
    {
        FillWithSolids(maze, width, height, depth);
        int y = EnsureOddInterior(Mathf.Clamp(height / 2, 1, height - 2), height);
        int z = EnsureOddInterior(Mathf.Clamp(depth / 2, 1, depth - 2), depth);
        for (int x = 1; x < width - 1; x++)
        {
            maze[x, y, z] = (int)MazeData.CellType.Void;
        }

        maze[width - 1, y, z] = (int)MazeData.CellType.Void;
    }

    private int EnsureOddInterior(int value, int size)
    {
        value = Mathf.Clamp(value, 1, size - 2);
        if ((value & 1) == 0)
        {
            value = Mathf.Clamp(value + 1, 1, size - 2);
            if ((value & 1) == 0)
            {
                value = Mathf.Clamp(value - 1, 1, size - 2);
            }
        }
        return Mathf.Max(1, Mathf.Min(size - 2, value));
    }

    private bool IsSideBoundaryCell(Vector3Int cell, int width, int height, int depth)
    {
        bool onSide = (cell.x == 0 || cell.x == width - 1 || cell.z == 0 || cell.z == depth - 1);
        return onSide && cell.y > 0 && cell.y < height - 1;
    }

    private bool IsInsideBounds(Vector3Int cell, int width, int height, int depth)
    {
        return cell.x >= 0 && cell.x < width &&
               cell.y >= 0 && cell.y < height &&
               cell.z >= 0 && cell.z < depth;
    }

    private Vector3Int GetFallbackExit(int width, int height, int depth)
    {
        int y = EnsureOddInterior(Mathf.Clamp(height / 2, 1, height - 2), height);
        int z = EnsureOddInterior(Mathf.Clamp(depth / 2, 1, depth - 2), depth);
        return new Vector3Int(width - 1, y, z);
    }

    private Vector3Int GetInteriorAdjacent(Vector3Int boundary, int width, int height, int depth)
    {
        if (boundary.x == 0)
        {
            return new Vector3Int(1, boundary.y, boundary.z);
        }

        if (boundary.x == width - 1)
        {
            return new Vector3Int(width - 2, boundary.y, boundary.z);
        }

        if (boundary.z == 0)
        {
            return new Vector3Int(boundary.x, boundary.y, 1);
        }

        if (boundary.z == depth - 1)
        {
            return new Vector3Int(boundary.x, boundary.y, depth - 2);
        }

        return boundary;
    }

}