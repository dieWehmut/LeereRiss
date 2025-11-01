using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
	[Header("Interaction Settings")]
	[Tooltip("Max distance for interaction raycasts. Should match PlayerCrosshair.rayDistance.")]
	public float rayDistance = 300f;
	[Tooltip("LayerMask used for interaction raycasts.")]
	public LayerMask raycastMask = Physics.DefaultRaycastLayers;

	private PlayerCrosshair playerCrosshair;
	private Transform playerRootTransform;

	private void Awake()
	{
		playerCrosshair = FindObjectOfType<PlayerCrosshair>();
		
		// Cache the Player root to ignore its colliders during raycasts
		Player playerComp = GetComponentInParent<Player>();
		if (playerComp == null)
		{
			playerComp = FindObjectOfType<Player>();
		}
		if (playerComp != null)
		{
			playerRootTransform = playerComp.transform;
		}
	}

	// Try to interact with object at center screen. Returns true if interacted.
	public bool TryInteract()
	{
		// Prefer the player's crosshair camera if available to avoid briefly using Camera.main during camera swaps
		Camera cam = null;
		if (playerCrosshair != null) cam = playerCrosshair.GetResolvedCamera();
		if (cam == null) cam = Camera.main;
		if (cam == null) return false;

		// Generate ray from screen center, matching PlayerCrosshair's logic
		Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
		Ray ray = cam.ScreenPointToRay(screenCenter);

		// Use RaycastAll to get all hits and filter properly
		RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance, raycastMask);
		if (hits == null || hits.Length == 0)
			return false;

		// Sort by distance ascending
		System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

		// Find the first non-player NPC hit
		foreach (var hit in hits)
		{
			// Ignore null colliders
			if (hit.collider == null) continue;

			// Skip if this hit belongs to the player
			if (hit.collider.GetComponentInParent<Player>() != null)
				continue;

			// Fallback: if we cached a specific player root transform, also skip children of it
			if (playerRootTransform != null)
			{
				Transform t = hit.collider.transform;
				bool belongsToPlayer = false;
				while (t != null)
				{
					if (t == playerRootTransform)
					{
						belongsToPlayer = true;
						break;
					}
					t = t.parent;
				}
				if (belongsToPlayer)
					continue;
			}

			// Found a non-player hit; check if it's an NPC
			if (hit.collider.CompareTag("NPC"))
			{
				return true;
			}

			// Stop at the first non-player, non-NPC hit (don't keep looking behind)
			break;
		}

		return false;
	}
}

