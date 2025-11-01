using UnityEngine;

public class Player : MonoBehaviour
{
	[Header("References")]
	public Animator animator;
	public PlayerInput playerInput;
	public PlayerMovement playerMovement;
	public PlayerCamera playerCamera;
	public PlayerAnimation playerAnimation;
	public PlayerMode playerMode;
	public PlayerInteract playerInteract;
	public PlayerShoot playerShoot;
	public PlayerCrosshair playerCrosshair;
	public PlayerResource playerResource;
	private bool isRotatingToAim = false;
	private Vector3? pendingAimPoint = null;
	private PlayerMode.Mode? pendingActionMode = null;

		// Ethereal area effects
		private MazeGenerator mazeGenerator;
		private float etherealHealTimer = 0f;
		private const float etherealHealInterval = 1f; // 每1秒触发一次回血
		private const float etherealHealAmount = 0.02f; // 每次回血的归一化量（2%）
		private const float etherealSpeedMultiplier = 1.5f; // 50% 速度加成
		private float originalMoveSpeed = -1f;
		private bool isInEthereal = false;
	void Awake()
	{
		if (animator == null) animator = GetComponent<Animator>();
		if (playerInput == null) playerInput = GetComponent<PlayerInput>();
		if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
		if (playerCamera == null) playerCamera = GetComponent<PlayerCamera>();
		if (playerAnimation == null) playerAnimation = GetComponent<PlayerAnimation>();
		if (playerMode == null) playerMode = GetComponent<PlayerMode>();
		if (playerInteract == null) playerInteract = GetComponent<PlayerInteract>();
		if (playerShoot == null) playerShoot = GetComponent<PlayerShoot>();
		if (playerCrosshair == null) playerCrosshair = GetComponent<PlayerCrosshair>();
		if (playerResource == null) playerResource = GetComponent<PlayerResource>();
		// find maze generator for cell lookup
		mazeGenerator = FindObjectOfType<MazeGenerator>();
		if (playerMovement != null)
		{
			originalMoveSpeed = playerMovement.moveSpeed;
		}
		// Ensure PlayerCamera knows initial mode so it can choose the correct camera variants without a frame of fallback
		if (playerCamera != null && playerMode != null)
		{
			playerCamera.SetMode(playerMode.currentMode);
		}
		if (playerShoot != null && playerResource != null)
		{
			playerShoot.playerResource = playerResource;
		}
		
	}

	void Update()
	{
		playerResource?.Tick(Time.deltaTime);
		// 采集输入
		playerInput?.HandleInput();
		// 切换模式按键
		if (playerInput != null && playerInput.switchModePressed && playerMode != null)
		{
			playerMode.SwitchMode();
			// Notify PlayerCamera of the new mode immediately to make camera swap atomic and avoid fallback to other cameras
			if (playerCamera != null)
			{
				playerCamera.SetMode(playerMode.currentMode);
			}
		}
		// 处理移动
		playerMovement?.HandleMovement(playerInput);

		// ------ Ethereal 区域效果检测（移动后根据当前位置判断） ------
		if (mazeGenerator != null && playerMovement != null && playerResource != null)
		{
			Vector3 pos = transform.position;
			int cx = Mathf.FloorToInt(pos.x / mazeGenerator.cellSize);
			int cy = Mathf.FloorToInt(pos.y / mazeGenerator.LayerHeight);
			int cz = Mathf.FloorToInt(pos.z / mazeGenerator.cellSize);
			Vector3Int cell = new Vector3Int(cx, cy, cz);
			int[,,] maze = mazeGenerator.GetMaze();
			bool nowEthereal = false;
			if (maze != null && mazeGenerator.IsInsideBounds(cell))
			{
				int v = maze[cell.x, cell.y, cell.z];
				if (v == (int)MazeData.CellType.Ethereal)
				{
					nowEthereal = true;
				}
			}

			if (nowEthereal)
			{
				if (!isInEthereal)
				{
					// enter ethereal
					isInEthereal = true;
					if (originalMoveSpeed < 0f && playerMovement != null) originalMoveSpeed = playerMovement.moveSpeed;
					if (playerMovement != null) playerMovement.moveSpeed = originalMoveSpeed * etherealSpeedMultiplier;
				}

				// 治疗计时器
				etherealHealTimer += Time.deltaTime;
				while (etherealHealTimer >= etherealHealInterval)
				{
					playerResource.AddHealthNormalized(etherealHealAmount);
					etherealHealTimer -= etherealHealInterval;
				}
			}
			else
			{
				if (isInEthereal)
				{
					// leave ethereal
					isInEthereal = false;
					etherealHealTimer = 0f;
					if (playerMovement != null && originalMoveSpeed > 0f)
					{
						playerMovement.moveSpeed = originalMoveSpeed;
					}
				}
			}
		}
		// 摄像机控制
		playerCamera?.HandleCamera(playerInput);
		// 动画控制
		playerAnimation?.HandleAnimation(playerMovement, animator, playerMode != null ? playerMode.currentMode : PlayerMode.Mode.Interaction, playerInput != null && playerInput.shootPressed);

		// 准星更新
		playerCrosshair?.UpdateCrosshair(playerMode != null ? playerMode.currentMode : PlayerMode.Mode.Interaction, playerInput != null && playerInput.shootPressed);

		// 鼠标左键点击时，先转向，转向完成后再执行射击/交互
		if (playerInput != null && playerInput.shootPressed)
		{
			if (!isRotatingToAim && playerCrosshair != null)
			{
				if (playerCrosshair.GetAimPointOrFallback(out Vector3 aimPoint))
				{
					pendingAimPoint = aimPoint;
					pendingActionMode = playerMode != null ? playerMode.currentMode : (PlayerMode.Mode?)null;
					StartCoroutine(RotateAndThenAct(aimPoint, 0.05f)); // 更快的转向
				}
			}
		}
	}

	// 先转向，转向完成后再执行射击/交互
	private System.Collections.IEnumerator RotateAndThenAct(Vector3 targetPoint, float duration)
	{
		isRotatingToAim = true;
		Vector3 dir = (targetPoint - transform.position);
		dir.y = 0f; // only rotate on the Y axis
		if (dir.sqrMagnitude < 0.0001f)
		{
			isRotatingToAim = false;
			yield break;
		}

		Quaternion startRot = transform.rotation;
		Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
			transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
			yield return null;
		}
		transform.rotation = targetRot;
		isRotatingToAim = false;

		// 转向完成后再执行射击/交互
		if (pendingActionMode.HasValue)
		{
			if (pendingActionMode.Value == PlayerMode.Mode.Shooting)
			{
				playerShoot?.OnShoot();
			}
			else
			{
				playerInteract?.TryInteract();
			}
		}
		pendingAimPoint = null;
		pendingActionMode = null;
	}
}
