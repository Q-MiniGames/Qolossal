using UnityEngine;

// M-02, the Echo Warden (Echo Vault): a bell-headed guard. Its bell rings and a glint marks where Qori stood: the hammer
// lands there a moment later, so keep moving. The clapper shows after each attack.
public sealed class EchoWardenBoss : UndergroundGuardian
{
    Vector2 mark; bool marked;
    protected override string DesignId => "M-02";
    protected override int Hits => 12;
    protected override Vector2 WeakOffset => new Vector2(.6f, 3f);
    protected override Vector2 BodySize => new Vector2(2.4f, 3.4f);
    protected override int NextAttack(int n) => 0;
    protected override void Awake() { base.Awake(); warnTime = 1f; }
    void Update()
    {
        if (Current != State.Warn) { marked = false; return; }
        if (marked) return;
        marked = true;
        var p = Player; mark = p != null ? (Vector2)p.transform.position : Feet;
        Fx.Glint(mark);
    }
    protected override void OnStrike(int a) { if (Fx.Library != null) Fx.Play(Fx.Library.dustLand, mark, 14f, 2f, 30); }
    protected override bool StrikeZone(int a, out Vector2 centre, out Vector2 size) { centre = mark; size = new Vector2(3f, 2.4f); return true; }
}
