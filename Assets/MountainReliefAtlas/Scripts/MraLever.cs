using UnityEngine;

// A control Qori works by standing at it and pressing Up: a rescue lever, a sluice wheel, a light in
// a sequence. This is the route's proximity-interaction adapter: no weapon is needed, and it never
// stands in for the Sling-only SeedSwitch. A lever signals its targets (gates, water); a sequence
// light reports to its MraSequence instead.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraLever : MechanismSwitch
{
    public string label = "Pull";
    [Tooltip("Stays on once pulled.")] public bool latch = true;
    [Tooltip("Part of a light sequence: reports its index there instead of signalling.")] public MraSequence sequence;
    public int index;
    public SpriteRenderer image;
    public Sprite off, on;
    [Tooltip("Tinted while lit (a sequence light).")] public Color litColor = new Color(1f, .85f, .45f);

    bool near, isOn; Color restColor = Color.white; float flashUntil;
    public bool IsOn => isOn;
    public bool Near => near;

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (image != null) restColor = image.color;
    }
    void OnTriggerEnter2D(Collider2D other) { if (MraState.IsQori(other)) near = true; }
    void OnTriggerExit2D(Collider2D other) { if (MraState.IsQori(other)) near = false; }

    void Update()
    {
        if (near && !MraState.Busy && !GamePauseMenu.BlocksGameplayInput && TownInput.Up()) Use();
        if (image != null && sequence != null) image.color = isOn || Time.time < flashUntil ? litColor : restColor;
    }

    // Up calls this; so can tests.
    public void Use()
    {
        if (sequence != null) { sequence.Press(this); return; }
        if (latch && isOn) return;
        SetLit(!isOn || latch);
        Signal(isOn);
        if (Fx.Library != null) Fx.Leaves(transform.position + Vector3.up, 4, .8f);
    }

    public void SetLit(bool lit)
    {
        isOn = lit;
        if (image != null && on != null && off != null) image.sprite = lit ? on : off;
    }

    public void Flash(float seconds) => flashUntil = Time.time + seconds;

    void OnGUI()
    {
        if (near && !MraState.Busy && !(latch && isOn && sequence == null)) MraState.Prompt(transform.position + Vector3.up * 2.4f, "▲ " + label);
    }
}
