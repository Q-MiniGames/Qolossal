using UnityEngine;

// An underground control Qori works by standing at it and pressing Up (W / Up arrow, D-pad Up or
// the left stick up; the existing interaction binding): a door, a valve, a bowl, a lift station.
// Every one needs a new press (Up is read as pressed this frame), and none answers during a
// transition or in the first 0.6 s of a scene, so the press that opened a door can never work the
// one Qori arrives beside. (Surface doorways' landings are outside their triggers altogether.)
[RequireComponent(typeof(BoxCollider2D))]
public abstract class MraUgInteract : MonoBehaviour
{
    [Tooltip("The prompt's height over the control's foot.")] public float promptHeight = 2.4f;
    protected bool near;
    const float ArrivalCooldown = .6f;
    protected PlayerMovement qori;
    public bool Near => near;
    /// <summary>True during a transition and the first moments of a scene: an arrival can't trigger it.</summary>
    public bool LockedOut => AreaTransition.IsTransitioning || Time.timeSinceLevelLoad < ArrivalCooldown;

    protected virtual void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        var q = MraState.QoriOf(other); if (q == null) return;
        near = true; qori = q;
        Entered();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!MraState.IsQori(other)) return;
        near = false;
    }

    protected virtual void Entered() { }

    protected virtual void Update()
    {
        if (near && !LockedOut && !MraState.Busy && !GamePauseMenu.BlocksGameplayInput && TownInput.Up()) Use();
    }

    // Up calls this; so can tests. True when it did something.
    public abstract bool Use();

    // The text over it while Qori is near; empty for none. "▲" draws the Up key cap.
    protected abstract string PromptText { get; }

    void OnGUI()
    {
        if (!near || LockedOut || MraState.Busy) return;
        string text = PromptText;
        if (!string.IsNullOrEmpty(text)) MraState.Prompt(transform.position + Vector3.up * promptHeight, text);
    }

    protected PlayerMovement Qori => qori != null ? qori : qori = FindAnyObjectByType<PlayerMovement>();
}
