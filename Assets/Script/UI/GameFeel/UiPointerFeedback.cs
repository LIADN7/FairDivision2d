using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Display only (game-feel batch). Hover lift and press response for ordinary buttons. Pointer enter/exit/down/up only:
// no click handler, so the Button's own click and OnClick are untouched. The scale never drops below the original, so
// a release that would have clicked before still lands inside the button. Not used on the region buttons.
[DisallowMultipleComponent]
public class UiPointerFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Selectable selectable;   // read only (IsInteractable)
    [SerializeField] private float hoverScale = 1.04f;
    [SerializeField] private float pressScale = 1.0f;

    private RectTransform target;
    private Vector3 baseScale = Vector3.one;
    private float current = 1f;
    private bool inside;
    private bool pressed;
    private int tweenId = -1;

    private void Awake()
    {
        target = (RectTransform)transform;
        baseScale = target.localScale;
        if (selectable == null)
        {
            selectable = GetComponent<Selectable>();
        }
    }

    private void OnDisable()
    {
        UiMotion.Stop(ref tweenId);
        inside = false;
        pressed = false;
        current = 1f;
        if (target != null)
        {
            target.localScale = baseScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) { inside = true; Refresh(); }
    public void OnPointerExit(PointerEventData eventData) { inside = false; pressed = false; Refresh(); }
    public void OnPointerDown(PointerEventData eventData) { pressed = true; Refresh(); }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; Refresh(); }

    private void Refresh()
    {
        bool live = selectable == null || selectable.IsInteractable();
        float goal = !live ? 1f : pressed ? pressScale : inside ? hoverScale : 1f;
        goal = Mathf.Max(goal, 1f);
        UiMotion.Stop(ref tweenId);
        if (!GameFeel.Animate(this))
        {
            current = 1f;
            target.localScale = baseScale;
            return;
        }
        float from = current;
        tweenId = UiMotion.Play(gameObject, pressed ? 0.07f : UiMotion.Fast, t =>
        {
            current = Mathf.LerpUnclamped(from, goal, t);
            if (target != null) target.localScale = baseScale * current;
        });
    }
}
