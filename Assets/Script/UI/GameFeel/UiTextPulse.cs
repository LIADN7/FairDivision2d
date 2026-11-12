using TMPro;
using UnityEngine;

// Display only (game-feel batch). When existing code writes a new message into an already visible status plate
// (e.g. "choose two parts" -> "choose another part", "waiting" -> "moving to the next round"), the label gives one short
// scale pulse. The text itself changes instantly; this only reacts to it. It scales the label only (never a click
// target, never the plate that UiAppear may be animating).
[DisallowMultipleComponent]
public class UiTextPulse : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private float amount = 0.08f;

    private RectTransform target;
    private string last;
    private Vector3 baseScale = Vector3.one;
    private float enabledAt;
    private int tweenId = -1;

    private void Awake()
    {
        if (label != null)
        {
            target = label.rectTransform;
            baseScale = target.localScale;
        }
    }

    private void OnEnable()
    {
        last = label != null ? label.text : null;
        enabledAt = Time.unscaledTime;
    }

    private void OnDisable()
    {
        UiMotion.Stop(ref tweenId);
        if (target != null) target.localScale = baseScale;
    }

    private void LateUpdate()
    {
        if (label == null || target == null || label.text == last)
        {
            return;
        }
        last = label.text;
        if (Time.unscaledTime - enabledAt < 0.25f)
        {
            return; // text written as the plate appeared: the entrance is enough
        }
        UiMotion.Stop(ref tweenId);
        target.localScale = baseScale;
        if (!GameFeel.Animate(this))
        {
            return;
        }
        tweenId = UiMotion.Play(gameObject, UiMotion.Emphasis, t =>
        {
            if (target != null) target.localScale = baseScale * (1f + amount * UiMotion.Bump(t));
        });
    }
}
