using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Grounding props on the walk line by what is actually painted: each sprite's lowest opaque row
// (so transparent padding under the art never floats it) and the width of its contact (the opaque
// columns of its lowest rows), sampled against the real terrain across that width. A prop whose
// contact spans uneven ground sits on its lowest point, slightly bedded into the higher side; one
// wider than 1.2 u on ground that differs by more than 0.35 u gets a small stone footing under it,
// level with its highest contact. Every placement is recorded for the audit.
public static partial class MraWorldBuilder
{
    public sealed class GroundRecord { public string scene, prop; public float moved, spread; public bool footing; }
    public static readonly List<GroundRecord> GroundAudit = new List<GroundRecord>();
    static string groundingScene = "";

    // The painted foot of a sprite, in its own pixels from the rect's bottom-left: the lowest opaque
    // row, and the opaque columns across its lowest rows (its contact with the ground).
    static readonly Dictionary<Sprite, Vector3> footCache = new Dictionary<Sprite, Vector3>();
    static Vector3 SpriteFoot(Sprite s)
    {
        if (footCache.TryGetValue(s, out var cached)) return cached;
        var tex = Readable(s);
        var result = new Vector3(0f, 0f, s.rect.width);
        if (tex != null)
        {
            // The decoded source may be larger than the imported texture (a max-size import).
            float scale = (float)tex.width / s.texture.width;
            int x0 = Mathf.RoundToInt(s.rect.xMin * scale), y0 = Mathf.RoundToInt(s.rect.yMin * scale);
            int w = Mathf.RoundToInt(s.rect.width * scale), h = Mathf.RoundToInt(s.rect.height * scale);
            int bottom = -1;
            for (int y = 0; y < h && bottom < 0; y++)
                for (int x = 0; x < w; x += 2) if (tex.GetPixel(x0 + x, y0 + y).a > .5f) { bottom = y; break; }
            if (bottom >= 0)
            {
                int band = Mathf.Max(3, Mathf.RoundToInt(h * .04f)), c0 = w, c1 = -1;
                for (int y = bottom; y < Mathf.Min(h, bottom + band); y++)
                    for (int x = 0; x < w; x++)
                        if (tex.GetPixel(x0 + x, y0 + y).a > .5f) { c0 = Mathf.Min(c0, x); c1 = Mathf.Max(c1, x); }
                result = new Vector3(bottom / scale, c0 / scale, (c1 + 1) / scale);
            }
        }
        return footCache[s] = result;
    }

    // The world-space foot of a renderer: (bottom y, contact x from, contact x to).
    static Vector3 FootOf(SpriteRenderer r)
    {
        var s = r.sprite; var foot = SpriteFoot(s);
        Vector3 scale = r.transform.lossyScale;
        float ppu = s.pixelsPerUnit;
        float bottom = r.transform.position.y + (foot.x - s.pivot.y) / ppu * scale.y;
        float a = (foot.y - s.pivot.x) / ppu * scale.x, b = (foot.z - s.pivot.x) / ppu * scale.x;
        if (r.flipX) { float t = -a; a = -b; b = t; }
        float x = r.transform.position.x;
        return new Vector3(bottom, x + Mathf.Min(a, b), x + Mathf.Max(a, b));
    }

    // Seats `prop` (moving it, children and all) so the lowest painted pixels of `feet` touch the
    // terrain, `sink` u bedded in. Returns how far it moved.
    public static float GroundProp(Transform prop, List<Piece> pieces, IEnumerable<SpriteRenderer> feet, float sink = .06f, Family f = null)
    {
        var rs = feet.Where(r => r != null && r.sprite != null).ToList();
        if (rs.Count == 0) return 0f;
        var foots = rs.Select(FootOf).ToList();
        float bottom = foots.Min(v => v.x), x0 = foots.Min(v => v.y), x1 = foots.Max(v => v.z);
        float inset = Mathf.Min(.15f, (x1 - x0) * .1f);
        var samples = Enumerable.Range(0, 5).Select(i => Ground(pieces, Mathf.Lerp(x0 + inset, x1 - inset, i / 4f))).Where(g => !float.IsNaN(g)).ToList();
        if (samples.Count == 0) return 0f;
        float low = samples.Min(), high = samples.Max(), target = low;
        bool footing = false;
        if (high - low > .35f && x1 - x0 > 1.2f && f != null)
        {
            // A level stone footing under a wide prop on uneven ground, its top at the highest contact.
            var foot = Terrain(prop.parent, "Footing for " + prop.name, new List<Vector2> { new Vector2(x0 - .3f, high), new Vector2(x1 + .3f, high) }, low - 1.2f, f, false, true, true, null, 1);
            foot.enabled = false; foot.riserFaces = true; foot.enabled = true;
            target = high; footing = true;
        }
        float moved = target - sink - bottom;
        prop.position += new Vector3(0f, moved, 0f);
        GroundAudit.Add(new GroundRecord { scene = groundingScene, prop = prop.name, moved = moved, spread = high - low, footing = footing });
        return moved;
    }

    public static float GroundProp(Transform prop, List<Piece> pieces, float sink = .06f, Family f = null) =>
        GroundProp(prop, pieces, prop.GetComponentsInChildren<SpriteRenderer>(true).Where(IsFoot), sink, f);

    // Hovering parts (a relic over its pedestal, glows, lamps' halos) don't count as a foot.
    static bool IsFoot(SpriteRenderer r)
    {
        string n = r.name.ToLowerInvariant();
        return !(n.Contains("relic") || n.Contains("glow") || n.Contains("hanging") || n.Contains("halo"));
    }

    static void WriteGroundAudit()
    {
        Directory.CreateDirectory("Tools/MountainReliefAtlas/QA");
        var rows = GroundAudit.Select(g => $"  {{\"scene\": \"{g.scene}\", \"prop\": \"{g.prop.Replace("\"", "'")}\", \"moved\": {g.moved:0.000}, \"ground_spread\": {g.spread:0.000}, \"footing\": {g.footing.ToString().ToLower()}}}");
        File.WriteAllText("Tools/MountainReliefAtlas/QA/grounding_audit.json", "[\n" + string.Join(",\n", rows) + "\n]\n");
    }
}
