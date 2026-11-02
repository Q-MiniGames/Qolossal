using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the authored greybox platforming (Tools/Design/UndergroundPlatforming_20261030/platforming_v1.json)
// into the connected underground. Each room gets its own "UGP Platforming" group under its Platforms
// group; a re-run replaces only that group. In a room marked replace, the old generated platforms are
// switched off (not deleted) and listed on the group, so Remove switches them back on.
public static class UndergroundPlatformingBuilder
{
    const string LayoutPath = "Tools/Design/UndergroundPlatforming_20261030/platforming_v1.json";
    const string SpritePath = "Assets/MountainReliefAtlas/Underground/Platforming/UGP_White.png";
    const string GroupName = "UGP Platforming";
    const int Order = 5;

    [Serializable] public sealed class Shape
    {
        public string kind, flag, label, done, prefab, design;
        public float[] rect, offset, feet;
        public float seconds = 3f, pause = .6f, up = 1.6f, safe = 1.2f, phase;
        public int amber = 20;
        public float half = 11f;
        public string[] requires;
    }
    [Serializable] public sealed class RoomSpec { public string id, note; public bool replace; public Shape[] shapes; }
    [Serializable] sealed class Layout { public RoomSpec[] roomList; }

    static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>
    {
        { "solid", new Color(.42f, .55f, .45f) }, { "rock", new Color(.45f, .45f, .48f) }, { "deck", new Color(.85f, .74f, .4f) },
        { "crumble", new Color(.78f, .5f, .25f) }, { "mover", new Color(.55f, .7f, .9f) }, { "water", new Color(.25f, .5f, .95f, .55f) },
        { "thorns", new Color(.9f, .2f, .25f) }, { "spill", new Color(.4f, .75f, 1f, .8f) }, { "valve", new Color(1f, .85f, .2f) },
        { "gate", new Color(.3f, .3f, .34f) }, { "anchor", new Color(1f, 1f, 1f) }, { "pod", new Color(.9f, .45f, .85f) },
        { "gust", new Color(.8f, .9f, 1f, .12f) }, { "grille", new Color(.75f, .8f, .9f, .3f) }, { "quarterlight", new Color(.35f, .3f, .3f, .35f) }, { "shelter", new Color(.1f, .12f, .16f, .6f) }, { "seal", new Color(.55f, .25f, .25f) },
    };

    [MenuItem("Qolossal/Underground/Build Platforming (greybox)")]
    public static void BuildMenu() => Build(EditorSceneManager.GetActiveScene().path == ScenePath ? null : ScenePath, save: false);

    [MenuItem("Qolossal/Underground/Remove Platforming (greybox)")]
    public static void RemoveMenu() { foreach (var room in Rooms()) Remove(room); Mark(); }

    // Batch: -executeMethod UndergroundPlatformingBuilder.BuildBatch (opens, builds and saves the scene).
    public static void BuildBatch() => Build(ScenePath, save: true);

    // Batch: -executeMethod UndergroundPlatformingBuilder.CheckBatch -captureDir <dir>
    // Static checks on the saved scene (greybox overlapping the terrain, checkpoints and arrivals buried
    // or over hazards) and one capture per built room.
    public static void CheckBatch()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Physics2D.SyncTransforms();
        var args = Environment.GetCommandLineArgs();
        int at = Array.IndexOf(args, "-captureDir");
        string dir = at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        var problems = new List<string>();
        int ground = LayerMask.GetMask("Ground");
        var groups = UnityEngine.Object.FindObjectsByType<UndergroundPlatformingGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var own = new HashSet<Collider2D>(groups.SelectMany(g => g.GetComponentsInChildren<Collider2D>(true)));
        var hits = new List<Collider2D>();
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = ground, useTriggers = false };
        foreach (var group in groups)
        {
            string room = group.GetComponentInParent<UndergroundRoom>().roomId;
            foreach (var c in group.GetComponentsInChildren<BoxCollider2D>(true))
            {
                // Solid blocks are meant to sit against the cave's walls, floor and corner chamfers.
                if (c.isTrigger || c.name.StartsWith("UGP solid") || c.name.StartsWith("UGP rock")) continue;
                var b = c.bounds;
                hits.Clear();
                Physics2D.OverlapBox(b.center, (Vector2)b.size - new Vector2(.1f, .1f), 0f, filter, hits);
                foreach (var h in hits.Where(h => !own.Contains(h) && h.enabled))
                    problems.Add($"{room}: {c.name} overlaps {PathOf(h.transform)}");
            }
            foreach (var cp in group.GetComponentInParent<UndergroundRoom>().GetComponentsInChildren<Checkpoint>(true))
            {
                var p = (Vector2)cp.transform.position + Vector2.up * .3f;
                hits.Clear(); Physics2D.OverlapPoint(p, filter, hits);
                if (hits.Count > 0) problems.Add($"{room}: checkpoint {cp.name} is inside {PathOf(hits[0].transform)}");
                foreach (var hz in group.GetComponentsInChildren<Collider2D>(true).Where(x => x.isTrigger && (x.GetComponent<Hazard>() || x.GetComponent<ThornHazard>())))
                    if (hz.bounds.Contains(new Vector3(p.x, p.y, hz.bounds.center.z))) problems.Add($"{room}: checkpoint {cp.name} stands in {hz.name}");
            }
        }
        problems.AddRange(HeartSealing(filter));
        Debug.Log($"UGP check: {groups.Length} rooms, {problems.Count} problems\n" + string.Join("\n", problems));
        File.WriteAllLines("Tools/Design/UndergroundPlatforming_20261030/check_results.txt",
            new[] { $"{groups.Length} rooms, {problems.Count} problems" }.Concat(problems));
        if (dir != null) Capture(groups, dir);
    }

    // The Heart's sectors are sealed: a flood fill of every place Qori's body fits (one-way decks count as open,
    // gravity ignored, so it over-approximates movement) from each entrance never reaches another entrance;
    // nobody reaches the heart (it is only seen).
    static IEnumerable<string> HeartSealing(ContactFilter2D filter)
    {
        var heart = Rooms().FirstOrDefault(r => r.roomId == "HEART");
        if (heart == null) return new string[0];
        var b = heart.WorldBounds; Vector2 o = b.min;
        var seeds = new Dictionary<string, Vector2> {
            { "A", o + new Vector2(1.5f, 12.8f) }, { "C", o + new Vector2(22f, 46.5f) }, { "D", o + new Vector2(70f, 46.5f) },
            { "E", o + new Vector2(82.5f, 16.8f) }, { "heart", o + new Vector2(42f, 12.8f) } };
        const float cell = .25f;
        int nx = Mathf.CeilToInt(b.size.x / cell), ny = Mathf.CeilToInt(b.size.y / cell);
        var hits = new List<Collider2D>();
        var known = new bool?[nx, ny];
        bool F(int i, int j)
        {
            if (i < 0 || j < 0 || i >= nx || j >= ny) return false;
            if (known[i, j] is bool k) return k;
            hits.Clear();
            Physics2D.OverlapBox(o + new Vector2((i + .5f) * cell, (j + .5f) * cell), new Vector2(.78f, 1.18f), 0f, filter, hits);
            bool open = hits.All(h => h.TryGetComponent(out PlatformEffector2D e) && e.useOneWay);
            known[i, j] = open; return open;
        }
        var reach = new Dictionary<string, HashSet<string>>();
        var errors = new List<string>();
        foreach (var seed in seeds.Where(k => k.Key != "heart"))
        {
            var seen = new bool[nx, ny]; var q = new Queue<Vector2Int>();
            var s0 = Vector2Int.FloorToInt((seed.Value - o) / cell);
            if (!F(s0.x, s0.y)) { errors.Add($"HEART: entrance {seed.Key} seed is blocked"); continue; }
            seen[s0.x, s0.y] = true; q.Enqueue(s0);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var n = c + d;
                    if (F(n.x, n.y) && !seen[n.x, n.y]) { seen[n.x, n.y] = true; q.Enqueue(n); }
                }
            }
            reach[seed.Key] = new HashSet<string>(seeds.Where(t => t.Key != seed.Key).Where(t =>
            {
                var c = Vector2Int.FloorToInt((t.Value - o) / cell);
                for (int dx = -2; dx <= 2; dx++) for (int dy = -2; dy <= 2; dy++)
                    if (c.x + dx >= 0 && c.y + dy >= 0 && c.x + dx < nx && c.y + dy < ny && seen[c.x + dx, c.y + dy]) return true;
                return false;
            }).Select(t => t.Key));
        }
        foreach (var kv in reach)
        {
            var expected = new string[0];
            Debug.Log($"UGP heart: from {kv.Key} reaches [{string.Join(", ", kv.Value)}]");
            if (!kv.Value.SetEquals(expected)) errors.Add($"HEART: from {kv.Key} reaches [{string.Join(", ", kv.Value)}], expected [{string.Join(", ", expected)}]");
        }
        return errors;
    }

    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

    static void Capture(UndergroundPlatformingGroup[] groups, string dir)
    {
        Directory.CreateDirectory(dir);
        var cam = new GameObject("UGP capture").AddComponent<Camera>();
        cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        var rt = new RenderTexture(1600, 900, 24);
        cam.targetTexture = rt;
        foreach (var group in groups)
        {
            var room = group.GetComponentInParent<UndergroundRoom>();
            var b = room.WorldBounds;
            cam.transform.position = new Vector3(b.center.x, b.center.y, -50f);
            cam.orthographicSize = Mathf.Max(b.size.y / 2f + 2f, (b.size.x / 2f + 2f) * 9f / 16f);
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            File.WriteAllBytes(System.IO.Path.Combine(dir, room.roomId + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(cam.gameObject);
    }

    // Batch: -executeMethod UndergroundPlatformingBuilder.DeleteOldPlatformsBatch. Deletes for good the old generated
    // platforms and their painted art that the build switched off (user, 31 Oct), so Remove can no longer bring them back.
    public static void DeleteOldPlatformsBatch()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int deleted = 0;
        foreach (var group in UnityEngine.Object.FindObjectsByType<UndergroundPlatformingGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (var go in group.switchedOff) if (go != null) { UnityEngine.Object.DestroyImmediate(go); deleted++; }
            group.switchedOff.Clear();
        }
        Mark();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"UGP: deleted {deleted} old platform objects.");
    }

    static string ScenePath => RegionalSceneCatalog.Folder + RegionalSceneCatalog.Underground + ".unity";

    static void Build(string openPath, bool save)
    {
        if (openPath != null) EditorSceneManager.OpenScene(openPath, OpenSceneMode.Single);
        var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
        var sprite = WhiteSprite();
        // New finds copy one of the scene's own Amber finds (its art, trigger and bob).
        var amberModel = AmberModel();
        var rooms = Rooms().ToDictionary(r => r.roomId);
        foreach (var room in rooms.Values) Remove(room);
        var paintedPlatforms = Resources.FindObjectsOfTypeAll<Transform>()
            .Where(t => t.name.StartsWith("Natural_Platforms_") && t.gameObject.scene.IsValid())
            .GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First().gameObject);
        int built = 0, shapes = 0;
        foreach (var spec in layout.roomList)
        {
            if (!rooms.TryGetValue(spec.id, out var room)) { Debug.LogError("UGP: no room " + spec.id); continue; }
            Remove(room);
            var platforms = room.transform.Find("Platforms") ?? new GameObject("Platforms").transform;
            if (platforms.parent != room.transform) platforms.SetParent(room.transform, false);
            var group = new GameObject(GroupName).AddComponent<UndergroundPlatformingGroup>();
            group.transform.SetParent(platforms, false);
            group.note = spec.note;
            if (spec.replace)
            {
                foreach (Transform child in platforms)
                    if (child != group.transform && child.gameObject.activeSelf) { child.gameObject.SetActive(false); group.switchedOff.Add(child.gameObject); }
                // The painted art of those old platforms lives in the shared terrain, one object per room.
                if (paintedPlatforms.TryGetValue("Natural_Platforms_" + spec.id, out var painted) && painted.activeSelf)
                { painted.SetActive(false); group.switchedOff.Add(painted); }
            }
            foreach (var s in spec.shapes)
            {
                if (s.kind == "reward") AmberFind(s, spec.id, room, group.transform, amberModel);
                else if (s.kind == "enemy" || s.kind == "boss") Creature(s, group.transform);
                else Make(s, group.transform, sprite);
                shapes++;
            }
            UndergroundArtKit.DressRoom(room, group.transform);
            LinkHeartQuarters(room, group);
            foreach (var keeper in group.GetComponentsInChildren<MraArenaGuardian>(true))
                if (keeper.flag.StartsWith("ug:MINI:")) keeper.rewards = room.GetComponentsInChildren<MraReward>(true);
            built++;
        }
        Mark();
        if (save) EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"UGP: built {built} rooms, {shapes} shapes.");
    }

    static IEnumerable<UndergroundRoom> Rooms() => UnityEngine.Object.FindObjectsByType<UndergroundRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None);

    static void Remove(UndergroundRoom room)
    {
        foreach (var group in room.GetComponentsInChildren<UndergroundPlatformingGroup>(true))
        {
            foreach (var go in group.switchedOff) if (go != null) go.SetActive(true);
            UnityEngine.Object.DestroyImmediate(group.gameObject);
        }
    }

    // The scene's own Amber find to copy: the same one every build, so a rebuild can't change the rewards' art or trigger.
    static MraReward AmberModel() => UnityEngine.Object.FindObjectsByType<MraReward>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .Where(m => m.kind == MraReward.Kind.Amber && m.GetComponentInParent<UndergroundPlatformingGroup>(true) == null)
        .OrderBy(m => m.name == "Reward amber" ? 0 : 1).ThenBy(m => m.rewardId, StringComparer.Ordinal).FirstOrDefault();

    // Lists the candidates and what the built rewards look like now.
    public static void AmberCheckBatch()
    {
        EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder + RegionalSceneCatalog.Underground + ".unity", OpenSceneMode.Single);
        string Look(MraReward m) { var r = m.GetComponentInChildren<SpriteRenderer>(true); var c = m.GetComponent<Collider2D>(); return $"{m.name} id={m.rewardId} sprite={(r != null && r.sprite != null ? r.sprite.name : "none")} collider={(c != null ? c.GetType().Name : "none")}"; }
        var all = UnityEngine.Object.FindObjectsByType<MraReward>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(m => m.kind == MraReward.Kind.Amber).ToList();
        foreach (var m in all.Where(m => m.GetComponentInParent<UndergroundPlatformingGroup>(true) == null).OrderBy(m => m.rewardId, StringComparer.Ordinal)) Debug.Log("UGP amber candidate: " + Look(m));
        foreach (var g in all.Where(m => m.GetComponentInParent<UndergroundPlatformingGroup>(true) != null).GroupBy(m => Look(m).Split(new[] { "sprite=" }, StringSplitOptions.None)[1])) Debug.Log($"UGP amber built: {g.Count()} x {g.Key}");
        Debug.Log("UGP amber model now: " + (AmberModel() != null ? AmberModel().rewardId : "none"));
    }

    static void Mark() => EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

    // The Heart's wake points light its quarter overlays (in MraHeartQuarter.Sectors order) and play the ending.
    static void LinkHeartQuarters(UndergroundRoom room, UndergroundPlatformingGroup group)
    {
        var wakes = group.GetComponentsInChildren<MraHeartQuarter>(true);
        if (wakes.Length == 0) return;
        var lights = MraHeartQuarter.Sectors.Select(k => group.transform.Cast<Transform>().FirstOrDefault(t => t.name == "UGP heart quarter " + k))
            .Select(t => t != null ? t.GetComponentInChildren<SpriteRenderer>() : null).ToArray();
        var ending = room.GetComponentInChildren<MraEnding>(true);
        foreach (var w in wakes) { w.quarters = lights; w.ending = ending; }
        if (ending == null) Debug.LogError("UGP: the Heart has no MraEnding to play.");
    }

    // A stand-in creature from Assets/Prefabs, named for the design it will become; a boss also gets its arena keeper.
    static void Creature(Shape s, Transform parent)
    {
        string id = s.design.Split(' ')[0];
        if (s.kind == "boss") { Guardian(s, id, parent); return; }
        if (OwnBehaviour(s, id, parent)) return;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(s.prefab);
        if (prefab == null) { Debug.LogError("UGP: missing creature prefab " + s.prefab); return; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = $"UGP {s.kind} {s.design} (stand-in {prefab.name})";
        go.transform.position = new Vector3(s.rect[0] + s.rect[2] / 2f, s.rect[1] + s.phase, 0f);
        // Drawn as the creature it stands in for, when its Ludo frames are in the library (UndergroundCreatureFrames).
        if (UndergroundCreatureLibrary.Find(id) != null) go.AddComponent<UndergroundCreatureFrames>().designId = id;
    }

    // A creature with its own script, in place of a stand-in prefab. A flyer hovers above its mark; the rest stand on it.
    static bool OwnBehaviour(Shape s, string id, Transform parent)
    {
        Type type = id switch
        {
            "E-10" => typeof(SiltLeechEnemy), "E-11" => typeof(SluiceCrabEnemy), "E-12" => typeof(PumpBloaterEnemy),
            "E-13" => typeof(RootSnapperEnemy), "E-14" => typeof(SeedSwarmEnemy), "E-15" => typeof(BrambleCreeperEnemy),
            "E-16" => typeof(KiteRayEnemy), "E-17" => typeof(GaleWispEnemy), "E-18" => typeof(HailSentryEnemy),
            "E-20" => typeof(WiltedHuskEnemy), "M-01" => typeof(MossMatriarchBoss), "M-02" => typeof(EchoWardenBoss),
            "M-03" => typeof(GrainGolemBoss), _ => null,
        };
        if (type == null) return false;
        var go = new GameObject($"UGP {s.kind} {s.design}");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(s.rect[0] + s.rect[2] / 2f, s.rect[1] + (s.phase >= 2f ? s.phase : 0f), 0f);
        go.AddComponent<Rigidbody2D>();
        var creature = (EnemyBase)go.AddComponent(type);
        if (id[0] == 'M')
        {
            // A mini-boss stays beaten and guards its room's reward (filled in once the room is built).
            var keeper = new GameObject($"UGP arena keeper {id}").AddComponent<MraArenaGuardian>();
            keeper.transform.SetParent(parent, false);
            keeper.guardian = creature; keeper.designId = s.design; keeper.defeatText = "The way is clear.";
            keeper.flag = "ug:MINI:" + parent.GetComponentInParent<UndergroundRoom>().roomId + ":down";
        }
        return true;
    }

    // A route's guardian on its arena floor, with the keeper that saves its defeat and opens the gate.
    static void Guardian(Shape s, string id, Transform parent)
    {
        var go = new GameObject($"UGP guardian {s.design}");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(s.rect[0] + s.rect[2] / 2f, s.rect[1], 0f);
        go.AddComponent<Rigidbody2D>();
        EnemyBase boss = id switch
        {
            "G-03" => go.AddComponent<SluiceWardenBoss>(),
            "G-04" => go.AddComponent<RootMatronBoss>(),
            "G-05" => go.AddComponent<TempestOwlBoss>(),
            _ => null,
        };
        if (boss == null) { Debug.LogError("UGP: no guardian for " + id); return; }
        var keeper = new GameObject($"UGP arena keeper {s.flag}").AddComponent<MraArenaGuardian>();
        keeper.transform.SetParent(parent, false);
        keeper.guardian = boss; keeper.flag = s.flag; keeper.designId = s.design;
    }

    // Saved for good under its own id (ugp:<room>:reward), so it never pays twice.
    static void AmberFind(Shape s, string roomId, UndergroundRoom room, Transform parent, MraReward model)
    {
        if (model == null) { Debug.LogError("UGP: no Amber find in the scene to copy."); return; }
        var find = UnityEngine.Object.Instantiate(model, parent);
        find.name = "UGP reward amber";
        find.transform.position = new Vector3(s.rect[0] + s.rect[2] / 2f, s.rect[1] + s.rect[3] / 2f, model.transform.position.z);
        find.kind = MraReward.Kind.Amber; find.amber = s.amber;
        find.rewardId = "ugp:" + roomId + ":reward"; find.title = room.roomName; find.text = "";
        find.gameObject.SetActive(true);
    }

    static void Make(Shape s, Transform parent, Sprite sprite)
    {
        var r = new Rect(s.rect[0], s.rect[1], s.rect[2], s.rect[3]);
        var go = new GameObject($"UGP {s.kind} {r.x:0.#},{r.y:0.#}");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(r.center.x, r.center.y, 0f);
        var art = Art(go.transform, sprite, r.size, Colors.TryGetValue(s.kind, out var c) ? c : Color.magenta,
                      s.kind is "water" or "gust" or "shelter" or "spill" ? Order + 2 : Order);
        BoxCollider2D Box(bool trigger)
        {
            var box = go.AddComponent<BoxCollider2D>(); box.size = r.size; box.isTrigger = trigger; return box;
        }
        // Painted art where the room's kit has it; greybox until then. Colliders are never touched.
        string roomId = parent.GetComponentInParent<UndergroundRoom>(true)?.roomId;
        int seed = Mathf.RoundToInt(r.center.x * 7f + r.center.y * 13f);
        bool painted = s.kind is "deck" or "crumble" or "mover" or "solid" or "rock" or "gate" or "seal" or "grille" or "thorns" or "water"
            or "spill" or "gust" or "shelter" or "pod" or "valve" or "quarter" or "quarterlight"
            && UndergroundArtKit.Dress(art, roomId, s.kind, r.size, seed);
        void Ground() => go.layer = LayerMask.NameToLayer("Ground");
        void OneWay(BoxCollider2D box)
        {
            var effector = go.AddComponent<PlatformEffector2D>(); effector.useOneWay = true; effector.surfaceArc = 160f;
            box.usedByEffector = true;
        }
        switch (s.kind)
        {
            case "solid": Ground(); Box(false); break;
            // A door that shuts while its room's boss is fighting.
            case "seal": { Ground(); var seal = go.AddComponent<UgArenaSeal>(); seal.solid = Box(false); seal.art = art; break; }
            case "rock": Ground(); Box(false); go.AddComponent<WallSurface>().allowsWallCling = false; break;
            // See-through bars between the Heart's sectors: they block Qori, the thread and attacks like rock.
            case "grille": Ground(); Box(false); go.AddComponent<WallSurface>().allowsWallCling = false; break;
            case "quarter":
            {
                Box(true); var wake = go.AddComponent<MraHeartQuarter>(); wake.sector = s.flag; wake.promptHeight = 2.6f;
                if (!painted) { art.color = new Color(.6f, .45f, .3f); art.transform.localScale = new Vector3(.8f, 1.6f, 1f); }
                break;
            }
            case "quarterlight":
            {
                // A quarter of the big oval heart: the quarter-disc sprite, mirrored so its round edge faces out.
                go.name = "UGP heart quarter " + s.flag; if (!painted) art.sprite = QuarterDisc(); art.sortingOrder = Order - 1;
                art.flipX = s.flag == "A" || s.flag == "C"; art.flipY = s.flag == "A" || s.flag == "E";
                break;
            }
            case "look":
            {
                Box(true); var look = go.AddComponent<MraUgLook>();
                look.centre = new Vector2(s.feet[0], s.feet[1]); look.halfHeight = s.half; look.label = "Hold to look at the heart";
                art.enabled = false;
                break;
            }
            case "deck": Ground(); OneWay(Box(false)); break;
            case "crumble":
            {
                Ground(); var box = Box(false); OneWay(box);
                var crumble = go.AddComponent<CrumblePlatform>(); crumble.image = art; crumble.solid = box; crumble.pieces = new Sprite[0];
                crumble.delay = .6f; crumble.respawn = 3f;
                break;
            }
            case "mover":
            {
                Ground(); var box = Box(false); OneWay(box);
                var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
                var mover = go.AddComponent<MovingPlatform>(); mover.solid = box;
                mover.offset = new Vector2(s.offset[0], s.offset[1]); mover.travelSeconds = s.seconds; mover.pauseSeconds = s.pause;
                break;
            }
            case "water": Box(true); go.AddComponent<Hazard>(); break;
            case "thorns": Box(true); go.AddComponent<ThornHazard>(); break;
            case "spill":
            {
                Box(true); var hazard = go.AddComponent<MraTimedHazard>(); hazard.art = art.transform;
                hazard.upSeconds = s.up; hazard.safeSeconds = s.safe;
                break;
            }
            case "safe":
            {
                Box(true); var shelter = go.AddComponent<MraUgShelter>(); shelter.feet = new Vector2(s.feet[0], s.feet[1]);
                art.enabled = false;
                break;
            }
            case "shelter":
            {
                Box(true); var shelter = go.AddComponent<MraUgShelter>(); shelter.feet = new Vector2(s.feet[0], s.feet[1]);
                break;
            }
            case "valve":
            {
                Box(true); var valve = go.AddComponent<MraUgSwitch>();
                valve.flag = s.flag; valve.label = s.label; valve.doneText = s.done; valve.wheel = art.transform;
                if (!painted) { art.transform.localScale = new Vector3(.9f, .9f, 1f); art.transform.localPosition = new Vector3(0f, -.2f, 0f); }
                break;
            }
            case "gate":
            {
                Ground(); var gate = go.AddComponent<MraGate>(); gate.solid = Box(false); gate.requires = s.requires; gate.plainArt = new[] { art };
                break;
            }
            case "anchor":
                go.AddComponent<ThreadAnchor>(); UnityEngine.Object.DestroyImmediate(art.gameObject); AddOwnSprite(go, sprite, r.size);
                UndergroundArtKit.Dress(go.GetComponent<SpriteRenderer>(), roomId, s.kind, r.size, seed);
                break;
            case "pod": Ground(); Box(false); go.AddComponent<MraPod>().art = art.transform; break;
            case "gust":
            {
                Box(true); var gust = go.AddComponent<MraUgGust>(); gust.art = art; gust.offset = s.phase;
                break;
            }
            default: Debug.LogError("UGP: unknown kind " + s.kind); break;
        }
    }

    static SpriteRenderer Art(Transform parent, Sprite sprite, Vector2 size, Color color, int order)
    {
        var go = new GameObject("Art");
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var art = go.AddComponent<SpriteRenderer>();
        art.sprite = sprite; art.color = color; art.sortingOrder = order;
        return art;
    }

    // ThreadAnchor needs its own SpriteRenderer.
    static void AddOwnSprite(GameObject go, Sprite sprite, Vector2 size)
    {
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var art = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
        art.sprite = sprite; art.color = Colors["anchor"]; art.sortingOrder = Order + 1;
    }

    const string QuarterPath = "Assets/MountainReliefAtlas/Underground/Platforming/UGP_QuarterDisc.png";

    // A filled quarter disc whose centre is the texture's bottom-left corner.
    static Sprite QuarterDisc()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(QuarterPath);
        if (sprite != null) return sprite;
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2((x + .5f) / size, (y + .5f) / size).magnitude;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * size / 2f)));
            }
        File.WriteAllBytes(QuarterPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(QuarterPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(QuarterPath);
        importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = size; importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(QuarterPath);
    }

    static Sprite WhiteSprite()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (sprite != null) return sprite;
        Directory.CreateDirectory(Path.GetDirectoryName(SpritePath));
        var tex = new Texture2D(4, 4); tex.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
        File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(SpritePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
        importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 4; importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }
}
