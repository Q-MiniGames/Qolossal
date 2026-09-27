using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Keeps Assets/Resources/World/WorldAtlas.asset up to date: each room builder records its scene
// as it saves it (Record), and Qolossal > World > Rebuild Atlas records every scene in the build.
public static class WorldAtlasBuilder
{
    const string AtlasPath = "Assets/Resources/World/WorldAtlas.asset";

    static WorldAtlas Atlas()
    {
        var atlas = AssetDatabase.LoadAssetAtPath<WorldAtlas>(AtlasPath);
        if (atlas != null) return atlas;
        Directory.CreateDirectory(Path.GetDirectoryName(AtlasPath));
        atlas = ScriptableObject.CreateInstance<WorldAtlas>();
        AssetDatabase.CreateAsset(atlas, AtlasPath);
        return atlas;
    }

    // Records the open scene: its GameArea, its Waymark and its portals (including those inside
    // stir variants, with the knot they depend on).
    public static void Record()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var area = Object.FindAnyObjectByType<GameArea>(FindObjectsInactive.Include);
        if (area == null) { Debug.LogWarning($"[WorldAtlas] {scene.name} has no GameArea; not recorded"); return; }
        var atlas = Atlas();
        atlas.levels.RemoveAll(l => string.IsNullOrEmpty(l.scene) || l.scene == scene.name);
        var level = new WorldAtlas.Level { scene = scene.name, levelId = area.levelId, displayName = area.displayName, region = area.region };
        var waymarks = Object.FindObjectsByType<Waymark>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (waymarks.Length > 1) Debug.LogWarning($"[WorldAtlas] {scene.name} has {waymarks.Length} Waymarks; only one charts the level");
        if (waymarks.Length > 0) level.waymark = waymarks[0].GetComponent<Checkpoint>().CheckpointId;
        foreach (var portal in Object.FindObjectsByType<Portal>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(p => p.portalId))
        {
            var variant = portal.GetComponentInParent<StirVariant>(true);
            level.veins.Add(new WorldAtlas.VeinEnd
            {
                portalId = portal.portalId, destinationScene = portal.destinationScene, destinationPortal = portal.destinationPortal,
                wild = portal.vein == Portal.Vein.Wild,
                knot = variant != null ? variant.knot : "", afterKnot = variant != null && variant.when == StirVariant.When.AfterKnot,
            });
        }
        atlas.levels.Add(level);
        atlas.levels.Sort((a, b) => string.CompareOrdinal(a.scene, b.scene));
        EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssets();
        Debug.Log($"[WorldAtlas] recorded {scene.name}: level {level.levelId}, Waymark '{level.waymark}', {level.veins.Count} vein end(s)");
    }

    // Rebuilds every scene of the world as it stands (each records itself in the atlas).
    [MenuItem("Qolossal/World/Rebuild World Scenes")]
    public static void BuildAllScenes()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        A0TestRoomBuilder.Build();
        A1RoomBuilder.Build();
        A2RoomBuilder.Build();
        R1GripKnotBuilder.Build();
    }

    [MenuItem("Qolossal/World/Rebuild Atlas")]
    public static void RebuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string open = EditorSceneManager.GetActiveScene().path;
        foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            EditorSceneManager.OpenScene(entry.path);
            Record();
        }
        if (!string.IsNullOrEmpty(open)) EditorSceneManager.OpenScene(open);
    }
}
