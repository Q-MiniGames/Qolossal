using UnityEngine;

// New ability behaviours extend this small contract, never the movement controller.
public abstract class AbilityDefinition : ScriptableObject
{
    public string abilityId;
    [Min(0)] public float resourceCost,cooldown;
    public string[] requirements=new string[0];
    public virtual bool CanActivate(PlayerAbilityController owner)=>true;
    public abstract bool Activate(PlayerAbilityController owner);
    public virtual void OnAttackHit(PlayerAbilityController owner,AttackHitResult hit){}
}
