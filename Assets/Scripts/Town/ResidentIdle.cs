using UnityEngine;

// One resident's idle frames, in Resources/LudoFrames/Residents/<Rig> (Qolossal > FX > Build Ambient Kit), so a scene
// loads only the figures it holds.
public sealed class ResidentIdle : ScriptableObject
{
    public Sprite[] idle;
}
