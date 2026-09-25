using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch-mode review of the crawler rig in the A0 test room: walk phases and the charge telegraph.
// Usage: -executeMethod CrawlerCapture.Capture -captureDir <folder>
public static class CrawlerCapture
{
    public static void Capture()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-captureDir");
        string folder = index >= 0 && index + 1 < args.Length ? args[index + 1] : "Temp/CrawlerCaptures";
        Directory.CreateDirectory(folder);
        EditorSceneManager.OpenScene("Assets/Scenes/A0_TestRoom.unity");
        foreach (TerrainBlock block in UnityEngine.Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None)) block.Rebuild();
        var all = UnityEngine.Object.FindObjectsByType<CrawlerAnimator>(FindObjectsSortMode.InstanceID);
        var crawler = all[0];
        for (int i = 1; i < all.Length; i++) all[i].transform.parent.gameObject.SetActive(false);
        crawler.CaptureRest();
        Transform creature = crawler.transform.parent;
        creature.position = new Vector3(-6f, .275f, 0f);
        Camera camera = Camera.main; camera.aspect = 1f; camera.orthographicSize = .9f;
        camera.transform.position = new Vector3(-6f, .55f, -10f);
        foreach (ParallaxLayer layer in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(camera);
        (float phase, float blend, float tele, bool charge)[] shots =
            { (0f, 0f, 0f, false), (0f, 1f, 0f, false), (.25f, 1f, 0f, false), (.5f, 1f, 0f, false), (.75f, 1f, 0f, false), (0f, 0f, 1f, false), (.4f, 1f, 1f, true) };
        const int size = 420;
        var sheet = new Texture2D(size * shots.Length, size, TextureFormat.RGB24, false);
        var target = new RenderTexture(size, size, 24);
        var read = new Texture2D(size, size, TextureFormat.RGB24, false);
        for (int i = 0; i < shots.Length; i++)
        {
            crawler.Pose(shots[i].phase, shots[i].blend, shots[i].tele, shots[i].charge, Color.white);
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            read.ReadPixels(new Rect(0, 0, size, size), 0, 0); read.Apply();
            sheet.SetPixels(i * size, 0, size, size, read.GetPixels());
        }
        sheet.Apply(); camera.targetTexture = null; RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(folder, "crawler.png"), sheet.EncodeToPNG());
        Debug.Log("[CrawlerCapture] written");
    }
}
