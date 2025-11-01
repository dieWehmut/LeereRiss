using System.Collections.Generic;
using UnityEngine;

public class TeleportMovement : INPCMovementAlgorithm
{
	private NPCMovement movement;
	private CharacterController characterController;
	private NPCPerception perception;
	private float teleportTimer = 0f;
	private readonly float teleportInterval = 5f; // Teleport every 5 seconds
	private readonly float chasingTeleportInterval = 0.8f; // Faster teleport when chasing player

	public void Initialize(NPCMovement movement)
	{
		this.movement = movement;
		teleportTimer = 0f;

		// Get NPCPerception component if available
		if (movement != null)
		{
			perception = movement.GetComponent<NPCPerception>();
			characterController = movement.GetComponent<CharacterController>();
		}
	}

	public void Tick(float deltaTime)
	{
		if (movement == null || characterController == null)
		{
			return;
		}

		// Stop any residual movement from other states
		movement.Input.ClearMoveInput();

		teleportTimer -= deltaTime;

		if (teleportTimer <= 0f)
		{
			// Check if we can see the player
			bool playerSeen = perception != null && perception.PlayerSeen && perception.SeenPlayerTransform != null;

			if (playerSeen)
			{
				// Teleport closer to the player
				TeleportTowardPlayer(perception.SeenPlayerTransform);
				teleportTimer = chasingTeleportInterval;
			}
			else
			{
				// Teleport to a random void cell
				TeleportToRandomVoidCell();
				teleportTimer = teleportInterval;
			}
		}
	}

	private void Teleport(Vector3 targetPosition)
	{
		if (characterController == null || movement == null) return;

		characterController.enabled = false;
		movement.transform.position = targetPosition;
		characterController.enabled = true;
	}

	private void TeleportTowardPlayer(Transform playerTransform)
	{
		if (movement == null || playerTransform == null || movement.Maze == null)
		{
			return;
		}

		// Get player cell position
		Vector3Int playerCell = movement.WorldToCell(playerTransform.position);

		// Gather all void cells that are closer to the player than current position
		Vector3Int currentCell = movement.WorldToCell(movement.transform.position);
		int[,,] maze = movement.Maze;
		List<Vector3Int> candidateCells = new List<Vector3Int>();

		int width = maze.GetLength(0);
		int height = maze.GetLength(1);
		int depth = maze.GetLength(2);

		float currentDistance = Vector3Int.Distance(currentCell, playerCell);

		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				for (int z = 0; z < depth; z++)
				{
					if (maze[x, y, z] == (int)MazeData.CellType.Void)
					{
						Vector3Int cellPos = new Vector3Int(x, y, z);
						float distToPlayer = Vector3Int.Distance(cellPos, playerCell);

						// Only consider cells that are closer to the player than current position
						if (distToPlayer < currentDistance)
						{
							candidateCells.Add(cellPos);
						}
					}
				}
			}
		}

		// If we found cells closer to player, pick one randomly; otherwise teleport to random cell
		if (candidateCells.Count > 0)
		{
			Vector3Int targetCell = candidateCells[Random.Range(0, candidateCells.Count)];
			Vector3 targetWorldPos = movement.CellToWorldCenter(targetCell);
			Teleport(targetWorldPos);
		}
		else
		{
			// Fallback to random teleport if no closer cells found
			TeleportToRandomVoidCell();
		}
	}

	private void TeleportToRandomVoidCell()
	{
		if (movement == null || movement.Maze == null)
		{
			return;
		}

		int[,,] maze = movement.Maze;
		int width = maze.GetLength(0);
		int height = maze.GetLength(1);
		int depth = maze.GetLength(2);

		// Gather all void cells
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

		// Pick a random void cell and teleport there
		if (voidCells.Count > 0)
		{
			Vector3Int randomCell = voidCells[Random.Range(0, voidCells.Count)];
			Vector3 targetWorldPos = movement.CellToWorldCenter(randomCell);
			Teleport(targetWorldPos);
		}
	}
}
