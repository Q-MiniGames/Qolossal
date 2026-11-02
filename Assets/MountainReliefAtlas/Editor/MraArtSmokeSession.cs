using System.Reflection;
using UnityEditor;
using UnityEngine;

// Opt-in editor smoke tests use a separate save before any scene Awake runs.
// This file is editor-only and cannot affect player builds or ordinary Play mode.
public static class MraArtSmokeSession
{
    const string Key = "Qolossal.ArtSmokeSession";
    public static void Enable() => SessionState.SetBool(Key, true);
    public static void Disable()
    {
        SessionState.SetBool(Key, false);
        typeof(GameSave).GetField("fileName", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        GameSave.Reload();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void IsolateSave()
    {
        if (!SessionState.GetBool(Key, false)) return;
        typeof(GameSave).GetField("fileName", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, "qolossal_art_smoke.json");
        GameSave.Reload();
        Application.runInBackground = true;
        Debug.Log("[ArtSmoke] Isolated save: " + GameSave.FilePath);
    }
}
