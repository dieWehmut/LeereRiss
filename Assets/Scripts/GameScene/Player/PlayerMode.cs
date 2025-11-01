using UnityEngine;

public class PlayerMode : MonoBehaviour
{
    public enum Mode
    {
        Interaction,
        Shooting
    }

    [Header("Mode Settings")]
    public Mode currentMode = Mode.Interaction;

    public void SwitchMode()
    {
        if (currentMode == Mode.Interaction)
        {
            currentMode = Mode.Shooting;
        }
        else
        {
            currentMode = Mode.Interaction;
        }
    }
}
