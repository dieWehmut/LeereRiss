using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NPCManager : MonoBehaviour
{
	public static NPCManager Instance { get; private set; }

	[Header("Shared Context")]
	public MazeGenerator mazeGenerator;
	public MovementLib movementLib;
	[SerializeField] private int defaultMovementAlgorithmIndex = 0;

	private readonly List<NPCBase> activeNPCs = new List<NPCBase>();

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(this);
			return;
		}
		Instance = this;

		if (mazeGenerator == null)
		{
			mazeGenerator = FindObjectOfType<MazeGenerator>();
		}
		if (movementLib == null)
		{
			movementLib = FindObjectOfType<MovementLib>();
		}
	}

	public MazeGenerator MazeGenerator => mazeGenerator;
	public MovementLib MovementLibrary => movementLib;
	public int DefaultMovementAlgorithmIndex => defaultMovementAlgorithmIndex;

	public void RegisterNPC(NPCBase npc)
	{
		if (npc == null)
		{
			return;
		}

		if (!activeNPCs.Contains(npc))
		{
			activeNPCs.Add(npc);
		}

		npc.Initialize(this);
	}

	public void UnregisterNPC(NPCBase npc)
	{
		if (npc == null)
		{
			return;
		}

		activeNPCs.Remove(npc);
		npc.Deinitialize();
	}
}
