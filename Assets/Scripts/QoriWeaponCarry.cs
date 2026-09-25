using UnityEngine;
using System.Collections.Generic;

// One rigid weapon/grip transform, with two fixed-length arms reaching its handles.
public sealed class QoriWeaponCarry
{
    static Vector2 P(float x,float y)=>new Vector2((x-591)/100,(665-y)/100);
    static readonly Vector2[] shoulders={P(624,578),P(717,591)}, elbows={P(588,697),P(756,747)}, hands={P(647,824),P(817,850)};
    readonly Vector2[] posedElbows=new Vector2[2],posedHands=new Vector2[2];
    readonly Vector2[] posedShoulders=new Vector2[2];
    readonly Vector2[] upperRotation=new Vector2[2],lowerRotation=new Vector2[2];
    Vector2 weaponRotation=Vector2.right;
    Vector2 weaponOffset;
    struct Binding { public float left,right,leftJoint,rightJoint,weapon; }
    readonly Dictionary<Vector2,Binding> bindings=new Dictionary<Vector2,Binding>();
    float lift,angle,liftVelocity,angleVelocity;
    int frame=-1;
    public float OneHandWeight {get;private set;}
    public Vector2 Grip=>Vector2.Lerp(Weapon(hands[1]),Weapon(hands[0]),OneHandWeight);
    public static float FreePalmMask(Vector2 p)=>1-Smooth(.30f,.43f,Vector2.Distance(p,hands[1]));
    public static float CleanGripShaft(Vector2 p)=>Stroke(p,P(760,840),P(880,859),.055f);
    public void Step(float targetLift,float targetAngle,Vector2 bridgeGrip=default(Vector2),float bridgeAngle=0f,float bridgeWeight=0f,Vector2 shoulderReach=default(Vector2),float oneHandWeight=0,Vector2 freeHand=default(Vector2))
    {
        if(frame==Time.frameCount)return;frame=Time.frameCount;
        OneHandWeight=Mathf.Clamp01(oneHandWeight);
        lift=Mathf.SmoothDamp(lift,targetLift,ref liftVelocity,.12f,Mathf.Infinity,Time.deltaTime);
        angle=Mathf.SmoothDampAngle(angle,targetAngle,ref angleVelocity,.12f,Mathf.Infinity,Time.deltaTime);
        weaponRotation=Rotation(Mathf.LerpAngle(angle,bridgeAngle,bridgeWeight));
        weaponOffset=new Vector2(-lift*.12f,lift);
        weaponOffset+= (bridgeGrip-Weapon(hands[0]))*bridgeWeight;
        for(int i=0;i<2;i++)posedShoulders[i]=shoulders[i]+shoulderReach;
        // Project the rigid handle pair into both arms' reachable disks. This
        // retains fixed bone lengths without separating either palm from the shaft.
        for(int pass=0;pass<(bridgeWeight>0?8:0);pass++)for(int i=0;i<2;i++)
        {
            Vector2 delta=Weapon(hands[i])-posedShoulders[i];
            float upper=Vector2.Distance(shoulders[i],elbows[i]),lower=Vector2.Distance(elbows[i],hands[i]);
            float reach=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper-lower)+.01f,upper+lower-.01f);
            if(delta.sqrMagnitude>.000001f)weaponOffset+=(delta.normalized*reach-delta)*(i==1?1-OneHandWeight:1);
        }
        for(int i=0;i<2;i++)
        {
            posedHands[i]=Weapon(hands[i]);
            if(i==1)posedHands[i]=Vector2.Lerp(posedHands[i],freeHand,OneHandWeight);
            float upper=Vector2.Distance(shoulders[i],elbows[i]),lower=Vector2.Distance(elbows[i],hands[i]);
            Vector2 delta=posedHands[i]-posedShoulders[i];float length=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper-lower)+.01f,upper+lower-.01f);
            Vector2 axis=delta.normalized;
            if(i==1&&OneHandWeight>0)posedHands[i]=posedShoulders[i]+axis*length;
            float along=(upper*upper-lower*lower+length*length)/(2*Mathf.Max(.001f,length));
            float bend=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            // Both authored elbows bend outward on the left of their reach axis.
            posedElbows[i]=posedShoulders[i]+axis*along-new Vector2(-axis.y,axis.x)*bend;
            upperRotation[i]=Rotation(Vector2.SignedAngle(elbows[i]-shoulders[i],posedElbows[i]-posedShoulders[i]));
            lowerRotation[i]=Rotation(Vector2.SignedAngle(hands[i]-elbows[i],posedHands[i]-posedElbows[i]));
        }
    }
    static Vector2 Rotation(float degrees){float r=degrees*Mathf.Deg2Rad;return new Vector2(Mathf.Cos(r),Mathf.Sin(r));}
    static Vector2 Rotate(Vector2 p,Vector2 r)=>new Vector2(p.x*r.x-p.y*r.y,p.x*r.y+p.y*r.x);
    Vector2 Weapon(Vector2 p)=>hands[1]+weaponOffset+Rotate(p-hands[1],weaponRotation);
    public Vector2 TransformWeapon(Vector2 p)=>Weapon(p);
    public Vector2 TransformFreePalm(Vector2 p)=>posedHands[1]+Rotate(p-hands[1],lowerRotation[1]);
    public static float WeaponMask(Vector2 p)
    {
        float shaft=Stroke(p,P(465,798),P(953,868),.19f);
        float blade=Mathf.Max(Stroke(p,P(953,867),P(1088,886),.48f),Stroke(p,P(1088,886),P(1169,933),.22f));
        float grip=Mathf.Max(1-Smooth(.29f,.37f,Vector2.Distance(p,hands[0])),1-Smooth(.29f,.37f,Vector2.Distance(p,hands[1])));
        return Mathf.Max(Mathf.Max(shaft,blade),grip);
    }
    static float Smooth(float a,float b,float v)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));
    static float Stroke(Vector2 p,Vector2 a,Vector2 b,float width)
    {Vector2 d=b-a;return 1-Smooth(width,width+.09f,Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude)));}
    public Vector2 Skin(Vector2 p)
    {
        if(!bindings.TryGetValue(p,out Binding binding))
        {
        float shaft=Stroke(p,P(465,798),P(953,868),.19f);
        float blade=Mathf.Max(Stroke(p,P(953,867),P(1088,886),.48f),Stroke(p,P(1088,886),P(1169,933),.22f));
        float grip=Mathf.Max(1-Smooth(.29f,.37f,Vector2.Distance(p,hands[0])),1-Smooth(.29f,.37f,Vector2.Distance(p,hands[1])));
            binding=new Binding {
                left=Mathf.Max(Stroke(p,shoulders[0],elbows[0],.24f),Stroke(p,elbows[0],hands[0],.27f)),
                right=Mathf.Max(Stroke(p,shoulders[1],elbows[1],.24f),Stroke(p,elbows[1],hands[1],.27f)),
                leftJoint=Smooth(elbows[0].y+.10f,elbows[0].y-.10f,p.y),rightJoint=Smooth(elbows[1].y+.10f,elbows[1].y-.10f,p.y),
                weapon=Mathf.Max(Mathf.Max(shaft,blade),grip)
            };
            bindings.Add(p,binding);
        }
        Vector2 result=p;
        for(int i=0;i<2;i++)
        {
            float region=i==0?binding.left:binding.right;
            if(region<=0)continue;
            Vector2 upper=posedShoulders[i]+Rotate(p-shoulders[i],upperRotation[i]);
            Vector2 lower=posedElbows[i]+Rotate(p-elbows[i],lowerRotation[i]);
            float joint=i==0?binding.leftJoint:binding.rightJoint;
            result=Vector2.Lerp(result,Vector2.Lerp(upper,lower,joint),region);
        }
        float palm=FreePalmMask(p)*OneHandWeight;
        result=Vector2.Lerp(result,posedHands[1]+Rotate(p-hands[1],lowerRotation[1]),palm);
        return Vector2.Lerp(result,Weapon(p),binding.weapon*(1-palm));
    }
}
