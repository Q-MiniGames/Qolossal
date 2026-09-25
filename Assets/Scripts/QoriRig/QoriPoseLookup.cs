using UnityEngine;

// One place where gameplay asks "where is Qori's blade / hand right now?".
// Prefers the new cutout rig (QoriAnimator) and falls back to the legacy
// procedural QoriBodyRig, so combat keeps working with either visual.
public static class QoriPoseLookup
{
    public static bool TryGetWeapon(Component owner, ref QoriAnimator rig, ref QoriBodyRig legacy,
        out Transform mount, out Transform tip, out float facing)
    {
        mount = tip = null; facing = 1f;
        if (owner == null) return false;
        if (rig == null) rig = owner.GetComponentInChildren<QoriAnimator>();
        if (rig != null && rig.Ready)
        {
            mount = rig.WeaponMount; tip = rig.WeaponTip; facing = rig.VisualFacing;
            return mount != null && tip != null;
        }
        if (legacy == null) legacy = owner.GetComponentInChildren<QoriBodyRig>();
        if (legacy != null && legacy.BodyActive)
        {
            mount = legacy.GetJoint("WeaponMount"); tip = legacy.GetJoint("WeaponTip"); facing = legacy.VisualFacing;
            return mount != null && tip != null;
        }
        return false;
    }
}
