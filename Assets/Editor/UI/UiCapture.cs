using System;
using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch-mode review of the HUD and pause menu: renders them over the A0 test room at several
// health values and both menu pages. Usage: -executeMethod UiCapture.Capture -captureDir <folder>
public static class UiCapture
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public static void Capture()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-captureDir");
        string folder = index >= 0 && index + 1 < args.Length ? args[index + 1] : "Temp/UiCaptures";
        Directory.CreateDirectory(folder);
        EditorSceneManager.OpenScene("Assets/Scenes/A0_TestRoom.unity");
        foreach (var b in UnityEngine.Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None)) b.Rebuild();
        Camera camera = Camera.main;
        foreach (var layer in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(camera);

        var player = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
        typeof(PlayerHealth).GetMethod("Awake", Any).Invoke(player, null);
        var hud = new GameObject("Game HUD").AddComponent<GameHud>();
        typeof(GameHud).GetMethod("Build", Any).Invoke(hud, new object[] { player });

        // The armory only runs in play mode; show the sword icon for the capture.
        var weaponImage = (UnityEngine.UI.Image)typeof(GameHud).GetField("weapon", Any).GetValue(hud);
        weaponImage.sprite = UiSkin.Load().weaponIcons[0]; weaponImage.enabled = true;
        typeof(GameHud).GetField("shownWeapon", Any).SetValue(hud, null);

        var pause = UnityEngine.Object.FindFirstObjectByType<GamePauseMenu>() ?? new GameObject("Pause").AddComponent<GamePauseMenu>();

        void Shot(string name, int health, bool menu, bool confirm)
        {
            typeof(PlayerHealth).GetField("health", Any).SetValue(player, health);
            typeof(GameHud).GetMethod("Update", Any).Invoke(hud, null);
            weaponImage.sprite = UiSkin.Load().weaponIcons[0]; weaponImage.enabled = true;
            typeof(GamePauseMenu).GetField("confirmingNewGame", Any).SetValue(pause, confirm);
            typeof(GamePauseMenu).GetMethod("ShowMenu", Any).Invoke(pause, new object[] { menu });
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera; canvas.planeDistance = 1f;
            }
            var target = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var read = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            read.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); read.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), read.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
        }
        Shot("hud_full", 5, false, false);
        Shot("hud_hurt", 2, false, false);
        Shot("menu_main", 5, true, false);
        Shot("menu_confirm", 5, true, true);
        Debug.Log("[UiCapture] written");
    }
}
