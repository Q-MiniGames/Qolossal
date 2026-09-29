using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds Assets/Scenes/A2_Grove.unity, the Ancient Grove, from the A2 terrain kit. West to east:
// the portal from A1 and the grove floor, where a Bark Sentinel holds the way (the spear pierces
// its shield; a strike from above or behind lands too); a slope up to the root terrace; a resin
// thorn pit crossed on a floating hollow log, with a Thornwing to pogo off; the east terrace and
// a second Sentinel; then the Great Trunk, climbed by wall jumps (Climbing Moss). From its top
// the canopy runs east over the arena: a branch, a shelf fungus and two swing rings (Living
// Thread) lead to a ledge whose end hides a heart seed behind a false wall. Below lies the Warden's
// arena (the Grove Warden arrives in the next step) with a checkpoint and the portal on to A3.
// Menu: Qolossal > Scenes > Build A2 Grove
public static class A2RoomBuilder
{
    public const string SceneName = "A2_Grove";
    const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";
    const float StartX = 3f, StartY = 1.5f;

    [MenuItem("Qolossal/Scenes/Build A2 Grove")]
    public static void Build()
    {
        var scene = A0TestRoomBuilder.NewRoom("A2 Ancient Grove", new Vector2(StartX, StartY), out Camera camera, "r3-ribwood", "R3", new Vector2(.55f, .56f));   // the rib forest
        camera.backgroundColor = new Color(.543f, .603f, .597f);   // the far painting's top row
        var room = new AreaRoom("A2");

        // Grove floor, running on under the slope; the west wall closes the room.
        room.Block("West Wall", -8f, 16f, 2f, 30f, 20, right: true);
        room.Block("Grove Floor", -6f, 0f, 66f, 14f, 0);   // runs on under the slope and the terrace's blended edge
        var slope = room.Piece("Slope 30", TerrainPiece.Kind.Slope30, 40f, 0f);
        float P = slope.SlopeRise;   // the terraces' height

        // Root terrace up to the resin pit, the pit itself, and the east terrace beyond.
        float terraceX = 40f + slope.SurfaceWidth - 1.6f;
        room.AfterSlope(slope, "Root Terrace", 72f - terraceX, P + 14f, 10, right: true);
        room.Block("Resin Pit Floor", 72f, -4f, 16f, 10f, 0);
        room.Block("East Terrace", 88f, P, 22f, P + 14f, 10, left: true);

        // The Great Trunk: climbed by wall jumps from the terrace, and dropped from into the arena.
        room.Block("Great Trunk", 110f, 22f, 3f, 36f, 20, left: true, right: true, surface: TerrainBlock.Surface.Climbable);
        // The Warden's arena floor, and the east wall.
        room.Block("Arena Floor", 113f, 0f, 55f, 14f, 0);
        room.Block("East Wall", 168f, 32f, 2f, 46f, 20, left: true);

        // The canopy over the arena: a branch walk, a shelf fungus, then a ledge whose east end is
        // a hidden niche (floor, roof and back wall), covered by a false wall.
        room.Piece("Canopy Branch", TerrainPiece.Kind.OneWay, 116f, 20f);
        room.Piece("Shelf Fungus", TerrainPiece.Kind.FloatingMedium, 127.5f, 21.5f);
        room.Block("Canopy Ledge", 145f, 21f, 8f, 5f, 30, left: true, bottom: true);
        room.Block("Niche Floor", 153f, 21f, 5f, 5f, 30, bottom: true);
        room.Block("Niche Roof", 151f, 28f, 7f, 3.5f, 30, left: true, bottom: true);
        room.Block("Niche Back", 158f, 28f, 3f, 12f, 30, right: true, bottom: true);

        room.Background(camera, "Assets/Resources/WorldBackground/AncientGrove.png", new Vector2(StartX, StartY),
            new Color(.577f, .6f, .606f), new Color(.132f, .155f, .131f));
        Color shade = new Color(.13f, .1f, .07f, 1f);   // dark heartwood, so it never reads as ground Qori could stand on
        room.BackWall("Resin Pit Back Wall", 71f, P - .5f, 18f, P + 4f, shade, .8f);
        room.BackWall("Niche Back Wall", 150.5f, 25.2f, 8.5f, 4.4f, shade);   // the dark hollow seen when the false wall fades

        BuildMechanics(room, P);
        BuildAmbience(room);

        room.Enemy("Sentinel01", new Vector2(24f, .05f), false);     // Bark Sentinel on the grove floor
        room.Enemy("GroundCreature01", new Vector2(64f, P + .4f));    // Crawler on the root terrace
        room.Enemy("Thornwing01", new Vector2(80f, P + 2.5f));        // over the resin pit: a pogo target
        room.Enemy("Sentinel01", new Vector2(99f, P + .05f), false); // a second Sentinel on the east terrace
        room.Enemy("FlyingCreature01", new Vector2(138f, 23.5f));     // Seed Carrier drifting over the canopy (an anchor too)

        // Grove floor
        room.Decor("fern", -3f, 0f); room.Decor("hollow_stump", 12f, 0f, solid: true); room.Decor("fallen_leaf", 16f, 0f, 5);
        room.Decor("grass", 19f, 0f, 5); room.Decor("root_rock_small", 31f, 0f, solid: true); room.Decor("beetle", 32.2f, 0f);
        room.Decor("hanging_roots", 34f, 12f, hanging: true);
        // Root terrace and the resin pit
        room.Decor("root_carved_block", 60f, P, solid: true); room.Decor("fern", 67.5f, P, flip: true);
        room.Decor("resin_strand", 75f, P + 5f, hanging: true); room.Decor("resin_strand", 84f, P + 5.5f, hanging: true);
        // East terrace
        room.Decor("root_rock_large", 91f, P, solid: true); room.Decor("grass", 95f, P, 5); room.Decor("broken_pillar", 105f, P, solid: true);
        // Canopy and arena
        room.Decor("shelf_fungus", 112.9f, 14f); room.Decor("fern", 147f, 21f); room.Decor("hanging_roots", 147.5f, 16f, hanging: true);
        room.Decor("fallen_leaf", 124f, 0f, 5); room.Decor("root_rock_small", 140f, 0f, solid: true); room.Decor("grass", 158f, 0f, 5);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        WorldAtlasBuilder.Record();   // after saving, so the scene has its name
        A0TestRoomBuilder.AddToBuild(ScenePath, 2);
        AssetDatabase.SaveAssets();
        Debug.Log("[A2RoomBuilder] Built " + ScenePath);
    }

    static void BuildMechanics(AreaRoom room, float P)
    {
        room.Portal(new Vector2(0f, 0f), "a2-west", A1RoomBuilder.SceneName, "a1-east", "A1 Aqueduct Ravine", 1f);
        room.Portal(new Vector2(163f, 0f), "a2-east", "A3_Falls", "a3-west", "A3 Falls Sanctuary", -1f);
        room.Checkpoint(new Vector2(7f, 0f), "a2-entry");
        room.Waymark(new Vector2(117f, 0f), "a2-arena");

        room.Thorns("Resin Thorns", 73f, 87f, -4f);
        room.SwingRings(25.5f, 135f, 141f);   // over the arena, between the shelf fungus and the ledge

        // A hollow log floating back and forth across the resin pit.
        const float deckAboveArt = .79f;   // Platform_Moving_A2's walk line is .79 u above its pivot
        float deck = P - .3f;
        var log = new GameObject("Floating Log") { layer = LayerMask.NameToLayer("Ground") };
        log.transform.SetParent(room.mechanics, false); log.transform.position = new Vector3(74f, deck - .15f, 0f);
        log.AddComponent<BoxCollider2D>().size = new Vector2(2.9f, .3f);
        AreaRoom.Image(log.transform, "Art", AreaRoom.Art("Hazards", "Platform_Moving_A2"), new Vector2(74f, deck - deckAboveArt), A0TestRoomBuilder.PropOrder + 1);
        var logBody = log.AddComponent<Rigidbody2D>(); logBody.bodyType = RigidbodyType2D.Kinematic;
        var mover = log.AddComponent<MovingPlatform>();
        mover.offset = new Vector2(12f, 0f); mover.travelSeconds = 4.5f; mover.solid = log.GetComponent<Collider2D>();

        room.SecretAlcove(new Vector2(155.5f, 23f), new Vector2(5f, 4f), 21f, new Vector2(156.8f, 21.6f), "a2-heartseed");   // tucked up under the niche roof
    }

    // Fireflies drifting in the grove's shade.
    static void BuildAmbience(AreaRoom room)
    {
        var root = new GameObject("Ambience").transform;
        Sprite firefly = AreaRoom.Art("Effects", "FX_Ambient_Firefly");
        void Swarm(string name, Rect area, int count)
        {
            var motes = new GameObject(name).AddComponent<AmbientMotes>();
            motes.transform.SetParent(root, false);
            motes.sprite = firefly; motes.area = area; motes.count = count;
        }
        Swarm("Fireflies - Grove Floor", new Rect(2f, .5f, 36f, 5f), 14);
        Swarm("Fireflies - Resin Pit", new Rect(72f, -3f, 16f, 8f), 8);
        Swarm("Fireflies - Arena", new Rect(114f, 1f, 50f, 12f), 16);
    }

    // Batch-mode review: renders the room from several camera positions.
    // Usage: -executeMethod A2RoomBuilder.Capture -captureDir <folder>
    public static void Capture()
    {
        A0TestRoomBuilder.RenderShots(ScenePath, new (string, Vector2, float)[]
        {
            ("a2_overview", new Vector2(80f, 10f), 44f), ("a2_entry", new Vector2(10f, 2.5f), 5.5f),
            ("a2_sentinel", new Vector2(24f, 1.5f), 3f), ("a2_slope", new Vector2(50f, 6f), 6f),
            ("a2_resin_pit", new Vector2(80f, 6f), 7f), ("a2_east_terrace", new Vector2(100f, 11f), 5.5f),
            ("a2_trunk", new Vector2(111f, 15f), 8f), ("a2_canopy", new Vector2(136f, 22f), 7f),
            ("a2_niche", new Vector2(153f, 23f), 4f), ("a2_arena", new Vector2(140f, 5f), 9f),
        });
    }
}
