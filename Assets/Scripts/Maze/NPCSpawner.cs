using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
	[Header("References")]
	public SpawnLib spawnLib;
	public MazeGenerator mazeGenerator;
	public NPCManager npcManager;
	public MovementLib movementLib;

	[Header("NPC Setup")]
	public List<NPCBase> npcs = new List<NPCBase>();
	[SerializeField, Min(0)] private int spawnAlgorithmIndex = 0;

	private const int SpawnAlgorithmDefaultIndex = 0;

	void Awake()
	{
		if (spawnLib == null) spawnLib = GetComponent<SpawnLib>();
		if (spawnLib == null) spawnLib = FindObjectOfType<SpawnLib>();
		if (mazeGenerator == null) mazeGenerator = GetComponent<MazeGenerator>();
		if (mazeGenerator == null) mazeGenerator = FindObjectOfType<MazeGenerator>();
		if (movementLib == null) movementLib = GetComponent<MovementLib>();
		if (movementLib == null) movementLib = FindObjectOfType<MovementLib>();
		if (npcManager == null) npcManager = NPCManager.Instance != null ? NPCManager.Instance : FindObjectOfType<NPCManager>();
		spawnAlgorithmIndex = SpawnAlgorithmDefaultIndex;

		// Ensure NPC manager has the same context so NPCs can access map data and algorithms.
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

		if (npcs.Count == 0)
		{
			npcs.AddRange(GetComponentsInChildren<NPCBase>(true));
		}
	}

	void Start()
	{
		SpawnAll();
	}

	public void SpawnAll()
	{
		foreach (NPCBase npc in npcs)
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

		if (!npc.gameObject.activeInHierarchy)
		{
			npc.gameObject.SetActive(true);
		}

		if (spawnLib != null && mazeGenerator != null)
		{
			spawnLib.RunSpawn(npc.gameObject, mazeGenerator, SpawnAlgorithmDefaultIndex);
		}
		else if (mazeGenerator != null)
		{
			npc.transform.position = mazeGenerator.GetRandomVoidPosition();
		}

		if (npcManager == null)
		{
			npcManager = NPCManager.Instance != null ? NPCManager.Instance : FindObjectOfType<NPCManager>();
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

		if (npcManager != null)
		{
			npcManager.RegisterNPC(npc);
		}
	}

#if UNITY_EDITOR
	[ContextMenu("Refresh NPC List")]
	private void RefreshNPCList()
	{
		npcs.Clear();
		npcs.AddRange(GetComponentsInChildren<NPCBase>(true));
	}
#endif
}
