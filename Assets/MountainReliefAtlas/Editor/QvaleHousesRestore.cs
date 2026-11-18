using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Brings Qvale's five homes back as their own scenes (the user's word, 3 Nov 2026), after the cut to 8 shipping scenes
// emptied them onto the street. Only touches Qvale: builds the rooms with BuildHouse, gives each street door its way in,
// and removes the residents and shops left at the street scene's root, since the rooms hold them now. mra_world.json
// must already mark the homes "house-instance". Run: -batchmode -quit -executeMethod MraWorldBuilder.RestoreQvaleHouses
public static partial class MraWorldBuilder
{
    static readonly string[] StreetResidents = { "NPC Sorrel", "NPC Scribble", "NPC Pip", "NPC Marrow", "NPC Grandfather Tallow" };

    [MenuItem("Qolossal/Qvale/Restore House Interiors")]
    public static void RestoreQvaleHouses()
    {
        Prepare();
        var houses = world.ChambersOf("MR04").Where(c => c.IsHouse).ToArray();
        if (houses.Length != 5) throw new System.InvalidOperationException($"Expected 5 Qvale homes in mra_world.json, found {houses.Length}.");
        foreach (var c in houses) BuildHouse(c);

        var street = EditorSceneManager.OpenScene(ScenePath("MRAtlas_MR04_Qvale"), OpenSceneMode.Single);
        var all = street.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (var c in houses)
        {
            string name = $"Door {c.id} {c.name}";
            var obj = all.Single(t => t.name == name).gameObject;
            if (!obj.TryGetComponent(out BoxCollider2D _)) throw new MissingComponentException(name + " has no trigger.");
            if (!obj.TryGetComponent(out MraChamberDoor door)) door = obj.AddComponent<MraChamberDoor>();
            door.chamberId = c.id; door.chamberName = c.name; door.chamberScene = c.scene; door.prerequisite = c.prerequisite;
            door.promptHeight = 2.6f;
            EditorUtility.SetDirty(door);
        }
        foreach (var root in street.GetRootGameObjects())
            if (StreetResidents.Contains(root.name)) Object.DestroyImmediate(root);
        EditorSceneManager.MarkSceneDirty(street);
        EditorSceneManager.SaveScene(street);

        EditorBuildSettings.scenes = RegionalSceneCatalog.BuildPaths.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log($"[QvaleHouses] {houses.Length} rooms built, doors connected, {EditorBuildSettings.scenes.Length} scenes in the build.");
    }
}
