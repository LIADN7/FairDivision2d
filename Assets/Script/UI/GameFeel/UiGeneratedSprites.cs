using UnityEngine;

// Display only (visual polish). Small textures and sprites generated once at runtime, so the polish needs no new art
// assets: anti-aliased line profiles for LineRenderer outlines, a thin circular ring and a hollow soft glow for frames.
// Pure pixels; nothing here reads or writes game state. Cached for the session and rebuilt if Unity unloaded them.
public static class UiGeneratedSprites
{
    private static Texture2D lineCore;
    private static Texture2D lineHalo;
    private static Material lineCoreMaterial;
    private static Material lineHaloMaterial;
    private static Sprite circleRing;
    private static Sprite hollowGlow;

    public const int HollowGlowBorder = 32; // pixels of the sliced glow sprite's border (the glow band)

    // LineRenderer profile across the width (texture v): opaque core with soft edges, i.e. an anti-aliased stroke.
    public static Texture2D LineCore
    {
        get
        {
            if (lineCore == null)
            {
                lineCore = MakeProfile("UiLineCore", v => Mathf.Clamp01(Mathf.Min(v, 1f - v) / 0.24f));
            }
            return lineCore;
        }
    }

    // LineRenderer profile across the width: a soft bell, used for the dark halo under an outline.
    public static Texture2D LineHalo
    {
        get
        {
            if (lineHalo == null)
            {
                lineHalo = MakeProfile("UiLineHalo", v =>
                {
                    float d = Mathf.Abs(v - 0.5f) * 2f; // 0 centre .. 1 edge
                    float a = 1f - d;
                    return a * a * (3f - 2f * a);
                });
            }
            return lineHalo;
        }
    }

    // Materials for LineRenderers built on the given base material (a Sprites/Default material already in use, so
    // the shader is guaranteed to be in the build). Vertex colour tints; the texture gives the soft profile.
    public static Material LineCoreMaterial(Material baseMaterial)
    {
        if (lineCoreMaterial == null)
        {
            lineCoreMaterial = MakeLineMaterial(baseMaterial, LineCore, "UiLineCoreMat");
        }
        return lineCoreMaterial;
    }

    public static Material LineHaloMaterial(Material baseMaterial)
    {
        if (lineHaloMaterial == null)
        {
            lineHaloMaterial = MakeLineMaterial(baseMaterial, LineHalo, "UiLineHaloMat");
        }
        return lineHaloMaterial;
    }

    // White ring for UI Images: inner radius 0.875, outer 0.94 of the half size, anti-aliased.
    public static Sprite CircleRing
    {
        get
        {
            if (circleRing == null)
            {
                const int size = 160;
                Texture2D tex = NewTexture("UiCircleRing", size, size);
                Color32[] px = new Color32[size * size];
                float half = size * 0.5f;
                float inner = 0.875f * half;
                float outer = 0.94f * half;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half));
                        float a = Mathf.Clamp01(r - inner + 0.5f) * Mathf.Clamp01(outer - r + 0.5f);
                        px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                circleRing = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                circleRing.name = "UiCircleRing";
            }
            return circleRing;
        }
    }

    // Sliced sprite for a SpriteRenderer (draw mode Sliced): transparent centre, and a glow band of HollowGlowBorder
    // pixels on every side that rises just outside the centre and fades out. Placed around a box, it glows outside it
    // only, so whatever is inside the box is never tinted. The first HollowGlowInset pixels of the band stay transparent
    // too: the centre is stretched over the whole box, so any alpha next to it would be smeared inwards by filtering.
    public const int HollowGlowInset = 3;

    public static Sprite HollowGlow
    {
        get
        {
            if (hollowGlow == null)
            {
                const int b = HollowGlowBorder;
                const int size = b * 2 + 2;
                Texture2D tex = NewTexture("UiHollowGlow", size, size);
                Color32[] px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // Distance (pixels) from the 2x2 centre square, i.e. from the box edge once sliced.
                        float dx = Mathf.Max(0f, Mathf.Max(b - (x + 0.5f), (x + 0.5f) - (b + 2)));
                        float dy = Mathf.Max(0f, Mathf.Max(b - (y + 0.5f), (y + 0.5f) - (b + 2)));
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = 0f;
                        if (d > HollowGlowInset)
                        {
                            float u = Mathf.Clamp01((d - HollowGlowInset) / (b - HollowGlowInset));
                            float rise = Mathf.Clamp01((d - HollowGlowInset) / 2f);
                            a = rise * Mathf.Pow(1f - u, 2.2f);
                        }
                        px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                hollowGlow = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(b, b, b, b));
                hollowGlow.name = "UiHollowGlow";
            }
            return hollowGlow;
        }
    }

    private static Texture2D MakeProfile(string name, System.Func<float, float> alphaAcross)
    {
        const int h = 32;
        Texture2D tex = NewTexture(name, 2, h);
        Color32[] px = new Color32[2 * h];
        for (int y = 0; y < h; y++)
        {
            byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alphaAcross((y + 0.5f) / h)) * 255f);
            px[y * 2] = new Color32(255, 255, 255, a);
            px[y * 2 + 1] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return tex;
    }

    private static Texture2D NewTexture(string name, int w, int h)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
    }

    private static Material MakeLineMaterial(Material baseMaterial, Texture2D texture, string name)
    {
        Material m = baseMaterial != null ? new Material(baseMaterial) : new Material(Shader.Find("Sprites/Default"));
        m.name = name;
        m.mainTexture = texture;
        m.color = Color.white;
        return m;
    }
}
