using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class NPCMovement : MonoBehaviour
{
	[Header("Movement")]
	public float moveSpeed = 4.5f;
	public float jumpHeight = 1.4f;
	public float gravity = -9.81f;

	[Header("Rotation")]
	public float rotationLerpSpeed = 10f;

	[Header("Algorithm")]
	public MovementLib movementLib;
	public int algorithmIndex = 0;

	private CharacterController controller;
	private NPCImitateInput npcInput;
	private INPCMovementAlgorithm algorithm;
	private MazeGenerator mazeGenerator;
	private bool configured;
	private float verticalVelocity;
	private bool algorithmSuspended;

	public Vector3 Velocity { get; private set; } = Vector3.zero;
	public bool IsGrounded { get; private set; }
	public bool JumpPressedThisFrame { get; private set; }

	public MazeGenerator MazeGenerator => mazeGenerator;
	public NPCImitateInput Input => npcInput;

	void Awake()
	{
		controller = GetComponent<CharacterController>();
		npcInput = GetComponent<NPCImitateInput>();
	}

	public bool Configure(MazeGenerator generator, MovementLib lib, int index)
	{
		mazeGenerator = generator;
		movementLib = lib != null ? lib : movementLib;
		if (movementLib == null)
		{
			movementLib = FindObjectOfType<MovementLib>();
		}
		algorithmIndex = Mathf.Max(0, index);
		if (npcInput == null || controller == null || movementLib == null)
		{
			configured = false;
			if (movementLib == null)
			{
				Debug.LogWarning("NPCMovement requires a MovementLib reference to operate.", this);
			}
			return false;
		}
		configured = true;
		algorithm = null;
		Velocity = Vector3.zero;
		JumpPressedThisFrame = false;
		verticalVelocity = 0f;
		IsGrounded = controller.isGrounded;
		return true;
	}

	public void Deinitialize()
	{
		configured = false;
		algorithm = null;
		Velocity = Vector3.zero;
		JumpPressedThisFrame = false;
		verticalVelocity = 0f;
		IsGrounded = false;
	}

	void Update()
	{
		if (!configured)
		{
			return;
		}

		if (algorithm == null && movementLib != null)
		{
			algorithm = movementLib.GetAlgorithm(algorithmIndex);
			algorithm?.Initialize(this);
		}

		// If NPCPerception reports a seen player, we may suspend the algorithm and drive input manually.
		if (!algorithmSuspended)
		{
			algorithm?.Tick(Time.deltaTime);
		}
		ApplyMovement(Time.deltaTime);
	}

	private void ApplyMovement(float deltaTime)
	{
		Vector2 inputVector = npcInput != null ? npcInput.MoveInput : Vector2.zero;
		Vector3 desiredDirection = transform.right * inputVector.x + transform.forward * inputVector.y;
		if (desiredDirection.sqrMagnitude > 1f)
		{
			desiredDirection.Normalize();
		}

		Vector3 horizontalVelocity = desiredDirection * moveSpeed;
		bool wasGrounded = IsGrounded;
		IsGrounded = controller.isGrounded;
		if (IsGrounded && verticalVelocity < 0f)
		{
			verticalVelocity = -2f;
		}

		JumpPressedThisFrame = false;
		if (npcInput != null && npcInput.ConsumeJumpRequest() && IsGrounded)
		{
			verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
			JumpPressedThisFrame = true;
			IsGrounded = false;
		}

		verticalVelocity += gravity * deltaTime;

		Vector3 startPosition = transform.position;
		Vector3 motion = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z) * deltaTime;
		CollisionFlags flags = controller.Move(motion);
		IsGrounded = (flags & CollisionFlags.Below) != 0;
		if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
		{
			verticalVelocity = 0f;
		}
		if (IsGrounded && verticalVelocity < 0f)
		{
			verticalVelocity = -2f;
		}

		float safeDelta = Mathf.Max(deltaTime, 0.0001f);
		Vector3 frameDisplacement = (transform.position - startPosition);
		Vector3 frameVelocity = frameDisplacement / safeDelta;
		Velocity = frameVelocity;

		Vector3 flatVelocity = new Vector3(frameVelocity.x, 0f, frameVelocity.z);
		if (flatVelocity.sqrMagnitude > 0.001f)
		{
			Quaternion targetRotation = Quaternion.LookRotation(flatVelocity.normalized, Vector3.up);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * deltaTime);
		}
		else if (!wasGrounded && !IsGrounded && desiredDirection.sqrMagnitude > 0.001f)
		{
			Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * deltaTime);
		}
	}

	public int[,,] Maze => mazeGenerator != null ? mazeGenerator.GetMaze() : null;
	public float CellSize => mazeGenerator != null ? Mathf.Max(mazeGenerator.cellSize, 0.0001f) : 1f;
	public float LayerHeight => mazeGenerator != null ? mazeGenerator.LayerHeight : 1f;

	public Vector3Int WorldToCell(Vector3 worldPosition)
	{
		float inv = 1f / CellSize;
		float invLayer = 1f / Mathf.Max(LayerHeight, 0.0001f);
		int x = Mathf.FloorToInt(worldPosition.x * inv);
		int y = Mathf.FloorToInt(worldPosition.y * invLayer);
		int z = Mathf.FloorToInt(worldPosition.z * inv);

		var maze = Maze;
		if (maze != null && maze.Length > 0)
		{
			int width = maze.GetLength(0);
			int height = maze.GetLength(1);
			int depth = maze.GetLength(2);
			x = Mathf.Clamp(x, 0, width - 1);
			y = Mathf.Clamp(y, 0, height - 1);
			z = Mathf.Clamp(z, 0, depth - 1);
		}
		else
		{
			x = Mathf.Max(0, x);
			y = Mathf.Max(0, y);
			z = Mathf.Max(0, z);
		}

		return new Vector3Int(x, y, z);
	}

	public Vector3 CellToWorldCenter(Vector3Int cell)
	{
		if (mazeGenerator != null)
		{
			return mazeGenerator.CellToWorldCenter(cell);
		}

		return new Vector3((cell.x + 0.5f) * CellSize,
			(cell.y + 0.5f) * LayerHeight,
			(cell.z + 0.5f) * CellSize);
	}

	public bool IsCellInsideBounds(Vector3Int cell)
	{
		var maze = Maze;
		if (maze == null || maze.Length == 0)
		{
			return false;
		}

		return cell.x >= 0 && cell.x < maze.GetLength(0) &&
		       cell.y >= 0 && cell.y < maze.GetLength(1) &&
		       cell.z >= 0 && cell.z < maze.GetLength(2);
	}

	public bool IsCellVoid(Vector3Int cell)
	{
		var maze = Maze;
		if (maze == null || maze.Length == 0)
		{
			return true;
		}
		if (!IsCellInsideBounds(cell))
		{
			return false;
		}
		return maze[cell.x, cell.y, cell.z] == (int)MazeData.CellType.Void;
	}

	public bool IsCellWalkable(Vector3Int cell)
	{
		return IsCellVoid(cell);
	}

	public bool IsDirectionWalkable(Vector3Int origin, Vector3Int offset)
	{
		return IsCellWalkable(origin + offset);
	}

	public void SuspendAlgorithm()
	{
		algorithmSuspended = true;
	}

	public void ResumeAlgorithm()
	{
		algorithmSuspended = false;
		if (npcInput != null)
		{
			npcInput.ClearMoveInput();
		}
		algorithm?.Initialize(this);
	}

	public bool IsAlgorithmSuspended => algorithmSuspended;
}
