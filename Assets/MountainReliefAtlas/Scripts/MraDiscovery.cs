using UnityEngine;

// A place found by walking up to it (a Qvale home or shop): saved as discovered, so the Chart pins
// it. A service home (a shop, a resident's story) also counts its reward as found on the first
// visit; a home with a find inside (the musician's loft) leaves that to the find itself.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraDiscovery : MonoBehaviour
{
    public string flag, label, rewardId;
    public bool rewardOnVisit = true;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!MraState.IsQori(other)) return;
        if (GameSave.SetFlag(flag)) { TownHud.Toast(label); Sfx.Play("Discovery"); }
        if (rewardOnVisit && !string.IsNullOrEmpty(rewardId)) GameSave.SetFlag(rewardId);
    }
}
