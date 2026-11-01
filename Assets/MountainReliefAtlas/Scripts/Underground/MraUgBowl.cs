using UnityEngine;

// One of the Resonator Door's bowls (MraUgSequence), carved with its landmark's shape.
public sealed class MraUgBowl : MraUgInteract
{
    public MraUgSequence sequence;
    public string label = "Strike the bowl";
    public SpriteRenderer image, symbol;
    public Color restColor = new Color(.62f, .58f, .52f), litColor = new Color(1f, .86f, .48f);
    bool lit, glow;

    public void SetLit(bool on) { lit = on; Paint(); }
    public void Glow(bool on) { glow = on; Paint(); }
    void Paint()
    {
        var c = lit || glow ? litColor : restColor;
        if (image != null) image.color = c;
        if (symbol != null) symbol.color = lit || glow ? Color.white : new Color(.9f, .86f, .78f);
    }

    void Start() => Paint();

    public override bool Use()
    {
        if (sequence == null || sequence.Solved) return false;
        sequence.Press(this);
        return true;
    }

    protected override string PromptText => sequence != null && sequence.Solved ? "" : "▲ " + label;
}
