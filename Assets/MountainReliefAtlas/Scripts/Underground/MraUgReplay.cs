using UnityEngine;

// The replay stone beside the Resonator Door's bowls: lights them in order and clears the attempt.
public sealed class MraUgReplay : MraUgInteract
{
    public MraUgSequence sequence;
    public string label = "Touch the stone (show the order)";

    public override bool Use()
    {
        if (sequence == null || sequence.Solved) return false;
        sequence.Replay();
        Sfx.Play("LoreStone_Read", transform.position);
        return true;
    }

    protected override string PromptText => sequence != null && sequence.Solved ? "" : "▲ " + label;
}
