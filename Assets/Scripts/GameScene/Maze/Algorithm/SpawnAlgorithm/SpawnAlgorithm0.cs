using UnityEngine;

public class SpawnAlgorithm0 : ISpawnAlgorithm
{
	public void Spawn(GameObject entity, MazeGenerator generator)
	{
		if (entity == null || generator == null)
		{
			return;
		}

		Transform entityTransform = entity.transform;
		Transform originalParent = entityTransform.parent;
		Vector3 originalLocalScale = entityTransform.localScale;
		entityTransform.SetParent(null, true);

		Vector3 spawnPosition;
		Vector3Int cell;
		if (generator.TryGetRandomVoidCell(out cell))
		{
			float cellSize = Mathf.Max(generator.cellSize, 0.0001f);
			float layerHeight = generator.LayerHeight;
			float groundY = cell.y * layerHeight;
			spawnPosition = generator.CellToWorldCenter(cell);
			CharacterController controller = entity.GetComponent<CharacterController>();
			if (controller != null)
			{
				float halfHeight = Mathf.Max(controller.height * 0.5f, controller.radius);
				spawnPosition.y = groundY + halfHeight - controller.center.y;
			}
			else
			{
				spawnPosition.y = groundY + layerHeight * 0.5f;
			}
		}
		else
		{
			spawnPosition = generator.transform.position;
		}

		entityTransform.position = spawnPosition;
		if (originalParent != null)
		{
			entityTransform.SetParent(originalParent, true);
			entityTransform.localScale = originalLocalScale;
		}
	}
}
