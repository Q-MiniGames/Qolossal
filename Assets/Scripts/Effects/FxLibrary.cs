using UnityEngine;

// The accepted Codex effect sprites, in Resources so any script can play them.
[CreateAssetMenu(menuName = "Qolossal/FX Library")]
public sealed class FxLibrary : ScriptableObject
{
    public Sprite[] dustLand, dustRun, leaves, portalSwirl, deathPuff, hitSparks, dashBurst;
    public Sprite wallScrape, checkpointMote, hitBlock, telegraphGlint, sapOrb, dashTrail;
    [Tooltip("The Glidecap opened overhead (its pivot is the grip).")] public Sprite glidecapHeld;
}
