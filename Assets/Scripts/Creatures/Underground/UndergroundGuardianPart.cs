using UnityEngine;

// A part of an underground guardian: its weak point (where hits count) or its strike (which hurts as it lands).
[DisallowMultipleComponent]
public sealed class UndergroundGuardianPart : MonoBehaviour, IContactHazard, ICombatDamageReceiver
{
    public enum Kind { Weak, Strike }
    UndergroundGuardian boss; Kind kind;
    public void Set(UndergroundGuardian owner, Kind k) { boss = owner; kind = k; }
    public bool HurtsOnContact => boss != null && kind == Kind.Strike && boss.StrikeLive;
    public CombatDamageResponse ReceiveCombatHit(CombatDamage hit) =>
        boss == null ? new CombatDamageResponse { Disposition = CombatHitDisposition.Ignored } : kind == Kind.Weak ? boss.HitWeak(hit) : boss.ReceiveCombatHit(hit);
}
