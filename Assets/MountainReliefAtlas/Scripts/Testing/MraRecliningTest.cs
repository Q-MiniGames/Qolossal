#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Mra;
using UnityEngine;

// The reclining titan route (Tools/MountainReliefAtlas/reclining.py): the data's body map, the
// Terraces' thigh descent and the Heights' neck dip walked by the autopilot, and the four
// floating-platform levels (drift levels): their launch points, stones, movers, crumbling slabs,
// a fall returning Qori to the last safe stone, and the reward.
public sealed partial class MraTestDriver
{
    IEnumerator RecliningChecks()
    {
        Expect(W.layout == "reclining-titan-2026-10-08", $"reclining: the world data uses the reclining layout ({W.layout})");
        Expect(W.regions.All(r => !string.IsNullOrEmpty(r.body)), "reclining: every region has its (designer-only) body place");
        var mr03 = W.RegionById("MR03");
        Vector2 knot = mr03.NodeById("MR03_N08").position, n09 = mr03.NodeById("MR03_N09").position, exit = mr03.NodeById(mr03.exit).position;
        Expect(knot.y > mr03.nodes.Where(n => n.id != "MR03_N08").Max(n => n.position.y) && exit.y < knot.y - 50f,
            $"reclining: the Terraces crest at the knot (y {knot.y:F0}) and run down the thigh to the exit (y {exit.y:F0})");
        yield return ClimbRepeat("MRAtlas_MR03_Terraces", mr03.NodeById("MR03_N09").spawn, exit.x - 3f, 1);
        Expect(Body.position.y < n09.y - 30f, $"reclining: the thigh descent ends low ({Body.position.y:F0} vs {n09.y:F0} at the pass)");
        var mr06 = W.RegionById("MR06");
        Vector2 n09h = mr06.NodeById("MR06_N09").position, n10 = Pos(mr06.NodeById("MR06_N10")), out6 = Pos(mr06.NodeById(mr06.exit));
        Expect(out6.y < n09h.y - 25f, $"reclining: the Heights end in the neck dip (exit y {out6.y:F0}, shoulder {n09h.y:F0})");
        float landing = n10.x + 4f + 18f;
        yield return ClimbRepeat("MRAtlas_MR06_Heights", new Vector2(landing + 2f, n10.y - 1.5f), out6.x - 2f, 1);
        // The face: the Prow (a long bare ridge) after the pale steps, walked; the reveal looks back down first.
        var mr07 = W.RegionById("MR07");
        Vector2 steps = mr07.NodeById("MR07_N02").position, reeds = mr07.NodeById("MR07_N03").position;
        Expect(reeds.x - steps.x >= 55f && (reeds.y - steps.y) / (reeds.x - steps.x) < .45f, $"reclining: the Prow runs {reeds.x - steps.x:F0} m from the pale steps to the reeds");
        yield return ClimbRepeat("MRAtlas_MR07_Summit", mr07.NodeById("MR07_N02").spawn + new Vector2(5f, 0f), reeds.x - 3f, 1);
        var reveal = FindAnyObjectByType<MraReveal>();
        Expect(reveal != null && reveal.lookBackSize > 0f && reveal.lookBack.x < reveal.transform.position.x - 100f && reveal.lookBack.y < reveal.transform.position.y - 50f,
            $"reclining: the reveal first looks back down the face ({(reveal != null ? reveal.lookBack : Vector2.zero)}), then pulls out to {(reveal != null ? reveal.pullCentre : Vector2.zero)}");
    }

    IEnumerator DriftChecks()
    {
        var drifts = W.chambers.Where(c => c.IsDrift).ToList();
        Expect(drifts.Count == 4 && drifts.Select(c => c.region).Distinct().Count() == 4, $"drift: four floating-platform levels over four parts of the route ({string.Join(", ", drifts.Select(c => c.region))})");
        foreach (var c in drifts)
        {
            var region = W.RegionById(c.region);
            yield return Load(region.scene);
            var door = FindObjectsByType<MraChamberDoor>(FindObjectsSortMode.None).FirstOrDefault(d => d.chamberId == c.id);
            Expect(door != null && door.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name.StartsWith("Launch")), $"{c.id}: a launch point on {region.name}");
            yield return Load(c.scene);
            Expect(Vector2.Distance(Body.position, c.playerSpawn) < .8f && !InGround(), $"{c.id}: Qori arrives on the start stone {Body.position}");
            yield return Trial(c);
            var reward = FindAnyObjectByType<MraReward>();
            Expect(reward != null && Vector2.Distance(reward.transform.position, c.rewardAt[0]) < .1f, $"{c.id}: the {c.reward} waits on the last stone");
        }
    }

    // A drift level's own check (also run by the side-chamber pass): its stones, then a fall.
    IEnumerator DriftTrial(Chamber c)
    {
        var movers = FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None);
        var crumbles = FindObjectsByType<CrumblePlatform>(FindObjectsSortMode.None);
        Expect(movers.Length == c.movers.Length && crumbles.Length == c.crumbles.Length
            && FindObjectsByType<ThreadAnchor>(FindObjectsSortMode.None).Length == c.anchors.Length && FindObjectsByType<MraPod>(FindObjectsSortMode.None).Length == c.pods.Length,
            $"{c.id}: {movers.Length} drifting stones, {crumbles.Length} crumbling, {c.anchors.Length} anchors, {c.pods.Length} pods placed");
        Vector3 before = movers.Length > 0 ? movers[0].transform.position : Vector3.zero;
        yield return Wait(1.2f);
        Expect(movers.Length == 0 || Vector3.Distance(before, movers[0].transform.position) > .05f, $"{c.id}: the drifting stones move");
        // Stand on a safe stone, then drop into the drift: back on that stone.
        Vector2 rest = c.rests.Length > 1 ? c.rests[1] : c.rests[0];
        Place(rest); yield return Wait(0.6f);
        Place(new Vector2(rest.x + .5f, -2f)); yield return Wait(1.2f);
        Expect(Vector2.Distance(Body.position, rest) < 1.6f, $"{c.id}: a fall returns Qori to the last safe stone ({Body.position} vs {rest})");
    }
}
#endif
