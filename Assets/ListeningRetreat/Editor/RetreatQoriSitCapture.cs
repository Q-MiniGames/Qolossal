using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Renders the real Qori rig in its Sit pose (QoriRig.prefab, clip Qori_Sit, no weapon) on a transparent
// background at a fixed scale, and writes its landmarks, so the Batch 25a-4 bench can be built around
// the exact seated silhouette the game will draw.
// Batch: -executeMethod RetreatQoriSitCapture.Capture -captureDir <folder>
public static class RetreatQoriSitCapture
{
    const float PixelsPerUnit = 320f;
    const int Size = 1024;   // 3.2 u square

    public static void Capture()
    {
        string folder = Path.Combine(Arg("-captureDir", "Temp/Captures"), "retreat_sit");
        Directory.CreateDirectory(folder);
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
        new GameObject("Light").AddComponent<Light2D>().lightType = Light2D.LightType.Global;
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = Size / PixelsPerUnit * .5f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;

        var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/QoriRig/QoriRig.prefab"));
        rig.transform.position = Vector3.zero;
        var animator = rig.GetComponentInChildren<QoriAnimator>();
        var clip = AssetDatabase.FindAssets("Qori_Sit t:AnimationClip", new[] { "Assets/Art/Characters/QoriRig/Clips" })
            .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g))).First(c => c.name == "Qori_Sit");
        clip.SampleAnimation(animator.animator.gameObject, 0f);
        if (animator.weapon != null) animator.weapon.enabled = false;
        foreach (var r in animator.reachArms) r.enabled = false;
        foreach (var r in animator.normalArms) r.enabled = true;

        // Frame on the hip: the seat contact the bench must meet.
        var hip = Find(rig.transform, "ThighNear");
        Vector3 hipPos = hip != null ? hip.position : Vector3.zero;
        camera.transform.position = new Vector3(hipPos.x + .2f, hipPos.y + .35f, -10f);

        Shot(camera, Path.Combine(folder, "Qori_Sit_320ppu.png"), out var pixels);
        animator.head.sprite = animator.headBlink;
        Shot(camera, Path.Combine(folder, "Qori_Sit_Blink_320ppu.png"), out _);
        animator.head.sprite = animator.headNeutral;

        // Landmarks in canvas px (y down) from the alpha.
        int minX = Size, maxX = -1, minY = Size, maxY = -1;
        for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            if (pixels[y * Size + x].a > 8) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); int yd = Size - 1 - y; minY = Mathf.Min(minY, yd); maxY = Mathf.Max(maxY, yd); }
        Vector2 Px(Vector3 w) { var v = camera.WorldToViewportPoint(w); return new Vector2(v.x * Size, (1f - v.y) * Size); }
        string P(Vector2 v) => string.Format(CultureInfo.InvariantCulture, "[{0:F1}, {1:F1}]", v.x, v.y);
        var names = new[] { "ThighNear", "ThighFar", "ShinNear", "ShinFar", "FootNear", "FootFar", "Head", "Torso", "Body" };
        var json = "{\n  \"pixelsPerUnit\": 320,\n  \"canvas\": [1024, 1024],\n  \"coordinates\": \"top-left origin, y down\",\n" +
            $"  \"alphaBounds\": [{minX}, {minY}, {maxX}, {maxY}],\n  \"joints\": {{\n" +
            string.Join(",\n", names.Select(n => (n, t: Find(rig.transform, n))).Where(p => p.t != null).Select(p => $"    \"{p.n}\": {P(Px(p.t.position))}")) +
            "\n  },\n  \"note\": \"ThighNear is the hip: the game seats Qori with this joint 0.07 u above the seat top (ListeningSpot.Sit).\"\n}\n";
        File.WriteAllText(Path.Combine(folder, "Qori_Sit_Landmarks.json"), json);
        Debug.Log("[Retreat] sit capture: bounds " + minX + "," + minY + " to " + maxX + "," + maxY + " in " + folder);
    }

    static void Shot(Camera camera, string path, out Color32[] pixels)
    {
        var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        camera.targetTexture = target; camera.Render();
        RenderTexture.active = target;
        var read = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        read.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); read.Apply();
        pixels = read.GetPixels32();
        File.WriteAllBytes(path, read.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        Object.DestroyImmediate(target); Object.DestroyImmediate(read);
    }

    static Transform Find(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t) { var f = Find(c, name); if (f != null) return f; }
        return null;
    }

    static string Arg(string name, string fallback)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
        return fallback;
    }
}
