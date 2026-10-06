using UnityEngine;

// A solid obstacle that only one kind of attack breaks: rubble walls (mace), thorn curtains
// (sword; they leave a cut, wilted curtain behind), weak floors (any downward attack).
[DisallowMultipleComponent]
public sealed class Breakable : MonoBehaviour, ICombatDamageReceiver
{
    public enum Rule { Mace, Sword, DownwardAttack }

    public Rule breaksWith = Rule.Mace;
    public SpriteRenderer intact;
    [Tooltip("Shown after breaking (e.g. the cut thorn curtain). Optional.")] public Sprite brokenSprite;
    [Tooltip("Loose pieces thrown when it breaks. Optional.")] public Sprite[] pieces;
    public Collider2D solid;
    [Min(1)] public int hitsToBreak = 1;

    int hits; bool broken; float shakeUntil; Vector3 restPosition;

    public bool IsBroken => broken;

    void Awake() { if (intact != null) restPosition = intact.transform.localPosition; }

    public CombatDamageResponse ReceiveCombatHit(CombatDamage hit)
    {
        if (broken) return new CombatDamageResponse { Disposition = CombatHitDisposition.Ignored };
        bool right = breaksWith == Rule.Mace ? WeaponKinds.Of(hit) == WeaponKind.Mace
            : breaksWith == Rule.Sword ? WeaponKinds.Of(hit) == WeaponKind.Sword
            : WeaponKinds.IsDownward(hit);
        shakeUntil = Time.time + (right ? .12f : .2f);
        if (!right) return WeaponKinds.Blocked();
        if (++hits >= hitsToBreak) Break(hit.Direction);
        return CombatDamageResponse.Applied(1f, breaksWith == Rule.DownwardAttack);
    }

    public void Break(Vector2 direction)
    {
        broken = true;
        Sfx.Play("Breakable_Smash", transform.position);
        if (solid != null) solid.enabled = false;
        if (intact == null) return;
        Debris.Burst(pieces, intact.bounds, intact.sortingOrder + 1, direction * 2.5f);
        intact.transform.localPosition = restPosition;
        if (brokenSprite != null) intact.sprite = brokenSprite;
        else intact.enabled = false;
    }

    void Update()
    {
        if (intact == null || broken) return;
        intact.transform.localPosition = restPosition + (Time.time < shakeUntil ? (Vector3)(Random.insideUnitCircle * .03f) : Vector3.zero);
    }
}
