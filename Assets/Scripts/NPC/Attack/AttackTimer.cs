using System;
using UnityEngine;

[DisallowMultipleComponent]
public class AttackTimer : MonoBehaviour
{
	[Tooltip("Length of the active attack phase in seconds.")]
	[Min(0f)] public float attackDuration = 8f;

	[Tooltip("Cooldown length applied after an attack finishes.")]
	[Min(0f)] public float cooldownDuration = 0f;

	public event Action AttackStarted;
	public event Action AttackEnded;

	public bool IsAttacking { get; private set; }
	public float AttackElapsed { get; private set; }
	public float CooldownRemaining => Mathf.Max(0f, cooldownRemaining);
	public bool IsReady => !IsAttacking && CooldownRemaining <= 0f;
	public bool IsOnCooldown => CooldownRemaining > 0f;
	public float AttackProgress => attackDuration <= 0f ? 1f : Mathf.Clamp01(AttackElapsed / attackDuration);

	private float cooldownRemaining;

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		if (IsAttacking)
		{
			AttackElapsed += deltaTime;
			if (AttackElapsed >= attackDuration)
			{
				CompleteAttack();
			}
		}
		else if (cooldownRemaining > 0f)
		{
			cooldownRemaining = Mathf.Max(0f, cooldownRemaining - deltaTime);
		}
	}

	public bool TryBeginAttack()
	{
		if (!IsReady)
		{
			return false;
		}

		IsAttacking = true;
		AttackElapsed = 0f;
		AttackStarted?.Invoke();
		return true;
	}

	public void StopAttack()
	{
		if (!IsAttacking)
		{
			return;
		}

		CompleteAttack();
	}

	public void ResetTimer()
	{
		IsAttacking = false;
		AttackElapsed = 0f;
		cooldownRemaining = 0f;
	}

	private void CompleteAttack()
	{
		IsAttacking = false;
		AttackElapsed = 0f;
		cooldownRemaining = cooldownDuration;
		AttackEnded?.Invoke();
	}
}

