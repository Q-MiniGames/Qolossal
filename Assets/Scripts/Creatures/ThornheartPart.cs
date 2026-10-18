using UnityEngine;

// A part of the Thornheart: the heart (where hits count), the claw slam and the charging lower body (which hurt).
[DisallowMultipleComponent]
public sealed class ThornheartPart : MonoBehaviour, IContactHazard, ICombatDamageReceiver
{
    public enum Kind { Heart, Slam, Charge }
    ThornheartBoss boss; Kind kind;
    public void Set(ThornheartBoss owner, Kind k) { boss = owner; kind = k; }
    public bool HurtsOnContact => boss != null && (kind == Kind.Slam ? boss.SlamLanding : kind == Kind.Charge && boss.Charging);
    public CombatDamageResponse ReceiveCombatHit(CombatDamage hit) =>
        boss == null ? new CombatDamageResponse { Disposition = CombatHitDisposition.Ignored } : kind == Kind.Heart ? boss.HitHeart(hit) : boss.ReceiveCombatHit(hit);
}
