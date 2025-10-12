using System.Collections.Generic;
using UnityEngine;

public interface ISpawnAlgorithm
{
	void Spawn(GameObject entity, MazeGenerator generator);
}

public class SpawnLib : MonoBehaviour
{
	public List<ISpawnAlgorithm> algorithms = new List<ISpawnAlgorithm>();

	void Awake()
	{
		// 注册已完成的放置算法
		algorithms.Add(new SpawnAlgorithm0());
		algorithms.Add(new SpawnAlgorithm1());
		algorithms.Add(new SpawnAlgorithm2());
	}

	public void RunSpawn(GameObject entity, MazeGenerator generator, int algorithmIndex = 0)
	{
		if (entity == null || generator == null || algorithms.Count == 0)
		{
			return;
		}

		if (algorithmIndex < 0 || algorithmIndex >= algorithms.Count)
		{
			Debug.LogWarning($"Spawn algorithm index {algorithmIndex} is out of range.");
			return;
		}

		algorithms[algorithmIndex]?.Spawn(entity, generator);
	}

	public ISpawnAlgorithm GetAlgorithm(int index)
	{
		if (index < 0 || index >= algorithms.Count)
		{
			return null;
		}

		return algorithms[index];
	}
}
