using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch: -executeMethod UndergroundFightAreaCheck.Run -saveFile ugp_area.json -captureDir <dir> (no -quit).
// Measures the ground each placed underground creature is fought on, from the built scene's real colliders, and writes
// <dir>/fight_areas.txt. Clear floor is flat ground with no wall, pit or thorns; rules are below in Judge.
public static class UndergroundFightAreaCheck
{
    const string Key = "UGP.AreaCheck.Dir";
    const string RoomsPath = "Tools/Design/UndergroundPlatforming_20261030/rooms.json";
    static string dir;

    public static void Run()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-saveFile") < 0) { Debug.LogError("UGP check needs -saveFile"); EditorApplication.Exit(1); return; }
        Directory.CreateDirectory(args[System.Array.IndexOf(args, "-captureDir") + 1]);
        GameSave.Clear();
        EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder + RegionalSceneCatalog.Underground + ".unity", OpenSceneMode.Single);
        SessionState.SetString(Key, args[System.Array.IndexOf(args, "-captureDir") + 1]);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod] static void Resume()
    {
        dir = SessionState.GetString(Key, "");
        if (!string.IsNullOrEmpty(dir)) EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.time < 2f) return;
        EditorApplication.update -= Tick; SessionState.EraseString(Key);
        var lines = new List<string>(); int fails = 0;
        UgArenaSeal.ForceClosed = true; foreach (var s in Object.FindObjectsByType<UgArenaSeal>(FindObjectsSortMode.None)) s.Apply(); Physics2D.SyncTransforms();
        var doors = Doors();
        var all = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Where(e => e.name.StartsWith("UGP ")).OrderBy(e => Id(e)).ThenBy(e => e.name);
        foreach (var e in all)
        {
            var room = e.GetComponentInParent<UndergroundRoom>(true);
            if (room == null) { lines.Add("NO ROOM " + e.name); fails++; continue; }
            bool flyer = e is SiltLeechEnemy || e is PumpBloaterEnemy || e is SeedSwarmEnemy || e is KiteRayEnemy || e is GaleWispEnemy || e is ThornwingEnemy || e is GustMothEnemy;
            string line = Measure(e, room, flyer, doors, out bool ok);
            if (!ok) fails++;
            lines.Add((ok ? "ok   " : "FAIL ") + line);
        }
        lines.Insert(0, $"{lines.Count} creatures, {fails} fail");
        File.WriteAllLines(Path.Combine(dir, "fight_areas.txt"), lines);
        EditorApplication.ExitPlaymode(); EditorApplication.Exit(0);
    }

    static string Id(EnemyBase e) { foreach (var p in e.name.Split(' ')) if (p.Length == 4 && p[1] == '-') return p; return e.name; }

    static Dictionary<string, List<Vector2>> Doors()
    {
        // Door centres in world units, from the rooms' own data.
        var map = new Dictionary<string, List<Vector2>>();
        var json = File.ReadAllText(RoomsPath);
        var rooms = MiniJson(json);
        foreach (var r in rooms) map[r.id] = r.doors;
        return map;
    }

    sealed class RoomDoors { public string id; public List<Vector2> doors = new List<Vector2>(); }

    // rooms.json is a dictionary keyed by room id, which JsonUtility can't read: this pulls out just the door fields.
    static List<RoomDoors> MiniJson(string json)
    {
        var result = new List<RoomDoors>();
        var room = new System.Text.RegularExpressions.Regex("\"id\":\\s*\"([^\"]+)\",\\s*\"name\"[\\s\\S]*?\"x\":\\s*(-?[\\d.]+),\\s*\"y\":\\s*(-?[\\d.]+),\\s*\"w\":\\s*(-?[\\d.]+),\\s*\"h\":\\s*(-?[\\d.]+),\\s*\"doors\":\\s*\\[([\\s\\S]*?)\\]");
        var door = new System.Text.RegularExpressions.Regex("\\{[^{}]*\\}");
        foreach (System.Text.RegularExpressions.Match m in room.Matches(json))
        {
            var r = new RoomDoors { id = m.Groups[1].Value };
            float x = F(m.Groups[2].Value), y = F(m.Groups[3].Value), w = F(m.Groups[4].Value), h = F(m.Groups[5].Value);
            foreach (System.Text.RegularExpressions.Match d in door.Matches(m.Groups[6].Value))
            {
                string side = Field(d.Value, "side");
                float width = F(Field(d.Value, "width"));
                if (side == "L" || side == "R") r.doors.Add(new Vector2(side == "L" ? x : x + w, y + F(Field(d.Value, "sill")) + width * .5f));
                else r.doors.Add(new Vector2(x + (F(Field(d.Value, "x0")) + F(Field(d.Value, "x1"))) * .5f, side == "B" ? y : y + h));
            }
            result.Add(r);
        }
        return result;
    }
    static float F(string s) => float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : 0f;
    static string Field(string json, string name)
    {
        var m = System.Text.RegularExpressions.Regex.Match(json, "\"" + name + "\":\\s*\"?([^\",}]+)\"?");
        return m.Success ? m.Groups[1].Value : "";
    }

    static string Measure(EnemyBase e, UndergroundRoom room, bool flyer, Dictionary<string, List<Vector2>> doors, out bool ok)
    {
        Vector2 at = e.transform.position; var b = room.WorldBounds;
        float door = 99f;
        if (doors.TryGetValue(room.roomId, out var list)) foreach (var d in list) door = Mathf.Min(door, Vector2.Distance(d, at));
        string id = Id(e);
        var box = e.GetComponent<Collider2D>();
        float bodyW = box != null ? box.bounds.size.x : 1f, bodyH = box != null ? box.bounds.size.y : 1f;
        var notes = new List<string>();
        string where = $"{id} {room.roomId} at ({at.x - b.min.x:0.#},{at.y - b.min.y:0.#}) of {b.size.x:0}x{b.size.y:0}";
        if (flyer)
        {
            // The best thorn-free ground to fight from, within double-jump-and-swing reach of its height.
            float best = 0f, under = 99f;
            for (float dx = -6f; dx <= 6f; dx += 1f)
            {
                var g = Below(new Vector2(at.x + dx, at.y), e is SiltLeechEnemy ? 14f : 9f);
                if (g == null) continue;
                if (Thorny(g.Value)) continue;
                Span(g.Value, b, out float l, out float r, out _, out _);
                if (l + r > best) { best = l + r; under = at.y - g.Value.y; }
            }
            ok = best >= 5f && door >= 5f;
            if (best < 5f) notes.Add("no safe standing ground in reach");
            if (door < 5f) notes.Add("too close to a door");
            return $"{where} flyer: best ground {best:0.#} wide, {under:0.#} below it; door {door:0.#}" + Tail(notes);
        }
        var ground = Below(at + Vector2.up * .5f, 8f);
        if (ground == null) { ok = false; return $"{where}: NO GROUND"; }
        Span(ground.Value, b, out float left, out float right, out string stopL, out string stopR);
        // Headroom up to rock: one-way decks are jumped through.
        float head = 60f;
        foreach (var h in Physics2D.RaycastAll(ground.Value + Vector2.up * .2f, Vector2.up, 60f, GroundLayer.Mask))
            if (!h.collider.TryGetComponent(out PlatformEffector2D oneWay) || !oneWay.useOneWay) { head = h.distance; break; }
        bool mini = id[0] == 'M', boss = id[0] == 'G';
        float need = boss ? 26f : mini ? 18f : e is NewtEnemy ? 4f : 7f, needDoor = boss || mini ? bodyW * .5f + 5f : 5f, needHead = boss || mini ? bodyH + 2f : bodyH + 1.5f;
        if (left + right < need) notes.Add($"needs {need:0} of clear floor");
        if (door < needDoor) notes.Add($"needs {needDoor:0.#} from a door");
        if (head < needHead) notes.Add($"needs {needHead:0.#} headroom");
        if (!boss && !mini && (left < 2f || right < 2f) && !(e is NewtEnemy) && !(e is GrubEnemy)) { /* a wall at its back is fine: it's fought from the front */ }
        ok = notes.Count == 0;
        return $"{where}: clear floor {left:0.#} L ({stopL}) + {right:0.#} R ({stopR}) = {left + right:0.#}; headroom {head:0.#}; body {bodyW:0.#}x{bodyH:0.#}; door {door:0.#}" + Tail(notes);
    }

    static string Tail(List<string> notes) => notes.Count > 0 ? " -- " + string.Join("; ", notes) : "";

    static Vector2? Below(Vector2 from, float reach)
    {
        var hit = Physics2D.Raycast(from, Vector2.down, reach, GroundLayer.Mask);
        return hit ? hit.point : (Vector2?)null;
    }

    static bool Thorny(Vector2 p)
    {
        foreach (var c in Physics2D.OverlapCircleAll(p + Vector2.up * .2f, .3f)) if (c.GetComponent<ThornHazard>() != null) return true;
        return false;
    }

    static void Span(Vector2 ground, Bounds room, out float left, out float right, out string stopL, out string stopR)
    {
        left = Walk(ground, -1f, room, out stopL);
        right = Walk(ground, 1f, room, out stopR);
    }

    static float Walk(Vector2 from, float dir, Bounds room, out string stop)
    {
        float y = from.y; const float step = .25f;
        for (float d = step; d < 40f; d += step)
        {
            float x = from.x + dir * d;
            if (x < room.min.x || x > room.max.x) { stop = "room edge"; return d - step; }
            if (Physics2D.Raycast(new Vector2(x - dir * step, y + .7f), new Vector2(dir, 0f), step, GroundLayer.Mask)) { stop = "wall"; return d - step; }
            var g = Physics2D.Raycast(new Vector2(x, y + .7f), Vector2.down, 1.6f, GroundLayer.Mask);
            if (!g || Mathf.Abs(g.point.y - y) > .6f) { stop = "drop"; return d - step; }
            if (Thorny(g.point)) { stop = "thorns"; return d - step; }
            y = g.point.y;
        }
        stop = "far"; return 40f;
    }
}
