using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mra;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Shared pieces for the Mountain Relief Atlas builder: each region's accepted art family (the
// fallbacks the design names: A0 Cradle, A1 Causeway, A5 Terraces, A2 Ribwood, A3 Heights, A6 Summit,
// A4 Descent, the town kit for Qvale, and the newer Body/Causeway/Terraces surfaces where listed),
// art lookup by accepted path, the scene scaffold (light, Qori, camera, managers), and geometry.
public static partial class MraWorldBuilder
{
    public const string Folder = "Assets/MountainReliefAtlas/";
    public const string SceneFolder = Folder + "Scenes/";
    public const string GeneratedRoot = "Generated (MRA builder)";
    const string ArtRoot = "Assets/Art/Codex/";

    public sealed class Family
    {
        public string id, top, fill, face, caveTop, caveFill, sky, far, mid, near, decor, shrine, ring, thorns, entrance;
        /// <summary>The painted cliff kit's name prefix (Batch 13), e.g. Terrain/Body/Body; null for the legacy kits.</summary>
        public string cliffKit;
        public Color skyColour, midBand, nearBand;
    }

    // Accepted art per region, by accepted path (Tools/ArtImport/codex_v2_accepted.json).
    public static Family FamilyOf(string region)
    {
        Family F(string id, string surface, string legacy, string sky, string bg) => new Family
        {
            id = id, cliffKit = surface,
            top = surface != null ? surface + "_Top_Strip" : "Terrain/" + legacy + "_Ground_Top",
            fill = surface != null ? surface + "_Fill" : "Terrain/" + legacy + "_Ground_Fill",
            face = surface != null ? surface + "_Cliff_Face" : "Terrain/" + legacy + "_Wall_Side",
            caveTop = "Terrain/Body/Cave_Edge_Strip", caveFill = "Terrain/Body/Cave_Fill",
            sky = "Backgrounds/" + sky, mid = "Backgrounds/BG_" + bg + "_Mid", near = "Backgrounds/BG_" + bg + "_Near",
            decor = "Decor/Decor_" + legacy + "_Sheet",
            // A0 has no accepted checkpoint shrine (its runtime lantern floated): the Cradle uses A1's.
            shrine = "Props/Checkpoint_Shrine_" + (legacy == "A0" ? "A1" : legacy),
            ring = "Props/Anchor_Ring_" + (legacy == "A0" ? "A1" : legacy),
            thorns = "Hazards/Hazard_Thorns_Floor" + (legacy == "A0" ? "" : "_" + legacy),
            // The legacy kits' own cave mouths (Batch 13); the limestone WallCave elsewhere.
            entrance = surface == null && (legacy == "A2" || legacy == "A3" || legacy == "A4" || legacy == "A6") ? "Props/Prop_Chamber_Entrance_" + legacy : "Props/Prop_Chamber_Entrance_WallCave",
            skyColour = new Color(.86f, .84f, .78f), midBand = new Color(.70f, .72f, .70f), nearBand = new Color(.62f, .62f, .55f),
        };
        switch (region)
        {
            case "MR01": { var f = F("A0", "Terrain/Body/Body", "A0", "BG_Sky_A0", "A0"); return f; }
            case "MR02":
            {
                var f = F("A1", "Terrain/Body/Causeway/Causeway", "A1", "BG_Sky_A1", "A1");
                f.caveTop = "Terrain/Body/Causeway/Causeway_Cave_Edge_Strip"; f.caveFill = "Terrain/Body/Causeway/Causeway_Cave_Fill";
                f.decor = "Decor/Decor_Causeway_Sheet"; f.entrance = "Props/Prop_Causeway_Chamber_Entrance";
                return f;
            }
            case "MR03":
            {
                var f = F("A5", "Terrain/Body/Terraces/Terraces", "A5", "BG_Sky_Terraces", "A5");
                f.decor = "Decor/Decor_Terraces_Sheet";
                return f;
            }
            case "MR04":
            {
                // The town: the Causeway's laid-stone road surface (not the homes' own pale limestone, so the
                // street reads as a street in front of them), Qvale's own backgrounds and decor.
                var f = F("Qvale", "Terrain/Body/Causeway/Causeway", "A5", "BG_Sky_A5", "Qvale");
                f.caveTop = "Terrain/Body/Causeway/Causeway_Cave_Edge_Strip"; f.caveFill = "Terrain/Body/Causeway/Causeway_Cave_Fill";
                f.far = "Backgrounds/BG_Qvale_Far"; f.decor = "Decor/Decor_Qvale_Sheet"; f.shrine = "Props/Checkpoint_Shrine_A5";
                return f;
            }
            case "MR05": return F("A2", null, "A2", "BG_Sky_A2", "A2");
            case "MR06": return F("A3", null, "A3", "BG_Sky_A3", "A3");
            case "MR07": return F("A6", null, "A6", "BG_Sky_Terraces", "A6");   // A6 has no sky of its own
            default: return F("A4", null, "A4", "BG_Sky_A4", "A4");
        }
    }

    public static Sprite Art(string path) =>
        AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + path + ".png") ?? throw new FileNotFoundException("Accepted art missing: " + path);
    public static Sprite ArtOrNull(string path) => path == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + path + ".png");
    public static Sprite[] Slices(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(ArtRoot + path + ".png").OfType<Sprite>().OrderBy(s => s.name).ToArray();

    public static SpriteRenderer Image(Transform parent, string name, Sprite sprite, Vector2 at, int order, float scale = 1f)
    {
        var r = new GameObject(name).AddComponent<SpriteRenderer>();
        r.transform.SetParent(parent, false); r.transform.position = at; r.transform.localScale = new Vector3(scale, scale, 1f);
        r.sprite = sprite; r.sortingOrder = order;
        return r;
    }

    public static Transform Group(Transform parent, string name)
    {
        var t = new GameObject(name).transform; t.SetParent(parent, false); return t;
    }

    // ---------------------------------------------------------------- scenes

    // Opens the scene if it exists (keeping anything authored by hand), else makes a new one, and
    // replaces only its generated root.
    static Scene Begin(string path, out Transform root)
    {
        Scene scene = File.Exists(path) ? EditorSceneManager.OpenScene(path, OpenSceneMode.Single) : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects()) if (go.name == GeneratedRoot) Object.DestroyImmediate(go);
        root = new GameObject(GeneratedRoot).transform;
        return scene;
    }

    static GameObject Root(Scene scene, string name) => scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);

    // The light, Qori (the live Player prefab, unchanged), the camera following him, and the game
    // manager (pause menu, the area for the save, the route session). Kept if already there.
    static Camera Scaffold(Scene scene, Vector2 start, string displayName, string levelId, string regionId, string chamberId, Color sky)
    {
        if (Root(scene, "Global Light 2D") == null) new GameObject("Global Light 2D").AddComponent<Light2D>().lightType = Light2D.LightType.Global;
        var player = Root(scene, "Player") ?? (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"));
        player.transform.position = start;

        var camObj = Root(scene, "Main Camera");
        Camera camera;
        if (camObj == null)
        {
            camera = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camera.gameObject.AddComponent<CameraFollow>();
            camera.gameObject.AddComponent<AudioListener>();
        }
        else camera = camObj.GetComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = sky;
        camera.transform.position = new Vector3(start.x, start.y + 1f, -10f);
        var follow = new SerializedObject(camera.GetComponent<CameraFollow>());
        follow.FindProperty("target").objectReferenceValue = player.transform;
        follow.ApplyModifiedPropertiesWithoutUndo();

        var manager = Root(scene, "GameManager") ?? new GameObject("GameManager");
        if (!manager.TryGetComponent(out GamePauseMenu _)) manager.AddComponent<GamePauseMenu>();
        if (!manager.TryGetComponent(out GameArea area)) area = manager.AddComponent<GameArea>();
        area.displayName = displayName; area.levelId = levelId; area.region = regionId; area.legacyChart = false;
        if (!manager.TryGetComponent(out MraSession session)) session = manager.AddComponent<MraSession>();
        session.regionId = regionId; session.chamberId = chamberId ?? "";
        return camera;
    }

    static void Save(Scene scene, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        EditorSceneManager.SaveScene(scene, path);
    }

    // ---------------------------------------------------------------- geometry

    // Stone steps from a to b: a tread, then a riser at its end, each riser at most maxRise.
    public static List<Vector2> Steps(Vector2 a, Vector2 b, float maxRise = 1.2f)
    {
        var pts = new List<Vector2> { a };
        float dy = b.y - a.y;
        if (Mathf.Abs(dy) < 1e-3f) { pts.Add(b); return pts; }
        int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(dy) / maxRise - 1e-4f));
        float run = (b.x - a.x) / n, rise = dy / n, y = a.y;
        for (int i = 1; i <= n; i++)
        {
            float x = a.x + run * i;
            pts.Add(new Vector2(x, y)); y += rise; pts.Add(new Vector2(x, y));
        }
        return pts;
    }

    // The walk line's height at x (from the left side at a riser), or NaN outside the line.
    public static float HeightAt(List<Vector2> line, float x, bool fromLeft = true)
    {
        for (int i = 0; i < line.Count - 1; i++)
        {
            Vector2 a = line[i], b = line[i + 1];
            if (b.x - a.x < 1e-5f) continue;
            if (fromLeft ? (x > a.x - 1e-5f && x <= b.x + 1e-5f) : (x >= a.x - 1e-5f && x < b.x + 1e-5f))
                return Mathf.Lerp(a.y, b.y, (x - a.x) / (b.x - a.x));
        }
        return float.NaN;
    }

    // Flattens [x0, x1] of the line to y (a landing pad, an encounter shelf), with risers back to
    // the line's own height at each end.
    public static List<Vector2> Flatten(List<Vector2> line, float x0, float x1, float y)
    {
        if (line.Count < 2 || x1 <= line[0].x || x0 >= line[line.Count - 1].x) return line;
        x0 = Mathf.Max(x0, line[0].x); x1 = Mathf.Min(x1, line[line.Count - 1].x);
        float h0 = HeightAt(line, x0, true), h1 = HeightAt(line, x1, false);
        // A step under 0.35 u becomes a short ramp instead of a riser, so the moss lip doesn't jump.
        const float Ramp = .35f, RampRun = .6f;
        bool ramp0 = !float.IsNaN(h0) && Mathf.Abs(h0 - y) > 1e-3f && Mathf.Abs(h0 - y) < Ramp, ramp1 = !float.IsNaN(h1) && Mathf.Abs(h1 - y) > 1e-3f && Mathf.Abs(h1 - y) < Ramp;
        float cut0 = ramp0 ? x0 - RampRun : x0, cut1 = ramp1 ? x1 + RampRun : x1;
        // The ramp's foot must stay on the same tread (within the small step); otherwise keep a riser.
        if (ramp0) { float h = HeightAt(line, cut0, true); if (float.IsNaN(h) || Mathf.Abs(h - h0) > 1e-3f) { cut0 = x0; ramp0 = false; } }
        if (ramp1) { float h = HeightAt(line, cut1, false); if (float.IsNaN(h) || Mathf.Abs(h - h1) > 1e-3f) { cut1 = x1; ramp1 = false; } }
        var outLine = new List<Vector2>();
        foreach (var p in line) if (p.x < cut0 - 1e-4f) outLine.Add(p);
        if (ramp0) outLine.Add(new Vector2(cut0, h0));
        else if (!float.IsNaN(h0) && Mathf.Abs(h0 - y) > 1e-3f) outLine.Add(new Vector2(x0, h0));
        outLine.Add(new Vector2(x0, y)); outLine.Add(new Vector2(x1, y));
        if (ramp1) outLine.Add(new Vector2(cut1, h1));
        else if (!float.IsNaN(h1) && Mathf.Abs(h1 - y) > 1e-3f) outLine.Add(new Vector2(x1, h1));
        foreach (var p in line) if (p.x > cut1 + 1e-4f) outLine.Add(p);
        return Clean(outLine);
    }

    // Splits any riser taller than `maxRise` (a pad cut through a stair can merge two) into steps,
    // built back into the lower side's tread: `run` u per step, never past the previous point.
    public static List<Vector2> LimitRisers(List<Vector2> line, float maxRise = 1.2f, float run = .9f)
    {
        var pts = new List<Vector2>(line);
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector2 a = pts[i], b = pts[i + 1];
            float dy = b.y - a.y;
            if (Mathf.Abs(b.x - a.x) > 1e-4f || Mathf.Abs(dy) <= maxRise + 1e-3f) continue;
            int n = Mathf.CeilToInt(Mathf.Abs(dy) / maxRise - 1e-4f);
            float rise = dy / n;
            bool up = dy > 0f;
            // The lower tread: before the riser going up, after it going down.
            float limit = up ? (i > 0 ? pts[i - 1].x : a.x - run * n) : (i + 2 < pts.Count ? pts[i + 2].x : a.x + run * n);
            float step = Mathf.Min(run, Mathf.Abs(a.x - limit) / n);
            var stairs = new List<Vector2>();
            if (up)
            {
                float y = a.y;
                for (int k = n - 1; k >= 0; k--) { float x = a.x - step * k; stairs.Add(new Vector2(x, y)); y += rise; stairs.Add(new Vector2(x, y)); }
            }
            else
            {
                float y = a.y;
                for (int k = 0; k < n; k++) { float x = a.x + step * k; stairs.Add(new Vector2(x, y)); y += rise; stairs.Add(new Vector2(x, y)); }
            }
            pts.RemoveRange(i, 2);
            pts.InsertRange(i, stairs);
            i += stairs.Count - 2;
        }
        return pts;
    }

    // Drops repeated points and middle points of straight horizontal or vertical runs.
    public static List<Vector2> Clean(List<Vector2> line)
    {
        var pts = new List<Vector2>();
        foreach (var p in line) if (pts.Count == 0 || (pts[pts.Count - 1] - p).sqrMagnitude > 1e-6f) pts.Add(p);
        for (int i = pts.Count - 2; i >= 1; i--)
        {
            Vector2 a = pts[i - 1], b = pts[i], c = pts[i + 1];
            if (Mathf.Abs((b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x)) < 1e-5f && Vector2.Dot(b - a, c - b) > 0f) pts.RemoveAt(i);
        }
        return pts;
    }

    public static MraTerrain Terrain(Transform parent, string name, List<Vector2> surface, float bottom, Family f, bool cave = false, bool clingable = true, bool solid = true, Color? tint = null, int sortingOffset = 0)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var t = obj.AddComponent<MraTerrain>();
        t.enabled = false;   // configure before it builds
        t.surface = surface.ToArray(); t.bottom = bottom;
        t.fill = Art(cave ? f.caveFill : f.fill); t.top = Art(cave ? f.caveTop : f.top);
        // Legacy kit strips (Terrain/A*_Ground_Top) are opaque rock all the way down: only their lip.
        t.stripDepth = !cave && f.top.StartsWith("Terrain/A") ? .5f : 1.4f;
        // Riser faces: the family's face art (Cliff_Face, opaque; legacy Wall_Side strips have a
        // ragged painted edge at about u 0.84, laid on the riser line). Cave pieces keep plain fill.
        t.face = cave ? null : ArtOrNull(f.face);
        t.faceEdgeU = f.face.EndsWith("_Wall_Side") ? .84f : 0f;
        t.contourReach = .4f;   // the irregular rock edge on opaque faces (visual only)
        if (!cave && f.cliffKit != null)
        {
            // The painted cliff kit: corners, sides and feet on every wall (Batch 13, Review 24).
            t.cliffSideL = ArtOrNull(f.cliffKit + "_Cliff_Side_L"); t.cliffSideR = ArtOrNull(f.cliffKit + "_Cliff_Side_R");
            t.cornerTopL = ArtOrNull(f.cliffKit + "_Corner_Top_L"); t.cornerTopR = ArtOrNull(f.cliffKit + "_Corner_Top_R");
            t.cornerFootL = ArtOrNull(f.cliffKit + "_Corner_Foot_L"); t.cornerFootR = ArtOrNull(f.cliffKit + "_Corner_Foot_R");
        }
        t.walkLinePx = 96f; t.solid = solid; t.clingable = clingable; t.tint = tint ?? Color.white; t.sortingOffset = sortingOffset;
        t.enabled = true;
        return t;
    }

    // A plain solid box (a lintel, a chamber wall), drawn with the family's fill.
    public static MraTerrain Box(Transform parent, string name, Rect r, Family f, bool cave, bool clingable, int sortingOffset = 2)
    {
        var t = Terrain(parent, name, new List<Vector2> { new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax) }, r.yMin, f, cave, clingable, true, null, sortingOffset);
        t.enabled = false; t.top = null;
        t.cliffSideL = t.cliffSideR = t.cornerTopL = t.cornerTopR = t.cornerFootL = t.cornerFootR = null;   // a lintel or a wall block: plain rock
        t.enabled = true;
        return t;
    }

    public static BoxCollider2D Trigger(GameObject obj, Vector2 size, Vector2 offset)
    {
        if (!obj.TryGetComponent(out BoxCollider2D box)) box = obj.AddComponent<BoxCollider2D>();
        box.isTrigger = true; box.size = size; box.offset = offset;
        return box;
    }

    // ---------------------------------------------------------------- townspeople

    [System.Serializable] sealed class FolkPart { public string part; public Vector2 at; public float angle; public int order; }
    [System.Serializable] sealed class Folk { public string who; public float ppu; public Vector2 ground; public FolkPart[] parts; }
    [System.Serializable] sealed class FolkFile { public Folk[] folk; }
    static FolkFile folkFile;

    // A townsperson in the accepted rig's rest pose, standing with its feet at `feet`, facing right.
    public static Transform Townsfolk(Transform parent, string who, Vector2 feet, int order = 4)
    {
        folkFile ??= JsonUtility.FromJson<FolkFile>(File.ReadAllText(Folder + "Editor/townsfolk_rigs.json"));
        var rig = folkFile.folk.First(f => f.who == who);
        var root = new GameObject("Figure " + who).transform;
        root.SetParent(parent, false); root.position = feet;
        foreach (var p in rig.parts)
        {
            var sprite = Art("Characters/Town/Town_" + who + "_" + p.part);
            Vector2 local = new Vector2(p.at.x - rig.ground.x, rig.ground.y - p.at.y) / rig.ppu;
            var r = Image(root, p.part, sprite, feet + local, order + p.order);
            r.transform.localRotation = Quaternion.Euler(0f, 0f, -p.angle);   // positive turns the hanging bone left
        }
        return root;
    }
}
