using UnityEngine;

public class NPCAnimation : MonoBehaviour
{
	public void HandleAnimation(NPCMovement movement, Animator animator)
	{
		if (movement == null || animator == null)
		{
			return;
		}

		Vector3 horizontalVelocity = new Vector3(movement.Velocity.x, 0f, movement.Velocity.z);
		float speed = horizontalVelocity.magnitude;
		if (speed < 0.1f) speed = 0f;
		animator.SetFloat("Speed", speed);

		bool jumping = movement.JumpPressedThisFrame || (!movement.IsGrounded && movement.Velocity.y > 0f);
		animator.SetBool("Jump", jumping);
	}
}
