using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// The saved game: relics found, pickups collected (heart seeds), the checkpoint touched in each area, and the last checkpoint
// overall (where a fresh launch continues from). Written as JSON to the persistent data folder
// whenever something changes. Health is not saved; a loaded game starts with full hearts.
public static class GameSave
{
    [Serializable] sealed class Checkpoint { public string scene, id; }
    [Serializable] sealed class Data
    {
        public int version = 1;
        public List<string> relics = new List<string>();
        public List<string> pickups = new List<string>();
        public List<Checkpoint> checkpoints = new List<Checkpoint>();
        public string lastScene = "";
    }

    static Data data;
    public static string FilePath => Path.Combine(Application.persistentDataPath, "qolossal_save.json");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => data = null;

    static Data Current
    {
        get
        {
            if (data != null) return data;
            try { data = File.Exists(FilePath) ? JsonUtility.FromJson<Data>(File.ReadAllText(FilePath)) : null; }
            catch (Exception e) { Debug.LogWarning("[GameSave] could not read the save, starting fresh: " + e.Message); }
            return data ??= new Data();
        }
    }

    static void Write()
    {
        try { File.WriteAllText(FilePath, JsonUtility.ToJson(Current, true)); }
        catch (Exception e) { Debug.LogWarning("[GameSave] could not write the save: " + e.Message); }
    }

    public static IReadOnlyList<string> Relics => Current.relics;
    public static bool HasRelic(string id) => Current.relics.Contains(id);
    public static void AddRelic(string id)
    {
        if (string.IsNullOrEmpty(id) || HasRelic(id)) return;
        Current.relics.Add(id); Write();
    }

    // One-off pickups (heart seeds), by their permanent ids.
    public static bool HasPickup(string id) => Current.pickups != null && Current.pickups.Contains(id);
    public static void AddPickup(string id)
    {
        if (string.IsNullOrEmpty(id) || HasPickup(id)) return;
        (Current.pickups ??= new List<string>()).Add(id); Write();
    }
    public static int HeartSeeds => Current.pickups == null ? 0 : Current.pickups.FindAll(p => p.Contains("heartseed")).Count;

    // The checkpoint last touched in `scenePath`, or "" if none.
    public static string CheckpointIn(string scenePath) => Current.checkpoints.Find(c => c.scene == scenePath)?.id ?? "";
    public static string LastScene => Current.lastScene;
    public static void SetCheckpoint(string scenePath, string id)
    {
        var entry = Current.checkpoints.Find(c => c.scene == scenePath);
        if (entry == null) Current.checkpoints.Add(entry = new Checkpoint { scene = scenePath });
        entry.id = id; Current.lastScene = scenePath;
        Write();
    }

    // A new game: forgets everything.
    public static void Clear()
    {
        data = new Data();
        try { if (File.Exists(FilePath)) File.Delete(FilePath); }
        catch (Exception e) { Debug.LogWarning("[GameSave] could not delete the save: " + e.Message); }
    }

    // A launched game (not the editor) continues in the area of the last checkpoint touched.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Continue()
    {
        if (Application.isEditor) return;
        string scene = LastScene;
        if (string.IsNullOrEmpty(scene) || scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene().path) return;
        if (UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(scene) < 0) return;
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
    }
}
