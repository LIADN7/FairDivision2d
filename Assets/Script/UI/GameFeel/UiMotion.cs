using System;
using UnityEngine;

// Display only (game-feel batch). Thin wrapper around the project's existing LeanTween (Assets/LeanTween, v2.50).
// Every tween is a 0..1 value tween owned by the calling component's GameObject, uses unscaled time, and carries no
// completion logic; LeanTween ends a tween by itself when its owner is destroyed (scene load).
public static class UiMotion
{
    public const float Fast = 0.11f;   // hover / press
    public const float Normal = 0.17f; // state changes, panels
    public const float Emphasis = 0.24f;

    public static int Play(GameObject owner, float duration, Action<float> onUpdate, bool overshoot = false, float delay = 0f)
    {
        LTDescr d = LeanTween.value(owner, onUpdate, 0f, 1f, duration).setIgnoreTimeScale(true);
        if (overshoot)
        {
            d.setEaseOutBack().setOvershoot(0.9f);
        }
        else
        {
            d.setEaseOutCubic();
        }
        if (delay > 0f)
        {
            d.setDelay(delay);
        }
        return d.uniqueId;
    }

    public static void Stop(ref int id)
    {
        if (id >= 0 && LeanTween.isTweening(id))
        {
            LeanTween.cancel(id);
        }
        id = -1;
    }

    // 0 -> 1 -> 0 bump used for short emphasis pulses.
    public static float Bump(float t)
    {
        return Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
    }
}
