using UnityEngine;

[DisallowMultipleComponent]
public class NPCBase : MonoBehaviour
{
	[Header("Core References")]
	public Animator animator;
	public NPCImitateInput npcInput;
	public NPCMovement npcMovement;
	public NPCAnimation npcAnimation;
	public NPCResource npcResource;
	protected NPCManager manager;
	protected bool isConfigured;

	protected virtual void Awake()
	{
		if (animator == null) animator = GetComponentInChildren<Animator>();
		if (npcInput == null) npcInput = GetComponent<NPCImitateInput>();
		if (npcMovement == null) npcMovement = GetComponent<NPCMovement>();
		if (npcAnimation == null) npcAnimation = GetComponent<NPCAnimation>();
		if (npcResource == null) npcResource = GetComponent<NPCResource>();
	}

	protected virtual void OnEnable()
	{
		TryRegisterWithManager();
	}

	protected virtual void Start()
	{
		TryRegisterWithManager();
	}

	protected virtual void OnDisable()
	{
		if (NPCManager.Instance != null)
		{
			NPCManager.Instance.UnregisterNPC(this);
		}
		isConfigured = false;
	}

	protected virtual void Update()
	{
		if (!isConfigured)
		{
			TryRegisterWithManager();
		}
	}

	private void TryRegisterWithManager()
	{
		if (isConfigured)
		{
			return;
		}

		NPCManager currentManager = NPCManager.Instance != null ? NPCManager.Instance : manager;
		if (currentManager == null)
		{
			currentManager = FindObjectOfType<NPCManager>();
		}
		if (currentManager != null)
		{
			currentManager.RegisterNPC(this);
		}
	}

	public virtual void Initialize(NPCManager npcManager)
	{
		if (isConfigured && manager == npcManager)
		{
			return;
		}

		manager = npcManager != null ? npcManager : (NPCManager.Instance != null ? NPCManager.Instance : FindObjectOfType<NPCManager>());
		if (manager == null)
		{
			isConfigured = false;
			return;
		}

		MazeGenerator generator = manager.MazeGenerator != null ? manager.MazeGenerator : FindObjectOfType<MazeGenerator>();
		MovementLib movementLib = manager.MovementLibrary != null ? manager.MovementLibrary : FindObjectOfType<MovementLib>();
		int algorithmIndex = Mathf.Max(0, manager.DefaultMovementAlgorithmIndex);
		if (manager.mazeGenerator == null && generator != null)
		{
			manager.mazeGenerator = generator;
		}
		if (manager.movementLib == null && movementLib != null)
		{
			manager.movementLib = movementLib;
		}

		bool configuredSuccessfully = npcMovement != null && npcMovement.Configure(generator, movementLib, algorithmIndex);
		isConfigured = configuredSuccessfully;
		if (isConfigured)
		{
			OnInitialized();
		}
	}

	public virtual void Deinitialize()
	{
		isConfigured = false;
		npcMovement?.Deinitialize();
	}

	protected virtual void LateUpdate()
	{
		if (!isConfigured)
		{
			return;
		}

		npcAnimation?.HandleAnimation(npcMovement, animator);
	}

	protected virtual void OnInitialized()
	{
		// Hook for subclasses
	}
}
