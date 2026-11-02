using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Exact two reviewed Terraces A5 checkpoints. No global A5 replacement or trigger/respawn changes.
public static class MraTerracesShrines4K
{
    public const string TargetPath = "Assets/Scenes/Game/MRAtlas_MR03_Terraces.unity";
    const string OriginalPath = "Assets/Art/Codex/Props/Checkpoint_Shrine_A5.png";
    const string ReplacementPath = "Assets/Art/Codex/Props/Qvale4K/Checkpoint_Shrine_A5_MR04_4K_B48_R1.png";
    const string WaymarkPath = "Assets/Art/Codex/WorldSystems/Waymark_Dormant.png";
    static readonly string[] TargetIds = { "mra:MR03:cp:01", "mra:MR03:cp:10" };

    public static bool IsReviewedBuildTarget(string path) => string.Equals(path, TargetPath, StringComparison.Ordinal);

    public static int Apply(Scene scene, string sceneName)
    {
        if (sceneName != "MRAtlas_MR03_Terraces" || !scene.IsValid() || !scene.isLoaded || scene.path != TargetPath)
            throw new InvalidOperationException("Shrine integration requires the exact canonical Terraces scene.");
        return ApplyValidated(scene);
    }

    public static int ApplyForBuild(Scene scene, string path)
    {
        if (!IsReviewedBuildTarget(path) || !scene.IsValid() || !scene.isLoaded ||
            (!string.IsNullOrEmpty(scene.path) && scene.path != TargetPath))
            throw new InvalidOperationException("Shrine build requires a fresh unsaved scene or exact canonical Terraces target.");
        return ApplyValidated(scene);
    }

    sealed class Assignment
    {
        public Checkpoint checkpoint;
        public SpriteRenderer renderer;
        public Sprite original;
        public Bounds bounds;
    }

    static int ApplyValidated(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Shrine art integration is Edit-mode only.");
        var roots = scene.GetRootGameObjects();
        var generated = roots.Where(root => root.name == "Generated (MRA builder)").ToArray();
        if (generated.Length != 1)
            throw new InvalidOperationException("Exactly one generated MRA root is required.");
        var checkpoints = roots.SelectMany(root => root.GetComponentsInChildren<Checkpoint>(true)).ToArray();
        var expectedIds = TargetIds.Concat(new[] { "mra:MR03:cp:05" }).OrderBy(id => id).ToArray();
        if (!checkpoints.Select(cp => cp.CheckpointId).OrderBy(id => id).SequenceEqual(expectedIds))
            throw new InvalidOperationException("Expected exactly cp01, cp05 and cp10, with no missing or duplicate checkpoint IDs.");
        var original = Load(OriginalPath, "Checkpoint_Shrine_A5");
        var replacement = Load(ReplacementPath, "Checkpoint_Shrine_A5_MR04_4K_B48_R1");
        var waymark = Load(WaymarkPath, "Waymark_Dormant");
        if ((original.bounds.center - replacement.bounds.center).magnitude +
            (original.bounds.size - replacement.bounds.size).magnitude > .00001f ||
            original.rect.size / original.pixelsPerUnit != replacement.rect.size / replacement.pixelsPerUnit)
            throw new InvalidOperationException("Reviewed shrine authored registration changed.");

        // Build the complete validated plan before mutating any checkpoint.
        var plan = new System.Collections.Generic.List<Assignment>();
        foreach (var cp in checkpoints)
        {
            if (!cp.transform.IsChildOf(generated[0].transform))
                throw new InvalidOperationException("Checkpoint is outside the generated MRA root.");
            var sr = cp.GetComponent<SpriteRenderer>();
            var colliders = cp.GetComponents<Collider2D>();
            using (var so = new SerializedObject(cp))
            {
                var art = so.FindProperty("shrineArt");
                if (so.FindProperty("checkpointId").stringValue != cp.CheckpointId ||
                    sr == null || sr.drawMode != SpriteDrawMode.Simple || sr.transform.childCount != 0 ||
                    so.FindProperty("shrineRenderer").objectReferenceValue != null ||
                    colliders.Length != 1 || !(colliders[0] is BoxCollider2D) || !colliders[0].isTrigger)
                    throw new InvalidOperationException("Reviewed checkpoint ownership or trigger contract changed.");
                var painted = art.objectReferenceValue as Sprite;
                if (cp.CheckpointId == "mra:MR03:cp:05")
                {
                    if (painted != waymark || sr.sprite != waymark)
                        throw new InvalidOperationException("Unrelated cp05 Waymark binding changed.");
                    continue;
                }
                if ((painted != original && painted != replacement) || sr.sprite != painted)
                    throw new InvalidOperationException("Shrine runtime and renderer bindings must match the original or reviewed replacement.");
                plan.Add(new Assignment { checkpoint = cp, renderer = sr, original = painted, bounds = sr.bounds });
            }
        }
        if (plan.Count != 2 || roots.SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
                .Count(sr => sr.sprite == original || sr.sprite == replacement) != 2)
            throw new InvalidOperationException("Additional unreviewed A5 shrine visual or missing reviewed target.");
        var changes = plan.Where(item => item.original == original).ToArray();
        if (changes.Length == 0) return 0;
        try
        {
            foreach (var item in changes)
            {
                SetArt(item.checkpoint, replacement);
                item.renderer.sprite = replacement;
                if ((item.renderer.bounds.center - item.bounds.center).magnitude +
                    (item.renderer.bounds.size - item.bounds.size).magnitude > .00001f)
                    throw new InvalidOperationException("Shrine authored world bounds changed.");
            }
        }
        catch
        {
            foreach (var item in changes)
            {
                SetArt(item.checkpoint, item.original);
                item.renderer.sprite = item.original;
            }
            throw;
        }
        foreach (var item in changes)
        {
            EditorUtility.SetDirty(item.checkpoint);
            EditorUtility.SetDirty(item.renderer);
            if (PrefabUtility.IsPartOfPrefabInstance(item.checkpoint))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(item.checkpoint);
                PrefabUtility.RecordPrefabInstancePropertyModifications(item.renderer);
            }
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        return changes.Length;
    }

    static void SetArt(Checkpoint cp, Sprite sprite)
    {
        using (var so = new SerializedObject(cp))
        {
            so.FindProperty("shrineArt").objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static Sprite Load(string path, string name)
    {
        if (CodexArtImporter.Find(path) == null)
            throw new InvalidOperationException("Unaccepted shrine/Waymark source " + path);
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single(sprite => sprite.name == name);
    }
}
