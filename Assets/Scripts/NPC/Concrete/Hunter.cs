using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NPCImitateInput))]
[RequireComponent(typeof(NPCMovement))]
[RequireComponent(typeof(NPCAnimation))]
[RequireComponent(typeof(HunterResource))]

public class Hunter : NPCBase
{
	protected override void OnInitialized()
	{
		base.OnInitialized();

		if (npcResource == null)
		{
			npcResource = GetComponent<NPCResource>();
		}

		if (npcInput != null)
		{
			npcInput.ClearMoveInput();
			npcInput.ClearJumpRequest();
		}

		if (manager != null && npcMovement != null)
		{
			// Keep Hunter on the scene-wide random movement algorithm for now.
			npcMovement.algorithmIndex = manager.DefaultMovementAlgorithmIndex;
		}

		// Ensure this Hunter has perception and will chase when seeing the player
		var perception = GetComponent<NPCPerception>();
		if (perception == null)
		{
			perception = gameObject.AddComponent<NPCPerception>();
		}
		if (perception != null)
		{
			perception.perceptionLib = FindObjectOfType<PerceptionLib>();
		}
	}
}
