using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Builds the Bramble Crawler cutout rig from Assets/Art/Characters/Crawler/CrawlerRig.json
// (written by Tools/CreatureRigAuthoring/crawler.py) and installs it on GroundCreature01.
// Menu: Qolossal > Creatures > Build Crawler Rig
public static class CrawlerRigBuilder
{
    const string DataPath = "Assets/Art/Characters/Crawler/CrawlerRig.json";
    const string PrefabPath = "Assets/Art/Characters/Crawler/CrawlerRig.prefab";
    const string CreaturePrefabPath = "Assets/Prefabs/GroundCreature01.prefab";
    const string SpriteFolder = "Assets/Art/Codex/Enemies/";
    // Collider fitted to the painted body (world units); the rig's ground line sits on its bottom.
    static readonly Vector2 ColliderSize = new Vector2(1.1f, .55f);

    [Serializable] class RigData { public int version; public BoneData[] bones; }
    [Serializable] class BoneData
    {
        public string name, parent, sprite;
        public float[] pos, centreFromPivot;
        public float rot, length, thick, artRot, artScale, tint;
        public int order;
    }

    [MenuItem("Qolossal/Creatures/Build Crawler Rig")]
    public static void BuildAndInstall()
    {
        GameObject prefab = Build();
        if (prefab != null) Install(prefab);
    }

    static GameObject Build()
    {
        var data = JsonUtility.FromJson<RigData>(File.ReadAllText(DataPath));
        var root = new GameObject("CrawlerRig");
        try
        {
            var facing = new GameObject("Facing").transform;
            facing.SetParent(root.transform, false);
            root.AddComponent<SortingGroup>();
            var bones = new Dictionary<string, Transform>();
            var renderers = new List<SpriteRenderer>();
            foreach (BoneData b in data.bones)
            {
                var bone = new GameObject(b.name).transform;
                bone.SetParent(string.IsNullOrEmpty(b.parent) ? facing : bones[b.parent], false);
                bone.localPosition = new Vector3(b.pos[0], b.pos[1], 0f);
                bone.localEulerAngles = new Vector3(0f, 0f, b.rot);
                bones[b.name] = bone;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + b.sprite + ".png");
                if (sprite == null) throw new FileNotFoundException("Missing crawler sprite; import accepted Codex art first.", b.sprite);
                var thick = new GameObject("Thick").transform;
                thick.SetParent(bone, false);
                thick.localScale = new Vector3(1f, b.thick, 1f);
                var art = new GameObject("Art").transform;
                art.SetParent(thick, false);
                art.localEulerAngles = new Vector3(0f, 0f, b.artRot);
                float ppu = sprite.pixelsPerUnit;
                // Sprite pivot is its centre; move the centre to where it sits relative to landmark A.
                Vector2 centreOffset = new Vector2(b.centreFromPivot[0], b.centreFromPivot[1]) * b.artScale;
                art.localPosition = Quaternion.Euler(0f, 0f, b.artRot) * centreOffset;
                art.localScale = Vector3.one * b.artScale * ppu;
                var renderer = art.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = b.order;
                renderer.color = new Color(b.tint, b.tint, b.tint, 1f);
                renderers.Add(renderer);
            }
            var animator = root.AddComponent<CrawlerAnimator>();
            animator.Configure(facing, bones, data.bones.ToDictionary(b => b.name, b => b.length), renderers.ToArray());
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"Crawler Rig: built {PrefabPath} with {data.bones.Length} bones.");
            return prefab;
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void Install(GameObject rigPrefab)
    {
        GameObject creature = PrefabUtility.LoadPrefabContents(CreaturePrefabPath);
        try
        {
            Transform existing = creature.transform.Find("CrawlerRig");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var box = creature.GetComponent<BoxCollider2D>();
            Vector3 scale = creature.transform.localScale;
            box.size = new Vector2(ColliderSize.x / Mathf.Abs(scale.x), ColliderSize.y / Mathf.Abs(scale.y));
            box.offset = Vector2.zero;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, creature.transform);
            instance.name = "CrawlerRig";
            instance.transform.localPosition = new Vector3(0f, -box.size.y * .5f, 0f);
            instance.transform.localScale = new Vector3(1f / Mathf.Abs(scale.x), 1f / Mathf.Abs(scale.y), 1f);
            var source = creature.GetComponent<SpriteRenderer>();
            instance.GetComponent<SortingGroup>().sortingOrder = source.sortingOrder;
            source.enabled = false;
            PrefabUtility.SaveAsPrefabAsset(creature, CreaturePrefabPath);
            Debug.Log("Crawler Rig: installed on " + CreaturePrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(creature); }
    }
}
