using System;
using UnityEngine;

// Display only (game-feel batch). One switch for all UI motion. With effects off every effect component shows its final
// state at once; nothing in gameplay, networking or saving reads this flag.
public static class GameFeel
{
    public static bool EffectsEnabled { get; private set; } = true;

    // Set when a tester toggles effects (hotkey or script), so a scene's default does not override the choice.
    public static bool OverriddenByTester { get; private set; }

    public static event Action Changed;

    public static void SetEffects(bool enabled, bool byTester = false)
    {
        if (byTester)
        {
            OverriddenByTester = true;
        }
        if (EffectsEnabled == enabled)
        {
            return;
        }
        EffectsEnabled = enabled;
        Debug.Log("GAMEFEEL: UI effects " + (enabled ? "ON" : "OFF"));
        Changed?.Invoke();
    }

    // Motion is used only in Play Mode with effects on; edit-mode previews and the off switch apply states instantly.
    public static bool Animate(Behaviour owner)
    {
        return EffectsEnabled && Application.isPlaying && owner != null && owner.isActiveAndEnabled;
    }
}
