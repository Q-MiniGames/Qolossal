using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Builds the A0 terrain kit asset and the A0 test room: one scene that exercises every
// A0 kit piece (flat ground, slope, ledges, pit, floating and one-way platforms,
// climbable and slippery walls, a ceiling overhang). Re-running rebuilds both from scratch.
public static class A0TestRoomBuilder
{
    const string KitPath = "Assets/Art/Codex/Terrain/A0_TerrainKit.asset";
    const string ScenePath = "Assets/Scenes/A0_TestRoom.unity";
    const string TerrainArt = "Assets/Art/Codex/Terrain/";

    [MenuItem("Qolossal/Scenes/Build A0 Test Room")]
    public static void Build()
    {
        TerrainKit kit = BuildKit();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;

        var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"));
        player.transform.position = new Vector3(-18f, 1.5f, 0f);

        var camera = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.72f, .79f, .77f);
        camera.transform.position = new Vector3(-18f, 2.5f, -10f);
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        var follow = camera.gameObject.AddComponent<CameraFollow>();
        var followSettings = new SerializedObject(follow);
        followSettings.FindProperty("target").objectReferenceValue = player.transform;
        followSettings.ApplyModifiedPropertiesWithoutUndo();

        new GameObject("GameManager").AddComponent<GamePauseMenu>();
        var terrain = new GameObject("Terrain").transform;
        int ground = LayerMask.NameToLayer("Ground");

        TerrainBlock Block(string name, float x, float top, float width, float height, int offset,
            bool left = false, bool right = false, bool bottom = false, TerrainBlock.Surface surface = TerrainBlock.Surface.Rock)
        {
            var obj = new GameObject(name) { layer = ground };
            obj.transform.SetParent(terrain, false);
            obj.transform.position = new Vector3(x, top, 0f);
            var block = obj.AddComponent<TerrainBlock>();
            block.kit = kit; block.width = width; block.height = height; block.sortingOffset = offset;
            block.leftFace = left; block.rightFace = right; block.bottom = bottom; block.surface = surface;
            block.Rebuild();
            return block;
        }

        TerrainPiece Piece(string name, TerrainPiece.Kind kind, float x, float y)
        {
            var obj = new GameObject(name) { layer = ground };
            obj.transform.SetParent(terrain, false);
            obj.transform.position = new Vector3(x, y, 0f);
            var piece = obj.AddComponent<TerrainPiece>();
            piece.kit = kit; piece.kind = kind;
            piece.Rebuild();
            return piece;
        }

        // Start: flat ground with a left cliff edge, then a 30 degree slope up to a plateau.
        Block("Start Ground", -24f, 0f, 50.5f, 14f, 0, left: true);
        TerrainPiece slope = Piece("Slope 30", TerrainPiece.Kind.Slope30, 10f, 0f);
        float plateau = slope.SlopeRise;
        TerrainBlock plateauBlock = Block("Plateau", 24.8f, plateau, 19.2f, plateau + 5f, 10, right: true);
        plateauBlock.blendLeft = 1.2f;
        // The slope's rounded crest carries Qori until it is at full height; the plateau collider starts there.
        plateauBlock.colliderInsetLeft = Mathf.Max(0f, 10f + slope.SlopeTopStart + .05f - 24.8f);
        plateauBlock.Rebuild();
        Piece("One-way Log", TerrainPiece.Kind.OneWay, 30f, plateau + 2.2f);
        Block("Overhang", 32f, plateau + 7.4f, 10f, 4.5f, 30, left: true, right: true, bottom: true);

        // Pit: a lower floor crossed by floating platforms.
        Block("Pit Floor", 26f, -5f, 44f, 9f, 0);
        Piece("Floating L", TerrainPiece.Kind.FloatingLarge, 45.5f, 4f);
        Piece("Floating M", TerrainPiece.Kind.FloatingMedium, 50.8f, 5.5f);
        Piece("Floating S", TerrainPiece.Kind.FloatingSmall, 47f, 0f);

        // Wall columns: wall jumps work on the climbable one and not on the slippery one.
        Block("Climbable Column", 54f, 5f, 3f, 10f, 20, left: true, right: true, surface: TerrainBlock.Surface.Climbable);
        Block("Slippery Column", 60f, 5f, 3f, 10f, 20, left: true, right: true, surface: TerrainBlock.Surface.Slippery);

        // End: a tall rock plateau with ledges on both sides, reached by climbing its left face.
        Block("End Plateau", 66f, 9f, 20f, 23f, 10, left: true);

        // Mechanics gallery on the end plateau, then pits spanned by a weak floor and platforms.
        Block("Gallery Ground 1", 86f, 9f, 22f, 23f, 10, right: true);
        Block("Weak Floor Pit", 108f, 5f, 3f, 19f, 0);
        Block("Gallery Ground 2", 111f, 9f, 19f, 23f, 10, left: true, right: true);
        Block("Thorn Pit Floor", 130f, 3f, 15f, 17f, 0);
        Block("Gallery Ground 3", 145f, 9f, 13f, 23f, 10, left: true, right: true);
        BuildMechanics(kit);

        // A Bramble Crawler patrolling the start ground, and one on the plateau.
        var crawlerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GroundCreature01.prefab");
        foreach (Vector2 at in new[] { new Vector2(-6f, .4f), new Vector2(38f, plateau + .4f) })
        {
            var crawler = (GameObject)PrefabUtility.InstantiatePrefab(crawlerPrefab);
            crawler.transform.position = at;
        }
        void Enemy(string prefab, Vector2 at)
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + prefab + ".prefab"));
            obj.transform.position = at;
        }
        Enemy("FlyingCreature01", new Vector2(20f, 9.5f));      // Seed Carrier over the slope (grapple anchor)
        Enemy("PodSpitter01", new Vector2(42.5f, plateau));      // on the plateau, facing the overhang
        Enemy("Thornwing01", new Vector2(50f, 8.5f));            // over the floating islands
        Enemy("Shellback01", new Vector2(76f, 9.05f));           // on the end plateau (mace cracks its shell)

        BuildBackground(camera, plateau);
        BuildDecor(terrain, plateau);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[A0TestRoomBuilder] Built " + ScenePath);
    }

    // Far painting, then the A0 Mid and Near parallax layers. Heights are set for a camera
    // that sits about 2.5 u above the start ground.
    static void BuildBackground(Camera camera, float plateau)
    {
        var root = new GameObject("Background").transform;
        void Layer(string name, string path, float height, bool repeat, Vector2 follow, float bottomAtStart, int order, Color below)
        {
            var layer = new GameObject(name).AddComponent<ParallaxLayer>();
            layer.transform.SetParent(root, false);
            layer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new FileNotFoundException(path);
            layer.height = height;
            layer.repeat = repeat;
            layer.follow = follow;
            layer.baseY = bottomAtStart - StartCameraY * follow.y;
            layer.baseX = repeat ? 0f : StartCameraX * (1f - follow.x);
            layer.extendBelow = repeat ? 14f : 0f;
            layer.belowColor = below;
            layer.sortingOrder = order;
            layer.targetCamera = camera;
        }
        // The far painting doesn't repeat, so it drifts only 1.5% of the camera's travel and is
        // sized to cover the whole room. Band colours are each layer's average bottom row.
        Layer("Far - Misty Valley", "Assets/Art/Backgrounds/MistyValley_Background_v1.png", 16f, false, new Vector2(.985f, .96f), -4.8f, -100, Color.white);
        Layer("Mid - BG_A0_Mid", "Assets/Art/Codex/Backgrounds/BG_A0_Mid.png", 0f, true, new Vector2(.82f, .85f), -.3f, -90, new Color(.668f, .717f, .749f));
        Layer("Near - BG_A0_Near", "Assets/Art/Codex/Backgrounds/BG_A0_Near.png", 0f, true, new Vector2(.62f, .72f), -2.2f, -80, new Color(.741f, .815f, .867f));
    }

    const float StartCameraX = -18f, StartCameraY = 2.5f;
    const int PropOrder = -15;

    static Sprite Art(string category, string name) =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Codex/" + category + "/" + name + ".png") ?? throw new FileNotFoundException(name);
    static Sprite[] Pieces(string category, string name) =>
        AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Codex/" + category + "/" + name + ".png").OfType<Sprite>().OrderBy(p => p.name).ToArray();

    static SpriteRenderer Image(Transform parent, string name, Sprite sprite, Vector2 at, int order = PropOrder)
    {
        var r = new GameObject(name).AddComponent<SpriteRenderer>();
        r.transform.SetParent(parent, false); r.transform.position = at;
        r.sprite = sprite; r.sortingOrder = order;
        return r;
    }

    static GameObject Solid(Transform parent, string name, Vector2 centre, Vector2 size)
    {
        var obj = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        obj.transform.SetParent(parent, false); obj.transform.position = centre;
        obj.AddComponent<BoxCollider2D>().size = size;
        return obj;
    }

    // One of each A0 mechanic, left to right along the gallery (ground top y = 9).
    static void BuildMechanics(TerrainKit kit)
    {
        var root = new GameObject("Mechanics").transform;
        const float G = 9f;

        // Floor thorns to jump over, and a pit lined with them under the platforms.
        void Thorns(string name, float x0, float x1, float y)
        {
            var r = Image(root, name, Art("Hazards", "Hazard_Thorns_Floor"), new Vector2((x0 + x1) * .5f, y), PropOrder + 1);
            r.drawMode = SpriteDrawMode.Tiled;
            r.size = new Vector2(x1 - x0, r.sprite.bounds.size.y);
            var box = r.gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true; box.size = new Vector2(x1 - x0 - .2f, .3f); box.offset = new Vector2(0f, .18f);
            r.gameObject.AddComponent<ThornHazard>();
        }
        Thorns("Floor Thorns", 89f, 92f, G);
        Thorns("Pit Thorns", 130f, 145f, 3f);

        // Rubble wall: only the mace breaks it.
        var rubble = Solid(root, "Rubble Barrier (mace)", new Vector2(96f, G + 1.5f), new Vector2(1.3f, 3f));
        var b1 = rubble.AddComponent<Breakable>();
        b1.breaksWith = Breakable.Rule.Mace; b1.solid = rubble.GetComponent<Collider2D>();
        b1.intact = Image(rubble.transform, "Art", Art("Props", "Barrier_Rubble_Intact"), new Vector2(96f, G));
        b1.pieces = Pieces("Props", "Barrier_Rubble_Pieces");

        // Thorn curtain: only the sword cuts it; the cut, wilted curtain stays behind.
        var curtain = Solid(root, "Thorn Curtain (sword)", new Vector2(101f, G + 1.25f), new Vector2(.9f, 2.5f));
        var b2 = curtain.AddComponent<Breakable>();
        b2.breaksWith = Breakable.Rule.Sword; b2.solid = curtain.GetComponent<Collider2D>();
        b2.intact = Image(curtain.transform, "Art", Art("Props", "Barrier_Thorns_Intact"), new Vector2(101f, G));
        b2.brokenSprite = Art("Props", "Barrier_Thorns_Cut");

        // Weak floor over the first pit: a downward attack breaks it.
        var floor = Solid(root, "Weak Floor (down attack)", new Vector2(109.5f, G - .15f), new Vector2(3f, .3f));
        var b3 = floor.AddComponent<Breakable>();
        b3.breaksWith = Breakable.Rule.DownwardAttack; b3.solid = floor.GetComponent<Collider2D>();
        b3.intact = Image(floor.transform, "Art", Art("Props", "Floor_Weak"), new Vector2(109.5f, G - .589f));
        b3.pieces = Pieces("Props", "Floor_Weak_Pieces");

        RootGate Gate(string name, float x)
        {
            var gate = Solid(root, name, new Vector2(x, G + 1.5f), new Vector2(.8f, 3f));
            var g = gate.AddComponent<RootGate>();
            g.topClosed = Art("Props", "Gate_Root_Closed_Top"); g.bottomClosed = Art("Props", "Gate_Root_Closed_Bottom");
            g.topOpen = Art("Props", "Gate_Root_Open_Top"); g.bottomOpen = Art("Props", "Gate_Root_Open_Bottom");
            // The halves are tapered root masses: the top hangs from a stone slab, the bottom rises
            // from a base. Scaled so the slab meets the ceiling at G+3 and the tips interlock.
            const float S = 1.35f;
            g.top = Image(gate.transform, "Top", g.topClosed, new Vector2(x, G + 3f - (.768f - .093f) * S));
            g.bottom = Image(gate.transform, "Bottom", g.bottomClosed, new Vector2(x, G + (1.41f - .768f) * S));
            g.top.transform.localScale = g.bottom.transform.localScale = new Vector3(S, S, 1f);
            // A rock ceiling over the passage, too high to jump over.
            var ceiling = new GameObject("Gate Ceiling " + x) { layer = LayerMask.NameToLayer("Ground") };
            ceiling.transform.position = new Vector3(x - 1.6f, G + 6.5f, 0f);
            var block = ceiling.AddComponent<TerrainBlock>();
            block.kit = kit; block.width = 3.2f; block.height = 3.5f; block.top = true; block.bottom = true;
            block.leftFace = block.rightFace = true; block.sortingOffset = 25;
            block.Rebuild();
            g.solid = gate.GetComponent<Collider2D>();
            return g;
        }

        // Pressure plate holding a root gate open.
        var plate = new GameObject("Pressure Plate").AddComponent<PressurePlate>();
        plate.transform.SetParent(root, false); plate.transform.position = new Vector3(114f, G, 0f);
        plate.up = Art("Props", "Switch_Plate_Up"); plate.down = Art("Props", "Switch_Plate_Down");
        plate.image = Image(plate.transform, "Art", plate.up, new Vector2(114f, G - .37f));
        plate.sensor = plate.gameObject.AddComponent<BoxCollider2D>();
        plate.sensor.isTrigger = true; plate.sensor.size = new Vector2(1.2f, .3f); plate.sensor.offset = new Vector2(0f, .15f);
        plate.targets.Add(Gate("Root Gate (plate)", 118f));

        // Seed switch: a sling shot opens its gate for good.
        var seed = new GameObject("Seed Switch (sling)").AddComponent<SeedSwitch>();
        seed.transform.SetParent(root, false); seed.transform.position = new Vector3(122f, G, 0f);
        seed.off = Art("Props", "Switch_Seed_Off"); seed.on = Art("Props", "Switch_Seed_On");
        seed.image = Image(seed.transform, "Art", seed.off, new Vector2(122f, G));
        var seedBox = seed.gameObject.AddComponent<BoxCollider2D>();
        seedBox.isTrigger = true; seedBox.size = new Vector2(.6f, .9f); seedBox.offset = new Vector2(0f, .5f);
        seed.targets.Add(Gate("Root Gate (sling)", 126f));

        // Crumbling platform, then a moving raft across the thorn pit.
        var crumble = Solid(root, "Crumble Platform", new Vector2(132f, G - .15f), new Vector2(2.9f, .3f));
        var cp = crumble.AddComponent<CrumblePlatform>();
        cp.image = Image(crumble.transform, "Art", Art("Hazards", "Platform_Crumble"), new Vector2(132f, G - .457f));
        cp.pieces = Pieces("Hazards", "Platform_Crumble_Pieces"); cp.solid = crumble.GetComponent<Collider2D>();

        var raft = Solid(root, "Moving Raft", new Vector2(136.5f, G - .15f), new Vector2(2.9f, .3f));
        Image(raft.transform, "Art", Art("Hazards", "Platform_Moving_A0"), new Vector2(136.5f, G - 1.003f));
        var raftBody = raft.AddComponent<Rigidbody2D>(); raftBody.bodyType = RigidbodyType2D.Kinematic;
        var mp = raft.AddComponent<MovingPlatform>();
        mp.offset = new Vector2(6f, 0f); mp.solid = raft.GetComponent<Collider2D>();

        // The A0 area portal at the far end.
        var portal = new GameObject("Portal A0").AddComponent<Portal>();
        portal.transform.SetParent(root, false); portal.transform.position = new Vector3(152f, G, 0f);
        Image(portal.transform, "Arch", Art("Props", "Portal_Gate_A0"), new Vector2(152f, G), PropOrder + 2);
        portal.membrane = Image(portal.transform, "Membrane", Art("Props", "Portal_Membrane_A0"), new Vector2(152f, G), PropOrder + 1);
        var portalBox = portal.gameObject.AddComponent<BoxCollider2D>();
        portalBox.isTrigger = true; portalBox.size = new Vector2(1.2f, 2.4f); portalBox.offset = new Vector2(0f, 1.2f);
    }

    // A0 decor from the sliced sheet, standing on (or hanging from) the room's surfaces.
    static void BuildDecor(Transform terrain, float plateau)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Codex/Decor/Decor_A0_Sheet.png").OfType<Sprite>()
            .ToDictionary(sprite => sprite.name.Replace("Decor_A0_", ""));
        var root = new GameObject("Decor").transform;
        const float Sink = .12f;  // sprite rects keep a little transparent margin below the contact point
        void Place(string name, float x, float y, int order = -20, bool hanging = false, bool flip = false)
        {
            var renderer = new GameObject("Decor " + name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(root, false);
            renderer.transform.position = new Vector3(x, y + (hanging ? Sink : -Sink), 0f);
            renderer.sprite = sprites[name];
            renderer.sortingOrder = order;
            renderer.flipX = flip;
        }
        float overhangUnderside = plateau + 7.4f - 4.5f;
        Place("fern", -21f, 0f); Place("boulder", -13f, 0f); Place("white_flowers", -9.5f, 0f);
        Place("broken_pillar", -4f, 0f); Place("grass", 1f, 0f, 5); Place("small_rock", 4.5f, 0f); Place("snail", 5.3f, 0f);
        Place("carved_block", 29f, plateau); Place("mushrooms", 36.5f, plateau); Place("fern", 41f, plateau, flip: true);
        Place("hanging_ivy", 33.5f, overhangUnderside, hanging: true); Place("hanging_root", 39.5f, overhangUnderside, hanging: true);
        Place("small_rock", 48.5f, -5f); Place("grass", 52f, -5f, 5); Place("mushrooms", 64.8f, -5f);
        Place("fallen_log", 71f, 9f); Place("white_flowers", 75f, 9f); Place("broken_pillar", 79f, 9f); Place("grass", 82f, 9f, 5);
    }

    static TerrainKit BuildKit()
    {
        var kit = AssetDatabase.LoadAssetAtPath<TerrainKit>(KitPath);
        if (kit == null)
        {
            kit = ScriptableObject.CreateInstance<TerrainKit>();
            AssetDatabase.CreateAsset(kit, KitPath);
        }
        Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(TerrainArt + name + ".png")
            ?? throw new FileNotFoundException("Missing terrain sprite; run Qolossal/Art/Import Accepted Codex Art first.", name);
        kit.groundTop = Load("A0_Ground_Top");
        kit.groundFill = Load("A0_Ground_Fill");
        kit.wallSide = Load("A0_Wall_Side");
        kit.ceilingUnder = Load("A0_Ceiling_Under");
        kit.wallClimbable = Load("A0_Wall_Climbable");
        kit.wallSlippery = Load("A0_Wall_Slippery");
        kit.cornerOuterTopRight = Load("A0_Corner_Outer_TopRight");
        kit.cornerOuterBottomRight = Load("A0_Corner_Outer_BottomRight");
        kit.slope30 = Load("A0_Slope_30");
        kit.platformOneWay = Load("A0_Platform_OneWay");
        kit.floatingSmall = Load("A0_Platform_Floating_S");
        kit.floatingMedium = Load("A0_Platform_Floating_M");
        kit.floatingLarge = Load("A0_Platform_Floating_L");
        // Take the code's measured landmarks (the asset keeps older serialized values otherwise).
        var measured = ScriptableObject.CreateInstance<TerrainKit>();
        kit.slopeSurface = measured.slopeSurface;
        kit.wallSideFace = measured.wallSideFace;
        kit.cornerOuterTopRightLedge = measured.cornerOuterTopRightLedge;
        kit.cornerOuterBottomRightEdge = measured.cornerOuterBottomRightEdge;
        UnityEngine.Object.DestroyImmediate(measured);
        EditorUtility.SetDirty(kit);
        return kit;
    }

    // Batch-mode review: renders the test room from several camera positions.
    // Usage: -executeMethod A0TestRoomBuilder.Capture -captureDir <folder>
    public static void Capture()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-captureDir");
        string folder = index >= 0 && index + 1 < args.Length ? args[index + 1] : "Temp/A0Captures";
        Directory.CreateDirectory(folder);
        EditorSceneManager.OpenScene(ScenePath);
        foreach (TerrainBlock block in UnityEngine.Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None)) block.Rebuild();
        foreach (TerrainPiece piece in UnityEngine.Object.FindObjectsByType<TerrainPiece>(FindObjectsSortMode.None)) piece.Rebuild();
        Camera camera = Camera.main;
        (string name, Vector2 at, float size)[] shots =
        {
            ("overview", new Vector2(31f, 2f), 22f), ("start", new Vector2(-18f, 2.5f), 5f),
            ("slope", new Vector2(18f, 5f), 6f), ("plateau_ledge", new Vector2(40f, 8f), 5f),
            ("overhang", new Vector2(37f, 14f), 5f), ("pit", new Vector2(50f, 1f), 6f),
            ("columns", new Vector2(60f, 3f), 6f), ("end_plateau", new Vector2(70f, 6f), 6f),
            ("gallery_1", new Vector2(96f, 11f), 5.5f), ("gallery_2", new Vector2(113f, 11f), 5.5f),
            ("gallery_3", new Vector2(122f, 12f), 5.5f), ("gallery_4", new Vector2(138f, 8f), 6.5f),
            ("gallery_5", new Vector2(151f, 11f), 4f),
            ("enemy_carrier", new Vector2(20f, 9.3f), 2.2f), ("enemy_spitter", new Vector2(41.5f, 9.4f), 2.2f),
            ("enemy_thornwing", new Vector2(50f, 8.4f), 2.2f), ("enemy_shellback", new Vector2(76f, 9.8f), 2.2f),
        };
        var texture = new RenderTexture(1920, 1080, 24);
        var read = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        foreach (var shot in shots)
        {
            camera.transform.position = new Vector3(shot.at.x, shot.at.y, -10f);
            camera.orthographicSize = shot.size;
            foreach (ParallaxLayer layer in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(camera);
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            read.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            read.Apply();
            File.WriteAllBytes(Path.Combine(folder, shot.name + ".png"), read.EncodeToPNG());
        }
        camera.targetTexture = null;
        RenderTexture.active = null;

        // Walk-surface probes: where a ray straight down first meets ground at each x.
        Physics2D.SyncTransforms();
        int mask = LayerMask.GetMask("Ground");
        var report = new System.Text.StringBuilder();
        report.AppendLine("x, surface y, collider");
        for (float x = -22f; x <= 84f; x += 2f)
        {
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, 30f), Vector2.down, 60f, mask);
            report.AppendLine(hit ? $"{x}, {hit.point.y:0.00}, {hit.collider.name}" : $"{x}, none, -");
        }
        File.WriteAllText(Path.Combine(folder, "surface_probe.csv"), report.ToString());
        Debug.Log("[A0TestRoomBuilder] Captures written to " + Path.GetFullPath(folder));
    }
}
