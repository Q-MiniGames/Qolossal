using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Builds Assets/Prefabs/Knucklebramble01.prefab, the Grip Knot's guardian, from Codex's parts
// (Assets/Art/Codex/Guardians) and Assets/Art/Characters/Knucklebramble/KnucklebrambleRig.json,
// which Tools/CreatureRigAuthoring/knucklebramble.py writes from Codex's Review 11 registration.
// The root stands on the ground under the body's centre, facing right. Each lower arm carries
// a trigger along its bone that hurts while the arm slams (GuardianArmHazard); the body is a
// solid, harmless box. Menu: Qolossal > Creatures > Build Knucklebramble
public static class KnucklebrambleRigBuilder
{
    public const string PrefabPath = "Assets/Prefabs/Knucklebramble01.prefab";
    const string RigPath = "Assets/Art/Characters/Knucklebramble/KnucklebrambleRig.json";
    const string SpriteFolder = "Assets/Art/Codex/Guardians/";

    [Serializable] class RigData { public float ppu, height; public BoneData[] bones; public PoseData[] poses; }
    [Serializable] class BoneData { public string name, parent, sprite; public float[] pos; public float artRot, length; public int order; }
    [Serializable] class PoseData { public string name; public float[] armAngles; }

    static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + name + ".png")
        ?? throw new FileNotFoundException("Missing guardian sprite; import accepted Codex art first.", name);

    [MenuItem("Qolossal/Creatures/Build Knucklebramble")]
    public static GameObject Build()
    {
        var data = JsonUtility.FromJson<RigData>(File.ReadAllText(RigPath));
        PoseData rest = Array.Find(data.poses, p => p.name == "Rest");
        var root = new GameObject("Knucklebramble");
        try
        {
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic; body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var solid = root.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(2.3f, 1.8f); solid.offset = new Vector2(0f, .95f);

            var rigObject = new GameObject("Rig"); rigObject.transform.SetParent(root.transform, false);
            rigObject.AddComponent<SortingGroup>();
            var facing = new GameObject("Facing").transform; facing.SetParent(rigObject.transform, false);

            var bones = new Dictionary<string, Transform>(); var worldPos = new Dictionary<string, Vector2>();
            var names = new List<string>(); var list = new List<Transform>(); var lengths = new List<float>();
            var renderers = new List<SpriteRenderer>(); var pointNames = new List<string>(); var points = new List<Transform>();
            var claws = new Transform[3];
            foreach (BoneData b in data.bones)
            {
                var bone = new GameObject(b.name).transform;
                Transform parent = string.IsNullOrEmpty(b.parent) ? facing : bones[b.parent];
                bone.SetParent(parent, false);
                int arm = b.name.StartsWith("Arm") ? b.name[3] - '0' : 0;
                if (arm > 0 && b.name.EndsWith("_Lower"))
                {
                    // Hangs off the end of its upper arm, at the rest pose's elbow angle.
                    bone.localPosition = new Vector3(lengths[names.IndexOf(b.parent)], 0f, 0f);
                    bone.localEulerAngles = new Vector3(0f, 0f, rest.armAngles[2 * (arm - 1) + 1] - rest.armAngles[2 * (arm - 1)]);
                }
                else
                {
                    Vector2 at = new Vector2(b.pos[0], b.pos[1]);
                    bone.localPosition = at - (string.IsNullOrEmpty(b.parent) ? Vector2.zero : worldPos[b.parent]);
                    worldPos[b.name] = at;
                    if (arm > 0) bone.localEulerAngles = new Vector3(0f, 0f, rest.armAngles[2 * (arm - 1)]);
                }
                bones[b.name] = bone; names.Add(b.name); list.Add(bone); lengths.Add(b.length);

                var art = new GameObject("Art").transform; art.SetParent(bone, false);
                art.localEulerAngles = new Vector3(0f, 0f, b.artRot);
                var renderer = art.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = Load(b.sprite); renderer.sortingOrder = b.order;
                renderers.Add(renderer);

                if (arm > 0 && b.name.EndsWith("_Lower"))
                {
                    // The claws: a slam lands here, and this length of the arm hurts while it strikes.
                    var claw = new GameObject("Claw").transform; claw.SetParent(bone, false);
                    claw.localPosition = new Vector3(b.length + .12f, 0f, 0f);
                    claws[arm - 1] = claw; pointNames.Add($"Arm{arm}_Claw"); points.Add(claw);
                    var hurt = bone.gameObject.AddComponent<CapsuleCollider2D>();
                    hurt.isTrigger = true; hurt.direction = CapsuleDirection2D.Horizontal;
                    hurt.size = new Vector2(b.length * .75f + .4f, .5f);
                    hurt.offset = new Vector2(b.length - hurt.size.x * .5f + .3f, 0f);   // the forearm and claws, not the elbow
                    bone.gameObject.AddComponent<GuardianArmHazard>().arm = arm;
                }
            }

            var rig = rigObject.AddComponent<CreatureRig>();
            rig.facing = facing; rig.boneNames = names.ToArray(); rig.bones = list.ToArray(); rig.lengths = lengths.ToArray();
            rig.renderers = renderers.ToArray(); rig.pointNames = pointNames.ToArray(); rig.points = points.ToArray();

            var guardian = root.AddComponent<KnucklebrambleGuardian>();
            var so = new SerializedObject(guardian);
            so.FindProperty("maximumHealth").intValue = 6;
            so.FindProperty("rig").objectReferenceValue = rig;
            var poseNames = so.FindProperty("poseNames"); var poseAngles = so.FindProperty("poseAngles");
            poseNames.arraySize = data.poses.Length; poseAngles.arraySize = data.poses.Length * 6;
            for (int p = 0; p < data.poses.Length; p++)
            {
                poseNames.GetArrayElementAtIndex(p).stringValue = data.poses[p].name;
                for (int i = 0; i < 6; i++) poseAngles.GetArrayElementAtIndex(p * 6 + i).floatValue = data.poses[p].armAngles[i];
            }
            so.FindProperty("bodyImage").objectReferenceValue = bones["Body"].GetComponentInChildren<SpriteRenderer>();
            so.FindProperty("glowImage").objectReferenceValue = bones["KnotGlow"].GetComponentInChildren<SpriteRenderer>();
            so.FindProperty("bodySprite").objectReferenceValue = Load("Knucklebramble_Body");
            so.FindProperty("exposedSprite").objectReferenceValue = Load("Knucklebramble_KnotExposed");
            so.FindProperty("glowSprite").objectReferenceValue = Load("Knucklebramble_KnotGlow");
            so.FindProperty("glowBareSprite").objectReferenceValue = Load("Knucklebramble_KnotGlow_Bare");
            var clawList = so.FindProperty("claws"); clawList.arraySize = 3;
            for (int i = 0; i < 3; i++) clawList.GetArrayElementAtIndex(i).objectReferenceValue = claws[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"[KnucklebrambleRigBuilder] built {PrefabPath}: {data.bones.Length} bones, {data.poses.Length} poses, {data.height} u tall");
            return prefab;
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
