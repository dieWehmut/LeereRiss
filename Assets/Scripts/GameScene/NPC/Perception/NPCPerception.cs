using UnityEngine;

[DisallowMultipleComponent]
public class NPCPerception : MonoBehaviour
{
    [Header("References")]
    public PerceptionLib perceptionLib;
    public NPCMovement npcMovement;

    [Header("Settings")]
    public string playerTag = "Player";

    public bool PlayerSeen { get; private set; }
    public Transform SeenPlayerTransform { get; private set; }
    private bool chasing;

    void Awake()
    {
        if (perceptionLib == null) perceptionLib = FindObjectOfType<PerceptionLib>();
        if (npcMovement == null) npcMovement = GetComponent<NPCMovement>();
    }

    void Update()
    {
        if (perceptionLib != null)
        {
            perceptionLib.RunPerception(this);
        }

        if (PlayerSeen && SeenPlayerTransform != null && npcMovement != null)
        {
            if (!chasing)
            {
                chasing = true;
                npcMovement.SuspendAlgorithm();
            }

            Vector3 dir = (SeenPlayerTransform.position - npcMovement.transform.position);
            dir.y = 0f;
            float sqrMag = dir.sqrMagnitude;
            Vector2 moveInput = Vector2.zero;
            if (sqrMag > 0.001f)
            {
                dir.Normalize();
                moveInput = new Vector2(Vector3.Dot(npcMovement.transform.right, dir), Vector3.Dot(npcMovement.transform.forward, dir));
                if (moveInput.sqrMagnitude > 1f)
                {
                    moveInput.Normalize();
                }
            }
            var imitate = npcMovement.Input;
            imitate?.SetMoveInput(moveInput);
        }
        else if (chasing && npcMovement != null)
        {
            chasing = false;
            npcMovement.ResumeAlgorithm();
        }
    }

    public void MarkPlayerSeen(Transform player)
    {
        PlayerSeen = true;
        SeenPlayerTransform = player;
    }

    public void ClearSeen()
    {
        PlayerSeen = false;
        SeenPlayerTransform = null;
    }
}
