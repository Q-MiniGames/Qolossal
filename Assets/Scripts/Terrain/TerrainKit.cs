using UnityEngine;

// One area's modular terrain art plus the pixel landmarks that line the pieces up:
// where Qori walks on each piece and where each rock face ends. Landmarks are
// measured in sprite pixels, x from the left edge and y from the top edge, so they
// can be read straight off the source PNG.
[CreateAssetMenu(menuName = "Qolossal/Terrain Kit")]
public sealed class TerrainKit : ScriptableObject
{
    [Header("Strips and fills")]
    public Sprite groundTop;
    [Tooltip("Walk line of Ground_Top, px from the top edge.")] public float groundTopWalkLine = 96;
    public Sprite groundFill;
    public Sprite wallSide;
    [Tooltip("Right-hand rock face of Wall_Side, px from the left edge.")] public float wallSideFace = 383;
    public Sprite ceilingUnder;
    [Tooltip("Solid underside of Ceiling_Under, px from the top edge.")] public float ceilingUnderside = 105;
    public Sprite wallClimbable;
    public Sprite wallSlippery;

    [Header("Corners (right-hand versions; left ones are mirrored)")]
    public Sprite cornerOuterTopRight;
    public Vector2 cornerOuterTopRightLedge = new Vector2(488, 72);
    public Sprite cornerOuterBottomRight;
    public Vector2 cornerOuterBottomRightEdge = new Vector2(411, 316);

    [Header("Slope: surface outline in px, lower-left end first")]
    public Sprite slope30;
    public Vector2[] slopeSurface =
    {
        new Vector2(40, 1280), new Vector2(100, 1238), new Vector2(700, 869), new Vector2(1300, 519),
        new Vector2(1650, 255), new Vector2(2004, 248), new Vector2(2004, 1280)
    };

    [Header("Platforms: walk line px from the top, usable span px from the left")]
    public Sprite platformOneWay;
    public float oneWayWalkLine = 108;
    public Vector2 oneWaySpan = new Vector2(48, 985);
    public Sprite floatingSmall, floatingMedium, floatingLarge;
    public Vector3 floatingSmallLine = new Vector3(20, 252, 36);
    public Vector3 floatingMediumLine = new Vector3(24, 428, 46);
    public Vector3 floatingLargeLine = new Vector3(24, 608, 47);

    // Converts a landmark in sprite pixels to a local offset from the sprite's pivot.
    public static Vector2 Local(Sprite sprite, Vector2 pixelFromTopLeft)
    {
        float ppu = sprite.pixelsPerUnit;
        return new Vector2((pixelFromTopLeft.x - sprite.pivot.x) / ppu,
            (sprite.rect.height - pixelFromTopLeft.y - sprite.pivot.y) / ppu);
    }
}
