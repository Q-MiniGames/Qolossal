using UnityEngine;

// Marks a ground collider whose walls Qori can't cling to (no wall slide, wall jump or ledge grab).
[DisallowMultipleComponent]
public sealed class WallSurface : MonoBehaviour
{
    public bool allowsWallCling = true;

    public static bool AllowsCling(Collider2D collider) =>
        !collider.TryGetComponent(out WallSurface surface) || surface.allowsWallCling;
}
