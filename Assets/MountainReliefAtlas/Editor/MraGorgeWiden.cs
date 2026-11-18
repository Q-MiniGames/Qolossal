using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Widens the Heights glide gorge in place, from 18 u to 28 u (WS3, 3 Nov 2026):
// a sprinting double jump cleared 18 u without the Glidecap in the engine test (MraGorgeTest).
// The launch stays; the landing, the gorge floor's far end and the ground past them move out. A full
// region rebuild would lose the later cave and art passes, so this edits those points only.
// Not run yet. After it: Tools/Design/AbilityAccess/gorge_data_28.py sets GlideGap, the route and the test to match.
// Batch: -executeMethod MraGorgeWiden.Run -quit
public static class MraGorgeWiden
{
    const string ScenePath = "Assets/Scenes/Game/MRAtlas_MR06_Heights.unity";
    const float NewGap = 28f, OldLanding = 412f, LandingWidth = 6f, LandingY = 221f;

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var pieces = Object.FindObjectsByType<MraTerrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        MraTerrain Piece(string name) => pieces.First(p => p.name.StartsWith(name));
        var landing = Piece("Route MR06 piece 01");
        var beyond = Piece("Route MR06 piece 02");
        var floor = Piece("Gorge floor");
        if (!Mathf.Approximately(landing.surface[0].x, OldLanding)) { Debug.LogError("[MraGorgeWiden] the landing isn't at " + OldLanding + ": already widened?"); EditorApplication.Exit(1); return; }

        float newLanding = 394f + NewGap, shift = newLanding - OldLanding, landEnd = newLanding + LandingWidth;
        landing.surface = new[] { new Vector2(newLanding, LandingY), new Vector2(landEnd, LandingY) };

        // Past the landing: a drop to where the old slope's line had reached, then the same ground.
        var old = beyond.surface.ToList();
        int resume = old.FindIndex(p => p.x > landEnd + .5f);
        Vector2 a = old[resume - 1], b = old[resume];
        var next = new List<Vector2> { new Vector2(landEnd, LandingY), new Vector2(landEnd, Mathf.LerpUnclamped(a.y, b.y, (landEnd - a.x) / (b.x - a.x))) };
        next.AddRange(old.Skip(resume));
        beyond.surface = next.ToArray();

        var f = floor.surface.ToList();
        f[f.Count - 1] = new Vector2(newLanding, f[f.Count - 1].y);
        floor.surface = f.ToArray();
        foreach (var t in new[] { landing, beyond, floor }) { t.Rebuild(); EditorUtility.SetDirty(t); }

        // What stood on the old landing goes with it.
        foreach (var tr in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)))
        {
            var p = tr.position;
            if (tr.name.StartsWith("Decor Decor_A3_mushrooms") && Mathf.Abs(p.x - 413.3f) < .5f && p.y > 215f) tr.position = p + Vector3.right * shift;
            else if (tr.name.StartsWith("Hanging Decor_A3_hanging_ivy") && Mathf.Abs(p.x - 427.6f) < .5f) tr.position = new Vector3(landEnd + .1f, p.y, p.z);
            else if (tr.name.StartsWith("Glidecap needed here")) tr.position = new Vector3(394f + NewGap * .5f, p.y, p.z);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[MraGorgeWiden] gorge is now {NewGap} u, landing {newLanding} to {landEnd}");
    }
}
