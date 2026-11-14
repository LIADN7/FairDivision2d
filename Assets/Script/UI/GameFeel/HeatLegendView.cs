using UnityEngine;
using UnityEngine.UI;

// Display only (UX clarity batch). Keeps the existing map legend (low / medium / high samples on a red and a green cell)
// in step with the cell shading: each sample is drawn at runtime the way a map cell is drawn - the region colour, the
// inner heat square from HeatShading (same size and alpha as PointOfState's child square) and the map's translucent
// white cover (MapBoundaries) on top. Labels, title and layout are unchanged; only the sample textures are replaced.
// The legend shows relative levels only (no numbers), the three levels the map already uses (values 2, 4 and 8).
[DisallowMultipleComponent]
public class HeatLegendView : MonoBehaviour
{
    [SerializeField] private RawImage lowGreen;
    [SerializeField] private RawImage lowRed;
    [SerializeField] private RawImage midGreen;
    [SerializeField] private RawImage midRed;
    [SerializeField] private RawImage highGreen;
    [SerializeField] private RawImage highRed;
    [SerializeField] private Color mapCover = new Color(1f, 1f, 1f, 0.318f); // MapBoundaries colour in both scenes
    [SerializeField] private int texSize = 40;

    private readonly System.Collections.Generic.List<Texture2D> created = new System.Collections.Generic.List<Texture2D>();

    private void Awake()
    {
        Apply(lowGreen, 0f, Color.green);
        Apply(lowRed, 0f, Color.red);
        Apply(midGreen, 0.5f, Color.green);
        Apply(midRed, 0.5f, Color.red);
        Apply(highGreen, 1f, Color.green);
        Apply(highRed, 1f, Color.red);
    }

    private void OnDestroy()
    {
        foreach (Texture2D t in created)
        {
            if (t != null) Destroy(t);
        }
        created.Clear();
    }

    private void Apply(RawImage image, float level, Color region)
    {
        if (image == null)
        {
            return;
        }
        int n = Mathf.Max(8, texSize);
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color heat = HeatShading.LevelColor(level);
        Color inner = Cover(Color.Lerp(region, new Color(heat.r, heat.g, heat.b, 1f), heat.a));
        Color outer = Cover(region);
        int innerSize = Mathf.RoundToInt(n * HeatShading.InnerScale);
        int lo = (n - innerSize) / 2;
        int hi = lo + innerSize;
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                bool inInner = x >= lo && x < hi && y >= lo && y < hi;
                px[y * n + x] = inInner ? inner : outer;
            }
        }
        tex.SetPixels(px);
        tex.Apply(false, true);
        created.Add(tex);
        image.texture = tex;
        image.uvRect = new Rect(0f, 0f, 1f, 1f);
    }

    private Color Cover(Color c)
    {
        Color o = Color.Lerp(c, new Color(mapCover.r, mapCover.g, mapCover.b, 1f), mapCover.a);
        o.a = 1f;
        return o;
    }
}
