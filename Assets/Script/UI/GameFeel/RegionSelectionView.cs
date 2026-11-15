using System;
using UnityEngine;

// Display only (game-feel batch). Receives the region-selection state that Player already derives (phase, whose turn,
// who took which region) and forwards it to the four RegionCard visuals. It gates nothing: every call is wrapped so a
// failure here can never interrupt Player's RPC handlers, and Player's rules, RPCs and saves do not read anything back.
// UX clarity batch: the same state also drives the map link (RegionMapHighlight): hovering or keyboard-focusing a card
// spotlights its region on the map, and taken regions keep an outline (yours) or a veil (the other player's).
public class RegionSelectionView : MonoBehaviour
{
    // Same order as Player.colorsValueObj: {red, green, yellow, blue}.
    [SerializeField] private RegionCard[] cards = new RegionCard[4];

    [Header("Map link (display only)")]
    [SerializeField] private Transform mapRoot; // Canvas/States: the map cells (tag SquarePoint) live under it
    [SerializeField] private Color hoverOutline = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color mineOutline = new Color(1f, 0.784f, 0.341f, 1f); // amber #FFC857, as on the card
    [SerializeField] private Color outlineShadow = new Color(0.043f, 0.078f, 0.141f, 0.9f);
    [SerializeField] private Color spotlightVeil = new Color(0.043f, 0.078f, 0.141f, 0.55f);
    [SerializeField] private Color otherVeil = new Color(0.043f, 0.078f, 0.141f, 0.38f);
    [SerializeField] private float outlineWidth = 0.03f; // world units

    private static RegionSelectionView current;

    private RegionMapHighlight map;
    private bool regionsShown;
    private int pointerIndex = -1;
    private int focusIndex = -1;

    private void Awake()
    {
        current = this;
    }

    private void OnDestroy()
    {
        if (current == this)
        {
            current = null;
        }
    }

    private static RegionSelectionView Current
    {
        get
        {
#if UNITY_EDITOR
            // Edit-mode previews only (no Awake outside Play Mode).
            if (current == null && !Application.isPlaying)
            {
                current = FindAnyObjectByType<RegionSelectionView>(FindObjectsInactive.Include);
            }
#endif
            return current;
        }
    }

    // bothCut: the four regions exist (choose phase). myTurnToChoose: the phase instruction asks this player to choose.
    public static void NotifyPhase(bool bothCut, bool myTurnToChoose)
    {
        try
        {
            RegionSelectionView view = Current;
            if (view == null || view.cards == null)
            {
                return;
            }
            foreach (RegionCard card in view.cards)
            {
                if (card != null)
                {
                    card.SetPhase(bothCut, bothCut && myTurnToChoose);
                }
            }
            view.regionsShown = bothCut;
            view.RefreshMap();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("GAMEFEEL: region phase display failed: " + ex.Message);
        }
    }

    // Called after Player has already applied the choice (label, interactable, chooseNum).
    public static void NotifyTaken(int index, bool takenByLocalPlayer)
    {
        try
        {
            RegionSelectionView view = Current;
            if (view == null || view.cards == null || index < 0 || index >= view.cards.Length || view.cards[index] == null)
            {
                return;
            }
            view.cards[index].SetOwner(takenByLocalPlayer ? RegionCard.Owner.Mine : RegionCard.Owner.Other);
            view.RefreshMap();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("GAMEFEEL: region ownership display failed: " + ex.Message);
        }
    }

    // From RegionCard (pointer enter/exit on its button). Display only.
    public static void NotifyPointer(RegionCard card, bool over)
    {
        try
        {
            RegionSelectionView view = Current;
            if (view == null)
            {
                return;
            }
            int index = view.IndexOf(card);
            if (over)
            {
                view.pointerIndex = index;
            }
            else if (view.pointerIndex == index)
            {
                view.pointerIndex = -1;
            }
            view.RefreshMap();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("GAMEFEEL: region hover display failed: " + ex.Message);
        }
    }

    // From RegionCard (EventSystem select/deselect of its button, e.g. keyboard navigation). Display only.
    public static void NotifyFocus(RegionCard card, bool focused)
    {
        try
        {
            RegionSelectionView view = Current;
            if (view == null)
            {
                return;
            }
            int index = view.IndexOf(card);
            if (focused)
            {
                view.focusIndex = index;
            }
            else if (view.focusIndex == index)
            {
                view.focusIndex = -1;
            }
            view.RefreshMap();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("GAMEFEEL: region focus display failed: " + ex.Message);
        }
    }

    private int IndexOf(RegionCard card)
    {
        if (cards == null || card == null)
        {
            return -1;
        }
        return Array.IndexOf(cards, card);
    }

    private void RefreshMap()
    {
        if (mapRoot == null || cards == null)
        {
            return;
        }
        if (map == null)
        {
            map = new RegionMapHighlight(mapRoot, new RegionMapHighlight.Style
            {
                hoverOutline = hoverOutline,
                mineOutline = mineOutline,
                outlineShadow = outlineShadow,
                spotlightVeil = spotlightVeil,
                otherVeil = otherVeil,
                outlineWidth = outlineWidth,
            });
        }
        RegionCard.Owner[] owners = new RegionCard.Owner[cards.Length];
        for (int i = 0; i < cards.Length; i++)
        {
            owners[i] = cards[i] != null ? cards[i].CurrentOwner : RegionCard.Owner.None;
        }
        int spotlight = pointerIndex >= 0 ? pointerIndex : focusIndex;
        map.Render(regionsShown && Application.isPlaying, spotlight, owners);
    }
}
