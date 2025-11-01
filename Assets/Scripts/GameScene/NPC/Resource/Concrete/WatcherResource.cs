using UnityEngine;

[DisallowMultipleComponent]
public class WatcherResource : NPCResource
{
	protected override void Awake()
	{
		base.Awake();
		startingHealth = 1f;
		startingMana = 1f;
		damagePerBullet = 0.1f;
		SetHealthNormalized(1f);
		SetManaNormalized(1f);
	}
}
