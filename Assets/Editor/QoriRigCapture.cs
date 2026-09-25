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

    // Poses Qori hanging on the End Plateau's left ledge in the A0 test room, at the body
    // position PlayerMovement.TryGrabLedge computes, and renders it with the real terrain.
    // Usage: -executeMethod QoriRigCapture.CaptureLedge -captureDir <folder>
    public static void CaptureLedge()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-captureDir");
        string folder = index >= 0 && index + 1 < args.Length ? args[index + 1] : "Temp/QoriCaptures";
        Directory.CreateDirectory(folder);
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/A0_TestRoom.unity");
        foreach (TerrainBlock block in UnityEngine.Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None)) block.Rebuild();

        GameObject player = GameObject.Find("Player");
        var collider = player.GetComponent<BoxCollider2D>();
        Vector2 extents = Vector2.Scale(collider.size, (Vector2)player.transform.lossyScale) * .5f;
        const float wallX = 66f, ledgeTop = 9f;   // End Plateau left face and top
        player.transform.position = new Vector3(wallX - extents.x, ledgeTop - extents.y - .06f, 0f);

        var animator = player.GetComponentInChildren<QoriAnimator>(true);
        Vector3 s = player.transform.lossyScale;   // QoriAnimator.CancelParentScale, which only runs in play mode
        animator.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);
        var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Art/Characters/QoriRig/Clips" })
            .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g))).ToDictionary(c => c.name);
        clips["Qori_LedgeHang"].SampleAnimation(animator.animator.gameObject, 0f);
        foreach (SpriteRenderer r in animator.normalArms) r.enabled = false;
        foreach (SpriteRenderer r in animator.reachArms) r.enabled = true;
        animator.head.sprite = animator.headUp;
        if (animator.weapon != null) animator.weapon.enabled = false;

        Camera camera = Camera.main;
        camera.transform.position = new Vector3(wallX, ledgeTop - .3f, -10f);
        camera.orthographicSize = 1.6f;
        foreach (ParallaxLayer layer in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(camera);
        var target = new RenderTexture(1200, 1200, 24);
        camera.aspect = 1f; camera.targetTexture = target; camera.Render();
        RenderTexture.active = target;
        var read = new Texture2D(1200, 1200, TextureFormat.RGB24, false);
        read.ReadPixels(new Rect(0, 0, 1200, 1200), 0, 0); read.Apply();
        File.WriteAllBytes(Path.Combine(folder, "ledge_in_room.png"), read.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        Debug.Log($"[QoriRigCapture] Ledge capture: player at {player.transform.position}, extents {extents}");
        // Map the real ledge corner into rig px through the animated root, so clips.LEDGE can be checked.
        Transform root = animator.animator.transform;
        Vector3 corner = root.InverseTransformPoint(new Vector3(wallX, ledgeTop, 0f));
        Debug.Log($"[QoriRigCapture] Ledge corner in rig root space: {corner.x:F4},{corner.y:F4}; handNear {animator.handNear.position:F3} handFar {animator.handFar.position:F3}; root lossyScale {root.lossyScale:F4}");
    }

    // Stands Qori on the start ground (walk line y = 0) with the collider bottom on it and
    // logs where the rig's sole line lands. Usage: -executeMethod QoriRigCapture.CaptureStanding
    public static void CaptureStanding()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-captureDir");
        string folder = index >= 0 && index + 1 < args.Length ? args[index + 1] : "Temp/QoriCaptures";
        Directory.CreateDirectory(folder);
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/A0_TestRoom.unity");
        foreach (TerrainBlock block in UnityEngine.Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None)) block.Rebuild();
        GameObject player = GameObject.Find("Player");
        var collider = player.GetComponent<BoxCollider2D>();
        Vector2 extents = Vector2.Scale(collider.size, (Vector2)player.transform.lossyScale) * .5f;
        player.transform.position = new Vector3(-16f, extents.y, 0f);
        var animator = player.GetComponentInChildren<QoriAnimator>(true);
        Vector3 s = player.transform.lossyScale;
        animator.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Characters/QoriRig/Clips/Qori_Idle.anim");
        clip.SampleAnimation(animator.animator.gameObject, 0f);
        Transform root = animator.animator.transform;
        // Rig ground line (anim.GROUND = -1245 rig px, root at rig y = -665) in world space.
        float groundWorld = root.TransformPoint(new Vector3(0f, (-1245f + 665f) / 100f, 0f)).y;
        Debug.Log($"[QoriRigCapture] Standing: collider bottom 0.000, rig ground line {groundWorld:F3}, rig root {root.position:F3}, rig local {animator.transform.localPosition:F3}");
        Camera camera = Camera.main;
        camera.transform.position = new Vector3(-16f, .9f, -10f); camera.orthographicSize = 1.4f; camera.aspect = 1f;
        foreach (ParallaxLayer layer in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(camera);
        var target = new RenderTexture(900, 900, 24); camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
        var read = new Texture2D(900, 900, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, 900, 900), 0, 0); read.Apply();
        File.WriteAllBytes(Path.Combine(folder, "standing.png"), read.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
    }

    // Walk/run frames on the A0 slope, without and with foot grounding (top and bottom rows).
    // Usage: -executeMethod QoriRigCapture.CaptureSlope -captureDir <folder>
    public static void CaptureSlope()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-captureDir");
        string folder = index >= 0 && index + 1 < args.Length ? args[index + 1] : "Temp/QoriCaptures";
        Directory.CreateDirectory(folder);
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/A0_TestRoom.unity");
        foreach (TerrainBlock block in UnityEngine.Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None)) block.Rebuild();
        foreach (TerrainPiece piece in UnityEngine.Object.FindObjectsByType<TerrainPiece>(FindObjectsSortMode.None)) piece.Rebuild();
        GameObject player = GameObject.Find("Player");
        var movement = player.GetComponent<PlayerMovement>();
        var collider = player.GetComponent<BoxCollider2D>();
        Vector2 extents = Vector2.Scale(collider.size, (Vector2)player.transform.lossyScale) * .5f;
        var animator = player.GetComponentInChildren<QoriAnimator>(true);
        Vector3 s = player.transform.lossyScale;
        animator.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);
        animator.head.sprite = animator.headNeutral;
        foreach (SpriteRenderer r in animator.reachArms) r.enabled = false;
        var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Art/Characters/QoriRig/Clips" })
            .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g))).ToDictionary(c => c.name);
        int mask = LayerMask.GetMask("Ground");
        Camera camera = Camera.main; camera.aspect = 1f; camera.orthographicSize = 1.3f;
        (string clip, float t, float x)[] shots = { ("Idle", 0f, 17f), ("Walk", .1f, 17f), ("Run", .4f, 17f), ("Idle", 0f, 22.3f), ("Idle", 0f, 23.8f), ("Walk", .35f, 25.2f) };
        const int size = 500;
        var sheet = new Texture2D(size * shots.Length, size * 2, TextureFormat.RGB24, false);
        var target = new RenderTexture(size, size, 24);
        var read = new Texture2D(size, size, TextureFormat.RGB24, false);
        for (int row = 0; row < 2; row++)
            for (int i = 0; i < shots.Length; i++)
            {
                float x = shots[i].x;
                float bottom = Mathf.Max(Physics2D.Raycast(new Vector2(x - extents.x, 30f), Vector2.down, 60f, mask).point.y,
                                         Physics2D.Raycast(new Vector2(x + extents.x, 30f), Vector2.down, 60f, mask).point.y);
                player.transform.position = new Vector3(x, bottom + extents.y, 0f);
                Physics2D.SyncTransforms();
                clips["Qori_" + shots[i].clip].SampleAnimation(animator.animator.gameObject, shots[i].t);
                if (animator.weapon != null) animator.weapon.enabled = false;
                if (row == 1) new QoriFootGrounding(animator.animator.transform, movement).Apply(true, 1f);
                camera.transform.position = new Vector3(x, bottom + .7f, -10f);
                foreach (ParallaxLayer layer in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(camera);
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                read.ReadPixels(new Rect(0, 0, size, size), 0, 0); read.Apply();
                sheet.SetPixels(i * size, (1 - row) * size, size, size, read.GetPixels());
            }
        sheet.Apply();
        camera.targetTexture = null; RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(folder, "slope_feet.png"), sheet.EncodeToPNG());
        Debug.Log("[QoriRigCapture] Slope capture written");
    }
}
