using UnityEngine;

// An underground valve, wheel or latch: Up saves its flag for good, when its own requirements hold
// (the second sluice needs the basin drained first, however Qori got to it). What the flag does is
// up to whatever listens for it: water (MraUgWater), gates (MraGate.requires), the return lift.
public sealed class MraUgSwitch : MraUgInteract
{
    public string flag;
    public string label = "Turn the wheel";
    [Tooltip("Saved predicates needed first.")] public string[] requires = new string[0];
    public string refusedText = "It won't turn.";
    public string doneText = "";
    [Tooltip("Turned once when worked (a wheel's art).")] public Transform wheel;
    public string sound = "Wheel_Turn";
    [Tooltip("Painted states (Batch 29's valve): the rest art, swapped for doneSprite once worked.")] public SpriteRenderer image;
    public Sprite doneSprite;
    void Start() { if (Done && image != null && doneSprite != null) image.sprite = doneSprite; }
    float turn;

    public bool Done => GameSave.HasFlag(flag);

    public override bool Use()
    {
        if (Done) return false;
        if (!MraState.Holds(requires)) { TownHud.Toast(refusedText); Sfx.Play("Lever_Pull", transform.position); return false; }
        GameSave.SetFlag(flag);
        turn = 360f;
        if (image != null && doneSprite != null) image.sprite = doneSprite;
        if (!string.IsNullOrEmpty(sound)) Sfx.Play(sound, transform.position);
        if (!string.IsNullOrEmpty(doneText)) TownHud.Toast(doneText);
        if (Fx.Library != null) Fx.Leaves(transform.position + Vector3.up, 4, .8f);
        return true;
    }

    protected override void Update()
    {
        base.Update();
        if (turn <= 0f || wheel == null) return;
        float step = Mathf.Min(turn, 300f * Time.deltaTime);
        turn -= step; wheel.Rotate(0f, 0f, -step);
    }

    protected override string PromptText => Done ? "" : "▲ " + label;
}
