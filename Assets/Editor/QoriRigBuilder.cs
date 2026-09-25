#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

// Builds Qori's cutout rig from Assets/Art/Characters/QoriRig/QoriRigData.json:
// imports the part sprites with joint pivots, creates the AnimationClips and the
// Animator Controller, saves QoriRig.prefab and installs it on Player.prefab.
// Menu: Qolossal > Qori Rig > ...
public static class QoriRigBuilder
{
    const string Folder = "Assets/Art/Characters/QoriRig";
    const string DataPath = Folder + "/QoriRigData.json";
    const string ClipFolder = Folder + "/Clips";
    const string ControllerPath = Folder + "/QoriRig.controller";
    const string PrefabPath = Folder + "/QoriRig.prefab";
    const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

    [Serializable] class RigData { public int version; public float pixelsPerUnit = 100; public float weaponRestAngle; public int weaponOrder = 12; public PartData[] parts; public BoneData[] bones; public ClipData[] clips; }
    [Serializable] class PartData { public string name; public string file; public float[] pivot; }
    [Serializable] class BoneData { public string name; public string parent; public string path; public string part; public float[] pos; public float rot; public int order; public float tint = 1; }
    [Serializable] class ClipData { public string name; public float length; public bool loop; public float[] times; public TrackData[] tracks; }
    [Serializable] class TrackData { public string path; public string attr; public float[] v; public float[] t; }

    static readonly string[] LocomotionStates = { "Walk", "Run" };

    [MenuItem("Qolossal/Qori Rig/Build and Install on Player", priority = 0)]
    public static void BuildAndInstall()
    {
        GameObject prefab = Build();
        if (prefab != null) Install(prefab);
    }

    [MenuItem("Qolossal/Qori Rig/Rebuild Rig Only (no Player change)", priority = 1)]
    public static void BuildOnly() => Build();

    [MenuItem("Qolossal/Qori Rig/Revert Player to Legacy Visuals", priority = 20)]
    public static void RevertToLegacy()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            Transform rig = root.transform.Find("QoriRig");
            if (rig != null) UnityEngine.Object.DestroyImmediate(rig.gameObject);
            Transform legacy = root.transform.Find("QoriVisual");
            if (legacy != null) legacy.gameObject.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Debug.Log("Qori Rig: Player restored to the legacy QoriVisual.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static GameObject Build()
    {
        var text = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
        if (text == null) { Debug.LogError("Qori Rig: missing " + DataPath); return null; }
        RigData data = JsonUtility.FromJson<RigData>(text.text);

        // 1. Sprites with pivots at the joints.
        var sprites = new Dictionary<string, Sprite>();
        foreach (PartData part in data.parts)
        {
            string path = Folder + "/" + part.file;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError("Qori Rig: missing part " + path); return null; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = data.pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 1024;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(part.pivot[0], part.pivot[1]);
            settings.spriteMeshType = SpriteMeshType.Tight;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            sprites[part.name] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // 2. Clips.
        EnsureFolder(ClipFolder);
        var restPositions = new Dictionary<string, Vector2>();
        foreach (BoneData b in data.bones) restPositions[b.path] = new Vector2(b.pos[0], b.pos[1]);
        var clips = new Dictionary<string, AnimationClip>();
        foreach (ClipData c in data.clips) clips[c.name] = CreateClip(c, restPositions);

        // 3. Animator Controller (states only: QoriAnimator cross-fades from code).
        // Reuse the existing asset so its GUID never changes: deleting and recreating
        // it left Player.prefab pointing at a missing controller (body stuck in bind pose).
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        else
        {
            AnimatorStateMachine old = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in old.states) old.RemoveState(child.state);
            foreach (AnimatorControllerParameter p in controller.parameters) controller.RemoveParameter(p);
        }
        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("AttackTime", AnimatorControllerParameterType.Float);
        var parameters = controller.parameters;
        foreach (var p in parameters) if (p.name == "MoveSpeed") p.defaultFloat = 1f;
        controller.parameters = parameters;
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = null;
        int column = 0;
        foreach (ClipData c in data.clips)
        {
            AnimatorState state = machine.AddState(c.name, new Vector3(260 + 220 * (column % 4), 60 + 70 * (column / 4), 0));
            column++;
            state.motion = clips[c.name];
            state.writeDefaultValues = true;
            if (Array.IndexOf(LocomotionStates, c.name) >= 0) { state.speedParameterActive = true; state.speedParameter = "MoveSpeed"; }
            // Attacks (incl. per-weapon Mace_/Spear_/Whip_/Sling_ clips) and the ledge climb are scrubbed by code.
            if (c.name.StartsWith("Attack") || c.name.Contains("_") || c.name == "LedgeClimb") { state.timeParameterActive = true; state.timeParameter = "AttackTime"; }
            if (c.name == "Idle") idle = state;
        }
        if (idle != null) machine.defaultState = idle;
        EditorUtility.SetDirty(controller);

        // 4. Prefab: QoriRig (QoriAnimator, cancels parent scale) > Facing (flip/tilt) > Root (Animator) > bones.
        var anchor = new GameObject("QoriRig");
        try
        {
            var q = anchor.AddComponent<QoriAnimator>();
            Transform facing = new GameObject("Facing").transform; facing.SetParent(anchor.transform, false);
            Transform root = new GameObject("Root").transform; root.SetParent(facing, false);
            root.localPosition = new Vector3(-.12f, 0f, 0f);
            root.localScale = new Vector3(.1464f, .1464f, 1f);
            var animator = root.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            root.gameObject.AddComponent<SortingGroup>();

            var bones = new Dictionary<string, Transform> { { "Root", root } };
            var renderers = new List<SpriteRenderer>();
            foreach (BoneData b in data.bones)
            {
                Transform t = new GameObject(b.name).transform;
                t.SetParent(bones[string.IsNullOrEmpty(b.parent) ? "Root" : b.parent], false);
                t.localPosition = new Vector3(b.pos[0], b.pos[1], 0f);
                t.localEulerAngles = new Vector3(0f, 0f, b.rot);
                bones[b.name] = t;
                if (!string.IsNullOrEmpty(b.part) && sprites.TryGetValue(b.part, out Sprite sprite))
                {
                    var r = t.gameObject.AddComponent<SpriteRenderer>();
                    r.sprite = sprite; r.sortingOrder = b.order; r.color = new Color(b.tint, b.tint, b.tint, 1f);
                    renderers.Add(r);
                }
            }
            Transform mount = bones["WeaponMount"];
            var weaponObject = new GameObject("Weapon");
            weaponObject.transform.SetParent(mount, false);
            var weapon = weaponObject.AddComponent<SpriteRenderer>();
            weapon.sortingOrder = data.weaponOrder;
            renderers.Add(weapon);
            Transform tip = new GameObject("WeaponTip").transform; tip.SetParent(weaponObject.transform, false);
            tip.localPosition = new Vector3(7.3f, 0f, 0f);

            q.animator = animator;
            q.facingPivot = facing;
            q.handNear = bones["HandNear"]; q.handFar = bones["HandFar"];
            q.weaponMount = mount; q.weaponTip = tip;
            q.weapon = weapon;
            q.head = bones["Head"].GetComponent<SpriteRenderer>();
            sprites.TryGetValue("Head_Neutral", out q.headNeutral);
            sprites.TryGetValue("Head_Up", out q.headUp);
            sprites.TryGetValue("Head_Down", out q.headDown);
            sprites.TryGetValue("Head_Focus", out q.headFocus);
            sprites.TryGetValue("Head_Blink", out q.headBlink);
            sprites.TryGetValue("Head_Hurt", out q.headHurt);
            sprites.TryGetValue("Head_Effort", out q.headEffort);
            q.freeForearm = bones.TryGetValue("ForearmFar", out Transform freeFore) ? freeFore.GetComponent<SpriteRenderer>() : null;
            sprites.TryGetValue("ForearmFar", out q.freeFist);
            sprites.TryGetValue("ForearmOpen", out q.freeOpen);
            SpriteRenderer[] ArmRenderers(bool reach)
            {
                var list = new List<SpriteRenderer>();
                foreach (string side in new[] { "Near", "Far" })
                    foreach (string segment in new[] { "UpperArm", "Forearm" })
                        if (bones.TryGetValue(segment + (reach ? "Reach" : "") + side, out Transform arm) && arm.TryGetComponent(out SpriteRenderer r))
                            list.Add(r);
                return list.ToArray();
            }
            q.normalArms = ArmRenderers(false);
            q.reachArms = ArmRenderers(true);
            foreach (SpriteRenderer arm in q.reachArms) arm.enabled = false;   // shown only while hanging from a ledge
            var ears = new List<Transform>();
            foreach (string ear in new[] { "EarUpper", "EarLower" }) if (bones.TryGetValue(ear, out Transform e)) ears.Add(e);
            q.ears = ears.ToArray();
            q.renderers = renderers.ToArray();
            var capeUpper = new List<Transform>(); var capeLower = new List<Transform>();
            for (int i = 1; bones.ContainsKey("Cape" + i); i++)
            { capeUpper.Add(bones["Cape" + i]); capeLower.Add(bones.TryGetValue("Cape" + i + "Lower", out Transform low) ? low : null); }
            q.capeUpper = capeUpper.ToArray(); q.capeLower = capeLower.ToArray();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(anchor, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Qori Rig: built {PrefabPath} with {data.clips.Length} clips.");
            return prefab;
        }
        finally { UnityEngine.Object.DestroyImmediate(anchor); }
    }

    static AnimationClip CreateClip(ClipData c, Dictionary<string, Vector2> rest)
    {
        string path = $"{ClipFolder}/Qori_{c.name}.anim";
        // Update clips in place (stable GUIDs) instead of delete + create.
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        bool isNew = clip == null;
        if (isNew) clip = new AnimationClip();
        else clip.ClearCurves();
        clip.name = "Qori_" + c.name; clip.frameRate = 30f;
        var seen = new HashSet<string>();
        foreach (TrackData track in c.tracks)
        {
            var curve = new AnimationCurve();
            for (int i = 0; i < c.times.Length; i++)
                curve.AddKey(new Keyframe(c.times[i], track.v[i], track.t[i], track.t[i]));
            SetCurve(clip, track.path, track.attr, curve);
            seen.Add(track.path + "|" + track.attr);
        }
        // Complete the vector properties so Unity never guesses the missing axes.
        foreach (TrackData track in c.tracks)
        {
            string group = track.attr.Substring(0, track.attr.LastIndexOf('.'));
            foreach (string axis in new[] { "x", "y", "z" })
            {
                string attr = group + "." + axis;
                if (!seen.Add(track.path + "|" + attr)) continue;
                float value = 0f;
                if (group == "m_LocalScale") value = 1f;
                else if (group == "m_LocalPosition" && axis != "z" && rest.TryGetValue(track.path, out Vector2 p)) value = axis == "x" ? p.x : p.y;
                SetCurve(clip, track.path, attr, AnimationCurve.Constant(0f, c.length, value));
            }
        }
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = c.loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (isNew) AssetDatabase.CreateAsset(clip, path);
        else EditorUtility.SetDirty(clip);
        return clip;
    }

    static void SetCurve(AnimationClip clip, string path, string attr, AnimationCurve curve)
    {
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), attr), curve);
    }

    static void Install(GameObject rigPrefab)
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            Transform existing = player.transform.Find("QoriRig");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            // Match the legacy visual's placement so feet land where they used to.
            Vector3 playerScale = player.transform.localScale;
            Vector3 legacyPosition = new Vector3(-.15f, .2f, 0f);
            float rigScale = .1464f;
            int sortingLayer = 0, sortingOrder = 2;
            Transform legacy = player.transform.Find("QoriVisual");
            if (legacy != null)
            {
                legacyPosition = legacy.localPosition;
                rigScale = Mathf.Abs(legacy.localScale.y * playerScale.y);
                var legacyRenderer = legacy.GetComponent<SpriteRenderer>();
                if (legacyRenderer != null) { sortingLayer = legacyRenderer.sortingLayerID; sortingOrder = legacyRenderer.sortingOrder; }
                legacy.gameObject.SetActive(false);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, player.transform);
            instance.name = "QoriRig";
            instance.transform.localPosition = new Vector3(0f, legacyPosition.y, 0f);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = new Vector3(1f / Mathf.Max(1e-4f, Mathf.Abs(playerScale.x)), 1f / Mathf.Max(1e-4f, Mathf.Abs(playerScale.y)), 1f);
            Transform root = instance.transform.Find("Facing/Root");
            root.localPosition = new Vector3(legacyPosition.x * playerScale.x, 0f, 0f);
            root.localScale = new Vector3(rigScale, rigScale, 1f);
            // Never let the Player carry an Animator override: always use the rig's controller.
            var rigAnimator = root.GetComponent<Animator>();
            PrefabUtility.RevertObjectOverride(rigAnimator, InteractionMode.AutomatedAction);
            if (rigAnimator.runtimeAnimatorController == null)
                rigAnimator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            var group = root.GetComponent<SortingGroup>();
            group.sortingLayerID = sortingLayer; group.sortingOrder = sortingOrder;

            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Debug.Log("Qori Rig: installed on Player.prefab (legacy QoriVisual disabled, not deleted). Press Play to test.");
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
#endif
