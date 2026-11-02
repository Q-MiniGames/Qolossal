using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Three source-reviewed neutral A5 arches, at separately reviewed MR03/MR04 placements.
public static class MraRegionalPortals4K
{
    public const string TerracesPath = "Assets/Scenes/Game/MRAtlas_MR03_Terraces.unity";
    public const string QvalePath = "Assets/Scenes/Game/MRAtlas_MR04_Qvale.unity";
    public const string OriginalPath = "Assets/Art/Codex/Props/Portal_Gate_A5.png";
    public const string ReplacementPath = "Assets/Art/Codex/Props/Causeway4K/Portal_Gate_A5_MR02_4K_B46_R1b.png";
    const string Prefix = "Generated (MRA builder)/Beats and mechanics/Exits and landings/Threshold arch ";
    public static bool IsReviewedBuildTarget(string path) => path == TerracesPath || path == QvalePath;

    public static int Apply(Scene scene, string sceneName)
    {
        if (!scene.IsValid() || !scene.isLoaded || !IsReviewedBuildTarget(scene.path) ||
            sceneName != System.IO.Path.GetFileNameWithoutExtension(scene.path) || scene.name != sceneName)
            throw new InvalidOperationException("Portal application requires exact canonical MR03/MR04 main scene.");
        return ApplyValidated(scene, scene.path);
    }
    public static int ApplyForBuild(Scene scene, string targetPath)
    {
        if (!scene.IsValid() || !scene.isLoaded || !IsReviewedBuildTarget(targetPath) ||
            (!string.IsNullOrEmpty(scene.path) && scene.path != targetPath))
            throw new InvalidOperationException("Portal build requires fresh unsaved or matching canonical scene.");
        return ApplyValidated(scene, targetPath);
    }
    static string Key(Transform t) => t.parent == null ? t.name : Key(t.parent) + "/" + t.name;
    static Sprite Load(string path)
    {
        if (CodexArtImporter.Find(path) == null) throw new InvalidOperationException("Unaccepted portal source: " + path);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Missing accepted portal: " + path);
        return sprite;
    }
    static int ApplyValidated(Scene scene, string targetPath)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Portal integration is Edit-mode only.");
        var original = Load(OriginalPath); var replacement = Load(ReplacementPath);
        if (original.rect.size != new Vector2(405,480) || replacement.rect.size != new Vector2(810,960) ||
            original.pixelsPerUnit != 120 || replacement.pixelsPerUnit != 240 ||
            original.pivot != new Vector2(202.5f,240) || replacement.pivot != new Vector2(405,480) ||
            (original.bounds.center-replacement.bounds.center).magnitude +
            (original.bounds.size-replacement.bounds.size).magnitude > .00001f)
            throw new InvalidOperationException("Accepted portal registration differs from review.");
        var roots = scene.GetRootGameObjects();
        var generated = roots.Where(r => r.name == "Generated (MRA builder)").ToArray();
        if (generated.Length != 1) throw new InvalidOperationException("Exactly one generated MRA root required.");
        bool terraces = targetPath == TerracesPath;
        var expected = terraces ? new[] { "MR02_MR03", "MR03_MR04" } : new[] { "MR03_MR04" };
        var all = roots.SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>(true)).ToArray();
        var plan = new System.Collections.Generic.List<SpriteRenderer>();
        // Validate the full region plan before changing any sprite; mixed old/new is valid for idempotence.
        foreach (string link in expected)
        {
            var matches = all.Where(sr => Key(sr.transform) == Prefix + link).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Missing or duplicate portal identity: " + link);
            var sr = matches[0];
            var position = terraces ? (link == "MR02_MR03" ? new Vector3(-2.9f,13.440001f,0) : new Vector3(407.4f,93.439995f,0)) : new Vector3(-2.9f,29.439999f,0);
            if (!sr.transform.IsChildOf(generated[0].transform) || sr.transform.childCount != 0 ||
                sr.GetComponents<Component>().Length != 2 || sr.drawMode != SpriteDrawMode.Simple ||
                !sr.enabled || !sr.gameObject.activeInHierarchy || sr.flipX || sr.flipY || sr.color != Color.white ||
                sr.sortingLayerID != 0 || sr.sortingOrder != -25 ||
                (sr.sprite != original && sr.sprite != replacement) ||
                (sr.transform.localScale-new Vector3(.975f,.975f,1)).magnitude > .000001f ||
                (sr.transform.position-position).magnitude > .0002f ||
                Quaternion.Angle(sr.transform.rotation,Quaternion.identity) > .0001f ||
                (sr.transform.lossyScale-new Vector3(.975f,.975f,1)).magnitude > .000001f)
                throw new InvalidOperationException("Portal visual ownership, placement or rendering contract differs: " + link);
            plan.Add(sr);
        }
        if (all.Count(sr => sr.sprite == original || sr.sprite == replacement) != expected.Length)
            throw new InvalidOperationException("Unexpected additional A5 portal binding in target scene.");
        var old = plan.Select(sr => sr.sprite).ToArray(); var bounds = plan.Select(sr => sr.bounds).ToArray();
        int changes = 0;
        try
        {
            for (int i=0;i<plan.Count;i++)
            {
                if (plan[i].sprite == replacement) continue;
                plan[i].sprite = replacement; changes++;
                if ((plan[i].bounds.center-bounds[i].center).magnitude + (plan[i].bounds.size-bounds[i].size).magnitude > .00001f)
                    throw new InvalidOperationException("Portal world bounds changed.");
            }
        }
        catch { for(int i=0;i<plan.Count;i++) plan[i].sprite=old[i]; throw; }
        if(changes>0)
        {
            foreach(var sr in plan) EditorUtility.SetDirty(sr);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }
        return changes;
    }
}
