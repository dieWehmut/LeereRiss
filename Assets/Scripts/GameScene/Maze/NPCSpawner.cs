using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NPCSpawner : MonoBehaviour
{
	[System.Serializable]
	public class NPCSpawnSettings
	{
		public NPCBase prefab;
		[Min(1)] public int minCount = 1;
		[Min(1)] public int maxCount = 12;
		[Tooltip("Average number of traversable cells per NPC. Smaller values spawn more NPCs.")]
		[Min(1)] public int cellsPerNPC = 80;
	}

	private const int SpawnAlgorithmDefaultIndex = 0;
	private const int BaseSampleCount = 96;
	private const int AdditionalSamplesPerNPC = 12;

	[Header("References")]
	public SpawnLib spawnLib;
	public MazeGenerator mazeGenerator;
	public NPCManager npcManager;
	public MovementLib movementLib;

	[Header("NPC Setup")]
	public List<NPCBase> npcs = new List<NPCBase>();
	[SerializeField, Min(0)] private int spawnAlgorithmIndex = 0;
	[SerializeField] private List<NPCSpawnSettings> spawnSettings = new List<NPCSpawnSettings>();

	private readonly Dictionary<NPCBase, List<NPCBase>> runtimePools = new Dictionary<NPCBase, List<NPCBase>>();
	private readonly Dictionary<NPCBase, Vector3> npcSpawnPositions = new Dictionary<NPCBase, Vector3>();
	private readonly List<Vector3Int> cachedVoidCells = new List<Vector3Int>();
	private readonly List<NPCBase> legacyPrototypes = new List<NPCBase>();

	void Awake()
	{
		if (spawnLib == null) spawnLib = GetComponent<SpawnLib>();
		if (spawnLib == null) spawnLib = FindObjectOfType<SpawnLib>();
		if (mazeGenerator == null) mazeGenerator = GetComponent<MazeGenerator>();
		if (mazeGenerator == null) mazeGenerator = FindObjectOfType<MazeGenerator>();
		if (movementLib == null) movementLib = GetComponent<MovementLib>();
		if (movementLib == null) movementLib = FindObjectOfType<MovementLib>();
		if (npcManager == null) npcManager = NPCManager.Instance != null ? NPCManager.Instance : FindObjectOfType<NPCManager>();
		if (spawnAlgorithmIndex < 0)
		{
			spawnAlgorithmIndex = SpawnAlgorithmDefaultIndex;
		}

		EnsureManagerContext();

		if (npcs.Count == 0)
		{
			npcs.AddRange(GetComponentsInChildren<NPCBase>(true));
		}

		legacyPrototypes.Clear();
		foreach (NPCBase npc in npcs)
		{
			if (npc == null || legacyPrototypes.Contains(npc))
			{
				continue;
			}
			legacyPrototypes.Add(npc);
		}

		EnsureSpawnSettingsInitialized();
	}

	void Start()
	{
		SpawnAll();
	}

	public void SpawnAll()
	{
		EnsureSpawnSettingsInitialized();

		List<NPCBase> spawnList;
		if (spawnSettings != null && spawnSettings.Count > 0)
		{
			spawnList = BuildRuntimeSpawnList();
		}
		else
		{
			spawnList = new List<NPCBase>();
			foreach (NPCBase npc in npcs)
			{
				if (npc != null)
				{
					spawnList.Add(npc);
				}
			}
		}

		if (spawnList.Count == 0)
		{
			return;
		}

		npcSpawnPositions.Clear();

		foreach (NPCBase npc in spawnList)
		{
			Spawn(npc);
		}
	}

	public void Spawn(NPCBase npc)
	{
		if (npc == null)
		{
			return;
		}

		npcSpawnPositions.Remove(npc);

		Vector3 spawnPosition;
		bool positioned = TryFindDistributedSpawnPosition(npc, out spawnPosition);
		if (positioned)
		{
			npc.transform.position = spawnPosition;
		}
		else if (spawnLib != null && mazeGenerator != null)
		{
			spawnLib.RunSpawn(npc.gameObject, mazeGenerator, spawnAlgorithmIndex);
		}
		else if (mazeGenerator != null)
		{
			npc.transform.position = mazeGenerator.GetRandomVoidPosition();
		}

		if (!npc.gameObject.activeSelf)
		{
			npc.gameObject.SetActive(true);
		}

		npcSpawnPositions[npc] = npc.transform.position;

		EnsureManagerContext();
		if (npcManager != null)
		{
			npcManager.RegisterNPC(npc);
		}
	}

	private void EnsureManagerContext()
	{
		if (npcManager == null)
		{
			npcManager = NPCManager.Instance != null ? NPCManager.Instance : FindObjectOfType<NPCManager>();
		}

		if (npcManager != null)
		{
			if (npcManager.mazeGenerator == null && mazeGenerator != null)
			{
				npcManager.mazeGenerator = mazeGenerator;
			}
			if (npcManager.movementLib == null && movementLib != null)
			{
				npcManager.movementLib = movementLib;
			}
		}
	}

	private void EnsureSpawnSettingsInitialized()
	{
		if (spawnSettings == null)
		{
			spawnSettings = new List<NPCSpawnSettings>();
		}

		if (spawnSettings.Count == 0 && legacyPrototypes.Count > 0)
		{
			foreach (NPCBase prototype in legacyPrototypes)
			{
				if (prototype == null)
				{
					continue;
				}

				spawnSettings.Add(new NPCSpawnSettings { prefab = prototype });
			}
		}
	}

	private List<NPCBase> BuildRuntimeSpawnList()
	{
		RefreshVoidCellCache();

		List<NPCBase> result = new List<NPCBase>();
		foreach (NPCSpawnSettings settings in spawnSettings)
		{
			if (settings == null || settings.prefab == null)
			{
				continue;
			}

			List<NPCBase> pool = GetOrCreatePool(settings);
			int desiredCount = CalculateTargetCount(settings);

			for (int i = 0; i < desiredCount; i++)
			{
				NPCBase npc = GetOrCreateInstance(settings, pool, i);
				if (npc != null && !result.Contains(npc))
				{
					result.Add(npc);
				}
			}

			for (int i = desiredCount; i < pool.Count; i++)
			{
				NPCBase extra = pool[i];
				if (extra == null)
				{
					continue;
				}

				if (extra.gameObject.activeSelf)
				{
					extra.gameObject.SetActive(false);
				}
			}
		}

		npcs.Clear();
		npcs.AddRange(result);

		return result;
	}

	private List<NPCBase> GetOrCreatePool(NPCSpawnSettings settings)
	{
		if (!runtimePools.TryGetValue(settings.prefab, out List<NPCBase> pool) || pool == null)
		{
			pool = new List<NPCBase>();
			runtimePools[settings.prefab] = pool;

			if (settings.prefab != null && settings.prefab.gameObject.scene.IsValid())
			{
				pool.Add(settings.prefab);
				PreparePrototypeForPooling(settings.prefab);
			}
		}

		return pool;
	}

	private void PreparePrototypeForPooling(NPCBase prototype)
	{
		if (prototype == null)
		{
			return;
		}

		if (!prototype.gameObject.scene.IsValid())
		{
			return;
		}

		if (prototype.transform.parent != transform)
		{
			prototype.transform.SetParent(transform, true);
		}

		if (prototype.gameObject.activeSelf)
		{
			prototype.gameObject.SetActive(false);
		}
	}

	private NPCBase GetOrCreateInstance(NPCSpawnSettings settings, List<NPCBase> pool, int index)
	{
		if (index < pool.Count)
		{
			return pool[index];
		}

		NPCBase template = pool.Count > 0 ? pool[0] : settings.prefab;
		if (template == null)
		{
			return null;
		}

		NPCBase clone = Instantiate(template, transform);
		clone.gameObject.SetActive(false);
		pool.Add(clone);
		return clone;
	}

	private int CalculateTargetCount(NPCSpawnSettings settings)
	{
		int min = Mathf.Max(1, settings.minCount);
		int max = Mathf.Max(min, settings.maxCount);
		int cellsPerNpc = Mathf.Max(1, settings.cellsPerNPC);

		int availableCells = cachedVoidCells.Count > 0 ? cachedVoidCells.Count : Mathf.Max(mazeGenerator != null ? mazeGenerator.width * mazeGenerator.depth : 1, 1);
		int target = Mathf.RoundToInt((float)availableCells / cellsPerNpc);
		target = Mathf.Clamp(target, min, max);

		return target;
	}

	private bool RefreshVoidCellCache()
	{
		cachedVoidCells.Clear();

		if (mazeGenerator == null)
		{
			return false;
		}

		int[,,] maze = mazeGenerator.GetMaze();
		if (maze == null)
		{
			return false;
		}

		int width = maze.GetLength(0);
		int height = maze.GetLength(1);
		int depth = maze.GetLength(2);

		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				for (int z = 0; z < depth; z++)
				{
					if (maze[x, y, z] == (int)MazeData.CellType.Void)
					{
						cachedVoidCells.Add(new Vector3Int(x, y, z));
					}
				}
			}
		}

		return cachedVoidCells.Count > 0;
	}

	private bool TryFindDistributedSpawnPosition(NPCBase npc, out Vector3 position)
	{
		position = Vector3.zero;

		if (mazeGenerator == null)
		{
			return false;
		}

		if (cachedVoidCells.Count == 0)
		{
			RefreshVoidCellCache();
		}

		int candidateCount = cachedVoidCells.Count;
		if (candidateCount == 0)
		{
			return false;
		}

		int sampleBudget = Mathf.Max(1, BaseSampleCount + npcSpawnPositions.Count * AdditionalSamplesPerNPC);

		float bestScore = float.NegativeInfinity;
		Vector3 bestPosition = Vector3.zero;

		for (int i = 0; i < sampleBudget; i++)
		{
			Vector3Int cell = cachedVoidCells[UnityEngine.Random.Range(0, candidateCount)];
			Vector3 candidate = ComputeWorldPosition(npc, cell);
			float score = CalculateDistributionScore(candidate);
			if (score > bestScore)
			{
				bestScore = score;
				bestPosition = candidate;
			}
		}

		if (bestScore <= float.NegativeInfinity)
		{
			return false;
		}

		position = bestPosition;
		return true;
	}

	private float CalculateDistributionScore(Vector3 candidatePosition)
	{
		if (npcSpawnPositions.Count == 0)
		{
			return float.MaxValue;
		}

		Vector2 candidateXZ = new Vector2(candidatePosition.x, candidatePosition.z);
		float minSqr = float.MaxValue;

		foreach (KeyValuePair<NPCBase, Vector3> kvp in npcSpawnPositions)
		{
			Vector2 otherXZ = new Vector2(kvp.Value.x, kvp.Value.z);
			float sqr = (candidateXZ - otherXZ).sqrMagnitude;
			if (sqr < minSqr)
			{
				minSqr = sqr;
			}
		}

		return minSqr;
	}

	private Vector3 ComputeWorldPosition(NPCBase npc, Vector3Int cell)
	{
		Vector3 position = mazeGenerator.CellToWorldCenter(cell);
		float groundY = cell.y * mazeGenerator.LayerHeight;
		float desiredY = groundY + mazeGenerator.LayerHeight * 0.5f;

		CharacterController controller = npc != null ? npc.GetComponent<CharacterController>() : null;
		if (controller != null)
		{
			float halfHeight = Mathf.Max(controller.height * 0.5f, controller.radius);
			desiredY = groundY + halfHeight - controller.center.y;
		}

		position.y = desiredY;
		return position;
	}

#if UNITY_EDITOR
	[ContextMenu("Refresh NPC List")]
	private void RefreshNPCList()
	{
		npcs.Clear();
		npcs.AddRange(GetComponentsInChildren<NPCBase>(true));

		legacyPrototypes.Clear();
		foreach (NPCBase npc in npcs)
		{
			if (npc == null || legacyPrototypes.Contains(npc))
			{
				continue;
			}
			legacyPrototypes.Add(npc);
		}
	}
#endif
}
