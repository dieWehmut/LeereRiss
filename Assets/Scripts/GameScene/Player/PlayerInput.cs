using UnityEngine;

public class PlayerInput : MonoBehaviour
{
	[Header("Input State")]
	public Vector2 moveInput; // x:横向, y:纵向
	public bool jumpPressed;
	public bool switchViewPressed;
	public bool switchModePressed;
	public bool shootPressed;
	public bool guideCyclePressed;
	public Vector2 mouseDelta;
	public bool pausePressed;

	public void HandleInput()
	{
		// 若处于模态阻塞（GameOver / Pause），禁止键盘/键位输入；但仍允许鼠标左键射击
		if (InputBlocker.ShouldBlockKeyboard())
		{
			moveInput = Vector2.zero;
			jumpPressed = false;
			switchViewPressed = false;
			switchModePressed = false;
			guideCyclePressed = false;
		}
		else
		{
			moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
			jumpPressed = Input.GetButtonDown("Jump");
			switchViewPressed = Input.GetKeyDown(KeyCode.V);
			switchModePressed = Input.GetKeyDown(KeyCode.C);
			guideCyclePressed = Input.GetKeyDown(KeyCode.G);
		}

		mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
		shootPressed = Input.GetMouseButtonDown(0);

		// 右键暂停/打开菜单只有在未被阻塞时才有效
		pausePressed = !InputBlocker.ShouldBlockRightMouse() && Input.GetMouseButtonDown(1);
	}
}
