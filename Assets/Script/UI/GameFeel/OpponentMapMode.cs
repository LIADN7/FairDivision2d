using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Display only (visual polish). The look of the "other player's values" view in Info rounds as one dark-red mode:
// - map frame: the existing OtherMapFrame bars (colour as authored in the scene) plus a soft outer glow;
// - map board: a subtle dim of the board BEHIND the cells (heat squares, cell colours and labels are not tinted);
// - see-other toggle: deep-red fill (was plain red), white label, a light ring and one soft pulse when the mode starts;
// - value-owner plate: deep-red fill with a light border; legend and map titles in a soft rose;
// all with one short transition. Manager calls Set with the state it already derives (isShowView while the toggle is
// available); this class reads nothing else, writes only colours / its own decor objects, and gates nothing. The
// toggle's rules, CountPlayerSeeOther, the readouts and every text are unchanged.
public sealed class OpponentMapMode
{
    public struct Parts
    {
        public GameObject frame;          // Canvas/States/OtherMapFrame: world sprite bars around the map box
        public Image toggle;              // SeeOtherPlayerBT's own Image (its colour only)
        public TMP_Text toggleLabel;      // its TextRTL label (colour only)
        public Color toggleLabelActive;
        public Image plate;               // value-owner plate (colour only)
        public Color plateOwn;
        public Color plateOther;
        public TMP_Text legendTitle;      // legend title (colour only)
    }

    // One accent family: deep red (no violet), on the game's dark navy UI.
    private static readonly Color ToggleOwn = Color.white;
    private static readonly Color ToggleFill = new Color(0.62f, 0.10f, 0.13f, 1f);   // #9E1A21
    private static readonly Color AccentLight = new Color(1f, 0.56f, 0.56f, 1f);     // ring / plate border
    private static readonly Color TitleRose = new Color(1f, 0.74f, 0.74f, 1f);       // legend + map title text
    // Behind the cells only. Neutral dark, not red: any red over the blue water would read violet.
    private static readonly Color BoardDim = new Color(0.02f, 0.03f, 0.06f, 0.24f);
    private static readonly Color GlowColor = new Color(0.86f, 0.17f, 0.20f, 1f);   // a little brighter than the bars
    private const float GlowAlpha = 0.5f;
    private const float GlowExtent = 0.34f; // world units beyond the frame bars
    private const float RingScale = 1.2f;   // ring image size / toggle size
    private const float PulseGrow = 0.10f;

    private readonly MonoBehaviour owner;
    private readonly Parts parts;
    private readonly SpriteRenderer[] bars;
    private readonly Color[] barColors;
    private readonly Color toggleLabelOwn;
    private readonly Color legendOwn;
    private readonly Text mapTitle;
    private readonly Color mapTitleOwn;
    private SpriteRenderer glow;
    private SpriteRenderer boardDim;
    private Image ring;
    private Outline plateBorder;
    private float amount;   // shown: 0 own view .. 1 other player's view
    private bool target;
    private int tween = -1;
    private int pulseTween = -1;

    public OpponentMapMode(MonoBehaviour owner, Parts parts)
    {
        this.owner = owner;
        this.parts = parts;
        bars = parts.frame != null ? parts.frame.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        barColors = new Color[bars.Length];
        for (int i = 0; i < bars.Length; i++)
        {
            barColors[i] = bars[i].color;
        }
        toggleLabelOwn = parts.toggleLabel != null ? parts.toggleLabel.color : Color.black;
        legendOwn = parts.legendTitle != null ? parts.legendTitle.color : Color.white;
        Transform mapTexts = parts.frame != null && parts.frame.transform.parent != null ? parts.frame.transform.parent.Find("MapTexts/BelfastTitle") : null;
        mapTitle = mapTexts != null ? mapTexts.GetComponent<Text>() : null;
        mapTitleOwn = mapTitle != null ? mapTitle.color : Color.white;
    }

    // Called after Manager has written its own instant state (plate colour, toggle colour), so the shown blend is
    // re-applied first; then a short transition runs to the new state.
    public void Set(bool showingOther)
    {
        bool starts = showingOther && !target;
        target = showingOther;
        float to = showingOther ? 1f : 0f;
        UiMotion.Stop(ref tween);
        Apply(amount);
        if (Mathf.Approximately(amount, to))
        {
            return;
        }
        if (owner == null || !GameFeel.Animate(owner))
        {
            Apply(to);
            return;
        }
        float from = amount;
        tween = UiMotion.Play(owner.gameObject, UiMotion.Normal, t => SafeApply(Mathf.Lerp(from, to, t)));
        if (starts)
        {
            Pulse();
        }
    }

    // Tween callbacks run inside LeanTween's update; a display failure is logged once per tween and the tween stopped.
    private void SafeApply(float m)
    {
        try
        {
            Apply(m);
        }
        catch (System.Exception ex)
        {
            UiMotion.Stop(ref tween);
            Debug.LogWarning("GAMEFEEL: opponent-map mode transition failed: " + ex.Message);
        }
    }

    private void Apply(float m)
    {
        amount = Mathf.Clamp01(m);
        bool on = amount > 0.001f;

        if (parts.frame != null)
        {
            if (on) EnsureFrameDecor();
            if (parts.frame.activeSelf != on) parts.frame.SetActive(on);
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i] == null || bars[i] == glow || bars[i] == boardDim) continue;
                Color c = barColors[i];
                c.a *= amount;
                bars[i].color = c;
            }
            if (glow != null)
            {
                Color g = GlowColor;
                g.a = GlowAlpha * amount;
                glow.color = g;
            }
            if (boardDim != null)
            {
                Color d = BoardDim;
                d.a *= amount;
                boardDim.color = d;
            }
        }

        if (parts.toggle != null)
        {
            parts.toggle.color = Color.Lerp(ToggleOwn, ToggleFill, amount);
            if (on) EnsureRing();
            if (ring != null)
            {
                Color r = AccentLight;
                r.a *= amount;
                ring.color = r;
                if (ring.enabled != on) ring.enabled = on;
            }
        }
        if (parts.toggleLabel != null)
        {
            parts.toggleLabel.color = Color.Lerp(toggleLabelOwn, parts.toggleLabelActive, amount);
        }
        if (parts.plate != null)
        {
            parts.plate.color = Color.Lerp(parts.plateOwn, parts.plateOther, amount);
            if (on && plateBorder == null)
            {
                plateBorder = parts.plate.gameObject.AddComponent<Outline>();
                plateBorder.effectDistance = new Vector2(1.6f, -1.6f);
                plateBorder.useGraphicAlpha = false;
            }
            if (plateBorder != null)
            {
                Color b = AccentLight;
                b.a = 0.85f * amount;
                plateBorder.effectColor = b;
                if (plateBorder.enabled != on) plateBorder.enabled = on;
            }
        }
        if (parts.legendTitle != null)
        {
            parts.legendTitle.color = Color.Lerp(legendOwn, TitleRose, amount);
        }
        if (mapTitle != null)
        {
            mapTitle.color = Color.Lerp(mapTitleOwn, TitleRose, amount);
        }
    }

    // Glow around the frame and the board dim, created once under the frame (so they show and hide with it, and the
    // Help page hides them with the map). The glow lies outside the frame bars; the dim lies just in front of the
    // board and behind the cells, so no cell, heat value or label is tinted.
    private void EnsureFrameDecor()
    {
        if (glow != null || bars.Length == 0)
        {
            return;
        }
        Bounds outer = new Bounds();
        bool any = false;
        float barZ = 0f;
        float thickness = float.MaxValue;
        foreach (SpriteRenderer bar in bars)
        {
            if (bar == null) continue;
            Bounds b = bar.bounds;
            thickness = Mathf.Min(thickness, Mathf.Min(b.size.x, b.size.y));
            barZ = b.center.z;
            if (any) outer.Encapsulate(b);
            else { outer = b; any = true; }
        }
        if (!any)
        {
            return;
        }
        Sprite square = bars[0].sprite;
        Transform frameT = parts.frame.transform;

        glow = NewSprite("ModeGlow", frameT, UiGeneratedSprites.HollowGlow, bars[0].sortingLayerID, bars[0].sortingOrder);
        glow.drawMode = SpriteDrawMode.Sliced;
        Vector3 ls = frameT.lossyScale;
        // The glow band starts at the inner edge of the bars (its transparent inset and rise stay hidden behind them) and
        // reaches GlowExtent beyond them. Sprite PPU 100: scale the renderer so the border spans the band, size it in
        // sprite units.
        Vector2 inner = new Vector2(outer.size.x - 2f * thickness, outer.size.y - 2f * thickness);
        float band = thickness + GlowExtent;
        float k = band / (UiGeneratedSprites.HollowGlowBorder / 100f);
        glow.transform.position = new Vector3(outer.center.x, outer.center.y, barZ + 0.2f);
        glow.transform.localScale = new Vector3(k / SafeAbs(ls.x), k / SafeAbs(ls.y), 1f);
        glow.size = new Vector2((inner.x + 2f * band) / k, (inner.y + 2f * band) / k);

        Transform board = frameT.parent != null ? frameT.parent.Find("MapBoard") : null;
        SpriteRenderer boardSr = board != null ? board.GetComponent<SpriteRenderer>() : null;
        if (boardSr != null && square != null)
        {
            Bounds bb = boardSr.bounds;
            boardDim = NewSprite("ModeBoardDim", frameT, square, boardSr.sortingLayerID, boardSr.sortingOrder);
            boardDim.transform.position = new Vector3(bb.center.x, bb.center.y, bb.center.z - 1f);
            Vector2 spriteSize = square.bounds.size;
            boardDim.transform.localScale = new Vector3(
                bb.size.x / (spriteSize.x * SafeAbs(ls.x)), bb.size.y / (spriteSize.y * SafeAbs(ls.y)), 1f);
        }
    }

    private static SpriteRenderer NewSprite(string name, Transform parent, Sprite sprite, int layer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerID = layer;
        sr.sortingOrder = order;
        sr.color = new Color(1f, 1f, 1f, 0f);
        return sr;
    }

    private static float SafeAbs(float v)
    {
        return Mathf.Abs(v) > 1e-6f ? Mathf.Abs(v) : 1f;
    }

    // Thin light ring just outside the round toggle (a non-raycast child added last, so existing child indices and the
    // label are unchanged).
    private void EnsureRing()
    {
        if (ring != null || parts.toggle == null)
        {
            return;
        }
        GameObject go = new GameObject("ModeRing", typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parts.toggle.transform, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        Vector2 size = parts.toggle.rectTransform.rect.size;
        float d = Mathf.Max(size.x, size.y) * RingScale;
        rt.sizeDelta = new Vector2(d, d);
        ring = go.AddComponent<Image>();
        ring.sprite = UiGeneratedSprites.CircleRing;
        ring.raycastTarget = false;
        ring.preserveAspect = true;
        ring.color = new Color(1f, 1f, 1f, 0f);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    // One soft outward pulse of the ring when the mode starts.
    private void Pulse()
    {
        UiMotion.Stop(ref pulseTween);
        EnsureRing();
        if (ring == null || owner == null)
        {
            return;
        }
        RectTransform rt = ring.rectTransform;
        pulseTween = UiMotion.Play(owner.gameObject, UiMotion.Emphasis + 0.06f, t =>
        {
            if (rt != null) rt.localScale = Vector3.one * (1f + PulseGrow * UiMotion.Bump(t));
        });
    }
}
