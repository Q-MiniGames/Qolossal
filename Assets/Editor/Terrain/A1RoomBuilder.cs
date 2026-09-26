using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds Assets/Scenes/A1_Aqueduct.unity, the Aqueduct Ravine, from the A1 terrain kit. West to
// east: the portal from A0 and a wading stream; a slope up to the ravine plateau; the ravine,
// crossed by swinging from rings under a broken aqueduct span (Living Thread), or climbed out
// of up a vine column (Climbing Moss); the east plateau with thorns and a rubble wall (mace);
// a drop to the lower gallery; a thorn pit crossed on a chain-hung platform, with a Thornwing
// to pogo off (Bloomfall); a checkpoint and the portal on to A2. The way back up uses the rock
// walls, so every stretch works in both directions. Enemies wear their A1 palettes.
// Menu: Qolossal > Scenes > Build A1 Aqueduct
public static class A1RoomBuilder
{
    public const string SceneName = "A1_Aqueduct";
    const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";
    const string FarPainting = "Assets/Resources/WorldBackground/Aqueduct.png";
    const float StartX = 3f, StartY = 1.5f;
    const int PropOrder = A0TestRoomBuilder.PropOrder;
    static TerrainKit kit;

    static Sprite Art(string category, string name) => A0TestRoomBuilder.Art(category, name);
    static SpriteRenderer Image(Transform parent, string name, Sprite sprite, Vector2 at, int order = PropOrder) =>
        A0TestRoomBuilder.Image(parent, name, sprite, at, order);

    [MenuItem("Qolossal/Scenes/Build A1 Aqueduct")]
    public static void Build()
    {
        kit = TerrainKitBuilder.Build("A1");
        var scene = A0TestRoomBuilder.NewRoom("A1 Aqueduct Ravine", new Vector2(StartX, StartY), out Camera camera);
        camera.backgroundColor = new Color(.665f, .744f, .77f);   // the far painting's top row

        var terrain = new GameObject("Terrain").transform;
        TerrainBlock Block(string name, float x, float top, float width, float height, int offset,
            bool left = false, bool right = false, bool bottom = false, TerrainBlock.Surface surface = TerrainBlock.Surface.Rock) =>
            A0TestRoomBuilder.AddBlock(terrain, kit, name, x, top, width, height, offset, left, right, bottom, surface);

        // Entry ground, running on under the slope; walls close both ends of the room.
        Block("West Wall", -8f, 16f, 2f, 30f, 20, right: true);
        Block("Entry Ground", -6f, 0f, 52.5f, 14f, 0);
        var slopeObj = new GameObject("Slope 30") { layer = LayerMask.NameToLayer("Ground") };
        slopeObj.transform.SetParent(terrain, false); slopeObj.transform.position = new Vector3(30f, 0f, 0f);
        var slope = slopeObj.AddComponent<TerrainPiece>(); slope.kit = kit; slope.kind = TerrainPiece.Kind.Slope30; slope.Rebuild();
        float P = slope.SlopeRise;   // the plateaus' height

        // Ravine plateau: starts under the slope's crest and ends at the ravine's west cliff.
        float plateauX = 30f + slope.SurfaceWidth - 1.6f;
        var plateau = Block("Ravine Plateau", plateauX, P, 58f - plateauX, P + 14f, 10, right: true);
        plateau.blendLeft = 1.2f;
        plateau.colliderInsetLeft = Mathf.Max(0f, 30f + slope.SlopeTopStart + .05f - plateauX);
        plateau.Rebuild();

        // The ravine: a stream along its floor, a broken aqueduct span overhead, and a vine
        // column up its east wall.
        Block("Ravine Floor", 58f, -4f, 20f, 10f, 0);
        Block("Aqueduct Span", 59f, 18f, 18f, 4f, 30, left: true, right: true, bottom: true);
        Block("Vine Column", 76.5f, 8f, 1.5f, 12.2f, 20, left: true, surface: TerrainBlock.Surface.Climbable);

        // East plateau, then the drop to the lower gallery under a rock ceiling.
        Block("East Plateau", 78f, P, 24f, P + 14f, 10, left: true, right: true);
        Block("Lower Gallery Floor", 101f, -6f, 41f, 8f, 0);
        Block("Gallery Ceiling", 106f, 8f, 26f, 5f, 30, left: true, right: true, bottom: true);
        Block("East Wall", 142f, 16f, 2f, 24f, 20, left: true);

        BuildBackground(camera);
        // Dark back walls of fill rock, so the ravine and the lower gallery read as deep and
        // enclosed rather than opening onto the valley painting.
        var backs = new GameObject("Back Walls").transform;
        Color shade = new Color(.1f, .12f, .13f, 1f);   // dark and cool, so it never reads as ground Qori could stand on
        Strip(backs, "Ravine Back Wall", kit.groundFill, 57f, P - .5f, 22f, P + 4f, Vector2.zero, shade, -60, false, false, 2.5f, 0f, .8f);
        Strip(backs, "Gallery Back Wall", kit.groundFill, 101f, 8f, 41f, 14.5f, Vector2.zero, shade, -60, false, false, 2.5f, 0f, .8f);
        BuildWater(P);
        BuildMechanics(P);
        BuildEnemies(P);
        BuildDecor(P);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        A0TestRoomBuilder.AddToBuild(ScenePath, 1);
        AssetDatabase.SaveAssets();
        Debug.Log("[A1RoomBuilder] Built " + ScenePath);
    }

    // The Aqueduct far painting (imported as a sprite here), then the A1 Mid and Near layers.
    static void BuildBackground(Camera camera)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(FarPainting);
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f; importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        var root = new GameObject("Background").transform;
        void Layer(string name, Sprite sprite, float height, bool repeat, Vector2 follow, float bottomAtStart, int order, Color below)
        {
            var layer = new GameObject(name).AddComponent<ParallaxLayer>();
            layer.transform.SetParent(root, false);
            layer.sprite = sprite ?? throw new FileNotFoundException(name);
            layer.height = height; layer.repeat = repeat; layer.follow = follow;
            layer.baseY = bottomAtStart - (StartY + 1f) * follow.y;
            layer.baseX = repeat ? 0f : StartX * (1f - follow.x);
            layer.extendBelow = repeat ? 14f : 0f;
            layer.belowColor = below; layer.sortingOrder = order; layer.targetCamera = camera;
        }
        // Band colours are each layer's average bottom row.
        Layer("Far - Aqueduct", AssetDatabase.LoadAssetAtPath<Sprite>(FarPainting), 16f, false, new Vector2(.985f, .96f), -4.8f, -100, Color.white);
        Layer("Mid - BG_A1_Mid", Art("Backgrounds", "BG_A1_Mid"), 0f, true, new Vector2(.82f, .85f), -.3f, -90, new Color(.613f, .656f, .685f));
        Layer("Near - BG_A1_Near", Art("Backgrounds", "BG_A1_Near"), 0f, true, new Vector2(.62f, .72f), -2.2f, -80, new Color(.174f, .202f, .181f));
    }

    // Shallow streams Qori wades through, and the waterfall off the ravine's west lip. The water
    // sits just above the walk line and fades out at both banks and into the ground below, so it
    // reads as a shallow channel rather than a box laid on the ground.
    static void BuildWater(float P)
    {
        var root = new GameObject("Water").transform;
        Sprite surface = Art("Hazards", "Water_Surface"), body = Art("Hazards", "Water_Body");
        void Stream(string name, float x0, float x1, float floor)
        {
            float top = floor + .07f;
            var pool = new GameObject(name).AddComponent<WaterPool>();
            pool.transform.SetParent(root, false); pool.transform.position = new Vector3(x0, top, 0f);
            pool.surfaceY = top; pool.ripple = Art("Effects", "FX_Water_Ripple");
            var box = pool.gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true; box.size = new Vector2(x1 - x0, 1f); box.offset = new Vector2((x1 - x0) * .5f, -.3f);
            Strip(pool.transform, "Body", body, x0, top, x1 - x0, .5f, new Vector2(.03f, .01f), new Color(1f, 1f, 1f, .5f), 18, false, false, 0f, .3f, 1.1f);
            float h = surface.rect.height / surface.pixelsPerUnit;   // the painted line is at the canvas's middle
            Strip(pool.transform, "Surface", surface, x0, top + h * .5f, x1 - x0, h, new Vector2(.08f, 0f), new Color(1f, 1f, 1f, .85f), 19, true, false, 0f, 0f, 1.1f);
        }
        Stream("Entry Stream", 12f, 19f, 0f);
        Stream("Ravine Stream", 58.2f, 76.4f, -4f);

        Sprite fall = Art("Hazards", "Waterfall_Column");
        // Pours from the broken aqueduct span overhead: the column's top starts inside the span, which
        // is drawn in front of it, so the water spills out of the stonework into the ravine stream.
        const float fallX = 59.4f, spanUnderside = 14f, streamY = -3.93f;
        Strip(root, "Waterfall", fall, fallX, spanUnderside + .4f, 2.4f, spanUnderside + .4f - streamY, new Vector2(0f, .9f), new Color(1f, 1f, 1f, .85f), -14, false, true);
        var splash = Image(root, "Waterfall Splash", Art("Hazards", "Waterfall_Splash_Base"), new Vector2(fallX + 1.2f, -3.6f), -13);
        splash.transform.localScale = new Vector3(.38f, .38f, 1f);
    }

    static ScrollingStrip Strip(Transform parent, string name, Sprite sprite, float x, float top, float width, float height, Vector2 scroll, Color tint, int order, bool fitHeight, bool fitWidth,
        float fadeTop = 0f, float fadeBottom = 0f, float fadeSides = 0f)
    {
        var strip = new GameObject(name).AddComponent<ScrollingStrip>();
        strip.enabled = false;   // configure before it builds its mesh
        strip.transform.SetParent(parent, false); strip.transform.position = new Vector3(x, top, 0f);
        strip.sprite = sprite; strip.width = width; strip.height = height; strip.scroll = scroll; strip.tint = tint;
        strip.sortingOrder = order; strip.fitHeight = fitHeight; strip.fitWidth = fitWidth;
        strip.fadeTop = fadeTop; strip.fadeBottom = fadeBottom; strip.fadeSides = fadeSides;
        strip.enabled = true;
        return strip;
    }

    static void BuildMechanics(float P)
    {
        var root = new GameObject("Mechanics").transform;
        A0TestRoomBuilder.AddPortal(root, new Vector2(0f, 0f), "a1-west", "A0_TestRoom", "a0-east", "A0 Mossy Hollow", 1f, "A1", 48f, kit);
        A0TestRoomBuilder.AddPortal(root, new Vector2(137f, -6f), "a1-east", "A2_Grove", "a2-west", "A2 Ancient Grove", -1f, "A1", 48f, kit);
        Sprite shrine = Art("Props", "Checkpoint_Shrine_A1");
        A0TestRoomBuilder.AddCheckpoint(root, new Vector2(7f, 0f), "a1-entry", shrine, 27f, kit);
        A0TestRoomBuilder.AddCheckpoint(root, new Vector2(128f, -6f), "a1-gallery", shrine, 27f, kit);

        // Swing rings under the aqueduct span, 6 u apart (the thread catches within 4 u).
        var anchorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ThreadAnchor01.prefab");
        Sprite ring = Art("Props", "Anchor_Ring_A1");
        foreach (float x in new[] { 62.5f, 68.5f, 74.5f })
        {
            var anchor = (GameObject)PrefabUtility.InstantiatePrefab(anchorPrefab, root);
            anchor.name = "Swing Ring " + x;
            anchor.transform.position = new Vector3(x, 13.2f, 0f); anchor.transform.localScale = Vector3.one;
            // The prefab's visible art is its AnchorVisual child (the root renderer only carries the highlight tint).
            var art = anchor.transform.Find("AnchorVisual");
            art.localPosition = Vector3.zero; art.localScale = Vector3.one;
            var r = art.GetComponent<SpriteRenderer>(); r.sprite = ring; r.sortingOrder = PropOrder + 3;
        }

        // Thorns to jump on the east plateau, and lining the lower gallery's pit. The strip's flat
        // stone base is sunk to its middle row (112 px) at the walk line, its ends fade out, and
        // the ground's moss lip is drawn in front of it.
        Sprite thornArt = Art("Hazards", "Hazard_Thorns_Floor_A1");
        void Thorns(string name, float x0, float x1, float ground)
        {
            float h = thornArt.rect.height / thornArt.pixelsPerUnit, w = x1 - x0;
            var strip = Strip(root, name, thornArt, x0, ground + 112f / thornArt.pixelsPerUnit, w, h, Vector2.zero, Color.white, PropOrder + 1, true, false, 0f, 0f, .45f);
            var seat = new GameObject("Grounding");   // the moss lip centres on its object
            seat.transform.SetParent(strip.transform, false); seat.transform.position = new Vector3(x0 + w * .5f, ground, 0f);
            var grounded = A0TestRoomBuilder.Ground(seat, kit, ground, w - .6f, PropOrder + 1);
            grounded.enabled = false; grounded.shadow = false; grounded.enabled = true;
            var hazard = new GameObject("Hazard");
            hazard.transform.SetParent(strip.transform, false); hazard.transform.position = new Vector3(x0 + w * .5f, ground + .18f, 0f);
            var box = hazard.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = new Vector2(w - .2f, .3f);
            hazard.AddComponent<ThornHazard>();
        }
        Thorns("Plateau Thorns", 86f, 89f, P);
        Thorns("Pit Thorns", 113f, 126f, -6f);

        // Rubble wall before the drop: only the mace breaks it.
        Sprite rubbleArt = Art("Props", "Barrier_Rubble_Intact_A1");
        var rubble = new GameObject("Rubble Barrier (mace)") { layer = LayerMask.NameToLayer("Ground") };
        rubble.transform.SetParent(root, false); rubble.transform.position = new Vector3(99f, P + 1.7f, 0f);
        rubble.AddComponent<BoxCollider2D>().size = new Vector2(1.3f, 3.4f);
        var breakable = rubble.AddComponent<Breakable>();
        breakable.breaksWith = Breakable.Rule.Mace; breakable.solid = rubble.GetComponent<Collider2D>();
        breakable.intact = Image(rubble.transform, "Art", rubbleArt, new Vector2(99f, P + A0TestRoomBuilder.Grounded(rubbleArt, 45f)));
        breakable.pieces = A0TestRoomBuilder.Pieces("Props", "Barrier_Rubble_Pieces_A1");
        A0TestRoomBuilder.Ground(breakable.intact.gameObject, kit, P, 1.7f, PropOrder);

        // A platform hung on chains from the gallery ceiling, carrying Qori across the thorn pit.
        const float deck = -4.4f, deckAboveArt = .84f;   // Platform_Moving_A1's walk line is .84 u above its pivot
        var raft = new GameObject("Hanging Platform") { layer = LayerMask.NameToLayer("Ground") };
        raft.transform.SetParent(root, false); raft.transform.position = new Vector3(115.5f, deck - .15f, 0f);
        raft.AddComponent<BoxCollider2D>().size = new Vector2(2.9f, .3f);
        Image(raft.transform, "Art", Art("Hazards", "Platform_Moving_A1"), new Vector2(115.5f, deck - deckAboveArt), PropOrder + 1);
        var raftBody = raft.AddComponent<Rigidbody2D>(); raftBody.bodyType = RigidbodyType2D.Kinematic;
        var mover = raft.AddComponent<MovingPlatform>();
        mover.offset = new Vector2(8f, 0f); mover.travelSeconds = 4f; mover.solid = raft.GetComponent<Collider2D>();
        var chains = raft.AddComponent<HangingChains>();
        chains.chain = Art("Hazards", "Platform_Moving_A1_Chain"); chains.ceilingY = 3f;
        chains.artTop = 1.62f - deckAboveArt + .15f;   // painted chain tops, relative to the platform
    }

    static void BuildEnemies(float P)
    {
        var root = new GameObject("Enemies").transform;
        void Enemy(string prefab, Vector2 at)
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + prefab + ".prefab"), root);
            obj.transform.position = at;
            if (AreaPalette.Apply(obj, "A1") == 0) Debug.LogWarning($"[A1RoomBuilder] {prefab} has no A1 palette parts");
        }
        Enemy("FlyingCreature01", new Vector2(20f, 6.5f));      // Seed Carrier over the entry (also a grapple anchor)
        Enemy("GroundCreature01", new Vector2(52f, P + .4f));    // Crawler on the ravine plateau
        Enemy("GroundCreature01", new Vector2(93f, P + .4f));    // Crawler past the plateau thorns
        Enemy("Shellback01", new Vector2(107f, -5.95f));         // at the foot of the drop (mace cracks its shell)
        Enemy("Thornwing01", new Vector2(119.5f, -1f));          // over the thorn pit: a pogo target
        Enemy("PodSpitter01", new Vector2(133f, -6f));           // guarding the east portal
    }

    // A1 decor from the sliced sheet, standing on (or hanging from) the room's surfaces.
    static void BuildDecor(float P)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Codex/Decor/Decor_A1_Sheet.png").OfType<Sprite>()
            .ToDictionary(sprite => sprite.name.Replace("Decor_A1_", ""));
        var root = new GameObject("Decor").transform;
        const float Sink = .12f;   // slice rects keep a little transparent margin below the contact point
        // Solid pieces standing on the ground get a contact shadow and the ground's moss in front.
        string[] solid = { "stone_basin", "aqueduct_pillar", "carved_block", "algae_rock_large", "algae_rock_small" };
        void Place(string name, float x, float y, int order = -20, bool hanging = false, bool flip = false)
        {
            var r = new GameObject("Decor " + name).AddComponent<SpriteRenderer>();
            r.transform.SetParent(root, false);
            r.transform.position = new Vector3(x, y + (hanging ? Sink : -Sink), 0f);
            r.sprite = sprites[name]; r.sortingOrder = order; r.flipX = flip;
            if (!hanging && System.Array.IndexOf(solid, name) >= 0) A0TestRoomBuilder.Ground(r.gameObject, kit, y, r.sprite.bounds.size.x * .75f, order);
        }
        // Entry
        Place("reeds", 11.3f, 0f, 5); Place("water_lilies", 15.5f, .05f, 21); Place("reeds", 19.6f, 0f, 5, flip: true);
        Place("stone_basin", 22.5f, 0f); Place("snail", 23.6f, 0f); Place("aqueduct_pillar", 25.5f, 0f); Place("grass", 28f, 0f, 5);
        // Ravine plateau, span and floor
        Place("carved_block", 49f, P); Place("grass", 51.5f, P, 5); Place("mushrooms", 55.5f, P);
        Place("hanging_ivy", 65.6f, 14f, hanging: true); Place("hanging_roots", 71.5f, 14f, hanging: true);
        Place("algae_rock_small", 65.5f, -4f); Place("reeds", 72f, -4f, 5);
        // East plateau
        Place("algae_rock_large", 81f, P); Place("grass", 91f, P, 5); Place("aqueduct_pillar", 96f, P);
        // Lower gallery
        Place("mushrooms", 104f, -6f); Place("hanging_roots", 110f, 3f, hanging: true); Place("hanging_ivy", 129.5f, 3f, hanging: true);
        Place("algae_rock_small", 131f, -6f); Place("grass", 139.5f, -6f, 5);
    }

    // Batch-mode review: renders the room from several camera positions.
    // Usage: -executeMethod A1RoomBuilder.Capture -captureDir <folder>
    public static void Capture()
    {
        A0TestRoomBuilder.RenderShots(ScenePath, new (string, Vector2, float)[]
        {
            ("a1_overview", new Vector2(68f, 5f), 40f), ("a1_entry", new Vector2(8f, 2.5f), 5.5f),
            ("a1_slope", new Vector2(40f, 6f), 6f), ("a1_ravine", new Vector2(68f, 6f), 9f),
            ("a1_east_plateau", new Vector2(92f, 11f), 5.5f), ("a1_gallery", new Vector2(118f, -2f), 7f),
            ("a1_exit", new Vector2(132f, -3.5f), 5.5f), ("a1_rings", new Vector2(68.5f, 12f), 4f),
            ("a1_ravine_play", new Vector2(62f, 10f), 5f),
        });
    }
}
