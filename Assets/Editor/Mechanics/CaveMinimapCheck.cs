using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Qolossal.Atlas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// The cave minimap in the real scene (WS4): does the map data still match the scene, do rooms reveal on entry,
// do the Chart and the minimap agree, do markers follow the save, do the pad and keyboard work.
// Run: -executeMethod CaveMinimapCheck.Run -saveFile cavemap_check.json -captureDir <dir> (no -quit). Exits 1 on any failure.
// Rerun after update_map.py or any change to rooms, doors or platforming.
public static class CaveMinimapCheck
{
    const string StateKey = "Qolossal.CaveMinimapCheck";
    public static string Dir => SessionState.GetString(StateKey, "");

    public static void Run()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-saveFile") < 0) { Debug.LogError("CaveMinimapCheck needs -saveFile"); EditorApplication.Exit(1); return; }
        int at = System.Array.IndexOf(args, "-captureDir");
        string dir = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Temp/CaveMinimap";
        Directory.CreateDirectory(dir);
        SessionState.SetString(StateKey, dir);
        EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder + "Qolossal_Underground.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Resume()
    {
        if (string.IsNullOrEmpty(Dir)) return;
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredPlayMode && !string.IsNullOrEmpty(Dir))
                new GameObject("Cave minimap check").AddComponent<Driver>();
        };
    }

    sealed class Driver : MonoBehaviour
    {
        int failures, errors;
        readonly List<string> report = new List<string>();
        CaveMinimap map; PlayerMovement qori; AtlasData data; int keptSize;
        void Say(string s) { report.Add(s); Debug.Log("[CaveMap] " + s); }
        void Expect(bool ok, string what) { if (!ok) failures++; Say((ok ? "PASS " : "FAIL ") + what); }
        void Limit(List<string> bad, string what) { Expect(bad.Count == 0, $"{what} ({bad.Count} wrong)"); foreach (var b in bad.Take(25)) Say("     " + b); }

        IEnumerator Start()
        {
            Application.logMessageReceived += (m, st, t) => { if (t == LogType.Exception || t == LogType.Error) { errors++; Debug.Log("[CaveMap] runtime error: " + m); } };
            keptSize = PlayerPrefs.GetInt(CaveMinimap.SizeKey, 0);
            PlayerPrefs.DeleteKey(CaveMinimap.SizeKey);
            GameSave.Clear();
            float until = Time.realtimeSinceStartup + 30f;
            while ((map = CaveMinimap.Instance) == null || map.RoomCount == 0) { if (Time.realtimeSinceStartup > until) break; yield return null; }
            Expect(map != null && map.RoomCount == 89, "the minimap starts by itself, with 89 rooms");
            if (map == null) { Finish(); yield break; }
            Expect(map.ShortestStub >= 8f, $"every door shows the beginning of its passage (shortest {map.ShortestStub:F1} u of wall)");
            Expect(map.OpenedExits == FindObjectsByType<MraUgPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, $"every cave exit opens into a passage beginning ({map.OpenedExits})");
            qori = FindAnyObjectByType<PlayerMovement>();
            var clock = System.Diagnostics.Stopwatch.StartNew();
            data = JsonUtility.FromJson<AtlasData>(Resources.Load<TextAsset>("QolossalAtlas/AtlasData").text);
            CaveDiscovery.ContourOwners(data);
            Say($"INFO reading the map data and sorting the walls takes {clock.ElapsedMilliseconds} ms, once, on entering the caves");
            yield return new WaitForSecondsRealtime(.2f);

            MapVersusScene();

            // From here Qori is carried around the map, so nothing in the caves should touch him.
            var body = qori.GetComponent<Rigidbody2D>(); if (body != null) body.simulated = false;
            foreach (var enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
            GameSave.Clear();
            yield return null;

            var rooms = FindObjectsByType<UndergroundRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(r => r.roomId).ToArray();
            var byId = rooms.ToDictionary(r => r.roomId);

            // Route E first: entering it before the reveal must not put it on any map.
            yield return Visit(byId["E05"]);
            Expect(GameSave.HasFlag(CaveDiscovery.Flag("E05")) && map.KnownRoomCount == 0 && !map.DrawnRooms.Contains("E05"), "before the reveal, Route E stays off the map even if entered");
            GameSave.Clear();

            // The first half of the rooms: each appears on entry, and nothing unvisited is ever drawn.
            var visited = new HashSet<string>();
            var wrong = new List<string>();
            var half = rooms.Where(r => !r.roomId.StartsWith("E")).Where((r, i) => i % 2 == 0).ToArray();
            foreach (var room in half)
            {
                yield return Visit(room);
                visited.Add(room.roomId);
                if (!GameSave.HasFlag(CaveDiscovery.Flag(room.roomId))) wrong.Add(room.roomId + ": no flag on entry");
                if (map.CurrentRoomId != room.roomId) wrong.Add(room.roomId + ": minimap thinks it's in " + map.CurrentRoomId);
                if (!map.DrawnRooms.Contains(room.roomId)) wrong.Add(room.roomId + ": not drawn after entering");
                foreach (string drawn in map.DrawnRooms) if (!visited.Contains(drawn)) wrong.Add(room.roomId + ": draws unvisited " + drawn);
                if (map.VertexCount == 0) wrong.Add(room.roomId + ": empty mesh");
            }
            Limit(wrong, $"{half.Length} rooms reveal on entry, and nothing unvisited is drawn");

            // The Chart shows the same discoveries as the minimap.
            MraChart.Ensure();
            MraChart.Instance.Show();
            yield return null; yield return null;
            var atlas = MraChart.Instance.Atlas;
            var mismatch = data.nodes.Where(n => n.type == "underground" && atlas.RoomKnown(n.id) != visited.Contains(n.id)).Select(n => n.id).ToList();
            Limit(mismatch, "the Chart knows exactly the rooms the minimap knows");
            Expect(atlas.KnownRoomCount == map.KnownRoomCount, $"both count {atlas.KnownRoomCount} rooms");
            atlas.FocusRegion("MR04");
            yield return null;
            int hiddenSegments = 0, shownSegments = 0;
            for (int c = 0; c < atlas.Data.contours.Length; c++)
            {
                var known = atlas.ContourKnown(c); if (known == null) continue;
                foreach (bool k in known) { if (k) shownSegments++; else hiddenSegments++; }
            }
            Expect(hiddenSegments > 0 && shownSegments > 0, $"the Chart hides undiscovered outlines ({shownSegments} shown, {hiddenSegments} hidden)");
            CaptureCanvases("chart_MR04_half_explored");
            MraChart.Instance.Hide();
            yield return null; yield return null;

            // The remaining rooms, including Route E after the reveal.
            GameSave.SetFlag(MraState.RevealFlag);
            wrong.Clear();
            foreach (var room in rooms.Where(r => !visited.Contains(r.roomId)))
            {
                yield return Visit(room);
                visited.Add(room.roomId);
                if (!GameSave.HasFlag(CaveDiscovery.Flag(room.roomId)) || map.CurrentRoomId != room.roomId || !map.DrawnRooms.Contains(room.roomId)) wrong.Add(room.roomId);
            }
            Limit(wrong, "every other room reveals too, Route E after the reveal");
            Expect(map.KnownRoomCount == 89, $"all 89 rooms known ({map.KnownRoomCount})");

            yield return Markers(byId);
            yield return Inputs(byId);
            yield return Captures(byId);
            Finish();
        }

        IEnumerator Visit(UndergroundRoom room)
        {
            var b = room.WorldBounds;
            qori.transform.position = new Vector3(b.center.x, b.center.y, qori.transform.position.z);
            yield return new WaitForSecondsRealtime(.45f);   // room poll, minimap poll, one rebuild
            yield return null;
        }

        IEnumerator Markers(Dictionary<string, UndergroundRoom> byId)
        {
            GameSave.ClearFlag("cw:A:silt_gate");
            yield return Visit(byId["A02"]);
            yield return Visit(byId["A03"]);
            int shut = map.ShutMarks;
            Expect(shut >= 1, $"a shut gate is drawn near A03 ({shut})");
            GameSave.SetFlag("cw:A:silt_gate");
            yield return new WaitForSecondsRealtime(.45f);
            Expect(map.ShutMarks == shut - 1, $"opening it removes its mark ({shut} to {map.ShutMarks})");

            var reward = FindObjectsByType<MraReward>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(r => !r.Taken && byId["A03"].WorldBounds.Contains((Vector3)(Vector2)r.transform.position + Vector3.forward * byId["A03"].WorldBounds.center.z));
            int rewards = map.RewardMarks;
            GameSave.SetFlag(reward.rewardId);
            yield return new WaitForSecondsRealtime(.45f);
            Expect(rewards > 0 && map.RewardMarks == rewards - 1, $"taking a reward removes its mark ({rewards} to {map.RewardMarks})");

            var keeper = FindObjectsByType<MraArenaGuardian>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(k => k.flag == "ug:BOSS_A:open");
            var bossRoom = FindObjectsByType<UndergroundRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(r => CaveDiscovery.Distance(new Rect(r.WorldBounds.min, r.WorldBounds.size), keeper.guardian.transform.position)).First();
            yield return Visit(bossRoom);
            int bosses = map.BossMarks;
            GameSave.SetFlag(keeper.flag);
            yield return new WaitForSecondsRealtime(.45f);
            Expect(bosses > 0 && map.BossMarks == bosses - 1, $"a beaten guardian loses its mark, in {bossRoom.roomId} ({bosses} to {map.BossMarks})");
            GameSave.ClearFlag(keeper.flag);
            Expect(map.DoorMarks == 0, $"with every room known, no door is marked unexplored ({map.DoorMarks})");
            Say($"INFO mesh in {bossRoom.roomId}: {map.VertexCount} vertices, {map.RestMarks} rests, {map.ExitMarks} exits drawn");

            // Doors: a known room next to an unknown one shows the door.
            GameSave.Clear();
            GameSave.SetFlag(MraState.RevealFlag);
            yield return Visit(byId["A03"]);
            Expect(map.DoorMarks > 0, $"from a lone room, its unexplored doors are marked ({map.DoorMarks})");
        }

        IEnumerator Inputs(Dictionary<string, UndergroundRoom> byId)
        {
            // A batch-mode editor has no focus, and a keyboard is ignored without it.
            var background = InputSystem.settings.backgroundBehavior; var focus = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>("CaveMap keyboard");
            var pad = InputSystem.AddDevice<Gamepad>("CaveMap pad");
            pad.MakeCurrent(); keyboard.MakeCurrent();
            Expect(map.CurrentSize == CaveMinimap.Size.Small && map.IsShown, "starts small and shown");
            IEnumerator Stick() { InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.RightStick)); yield return null; yield return null; InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null; yield return null; }
            IEnumerator Tab() { InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab)); yield return null; yield return null; InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null; }
            yield return Stick();
            Expect(map.CurrentSize == CaveMinimap.Size.Medium, "right-stick click: medium");
            yield return Tab();
            Expect(map.CurrentSize == CaveMinimap.Size.Large, "Tab: large");
            yield return Stick();
            yield return null;
            Expect(map.CurrentSize == CaveMinimap.Size.Off && !map.IsShown, "again: off and hidden");
            Expect(PlayerPrefs.GetInt(CaveMinimap.SizeKey, -1) == (int)CaveMinimap.Size.Off, "the choice is remembered");
            yield return Tab();
            yield return null;
            Expect(map.CurrentSize == CaveMinimap.Size.Small && map.IsShown, "again: back to small");

            ModalUi.Open();
            yield return null; yield return null;
            Expect(!map.IsShown, "hidden while a window is open");
            yield return Tab();
            Expect(map.CurrentSize == CaveMinimap.Size.Small, "the key does nothing behind a window");
            ModalUi.Close();
            yield return null; yield return null; yield return null;
            Expect(map.IsShown, "back when the window closes");
            InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(pad);
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = focus;
        }

        IEnumerator Captures(Dictionary<string, UndergroundRoom> byId)
        {
            // Everything found, every mark live, Route A's mid point and the Heart.
            foreach (var room in byId.Keys) GameSave.SetFlag(CaveDiscovery.Flag(room));
            foreach (var (id, size) in new[] { ("A04", 0), ("HEART", 0), ("HEART", 1), ("C03", 1), ("D05", 2), ("MR01_C04", 2), ("MR05_C01", 0) })
            {
                yield return Visit(byId[id]);
                if ((int)map.CurrentSize != size) { PlayerPrefs.SetInt(CaveMinimap.SizeKey, size); typeof(CaveMinimap).GetProperty("CurrentSize").SetValue(map, (CaveMinimap.Size)size); }
                yield return null; yield return new WaitForSecondsRealtime(.45f); yield return null;
                Capture($"minimap_{id}_{(CaveMinimap.Size)size}");
            }
            // A part-explored Route A: the unexplored door marks.
            GameSave.Clear(); GameSave.SetFlag(MraState.RevealFlag);
            foreach (string id in new[] { "A01", "A02", "A03" }) yield return Visit(byId[id]);
            typeof(CaveMinimap).GetProperty("CurrentSize").SetValue(map, CaveMinimap.Size.Small);
            yield return null; yield return new WaitForSecondsRealtime(.45f); yield return null;
            Capture("minimap_A03_part_explored");
        }

        void Capture(string name) => CaptureCanvases(name);

        void CaptureCanvases(string name)
        {
            var camera = Camera.main; if (camera == null) return;
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas).ToArray();
            var modes = canvases.Select(c => (c.renderMode, c.worldCamera, c.planeDistance)).ToArray();
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = camera; c.planeDistance = 1f; }
            var target = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var read = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            read.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); read.Apply();
            File.WriteAllBytes(Path.Combine(Dir, name + ".png"), read.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null; target.Release(); Destroy(read);
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = modes[i].renderMode; canvases[i].worldCamera = modes[i].worldCamera; canvases[i].planeDistance = modes[i].planeDistance; }
        }

        // ---------------------------------------------------------------- the map against the scene

        void MapVersusScene()
        {
            Physics2D.SyncTransforms();
            var rooms = FindObjectsByType<UndergroundRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToDictionary(r => r.roomId);
            var nodes = data.nodes.Where(n => n.type == "underground").ToDictionary(n => n.id);
            var mappings = data.mappings.ToDictionary(m => m.id);
            var bad = new List<string>();

            foreach (var id in rooms.Keys.Except(nodes.Keys)) bad.Add(id + " is in the scene, not on the map");
            foreach (var id in nodes.Keys.Except(rooms.Keys)) bad.Add(id + " is on the map, not in the scene");
            foreach (var (id, room) in rooms.Select(kv => (kv.Key, kv.Value)))
            {
                if (!nodes.TryGetValue(id, out var n)) continue;
                var b = room.WorldBounds; var w = n.worldBounds;
                if (Mathf.Abs(b.min.x - w[0]) > .05f || Mathf.Abs(b.min.y - w[1]) > .05f || Mathf.Abs(b.size.x - w[2]) > .05f || Mathf.Abs(b.size.y - w[3]) > .05f)
                    bad.Add($"{id}: scene {b.min.x},{b.min.y} {b.size.x}x{b.size.y}, map {w[0]},{w[1]} {w[2]}x{w[3]}");
            }
            Limit(bad, "map and scene agree on all room bounds");

            // Doors: every link has its two ends, and neither is walled up on the map's word alone.
            bad = new List<string>();
            foreach (var group in data.caveDoors.GroupBy(d => d.link)) if (group.Count() != 2) bad.Add($"{group.Key}: {group.Count()} ends");
            foreach (var d in data.caveDoors)
            {
                if (!nodes.ContainsKey(d.room) || !nodes.ContainsKey(d.to)) { bad.Add($"{d.link}: unknown room {d.room} or {d.to}"); continue; }
                var mid = new Vector2(d.x + d.w * .5f, d.y + d.h * .5f);
                if (CaveDiscovery.Distance(CaveDiscovery.WorldRect(nodes[d.room]), mid) > .05f) bad.Add($"{d.link}: {d.room} end is off its room");
                var hits = Physics2D.OverlapCircleAll(mid, .2f).Where(c => !c.isTrigger && c.GetComponentInParent<MraGate>() == null && c.GetComponentInParent<UgArenaSeal>() == null).ToArray();
                if (hits.Length > 0) bad.Add($"{d.link}: {d.room} end at {mid.x},{mid.y} is inside {hits[0].name}");
            }
            Limit(bad, $"all {data.caveDoors.Length} door ends are open in the scene");

            // Solid pieces: each outline has its collider, and each UGP collider has its outline.
            bad = new List<string>();
            var boxes = new List<Rect>();
            foreach (var c in data.contours)
            {
                if (string.IsNullOrEmpty(c.kind) || c.kind == "gate" || c.kind == "mover" || c.kind == "pod") continue;
                var m = mappings[c.mappingId]; var pts = c.path.Select(p => CaveDiscovery.World(m, p)).ToArray();
                float x0 = pts.Min(p => p.x), x1 = pts.Max(p => p.x), y0 = pts.Min(p => p.y), y1 = pts.Max(p => p.y);
                boxes.Add(Rect.MinMaxRect(x0, y0, x1, y1));
                var centre = new Vector2((x0 + x1) * .5f, (y0 + y1) * .5f);
                if (!Physics2D.OverlapBoxAll(centre, new Vector2(Mathf.Max(x1 - x0, .1f) * .9f, Mathf.Max(y1 - y0, .1f) * .9f), 0f).Any(h => !h.isTrigger))
                    bad.Add($"{c.room}: {c.kind} outline at {centre.x:F1},{centre.y:F1} has no collider");
            }
            Limit(bad, $"all {boxes.Count} piece outlines have colliders");

            bad = new List<string>();
            int solids = 0;
            foreach (var group in FindObjectsByType<UndergroundPlatformingGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                foreach (var box in group.GetComponentsInChildren<BoxCollider2D>(true))
                {
                    if (box.isTrigger || !box.enabled || !box.gameObject.activeInHierarchy) continue;
                    if (box.GetComponentInParent<MraGate>() != null || box.GetComponentInParent<UgArenaSeal>() != null || box.GetComponentInParent<MovingPlatform>() != null || box.GetComponentInParent<MraPod>() != null) continue;
                    solids++;
                    var b = box.bounds;
                    if (!boxes.Any(r => r.Overlaps(new Rect(b.center.x - .05f, b.center.y - .05f, .1f, .1f)))) bad.Add($"{box.name} at {b.center.x:F1},{b.center.y:F1} is not on the map");
                }
            Limit(bad, $"all {solids} solid pieces in the scene are on the map");

            // The walls: outline points lie on terrain colliders.
            int samples = 0, off = 0;
            bad = new List<string>();
            foreach (var c in data.contours)
            {
                if (!c.underground || !string.IsNullOrEmpty(c.kind)) continue;
                var m = mappings[c.mappingId];
                for (int i = 0; i < c.path.Length; i += 4)
                {
                    samples++;
                    Vector2 p = CaveDiscovery.World(m, c.path[i]);
                    if (Physics2D.OverlapCircleAll(p, .6f).Any(h => !h.isTrigger)) continue;
                    off++;
                    if (bad.Count < 25) bad.Add($"{c.region} {c.room} wall point at {p.x:F1},{p.y:F1} has no collider");
                }
            }
            Expect(off * 200 <= samples, $"wall outlines lie on the terrain: {samples - off} of {samples} points (0.5% allowed)");
            foreach (var b in bad.Take(25)) Say("     " + b);

            // No room owns a wall that doesn't join up with it: nothing floats on the map.
            bad = new List<string>();
            var owners = CaveDiscovery.ContourOwners(data);
            var walls = new Dictionary<string, List<(Vector2 a, Vector2 b)>>();
            for (int c = 0; c < data.contours.Length; c++)
            {
                var contour = data.contours[c];
                if (owners[c] == null || !string.IsNullOrEmpty(contour.kind)) continue;
                var m = mappings[contour.mappingId];
                for (int i = 1; i < contour.path.Length; i++)
                {
                    if (owners[c][i] == null) continue;
                    if (!walls.TryGetValue(owners[c][i], out var list)) walls[owners[c][i]] = list = new List<(Vector2, Vector2)>();
                    list.Add((CaveDiscovery.World(m, contour.path[i - 1]), CaveDiscovery.World(m, contour.path[i])));
                }
            }
            foreach (var kv in walls)
            {
                var rect = CaveDiscovery.WorldRect(nodes[kv.Key]);
                (int, int) Key(Vector2 p) => (Mathf.RoundToInt(p.x * 10f), Mathf.RoundToInt(p.y * 10f));
                var reached = new HashSet<(int, int)>();
                var left = new List<(Vector2 a, Vector2 b)>(kv.Value);
                bool grew = true;
                while (grew)
                {
                    grew = false;
                    for (int i = left.Count - 1; i >= 0; i--)
                    {
                        var (a, b) = left[i];
                        if (CaveDiscovery.Distance(rect, a) >= .6f && CaveDiscovery.Distance(rect, b) >= .6f && !reached.Contains(Key(a)) && !reached.Contains(Key(b))) continue;
                        reached.Add(Key(a)); reached.Add(Key(b));
                        left.RemoveAt(i);
                        grew = true;
                    }
                }
                float stray = left.Sum(s => Vector2.Distance(s.a, s.b));
                if (stray > .5f) bad.Add($"{kv.Key}: {stray:F1} u of wall doesn't join the room");
            }
            Limit(bad, "no room owns a floating wall");

            // Markers sit in the rooms they're filed under.
            bad = new List<string>();
            void InRoom(string what, Vector2 at, float slack)
            {
                float best = nodes.Values.Min(n => CaveDiscovery.Distance(CaveDiscovery.WorldRect(n), at));
                if (best > slack) bad.Add($"{what} at {at.x:F1},{at.y:F1} is {best:F1} u from any room");
            }
            foreach (var x in FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)) InRoom(x.name, x.transform.position, .5f);
            foreach (var x in FindObjectsByType<MraReward>(FindObjectsInactive.Include, FindObjectsSortMode.None)) InRoom(x.name, x.transform.position, .5f);
            foreach (var x in FindObjectsByType<MraUgPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None)) InRoom(x.name, x.transform.position, 4f);
            foreach (var x in FindObjectsByType<MraGate>(FindObjectsInactive.Include, FindObjectsSortMode.None)) InRoom(x.name, x.transform.position, 30f);
            Limit(bad, "rests, rewards, exits and gates all sit in or by a room");
        }

        void Finish()
        {
            PlayerPrefs.SetInt(CaveMinimap.SizeKey, keptSize);
            Say($"== {failures} failures, {errors} runtime errors");
            File.WriteAllLines(Path.Combine(Dir, "cave_minimap_results.txt"), report);
            SessionState.EraseString(StateKey);
            EditorApplication.Exit(failures + errors > 0 ? 1 : 0);
        }
    }
}
