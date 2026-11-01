using UnityEngine;

// M-01, the Moss Matriarch (Moss Pocket): a moss-covered sloth. She swipes low in front of her; from phase two she also
// climbs out of reach and drops on Qori's spot. The seed in her belly shows after each attack.
public sealed class MossMatriarchBoss : UndergroundGuardian
{
    [SerializeField] float climb = 6f, dropSpeed = 15f;
    Vector2 target, home; bool landed;
    protected override string DesignId => "M-01";
    protected override int Hits => 12;
    protected override Vector2 WeakOffset => new Vector2(.4f, 1.2f);
    protected override Vector2 BodySize => new Vector2(6f, 3.2f);
    protected override int NextAttack(int n) => Phase == 1 ? 0 : n % 2;
    protected override void Awake() { base.Awake(); home = transform.position; strikeTime = .8f; }
    void Update()
    {
        if (Current != State.Warn || Attack != 1) return;
        float k = Mathf.Clamp01((Time.time - since) / (warnTime / Speed));
        var p = Player;
        if (p != null) { Face(); target = new Vector2(Mathf.Clamp(p.transform.position.x, Room.min.x + 3f, Room.max.x - 3f), home.y); }
        body.MovePosition(new Vector2(Feet.x, home.y + climb * Mathf.SmoothStep(0f, 1f, k)));
    }
    protected override void OnStrike(int a) => landed = a != 1;
    protected override void OnStrikeTick(int a, float t)
    {
        if (a != 1 || landed) return;
        Vector2 next = Vector2.MoveTowards(Feet, target, dropSpeed * Speed * Time.fixedDeltaTime);
        body.MovePosition(next);
        if ((next - target).sqrMagnitude < .01f) { landed = true; CombatCameraShake.Kick(.14f); if (Fx.Library != null) Fx.Play(Fx.Library.dustLand, next, 14f, 2.2f, 30); }
    }
    protected override bool StrikeZone(int a, out Vector2 centre, out Vector2 size)
    {
        if (a == 1) { centre = Feet + Vector2.up * 1.5f; size = new Vector2(4f, 3f); return !landed; }
        centre = Feet + new Vector2(facing * 4f, .8f); size = new Vector2(3.5f, 1.6f); return true;
    }
}
