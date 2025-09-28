using UnityEngine;

public class PlayerInput : MonoBehaviour
{
	[Header("Input State")]
	public Vector2 moveInput; // x:横向, y:纵向
	public bool jumpPressed;
	public bool switchViewPressed;
	public bool switchModePressed;
	public bool shootPressed;
	public Vector2 mouseDelta;

	public void HandleInput()
	{
		moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		jumpPressed = Input.GetButtonDown("Jump");
		switchViewPressed = Input.GetKeyDown(KeyCode.V);
		switchModePressed = Input.GetKeyDown(KeyCode.C);
		mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
		shootPressed = Input.GetMouseButtonDown(0);
	}
}
