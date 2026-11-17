using System.Collections.Generic;
using UnityEngine;

// Display only (UX clarity batch). Draws the link between the four region cards and the map:
// - while a card is hovered or keyboard-focused, the cells of every other region get a dark veil (spotlight);
// - regions the local player received get an amber outline, regions the other player received a muted veil.
// It only READS each map cell's current region status (1-4, written by Player / PlayerAITutorial after both cuts) and its
// position. It owns its own renderers under one holder beside the cells (not under a county, not tagged SquarePoint, no
// PointOfState), so cell lookups, county sums, cut classification, region values, RPCs and saves are unaffected.
// Ownership comes from the existing RegionCard state, which follows the Choose / GetValues notifications: nothing is
// shown before the card itself shows it.
// Visual polish: each region's outline is traced as closed contours around its cells (fine mask + marching squares),
// smoothed (Taubin, then Chaikin) and drawn as an anti-aliased line over a soft dark halo, instead of one square edge
// per cell side. Veils and outlines move to a new state through Step(t) (the caller animates t 0 -> 1). If a contour
// cannot be traced, that region falls back to the per-cell edges.
public class RegionMapHighlight
{
    public struct Style
    {
        public Color hoverOutline;
        public Color mineOutline;
        public Color outlineShadow;
        public Color spotlightVeil;
        public Color otherVeil;
        public float outlineWidth;
    }

    private struct Cell
    {
        public PointOfState core;
        public PointOfStateAITutorial tutorial; // practice cells (Tyrone cells there carry both components; this one is live)
        public Vector3 pos;
        public float size;
        public float frontZ; // just in front of the cell's heat square
    }

    // One region's outline renderers and its animated stroke colour (the halo follows the stroke's alpha).
    private sealed class RegionOutline
    {
        public readonly List<LineRenderer> strokes = new List<LineRenderer>();
        public readonly List<LineRenderer> halos = new List<LineRenderer>();
        public readonly List<SpriteRenderer> edgeStrokes = new List<SpriteRenderer>(); // fallback only
        public readonly List<SpriteRenderer> edgeHalos = new List<SpriteRenderer>();   // fallback only
        public int strokesUsed;
        public int edgesUsed;
        public Color from;
        public Color to;
        public Color now;
    }

    private static readonly Vector2[] Dirs = { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
    private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

    private const float StrokeFactor = 1.3f;  // smooth stroke width = outlineWidth * factor
    private const float HaloFactor = 3.4f;    // soft halo width = outlineWidth * factor
    private const float MaskScale = 1.06f;    // cell square in the mask, relative to the cell (closes hairline gaps)
    private const float BlurSigma = 0.42f;    // Gaussian blur of the mask, in cells
    private const float MaskIso = 0.32f;      // contour level (< 0.5: just outside the cells, never through them)
    private const float ResampleSpacing = 0.35f; // contour point spacing before the final smoothing, in cells
    private const int TaubinPasses = 3;
    private const int ChaikinPasses = 1;

    private readonly Transform mapRoot;
    private readonly Style style;
    private readonly List<Cell> cells = new List<Cell>();
    private readonly RegionOutline[] outlines = { new RegionOutline(), new RegionOutline(), new RegionOutline(), new RegionOutline() };
    private readonly List<int> animatingVeils = new List<int>();
    private int[,] neighbours;
    private int[] region; // region index per cell when the outlines were built (-1: none)
    private SpriteRenderer[] veils;
    private Color[] veilNow;
    private Color[] veilFrom;
    private Color[] veilTo;
    private Transform holder;
    private Sprite squareSprite;
    private Material baseMaterial;
    private int sortingLayerId;
    private int sortingOrder;
    private float minFrontZ;
    private float cellSize; // typical (median) cell size, world units

    public RegionMapHighlight(Transform mapRoot, Style style)
    {
        this.mapRoot = mapRoot;
        this.style = style;
    }

    // active: the four regions exist (choose phase or later). spotlight: region index 0-3 shown in focus, or -1.
    // owners: per region index (same order as the cards: red, green, yellow, blue), from RegionCard.CurrentOwner.
    // Sets the new target look; Step(t) moves the shown look towards it (Step(1) applies it at once).
    public void Render(bool active, int spotlight, RegionCard.Owner[] owners)
    {
        if (!active)
        {
            HideAll();
            return;
        }
        if (!EnsureCells())
        {
            return;
        }

        int[] current = new int[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            current[i] = ReadRegion(cells[i]);
        }
        if (region == null || !SameRegions(current))
        {
            region = current;
            RebuildOutlines();
        }

        animatingVeils.Clear();
        for (int i = 0; i < cells.Count; i++)
        {
            int r = region[i];
            Color target = Clear;
            if (r >= 0)
            {
                if (spotlight >= 0)
                {
                    if (r != spotlight) target = style.spotlightVeil;
                }
                else if (owners != null && r < owners.Length && owners[r] == RegionCard.Owner.Other)
                {
                    target = style.otherVeil;
                }
            }
            Color now = veilNow[i];
            if (now == target || (now.a <= 0.001f && target.a <= 0.001f))
            {
                continue;
            }
            veilFrom[i] = now.a <= 0.001f ? new Color(target.r, target.g, target.b, 0f) : now;
            veilTo[i] = target.a <= 0.001f ? new Color(now.r, now.g, now.b, 0f) : target;
            animatingVeils.Add(i);
        }

        for (int r = 0; r < 4; r++)
        {
            RegionOutline o = outlines[r];
            bool mine = owners != null && r < owners.Length && owners[r] == RegionCard.Owner.Mine;
            Color target = mine ? style.mineOutline : (r == spotlight ? style.hoverOutline : new Color(o.now.r, o.now.g, o.now.b, 0f));
            o.from = o.now.a <= 0.001f ? new Color(target.r, target.g, target.b, 0f) : o.now;
            o.to = target;
        }
    }

    // t in 0..1: the shown look between the previous one and the target set by Render.
    public void Step(float t)
    {
        if (region == null || veilNow == null)
        {
            return;
        }
        t = Mathf.Clamp01(t);
        foreach (int i in animatingVeils)
        {
            ApplyVeil(i, Color.Lerp(veilFrom[i], veilTo[i], t));
        }
        for (int r = 0; r < 4; r++)
        {
            RegionOutline o = outlines[r];
            o.now = Color.Lerp(o.from, o.to, t);
            ApplyOutline(o);
        }
    }

    public void HideAll()
    {
        animatingVeils.Clear();
        if (veils != null)
        {
            for (int i = 0; i < veils.Length; i++)
            {
                if (veils[i] != null && veils[i].enabled) veils[i].enabled = false;
                veilNow[i] = Clear;
                veilFrom[i] = Clear;
                veilTo[i] = Clear;
            }
        }
        foreach (RegionOutline o in outlines)
        {
            o.now = Clear;
            o.from = Clear;
            o.to = Clear;
            ApplyOutline(o);
        }
    }

    private static int ReadRegion(Cell c)
    {
        int status = c.tutorial != null ? c.tutorial.getSpriteStatus() : (c.core != null ? c.core.getSpriteStatus() : 0);
        return status >= 1 && status <= 4 ? status - 1 : -1;
    }

    private bool SameRegions(int[] current)
    {
        if (current.Length != region.Length) return false;
        for (int i = 0; i < current.Length; i++)
        {
            if (current[i] != region[i]) return false;
        }
        return true;
    }

    private bool EnsureCells()
    {
        if (cells.Count > 0)
        {
            return holder != null;
        }
        if (mapRoot == null)
        {
            return false;
        }
        foreach (Transform t in mapRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!t.CompareTag("SquarePoint"))
            {
                continue;
            }
            PointOfStateAITutorial tutorial = t.GetComponent<PointOfStateAITutorial>();
            PointOfState core = t.GetComponent<PointOfState>();
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if ((tutorial == null && core == null) || sr == null)
            {
                continue;
            }
            if (squareSprite == null)
            {
                squareSprite = sr.sprite;
                baseMaterial = sr.sharedMaterial;
                sortingLayerId = sr.sortingLayerID;
                sortingOrder = sr.sortingOrder;
            }
            cells.Add(new Cell
            {
                core = core,
                tutorial = tutorial,
                pos = t.position,
                size = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y),
                frontZ = t.TransformPoint(new Vector3(0f, 0f, -3f)).z, // the heat square sits at local z -3
            });
        }
        if (cells.Count == 0 || squareSprite == null)
        {
            cells.Clear();
            return false;
        }

        // Holder at the world origin with unit world scale, so child sprites take world positions and sizes directly.
        holder = new GameObject("RegionMapHighlight").transform;
        holder.SetParent(mapRoot, false);
        holder.position = Vector3.zero;
        holder.rotation = Quaternion.identity;
        Vector3 ps = mapRoot.lossyScale;
        holder.localScale = new Vector3(SafeInverse(ps.x), SafeInverse(ps.y), SafeInverse(ps.z));

        minFrontZ = float.MaxValue;
        List<float> sizes = new List<float>(cells.Count);
        foreach (Cell c in cells)
        {
            minFrontZ = Mathf.Min(minFrontZ, c.frontZ);
            sizes.Add(c.size);
        }
        sizes.Sort();
        cellSize = Mathf.Max(1e-3f, sizes[sizes.Count / 2]);
        veils = new SpriteRenderer[cells.Count];
        veilNow = new Color[cells.Count];
        veilFrom = new Color[cells.Count];
        veilTo = new Color[cells.Count];
        BuildNeighbours();
        return true;
    }

    private static float SafeInverse(float v)
    {
        return Mathf.Abs(v) > 1e-6f ? 1f / v : 1f;
    }

    // Nearest cell on each side (right, up, left, down) of every cell, regardless of region. The cells sit on an almost
    // regular grid (spacing ~ cell size), so a neighbour is a cell about one cell size away in that direction.
    private void BuildNeighbours()
    {
        neighbours = new int[cells.Count, 4];
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2 p = cells[i].pos;
            float s = cells[i].size;
            for (int d = 0; d < 4; d++)
            {
                Vector2 dir = Dirs[d];
                Vector2 perpDir = new Vector2(-dir.y, dir.x);
                int best = -1;
                float bestScore = float.MaxValue;
                for (int j = 0; j < cells.Count; j++)
                {
                    if (j == i) continue;
                    Vector2 delta = (Vector2)cells[j].pos - p;
                    float along = Vector2.Dot(delta, dir);
                    float perp = Mathf.Abs(Vector2.Dot(delta, perpDir));
                    if (along < 0.35f * s || along > 1.6f * s || perp > 0.7f * s) continue;
                    float score = Mathf.Abs(along - s) + perp;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = j;
                    }
                }
                neighbours[i, d] = best;
            }
        }
    }

    private void ApplyVeil(int i, Color color)
    {
        veilNow[i] = color;
        bool on = color.a > 0.001f;
        SpriteRenderer v = veils[i];
        if (v == null)
        {
            if (!on) return;
            v = CreateSprite("Veil");
            Cell c = cells[i];
            v.transform.localPosition = new Vector3(c.pos.x, c.pos.y, c.frontZ - 0.2f);
            v.transform.localScale = new Vector3(c.size, c.size, 1f);
            veils[i] = v;
        }
        if (on) v.color = color;
        if (v.enabled != on) v.enabled = on;
    }

    private void ApplyOutline(RegionOutline o)
    {
        bool on = o.now.a > 0.002f;
        Color halo = style.outlineShadow;
        halo.a *= Mathf.Clamp01(o.now.a);
        for (int k = 0; k < o.strokes.Count; k++)
        {
            bool used = on && k < o.strokesUsed;
            SetLine(o.strokes[k], used, o.now);
            SetLine(o.halos[k], used, halo);
        }
        for (int k = 0; k < o.edgeStrokes.Count; k++)
        {
            bool used = on && k < o.edgesUsed;
            SetSprite(o.edgeStrokes[k], used, o.now);
            SetSprite(o.edgeHalos[k], used, halo);
        }
    }

    private static void SetLine(LineRenderer lr, bool on, Color c)
    {
        if (on)
        {
            lr.startColor = c;
            lr.endColor = c;
        }
        if (lr.enabled != on) lr.enabled = on;
    }

    private static void SetSprite(SpriteRenderer sr, bool on, Color c)
    {
        if (on) sr.color = c;
        if (sr.enabled != on) sr.enabled = on;
    }

    // Geometry of all four outlines for the current region map (colours are kept; ApplyOutline shows them).
    private void RebuildOutlines()
    {
        List<List<Vector2>> loops = new List<List<Vector2>>();
        for (int r = 0; r < 4; r++)
        {
            RegionOutline o = outlines[r];
            o.strokesUsed = 0;
            o.edgesUsed = 0;
            loops.Clear();
            if (TraceLoops(r, loops))
            {
                foreach (List<Vector2> loop in loops)
                {
                    AddSmoothLoop(o, loop);
                }
            }
            else
            {
                AddCellEdges(o, r);
            }
            ApplyOutline(o);
        }
    }

    private bool InRegion(int cell, int r)
    {
        return cell >= 0 && region[cell] == r;
    }

    // Closed contours of region r. The cells only sit on a NEAR-regular grid (neighbour links are not always mutual), so
    // the region is painted into a fine mask (a quarter cell per sample), blurred (Gaussian, BlurSigma cells), and the
    // iso-line at MaskIso is extracted with marching squares (linear interpolation), oriented with the region on the
    // left and chained into loops. The blur rounds the square cell steps into a smooth curve; the iso value below one
    // half keeps that curve just outside the cells (it never cuts through a cell). False if the contour is inconsistent
    // (the caller falls back to the per-cell edges).
    private bool TraceLoops(int r, List<List<Vector2>> loops)
    {
        float h = cellSize * 0.25f;
        float e = cellSize * 0.5f * MaskScale;
        float sigma = cellSize * BlurSigma;
        int radius = Mathf.Max(1, Mathf.CeilToInt(3f * sigma / h));
        float margin = e + (radius + 2) * h;
        bool any = false;
        Vector2 min = Vector2.zero, max = Vector2.zero;
        for (int i = 0; i < cells.Count; i++)
        {
            if (region[i] != r) continue;
            Vector2 p = cells[i].pos;
            if (!any) { min = p; max = p; any = true; }
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
        if (!any)
        {
            return true; // empty region: nothing to outline
        }
        Vector2 origin = min - Vector2.one * margin;
        int w = Mathf.CeilToInt((max.x + margin - origin.x) / h) + 1;
        int ht = Mathf.CeilToInt((max.y + margin - origin.y) / h) + 1;
        float[] field = new float[w * ht];
        for (int i = 0; i < cells.Count; i++)
        {
            if (region[i] != r) continue;
            Vector2 p = cells[i].pos;
            int x0 = Mathf.Max(1, Mathf.CeilToInt((p.x - e - origin.x) / h));
            int x1 = Mathf.Min(w - 2, Mathf.FloorToInt((p.x + e - origin.x) / h));
            int y0 = Mathf.Max(1, Mathf.CeilToInt((p.y - e - origin.y) / h));
            int y1 = Mathf.Min(ht - 2, Mathf.FloorToInt((p.y + e - origin.y) / h));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    field[y * w + x] = 1f;
                }
            }
        }
        Blur(field, w, ht, sigma / h, radius);

        // Edge keys: horizontal sample edge (x,y)-(x+1,y) = 2*(y*w+x); vertical (x,y)-(x,y+1) = 2*(y*w+x)+1.
        Dictionary<int, int> next = new Dictionary<int, int>();
        for (int y = 0; y < ht - 1; y++)
        {
            for (int x = 0; x < w - 1; x++)
            {
                float v0 = field[y * w + x], v1 = field[y * w + x + 1], v2 = field[(y + 1) * w + x + 1], v3 = field[(y + 1) * w + x];
                int code = (v0 >= MaskIso ? 1 : 0) | (v1 >= MaskIso ? 2 : 0) | (v2 >= MaskIso ? 4 : 0) | (v3 >= MaskIso ? 8 : 0);
                if (code == 0 || code == 15) continue;
                int b = 2 * (y * w + x), t = 2 * ((y + 1) * w + x), l = 2 * (y * w + x) + 1, rr = 2 * (y * w + x + 1) + 1;
                bool joined = (v0 + v1 + v2 + v3) * 0.25f >= MaskIso; // saddles: connect the inside corners if the centre is inside
                bool ok = true;
                switch (code)
                {
                    case 1: case 14: ok = AddSegment(next, b, EdgeB, l, EdgeL, code); break;
                    case 2: case 13: ok = AddSegment(next, b, EdgeB, rr, EdgeR, code); break;
                    case 3: case 12: ok = AddSegment(next, l, EdgeL, rr, EdgeR, code); break;
                    case 4: case 11: ok = AddSegment(next, rr, EdgeR, t, EdgeT, code); break;
                    case 6: case 9: ok = AddSegment(next, b, EdgeB, t, EdgeT, code); break;
                    case 7: case 8: ok = AddSegment(next, l, EdgeL, t, EdgeT, code); break;
                    case 5:
                        ok = joined
                            ? AddSegment(next, b, EdgeB, rr, EdgeR, code) && AddSegment(next, l, EdgeL, t, EdgeT, code)
                            : AddSegment(next, b, EdgeB, l, EdgeL, code) && AddSegment(next, rr, EdgeR, t, EdgeT, code);
                        break;
                    case 10:
                        ok = joined
                            ? AddSegment(next, b, EdgeB, l, EdgeL, code) && AddSegment(next, rr, EdgeR, t, EdgeT, code)
                            : AddSegment(next, b, EdgeB, rr, EdgeR, code) && AddSegment(next, l, EdgeL, t, EdgeT, code);
                        break;
                }
                if (!ok) return false;
            }
        }

        HashSet<int> visited = new HashSet<int>();
        foreach (KeyValuePair<int, int> start in next)
        {
            if (visited.Contains(start.Key)) continue;
            List<Vector2> pts = new List<Vector2>();
            int key = start.Key;
            int guard = next.Count + 1;
            do
            {
                if (!visited.Add(key) || --guard < 0) return false;
                pts.Add(origin + IsoPoint(field, key, w) * h);
                if (!next.TryGetValue(key, out key)) return false;
            } while (key != start.Key);
            if (pts.Count >= 3) loops.Add(pts);
        }
        return true;
    }

    // Grid position (in samples) where the iso-line crosses the sample edge with this key (linear interpolation).
    private static Vector2 IsoPoint(float[] field, int key, int w)
    {
        int cell = key >> 1;
        int x = cell % w, y = cell / w;
        bool horizontal = (key & 1) == 0;
        float a = field[cell];
        float b = horizontal ? field[cell + 1] : field[cell + w];
        float f = Mathf.Abs(b - a) > 1e-6f ? Mathf.Clamp01((MaskIso - a) / (b - a)) : 0.5f;
        return horizontal ? new Vector2(x + f, y) : new Vector2(x, y + f);
    }

    // Separable Gaussian blur (sigma in samples) of the mask, in place.
    private static void Blur(float[] field, int w, int h, float sigma, int radius)
    {
        float[] kernel = new float[radius * 2 + 1];
        float sum = 0f;
        for (int k = -radius; k <= radius; k++)
        {
            float v = Mathf.Exp(-(k * k) / (2f * sigma * sigma));
            kernel[k + radius] = v;
            sum += v;
        }
        for (int k = 0; k < kernel.Length; k++) kernel[k] /= sum;

        float[] tmp = new float[field.Length];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float acc = 0f;
                for (int k = -radius; k <= radius; k++)
                {
                    int xx = x + k;
                    if (xx >= 0 && xx < w) acc += field[y * w + xx] * kernel[k + radius];
                }
                tmp[y * w + x] = acc;
            }
        }
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float acc = 0f;
                for (int k = -radius; k <= radius; k++)
                {
                    int yy = y + k;
                    if (yy >= 0 && yy < h) acc += tmp[yy * w + x] * kernel[k + radius];
                }
                field[y * w + x] = acc;
            }
        }
    }

    // Midpoints of a marching-squares square's sides (local 0..1; corners 0..3 = lower-left, lower-right, upper-right,
    // upper-left, matching the case bits).
    private static readonly Vector2 EdgeB = new Vector2(0.5f, 0f);
    private static readonly Vector2 EdgeR = new Vector2(1f, 0.5f);
    private static readonly Vector2 EdgeT = new Vector2(0.5f, 1f);
    private static readonly Vector2 EdgeL = new Vector2(0f, 0.5f);
    private static readonly Vector2[] SquareCorners = { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };

    // One marching-squares segment, oriented so the corner nearest to it on its left is an inside corner (region on the
    // left of every contour). Each sample edge starts at most one segment in a consistent contour.
    private static bool AddSegment(Dictionary<int, int> next, int e1, Vector2 p1, int e2, Vector2 p2, int code)
    {
        Vector2 d = p2 - p1;
        Vector2 n = new Vector2(-d.y, d.x);
        Vector2 mid = (p1 + p2) * 0.5f;
        int nearest = -1;
        float best = float.MaxValue;
        for (int c = 0; c < 4; c++)
        {
            if (Vector2.Dot(SquareCorners[c] - p1, n) <= 0f) continue;
            float dist = (SquareCorners[c] - mid).sqrMagnitude;
            if (dist < best)
            {
                best = dist;
                nearest = c;
            }
        }
        bool leftInside = nearest >= 0 && (code & (1 << nearest)) != 0;
        int from = leftInside ? e1 : e2;
        int to = leftInside ? e2 : e1;
        if (next.ContainsKey(from)) return false;
        next[from] = to;
        return true;
    }

    // Evenly spaced points along a closed polyline, about `spacing` apart (at least 4).
    private static List<Vector2> Resample(List<Vector2> pts, float spacing)
    {
        int n = pts.Count;
        float total = 0f;
        for (int i = 0; i < n; i++)
        {
            total += Vector2.Distance(pts[i], pts[(i + 1) % n]);
        }
        int count = Mathf.Max(4, Mathf.RoundToInt(total / Mathf.Max(spacing, 1e-4f)));
        float step = total / count;
        List<Vector2> result = new List<Vector2>(count);
        int seg = 0;
        float segStart = 0f;
        float segLen = Vector2.Distance(pts[0], pts[1 % n]);
        for (int k = 0; k < count; k++)
        {
            float s = k * step;
            while (segStart + segLen < s && seg < n - 1)
            {
                segStart += segLen;
                seg++;
                segLen = Vector2.Distance(pts[seg], pts[(seg + 1) % n]);
            }
            float t = segLen > 1e-6f ? (s - segStart) / segLen : 0f;
            result.Add(Vector2.Lerp(pts[seg], pts[(seg + 1) % n], Mathf.Clamp01(t)));
        }
        return result;
    }

    // The iso-line is already smooth at cell scale; evenly resampled, a light Taubin pass (lambda / mu, no shrinking)
    // removes the sample-grid kinks and one Chaikin pass rounds what is left.
    private List<Vector2> Smooth(List<Vector2> raw)
    {
        List<Vector2> pts = Resample(raw, cellSize * ResampleSpacing);
        int n = pts.Count;
        Vector2[] a = pts.ToArray();
        if (n >= 4)
        {
            Vector2[] tmp = new Vector2[n];
            for (int pass = 0; pass < TaubinPasses * 2; pass++)
            {
                float k = pass % 2 == 0 ? 0.5f : -0.53f;
                for (int i = 0; i < n; i++)
                {
                    Vector2 avg = (a[(i + n - 1) % n] + a[(i + 1) % n]) * 0.5f;
                    tmp[i] = a[i] + k * (avg - a[i]);
                }
                Vector2[] swap = a;
                a = tmp;
                tmp = swap;
            }
        }
        List<Vector2> result = new List<Vector2>(a);
        for (int it = 0; it < ChaikinPasses; it++)
        {
            int m = result.Count;
            List<Vector2> next = new List<Vector2>(m * 2);
            for (int i = 0; i < m; i++)
            {
                Vector2 p = result[i];
                Vector2 q = result[(i + 1) % m];
                next.Add(Vector2.Lerp(p, q, 0.25f));
                next.Add(Vector2.Lerp(p, q, 0.75f));
            }
            result = next;
        }
        return result;
    }

    private void AddSmoothLoop(RegionOutline o, List<Vector2> loop)
    {
        List<Vector2> smooth = Smooth(loop);
        Vector2 min = smooth[0], max = smooth[0];
        foreach (Vector2 p in smooth)
        {
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
        // Thin the strokes on very small pieces (a single cell), so a wide halo cannot fold over itself.
        float extent = Mathf.Min(max.x - min.x, max.y - min.y);
        float strokeWidth = Mathf.Min(style.outlineWidth * StrokeFactor, extent * 0.35f);
        float haloWidth = Mathf.Min(style.outlineWidth * HaloFactor, extent * 0.8f);

        if (o.strokesUsed >= o.strokes.Count)
        {
            o.halos.Add(CreateLine("OutlineHalo", UiGeneratedSprites.LineHaloMaterial(baseMaterial)));
            o.strokes.Add(CreateLine("Outline", UiGeneratedSprites.LineCoreMaterial(baseMaterial)));
        }
        SetLoop(o.halos[o.strokesUsed], smooth, minFrontZ - 0.4f, haloWidth);
        SetLoop(o.strokes[o.strokesUsed], smooth, minFrontZ - 0.5f, strokeWidth);
        o.strokesUsed++;
    }

    private static void SetLoop(LineRenderer lr, List<Vector2> pts, float z, float width)
    {
        Vector3[] positions = new Vector3[pts.Count];
        for (int i = 0; i < pts.Count; i++)
        {
            positions[i] = new Vector3(pts[i].x, pts[i].y, z);
        }
        lr.positionCount = positions.Length;
        lr.SetPositions(positions);
        lr.widthMultiplier = width;
    }

    private LineRenderer CreateLine(string name, Material material)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(holder, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; // the holder sits at the world origin with unit world scale
        lr.loop = true;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.View;
        lr.numCornerVertices = 0;
        lr.numCapVertices = 0;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.generateLightingData = false;
        lr.sharedMaterial = material;
        lr.sortingLayerID = sortingLayerId;
        lr.sortingOrder = sortingOrder;
        lr.enabled = false;
        return lr;
    }

    // Fallback outline (as before the polish): the side of each region cell whose neighbour is missing or belongs to
    // another region, as a coloured stroke over a dark under-stroke.
    private void AddCellEdges(RegionOutline o, int r)
    {
        float w = style.outlineWidth;
        for (int i = 0; i < cells.Count; i++)
        {
            if (region[i] != r) continue;
            Cell c = cells[i];
            for (int d = 0; d < 4; d++)
            {
                if (InRegion(neighbours[i, d], r)) continue;
                Vector2 mid = (Vector2)c.pos + Dirs[d] * (c.size * 0.5f);
                bool horizontalEdge = d == 1 || d == 3;
                float len = c.size + w;
                if (o.edgesUsed >= o.edgeStrokes.Count)
                {
                    o.edgeHalos.Add(CreateSprite("EdgeHalo"));
                    o.edgeStrokes.Add(CreateSprite("Edge"));
                }
                PlaceEdge(o.edgeHalos[o.edgesUsed], mid, horizontalEdge, len, w * 2.4f, c.frontZ - 0.4f);
                PlaceEdge(o.edgeStrokes[o.edgesUsed], mid, horizontalEdge, len, w, c.frontZ - 0.5f);
                o.edgesUsed++;
            }
        }
    }

    private static void PlaceEdge(SpriteRenderer e, Vector2 mid, bool horizontal, float length, float width, float z)
    {
        e.transform.localPosition = new Vector3(mid.x, mid.y, z);
        e.transform.localScale = horizontal ? new Vector3(length, width, 1f) : new Vector3(width, length, 1f);
    }

    private SpriteRenderer CreateSprite(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(holder, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.enabled = false;
        return sr;
    }
}
