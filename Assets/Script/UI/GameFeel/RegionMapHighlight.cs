using System.Collections.Generic;
using UnityEngine;

// Display only (UX clarity batch). Draws the link between the four region cards and the map:
// - while a card is hovered or keyboard-focused, the cells of every other region get a dark veil (spotlight);
// - regions the local player received get an amber outline, regions the other player received a muted veil.
// It only READS each map cell's current region status (1-4, written by Player / PlayerAITutorial after both cuts) and its
// position. It owns its own sprites under one holder beside the cells (not under a county, not tagged SquarePoint, no
// PointOfState), so cell lookups, county sums, cut classification, region values, RPCs and saves are unaffected.
// Ownership comes from the existing RegionCard state, which follows the Choose / GetValues notifications: nothing is
// shown before the card itself shows it.
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

    private static readonly Vector2[] Dirs = { Vector2.right, Vector2.up, Vector2.left, Vector2.down };

    private readonly Transform mapRoot;
    private readonly Style style;
    private readonly List<Cell> cells = new List<Cell>();
    private readonly List<SpriteRenderer> edgePool = new List<SpriteRenderer>();
    private int[,] neighbours;
    private SpriteRenderer[] veils;
    private Transform holder;
    private Sprite squareSprite;
    private int edgesUsed;

    public RegionMapHighlight(Transform mapRoot, Style style)
    {
        this.mapRoot = mapRoot;
        this.style = style;
    }

    // active: the four regions exist (choose phase or later). spotlight: region index 0-3 shown in focus, or -1.
    // owners: per region index (same order as the cards: red, green, yellow, blue), from RegionCard.CurrentOwner.
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

        int[] region = new int[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            region[i] = ReadRegion(cells[i]);
        }

        for (int i = 0; i < cells.Count; i++)
        {
            int r = region[i];
            Color veil = new Color(0f, 0f, 0f, 0f);
            if (r >= 0)
            {
                if (spotlight >= 0)
                {
                    if (r != spotlight) veil = style.spotlightVeil;
                }
                else if (owners != null && r < owners.Length && owners[r] == RegionCard.Owner.Other)
                {
                    veil = style.otherVeil;
                }
            }
            SetVeil(i, veil);
        }

        edgesUsed = 0;
        for (int r = 0; r < 4; r++)
        {
            bool mine = owners != null && r < owners.Length && owners[r] == RegionCard.Owner.Mine;
            if (mine)
            {
                DrawOutline(r, region, style.mineOutline);
            }
            else if (r == spotlight)
            {
                DrawOutline(r, region, style.hoverOutline);
            }
        }
        for (int e = edgesUsed; e < edgePool.Count; e++)
        {
            if (edgePool[e].enabled) edgePool[e].enabled = false;
        }
    }

    public void HideAll()
    {
        if (veils != null)
        {
            foreach (SpriteRenderer v in veils)
            {
                if (v != null && v.enabled) v.enabled = false;
            }
        }
        foreach (SpriteRenderer e in edgePool)
        {
            if (e != null && e.enabled) e.enabled = false;
        }
        edgesUsed = 0;
    }

    private static int ReadRegion(Cell c)
    {
        int status = c.tutorial != null ? c.tutorial.getSpriteStatus() : (c.core != null ? c.core.getSpriteStatus() : 0);
        return status >= 1 && status <= 4 ? status - 1 : -1;
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
            if (squareSprite == null) squareSprite = sr.sprite;
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

    private void SetVeil(int i, Color color)
    {
        bool on = color.a > 0.001f;
        if (veils == null)
        {
            if (!on) return;
            veils = new SpriteRenderer[cells.Count];
        }
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

    // Outline of one region: the side of each of its cells whose neighbour is missing or belongs to another region.
    // A dark under-stroke keeps the coloured stroke visible on every region colour.
    private void DrawOutline(int r, int[] region, Color color)
    {
        float w = style.outlineWidth;
        for (int i = 0; i < cells.Count; i++)
        {
            if (region[i] != r) continue;
            Cell c = cells[i];
            for (int d = 0; d < 4; d++)
            {
                int n = neighbours[i, d];
                if (n >= 0 && region[n] == r) continue;
                Vector2 dir = Dirs[d];
                Vector2 mid = (Vector2)c.pos + dir * (c.size * 0.5f);
                bool horizontalEdge = d == 1 || d == 3;
                float len = c.size + w;
                PlaceEdge(mid, horizontalEdge, len, w * 2.4f, c.frontZ - 0.4f, style.outlineShadow);
                PlaceEdge(mid, horizontalEdge, len, w, c.frontZ - 0.5f, color);
            }
        }
    }

    private void PlaceEdge(Vector2 mid, bool horizontal, float length, float width, float z, Color color)
    {
        SpriteRenderer e;
        if (edgesUsed < edgePool.Count)
        {
            e = edgePool[edgesUsed];
        }
        else
        {
            e = CreateSprite("Edge");
            edgePool.Add(e);
        }
        edgesUsed++;
        e.transform.localPosition = new Vector3(mid.x, mid.y, z);
        e.transform.localScale = horizontal ? new Vector3(length, width, 1f) : new Vector3(width, length, 1f);
        e.color = color;
        if (!e.enabled) e.enabled = true;
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
