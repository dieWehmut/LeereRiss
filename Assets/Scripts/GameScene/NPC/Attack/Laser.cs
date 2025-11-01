using UnityEngine;

[DisallowMultipleComponent]
public class Laser : MonoBehaviour
{
	[System.Serializable]
	private class Beam
	{
		public Transform firePoint;
		public LineRenderer lineRenderer;
		[HideInInspector] public float currentLength;
	}

	[Header("References")]
	public AttackTimer attackTimer;
	public NPCPerception perception;
	public NPCResource npcResource;

	[Header("Laser Settings")]
	[Range(0f, 1f)] public float manaCost = 0.2f;
	[Range(0f, 1f)] public float damagePercent = 0.2f;
	[Min(0f)] public float extendSpeed = 2f; // slower default so damage is not immediate
	[Min(0f)] public float maxDistance = 80f;
	public LayerMask obstacleMask = Physics.DefaultRaycastLayers;

	[Tooltip("Vertical offset (meters) added to the player's center to aim at the face/head.")]
	public float aimFaceOffset = 0.8f;

	[Header("Mana Regeneration")]
	[Range(0f, 1f)] public float manaRegenPerSecond = 0.1f;

	[Header("Beams")]
	[SerializeField] private Beam[] beams = new Beam[0];

	private PlayerResource targetResource;
	private bool damageApplied;

	private void Awake()
	{
		if (attackTimer == null) attackTimer = GetComponent<AttackTimer>();
		if (perception == null) perception = GetComponent<NPCPerception>();
		if (npcResource == null) npcResource = GetComponent<NPCResource>();
		DisableBeams();
		HookAttackTimer();
	}

	private void OnEnable()
	{
		DisableBeams();
	}

	private void OnDestroy()
	{
		UnhookAttackTimer();
	}

	private void OnDisable()
	{
		DisableBeams();
		if (attackTimer != null)
		{
			attackTimer.ResetTimer();
		}
		targetResource = null;
		damageApplied = false;
	}

	private void Update()
	{
		if (npcResource == null)
		{
			npcResource = GetComponent<NPCResource>();
			if (npcResource == null)
			{
				return;
			}
		}

		RegenerateMana(Time.deltaTime);

		if (attackTimer == null)
		{
			attackTimer = GetComponent<AttackTimer>();
			HookAttackTimer();
			if (attackTimer == null)
			{
				return;
			}
		}

		if (perception == null)
		{
			perception = GetComponent<NPCPerception>();
			if (perception == null)
			{
				return;
			}
		}

		if (!attackTimer.IsAttacking)
		{
			TryStartAttack();
			return;
		}

		if (!perception.PlayerSeen || perception.SeenPlayerTransform == null)
		{
			attackTimer.StopAttack();
			return;
		}

		UpdateRunningAttack(Time.deltaTime);
	}

	private void TryStartAttack()
	{
		if (!perception.PlayerSeen || perception.SeenPlayerTransform == null)
		{
			return;
		}

		if (npcResource.CurrentMana < manaCost)
		{
			return;
		}

		if (attackTimer.TryBeginAttack())
		{
			float newMana = Mathf.Clamp01(npcResource.CurrentMana - manaCost);
			npcResource.SetManaNormalized(newMana);
		}
	}

	private void UpdateRunningAttack(float deltaTime)
	{
		for (int i = 0; i < beams.Length; i++)
		{
			Beam beam = beams[i];
			if (beam == null || beam.firePoint == null || beam.lineRenderer == null)
			{
				continue;
			}

			Vector3 start = beam.firePoint.position;
			beam.lineRenderer.SetPosition(0, start);

			Vector3 targetPos = GetPlayerAimPoint(perception.SeenPlayerTransform);
			Vector3 direction = targetPos - start;
			float desiredDistance = direction.magnitude;
			if (desiredDistance > 0.0001f)
			{
				direction /= desiredDistance;
			}
			else
			{
				direction = beam.firePoint.forward;
				desiredDistance = 0f;
			}

			bool hitIsPlayer = false;
			RaycastHit hit;
			if (Physics.Raycast(start, direction, out hit, maxDistance, obstacleMask, QueryTriggerInteraction.Ignore))
			{
				targetPos = hit.point;
				desiredDistance = hit.distance;
				// check if the hit object is the player (or contains PlayerResource)
				if (hit.collider != null)
				{
					var pr = hit.collider.GetComponent<PlayerResource>();
					if (pr == null)
					{
						pr = hit.collider.GetComponentInParent<PlayerResource>();
					}
					hitIsPlayer = pr != null;
				}
			}
			else if (desiredDistance > maxDistance)
			{
				desiredDistance = maxDistance;
				targetPos = start + direction * desiredDistance;
			}

			beam.currentLength = Mathf.MoveTowards(beam.currentLength, desiredDistance, extendSpeed * deltaTime);
			Vector3 endPoint = start + direction * beam.currentLength;
			beam.lineRenderer.SetPosition(1, endPoint);

			// If this beam hit the player and we have extended to the hit point, apply damage once.
			if (!damageApplied && hitIsPlayer && beam.currentLength >= desiredDistance - 0.05f)
			{
				ApplyDamageOnce();
			}
		}
	}

	private Vector3 GetPlayerAimPoint(Transform player)
	{
		if (player == null) return Vector3.zero;
		// Prefer collider bounds center
		Collider col = player.GetComponent<Collider>();
		if (col == null) col = player.GetComponentInChildren<Collider>();
		if (col != null) return col.bounds.center;
		Renderer rend = player.GetComponent<Renderer>();
		if (rend == null) rend = player.GetComponentInChildren<Renderer>();
		if (rend != null) return rend.bounds.center;
		// fallback to transform position with small upward offset
		return player.position + Vector3.up * Mathf.Max(0.1f, aimFaceOffset);
	}

	private void HandleAttackStarted()
	{
		// Do not apply damage immediately. Damage will be applied only when a beam actually reaches the player.
		damageApplied = false;
		targetResource = ResolvePlayerResource();
		EnableBeams();
	}

	private void HandleAttackEnded()
	{
		DisableBeams();
		targetResource = null;
		damageApplied = false;
	}

	private void ApplyDamageOnce()
	{
		if (damageApplied)
		{
			return;
		}

		targetResource = targetResource ?? ResolvePlayerResource();
		if (targetResource == null)
		{
			return;
		}

		float newHealth = Mathf.Max(0f, targetResource.CurrentHealth - damagePercent);
		targetResource.SetHealthNormalized(newHealth);
		damageApplied = true;
	}

	private PlayerResource ResolvePlayerResource()
	{
		if (perception == null || perception.SeenPlayerTransform == null)
		{
			return null;
		}

		PlayerResource resource = perception.SeenPlayerTransform.GetComponent<PlayerResource>();
		if (resource == null)
		{
			resource = perception.SeenPlayerTransform.GetComponentInParent<PlayerResource>();
		}
		return resource;
	}

	private void RegenerateMana(float deltaTime)
	{
		if (npcResource == null || npcResource.CurrentMana >= 1f || manaRegenPerSecond <= 0f)
		{
			return;
		}

		float newMana = Mathf.MoveTowards(npcResource.CurrentMana, 1f, manaRegenPerSecond * deltaTime);
		npcResource.SetManaNormalized(newMana);
	}

	private void EnableBeams()
	{
		for (int i = 0; i < beams.Length; i++)
		{
			Beam beam = beams[i];
			if (beam == null || beam.lineRenderer == null)
			{
				continue;
			}

			if (beam.lineRenderer.positionCount < 2)
			{
				beam.lineRenderer.positionCount = 2;
			}
			beam.currentLength = 0f;
			beam.lineRenderer.enabled = true;
			Vector3 start = beam.firePoint != null ? beam.firePoint.position : transform.position;
			beam.lineRenderer.SetPosition(0, start);
			beam.lineRenderer.SetPosition(1, start);
		}
	}

	private void DisableBeams()
	{
		for (int i = 0; i < beams.Length; i++)
		{
			Beam beam = beams[i];
			if (beam == null)
			{
				continue;
			}

			beam.currentLength = 0f;
			if (beam.lineRenderer != null)
			{
				beam.lineRenderer.enabled = false;
			}
		}
	}

	private void HookAttackTimer()
	{
		if (attackTimer == null)
		{
			return;
		}

		attackTimer.AttackStarted -= HandleAttackStarted;
		attackTimer.AttackEnded -= HandleAttackEnded;
		attackTimer.AttackStarted += HandleAttackStarted;
		attackTimer.AttackEnded += HandleAttackEnded;
	}

	private void UnhookAttackTimer()
	{
		if (attackTimer == null)
		{
			return;
		}

		attackTimer.AttackStarted -= HandleAttackStarted;
		attackTimer.AttackEnded -= HandleAttackEnded;
	}
}

