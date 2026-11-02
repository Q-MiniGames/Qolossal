using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// A disposable Editor playthrough. The normal save is never moved, cleared or overwritten.
[InitializeOnLoad]
public static class MossPocketPreview
{
    const string Key = "Qolossal.MossPocketPreview.";
    const string ScenePath = "Assets/Scenes/Game/Qolossal_Underground.unity";
    static readonly BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Static;
    [Serializable] sealed class Setup { public SceneSetup[] scenes; }

    static MossPocketPreview()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.update += Tick;
        // A script reload during a preview must keep using its disposable save.
        if (Active) SetSave(SessionState.GetString(Key + "save", ""));
    }

    public static bool Active => SessionState.GetBool(Key + "active", false);

    [MenuItem("Qolossal/Underground/Play Moss Pocket (fresh preview)")]
    public static void Play()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Active) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string previous = (string)typeof(GameSave).GetField("fileName", Hidden).GetValue(null);
        SessionState.SetBool(Key + "saveWasNull", previous == null);
        SessionState.SetString(Key + "previousSave", previous ?? "");
        SessionState.SetString(Key + "scenes", JsonUtility.ToJson(new Setup { scenes = EditorSceneManager.GetSceneManagerSetup() }));
        SessionState.SetBool(Key + "optionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
        SessionState.SetInt(Key + "options", (int)EditorSettings.enterPlayModeOptions);
        string folder = Path.GetFullPath("Library/MossPocketPreview");
        Directory.CreateDirectory(folder);
        string save = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".json");
        SessionState.SetString(Key + "save", save);
        SessionState.SetBool(Key + "active", true);
        SessionState.SetBool(Key + "placed", false);
        try
        {
            SetSave(save);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }
        catch { Restore(); throw; }
    }

    [MenuItem("Qolossal/Underground/Play Moss Pocket (fresh preview)", true)]
    static bool CanPlay() => !EditorApplication.isPlayingOrWillChangePlaymode && !Active;

    static void SetSave(string path)
    {
        typeof(GameSave).GetField("fileName", Hidden).SetValue(null, path);
        typeof(GameSave).GetField("data", Hidden).SetValue(null, null);
    }

    static void Tick()
    {
        if (!Active || !EditorApplication.isPlaying || SessionState.GetBool(Key + "placed", false)
            || Time.timeSinceLevelLoad < 1f || AreaTransition.IsTransitioning) return;
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        var rest = UnityEngine.Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.name == "Rest Moss Pocket");
        if (!player || !rest) return;
        player.PlaceAt(rest.SpawnPosition, 1, true);
        SessionState.SetBool(Key + "placed", true);
        TownHud.Toast("MR01-C02 · Moss Pocket — fresh preview. Read the water carving. The surface exit is in MR01-C06. Stop Play to restore your usual session.", 7);
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        if (Active && state == PlayModeStateChange.EnteredEditMode) Restore();
    }

    static void Restore()
    {
        if (!Active) return;
        SessionState.SetBool(Key + "active", false);
        SetSave(SessionState.GetBool(Key + "saveWasNull", true) ? null : SessionState.GetString(Key + "previousSave", ""));
        EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + "optionsEnabled", false);
        EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(Key + "options", 0);
        var setup = JsonUtility.FromJson<Setup>(SessionState.GetString(Key + "scenes", "{}"));
        if (setup?.scenes != null) EditorSceneManager.RestoreSceneManagerSetup(setup.scenes);
    }
}
