using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Reviewed Batch45 source replacements and opt-in MR01 rendering. Old accepted sources remain intact.
public static class MraCradleCompletion
{
    public const string ReceiptPath = "Tools/ArtImport/batch45_accepted.json";
    [Serializable] public class Receipt { public Replacement[] replacements; }
    [Serializable] public class Replacement { public string original, originalName, replacement, replacementName; }

    public static int Apply(Scene scene, string sceneName)
    {
        var allowed = new[] { "MRAtlas_MR01_Cradle", "MRAtlas_MR01_C01", "MRAtlas_MR01_C02", "MRAtlas_MR01_C03", "MRAtlas_MR01_C04", "MRAtlas_MR01_C05", "MRAtlas_MR01_C06" };
        if (!allowed.Contains(sceneName) || (!string.IsNullOrEmpty(scene.path) && Path.GetFileNameWithoutExtension(scene.path) != sceneName))
            throw new InvalidOperationException("Batch45 is restricted to the reviewed MR01 scenes.");
        if (!File.Exists(ReceiptPath)) return 0;
        var map = new Dictionary<Sprite, Sprite>();
        var receipt = JsonUtility.FromJson<Receipt>(File.ReadAllText(ReceiptPath));
        foreach (var entry in receipt.replacements)
        {
            var old = Load(entry.original, entry.originalName);
            var next = Load(entry.replacement, entry.replacementName);
            if (CodexArtImporter.Find(entry.replacement) == null) throw new InvalidOperationException("Unaccepted replacement " + entry.replacement);
            map.Add(old, next);
        }
        int changes = 0;
        var terrains = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MraTerrain>(true)).ToArray();
        foreach (var terrain in terrains)
        {
            bool changed = false;
            foreach (var field in typeof(MraTerrain).GetFields())
            {
                if (field.FieldType == typeof(Sprite))
                {
                    var old = (Sprite)field.GetValue(terrain);
                    if (old != null && map.TryGetValue(old, out var next)) { field.SetValue(terrain, next); changed = true; changes++; }
                }
                else if (field.FieldType == typeof(Sprite[]))
                {
                    var original = (Sprite[])field.GetValue(terrain); if (original == null) continue;
                    var values = (Sprite[])original.Clone(); bool arrayChanged = false;
                    for (int i = 0; i < values.Length; i++)
                        if (values[i] != null && map.TryGetValue(values[i], out var next)) { values[i] = next; arrayChanged = changed = true; changes++; }
                    if (arrayChanged) field.SetValue(terrain, values);
                }
            }
            if (changed) EditorUtility.SetDirty(terrain);
        }
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
        bool chamber = sceneName != "MRAtlas_MR01_Cradle";
        if (chamber)
        {
            var back = terrains.Single(t => t.name == "Back wall");
            var ceiling = terrains.Single(t => t.name == "Ceiling");
            var floor = terrains.First(t => t.name == "Catch floor");
            foreach (var t in terrains)
            {
                if (!t.isActiveAndEnabled || t.fill != back.fill) continue;
                if (t != back)
                {
                    t.chamberBackdrop = back; t.chamberBlendLeft = t.name != "West rock"; t.chamberBlendRight = t.name != "East rock";
                    t.chamberSolidColumn = t.name.StartsWith("Climbing wall", StringComparison.Ordinal);
                    t.face = t.fill; t.faceEdgeU = 0f; t.faceDepth = .5f; t.roughContour = true; t.riserFaces = true;
                    t.contourReach = t.chamberSolidColumn ? .25f : .12f;
                    if (t.chamberSolidColumn) { t.top = floor.top; t.under = ceiling.under; t.underTint = ceiling.underTint; }
                }
                if (t.under != null) t.fullUnderSilhouette = true;
                EditorUtility.SetDirty(t);
            }
            // Coplanar transparent Top meshes otherwise depend on renderer creation order.
            // Keep all geometry and other parts unchanged, below the chamber props.
            string HierarchyKey(Transform t) => t.parent == null ? t.GetSiblingIndex().ToString("D5") : HierarchyKey(t.parent) + "/" + t.GetSiblingIndex().ToString("D5");
            var tops = terrains.Where(t => t.isActiveAndEnabled && t.chamberBackdrop == back && t.top != null)
                .OrderBy(t => t.transform.TransformPoint(t.surface[0]).y)
                .ThenBy(t => t.transform.TransformPoint(t.surface[0]).x)
                .ThenBy(t => t.name, StringComparer.Ordinal).ThenBy(t => HierarchyKey(t.transform), StringComparer.Ordinal).ToArray();
            for (int i = 0; i < tops.Length; i++)
            {
                tops[i].chamberTopPriority = 1 + i - tops[i].sortingOffset;
                EditorUtility.SetDirty(tops[i]);
            }
            var foreground = terrains.Where(t => t.isActiveAndEnabled && t.chamberBackdrop == back).ToArray();
            // Reconstruct logical order before assigning unique per-part ranks.
            foreach (var t in foreground) { t.chamberPartLabels = new string[0]; t.chamberPartOrders = new int[0]; t.Rebuild(); }
            var parts = foreground.SelectMany(t => t.GetComponentsInChildren<MeshRenderer>().Where(r => r.transform.parent == t.transform)
                .Select(r => new { terrain = t, renderer = r, label = r.name.Replace(" (generated)", "") }))
                .OrderBy(p => p.renderer.sortingOrder)
                .ThenBy(p => p.terrain.transform.TransformPoint(p.terrain.surface[0]).y)
                .ThenBy(p => p.terrain.transform.TransformPoint(p.terrain.surface[0]).x)
                .ThenBy(p => p.terrain.name, StringComparer.Ordinal)
                .ThenBy(p => HierarchyKey(p.terrain.transform), StringComparer.Ordinal)
                .ThenBy(p => p.label, StringComparer.Ordinal).ToArray();
            if (parts.Length > 45) throw new InvalidOperationException("Chamber part count exceeds reviewed ordering band.");
            var participants = new HashSet<Renderer>(parts.Select(p => (Renderer)p.renderer));
            foreach (var renderer in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Renderer>(true)))
                if (!participants.Contains(renderer) && renderer.sortingOrder >= -59 && renderer.sortingOrder <= -15)
                    throw new InvalidOperationException("Chamber order band occupied by " + renderer.name);
            foreach (var t in foreground)
            {
                var indexed = parts.Select((p, i) => new { part = p, order = -15 - (parts.Length - 1) + i }).Where(p => p.part.terrain == t).ToArray();
                t.chamberPartLabels = indexed.Select(p => p.part.label).ToArray();
                t.chamberPartOrders = indexed.Select(p => p.order).ToArray();
                if (t.chamberPartLabels.Distinct().Count() != t.chamberPartLabels.Length) throw new InvalidOperationException("Duplicate chamber part label.");
                EditorUtility.SetDirty(t);
            }
        }
        else foreach (var t in terrains) if (t.HasStepSet && t.stripDerivedRuns) { t.shortNaturalSoilTail = true; EditorUtility.SetDirty(t); }
        foreach (var t in terrains) if (t.isActiveAndEnabled) t.Rebuild();
        if (sceneName == "MRAtlas_MR01_C03")
        {
            var reward = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MraReward>(true)).Single(r => r.rewardId == "mra:MR01_C03:reward");
            var sr = reward.image;
            if (sr == null || sr.sprite == null || sr.sprite.name != "Collectible_LoreStone_4K") throw new InvalidOperationException("Unexpected LoreStone visual");
            var support = terrains.Single(t => t.name == "Platform 2");
            float supportY = support.transform.TransformPoint(support.surface[0]).y;
            float baseY = sr.bounds.min.y + sr.bounds.size.y * (34f / 315f);
            float groundingDelta = supportY - baseY;
            if (Mathf.Abs(groundingDelta) > .00001f) sr.transform.position += Vector3.up * groundingDelta;
            reward.imageBobHeight = 0f;
            EditorUtility.SetDirty(sr.transform); EditorUtility.SetDirty(reward);
        }
        if (sceneName == "MRAtlas_MR01_Cradle")
        {
            var pillar = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>(true))
                .Single(r => r.name == "Fallen pillar (art)" && r.transform.parent != null &&
                    r.transform.parent.name == "After the quake (knot:grip)");
            if (pillar.sprite == null || AssetDatabase.GetAssetPath(pillar.sprite) != "Assets/Art/Codex/Terrain/Body/Pillar_2_Middle.png")
                throw new InvalidOperationException("Unexpected accepted fallen-pillar source.");
            var uv = pillar.GetComponent<MraCradlePillarU2>();
            if (uv == null) uv = pillar.gameObject.AddComponent<MraCradlePillarU2>();
            uv.enabled = true;
            if (!uv.Refresh()) throw new InvalidOperationException("Fallen-pillar U2 preconditions failed.");
            EditorUtility.SetDirty(uv);
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        return changes;
    }

    static Sprite Load(string path, string name)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        if (sprites.Length == 1) return sprites[0];
        return sprites.Single(s => s.name == name);
    }
}
