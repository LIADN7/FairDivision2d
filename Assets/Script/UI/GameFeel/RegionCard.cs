using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Display only (game-feel batch). Styles one region button: fill colour, label colour, outline ring and ownership badge,
// with short transitions. It reads Button.interactable but never writes it, never touches the button's OnClick, and owns
// no raycast targets (all of its graphics have raycastTarget off), so region selection (Player.ChooseView / GetLast,
// which use Button.IsInteractable) is unchanged. Lives on a decor child of the region button. Motion is cosmetic only:
// every state is applied at once when effects are off, outside Play Mode, or while the card is hidden.
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
    [SerializeField] private Image flash;
    [SerializeField] private Image badge;
    [SerializeField] private Image badgeIcon;
    [SerializeField] private Sprite checkIcon;
    [SerializeField] private Sprite lockIcon;
    [SerializeField] private int staggerIndex;

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

    private Look shown;
    private bool hasShown;
    private int lookTween = -1;
    private int fxTween = -1;
    private int badgeTween = -1;

    public Owner CurrentOwner => owner;

    // True only while this player may pick this region now (display hint; gates nothing).
    public bool IsSelectableNow => choosePhase && myTurn && owner == Owner.None && button != null && button.interactable;

    private struct Look
    {
        public Color fill;
        public Color label;
        public Color ring;
        public Color badge;
        public bool badgeOn;
        public Sprite icon;
        public Color iconColor;
    }

    private void OnEnable()
    {
        Refresh(true);
    }

    private void OnDisable()
    {
        UiMotion.Stop(ref lookTween);
        UiMotion.Stop(ref fxTween);
        UiMotion.Stop(ref badgeTween);
        ResetFx();
        Refresh(true); // settle on the final look while hidden
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
        bool valuesAppeared = isChoosePhase && !choosePhase;
        bool turnStarted = isChoosePhase && isMyTurn && !myTurn;
        choosePhase = isChoosePhase;
        myTurn = isMyTurn;
        Refresh(false);
        if (valuesAppeared)
        {
            PlayFlash(0.35f, UiMotion.Emphasis + 0.06f, 0.06f * staggerIndex, 0f);
        }
        else if (turnStarted && IsSelectableNow)
        {
            PlayFlash(0f, UiMotion.Emphasis + 0.06f, 0.05f * staggerIndex, 0.10f);
        }
    }

    public void SetOwner(Owner newOwner)
    {
        bool changed = newOwner != owner;
        owner = newOwner;
        Refresh(false);
        if (changed && newOwner != Owner.None)
        {
            // The chosen region gets the strongest emphasis; a region taken by the other player a lighter one.
            PlayFlash(newOwner == Owner.Mine ? 0.55f : 0.25f, 0.30f, 0f, newOwner == Owner.Mine ? 0.14f : 0.06f);
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

    private Look ComputeLook()
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
            if (hover)
            {
                look.fill = Color.Lerp(regionColor, Color.white, 0.14f);
            }
        }
        else if (choosePhase)
        {
            // Available, but the other player is choosing now.
            look.fill = Color.Lerp(regionColor, waitDim, 0.28f);
        }
        return look;
    }

    private void Refresh(bool instant)
    {
        Look target = ComputeLook();
        bool badgeAppears = target.badgeOn && (!hasShown || !shown.badgeOn);
        UiMotion.Stop(ref lookTween);
        if (instant || !hasShown || !GameFeel.Animate(this))
        {
            Apply(target, target, 1f);
            if (badgeAppears) SetBadgeScale(1f);
            return;
        }
        Look from = shown;
        lookTween = UiMotion.Play(gameObject, hover || !target.badgeOn ? UiMotion.Fast + 0.03f : UiMotion.Normal, t => Apply(from, target, t));
        if (badgeAppears)
        {
            UiMotion.Stop(ref badgeTween);
            SetBadgeScale(0.3f);
            badgeTween = UiMotion.Play(gameObject, UiMotion.Emphasis, t => SetBadgeScale(Mathf.LerpUnclamped(0.3f, 1f, t)), true, 0.05f);
        }
    }

    private void Apply(Look from, Look to, float t)
    {
        Look now = new Look
        {
            fill = Color.Lerp(from.fill, to.fill, t),
            label = Color.Lerp(from.label, to.label, t),
            ring = Color.Lerp(from.ring, to.ring, t),
            badge = to.badge,
            badgeOn = to.badgeOn,
            icon = to.icon,
            iconColor = to.iconColor,
        };
        shown = now;
        hasShown = true;
        if (fill != null) fill.color = now.fill;
        if (label != null) label.color = now.label;
        if (ring != null) ring.color = now.ring;
        if (badge != null)
        {
            badge.color = now.badge;
            if (badge.gameObject.activeSelf != now.badgeOn) badge.gameObject.SetActive(now.badgeOn);
        }
        if (badgeIcon != null)
        {
            badgeIcon.sprite = now.icon;
            badgeIcon.color = now.iconColor;
            badgeIcon.enabled = now.icon != null;
        }
    }

    private void SetBadgeScale(float s)
    {
        if (badge != null) badge.rectTransform.localScale = Vector3.one * s;
    }

    // White flash over the card plus an outward ring pulse; purely cosmetic and self-resetting.
    private void PlayFlash(float flashAlpha, float duration, float delay, float ringGrow)
    {
        UiMotion.Stop(ref fxTween);
        ResetFx();
        if (!GameFeel.Animate(this))
        {
            return;
        }
        fxTween = UiMotion.Play(gameObject, duration, t =>
        {
            float b = UiMotion.Bump(t);
            if (flash != null) flash.color = new Color(1f, 1f, 1f, flashAlpha * b);
            if (ring != null) ring.rectTransform.localScale = Vector3.one * (1f + ringGrow * b);
        }, false, delay);
    }

    private void ResetFx()
    {
        if (flash != null) flash.color = new Color(1f, 1f, 1f, 0f);
        if (ring != null) ring.rectTransform.localScale = Vector3.one;
        if (badge != null && !GameFeel.Animate(this)) badge.rectTransform.localScale = Vector3.one;
    }
}
