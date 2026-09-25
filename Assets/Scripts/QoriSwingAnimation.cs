using System;
using UnityEngine;

// Reads the pendulum; never writes gameplay velocity, transforms or constraints.
public sealed class QoriSwingAnimation : MonoBehaviour
{
    [Serializable] public sealed class Tuning
    {
        [Range(0,1)] public float ropeLeanStrength=.72f;
        [Range(5,45)] public float maximumLean=28f;
        [Min(.03f)] public float torsoLag=.16f,hipLag=.22f,legLag=.28f;
        [Range(0,20)] public float legFollowThrough=12f;
        [Range(0,1)] public float apexCompression=.32f;
        [Range(0,1)] public float tensionExtension=.22f;
        [Min(.05f)] public float attachSeconds=.20f,releaseSeconds=.26f;
        [Range(0,1)] public float secondaryStrength=.65f;
    }
    public Tuning tuning=new Tuning();
    public float Weight {get;private set;}
    public float Facing {get;private set;}=1;
    public float TorsoAngle {get;private set;}
    public float HipAngle {get;private set;}
    public float LegAngle {get;private set;}
    public float HeadAngle=>TorsoAngle-Mathf.Clamp(TorsoAngle*.18f,-5,5);
    public float SecondaryForce {get;private set;}
    public float Load {get;private set;}
    public float Compact {get;private set;}
    public float AttachBlend {get;private set;}
    public bool Releasing=>!thread.IsAttached&&Weight>0;
    public bool PreserveReleaseFacing=>!thread.IsAttached&&retainReleaseFacing;
    PlayerMovement movement;PlayerThread thread;Rigidbody2D body;
    int version=-1,reset=-1;
    int visualReset=-1;
    bool releaseValid,retainReleaseFacing;
    Vector2 priorAnchor;
    float omega,acceleration,speed,climb,torsoVelocity,hipVelocity,legVelocity,forceVelocity;
    float priorOmega;bool sampled,wasAttached;
    void Awake(){movement=GetComponentInParent<PlayerMovement>();thread=movement.GetComponent<PlayerThread>();body=movement.GetComponent<Rigidbody2D>();}
    void FixedUpdate()
    {
        if(!thread.IsAttached){sampled=false;return;}
        Vector2 anchor=thread.AnchorPosition;
        bool fresh=!sampled||version!=thread.AttachmentVersion||reset!=movement.ResetVersion;
        Vector2 anchorVelocity=fresh?Vector2.zero:(anchor-priorAnchor)/Time.fixedDeltaTime;
        Vector2 r=body.position-anchor,velocity=body.linearVelocity-anchorVelocity;
        float next=Mathf.Clamp((r.x*velocity.y-r.y*velocity.x)/Mathf.Max(.25f,r.sqrMagnitude)*Mathf.Rad2Deg,-360,360);
        float alpha=fresh?0:Mathf.Clamp((next-priorOmega)/Time.fixedDeltaTime,-600,600);
        float blend=1-Mathf.Exp(-Time.fixedDeltaTime*12);
        omega=Mathf.Lerp(omega,next,blend);acceleration=Mathf.Lerp(acceleration,alpha,blend);
        speed=Mathf.Abs(omega*Mathf.Deg2Rad)*r.magnitude;
        climb=Mathf.Clamp(velocity.y/6,-1,1);
        priorAnchor=anchor;priorOmega=next;version=thread.AttachmentVersion;reset=movement.ResetVersion;sampled=true;
    }
    public void Step(float facing)
    {
        float dt=Time.deltaTime;
        // SmoothDamp may divide by zero at an already-settled target while paused.
        if(dt<=0)return;
        if(visualReset!=movement.ResetVersion)
        {
            visualReset=movement.ResetVersion;sampled=wasAttached=releaseValid=retainReleaseFacing=false;
            omega=acceleration=speed=climb=priorOmega=torsoVelocity=hipVelocity=legVelocity=forceVelocity=0;
            TorsoAngle=HipAngle=LegAngle=SecondaryForce=Load=Compact=Weight=AttachBlend=0;Facing=facing;
        }
        bool attached=thread.IsAttached;
        if(wasAttached&&!attached)releaseValid=retainReleaseFacing=true;
        if(attached)releaseValid=retainReleaseFacing=false;
        if(attached&&!wasAttached)Facing=facing;
        AttachBlend=attached?Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-thread.AttachedAt)/tuning.attachSeconds)):0;
        Weight=attached?AttachBlend:releaseValid?Mathf.Clamp01(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-thread.ReleasedAt)/tuning.releaseSeconds))):0;
        if(float.IsInfinity(thread.ReleasedAt)&&!attached)Weight=0;
        // Old pumping input may leave gameplay facing opposite the held pose.
        // Keep that pose through neutral freefall; new input or contact takes over.
        if(!attached&&Weight<=0&&(movement.HasGroundContact||Mathf.Abs(movement.MoveInput)>.1f))retainReleaseFacing=false;
        Vector2 rope=thread.AnchorPosition-(Vector2)movement.transform.position;
        float angle=attached?Vector2.SignedAngle(Vector2.up,rope)*Facing:0;
        float pumping=movement.MoveInput*Facing;
        float lean=angle*tuning.ropeLeanStrength-omega*Facing*.025f+pumping*2;
        if(movement.HasGroundContact)lean=0;
        float target=attached?Mathf.Clamp(lean,-tuning.maximumLean,tuning.maximumLean):TorsoAngle;
        float torso=TorsoAngle;TorsoAngle=Mathf.SmoothDampAngle(torso,target,ref torsoVelocity,tuning.torsoLag,Mathf.Infinity,dt);
        float hip=HipAngle;HipAngle=Mathf.SmoothDampAngle(hip,TorsoAngle-acceleration*Facing*.006f,ref hipVelocity,tuning.hipLag,Mathf.Infinity,dt);
        float trail=-Mathf.Clamp(omega/110,-1,1)*Facing*tuning.legFollowThrough;
        float leg=LegAngle;LegAngle=Mathf.SmoothDampAngle(leg,trail+pumping*2,ref legVelocity,tuning.legLag,Mathf.Infinity,dt);
        float apex=(1-Mathf.Clamp01(speed/5))*Mathf.Clamp01(Mathf.Abs(angle)/18);
        float blend=1-Mathf.Exp(-dt*8);
        Compact=Mathf.Lerp(Compact,Mathf.Clamp01(apex+Mathf.Max(0,climb)*.35f),blend);
        Load=Mathf.Lerp(Load,attached&&thread.IsTaut?Mathf.Clamp01(.55f+speed/16):.15f,blend);
        float force=SecondaryForce;SecondaryForce=Mathf.SmoothDamp(force,Mathf.Clamp(-acceleration*Facing*.018f,-5,5)*tuning.secondaryStrength,ref forceVelocity,.3f,Mathf.Infinity,dt);
        wasAttached=attached;
    }
}
