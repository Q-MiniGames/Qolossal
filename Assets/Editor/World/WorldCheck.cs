using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch check of the world state without play mode: the saved game's knots, charting, veins and
// collectibles (and an old version 1 save loading into them), the world atlas the builders
// record, where a Wild Vein may throw Qori, and a stir variant changing A0's Crease. The
// player's own save file is set aside first and put back at the end.
// Usage: -executeMethod WorldCheck.Run
public static class WorldCheck
{
    static int failures;
    static void Expect(bool ok, string what) { if (!ok) failures++; Debug.Log($"[WorldCheck] {(ok ? "PASS" : "FAIL")} {what}"); }

    public static void Run()
    {
        string backup = GameSave.FilePath + ".worldcheck";
        if (File.Exists(GameSave.FilePath)) File.Copy(GameSave.FilePath, backup, true);
        failures = 0;
        try { Checks(); }
        catch (System.Exception e) { failures++; Debug.LogError("[WorldCheck] " + e); }
        finally
        {
            GameSave.Clear();
            if (File.Exists(backup)) { File.Copy(backup, GameSave.FilePath, true); File.Delete(backup); }
        }
        Debug.Log($"[WorldCheck] finished with {failures} failure(s)");
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }

    static void Checks()
    {
        // An old save (version 1) keeps its relics and gains the empty world lists.
        File.WriteAllText(GameSave.FilePath, "{\"version\":1,\"relics\":[\"climbing-moss\"],\"pickups\":[\"a1-heartseed\"],\"checkpoints\":[],\"lastScene\":\"\"}");
        GameSave.Reload();
        Expect(GameSave.HasRelic(Relics.ClimbingMoss) && GameSave.HeartSeeds == 1 && GameSave.KnotsAwake.Count == 0 && GameSave.Veins.Count == 0,
               "a version 1 save loads with its relics and seeds, and no knots or veins");

        GameSave.Clear();
        int changes = 0; System.Action count = () => changes++;
        GameSave.WorldChanged += count;
        GameSave.WakeKnot(Knots.Grip); GameSave.WakeKnot(Knots.Grip);
        Expect(GameSave.IsKnotAwake(Knots.Grip) && !GameSave.IsKnotAwake(Knots.Reach) && changes == 1, "waking a knot saves it and announces the change once");
        GameSave.SetKnotAwake(Knots.Grip, false);
        Expect(!GameSave.IsKnotAwake(Knots.Grip) && changes == 2, "a knot can be put back to sleep (tests and the World State window)");
        GameSave.WorldChanged -= count;

        Expect(GameSave.Chart("a0-mossy-hollow") && !GameSave.Chart("a0-mossy-hollow") && GameSave.IsCharted("a0-mossy-hollow"), "charting a level is saved, and reported new only the first time");
        Expect(GameSave.TravelVein("r1k-west", "a0-knot") && !GameSave.TravelVein("a0-knot", "r1k-west"), "a vein is one vein whichever way it is travelled");
        Expect(GameSave.KnowsVein("a0-knot", "r1k-west") && GameSave.KnowsVeinEnd("a0-knot") && GameSave.KnowsVeinEnd("r1k-west") && !GameSave.KnowsVeinEnd("a0"),
               "both ends of a travelled vein are known (and ids are matched whole)");
        GameSave.AddLoreStone("a0-lore"); GameSave.AddLoreStone("a0-lore"); GameSave.AddSproutling("s1");
        Expect(GameSave.LoreStones.Count == 1 && GameSave.Sproutlings == 1, "Lore Stones and Sproutlings are saved once each");
        GameSave.Reload();
        Expect(GameSave.IsCharted("a0-mossy-hollow") && GameSave.KnowsVein("a0-knot", "r1k-west") && GameSave.HasLoreStone("a0-lore"), "the world state survives a reload from the file");

        // Safe saving: each write swaps a finished file in and keeps the one before as a backup.
        GameSave.AddLoreStone("a0-lore-2");
        Expect(File.Exists(GameSave.BackupPath) && !File.Exists(GameSave.FilePath + ".tmp"), "a save keeps the previous one as a backup and leaves no temporary file");
        Expect(File.ReadAllText(GameSave.BackupPath).Contains("a0-lore") && !File.ReadAllText(GameSave.BackupPath).Contains("a0-lore-2"), "the backup is the save from before the last change");
        File.WriteAllText(GameSave.FilePath, "{\"version\":2,\"relics\":[\"cli");   // a write cut off half-way
        GameSave.Reload();
        Expect(GameSave.IsCharted("a0-mossy-hollow") && GameSave.HasLoreStone("a0-lore"), "a cut-off save falls back to the backup instead of starting over");
        File.WriteAllText(GameSave.FilePath, "");
        GameSave.Reload();
        Expect(GameSave.IsCharted("a0-mossy-hollow"), "so does an empty one");

        // The atlas the room builders recorded.
        var atlas = AssetDatabase.LoadAssetAtPath<WorldAtlas>("Assets/Resources/World/WorldAtlas.asset");
        Expect(atlas != null && atlas.levels.Count >= 4, $"the world atlas has the four levels ({atlas?.levels.Count})");
        var a0 = atlas.LevelOf("A0_TestRoom"); var r1k = atlas.LevelOf(R1GripKnotBuilder.SceneName);
        var a1 = atlas.LevelOf(A1RoomBuilder.SceneName); var a2 = atlas.LevelOf(A2RoomBuilder.SceneName);
        Expect(a0.waymark == "a0-waymark" && r1k.waymark == "r1k-waymark" && a1.waymark == "a1-gallery" && a2.waymark == "a2-arena", "each level's Waymark is recorded");
        Expect(a0.veins.Single(v => v.portalId == "a0-wild").wild, "A0's Wild Vein is recorded as wild");
        var elbow = r1k.veins.Single(v => v.portalId == "r1k-elbow");
        Expect(elbow.knot == Knots.Grip && elbow.afterKnot && a1.veins.Single(v => v.portalId == "a1-palm").afterKnot, "the veins the Grip stir grows are recorded as after-Grip");
        // Every fixed vein end names a partner that names it back.
        var ends = atlas.levels.SelectMany(l => l.veins.Select(v => (level: l, end: v))).Where(e => !e.end.wild).ToList();
        var broken = ends.Where(e => atlas.LevelOf(e.end.destinationScene) != null &&
            !atlas.LevelOf(e.end.destinationScene).veins.Any(v => v.portalId == e.end.destinationPortal && v.destinationPortal == e.end.portalId)).Select(e => e.end.portalId).ToList();
        Expect(broken.Count == 0, "every vein between built levels is a matched pair" + (broken.Count > 0 ? ": " + string.Join(", ", broken) : ""));

        // Wild Vein destinations: charted Waymarks and untravelled ends that exist now, never wild ones.
        var places = atlas.WildDestinations("a0-wild");
        bool Has(string arrival) => places.Any(p => p.arrival == arrival);
        Expect(Has("a0-waymark") && !Has("a1-gallery"), "a Wild Vein can throw Qori to a charted Waymark, not an uncharted one");
        Expect(Has("a1-west") && Has("a2-east") && !Has("a0-knot") && !Has("r1k-west"), "and to vein ends not yet travelled, not known ones");
        Expect(!Has("a0-wild") && !Has("r1k-elbow") && !Has("a1-palm"), "never to itself, nor to veins the Grip stir hasn't grown yet");
        GameSave.WakeKnot(Knots.Grip); GameSave.Chart("r3-ribwood");
        places = atlas.WildDestinations("a0-wild");
        Expect(Has("r1k-elbow") && Has("a1-palm") && Has("a2-arena"), "after the Grip stir its new veins count, and newly charted Waymarks too");
        GameSave.SetKnotAwake(Knots.Grip, false);

        // A0's Crease: the finger bridge exists only after the Grip Knot wakes.
        EditorSceneManager.OpenScene("Assets/Scenes/A0_TestRoom.unity");
        foreach (var b in Object.FindObjectsByType<TerrainBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None)) b.Rebuild();
        var variant = Object.FindObjectsByType<StirVariant>(FindObjectsSortMode.None).Single(v => v.name.StartsWith("Crease Bridge"));
        Physics2D.SyncTransforms();
        RaycastHit2D Probe() => Physics2D.Raycast(new Vector2(181f, 20f), Vector2.down, 30f, LayerMask.GetMask("Ground"));
        Expect(!variant.transform.GetChild(0).gameObject.activeSelf && Probe().point.y < 4f, $"before the Grip stir the Crease is an open pit (ground at {Probe().point.y:F1})");
        GameSave.WakeKnot(Knots.Grip); variant.Apply(); Physics2D.SyncTransforms();
        Expect(Probe().collider != null && Probe().collider.name == "Clenched Finger" && Mathf.Abs(Probe().point.y - 9f) < .3f,
               $"after it a clenched finger bridges the Crease at the walk line ({Probe().collider?.name} at {Probe().point.y:F2})");
        GameSave.SetKnotAwake(Knots.Grip, false); variant.Apply();
        Expect(!variant.transform.GetChild(0).gameObject.activeSelf, "and the pit returns if the knot sleeps again");

        GameSave.Clear();
        Expect(!File.Exists(GameSave.FilePath) && !File.Exists(GameSave.BackupPath), "a new game removes the save and its backup");
    }
}
