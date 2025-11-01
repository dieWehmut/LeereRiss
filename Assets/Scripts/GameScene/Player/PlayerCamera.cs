using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
	void Awake()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		// 如果编辑器中 thirdPersonCameraPivot 被设为角色的子物体，运行时解除父级以避免角色旋转影响摄像机朝向
		if (thirdPersonCameraPivot != null && thirdPersonCameraPivot.parent != null)
		{
			// 保持世界位置/旋转，解除父级
			thirdPersonCameraPivot.SetParent(null, true);
		}
	}

		
	[Header("Optional Links")]
	public PlayerCrosshair playerCrosshair;

	void Start()
	{

		// Notify crosshair of the initial active camera (if any)
		Camera activeCam = GetActiveCameraFromState();
		if (playerCrosshair != null && activeCam != null)
		{
			playerCrosshair.SetCamera(activeCam);
		}

		// Ensure the active camera objects match the configured isFirstPerson at start
		// (this avoids starting the game in the wrong view if scene objects' active states differ)
		SwitchCamera(isFirstPerson);

		// Ensure AudioListener is attached to the active camera at start
		Camera cur = GetActiveCameraFromState();
		EnsureSingleAudioListener(cur);
	}
	[Header("Camera")]
	public float mouseSensitivity = 2f;
	public Transform firstPersonCamera;
	// optional shooting-mode variants
	[Tooltip("Optional camera transform to use for first-person while in Shooting mode")]
	public Transform firstPersonShootCamera;
	public Transform thirdPersonCameraPivot;
	public Transform thirdPersonCamera;
	[Tooltip("Optional camera transform to use for third-person while in Shooting mode")]
	public Transform thirdPersonShootCamera;
	public bool isFirstPerson = true;
	[Tooltip("第三人称摄像机允许仰视的最小角度（度），负值表示向下看更深）")]
	public float thirdPersonMinPitch = -60f;
	[Tooltip("第三人称摄像机允许俯视的最大角度（度），正值表示向上看更高）")]
	public float thirdPersonMaxPitch = 40f;
	[Header("Shooting Mode Limits (Third Person)")]
	[Tooltip("第三人称射击模式时允许仰视的最小角度（度），建议值范围比普通模式更窄）")]
	public float thirdPersonShootMinPitch = -20f;
	[Tooltip("第三人称射击模式时允许俯视的最大角度（度），建议值范围比普通模式更窄）")]
	public float thirdPersonShootMaxPitch = 20f;	

	[Header("Shooting Mode Limits (First Person)")]
	[Tooltip("第一人称射击模式时允许仰视的最小角度（度），建议值范围比普通第一人称更窄）")]
	public float firstPersonShootMinPitch = 0f;
	[Tooltip("第一人称射击模式时允许俯视的最大角度（度），建议值范围比普通第一人称更窄）")]
	public float firstPersonShootMaxPitch = 0f;

	private float xRotation = 0f;

	public void HandleCamera(PlayerInput input)
	{
		if (input == null) return;

		// 视角切换
		if (input.switchViewPressed)
		{
			isFirstPerson = !isFirstPerson;
			// keep current shooting-mode selection when switching view
			SwitchCamera(isFirstPerson);
		}

		// 视角旋转
		float mouseX = input.mouseDelta.x * mouseSensitivity;
		float mouseY = input.mouseDelta.y * mouseSensitivity;

		if (isFirstPerson)
		{
			// Prefer to apply rotation to the camera transform that is actually active in the hierarchy.
			Transform fp = null;
			PlayerMode.Mode currentMode = PlayerMode.Mode.Interaction;
			PlayerMode pm = FindObjectOfType<PlayerMode>();
			if (pm != null) currentMode = pm.currentMode;

			// If shooting mode and shoot-variant is assigned and active, prefer it
			if (currentMode == PlayerMode.Mode.Shooting && firstPersonShootCamera != null && firstPersonShootCamera.gameObject.activeInHierarchy)
				fp = firstPersonShootCamera;
			// Otherwise prefer the standard first-person camera if active
			if (fp == null && firstPersonCamera != null && firstPersonCamera.gameObject.activeInHierarchy)
				fp = firstPersonCamera;
			// Fallback: if nothing active, use shoot variant if exists else standard
			if (fp == null)
			{
				if (currentMode == PlayerMode.Mode.Shooting && firstPersonShootCamera != null)
					fp = firstPersonShootCamera;
				else
					fp = firstPersonCamera;
			}

			if (fp != null)
			{
				xRotation -= mouseY;
				if (currentMode == PlayerMode.Mode.Shooting)
					xRotation = Mathf.Clamp(xRotation, firstPersonShootMinPitch, firstPersonShootMaxPitch);
				else
					xRotation = Mathf.Clamp(xRotation, -90f, 90f);

				fp.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
			}

			// Always rotate the player for yaw
			transform.Rotate(Vector3.up * mouseX);
		}
		else if (!isFirstPerson && thirdPersonCameraPivot != null && thirdPersonCamera != null)
		{
			// 只改变 pivot 的旋转（世界空间），但 pivot 不应该是角色的子物体，这样角色旋转不会直接影响摄像机朝向
			thirdPersonCameraPivot.Rotate(Vector3.up * mouseX, Space.World);
			float pitch = -mouseY;
			Vector3 euler = thirdPersonCameraPivot.localEulerAngles;
			euler.x += pitch;
			// 把欧拉角从 0-360 映射到 -180 到 180 方便 clamp
			float mappedX = euler.x > 180 ? euler.x - 360 : euler.x;
			// If currently in shooting mode, use the tighter shooting-pitch limits when available
			PlayerMode.Mode currentMode = PlayerMode.Mode.Interaction;
			PlayerMode pm = FindObjectOfType<PlayerMode>();
			if (pm != null) currentMode = pm.currentMode;
			if (currentMode == PlayerMode.Mode.Shooting)
			{
				mappedX = Mathf.Clamp(mappedX, thirdPersonShootMinPitch, thirdPersonShootMaxPitch);
			}
			else
			{
				mappedX = Mathf.Clamp(mappedX, thirdPersonMinPitch, thirdPersonMaxPitch);
			}
			thirdPersonCameraPivot.localEulerAngles = new Vector3(mappedX, euler.y, 0);
			// 仅更新 pivot 的世界位置以跟随角色，但不要把 pivot 设置为角色的子物体，保持其世界旋转独立于角色旋转
			thirdPersonCameraPivot.position = transform.position + Vector3.up * 1.5f;
		}
	}

	private void SwitchCamera(bool firstPerson)
	{
		// Determine which first/third camera to enable depending on whether we're in shooting mode
		Transform fp = firstPersonCamera;
		Transform tp = thirdPersonCamera;
		// if there's an active shooting-mode variant, prefer it
		PlayerMode.Mode currentMode = PlayerMode.Mode.Interaction;
		PlayerMode pm = FindObjectOfType<PlayerMode>();
		if (pm != null) currentMode = pm.currentMode;
		if (currentMode == PlayerMode.Mode.Shooting)
		{
			if (firstPersonShootCamera != null) fp = firstPersonShootCamera;
			if (thirdPersonShootCamera != null) tp = thirdPersonShootCamera;
		}

		if (fp != null) fp.gameObject.SetActive(firstPerson);
		if (tp != null) tp.gameObject.SetActive(!firstPerson);
		// After changing active state, notify PlayerCrosshair of the new active Camera
		if (playerCrosshair != null)
		{
			Camera activeCam = GetActiveCameraFromState();
			if (activeCam != null)
				playerCrosshair.SetCamera(activeCam);
		}

		// Ensure exactly one AudioListener is enabled and attached to the active camera
		Camera active = GetActiveCameraFromState();
		EnsureSingleAudioListener(active);
	}

	// Public method to set camera mode (Interaction vs Shooting) from Player controller.
	public void SetMode(PlayerMode.Mode mode)
	{
		// When changing mode, ensure the active camera is swapped atomically: enable the desired camera before disabling the old one
		bool first = isFirstPerson;

		Transform desiredFP = (mode == PlayerMode.Mode.Shooting && firstPersonShootCamera != null) ? firstPersonShootCamera : firstPersonCamera;
		Transform desiredTP = (mode == PlayerMode.Mode.Shooting && thirdPersonShootCamera != null) ? thirdPersonShootCamera : thirdPersonCamera;

		// Activate desired cameras then deactivate the other variants to avoid a frame where no camera is active or another camera becomes Camera.main
		if (desiredFP != null && first)
			desiredFP.gameObject.SetActive(true);
		if (desiredTP != null && !first)
			desiredTP.gameObject.SetActive(true);

		// Disable the non-desired variants
		// For first person: disable the non-desired first person transform if different
		if (firstPersonCamera != null && firstPersonCamera != desiredFP)
			firstPersonCamera.gameObject.SetActive(false);
		if (firstPersonShootCamera != null && firstPersonShootCamera != desiredFP)
			firstPersonShootCamera.gameObject.SetActive(false);

		if (thirdPersonCamera != null && thirdPersonCamera != desiredTP)
			thirdPersonCamera.gameObject.SetActive(false);
		if (thirdPersonShootCamera != null && thirdPersonShootCamera != desiredTP)
			thirdPersonShootCamera.gameObject.SetActive(false);

		// Notify crosshair of new active camera
		if (playerCrosshair != null)
		{
			Camera activeCam = GetActiveCameraFromState();
			if (activeCam != null)
				playerCrosshair.SetCamera(activeCam);
		}

		// Ensure exactly one AudioListener is enabled and attached to the active camera
		Camera active = GetActiveCameraFromState();
		EnsureSingleAudioListener(active);
	}

	// Helper: given current isFirstPerson state, find the Camera component that is currently active
	private Camera GetActiveCameraFromState()
	{
		// If first person, prefer the active first-person transform or its shooting variant
		if (isFirstPerson)
		{
			Transform fp = null;
			PlayerMode pm = FindObjectOfType<PlayerMode>();
			PlayerMode.Mode currentMode = pm != null ? pm.currentMode : PlayerMode.Mode.Interaction;
			if (currentMode == PlayerMode.Mode.Shooting && firstPersonShootCamera != null && firstPersonShootCamera.gameObject.activeInHierarchy)
				fp = firstPersonShootCamera;
			if (fp == null && firstPersonCamera != null && firstPersonCamera.gameObject.activeInHierarchy)
				fp = firstPersonCamera;
			if (fp != null)
			{
				Camera c = GetCameraFromTransform(fp);
				if (c != null) return c;
			}
		}
		else
		{
			// third person: prefer active third person transform or its shooting variant
			Transform tp = null;
			PlayerMode pm = FindObjectOfType<PlayerMode>();
			PlayerMode.Mode currentMode = pm != null ? pm.currentMode : PlayerMode.Mode.Interaction;
			if (currentMode == PlayerMode.Mode.Shooting && thirdPersonShootCamera != null && thirdPersonShootCamera.gameObject.activeInHierarchy)
				tp = thirdPersonShootCamera;
			if (tp == null && thirdPersonCamera != null && thirdPersonCamera.gameObject.activeInHierarchy)
				tp = thirdPersonCamera;
			if (tp != null)
			{
				Camera c = GetCameraFromTransform(tp);
				if (c != null) return c;
			}
		}

		// fallback: Camera.main
		if (Camera.main != null) return Camera.main;
		// last resort: any enabled camera
		Camera[] all = Camera.allCameras;
		for (int i = 0; i < all.Length; i++)
		{
			if (all[i] != null && all[i].enabled && all[i].gameObject.activeInHierarchy)
				return all[i];
		}
		return null;
	}

	private Camera GetCameraFromTransform(Transform t)
	{
		if (t == null) return null;
		Camera c = t.GetComponent<Camera>();
		if (c != null) return c;
		return t.GetComponentInChildren<Camera>();
	}

	// Public accessor for other components wanting to know current active camera
	public Camera GetActiveCamera()
	{
		return GetActiveCameraFromState();
	}

	// Ensure there's exactly one enabled AudioListener in the scene. If an active camera is provided,
	// prefer placing/enabling the AudioListener on that camera's GameObject. Any other AudioListeners will be disabled.
	private void EnsureSingleAudioListener(Camera preferred)
	{
		AudioListener[] listeners = FindObjectsOfType<AudioListener>();
		// If there's a preferred camera, try to get/create an AudioListener on it
		if (preferred != null)
		{
			AudioListener prefListener = preferred.GetComponent<AudioListener>();
			if (prefListener == null)
			{
				prefListener = preferred.gameObject.AddComponent<AudioListener>();
			}
			// Enable preferred listener and disable others
			for (int i = 0; i < listeners.Length; i++)
			{
				if (listeners[i] == prefListener)
					listeners[i].enabled = true;
				else
					listeners[i].enabled = false;
			}
			return;
		}
		// No preferred camera: if multiple listeners exist, keep the first enabled and disable others
		bool keptOne = false;
		for (int i = 0; i < listeners.Length; i++)
		{
			if (!keptOne && listeners[i] != null)
			{
				listeners[i].enabled = true;
				keptOne = true;
			}
			else if (listeners[i] != null)
			{
				listeners[i].enabled = false;
			}
		}
	}
}
