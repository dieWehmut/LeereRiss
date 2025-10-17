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

        ClearExistingVisuals();

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
                    if (maze[x, y, z] != (int)MazeData.CellType.Solid)
                    {
                        continue;
                    }

                    GameObject selectedPrefab = solidPrefabs[Random.Range(0, solidPrefabs.Length)];
                    Vector3 position = mazeGenerator.CellToWorldCenter(new Vector3Int(x, y, z));
                    GameObject instance = Instantiate(selectedPrefab, position, Quaternion.identity, transform);
                    instance.transform.localScale = new Vector3(mazeGenerator.cellSize, mazeGenerator.LayerHeight, mazeGenerator.cellSize);
                    visualObjects[x, y, z] = instance;
                }
            }
        }
    }

    private void ClearExistingVisuals()
    {
        if (visualObjects == null)
        {
            return;
        }

        for (int x = 0; x < visualObjects.GetLength(0); x++)
        {
            for (int y = 0; y < visualObjects.GetLength(1); y++)
            {
                for (int z = 0; z < visualObjects.GetLength(2); z++)
                {
                    if (visualObjects[x, y, z] != null)
                    {
                        Destroy(visualObjects[x, y, z]);
                        visualObjects[x, y, z] = null;
                    }
                }
            }
        }
    }
}