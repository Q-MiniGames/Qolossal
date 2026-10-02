using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds Assets/Scenes/Proto_Palm.unity, the Palm prototype from the world redesign proposal v2
// (Tools/Design/QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md): one hand of the seated titan, at the approved
// 2.5x scale (hand ~300 u), in placeholder shapes. The titan is a mystery: nothing here names the
// body or shows it in the background; the hand reads as a strange landscape of long rounded ridges.
// Units: the camera shows 10 u of height; Qori is about 1.4 u tall and jumps about 2.4 u.
//
// West to east: Qori wakes in a moss nest near the end of a long ridge (the middle finger, ~22 u
// above the valley) and follows it east. In its first dip (a joint crease) a crack drops into a side
// chamber: a root gate closes off a nook, a seed switch on the far wall opens it to a sling shot,
// and behind it is a find (a song shell); a tunnel climbs back out to the ridge. The ridge runs down
// into a wide warm bowl (the palm), where a pulsing knot of roots lies. Beyond it a ravine (a crease)
// ends at a cliff too tall to climb (the heel of the hand), with a tall stone pillar on top (the
// thumb). Touching the knot shakes the ground; the pillar topples across the ravine and comes to rest
// as a bridge (PalmGripStir). Over the cliff top, a dip (the wrist), and an old causeway slope (the
// forearm) climbs on. Behind the first ridge, two more ridges run parallel (the ring and little
// fingers) and bend a little in the quake. Vista zones pull the camera out on high points.
// Nothing here is saved.
// Menu: Qolossal > Prototype > Build Palm Prototype
public static class PalmPrototypeBuilder
{
    public const string ScenePath = "Assets/Scenes/Proto_Palm.unity";
    public const float Scale = 2.5f;

    // The hand's walk surface at the v1 scale (fingertip at x = 0, valley floor at y = 0), scaled by
    // Scale below. The first joint crease (13.5, 7) is where the chamber's crack opens.
    static readonly Vector2[] SurfaceV1 =
    {
        new Vector2(.4f, -.3f), new Vector2(.15f, 1.4f), new Vector2(0f, 3f), new Vector2(.3f, 5.2f), new Vector2(1.2f, 6.5f),
        new Vector2(2.6f, 7.3f), new Vector2(4.5f, 7.9f), new Vector2(7f, 8.3f), new Vector2(9.5f, 8.4f), new Vector2(11.5f, 8.1f),
        new Vector2(12.8f, 7.4f), new Vector2(13.5f, 7f), new Vector2(14.2f, 7.4f),                     // first joint crease
        new Vector2(16f, 8.4f), new Vector2(20f, 8.8f), new Vector2(24f, 8.6f), new Vector2(25.6f, 7.9f),
        new Vector2(26.3f, 7.4f), new Vector2(27f, 7.9f),                                               // second joint crease
        new Vector2(29f, 8.9f), new Vector2(34f, 9.3f), new Vector2(39f, 9.3f), new Vector2(42f, 9f),
        new Vector2(43.5f, 8.4f), new Vector2(44.5f, 8f), new Vector2(45.5f, 8.6f),                     // where finger meets palm
        new Vector2(47f, 9.4f), new Vector2(50f, 9.6f), new Vector2(55f, 8.8f), new Vector2(60f, 7f),
        new Vector2(65f, 5.4f), new Vector2(70f, 4.6f), new Vector2(75f, 4.4f), new Vector2(80f, 4.6f), new Vector2(84f, 4.9f),  // the cupped palm
        new Vector2(85.5f, 3.8f), new Vector2(87.5f, 1.6f), new Vector2(88.5f, 1f), new Vector2(90.5f, 1f), new Vector2(91.5f, 1.4f), // the crease ravine
        new Vector2(92f, 3f), new Vector2(92.2f, 10f), new Vector2(92.6f, 13f), new Vector2(94f, 14.2f),  // the heel's cliff
        new Vector2(98f, 14.8f), new Vector2(104f, 15f), new Vector2(108f, 14.6f), new Vector2(111f, 13.8f),
        new Vector2(112.5f, 12.6f), new Vector2(114f, 13.4f),                                           // the wrist crease
        new Vector2(118f, 14.6f), new Vector2(125f, 16.6f), new Vector2(135f, 19.4f), new Vector2(150f, 23.5f), new Vector2(166f, 27.8f),
    };
    static readonly Vector2[] Surface = SurfaceV1.Select(p => p * Scale).ToArray();

    const float FingerBase = 44.5f * Scale, KnotX = 66f * Scale, ThumbBaseX = 100f * Scale, ThumbBaseY = 11.5f * Scale, End = 166f * Scale;
    public static readonly Vector2 Start = new Vector2(12f, 21.4f);

    // The side chamber under the first dip: a crack (x CrackL..CrackR) from the surface down to a
    // room (x RoomL..RoomR, floor RoomFloor, ceiling RoomTop); a tunnel climbs from the room's east
    // end (floor from (RoomR, RoomFloor)) back up to the surface at TunnelOut.
    public const float CrackL = 32.4f, CrackR = 36.4f, RoomL = 22f, RoomR = 52f, RoomFloor = 3f, RoomTop = 9f, TunnelOut = 84f;
    const float TunnelHeight = 5f;
    public const float GateX = 28f, SwitchX = 49.5f;

    // Placeholder colours: warm stone with a moss line, paler and bluer with distance.
    static readonly Color SkinTop = new Color(.63f, .56f, .45f), SkinBottom = new Color(.38f, .33f, .28f), Moss = new Color(.47f, .6f, .33f);
    static readonly Color FarTop = new Color(.56f, .62f, .67f), FarBottom = new Color(.64f, .7f, .75f);
    static readonly Color Crease = new Color(.28f, .23f, .2f, .9f), Nail = new Color(.86f, .82f, .72f);
    static readonly Color CaveTop = new Color(.27f, .23f, .2f), CaveBottom = new Color(.18f, .15f, .13f);
    const int HandOrder = -25;

    [MenuItem("Qolossal/Prototype/Build Palm Prototype")]
    public static void Build()
    {
        var scene = A0TestRoomBuilder.NewRoom("Palm Prototype", Start, out Camera camera);
        camera.backgroundColor = new Color(.74f, .81f, .84f);
        var room = new AreaRoom("A0");
        room.Background(camera, "Assets/Art/Backgrounds/MistyValley_Background_v1.png", Start,
            new Color(.668f, .717f, .749f), new Color(.741f, .815f, .867f));
        // Keep only the distant misty hills: the near layer's trees and ruins are painted at Qori's
        // scale and would stand as tall as the ridges, and the far painting's edges show when the
        // camera pulls out. No titan in the backgrounds: it's a mystery until the end.
        foreach (Transform layer in new List<Transform>(GameObject.Find("Background").transform.Cast<Transform>()))
            if (!layer.name.StartsWith("Mid")) Object.DestroyImmediate(layer.gameObject);

        // The valley floor far below, and invisible walls at the ridge's end and the level's end.
        room.Block("Valley Floor", -40f, 0f, End + 50f, 14f, 0);
        Wall("West Wall (invisible)", new Vector2(3f, 40f), new Vector2(2f, 50f));
        Wall("East Wall (invisible)", new Vector2(End + 1f, 90f), new Vector2(2f, 60f));

        var body = new GameObject("Titan").transform;

        // The hand: the main outline, with the chamber's crack, room and tunnel cut into it, and the
        // block of ridge between the crack and the tunnel as its own solid piece.
        float TunnelFloor(float x) => RoomFloor + (x - RoomR) * (SurfaceY(TunnelOut) - RoomFloor) / (TunnelOut - RoomR);
        float TunnelCeiling(float x) => TunnelFloor(x) + TunnelHeight;
        float ceilingOut = RoomR;   // where the tunnel's ceiling meets the surface
        for (float x = RoomR; x < TunnelOut; x += .25f) { ceilingOut = x; if (TunnelCeiling(x) >= SurfaceY(x)) break; }
        float roomCeilingEnd = RoomR + (RoomTop - TunnelCeiling(RoomR)) / ((SurfaceY(TunnelOut) - RoomFloor) / (TunnelOut - RoomR));

        var outline = new List<Vector2>();
        outline.AddRange(Surface.Where(p => p.x < CrackL - .4f));
        outline.Add(new Vector2(CrackL, SurfaceY(CrackL)));
        outline.Add(new Vector2(CrackL, RoomTop)); outline.Add(new Vector2(RoomL, RoomTop));
        outline.Add(new Vector2(RoomL, RoomFloor)); outline.Add(new Vector2(RoomR, RoomFloor));
        outline.Add(new Vector2(TunnelOut, SurfaceY(TunnelOut)));
        outline.AddRange(Surface.Where(p => p.x > TunnelOut + .4f));
        outline.Add(new Vector2(End, -.3f));
        var walk = Surface.Where(p => p.x < CrackL - .4f).Append(new Vector2(CrackL, SurfaceY(CrackL))).ToList();
        Shape(body, "Ridge and bowl (middle finger, palm, heel, wrist, forearm)", outline.ToArray(), walk.ToArray(), SkinTop, SkinBottom, Moss, HandOrder, true);
        // The moss line on from the tunnel mouth (a second rim piece on the same ground).
        Line(body, "Moss line (east)", Moss, .3f, HandOrder + 1, Surface.Where(p => p.x >= TunnelOut).Prepend(new Vector2(TunnelOut, SurfaceY(TunnelOut))).ToArray());

        var block = new List<Vector2> { new Vector2(CrackR, SurfaceY(CrackR)) };
        block.AddRange(Surface.Where(p => p.x > CrackR + .4f && p.x < ceilingOut - .4f));
        block.Add(new Vector2(ceilingOut, SurfaceY(ceilingOut)));
        block.Add(new Vector2(roomCeilingEnd, RoomTop)); block.Add(new Vector2(CrackR, RoomTop));
        var blockWalk = block.Take(block.Count - 2).ToArray();
        Shape(body, "Ridge over the chamber", block.ToArray(), blockWalk, SkinTop, SkinBottom, Moss, HandOrder, true);

        // The chamber's dark back wall, filling the crack, room and tunnel.
        var cave = new List<Vector2>
        {
            new Vector2(CrackL, SurfaceY(CrackL)), new Vector2(CrackL, RoomTop), new Vector2(RoomL, RoomTop), new Vector2(RoomL, RoomFloor),
            new Vector2(RoomR, RoomFloor), new Vector2(TunnelOut, SurfaceY(TunnelOut)), new Vector2(ceilingOut, SurfaceY(ceilingOut)),
            new Vector2(roomCeilingEnd, RoomTop), new Vector2(CrackR, RoomTop), new Vector2(CrackR, SurfaceY(CrackR)),
        };
        Shape(body, "Chamber back wall", cave.ToArray(), null, CaveTop, CaveBottom, Moss, HandOrder - 4, false);

        Shape(body, "Pale cliff (fingernail)", new[] { new Vector2(-.5f, -.3f), new Vector2(3.8f, -.3f), new Vector2(2.5f, 3f), new Vector2(1f, 6.5f), new Vector2(.1f, 7.7f), new Vector2(-.4f, 3.8f) },
            null, Nail, new Color(.74f, .7f, .62f), Moss, HandOrder + 1, false);
        Line(body, "Crease (second dip)", Crease, .4f, HandOrder + 1, new Vector2(65.75f, 18.5f), new Vector2(65.2f, 14.5f), new Vector2(65.5f, 11f));
        Line(body, "Crease (ridge meets bowl)", Crease, .45f, HandOrder + 1, new Vector2(111.25f, 20f), new Vector2(110.5f, 15f), new Vector2(111.5f, 9.5f));
        Line(body, "Crease (wrist dip)", Crease, .45f, HandOrder + 1, new Vector2(281.25f, 31.5f), new Vector2(280.5f, 24f), new Vector2(281.5f, 15f));
        Line(body, "Bowl line", new Color(.36f, .3f, .26f, .6f), .3f, HandOrder + 1, new Vector2(140f, 19f), new Vector2(165f, 8.5f), new Vector2(195f, 7.8f), new Vector2(215f, 6f));

        BuildChamber(room, body);

        // The pillar (the thumb): two segments that topple about their base and joint (PalmGripStir).
        var stirObj = new GameObject("Quake (the stir)");
        stirObj.transform.SetParent(body, false);
        var stir = stirObj.AddComponent<PalmGripStir>();
        var thumbBase = new GameObject("Pillar Base").transform;
        thumbBase.SetParent(body, false); thumbBase.position = new Vector3(ThumbBaseX, ThumbBaseY, 0f);
        float l1 = 14f * Scale, r1 = 3f * Scale, l2 = 11f * Scale, r2 = 2.6f * Scale;
        var seg1 = Shape(thumbBase, "Pillar (lower)", Capsule(l1, r1), null, SkinTop, SkinBottom, Moss, HandOrder - 3, true);
        var knuckle = new GameObject("Pillar Joint").transform;
        knuckle.SetParent(thumbBase, false); knuckle.localPosition = new Vector3(l1, 0f, 0f);
        var seg2 = Shape(knuckle, "Pillar (upper)", Capsule(l2, r2), null, SkinTop, SkinBottom, Moss, HandOrder - 3, true);
        Shape(knuckle, "Pale face (thumbnail)", new[] { new Vector2(6f, 2.1f), new Vector2(11.2f, 1.6f), new Vector2(12.6f, .6f), new Vector2(11.5f, .9f), new Vector2(6.2f, 1.4f) }.Select(p => p * Scale).ToArray(),
            null, Nail, new Color(.74f, .7f, .62f), Moss, HandOrder - 2, false);
        // The moss line runs along the side of each segment that faces up once it has fallen.
        seg1.rim = new[] { new Vector2(-1.2f, -2.75f), new Vector2(7f, -3f), new Vector2(14.6f, -2.75f) }.Select(p => p * Scale).ToArray(); seg1.Rebuild();
        seg2.rim = new[] { new Vector2(-.8f, -2.4f), new Vector2(6f, -2.6f), new Vector2(12f, -1.9f), new Vector2(13.3f, -.6f) }.Select(p => p * Scale).ToArray(); seg2.Rebuild();
        stir.thumbBase = thumbBase; stir.thumbKnuckle = knuckle; stir.lengthToKnuckle = l1;
        stir.thumbSegments = new[] { seg1, seg2 };
        stir.caption = "The ground has shifted."; stir.subCaption = "The great stone pillar has fallen across the ravine.";

        // The knot in the bowl (Codex's Wakeknot art): touching it starts the quake.
        float knotY = SurfaceY(KnotX);
        stirObj.transform.position = new Vector3(KnotX, knotY, 0f);
        Vector2 centre = new Vector2(KnotX, knotY + 2.05f - .35f);
        stir.coreDim = AreaRoom.Art("WorldSystems", "Wakeknot_Dormant"); stir.coreLit = AreaRoom.Art("WorldSystems", "Wakeknot_Awake");
        stir.core = AreaRoom.Image(stirObj.transform, "Knot", stir.coreDim, centre, A0TestRoomBuilder.PropOrder + 2);
        stir.glow = AreaRoom.Image(stirObj.transform, "Glow", AreaRoom.Art("WorldSystems", "Wakeknot_Glow"), centre, A0TestRoomBuilder.PropOrder + 3);
        stir.glow.color = new Color(1f, 1f, 1f, 0f);
        var knotBox = stirObj.AddComponent<BoxCollider2D>(); knotBox.isTrigger = true; knotBox.size = new Vector2(2.4f, 3.6f); knotBox.offset = new Vector2(0f, 1.8f);
        stir.frameCentre = new Vector2(212f, 38f); stir.frameSize = 30f;

        // Two more ridges behind, parallel to the first (the ring and little fingers), hazier and
        // higher, bending a little in the quake.
        var near = Group("Near - the other ridges", new Vector2(.05f, .03f), new Vector2(60f, 20f), camera);
        var fingers = new List<Transform>();
        foreach (var (label, shift, scale, haze, order) in new[] { ("Second ridge (ring finger)", new Vector2(1.5f, 3.5f), .96f, .3f, -72), ("Third ridge (little finger)", new Vector2(6f, 6.5f), .84f, .5f, -74) })
        {
            var pivot = new GameObject(label).transform;
            pivot.SetParent(near, false); pivot.position = new Vector3(FingerBase + shift.x, shift.y, 0f);   // world space at the aligned camera
            var top = new List<Vector2>();
            foreach (var p in Surface) if (p.x <= FingerBase + .01f) top.Add(new Vector2((p.x - FingerBase) * scale, p.y * scale));
            var shape = new List<Vector2>(top) { new Vector2(5f, 21f * scale), new Vector2(5f, -14f), new Vector2(-FingerBase * scale + 1f, -14f) };
            Shape(pivot, label, shape.ToArray(), top.ToArray(),
                Color.Lerp(SkinTop, FarTop, haze), Color.Lerp(SkinBottom, FarBottom, haze), Color.Lerp(Moss, FarTop, haze), order, false);
            fingers.Add(pivot);
        }
        stir.fingers = fingers.ToArray(); stir.fingerCurl = 8f;

        // Vista points: the ridge's high point, the cliff top, the causeway.
        Vista("Vista - ridge top", new Vector2(75f, 22f), new Vector2(108f, 30f), 13f, 4f);
        Vista("Vista - cliff top", new Vector2(236f, 36f), new Vector2(276f, 44f), 16f, 5f);
        Vista("Vista - causeway", new Vector2(370f, 57f), new Vector2(414f, 74f), 18f, 6f);

        Note("Note - start", new Vector2(5f, 19f), new Vector2(22f, 26f), "Qori wakes in a nest of moss on a long, rounded ridge. The ground is warm. Follow it east.");
        Note("Note - crack", new Vector2(CrackL - 1f, 17f), new Vector2(CrackR + 1f, 22f), "A crack in the ridge. Warm air rises from below.");
        Note("Note - knot", new Vector2(KnotX - 3f, knotY), new Vector2(KnotX + 3f, knotY + 4f), "A knot of roots, pulsing slowly. Touch it.");
        Note("Note - end", new Vector2(390f, 60f), new Vector2(414f, 74f), "End of the prototype. The old causeway climbs on.");

        // Qori-scale life on the ridges: what makes them read as huge.
        room.Decor("fern", 9f, SurfaceY(9f) - .15f, -20); room.Decor("mushrooms", 15.5f, SurfaceY(15.5f) - .15f, -20);
        foreach (float x in new[] { 20f, 45f, 58f, 78f, 95f, 104f, 128f, 150f, 175f, 190f, 205f, 248f, 262f, 300f, 330f, 355f })
            room.Decor(x % 3f == 0f ? "white_flowers" : "grass", x, SurfaceY(x) - .15f, -20);
        room.Decor("fern", 310f, SurfaceY(310f) - .15f, -20); room.Decor("snail", 120f, SurfaceY(120f) - .15f, -20);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[PalmPrototypeBuilder] Built " + ScenePath);
    }

    // Inside the chamber: the nook behind a root gate at its west end, and a seed switch on the far
    // (east) wall that a sling shot opens, which opens the gate for good. In the nook, the find.
    static void BuildChamber(AreaRoom room, Transform body)
    {
        var root = new GameObject("Side Chamber").transform;
        root.SetParent(body, false);
        float G = RoomFloor;
        var gate = new GameObject("Root Gate (seed switch)") { layer = LayerMask.NameToLayer("Ground") };
        gate.transform.SetParent(root, false); gate.transform.position = new Vector3(GateX, G + 1.5f, 0f);
        gate.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 3f);
        var g = gate.AddComponent<RootGate>();
        g.topClosed = AreaRoom.Art("Props", "Gate_Root_Closed_Top"); g.bottomClosed = AreaRoom.Art("Props", "Gate_Root_Closed_Bottom");
        g.topOpen = AreaRoom.Art("Props", "Gate_Root_Open_Top"); g.bottomOpen = AreaRoom.Art("Props", "Gate_Root_Open_Bottom");
        const float S = 1.35f;   // as in A0: the slab meets the ceiling at G + 3 and the tips interlock
        g.top = AreaRoom.Image(gate.transform, "Top", g.topClosed, new Vector2(GateX, G + 3f - (.768f - .093f) * S));
        g.bottom = AreaRoom.Image(gate.transform, "Bottom", g.bottomClosed, new Vector2(GateX, G + (1.41f - .768f) * S));
        g.top.transform.localScale = g.bottom.transform.localScale = new Vector3(S, S, 1f);
        g.solid = gate.GetComponent<Collider2D>();
        // A stone lintel from the gate's top to the chamber's ceiling, so the gate can't be jumped.
        Shape(root, "Lintel", new[] { new Vector2(GateX - .9f, G + 2.9f), new Vector2(GateX + .9f, G + 2.9f), new Vector2(GateX + .9f, RoomTop + .2f), new Vector2(GateX - .9f, RoomTop + .2f) },
            null, SkinBottom, CaveTop, Moss, HandOrder + 2, true);

        var seed = new GameObject("Seed Switch (sling)").AddComponent<SeedSwitch>();
        seed.transform.SetParent(root, false); seed.transform.position = new Vector3(SwitchX, G, 0f);
        seed.off = AreaRoom.Art("Props", "Switch_Seed_Off"); seed.on = AreaRoom.Art("Props", "Switch_Seed_On");
        seed.image = AreaRoom.Image(seed.transform, "Art", seed.off, new Vector2(SwitchX, G));
        var seedBox = seed.gameObject.AddComponent<BoxCollider2D>();
        seedBox.isTrigger = true; seedBox.size = new Vector2(.6f, .9f); seedBox.offset = new Vector2(0f, .5f);
        seed.targets.Add(g);

        // The find: a song shell (a pale spiral), in the nook.
        var shell = new GameObject("Find - song shell");
        shell.transform.SetParent(root, false); shell.transform.position = new Vector3(RoomL + 2.5f, G + 1f, 0f);
        Shape(shell.transform, "Shell", new[] { new Vector2(-.6f, -.35f), new Vector2(.6f, -.35f), new Vector2(.5f, .1f), new Vector2(0f, .55f), new Vector2(-.5f, .1f) },
            null, new Color(.98f, .93f, .86f), new Color(.86f, .76f, .7f), Moss, A0TestRoomBuilder.PropOrder + 3, false);
        Line(shell.transform, "Spiral", new Color(.62f, .5f, .45f), .06f, A0TestRoomBuilder.PropOrder + 4,
            Enumerable.Range(0, 20).Select(i => new Vector2(Mathf.Cos(i * .55f) * (.04f + .018f * i), .05f + Mathf.Sin(i * .55f) * (.04f + .018f * i))).ToArray());
        shell.AddComponent<CircleCollider2D>().radius = .7f;
        shell.AddComponent<PrototypeFind>().message = "Found: a song shell.\nIn the full game, a track for the listening spot in town.";

        Note("Note - chamber", new Vector2(RoomL + 6f, G), new Vector2(RoomR - 2f, G + 5f), "Roots bar the way west. A closed bud sits on the far wall... try the sling.");
        room.Decor("mushrooms", 40f, G - .15f, -20); room.Decor("hanging_root", 44f, RoomTop + .1f, -20, hanging: true); room.Decor("hanging_ivy", 25.5f, RoomTop + .1f, -20, hanging: true);
    }

    // The hand's walk surface height at x.
    public static float SurfaceY(float x)
    {
        for (int i = 1; i < Surface.Length; i++)
            if (x <= Surface[i].x)
            {
                Vector2 a = Surface[i - 1], b = Surface[i];
                return Mathf.Lerp(a.y, b.y, Mathf.InverseLerp(a.x, b.x, x));
            }
        return Surface[Surface.Length - 1].y;
    }

    static BodyShape Shape(Transform parent, string name, Vector2[] outline, Vector2[] rim, Color top, Color bottom, Color rimColor, int order, bool solid)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var shape = obj.AddComponent<BodyShape>();
        shape.outline = outline; shape.rim = rim ?? new Vector2[0];
        shape.top = top; shape.bottom = bottom; shape.rimColor = rimColor;
        shape.sortingOrder = order; shape.solid = solid;
        shape.Rebuild();
        return shape;
    }

    // A capsule along +x from 0 to `length`, `radius` thick each side.
    static Vector2[] Capsule(float length, float radius)
    {
        var points = new List<Vector2>();
        for (int i = 0; i <= 10; i++) { float a = -90f + 180f * i / 10f; points.Add(new Vector2(length + radius * Mathf.Cos(a * Mathf.Deg2Rad), radius * Mathf.Sin(a * Mathf.Deg2Rad))); }
        for (int i = 0; i <= 10; i++) { float a = 90f + 180f * i / 10f; points.Add(new Vector2(radius * Mathf.Cos(a * Mathf.Deg2Rad), radius * Mathf.Sin(a * Mathf.Deg2Rad))); }
        return points.ToArray();
    }

    static void Line(Transform parent, string name, Color color, float width, int order, params Vector2[] points)
    {
        var line = Shape(parent, name, new Vector2[0], points, color, color, color, order, false);
        line.rimWidth = width; line.Rebuild();
    }

    static void Wall(string name, Vector2 centre, Vector2 size)
    {
        var wall = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        wall.transform.position = centre;
        wall.AddComponent<BoxCollider2D>().size = size;
        wall.AddComponent<WallSurface>().allowsWallCling = false;
    }

    static Transform Group(string name, Vector2 follow, Vector2 alignedAt, Camera camera)
    {
        var group = new GameObject(name).AddComponent<ParallaxShape>();
        group.follow = follow; group.alignedAt = alignedAt; group.targetCamera = camera;
        group.home = alignedAt; group.transform.position = alignedAt;
        return group.transform;
    }

    static void Vista(string name, Vector2 min, Vector2 max, float size, float lift)
    {
        var zone = new GameObject(name);
        zone.transform.position = (min + max) * .5f;
        zone.AddComponent<BoxCollider2D>().size = max - min;
        var vista = zone.AddComponent<VistaZone>(); vista.size = size; vista.lift = lift;
    }

    static void Note(string name, Vector2 min, Vector2 max, string text)
    {
        var obj = new GameObject(name);
        obj.transform.position = (min + max) * .5f;
        obj.AddComponent<BoxCollider2D>().size = max - min;
        obj.AddComponent<PrototypeNote>().text = text;
    }

    // Batch-mode review: builds the scene and renders it from several camera positions, at rest
    // and after the quake. Usage: -executeMethod PalmPrototypeBuilder.Capture -captureDir <folder>
    public static void Capture()
    {
        Build();
        string[] args = System.Environment.GetCommandLineArgs();
        int index = System.Array.IndexOf(args, "-captureDir");
        string folder = index >= 0 && index + 1 < args.Length ? args[index + 1] : "Temp/Captures";
        Directory.CreateDirectory(folder);
        foreach (var old in Directory.GetFiles(folder, "palm_*.png")) File.Delete(old);
        EditorSceneManager.OpenScene(ScenePath);
        foreach (var b in Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None)) b.Rebuild();
        foreach (var shape in Object.FindObjectsByType<BodyShape>(FindObjectsSortMode.None)) shape.Rebuild();
        var stir = Object.FindFirstObjectByType<PalmGripStir>();
        Camera camera = Camera.main; camera.aspect = 16f / 9f;
        var texture = new RenderTexture(1920, 1080, 24);
        var read = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        void Shot(string name, Vector2 at, float size, float grip)
        {
            stir.Pose(grip);
            camera.transform.position = new Vector3(at.x, at.y, -10f); camera.orthographicSize = size;
            foreach (var layer in Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(camera);
            foreach (var layer in Object.FindObjectsByType<ParallaxShape>(FindObjectsSortMode.None)) layer.Refresh(camera);
            camera.targetTexture = texture; camera.Render();
            RenderTexture.active = texture; read.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); read.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), read.EncodeToPNG());
        }
        Shot("palm_01_start", Start + new Vector2(2f, 1f), 5f, 0f);
        Shot("palm_02_crack", new Vector2(34f, 18f), 5f, 0f);
        Shot("palm_03_chamber", new Vector2(37f, 7f), 7f, 0f);
        Shot("palm_04_tunnel_out", new Vector2(76f, 19f), 6f, 0f);
        Shot("palm_05_ridge_vista", new Vector2(92f, 28f), 13f, 0f);
        Shot("palm_06_bowl", new Vector2(165f, 13f), 5f, 0f);
        Shot("palm_07_quake_before", new Vector2(212f, 38f), 30f, 0f);
        Shot("palm_08_quake_half", new Vector2(212f, 38f), 30f, .5f);
        Shot("palm_09_quake_after", new Vector2(212f, 38f), 30f, 1f);
        Shot("palm_10_on_fallen_pillar", new Vector2(222f, 24f), 5f, 1f);
        Shot("palm_11_cliff_vista", new Vector2(256f, 45f), 16f, 1f);
        Shot("palm_12_causeway_vista", new Vector2(392f, 72f), 18f, 1f);
        Shot("palm_13_overview", new Vector2(208f, 40f), 120f, 0f);
        camera.targetTexture = null; RenderTexture.active = null;

        // The walk line after the quake: where a ray straight down first meets ground.
        stir.Pose(1f);
        foreach (var shape in Object.FindObjectsByType<BodyShape>(FindObjectsSortMode.None)) if (shape.TryGetComponent(out PolygonCollider2D c)) c.enabled = true;
        Physics2D.SyncTransforms();
        var report = new System.Text.StringBuilder("x, surface y, collider\n");
        for (float x = 0f; x <= End; x += 2f)
        {
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, 150f), Vector2.down, 200f, LayerMask.GetMask("Ground"));
            report.AppendLine(hit ? $"{x}, {hit.point.y:0.00}, {hit.collider.name}, normal.y {hit.normal.y:0.00}" : $"{x}, none, -");
        }
        File.WriteAllText(Path.Combine(folder, "palm_surface_probe.csv"), report.ToString());
        Debug.Log("[PalmPrototypeBuilder] Captures written to " + Path.GetFullPath(folder));
    }
}
