using UnityEngine;

// A side chamber's find, taken by touching it, saved by its permanent reward id so it never comes
// back and never pays twice. What it gives depends on its kind:
//   song shell  - unlocks its track at Qvale's listening tree (town flag shell:<id>);
//   heart seed  - one more heart for good (a saved "heartseed" pickup, which PlayerHealth counts);
//   amber       - Amber for Qvale's shops (proposed amount; see the handoff's balance note);
//   lore / find / reflection - a saved find with a line of text.
[RequireComponent(typeof(Collider2D))]
public sealed class MraReward : MonoBehaviour
{
    public enum Kind { SongShell, HeartSeed, Amber, Lore, Find, Reflection }
    public Kind kind;
    public string rewardId, title;
    [TextArea] public string text = "";
    public int amber = 25;
    public SpriteRenderer image;

    Vector3 rest; float takenAt = -1f;
    public bool Taken => GameSave.HasFlag(rewardId);

    void Awake() { GetComponent<Collider2D>().isTrigger = true; if (image != null) rest = image.transform.localPosition; }
    void Start() { if (Taken) gameObject.SetActive(false); }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (takenAt >= 0f || other.attachedRigidbody == null) return;
        var health = other.attachedRigidbody.GetComponent<PlayerHealth>();
        if (health != null) Collect(health);
    }

    // Touching calls this; so can tests. False if it was already taken.
    public bool Collect(PlayerHealth health)
    {
        if (takenAt >= 0f || !GameSave.SetFlag(rewardId)) return false;
        takenAt = Time.time;
        Sfx.Play(kind == Kind.SongShell ? "SongShell_Get" : kind == Kind.HeartSeed ? "HeartSeed_Get" : kind == Kind.Amber ? "Amber_PickupBig" : "LoreStone_Read");
        switch (kind)
        {
            case Kind.SongShell: TownState.Set("shell:" + rewardId); TownHud.Toast($"Found a song shell: “{title}”.\nThe listening tree in Qvale can play it now.", 5f); break;
            case Kind.HeartSeed:
                GameSave.AddPickup(rewardId + ":heartseed");
                if (health != null) health.AddMaximum(1);
                TownHud.Toast("Heart Seed: one more heart."); break;
            case Kind.Amber: TownState.AddAmber(amber); TownHud.Toast($"Found Amber (+{amber})."); break;
            default: TownHud.Toast(string.IsNullOrEmpty(text) ? "Found: " + title : title + "\n" + text, 6f); break;
        }
        if (Fx.Library != null) { Fx.Pop(Fx.Library.telegraphGlint, transform.position, 1.4f, .45f, 45); Fx.Leaves(transform.position, 8, 1f); }
        if (image != null) image.enabled = false;
        foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
        return true;
    }

    void Update()
    {
        if (takenAt < 0f && image != null) image.transform.localPosition = rest + Vector3.up * (.08f * Mathf.Sin(Time.time * 2.4f));
    }
}
