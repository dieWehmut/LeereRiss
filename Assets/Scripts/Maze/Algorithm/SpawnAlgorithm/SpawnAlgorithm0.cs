using UnityEngine;

public class SpawnAlgorithm0 : ISpawnAlgorithm
{
	public void Spawn(GameObject entity, MazeGenerator generator)
	{
		if (entity == null || generator == null)
		{
			return;
		}

		// Randomly choose a void cell and place the entity at its center
		Vector3 spawnPosition = generator.GetRandomVoidPosition();
		entity.transform.position = spawnPosition;
	}
}
