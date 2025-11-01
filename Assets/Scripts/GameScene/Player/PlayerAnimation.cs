using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public void HandleAnimation(PlayerMovement movement, Animator animator, PlayerMode.Mode mode, bool shootPressed)
    {
        if (animator == null || movement == null) return;

        // 计算水平速度
        Vector3 horizontalVelocity = new Vector3(movement.velocity.x, 0, movement.velocity.z);
float speed = horizontalVelocity.magnitude;
if (speed < 0.1f) speed = 0f;
animator.SetFloat("Speed", speed);

animator.SetBool("Jump", movement.jumpPressedThisFrame || (!movement.isGrounded && movement.velocity.y > 0));


        if (mode == PlayerMode.Mode.Shooting && shootPressed)
        {
            animator.SetTrigger("Shoot");

        }
        else if (mode == PlayerMode.Mode.Interaction)
        {
            // Future interaction animation logic can be added here
        }
    }
}
