using UnityEngine;

[CreateAssetMenu(menuName="Qolossal/Combat/Attack")]
public sealed class AttackDefinition : ScriptableObject
{
    public string attackId="attack";
    public AttackAim direction;
    public AttackAnimationProfile animation;
    [Header("Timeline (seconds)")]
    [Min(.001f)] public float startup=.07f,active=.10f,recovery=.13f;
    [Min(0)] public float cooldown=.30f;
    public float TotalDuration=>Mathf.Max(.001f,startup)+Mathf.Max(.001f,active)+Mathf.Max(.001f,recovery);
    [Header("Availability")]
    public bool allowGround=true,allowAir=true;
    public string[] requiredAbilities=new string[0];
    [Header("Hit detection")]
    public CombatHitShape hitShape=CombatHitShape.WeaponCapsule;
    public Vector2 hitboxSize=new Vector2(.7f,.7f),hitboxOffset=new Vector2(.75f,0);
    [Min(.01f)] public float bladeRadius=.10f;
    [Range(0,1)] public float bladeStart=.25f;
    public LayerMask targetLayers=~0;
    public bool multipleTargets=true,repeatTarget=false,pogoCompatible=false;
    [Min(.02f)] public float repeatInterval=.12f;
    [Min(0)] public float damage=1;
    public Vector2 knockback=new Vector2(2f,1f);
    [Header("Movement (multipliers preserve existing momentum)")]
    [Range(0,2)] public float groundControl=1,airControl=1,gravity=1;
    public Vector2 impulse,recoil;
    [Tooltip("The impulse's forward push only when the stick is held toward the attack (no drifting forward on the spot).")] public bool impulseNeedsInput;
    public bool lockFacing=true;
    [Header("Cancel and combo rules")]
    public CombatCancel startupCancels=CombatCancel.Jump|CombatCancel.Grapple;
    public CombatCancel activeCancels=CombatCancel.Jump|CombatCancel.Grapple;
    public CombatCancel recoveryCancels=CombatCancel.Jump|CombatCancel.Grapple|CombatCancel.Ability|CombatCancel.Attack;
    [Range(0,1)] public float comboWindowStart=.45f;
    public AttackDefinition[] followUps=new AttackDefinition[0];
    [Tooltip("A plunge: above 0, Qori dives straight down at this speed (u/s) through the hit, until he lands or hits something. The wind-up hangs him in the air.")]
    [Min(0)] public float plungeSpeed;
    [Tooltip("Heavy enough to crack stone: breaks the Shellback's shell and rubble walls, like the mace did.")] public bool breaksArmor;
    [Tooltip("A whole-game freeze (seconds) when this attack lands, for weight; 0 = none.")] [Range(0,.2f)] public float hitFreeze;
    [Tooltip("Moving during the follow-through ends the attack at once (the combo can still continue).")] public bool moveCancelsRecovery;
    [Tooltip("The plunge's landing burst, started when the dive meets the ground.")] public AttackDefinition plungeLanding;
    [Tooltip("The plunge's bounce (u/s up) when the dive hits something.")] [Min(0)] public float plungeBounce=9;
    [Header("Feedback hooks")]
    [Range(0,.1f)] public float hitStop=.025f;
    [Range(0,1)] public float cameraShake=.025f,rumble=.1f;
    public AudioClip startSound,hitSound;
    public GameObject impactEffect;
    [Header("Ranged delivery")]
    public bool slingProjectile;
    public float projectileSpeed=16,projectileLifetime=1.2f;
    public CombatCancel Cancels(AttackPhase phase)=>phase==AttackPhase.Startup?startupCancels:phase==AttackPhase.Active?activeCancels:recoveryCancels;
}
