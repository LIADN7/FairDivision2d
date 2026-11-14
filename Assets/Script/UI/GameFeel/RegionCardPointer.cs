using UnityEngine;
using UnityEngine.EventSystems;

// Display only (game-feel batch). Sits on a region button only to learn hover enter/exit for its RegionCard visual.
// It implements no click or press handler, does not scale the button and does not touch Button.interactable or OnClick.
[DisallowMultipleComponent]
public class RegionCardPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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

    private void OnDisable()
    {
        if (card != null) card.SetHover(false);
    }
}
