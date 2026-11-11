using UnityEngine;

// Display only (game-feel batch). Short entrance when the object is activated by existing code: fades a CanvasGroup's
// alpha and/or eases the scale of a non-interactive visual. Activation and deactivation stay exactly as the game sets
// them (no delayed hide, so no fade-out); only alpha and scale are touched. The CanvasGroup's interactable and
// blocksRaycasts are never written. Never place this on an ancestor of the region buttons.
[DisallowMultipleComponent]
public class UiAppear : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;          // optional: alpha only
    [SerializeField] private float fromAlpha = 0f;       // use >= 0.35 for anything clickable (never fully invisible)
    [SerializeField] private RectTransform scaleTarget;  // optional: must not be a click target
    [SerializeField] private float fromScale = 1f;
    [SerializeField] private SpriteRenderer[] fadeSprites; // world sprites a CanvasGroup cannot fade (e.g. Help shade)
    [SerializeField] private float duration = UiMotion.Normal;
    [SerializeField] private float delay = 0f;
    [SerializeField] private bool overshoot = false;
    [SerializeField] private bool playOnce = false;

    private Vector3 baseScale = Vector3.one;
    private float[] spriteAlpha;
    private bool captured;
    private bool played;
    private int tweenId = -1;

    private void Capture()
    {
        if (captured)
        {
            return;
        }
        captured = true;
        if (scaleTarget != null)
        {
            baseScale = scaleTarget.localScale;
        }
        if (fadeSprites != null)
        {
            spriteAlpha = new float[fadeSprites.Length];
            for (int i = 0; i < fadeSprites.Length; i++)
            {
                spriteAlpha[i] = fadeSprites[i] != null ? fadeSprites[i].color.a : 1f;
            }
        }
    }

    private void OnEnable()
    {
        Capture();
        UiMotion.Stop(ref tweenId);
        // Objects switched on/off while the scene loads (saved active, then hidden by Manager.Start) are not appearances.
        bool sceneLoading = Time.timeSinceLevelLoad < 0.25f;
        if (!GameFeel.Animate(this) || sceneLoading || (playOnce && played))
        {
            Set(1f);
            return;
        }
        played = true;
        Set(0f);
        tweenId = UiMotion.Play(gameObject, duration, Set, overshoot, delay);
    }

    private void OnDisable()
    {
        UiMotion.Stop(ref tweenId);
        Set(1f); // a re-enable with effects off, or a hide/show, starts from the final look
    }

    private void Set(float t)
    {
        if (group != null)
        {
            group.alpha = Mathf.LerpUnclamped(fromAlpha, 1f, Mathf.Clamp01(t));
        }
        if (scaleTarget != null)
        {
            scaleTarget.localScale = baseScale * Mathf.LerpUnclamped(fromScale, 1f, t);
        }
        if (fadeSprites != null && spriteAlpha != null)
        {
            for (int i = 0; i < fadeSprites.Length; i++)
            {
                if (fadeSprites[i] == null) continue;
                Color c = fadeSprites[i].color;
                c.a = spriteAlpha[i] * Mathf.Clamp01(t);
                fadeSprites[i].color = c;
            }
        }
    }
}
