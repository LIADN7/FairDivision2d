using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Display only (game-feel Stage 4, lobby). Shows a background plate only while an existing status text has content
// (e.g. the lobby's "room is full" message written by Launcher) and gives the text one short pulse when the message
// appears or changes. It only reads the text; it never writes it, and the plate graphics are never raycast targets.
// Works with a legacy Text or a TMP text.
[DisallowMultipleComponent]
public class UiTextPlate : MonoBehaviour
{
    [SerializeField] private Graphic text;              // Text or TMP_Text (read only)
    [SerializeField] private Graphic[] plateGraphics;   // enabled only while the text has content
    [SerializeField] private float pulse = 0.06f;

    private RectTransform pulseTarget;
    private Vector3 baseScale = Vector3.one;
    private string last;
    private int tweenId = -1;

    private void Awake()
    {
        if (text != null)
        {
            pulseTarget = text.rectTransform;
            baseScale = pulseTarget.localScale;
        }
        last = Read();
        SetPlate(HasContent(last));
    }

    private void OnDisable()
    {
        UiMotion.Stop(ref tweenId);
        if (pulseTarget != null) pulseTarget.localScale = baseScale;
    }

    private void LateUpdate()
    {
        string current = Read();
        if (current == last)
        {
            return;
        }
        last = current;
        bool has = HasContent(current);
        SetPlate(has);
        UiMotion.Stop(ref tweenId);
        if (pulseTarget == null)
        {
            return;
        }
        pulseTarget.localScale = baseScale;
        if (!has || !GameFeel.Animate(this))
        {
            return;
        }
        tweenId = UiMotion.Play(gameObject, UiMotion.Emphasis, t =>
        {
            if (pulseTarget != null) pulseTarget.localScale = baseScale * (1f + pulse * UiMotion.Bump(t));
        });
    }

    private string Read()
    {
        if (text is Text legacy) return legacy.text;
        if (text is TMP_Text tmp) return tmp.text;
        return null;
    }

    private static bool HasContent(string value)
    {
        return !string.IsNullOrEmpty(value) && value.Trim().Length > 0;
    }

    private void SetPlate(bool visible)
    {
        if (plateGraphics == null) return;
        foreach (Graphic g in plateGraphics)
        {
            if (g != null && g.enabled != visible) g.enabled = visible;
        }
    }
}
