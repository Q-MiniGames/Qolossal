using System.Linq;
using UnityEngine;

// Batch-mode review of the world pieces: renders them in each scene as a new game sees them, and
// again with the Grip Knot awake (every stir variant switched to its after-Grip state).
// Usage: -executeMethod WorldCapture.Run -captureDir <folder>
public static class WorldCapture
{
    // Close-ups for placement: the knot on its dais, and the vein gates beside A0's area portal.
    // Usage: -executeMethod WorldCapture.Details -captureDir <folder>
    public static void Details()
    {
        A0TestRoomBuilder.RenderShots("Assets/Scenes/R1_GripKnot.unity", new (string, Vector2, float)[]
            { ("detail_knot", new Vector2(50f, 3f), 3.2f), ("detail_chamber_east", new Vector2(54f, 3f), 4.5f) });
        A0TestRoomBuilder.RenderShots("Assets/Scenes/A0_TestRoom.unity", new (string, Vector2, float)[]
            { ("detail_a0_gates", new Vector2(166f, 12f), 4.5f) });
    }

    public static void Run()
    {
        void Both(string scene, (string, Vector2, float)[] shots)
        {
            A0TestRoomBuilder.RenderShots(scene, shots);
            A0TestRoomBuilder.RenderShots(scene, shots.Select(s => (s.Item1 + "_after_grip", s.Item2, s.Item3)).ToArray(), () =>
            {
                foreach (var variant in Object.FindObjectsByType<StirVariant>(FindObjectsSortMode.None).Where(v => v.knot == Knots.Grip))
                    foreach (Transform child in variant.transform) child.gameObject.SetActive(variant.when == StirVariant.When.AfterKnot);
                foreach (var block in Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None)) block.Rebuild();
            });
        }
        Both("Assets/Scenes/A0_TestRoom.unity", new (string, Vector2, float)[]
        {
            ("a0_reach", new Vector2(177f, 12f), 12f), ("a0_gates", new Vector2(164f, 11f), 5f), ("a0_crease", new Vector2(183f, 9f), 6f),
        });
        Both("Assets/Scenes/R1_GripKnot.unity", new (string, Vector2, float)[]
        {
            ("r1k_overview", new Vector2(29f, 5f), 20f), ("r1k_entry", new Vector2(4f, 2.5f), 4.5f),
            ("r1k_guardian", new Vector2(28f, 2.5f), 5f), ("r1k_knot", new Vector2(53f, 3f), 5f),
        });
        Both("Assets/Scenes/A1_Aqueduct.unity", new (string, Vector2, float)[] { ("a1_palm_vein", new Vector2(47f, 9f), 5f), ("a1_waymark", new Vector2(128f, -4.5f), 3.5f) });
        A0TestRoomBuilder.RenderShots("Assets/Scenes/A2_Grove.unity", new (string, Vector2, float)[] { ("a2_waymark", new Vector2(117f, 1.5f), 3.5f) });
    }
}
