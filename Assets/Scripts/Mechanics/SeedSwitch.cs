using UnityEngine;

// A closed bud on a wall mount; a sling shot opens it, and it stays open.
[DisallowMultipleComponent]
public sealed class SeedSwitch : MechanismSwitch, ICombatDamageReceiver
{
    public SpriteRenderer image;
    public Sprite off, on;

    bool triggered; float shakeUntil; Vector3 rest;

    void Awake() { rest = image.transform.localPosition; }

    public CombatDamageResponse ReceiveCombatHit(CombatDamage hit)
    {
        if (triggered) return new CombatDamageResponse { Disposition = CombatHitDisposition.Ignored };
        shakeUntil = Time.time + .15f;
        if (WeaponKinds.Of(hit) != WeaponKind.Sling) return WeaponKinds.Blocked();
        triggered = true;
        image.sprite = on;
        Signal(true);
        return CombatDamageResponse.Applied(0f);
    }

    void Update() => image.transform.localPosition = rest + (Time.time < shakeUntil ? (Vector3)(Random.insideUnitCircle * .02f) : Vector3.zero);
}
