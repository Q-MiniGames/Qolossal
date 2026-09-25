using UnityEngine;

// Optional unlock. No pogo is enabled by the default weapon or animation.
[CreateAssetMenu(menuName="Qolossal/Abilities/Pogo")]
public sealed class PogoAbilityDefinition : AbilityDefinition
{
    [Min(0)] public float bounceSpeed=10;
    public override bool Activate(PlayerAbilityController owner)=>false;
    public override void OnAttackHit(PlayerAbilityController owner,AttackHitResult hit)
    {
        if(hit.CanPogo&&!owner.Movement.IsGrounded&&owner.ConsumePassive(this,hit.ExecutionId))owner.Movement.QueueCombatBounce(bounceSpeed);
    }
}
