using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds Assets/Scenes/A1_Aqueduct.unity. For now a stand-in so the A0 portal has somewhere to go:
// flat ground built from the A0 kit with walls at both ends, the return portal and a checkpoint.
// Step 9b replaces it with the real room made from the A1 kit.
// Menu: Qolossal > Scenes > Build A1 Aqueduct
public static class A1RoomBuilder
{
    public const string SceneName = "A1_Aqueduct";
    const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";

    [MenuItem("Qolossal/Scenes/Build A1 Aqueduct")]
    public static void Build()
    {
        var kit = AssetDatabase.LoadAssetAtPath<TerrainKit>("Assets/Art/Codex/Terrain/A0_TerrainKit.asset")
            ?? throw new FileNotFoundException("Build the A0 test room first (it builds the terrain kit).");
        var scene = A0TestRoomBuilder.NewRoom("A1 Aqueduct Ravine", new Vector2(0f, 1.5f), out Camera camera);
        camera.backgroundColor = new Color(.7f, .78f, .8f);
        var terrain = new GameObject("Terrain").transform;
        A0TestRoomBuilder.AddBlock(terrain, kit, "Ground", -14f, 0f, 58f, 14f, 0);
        A0TestRoomBuilder.AddBlock(terrain, kit, "West Wall", -16f, 12f, 2f, 26f, 20, right: true);
        A0TestRoomBuilder.AddBlock(terrain, kit, "East Wall", 44f, 12f, 2f, 26f, 20, left: true);

        var far = new GameObject("Far - Misty Valley").AddComponent<ParallaxLayer>();
        far.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/MistyValley_Background_v1.png");
        far.height = 16f; far.follow = new Vector2(.985f, .96f); far.baseY = -4.8f - 1.5f * far.follow.y;
        far.sortingOrder = -100; far.targetCamera = camera;

        var root = new GameObject("Mechanics").transform;
        A0TestRoomBuilder.AddPortal(root, new Vector2(-10f, 0f), "a1-west", "A0_TestRoom", "a0-east", "A0 Mossy Hollow", 1f);
        A0TestRoomBuilder.AddCheckpoint(root, new Vector2(6f, 0f), "a1-entry");

        EditorSceneManager.SaveScene(scene, ScenePath);
        A0TestRoomBuilder.AddToBuild(ScenePath, 1);
        AssetDatabase.SaveAssets();
        Debug.Log("[A1RoomBuilder] Built " + ScenePath);
    }
}
