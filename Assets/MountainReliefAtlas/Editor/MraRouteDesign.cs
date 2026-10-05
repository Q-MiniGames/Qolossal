using System;
using System.IO;
using System.Linq;
using UnityEngine;

// The designed walk lines (Resources/MRA/mra_route.json, written by
// Tools/MountainReliefAtlas/design_route.py from mra_world.json): per region, the ground pieces
// (terraces, ramps, walls, the modules' fixed ground), where each side chamber's mouth sits, the
// combat shelves, and every wall and ramp the designer placed (for decor and the audit).
namespace Mra
{
    [Serializable] public sealed class DesignPoint { public float x, y; }
    [Serializable] public sealed class DesignPiece { public string label; public bool clingable; public DesignPoint[] points; }
    [Serializable] public sealed class DesignDoor { public string chamber; public float x, y, wallX, wallTop; public bool embedded; }
    [Serializable] public sealed class DesignShelf { public string encounter; public float x0, x1, y; }
    [Serializable] public sealed class DesignFeature { public string kind, edge, name; public float x, y, rise, run; public bool descent; }
    [Serializable] public sealed class DesignRegion
    {
        public string id;
        public DesignPiece[] pieces;
        public DesignDoor[] doors;
        public DesignShelf[] shelves;
        public DesignFeature[] features;
        public DesignDoor Door(string chamber) => Array.Find(doors, d => d.chamber == chamber);
        public DesignShelf Shelf(string encounter) => Array.Find(shelves, s => s.encounter == encounter);
    }

    [Serializable] public sealed class RouteDesign
    {
        public int schema;
        public string worldSha256;
        public DesignRegion[] regions;

        public const string FilePath = MraWorldBuilder.Folder + "Resources/MRA/mra_route.json";
        static RouteDesign loaded;

        public static RouteDesign Load()
        {
            if (loaded != null) return loaded;
            if (!File.Exists(FilePath)) throw new FileNotFoundException("Run Tools/MountainReliefAtlas/design_route.py first.", FilePath);
            loaded = JsonUtility.FromJson<RouteDesign>(File.ReadAllText(FilePath));
            string worldPath = MraWorldBuilder.Folder + "Resources/MRA/mra_world.json";
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                string hash = string.Concat(sha.ComputeHash(File.ReadAllBytes(worldPath)).Select(b => b.ToString("x2")));
                if (hash != loaded.worldSha256) Debug.LogWarning("[MraRouteDesign] mra_route.json was designed from another mra_world.json: rerun design_route.py.");
            }
            return loaded;
        }

        public static void Forget() => loaded = null;
        public static DesignRegion Of(string region) => Array.Find(Load().regions, r => r.id == region);
    }
}
