using UnityEngine;

[DisallowMultipleComponent]
public class NPCImitateInput : MonoBehaviour
{
	[Header("Input State")]
	[SerializeField] private Vector2 moveInput = Vector2.zero;
	[SerializeField] private bool jumpRequested = false;

	public Vector2 MoveInput => moveInput;
	public bool JumpRequested => jumpRequested;

	public void SetMoveInput(Vector2 input)
	{
		moveInput = Vector2.ClampMagnitude(input, 1f);
	}

	public void ClearMoveInput()
	{
		moveInput = Vector2.zero;
	}

	public void RequestJump()
	{
		jumpRequested = true;
	}

	public bool ConsumeJumpRequest()
	{
		if (!jumpRequested)
		{
			return false;
		}
		jumpRequested = false;
		return true;
	}

	public void ClearJumpRequest()
	{
		jumpRequested = false;
	}
}
