
using UnityEngine;

[DisallowMultipleComponent]
public class NPCResource : MonoBehaviour
{
	[Header("Resource Settings")]
	[Range(0f, 1f)] public float startingHealth = 1f;
	[Range(0f, 1f)] public float startingMana = 1f;
	[Tooltip("Normalized health removed when a single bullet hits this NPC.")]
	[Range(0f, 1f)] public float damagePerBullet = 0.1f;

	[Header("UI References")]
	public HealthBar healthBar;
	public ManaBar manaBar;

	public float CurrentHealth { get; private set; } = 1f;
	public float CurrentMana { get; private set; } = 1f;

	protected virtual void Awake()
	{
		AutoAssignUI();
		ResetResources();
	}

	protected virtual void OnEnable()
	{
		UpdateHealthUI();
		UpdateManaUI();
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
		// Hook for subclasses to react when health reaches zero.
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

		ApplyDamage(damagePerBullet);
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
