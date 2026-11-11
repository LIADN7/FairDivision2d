using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Display only (game-feel batch). Styles one region button: fill colour, label colour, outline ring and ownership badge.
// It reads Button.interactable but never writes it, never touches the button's OnClick, and owns no raycast targets
// (all of its graphics have raycastTarget off), so region selection (Player.ChooseView / GetLast, which use
// Button.IsInteractable) is unchanged. Lives on a decor child of the region button.
[DisallowMultipleComponent]
public class RegionCard : MonoBehaviour
{
    public enum Owner { None, Mine, Other }

    [Header("Existing objects (read only / colour only)")]
    [SerializeField] private Button button;     // region button: interactable is only read
    [SerializeField] private Image fill;        // the region button's own Image (its Button target graphic)
    [SerializeField] private TMP_Text label;    // the existing TextRTL label (Player writes its text; we set colour only)

    [Header("Decor (no raycast)")]
    [SerializeField] private Image ring;
    [SerializeField] private Image badge;
    [SerializeField] private Image badgeIcon;
    [SerializeField] private Sprite checkIcon;
    [SerializeField] private Sprite lockIcon;

    [Header("Palette")]
    [SerializeField] private Color regionColor = Color.white;
    [SerializeField] private Color labelOnRegion = Color.white;
    [SerializeField] private Color mutedTint = new Color(0.33f, 0.37f, 0.45f, 1f);
    [SerializeField] private Color mutedLabel = new Color(0.88f, 0.91f, 0.95f, 1f);
    [SerializeField] private Color waitDim = new Color(0.07f, 0.11f, 0.19f, 1f);
    [SerializeField] private Color mineAccent = new Color(1f, 0.79f, 0.34f, 1f);
    [SerializeField] private Color mineIcon = new Color(0.11f, 0.10f, 0.07f, 1f);
    [SerializeField] private Color otherBadge = new Color(0.16f, 0.21f, 0.29f, 1f);
    [SerializeField] private Color turnRing = new Color(1f, 1f, 1f, 0.55f);
    [SerializeField] private Color hoverRing = new Color(1f, 1f, 1f, 1f);

    private Owner owner = Owner.None;
    private bool choosePhase;
    private bool myTurn;
    private bool hover;
    private bool lastInteractable = true;

    public Owner CurrentOwner => owner;

    // True only while this player may pick this region now (display hint for hover feedback; gates nothing).
    public bool IsSelectableNow => choosePhase && myTurn && owner == Owner.None && button != null && button.interactable;

    public RectTransform BadgeTransform => badge != null ? badge.rectTransform : null;
    public Image Ring => ring;

    protected virtual void OnEnable()
    {
        Refresh(true);
    }

    private void LateUpdate()
    {
        // Fallback: a region that became non-interactable without an ownership report still looks taken.
        if (button != null && button.interactable != lastInteractable)
        {
            Refresh(false);
        }
    }

    public void SetPhase(bool isChoosePhase, bool isMyTurn)
    {
        if (isChoosePhase == choosePhase && isMyTurn == myTurn)
        {
            return;
        }
        choosePhase = isChoosePhase;
        myTurn = isMyTurn;
        Refresh(false);
    }

    public void SetOwner(Owner newOwner)
    {
        bool changed = newOwner != owner;
        owner = newOwner;
        Refresh(false);
        if (changed && newOwner != Owner.None)
        {
            OnTaken(newOwner);
        }
    }

    public void SetHover(bool isHover)
    {
        if (isHover == hover)
        {
            return;
        }
        hover = isHover;
        Refresh(false);
    }

    protected struct Look
    {
        public Color fill;
        public Color label;
        public Color ring;
        public Color badge;
        public bool badgeOn;
        public Sprite icon;
        public Color iconColor;
    }

    protected Look ComputeLook()
    {
        lastInteractable = button == null || button.interactable;
        bool taken = owner != Owner.None || !lastInteractable;
        Look look = new Look
        {
            fill = regionColor,
            label = labelOnRegion,
            ring = new Color(1f, 1f, 1f, 0f),
            badge = new Color(1f, 1f, 1f, 0f),
            badgeOn = false,
            icon = null,
            iconColor = Color.white,
        };
        if (owner == Owner.Mine)
        {
            look.ring = mineAccent;
            look.badge = mineAccent;
            look.badgeOn = true;
            look.icon = checkIcon;
            look.iconColor = mineIcon;
        }
        else if (taken)
        {
            look.fill = Color.Lerp(regionColor, mutedTint, 0.72f);
            look.label = mutedLabel;
            if (owner == Owner.Other)
            {
                look.badge = otherBadge;
                look.badgeOn = true;
                look.icon = lockIcon;
                look.iconColor = mutedLabel;
            }
        }
        else if (choosePhase && myTurn)
        {
            look.ring = hover ? hoverRing : turnRing;
        }
        else if (choosePhase)
        {
            // Available, but the other player is choosing now.
            look.fill = Color.Lerp(regionColor, waitDim, 0.28f);
        }
        return look;
    }

    protected virtual void Refresh(bool instant)
    {
        Apply(ComputeLook());
    }

    protected void Apply(Look look)
    {
        if (fill != null) fill.color = look.fill;
        if (label != null) label.color = look.label;
        if (ring != null) ring.color = look.ring;
        if (badge != null)
        {
            badge.color = look.badge;
            badge.gameObject.SetActive(look.badgeOn);
        }
        if (badgeIcon != null)
        {
            badgeIcon.sprite = look.icon;
            badgeIcon.color = look.iconColor;
            badgeIcon.enabled = look.icon != null;
        }
    }

    // Stage 2 hook for the selection emphasis.
    protected virtual void OnTaken(Owner newOwner)
    {
    }
}
