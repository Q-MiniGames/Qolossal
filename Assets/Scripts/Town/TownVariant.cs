using UnityEngine;

// A part of the town that exists only while a condition holds (TownState.Check): a crack in the
// square after the first quake, lamps lit once their oil is paid for, a hint marker. Its children
// are shown or hidden whenever the town changes.
[DefaultExecutionOrder(-1000), DisallowMultipleComponent]
public sealed class TownVariant : MonoBehaviour
{
    public string condition = "quake";

    void Awake() { Apply(); TownState.Changed += Apply; }
    void OnDestroy() => TownState.Changed -= Apply;

    public void Apply()
    {
        if (this == null) return;
        bool on = TownState.Check(condition);
        foreach (Transform child in transform) child.gameObject.SetActive(on);
    }
}
