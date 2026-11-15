using UnityEngine;

// Display only (UX clarity batch). The single mapping from a map cell's shading intensity to the colour of its inner
// ("heat") square, shared by PointOfState, PointOfStateAITutorial and the map legend (HeatLegendView).
//
// colorPower is unchanged: PointOfState.setmyPowerColor / setOtherPowerColor = value / that player's total x 500, and 0
// for values below 4. The level t = clamp01(colorPower x 0.5) is exactly the red channel the old colour used, so every
// cell keeps its level, the order of levels and the per-player scale. Only the colours of the levels move further apart:
// a dark-to-white lightness ramp instead of a red-channel-only ramp, because the Help page tells participants that a
// lighter square is worth more to them. Alpha and the inner square's size are unchanged.
public static class HeatShading
{
    public static readonly Color Low = new Color(0.16f, 0.18f, 0.22f, 1f);
    public static readonly Color High = new Color(1f, 1f, 1f, 1f);
    public const float InnerAlpha = 0.8f; // as before
    public const float InnerScale = 0.7f; // PointOfState child square scale, as before

    public static float Level(float colorPower)
    {
        return Mathf.Clamp01(colorPower * 0.5f);
    }

    public static Color InnerColor(float colorPower)
    {
        return LevelColor(Level(colorPower));
    }

    public static Color LevelColor(float level)
    {
        Color c = Color.Lerp(Low, High, Mathf.Clamp01(level));
        c.a = InnerAlpha;
        return c;
    }
}
