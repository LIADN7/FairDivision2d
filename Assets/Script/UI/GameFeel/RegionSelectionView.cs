using System;
using UnityEngine;

// Display only (game-feel batch). Receives the region-selection state that Player already derives (phase, whose turn,
// who took which region) and forwards it to the four RegionCard visuals. It gates nothing: every call is wrapped so a
// failure here can never interrupt Player's RPC handlers, and Player's rules, RPCs and saves do not read anything back.
public class RegionSelectionView : MonoBehaviour
{
    // Same order as Player.colorsValueObj: {red, green, yellow, blue}.
    [SerializeField] private RegionCard[] cards = new RegionCard[4];

    private static RegionSelectionView current;

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
        }
        catch (Exception ex)
        {
            Debug.LogWarning("GAMEFEEL: region ownership display failed: " + ex.Message);
        }
    }
}
