using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mra;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the Mountain Relief Atlas prototype from Resources/MRA/mra_world.json (converted from the
// design package by Tools/MountainReliefAtlas/convert_spec.py): one scene per region with its 12
// beats, and one small scene per side chamber, in Assets/MountainReliefAtlas/Scenes. Deterministic
// and idempotent: each run replaces only the scene's "Generated (MRA builder)" root, so anything
// authored by hand beside it is kept; generated objects are named by their data ids.
//
// The route: the designed walk line (Resources/MRA/mra_route.json, written by
// Tools/MountainReliefAtlas/design_route.py): broad pads at the beats, terraces, ramps, jump walls and
// Climbing Moss climbs between them, combat shelves, from the region's left camera edge to its right,
// filled down to the camera floor with the region's accepted terrain art. The modules:
//   quake crossing (Cradle E08)  a root gate (open once knot:grip wakes) under a sealed, unclimbable
//                                lintel, a ravine with a catch floor and return steps, and the
//                                fallen crossing that appears after the quake;
//   thread lift (Causeway E07)   a 5 u unclimbable wall after the Living Thread shrine, with an
//                                anchor over its lip that a jump can reach and the thread pulls up;
//   sluice (Terraces E07)        a gate under a lintel, opened for good by the wheel before it;
//   swell (Ribwood E07)          cosmetic rock heave, static collision;
//   glide span (Heights)         an 18 u gorge with a lower landing and a catch floor with return
//                                steps. Moved to E10 (exit node +15 u): E06's 36 u run can't hold a
//                                gap wider than a running jump plus a Wind Leaf dash (see handoff);
//   reveal (Summit E09)          the one-time reveal on the ledge; terrain unchanged;
//   ending (Descent E08)         the careful ending at the final chamber.
// Encounters stand on the designer's combat shelves (about 11 u), away from beats, doors and modules.
// Menu: Qolossal > Mountain Relief Atlas > Build All Scenes
public static partial class MraWorldBuilder
{
    static World world;
    // Beats moved from their authored position to make room for a module (documented in the handoff).
    static readonly Dictionary<string, Vector2> Moved = new Dictionary<string, Vector2> { { "MR06_N11", new Vector2(440f, 270f) } };
    public static Vector2 Pos(Node n) => Moved.TryGetValue(n.id, out var p) ? p : n.position;
    public static Vector2 Spawn(Node n) => Pos(n) + (n.spawn - n.position);

    public const float GlideGap = 18f, GlideDrop = 3f, LiftHeight = 5f;

    [MenuItem("Qolossal/Mountain Relief Atlas/Build All Scenes")]
    public static void BuildAll()
    {
        Prepare();
        GroundAudit.Clear();
        foreach (var r in world.regions) BuildRegion(r);
        foreach (var c in world.chambers)
        {
            if (c.IsHouse) BuildHouse(c);
            else if (!c.InPlace) BuildChamber(c);
        }
        RegisterScenes();
        WriteIndex();
        WriteGroundAudit();
        AssetDatabase.SaveAssets();
        Debug.Log($"[MraWorldBuilder] Built {world.regions.Length} regions and {world.chambers.Count(c => !c.InPlace)} chambers in {SceneFolder}");
    }

    /// <summary>Batch: rebuilds only the regions named by -mraOnly (e.g. MR03); chambers and houses untouched.</summary>
    public static void BuildRegionsBatch()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-mraOnly");
        string only = i >= 0 && i + 1 < args.Length ? args[i + 1] : "";
        if (only == "") { Debug.LogError("[MraWorldBuilder] BuildRegionsBatch needs -mraOnly"); return; }
        Prepare();
        foreach (var r in world.regions.Where(r => only.Split(',').Contains(r.id))) { BuildRegion(r); Debug.Log("[MraWorldBuilder] rebuilt " + r.scene); }
        AssetDatabase.SaveAssets();
    }

    public static World Prepare()
    {
        AssetDatabase.ImportAsset(Folder + "Resources/MRA/mra_world.json", ImportAssetOptions.ForceUpdate);
        World.Forget();
        world = World.Load() ?? throw new FileNotFoundException("Run Tools/MountainReliefAtlas/convert_spec.py first.");
        AssetDatabase.ImportAsset(RouteDesign.FilePath, ImportAssetOptions.ForceUpdate);
        RouteDesign.Forget(); RouteDesign.Load();
        ImportReviewArt();
        BuildChartArt();
        return world;
    }

    public static string ScenePath(string scene) => SceneFolder + scene + ".unity";

    // ---------------------------------------------------------------- a region

    public static void BuildRegion(Region r)
    {
        var f = FamilyOf(r.id);
        var scene = Begin(ScenePath(r.scene), out Transform root);
        groundingScene = r.scene;
        var entry = r.NodeById(r.entry);
        var camera = Scaffold(scene, Spawn(entry), r.name, "mra:" + r.id, r.id, null, f.skyColour);
        new GameObject("Route Tracker").AddComponent<MraRouteTracker>().transform.SetParent(root, false);
        Music(root, RegionMusic[r.id]);
        var bounds = new GameObject("Camera Bounds").AddComponent<CameraBounds>();
        bounds.transform.SetParent(root, false);
        bounds.room = Rect.MinMaxRect(r.cameraMin.x, r.cameraMin.y, r.cameraMax.x, r.cameraMax.y); bounds.top = true;

        Background(root, camera, r, f);
        float bottom = r.cameraMin.y - 6f;
        var pieces = Route(r);
        var terrain = Group(root, "Terrain");
        for (int i = 0; i < pieces.Count; i++)
            Terrain(terrain, $"Route {r.id} piece {i:00}{(pieces[i].label != null ? " (" + pieces[i].label + ")" : "")}", pieces[i].points, bottom, f, false, pieces[i].clingable);

        var mech = Group(root, "Beats and mechanics");
        Modules(r, f, mech, pieces, bottom);
        Beats(r, f, mech, pieces);
        Travel(r, f, mech, pieces);
        if (r.id == "MR04") Town(r, f, Group(root, "Qvale"), pieces);
        else Doors(r, f, mech, pieces);
        Encounters(r, Group(root, "Encounters"), pieces);
        Decor(r, f, Group(root, "Decor"), pieces);
        RockDressing(r, f, Group(root, "Rock dressing"), pieces);
        CliffDressing(r, f, Group(root, "Cliff dressing"), pieces);
        if (r.id == "MR03") DepthExtras(r, camera, root, pieces);
        Save(scene, ScenePath(r.scene));
    }

    // ---------------------------------------------------------------- the route

    public sealed class Piece { public List<Vector2> points = new List<Vector2>(); public bool clingable = true; public string label; }

    // The designed walk line (mra_route.json, from Tools/MountainReliefAtlas/design_route.py):
    // pads at the beats, terraces, ramps and walls between them, combat shelves, and the modules'
    // fixed ground, split into pieces wherever a module needs a gap or an unclimbable wall.
    public static List<Piece> Route(Region r)
    {
        var design = RouteDesign.Of(r.id) ?? throw new System.InvalidOperationException("No designed route for " + r.id);
        return design.pieces.Select(p => new Piece
        {
            clingable = p.clingable, label = string.IsNullOrEmpty(p.label) ? null : p.label,
            points = p.points.Select(q => new Vector2(q.x, q.y)).ToList(),
        }).Where(p => p.points.Count >= 2).ToList();
    }

    // The ground height under x (the highest piece there), for placing things on the route.
    public static float Ground(List<Piece> pieces, float x)
    {
        float best = float.NaN;
        foreach (var p in pieces)
        {
            float h = HeightAt(p.points, x);
            if (!float.IsNaN(h) && (float.IsNaN(best) || h > best)) best = h;
        }
        return best;
    }

    // ---------------------------------------------------------------- modules

    static void Modules(Region r, Family f, Transform parent, List<Piece> pieces, float bottom)
    {
        foreach (var e in r.edges)
        {
            if (!e.module.Present) continue;
            var m = e.module;
            switch (m.template)
            {
                case "root-gate-with-fallen-crossing": QuakeCrossing(r, f, parent, e, pieces, bottom); break;
                case "flat-thread-gap": ThreadLift(r, f, parent, e, pieces); break;
                case "sluice-root-gate": Sluice(r, f, parent, e, pieces); break;
                case "cosmetic-rock-swell": Swell(r, f, parent, e, pieces); break;
                case "one-time-reveal": Reveal(r, parent, e); break;
                case "careful-ending": Ending(r, parent, e); break;
            }
        }
        if (r.id == "MR06") GlideSpan(r, f, parent, r.edges.First(x => x.id == "MR06_E10"), pieces, bottom);
    }

    static void QuakeCrossing(Region r, Family f, Transform parent, Edge e, List<Piece> pieces, float bottom)
    {
        var root = Group(parent, $"Module {e.id} quake crossing");
        float lip = e.module.position.x - e.module.bridgeWidth * .5f, far = lip + e.module.bridgeWidth;
        float yPad = Ground(pieces, lip - .5f), yFar = Ground(pieces, far + .5f);
        // The root gate on the pad, opened by the quake (knot:grip), never climbable.
        float gateX = lip - 1f, gateH = e.module.gateSize.y;
        var gateObj = new GameObject("Root Gate (knot:grip)") { layer = LayerMask.NameToLayer("Ground") };
        gateObj.transform.SetParent(root, false); gateObj.transform.position = new Vector3(gateX, yPad + gateH * .5f, 0f);
        var solid = gateObj.AddComponent<BoxCollider2D>(); solid.size = new Vector2(e.module.gateSize.x, gateH);
        gateObj.AddComponent<WallSurface>().allowsWallCling = false;
        var rg = RootGateArt(gateObj.transform, gateX, yPad, gateH);
        rg.solid = solid;
        var gate = gateObj.AddComponent<MraGate>(); gate.requires = e.requires; gate.solid = solid; gate.visual = rg;
        // The sealed overhang: an unclimbable rock cap over the gate. The gate (5.5 u, no cling or
        // ledge grab) is already more than twice a jump's height; the cap seals the top visually.
        Box(root, "Sealed overhang", Rect.MinMaxRect(gateX - 1.2f, yPad + gateH - .05f, gateX + .9f, yPad + gateH + 2.5f), f, false, false);
        // The ravine: a catch floor 4 u down with return steps up its west wall.
        var floor = Steps(new Vector2(lip, yPad - 1f), new Vector2(lip + 3.2f, yPad - 4f), 1f);
        floor.Add(new Vector2(far, yPad - 4f));
        Terrain(root, "Ravine floor (catch floor and return steps)", Clean(floor), bottom, f);
        // After the quake: the fallen crossing over the ravine.
        var after = new GameObject("After the quake (knot:grip)").AddComponent<MraVariant>();
        after.transform.SetParent(root, false); after.requires = new[] { "knot:grip" };
        var slab = Terrain(after.transform, "Fallen stone crossing", new List<Vector2> { new Vector2(lip - .3f, yPad), new Vector2(far + .3f, yFar) }, Mathf.Min(yPad, yFar) - 1.4f, f);
        var pillar = ArtOrNull("Terrain/Body/Pillar_2_Middle");
        if (pillar != null)
        {
            var art = Image(after.transform, "Fallen pillar (art)", pillar, new Vector2(lip - .6f, (yPad + yFar) * .5f - .9f), -34, .14f);
            art.transform.localScale = new Vector3((far - lip + 1.2f) / pillar.bounds.size.x, .14f, 1f);
            art.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(yFar - yPad, far - lip) * Mathf.Rad2Deg);
        }
        AreaRoomStyle.Freshen(after);
    }

    static RootGate RootGateArt(Transform parent, float x, float ground, float height)
    {
        var g = parent.gameObject.AddComponent<RootGate>();
        g.topClosed = Art("Props/Gate_Root_Closed_Top"); g.bottomClosed = Art("Props/Gate_Root_Closed_Bottom");
        g.topOpen = Art("Props/Gate_Root_Open_Top"); g.bottomOpen = Art("Props/Gate_Root_Open_Bottom");
        float s = 1.35f * height / 3f;   // the A0 rule: 1.35 for a 3 u gate
        g.top = Image(parent, "Top", g.topClosed, new Vector2(x, ground + height - (.768f - .093f) * s), -13, s);
        g.bottom = Image(parent, "Bottom", g.bottomClosed, new Vector2(x, ground + (1.41f - .768f) * s), -13, s);
        return g;
    }

    static void ThreadLift(Region r, Family f, Transform parent, Edge e, List<Piece> pieces)
    {
        var root = Group(parent, $"Module {e.id} thread lift");
        float lip = e.module.position.x + .5f, yU = Ground(pieces, lip + .5f);
        // On the near side of the lip: the pull is a straight hoist up the wall face (the thread
        // doesn't wrap, and its pull won't drag Qori into rock), ending above the ledge.
        Anchor(root, new Vector2(lip - .7f, yU + 1.4f), f, "Thread anchor (lift)");
    }

    public static GameObject Anchor(Transform parent, Vector2 at, Family f, string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ThreadAnchor01.prefab");
        var anchor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        anchor.name = name; anchor.transform.position = at; anchor.transform.localScale = Vector3.one;
        var art = anchor.transform.Find("AnchorVisual");
        if (art != null)
        {
            art.localPosition = Vector3.zero; art.localScale = Vector3.one;
            var sr = art.GetComponent<SpriteRenderer>(); sr.sprite = Art(f.ring); sr.sortingOrder = -12;
        }
        return anchor;
    }

    static void Sluice(Region r, Family f, Transform parent, Edge e, List<Piece> pieces)
    {
        var root = Group(parent, $"Module {e.id} sluice");
        float x = e.module.position.x + 1f, ground = Ground(pieces, x), h = e.module.gateSize.y;
        var gateObj = new GameObject("Sluice gate") { layer = LayerMask.NameToLayer("Ground") };
        gateObj.transform.SetParent(root, false); gateObj.transform.position = new Vector3(x, ground + h * .5f, 0f);
        var solid = gateObj.AddComponent<BoxCollider2D>(); solid.size = new Vector2(e.module.gateSize.x, h);
        gateObj.AddComponent<WallSurface>().allowsWallCling = false;
        var gate = gateObj.AddComponent<MraGate>(); gate.solid = solid; gate.latchFlag = "mra:" + e.id + ":sluice-open";
        Sprite closed = Art("Mechanics/Sluice_Gate_Closed"), open = Art("Mechanics/Sluice_Gate_Open");
        float s = h / 4f;
        gate.plainArt = new[] { Image(root, "Sluice (closed)", closed, new Vector2(x, ground + .5f * s), -13, s) };
        gate.openArt = new[] { Image(root, "Sluice (open)", open, new Vector2(x, ground + .5f * s), -13, s) };
        // The lintel over the 3 u sluice: rock to 6 u, so no jump clears the gate.
        Box(root, "Lintel over the sluice", Rect.MinMaxRect(x - .9f, ground + h - .05f, x + .9f, ground + 6f), f, false, false);
        var wheel = Lever(root, new Vector2(x - 2.6f, Ground(pieces, x - 2.6f)), "Turn the sluice wheel", gate);
        GroundProp(wheel.image.transform, pieces, new[] { wheel.image }, .05f);
    }

    public static MraLever Lever(Transform parent, Vector2 foot, string label, params MonoBehaviour[] targets)
    {
        var obj = new GameObject("Wheel: " + label);
        obj.transform.SetParent(parent, false); obj.transform.position = foot;
        var lever = obj.AddComponent<MraLever>(); lever.label = label;
        lever.image = Image(obj.transform, "Wheel", Art("Mechanics/Mill_Wheel"), foot + new Vector2(0f, .95f), -11, .34f);
        lever.gameObject.AddComponent<MraWheelSpin>().wheel = lever.image.transform;
        Trigger(obj, new Vector2(2.2f, 2.6f), new Vector2(0f, 1.3f));
        lever.targets.AddRange(targets);
        return lever;
    }

    static void Swell(Region r, Family f, Transform parent, Edge e, List<Piece> pieces)
    {
        var root = Group(parent, $"Module {e.id} swell (cosmetic)");
        var rocks = Slices(f.decor).Where(s => !s.name.Contains("hanging") && !s.name.Contains("stalactite") && !s.name.Contains("cable") && !s.name.Contains("strand") && !s.name.Contains("banner")).ToArray();
        for (int i = 0; i < 3; i++)
        {
            float x = e.module.position.x - 6f + i * 6f;
            var swell = new GameObject("Heaving rock " + i).AddComponent<MraSwell>();
            swell.transform.SetParent(root, false); swell.transform.position = new Vector3(x, Ground(pieces, x) - .1f, 0f);
            swell.amplitude = e.module.amplitude; swell.period = e.module.period + i * .7f;
            if (rocks.Length > 0) Image(swell.transform, "Rock", rocks[(i * 5) % rocks.Length], swell.transform.position, -22);
        }
    }

    static void GlideSpan(Region r, Family f, Transform parent, Edge e, List<Piece> pieces, float bottom)
    {
        var root = Group(parent, "Module MR06 glide span (moved to E10)");
        Vector2 a = Pos(r.NodeById(e.source));
        float launch = a.x + 4f, landing = launch + GlideGap;
        // A catch floor 8 u below the launch, with return steps up to it; the landing's wall is unclimbable.
        var floor = Steps(new Vector2(launch, a.y - 1.15f), new Vector2(launch + 6.3f, a.y - 8f), 1.15f);
        floor.Add(new Vector2(landing, a.y - 8f));
        Terrain(root, "Gorge floor (catch floor and return steps)", Clean(floor), bottom, f);
        var gateNote = new GameObject("Glidecap needed here (no gate: the gap is the gate)");
        gateNote.transform.SetParent(root, false); gateNote.transform.position = new Vector3(launch + GlideGap * .5f, a.y, 0f);
    }

    static void Reveal(Region r, Transform parent, Edge e)
    {
        var node = r.NodeById(e.target);
        var obj = new GameObject("Module " + e.id + " reveal");
        obj.transform.SetParent(parent, false); obj.transform.position = Pos(node);
        Trigger(obj, new Vector2(4f, 4f), new Vector2(0f, 2f));
        var reveal = obj.AddComponent<MraReveal>();
        reveal.pullCentre = new Vector2(180f, 118f); reveal.pullSize = 62f; reveal.pullSeconds = 6f;
        reveal.brow = ArtOrNull("Cinematics/Reveal_Face_01_Brow");
        reveal.finalAssembly = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "ReviewArt/" + FinalAssemblyFile);
    }

    static void Ending(Region r, Transform parent, Edge e)
    {
        var node = r.NodeById(e.target);
        var obj = new GameObject("Module " + e.id + " careful ending");
        obj.transform.SetParent(parent, false); obj.transform.position = Pos(node);
        Trigger(obj, new Vector2(4f, 4f), new Vector2(0f, 2f));
        var ending = obj.AddComponent<MraEnding>();
        ending.knot = string.IsNullOrEmpty(node.knot) ? "heart" : node.knot;
        ending.stills = new[] { ArtOrNull("Cinematics/Spoilers/Ending_Careful_01_HeadLift"), ArtOrNull("Cinematics/Spoilers/Ending_Careful_02_Lap") };
        ending.captions = new[] { "", "" };
        var art = Image(obj.transform, "Final knot", Art("WorldSystems/Wakeknot_Dormant"), Pos(node) + new Vector2(1.5f, 1.7f), -13, .7f);
        // Healed everywhere after the ending.
        var after = new GameObject("After the ending (healed)").AddComponent<MraVariant>();
        after.transform.SetParent(obj.transform, false); after.requires = new[] { "flag:" + MraState.EndingFlag };
        Image(after.transform, "Knot (healed)", Art("WorldSystems/Wakeknot_Awake"), Pos(node) + new Vector2(1.5f, 1.7f), -12, .7f);
        AreaRoomStyle.Freshen(after);
    }

    // ---------------------------------------------------------------- beats

    static readonly Dictionary<string, string> RelicAsset = new Dictionary<string, string>
    {
        { "climbing-moss", "ClimbingMoss" }, { "living-thread", "LivingThread" }, { "bloomfall", "Bloomfall" }, { "wind-leaf", "WindLeaf" }, { "glidecap", "Glidecap" },
    };

    static void Beats(Region r, Family f, Transform parent, List<Piece> pieces)
    {
        foreach (var n in r.nodes)
        {
            Vector2 p = Pos(n);
            var beat = Group(parent, $"Beat {n.id} {n.name}");
            if (n.waymark)
            {
                var wm = AreaRoom.AddWaymark(beat, null, p, n.checkpointId);
                wm.gameObject.name = "Waymark " + n.checkpointId;
                GroundProp(wm.transform, pieces, new[] { wm.GetComponent<SpriteRenderer>() });
            }
            else if (n.checkpoint)
            {
                var cp = A0TestRoomBuilder.AddCheckpoint(beat, p, n.checkpointId, ArtOrNull(f.shrine), 27f, null);
                GroundProp(cp.transform, pieces, new[] { cp.GetComponent<SpriteRenderer>() });
            }
            if (!string.IsNullOrEmpty(n.ability) && RelicAsset.TryGetValue(n.ability, out string asset))
            {
                var ability = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Resources/Relics/" + asset + ".asset");
                var shrine = new GameObject("Shrine " + ability.displayName).AddComponent<AbilityShrine>();
                shrine.transform.SetParent(beat, false); shrine.transform.position = p - new Vector2(1f, 0f);
                shrine.ability = ability;
                var pedestal = Image(shrine.transform, "Pedestal", Art("Props/Shrine_Ability"), p - new Vector2(1f, 0f), -15);
                shrine.relic = Image(shrine.transform, "Relic", ability.relic, p + new Vector2(-1f, shrine.hoverHeight), -13);
                Trigger(shrine.gameObject, new Vector2(1.4f, 2.6f), new Vector2(0f, 1.3f));
                GroundProp(shrine.transform, pieces, new[] { pedestal });
            }
            if (!string.IsNullOrEmpty(n.knot) && n.knot != "heart")
            {
                var knot = Knot(beat, p + new Vector2(1.5f, 0f), n.knot);
                GroundProp(knot.transform, pieces, new[] { knot.core });
            }
            if (n.name.ToLowerInvariant().Contains("lookout"))
            {
                var vista = new GameObject("Lookout vista").AddComponent<VistaZone>();
                vista.transform.SetParent(beat, false); vista.transform.position = p + new Vector2(0f, 2f);
                vista.GetComponent<BoxCollider2D>().size = new Vector2(n.padWidth, 4f);
                vista.size = 7f; vista.lift = 1.5f;
            }
        }
    }

    static MraKnot Knot(Transform parent, Vector2 at, string id)
    {
        var knot = new GameObject("Knot " + id).AddComponent<MraKnot>();
        knot.transform.SetParent(parent, false); knot.transform.position = at;
        knot.knot = id;
        knot.coreDim = Art("WorldSystems/Wakeknot_Dormant"); knot.coreLit = Art("WorldSystems/Wakeknot_Awake");
        Vector2 centre = at + new Vector2(0f, 2.05f - .35f);
        knot.core = Image(knot.transform, "Knot", knot.coreDim, centre, -13);
        knot.glow = Image(knot.transform, "Glow", Art("WorldSystems/Wakeknot_Glow"), centre, -12);
        knot.glow.color = new Color(1f, 1f, 1f, 0f);
        Trigger(knot.gameObject, new Vector2(2.4f, 3.6f), new Vector2(0f, 1.8f));
        return knot;
    }

    // ---------------------------------------------------------------- exits and landings

    // Each region boundary's threshold: an accepted arch the route passes through, by boundary.
    static readonly Dictionary<string, (string art, float height)> Threshold = new Dictionary<string, (string, float)>
    {
        { "MR01_MR02", ("Props/Prop_Causeway_Aqueduct_Arch_Intact", 4.2f) },   // the road into the Long Causeway
        { "MR02_MR03", ("Props/Portal_Gate_A5", 3.9f) },
        { "MR03_MR04", ("Props/Portal_Gate_A5", 3.9f) },
        { "MR04_MR05", ("Props/Portal_Gate_Milestone", 3.9f) },
        { "MR05_MR06", ("Props/Portal_Gate_Milestone", 3.9f) },
        { "MR06_MR07", ("Props/Portal_Gate_A5", 3.9f) },
        { "MR07_MR08", ("Props/Portal_Gate_Milestone", 3.9f) },
    };

    static void Travel(Region r, Family f, Transform parent, List<Piece> pieces)
    {
        var root = Group(parent, "Exits and landings");
        Vector2 first = Pos(r.NodeById(r.entry)), last = Pos(r.NodeById(r.exit));
        foreach (var l in world.links)
        {
            if (l.target == r.entry)
            {
                Arrival(root, "arrive:" + l.id, l.arrival, 1f);
                var from = world.RegionByScene(l.sourceScene);
                var back = Exit(root, "Exit back to " + from.name, new Vector2(r.cameraMin.x + 2.5f, first.y + 5f), l.sourceScene, "return:" + l.id, new string[0]);
                back.destinationName = from.name; back.direction = -1f;
                Arch(root, l.id, back.transform.position.x + .6f, pieces);
            }
            if (l.source == r.exit)
            {
                Vector2 reverse = Moved.ContainsKey(r.exit) ? Spawn(r.NodeById(r.exit)) : l.reverseArrival;
                Arrival(root, "return:" + l.id, reverse, -1f);
                var to = world.RegionByScene(l.targetScene);
                var on = Exit(root, "Exit on to " + to.name, new Vector2(last.x + 8f, last.y + 5f), l.targetScene, "arrive:" + l.id, l.requires);
                on.destinationName = to.name; on.direction = 1f;
                Arch(root, l.id, on.transform.position.x - .6f, pieces);
            }
        }
        // Caves' safe spots (a house's is at its own door: HouseFront).
        foreach (var c in world.ChambersOf(r.id)) if (!c.InPlace && !c.IsHouse) Arrival(root, c.returnId, c.returnSpawn, 1f);
        // The region's ends: walls beyond the exits.
        Wall(root, "West end", new Vector2(r.cameraMin.x - .5f, (r.cameraMin.y + r.cameraMax.y) * .5f), new Vector2(1f, r.cameraMax.y - r.cameraMin.y + 60f));
        Wall(root, "East end", new Vector2(r.cameraMax.x + .5f, (r.cameraMin.y + r.cameraMax.y) * .5f), new Vector2(1f, r.cameraMax.y - r.cameraMin.y + 60f));
    }

    // The threshold arch over a region exit, grounded by its painted feet, behind Qori.
    static void Arch(Transform parent, string link, float x, List<Piece> pieces)
    {
        if (!Threshold.TryGetValue(link, out var t)) return;
        var sprite = ArtOrNull(t.art); if (sprite == null) return;
        float scale = t.height / sprite.bounds.size.y;
        var arch = Image(parent, "Threshold arch " + link, sprite, new Vector2(x, Ground(pieces, x) + sprite.bounds.extents.y * scale), -25, scale);
        GroundProp(arch.transform, pieces, new[] { arch }, .12f);
    }

    public static MraArrival Arrival(Transform parent, string id, Vector2 at, float facing)
    {
        var a = new GameObject("Landing " + id).AddComponent<MraArrival>();
        a.transform.SetParent(parent, false); a.transform.position = at;
        a.arrivalId = id; a.facing = facing;
        return a;
    }

    static MraExit Exit(Transform parent, string name, Vector2 centre, string scene, string arrival, string[] requires)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false); obj.transform.position = centre;
        Trigger(obj, new Vector2(2f, 14f), Vector2.zero);
        var exit = obj.AddComponent<MraExit>(); exit.targetScene = scene; exit.arrivalId = arrival; exit.requires = requires ?? new string[0];
        return exit;
    }

    public static void Wall(Transform parent, string name, Vector2 centre, Vector2 size)
    {
        var wall = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        wall.transform.SetParent(parent, false); wall.transform.position = centre;
        wall.AddComponent<BoxCollider2D>().size = size;
        wall.AddComponent<WallSurface>().allowsWallCling = false;
    }

    // ---------------------------------------------------------------- side chamber doors

    // Each mouth sits where the designer put it: set into the wall that rises at the end of its
    // beat's broad pad (the art's back edge buried in the rock), or standing free on the pad where
    // the ground falls away. Its base is sunk a little into the ground, plants hide the joins, and
    // the trigger, the art and the prompt share the mouth's centre.
    public const float EntranceScale = .74f;
    static void Doors(Region r, Family f, Transform parent, List<Piece> pieces)
    {
        var root = Group(parent, "Side chamber doors");
        var design = RouteDesign.Of(r.id);
        var plants = Slices(f.decor).Where(s => !s.name.Contains("hanging") && s.bounds.size.y < 1.6f).ToArray();
        foreach (var c in world.ChambersOf(r.id))
        {
            var d = design.Door(c.id);
            float x = d != null ? d.x : Pos(r.NodeById(c.entranceNode)).x + 5.5f;
            float y = Ground(pieces, x);
            var obj = new GameObject($"Door {c.id} {c.name}");
            obj.transform.SetParent(root, false); obj.transform.position = new Vector2(x, y);
            Trigger(obj, new Vector2(2.6f, 3f), new Vector2(0f, 1.5f));
            var door = obj.AddComponent<MraChamberDoor>();
            door.chamberId = c.id; door.chamberName = c.name; door.chamberScene = c.scene; door.prerequisite = c.prerequisite;
            door.promptHeight = 2.9f;
            var mouth = Image(obj.transform, "Entrance", Art(f.entrance), new Vector2(x, y), -30, EntranceScale);
            // The cave art is pale limestone; in the bark and slate regions it takes on the regional
            // rock's colour (a renderer tint; the accepted image is unchanged).
            if (f.fill.StartsWith("Terrain/A") && f.entrance.EndsWith("WallCave"))
            {
                Color rock = Average(Art(f.fill), 0f, 1f);
                float peak = Mathf.Max(.01f, rock.maxColorComponent);
                var tone = Color.Lerp(Color.white, new Color(rock.r / peak * .72f, rock.g / peak * .72f, rock.b / peak * .72f), .7f);
                tone.a = 1f; mouth.color = tone;
            }
            if (plants.Length > 0)
            {
                var random = new System.Random(c.id.GetHashCode() & 0x7fffffff);
                var left = Image(obj.transform, "Dressing (left foot)", plants[random.Next(plants.Length)], new Vector2(x - 2.05f, y - .1f), -27);
                left.flipX = true;
                if (d == null || !d.embedded) Image(obj.transform, "Dressing (right foot)", plants[random.Next(plants.Length)], new Vector2(x + 2.05f, y - .1f), -27);
                else Image(obj.transform, "Dressing (on the wall)", plants[random.Next(plants.Length)], new Vector2(d.wallX + .9f, d.wallTop - .1f), -27);
            }
            // The mouth's painted base on the pad (its padding ignored), bedded in a little; then
            // each plant on the ground under it.
            GroundProp(obj.transform, pieces, new[] { mouth }, .12f);
            foreach (var plant in obj.GetComponentsInChildren<SpriteRenderer>().Where(x => x.name.StartsWith("Dressing")))
                GroundProp(plant.transform, pieces, new[] { plant }, .1f);
        }
    }

    // ---------------------------------------------------------------- encounters

    static void Encounters(Region r, Transform parent, List<Piece> pieces)
    {
        foreach (var enc in world.encounters.Where(e => e.region == r.id))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(enc.prefab);
            if (prefab == null) { Debug.LogWarning("[MraWorldBuilder] encounter prefab missing: " + enc.prefab); continue; }
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            obj.name = $"Encounter {enc.id} ({Path.GetFileNameWithoutExtension(enc.prefab)})";
            bool flying = enc.prefab.Contains("Thornwing") || enc.prefab.Contains("GustMoth");
            // On its shelf: the middle of the designer's flat ground for this fight.
            var shelf = RouteDesign.Of(r.id).Shelf(enc.id);
            float x = shelf != null ? (shelf.x0 + shelf.x1) * .5f : enc.center.x;
            float ground = Ground(pieces, x);
            obj.transform.position = new Vector3(x, (float.IsNaN(ground) ? enc.center.y : ground) + (flying ? 2.5f : .6f), 0f);
        }
    }

    // ---------------------------------------------------------------- decor and background

    static void Decor(Region r, Family f, Transform parent, List<Piece> pieces)
    {
        // Only things that rest on the ground: hanging roots, strands, cables and banners belong on
        // lips and ceilings (CliffDressing), not standing on a tread.
        bool Hangs(Sprite x) { string n = x.name.ToLowerInvariant(); return n.Contains("hanging") || n.Contains("stalactite") || n.Contains("cable") || n.Contains("strand") || n.Contains("banner") || n.Contains("trailing"); }
        var sprites = Slices(f.decor).Where(x => !Hangs(x)).ToArray();
        if (sprites.Length == 0) return;
        var keepClear = new List<float>();
        foreach (var n in r.nodes) keepClear.Add(Pos(n).x);
        foreach (var e in r.edges) if (e.module.Present) keepClear.Add(e.module.position.x);
        var design = RouteDesign.Of(r.id);
        foreach (var d in design.doors) keepClear.Add(d.x);
        foreach (var sh in design.shelves) { keepClear.Add(sh.x0); keepClear.Add((sh.x0 + sh.x1) * .5f); keepClear.Add(sh.x1); }
        // Walls and their landings stay clear (a plant on a lip hides the edge a jump needs).
        foreach (var fe in design.features) if (fe.kind == "step" || fe.kind == "ledge" || fe.kind == "climb") keepClear.Add(fe.x);
        // Qvale: nothing scattered in front of the house fronts or on the listening overlook.
        var clearSpans = new List<Vector2>();
        if (r.id == "MR04")
        {
            foreach (var c in world.ChambersOf(r.id).Where(c => c.IsHouse || c.InPlace)) clearSpans.Add(new Vector2(c.entrance.x - 3.5f, c.entrance.x + 17f));
            float n9 = Pos(r.NodeById("MR04_N09")).x; clearSpans.Add(new Vector2(n9 - 5f, n9 + 16f));
        }
        var random = new System.Random(r.ordinal * 7919);
        for (float x = r.cameraMin.x + 4f; x < r.cameraMax.x - 4f; x += 8f + (float)random.NextDouble() * 4f)
        {
            if (keepClear.Any(k => Mathf.Abs(k - x) < 2.5f) || clearSpans.Any(sp => x > sp.x && x < sp.y)) continue;
            float y = Ground(pieces, x);
            if (float.IsNaN(y) || Mathf.Abs(Ground(pieces, x - .5f) - y) > .05f || Mathf.Abs(Ground(pieces, x + .5f) - y) > .05f) continue;   // treads only
            var sprite = sprites[random.Next(sprites.Length)];
            var d = Image(parent, "Decor " + sprite.name, sprite, new Vector2(x, y), -20);
            d.flipX = random.Next(2) == 0;
            GroundProp(d.transform, pieces, new[] { d }, .1f);
        }
    }

    // The earlier regional far paintings (the approved A0-A3 rooms' far layer), by region: misty
    // valleys, the aqueduct, the grove, the falls. Regions without one get a hazed, smaller copy of
    // their Mid strip as a far ridge.
    static readonly Dictionary<string, string> FarPainting = new Dictionary<string, string>
    {
        { "MR01", "Assets/Art/Backgrounds/MistyValley_Background_v1.png" },
        { "MR02", "Assets/Resources/WorldBackground/Aqueduct.png" },
        { "MR05", "Assets/Resources/WorldBackground/AncientGrove.png" },
        { "MR06", "Assets/Resources/WorldBackground/FallsSanctuary.png" },
    };

    // Layers are placed relative to the camera (the climb is hundreds of units tall, so they follow it
    // almost fully up and down, and drift sideways for depth), so each skyline sits at a chosen
    // height on screen: the far ridge high, the valley's Mid above the middle, the Near just under it.
    // The heights are set for the region's middle and drift under a unit across its whole climb.
    // Large rock masses: the accepted crease decals (cracks with roots and moss) scattered through
    // the fill of the limestone regions, always well inside the rock (never on a walk line or a
    // wall face), so a big mass reads as weathered stone rather than one repeating texture.
    static void RockDressing(Region r, Family f, Transform parent, List<Piece> pieces)
    {
        string kit = r.id == "MR02" ? "Terrain/Body/Causeway/Causeway_Crease_" : "Terrain/Body/Body_Crease_";
        if (!(r.id == "MR01" || r.id == "MR02" || r.id == "MR03" || r.id == "MR04")) return;
        var creases = new[] { "Arc_Fold", "Branching_Fissure", "Forked_Diagonal", "Vertical_Root_Seam", "Y_Split", "Zigzag_Crease" }
            .Select(n => ArtOrNull(kit + n)).Where(c => c != null).ToArray();
        if (creases.Length == 0) return;
        var random = new System.Random(r.ordinal * 104729);
        Color shade = new Color(.95f, .93f, .9f, .62f);
        for (float x = r.cameraMin.x + 6f; x < r.cameraMax.x - 6f; x += 6f + (float)random.NextDouble() * 6f)
        {
            var sprite = creases[random.Next(creases.Length)];
            float scale = .85f + (float)random.NextDouble() * .35f, half = sprite.bounds.size.x * scale * .5f + .6f, tall = sprite.bounds.size.y * scale;
            float top = Mathf.Min(Ground(pieces, x - half), Ground(pieces, x), Ground(pieces, x + half));
            if (float.IsNaN(top)) continue;
            float y = top - 2.6f - tall * .5f - (float)random.NextDouble() * 5f;
            if (y - tall * .5f < r.cameraMin.y - 5f) continue;
            var d = Image(parent, "Crease " + sprite.name, sprite, new Vector2(x, y), MraTerrain.FillOrder + 1, scale);
            d.flipX = random.Next(2) == 0; d.color = shade;
        }
    }

    // Vegetation at the cliffs: hanging ivy or roots over the lip of walls 1.6 u and taller, a plant
    // or pebbles at the foot, from the region's own decor sheets (deterministic; caves kept clear).
    static void CliffDressing(Region r, Family f, Transform parent, List<Piece> pieces)
    {
        string legacy = "Decor/Decor_" + (f.id == "Qvale" ? "A5" : f.id) + "_Sheet";
        var sheets = new[] { f.decor, legacy, "Decor/Decor_Body_Sheet" }.Distinct().Select(Slices).ToList();
        bool Hanging(Sprite x) { string n = x.name.ToLowerInvariant(); return n.Contains("hanging") || n.Contains("trailing_ivy") || n.Contains("stalactite"); }
        var hanging = sheets.Select(sh => sh.Where(Hanging).ToArray()).FirstOrDefault(a => a.Length > 0) ?? new Sprite[0];
        var foot = sheets[0].Where(x => !Hanging(x) && x.bounds.size.y < 1f && x.bounds.size.x < 1.6f).ToArray();
        var doors = RouteDesign.Of(r.id).doors.Select(d => d.x).ToList();
        // The painted cliff kit carries its own roots, moss and talus: only its rock outcrops are added,
        // on taller walls, never walkable (decor).
        var ledges = f.cliffKit != null ? new[] { ArtOrNull(f.cliffKit + "_Cliff_Ledge_A"), ArtOrNull(f.cliffKit + "_Cliff_Ledge_B") }.Where(x => x != null).ToArray() : null;
        if (ledges != null)
        {
            if (ledges.Length == 0) return;
            int m = 0;
            foreach (var p in pieces)
                for (int i = 0; i < p.points.Count - 1; i++)
                {
                    Vector2 a = p.points[i], b = p.points[i + 1];
                    float h = Mathf.Abs(b.y - a.y);
                    if (Mathf.Abs(b.x - a.x) > 1e-3f || h < 3.6f || doors.Any(d => Mathf.Abs(d - a.x) < 2.6f)) continue;
                    bool rockRight = b.y > a.y;
                    float low = Mathf.Min(a.y, b.y);
                    var s0 = ledges[m++ % ledges.Length];
                    var ledge = Image(parent, "Ledge " + s0.name, s0, new Vector2(a.x, low + h * (.42f + .16f * Mathf.Abs(Mathf.Sin(a.x * 2.3f)))), MraTerrain.CliffCornerOrder + 2);
                    ledge.flipX = !rockRight;   // the outcrops face left: mirrored onto a right-facing wall
                }
            return;
        }
        int k = 0;
        foreach (var p in pieces)
            for (int i = 0; i < p.points.Count - 1; i++)
            {
                Vector2 a = p.points[i], b = p.points[i + 1];
                if (Mathf.Abs(b.x - a.x) > 1e-3f || Mathf.Abs(b.y - a.y) < 1.6f) continue;
                if (doors.Any(d => Mathf.Abs(d - a.x) < 2.6f)) continue;
                float top = Mathf.Max(a.y, b.y), low = Mathf.Min(a.y, b.y), outward = b.y > a.y ? -1f : 1f;   // the open side
                float h = Mathf.Abs(Mathf.Sin(a.x * 3.17f + k * 1.7f)); k++;
                if (hanging.Length > 0 && h > .35f)
                {
                    var s0 = hanging[Mathf.FloorToInt(h * 997f) % hanging.Length];
                    float scale = Mathf.Min(1f, (top - low) * .6f / Mathf.Max(.1f, s0.bounds.size.y));
                    var hang = Image(parent, "Hanging " + s0.name, s0, new Vector2(a.x + outward * .12f, top - .06f - s0.bounds.max.y * scale), -27, scale);
                    hang.flipX = outward > 0f;
                }
                if (foot.Length > 0 && h < .6f)
                {
                    var s1 = foot[Mathf.FloorToInt(h * 613f) % foot.Length];
                    var plant = Image(parent, "Foot " + s1.name, s1, new Vector2(a.x + outward * .55f, low), -21, .85f);
                    GroundProp(plant.transform, pieces, new[] { plant }, .08f);
                }
            }
    }

    static void Background(Transform parent, Camera camera, Region r, Family f)
    {
        var root = Group(parent, "Background");
        float refX = r.nodes.Average(n => Pos(n).x);
        // The climb the layers sink across: the region's beats, camera framing included.
        var climb = new Vector2(r.nodes.Min(n => Pos(n).y) + 1f, r.nodes.Max(n => Pos(n).y) + 1f);
        // How far each depth sinks on screen over the whole climb: near layers most, far least.
        // The valley layers move as one painting (user feedback, 5 Oct 2026: separate speeds made the
        // three layers obvious): one horizontal rate and one vertical sink for all of them, so they never
        // slide against each other; the depth comes from the painting itself, and the terrain moves at
        // full speed in front. Only the sky (camera-locked) differs.
        const float Together = .9f;
        float Shift(int order) => .6f;
        ParallaxLayer Layer(string name, Sprite sprite, Texture2D texture, float height, Vector2 follow, float bottom, int order, Color tint, Color below, float extend, bool repeat = true)
        {
            if (sprite == null && texture == null) return null;
            var layer = new GameObject(name).AddComponent<ParallaxLayer>();
            layer.transform.SetParent(root, false);
            bool sky = follow.y >= 1f;
            if (!sky) follow = new Vector2(Together, follow.y);
            layer.sprite = sprite; layer.texture = texture; layer.height = height; layer.repeat = repeat; layer.follow = follow;
            layer.climbProgression = !sky; layer.climbRange = climb; layer.climbShift = sky ? 0f : Shift(order);
            layer.baseY = bottom;   // the bottom relative to the camera centre (the sky follows the camera fully)
            layer.baseX = repeat ? 0f : refX * (1f - follow.x);
            layer.tint = tint; layer.extendBelow = extend; layer.belowColor = below; layer.sortingOrder = order; layer.targetCamera = camera;
            layer.composedForSize = 5f; layer.belowBlend = 1.2f;
            return layer;
        }
        var sky = ArtOrNull(f.sky);
        Color skyTone = sky != null ? Average(sky, 0f, 1f) : f.skyColour;
        Color HazeTo(float amount) => Color.Lerp(Color.white, new Color(skyTone.r / Mathf.Max(.01f, skyTone.maxColorComponent), skyTone.g / Mathf.Max(.01f, skyTone.maxColorComponent), skyTone.b / Mathf.Max(.01f, skyTone.maxColorComponent)), amount);

        // Atmospheric separation: where the valley layers are as dark as the walkable rock (the slate
        // Summit, the Descent's deep green) they're hazed more toward the sky, so the route reads first.
        // The layers can only be darkened by a tint, so the lift comes from a veil of sky colour
        // between the valley and the rock, hung on the camera.
        var (nearHaze, midHaze) = (.08f, .18f);
        // A depth region's own warm set needs none (the Summit's veil was for the dark slate A6 set).
        float veil = DepthRegions.Contains(r.id) ? 0f : r.id switch { "MR07" => .3f, "MR08" => .24f, "MR05" => .1f, _ => 0f };
        if (veil > 0f)
        {
            string veilPath = Folder + "Resources/MRA/MRA_HazeVeil.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(veilPath);
            if (importer.textureType != TextureImporterType.Sprite || importer.spritePixelsPerUnit != 1f)
            {
                importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 1f; importer.mipmapEnabled = false; importer.SaveAndReimport();
            }
            var cameraVeil = camera.transform.Find("Haze veil (MRA builder)");
            if (cameraVeil != null) Object.DestroyImmediate(cameraVeil.gameObject);
            var v = Image(camera.transform, "Haze veil (MRA builder)", AssetDatabase.LoadAssetAtPath<Sprite>(veilPath), camera.transform.position + new Vector3(0f, 0f, 10f), -70);
            v.transform.localPosition = new Vector3(0f, 0f, 10f); v.transform.localScale = new Vector3(18f, 11f, 1f);   // 72 x 44 u: any zoom
            Color tone = sky != null ? Average(sky, 0f, .5f) : f.skyColour;
            v.color = new Color(tone.r, tone.g, tone.b, veil);
        }
        Layer("Sky - " + Path.GetFileName(f.sky), sky, null, 24f, new Vector2(1f, 1f), -12f, -110, Color.white, f.skyColour, 0f);
        if (DepthRegions.Contains(r.id)) { DepthLayers(root, camera, r, climb, HazeTo); return; }
        Sprite mid = ArtOrNull(f.mid), near = ArtOrNull(f.near), far = ArtOrNull(f.far);
        if (FarPainting.TryGetValue(r.id, out string painting))
            Layer("Far painting - " + Path.GetFileNameWithoutExtension(painting), null, AssetDatabase.LoadAssetAtPath<Texture2D>(painting), 20f, new Vector2(.985f, .997f), -10.6f, -105, Color.white, f.skyColour, 0f, false);
        if (far != null)
            Layer("Far - " + Path.GetFileName(f.far), far, null, 4.8f, new Vector2(.92f, .997f), 2.4f - Skyline(far) * 4.8f, -100, HazeTo(.25f), Average(far, 0f, .03f), 14f);
        else if (!FarPainting.ContainsKey(r.id) && mid != null)
        {
            var tint = HazeTo(.45f);
            Layer("Far ridge (hazed Mid) - " + Path.GetFileName(f.mid), mid, null, 4.4f, new Vector2(.93f, .997f), 2.7f - Skyline(mid) * 4.4f, -100, tint, Average(mid, 0f, .03f) * tint, 14f);
        }
        if (mid != null)
            Layer("Mid - " + Path.GetFileName(f.mid), mid, null, 7.2f, new Vector2(.82f, .997f), 1.0f - Skyline(mid) * 7.2f, -90, HazeTo(midHaze), Average(mid, 0f, .03f) * HazeTo(midHaze), 16f);
        if (near != null)
            // The Near strip stands in the lower third (its foot at the frame's bottom), a touch hazed,
            // so Qori, enemies and landing edges read in front of it, not against it.
            Layer("Near - " + Path.GetFileName(f.near), near, null, 7.0f, new Vector2(.62f, .994f), -5.2f, -80, HazeTo(nearHaze), Average(near, 0f, .03f) * HazeTo(nearHaze), 16f);
        if (r.id == "MR04")
        {
            // Qvale's own basin (Batch 14, Review 25): the far side of it, homes under the overhangs, a 5 u
            // band over the Mid's hills (mockup M2) with its skyline 1.3 u above the frame's centre (37% down).
            var rim = Art("Backgrounds/BG_Qvale_VaultRim_Mid");
            Layer("Vault rim - BG_Qvale_VaultRim_Mid", rim, null, 5f, new Vector2(.9f, .997f), 1.3f - Skyline(rim) * 5f, -88, HazeTo(.12f), Average(rim, 0f, .03f) * HazeTo(.12f), 14f);
            OverlookVista(root, camera, r);
        }
        if (r.id == "MR03")
        {
            // The accepted mill landmark: one painting (not repeating) that drifts into view over the
            // Mill Ridge beat, standing on the far valley between the far ridge and the Mid layer.
            Vector2 ridge = Pos(r.NodeById("MR03_N07"));
            var mill = Layer("Landmark - BG_Terraces_Mill_Landmark", Art("Backgrounds/BG_Terraces_Mill_Landmark"), null, 5.5f, new Vector2(.9f, .997f), -1.2f, -95, HazeTo(.1f), Color.white, 0f, false);
            mill.baseX = (ridge.x + 6f) * (1f - mill.follow.x);
        }
    }

    // ---------------------------------------------------------------- depth backgrounds

    // Regions whose backgrounds use real depth (ParallaxLayer.depth): each plane moves at the speed
    // its distance gives it (screen speed 1/depth, the terrain being 1), zooms like a camera pulling
    // back, and sinks across the climb by its distance. Each has its own Far/Mid/Near set over
    // BG_Sky_Terraces: the Terraces (Review 23), the Cradle and the Summit (Batch 14, Review 25).
    // The Terraces also has its mill landmark, foreground plants and Mill Ridge lookout (DepthExtras).
    static readonly Dictionary<string, string> DepthSets = new Dictionary<string, string>
    {
        { "MR01", "Cradle" }, { "MR03", "Terraces" }, { "MR07", "Summit" },
    };
    static ICollection<string> DepthRegions => DepthSets.Keys;
    const float FarDepth = 25f, MillDepth = 14f, MidDepth = 10f, NearDepth = 5f, ForegroundDepth = .75f;
    // How far a plane sinks on screen over the whole climb: 5 / depth (near 1 u, mid .5 u, far .2 u).
    static float ClimbSink(float depth) => Mathf.Min(1.2f, 5f / depth);

    static ParallaxLayer DepthLayer(Transform root, Camera camera, Vector2 climb, string name, Sprite sprite, Texture2D texture, float height, float depth,
        float bottom, int order, Color tint, float extend, bool repeat, float climbSink)
    {
        var layer = new GameObject(name).AddComponent<ParallaxLayer>();
        layer.transform.SetParent(root, false);
        layer.sprite = sprite; layer.texture = texture; layer.height = height; layer.repeat = repeat; layer.depth = depth;
        layer.follow = new Vector2(1f - 1f / depth, .997f);   // for reading; FollowX comes from depth
        layer.climbProgression = true; layer.climbRange = climb; layer.climbShift = climbSink;
        layer.baseY = bottom; layer.tint = tint; layer.extendBelow = extend; layer.sortingOrder = order; layer.targetCamera = camera;
        layer.belowColor = sprite != null && extend > 0f ? Average(sprite, 0f, .03f) * tint : Color.white;
        layer.composedForSize = 5f; layer.belowBlend = 1.2f;
        return layer;
    }

    // A region's set: one painting split by distance, drawn 10 u tall (1:1 at 1080p for the Terraces'
    // 1920 x 1080 canvases; the Batch 14 sets are 2560 x 1440, the same proportions). The Far and Mid
    // stand 1.2 u higher than the Near, so the valley floor shows above the walk line. In the Terraces
    // the mill landmark stands between them at its own depth, 2.8 u tall.
    static void DepthLayers(Transform root, Camera camera, Region r, Vector2 climb, System.Func<float, Color> hazeTo)
    {
        string set = DepthSets[r.id];
        DepthLayer(root, camera, climb, $"Far - BG_{set}_Far (depth 25)", Art($"Backgrounds/BG_{set}_Far"), null, 10f, FarDepth, -3.8f, -105, Color.white, 14f, true, ClimbSink(FarDepth));
        if (r.id == "MR03")
        {
            Vector2 ridge = Pos(r.NodeById("MR03_N07"));
            var mill = DepthLayer(root, camera, climb, "Landmark - BG_Terraces_Mill_Landmark (depth 14)", Art("Backgrounds/BG_Terraces_Mill_Landmark"), null, 2.8f, MillDepth, .2f, -97, hazeTo(.18f), 0f, false, ClimbSink(MillDepth));
            mill.baseX = (ridge.x + 6f) / MillDepth;   // centred when the camera is 6 u past Mill Ridge
        }
        DepthLayer(root, camera, climb, $"Mid - BG_{set}_Mid (depth 10)", Art($"Backgrounds/BG_{set}_Mid"), null, 10f, MidDepth, -3.8f, -90, Color.white, 14f, true, ClimbSink(MidDepth));
        DepthLayer(root, camera, climb, $"Near - BG_{set}_Near (depth 5)", Art($"Backgrounds/BG_{set}_Near"), null, 10f, NearDepth, -5f, -80, Color.white, 16f, true, ClimbSink(NearDepth));
    }

    // Qvale's listening overlook (Batch 14, Review 25): the view down the valley, shown only while Qori
    // sits on the bench. It fades in over 1.4 s as the seated camera eases out and the street's layers
    // fade under it (SeatedVistaFade). Composed for the street's size 5 as 10 u from the frame's bottom,
    // so at the seated size 6.2 it fills the frame's height, and its 35 u width covers the frame and
    // the camera's travel. Centred on the seated frame (the bench, 7 u past MR04_N09; MraTownBuilder).
    static void OverlookVista(Transform root, Camera camera, Region r)
    {
        var street = root.GetComponentsInChildren<ParallaxLayer>().Where(l => l.follow.y < 1f).ToArray();
        float frameX = r.NodeById("MR04_N09").position.x + 7f - 1.5f;
        var vista = new GameObject("Overlook vista - BG_Qvale_Overlook_Vista (seated)").AddComponent<ParallaxLayer>();
        vista.transform.SetParent(root, false);
        vista.sprite = Art("Backgrounds/BG_Qvale_Overlook_Vista"); vista.height = 10f; vista.repeat = false;
        vista.follow = new Vector2(.95f, 1f); vista.baseY = -5f; vista.baseX = frameX * (1f - .95f);
        vista.composedForSize = 5f; vista.extendBelow = 0f; vista.sortingOrder = -78; vista.targetCamera = camera;
        vista.tint = new Color(1f, 1f, 1f, 0f);
        var fade = vista.gameObject.AddComponent<SeatedVistaFade>();
        fade.vista = vista; fade.under = street;
    }

    // In front of the terrain: sparse clumps of accepted Terraces decor (dry grass, olive fern, hay
    // tufts), drawn large and darkened like plants close to the camera, passing faster than the
    // terrain at the bottom of the frame. Each is a sub-rectangle of its decor sheet (the file is
    // untouched). Not where a big drop is near, where the camera looks down for the landing.
    // Also a lookout zoom at Mill Ridge (the region had none), to show zoom with depth.
    static void DepthExtras(Region r, Camera camera, Transform root, List<Piece> pieces)
    {
        var bg = root.Find("Background");
        var wanted = new[] { "Decor_Terraces_dry_grass", "Decor_Terraces_olive_fern", "Decor_A5_hay_tuft", "Decor_A5_dry_grass", "Decor_A5_fern" };
        var plants = Slices("Decor/Decor_Terraces_Sheet").Concat(Slices("Decor/Decor_A5_Sheet")).Where(x => wanted.Contains(x.name)).ToArray();
        if (bg != null && plants.Length > 0)
        {
            var climb = new Vector2(r.nodes.Min(n => Pos(n).y) + 1f, r.nodes.Max(n => Pos(n).y) + 1f);
            var random = new System.Random(r.ordinal * 7919);
            int k = 0;
            for (float x = r.cameraMin.x + 14f; x < r.cameraMax.x - 14f; x += 14f + (float)random.NextDouble() * 12f)
            {
                // It stays in the bottom fifth of the frame (under the walk line, at 35-40%). Climbs are
                // fine; a big drop nearby (over 3 u within 6 u) is not.
                float g0 = Ground(pieces, x - 6f), g1 = Ground(pieces, x), g2 = Ground(pieces, x + 6f);
                if (float.IsNaN(g0) || float.IsNaN(g1) || float.IsNaN(g2) || g1 - Mathf.Min(g0, g2) > 3f) continue;
                var plant = plants[random.Next(plants.Length)];
                var tex = plant.texture; var tr = plant.textureRect;
                // Large and cropped by the frame's bottom edge: only the top ~55% rises into view
                // (the bottom fifth of the frame), as a plant right in front of the camera would.
                float tall = 3f + (float)random.NextDouble();
                var clump = DepthLayer(bg, camera, climb, $"Foreground - {plant.name} {k}", null, tex, tall, ForegroundDepth,
                    -5f - tall * .45f, 60, new Color(.5f, .46f, .4f, 1f), 0f, false, 0f);
                clump.uvRect = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
                clump.baseX = x / ForegroundDepth;   // passes the frame's centre when the camera is at x
                k++;
            }
        }
        var n7 = r.NodeById("MR03_N07");
        if (n7 != null && Object.FindObjectsByType<VistaZone>(FindObjectsSortMode.None).All(v => Mathf.Abs(v.transform.position.x - Pos(n7).x) > 8f))
        {
            var vista = new GameObject("Lookout vista (Mill Ridge, depth prototype)").AddComponent<VistaZone>();
            vista.transform.SetParent(root, false); vista.transform.position = Pos(n7) + new Vector2(0f, 2f);
            vista.GetComponent<BoxCollider2D>().size = new Vector2(Mathf.Max(8f, n7.padWidth), 4f);
            vista.size = 7f; vista.lift = 1.5f;
        }
    }

    // A strip's skyline: the share of its height under the median top of its painted pixels.
    static float Skyline(Sprite sprite)
    {
        var tex = Readable(sprite); if (tex == null) return .4f;
        var tops = new List<int>();
        for (int x = 0; x < tex.width; x += 32)
            for (int y = tex.height - 1; y >= 0; y--) if (tex.GetPixel(x, y).a > .5f) { tops.Add(y); break; }
        if (tops.Count == 0) return .4f;
        tops.Sort();
        return (tops[tops.Count / 2] + 1f) / tex.height;
    }

    // The average colour of a horizontal band of a sprite (0 = bottom row, 1 = top), for the flat
    // band under a layer and the haze tint.
    static Color Average(Sprite sprite, float from, float to)
    {
        var tex = Readable(sprite); if (tex == null) return Color.gray;
        int y0 = Mathf.Clamp(Mathf.FloorToInt(from * tex.height), 0, tex.height - 1), y1 = Mathf.Clamp(Mathf.CeilToInt(to * tex.height), y0 + 1, tex.height);
        Color sum = Color.clear; int n = 0;
        for (int y = y0; y < y1; y += 2) for (int x = 0; x < tex.width; x += 8) { var c = tex.GetPixel(x, y); if (c.a > .5f) { sum += c; n++; } }
        if (n == 0) return Color.gray;
        var avg = sum / n; avg.a = 1f; return avg;
    }

    static readonly Dictionary<string, Texture2D> readable = new Dictionary<string, Texture2D>();
    // The source PNG decoded (the imported texture may not be readable); the bytes are only read.
    static Texture2D Readable(Sprite sprite)
    {
        string path = AssetDatabase.GetAssetPath(sprite);
        if (readable.TryGetValue(path, out var t)) return t;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(path))) tex = null;
        return readable[path] = tex;
    }

    // ---------------------------------------------------------------- music

    // The soundtrack (Assets/Audio/Music; source titles and hashes in Tools/Music/MUSIC_IMPORT.json).
    // A first assignment by title, to be refined by ear: one track per region, one shared by the side
    // caves; Qvale's homes carry on with the town's track. Titles are never shown in game.
    public static readonly Dictionary<string, string> RegionMusic = new Dictionary<string, string>
    {
        { "MR01", "AwakeningInTheTitansPalm" }, { "MR02", "TheWetlandsSecret" }, { "MR03", "TerracesOfTheTitan" },
        { "MR04", "ASmallLightInTheDark" }, { "MR05", "Forest" }, { "MR06", "ThePathToThePeak" },
        { "MR07", "TheTitansDream" }, { "MR08", "TheTitansSleep" },
    };
    public const string CaveMusic = "SanctuaryOfLivingRoots";
    public static AudioClip MusicClip(string track) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/Music_" + track + ".wav");

    static void Music(Transform root, string track)
    {
        var clip = MusicClip(track);
        if (clip == null) { Debug.LogWarning("[MraWorldBuilder] music missing: " + track); return; }
        var cue = new GameObject("Music: " + track).AddComponent<MraMusic>();
        cue.transform.SetParent(root, false); cue.clip = clip;
    }

    // ---------------------------------------------------------------- the build list and index

    static void RegisterScenes()
    {
        var paths = world.regions.Select(r => ScenePath(r.scene)).Concat(world.chambers.Where(c => !c.InPlace).Select(c => ScenePath(c.scene))).ToList();
        var scenes = EditorBuildSettings.scenes.Where(s => !paths.Contains(s.path)).ToList();
        scenes.AddRange(paths.Select(p => new EditorBuildSettingsScene(p, true)));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void WriteIndex()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("{\n \"generated_by\": \"MraWorldBuilder\",\n \"spec_sha256\": \"" + world.specSha256 + "\",\n \"scenes\": [");
        var rows = new List<string>();
        foreach (var r in world.regions)
            rows.Add($"  {{\"scene\": \"{ScenePath(r.scene)}\", \"kind\": \"region\", \"id\": \"{r.id}\", \"name\": \"{r.name}\", \"beats\": {r.nodes.Length}, \"edges\": {r.edges.Length}, \"family\": \"{FamilyOf(r.id).id}\"}}");
        foreach (var c in world.chambers)
            rows.Add($"  {{\"scene\": \"{(c.InPlace ? ScenePath(world.RegionById(c.region).scene) + " (in place)" : ScenePath(c.scene))}\", \"kind\": \"{(c.InPlace ? "interior" : "chamber")}\", \"id\": \"{c.id}\", \"name\": \"{c.name.Replace("\"", "'")}\", \"type\": \"{c.type}\", \"reward\": \"{c.rewardId}\", \"return\": \"{c.returnId}\"}}");
        sb.AppendLine(string.Join(",\n", rows));
        sb.AppendLine(" ]\n}");
        File.WriteAllText(Folder + "SCENE_INDEX.json", sb.ToString());
    }
}

// Shows a generated before/after variant in the state a new game sees.
static class AreaRoomStyle
{
    public static void Freshen(MraVariant variant)
    {
        bool shown = false;   // a new game: no knots, no flags
        if (variant.invert) shown = true;
        foreach (Transform child in variant.transform) child.gameObject.SetActive(shown);
    }
}
