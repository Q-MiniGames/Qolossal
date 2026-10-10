using System.Collections.Generic;
using System.Linq;
using Mra;
using UnityEditor;
using UnityEngine;

// The four optional floating-platform levels of the reclining route (drift levels, P1-P4; data from
// Tools/MountainReliefAtlas/reclining.py): drift stones on the warm air beside the body, each its own
// open-sky scene entered from a launch point on the main route and left by its west end.
// Placeholder art until the drift stone kit is painted: the region kit's accepted floating and moving
// platforms, the accepted crumbling slab, seed pods and thread anchors, and the warm-air current.
// A fall into the drift returns Qori to the last safe stone he stood on (MraDriftRest).
public static partial class MraWorldBuilder
{
    const float DriftDepth = .5f;

    public static void BuildDrift(Chamber c)
    {
        var region = world.RegionById(c.region);
        var f = FamilyOf(c.region);
        groundingScene = c.scene;
        var scene = Begin(ScenePath(c.scene), out Transform root);
        var camera = Scaffold(scene, c.playerSpawn, c.name, "mra:" + c.id, c.region, c.id, f.skyColour);
        var bounds = new GameObject("Camera Bounds").AddComponent<CameraBounds>();
        bounds.transform.SetParent(root, false);
        bounds.room = new Rect(-1f, -3f, c.size.x + 2f, c.size.y + 6f); bounds.top = true;
        Arrival(root, "entry", c.playerSpawn, 1f);
        Music(root, RegionMusic[c.region]);
        DriftSky(root, camera, c, f);

        var geo = Group(root, "Drift stones");
        string kit = f.id == "Qvale" ? "A5" : f.id;
        var stones = new[] { "S", "M", "L" }.Select(s => ArtOrNull($"Terrain/{kit}_Platform_Floating_{s}")).Where(s => s != null).ToArray();
        for (int i = 0; i < c.platforms.Length; i++)
        {
            var p = c.platforms[i];
            var slab = Slab(geo, (i == 0 ? "Start stone" : i == c.platforms.Length - 1 ? "Last stone" : "Drift stone " + i), p.x, p.y, p.z);
            Sprite art = stones.Length == 0 ? null : stones[p.z < 3.2f ? 0 : p.z < 5f ? Mathf.Min(1, stones.Length - 1) : stones.Length - 1];
            Dress(slab.transform, art, p.x + p.z * .5f, p.y, p.z, -14);
        }
        var movingArt = ArtOrNull($"Hazards/Platform_Moving_{kit}") ?? ArtOrNull("Hazards/Platform_Moving_A0");
        var current = ArtOrNull("Mechanics/FX_Breath_Current");
        foreach (var (m, i) in (c.movers ?? new Vector4[0]).Select((m, i) => (m, i)))
        {
            var slab = Slab(geo, $"Drifting stone {i} ({(Mathf.Abs(m.w) > 1e-3f ? "rises" : "crosses")})", m.x, m.y, 3f);
            Dress(slab.transform, movingArt, m.x + 1.5f, m.y, 3f, -13);
            var body = slab.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            var mover = slab.AddComponent<MovingPlatform>();
            mover.offset = new Vector2(m.z, m.w); mover.solid = slab.GetComponent<Collider2D>();
            // The swell's rhythm: small bobs are slow; lifts and crossings take their time.
            mover.travelSeconds = Mathf.Abs(m.w) > 0f && Mathf.Abs(m.w) < 2f ? 4f : 3.5f; mover.pauseSeconds = .8f;
            // Warm air under a rising stone (the gust that carries it).
            if (current != null && Mathf.Abs(m.w) >= 2f)
            {
                var air = Image(geo, $"Warm air (stone {i})", current, new Vector2(m.x + 1.5f, m.y + m.w * .5f), -40);
                air.transform.localScale = new Vector3(.6f, (m.w + 3f) / current.bounds.size.y, 1f);
                air.color = new Color(1f, 1f, 1f, .35f);
            }
        }
        var crumbleArt = ArtOrNull("Hazards/Platform_Crumble");
        var crumblePieces = Slices("Hazards/Platform_Crumble_Pieces");
        foreach (var (p, i) in (c.crumbles ?? new Vector4[0]).Select((p, i) => (p, i)))
        {
            var slab = Slab(geo, "Crumbling stone " + i, p.x, p.y, p.z);
            var cp = slab.AddComponent<CrumblePlatform>();
            cp.image = Dress(slab.transform, crumbleArt, p.x + p.z * .5f, p.y, p.z, -13);
            cp.pieces = crumblePieces; cp.solid = slab.GetComponent<Collider2D>();
        }

        // The west end: an invisible wall, and the way back to the main route just inside it.
        Wall(root, "West end", new Vector2(-.5f, c.size.y * .5f), new Vector2(1f, c.size.y + 40f));
        Wall(root, "East end", new Vector2(c.size.x + .5f, c.size.y * .5f), new Vector2(1f, c.size.y + 40f));
        var exit = new GameObject("Return to " + region.name);
        exit.transform.SetParent(root, false); exit.transform.position = new Vector3(.2f, c.size.y * .5f, 0f);
        Trigger(exit, new Vector2(1.2f, c.size.y + 4f), Vector2.zero);
        var ret = exit.AddComponent<MraChamberReturn>(); ret.parentScene = region.scene; ret.returnId = c.returnId; ret.label = region.name;

        // A fall: back to the last safe stone.
        var fall = new GameObject("The drift below (fall returns to the last safe stone)");
        fall.transform.SetParent(root, false); fall.transform.position = new Vector3(c.size.x * .5f, -5f, 0f);
        fall.AddComponent<BoxCollider2D>().size = new Vector2(c.size.x + 20f, 2f);
        fall.AddComponent<Hazard>();
        foreach (var (r, i) in (c.rests ?? new Vector2[0]).Select((r, i) => (r, i)))
        {
            var rest = new GameObject("Safe stone " + i);
            rest.transform.SetParent(root, false); rest.transform.position = r;
            rest.AddComponent<BoxCollider2D>().size = new Vector2(2.4f, 2f);
            rest.AddComponent<MraDriftRest>();
        }

        var mech = Group(root, "Trial");
        foreach (var a in c.anchors) Anchor(mech, a, f, "Thread anchor");
        foreach (var p in c.pods) Pod(mech, p);
        Reward(mech, c, f);
        if (!string.IsNullOrEmpty(c.objective)) Note(root, c.objective, new Vector2(c.playerSpawn.x + 2f, c.playerSpawn.y));
        Save(scene, ScenePath(c.scene));
    }

    // A stone's solid top: [left, left + width] at `top`, DriftDepth deep, never clung to.
    static GameObject Slab(Transform parent, string name, float left, float top, float width)
    {
        var obj = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        obj.transform.SetParent(parent, false); obj.transform.position = new Vector3(left + width * .5f, top - DriftDepth * .5f, 0f);
        obj.AddComponent<BoxCollider2D>().size = new Vector2(width, DriftDepth);
        obj.AddComponent<WallSurface>().allowsWallCling = false;
        return obj;
    }

    // The stone's art, scaled to its width, its painted top (the median skyline) on the solid top.
    static SpriteRenderer Dress(Transform parent, Sprite art, float centreX, float top, float width, int order)
    {
        if (art == null) return null;
        float scale = width * 1.08f / art.bounds.size.x;
        var r = Image(parent, "Art", art, new Vector2(centreX, top), order, scale);
        float paintedTop = art.bounds.min.y + Skyline(art) * art.bounds.size.y;
        r.transform.position = new Vector3(centreX, top - paintedTop * scale + .06f, 0f);
        return r;
    }

    static void DriftSky(Transform root, Camera camera, Chamber c, Family f)
    {
        var bg = Group(root, "Background");
        var sky = ArtOrNull(f.sky);
        if (sky != null)
        {
            var layer = new GameObject("Sky - " + System.IO.Path.GetFileName(f.sky)).AddComponent<ParallaxLayer>();
            layer.transform.SetParent(bg, false);
            layer.sprite = sky; layer.height = 24f; layer.repeat = true; layer.follow = new Vector2(1f, 1f);
            layer.baseY = -12f; layer.tint = Color.white; layer.extendBelow = 0f; layer.belowColor = f.skyColour;
            layer.sortingOrder = -110; layer.targetCamera = camera; layer.composedForSize = 5f; layer.belowBlend = 1.2f;
        }
        // The far valley only (rule 2: backgrounds show sky, weather and the far valley).
        if (DepthSets.TryGetValue(c.region, out var set))
            DepthLayer(bg, camera, new Vector2(0f, c.size.y), $"Far - {set.far} (depth 25)", Art($"Backgrounds/{set.far}"), null, 10f, FarDepth, -6.5f, -105, Color.white, 14f, true, ClimbSink(FarDepth));
    }

    // A launch point on the main route: warm air rising from a vent in the ground (placeholder art).
    static void DriftLaunch(GameObject door, float x, float y, List<Piece> pieces)
    {
        var vent = Image(door.transform, "Launch (warm-air vent, placeholder)", Art("Mechanics/Breath_Vent_Exhale"), new Vector2(x, y), -30, .85f);
        var current = ArtOrNull("Mechanics/FX_Breath_Current");
        if (current != null)
        {
            var air = Image(door.transform, "Warm air rising", current, new Vector2(x, y + 4.4f), -31, .9f);
            air.color = new Color(1f, 1f, 1f, .45f);
        }
        GroundProp(door.transform, pieces, new[] { vent }, .12f);
    }

    /// <summary>Batch: builds the four drift levels and adds them to the build list (their launch points come with BuildRegionsBatch).</summary>
    public static void BuildDriftBatch()
    {
        Prepare();
        foreach (var c in world.chambers.Where(c => c.IsDrift)) { BuildDrift(c); Debug.Log("[MraWorldBuilder] built " + c.scene); }
        RegisterScenes();
        WriteIndex();
        AssetDatabase.SaveAssets();
    }
}
