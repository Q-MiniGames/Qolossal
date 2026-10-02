using UnityEngine;

// Something Qori picks up by touching it: Amber (the currency) or a song shell, which unlocks a
// track at the listening spot. It bobs gently until it's taken. Nothing is saved (TownState).
[DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
public sealed class TownPickup : MonoBehaviour
{
    public enum Kind { Amber, SongShell }
    public Kind kind;
    [Tooltip("Amber: how much.")] public int amount = 5;
    [Tooltip("Song shell: its id (flag shell:<id>) and the track's title.")] public string shellId = "", trackTitle = "";

    public bool Taken { get; private set; }
    Vector3 rest; float phase;

    void Awake() { GetComponent<Collider2D>().isTrigger = true; rest = transform.position; phase = rest.x * .7f; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Taken || other.attachedRigidbody == null || other.attachedRigidbody.GetComponent<PlayerMovement>() == null) return;
        Taken = true;
        if (kind == Kind.Amber) TownState.AddAmber(amount);
        else
        {
            TownState.Set("shell:" + shellId);
            TownHud.Toast($"Found a song shell: “{trackTitle}”.\nThe musician under the big tree can play it now.", 5f);
        }
        if (Fx.Library != null) Fx.Leaves(transform.position, kind == Kind.Amber ? 4 : 10, 1f);
        gameObject.SetActive(false);
    }

    void Update() => transform.position = rest + Vector3.up * (.1f * Mathf.Sin(Time.time * 2.4f + phase));
}
