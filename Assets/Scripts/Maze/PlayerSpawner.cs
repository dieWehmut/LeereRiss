using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
	[Header("References")]
	public SpawnLib spawnLib;
	public MazeGenerator mazeGenerator;
	[SerializeField] private int defaultAlgorithmIndex = 0;

	void Awake()
	{
		if (spawnLib == null) spawnLib = GetComponent<SpawnLib>();
		if (mazeGenerator == null) mazeGenerator = GetComponent<MazeGenerator>();
	}

	public void Spawn(Player player)
	{
		if (player == null)
		{
			return;
		}

		if (spawnLib != null && mazeGenerator != null)
		{
			spawnLib.RunSpawn(player.gameObject, mazeGenerator, defaultAlgorithmIndex);
		}
		else if (mazeGenerator != null)
		{
			player.transform.position = mazeGenerator.GetRandomVoidPosition();
		}
	}
}
