using UnityEngine;

[CreateAssetMenu(menuName="Qolossal/Abilities/Combat Move")]
public sealed class CombatAbilityDefinition : AbilityDefinition
{
    public AttackDefinition attack;
    public override bool CanActivate(PlayerAbilityController owner)=>owner.Combat.CanUse(attack)&&owner.Combat.CanCancel(CombatCancel.Ability);
    public override bool Activate(PlayerAbilityController owner)=>owner.Combat.TryStartAbilityAttack(attack);
}
