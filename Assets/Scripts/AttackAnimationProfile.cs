using UnityEngine;

// Authored key poses in Qori's canonical cutout-rig coordinates, independent of gameplay timing.
[CreateAssetMenu(menuName="Qolossal/Combat/Attack Animation")]
public sealed class AttackAnimationProfile : ScriptableObject
{
    [System.Serializable] public struct Pose
    {
        public Vector2 leftGrip;
        public Vector2 shoulderReach;
        public Vector2 freeHand;
        public float weaponAngle,weaponYaw,weaponRoll,torsoAngle,compression,kneeBend;
        public static Pose Blend(Pose a,Pose b,float t)=>new Pose {
            leftGrip=Vector2.Lerp(a.leftGrip,b.leftGrip,t),weaponAngle=Mathf.LerpAngle(a.weaponAngle,b.weaponAngle,t),
            weaponYaw=Mathf.Lerp(a.weaponYaw,b.weaponYaw,t),
            weaponRoll=Mathf.Lerp(a.weaponRoll,b.weaponRoll,t),
            shoulderReach=Vector2.Lerp(a.shoulderReach,b.shoulderReach,t),
            freeHand=Vector2.Lerp(a.freeHand,b.freeHand,t),
            torsoAngle=Mathf.LerpAngle(a.torsoAngle,b.torsoAngle,t),compression=Mathf.Lerp(a.compression,b.compression,t),kneeBend=Mathf.Lerp(a.kneeBend,b.kneeBend,t)};
    }
    public Pose anticipation,contact,followThrough;
    public bool straightTrail;
    public bool oneHanded;
    public bool sweepThroughActive;
    public bool depthSweep;
    // Optional whole-swing timing avoids easing to a stop at the contact pose.
    public AnimationCurve swingTiming;
    [Range(.2f,.8f)] public float contactFraction=.5f;
    [Range(0,.5f)] public float recoveryHold=.2f;
    [Range(0,1)] public float secondaryImpulse=.3f;
    public Pose Sample(AttackPhase phase,float progress)
    {
        if(phase==AttackPhase.Startup)return anticipation;
        if(phase==AttackPhase.Active)
        {
            float contactAt=Mathf.Clamp(contactFraction,.2f,.8f);
            if(sweepThroughActive && swingTiming!=null && swingTiming.length>1)
            {
                float t=Mathf.Clamp01(swingTiming.Evaluate(progress));
                return t<=contactAt?Pose.Blend(anticipation,contact,t/contactAt):
                    Pose.Blend(contact,followThrough,(t-contactAt)/(1-contactAt));
            }
            if(progress<=contactAt)return Pose.Blend(anticipation,contact,Mathf.SmoothStep(0,1,progress/contactAt));
            return sweepThroughActive?Pose.Blend(contact,followThrough,Mathf.SmoothStep(0,1,(progress-contactAt)/(1-contactAt))):contact;
        }
        if(sweepThroughActive)return followThrough;
        return Pose.Blend(contact,followThrough,Mathf.SmoothStep(0,1,Mathf.Clamp01(progress*2)));
    }
}
