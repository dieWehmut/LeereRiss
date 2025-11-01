
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class NPCResource : MonoBehaviour
{
	[Header("Resource Settings")]
	[Range(0f, 1f)] public float startingHealth = 1f;
	[Range(0f, 1f)] public float startingMana = 1f;
	[Tooltip("Normalized health removed when a single bullet hits this NPC.")]
	[Range(0f, 1f)] public float damagePerBullet = 0.1f;

	[Header("Knockback Settings")]
	[Tooltip("Distance to push NPC back when hit by a bullet.")]
	public float knockbackDistance = 2f;
	[Tooltip("Duration of the knockback effect in seconds.")]
	public float knockbackDuration = 0.3f;

	[Header("UI References")]
	public HealthBar healthBar;
	public ManaBar manaBar;

	[Header("Respawn")]
	[Tooltip("Delay (seconds) before this NPC is respawned after death. Set to 0 for immediate respawn.")]
	public float respawnDelay = 2f;

	public float CurrentHealth { get; private set; } = 1f;
	public float CurrentMana { get; private set; } = 1f;

	private Vector3 knockbackVelocity = Vector3.zero;
	private float knockbackTimer = 0f;
	private bool isInKnockback = false;

	protected virtual void Awake()
	{
		AutoAssignUI();
		ResetResources();
	}

	protected virtual void OnEnable()
	{
		UpdateHealthUI();
		UpdateManaUI();
		ResetKnockback();
	}

	protected virtual void Update()
	{
		// Process knockback movement if active
		if (isInKnockback)
		{
			knockbackTimer -= Time.deltaTime;
			if (knockbackTimer <= 0f)
			{
				isInKnockback = false;
				knockbackVelocity = Vector3.zero;
			}
			else
			{
				// Move character controller backward during knockback
				CharacterController cc = GetComponent<CharacterController>();
				if (cc != null && cc.enabled)
				{
					cc.Move(knockbackVelocity * Time.deltaTime);
				}
			}
		}
	}

	protected void ResetResources()
	{
		CurrentHealth = Mathf.Clamp01(startingHealth);
		CurrentMana = Mathf.Clamp01(startingMana);
		UpdateHealthUI();
		UpdateManaUI();
	}

	public virtual void ApplyDamage(float normalizedAmount)
	{
		if (normalizedAmount <= 0f || CurrentHealth <= 0f)
		{
			return;
		}

		CurrentHealth = Mathf.Clamp01(CurrentHealth - normalizedAmount);
		UpdateHealthUI();
		if (CurrentHealth <= 0f)
		{
			OnHealthDepleted();
		}
	}

	public virtual void ApplyKnockback(Vector3 knockbackDir)
	{
		if (knockbackDistance <= 0f || knockbackDuration <= 0f)
		{
			return;
		}

		// Apply knockback in the direction provided (should be bullet travel direction)
		knockbackDir = knockbackDir.normalized;
		knockbackDir.y = 0f; // Don't knock back vertically
		
		knockbackVelocity = knockbackDir * (knockbackDistance / knockbackDuration);
		knockbackTimer = knockbackDuration;
		isInKnockback = true;
	}

	private void ResetKnockback()
	{
		knockbackVelocity = Vector3.zero;
		knockbackTimer = 0f;
		isInKnockback = false;
	}

	public virtual void SetHealthNormalized(float value)
	{
		CurrentHealth = Mathf.Clamp01(value);
		UpdateHealthUI();
	}

	public virtual void SetManaNormalized(float value)
	{
		CurrentMana = Mathf.Clamp01(value);
		UpdateManaUI();
	}

	protected virtual void OnHealthDepleted()
	{
		// 默认行为：禁用该 NPC，然后请求 NPCSpawner 重新生成（若存在），否则简单重新启用在随机位置
		StartCoroutine(HandleDeathAndRespawn());
	}

	private IEnumerator HandleDeathAndRespawn()
	{
		// Try to play death animation or other effects here (subclasses can override OnHealthDepleted to customize)

		// Deactivate the NPC so it disappears
		var npcBase = GetComponent<NPCBase>();
		// Unregister will be called in NPCBase.OnDisable when we deactivate
		gameObject.SetActive(false);

		// Wait for respawn delay (real-time, independent of Time.timeScale)
		float timer = 0f;
		while (timer < respawnDelay)
		{
			timer += Time.unscaledDeltaTime;
			yield return null;
		}

		// Try to find a spawner
		var spawner = FindObjectOfType<NPCSpawner>();
		if (spawner != null && npcBase != null)
		{
			spawner.Spawn(npcBase);
			yield break;
		}

		// Fallback: reactivate and place at a random void position if MazeGenerator exists
		var mg = FindObjectOfType<MazeGenerator>();
		if (mg != null)
		{
			gameObject.SetActive(true);
			transform.position = mg.GetRandomVoidPosition();
			// OnEnable of NPCBase will re-register with manager
			yield break;
		}

		// Last resort: just reactivate immediately
		gameObject.SetActive(true);
		yield break;
	}

	private void OnTriggerEnter(Collider other)
	{
		TryHandleBulletCollision(other != null ? other.gameObject : null);
	}

	private void OnCollisionEnter(Collision collision)
	{
		TryHandleBulletCollision(collision != null ? collision.gameObject : null);
	}

	protected virtual bool IsBullet(GameObject obj)
	{
		return obj != null && obj.GetComponent<BulletAcceleration>() != null;
	}

	private void TryHandleBulletCollision(GameObject other)
	{
		if (!IsBullet(other))
		{
			return;
		}

		HandleBulletHit(other);
	}

	protected virtual void HandleBulletHit(GameObject bullet)
	{
		if (bullet == null || !bullet.activeInHierarchy)
		{
			return;
		}

		// Apply damage
		ApplyDamage(damagePerBullet);

		// Apply knockback opposite to bullet travel direction
		Vector3 knockbackDirection = Vector3.zero;
		if (bullet.TryGetComponent<BulletAcceleration>(out var bulletAccel))
		{
			// Get bullet velocity direction and reverse it for knockback
			Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
			if (bulletRb != null)
			{
				knockbackDirection = -bulletRb.linearVelocity.normalized;
			}
			else
			{
				// Fallback: direction from NPC to bullet (opposite to bullet travel)
				knockbackDirection = (bullet.transform.position - transform.position).normalized;
			}
		}
		else
		{
			// Fallback: direction from NPC to bullet
			knockbackDirection = (bullet.transform.position - transform.position).normalized;
		}

		ApplyKnockback(knockbackDirection);

		DisableAndDestroyBullet(bullet);
	}

	protected void DisableAndDestroyBullet(GameObject bullet)
	{
		if (bullet == null)
		{
			return;
		}

		if (bullet.activeSelf)
		{
			bullet.SetActive(false);
		}
		Destroy(bullet);
	}

	protected void UpdateHealthUI()
	{
		if (healthBar != null)
		{
			healthBar.SetValue(CurrentHealth);
		}
	}

	protected void UpdateManaUI()
	{
		if (manaBar != null)
		{
			manaBar.SetValue(CurrentMana);
		}
	}

	private void AutoAssignUI()
	{
		if (healthBar == null)
		{
			healthBar = GetComponentInChildren<HealthBar>(includeInactive: true);
		}
		if (manaBar == null)
		{
			manaBar = GetComponentInChildren<ManaBar>(includeInactive: true);
		}
	}
}
