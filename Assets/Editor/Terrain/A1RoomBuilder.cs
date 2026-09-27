using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds Assets/Scenes/A1_Aqueduct.unity, the Aqueduct Ravine, from the A1 terrain kit. West to
// east: the portal from A0 and a wading stream; a slope up to the ravine plateau; the ravine,
// crossed by swinging from rings under a broken aqueduct span (Living Thread), or climbed out
// of up a vine column (Climbing Moss); the east plateau with thorns and a rubble wall (mace);
// a drop to the lower gallery under loose falling rocks; a thorn pit crossed on a chain-hung
// platform, with a Thornwing to pogo off (Bloomfall); a checkpoint, the portal on to A2, and a
// false wall hiding an alcove with a heart seed. The way back up uses the rock walls, so every
// stretch works in both directions. Ripple Newts lurk in both streams and a Burrow Grub under
// the east plateau; the enemies from A0 wear their A1 palettes.
// Menu: Qolossal > Scenes > Build A1 Aqueduct
public static class A1RoomBuilder
{
    public const string SceneName = "A1_Aqueduct";
    const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";
    const float StartX = 3f, StartY = 1.5f;

    [MenuItem("Qolossal/Scenes/Build A1 Aqueduct")]
    public static void Build()
    {
        var scene = A0TestRoomBuilder.NewRoom("A1 Aqueduct Ravine", new Vector2(StartX, StartY), out Camera camera);
        camera.backgroundColor = new Color(.665f, .744f, .77f);   // the far painting's top row
        var room = new AreaRoom("A1");

        // Entry ground, running on under the slope; walls close both ends of the room.
        room.Block("West Wall", -8f, 16f, 2f, 30f, 20, right: true);
        room.Block("Entry Ground", -6f, 0f, 52.5f, 14f, 0);
        var slope = room.Piece("Slope 30", TerrainPiece.Kind.Slope30, 30f, 0f);
        float P = slope.SlopeRise;   // the plateaus' height

        // Ravine plateau: starts under the slope's crest and ends at the ravine's west cliff.
        float plateauX = 30f + slope.SurfaceWidth - 1.6f;
        room.AfterSlope(slope, "Ravine Plateau", 58f - plateauX, P + 14f, 10, right: true);

        // The ravine: a stream along its floor, a broken aqueduct span overhead, and a vine
        // column up its east wall.
        room.Block("Ravine Floor", 58f, -4f, 20f, 10f, 0);
        room.Block("Aqueduct Span", 59f, 18f, 18f, 4f, 30, left: true, right: true, bottom: true);
        room.Block("Vine Column", 76.5f, 8f, 1.5f, 12.2f, 20, left: true, surface: TerrainBlock.Surface.Climbable);

        // East plateau, then the drop to the lower gallery under a rock ceiling.
        room.Block("East Plateau", 78f, P, 24f, P + 14f, 10, left: true, right: true);
        room.Block("Lower Gallery Floor", 101f, -6f, 46f, 8f, 0);
        room.Block("Gallery Ceiling", 106f, 8f, 26f, 5f, 30, left: true, right: true, bottom: true);
        // A hidden alcove at the gallery's east end: its low roof comes down to -1.5, and a false
        // wall (SecretWall) hides the opening beneath it.
        room.Block("Alcove Roof", 142f, 16f, 5f, 17.5f, 20, left: true, bottom: true);
        room.Block("East Wall", 147f, 16f, 2f, 24f, 20, left: true);

        room.Background(camera, "Assets/Resources/WorldBackground/Aqueduct.png", new Vector2(StartX, StartY),
            new Color(.613f, .656f, .685f), new Color(.174f, .202f, .181f));
        Color shade = new Color(.1f, .12f, .13f, 1f);   // dark and cool, so it never reads as ground Qori could stand on
        room.BackWall("Ravine Back Wall", 57f, P - .5f, 22f, P + 4f, shade, .8f);
        room.BackWall("Gallery Back Wall", 101f, 8f, 46f, 14.5f, shade);   // walled at both ends: no side fade
        BuildWater(room, P);
        BuildMechanics(room, P);

        room.Enemy("FlyingCreature01", new Vector2(20f, 6.5f));      // Seed Carrier over the entry (also a grapple anchor)
        room.Enemy("GroundCreature01", new Vector2(52f, P + .4f));    // Crawler on the ravine plateau
        room.Enemy("Grub01", new Vector2(93f, P), false);            // Burrow Grub under the east plateau
        room.Enemy("Newt01", new Vector2(15.5f, 0f), false);          // Ripple Newt in the entry stream
        room.Enemy("Newt01", new Vector2(70f, -4f), false);           // and one in the ravine stream
        room.Enemy("Shellback01", new Vector2(107f, -5.95f));         // at the foot of the drop (mace cracks its shell)
        room.Enemy("Thornwing01", new Vector2(119.5f, -1f));          // over the thorn pit: a pogo target
        room.Enemy("PodSpitter01", new Vector2(133f, -6f));           // guarding the east portal

        // Entry
        room.Decor("reeds", 11.3f, 0f, 5); room.Decor("water_lilies", 17.8f, .05f, 21); room.Decor("reeds", 19.6f, 0f, 5, flip: true);
        room.Decor("stone_basin", 22.5f, 0f, solid: true); room.Decor("snail", 23.6f, 0f); room.Decor("aqueduct_pillar", 25.5f, 0f, solid: true); room.Decor("grass", 28f, 0f, 5);
        // Ravine plateau, span and floor
        room.Decor("carved_block", 49f, P, solid: true); room.Decor("grass", 51.5f, P, 5); room.Decor("mushrooms", 55.5f, P);
        room.Decor("hanging_ivy", 65.6f, 14f, hanging: true); room.Decor("hanging_roots", 71.5f, 14f, hanging: true);
        room.Decor("algae_rock_small", 65.5f, -4f, solid: true); room.Decor("reeds", 72f, -4f, 5);
        // East plateau
        room.Decor("algae_rock_large", 81f, P, solid: true); room.Decor("grass", 91f, P, 5); room.Decor("aqueduct_pillar", 96f, P, solid: true);
        // Lower gallery
        room.Decor("mushrooms", 104f, -6f); room.Decor("hanging_roots", 114.5f, 3f, hanging: true); room.Decor("hanging_ivy", 129.5f, 3f, hanging: true);
        room.Decor("algae_rock_small", 131f, -6f, solid: true); room.Decor("grass", 139.5f, -6f, 5);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        A0TestRoomBuilder.AddToBuild(ScenePath, 1);
        AssetDatabase.SaveAssets();
        Debug.Log("[A1RoomBuilder] Built " + ScenePath);
    }

    // Shallow streams Qori wades through, and the waterfall pouring from the broken aqueduct span:
    // the column's top starts inside the span, which is drawn in front of it, so the water spills
    // out of the stonework into the ravine stream.
    static void BuildWater(AreaRoom room, float P)
    {
        var root = new GameObject("Water").transform;
        room.Stream(root, "Entry Stream", 12f, 19f, 0f);
        room.Stream(root, "Ravine Stream", 58.2f, 76.4f, -4f);
        const float fallX = 59.4f, spanUnderside = 14f, streamY = -3.93f;
        AreaRoom.Strip(root, "Waterfall", AreaRoom.Art("Hazards", "Waterfall_Column"), fallX, spanUnderside + .4f, 2.4f, spanUnderside + .4f - streamY,
            new Vector2(0f, .9f), new Color(1f, 1f, 1f, .85f), -14, false, true);
        var splash = AreaRoom.Image(root, "Waterfall Splash", AreaRoom.Art("Hazards", "Waterfall_Splash_Base"), new Vector2(fallX + 1.2f, -3.6f), -13);
        splash.transform.localScale = new Vector3(.38f, .38f, 1f);
        splash.gameObject.AddComponent<WaterfallSplash>().droplet = AreaRoom.Art("Effects", "FX_Water_Drip");
    }

    static void BuildMechanics(AreaRoom room, float P)
    {
        room.Portal(new Vector2(0f, 0f), "a1-west", "A0_TestRoom", "a0-east", "A0 Mossy Hollow", 1f);
        room.Portal(new Vector2(137f, -6f), "a1-east", A2RoomBuilder.SceneName, "a2-west", "A2 Ancient Grove", -1f);
        room.Checkpoint(new Vector2(7f, 0f), "a1-entry");
        room.Checkpoint(new Vector2(128f, -6f), "a1-gallery");

        room.SwingRings(13.2f, 62.5f, 68.5f, 74.5f);   // under the aqueduct span, 6 u apart (the thread catches within 4 u)
        room.Thorns("Plateau Thorns", 86f, 89f, P);
        room.Thorns("Pit Thorns", 113f, 126f, -6f);
        room.Rubble(99f, P);   // before the drop: only the mace breaks it

        // A platform hung on chains from the gallery ceiling, carrying Qori across the thorn pit.
        const float deck = -4.4f, deckAboveArt = .84f;   // Platform_Moving_A1's walk line is .84 u above its pivot
        var raft = new GameObject("Hanging Platform") { layer = LayerMask.NameToLayer("Ground") };
        raft.transform.SetParent(room.mechanics, false); raft.transform.position = new Vector3(115.5f, deck - .15f, 0f);
        raft.AddComponent<BoxCollider2D>().size = new Vector2(2.9f, .3f);
        AreaRoom.Image(raft.transform, "Art", AreaRoom.Art("Hazards", "Platform_Moving_A1"), new Vector2(115.5f, deck - deckAboveArt), A0TestRoomBuilder.PropOrder + 1);
        var raftBody = raft.AddComponent<Rigidbody2D>(); raftBody.bodyType = RigidbodyType2D.Kinematic;
        var mover = raft.AddComponent<MovingPlatform>();
        mover.offset = new Vector2(8f, 0f); mover.travelSeconds = 4f; mover.solid = raft.GetComponent<Collider2D>();
        var chains = raft.AddComponent<HangingChains>();
        chains.chain = AreaRoom.Art("Hazards", "Platform_Moving_A1_Chain"); chains.ceilingY = 3f;
        chains.artTop = 1.62f - deckAboveArt + .15f;   // painted chain tops, relative to the platform

        room.Rock(110.5f, 3f); room.Rock(121.5f, 3f);   // over the walk in, and over the thorn pit
        room.SecretAlcove(new Vector2(144.5f, -3.75f), new Vector2(5f, 4.5f), -6f, new Vector2(145.6f, -5.2f), "a1-heartseed");
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
