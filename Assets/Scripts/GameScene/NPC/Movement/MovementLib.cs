using UnityEngine;

public interface INPCMovementAlgorithm
{
	void Initialize(NPCMovement movement);
	void Tick(float deltaTime);
}

[DisallowMultipleComponent]
public class MovementLib : MonoBehaviour
{
	public const int RandomAlgorithmIndex = 0;
	public const int TeleportAlgorithmIndex = 1;

	private bool unsupportedIndexLogged;

	public INPCMovementAlgorithm GetAlgorithm(int index)
	{
		if (index != RandomAlgorithmIndex && index != TeleportAlgorithmIndex)
		{
			if (!unsupportedIndexLogged)
			{
				Debug.LogWarning($"Movement algorithm index {index} is not implemented. Falling back to RandomMovement.");
				unsupportedIndexLogged = true;
			}
		}

		switch (index)
		{
			case TeleportAlgorithmIndex:
				return new TeleportMovement();
			case RandomAlgorithmIndex:
			default:
				return new RandomMovement();
		}
	}
}
