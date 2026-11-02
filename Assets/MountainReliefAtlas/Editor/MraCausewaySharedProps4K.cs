using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Four accepted neutral props plus the independently reviewed Causeway-only vent.
// Context validation required before any builder hook or canonical scene save.
public static class MraCausewaySharedProps4K
{
    static readonly string[] ReviewedScenes = { "MRAtlas_MR02_C01", "MRAtlas_MR02_C02", "MRAtlas_MR02_C03", "MRAtlas_MR02_C04", "MRAtlas_MR02_C05", "MRAtlas_MR02_C06", "MRAtlas_MR02_Causeway", "MRAtlas_MR02_P1" };
    const string CanonicalSceneFolder = "Assets/Scenes/Game/";

    // Ordinary previews require an already named/saved scene; keep this entry strict.
    public static int Apply(Scene scene, string sceneName)
    {
        if (!scene.IsValid() || !scene.isLoaded || !ReviewedScenes.Contains(sceneName) ||
            !string.Equals(scene.path, CanonicalSceneFolder + sceneName + ".unity", StringComparison.Ordinal))
            throw new InvalidOperationException("Shared prop preview restricted to reviewed MR02 scene inventory.");
        return ApplyReplacements(scene, sceneName);
    }

    public static bool IsReviewedBuildTarget(string targetPath)
    {
        // Exact canonical asset paths only: no absolute, alternate-folder or traversal aliases.
        return ReviewedScenes.Any(name => string.Equals(targetPath,
            CanonicalSceneFolder + name + ".unity", StringComparison.Ordinal));
    }

    // Builder-only path: a fresh unsaved scene is allowed when its explicit target is canonical.
    // This method never saves a scene. The caller saves only after successful replacement.
    public static int ApplyForBuild(Scene scene, string targetPath)
    {
        if (!IsReviewedBuildTarget(targetPath))
            throw new InvalidOperationException("Unreviewed MR02 build target: " + targetPath);
        if (!scene.IsValid() || !scene.isLoaded ||
            (!string.IsNullOrEmpty(scene.path) && !string.Equals(scene.path, targetPath, StringComparison.Ordinal)))
            throw new InvalidOperationException("Build scene must be unsaved or match the exact canonical target.");
        if (scene.GetRootGameObjects().Count(root => root.name == "Generated (MRA builder)") != 1)
            throw new InvalidOperationException("Build requires exactly one generated MRA root.");
        return ApplyReplacements(scene, Path.GetFileNameWithoutExtension(targetPath));
    }

    static int ApplyReplacements(Scene scene, string sceneName)
    {
        var map = new Dictionary<Sprite, Sprite>();
        map.Add(Load("Assets/Art/Codex/Props/Anchor_Ring_A1.png", "Anchor_Ring_A1"), Load("Assets/Art/Codex/Props/CradleCompletion4K/Anchor_Ring_A1_MR01_216_B45.png", "Anchor_Ring_A1_MR01_216_B45"));
        map.Add(Load("Assets/Art/Codex/Props/Checkpoint_Shrine_A1.png", "Checkpoint_Shrine_A1"), Load("Assets/Art/Codex/Props/CradleCompletion4K/Checkpoint_Shrine_A1_MR01_216_B45.png", "Checkpoint_Shrine_A1_MR01_216_B45"));
        map.Add(Load("Assets/Art/Codex/Props/Pickup_HeartSeed.png", "Pickup_HeartSeed"), Load("Assets/Art/Codex/Props/CradleCompletion4K/Pickup_HeartSeed_MR01_216_B45.png", "Pickup_HeartSeed_MR01_216_B45"));
        map.Add(Load("Assets/Art/Codex/Props/Pickup_SapVial.png", "Pickup_SapVial"), Load("Assets/Art/Codex/Props/CradleCompletion4K/Pickup_SapVial_MR01_216_B45.png", "Pickup_SapVial_MR01_216_B45"));
        if (sceneName == "MRAtlas_MR02_Causeway")
            map.Add(Load("Assets/Art/Codex/Mechanics/Breath_Vent_Exhale.png", "Breath_Vent_Exhale"), Load("Assets/Art/Codex/Mechanics/Causeway4K/Breath_Vent_Exhale_MR02_4K_B46_R1.png", "Breath_Vent_Exhale_MR02_4K_B46_R1"));
        if (sceneName == "MRAtlas_MR02_Causeway")
            map.Add(Load("Assets/Art/Codex/Props/Portal_Gate_A5.png", "Portal_Gate_A5"), Load("Assets/Art/Codex/Props/Causeway4K/Portal_Gate_A5_MR02_4K_B46_R1b.png", "Portal_Gate_A5_MR02_4K_B46_R1b"));
        int changes = 0;
        foreach (var sr in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>(true)).ToArray())
        {
            if (sr.sprite == null || !map.TryGetValue(sr.sprite, out var next)) continue;
            if (sr.drawMode != SpriteDrawMode.Simple) throw new InvalidOperationException("Unreviewed sprite draw mode: " + sr.name);
            var old = sr.sprite; Bounds before = sr.bounds;
            var scale = new Vector3(old.bounds.size.x / next.bounds.size.x, old.bounds.size.y / next.bounds.size.y, 1f);
            SpriteRenderer target = sr;
            if (sr.GetComponentsInChildren<Collider2D>(true).Length > 0)
            {
                if (!sr.TryGetComponent<Checkpoint>(out var checkpoint)) throw new InvalidOperationException("Unreviewed visual/collider ownership: " + sr.name);
                var child = new GameObject("4K Shrine Art"); child.layer = sr.gameObject.layer; child.transform.SetParent(sr.transform, false); child.transform.localScale = scale;
                target = child.AddComponent<SpriteRenderer>(); target.sprite = next; target.sharedMaterials = sr.sharedMaterials;
                target.color = sr.color; target.sortingLayerID = sr.sortingLayerID; target.sortingOrder = sr.sortingOrder;
                target.flipX = sr.flipX; target.flipY = sr.flipY; target.maskInteraction = sr.maskInteraction; target.spriteSortPoint = sr.spriteSortPoint;
                target.enabled = sr.enabled;
                using (var so = new SerializedObject(checkpoint))
                {
                    so.FindProperty("shrineRenderer").objectReferenceValue = target;
                    so.FindProperty("shrineArt").objectReferenceValue = next;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                sr.enabled = false; sr.sprite = null;
            }
            else
            {
                // Keep the renderer object so reward/anchor animation and collection references stay valid.
                sr.sprite = next; sr.transform.localScale = Vector3.Scale(sr.transform.localScale, scale);
            }
            float delta = (before.center - target.bounds.center).magnitude + (before.size - target.bounds.size).magnitude;
            if (delta > .001f) throw new InvalidOperationException("Replacement changed visual bounds: " + old.name + " " + delta);
            EditorUtility.SetDirty(sr); EditorUtility.SetDirty(target); EditorUtility.SetDirty(target.transform);
            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                PrefabUtility.RecordPrefabInstancePropertyModifications(target.transform);
            }
            changes++;
        }

        if (changes > 0) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        return changes;
    }

    static Sprite Load(string path, string name)
    {
        if (CodexArtImporter.Find(path) == null) throw new InvalidOperationException("Unaccepted prop source " + path);
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single(s => s.name == name);
    }
}
