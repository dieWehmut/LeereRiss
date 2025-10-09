using UnityEngine;

public class MazeVisualizer : MonoBehaviour
{
    public GameObject[] solidPrefabs; // 多种Solid prefab数组
    public MazeGenerator mazeGenerator;

    private GameObject[,,] visualObjects;

    void Start()
    {
        if (mazeGenerator != null)
        {
            VisualizeMaze();
        }
    }

    public void VisualizeMaze()
    {
        if (solidPrefabs == null || solidPrefabs.Length == 0 || mazeGenerator == null) return;

        int[,,] maze = mazeGenerator.GetMaze();
        if (maze == null) return;

        int width = mazeGenerator.width;
        int height = mazeGenerator.height;
        int depth = mazeGenerator.depth;

        visualObjects = new GameObject[width, height, depth];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (maze[x, y, z] == (int)MazeData.CellType.Solid)
                    {
                        // 随机选择一个Solid prefab
                        GameObject selectedPrefab = solidPrefabs[Random.Range(0, solidPrefabs.Length)];
                        Vector3 position = new Vector3(x, y, z) * mazeGenerator.cellSize;
                        visualObjects[x, y, z] = Instantiate(selectedPrefab, position, Quaternion.identity, transform);
                        visualObjects[x, y, z].transform.localScale = Vector3.one * mazeGenerator.cellSize;
                    }
                }
            }
        }
    }
}