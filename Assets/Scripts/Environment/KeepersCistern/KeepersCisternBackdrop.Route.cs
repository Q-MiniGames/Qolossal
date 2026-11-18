using System.Linq;
using UnityEngine;

public sealed partial class KeepersCisternBackdrop
{
    public static readonly string[] RoomIds = { "A01", "A02", "A03", "A04", "A05", "A06", "A07", "A08" };
    static Rect[] MainRooms => new[] { A01, A02, A03, A04, A05, A06, A07, A08 };
    struct Passage { public string name; public Rect rect; public Passage(string n, Rect r) { name = n; rect = r; } }
    // Derived from the actual connection polylines and the 3/4 u door widths; never the A09/A10/Heart interiors.
    static readonly Passage[] RoutePassages = {
        new("P_A01_A02", new Rect(1516, 178, 12, 4)), new("P_A02_A03", new Rect(1532, 114, 4, 12)),
        new("T1", new Rect(1538, 106, 14, 4)), new("T2", new Rect(1616, 106, 12, 4)),
        new("P_A04_A09", new Rect(1566.5f, 68, 3, 16)),
        new("P_A05_A06_V", new Rect(1634, 140, 4, 24)), new("P_A05_A06_H", new Rect(1614, 160, 20, 4)),
        new("P_A06_A02", new Rect(1544, 172, 10, 4)), new("P_A06_A07", new Rect(1614, 176, 14, 4)),
        new("P_A07_A08", new Rect(1670, 176, 14, 4)), new("P_A08_A10", new Rect(1702.5f, 138, 3, 30)),
        new("P_A08_HEART", new Rect(1736, 182, 18, 4)) };

    bool Have(string n) { if (Paint(n)) return true; missingPaint.Add(n); return false; }

    void Unique(string n, float x, float y, Rect room, int order = -64, bool middle = false)
    {
        if (!Have(n)) return;
        var t = Paint(n);
        Quad(n, new Rect(x - t.width / 432f, y - t.height / 432f, t.width / 216f, t.height / 216f), room, order, !middle, middle ? new Vector2(.88f, .94f) : Vector2.one, room.center);
    }

    void Ceiling(Rect room, float[] centres) { foreach (float x in centres) Put("CK_Vault_Span", x, room.yMax - 1, room); }

    void CeilingRoots(Rect room, float x, int variant = 1)
    {
        Put("CK_Roots_Curtain_0" + variant, x, room.yMax, room);
        Quad("FG_Roots_0" + (variant == 2 ? 2 : 1), new Rect(x - 2, room.yMax - 1.6f, 4, 6), room, -25, true, new Vector2(1.1f, 1.05f), room.center);
    }

    void NativeTiledSet(string prefix, float x, float y, float tileW, float tileH, int rows, int cols, Rect room, bool far = false)
    {
        var names = Enumerable.Range(0, rows).SelectMany(r => Enumerable.Range(0, cols).Select(c => $"{prefix}_r{r}c{c}")).ToArray();
        bool ready = true; foreach (string n in names) if (!Have(n)) ready = false;
        if (!ready) return;
        for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++)
                Quad($"{prefix}_r{r}c{c}", new Rect(x + c * tileW, y + r * tileH, tileW, tileH), room, far ? -80 : -64, !far, far ? new Vector2(.5f, .6f) : Vector2.one, room.center);
    }

    void PassageWall(Passage p)
    {
        Rect r = p.rect;
        Quad("Backing", r, r, -85, false, Vector2.one, r.center, true); Wall(r, r);
        int variant = 0;
        if (r.width > r.height)
        {
            float x = r.xMin + .65f;
            while (x < r.xMax) { Put("CK_Rib_Tunnel_0" + (++variant % 3 + 1), x, r.center.y, r); x += new[] { 2.8f, 3.6f, 3.1f }[variant % 3]; }
            Put("CK_Niche_Lantern_02", r.center.x + .6f, r.center.y + .7f, r);
            Quad("CK_Pipe_H", new Rect(r.x, r.yMax - .35f, r.width, 1), r, -64, true, Vector2.one, r.center);
        }
        else
        {
            float y = r.yMin + .7f;
            while (y < r.yMax) { string n = "CK_Rib_Shaft_0" + (++variant % 2 + 1); if (Have(n)) Put(n, r.center.x, y, r); y += new[] { 3.1f, 3.8f, 2.9f }[variant % 3]; }
            Quad("CK_Pipe_V", new Rect(r.xMax - .65f, r.yMin, 1, r.height), r, -64, true, Vector2.one, r.center);
        }
    }

    void BuildRestOfRoute()
    {
        foreach (var r in new[] { A01, A02, A06, A07, A08 }) Quad("Backing", r, r, -85, false, Vector2.one, r.center, true);
        // A01 arrival: light-well and inlet attached to the wall; only the small winch has middle motion.
        Wall(A01, A01); Ceiling(A01, new[] { 1488f, 1504f, 1517f });
        foreach (float x in new[] { 1488f, 1504f }) Put("CK_Vault_Span", x, 178, A01);
        foreach (float x in new[] { 1480.5f, 1515.5f }) Pier(x, 194, A01, "CK_Column_01");
        Unique("SET_A01_InletGate", 1495, 176.5f, A01);
        Unique("MID_A01_SluiceWinch", 1508, 171.8f, A01, -62, true); Put("CK_Mound_01", 1508, 170, A01, -53);
        Put("CK_Niche_Lantern_01", 1484, 174, A01); Put("CK_Niche_Lantern_02", 1512.4f, 181, A01);
        Unique("CK_Light_Beam", 1495, 177, A01, -58);
        if (Last?.t.name == "CK_Light_Beam") { var m = Mat(Last); m.SetFloat("_Beam", 1); m.SetFloat("_Light", 0); m.SetInt("_DstBlend", 1); }
        CeilingRoots(A01, 1486, 1);
        // A02 drain shaft: one spout, one plunge pool, one suspended winch.
        Wall(A02, A02);
        foreach (float x in new[] { 1528.5f, 1543.5f }) Pier(x, 188, A02, "CK_Column_02");
        foreach (float y in new[] { 176f, 184f }) Put("CK_Vault_Span", 1536, y, A02);
        Put("CK_Recess_Arch", 1535.5f, 178.5f, A02, -64);
        Unique("SET_A02_AqueductSpout", 1536, 180, A02, -63); Unique("SET_A02_PlungePool", 1536, 127.5f, A02, -61);
        Unique("MID_A02_DrainWinch", 1532.5f, 152.6f, A02, -62, true);
        Quad("CK_Chain_V", new Rect(1532.8f, 152.6f, .5f, 28), A02, -63, true, Vector2.one, A02.center);
        Quad("CK_Water_Sheet_V", new Rect(1530, 128, 2, 47), A02, -62, true, Vector2.one, A02.center);
        foreach (var p in new[] { new Vector2(1541.7f, 175), new Vector2(1541.7f, 151), new Vector2(1541.7f, 135) }) Put("CK_Niche_Lantern_02", p.x, p.y, A02);
        CeilingRoots(A02, 1538, 1); Put("CK_Roots_Curtain_02", 1542, 139, A02);
        // Upper spillway.
        Unique("SET_A05_SpillHead", 1636, 135, A05, -63); Put("CK_Vault_Span", 1636, 138, A05); Put("CK_Vault_Span", 1636, 126, A05);
        Put("CK_Niche_Lantern_02", 1640.7f, 124, A05); Put("CK_Roots_Curtain_02", 1631, 136, A05);
        // A06 pump gallery.
        NativeTiledSet("FAR_A06_EngineDepths", 1584 - 20.5f, 170 - 13, 41f / 3, 13, 2, 3, A06, true);
        Wall(new Rect(1554, 154, 4, 32), A06); Wall(new Rect(1610, 154, 4, 32), A06); Wall(new Rect(1554, 182, 60, 4), A06);
        foreach (var c in new[] { new Vector2(1565, 179), new Vector2(1583, 184), new Vector2(1603.2f, 180) }) { Put("CK_Vault_Span", c.x, c.y, A06); if (c.y + 2 < 186) Wall(new Rect(c.x - 8, c.y + 2, 16, 186 - c.y - 2), A06); }
        foreach (float x in new[] { 1557f, 1573.2f, 1591f, 1611f }) Pier(x, 188, A06, "CK_Column_02");
        Unique("SET_A06_BeamEngine_01", 1566, 161.0111f, A06, -64); Unique("SET_A06_BeamEngine_02", 1603, 158.8861f, A06, -64);
        Unique("MID_A06_Flywheel", 1583, 155.5f, A06, -72, true); Put("CK_Mound_02", 1583, 154, A06, -53);
        Quad("CK_Basin_Submerged", new Rect(1554, 152.6f, 60, 2), A06, -68, true, Vector2.one, A06.center);
        Quad("CK_Basin_Coping", new Rect(1554, 154.6f, 60, .5f), A06, -67, true, Vector2.one, A06.center);
        if (Last?.t.name == "CK_Basin_Coping") { Mat(Last).SetFloat("_BasinWater", 1); Mat(Last).SetFloat("_WaterY", 154.6f); }
        foreach (var p in new[] { new Vector2(1556.5f, 163), new Vector2(1573.2f, 176), new Vector2(1611.5f, 178) }) Put("CK_Niche_Lantern_01", p.x, p.y, A06);
        CeilingRoots(A06, 1569, 2); CeilingRoots(A06, 1594, 3);
        // A07 refuge: everything anchored to terrain.
        Wall(A07, A07); Ceiling(A07, new[] { 1636f, 1652f, 1668f });
        foreach (float x in new[] { 1636f, 1652f, 1668f }) Put("CK_Vault_Span", x, 178.5f, A07);
        foreach (float x in new[] { 1628.4f, 1656.5f, 1669.5f }) Pier(x, 186, A07, "CK_Column_01");
        if (Have("SET_A07_KeeperQuarters_L") & Have("SET_A07_KeeperQuarters_R")) { Put("SET_A07_KeeperQuarters_L", 1641, 174.55f, A07, -64); Put("SET_A07_KeeperQuarters_R", 1651, 174.55f, A07, -64); }
        foreach (var p in new[] { new Vector2(1630, 178), new Vector2(1667, 177) }) Put("CK_Niche_Lantern_03", p.x, p.y, A07);
        CeilingRoots(A07, 1658, 2);
        // A08 guardian arena: architecture above the quiet fight band, gate closed.
        Wall(A08, A08); Ceiling(A08, new[] { 1692f, 1708f, 1724f, 1738f });
        foreach (float x in new[] { 1692f, 1728f }) Put("CK_Vault_Span", x, 177, A08);
        foreach (float x in new[] { 1684.5f, 1735.5f }) Pier(x, 198, A08, "CK_Column_02");
        NativeTiledSet("SET_A08_GreatSluice", 1702, 167.63f, 8, 10, 2, 2, A08);
        CeilingRoots(A08, 1697, 3); CeilingRoots(A08, 1724, 2); Put("CK_Roots_Curtain_03", 1733, 185, A08);
        foreach (var p in RoutePassages) if (p.name != "T1" && p.name != "T2") PassageWall(p);
    }
}
