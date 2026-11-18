using UnityEngine;

// Marks solid ledges (including temporary platforms) that have no platforming
// authoring component. This affects camera framing only, never collision or movement.
[DisallowMultipleComponent]
public sealed class CameraPlatformSurface : MonoBehaviour
{
    public static bool IsPlatform(Collider2D support)
    {
        if (support == null) return false;
        // These authored room obstacles sit above the continuous cave floor.
        // Solid stone landings belong to the same framing class as one-way decks.
        if (support.GetComponentInParent<UndergroundPlatformingGroup>() != null)
            return true;
        var piece = support.GetComponentInParent<TerrainPiece>();
        if (piece != null && piece.kind != TerrainPiece.Kind.Slope30)
            return true;
        var oneWay = support.usedByEffector ? support.GetComponentInParent<PlatformEffector2D>() : null;
        return (oneWay != null && oneWay.isActiveAndEnabled && oneWay.useOneWay) ||
            support.GetComponentInParent<MovingPlatform>() != null ||
            support.GetComponentInParent<CrumblePlatform>() != null ||
            support.GetComponentInParent<CameraPlatformSurface>() != null;
    }
}
