using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

// Qolossal > Scene Finder: every scene in the project and every underground room, sorted into categories, with a search
// box. Click a scene to open it; click a room to open the underground and frame that room. The search also finds rooms by
// the creature in them ("golem"). Nothing is moved on disk: scene paths are hard-wired in the build and the builders.
public sealed class SceneFinder : EditorWindow
{
    const string RoomsPath = "Tools/Design/UndergroundPlatforming_20261030/rooms.json";
    const string PlanPath = "Tools/Design/UndergroundPlatforming_20261030/platforming_v1.json";

    sealed class Entry { public string label, search, path, roomId; }
    sealed class Category { public string title; public readonly List<Entry> entries = new List<Entry>(); }

    [Serializable] sealed class PlanShape { public string kind, design; }
    [Serializable] sealed class PlanRoom { public string id; public PlanShape[] shapes; }
    [Serializable] sealed class Plan { public PlanRoom[] roomList; }

    // Underground room groups, in play order: the file's group name, then its heading here.
    static readonly (string group, string title)[] RoomGroups =
    {
        ("A", "Underground: Route A, the Sluice"), ("C", "Underground: Route C, the Garden"),
        ("D", "Underground: Route D, the Wind"), ("E", "Underground: Route E, the Wilted"), ("H", "Underground: the Heart"),
        ("MR01", "Underground: caves under the Cradle"), ("MR02", "Underground: caves under the Causeway"),
        ("MR03", "Underground: caves under the Terraces"), ("MR05", "Underground: caves under the Ribwood"),
        ("MR06", "Underground: caves under the Heights"), ("MR07", "Underground: caves under the Summit"),
    };

    List<Category> categories;
    ScrollView list; string query = "";

    [MenuItem("Qolossal/Scene Finder %#g")]
    public static void Open() => GetWindow<SceneFinder>("Scene Finder");

    public void CreateGUI()
    {
        categories = Build();
        var search = new TextField("Search");
        search.RegisterValueChangedCallback(e => { query = e.newValue.Trim().ToLowerInvariant(); Refresh(); });
        rootVisualElement.Add(search);
        list = new ScrollView();
        list.style.flexGrow = 1f;
        rootVisualElement.Add(list);
        Refresh();
    }

    void OnFocus() { if (list != null) { categories = Build(); Refresh(); } }

    void Refresh()
    {
        list.Clear();
        foreach (var c in categories)
        {
            var shown = query.Length == 0 ? c.entries : c.entries.Where(e => e.search.Contains(query)).ToList();
            if (shown.Count == 0) continue;
            var fold = new Foldout { text = $"{c.title} ({shown.Count})", viewDataKey = "scene-finder-" + c.title };
            // Searching opens everything it finds; otherwise each stays as it was left.
            if (query.Length > 0) { fold.viewDataKey = null; fold.value = true; }
            foreach (var e in shown)
            {
                var entry = e;
                var open = new Button(() => Go(entry)) { text = entry.label, tooltip = entry.path ?? entry.roomId };
                open.style.unityTextAlign = TextAnchor.MiddleLeft;
                fold.Add(open);
            }
            list.Add(fold);
        }
    }

    static void Go(Entry e)
    {
        if (e.roomId == null)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(e.path);
            return;
        }
        string path = RegionalSceneCatalog.Folder + RegionalSceneCatalog.Underground + ".unity";
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != path)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path);
        }
        var room = UnityEngine.Object.FindObjectsByType<UndergroundRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(r => r.roomId == e.roomId);
        if (room == null) { Debug.LogWarning("Scene Finder: no room " + e.roomId + " in the underground scene."); return; }
        UndergroundRoomNavigator.FocusRoom(room);
    }

    static List<Category> Build()
    {
        var result = new List<Category>();
        Category Get(string title)
        {
            var c = result.FirstOrDefault(x => x.title == title);
            if (c == null) { c = new Category { title = title }; result.Add(c); }
            return c;
        }

        // Fixed order first, so the game's own scenes always lead the list.
        Get("Game: the surface"); Get("Game: the underground scene");
        foreach (var (_, title) in RoomGroups) Get(title);
        Get("Listening Retreat");

        var scenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        foreach (var path in scenes)
        {
            string title = SceneCategory(path);
            if (title == null) continue;
            string name = Path.GetFileNameWithoutExtension(path);
            // Rehearsal copies share a name with the real scene: say which rehearsal it is.
            string parent = Path.GetFileName(Path.GetDirectoryName(path));
            string label = title.StartsWith("Development: art") ? $"{name}  ({parent})" : name;
            Get(title).entries.Add(new Entry { label = label, search = (label + " " + path).ToLowerInvariant(), path = path });
        }

        foreach (var room in Rooms())
        {
            string title = RoomGroups.FirstOrDefault(g => g.group == room.group).title;
            if (title == null) title = "Underground: other rooms";
            Get(title).entries.Add(new Entry { label = room.label, search = room.search, roomId = room.id });
        }
        result.RemoveAll(c => c.entries.Count == 0);
        return result;
    }

    // The folder says what a scene is for. Anything new lands somewhere sensible by itself.
    static string SceneCategory(string path)
    {
        if (path.StartsWith("Assets/Settings/")) return null;   // the editor's new-scene template
        if (path.StartsWith(RegionalSceneCatalog.Folder))
            return Path.GetFileNameWithoutExtension(path) == RegionalSceneCatalog.Underground ? "Game: the underground scene" : "Game: the surface";
        if (path.StartsWith("Assets/ListeningRetreat/")) return "Listening Retreat";
        if (path.StartsWith("Assets/Development/Layouts/")) return "Development: layouts";
        if (path.StartsWith("Assets/Development/ArtReview/")) return "Development: art review";
        if (path.StartsWith("Assets/Development/LegacyScenes/")) return "Development: old levels and prototypes";
        if (path.StartsWith("Assets/Development/")) return "Development: test scenes";
        return "Other scenes";
    }

    sealed class RoomRow { public string id, group, label, search; }

    // rooms.json is keyed by room id, which JsonUtility can't read: this takes the fields it needs from each room's start.
    static List<RoomRow> Rooms()
    {
        var rows = new List<RoomRow>();
        if (!File.Exists(RoomsPath)) return rows;
        var creatures = new Dictionary<string, PlanShape[]>();
        if (File.Exists(PlanPath))
        {
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(PlanPath));
            if (plan?.roomList != null) foreach (var r in plan.roomList) creatures[r.id] = (r.shapes ?? new PlanShape[0]).Where(s => !string.IsNullOrEmpty(s.design)).ToArray();
        }
        var pattern = new System.Text.RegularExpressions.Regex("\"id\":\\s*\"([^\"]+)\",\\s*\"name\":\\s*\"([^\"]*)\",\\s*\"kind\":\\s*\"[^\"]*\",\\s*\"group\":\\s*\"([^\"]*)\"");
        foreach (System.Text.RegularExpressions.Match m in pattern.Matches(File.ReadAllText(RoomsPath)))
        {
            string id = m.Groups[1].Value, name = m.Groups[2].Value;
            creatures.TryGetValue(id, out var inside);
            inside = inside ?? new PlanShape[0];
            // The room's boss on the button; all its creatures in the search.
            var boss = inside.FirstOrDefault(s => s.kind == "boss" || s.design.StartsWith("M-"));
            string bossName = boss == null ? "" : "   [" + (boss.kind == "boss" ? "Guardian: " : "Mini-boss: ") + Plain(boss.design) + "]";
            rows.Add(new RoomRow
            {
                id = id, group = m.Groups[3].Value, label = $"{id}  {name}{bossName}",
                search = (id + " " + name + " " + string.Join(" ", inside.Select(s => s.design))).ToLowerInvariant(),
            });
        }
        return rows;
    }

    // "M-01 Moss Matriarch (mini-boss)" becomes "Moss Matriarch".
    static string Plain(string design)
    {
        string text = design.Replace(" (mini-boss)", "");
        int space = text.IndexOf(' ');
        return space > 0 ? text.Substring(space + 1) : text;
    }

    // Batch check: -executeMethod SceneFinder.CheckBatch -quit. Lists the categories and confirms every room is in the scene.
    public static void CheckBatch()
    {
        var built = Build();
        foreach (var c in built) Debug.Log($"SceneFinder: {c.title}: {c.entries.Count} [{string.Join(" | ", c.entries.Take(3).Select(e => e.label))}]");
        EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder + RegionalSceneCatalog.Underground + ".unity");
        var inScene = new HashSet<string>(UnityEngine.Object.FindObjectsByType<UndergroundRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(r => r.roomId));
        var rooms = built.SelectMany(c => c.entries).Where(e => e.roomId != null).ToList();
        var missing = rooms.Where(e => !inScene.Contains(e.roomId)).Select(e => e.roomId).ToList();
        var paths = built.SelectMany(c => c.entries).Where(e => e.path != null).ToList();
        Debug.Log($"SceneFinder: {paths.Count} scenes, {rooms.Count} rooms listed; {inScene.Count} rooms in the scene; not found: [{string.Join(", ", missing)}]");
        Debug.Log("SceneFinder: golem finds " + string.Join(", ", rooms.Where(e => e.search.Contains("golem")).Select(e => e.label)));
    }
}
