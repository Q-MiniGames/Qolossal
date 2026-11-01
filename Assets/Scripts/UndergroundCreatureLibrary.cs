using UnityEngine;

// The underground creatures' Ludo.ai frame sets (Resources/LudoFrames/UndergroundCreatureFrames, written by
// UndergroundCreatureLibraryBuilder from Assets/Art/Underground/Creatures). Each entry is one design ID.
[CreateAssetMenu(menuName = "Qolossal/Underground Creature Library")]
public sealed class UndergroundCreatureLibrary : ScriptableObject
{
    [System.Serializable] public sealed class Set { public string name; public Sprite[] frames = new Sprite[0]; }
    [System.Serializable] public sealed class Entry
    {
        public string id, pivot = "feet";
        [Tooltip("The creature's height in world units (its approved concept) and in its reference frame's pixels.")]
        public float heightUnits = 1.5f, figureHeightPx = 400f;
        public Set[] sets = new Set[0];
        // Sprites import at 100 pixels per unit.
        public float Scale => heightUnits / (figureHeightPx / 100f);
        public Sprite[] Get(string n) { foreach (var s in sets) if (s.name == n) return s.frames; return null; }
    }
    public Entry[] creatures = new Entry[0];

    static UndergroundCreatureLibrary loaded;
    public static Entry Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (loaded == null) loaded = Resources.Load<UndergroundCreatureLibrary>("LudoFrames/UndergroundCreatureFrames");
        if (loaded == null) return null;
        foreach (var e in loaded.creatures) if (e.id == id) return e;
        return null;
    }
}
