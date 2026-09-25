using UnityEngine;

[CreateAssetMenu(menuName="Qolossal/Combat/Move Set")]
public sealed class CombatMoveSet : ScriptableObject
{
    [System.Serializable] public struct Entry {public CombatMoveSlot slot;public AttackDefinition attack;}
    public Entry[] moves=new Entry[0];
    public AttackDefinition Find(CombatMoveSlot slot)
    {foreach(var entry in moves)if(entry.slot==slot)return entry.attack;return null;}
    public AttackDefinition Select(AttackRequest request,bool airborne)
    {
        if(request.Slot==CombatMoveSlot.Dash||request.Slot==CombatMoveSlot.Charged||request.Slot==CombatMoveSlot.Special)return Find(request.Slot);
        CombatMoveSlot basic=request.Aim==AttackAim.Up?CombatMoveSlot.Upper:request.Aim==AttackAim.Down?CombatMoveSlot.Lower:CombatMoveSlot.Front;
        if(airborne){var air=Find(request.Aim==AttackAim.Up?CombatMoveSlot.AirUpper:request.Aim==AttackAim.Down?CombatMoveSlot.AirLower:CombatMoveSlot.AirFront);if(air!=null)return air;}
        return Find(basic);
    }
}
