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
    void Started(AttackDefinition attack){if(attack.startSound!=null)audioSource.PlayOneShot(attack.startSound,.3f);}
    void Hit(AttackHitResult hit)
    {
        if(hit.Response.Disposition!=CombatHitDisposition.Damaged)return;
        var attack=hit.Hit.Attack;
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
