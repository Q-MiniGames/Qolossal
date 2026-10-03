using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Batch-mode review of every Qori rig clip: renders each clip on QoriRig.prefab at five moments
// (0, 25, 50, 75 and 100 % of its length) into one row per clip, written as contact sheets of
// 8 clips each to <-captureDir>/clips_NN.png. The reach arms are shown in the ledge clips, as
// QoriAnimator does in play.
// Usage: -executeMethod QoriClipSheet.Capture -captureDir <folder>
public static class QoriClipSheet
{
    public static void Capture()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-captureDir");
        string folder = index >= 0 && index + 1 < args.Length ? args[index + 1] : "Temp/QoriClips";
        Directory.CreateDirectory(folder);

        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
        new GameObject("Light").AddComponent<Light2D>().lightType = Light2D.LightType.Global;
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 1.6f; camera.aspect = 1f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.59f, .67f, .69f);
        camera.transform.position = new Vector3(.2f, .35f, -10f);
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>();

        var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/QoriRig/QoriRig.prefab"));
        var q = rig.GetComponentInChildren<QoriAnimator>();
        GameObject animated = q.animator.gameObject;
        var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Art/Characters/QoriRig/Clips" })
            .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(c => !c.name.Contains("Whip")).OrderBy(c => c.name).ToList();

        const int Cell = 300, Cols = 5, Rows = 8, Label = 28;
        var target = new RenderTexture(Cell * 2, Cell * 2, 24);
        var read = new Texture2D(Cell * 2, Cell * 2, TextureFormat.RGB24, false);
        for (int sheet = 0; sheet * Rows < clips.Count; sheet++)
        {
            var page = new Texture2D(Cols * Cell, Rows * (Cell + Label), TextureFormat.RGB24, false);
            var fill = Enumerable.Repeat(new Color(.82f, .82f, .8f), page.width * page.height).ToArray();
            page.SetPixels(fill);
            for (int row = 0; row < Rows && sheet * Rows + row < clips.Count; row++)
            {
                AnimationClip clip = clips[sheet * Rows + row];
                bool reach = clip.name.Contains("LedgeHang") || clip.name.Contains("LedgeClimb");
                for (int col = 0; col < Cols; col++)
                {
                    float t = clip.length * col / (Cols - 1);
                    clip.SampleAnimation(animated, t);
                    bool reachNow = reach && (!clip.name.Contains("LedgeClimb") || col / (float)(Cols - 1) < q.reachArmsUntil);
                    foreach (SpriteRenderer r in q.normalArms) r.enabled = !reachNow;
                    foreach (SpriteRenderer r in q.reachArms) r.enabled = reachNow;
                    if (q.weapon != null) q.weapon.enabled = false;
                    camera.targetTexture = target; camera.Render();
                    RenderTexture.active = target;
                    read.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); read.Apply();
                    // Downsample 2:1 into the page cell (top-left origin for rows).
                    int ox = col * Cell, oy = page.height - (row + 1) * (Cell + Label);
                    for (int y = 0; y < Cell; y++)
                        for (int x = 0; x < Cell; x++)
                        {
                            Color c = (read.GetPixel(2 * x, 2 * y) + read.GetPixel(2 * x + 1, 2 * y) + read.GetPixel(2 * x, 2 * y + 1) + read.GetPixel(2 * x + 1, 2 * y + 1)) * .25f;
                            page.SetPixel(ox + x, oy + y, c);
                        }
                }
                File.AppendAllText(Path.Combine(folder, "clips_index.txt"), $"page {sheet + 1:00} row {row + 1}: {clip.name} ({clip.length:F2} s)\n");
            }
            page.Apply();
            File.WriteAllBytes(Path.Combine(folder, $"clips_{sheet + 1:00}.png"), page.EncodeToPNG());
        }
        camera.targetTexture = null; RenderTexture.active = null;
        Debug.Log($"[QoriClipSheet] {clips.Count} clips written to {Path.GetFullPath(folder)}");
    }

    // Key moments at full size: <-captureDir>/frame_<clip>_<t>.png (900 px, the whole body).
    public static void CaptureFrames()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-captureDir");
        string folder = index >= 0 && index + 1 < args.Length ? args[index + 1] : "Temp/QoriFrames";
        Directory.CreateDirectory(folder);
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
        new GameObject("Light").AddComponent<Light2D>().lightType = Light2D.LightType.Global;
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 1.5f; camera.aspect = 1f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.59f, .67f, .69f);
        camera.transform.position = new Vector3(.15f, .35f, -10f);
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/QoriRig/QoriRig.prefab"));
        var q = rig.GetComponentInChildren<QoriAnimator>();
        var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Art/Characters/QoriRig/Clips" })
            .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g))).ToDictionary(c => c.name);
        var frames = new (string clip, float at)[] { ("Idle", 0f), ("Walk", .5f), ("Run", 0f), ("Run", .5f), ("Rise", .5f), ("Fall", .5f), ("Land", .3f),
            ("Hang", .5f), ("WallSlide", .5f), ("WallJumpOff", .5f), ("LedgeHang", 0f), ("LedgeClimb", .3f), ("LedgeClimb", .8f),
            ("AttackFront", .3f), ("AttackFront", .45f), ("AttackUp", .45f), ("AttackDown", .45f), ("AttackAirFront", .45f),
            ("Mace_Front1", .45f), ("Spear_Up", .45f), ("Sling_Throw", .45f) };
        var target = new RenderTexture(900, 900, 24);
        var read = new Texture2D(900, 900, TextureFormat.RGB24, false);
        foreach (var (name, at) in frames)
        {
            var clip = clips["Qori_" + name];
            clip.SampleAnimation(q.animator.gameObject, clip.length * at);
            bool reach = name == "LedgeHang" || name == "LedgeClimb" && at < q.reachArmsUntil;
            foreach (SpriteRenderer r in q.normalArms) r.enabled = !reach;
            foreach (SpriteRenderer r in q.reachArms) r.enabled = reach;
            if (q.weapon != null) q.weapon.enabled = false;
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            read.ReadPixels(new Rect(0, 0, 900, 900), 0, 0); read.Apply();
            File.WriteAllBytes(Path.Combine(folder, $"frame_{name}_{Mathf.RoundToInt(at * 100):000}.png"), read.EncodeToPNG());
        }
        camera.targetTexture = null; RenderTexture.active = null;
        Debug.Log("[QoriClipSheet] frames written to " + Path.GetFullPath(folder));
    }
}
