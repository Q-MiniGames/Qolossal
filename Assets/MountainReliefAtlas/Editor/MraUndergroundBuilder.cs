using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// The underground's first playable phase as greybox scenes: the shared heart chamber
// (MRAtlas_UG_HEART) and the approaches A1-A3 (from Qvale) and B1-B3 (from the Terraces), built from
// Underground/underground_v1.json (Tools/Underground/import_underground.py, the reviewed design
// package with the measured adjustments in UNDERGROUND_ADJUSTMENTS.md). Room coordinates are the
// scene's world units, origin at the room's bottom-left; each room is its own scene, joined by
// doorways (Up), never one continuous world.
//   - Geometry is temporary: plain stone blocks with a pale lip on every surface Qori can stand on,
//     one tint per route, a simple global light. Doors, water, gates, wheels, anchors, shrines and
//     pickups use the accepted art where it exists.
//   - The heart's five arrival sectors share one scene but are sealed from one another: solid rock
//     plus permanent grilles in the ribs (they block Qori, the thread and attacks). A and B are
//     playable; C, D and E are built so they can be seen (each in its own colour) but not reached.
//     The A/B lift opens only after both sectors have been reached on their own and its latch is set.
//   - The surface entrances are placed in Qvale and the Terraces at run time (MraUgSurface) from
//     Resources/MRA/UgSurfaceEntrances, written here; the regional scenes are not modified.
//   - Rewards in the heart are placeholders (their heart seed / song shell allocation is pending);
//     the A3 guardian doorway is reserved and sealed.
// Menu: Qolossal > Mountain Relief Atlas > Underground > Rebuild Underground Routes A-E (keeps current skill caves)
// Batch: -executeMethod MraWorldBuilder.BuildUndergroundBatch
public static partial class MraWorldBuilder
{
    public const string UgFolder = Folder + "Underground/", UgBlockout = UgFolder + "Blockout/";
    public const string UgData = UgFolder + "underground_v1.json";
    public static readonly string[] UgRooms = { "HEART", "A1", "A2", "A3", "B1", "B2", "B3",
        "MR01_C02", "MR02_C03", "MR03_C02", "MR05_C04", "MR06_C01", "C1", "C2", "C3", "D1", "D2", "D3", "E1", "E2", "E3" };

    // Route E as the design has it: it replaces the Descent's walk. The Summit's existing way down
    // leads into E1, the Descent's side caves hang off E2 and E3, and the existing careful ending
    // (same flags, stills and knot) is at the heart's centre, reached only from E. False: E is built
    // as an extra post-reveal path (its own door by the Summit's exit), the centre has no ending,
    // and the Descent and its ending are untouched.
    public const bool EReplacesDescent = true;
    // The five skill caves keep their existing scenes (MRAtlas_MR01_C02, ...); new rooms are MRAtlas_UG_<id>.
    public static readonly string[] UgCaves = { "MR01_C02", "MR02_C03", "MR03_C02", "MR05_C04", "MR06_C01" };
    public static string UgScene(string room) => System.Array.IndexOf(UgCaves, room) >= 0 ? "MRAtlas_" + room : "MRAtlas_UG_" + room;
    static bool IsCave(UgRoom r) => System.Array.IndexOf(UgCaves, r.id) >= 0;

    // ---------------------------------------------------------------- data (JsonUtility shapes)

    [System.Serializable] public sealed class UgItem { public string id, kind, condition, activeWhen; public float[] rect; public string[] opensWhenAll; public bool clingable; }
    [System.Serializable] public sealed class UgMarker { public string id, kind, detail, target; public float[] feet; public string[] requires; }
    [System.Serializable] public sealed class UgSkill { public string cave, ability, oldSurfaceGrant, newGrant, entrance; public string[] requiresBeforeGrant; }
    [System.Serializable] public sealed class UgRoom
    {
        public string id, name, region, theme, rewardId, reward, existingScene, existingReturnId;
        public float[] size;
        public UgItem[] solids, platforms, hazards, gates;
        public UgMarker[] markers;
        public string[] routeRequires;
        public UgMarker Marker(string mid) => markers.FirstOrDefault(m => m.id == mid);
    }
    [System.Serializable] public sealed class UgEntrance { public string id, scene, node, room, note, returnId; public float[] feet, returnFeet; public string[] requires; }
    [System.Serializable] public sealed class UgEdge { public string source, target, kind; public string[] requires; }
    [System.Serializable] public sealed class UgDoc { public int schema; public string package; public UgRoom[] rooms; public UgEntrance[] entrances; public UgEdge[] edges; public UgSkill[] skills; }

    static UgDoc ug;
    static UgRoom UgRoomById(string id) => ug.rooms.First(r => r.id == id);
    static Rect R(float[] a) => new Rect(a[0], a[1], a[2], a[3]);
    static Vector2 V(float[] a) => new Vector2(a[0], a[1]);
    const float FeetToCentre = .6f;   // measured: Qori's transform is 0.6 u above his feet (body 0.8 x 1.2)

    // The surface doorways' surveyed feet (MraUgTestDriver's survey of the live scenes): where the
    // package's proposed feet needed moving to stand on the ground clear of other triggers.
    // Surveyed on the live scenes (17 Oct): both feet are on the ground, and neither trigger overlaps
    // a home door, chamber door or region exit. ENT_A at the package's (145, 8) stood in front of
    // Qvale's street lamp (x 144.2-147.8) and its wind chime; moved 2.4 u left, still on the
    // package's pad (142-148) and 8.9 u from the mapmaker's door. ENT_B is the package's own.
    static readonly Dictionary<string, Vector2> SurfaceSurvey = new Dictionary<string, Vector2> { { "ENT_A", new Vector2(142.6f, 8f) } };

    // The scene each route's rooms report to MraSession for the Chart (the heart lies under the
    // Descent in the design, which must stay hidden before the reveal: it reports Qvale for now).
    static string UgSessionRegion(UgRoom r) => r.id == "HEART" ? "MR04" : r.id.StartsWith("E") ? "MR07" : r.region;

    static string Predicate(string p) =>
        p == Relics.LivingThread || p == Relics.ClimbingMoss || p == Relics.Bloomfall || p == Relics.WindLeaf || p == Relics.Glidecap ? "relic:" + p : p;
    static string[] Predicates(IEnumerable<string> ps) => (ps ?? new string[0]).Select(Predicate).ToArray();

    // ---------------------------------------------------------------- palette

    sealed class UgTheme { public Color back, rock, deck, lip; }
    static UgTheme ThemeOf(string room)
    {
        if (room.StartsWith("A")) return new UgTheme { back = new Color(.13f, .16f, .19f), rock = new Color(.30f, .35f, .39f), deck = new Color(.46f, .52f, .56f), lip = new Color(.78f, .86f, .88f) };
        if (room.StartsWith("MR")) return new UgTheme { back = new Color(.14f, .13f, .1f), rock = new Color(.36f, .33f, .26f), deck = new Color(.55f, .5f, .38f), lip = new Color(.86f, .9f, .66f) };
        if (room.StartsWith("C")) return new UgTheme { back = new Color(.1f, .14f, .1f), rock = new Color(.24f, .33f, .22f), deck = new Color(.42f, .52f, .34f), lip = new Color(.78f, .92f, .62f) };
        if (room.StartsWith("E")) return new UgTheme { back = new Color(.16f, .13f, .08f), rock = new Color(.4f, .34f, .2f), deck = new Color(.62f, .54f, .32f), lip = new Color(1f, .9f, .6f) };
        if (room.StartsWith("D")) return new UgTheme { back = new Color(.15f, .16f, .2f), rock = new Color(.38f, .4f, .46f), deck = new Color(.58f, .6f, .66f), lip = new Color(.9f, .94f, 1f) };
        if (room.StartsWith("B")) return new UgTheme { back = new Color(.19f, .15f, .11f), rock = new Color(.42f, .33f, .24f), deck = new Color(.62f, .51f, .37f), lip = new Color(.93f, .82f, .6f) };
        return new UgTheme { back = new Color(.10f, .09f, .08f), rock = new Color(.27f, .23f, .19f), deck = new Color(.5f, .45f, .37f), lip = new Color(.9f, .84f, .7f) };
    }
    // The heart's sectors, each in its route's colour so it reads from across the chamber.
    static readonly Dictionary<string, Color> SectorTint = new Dictionary<string, Color>
    {
        { "A", new Color(.20f, .30f, .38f) }, { "B", new Color(.40f, .29f, .16f) }, { "C", new Color(.20f, .34f, .20f) },
        { "D", new Color(.36f, .38f, .44f) }, { "E", new Color(.42f, .35f, .14f) },
    };
    static readonly Dictionary<string, Rect> SectorRect = new Dictionary<string, Rect>
    {
        { "A", Rect.MinMaxRect(1f, 1f, 10f, 8f) }, { "B", Rect.MinMaxRect(1f, 12f, 10f, 19f) },
        { "C", Rect.MinMaxRect(20f, 1f, 29f, 8f) }, { "D", Rect.MinMaxRect(20f, 12f, 29f, 19f) }, { "E", Rect.MinMaxRect(12f, 1f, 18f, 19f) },
    };

    // ---------------------------------------------------------------- entry points

    [MenuItem("Qolossal/Mountain Relief Atlas/Underground/Rebuild Underground Routes A-E (keeps current skill caves)")]
    public static void BuildUnderground()
    {
        GuardRegionalReplacement();
        Prepare();
        AssetDatabase.ImportAsset(UgData, ImportAssetOptions.ForceUpdate);
        ug = JsonUtility.FromJson<UgDoc>(File.ReadAllText(UgData));
        UgBlockoutArt();
        foreach (var id in UgRooms.Where(id => System.Array.IndexOf(UgCaves, id) < 0)) BuildUgRoom(UgRoomById(id));
        UgSurfaceEntrances();
        var paths = UgRooms.Where(id => System.Array.IndexOf(UgCaves, id) < 0).Select(id => ScenePath(UgScene(id))).ToList();
        var scenes = EditorBuildSettings.scenes.Where(s => !paths.Contains(s.path)).ToList();
        scenes.AddRange(paths.Select(p => new EditorBuildSettingsScene(p, true)));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log($"[UgBuilder] built {UgRooms.Length} underground scenes from {ug.package}");
    }

    public static void BuildUndergroundBatch() => BuildUnderground();

    // ---------------------------------------------------------------- a room

    static void BuildUgRoom(UgRoom r)
    {
        var theme = ThemeOf(r.id);
        var scene = Begin(ScenePath(UgScene(r.id)), out Transform root);
        groundingScene = UgScene(r.id);
        float w = r.size[0], h = r.size[1];
        Vector2 start = UgStart(r);
        string display = r.id == "HEART" ? "The Great Hollow" : r.name;
        // A skill cave stays the region's side chamber (its chamber id, Chart place and level id).
        var camera = Scaffold(scene, start, display, IsCave(r) ? "mra:" + r.id : "mra:UG_" + r.id, UgSessionRegion(r), IsCave(r) ? r.id : null, theme.back);
        var session = Object.FindAnyObjectByType<MraSession>();
        session.underground = !IsCave(r); session.chamberId = IsCave(r) ? r.id : "";
        var light = Object.FindAnyObjectByType<Light2D>(); if (light != null) light.intensity = .95f;
        var bounds = new GameObject("Camera Bounds").AddComponent<CameraBounds>();
        bounds.transform.SetParent(root, false);
        bounds.room = new Rect(-1f, -1f, w + 2f, h + 2f); bounds.top = true;
        Music(root, CaveMusic);

        var geo = Group(root, "Blockout geometry");
        Block(geo, "Back wall", new Rect(-4f, -4f, w + 8f, h + 8f), theme.back, -60, false);
        if (r.id == "HEART") foreach (var kv in SectorRect) Block(geo, "Sector " + kv.Key + " (colour)", kv.Value, SectorTint[kv.Key], -58, false);
        foreach (var s in r.solids) Block(geo, (s.clingable ? "Moss wall " : "Rock ") + s.id, R(s.rect), s.clingable ? theme.rock * new Color(.8f, 1.1f, .8f) : theme.rock, -6, true, s.clingable);
        // Rock beyond the room's shell, so the camera margin never shows an edge.
        Block(geo, "Outer rock W", Rect.MinMaxRect(-4f, -4f, 0f, h + 4f), theme.rock, -6, false);
        Block(geo, "Outer rock E", Rect.MinMaxRect(w, -4f, w + 4f, h + 4f), theme.rock, -6, false);
        Block(geo, "Outer rock S", Rect.MinMaxRect(-4f, -4f, w + 4f, 0f), theme.rock, -6, false);
        Block(geo, "Outer rock N", Rect.MinMaxRect(-4f, h, w + 4f, h + 4f), theme.rock, -6, false);

        var mech = Group(root, "Mechanics");
        foreach (var p in r.platforms)
        {
            if (p.kind == "moving") continue;   // the lifts, below
            var deck = Block(geo, (p.kind == "oneWay" ? "Ledge (one-way) " : "Deck ") + p.id, R(p.rect), theme.deck, -5, true);
            Lip(deck.transform, R(p.rect), theme.lip);
            if (p.kind == "oneWay")
            {
                var col = deck.GetComponent<BoxCollider2D>(); col.usedByEffector = true;
                var eff = deck.AddComponent<PlatformEffector2D>(); eff.useOneWay = true; eff.surfaceArc = 160f;
            }
        }
        UgWaters(r, mech);
        UgGates(r, geo, mech, theme);
        UgMarkers(r, root, mech, theme);
        if (r.id == "A2") UgReturnWalk(r, mech, theme);
        if (r.id == "HEART") UgHeart(r, root, mech, theme);
        UgHazards(r, mech);
        UgPhase2(r, root, mech, theme);
        // The accepted art over the greybox, where it exists (MraUndergroundArt).
        UgCisternArt(r, root, geo, mech); UgHollowArt(r, root, geo, mech); UgSharedArt(mech);
        Save(scene, ScenePath(UgScene(r.id)));
    }

    // Where the scene starts when opened directly: the first arrival (the heart: sector A's spawn).
    static Vector2 UgStart(UgRoom r)
    {
        var m = r.id == "HEART" ? r.Marker("SPAWN_A") : r.markers.First(x => x.kind == "portal");
        return ArrivalFeet(r, m) + new Vector2(0f, FeetToCentre + .02f);
    }

    // A portal's arrival spot: up to 1.2 u in from the doorway toward the room's middle (the heart:
    // its sector's spawn marker), as far as the body (0.8 x 1.2, measured) stands clear of the room's
    // rock and solid decks. B1's entry step sits half a unit under the launch shelf, so its landing
    // stays 0.5 u from the door.
    static Vector2 ArrivalFeet(UgRoom r, UgMarker m)
    {
        if (m.kind == "spawn") return V(m.feet);
        Vector2 f = V(m.feet);
        float dir = f.x < r.size[0] * .5f ? 1f : -1f;
        var blocks = r.solids.Concat(r.platforms.Where(p => p.kind == "solid")).Select(x => R(x.rect)).ToList();
        for (float off = 1.2f; off > .15f; off -= .1f)
        {
            var body = new Rect(f.x + dir * off - .4f, f.y + .02f, .8f, 1.2f);
            if (!blocks.Any(b => b.Overlaps(body))) return f + new Vector2(dir * off, 0f);
        }
        return f;
    }

    // ---------------------------------------------------------------- blocks

    static Sprite UgSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(UgBlockout + name + ".png");

    // A temporary block: plain colour, and (solid) a box collider on the Ground layer that no wall
    // cling holds (nothing in phase 1 needs Climbing Moss, and sealed sectors must stay sealed).
    static GameObject Block(Transform parent, string name, Rect r, Color colour, int order, bool solid, bool clingable = false)
    {
        var obj = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        obj.transform.SetParent(parent, false); obj.transform.position = r.center;
        var art = obj.AddComponent<SpriteRenderer>();
        art.sprite = UgSprite("UG_Block"); art.drawMode = SpriteDrawMode.Sliced; art.size = r.size;
        art.color = colour; art.sortingOrder = order;
        if (solid)
        {
            obj.AddComponent<BoxCollider2D>().size = r.size;
            obj.AddComponent<WallSurface>().allowsWallCling = clingable;
        }
        return obj;
    }

    // The pale lip on a surface Qori stands on.
    static void Lip(Transform parent, Rect r, Color colour)
    {
        var lip = new GameObject("Walkable lip").AddComponent<SpriteRenderer>();
        lip.transform.SetParent(parent, false); lip.transform.position = new Vector2(r.center.x, r.yMax - .05f);
        lip.sprite = UgSprite("UG_Block"); lip.drawMode = SpriteDrawMode.Sliced; lip.size = new Vector2(r.width, .1f);
        lip.color = colour; lip.sortingOrder = -4;
    }

    static SpriteRenderer Art(Transform parent, string name, Sprite sprite, Vector2 at, int order, Vector2 size, Color tint)
    {
        var a = new GameObject(name).AddComponent<SpriteRenderer>();
        a.transform.SetParent(parent, false); a.transform.position = at;
        a.sprite = sprite; a.sortingOrder = order; a.color = tint;
        if (size != Vector2.zero) { a.drawMode = SpriteDrawMode.Tiled; a.size = size; }
        return a;
    }

    // ---------------------------------------------------------------- water, gates

    static void UgWaters(UgRoom r, Transform parent)
    {
        var high = r.hazards.FirstOrDefault(x => x.id == "water_high"); var low = r.hazards.FirstOrDefault(x => x.id == "water_low");
        if (high == null || low == null) return;
        string flag = low.activeWhen;   // high is "not <flag>"
        var obj = new GameObject($"Water (high until {flag})");
        obj.transform.SetParent(parent, false);
        obj.AddComponent<BoxCollider2D>().isTrigger = true;
        var water = obj.AddComponent<MraUgWater>();
        water.drainedFlag = flag; water.high = R(high.rect); water.low = R(low.rect);
        var art = Art(parent, "Water art", Art("Hazards/Water_Body"), water.high.center, 18, water.high.size, new Color(1f, 1f, 1f, .8f));
        water.art = art;
    }

    static void UgGates(UgRoom r, Transform geo, Transform parent, UgTheme theme)
    {
        foreach (var g in r.gates)
        {
            if (g.id.Contains("lift_hatch")) continue;   // the heart's lifts own their hatches
            var rect = R(g.rect);
            bool never = g.opensWhenAll != null && g.opensWhenAll.Contains("NEVER");
            var obj = new GameObject((never ? "Grille (permanent) " : "Gate ") + g.id) { layer = LayerMask.NameToLayer("Ground") };
            obj.transform.SetParent(never ? geo : parent, false); obj.transform.position = rect.center;
            var solid = obj.AddComponent<BoxCollider2D>(); solid.size = rect.size;
            obj.AddComponent<WallSurface>().allowsWallCling = false;
            var bars = Art(obj.transform, "Bars (temporary art)", UgSprite("UG_Grille"), rect.center, 6, rect.size, never ? new Color(.82f, .78f, .7f) : new Color(.95f, .8f, .5f));
            if (never) continue;
            var gate = obj.AddComponent<MraGate>();
            gate.requires = Predicates(g.opensWhenAll); gate.solid = solid; gate.plainArt = new[] { bars };
        }
    }

    // ---------------------------------------------------------------- markers

    // Where a doorway marker leads: the scene and arrival id (empty scene: not built in this phase).
    static (string scene, string arrival, string[] requires, string label) Destination(UgRoom r, UgMarker m)
    {
        string target = m.target;
        if (target == "ENT_E" && EReplacesDescent) return ("MRAtlas_MR07_Summit", "return:MR07_MR08", new string[0], "Climb back up");
        if (target.StartsWith("MR08_C0"))
        {
            var side = world.ChamberById(target.Split(':')[0]);
            return (side.scene, "entry", new string[0], "Into the " + side.name);
        }
        var ent = ug.entrances.FirstOrDefault(e => e.id == target);
        if (ent != null) return (ent.scene, string.IsNullOrEmpty(ent.returnId) ? "ug:" + ent.id + ":return" : ent.returnId, new string[0], IsCave(r) ? "Back outside" : "Climb back up");
        string room = target, arrival = null, node = target;
        if (target.Contains(":"))
        {
            room = target.Split(':')[0]; arrival = target.Split(':')[1];
            node = room == "HEART" ? "HEART_" + arrival : room;
        }
        if (!UgRooms.Contains(room)) return ("", "", new string[0], "");
        // The way back: the target room's doorway that leads here (the package names some by the room's
        // marker, e.g. C3:TO_HEART, where C3's doorway is called OUT).
        var back = UgRoomById(room).markers.Where(x => x.kind == "portal");
        if (arrival == null || room != "HEART" && !back.Any(x => x.id == arrival))
            arrival = back.First(x => x.target == r.id || x.target.StartsWith(r.id + ":") || r.id == "HEART" && x.target == "HEART:" + m.id.Replace("PORT_", "")).id;
        string source = r.id == "HEART" ? "HEART_" + m.id.Replace("PORT_", "") : r.id;
        var edge = ug.edges.FirstOrDefault(e => e.source == source && e.target == node);
        string label = room == "HEART" ? "Go on through" : UgRoomById(room).name == null ? "Go through" : "On to the " + UgRoomById(room).name;
        if (r.id == "HEART" || m.id == "IN" || m.id == "ENTRY") label = "Go back";
        return (UgScene(room), arrival, Predicates(edge?.requires), label);
    }

    static void UgMarkers(UgRoom r, Transform root, Transform mech, UgTheme theme)
    {
        var f = FamilyOf(r.id == "HEART" ? "MR04" : r.region);
        foreach (var m in r.markers)
        {
            Vector2 feet = V(m.feet);
            switch (m.kind)
            {
                case "portal":
                {
                    // A skill cave's way in keeps its old arrival id ("entry", what MraChamberDoor travels to).
                    Arrival(root, IsCave(r) && m.id == "ENTRY" ? "entry" : m.id, ArrivalFeet(r, m) + new Vector2(0f, FeetToCentre + .02f), feet.x < r.size[0] * .5f ? 1f : -1f);
                    if (m.target.StartsWith("BOSS_"))
                    {
                        // Reserved: the optional guardian annex is built in the combat pass, never required.
                        var sealedDoor = Doorway(mech, "BOSS doorway (reserved, sealed)", feet, f, .45f);
                        var p0 = sealedDoor.AddComponent<MraUgPortal>();
                        p0.label = "A heavy door"; p0.requires = new[] { "ug:" + m.target.Split(':')[0] + ":open" }; p0.closedText = "Fallen stone seals this door. Not yet.";
                        Trigger(sealedDoor, new Vector2(1.2f, 1.6f), new Vector2(0f, .8f));
                        break;
                    }
                    var (scene, arrival, requires, label) = Destination(r, m);
                    var door = Doorway(mech, "Doorway " + m.id + " to " + m.target, feet, f, 1f);
                    Trigger(door, new Vector2(1.2f, 1.6f), new Vector2(0f, .8f));
                    var portal = door.AddComponent<MraUgPortal>();
                    portal.targetScene = scene; portal.arrivalId = arrival; portal.requires = requires; portal.label = label;
                    portal.closedText = "The way on is shut.";
                    break;
                }
                case "spawn":
                {
                    string sector = m.id.Replace("SPAWN_", "");
                    Arrival(root, sector, feet + new Vector2(0f, FeetToCentre + .02f), feet.x < 15f ? 1f : -1f);
                    A0TestRoomBuilder.AddCheckpoint(mech, feet, "ug:HEART:" + sector, ArtOrNull(f.shrine), 27f, null).name = "Checkpoint (sector " + sector + " arrival)";
                    var visit = new GameObject("Visit floor sector " + sector);
                    visit.transform.SetParent(mech, false); visit.transform.position = feet + new Vector2(0f, .6f);
                    visit.AddComponent<BoxCollider2D>().size = new Vector2(2.4f, 1.2f);
                    visit.AddComponent<MraUgVisit>().flag = "cw:heart:visited:" + sector;
                    break;
                }
                case "checkpoint":
                    A0TestRoomBuilder.AddCheckpoint(mech, feet, "ug:" + r.id + ":check", ArtOrNull(f.shrine), 27f, null);
                    break;
                case "control": UgControl(r, m, mech, f); break;
                case "clue": UgClue(r, m, mech); break;
                case "anchor":
                {
                    if (m.id.StartsWith("WEIGHT_")) break;   // the Seed Cradle's weights, with the cradle (UgPhase2)
                    var anchor = Anchor(mech, feet, f, m.id == "PULL" ? "Thread latch (pull ring)" : "Thread anchor " + m.id);
                    if (m.id == "PULL")
                    {
                        var latch = anchor.AddComponent<MraUgThreadLatch>(); latch.flag = "cw:B:thread_latch";
                        latch.ring = anchor.transform.Find("AnchorVisual")?.GetComponent<SpriteRenderer>();
                    }
                    break;
                }
                case "reward": UgReward(r, m, mech); break;
                case "skill": UgSkillShrine(r, m, mech); break;
                case "pod": if (m.id != "POGO_LATCH") UgPod(mech, feet, m.id); break;
                case "look":
                {
                    if (m.id.StartsWith("SHELTER_"))
                    {
                        // A wind room's sheltered pocket: where a gust returns Qori.
                        var shelter = new GameObject("Shelter " + m.id);
                        shelter.transform.SetParent(mech, false); shelter.transform.position = feet;
                        var sb = shelter.AddComponent<BoxCollider2D>(); sb.size = new Vector2(2f, 2.4f); sb.offset = new Vector2(0f, 1.2f);
                        shelter.AddComponent<MraUgShelter>().feet = feet;
                        Art(shelter.transform, "Shelter niche (temporary art)", UgSprite("UG_Block"), feet + new Vector2(0f, 1.6f), -40, new Vector2(2f, 3.2f), new Color(.1f, .1f, .12f, .55f));
                        break;
                    }
                    string sector = m.id.Replace("LOOK_", "");
                    if (sector == "E") break;
                    var look = new GameObject("Look out over the hollow (" + m.id + ")");
                    look.transform.SetParent(mech, false); look.transform.position = feet;
                    // Toward the sector's own side, clear of its lift station.
                    var box = look.AddComponent<BoxCollider2D>(); box.size = new Vector2(2.6f, 2f); box.offset = new Vector2(feet.x < 15f ? 1f : -1f, 1f);
                    var l = look.AddComponent<MraUgLook>(); l.centre = new Vector2(15f, 10f); l.halfHeight = 11f;
                    break;
                }
                case "core":
                {
                    if (EReplacesDescent) UgCentreEnding(mech, feet);
                    // Background only: the centre's existing ending stays in MR08; nothing here names it.
                    var glow = Art(mech, "Glow at the centre (background)", UgSprite("UG_Glow"), feet + new Vector2(0f, 2.2f), -57, Vector2.zero, new Color(1f, .82f, .45f, .85f));
                    glow.transform.localScale = new Vector3(3.2f, 3.2f, 1f);
                    var lamp = new GameObject("Light at the centre").AddComponent<Light2D>();
                    lamp.transform.SetParent(mech, false); lamp.transform.position = feet + new Vector2(0f, 2.5f);
                    lamp.lightType = Light2D.LightType.Point; lamp.color = new Color(1f, .8f, .5f); lamp.intensity = .9f; lamp.pointLightOuterRadius = 7f;
                    break;
                }
            }
        }
    }

    // A doorway's art (the region's accepted cave mouth), standing on `feet`.
    static GameObject Doorway(Transform parent, string name, Vector2 feet, Family f, float alpha)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false); obj.transform.position = feet;
        var sprite = Art(f.entrance);
        var mouth = Image(obj.transform, "Doorway art", sprite, feet, -30, EntranceScale * .7f);
        mouth.transform.position = feet + new Vector2(0f, -sprite.bounds.min.y * EntranceScale * .7f - .1f);
        mouth.color = new Color(1f, 1f, 1f, alpha);
        return obj;
    }

    static void UgControl(UgRoom r, UgMarker m, Transform mech, Family f)
    {
        Vector2 feet = V(m.feet);
        MraUgSwitch Valve(string flag, string label, string doneText, string[] requires = null, string refused = "")
        {
            var obj = new GameObject("Valve " + m.id + " (" + flag + ")");
            obj.transform.SetParent(mech, false); obj.transform.position = feet;
            Trigger(obj, new Vector2(1.6f, 2.4f), new Vector2(0f, 1.2f));
            var s = obj.AddComponent<MraUgSwitch>();
            s.flag = flag; s.label = label; s.doneText = doneText; s.requires = requires ?? new string[0]; s.refusedText = refused;
            s.wheel = Image(obj.transform, "Wheel", Art("Mechanics/Mill_Wheel"), feet + new Vector2(0f, .95f), -11, .34f).transform;
            return s;
        }
        switch (r.id + ":" + m.id)
        {
            case "A1:VALVE": Valve("cw:A:drain", "Turn the drain valve", "Water drains from the basin. Somewhere ahead, a grate lifts."); break;
            case "A2:V1": Valve("cw:A:basin_drained", "Open the basin valve", "The basin empties to a trickle."); break;
            case "A2:V2":
                Valve("cw:A:sluice_solved", "Turn the sluice wheel", "A walkway rises behind you, and the far grate lifts.",
                    new[] { "cw:A:basin_drained" }, "The sluice won't turn while the basin behind it is full.");
                break;
            case "B2:ROOT":
            {
                var s = Valve("cw:B:maze_return", "Pull the root latch", "Far below, a gate grinds open.");
                Symbol(s.transform, "UG_Symbol_Root", feet + new Vector2(0f, 3.1f));
                break;
            }
            case "A3:RETURN":
            case "B3:RETURN":
                ReturnShaft(r, mech, feet);
                break;
            case "B3:BOWL1": case "B3:BOWL2": case "B3:BOWL3": break;   // UgBowls, with the sequence
            case "HEART:LATCH_AB": case "HEART:CALL_AB_UPPER": case "HEART:LATCH_CD": case "HEART:CALL_CD_UPPER": break;  // UgHeart, with the lifts
            // The skill caves' and route C's "return lifts": not built (see UgPhase2 and the adjustments).
            case "HEART:E_RETURN":
                if (!EReplacesDescent) goto default;
                {
                    // Back up to the E arrival with a fade: never a way to any other sector.
                    var obj = new GameObject("Back up to the E doorway");
                    obj.transform.SetParent(mech, false); obj.transform.position = feet;
                    Trigger(obj, new Vector2(1.2f, 2f), new Vector2(0f, 1f));
                    var p = obj.AddComponent<MraUgPortal>(); p.targetScene = UgScene("HEART"); p.arrivalId = "E"; p.label = "Climb back to the doorway"; p.sound = "";
                    Image(obj.transform, "Rope ladder", Art("Mechanics/Mill_Wheel"), feet + new Vector2(0f, .9f), -11, .3f);
                }
                break;
            case "E2:RETURN_LIFT": case "E3:RETURN_LIFT": break;   // UgPhase2: the call lift to the entry shelf
            case "MR01_C02:RETURN": case "MR02_C03:RETURN_LIFT": case "MR03_C02:RETURN_LIFT": case "C1:RETURN_LIFT": case "C3:RETURN_LIFT":
            case "D2:RETURN": case "D3:RETURN": case "MR06_C01:RETURN": case "MR06_C01:LESSON_LIFT": break;
            default:
            {
                // A control in a sector not built for play yet (the C/D latches, the centre's return):
                // its place is marked, nothing more.
                Art(mech, "Unbuilt control " + m.id, UgSprite("UG_Block"), feet + new Vector2(0f, .5f), -12, new Vector2(.5f, 1f), new Color(.6f, .55f, .45f, .6f));
                break;
            }
        }
        if (r.id == "B3" && m.id == "BOWL3") UgBowls(r, mech);
    }

    static SpriteRenderer Symbol(Transform parent, string sprite, Vector2 at, float scale = .9f)
    {
        var s = Art(parent, "Carving " + sprite.Replace("UG_Symbol_", ""), UgSprite(sprite), at, -9, Vector2.zero, new Color(.92f, .88f, .8f));
        s.transform.localScale = new Vector3(scale, scale, 1f);
        return s;
    }

    static readonly Dictionary<string, (string symbol, string name, string words)> Landmarks = new Dictionary<string, (string, string, string)>
    {
        { "BELL", ("UG_Symbol_Bell", "Bell", "One ring is carved here: the Bell. First.") },
        { "FOSSIL", ("UG_Symbol_Fossil", "Fossil", "Two spirals are carved here: the Fossil. Second.") },
        { "ROOT", ("UG_Symbol_Root", "Root", "Three branches are carved here: the Root. Third.") },
        { "LANDMARK", ("UG_Symbol_Fossil", "Worn carving", "A worn carving: a path, turning and turning, going down.") },
    };

    static void UgClue(UgRoom r, UgMarker m, Transform mech)
    {
        Vector2 feet = V(m.feet);
        if (m.id == "REPLAY") return;   // UgBowls
        if (!Landmarks.TryGetValue(m.id, out var lm)) return;
        var obj = new GameObject("Landmark " + lm.name + " (" + r.id + ")");
        obj.transform.SetParent(mech, false); obj.transform.position = feet;
        Trigger(obj, new Vector2(2f, 2.4f), new Vector2(0f, 1.2f));
        var clue = obj.AddComponent<MraUgClue>();
        clue.text = lm.words; clue.symbol = Symbol(obj.transform, lm.symbol, feet + new Vector2(0f, 1.6f), 1.1f);
        clue.promptHeight = 3f;
    }

    static void UgBowls(UgRoom r, Transform mech)
    {
        var seqObj = new GameObject("Resonator bowls (Bell, Fossil, Root)");
        seqObj.transform.SetParent(mech, false);
        var seq = seqObj.AddComponent<MraUgSequence>(); seq.solvedFlag = "cw:B:bowls_solved"; seq.order = new[] { 0, 1, 2 };
        string[] keys = { "BELL", "FOSSIL", "ROOT" };
        for (int i = 0; i < 3; i++)
        {
            var m = r.Marker("BOWL" + (i + 1)); Vector2 feet = V(m.feet);
            var obj = new GameObject("Bowl " + (i + 1) + " (" + Landmarks[keys[i]].name + ")");
            obj.transform.SetParent(seqObj.transform, false); obj.transform.position = feet;
            Trigger(obj, new Vector2(1.6f, 2.2f), new Vector2(0f, 1.1f));
            var bowl = obj.AddComponent<MraUgBowl>(); bowl.sequence = seq; bowl.label = "Strike the " + Landmarks[keys[i]].name + " bowl";
            bowl.image = Art(obj.transform, "Bowl (temporary art)", UgSprite("UG_Glow"), feet + new Vector2(0f, .45f), -10, Vector2.zero, Color.white);
            bowl.image.transform.localScale = new Vector3(1.1f, .55f, 1f);
            bowl.symbol = Symbol(obj.transform, Landmarks[keys[i]].symbol, feet + new Vector2(0f, 1.7f), .8f);
            seq.bowls.Add(bowl);
        }
        var rep = r.Marker("REPLAY");
        var stone = new GameObject("Replay stone");
        stone.transform.SetParent(seqObj.transform, false); stone.transform.position = V(rep.feet);
        Trigger(stone, new Vector2(1.4f, 2.2f), new Vector2(0f, 1.1f));
        stone.AddComponent<MraUgReplay>().sequence = seq;
        Image(stone.transform, "Stone", Art("Props/Collectible_LoreStone"), V(rep.feet) + new Vector2(0f, .5f), -10, 1.2f);
        // Its gate: the resonator door (MraGate with the solved flag) is built from the room's gates.
    }

    static void UgReward(UgRoom r, UgMarker m, Transform mech)
    {
        Vector2 at = V(m.feet) + new Vector2(0f, .7f);
        MraReward Make(string id, MraReward.Kind kind, string title, string text, Sprite sprite, float scale)
        {
            var obj = new GameObject("Reward " + m.id + " (" + id + ")");
            obj.transform.SetParent(mech, false); obj.transform.position = at;
            obj.AddComponent<CircleCollider2D>().radius = .6f;
            var rw = obj.AddComponent<MraReward>();
            rw.rewardId = id; rw.kind = kind; rw.title = title; rw.text = text; rw.amber = 25;
            rw.image = Image(obj.transform, "Art", sprite, at, -8, scale);
            return rw;
        }
        switch (r.id + ":" + m.id)
        {
            // The heart's rewards are placeholders: the design gives A a heart seed and B a song shell,
            // but which ones (the live registry's limited heart seeds, and a song shell needs a track)
            // is an allocation still to be made, so phase 1 doesn't add either.
            case "HEART:REWARD_A": Make("ug:HEART:A:reward", MraReward.Kind.Find, "A small alcove (placeholder)", "A heart seed will rest here.", Art("Props/Pickup_HeartSeed"), .9f).image.color = new Color(1f, 1f, 1f, .55f); break;
            case "HEART:REWARD_B": Make("ug:HEART:B:reward", MraReward.Kind.Find, "A small alcove (placeholder)", "A song shell will rest here.", Art("Props/Pickup_SongShell"), .9f).image.color = new Color(1f, 1f, 1f, .55f); break;
            case "B2:AMBER": Make("ug:B2:amber", MraReward.Kind.Amber, "Amber", "", Art("Props/Pickup_Amber_Medium"), 1f); break;
            // Sector C's rare find is a plain find (no limited registry); D's song shell waits for a track, like B's.
            case "HEART:REWARD_C": Make("ug:HEART:C:reward", MraReward.Kind.Find, "Something old", "Something old and well made, kept dry for a long time.", Art("Props/Pickup_SapVial"), 1f); break;
            case "HEART:REWARD_D": Make("ug:HEART:D:reward", MraReward.Kind.Find, "A small alcove (placeholder)", "A song shell will rest here.", Art("Props/Pickup_SongShell"), .9f).image.color = new Color(1f, 1f, 1f, .55f); break;
            default:
                if (!IsCave(r) || m.id != "REWARD") break;
                // The cave's existing reward, unchanged (id, kind, title as MraChamberBuilder made it),
                // reachable once the lesson is done.
                var c = world.ChamberById(r.id);
                var (kind, sprite, scale) = c.reward switch
                {
                    "song-shell" => (MraReward.Kind.SongShell, Art("Props/Pickup_SongShell"), 1f),
                    "heart-seed" => (MraReward.Kind.HeartSeed, Art("Props/Pickup_HeartSeed"), 1.4f),
                    "amber" => (MraReward.Kind.Amber, Art("Props/Pickup_Amber_Medium"), 1f),
                    _ => (MraReward.Kind.Find, Art("Props/Pickup_SapVial"), 1f),
                };
                var rw = Make(c.rewardId, kind, c.name, "", sprite, scale);
                rw.gameObject.AddComponent<MraUgRewardGate>().flag = m.requires.FirstOrDefault() ?? ("ug:" + r.id + ":lesson_complete");
                break;
        }
    }

    // ---------------------------------------------------------------- phase 2: skill caves, routes C and D

    // The skill's relic on its accessible floor pedestal, before the lesson (the accepted shrine art and
    // the existing AbilityShrine: it unlocks and saves the relic, and stands empty if Qori has it).
    static void UgSkillShrine(UgRoom r, UgMarker m, Transform mech)
    {
        var skill = ug.skills.First(k => k.cave == r.id);
        var ability = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Resources/Relics/" + RelicAsset[skill.ability] + ".asset");
        Vector2 p = V(m.feet);
        var shrine = new GameObject("Shrine " + ability.displayName + " (moved from " + skill.oldSurfaceGrant + ")").AddComponent<AbilityShrine>();
        shrine.transform.SetParent(mech, false); shrine.transform.position = p;
        shrine.ability = ability;
        var pedestal = Art("Props/Shrine_Ability");
        Image(shrine.transform, "Pedestal", pedestal, p + new Vector2(0f, -pedestal.bounds.min.y - .05f), -15);
        shrine.relic = Image(shrine.transform, "Relic", ability.relic, p + new Vector2(0f, shrine.hoverHeight), -13);
        Trigger(shrine.gameObject, new Vector2(1.4f, 2.6f), new Vector2(0f, 1.3f));
    }

    static MraPod UgPod(Transform mech, Vector2 feet, string id)
    {
        var obj = new GameObject("Bounce pod " + id);
        obj.transform.SetParent(mech, false); obj.transform.position = feet + new Vector2(0f, .55f);
        obj.AddComponent<CircleCollider2D>().radius = .55f;
        var pod = obj.AddComponent<MraPod>();
        pod.art = Image(obj.transform, "Pod", Art("Enemies/Carrier_SeedPod_A2"), obj.transform.position, -11, .45f).transform;
        return pod;
    }

    // Gusts and vents (MraUgGust, staggered so neighbouring lanes don't pulse together) and soft catches.
    static void UgHazards(UgRoom r, Transform mech)
    {
        int lane = 0;
        foreach (var hz in r.hazards)
        {
            var rect = R(hz.rect);
            if (hz.id.StartsWith("gust_") || hz.id.StartsWith("wind_"))
            {
                var obj = new GameObject("Gust " + hz.id + " (tell 1.2 s, pulse 1.0 s, calm 2.0 s)");
                obj.transform.SetParent(mech, false); obj.transform.position = rect.center;
                obj.AddComponent<BoxCollider2D>().size = rect.size;
                var gust = obj.AddComponent<MraUgGust>();
                gust.offset = lane++ * 1.4f;
                gust.art = Art(obj.transform, "Wind (temporary art)", UgSprite("UG_Block"), rect.center, 12, rect.size, new Color(.8f, .9f, 1f, .12f));
            }
            else if (hz.id == "soft_catch")
            {
                var launch = r.platforms.First(x => x.id == "launch");
                var obj = new GameObject("Soft catch (back to the launch)");
                obj.transform.SetParent(mech, false);
                var strip = new Rect(rect.x, rect.y, rect.width, .6f);   // the strip plus a little above, so a landing touches it
                obj.transform.position = strip.center;
                obj.AddComponent<BoxCollider2D>().size = strip.size;
                obj.AddComponent<MraUgCatch>().launchFeet = new Vector2(R(launch.rect).xMin + 3.5f, R(launch.rect).yMax);
                Art(obj.transform, "Catch net (temporary art)", UgSprite("UG_Grille"), new Vector2(rect.center.x, rect.yMin + .15f), -4, new Vector2(rect.width, .3f), new Color(.95f, .85f, .55f, .8f));
            }
        }
    }

    // A route's way back up from its last room (C3, D3 and the A3/B3 shafts): opened from below first.
    static void ReturnShaft(UgRoom r, Transform mech, Vector2 feet)
    {
        string route = r.id.Substring(0, 1);
        var ent = ug.entrances.First(e => e.room == route + "1");
        string arrivalId = route == "E" && EReplacesDescent ? "return:MR07_MR08" : "ug:" + ent.id + ":return";
        var obj = new GameObject("Way back up to " + ent.id);
        obj.transform.SetParent(mech, false); obj.transform.position = feet;
        Trigger(obj, new Vector2(1.4f, 2f), new Vector2(0f, 1f));
        Art(obj.transform, "Shaft (temporary art)", UgSprite("UG_Block"), feet + new Vector2(0f, 3.5f), -31, new Vector2(1.4f, 7f), new Color(.08f, .07f, .06f));
        var p = obj.AddComponent<MraUgPortal>();
        p.targetScene = ent.scene; p.arrivalId = arrivalId; p.label = "Climb back up";
        p.unlockFlag = "cw:return:" + route; p.unlockLabel = "Lower the old ladder";
        p.unlockText = "A ladder rattles down the shaft. It climbs back to where this way began.";
        p.sound = "Door_Exit";
    }

    // A plain call lift (no flags): a cabin between `bottom` (its centre on the floor's top) and the
    // height of `topY` (its deck flush), with a station beside each stop.
    static MraUgHeartLift CallLift(Transform mech, string name, Rect deck, float topY, Vector2 lowerStation, Vector2 upperStation)
    {
        var root = Group(mech, name);
        var cabin = new GameObject("Cabin") { layer = LayerMask.NameToLayer("Ground") };
        cabin.transform.SetParent(root, false); cabin.transform.position = deck.center;
        var body = cabin.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        cabin.AddComponent<BoxCollider2D>().size = deck.size;
        cabin.AddComponent<WallSurface>().allowsWallCling = false;
        var art = Art(cabin.transform, "Cabin deck", UgSprite("UG_Block"), deck.center, -4, Vector2.zero, new Color(.62f, .5f, .3f));
        art.drawMode = SpriteDrawMode.Sliced; art.size = deck.size;
        Lip(cabin.transform, deck, new Color(1f, .85f, .5f));
        Art(root, "Lift shaft rope (temporary art)", UgSprite("UG_Block"), new Vector2(deck.center.x, (deck.yMax + topY) * .5f), -41, new Vector2(.08f, topY - deck.yMax + 1f), new Color(.5f, .42f, .3f));
        var lift = root.gameObject.AddComponent<MraUgHeartLift>();
        lift.visitFlags = new string[0]; lift.linkFlag = "";
        lift.cabin = body; lift.lower = deck.center; lift.upper = deck.center + new Vector2(0f, topY - deck.yMax); lift.speed = 3f;
        foreach (var (feet, up) in new[] { (lowerStation, false), (upperStation, true) })
        {
            var obj = new GameObject("Lift station " + (up ? "top" : "bottom"));
            obj.transform.SetParent(root, false); obj.transform.position = feet;
            float left = Mathf.Min(deck.xMin, feet.x - .5f), right = Mathf.Max(deck.xMax, feet.x + .5f);
            var box = obj.AddComponent<BoxCollider2D>(); box.size = new Vector2(right - left, 2f); box.offset = new Vector2((left + right) * .5f - feet.x, 1f);
            var station = obj.AddComponent<MraUgLiftStation>(); station.lift = lift; station.upperStop = up;
            Image(obj.transform, "Lever", Art("Mechanics/Mill_Wheel"), feet + new Vector2(0f, .9f), -11, .3f);
        }
        return lift;
    }

    static MraUgLesson LessonZone(Transform mech, string cave, Rect zone, string text, bool dash = false, bool glide = false)
    {
        var obj = new GameObject("Lesson target (" + cave + ")");
        obj.transform.SetParent(mech, false); obj.transform.position = zone.center;
        Trigger(obj, zone.size, Vector2.zero);
        var lesson = obj.AddComponent<MraUgLesson>();
        lesson.kind = MraUgLesson.Kind.Zone; lesson.needsDash = dash; lesson.needsGlide = glide;
        lesson.lessonFlag = "ug:" + cave + ":lesson_complete"; lesson.solveFlag = "ug:" + cave + ":solved"; lesson.doneText = text;
        return lesson;
    }

    static ThreadAnchor AnchorNamed(Transform mech, string id) =>
        mech.GetComponentsInChildren<ThreadAnchor>().First(a => a.name == "Thread anchor " + id);

    static void UgPhase2(UgRoom r, Transform root, Transform mech, UgTheme theme)
    {
        Rect P(string id) => R(r.platforms.First(x => x.id == id).rect);
        switch (r.id)
        {
            case "MR01_C02":
                // The lesson: up a moss wall to the reward shelf. The way back is the drop to the floor
                // (no fall damage exists; the left ledges would block a cabin): see the adjustments.
                LessonZone(mech, r.id, new Rect(29f, 15f, 6f, 1.5f), "The moss holds. Climbing comes easy now.");
                break;
            case "MR02_C03":
            {
                var lesson = mech.gameObject.AddComponent<MraUgLesson>();
                lesson.kind = MraUgLesson.Kind.Anchor; lesson.anchor = AnchorNamed(mech, "ANCHOR_4");
                lesson.lessonFlag = "ug:MR02_C03:lesson_complete"; lesson.solveFlag = "ug:MR02_C03:solved";
                lesson.doneText = "The far bowl hums as the thread catches. A niche opens.";
                break;
            }
            case "MR03_C02":
            {
                var lesson = mech.gameObject.AddComponent<MraUgLesson>();
                lesson.kind = MraUgLesson.Kind.Pod; lesson.pod = mech.GetComponentsInChildren<MraPod>().First(x => x.name.EndsWith("POD_3"));
                lesson.lessonFlag = "ug:MR03_C02:lesson_complete"; lesson.solveFlag = "ug:MR03_C02:solved";
                lesson.doneText = "The last pod springs you high. The grain bin's shelf is in reach.";
                break;
            }
            case "MR05_C04":
                LessonZone(mech, r.id, new Rect(33f, 2f, 6f, 2f), "Through the choir's gusts.", dash: true);
                break;
            case "MR06_C01":
            {
                LessonZone(mech, r.id, new Rect(36f, 4f, 7f, 1.5f), "A long, soft glide across the nest.", glide: true);
                // One call lift to the launch (the package's RETURN and LESSON_LIFT, merged), its cabin
                // against the launch's open end so Qori steps straight across.
                var launch = P("launch");
                CallLift(mech, "Lift to the launch", new Rect(launch.xMax, 1.5f, 2f, .5f), launch.yMax, new Vector2(10.5f, 2f), new Vector2(launch.xMax - 1f, launch.yMax));
                break;
            }
            case "C2":
            {
                // The Seed Cradle: two thread weights on a balance; the latch pod at its foot.
                var f = FamilyOf(r.region);
                var w0 = Anchor(mech, V(r.Marker("WEIGHT_0").feet), f, "Weight (left)").GetComponent<ThreadAnchor>();
                var w1 = Anchor(mech, V(r.Marker("WEIGHT_1").feet), f, "Weight (right)").GetComponent<ThreadAnchor>();
                var cradle = new GameObject("Seed cradle (left weight down = detent)").AddComponent<MraUgCradle>();
                cradle.transform.SetParent(mech, false); cradle.transform.position = V(r.Marker("CRADLE").feet);
                cradle.left = w0; cradle.right = w1; cradle.detentFlag = "ug:C2:detent"; cradle.solvedFlag = "ug:C2:solved";
                var beam = Art(cradle.transform, "Balance beam (temporary art)", UgSprite("UG_Block"), V(r.Marker("CRADLE").feet) + new Vector2(0f, 2.4f), -9, Vector2.zero, new Color(.7f, .55f, .32f));
                beam.drawMode = SpriteDrawMode.Sliced; beam.size = new Vector2(5f, .35f);
                cradle.beam = beam.transform;
                Art(cradle.transform, "Balance post (temporary art)", UgSprite("UG_Block"), V(r.Marker("CRADLE").feet) + new Vector2(0f, 1.2f), -10, new Vector2(.4f, 2.4f), new Color(.5f, .4f, .25f));
                cradle.latchPod = UgPod(mech, V(r.Marker("POGO_LATCH").feet), "POGO_LATCH");
                break;
            }
            case "C3": ReturnShaft(r, mech, new Vector2(23.6f, 2f)); break;   // not in the package's room: added for cw:return:C
            case "D2":
                // Each climbing ledge is a rest: a vent's pulse returns Qori to the last one he stood on.
                foreach (var ledge in r.platforms.Where(x => x.id.StartsWith("ledge_") || x.id == "top_reward"))
                {
                    var rect = R(ledge.rect);
                    var obj = new GameObject("Rest " + ledge.id);
                    obj.transform.SetParent(mech, false); obj.transform.position = new Vector2(rect.center.x, rect.yMax + .6f);
                    obj.AddComponent<BoxCollider2D>().size = new Vector2(rect.width, 1.2f);
                    obj.AddComponent<MraUgShelter>().feet = new Vector2(rect.center.x, rect.yMax);
                }
                break;
            case "D3": ReturnShaft(r, mech, new Vector2(34.2f, 4f)); break;   // not in the package's room: added for cw:return:D
            case "E2":
            case "E3":
            {
                // "Always-callable return to top entry": a call lift beside the entry shelf's open end.
                var top = P("descent_0");
                CallLift(mech, "Lift back to the entry shelf", new Rect(top.xMax, 1.5f, 2f, .5f), top.yMax, new Vector2(6f, 2f), new Vector2(top.xMax - 1f, top.yMax));
                if (r.id == "E3" && EReplacesDescent) ReturnShaft(r, mech, new Vector2(16f, 2f));   // cw:return:E: back to the Summit's way down
                break;
            }
        }
    }

    // The existing careful ending, moved to the centre (route E replaces the Descent's walk): the same
    // MraEnding with the same knot, stills and captions as MraWorldBuilder.Ending, and the same healed
    // variant afterwards. Only sector E reaches it.
    static void UgCentreEnding(Transform mech, Vector2 feet)
    {
        var obj = new GameObject("Careful ending (moved from the Descent)");
        obj.transform.SetParent(mech, false); obj.transform.position = feet;
        Trigger(obj, new Vector2(2.4f, 3f), new Vector2(0f, 1.5f));
        var ending = obj.AddComponent<MraEnding>();
        ending.knot = "heart";
        ending.stills = new[] { ArtOrNull("Cinematics/Spoilers/Ending_Careful_01_HeadLift"), ArtOrNull("Cinematics/Spoilers/Ending_Careful_02_Lap") };
        ending.captions = new[] { "", "" };
        Image(obj.transform, "Final knot", Art("WorldSystems/Wakeknot_Dormant"), feet + new Vector2(0f, 1.7f), -13, .7f);
        var after = new GameObject("After the ending (healed)").AddComponent<MraVariant>();
        after.transform.SetParent(obj.transform, false); after.requires = new[] { "flag:" + MraState.EndingFlag };
        Image(after.transform, "Knot (healed)", Art("WorldSystems/Wakeknot_Awake"), feet + new Vector2(0f, 1.7f), -12, .7f);
        AreaRoomStyle.Freshen(after);
    }

    // ---------------------------------------------------------------- the Twin Sluices' walkway

    static void UgReturnWalk(UgRoom r, Transform mech, UgTheme theme)
    {
        var p = r.platforms.First(x => x.kind == "moving");
        var top = R(p.rect);
        var obj = new GameObject("Return walkway (rises with " + p.condition + ")") { layer = LayerMask.NameToLayer("Ground") };
        obj.transform.SetParent(mech, false);
        Vector2 raised = top.center, lowered = raised + new Vector2(0f, -1.5f);   // the package's fromRect: one and a half units down, under the water
        obj.transform.position = lowered;
        var body = obj.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        obj.AddComponent<BoxCollider2D>().size = top.size;
        obj.AddComponent<WallSurface>().allowsWallCling = false;
        var lift = obj.AddComponent<MraUgRaiseLift>();
        lift.flag = p.condition; lift.lowered = lowered; lift.raised = raised;
        lift.art = Art(obj.transform, "Walkway", UgSprite("UG_Block"), lowered, -5, Vector2.zero, theme.deck);
        lift.art.drawMode = SpriteDrawMode.Sliced; lift.art.size = top.size;
        Lip(obj.transform, new Rect(lowered - top.size * .5f, top.size), theme.lip);
    }

    // ---------------------------------------------------------------- the heart

    static void UgHeart(UgRoom r, Transform root, Transform mech, UgTheme theme)
    {
        UgLift(r, mech, theme, "AB", "LATCH_AB", "CALL_AB_UPPER", true);
        UgLift(r, mech, theme, "CD", "LATCH_CD", "CALL_CD_UPPER", true);
        // A note in the scene for whoever opens it.
        var note = new GameObject("README - phase 1 blockout");
        note.transform.SetParent(root, false);
        note.AddComponent<MraUgSceneNote>().text =
            "Phase 1 greybox of the shared heart. Five sectors share this scene: A (lower left, from A3) and B (upper left, from B3) are playable; " +
            "C (lower right), D (upper right) and E (the central column and its descent to the centre) are built to be seen, not reached. " +
            "Ribs carry permanent grilles. The A/B lift needs cw:heart:visited:A, cw:heart:visited:B and its latch cw:heart:link:AB.";
    }

    static void UgLift(UgRoom r, Transform mech, UgTheme theme, string id, string lowerId, string upperId, bool working)
    {
        var deckItem = r.platforms.First(p => p.id == id + "_lift");
        var hatchItem = r.gates.First(g => g.id == id + "_lift_hatch");
        Rect deck = R(deckItem.rect), hatchRect = R(hatchItem.rect);
        Vector2 lower = deck.center, upper = lower + new Vector2(0f, hatchRect.yMax - deck.yMax);   // the deck's top flush with each sector's floor
        var root = Group(mech, "Lift " + id + (working ? "" : " (sector not built: sealed)"));

        var cabin = new GameObject("Lift cabin " + id) { layer = LayerMask.NameToLayer("Ground") };
        cabin.transform.SetParent(root, false); cabin.transform.position = lower;
        var cabinBody = cabin.AddComponent<Rigidbody2D>(); cabinBody.bodyType = RigidbodyType2D.Kinematic;
        cabin.AddComponent<BoxCollider2D>().size = deck.size;
        cabin.AddComponent<WallSurface>().allowsWallCling = false;
        var cabinArt = Art(cabin.transform, "Cabin deck", UgSprite("UG_Block"), lower, -4, Vector2.zero, new Color(.62f, .5f, .3f));
        cabinArt.drawMode = SpriteDrawMode.Sliced; cabinArt.size = deck.size;
        Lip(cabin.transform, deck, new Color(1f, .85f, .5f));

        var hatch = new GameObject("Hatch " + id + " (sealed until both sectors are reached and latched)") { layer = LayerMask.NameToLayer("Ground") };
        hatch.transform.SetParent(root, false); hatch.transform.position = hatchRect.center;
        var hatchCol = hatch.AddComponent<BoxCollider2D>(); hatchCol.size = hatchRect.size;
        hatch.AddComponent<WallSurface>().allowsWallCling = false;
        var hatchArt = Art(hatch.transform, "Hatch bars", UgSprite("UG_Grille"), hatchRect.center, 6, hatchRect.size, new Color(.95f, .78f, .45f));
        var hatchRock = Block(hatch.transform, "Hatch plug", hatchRect, theme.rock * .9f, -7, false);
        if (!working) return;

        var guard = new GameObject("Guard plate " + id) { layer = LayerMask.NameToLayer("Ground") };
        guard.transform.SetParent(root, false);
        var guardRect = new Rect(hatchRect.xMin, hatchRect.yMax - .4f, hatchRect.width, .4f);
        guard.transform.position = guardRect.center;
        var guardCol = guard.AddComponent<BoxCollider2D>(); guardCol.size = guardRect.size;
        var guardArt = Art(guard.transform, "Guard plate", UgSprite("UG_Grille"), guardRect.center, -3, guardRect.size, new Color(.95f, .85f, .6f));

        var lift = root.gameObject.AddComponent<MraUgHeartLift>();
        string a = id.Substring(0, 1), b = id.Substring(1, 1);
        lift.visitFlags = new[] { "cw:heart:visited:" + a, "cw:heart:visited:" + b }; lift.linkFlag = "cw:heart:link:" + id;
        lift.cabin = cabinBody; lift.lower = lower; lift.upper = upper; lift.speed = 2f;
        lift.hatch = hatchCol; lift.hatchArt = new[] { hatchArt, hatchRock.GetComponent<SpriteRenderer>() };
        lift.guard = guardCol; lift.guardArt = guardArt;

        foreach (var (mid, up) in new[] { (lowerId, false), (upperId, true) })
        {
            var m = r.Marker(mid); Vector2 feet = V(m.feet);
            var obj = new GameObject("Lift station " + mid);
            obj.transform.SetParent(root, false); obj.transform.position = feet;
            // From the cabin's far edge to just past the marker: pressing Up on the cabin works too.
            float left = Mathf.Min(deck.xMin, feet.x - .5f), right = Mathf.Max(deck.xMax, feet.x + .5f);
            var box = obj.AddComponent<BoxCollider2D>(); box.size = new Vector2(right - left, 2f); box.offset = new Vector2((left + right) * .5f - feet.x, 1f);
            var station = obj.AddComponent<MraUgLiftStation>(); station.lift = lift; station.upperStop = up;
            Image(obj.transform, "Lever", Art("Mechanics/Mill_Wheel"), feet + new Vector2(0f, .9f), -11, .3f);
        }
    }

    // ---------------------------------------------------------------- surface entrances

    static readonly Dictionary<string, (string label, string found)> EntranceWords = new Dictionary<string, (string, string)>
    {
        { "ENT_A", ("Climb down into the old drain", "Found: an old drain") },
        { "ENT_B", ("Go in under the arch", "Found: a service arch") },
        { "ENT_C", ("Squeeze into the root cleft", "Found: a root cleft") },
        { "ENT_D", ("Duck into the sheltered ledge", "Found: a sheltered ledge") },
        { "ENT_E", ("Go down beside the old way", "Found: a narrow way down") },
    };

    static void UgSurfaceEntrances()
    {
        var set = ScriptableObject.CreateInstance<MraUgSurfaceSet>();
        var list = new List<MraUgSurfaceSet.Entrance>();
        var moves = new List<MraUgSurfaceSet.CaveMove>();
        foreach (var e in ug.entrances.Where(e => System.Array.IndexOf(UgCaves, e.room) >= 0))
        {
            Vector2 at = SurfaceSurvey.TryGetValue(e.id, out var sv) ? sv : V(e.feet);
            var skill = ug.skills.First(k => k.cave == e.room);
            moves.Add(new MraUgSurfaceSet.CaveMove { chamberId = e.room, scene = e.scene, returnId = e.returnId, ability = skill.ability,
                feet = at, returnFeet = at + (V(e.returnFeet) - V(e.feet)), feetToCentre = FeetToCentre + .02f });
        }
        // Main project retains its newer caves and surface skill grants. Archived phase-2 migration is not applied.
        set.caveMoves = new MraUgSurfaceSet.CaveMove[0];
        set.redirects = EReplacesDescent ? new[]
        {
            new MraUgSurfaceSet.Redirect { scene = "MRAtlas_MR07_Summit", fromScene = "MRAtlas_MR08_Descent", toScene = UgScene("E1"), toArrival = "ENTRY", label = "The way down" },
            new MraUgSurfaceSet.Redirect { scene = "MRAtlas_MR08_C01", fromScene = "MRAtlas_MR08_Descent", toScene = UgScene("E2"), toArrival = "SIDE_C01", label = "Quiet Descent" },
            new MraUgSurfaceSet.Redirect { scene = "MRAtlas_MR08_C02", fromScene = "MRAtlas_MR08_Descent", toScene = UgScene("E3"), toArrival = "SIDE_C02", label = "Core Threshold" },
        } : new MraUgSurfaceSet.Redirect[0];
        foreach (var e in ug.entrances.Where(e => System.Array.IndexOf(UgCaves, e.room) < 0 && !(e.id == "ENT_E" && EReplacesDescent)))
        {
            var region = world.RegionByScene(e.scene);
            var f = FamilyOf(region.id);
            Vector2 feet = SurfaceSurvey.TryGetValue(e.id, out var s) ? s : V(e.feet);
            Vector2 back = feet + (V(e.returnFeet) - V(e.feet));
            var sprite = Art(f.entrance);
            list.Add(new MraUgSurfaceSet.Entrance
            {
                id = e.id, scene = e.scene, targetScene = UgScene(e.room), targetArrival = UgRoomById(e.room).Marker("ENTRY") != null ? "ENTRY" : "IN",
                feet = feet, returnFeet = back, returnId = "ug:" + e.id + ":return", returnFacing = 1f, feetToCentre = FeetToCentre + .02f,
                label = EntranceWords[e.id].label,
                requires = Predicates(e.requires), closedText = "It's a long way into the dark.",
                discoveredFlag = "mra:UG_" + e.id + ":discovered", discoveredText = EntranceWords[e.id].found,
                art = sprite, artScale = EntranceScale * .8f, artOffset = new Vector2(0f, -sprite.bounds.min.y * EntranceScale * .8f - .12f), artOrder = -30,
                artTint = e.id == "ENT_A" ? new Color(.78f, .84f, .9f) : Color.white,
            });
        }
        set.entrances = list.ToArray();
        string path = Folder + "Resources/MRA/UgSurfaceEntrances.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(set, path);
    }

    // ---------------------------------------------------------------- temporary art

    // Plain shapes for the greybox (UG_*.png in Underground/Blockout), drawn here so they are
    // reproducible: a white block, grille bars, a soft glow, and the three landmark carvings.
    static void UgBlockoutArt()
    {
        Directory.CreateDirectory(UgBlockout);
        Write("UG_Block", 16, 16, (x, y) => Color.white, 16f, SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
        Write("UG_Grille", 32, 32, (x, y) =>
        {
            bool bar = x % 16 < 4 || y % 32 < 3;
            return bar ? Color.white : new Color(1f, 1f, 1f, 0f);
        }, 32f, SpriteMeshType.FullRect, Vector4.zero);
        Write("UG_Glow", 128, 128, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 64f;
            return new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d));
        }, 64f, SpriteMeshType.FullRect, Vector4.zero);
        Color Ink(bool on) => on ? Color.white : new Color(1f, 1f, 1f, 0f);
        bool Ring(float x, float y, float cx, float cy, float r, float t) => Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) - r) < t;
        bool Spiral(float x, float y, float cx, float cy, float scale)
        {
            Vector2 d = new Vector2(x - cx, y - cy); float r = d.magnitude / scale; float a = Mathf.Atan2(d.y, d.x);
            if (r > 1f) return false;
            float turns = (r * 3f - a / (2f * Mathf.PI)); return Mathf.Abs(turns - Mathf.Round(turns)) < .14f;
        }
        bool Line(float x, float y, Vector2 a, Vector2 b, float t)
        {
            Vector2 p = new Vector2(x, y), ab = b - a; float k = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * k) < t;
        }
        Write("UG_Symbol_Bell", 96, 96, (x, y) => Ink(Ring(x, y, 48f, 48f, 30f, 5f)), 96f, SpriteMeshType.FullRect, Vector4.zero);
        Write("UG_Symbol_Fossil", 96, 96, (x, y) => Ink(Spiral(x, y, 26f, 48f, 21f) || Spiral(x, y, 70f, 48f, 21f)), 96f, SpriteMeshType.FullRect, Vector4.zero);
        Write("UG_Symbol_Root", 96, 96, (x, y) =>
        {
            var stem = new Vector2(48f, 10f);
            return Ink(Line(x, y, stem, new Vector2(48f, 46f), 4f) || Line(x, y, new Vector2(48f, 46f), new Vector2(16f, 84f), 4f)
                || Line(x, y, new Vector2(48f, 46f), new Vector2(48f, 88f), 4f) || Line(x, y, new Vector2(48f, 46f), new Vector2(80f, 84f), 4f));
        }, 96f, SpriteMeshType.FullRect, Vector4.zero);
        AssetDatabase.Refresh();
    }

    static void Write(string name, int w, int h, System.Func<int, int, Color> pixel, float ppu, SpriteMeshType mesh, Vector4 border)
    {
        string path = UgBlockout + name + ".png";
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, y, pixel(x, y));
        tex.Apply();
        byte[] png = tex.EncodeToPNG(); Object.DestroyImmediate(tex);
        if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(png)) File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Repeat; importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = mesh; settings.spriteBorder = border; importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
}
