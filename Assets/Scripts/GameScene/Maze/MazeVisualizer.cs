using UnityEngine;

public class MazeVisualizer : MonoBehaviour
{
    public GameObject[] solidPrefabs; // 多种Solid prefab数组
    public GameObject[] etherealPrefabs; // 多种Ethereal prefab数组（可选）
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
    // 至少需要一种 prefab（solid 或 ethereal）来可视化
    bool haveSolid = solidPrefabs != null && solidPrefabs.Length > 0;
    bool haveEthereal = etherealPrefabs != null && etherealPrefabs.Length > 0;
    if ((!haveSolid && !haveEthereal) || mazeGenerator == null) return;

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
                    // 支持 Solid 与 Ethereal 的不同 prefab 组；若对应组未设置则回退到另一组
                    int cell = maze[x, y, z];
                    if (!(cell == (int)MazeData.CellType.Solid || cell == (int)MazeData.CellType.Ethereal))
                    {
                        continue;
                    }

                    GameObject selectedPrefab = null;
                    if (cell == (int)MazeData.CellType.Ethereal)
                    {
                        if (haveEthereal)
                            selectedPrefab = etherealPrefabs[Random.Range(0, etherealPrefabs.Length)];
                        else if (haveSolid)
                            selectedPrefab = solidPrefabs[Random.Range(0, solidPrefabs.Length)];
                    }
                    else // Solid
                    {
                        if (haveSolid)
                            selectedPrefab = solidPrefabs[Random.Range(0, solidPrefabs.Length)];
                        else if (haveEthereal)
                            selectedPrefab = etherealPrefabs[Random.Range(0, etherealPrefabs.Length)];
                    }

                    if (selectedPrefab == null) continue;
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