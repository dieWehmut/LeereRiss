using UnityEngine;

public class AlwaysSee : IPerceptionAlgorithm
{
	public void UpdatePerception(NPCPerception perception)
	{
		if (perception == null) return;
		var playerObj = GameObject.FindWithTag(perception.playerTag);
		if (playerObj != null)
		{
			perception.MarkPlayerSeen(playerObj.transform);
		}
	}
}

