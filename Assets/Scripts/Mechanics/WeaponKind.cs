using UnityEngine;

// Which of Qori's weapons (and which attack) a hit came from, for mechanics that only yield to one.
public enum WeaponKind { Sword, Mace, Spear, Sling, Staff, Unknown }

public static class WeaponKinds
{
    public static WeaponKind Of(CombatDamage hit)
    {
        if (hit.Attack != null && hit.Attack.slingProjectile) return WeaponKind.Sling;
        return OfWeapon(hit.Weapon);
    }

    public static WeaponKind OfWeapon(WeaponDefinition weapon)
    {
        if (weapon == null) return WeaponKind.Unknown;
        switch (weapon.weaponId)
        {
            case "forest-0": case "reedblade": return WeaponKind.Sword;
            case "forest-2": return WeaponKind.Mace;
            case "forest-3": return WeaponKind.Spear;
            case QoriArmoryFactory.StaffId: return WeaponKind.Staff;
            case "resin-sling": return WeaponKind.Sling;
            default: return WeaponKind.Unknown;
        }
    }

    // Heavy enough to crack stone (the Shellback's shell, rubble walls): the mace, or an attack marked breaksArmor (the
    // Leaf Staff's charged sweep, since the mace is gone from Qori's set, 16 Oct).
    public static bool IsHeavy(CombatDamage hit) => Of(hit) == WeaponKind.Mace || hit.Attack != null && hit.Attack.breaksArmor;

    public static bool IsDownward(CombatDamage hit) => hit.Attack != null && hit.Attack.direction == AttackAim.Down;

    public static CombatDamageResponse Blocked() => new CombatDamageResponse { Disposition = CombatHitDisposition.Blocked };
}
