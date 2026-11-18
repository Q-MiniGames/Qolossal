using UnityEngine;

// The accepted Codex art the ambient layer draws with, in Resources so AmbientLife can load it
// (Qolossal > FX > Build Ambient Kit).
[CreateAssetMenu(menuName = "Qolossal/Ambient Kit")]
public sealed class AmbientKit : ScriptableObject
{
    public Sprite mist, firefly, ripple, gust, leaf;
    [Tooltip("The forge fire's loop, cut by Tools/ArtRequests/LudoRun/export_forge.py. Empty: no overlay.")] public Sprite[] forgeFire;
    [Tooltip("Ludo: a sparrow on one canvas, sitting, leaving its twig, one wingbeat; and the twig alone.")] public Sprite[] birdPerch, birdRise, birdFlap;
    public Sprite birdTwig;
    [Tooltip("Ludo still; flapped in code.")] public Sprite butterfly;
    [Tooltip("Ludo: the churn where falling water lands.")] public Sprite[] splash;
}
