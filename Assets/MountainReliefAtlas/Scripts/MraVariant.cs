using UnityEngine;

// A part of a region that exists only while its saved predicates hold (or, inverted, only until
// they do): the fallen stone crossing after the Cradle's quake, a rescued resident at home in
// Qvale, the reveal's view. Its children are shown or hidden whenever the save changes.
[DefaultExecutionOrder(-900), DisallowMultipleComponent]
public sealed class MraVariant : MonoBehaviour
{
    public string[] requires = new string[0];
    [Tooltip("Show the children only while the predicates do NOT hold.")] public bool invert;

    void Awake() => Apply();
    void OnEnable() { GameSave.FlagsChanged += Apply; GameSave.WorldChanged += Apply; TownState.Changed += Apply; }
    void OnDisable() { GameSave.FlagsChanged -= Apply; GameSave.WorldChanged -= Apply; TownState.Changed -= Apply; }

    public bool Shown => MraState.Holds(requires) != invert;

    public void Apply()
    {
        if (this == null) return;
        bool on = Shown;
        foreach (Transform child in transform) if (child.gameObject.activeSelf != on) child.gameObject.SetActive(on);
    }
}
