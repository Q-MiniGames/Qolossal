using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Reviewed Batch43b replacements, scoped to Cradle. Original sprites and all reference-pixel
// terrain registration remain intact. Used both for the existing scene and future rebuilds.
public static class MraCradle4K
{
    public const string ReceiptPath = "Tools/ArtImport/batch43b_accepted.json";
    [Serializable] public class Receipt { public Replacement[] replacements; }
    [Serializable] public class Replacement { public string original, replacement; public float[] scale; }

    public static int Apply(Scene scene, bool newCradleScene = false)
    {
        if (scene.path != MraWorldBuilder.SceneFolder + "MRAtlas_MR01_Cradle.unity" && !(newCradleScene && string.IsNullOrEmpty(scene.path)))
            throw new InvalidOperationException("Batch43b replacements are scoped to MR01 Cradle.");
        if (!File.Exists(ReceiptPath)) return 0;
        var receipt = JsonUtility.FromJson<Receipt>(File.ReadAllText(ReceiptPath));
        var map = new Dictionary<Sprite, (Sprite sprite, Vector3 scale)>();
        foreach (var entry in receipt.replacements)
        {
            var old = AssetDatabase.LoadAssetAtPath<Sprite>(entry.original);
            var next = AssetDatabase.LoadAssetAtPath<Sprite>(entry.replacement);
            if (old == null || next == null) throw new InvalidOperationException("Missing reviewed sprite: " + entry.replacement);
            if (CodexArtImporter.Find(entry.replacement) == null) throw new InvalidOperationException("Sprite is outside the accepted manifest: " + entry.replacement);
            map.Add(old, (next, new Vector3(entry.scale[0], entry.scale[1], 1f)));
        }
        int changes = 0;
        var fields = typeof(MraTerrain).GetFields();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var terrain in root.GetComponentsInChildren<MraTerrain>(true))
            {
                bool dirty = false;
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(Sprite))
                    {
                        var old = (Sprite)field.GetValue(terrain);
                        if (old != null && map.TryGetValue(old, out var next))
                        { field.SetValue(terrain, next.sprite); dirty = true; changes++; }
                    }
                    else if (field.FieldType == typeof(Sprite[]))
                    {
                        var sprites = (Sprite[])field.GetValue(terrain);
                        if (sprites == null) continue;
                        for (int i = 0; i < sprites.Length; i++)
                            if (sprites[i] != null && map.TryGetValue(sprites[i], out var next))
                            { sprites[i] = next.sprite; dirty = true; changes++; }
                    }
                }
                if (dirty) { terrain.Rebuild(); EditorUtility.SetDirty(terrain); }
            }
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.sprite != null && map.TryGetValue(renderer.sprite, out var next))
                {
                    renderer.sprite = next.sprite;
                    renderer.transform.localScale = Vector3.Scale(renderer.transform.localScale, next.scale);
                    EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(renderer.transform); changes++;
                }
        }
        if (changes > 0) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        return changes;
    }
}
