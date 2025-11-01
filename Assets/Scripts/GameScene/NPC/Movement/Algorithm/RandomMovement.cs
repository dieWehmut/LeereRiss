using System.Collections.Generic;
using UnityEngine;

public class RandomMovement : INPCMovementAlgorithm
{
	private readonly Vector2[] inputDirections =
	{
		new Vector2(0f, 1f),  // North
		new Vector2(1f, 0f),  // East
		new Vector2(0f, -1f), // South
		new Vector2(-1f, 0f)  // West
	};

	private readonly Vector3Int[] gridOffsets =
	{
		new Vector3Int(0, 0, 1),  // North
		new Vector3Int(1, 0, 0),  // East
		new Vector3Int(0, 0, -1), // South
		new Vector3Int(-1, 0, 0)  // West
	};

	private NPCMovement movement;
	private Vector2 desiredMove = Vector2.zero;
	private int currentDirectionIndex = -1;
	private Vector3Int lastCell;
	private float decisionTimer;
	private float stuckTimer;
	private readonly List<int> reusableDirections = new List<int>(4);

	private const float DECISION_INTERVAL_MIN = 1.0f;
	private const float DECISION_INTERVAL_MAX = 2.5f;
	private const float STUCK_SPEED_THRESHOLD = 0.1f;
	private const float STUCK_TIMEOUT = 1.0f;

	public void Initialize(NPCMovement movement)
	{
		this.movement = movement;
		if (this.movement != null)
		{
			lastCell = this.movement.WorldToCell(this.movement.transform.position);
			decisionTimer = 0f;
			stuckTimer = 0f;
			PickNewDirection(GatherViableDirections(lastCell));
		}
	}

	public void Tick(float deltaTime)
	{
		if (movement == null || movement.Input == null) return;

		decisionTimer -= deltaTime;

		Vector3Int currentCell = movement.WorldToCell(movement.transform.position);
		bool hasMovedToNewCell = currentCell != lastCell;

		Vector3 flatVelocity = new Vector3(movement.Velocity.x, 0, movement.Velocity.z);
		bool isStuck = flatVelocity.sqrMagnitude < STUCK_SPEED_THRESHOLD * STUCK_SPEED_THRESHOLD;

		if (isStuck && desiredMove.sqrMagnitude > 0.1f)
		{
			stuckTimer += deltaTime;
		}
		else
		{
			stuckTimer = 0f;
		}

		var viableDirections = GatherViableDirections(currentCell);
		bool isCurrentDirectionInvalid = currentDirectionIndex != -1 && !viableDirections.Contains(currentDirectionIndex);
		bool needsNewDirection = hasMovedToNewCell || decisionTimer <= 0f || stuckTimer > STUCK_TIMEOUT || isCurrentDirectionInvalid;

		if (needsNewDirection)
		{
			PickNewDirection(viableDirections);
		}
		
		lastCell = currentCell;
		movement.Input.SetMoveInput(desiredMove);
	}

	private List<int> GatherViableDirections(Vector3Int currentCell)
	{
		reusableDirections.Clear();
		if (movement == null) return reusableDirections;

		for (int i = 0; i < gridOffsets.Length; i++)
		{
			if (movement.IsDirectionWalkable(currentCell, gridOffsets[i]))
			{
				reusableDirections.Add(i);
			}
		}
		return reusableDirections;
	}

	private void PickNewDirection(List<int> viableDirections)
	{
		if (viableDirections.Count > 0)
		{
			List<int> preferredDirections = new List<int>(viableDirections);
			// Try to avoid turning back unless it's the only option
			if (preferredDirections.Count > 1 && currentDirectionIndex != -1)
			{
				int oppositeDirection = (currentDirectionIndex + 2) % 4;
				if (preferredDirections.Contains(oppositeDirection))
				{
					preferredDirections.Remove(oppositeDirection);
				}
			}

			// Bias towards continuing forward
			if (preferredDirections.Contains(currentDirectionIndex) && Random.value > 0.2f)
			{
				// Keep current direction, no change to currentDirectionIndex
			}
			else
			{
				currentDirectionIndex = preferredDirections[Random.Range(0, preferredDirections.Count)];
			}
			
			desiredMove = inputDirections[currentDirectionIndex];
		}
		else
		{
			// No viable directions, must turn back
			int opposite = (currentDirectionIndex + 2) % 4;
			currentDirectionIndex = opposite >= 0 ? opposite : Random.Range(0, 4);
			desiredMove = inputDirections[currentDirectionIndex];
		}

		decisionTimer = Random.Range(DECISION_INTERVAL_MIN, DECISION_INTERVAL_MAX);
		stuckTimer = 0f;
	}

	private bool TryGetDirectionIndex(Vector2 move, out int index)
	{
		for (int i = 0; i < inputDirections.Length; i++)
		{
			if (Vector2.Dot(inputDirections[i], move) > 0.99f)
			{
				index = i;
				return true;
			}
		}

		index = -1;
		return false;
	}
}
