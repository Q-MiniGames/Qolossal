using UnityEngine;

// The surface entrances of the underground routes (MraUgSurface places them at run time). Written by
// MraUndergroundBuilder from Underground/underground_v1.json and the surveyed positions.
public sealed class MraUgSurfaceSet : ScriptableObject
{
    [System.Serializable]
    public sealed class Entrance
    {
        public string id, scene, targetScene, targetArrival = "IN";
        [Tooltip("The doorway's foot (on the ground) and where Qori's feet land coming back up.")] public Vector2 feet, returnFeet;
        public string returnId;
        public float returnFacing = 1f, feetToCentre = .6f;
        public Vector2 triggerSize = new Vector2(1.2f, 1.6f);
        public string label = "Go down", closedText = "It won't open from here.", discoveredFlag = "", discoveredText = "";
        public string[] requires = new string[0];
        public float promptHeight = 2.6f;
        public Sprite art; public Vector2 artOffset; public float artScale = 1f; public int artOrder = -30; public Color artTint = Color.white;
    }

    public Entrance[] entrances = new Entrance[0];

    // A skill cave moved to its region's former ability shrine (the grant migration): its existing
    // door (same chamber id, discovered flag and scene) and its return landing move to the surveyed
    // pad, and that shrine stands down, its relic now granted inside the cave.
    [System.Serializable]
    public sealed class CaveMove
    {
        public string chamberId, scene, returnId, ability;
        public Vector2 feet, returnFeet;
        public float feetToCentre = .62f;
    }

    public CaveMove[] caveMoves = new CaveMove[0];

    // Route E replacing the Descent's walk (the design's option): the Summit's existing way down
    // leads into E1 instead, and the Descent's two side caves (Quiet Alcove, Root Rest) return to
    // their new doorways in E2 and E3. Same entrance, same caves, same rewards; one finale.
    [System.Serializable]
    public sealed class Redirect
    {
        [Tooltip("The scene it applies in, and the destination it currently has.")] public string scene, fromScene;
        public string toScene, toArrival, label;
    }

    public Redirect[] redirects = new Redirect[0];
}
