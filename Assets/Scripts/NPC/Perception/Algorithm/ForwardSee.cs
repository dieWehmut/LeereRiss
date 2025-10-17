using UnityEngine;

public class ForwardSee : IPerceptionAlgorithm
{
	public float range = 10f;
	public float fov = 45f; // half-angle

	public void UpdatePerception(NPCPerception perception)
	{
		if (perception == null || perception.npcMovement == null) return;

		Transform npc = perception.transform;
		// find player by tag
		var playerObj = GameObject.FindWithTag(perception.playerTag);
		if (playerObj == null) return;

		Vector3 toPlayer = playerObj.transform.position - npc.position;
		float dist = toPlayer.magnitude;
		if (dist > range) return;

		toPlayer.y = 0f;
		Vector3 forward = npc.forward;
		forward.y = 0f;
		if (Vector3.Angle(forward, toPlayer) > fov) return;

		// 可视检测：射线检测避免墙体遮挡
		RaycastHit hit;
		if (Physics.Raycast(npc.position + Vector3.up * 0.5f, toPlayer.normalized, out hit, range))
		{
			if (hit.collider != null && hit.collider.gameObject.CompareTag(perception.playerTag))
			{
				perception.MarkPlayerSeen(playerObj.transform);
			}
		}
	}
}

