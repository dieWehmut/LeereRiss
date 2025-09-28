using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
	// Try to interact with object in center screen. Returns true if interacted.
	public bool TryInteract()
	{
		// Prefer the player's crosshair camera if available to avoid briefly using Camera.main during camera swaps
		Camera cam = null;
		PlayerCrosshair pc = FindObjectOfType<PlayerCrosshair>();
		if (pc != null) cam = pc.GetResolvedCamera();
		if (cam == null) cam = Camera.main;
		if (cam == null) return false;

		Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width/2f, Screen.height/2f, 0f));
		if (Physics.Raycast(ray, out RaycastHit hit, 3f))
		{
			// Placeholder: only consider Doors for now
			if (hit.collider.CompareTag("Door"))
			{
		
				return true;
			}
		}
		return false;
	}
}

