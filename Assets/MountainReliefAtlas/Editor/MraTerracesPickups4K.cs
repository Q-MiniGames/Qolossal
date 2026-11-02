using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Exact reviewed reward visuals only. No global source replacement or reward behavior changes.
public static class MraTerracesPickups4K
{
    const string SceneFolder = "Assets/Scenes/Game/";
    const string ArtFolder = "Assets/Art/Codex/Props/";
    static readonly string[] ReviewedPaths =
    {
        SceneFolder + "MRAtlas_MR03_C01.unity",
        SceneFolder + "MRAtlas_MR03_C05.unity",
        SceneFolder + "MRAtlas_MR03_P2.unity"
    };

    public static bool IsReviewedBuildTarget(string targetPath) =>
        ReviewedPaths.Any(path => string.Equals(path, targetPath, StringComparison.Ordinal));

    public static int Apply(Scene scene, string sceneName)
    {
        string targetPath = SceneFolder + sceneName + ".unity";
        if (!IsReviewedBuildTarget(targetPath) || !scene.IsValid() || !scene.isLoaded ||
            !string.Equals(scene.path, targetPath, StringComparison.Ordinal))
            throw new InvalidOperationException("Pickup integration requires one exact reviewed canonical Terraces scene.");
        return ApplyRewardVisual(scene, targetPath);
    }

    public static int ApplyForBuild(Scene scene, string targetPath)
    {
        if (!IsReviewedBuildTarget(targetPath) || !scene.IsValid() || !scene.isLoaded ||
            (!string.IsNullOrEmpty(scene.path) && !string.Equals(scene.path, targetPath, StringComparison.Ordinal)))
            throw new InvalidOperationException("Pickup build requires an unsaved scene or the matching canonical target.");
        if (scene.GetRootGameObjects().Count(root => root.name == "Generated (MRA builder)") != 1)
            throw new InvalidOperationException("Pickup build requires exactly one generated MRA root.");
        return ApplyRewardVisual(scene, targetPath);
    }

    static int ApplyRewardVisual(Scene scene, string targetPath)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Pickup art integration is an Edit-mode operation.");
        string name = Path.GetFileNameWithoutExtension(targetPath);
        bool find = name == "MRAtlas_MR03_C05";
        string rewardId = "mra:" + name.Substring("MRAtlas_".Length) + ":reward";
        var expectedKind = find ? MraReward.Kind.Find : MraReward.Kind.HeartSeed;
        string oldName = find ? "Pickup_SapVial" : "Pickup_HeartSeed";
        string newName = oldName + "_MR01_216_B45";
        Sprite original = Load(ArtFolder + oldName + ".png", oldName);
        Sprite replacement = Load(ArtFolder + "CradleCompletion4K/" + newName + ".png", newName);
        var roots = scene.GetRootGameObjects();
        var matches = roots.SelectMany(root => root.GetComponentsInChildren<MraReward>(true))
            .Where(reward => reward.rewardId == rewardId).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("Expected exactly one reviewed reward " + rewardId);
        var reward = matches[0];
        var generatedRoots = roots.Where(root => root.name == "Generated (MRA builder)").ToArray();
        if (generatedRoots.Length != 1 || !reward.transform.IsChildOf(generatedRoots[0].transform))
            throw new InvalidOperationException("Reviewed reward must belong to the unique generated MRA root.");
        var sr = reward.image;
        if (reward.kind != expectedKind || (find && reward.title != "Wind Shelf") ||
            sr == null || sr.transform.parent != reward.transform ||
            (sr.sprite != original && sr.sprite != replacement))
            throw new InvalidOperationException("Reviewed reward kind, title, image ownership or source changed: " + rewardId);
        if (sr.drawMode != SpriteDrawMode.Simple || sr.transform.childCount != 0 ||
            sr.GetComponents<MonoBehaviour>().Length != 0 || sr.GetComponents<Collider2D>().Length != 0)
            throw new InvalidOperationException("Pickup visual no longer has reviewed static ownership.");
        if (roots.SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
            .Count(view => view.sprite == original || view.sprite == replacement) != 1)
            throw new InvalidOperationException("Additional unreviewed instances of this pickup source.");
        if (sr.sprite == replacement) return 0;

        var before = sr.bounds;
        var previousScale = sr.transform.localScale;
        var ratio = new Vector3(original.bounds.size.x / replacement.bounds.size.x,
            original.bounds.size.y / replacement.bounds.size.y, 1f);
        try
        {
            sr.sprite = replacement;
            sr.transform.localScale = Vector3.Scale(previousScale, ratio);
            if ((before.center - sr.bounds.center).magnitude + (before.size - sr.bounds.size).magnitude > .001f)
                throw new InvalidOperationException("Pickup replacement changed reviewed world bounds.");
        }
        catch
        {
            sr.sprite = original;
            sr.transform.localScale = previousScale;
            throw;
        }
        EditorUtility.SetDirty(sr);
        EditorUtility.SetDirty(sr.transform);
        if (PrefabUtility.IsPartOfPrefabInstance(sr))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sr.transform);
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        return 1;
    }

    static Sprite Load(string path, string name)
    {
        if (CodexArtImporter.Find(path) == null)
            throw new InvalidOperationException("Unaccepted pickup source " + path);
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single(sprite => sprite.name == name);
    }
}
