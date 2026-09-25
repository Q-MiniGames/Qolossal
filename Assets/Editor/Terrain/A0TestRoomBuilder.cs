using System;
using System.IO;
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
        plateauBlock.Rebuild();
        Piece("One-way Log", TerrainPiece.Kind.OneWay, 30f, plateau + 2.2f);
        Block("Overhang", 32f, plateau + 7.4f, 10f, 4.5f, 30, left: true, right: true, bottom: true);

        // Pit: a lower floor crossed by floating platforms.
        Block("Pit Floor", 26f, -5f, 44f, 9f, 0);
        Piece("Floating L", TerrainPiece.Kind.FloatingLarge, 45.5f, 4f);
        Piece("Floating M", TerrainPiece.Kind.FloatingMedium, 50.8f, 5.5f);
        Piece("Floating S", TerrainPiece.Kind.FloatingSmall, 47f, 0f);

        // Wall columns: wall jumps work on the climbable one and not on the slippery one.
        Block("Climbable Column", 54f, 5f, 3f, 10f, 20, surface: TerrainBlock.Surface.Climbable);
        Block("Slippery Column", 60f, 5f, 3f, 10f, 20, surface: TerrainBlock.Surface.Slippery);

        // End: a tall rock plateau with ledges on both sides, reached by climbing its left face.
        Block("End Plateau", 66f, 9f, 20f, 23f, 10, left: true, right: true);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[A0TestRoomBuilder] Built " + ScenePath);
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
        };
        var texture = new RenderTexture(1920, 1080, 24);
        var read = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        foreach (var shot in shots)
        {
            camera.transform.position = new Vector3(shot.at.x, shot.at.y, -10f);
            camera.orthographicSize = shot.size;
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
