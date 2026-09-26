using UnityEngine;

// The accepted Codex UI art, kept in Resources so the HUD and menus can find it in any scene.
// Built by Qolossal > UI > Build UI Skin.
[CreateAssetMenu(menuName = "Qolossal/UI Skin")]
public sealed class UiSkin : ScriptableObject
{
    public Sprite hudFrame, leafFull, leafHalf, leafEmpty;
    public Sprite panel, buttonNormal, buttonHover, buttonPressed;
    [Header("HUD ring (HUD_01): round medallion, backing disc, tiling vine")]
    public Sprite ring, ringBacking, vine;
    [Tooltip("Ring canvas px: opening centre and diameter, and the tip of the vine stub on the right.")]
    public Vector2 ringOpeningCentre = new Vector2(256, 256);
    public float ringOpeningDiameter = 282;
    public Vector2 ringStubTip = new Vector2(445, 256);
    [Header("Weapon icons, by weaponId")]
    public string[] weaponIds = { "forest-0", "reedblade", "forest-1", "forest-2", "forest-3", "resin-sling" };
    public Sprite[] weaponIcons = new Sprite[6];

    public Sprite IconFor(string weaponId)
    {
        int i = System.Array.IndexOf(weaponIds, weaponId);
        return i >= 0 && i < weaponIcons.Length ? weaponIcons[i] : null;
    }

    [Header("HUD frame landmarks (px in HUD_Frame, from its top-left)")]
    // Centres of the transparent openings, measured from the art: the weapon ring's oval
    // (155 x 97 px) and the six leaf sockets (about 62 x 50 px each).
    public Vector2 weaponRingCentre = new Vector2(210, 130);
    public float weaponRingDiameter = 150;
    public Vector2[] leafSockets =
    {
        new Vector2(385, 134), new Vector2(483, 134), new Vector2(585, 134),
        new Vector2(686, 134), new Vector2(789, 134), new Vector2(891, 134)
    };
    [Tooltip("Leaf size in frame px; a little larger than a socket so the frame rim overlaps its edge.")]
    public float leafSize = 84;

    static UiSkin loaded;
    public static UiSkin Load() => loaded != null ? loaded : loaded = Resources.Load<UiSkin>("UI/UiSkin");

    public static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
}
