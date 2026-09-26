using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Builds Assets/Art/Codex/Terrain/<AREA>_TerrainKit.asset for A1-A4 from the area's kit sprites
// and its measured landmarks (<AREA>_TerrainKit.landmarks.json, written by
// Tools/ArtImport/measure_terrain_kit.py). A0's kit is built by A0TestRoomBuilder with its
// hand-set landmarks.
public static class TerrainKitBuilder
{
    const string Folder = "Assets/Art/Codex/Terrain/";

    [System.Serializable]
    sealed class Landmarks
    {
        public float groundTopWalkLine, wallSideFace, ceilingUnderside, oneWayWalkLine;
        public float[] cornerOuterTopRightLedge, cornerOuterBottomRightEdge, oneWaySpan;
        public float[] floatingSmallLine, floatingMediumLine, floatingLargeLine;
    }

    public static TerrainKit Build(string area)
    {
        string path = Folder + area + "_TerrainKit.asset";
        var kit = AssetDatabase.LoadAssetAtPath<TerrainKit>(path);
        if (kit == null) { kit = ScriptableObject.CreateInstance<TerrainKit>(); AssetDatabase.CreateAsset(kit, path); }
        Sprite Load(string piece) => AssetDatabase.LoadAssetAtPath<Sprite>(Folder + area + "_" + piece + ".png")
            ?? throw new FileNotFoundException("Missing terrain sprite; import accepted Codex art first.", area + "_" + piece);
        kit.groundTop = Load("Ground_Top"); kit.groundFill = Load("Ground_Fill"); kit.wallSide = Load("Wall_Side");
        kit.ceilingUnder = Load("Ceiling_Under"); kit.wallClimbable = Load("Wall_Climbable"); kit.wallSlippery = Load("Wall_Slippery");
        kit.cornerOuterTopRight = Load("Corner_Outer_TopRight"); kit.cornerOuterBottomRight = Load("Corner_Outer_BottomRight");
        kit.slope30 = Load("Slope_30"); kit.platformOneWay = Load("Platform_OneWay");
        kit.floatingSmall = Load("Platform_Floating_S"); kit.floatingMedium = Load("Platform_Floating_M"); kit.floatingLarge = Load("Platform_Floating_L");

        string json = Folder + area + "_TerrainKit.landmarks.json";
        if (!File.Exists(json)) throw new FileNotFoundException($"Run: python Tools/ArtImport/measure_terrain_kit.py {area}", json);
        // JsonUtility can't read nested arrays; the slope outline is read separately.
        var m = JsonUtility.FromJson<Landmarks>(File.ReadAllText(json));
        kit.groundTopWalkLine = m.groundTopWalkLine; kit.wallSideFace = m.wallSideFace; kit.ceilingUnderside = m.ceilingUnderside;
        kit.cornerOuterTopRightLedge = V2(m.cornerOuterTopRightLedge); kit.cornerOuterBottomRightEdge = V2(m.cornerOuterBottomRightEdge);
        kit.oneWayWalkLine = m.oneWayWalkLine; kit.oneWaySpan = V2(m.oneWaySpan);
        kit.floatingSmallLine = V3(m.floatingSmallLine); kit.floatingMediumLine = V3(m.floatingMediumLine); kit.floatingLargeLine = V3(m.floatingLargeLine);
        kit.slopeSurface = SlopeOutline(File.ReadAllText(json));
        EditorUtility.SetDirty(kit);
        return kit;
    }

    static Vector2 V2(float[] a) => new Vector2(a[0], a[1]);
    static Vector3 V3(float[] a) => new Vector3(a[0], a[1], a[2]);

    // "slopeSurface": [[x, y], ...]
    static Vector2[] SlopeOutline(string json)
    {
        int start = json.IndexOf('[', json.IndexOf("\"slopeSurface\"")), depth = 0, end = start;
        for (; end < json.Length; end++) { if (json[end] == '[') depth++; else if (json[end] == ']' && --depth == 0) break; }
        return json.Substring(start + 1, end - start - 1).Split(']')
            .Select(p => p.Trim(' ', ',', '[', '\n', '\r'))
            .Where(p => p.Length > 0)
            .Select(p => { var xy = p.Split(','); return new Vector2(float.Parse(xy[0].Trim()), float.Parse(xy[1].Trim())); })
            .ToArray();
    }
}
