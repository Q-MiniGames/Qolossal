using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// The saved game: relics found, pickups collected (heart seeds), the checkpoint touched in each area, and the last checkpoint
// overall (where a fresh launch continues from). Also the state of the titan's world: the knots
// woken, the levels charted (at their Waymarks), the veins travelled, and the Sproutlings and Lore
// Stones found. Written as JSON to the persistent data folder whenever something changes. Health
// is not saved; a loaded game starts with full hearts.
public static class GameSave
{
    [Serializable] sealed class Checkpoint { public string scene, id; }
    [Serializable] sealed class Data
    {
        public int version = 2;
        public List<string> relics = new List<string>();
        public List<string> knots = new List<string>();        // Knots ids, in the order woken
        public List<string> charted = new List<string>();      // GameArea level ids
        public List<string> veins = new List<string>();        // "portalA|portalB", ids sorted
        public List<string> sproutlings = new List<string>();
        public List<string> loreStones = new List<string>();
        public List<string> pickups = new List<string>();
        public List<Checkpoint> checkpoints = new List<Checkpoint>();
        public string lastScene = "";
    }

    static Data data;
    public static string FilePath => Path.Combine(Application.persistentDataPath, "qolossal_save.json");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { data = null; WorldChanged = null; }

    static Data Current
    {
        get
        {
            if (data != null) return data;
            try { data = File.Exists(FilePath) ? JsonUtility.FromJson<Data>(File.ReadAllText(FilePath)) : null; }
            catch (Exception e) { Debug.LogWarning("[GameSave] could not read the save, starting fresh: " + e.Message); }
            data ??= new Data();
            // Saves from before the world state (version 1) have none of its lists.
            data.knots ??= new List<string>(); data.charted ??= new List<string>(); data.veins ??= new List<string>();
            data.sproutlings ??= new List<string>(); data.loreStones ??= new List<string>(); data.pickups ??= new List<string>();
            return data;
        }
    }

    // Raised when the titan's state changes (a knot wakes or sleeps again), so the stir variants
    // in the open scene can show their before or after state.
    public static event Action WorldChanged;

    // Rereads the file (the editor's World State window, after it has been changed elsewhere).
    public static void Reload() { data = null; WorldChanged?.Invoke(); }

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

    // ---------------------------------------------------------------- the titan's world

    public static IReadOnlyList<string> KnotsAwake => Current.knots;
    public static bool IsKnotAwake(string knot) => !string.IsNullOrEmpty(knot) && Current.knots.Contains(knot);
    public static void WakeKnot(string knot) => SetKnotAwake(knot, true);
    // Asleep again is only for tests and the editor's World State window.
    public static void SetKnotAwake(string knot, bool awake)
    {
        if (string.IsNullOrEmpty(knot) || IsKnotAwake(knot) == awake) return;
        if (awake) Current.knots.Add(knot); else Current.knots.Remove(knot);
        Write(); WorldChanged?.Invoke();
    }

    public static IReadOnlyList<string> Charted => Current.charted;
    public static bool IsCharted(string level) => !string.IsNullOrEmpty(level) && Current.charted.Contains(level);
    // Returns true the first time.
    public static bool Chart(string level)
    {
        if (string.IsNullOrEmpty(level) || IsCharted(level)) return false;
        Current.charted.Add(level); Write();
        return true;
    }

    // A vein is known once travelled, in either direction; both its ends are then known.
    public static string VeinKey(string a, string b) => string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
    public static IReadOnlyList<string> Veins => Current.veins;
    public static bool KnowsVein(string a, string b) => Current.veins.Contains(VeinKey(a, b));
    public static bool KnowsVeinEnd(string portalId) =>
        !string.IsNullOrEmpty(portalId) && Current.veins.Exists(v => v.StartsWith(portalId + "|") || v.EndsWith("|" + portalId));
    // Returns true the first time.
    public static bool TravelVein(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || KnowsVein(a, b)) return false;
        Current.veins.Add(VeinKey(a, b)); Write();
        return true;
    }

    public static int Sproutlings => Current.sproutlings.Count;
    public static bool HasSproutling(string id) => Current.sproutlings.Contains(id);
    public static void AddSproutling(string id) { if (!string.IsNullOrEmpty(id) && !HasSproutling(id)) { Current.sproutlings.Add(id); Write(); } }

    public static IReadOnlyList<string> LoreStones => Current.loreStones;
    public static bool HasLoreStone(string id) => Current.loreStones.Contains(id);
    public static void AddLoreStone(string id) { if (!string.IsNullOrEmpty(id) && !HasLoreStone(id)) { Current.loreStones.Add(id); Write(); } }

    // ---------------------------------------------------------------- checkpoints

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
        WorldChanged?.Invoke();
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
