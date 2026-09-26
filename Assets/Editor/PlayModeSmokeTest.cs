using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Enters real Play mode in the A0 test room, lets it run for a few seconds, and logs what exists
// at runtime (HUD, rig, mechanics, crawlers) plus any errors, then exits the editor. A session
// flag carries the request across the domain reload that entering Play mode performs.
// Usage (no -quit): -executeMethod PlayModeSmokeTest.Run
[InitializeOnLoad]
public static class PlayModeSmokeTest
{
    const string Key = "Qolossal.PlayModeSmokeTest";
    static int frames, errors;

    static PlayModeSmokeTest()
    {
        if (!SessionState.GetBool(Key, false)) return;
        Application.logMessageReceived += (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception) { errors++; Debug.Log("[Smoke] runtime error: " + message + "\n" + stack); }
        };
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/A0_TestRoom.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        if (++frames < 240 || Time.time < 3f) return;
        EditorApplication.update -= Tick;
        SessionState.SetBool(Key, false);
        bool Has(string name) => Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(t => t.name == name);
        var hud = Object.FindFirstObjectByType<GameHud>();
        var health = Object.FindFirstObjectByType<PlayerHealth>();
        var qori = Object.FindFirstObjectByType<QoriAnimator>();
        int leaves = hud != null ? hud.GetComponentsInChildren<UnityEngine.UI.Image>().Count(i => i.name.StartsWith("Leaf") && i.enabled) : 0;
        Debug.Log($"[Smoke] frames {frames}, time {Time.time:F1}s");
        Debug.Log($"[Smoke] HUD present: {hud != null}, leaves shown: {leaves}");
        Debug.Log($"[Smoke] health {health?.Health}/{health?.MaximumHealth}");
        Debug.Log($"[Smoke] Qori weapon parent: {(qori != null && qori.weapon != null ? qori.weapon.transform.parent.name : "none")}, visible {qori?.weapon?.enabled}, sprite {qori?.weapon?.sprite?.name}");
        Debug.Log($"[Smoke] mechanics: {Has("Mechanics")}, portal {Has("Portal A0")}, crawlers {Object.FindObjectsByType<GroundCreature>(FindObjectsSortMode.None).Length}, parallax {Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None).Length}");
        Debug.Log($"[Smoke] runtime errors: {errors}");
        EditorApplication.Exit(0);
    }
}
