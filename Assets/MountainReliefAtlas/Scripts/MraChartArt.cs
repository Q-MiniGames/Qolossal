using UnityEngine;

// The Chart's art, referenced (never copied) by the builder: the eight regional relief paintings
// (review-only design sources, Resources/MRA/Maps), the accepted map icons and frame, and the
// post-reveal final assembly (the approved Batch 11 concept, shown only after the reveal).
public sealed class MraChartArt : ScriptableObject
{
    [Tooltip("By region ordinal - 1 (MR01 first).")] public Sprite[] maps = new Sprite[0];
    public Sprite frame, qori, waymark, shrine, cave, unexplored, npc;
    [Tooltip("Shown only once the reveal is saved.")] public Sprite finalAssembly;

    static MraChartArt loaded;
    public static MraChartArt Load() => loaded != null ? loaded : loaded = Resources.Load<MraChartArt>("MRA/MraChartArt");
}
