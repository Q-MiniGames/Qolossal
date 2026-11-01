using UnityEngine;

// A carving to look at (a landmark's shape in the Three Landmarks maze): Up describes it in words,
// so the shape never depends on seeing a colour or hearing a tone.
public sealed class MraUgClue : MraUgInteract
{
    public string label = "Look at the carving";
    [TextArea] public string text = "";
    public SpriteRenderer symbol;
    float flashUntil;

    public override bool Use()
    {
        TownHud.Toast(text, 5f);
        Sfx.Play("LoreStone_Read", transform.position);
        flashUntil = Time.time + .8f;
        return true;
    }

    protected override void Update()
    {
        base.Update();
        if (symbol != null) symbol.color = Time.time < flashUntil ? Color.white : new Color(.92f, .88f, .8f);
    }

    protected override string PromptText => "▲ " + label;
}
