using System;
using UnityEngine;

public enum AttackAim { Front, Up, Down }
public enum AttackPhase { None, Startup, Active, Recovery }
public enum CombatMoveSlot { Front, Upper, Lower, AirFront, AirUpper, AirLower, Dash, Charged, Special }
[Flags] public enum CombatCancel { None=0, Attack=1, Jump=2, Dash=4, Grapple=8, Ability=16 }
public enum CombatHitShape { Box, Circle, WeaponCapsule }
public enum CombatHitDisposition { Damaged, Blocked, Parried, Ignored }

public struct AttackRequest
{
    public AttackAim Aim;
    public float Facing;
    public CombatMoveSlot Slot;
    public AttackRequest(AttackAim aim,float facing,CombatMoveSlot slot)
    {Aim=aim;Facing=facing<0?-1:1;Slot=slot;}
}
public struct CombatDamage
{
    public GameObject Source;
    public AttackDefinition Attack;
    public WeaponDefinition Weapon;
    public Vector2 Point,Normal,Direction,Knockback;
    public float Damage;
}
public struct CombatDamageResponse
{
    public CombatHitDisposition Disposition;
    public float Damage;
    public bool SupportsPogo;
    public static CombatDamageResponse Applied(float damage,bool pogo=false)=>new CombatDamageResponse{Disposition=CombatHitDisposition.Damaged,Damage=damage,SupportsPogo=pogo};
}
public interface ICombatDamageReceiver { CombatDamageResponse ReceiveCombatHit(CombatDamage hit); }
public struct AttackHitResult
{
    public Component Target;
    public Collider2D Collider;
    public CombatDamage Hit;
    public CombatDamageResponse Response;
    public int ExecutionId;
    public bool CanPogo=>Hit.Attack!=null&&Hit.Attack.direction==AttackAim.Down&&Hit.Attack.pogoCompatible&&Response.SupportsPogo&&Response.Disposition==CombatHitDisposition.Damaged;
}
