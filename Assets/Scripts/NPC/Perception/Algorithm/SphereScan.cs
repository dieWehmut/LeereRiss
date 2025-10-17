using UnityEngine;

public class SphereScan : IPerceptionAlgorithm
{
	public float radius = 8f;

	public void UpdatePerception(NPCPerception perception)
	{
		if (perception == null) return;
		Collider[] hits = Physics.OverlapSphere(perception.transform.position, radius);
		foreach (var col in hits)
		{
			if (col != null && col.gameObject.CompareTag(perception.playerTag))
			{
				perception.MarkPlayerSeen(col.transform);
				return;
			}
		}
	}
}

