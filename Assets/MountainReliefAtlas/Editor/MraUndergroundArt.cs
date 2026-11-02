using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// The underground's accepted art (Batch 29/29b/29c, Reviews 82, 84, 87; Assets/Art/Codex/Underground at
// 216 px per unit), laid over the greybox where it exists: route A's three rooms get the Cistern kit
// (rock fill, walk edges, wall faces, ceiling band, ledges, backdrops, dressing, painted water, walkway,
// grates, valves and shrine), and the heart gets the Great Hollow pieces (the centre, the grille columns,
// the five sector accents, the lift cabins, hatches, guard plates and levers, the ladder). Collision is
// unchanged: every greybox collider stays, and only its temporary drawing is switched off where the
// painting takes over. Routes B to E, the skill caves and the heart's rock wait for their batches.
public static partial class MraWorldBuilder
{
    const string UgArtRoot = "Assets/Art/Codex/Underground/";
    static Sprite UgArt(string sub, string name) => AssetDatabase.LoadAssetAtPath<Sprite>(UgArtRoot + sub + "/" + name + ".png");
    static Sprite Cistern(string name) => UgArt("Cistern", "UG_Cistern_" + name);
    static Sprite Hollow(string name) => UgArt("Hollow", "UG_Hollow_" + name);
    static Sprite UgShared(string name) => UgArt("Shared", "UG_Shared_" + name);
    static bool HasCisternArt => Cistern("Fill") != null;
    static bool HasHollowArt => Hollow("Centre_Assembly") != null;

    // A sprite drawn over `rect` (tiled when it repeats), placed by its own pivot.
    static SpriteRenderer Paint(Transform parent, string name, Sprite sprite, Rect rect, int order, bool tiled = true)
    {
        var r = new GameObject(name).AddComponent<SpriteRenderer>();
        r.transform.SetParent(parent, false);
        r.sprite = sprite; r.sortingOrder = order;
        Vector2 pivot = sprite.pivot / sprite.rect.size;
        if (tiled) { r.drawMode = SpriteDrawMode.Tiled; r.size = rect.size; }
        else r.transform.localScale = new Vector3(rect.width / sprite.bounds.size.x, rect.height / sprite.bounds.size.y, 1f);
        r.transform.position = new Vector3(rect.x + rect.width * pivot.x, rect.y + rect.height * pivot.y, 0f);
        return r;
    }

    // A sprite at its own size, its pivot at `at` (scale optional).
    static SpriteRenderer Place(Transform parent, string name, Sprite sprite, Vector2 at, int order, float scale = 1f)
    {
        var r = new GameObject(name).AddComponent<SpriteRenderer>();
        r.transform.SetParent(parent, false); r.transform.position = at; r.transform.localScale = new Vector3(scale, scale, 1f);
        r.sprite = sprite; r.sortingOrder = order;
        return r;
    }

    // Where a wall face's painted contour sits, as the median over its rows of the outermost opaque
    // column, measured from its opaque side (units). Lets the contour, not the canvas edge, meet the
    // collider face.
    static readonly Dictionary<string, float> contourDepth = new Dictionary<string, float>();
    static float ContourDepth(Sprite face, bool contourOnRight)
    {
        string key = face.name + contourOnRight;
        if (contourDepth.TryGetValue(key, out float d)) return d;
        var tex = new Texture2D(2, 2); tex.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(face)));
        var px = tex.GetPixels32(); int w = tex.width, h = tex.height;
        var depths = new List<int>();
        for (int y = 0; y < h; y++)
        {
            int edge = 0;
            for (int i = 0; i < w; i++)
            {
                int x = contourOnRight ? i : w - 1 - i;
                if (px[y * w + x].a > 128) edge = i;
            }
            depths.Add(edge);
        }
        Object.DestroyImmediate(tex);
        depths.Sort();
        return contourDepth[key] = depths[depths.Count / 2] / face.pixelsPerUnit;
    }

    static void HideGreybox(Transform parent, System.Func<Transform, bool> which)
    {
        foreach (var r in parent.GetComponentsInChildren<SpriteRenderer>(true))
            if (which(r.transform)) r.enabled = false;
    }

    // ---------------------------------------------------------------- route A: the Cistern

    static void UgCisternArt(UgRoom r, Transform root, Transform geo, Transform mech)
    {
        if (!HasCisternArt || !r.id.StartsWith("A")) return;
        float w = r.size[0], h = r.size[1];
        var art = Group(root, "Art (Batch 29 Cistern kit)");
        Rect Solid(string id) => R(r.solids.First(s => s.id == id).rect);
        Rect floor = Solid("floor"), ceiling = Solid("ceiling"), left = Solid("left"), right = Solid("right");
        float floorTop = floor.yMax, ceilingBottom = ceiling.yMin;

        // Backdrops: the far back wall beyond the room, the near one inside it.
        Paint(art, "Back wall far (tiled)", Cistern("BG_Far"), new Rect(-4f, -4f, w + 8f, h + 8f), -61);
        Paint(art, "Back wall near (tiled)", Cistern("BG_Near"), Rect.MinMaxRect(left.xMax - .5f, floorTop - .5f, right.xMin + .5f, ceilingBottom + .5f), -59);

        // Rock: the shell (with the margin beyond it) in fill.
        Paint(art, "Rock fill (shell, floor)", Cistern("Fill"), Rect.MinMaxRect(-4f, -4f, w + 4f, floorTop), -7);
        Paint(art, "Rock fill (shell, ceiling)", Cistern("Fill"), Rect.MinMaxRect(-4f, ceilingBottom, w + 4f, h + 4f), -7);
        Paint(art, "Rock fill (shell, left)", Cistern("Fill"), Rect.MinMaxRect(-4f, floorTop, left.xMax, ceilingBottom), -7);
        Paint(art, "Rock fill (shell, right)", Cistern("Fill"), Rect.MinMaxRect(right.xMin, floorTop, w + 4f, ceilingBottom), -7);
        // The ceiling's underside band, hanging from its line.
        var band = Cistern("Ceiling");
        Paint(art, "Ceiling band (tiled)", band, new Rect(left.xMax - .3f, ceilingBottom - band.bounds.size.y + .25f, right.xMin - left.xMax + .6f, band.bounds.size.y), -5);
        // The side walls' faces: a room's left wall shows its block's right face (the *_Right piece).
        foreach (var (wall, onLeft) in new[] { (left, true), (right, false) })
        {
            var face = Cistern(onLeft ? "Wall_Right" : "Wall_Left");
            float depth = ContourDepth(face, onLeft), fw = face.bounds.size.x;
            float x0 = onLeft ? wall.xMax - depth : wall.xMin - fw + depth;   // the contour on the collider face
            Paint(art, (onLeft ? "Left" : "Right") + " wall face (tiled)", face, new Rect(x0, floorTop - 1.8f, fw, ceilingBottom - floorTop + 2.2f), -6);   // both ends past the camera's limits
            // The face's back edge is a straight cut against the fill: keep the camera inside it.
            var bounds = root.GetComponentInChildren<CameraBounds>();
            if (bounds != null)
            {
                Rect b = bounds.room;
                if (onLeft) b.xMin = Mathf.Max(b.xMin, x0 + .05f); else b.xMax = Mathf.Min(b.xMax, x0 + fw - .05f);
                bounds.room = b;
            }
        }
        // The floor strip's foot and the ceiling band's top are straight cuts against the fill too.
        var camBounds = root.GetComponentInChildren<CameraBounds>();
        if (camBounds != null)
        {
            Rect b = camBounds.room;
            b.yMin = Mathf.Max(b.yMin, floorTop - Cistern("WalkEdge").bounds.size.y + .05f);
            b.yMax = Mathf.Min(b.yMax, ceilingBottom + .2f);
            camBounds.room = b;
        }
        // The floor's walk edge (seen under water and between decks).
        Paint(art, "Floor walk edge (tiled)", Cistern("WalkEdge"), new Rect(left.xMax, floorTop - Cistern("WalkEdge").bounds.size.y, right.xMin - left.xMax, Cistern("WalkEdge").bounds.size.y), -6);   // behind the stone blocks standing on it

        // Decks: low ones read as stone blocks standing on the floor (the space under them is
        // sealed or under water), higher ones as painted ledges at their length.
        foreach (var p in r.platforms.Where(p => p.kind != "moving"))
        {
            var rect = R(p.rect);
            if (rect.yMin <= floorTop + 1.6f)
            {
                // One walk-edge strip, its height fitted from the deck's top down to the floor (0.8 to
                // 1.2 of its painted height here), so no straight foot of the strip shows against fill.
                var edge = Cistern("WalkEdge");
                float drop = rect.yMax - floorTop + .1f, stretch = drop / edge.bounds.size.y;
                var strip = Paint(art, "Stone block " + p.id, edge, new Rect(rect.xMin, rect.yMax - edge.bounds.size.y, rect.width, edge.bounds.size.y), -3);
                strip.transform.localScale = new Vector3(1f, stretch, 1f);
                strip.transform.position = new Vector3(rect.xMin, rect.yMax, 0f);
            }
            else
            {
                string len = rect.width <= 4f ? "3u" : rect.width <= 6f ? "5u" : "7u";
                Paint(art, "Ledge " + p.id + " (" + len + ")", Cistern("Ledge_" + len), new Rect(rect.xMin, rect.yMax - Cistern("Ledge_" + len).bounds.size.y, rect.width, Cistern("Ledge_" + len).bounds.size.y), -3, tiled: false);
            }
        }
        HideGreybox(geo, t => t.name.StartsWith("Rock ") || t.name.StartsWith("Deck ") || t.name.StartsWith("Ledge ") || t.name == "Walkable lip"
            || t.name.StartsWith("Outer rock") || t.name == "Back wall" || t.parent != null && (t.parent.name.StartsWith("Deck ") || t.parent.name.StartsWith("Ledge ")));

        // Dressing: a few props from the sheet, out of the way of play.
        var props = AssetDatabase.LoadAllAssetsAtPath(UgArtRoot + "Cistern/UG_Cistern_Dressing.png").OfType<Sprite>().ToDictionary(s => s.name.Replace("UG_Cistern_Dressing_", ""));
        void Prop(string id, float x, bool hanging, float scale = .8f)
        {
            if (!props.TryGetValue(id, out var s)) return;
            float half = s.bounds.extents.y * scale;
            Place(art, "Dressing " + id, s, new Vector2(x, hanging ? ceilingBottom - half + .15f : floorTop + half - .1f), hanging ? -4 : -45, scale);
        }
        Prop("HangingRoots", w * .3f, true); Prop("Drips", w * .72f, true, .7f); Prop("Calcite", w * .55f, true, .6f);

        // The mechanisms' paintings.
        foreach (var water in mech.GetComponentsInChildren<MraUgWater>(true))
        {
            water.art.sprite = Cistern("WaterBody"); water.art.color = Color.white; water.art.sortingOrder = 17;
            var surface = Paint(water.transform.parent, "Water surface (painted)", Cistern("WaterSurface"), water.high, 18);
            var lip = Paint(water.transform.parent, "Water lip (painted)", Cistern("WaterLip"), new Rect(water.high.xMin, water.high.yMax - .3f, water.high.width, .6f), 19);
            water.surface = surface; water.lip = lip;
        }
        foreach (var gate in mech.GetComponentsInChildren<MraGate>(true).Where(g => g.name.StartsWith("Gate ")))
        {
            var col = (BoxCollider2D)gate.solid;
            Vector2 c = gate.transform.position; Vector2 size = col.size;
            var closed = Paint(gate.transform, "Grate (closed, painted)", Cistern("ExitGrate_Closed"), new Rect(c.x - .5f, c.y - size.y * .5f, 1f, size.y), 6, tiled: false);
            var lifted = Paint(gate.transform, "Grate (lifted, painted)", Cistern("ExitGrate_Lifted"), new Rect(c.x - .5f, c.y - size.y * .5f + size.y - .6f, 1f, size.y), -8, tiled: false);   // drawn up into the rock, its foot still showing
            foreach (var bars in gate.plainArt) if (bars != null) bars.enabled = false;
            gate.plainArt = new[] { closed }; gate.openArt = new[] { lifted };
        }
        foreach (var valve in mech.GetComponentsInChildren<MraUgSwitch>(true).Where(s => s.name.StartsWith("Valve ")))
        {
            if (valve.wheel != null) valve.wheel.GetComponent<SpriteRenderer>().enabled = false;
            valve.wheel = null;
            var rest = Cistern("Valve_Rest");
            valve.image = Place(valve.transform, "Valve (painted)", rest, (Vector2)valve.transform.position + new Vector2(0f, rest.bounds.extents.y - .05f), -11);
            valve.doneSprite = Cistern("Valve_Turned");
        }
        foreach (var lift in mech.GetComponentsInChildren<MraUgRaiseLift>(true))
        {
            var box = lift.GetComponent<BoxCollider2D>();
            var raised = Cistern("Walkway_Raised");
            lift.art.sprite = raised; lift.art.drawMode = SpriteDrawMode.Simple; lift.art.color = Color.white;
            lift.art.transform.localScale = new Vector3(box.size.x / raised.bounds.size.x, 1f, 1f);
            lift.art.transform.localPosition = new Vector3(0f, box.size.y * .5f - raised.bounds.extents.y, 0f);   // its walk line on the collider's top
            lift.raisedSprite = raised; lift.submergedSprite = Cistern("Walkway_Submerged");
            foreach (var t in lift.GetComponentsInChildren<SpriteRenderer>(true)) if (t.name == "Walkable lip") t.enabled = false;
        }
        foreach (var cp in mech.GetComponentsInChildren<Checkpoint>(true))
        {
            var shrine = Cistern("Checkpoint_Rest");
            var so = new SerializedObject(cp); so.FindProperty("shrineArt").objectReferenceValue = shrine; so.ApplyModifiedPropertiesWithoutUndo();
            var sr = cp.GetComponent<SpriteRenderer>(); sr.sprite = shrine;
            cp.transform.position = new Vector3(cp.transform.position.x, floorTopUnder(cp.transform.position.x, r) - .03f, 0f);
        }
    }

    // The walk line under x in a room (the highest deck or floor top below the marker's own deck).
    static float floorTopUnder(float x, UgRoom r)
    {
        float best = R(r.solids.First(s => s.id == "floor").rect).yMax;
        foreach (var p in r.platforms.Where(p => p.kind != "moving"))
        {
            var rect = R(p.rect);
            if (x >= rect.xMin && x <= rect.xMax && rect.yMax > best && rect.yMax < 6f) best = rect.yMax;
        }
        return best;
    }

    // ---------------------------------------------------------------- the heart: the Great Hollow

    static void UgHollowArt(UgRoom r, Transform root, Transform geo, Transform mech)
    {
        if (!HasHollowArt || r.id != "HEART") return;
        var art = Group(root, "Art (Batch 29 Great Hollow)");
        // The centre: the painted set piece (its glow included) behind the E column's floor.
        var centre = Hollow("Centre_Assembly");
        Place(art, "Centre (painted)", centre, new Vector2(15f, 1.6f + centre.bounds.extents.y), -57);
        HideGreybox(mech, t => t.name.StartsWith("Glow at the centre"));
        // The ribs' grille columns, behind the rock: their painted bars show through the windows.
        var column = Hollow("Column_Assembly");
        foreach (float x in new[] { 11f, 19f })
            Place(art, "Grille column x " + x, column, new Vector2(x, 10f), -8);
        HideGreybox(geo, t => t.parent != null && t.parent.name.StartsWith("Grille (permanent)"));
        // The five sector accents, each in its sector, behind play.
        void Accent(string id, Vector2 centreAt, float scale) => Place(art, "Accent " + id, Hollow("Accent_" + id), centreAt, -56, scale);
        Accent("A", new Vector2(1f + 3.87f * .75f, 2f + 2.18f * .75f), .75f);   // the conduit against the west wall, its basin on the floor
        Accent("B", new Vector2(7.1f, 19f - 2.18f * .6f), .6f);                  // the bells hang from the ceiling
        Accent("C", new Vector2(24.2f, 2f + 2.18f * .7f), .7f);                  // roots on the floor
        Accent("D", new Vector2(23.4f, 12f + 2.18f * .6f), .6f);                 // wind-worn rock on the floor
        Accent("E", new Vector2(16.3f, 13f + 2.18f * .4f), .4f);                 // gold crystals on the centre's shelf
        // Lifts: cabins, hatches, guard plates, levers.
        foreach (var lift in mech.GetComponentsInChildren<MraUgHeartLift>(true).Where(l => l.hatch != null))
        {
            var cabinBox = lift.cabin.GetComponent<BoxCollider2D>();
            foreach (var t in lift.cabin.GetComponentsInChildren<SpriteRenderer>(true)) t.enabled = false;
            var cab = Hollow("LiftCabin");
            Place(lift.cabin.transform, "Cabin (painted)", cab, (Vector2)lift.cabin.transform.position + new Vector2(0f, cabinBox.size.y * .5f - cab.bounds.extents.y), -4, cabinBox.size.x / cab.bounds.size.x);   // its deck on the collider's top
            var hb = (BoxCollider2D)lift.hatch; Rect hr = new Rect((Vector2)hb.transform.position - hb.size * .5f, hb.size);
            foreach (var a in lift.hatchArt) if (a != null) a.enabled = false;
            var closed = Paint(hb.transform, "Hatch (closed, painted)", Hollow("Hatch_Closed"), hr, 6, tiled: false);
            var open = Paint(hb.transform.parent, "Hatch (open frame, painted)", Hollow("Hatch_Open"), hr, -9, tiled: false);
            lift.hatchArt = new[] { closed }; lift.hatchOpenArt = new[] { open };
            if (lift.guard != null)
            {
                var gb = (BoxCollider2D)lift.guard;
                if (lift.guardArt != null) lift.guardArt.enabled = false;
                lift.guardArt = Paint(gb.transform, "Guard plate (painted)", Hollow("GuardPlate"), new Rect((Vector2)gb.transform.position - gb.size * .5f, gb.size), -3, tiled: false);
            }
            foreach (var station in lift.GetComponentsInChildren<MraUgLiftStation>(true))
            {
                foreach (var t in station.GetComponentsInChildren<SpriteRenderer>(true)) t.enabled = false;
                var rest = Hollow("Lever_Rest");
                station.lever = Place(station.transform, "Lever (painted)", rest, (Vector2)station.transform.position + new Vector2(0f, rest.bounds.extents.y), -11);
                station.restSprite = rest; station.pulledSprite = Hollow("Lever_Pulled");
            }
        }
        // The rope ladder at the centre's foot.
        foreach (var p in mech.GetComponentsInChildren<MraUgPortal>(true).Where(p => p.name.StartsWith("Back up to the E doorway")))
        {
            foreach (var t in p.GetComponentsInChildren<SpriteRenderer>(true)) t.enabled = false;
            var ladder = Hollow("RopeLadder");
            Place(p.transform, "Rope ladder (painted)", ladder, (Vector2)p.transform.position + new Vector2(0f, ladder.bounds.size.y), -12);
        }
    }

    // ---------------------------------------------------------------- shared: the call lifts

    static void UgSharedArt(Transform mech)
    {
        var cab = UgShared("LiftCabin"); var rope = UgShared("LiftRope");
        if (cab == null) return;
        foreach (var lift in mech.GetComponentsInChildren<MraUgHeartLift>(true).Where(l => l.hatch == null))
        {
            var box = lift.cabin.GetComponent<BoxCollider2D>();
            foreach (var t in lift.cabin.GetComponentsInChildren<SpriteRenderer>(true)) t.enabled = false;
            Place(lift.cabin.transform, "Cabin (painted)", cab, (Vector2)lift.cabin.transform.position + new Vector2(0f, box.size.y * .5f - cab.bounds.extents.y), -4, box.size.x / cab.bounds.size.x);
            foreach (var t in lift.GetComponentsInChildren<SpriteRenderer>(true).Where(t => t.name.StartsWith("Lift shaft rope")))
            {
                t.enabled = false;
                float top = lift.upper.y + 2f, len = top - lift.lower.y;
                Paint(lift.transform, "Lift rope (painted, tiled)", rope, new Rect(lift.lower.x - .15f, lift.lower.y, .3f, len), -41);
            }
        }
    }
}
