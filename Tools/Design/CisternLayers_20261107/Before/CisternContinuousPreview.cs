using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Local Editor review preference, never a scene component or player-build hook.
// Persist across Play/Stop and domain reload so the user can revisit the trial.
[InitializeOnLoad]
public static class CisternContinuousPreview
{
    const string Menu = "Qolossal/Art Review/Continuous Cistern Preview";
    public const string ArtPath = "Assets/Art/Prototypes/CisternContinuous_20261106/CisternPanorama_Distant.png";
    static string Key => "Qolossal.ContinuousCisternPreview." + Application.dataPath;
    static double nextCheck;
    static CisternContinuousPreview() { EditorApplication.update += Tick; }

    public static bool Enabled => EditorPrefs.GetBool(Key, false);
    public static void SetEnabled(bool enabled)
    {
        EditorPrefs.SetBool(Key, enabled);
        if (!enabled)
            foreach (var c in Resources.FindObjectsOfTypeAll<CaveParallaxComparison>())
                if (c != null && c.gameObject.scene.IsValid()) Object.DestroyImmediate(c.gameObject);
        nextCheck = 0;
        Tick();
    }

    [MenuItem(Menu)] static void Toggle() => SetEnabled(!Enabled);
    [MenuItem(Menu, true)] static bool Validate() { UnityEditor.Menu.SetChecked(Menu, Enabled); return true; }

    static void Tick()
    {
        if (!Enabled || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying ||
            EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + .5;
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Game/Qolossal_Underground.unity");
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (Application.isPlaying && Time.time < 1) return;
        var painting = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtPath);
        if (painting == null) return;
        foreach (var existing in Resources.FindObjectsOfTypeAll<CaveParallaxComparison>())
            if (existing != null && existing.gameObject.scene == scene && existing.enabled && existing.BindingCount == 7)
            {
                if (!existing.HasContinuousPainting) existing.SetContinuousPainting(painting, CaveParallaxComparison.ContinuousReviewRect);
                return;
            }
        foreach (var old in Resources.FindObjectsOfTypeAll<CaveParallaxComparison>())
            if (old != null && old.gameObject.scene == scene) Object.DestroyImmediate(old.gameObject);
        var go = new GameObject("Continuous Cistern art preview (Editor only)") { hideFlags = HideFlags.DontSave };
        SceneManager.MoveGameObjectToScene(go, scene);
        var comparison = go.AddComponent<CaveParallaxComparison>();
        comparison.Configure(new Vector2(1610, 107.5f));
        comparison.SetContinuousPainting(painting, CaveParallaxComparison.ContinuousReviewRect);
        comparison.SetGentle(false, false);
    }
}
