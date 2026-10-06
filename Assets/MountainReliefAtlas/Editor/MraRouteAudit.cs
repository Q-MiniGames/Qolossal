using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// A static audit of the built main routes (no physics), against the level-design rules the live
// Player prefab supports (see Tools/MountainReliefAtlas/design_route.py): every wall is a jump
// (up to 1.9 u), a jump and a ledge grab (up to 3.2 u), or a Climbing Moss climb (clingable, only
// after the Cradle's moss shrine); walkable slopes stay under 0.9; terraces between walls; the gaps
// between pieces are only the modules'. Writes <captureDir>/mra_route_audit.json (default Temp).
// Usage: -executeMethod MraRouteAudit.Run
public static class MraRouteAudit
{
    public const float JumpWall = 1.9f, LedgeWall = 3.2f, MaxSlope = .9f, NarrowTread = 3.5f;

    public static void Run()
    {
        var world = MraWorldBuilder.Prepare();
        string[] args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        string folder = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Temp";
        var sb = new StringBuilder("{\n");
        int problems = 0;
        float mossX = MraWorldBuilder.Pos(world.RegionById("MR01").NodeById("MR01_N06")).x;
        foreach (var r in world.regions)
        {
            var pieces = MraWorldBuilder.Route(r);
            var issues = new List<string>();
            var walls = new List<float>(); var treads = new List<float>();
            int jump = 0, ledge = 0, climb = 0;
            float maxSlope = 0f;
            foreach (var p in pieces)
            {
                float lastWall = p.points[0].x;
                for (int i = 0; i < p.points.Count - 1; i++)
                {
                    Vector2 a = p.points[i], b = p.points[i + 1];
                    if (b.x < a.x - 1e-3f) issues.Add($"line runs backward at x {a.x:0.0}");
                    if (Mathf.Abs(b.x - a.x) < 1e-3f)
                    {
                        float h = Mathf.Abs(b.y - a.y);
                        if (h < .05f) continue;
                        walls.Add(h); treads.Add(a.x - lastWall); lastWall = a.x;
                        if (h <= JumpWall + 1e-3f) jump++;
                        else if (h <= LedgeWall + 1e-3f) ledge++;
                        else
                        {
                            climb++;
                            bool moss = r.id != "MR01" || a.x > mossX;
                            if (!moss) issues.Add($"a {h:0.0} u wall at x {a.x:0.0} before Climbing Moss (a jump and a ledge grab reach 3.2 u)");
                            if (!p.clingable) issues.Add($"a {h:0.0} u wall at x {a.x:0.0} can't be climbed (not clingable) outside a module");
                        }
                    }
                    else maxSlope = Mathf.Max(maxSlope, Mathf.Abs((b.y - a.y) / (b.x - a.x)));
                }
                treads.Add(p.points[p.points.Count - 1].x - lastWall);
            }
            if (maxSlope > MaxSlope) issues.Add($"slope {maxSlope:0.00} (walkable up to 1.2; the design keeps under {MaxSlope})");
            var gaps = new List<string>();
            for (int i = 0; i < pieces.Count - 1; i++)
            {
                Vector2 end = pieces[i].points.Last(), start = pieces[i + 1].points[0];
                gaps.Add($"after piece {i} ({pieces[i + 1].label}): {start.x - end.x:0.0} u across, {start.y - end.y:0.0} u up");
            }
            // Combat shelves: away from beats (checkpoints, shrines, Waymarks) and cave mouths.
            var design = Mra.RouteDesign.Of(r.id);
            foreach (var sh in design.shelves)
            {
                float beat = r.nodes.Min(n => Mathf.Max(0f, Mathf.Max(sh.x0 - MraWorldBuilder.Pos(n).x, MraWorldBuilder.Pos(n).x - sh.x1)));
                float door = design.doors.Length == 0 ? 99f : design.doors.Min(d => Mathf.Max(0f, Mathf.Max(sh.x0 - d.x, d.x - sh.x1)));
                if (beat < 4f) issues.Add($"shelf {sh.encounter} is {beat:0.0} u from a beat");
                if (door < 6f) issues.Add($"shelf {sh.encounter} is {door:0.0} u from a cave mouth");
                if (sh.x1 - sh.x0 < 8f) issues.Add($"shelf {sh.encounter} is only {sh.x1 - sh.x0:0.0} u wide");
            }
            problems += issues.Count;
            int narrow = treads.Count(t => t < NarrowTread);
            sb.Append($" \"{r.id}\": {{\"walls\": {walls.Count}, \"jump_walls\": {jump}, \"ledge_walls\": {ledge}, \"climb_walls\": {climb}, " +
                $"\"max_wall\": {walls.DefaultIfEmpty(0f).Max():0.00}, \"median_tread\": {Median(treads):0.00}, \"treads_under_{NarrowTread}\": {narrow}, \"max_slope\": {maxSlope:0.00},\n" +
                $"  \"module_gaps\": [{string.Join(", ", gaps.Select(g => "\"" + g + "\""))}],\n  \"issues\": [{string.Join(", ", issues.Select(s => "\"" + s + "\""))}]}},\n");
        }
        sb.Append(" \"problems\": " + problems + "\n}\n");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "mra_route_audit.json"), sb.ToString());
        Debug.Log($"[MraRouteAudit] {problems} problems; {Path.Combine(folder, "mra_route_audit.json")}");
    }

    static float Median(List<float> v) { if (v.Count == 0) return 0f; var s = v.OrderBy(x => x).ToList(); return s[s.Count / 2]; }
}
