using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch-mode review: lays out one frame of each effect beside Qori on the start ground at game
// scale and renders it. Usage: -executeMethod FxCapture.Capture -captureDir <folder>
public static class FxCapture
{
    public static void Capture()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-captureDir");
        string folder = index >= 0 && index + 1 < args.Length ? args[index + 1] : "Temp/FxCaptures";
        Directory.CreateDirectory(folder);
        EditorSceneManager.OpenScene("Assets/Scenes/A0_TestRoom.unity");
        foreach (var b in UnityEngine.Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None)) b.Rebuild();
        var lib = Fx.Library;
        void Put(Sprite s, float x, float y, float size, float rot = 0f)
        {
            var r = new GameObject("fx").AddComponent<SpriteRenderer>();
            r.sprite = s; r.sortingOrder = 40; r.transform.position = new Vector3(x, y, 0f);
            r.transform.rotation = Quaternion.Euler(0f, 0f, rot);
            r.transform.localScale = Vector3.one * (size / s.bounds.size.x);
        }
        GameObject.Find("Player").transform.position = new Vector3(-16f, .6f, 0f);
        Put(lib.dustLand[1], -16f, .1f, 1.3f);                       // landing dust at Qori's feet
        Put(lib.dustRun[1], -14.2f, .08f, .65f);                     // run dust
        Put(lib.hitSparks[0], -12.6f, .8f, .7f);                     // hit spark
        Put(lib.hitBlock, -11.6f, .8f, .55f);                        // blocked chip
        Put(lib.telegraphGlint, -10.8f, .9f, .55f);                  // warning glint
        Put(lib.deathPuff[1], -9.4f, .6f, 1.3f);                     // death puff
        Put(lib.sapOrb, -8.2f, .3f, .34f);                           // sap orb
        for (int i = 0; i < 3; i++) Put(lib.leaves[i], -7.6f + i * .3f, .7f + i * .15f, .26f, i * 70f);
        Camera camera = Camera.main; camera.transform.position = new Vector3(-12f, 1.3f, -10f); camera.orthographicSize = 2.2f;
        foreach (var layer in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(camera);
        var target = new RenderTexture(1920, 1080, 24); camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
        var read = new Texture2D(1920, 1080, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); read.Apply();
        File.WriteAllBytes(Path.Combine(folder, "fx.png"), read.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        Debug.Log("[FxCapture] written");
    }
}
