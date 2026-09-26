using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Builds <Species>Rig.prefab from Assets/Art/Characters/<Species>/<Species>Rig.json (written by
// Tools/CreatureRigAuthoring/creature_rig.py). Menu: Qolossal > Creatures > Build Enemy Rigs
public static class CreatureRigBuilder
{
    const string SpriteFolder = "Assets/Art/Codex/Enemies/";
    public static readonly string[] Species = { "Carrier", "Thornwing", "Spitter", "Shellback" };

    [Serializable] class RigData { public int version; public BoneData[] bones; }
    [Serializable] class PointData { public string name; public float[] pos; }
    [Serializable] class BoneData
    {
        public string name, parent, sprite;
        public float[] pos, centreFromPivot;
        public float rot, length, thick, artRot, artScale, tint;
        public int order;
        public PointData[] points;
    }

    [MenuItem("Qolossal/Creatures/Build Enemy Rigs")]
    public static void BuildAll() { foreach (string s in Species) Build(s); }

    public static GameObject Build(string species)
    {
        string folder = "Assets/Art/Characters/" + species + "/";
        var data = JsonUtility.FromJson<RigData>(File.ReadAllText(folder + species + "Rig.json"));
        var root = new GameObject(species + "Rig");
        try
        {
            root.AddComponent<SortingGroup>();
            var facing = new GameObject("Facing").transform; facing.SetParent(root.transform, false);
            var bones = new Dictionary<string, Transform>();
            var names = new List<string>(); var list = new List<Transform>(); var lengths = new List<float>();
            var renderers = new List<SpriteRenderer>(); var pointNames = new List<string>(); var points = new List<Transform>();
            foreach (BoneData b in data.bones)
            {
                var bone = new GameObject(b.name).transform;
                bone.SetParent(string.IsNullOrEmpty(b.parent) ? facing : bones[b.parent], false);
                bone.localPosition = new Vector3(b.pos[0], b.pos[1], 0f);
                bone.localEulerAngles = new Vector3(0f, 0f, b.rot);
                bones[b.name] = bone; names.Add(b.name); list.Add(bone); lengths.Add(b.length);

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + b.sprite + ".png")
                    ?? throw new FileNotFoundException("Missing enemy sprite; import accepted Codex art first.", b.sprite);
                var thick = new GameObject("Thick").transform; thick.SetParent(bone, false);
                thick.localScale = new Vector3(1f, b.thick, 1f);
                var art = new GameObject("Art").transform; art.SetParent(thick, false);
                art.localEulerAngles = new Vector3(0f, 0f, b.artRot);
                art.localPosition = Quaternion.Euler(0f, 0f, b.artRot) * (new Vector2(b.centreFromPivot[0], b.centreFromPivot[1]) * b.artScale);
                art.localScale = Vector3.one * b.artScale * sprite.pixelsPerUnit;
                var renderer = art.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite; renderer.sortingOrder = b.order; renderer.color = new Color(b.tint, b.tint, b.tint, 1f);
                renderers.Add(renderer);
                if (b.points != null)
                    foreach (PointData p in b.points)
                    {
                        var point = new GameObject(p.name).transform; point.SetParent(bone, false);
                        point.localPosition = new Vector3(p.pos[0], p.pos[1], 0f);
                        pointNames.Add(p.name); points.Add(point);
                    }
            }
            var rig = root.AddComponent<CreatureRig>();
            rig.facing = facing; rig.boneNames = names.ToArray(); rig.bones = list.ToArray(); rig.lengths = lengths.ToArray();
            rig.renderers = renderers.ToArray(); rig.pointNames = pointNames.ToArray(); rig.points = points.ToArray();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, folder + species + "Rig.prefab");
            Debug.Log($"[CreatureRigBuilder] built {species}Rig with {data.bones.Length} bones");
            return prefab;
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
