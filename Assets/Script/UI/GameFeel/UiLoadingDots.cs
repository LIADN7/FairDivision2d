using UnityEngine;
using UnityEngine.UI;

// Display only (game-feel Stage 4, lobby). Three small dots under the existing connection / joining message that pulse
// in turn, so the waiting state reads as "in progress". Driven by unscaled time while enabled (nothing is scheduled, so
// nothing outlives a disable or a scene load). With effects off the dots stay steady. The dots are never raycast targets
// and nothing here reads or writes the message, the connection or any button.
[DisallowMultipleComponent]
public class UiLoadingDots : MonoBehaviour
{
    [SerializeField] private Graphic[] dots;
    [SerializeField] private float period = 1.2f;   // one full sweep
    [SerializeField] private float stagger = 0.16f; // fraction of the period between dots
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float lift = 3f;

    private float[] baseAlpha;
    private Vector2[] basePosition;
    private bool captured;

    private void Capture()
    {
        if (captured || dots == null)
        {
            return;
        }
        captured = true;
        baseAlpha = new float[dots.Length];
        basePosition = new Vector2[dots.Length];
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null) continue;
            baseAlpha[i] = dots[i].color.a;
            basePosition[i] = dots[i].rectTransform.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        Capture();
    }

    private void OnDisable()
    {
        Apply(null);
    }

    private void Update()
    {
        Capture();
        if (!GameFeel.Animate(this))
        {
            Apply(null);
            return;
        }
        Apply(Time.unscaledTime);
    }

    // time == null: steady (final) look.
    private void Apply(float? time)
    {
        if (!captured || dots == null)
        {
            return;
        }
        for (int i = 0; i < dots.Length; i++)
        {
            Graphic dot = dots[i];
            if (dot == null) continue;
            float w = 1f;
            if (time.HasValue)
            {
                float phase = Mathf.Repeat(time.Value / period - i * stagger, 1f);
                w = phase < 0.5f ? UiMotion.Bump(phase / 0.5f) : 0f;
            }
            Color c = dot.color;
            c.a = baseAlpha[i] * (time.HasValue ? Mathf.Lerp(minAlpha, 1f, w) : 1f);
            dot.color = c;
            dot.rectTransform.anchoredPosition = basePosition[i] + new Vector2(0f, time.HasValue ? lift * w : 0f);
        }
    }
}
