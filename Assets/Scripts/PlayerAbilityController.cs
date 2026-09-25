using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAbilityController : MonoBehaviour
{
    [SerializeField] AbilityDefinition[] unlockedAbilities=new AbilityDefinition[0];
    [SerializeField,Min(0)] float resource=100;
    readonly Dictionary<string,AbilityDefinition> unlocked=new Dictionary<string,AbilityDefinition>();
    readonly Dictionary<AbilityDefinition,float> readyAt=new Dictionary<AbilityDefinition,float>();
    readonly Dictionary<AbilityDefinition,int> passiveExecution=new Dictionary<AbilityDefinition,int>();
    public PlayerCombat Combat {get;private set;}
    public PlayerMovement Movement {get;private set;}
    public float Resource=>resource;
    public event Action<AbilityDefinition> OnAbilityActivated;
    void Awake(){Combat=GetComponent<PlayerCombat>();Movement=GetComponent<PlayerMovement>();foreach(var ability in unlockedAbilities)Unlock(ability);}
    void OnEnable(){if(Combat!=null)Combat.OnAttackHit+=Hit;}
    void OnDisable(){if(Combat!=null)Combat.OnAttackHit-=Hit;}
    public void Unlock(AbilityDefinition ability){if(ability!=null&&!string.IsNullOrEmpty(ability.abilityId))unlocked[ability.abilityId]=ability;}
    public bool HasAbility(string id)=>string.IsNullOrEmpty(id)||unlocked.ContainsKey(id);
    public void AddResource(float amount)=>resource=Mathf.Max(0,resource+amount);
    bool Requirements(AbilityDefinition ability)
    {
        if(resource<ability.resourceCost||readyAt.TryGetValue(ability,out float time)&&Time.time<time)return false;
        foreach(string id in ability.requirements)if(!HasAbility(id))return false;return true;
    }
    void Consume(AbilityDefinition ability){resource-=ability.resourceCost;readyAt[ability]=Time.time+ability.cooldown;OnAbilityActivated?.Invoke(ability);}
    public bool TryActivate(string id)
    {
        if(!isActiveAndEnabled||GamePauseMenu.BlocksGameplayInput||Time.timeScale<=0||!unlocked.TryGetValue(id,out var ability)||!Requirements(ability)||!ability.CanActivate(this))return false;
        if(!ability.Activate(this))return false;Consume(ability);return true;
    }
    public bool ConsumePassive(AbilityDefinition ability,int execution)
    {
        if(passiveExecution.TryGetValue(ability,out int previous)&&previous==execution||!Requirements(ability))return false;
        passiveExecution[ability]=execution;Consume(ability);return true;
    }
    void Hit(AttackHitResult hit){foreach(var ability in unlocked.Values)ability.OnAttackHit(this,hit);}
}
