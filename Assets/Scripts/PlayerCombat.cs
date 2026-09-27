using System;
using UnityEngine;

[DefaultExecutionOrder(20)]
[DisallowMultipleComponent,RequireComponent(typeof(PlayerMovement))]
public sealed class PlayerCombat : MonoBehaviour
{
    [SerializeField] WeaponDefinition equippedWeapon;
    [SerializeField,Min(0)] float attackBufferSeconds=.12f;
    [SerializeField] bool showDebugHitbox;
    PlayerMovement movement;PlayerThread thread;PlayerAbilityController abilities;QoriBodyRig rig;QoriAnimator qoriRig;
    readonly AttackExecution execution=new AttackExecution();readonly CombatHitDetector hitDetector=new CombatHitDetector();
    AttackRequest queuedRequest;AttackDefinition queuedDefinition;
    AttackDefinition comboPrevious;
    float comboUntil,comboFacing;
    bool queuedCanChain;
    const float ComboGraceSeconds=.35f;
    static AttackDefinition Follow(AttackDefinition previous,AttackAim aim)
    {
        if(previous==null || previous.followUps==null)return null;
        foreach(var follow in previous.followUps)if(follow!=null&&follow.direction==aim)return follow;
        return null;
    }
    bool queued,running;float queuedUntil,nextAttack,hitStopRemaining,swipeDirection=1;
    int resetVersion,launchVersion,threadVersion;
    public event Action<AttackDefinition> OnAttackStarted,OnAttackActive,OnAttackFinished,OnAttackInterrupted;
    public event Action<AttackHitResult> OnAttackHit;
    public WeaponDefinition EquippedWeapon=>equippedWeapon;
    public WeaponDefinition PresentationWeapon=>CurrentAttack!=null&&CurrentAttack.slingProjectile&&armory!=null?armory.SlingWeapon:equippedWeapon;
    QoriArmory armory; bool projectileFired;
    public AttackDefinition CurrentAttack=>running?execution.Definition:null;
    public int ExecutionId {get;private set;}
    public bool IsAttackPoseActive=>isActiveAndEnabled&&running;
    public bool IsAttackWindup=>running&&execution.Phase==AttackPhase.Startup;
    public float WindupProgress=>IsAttackWindup?execution.Progress:1;
    public float StrikeProgress=>!running||execution.Phase==AttackPhase.Startup?-1:execution.Phase==AttackPhase.Active?execution.Progress:1+execution.Progress;
    public float AttackRecoveryProgress=>running&&execution.Phase==AttackPhase.Recovery?execution.Progress:0;
    public float AttackDirection=>swipeDirection;
    public bool IsUpwardAttack=>execution.Definition!=null&&execution.Definition.direction==AttackAim.Up;
    public bool IsDownwardAttack=>execution.Definition!=null&&execution.Definition.direction==AttackAim.Down;
    public AttackPhase Phase=>running?execution.Phase:AttackPhase.None;
    public float PhaseProgress=>running?execution.Progress:0;
    public float AttackElapsed=>execution.Elapsed;
    public bool IsHitStopped=>hitStopRemaining>0;
    public bool HasBufferedAttack=>queued;
    public bool ComboWindowOpen=>running&&Phase==AttackPhase.Recovery&&PhaseProgress>=CurrentAttack.comboWindowStart;
    public float GroundControl=>CurrentAttack!=null?CurrentAttack.groundControl:1;
    public float AirControl=>CurrentAttack!=null?CurrentAttack.airControl:1;
    public float GravityMultiplier=>CurrentAttack!=null?CurrentAttack.gravity:1;
    void Awake()
    {
        movement=GetComponent<PlayerMovement>();thread=GetComponent<PlayerThread>();
        if(equippedWeapon==null)equippedWeapon=Resources.Load<WeaponDefinition>("Combat/QoriReedblade");
        abilities=GetComponent<PlayerAbilityController>();if(abilities==null)abilities=gameObject.AddComponent<PlayerAbilityController>();
        if(GetComponent<PlayerCombatInput>()==null)gameObject.AddComponent<PlayerCombatInput>();
        if(GetComponent<PlayerCombatFeedback>()==null)gameObject.AddComponent<PlayerCombatFeedback>();
        gameObject.AddComponent<QoriSlashTrail>().Initialize(this);
        armory=GetComponent<QoriArmory>();if(armory==null)armory=gameObject.AddComponent<QoriArmory>();
        resetVersion=movement.ResetVersion;launchVersion=movement.LaunchVersion;
    }
    public void EquipWeapon(WeaponDefinition weapon){if(weapon==null)return;queued=false;Interrupt(false);equippedWeapon=weapon;}
    public bool BeginSlingAim(AttackDefinition move,Vector2 direction)
    {
        if(!isActiveAndEnabled||GamePauseMenu.BlocksGameplayInput||running||Time.time<nextAttack||!CanUse(move))return false;
        Begin(move,new AttackRequest(AttackAim.Front,Mathf.Abs(direction.x)>.05f?Mathf.Sign(direction.x):movement.FacingDirection,CombatMoveSlot.Special));
        return true;
    }
    public void ReleaseSlingAim()
    {
        if(CurrentAttack==null||!CurrentAttack.slingProjectile)return;
        nextAttack=Time.time+Mathf.Max(CurrentAttack.cooldown,CurrentAttack.TotalDuration-execution.Elapsed);
    }
    public bool TryStartAbilityAttack(AttackDefinition move)
    {
        if(!CanUse(move)||!CanCancel(CombatCancel.Ability))return false;
        TryCancel(CombatCancel.Ability);Begin(move,new AttackRequest(move.direction,movement.FacingDirection,CombatMoveSlot.Special));return true;
    }
    public bool RequestAttack(AttackRequest request,AttackDefinition overrideAttack=null)
    {
        if(!isActiveAndEnabled||GamePauseMenu.BlocksGameplayInput||!movement.isActiveAndEnabled)return false;
        var move=overrideAttack!=null?overrideAttack:equippedWeapon!=null&&equippedWeapon.moveSet!=null?equippedWeapon.moveSet.Select(request,!movement.IsGrounded):null;
        if(move==null)return false;
        queuedCanChain=overrideAttack==null && request.Slot!=CombatMoveSlot.Special && request.Slot!=CombatMoveSlot.Dash && request.Slot!=CombatMoveSlot.Charged;
        if(queuedCanChain && !running && Time.time<=comboUntil && request.Facing==comboFacing)
            move=Follow(comboPrevious,request.Aim)??move;
        queuedRequest=request;queuedDefinition=move;queued=true;
        // One press can reserve one follow-up; additional presses never enqueue a burst.
        bool reserve=queuedCanChain&&running&&request.Facing==swipeDirection&&Follow(CurrentAttack,request.Aim)!=null;
        queuedUntil=Time.time+(reserve?Mathf.Max(attackBufferSeconds,CurrentAttack.TotalDuration-execution.Elapsed+attackBufferSeconds):attackBufferSeconds);
        return true;
    }
    public bool CanUse(AttackDefinition move)
    {
        if(move==null||move.animation==null||!movement.isActiveAndEnabled||movement.IsLedgeHanging||movement.IsLedgeClimbing||movement.IsWallSliding)return false;
        // The current weapon rig needs both hands. Attached attacks require a
        // dedicated one-hand animation adapter; never deal damage from a hidden pose.
        if(thread!=null&&thread.IsAttached)return false;
        if(movement.IsGrounded?!move.allowGround:!move.allowAir)return false;
        foreach(string id in move.requiredAbilities)if(abilities==null||!abilities.HasAbility(id))return false;
        return true;
    }
    public bool CanCancel(CombatCancel action)=>!running||(CurrentAttack.Cancels(Phase)&action)!=0;
    public bool TryCancel(CombatCancel action){if(!CanCancel(action))return false;if(running)Interrupt(false);return true;}
    public void CancelAttack(){queued=false;Interrupt(true);}
    void Interrupt(bool resetCooldown)
    {
        var old=CurrentAttack;running=false;queued=false;comboPrevious=null;comboUntil=0;hitStopRemaining=0;hitDetector.Reset();
        if(resetCooldown)nextAttack=Time.time;
        if(old!=null)OnAttackInterrupted?.Invoke(old);
    }
    void Begin(AttackDefinition move,AttackRequest request)
    {
        queued=false;queuedDefinition=null;running=true;projectileFired=false;execution.Begin(move);ExecutionId++;
        comboPrevious=null;comboUntil=0;
        swipeDirection=request.Facing;hitDetector.Reset();hitStopRemaining=0;
        nextAttack=Time.time+Mathf.Max(move.cooldown,move.TotalDuration);
        launchVersion=movement.LaunchVersion;threadVersion=thread!=null?thread.AttachmentVersion:0;
        movement.AddCombatImpulse(new Vector2(move.impulse.x*swipeDirection,move.impulse.y));OnAttackStarted?.Invoke(move);
    }
    void Update()
    {
        if(!movement.isActiveAndEnabled||resetVersion!=movement.ResetVersion){resetVersion=movement.ResetVersion;CancelAttack();return;}
        if(GamePauseMenu.BlocksGameplayInput||Time.deltaTime<=0){queued=false;return;}
        if(running)
        {
            if(movement.LaunchVersion!=launchVersion&&CanCancel(CombatCancel.Jump)||thread!=null&&thread.AttachmentVersion!=threadVersion&&thread.IsAttached&&CanCancel(CombatCancel.Grapple))Interrupt(false);
            else
            {
                if(!CurrentAttack.lockFacing)swipeDirection=movement.FacingDirection;
                float dt=Time.deltaTime,consumed=Mathf.Min(dt,hitStopRemaining);hitStopRemaining-=consumed;dt-=consumed;
                if(CurrentAttack.slingProjectile&&armory!=null&&armory.IsAiming)
                {
                    if(!CanUse(CurrentAttack)){CancelAttack();return;}
                    if(Mathf.Abs(armory.SlingDirection.x)>.05f)swipeDirection=Mathf.Sign(armory.SlingDirection.x);
                    dt=Mathf.Min(dt,Mathf.Max(0,CurrentAttack.startup*.98f-execution.Elapsed));
                }
                var previous=Phase;execution.Advance(dt);
                if(previous==AttackPhase.Startup&&Phase!=AttackPhase.Startup)OnAttackActive?.Invoke(CurrentAttack);
                if(execution.Finished){var old=CurrentAttack;running=false;comboPrevious=old;comboFacing=swipeDirection;comboUntil=Time.time+ComboGraceSeconds;OnAttackFinished?.Invoke(old);}
            }
        }
        if(!queued)return;
        if(Time.time>queuedUntil){queued=false;return;}
        var next=queuedDefinition;
        if(running)
        {
            if(!ComboWindowOpen||!CanCancel(CombatCancel.Attack))return;
            next=queuedCanChain&&queuedRequest.Facing==swipeDirection?Follow(CurrentAttack,queuedRequest.Aim):null;
            if(next==null||!CanUse(next))return;
            var old=CurrentAttack;running=false;OnAttackFinished?.Invoke(old);
        }
        else
        {
            if(Time.time<nextAttack)return;
            if(queuedCanChain && Time.time<=comboUntil && queuedRequest.Facing==comboFacing)
                next=Follow(comboPrevious,queuedRequest.Aim)??next;
        }
        if(CanUse(next))Begin(next,queuedRequest);
    }
    void LateUpdate()
    {
        if(Phase!=AttackPhase.Active||IsHitStopped){hitDetector.ClearSweep();return;}
        if(!QoriPoseLookup.TryGetWeapon(this,ref qoriRig,ref rig,out Transform weaponMount,out Transform weaponTip,out _))return;
        if(CurrentAttack.slingProjectile)
        {
            if(!projectileFired && PhaseProgress>=CurrentAttack.animation.contactFraction){projectileFired=true;Vector2 origin=armory.Visual!=null?armory.Visual.SlingReleasePosition:(Vector2)weaponMount.position;QoriResinShot.Launch(this,origin,CurrentAttack,armory.SlingWeapon,armory.SlingDirection);}
            return;
        }
        hitDetector.Sample(this,weaponMount.position,weaponTip.position);
    }
    public void PublishHit(AttackHitResult result)
    {
        if(result.Response.Disposition==CombatHitDisposition.Damaged&&CurrentAttack!=null)movement.AddCombatImpulse(new Vector2(CurrentAttack.recoil.x*swipeDirection,CurrentAttack.recoil.y));
        OnAttackHit?.Invoke(result);
    }
    public void RequestHitStop(float seconds){if(running)hitStopRemaining=Mathf.Max(hitStopRemaining,Mathf.Clamp(seconds,0,.1f));}
    void OnDisable()=>CancelAttack();
    void OnDrawGizmosSelected()
    {
        if(!showDebugHitbox||!running)return;Gizmos.color=Phase==AttackPhase.Active?Color.red:Color.yellow;
        Gizmos.DrawSphere(transform.position,.025f);
        if(CurrentAttack.hitShape==CombatHitShape.WeaponCapsule)
        {
            Vector2 a=Vector2.Lerp(hitDetector.LastA,hitDetector.LastB,CurrentAttack.bladeStart),b=hitDetector.LastB;
            Vector2 axis=(b-a).normalized,normal=new Vector2(-axis.y,axis.x)*CurrentAttack.bladeRadius;
            Gizmos.DrawLine(a+normal,b+normal);Gizmos.DrawLine(a-normal,b-normal);
            Gizmos.DrawWireSphere(a,CurrentAttack.bladeRadius);Gizmos.DrawWireSphere(b,CurrentAttack.bladeRadius);
        }
        else
        {
            Vector2 o=CurrentAttack.hitboxOffset;o.x*=swipeDirection;
            if(CurrentAttack.hitShape==CombatHitShape.Circle)Gizmos.DrawWireSphere(transform.position+(Vector3)o,CurrentAttack.hitboxSize.x*.5f);
            else Gizmos.DrawWireCube(transform.position+(Vector3)o,CurrentAttack.hitboxSize);
        }
    }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void OnGUI(){if(showDebugHitbox)GUI.Box(new Rect(16,54,390,64),$"{(equippedWeapon!=null?equippedWeapon.displayName:"No weapon")} | {(CurrentAttack!=null?CurrentAttack.attackId:"Idle")}\n{Phase} {PhaseProgress:F2} | facing {swipeDirection} | buffer {queued} | combo {ComboWindowOpen}");}
#endif
}
