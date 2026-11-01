using UnityEngine;

// A station of the heart's lift (MraUgHeartLift), one beside each stop. Until both sectors have
// been reached on their own it explains nothing more than that it is stuck; once they have, the
// first press sets the latch (saved), and after that it calls or sends the cabin.
public sealed class MraUgLiftStation : MraUgInteract
{
    public MraUgHeartLift lift;
    public bool upperStop;
    [Tooltip("Painted lever (Batch 29): rest until the latch is set, pulled after.")] public SpriteRenderer lever;
    public Sprite restSprite, pulledSprite;
    protected override void Update()
    {
        base.Update();
        if (lever != null && pulledSprite != null && lift != null) lever.sprite = lift.Latched && lift.BothVisited ? pulledSprite : restSprite;
    }
    public string stuckText = "The mechanism is locked from both sides. Something has to be reached from the other side first.";
    public string latchText = "The latch drops. The lift will answer from either side now.";

    public override bool Use()
    {
        if (lift == null) return false;
        if (!lift.BothVisited) { TownHud.Toast(stuckText); Sfx.Play("Lever_Pull", transform.position); return false; }
        if (!lift.Latched)
        {
            GameSave.SetFlag(lift.linkFlag);
            TownHud.Toast(latchText);
            Sfx.Play("Gate_Open", transform.position);
            return true;
        }
        return lift.Call(upperStop);
    }

    protected override string PromptText
    {
        get
        {
            if (lift == null) return "";
            if (!lift.BothVisited) return "▲ Try the old lift";
            if (!lift.Latched) return "▲ Set the latch";
            if (lift.Moving) return "";
            bool here = upperStop ? lift.AtUpper : lift.AtLower;
            return here ? "▲ Ride the lift" : "▲ Call the lift";
        }
    }
}
