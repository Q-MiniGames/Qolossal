using UnityEngine;

// Marks a scene as an area of the game, as opposed to a sandbox or lab scene. In an area,
// Qori's relics come from the saved game and new ones are saved; elsewhere he has them all.
[DisallowMultipleComponent]
public sealed class GameArea : MonoBehaviour
{
    [Tooltip("Shown on arrival, e.g. \"A1 Aqueduct Ravine\".")] public string displayName;

    public static GameArea InScene => FindAnyObjectByType<GameArea>();
}
