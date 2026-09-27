using UnityEngine;

// Which of Qori's weapons (and which attack) a hit came from, for mechanics that only yield to one.
public enum WeaponKind { Sword, Mace, Spear, Sling, Unknown }

public static class WeaponKinds
{
    public static WeaponKind Of(CombatDamage hit)
    {
        if (hit.Attack != null && hit.Attack.slingProjectile) return WeaponKind.Sling;
        if (hit.Weapon == null) return WeaponKind.Unknown;
        switch (hit.Weapon.weaponId)
        {
            case "forest-0": case "reedblade": return WeaponKind.Sword;
            case "forest-2": return WeaponKind.Mace;
            case "forest-3": return WeaponKind.Spear;
            case "resin-sling": return WeaponKind.Sling;
            default: return WeaponKind.Unknown;
        }
    }

    public static bool IsDownward(CombatDamage hit) => hit.Attack != null && hit.Attack.direction == AttackAim.Down;

    public static CombatDamageResponse Blocked() => new CombatDamageResponse { Disposition = CombatHitDisposition.Blocked };
}
