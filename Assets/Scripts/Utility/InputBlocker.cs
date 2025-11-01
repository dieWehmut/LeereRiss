using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Simple global input blocker used by modal screens (GameOver / Pause).
/// - When blocked: keyboard input is considered disabled, right-mouse input is ignored, left-mouse still works.
/// - Also disables UI navigation via keyboard/joystick by toggling EventSystem.sendNavigationEvents.
/// </summary>
public static class InputBlocker
{
    private static bool blocked = false;
    private static bool lastSendNavigation = true;

    public static bool IsBlocked => blocked;

    /// <summary>
    /// Enable modal blocking (blocks keyboard and right mouse; leaves left mouse allowed).
    /// </summary>
    public static void EnableModalBlock()
    {
        if (blocked) return;
        blocked = true;
        // disable keyboard-driven UI navigation
        if (EventSystem.current != null)
        {
            lastSendNavigation = EventSystem.current.sendNavigationEvents;
            EventSystem.current.sendNavigationEvents = false;
        }
    }

    /// <summary>
    /// Disable modal blocking and restore previous EventSystem navigation setting.
    /// </summary>
    public static void DisableModalBlock()
    {
        if (!blocked) return;
        blocked = false;
        if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = lastSendNavigation;
        }
    }

    /// <summary>
    /// Helper: If input should be ignored, check this.
    /// </summary>
    public static bool ShouldBlockKeyboard() => blocked;
    public static bool ShouldBlockRightMouse() => blocked;
}
