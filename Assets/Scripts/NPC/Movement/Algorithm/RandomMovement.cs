using System.Collections.Generic;
using UnityEngine;

public class RandomMovement : INPCMovementAlgorithm
{
	private readonly Vector2[] inputDirections =
	{
		new Vector2(0f, 1f),
		new Vector2(0f, -1f),
		new Vector2(-1f, 0f),
		new Vector2(1f, 0f)
	};

	private readonly Vector3Int[] gridOffsets =
	{
		new Vector3Int(0, 0, 1),
		new Vector3Int(0, 0, -1),
		new Vector3Int(-1, 0, 0),
		new Vector3Int(1, 0, 0)
	};

	private NPCMovement movement;
	private Vector2 desiredMove = Vector2.zero;
	private int currentDirectionIndex = -1; // -1 = no locked direction
	private Vector3Int lastCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
	private float decisionTimer = 0f;
	private float stuckTimer = 0f;
	private readonly List<int> reusableDirections = new List<int>(4);

	// Increased intervals to reduce turning frequency
	private const float minDecisionInterval = 1.2f;
	private const float maxDecisionInterval = 3.0f;
	private const float stuckSpeedThreshold = 0.1f;
	private const float stuckTimeout = 1.2f;
	// Remove jump behavior: keep fields for compatibility but unused
	private const float jumpCooldownDuration = 0f;
	private float jumpCooldownTimer = 0f;

	public void Initialize(NPCMovement movement)
	{
		this.movement = movement;
		if (movement != null)
		{
			lastCell = movement.WorldToCell(movement.transform.position);
			stuckTimer = 0f;
			jumpCooldownTimer = 0f;
			PickNewDirection(GatherViableDirections(lastCell), forceJump: false);
		}
	}

	public void Tick(float deltaTime)
	{
		if (movement == null || movement.Input == null)
		{
			return;
		}

		decisionTimer -= deltaTime;
		// jumpCooldownTimer kept for compatibility but not used for jumping
		if (jumpCooldownTimer > 0f)
		{
			jumpCooldownTimer = Mathf.Max(0f, jumpCooldownTimer - deltaTime);
		}

		Vector3Int currentCell = movement.WorldToCell(movement.transform.position);
		bool cellChanged = currentCell != lastCell;
		lastCell = currentCell;

		Vector3 flatVelocity = new Vector3(movement.Velocity.x, 0f, movement.Velocity.z);
		bool onGround = movement.IsGrounded;
		bool movingSlowly = flatVelocity.sqrMagnitude < stuckSpeedThreshold * stuckSpeedThreshold;
		if (onGround && desiredMove.sqrMagnitude > 0.1f && movingSlowly)
		{
			stuckTimer += deltaTime;
		}
		else
		{
			stuckTimer = 0f;
		}

		List<int> viableDirections = GatherViableDirections(currentCell);
		// If we have a locked direction and it's still walkable, keep it until blocked
		if (currentDirectionIndex >= 0 && movement.IsDirectionWalkable(currentCell, gridOffsets[currentDirectionIndex]))
		{
			desiredMove = inputDirections[currentDirectionIndex];
			// keep decision timer so we don't re-evaluate too soon
			return;
		}
		bool directionBlocked = currentDirectionIndex >= 0 || !IsDirectionStillWalkable(currentCell, desiredMove);
		// We never jump; force decision only when no viable directions or stuck
		bool shouldForceDecision = viableDirections.Count == 0 || stuckTimer >= stuckTimeout;
		bool needsDecision = onGround && (cellChanged || decisionTimer <= 0f || directionBlocked || stuckTimer >= stuckTimeout || shouldForceDecision);

		if (needsDecision)
		{
			// clear locked direction when we need a new decision
			currentDirectionIndex = -1;
			PickNewDirection(viableDirections, false);
		}

		movement.Input.SetMoveInput(desiredMove);
	}

	private bool IsDirectionStillWalkable(Vector3Int currentCell, Vector2 move)
	{
		if (move.sqrMagnitude < 0.1f || movement == null)
		{
			return true;
		}

		return TryGetDirectionIndex(move, out int index) && movement.IsDirectionWalkable(currentCell, gridOffsets[index]);
	}

	private List<int> GatherViableDirections(Vector3Int currentCell)
	{
		reusableDirections.Clear();
		if (movement == null)
		{
			return reusableDirections;
		}

		for (int i = 0; i < gridOffsets.Length; i++)
		{
			if (movement.IsDirectionWalkable(currentCell, gridOffsets[i]))
			{
				reusableDirections.Add(i);
			}
		}

		return reusableDirections;
	}

	private void PickNewDirection(List<int> viableDirections, bool forceJump)
	{
		bool hasOptions = viableDirections != null && viableDirections.Count > 0;
		bool shouldJump = forceJump || !hasOptions;
		if (hasOptions)
		{
			int selected = SelectDirectionIndex(viableDirections);
			desiredMove = inputDirections[selected];
			decisionTimer = Random.Range(minDecisionInterval, maxDecisionInterval);
		}
		else
		{
			int fallback = Random.Range(0, inputDirections.Length);
			desiredMove = inputDirections[fallback];
			// longer but still jitter-resistant fallback interval
			decisionTimer = Random.Range(minDecisionInterval * 0.8f, maxDecisionInterval * 0.9f);
			shouldJump = false; // explicit: do not jump
			currentDirectionIndex = fallback;
		}

		stuckTimer = 0f;
	}

	private int SelectDirectionIndex(List<int> viableDirections)
	{
		if (viableDirections.Count == 0)
		{
			return 0;
		}

		// Bias strongly towards keeping current direction to reduce frequent turning
		if (TryGetDirectionIndex(desiredMove, out int currentIndex) && viableDirections.Contains(currentIndex))
		{
			if (viableDirections.Count == 1 || Random.value > 0.75f)
			{
				return currentIndex;
			}
		}

		return viableDirections[Random.Range(0, viableDirections.Count)];
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
