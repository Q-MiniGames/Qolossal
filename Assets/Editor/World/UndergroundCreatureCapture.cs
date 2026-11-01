using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch: -executeMethod UndergroundCreatureCapture.Run -captureDir <dir> (no -quit). Plays the underground for a few
// seconds and captures rooms with creatures, so the runtime Ludo frames (UndergroundCreatureFrames) can be seen.
public static class UndergroundCreatureCapture
{
    static readonly string[] Rooms = { "A03", "A04", "C04", "C06", "D03", "D05", "E04", "A08", "C08", "D08", "MR01_C02", "MR02_C03", "MR03_C04" };
    static string dir; static double startAt; static int next;

    public static void Run()
    {
        var args = System.Environment.GetCommandLineArgs();
        dir = args[System.Array.IndexOf(args, "-captureDir") + 1];
        Directory.CreateDirectory(dir);
        EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder + RegionalSceneCatalog.Underground + ".unity", OpenSceneMode.Single);
        SessionState.SetString(Key, dir);   // survives the domain reload on entering Play
        EditorApplication.EnterPlaymode();
    }

    const string Key = "UGP.CreatureCapture.Dir";

    [InitializeOnLoadMethod]
    static void Resume()
    {
        dir = SessionState.GetString(Key, "");
        if (string.IsNullOrEmpty(dir)) return;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        if (startAt == 0) { startAt = EditorApplication.timeSinceStartup; return; }
        if (EditorApplication.timeSinceStartup - startAt < 4 + next * 1.5) return;
        if (next < Rooms.Length) { Capture(Rooms[next]); next++; return; }
        EditorApplication.update -= Tick;
        SessionState.EraseString(Key);
        EditorApplication.ExitPlaymode();
        EditorApplication.Exit(0);
    }

    static void Capture(string id)
    {
        UndergroundRoom room = null;
        foreach (var r in Object.FindObjectsByType<UndergroundRoom>(FindObjectsSortMode.None)) if (r.roomId == id) room = r;
        if (room == null) return;
        var b = room.WorldBounds;
        var cam = new GameObject("capture").AddComponent<Camera>();
        cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(b.center.x, b.center.y, -50f);
        cam.orthographicSize = Mathf.Max(b.size.y / 2f + 1f, (b.size.x / 2f + 1f) * 9f / 16f);
        var rt = new RenderTexture(1920, 1080, 24); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        File.WriteAllBytes(Path.Combine(dir, id + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null; Object.DestroyImmediate(cam.gameObject);
        int dressed = 0; foreach (var f in Object.FindObjectsByType<UndergroundCreatureFrames>(FindObjectsSortMode.None)) if (f.enabled) dressed++;
        Debug.Log($"UGP capture {id}: {dressed} creatures dressed");
    }
}
