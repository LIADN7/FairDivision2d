using UnityEngine;
using UnityEngine.EventSystems;

// Display only (supervisor-approved 50% tooltip, 2026-11-27). Shows a tooltip panel while the pointer is over this
// small info icon; a tap toggles it (no hover on touch screens), a mouse click keeps it open. Hidden whenever the icon is
// disabled, so a phase change (the icon lives under CutStatusPlate, which is shown only while the player's own cut is
// pending) never leaves it open, and it always starts hidden. Implementing IPointerClickHandler also makes
// LineDrawingManager treat a press on the icon as a UI click, never as the start of a cut. Gates nothing.
[DisallowMultipleComponent]
public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private GameObject tooltip;

    public bool IsShown => tooltip != null && tooltip.activeSelf;

    private void OnEnable()
    {
        Show(false);
    }

    private void OnDisable()
    {
        Show(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Show(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Show(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Mouse pointers have negative ids (hover already shows the tooltip, so a click keeps it open); a touch toggles.
        Show(eventData.pointerId < 0 || !IsShown);
    }

    private void Show(bool shown)
    {
        if (tooltip != null && tooltip.activeSelf != shown)
        {
            tooltip.SetActive(shown);
        }
    }
}
