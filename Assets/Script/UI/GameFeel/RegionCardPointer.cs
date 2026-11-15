using UnityEngine;
using UnityEngine.EventSystems;

// Display only (game-feel batch). Sits on a region button only to learn hover enter/exit for its RegionCard visual.
// It implements no click or press handler, does not scale the button and does not touch Button.interactable or OnClick.
// UX clarity batch: also forwards EventSystem select/deselect (keyboard focus) so the card and its map region show it.
// ISelectHandler / IDeselectHandler are notifications only; the Button keeps its own selection and submit behaviour.
[DisallowMultipleComponent]
public class RegionCardPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private RegionCard card;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (card != null) card.SetHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (card != null) card.SetHover(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        // A mouse press also selects the button; that case is already covered by hover, and a click-selected button
        // must not keep its region spotlighted after the pointer leaves. Only non-pointer focus (keyboard) counts.
        if (eventData is PointerEventData)
        {
            return;
        }
        if (card != null) card.SetFocus(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (card != null) card.SetFocus(false);
    }

    private void OnDisable()
    {
        if (card != null)
        {
            card.SetHover(false);
            card.SetFocus(false);
        }
    }
}
