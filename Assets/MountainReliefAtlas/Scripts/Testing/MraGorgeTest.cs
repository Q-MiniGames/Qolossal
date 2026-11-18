#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Mra;
using UnityEngine;
using UnityEngine.InputSystem;

// The Heights glide gorge, tried in earnest (-mraGorgeOnly): every way across without the Glidecap,
// by real input at full sprint with the double jump and the air dash at many timings, then with it.
// The older GlideSpan check in MraTestDriver never tries the double jump.
public sealed partial class MraTestDriver
{
    IEnumerator GorgeLeaps()
    {
        yield return Load("MRAtlas_MR06_Heights");
        var r = W.RegionById("MR06");
        Vector2 n10 = Pos(r.NodeById("MR06_N10"));
        float launch = n10.x + GorgeLaunchFromNode, landing = launch + GorgeWidth;
        foreach (var id in new[] { Relics.DoubleJump, Relics.ClimbingMoss, Relics.WindLeaf }) { GameSave.AddRelic(id); Unlock(id); }
        Abilities.Lock(Relics.Glidecap); GameSave.RemoveRelic(Relics.Glidecap);

        float best = float.NegativeInfinity; string bestMoves = "";
        foreach (float second in new[] { -1f, .35f, .45f, .55f, .65f, .75f, .85f, .95f, 1.05f })
            foreach (float dash in new[] { -1f, .3f, .5f, .7f, .9f, 1.1f, 1.3f })
            {
                yield return GorgeLeap(launch, n10.y, second, dash, false);
                string moves = $"double jump {(second < 0 ? "none" : second.ToString("F2"))}, dash {(dash < 0 ? "none" : dash.ToString("F2"))}";
                Debug.Log($"[MraTest] gorge {moves}: furthest over the far edge's height x {gorgeReach:F2} (landing edge {landing:F1}), ended ({Body.position.x:F1}, {Body.position.y:F1})");
                if (gorgeReach > best) { best = gorgeReach; bestMoves = moves; }
            }
        Expect(best < landing, $"MR06 gorge: no way across without the Glidecap (best {best:F2} of {landing:F1}, by {bestMoves})");

        Unlock(Relics.Glidecap); GameSave.AddRelic(Relics.Glidecap);
        yield return GorgeLeap(launch, n10.y, -1f, -1f, true);
        Expect(gorgeReach >= landing, $"MR06 gorge: a plain jump and glide crosses (reached {gorgeReach:F2} of {landing:F1})");
        Debug.Log($"[MraTest] gorge jump and glide: reach {gorgeReach:F2}");
        yield return GorgeLeap(launch, n10.y, .5f, -1f, true);
        Debug.Log($"[MraTest] gorge double jump and glide: reach {gorgeReach:F2}");
        gorgeWalk = true;
        yield return GorgeLeap(launch, n10.y, -1f, -1f, true);
        Debug.Log($"[MraTest] gorge jump and glide at walking speed: reach {gorgeReach:F2}");
        yield return GorgeLeap(launch, n10.y, .5f, -1f, true);
        Debug.Log($"[MraTest] gorge double jump and glide at walking speed: reach {gorgeReach:F2}");
        gorgeWalk = false;
    }

    // PC04 Mason's Recess, end to end: through its door in the Causeway, onto the floor, and back up.
    IEnumerator MasonsRecess()
    {
        yield return Load("MRAtlas_MR02_Causeway");
        var door = FindObjectsByType<MraUgPortal>(FindObjectsSortMode.None).FirstOrDefault(p => p.arrivalId == "room:PC04");
        Expect(door != null, "PC04: its door is in the Causeway");
        if (door == null) yield break;
        Place((Vector2)door.transform.position + Vector2.up);
        yield return Wait(.5f);
        Expect(door.Use(), "PC04: the door opens with no relic");
        yield return WaitScene("Qolossal_Underground");
        yield return Wait(.8f);
        Vector2 at = Body.position;
        Expect(SceneName == "Qolossal_Underground" && Vector2.Distance(at, new Vector2(306.8f, 2.6f)) < 2f && Qori.IsGrounded, $"PC04: Qori arrives on the room's floor ({at.x:F1}, {at.y:F1}), grounded {Qori.IsGrounded}");
        var back = FindObjectsByType<MraUgPortal>(FindObjectsSortMode.None).OrderBy(p => Vector2.Distance(p.transform.position, at)).FirstOrDefault();
        Expect(back != null && back.Use(), "PC04: the way back works");
        yield return WaitScene("MRAtlas_MR02_Causeway");
        Expect(SceneName == "MRAtlas_MR02_Causeway", "PC04: back in the Causeway");
    }

    public const float GorgeLaunchFromNode = 4f, GorgeWidth = 18f;
    float gorgeReach; bool gorgeWalk;

    // A sprint up to the lip, then the moves at their times after take-off. Reach is how far Qori got
    // while still at or above the landing's height: past the landing's edge means he stands on it.
    IEnumerator GorgeLeap(float edge, float ground, float secondAt, float dashAt, bool glide)
    {
        gorgeReach = float.NegativeInfinity;
        float landingY = ground - 3f;
        Place(new Vector2(edge - 16f, ground + 1.5f));
        yield return Wait(.33f);
        float t = Time.time;
        while (Body.position.x < edge - .25f && Time.time - t < 8f) { if (gorgeWalk) Press(Key.D); else Press(Key.D, Key.LeftShift); yield return null; }
        t = Time.time;
        bool second = false, dashed = false;
        while (Time.time - t < 6f)
        {
            float now = Time.time - t;
            var keys = new System.Collections.Generic.List<Key> { Key.D };
            if (!gorgeWalk) keys.Add(Key.LeftShift);
            // Held for full height; let go a moment before the second press so it's a fresh one.
            bool letGo = secondAt >= 0f && !second && now > secondAt - .06f;
            if (!letGo) keys.Add(Key.Space);
            if (secondAt >= 0f && !second && now >= secondAt) { keys.Add(Key.Space); second = true; }
            if (dashAt >= 0f && !dashed && now >= dashAt) { keys.Add(Key.C); dashed = true; }
            // A second press after falling opens the cap; a plain jump held from take-off needs one too.
            if (glide && secondAt < 0f && now > .55f && now < .62f) keys.Remove(Key.Space);
            Press(keys.ToArray());
            yield return null;
            if (Body.position.y >= landingY) gorgeReach = Mathf.Max(gorgeReach, Body.position.x);
            if (now > .3f && Qori.IsGrounded) break;
            if (Body.position.y < ground - 8.5f) break;
        }
        Press();
        yield return Wait(.2f);
    }
}
#endif
