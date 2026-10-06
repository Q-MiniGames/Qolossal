using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Optional consumers can attach camera/impact reactions without entering hit detection.
[DisallowMultipleComponent]
public sealed class PlayerCombatFeedback : MonoBehaviour
{
    public event Action<Vector2,float> OnCameraImpulse;
    PlayerCombat combat;AudioSource audioSource;float rumbleUntil;Gamepad vibratingPad;int stoppedExecution=-1;
    void Awake(){combat=GetComponent<PlayerCombat>();audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;}
    void Start()
    {
        var camera=Camera.main;
        if(camera!=null&&camera.GetComponent<CameraFollow>()!=null)
        {
            var shake=camera.GetComponent<CombatCameraShake>();if(shake==null)shake=camera.gameObject.AddComponent<CombatCameraShake>();shake.Initialize(this);
        }
    }
    void OnEnable(){combat.OnAttackStarted+=Started;combat.OnAttackHit+=Hit;}
    void OnDisable(){combat.OnAttackStarted-=Started;combat.OnAttackHit-=Hit;StopRumble();}
    // An attack's own clip wins; otherwise the library's sound for the weapon in hand.
    void Started(AttackDefinition attack)
    {
        if(attack.startSound!=null){audioSource.PlayOneShot(attack.startSound,.3f);return;}
        if(attack.slingProjectile)return;   // QoriResinShot sounds the release itself
        switch(WeaponKinds.OfWeapon(combat.EquippedWeapon))
        {
            case WeaponKind.Mace:Sfx.Play("Weapon_Mace_Swing");break;
            case WeaponKind.Spear:Sfx.Play("Weapon_Spear_Thrust");break;
            case WeaponKind.Sword:Sfx.Play("Weapon_Reedblade_Swing");break;
            default:Sfx.Play("Weapon_Staff_Swing");break;
        }
    }
    static string HitSound(WeaponKind kind)=>kind==WeaponKind.Mace?"Weapon_Mace_Hit":kind==WeaponKind.Spear?"Weapon_Spear_Hit":kind==WeaponKind.Sword?"Weapon_Reedblade_Hit":kind==WeaponKind.Sling?"Weapon_Sling_Impact":"Weapon_Staff_Hit";
    void Hit(AttackHitResult hit)
    {
        Vector2 at=hit.Hit.Point;
        if(hit.Response.Disposition==CombatHitDisposition.Blocked)
        {
            Sfx.Play(hit.Target!=null&&hit.Target.GetComponentInParent<SentinelEnemy>()!=null?"Sentinel_ShieldBlock":"Combat_Blocked",at);
            return;
        }
        if(hit.Response.Disposition!=CombatHitDisposition.Damaged)return;
        var attack=hit.Hit.Attack;
        var kind=WeaponKinds.Of(hit.Hit);
        if(hit.CanPogo)Sfx.Play("Combat_Pogo_Bounce",at);
        else if(attack.hitSound==null)Sfx.Play(HitSound(kind),at);
        if(kind==WeaponKind.Mace&&hit.Response.Damage>0f)Sfx.Play("Combat_HitStop_Heavy",at);
        if(stoppedExecution!=hit.ExecutionId){stoppedExecution=hit.ExecutionId;combat.RequestHitStop(attack.hitStop);}
        if(hit.Response.Killed)HitStop.Freeze(HitStop.EnemyKilled);
        else if(WeaponKinds.Of(hit.Hit)==WeaponKind.Mace)HitStop.Freeze(HitStop.HeavyHit);
        if(attack.hitSound!=null)audioSource.PlayOneShot(attack.hitSound,.4f);
        if(attack.impactEffect!=null)Destroy(Instantiate(attack.impactEffect,hit.Hit.Point,Quaternion.identity),2);
        OnCameraImpulse?.Invoke(hit.Hit.Point,attack.cameraShake);
        if(Gamepad.current!=null&&attack.rumble>0){vibratingPad=Gamepad.current;vibratingPad.SetMotorSpeeds(attack.rumble,attack.rumble*.5f);rumbleUntil=Time.unscaledTime+.06f;}
    }
    void StopRumble(){if(vibratingPad!=null)vibratingPad.SetMotorSpeeds(0,0);vibratingPad=null;}
    void Update(){if(Time.unscaledTime>=rumbleUntil||GamePauseMenu.BlocksGameplayInput||Time.timeScale<=0)StopRumble();}
    void OnApplicationFocus(bool focus){if(!focus)StopRumble();}
}
