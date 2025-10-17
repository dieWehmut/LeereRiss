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

	private bool unsupportedIndexLogged;

	public INPCMovementAlgorithm GetAlgorithm(int index)
	{
		if (index != RandomAlgorithmIndex)
		{
			if (!unsupportedIndexLogged)
			{
				Debug.LogWarning($"Movement algorithm index {index} is not implemented. Falling back to RandomMovement.");
				unsupportedIndexLogged = true;
			}
		}

		switch (index)
		{
			case RandomAlgorithmIndex:
			default:
				return new RandomMovement();
		}
	}
}
