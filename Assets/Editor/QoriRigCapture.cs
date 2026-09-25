using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Batch-mode review of the built Qori rig: samples clips on QoriRig.prefab in Unity and renders
// them, so the in-engine result can be compared with the Python previews.
// Usage: -executeMethod QoriRigCapture.Capture -captureDir <folder>
public static class QoriRigCapture
{
    public static void Capture()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-captureDir");
        string folder = index >= 0 && index + 1 < args.Length ? args[index + 1] : "Temp/QoriCaptures";
        Directory.CreateDirectory(folder);

        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
        new GameObject("Light").AddComponent<Light2D>().lightType = Light2D.LightType.Global;
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 1.4f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.59f, .67f, .69f);
        camera.transform.position = new Vector3(.4f, .3f, -10f);
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>();

        var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/QoriRig/QoriRig.prefab"));
        var animator = rig.GetComponentInChildren<QoriAnimator>();
        var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Art/Characters/QoriRig/Clips" })
            .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g))).ToDictionary(c => c.name);
        GameObject animated = animator.animator.gameObject;

        void Arms(bool reach)
        {
            foreach (SpriteRenderer r in animator.normalArms) r.enabled = !reach;
            foreach (SpriteRenderer r in animator.reachArms) r.enabled = reach;
        }
        void Shot(string file, string clip, float time, bool reach, Sprite head)
        {
            clips["Qori_" + clip].SampleAnimation(animated, time);
            Arms(reach);
            animator.head.sprite = head;
            if (animator.weapon != null) animator.weapon.enabled = false;
            var target = new RenderTexture(900, 900, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var read = new Texture2D(900, 900, TextureFormat.RGB24, false);
            read.ReadPixels(new Rect(0, 0, 900, 900), 0, 0); read.Apply();
            File.WriteAllBytes(Path.Combine(folder, file + ".png"), read.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
        }
        Shot("1_idle_neutral", "Idle", 0f, false, animator.headNeutral);
        Shot("2_idle_hurt", "Idle", 0f, false, animator.headHurt);
        Shot("3_idle_effort", "Idle", 0f, false, animator.headEffort);
        Shot("4_ledge_hang", "LedgeHang", 0f, true, animator.headUp);
        Shot("5_ledge_climb_30", "LedgeClimb", .3f, true, animator.headEffort);
        Shot("6_ledge_climb_80", "LedgeClimb", .8f, false, animator.headNeutral);
        Debug.Log("[QoriRigCapture] Captures written to " + Path.GetFullPath(folder));
    }
}
