using System;
using System.Collections.Generic;
using UnityEngine;

// What the game knows about every level without loading it: its scene, level id and name, its
// Waymark, and its vein ends (portals). The room builders record their scene here when they
// build it (Qolossal > World > Rebuild Atlas records them all). Wild Veins pick their random
// destination from it, and the Chart will draw from it.
public sealed class WorldAtlas : ScriptableObject
{
    [Serializable] public sealed class VeinEnd
    {
        public string portalId, destinationScene, destinationPortal;
        public bool wild;
        [Tooltip("Wild Veins: the knot that settles it.")] public string settlesWith;
        [Tooltip("Set when the portal is inside a stir variant: it exists only before or after this knot.")] public string knot;
        public bool afterKnot;
        public bool Present => string.IsNullOrEmpty(knot) || GameSave.IsKnotAwake(knot) == afterKnot;
    }
    [Serializable] public sealed class Level
    {
        public string scene, levelId, displayName, region;
        [Tooltip("Where the level lies on the Chart, 0-1 from the bottom-left.")] public Vector2 chartPosition;
        [Tooltip("The Waymark's checkpoint id, or empty.")] public string waymark;
        [Tooltip("The knot this level holds (a Knot Chamber), or empty.")] public string knot;
        public List<VeinEnd> veins = new List<VeinEnd>();
    }

    public List<Level> levels = new List<Level>();

    static WorldAtlas loaded;
    public static WorldAtlas Load() => loaded != null ? loaded : loaded = Resources.Load<WorldAtlas>("World/WorldAtlas");

    public Level LevelOf(string scene) => levels.Find(l => l.scene == scene);
    public Level LevelById(string levelId) => levels.Find(l => l.levelId == levelId);

    // Whether the scene can be loaded. Outside Play mode (editor checks) the runtime can't tell, so
    // the editor's build list answers.
    static bool InBuild(string scene)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return System.Array.Exists(UnityEditor.EditorBuildSettings.scenes, s => s.enabled && System.IO.Path.GetFileNameWithoutExtension(s.path) == scene);
#endif
        return Application.CanStreamedLevelBeLoaded(scene);
    }

    // A place a vein can throw Qori: a scene and the portal or Waymark (checkpoint) id he arrives at.
    public struct Place { public string scene, arrival, name; public bool waymark; }

    // Where a Wild Vein may throw Qori: any charted level's Waymark, or any vein end he hasn't
    // travelled yet (never another Wild Vein, and never `fromPortal` itself). Only levels in the
    // build count, and only vein ends that exist in the titan's current state.
    public List<Place> WildDestinations(string fromPortal)
    {
        var places = new List<Place>();
        foreach (var level in levels)
        {
            if (!InBuild(level.scene)) continue;
            if (!string.IsNullOrEmpty(level.waymark) && GameSave.IsCharted(level.levelId))
                places.Add(new Place { scene = level.scene, arrival = level.waymark, name = level.displayName, waymark = true });
            foreach (var end in level.veins)
                if (!end.wild && end.Present && end.portalId != fromPortal && !GameSave.KnowsVeinEnd(end.portalId))
                    places.Add(new Place { scene = level.scene, arrival = end.portalId, name = level.displayName });
        }
        return places;
    }
}
