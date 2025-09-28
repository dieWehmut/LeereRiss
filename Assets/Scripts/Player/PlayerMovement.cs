using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	[Header("Movement")]
	public float moveSpeed = 5f;
	public float jumpHeight = 1.5f;
	public float gravity = -9.81f;

	[Header("Rotation")]
	public float rotationSpeed = 10f;
    public float turnSmoothTime = 0.12f;
    private float turnSmoothVelocity;

	[Header("State")]
	public Vector3 velocity;
	public bool isGrounded;

	private CharacterController controller;
	private Vector3 lastPosition;
    private PlayerCamera playerCamera;
public bool jumpPressedThisFrame;
	void Awake()
	{
		controller = GetComponent<CharacterController>();
		lastPosition = transform.position;
		playerCamera = GetComponent<PlayerCamera>();
	}

	public void HandleMovement(PlayerInput input)
	{
		if (controller == null || input == null) return;

		// 移动
		Vector3 horizontalMove;
		// 第三人称：以第三人称相机轴心（pivot）朝向为移动参考，使角色朝向移动方向但不改变摄像机
		if (playerCamera != null && !playerCamera.isFirstPerson && playerCamera.thirdPersonCameraPivot != null)
		{
			Vector3 camForward = playerCamera.thirdPersonCameraPivot.forward;
			camForward.y = 0f;
			camForward.Normalize();
			Vector3 camRight = playerCamera.thirdPersonCameraPivot.right;
			camRight.y = 0f;
			camRight.Normalize();
			horizontalMove = camRight * input.moveInput.x + camForward * input.moveInput.y;

			// 当有移动输入时，让角色面朝移动方向（平面内）
			Vector3 flatMove = new Vector3(horizontalMove.x, 0f, horizontalMove.z);
			if (flatMove.sqrMagnitude > 0.0001f)
			{
				float targetAngle = Mathf.Atan2(flatMove.x, flatMove.z) * Mathf.Rad2Deg;
				float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
				transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
			}
		}
		else
		{
			// 第一人称或找不到相机时：保持原逻辑（相对于角色前/右）
			horizontalMove = transform.right * input.moveInput.x + transform.forward * input.moveInput.y;
		}

		Vector3 move = horizontalMove * moveSpeed;
		move.y = velocity.y;
		controller.Move(move * Time.deltaTime);

		isGrounded = controller.isGrounded;
		if (isGrounded && velocity.y < 0)
			velocity.y = -2f;

		// 跳跃
    if (input.jumpPressed && isGrounded)
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        jumpPressedThisFrame = true; // 告诉动画系统立即播放 Jump
    }
    else
    {
        jumpPressedThisFrame = false;
    }
		// 重力
		velocity.y += gravity * Time.deltaTime;

		// 计算真实速度（包括水平分量）
		Vector3 displacement = (transform.position - lastPosition) / Time.deltaTime;
		velocity.x = displacement.x;
		velocity.z = displacement.z;
		lastPosition = transform.position;
	}
}
