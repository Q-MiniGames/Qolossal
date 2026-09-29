using UnityEngine;

// Marks a scene as an area of the game, as opposed to a sandbox or lab scene. In an area,
// Qori's relics come from the saved game and new ones are saved; elsewhere he has them all.
// Its level id is what the level's Waymark charts, and what the world atlas knows it by.
[DisallowMultipleComponent]
public sealed class GameArea : MonoBehaviour
{
    [Tooltip("Shown on arrival, e.g. \"A1 Aqueduct Ravine\".")] public string displayName;
    [Tooltip("Permanent id of this level, e.g. r1-grip-knot. Do not change after saving progress.")] public string levelId;
    [Tooltip("The region of the titan's body, R1 to R7 (world design, section 1.3).")] public string region;
    [Tooltip("Where the level lies on the Chart (Chart_Base), 0-1 from the bottom-left.")] public Vector2 chartPosition;

    public static GameArea InScene => FindAnyObjectByType<GameArea>();
}
