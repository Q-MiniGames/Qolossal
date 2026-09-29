using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds Assets/Scenes/R1_GripKnot.unity, a placeholder Grip Knot chamber (the Palm's Knot
// Chamber, world design 3.3) from the A0 kit, to try out the knot flow before the titan art and
// the real R1 levels exist. West to east: the Vein Gate from A0 and a Waymark; the guardian's
// floor, where a Bramble Crawler grown to twice its size stands in for the Knucklebramble; the
// knot on its dais, sealed in thorns while the guardian lives; and a dormant arch where, once
// the Grip Knot wakes and the hand clenches, a new vein to the Arm (A1) grows.
// Menu: Qolossal > Scenes > Build R1 Grip Knot
public static class R1GripKnotBuilder
{
    public const string SceneName = "R1_GripKnot";
    const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";
    const float StartX = 3f, StartY = 1.5f;

    [MenuItem("Qolossal/Scenes/Build R1 Grip Knot")]
    public static void Build()
    {
        var scene = A0TestRoomBuilder.NewRoom("The Grip Knot", new Vector2(StartX, StartY), out Camera camera, "r1-grip-knot", "R1", new Vector2(.46f, .43f));   // the heel of the palm
        camera.backgroundColor = new Color(.72f, .79f, .77f);
        var room = new AreaRoom("A0");

        room.Block("West Wall", -8f, 16f, 2f, 30f, 20, right: true);
        room.Block("Chamber Floor", -6f, 0f, 70f, 14f, 0);
        room.Block("Chamber Roof", -6f, 12f, 70f, 4f, 30, bottom: true);
        room.Block("East Wall", 64f, 16f, 2f, 30f, 20, left: true);
        room.Block("Knot Dais", 44f, 1.2f, 10f, 3f, 5, left: true, right: true);

        room.Background(camera, "Assets/Art/Backgrounds/MistyValley_Background_v1.png", new Vector2(StartX, StartY),
            new Color(.668f, .717f, .749f), new Color(.741f, .815f, .867f));
        room.BackWall("Chamber Back Wall", -6f, 11f, 70f, 11.4f, new Color(.16f, .19f, .14f, 1f));

        room.Portal(new Vector2(0f, 0f), "r1k-west", "A0_TestRoom", "a0-knot", "A0 Mossy Hollow", 1f);
        room.Waymark(new Vector2(7f, 0f), "r1k-waymark");

        // The guardian: a Crawler grown to twice its size, sturdier and roaming the whole floor.
        // It is part of the chamber only before the knot wakes; afterwards the chamber is quiet.
        GameObject guardian;
        using (room.Stir("Guardian", Knots.Grip, afterKnot: false))
        {
            guardian = room.Enemy("GroundCreature01", new Vector2(28f, .8f), false);
            guardian.name = "Knucklebramble (stand-in)";
            guardian.transform.localScale = new Vector3(2f, 2f, 1f);
            var settings = new SerializedObject(guardian.GetComponent<GroundCreature>());
            settings.FindProperty("maximumHealth").intValue = 8;
            settings.FindProperty("patrolHalfWidth").floatValue = 6f;
            settings.FindProperty("detectRange").floatValue = 7f;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        var moss = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Resources/Relics/ClimbingMoss.asset");
        room.Knot(new Vector2(49f, 1.2f), Knots.Grip, moss, new[]
        {
            "...a hand. I had forgotten I had hands.",
            "Little seed... is that you?",
        }, guardian);

        // A finger-bone gate that opens when the hand clenches, and beyond it where the stir
        // grows a vein to the Arm.
        room.StirGate(new Vector2(56f, 0f), Knots.Grip, "A0");
        using (room.Stir("Elbow Vein (dormant)", Knots.Grip, afterKnot: false)) room.DormantGate(new Vector2(59f, 0f));
        using (room.Stir("Elbow Vein", Knots.Grip, afterKnot: true))
            room.GrownVein(new Vector2(59f, 0f), "r1k-elbow", A1RoomBuilder.SceneName, "a1-palm", "A1 Aqueduct Ravine", -1f);

        room.Decor("fern", -3f, 0f); room.Decor("boulder", 12f, 0f, solid: true); room.Decor("grass", 16f, 0f, 5);
        room.Decor("mushrooms", 38f, 0f); room.Decor("small_rock", 41.5f, 0f, solid: true);
        room.Decor("hanging_ivy", 22f, 8f, hanging: true); room.Decor("hanging_root", 47f, 8f, hanging: true); room.Decor("hanging_ivy", 57f, 8f, hanging: true);
        room.Decor("white_flowers", 45.5f, 1.2f); room.Decor("grass", 52.8f, 1.2f, 5);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        WorldAtlasBuilder.Record();   // after saving, so the scene has its name
        A0TestRoomBuilder.AddToBuild(ScenePath, 3);
        AssetDatabase.SaveAssets();
        Debug.Log("[R1GripKnotBuilder] Built " + ScenePath);
    }

    // Batch-mode review: renders the chamber from several camera positions.
    // Usage: -executeMethod R1GripKnotBuilder.Capture -captureDir <folder>
    public static void Capture()
    {
        A0TestRoomBuilder.RenderShots(ScenePath, new (string, Vector2, float)[]
        {
            ("r1k_overview", new Vector2(29f, 5f), 20f), ("r1k_entry", new Vector2(4f, 2.5f), 5f),
            ("r1k_guardian", new Vector2(28f, 2.5f), 5f), ("r1k_knot", new Vector2(50f, 3f), 5f),
        });
    }
}
