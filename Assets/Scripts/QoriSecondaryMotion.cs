using System;
using UnityEngine;

public sealed class QoriSecondaryMotion
{
    [Serializable] public sealed class Tuning
    {
        public float earStiffness=65f, earDamping=12f;
        public float gripStiffness=110f, gripDamping=18f;
        public float breathSeconds=2.8f, attentionInterval=8f;
        [Range(0f,3f)] public float earWalkSway=1.5f, earRunSway=2.2f;
    }
    public Tuning tuning=new Tuning();
    public float EarAngle { get; private set; }
    public float GripOffset { get; private set; }
    public float ChestRise { get; private set; }
    public float AttentionAngle { get; private set; }
    public float Blink { get; private set; }
    public float CloakKick { get; private set; }
    float earVelocity,gripVelocity,idleTime,blinkClock,previousSpeed;
    float earPhase,earMotionBlend,previousFacing;
    int reset=-1,launch=-1,landing=-1;
    bool attached;
    int attackVersion=-1;
    static void Spring(ref float value,ref float velocity,float target,float stiffness,float damping,float dt)
    {
        velocity+=((target-value)*stiffness-velocity*damping)*dt;
        value+=velocity*dt;
    }
    public void Step(PlayerMovement movement,PlayerThread thread,bool attacking,QoriSwingAnimation swing=null)
    {
        if(movement==null) return;
        float dt=Mathf.Min(Time.deltaTime,.05f);
        CloakKick=0;
        if(reset!=movement.ResetVersion)
        {
            reset=movement.ResetVersion; launch=movement.LaunchVersion; landing=movement.LandingVersion;
            EarAngle=GripOffset=0;earVelocity=gripVelocity=previousSpeed=idleTime=blinkClock=0;
            earPhase=earMotionBlend=previousFacing=0;
        }
        bool connected=thread!=null && thread.IsAttached;
        float swingWeight=swing!=null?Mathf.Clamp01(swing.Weight):0f;
        float facing=swingWeight>0f?swing.Facing:movement.FacingDirection;
        float forward=movement.ObservedVelocity.x*facing;
        // A change of visual facing is not physical acceleration. Capture the
        // current speed on attach as well, leaving the intentional catch impulse below.
        if(previousFacing!=facing || connected&&!attached)previousSpeed=forward;
        previousFacing=facing;
        float speedChange=Mathf.Clamp(forward-previousSpeed,-3,3);previousSpeed=forward;
        earVelocity-=speedChange*2f;gripVelocity-=speedChange*.08f;CloakKick-=speedChange*3f;
        bool launched=launch!=movement.LaunchVersion;
        if(launched)
        {
            launch=movement.LaunchVersion;
            float force=movement.LastLaunchKind==PlayerMovement.LaunchKind.FlowerBoost?1.6f:1f;
            earVelocity-=18f*force;gripVelocity-=.8f*force;CloakKick-=30f*force;
        }
        if(landing!=movement.LandingVersion)
        {
            landing=movement.LandingVersion;float force=launched?0:Mathf.InverseLerp(2,22,movement.LastLandingSpeed);
            earVelocity+=32f*force;gripVelocity+=1.4f*force;CloakKick+=45f*force;
        }
        if(connected!=attached){earVelocity+=connected?20f:-12f;CloakKick+=connected?25f:-18f;attached=connected;}
        var combat=movement.GetComponent<PlayerCombat>();
        if(combat!=null&&combat.Phase==AttackPhase.Active&&attackVersion!=combat.ExecutionId)
        {
            attackVersion=combat.ExecutionId;
            float strength=combat.CurrentAttack.animation!=null?combat.CurrentAttack.animation.secondaryImpulse:.2f;
            earVelocity+=(combat.IsUpwardAttack?-1:1)*strength*12;
            CloakKick+=(combat.IsDownwardAttack?-1:1)*strength*24;
        }
        bool idle=movement.IsGrounded && Mathf.Abs(movement.ObservedVelocity.x)<.1f && Mathf.Abs(movement.MoveInput)<.1f && !attacking && !connected;
        idleTime=idle?idleTime+dt:0;
        float idleBlend=Mathf.SmoothStep(0,1,Mathf.Clamp01(idleTime/.5f));
        ChestRise=(.5f-.5f*Mathf.Cos(idleTime*Mathf.PI*2/Mathf.Max(.5f,tuning.breathSeconds)))*.055f*idleBlend;
        AttentionAngle=0;
        if(idleTime>3)
        {
            float window=Mathf.Repeat(idleTime-3,Mathf.Max(4,tuning.attentionInterval));
            float envelope=window<2?Mathf.Sin(window*Mathf.PI/2):0;
            float attention=2f;
            float nearest=16f;
            foreach(ThreadAnchor anchor in ThreadAnchor.Active)
            {
                if(anchor==null)continue;
                Vector2 delta=anchor.transform.position-movement.transform.position;
                if(delta.sqrMagnitude<nearest){nearest=delta.sqrMagnitude;attention=Mathf.Clamp(delta.y*1.4f,-3f,4f);}
            }
            AttentionAngle=attention*envelope;
        }
        blinkClock+=dt;float blinkPhase=Mathf.Repeat(blinkClock,4.7f);
        Blink=idle && blinkPhase>4.5f?Mathf.Sin((blinkPhase-4.5f)/.2f*Mathf.PI):0;
        // A distance-driven, low-amplitude follow-through; fade the oscillation
        // at stops instead of snapping the ears back to their rest pose.
        float speed=Mathf.Abs(movement.ObservedVelocity.x);
        float runBlend=Mathf.InverseLerp(movement.WalkSpeed,movement.RunSpeed,speed);
        float motionTarget=movement.IsGrounded && !connected && !attacking ? Mathf.Clamp01(speed/2f) : 0f;
        earMotionBlend=Mathf.MoveTowards(earMotionBlend,motionTarget,dt*5f);
        earPhase=Mathf.Repeat(earPhase+speed*dt/Mathf.Lerp(4.2f,4.8f,runBlend)*Mathf.PI*2f,Mathf.PI*2f);
        float sway=Mathf.Sin(earPhase-.6f)*Mathf.Lerp(tuning.earWalkSway,tuning.earRunSway,runBlend)*earMotionBlend;
        float idleSway=Mathf.Sin(idleTime*1.6f)*.65f*idleBlend;
        float earTarget=-Mathf.Clamp(forward,-11,11)*.25f+Mathf.Clamp(movement.ObservedVelocity.y/12,-1,1)*1.2f+sway+idleSway;
        if(swing!=null)earTarget+=Mathf.Clamp(swing.SecondaryForce,-5f,5f)*swingWeight;
        float ear=EarAngle,grip=GripOffset,remaining=dt;
        while(remaining>0)
        {
            float step=Mathf.Min(remaining,1f/120f);
            Spring(ref ear,ref earVelocity,earTarget,tuning.earStiffness,tuning.earDamping,step);
            Spring(ref grip,ref gripVelocity,AttentionAngle*.015f,tuning.gripStiffness,tuning.gripDamping,step);
            remaining-=step;
        }
        EarAngle=Mathf.Clamp(ear,-7,7);GripOffset=Mathf.Clamp(grip,-.16f,.16f);
    }
}
