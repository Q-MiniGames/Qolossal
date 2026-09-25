using System;
using System.Collections.Generic;
using UnityEngine;

// A constrained skin for the approved idle painting. Hidden limb surfaces still require cutout art.
public sealed class QoriBodyRig : MonoBehaviour
{
    [Serializable] public sealed class GroundTuning
    {
        [Min(.5f)] public float cycleDistance = 4.2f;
        public float footLift = .065f, pelvisBounce = .022f;
        [Range(.5f,.65f)] public float supportFraction=.56f;
        public float startLean = 6f, stopLean = 8f;
        [Range(0f,15f)] public float travelLean=8.5f;
        [Range(8f,18f)] public float runLean=13f;
        [Min(.5f)] public float runCycleDistance=4.8f;
        [Min(.04f)] public float turnSeconds = .12f;
    }
    public GroundTuning ground = new GroundTuning();
    [Serializable] public sealed class AirTuning
    {
        public float takeoffSeconds=.10f, apexSpeed=1.3f, preparationSeconds=.16f;
        public float normalTuck=.19f, boostTuck=.26f;
        public float landingCompression=.13f, landingRecovery=.22f;
    }
    public AirTuning air = new AirTuning();
    public QoriSecondaryMotion Secondary { get; set; }
    public QoriSwingAnimation Swing {get;set;}
    Vector2 airPoseOffset;
    readonly Vector2[] lastHips=new Vector2[2],lastKnees=new Vector2[2],lastAnkles=new Vector2[2];
    readonly Vector2[] bridgeHips=new Vector2[2],bridgeKnees=new Vector2[2],bridgeAnkles=new Vector2[2];
    Vector2 lastNeck,lastPelvis,lastHand,bridgeNeck,bridgePelvis,bridgeHand;
    Vector2 lastWeaponGrip,lastWeaponTip,bridgeWeaponGrip,bridgeWeaponTip;
    Vector2 threadWeaponGrip,threadWeaponElbow,threadWeaponShoulder;
    float threadWeaponAngle;
    readonly QoriThreadWeaponArt threadWeaponArt=new QoriThreadWeaponArt();
    struct ThreadWeaponBinding {public float arm,joint,weapon,palm;}
    ThreadWeaponBinding[] threadWeaponBindings;
    readonly Dictionary<Vector2,ThreadWeaponBinding> threadWeaponPointBindings=new Dictionary<Vector2,ThreadWeaponBinding>();
    Vector2 threadWeaponUpperRotation,threadWeaponLowerRotation,threadWeaponRotation;
    bool hasPose;
    int bridgeAttach=-1,bridgeRelease=-1;
    int poseReset=-1;
    public bool Ready => rendererMesh != null;
    public bool CanShowThread => Ready && threadTexture!=null;
    public bool BodyActive { get; private set; }
    public bool GroundActive { get; private set; }
    public bool WallPoseActive { get; private set; }
    public float VisualFacing { get; private set; } = 1f;
    public string State { get; private set; } = "Idle";
    public float StepWave { get; private set; }
    public int FootstepVersion { get; private set; }
    public Vector3 FootstepPosition { get; private set; }
    public bool LeftPlanted => GroundActive && legs[0].stance;
    public bool RightPlanted => GroundActive && legs[1].stance;
    public Vector3 LeftPlantTarget => legs[0].plant;
    public Vector3 RightPlantTarget => legs[1].plant;
    public Vector2 Brooch => combatPose>=0 ? CombatSkin(combatPose==1?new Vector2(.24f,-.10f):new Vector2(-.40f,.64f),new Vector2(0,500)) : threadPose ? ThreadSkin(Pixel(790,540),new Vector2(790,540)) : Turn(Upper(Pixel(707,565)));
    public Vector3 ThreadGrip => transform.TransformPoint(Mirror(ledgeArmActive?ledgeHand:ThreadSkin(Pixel(865,145),new Vector2(865,145))));
    public Transform GetJoint(string name) => joints.TryGetValue(name, out Transform joint) ? joint : null;

    sealed class Leg
    {
        public Vector2 hip, knee, ankle, sole, posedHip, posedKnee, posedAnkle, posedSole;
        public Vector3 plant, worldFoot;
        public bool stance;
        public float upperLength, lowerLength;
    }
    readonly Leg[] legs = { new Leg(), new Leg() };
    readonly Leg[] threadLegs = { new Leg(), new Leg() };
    readonly QoriGroundCycle groundCycle=new QoriGroundCycle();
    readonly QoriGroundLegRenderer groundLegs=new QoriGroundLegRenderer();
    readonly QoriWeaponCarry weaponCarry=new QoriWeaponCarry();
    readonly QoriHeldWeaponArt heldWeaponArt=new QoriHeldWeaponArt();
    float[] heldWeaponMask;
    Vector2 PoseFreePalm(Vector2 p)=>Upper(weaponCarry.TransformFreePalm(p));
    Vector2 PoseHeldWeapon(Vector2 p)
    {
        if(threadPose)
        {
            Vector2 grip=ThreadSkin(Pixel(650,825),new Vector2(650,825)),tip=ThreadSkin(Pixel(975,1260),new Vector2(975,1260));
            return grip+Rotate(p-Pixel(647,824),Vector2.SignedAngle(Pixel(1180,943)-Pixel(647,824),tip-grip));
        }
        return Turn(Upper(weaponCarry.TransformWeapon(p)));
    }
    float combatLayerWeight,combatLayerTorso;
    int combatEntryVersion=-1;
    Vector2 combatEntryGrip,combatEntryTip;
    bool combatEntryValid;
    AttackAnimationProfile.Pose layeredCombatPose;
    public float CombatLayerWeight=>combatLayerWeight;
    readonly float[] footPhase=new float[2];
    float groundBlend,groundBlendVelocity,lastGroundFacing;
    float runBlend;
    readonly Dictionary<string,Transform> joints = new Dictionary<string,Transform>();
    static readonly string[] HipNames={"LeftHip","RightHip"},KneeNames={"LeftKnee","RightKnee"},FootNames={"LeftFoot","RightFoot"};
    readonly Vector2 pelvis = Pixel(668,790), neck = Pixel(681,490);
    Mesh mesh;
    MeshRenderer rendererMesh;
    Material material;
    SpriteRenderer eyelid;
    Transform rigRoot;
    Texture groundTexture,threadTexture;
    readonly Sprite[] combatSprites=new Sprite[2];
    readonly Vector3[][] combatRest=new Vector3[2][];
    readonly Vector2[][] combatPixels=new Vector2[2][];
    int combatPose=-1;
    float combatAngle;
    float upwardArmAngle;
    float upwardThrust, upwardLook;
    bool threadPose;
    float reachAngle,threadLoad,hipLag;
    float ledgeBodyLean;
    Vector2 threadBodyOffset;
    readonly QoriLedgeArm ledgeArm=new QoriLedgeArm();
    readonly QoriHeadArt headArt=new QoriHeadArt();
    readonly QoriTorsoArt torsoArt=new QoriTorsoArt();
    readonly float[][] torsoLimbMasks=new float[4][];
    public bool HasThreeQuarterTorso=>torsoArt.Ready;
    public bool HasThreeQuarterLegs=>groundLegs.HasThreeQuarterArt;
    readonly Leg[] combatLegs={new Leg(),new Leg()};
    PlayerCombat combatSource;
    public int HeadPose=>headArt.Ready?headArt.Pose:-1;
    Vector2 ledgeShoulder,ledgeElbow,ledgeHand;
    bool ledgeArmActive;
    Vector3[] rest, vertices;
    Color[] colors;
    Vector2[] pixelCoordinates;
    struct VertexBinding
    {
        public QoriSkinWeights.Legs legs;
        public float arm,chest,headBlend,ear,kneeLeft,kneeRight,footLeft,footRight;
        public Vector2 earRoot;
    }
    struct BoneMap
    {
        public Vector2 rest,posed,rotation;
        public BoneMap(Vector2 a,Vector2 b,Vector2 posedA,Vector2 posedB)
        {rest=a;posed=posedA;rotation=Rotation(Vector2.SignedAngle(b-a,posedB-posedA));}
        public Vector2 Apply(Vector2 p)=>posed+ApplyRotation(p-rest,rotation);
    }
    VertexBinding[] bindings;
    readonly BoneMap[] upperBones=new BoneMap[2],lowerBones=new BoneMap[2];
    Vector2 torsoRotation,headRotation,earRotation,posedNeck;
    Vector2 actionRotation,actionPivot;
    Vector3 previousPosition;
    float phase, previousSpeed, lean, leanVelocity, turnAge, desiredFacing = 1f;
    float pelvisY, torsoAngle, headAngle, turnWidth = 1f;
    bool tracking, turning;
    int resetVersion = -1;
    int landingVersion = -1;
    float landingAge=1f, landingStrength, airTuck, airTuckVelocity;
    float airFootTrail,airFootTrailVelocity,airLeanVelocity;
    Collider2D colliderBody;
    PlayerMovement movement;
    QoriAuthoredWalk authoredWalk;
    Rigidbody2D physicsBody;
    PlayerThread threadSource;
    PlayerHealth health;
    float HurtAngle => health!=null && Time.time-health.LastHitTime<.2f ? -health.LastHitDirection*VisualFacing*8f*Mathf.Sin(Mathf.Clamp01((Time.time-health.LastHitTime)/.2f)*Mathf.PI) : 0;
    float FloorY => movement.IsGrounded ? movement.GroundPoint.y : colliderBody.bounds.min.y;
    float RenderedSoleY
    {
        get
        {
            // Collider bounds are a physics snapshot; do not mix them with an
            // interpolated visual transform when building the airborne skeleton.
            if(colliderBody is BoxCollider2D box)
                return box.transform.TransformPoint(box.offset+Vector2.down*box.size.y*.5f).y;
            return colliderBody.bounds.min.y+movement.transform.position.y-physicsBody.position.y;
        }
    }
    static Vector2 Pixel(float x, float y) => new Vector2((x-591f)/100f,(665f-y)/100f);
    static float Smooth(float a, float b, float value) => Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,value));
    static Vector2 Rotate(Vector2 p, float angle)
    {
        float r = angle*Mathf.Deg2Rad, c=Mathf.Cos(r), s=Mathf.Sin(r);
        return new Vector2(c*p.x-s*p.y,s*p.x+c*p.y);
    }
    static Vector2 Rotation(float angle)=>new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
    static Vector2 ApplyRotation(Vector2 p,Vector2 r)=>new Vector2(r.x*p.x-r.y*p.y,r.y*p.x+r.x*p.y);
    Vector3 Mirror(Vector2 p) => new Vector3(p.x*VisualFacing,p.y,0);

    public void Initialize(SpriteRenderer source, PlayerMovement owner)
    {
        if(owner==null)return;
        movement=owner; colliderBody=owner.GetComponent<Collider2D>(); physicsBody=owner.GetComponent<Rigidbody2D>();threadSource=owner.GetComponent<PlayerThread>();
        authoredWalk=owner.GetComponent<QoriAuthoredWalk>();
        health=owner.GetComponent<PlayerHealth>();
        combatSource=owner.GetComponent<PlayerCombat>();
        Sprite sprite=Resources.Load<Sprite>("QoriCloakPrototype/Qori_IdleBody_v1");
        Shader shader=Shader.Find("Sprites/Default");
        if(sprite==null || sprite.packed || shader==null) return;
        string[] names={"Root","Pelvis","LeftHip","LeftKnee","LeftFoot","RightHip","RightKnee","RightFoot",
            "LeftShoulder","LeftElbow","LeftHand","RightShoulder","RightElbow","RightHand","Neck","Head","LeftEar","RightEar","CloakMount","WeaponMount","WeaponTip"};
        GameObject root=new GameObject("Qori anatomical rig"); root.transform.SetParent(transform,false);
        rigRoot=root.transform;
        foreach(string name in names)
        {
            Transform joint=new GameObject(name).transform; joint.SetParent(root.transform,false); joints.Add(name,joint);
        }
        legs[0].hip=Pixel(615,876); legs[0].knee=Pixel(520,1035); legs[0].ankle=Pixel(485,1165); legs[0].sole=Pixel(493,1276);
        legs[1].hip=Pixel(703,873); legs[1].knee=Pixel(710,1010); legs[1].ankle=Pixel(701,1152); legs[1].sole=Pixel(729,1259);
        foreach(Leg leg in legs) {leg.upperLength=Vector2.Distance(leg.hip,leg.knee); leg.lowerLength=Vector2.Distance(leg.knee,leg.ankle);}
        const int columns=96, rows=112;
        int count=(columns+1)*(rows+1);
        rest=new Vector3[count]; vertices=new Vector3[count]; colors=new Color[count]; pixelCoordinates=new Vector2[count];
        Vector2[] uv=new Vector2[count]; int[] triangles=new int[columns*rows*6]; Rect rect=sprite.rect;
        for(int y=0;y<=rows;y++) for(int x=0;x<=columns;x++)
        {
            int i=y*(columns+1)+x; float px=rect.width*x/columns, py=rect.height*y/rows;
            rest[i]=new Vector3((px-sprite.pivot.x)/sprite.pixelsPerUnit,(py-sprite.pivot.y)/sprite.pixelsPerUnit);
            pixelCoordinates[i]=new Vector2(px,rect.height-py);
            uv[i]=new Vector2((rect.x+px)/sprite.texture.width,(rect.y+py)/sprite.texture.height); colors[i]=Color.white;
            if(x==columns || y==rows) continue;
            int k=(y*columns+x)*6; triangles[k]=i; triangles[k+1]=i+columns+1; triangles[k+2]=i+1;
            triangles[k+3]=i+1; triangles[k+4]=i+columns+1; triangles[k+5]=i+columns+2;
        }
        bindings=new VertexBinding[count];
        for(int i=0;i<count;i++)
        {
            Vector2 p=rest[i],pixel=pixelCoordinates[i];
            bindings[i]=new VertexBinding {
                legs=QoriSkinWeights.Evaluate(pixel),arm=QoriSkinWeights.ArmWeight(pixel),
                chest=Smooth(pelvis.y,neck.y,p.y),headBlend=Smooth(460,500,pixel.y),
                ear=pixel.y<370?1-Smooth(545,660,pixel.x):0,
                earRoot=pixel.y<290?Pixel(653,290):Pixel(624,337),
                kneeLeft=Smooth(legs[0].knee.y+.065f,legs[0].knee.y-.065f,p.y),
                kneeRight=Smooth(legs[1].knee.y+.065f,legs[1].knee.y-.065f,p.y),
                footLeft=QoriSkinWeights.FootWeight(pixel,0),footRight=QoriSkinWeights.FootWeight(pixel,1)
            };
        }
        mesh=new Mesh{name="Qori bound anatomical skin"}; mesh.vertices=rest; mesh.uv=uv; mesh.triangles=triangles; mesh.colors=colors; mesh.MarkDynamic();
        GameObject display=new GameObject("Qori joint driven body"); display.transform.SetParent(transform,false);
        display.AddComponent<MeshFilter>().sharedMesh=mesh; rendererMesh=display.AddComponent<MeshRenderer>();
        material=new Material(shader){mainTexture=sprite.texture}; rendererMesh.sharedMaterial=material;
        groundLegs.Initialize(transform,rest,pixelCoordinates,uv,triangles,material,source);
        groundTexture=sprite.texture;
        for(int pose=0;pose<2;pose++)
        {
            Sprite action=Resources.Load<Sprite>("QoriCloakPrototype/"+(pose==0?"Qori_AttackBody_v1":"Qori_AttackWindupBody_v1"));
            combatSprites[pose]=action;if(action==null || action.packed)continue;
            combatRest[pose]=new Vector3[count];combatPixels[pose]=new Vector2[count];
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                int i=y*(columns+1)+x;float px=action.rect.width*x/columns,py=action.rect.height*y/rows;
                combatRest[pose][i]=new Vector3((px-action.pivot.x)/action.pixelsPerUnit,(py-action.pivot.y)/action.pixelsPerUnit);
                combatPixels[pose][i]=new Vector2(px,action.rect.height-py);
            }
        }
        Sprite rope=Resources.Load<Sprite>("QoriCloakPrototype/Qori_RopeBody_v1");
        if(rope!=null && rope.rect.size==sprite.rect.size && rope.pivot==sprite.pivot) threadTexture=rope.texture;
        threadLegs[0].hip=Pixel(719,875);threadLegs[0].knee=Pixel(662,1010);threadLegs[0].ankle=Pixel(583,1138);threadLegs[0].sole=Pixel(580,1260);
        threadLegs[1].hip=Pixel(790,861);threadLegs[1].knee=Pixel(748,993);threadLegs[1].ankle=Pixel(714,1130);threadLegs[1].sole=Pixel(700,1250);
        foreach(Leg leg in threadLegs){leg.upperLength=Vector2.Distance(leg.hip,leg.knee);leg.lowerLength=Vector2.Distance(leg.knee,leg.ankle);}
        rendererMesh.sortingLayerID=source.sortingLayerID; rendererMesh.sortingOrder=source.sortingOrder; rendererMesh.enabled=false;
        headArt.Initialize(transform,source);
        torsoArt.Initialize(transform,source);
        if(torsoArt.Ready)
        {
            rendererMesh.sortingOrder=source.sortingOrder+1;
            for(int pose=0;pose<4;pose++)
            {
                torsoLimbMasks[pose]=new float[count];
                Vector2[] pixels=pose<2?pixelCoordinates:combatPixels[pose-2];
                if(pixels==null)continue;
                for(int i=0;i<count;i++)torsoLimbMasks[pose][i]=QoriTorsoArt.Limbs(pixels[i],pose<2?-1:pose-2,pose==1);
            }
        }
        Sprite blink=Resources.Load<Sprite>("QoriRig/Qori_Eyelid_v1");
        if(blink!=null)
        {
            GameObject eye=new GameObject("Qori authored blink overlay");eye.transform.SetParent(transform,false);
            eyelid=eye.AddComponent<SpriteRenderer>();eyelid.sprite=blink;eyelid.sortingLayerID=source.sortingLayerID;
            eyelid.sortingOrder=source.sortingOrder+2;eyelid.enabled=false;
        }
    }

    public bool Prepare(bool active, float facing)
    {
        if(!Ready) return false;
        torsoAngle-=combatLayerTorso;combatLayerTorso=0;
        bool wasActive=GroundActive; GroundActive=BodyActive=active; threadPose=false;combatPose=-1;WallPoseActive=false;ledgeBodyLean=0;ledgeArmActive=false;threadBodyOffset=Vector2.zero;airPoseOffset=Vector2.zero;
        if(poseReset!=movement.ResetVersion){poseReset=movement.ResetVersion;hasPose=false;bridgeAttach=bridgeRelease=-1;combatLayerWeight=0;}
        material.mainTexture=groundTexture;
        if(!active) {rendererMesh.enabled=false;headArt.Hide();torsoArt.Hide();if(eyelid!=null)eyelid.enabled=false; tracking=false; return false;}
        float dt=Mathf.Max(Time.deltaTime,.0001f);
        Vector3 position=movement.transform.position;
        bool reset=!tracking || (position-previousPosition).sqrMagnitude>4f || resetVersion!=movement.ResetVersion;
        float travel=reset?0f:position.x-previousPosition.x;
        float speed=travel/dt;
        previousPosition=position; tracking=true; resetVersion=movement.ResetVersion;
        if(reset) {VisualFacing=facing; desiredFacing=facing; previousSpeed=0; lean=leanVelocity=0; turning=false; phase=0;}
        if(facing!=desiredFacing) {desiredFacing=facing; turnAge=0; turning=true;}
        if(turning)
        {
            turnAge+=dt; float t=Mathf.Clamp01(turnAge/ground.turnSeconds);
            turnWidth=1f-.28f*Mathf.Sin(t*Mathf.PI);
            if(t>=.5f) VisualFacing=desiredFacing;
            if(t>=1) turning=false;
        }
        else turnWidth=1f;
        float input=movement.MoveInput;
        float acceleration=(speed-previousSpeed)/dt;
        bool moving=Mathf.Abs(speed)>.08f;
        float runTarget=Mathf.InverseLerp(movement.WalkSpeed,movement.RunSpeed,Mathf.Abs(speed));
        runBlend=Mathf.MoveTowards(runBlend,runTarget,dt*6f);
        State=turning?"Turn": !moving?"Idle": Mathf.Abs(input)<.05f?"Stop":
            Mathf.Abs(acceleration)>8 && Mathf.Abs(speed)<5.5f?"Start":movement.IsRunning?"Run":"Walk";
        float targetLean=-VisualFacing*Mathf.Clamp(acceleration/65f,-1,1)*(State=="Stop"?ground.stopLean:ground.startLean);
        targetLean-=Mathf.Clamp(speed*VisualFacing/Mathf.Max(.1f,movement.WalkSpeed),-1,1)*Mathf.Lerp(ground.travelLean,ground.runLean,runBlend);
        targetLean=Mathf.Clamp(targetLean,-Mathf.Max(13f,ground.runLean+2f),10f);
        lean=Mathf.SmoothDamp(lean,targetLean,ref leanVelocity,.10f,Mathf.Infinity,dt);
        if(reset){groundCycle.Reset();groundBlend=groundBlendVelocity=0;runBlend=0;}
        groundCycle.Advance(travel,Mathf.Lerp(ground.cycleDistance,ground.runCycleDistance,runBlend));
        phase=(float)groundCycle.Phase;
        StepWave=Mathf.Sin(phase*Mathf.PI*2f)*Mathf.Clamp01(Mathf.Abs(speed)/2f);
        float gaitAmount=Mathf.Clamp01(Mathf.Abs(speed)/2f);
        // Receive weight just after contact, then extend into the next push-off.
        float stepPhase=Mathf.Repeat(phase*2f,1f);
        pelvisY=(-Mathf.Lerp(.027f,.045f,runBlend)-ground.pelvisBounce*Mathf.Lerp(1f,1.5f,runBlend)*Mathf.Cos((stepPhase-.18f)*Mathf.PI*2f))*gaitAmount/Mathf.Max(.01f,transform.lossyScale.y);
        if(landingVersion!=movement.LandingVersion)
        {
            landingVersion=movement.LandingVersion; landingAge=0;
            landingStrength=Mathf.InverseLerp(2f,22f,movement.LastLandingSpeed);
        }
        landingAge+=dt;
        if(Mathf.Abs(input)>.1f) landingAge=air.landingRecovery;
        if(landingAge<air.landingRecovery && movement.LastLandingTime>movement.LastLaunchTime)
        {
            float recovery=landingAge/air.landingRecovery;
            pelvisY-=Mathf.Sin(recovery*Mathf.PI)*landingStrength*air.landingCompression/Mathf.Max(.01f,transform.lossyScale.y);
            State="Landing";
        }
        torsoAngle=lean+Mathf.Sin(phase*Mathf.PI*4f)*.9f*gaitAmount+HurtAngle;
        airTuck=airTuckVelocity=airFootTrail=airFootTrailVelocity=airLeanVelocity=0;
        headAngle=-torsoAngle*.78f;
        if(authoredWalk!=null)
        {
            // Ground attacks layer their compression and torso pose over the same
            // walking clock. Do not switch back to the faster gait mid-strike.
            authoredWalk.Step(speed,dt,reset,!movement.IsRunning && !turning);
            float w=authoredWalk.Weight;
            pelvisY=Mathf.Lerp(pelvisY,authoredWalk.Body.y,w);
            torsoAngle=Mathf.Lerp(torsoAngle,lean+authoredWalk.Body.x,w);
            headAngle=-torsoAngle*.78f;
        }
        previousSpeed=speed;
        // Keep the anatomical center fixed when mirroring the asymmetric painted canvas.
        Vector3 center=transform.TransformPoint(new Vector3(pelvis.x*VisualFacing,0,0));
        transform.position+=new Vector3(position.x-center.x,0,0);
        UpdateGroundLegs(speed,dt,reset || !wasActive || lastGroundFacing!=VisualFacing);
        lastGroundFacing=VisualFacing;
        return true;
    }

    public bool PrepareThread(bool active,PlayerThread thread,float facing)
    {
        if(!Ready || !active || threadTexture==null) return false;
        threadPose=BodyActive=true;GroundActive=false;tracking=false;turning=false;turnWidth=1;
        material.mainTexture=threadTexture;VisualFacing=desiredFacing=facing;
        if(bridgeAttach!=thread.AttachmentVersion){bridgeAttach=thread.AttachmentVersion;SaveBridge();}
        float age=Time.time-thread.AttachedAt;
        State=age<.08f?"Reach":age<.14f?"Catch":age<.28f && thread.IsTaut?"Tension":physicsBody.linearVelocity.magnitude<1.2f?"Hang":"Swing";
        reachAngle=-12f*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.12f)));
        float load=thread.IsTaut?1f:.25f;
        threadLoad=Mathf.MoveTowards(threadLoad,load,Time.deltaTime*7);
        hipLag=Swing!=null?Swing.LegAngle:0;
        // A fixed-length arm supports a rigid torso. Load changes elbow bend,
        // not body size; the painted grip stays at the rope endpoint.
        ledgeArmActive=true;
        float entry=Swing!=null?Swing.AttachBlend:1;
        ledgeBodyLean=Swing!=null?Swing.TorsoAngle:0;
        if(hasPose)ledgeBodyLean=Mathf.LerpAngle(BridgeAngle(),ledgeBodyLean,entry);
        torsoAngle=ledgeBodyLean;
        ledgeHand=Pixel(865,145);
        Vector2 shoulder=ThreadUpper(Pixel(798,510));
        float upperArm=Vector2.Distance(Pixel(798,510),Pixel(835,300));
        float forearm=Vector2.Distance(Pixel(835,300),Pixel(865,145));
        float bend=Swing!=null?.12f+Swing.Compact*Swing.tuning.apexCompression+(1-Swing.Load)*Swing.tuning.tensionExtension:.2f;
        float reach=upperArm+forearm-bend;
        // Keep the supporting arm beside the face, rather than crossing
        // through the head silhouette as the body leans under the grip.
        Vector2 shoulderFromGrip=Rotate(new Vector2(-1.65f,-3.1f).normalized,ledgeBodyLean)*reach;
        threadBodyOffset=ledgeHand+shoulderFromGrip-shoulder;
        if(hasPose&&entry<1)
        {
            threadBodyOffset+=(BridgeLocal(bridgeNeck)-ThreadUpper(Pixel(750,480)))*(1-entry);
            ledgeHand=Vector2.Lerp(BridgeLocal(bridgeHand),ledgeHand,entry);
        }
        ledgeShoulder=ThreadUpper(Pixel(798,510));
        Vector2 armDelta=ledgeHand-ledgeShoulder;
        reach=Mathf.Clamp(armDelta.magnitude,Mathf.Abs(upperArm-forearm)+.02f,upperArm+forearm-.02f);
        Vector2 correctedShoulder=ledgeHand-armDelta.normalized*reach;
        threadBodyOffset+=correctedShoulder-ledgeShoulder;ledgeShoulder=correctedShoulder;
        Vector2 axis=(ledgeHand-ledgeShoulder).normalized;
        float along=(upperArm*upperArm-forearm*forearm+reach*reach)/(2*reach);
        ledgeElbow=ledgeShoulder+axis*along+new Vector2(axis.y,-axis.x)*Mathf.Sqrt(Mathf.Max(0,upperArm*upperArm-along*along));
        for(int i=0;i<2;i++)
        {
            Leg leg=threadLegs[i];
            leg.stance=false;leg.posedHip=ThreadUpper(leg.hip);
            if(groundLegs.HasThreeQuarterArt)
            {
                leg.posedHip=ThreadUpper(Pixel(744,875))+Rotate(new Vector2(i==0?-.55f:.55f,0),Swing!=null?Swing.HipAngle:ledgeBodyLean);
                float gather=Swing!=null?Swing.Compact:0;
                float thigh=hipLag+(Swing!=null?Swing.HipAngle:ledgeBodyLean)+(i==0?5f:10f)*gather;
                float knee=12f+(i==0?18f:28f)*gather;
                leg.posedKnee=leg.posedHip+Rotate(Vector2.down*groundLegs.UpperLength,thigh);
                leg.posedAnkle=leg.posedKnee+Rotate(Vector2.down*groundLegs.LowerLength,thigh-knee);
                leg.posedSole=leg.posedAnkle+groundLegs.SoleOffset;
                if(hasPose&&entry<1)BlendBridgeLeg(leg,i,1-entry);
            }
            else
            {
                leg.posedKnee=leg.posedHip+Rotate(leg.knee-leg.hip,hipLag);
                leg.posedAnkle=leg.posedKnee+Rotate(leg.ankle-leg.knee,hipLag+Mathf.Abs(hipLag)*.35f);
                leg.posedSole=leg.posedAnkle+leg.sole-leg.ankle;
            }
            Vector3 soleWorld=transform.TransformPoint(Mirror(leg.posedSole));
            if(movement.HasGroundContact && soleWorld.y<movement.GroundPoint.y)
            {
                soleWorld.y=movement.GroundPoint.y;
                Vector3 local=transform.InverseTransformPoint(soleWorld);local.x*=facing;
                if(groundLegs.HasThreeQuarterArt)SolveGroundLeg(leg,local);else SolveLeg(leg,local);
            }
        }
        PrepareThreadWeapon(entry);
        return true;
    }
    void PrepareThreadWeapon(float entry)
    {
        Vector2 shoulder=Pixel(703,564),elbow=Pixel(660,697),hand=Pixel(650,825);
        threadWeaponShoulder=ThreadUpper(shoulder);
        Vector2 naturalGrip=ThreadUpper(elbow+Rotate(hand-elbow,hipLag*.3f));
        float naturalAngle=ledgeBodyLean+hipLag*.3f;
        threadWeaponGrip=hasPose?Vector2.Lerp(BridgeLocal(bridgeWeaponGrip),naturalGrip,entry):naturalGrip;
        float sourceAngle=Vector2.SignedAngle(Vector2.right,Pixel(975,1260)-hand);
        float bridgeAngle=Vector2.SignedAngle(Vector2.right,BridgeLocal(bridgeWeaponTip)-BridgeLocal(bridgeWeaponGrip))-sourceAngle;
        threadWeaponAngle=hasPose?Mathf.LerpAngle(bridgeAngle,naturalAngle,entry):naturalAngle;
        float upper=Vector2.Distance(shoulder,elbow),lower=Vector2.Distance(elbow,hand);
        Vector2 delta=threadWeaponGrip-threadWeaponShoulder;
        float reach=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper-lower)+.01f,upper+lower-.01f);
        Vector2 axis=delta.sqrMagnitude>.000001f?delta.normalized:Vector2.down;
        threadWeaponGrip=threadWeaponShoulder+axis*reach;
        float along=(upper*upper-lower*lower+reach*reach)/(2*reach);
        threadWeaponElbow=threadWeaponShoulder+axis*along-new Vector2(-axis.y,axis.x)*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
        threadWeaponUpperRotation=Rotation(Vector2.SignedAngle(elbow-shoulder,threadWeaponElbow-threadWeaponShoulder));
        threadWeaponLowerRotation=Rotation(Vector2.SignedAngle(hand-elbow,threadWeaponGrip-threadWeaponElbow));
        threadWeaponRotation=Rotation(threadWeaponAngle);
        if(threadWeaponBindings==null)
        {
            threadWeaponBindings=new ThreadWeaponBinding[rest.Length];
            for(int i=0;i<rest.Length;i++)threadWeaponBindings[i]=BuildThreadWeaponBinding(rest[i],pixelCoordinates[i]);
        }
    }
    public bool PrepareCombat(bool active,PlayerCombat combat,float facing)
    {
        if(!Ready || !active)return false;
        int pose=combat.IsAttackWindup && !combat.IsUpwardAttack?1:0;
        if(combatRest[pose]==null)return false;
        BodyActive=true;GroundActive=threadPose=false;tracking=false;turnWidth=1;VisualFacing=facing;combatPose=pose;
        material.mainTexture=combatSprites[pose].texture;
        if(combat.IsAttackWindup){State="Anticipation";combatAngle=4f*combat.WindupProgress;}
        else if(combat.StrikeProgress<1){State="Strike";combatAngle=-6f*Mathf.Sin(Mathf.Clamp01(combat.StrikeProgress)*Mathf.PI*.5f);}
        else {State="Recovery";combatAngle=-6f*(1-Mathf.SmoothStep(0,1,combat.AttackRecoveryProgress));}
        upwardArmAngle=upwardThrust=upwardLook=0;
        if(combat.IsUpwardAttack)
        {
            // Keep the blade vertical: draw down, drive straight up, then retract.
            upwardArmAngle=93;
            upwardThrust=combat.IsAttackWindup ? Mathf.Lerp(-.45f,-.85f,combat.WindupProgress) :
                combat.StrikeProgress<1 ? Mathf.Lerp(-.85f,.45f,Mathf.Sin(Mathf.Clamp01(combat.StrikeProgress)*Mathf.PI*.5f)) :
                Mathf.Lerp(.45f,-.45f,Mathf.SmoothStep(0,1,combat.AttackRecoveryProgress));
            upwardLook=combat.IsAttackWindup ? Mathf.Lerp(20,40,combat.WindupProgress) :
                combat.StrikeProgress<1 ? 40 : Mathf.Lerp(40,20,combat.AttackRecoveryProgress);
            combatAngle=-3;
        }
        combatAngle+=HurtAngle;return true;
    }
    bool combatOneHanded;
    public void ApplyCombatLayer(bool active,PlayerCombat combat)
    {
        if(!BodyActive||threadPose||WallPoseActive){combatLayerWeight=0;return;}
        if(active)
        {
            if(combatEntryVersion!=combat.ExecutionId)
            {
                combatEntryVersion=combat.ExecutionId;combatEntryValid=hasPose;
                combatEntryGrip=lastWeaponGrip;combatEntryTip=lastWeaponTip;
            }
            var profile=combat.CurrentAttack.animation;
            combatOneHanded=profile.oneHanded;
            layeredCombatPose=profile.Sample(combat.Phase,combat.PhaseProgress);
            float recoveryBlend=Mathf.InverseLerp(profile.recoveryHold,1,combat.PhaseProgress);
            combatLayerWeight=combat.Phase==AttackPhase.Startup?Mathf.SmoothStep(0,1,combat.PhaseProgress):combat.Phase==AttackPhase.Recovery?1-Mathf.SmoothStep(0,1,recoveryBlend):1;
            State=combat.Phase==AttackPhase.Startup?"Anticipation":combat.Phase==AttackPhase.Active?"Strike":"Recovery";
            VisualFacing=desiredFacing=combat.AttackDirection;turning=false;turnWidth=1;
        }
        else combatLayerWeight=Mathf.MoveTowards(combatLayerWeight,0,Time.deltaTime/ .10f);
        if(combatLayerWeight<=0)return;
        combatLayerTorso=layeredCombatPose.torsoAngle*combatLayerWeight;
        torsoAngle+=combatLayerTorso;headAngle-=combatLayerTorso*.6f;
        pelvisY-=layeredCombatPose.compression*combatLayerWeight;
        for(int i=0;i<2;i++)
        {
            var leg=legs[i];Vector2 sole=leg.posedSole;
            leg.posedHip+=new Vector2((i==0?-.04f:.04f)*combatLayerWeight,-layeredCombatPose.compression*combatLayerWeight);
            if(!GroundActive)sole+=new Vector2((i==0?-.12f:.12f)*combatLayerWeight,layeredCombatPose.kneeBend*combatLayerWeight*(i==0?.75f:1));
            if(groundLegs.HasThreeQuarterArt)SolveGroundLeg(leg,sole);else SolveLeg(leg,sole);
        }
    }
    Vector2 CombatPoint(float x,float y)
    {
        Sprite sprite=combatSprites[combatPose];return new Vector2((x-sprite.pivot.x)/sprite.pixelsPerUnit,(sprite.rect.height-y-sprite.pivot.y)/sprite.pixelsPerUnit);
    }
    Vector2 CombatSkin(Vector2 point,Vector2 pixel)
    {
        if(upwardArmAngle!=0)
        {
            float head=(1-Smooth(425,460,pixel.y))*(1-Smooth(820,880,pixel.x));
            Vector2 neckPivot=CombatPoint(677,425);
            point=Vector2.Lerp(point,neckPivot+Rotate(point-neckPivot,upwardLook),head);
            float arm=Smooth(705,800,pixel.x)*Mathf.Max(Smooth(835,900,pixel.x),
                Smooth(415,455,pixel.y))*(1-Smooth(580,630,pixel.y));
            Vector2 shoulder=CombatPoint(701,503);
            Vector2 thrustOffset=Rotate(new Vector2(.75f,upwardThrust),3);
            point=Vector2.Lerp(point,shoulder+Rotate(point-shoulder,upwardArmAngle)+thrustOffset,arm);
        }
        Vector2 pivot=combatPose==1?CombatPoint(810,620):CombatPoint(615,595);
        float weight=1-Smooth(620,895,pixel.y);
        return Vector2.Lerp(point,pivot+Rotate(point-pivot,combatAngle),weight);
    }
    Vector2 ThreadUpper(Vector2 p)
    {
        Vector2 pivot=threadPose&&!WallPoseActive?Pixel(865,145):Pixel(744,790);
        return pivot+Rotate(p-pivot,ledgeBodyLean)+new Vector2(0,-threadLoad*.18f)+threadBodyOffset;
    }
    static ThreadWeaponBinding BuildThreadWeaponBinding(Vector2 p,Vector2 pixel)
    {
        Vector2 shoulder=Pixel(703,564),elbow=Pixel(660,697),hand=Pixel(650,825);
        return new ThreadWeaponBinding{arm=Mathf.Max(SegmentWeight(p,shoulder,elbow,.28f),SegmentWeight(p,elbow,hand,.28f)),
            joint=Smooth(680,715,pixel.y),weapon=QoriThreadWeaponArt.Mask(pixel),palm=1-Smooth(.24f,.35f,Vector2.Distance(p,hand))};
    }
    Vector2 WallWeapon(Vector2 p,Vector2 pixel,Vector2 posed,bool detachedWeapon=false,int vertexIndex=-1)
    {
        if(threadPose&&!WallPoseActive)
        {
            Vector2 shoulder=Pixel(703,564),elbow=Pixel(660,697),hand=Pixel(650,825);
            ThreadWeaponBinding binding;
            if(vertexIndex>=0)binding=threadWeaponBindings[vertexIndex];
            else if(!threadWeaponPointBindings.TryGetValue(p,out binding))
            {binding=BuildThreadWeaponBinding(p,pixel);threadWeaponPointBindings.Add(p,binding);}
            if(binding.arm>0f)
            {
                Vector2 upper=threadWeaponShoulder+ApplyRotation(p-shoulder,threadWeaponUpperRotation);
                Vector2 lower=threadWeaponElbow+ApplyRotation(p-elbow,threadWeaponLowerRotation);
                posed=Vector2.Lerp(posed,Vector2.Lerp(upper,lower,binding.joint),binding.arm);
            }
            float rigidWeight=detachedWeapon?binding.palm:Mathf.Max(binding.weapon,binding.palm);
            return rigidWeight>0f?Vector2.Lerp(posed,threadWeaponGrip+ApplyRotation(p-hand,threadWeaponRotation),rigidWeight):posed;
        }
        if(pixel.y<670)return posed;
        float shaftX=550+(pixel.y-730)*.79f;
        float weight=Mathf.Max(1-Smooth(22,38,Mathf.Abs(pixel.x-shaftX)),Smooth(960,1040,pixel.y)*Smooth(745,795,pixel.x))*Smooth(700,730,pixel.y);
        float forearm=(1-Smooth(23,34,Mathf.Abs(pixel.x-Mathf.Lerp(660,650,Mathf.InverseLerp(697,825,pixel.y)))))*Smooth(680,710,pixel.y)*(1-Smooth(835,860,pixel.y));
        weight=Mathf.Max(weight,forearm);
        float angle=WallPoseActive?(movement.IsLedgeClimbing?-24f:-14f):hipLag*.3f;
        Vector2 wallElbow=Pixel(660,697);
        return Vector2.Lerp(posed,ThreadUpper(wallElbow+Rotate(p-wallElbow,angle)),weight);
    }
    static float SegmentWeight(Vector2 p,Vector2 a,Vector2 b,float width)
    {Vector2 d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);return 1-Smooth(width,width+.1f,Vector2.Distance(p,a+d*t));}
    Vector2 ThreadSkin(Vector2 point,Vector2 pixel)
    {
        Vector2 upper=ThreadUpper(point);
        // The raised arm rotates at its shoulder; the face retains a rigid shape.
        if(!ledgeArmActive && pixel.x>808 && pixel.y<340)
        {
            Vector2 shoulder=Pixel(798,510);
            return shoulder+Rotate(point-shoulder,reachAngle);
        }
        if(pixel.y<505)
        {
            if(pixel.x<680 && Secondary!=null)
            {
                Vector2 root=pixel.y<370?Pixel(684,292):Pixel(669,373);
                upper=ThreadUpper(root+Rotate(point-root,Secondary.EarAngle));
            }
            return upper;
        }
        float left=1-Smooth(650,715,pixel.x);
        float weight=Smooth(900,1010,pixel.y)*(1-Smooth(772,822,pixel.x));
        // Keep the diagonal reedblade bound to the lowered hand, including where it crosses a thigh.
        float shaftX=550+(pixel.y-730)*.79f;
        weight*=Smooth(22,45,Mathf.Abs(pixel.x-shaftX));
        Vector2 a=ThreadLeg(point,pixel,threadLegs[0]), b=ThreadLeg(point,pixel,threadLegs[1]);
        return WallWeapon(point,pixel,Vector2.Lerp(upper,Vector2.Lerp(b,a,left),weight));
    }
    Vector2 ThreadLeg(Vector2 point,Vector2 pixel,Leg leg)
    {
        Vector2 upper=Bone(point,leg.hip,leg.knee,leg.posedHip,leg.posedKnee);
        Vector2 lower=Bone(point,leg.knee,leg.ankle,leg.posedKnee,leg.posedAnkle);
        Vector2 foot=point+leg.posedAnkle-leg.ankle;
        return Vector2.Lerp(Vector2.Lerp(upper,lower,Smooth(leg.knee.y+.2f,leg.knee.y-.2f,point.y)),foot,Smooth(1095,1140,pixel.y));
    }

    public bool PrepareAir(bool active,float facing)
    {
        if(!Ready || !active) return false;
        if((movement.IsWallSliding || movement.IsLedgeHanging || movement.IsLedgeClimbing) && threadTexture!=null)return PrepareWallSlide(facing);
        BodyActive=true; GroundActive=false; tracking=false; turning=false; turnWidth=1;
        VisualFacing=desiredFacing=facing;
        Vector2 velocity=physicsBody.linearVelocity;
        float launchAge=Time.time-movement.LastLaunchTime;
        bool boosted=movement.LastLaunchKind==PlayerMovement.LaunchKind.FlowerBoost && launchAge<2f;
        bool launch=launchAge<air.takeoffSeconds;
        bool wallLaunch=movement.LastLaunchKind==PlayerMovement.LaunchKind.WallJump && launchAge<.26f;
        bool outward=wallLaunch && Mathf.Abs(velocity.x)>movement.WalkSpeed*.6f;
        bool preparing=velocity.y<0 && movement.LandingTimeEstimate<air.preparationSeconds;
        State=launch?"Takeoff":preparing?"PreLanding":velocity.y>air.apexSpeed?"Rise":velocity.y< -air.apexSpeed?"Fall":"Apex";
        bool releasing=Swing!=null&&Swing.Releasing;
        if(releasing&&bridgeRelease!=threadSource.ReleaseVersion){bridgeRelease=threadSource.ReleaseVersion;SaveBridge();}
        if(releasing) State="Release";
        if(wallLaunch)State=outward?"WallPushOff":"WallClimb";
        if(movement.IsWallSliding)State="WallSlide";
        float target=launch?0:State=="Rise"?(boosted?air.boostTuck:air.normalTuck)*.85f:State=="Apex"?air.normalTuck*.60f:State=="Fall"?.045f:0f;
        if(movement.IsWallSliding)target=air.normalTuck*.4f;
        if(wallLaunch)target=Mathf.Lerp(-.035f,air.normalTuck,Mathf.SmoothStep(0,1,Mathf.Clamp01(launchAge/.22f)));
        airTuck=Mathf.SmoothDamp(airTuck,target,ref airTuckVelocity,.07f,Mathf.Infinity,Time.deltaTime);
        float travelDirection=Mathf.Clamp(velocity.x*facing/Mathf.Max(.1f,movement.WalkSpeed),-1,1);
        // Gather feet behind the body on ascent, then bring them under and
        // slightly ahead of the hips to receive the landing.
        float trail=launch?-.18f:State=="Rise"?-.35f:State=="Apex"?-.08f:preparing?.20f:.08f;
        if(movement.IsWallSliding){trail=.3f;travelDirection=1f;}
        if(wallLaunch){trail=outward?-.65f:.18f;travelDirection=1f;}
        airFootTrail=Mathf.SmoothDamp(airFootTrail,trail*travelDirection,ref airFootTrailVelocity,.09f,Mathf.Infinity,Time.deltaTime);
        float airLean=Mathf.Clamp(-velocity.x*facing*.5f,-5f,5f)+(launch?-3f:preparing?2f:0);
        if(movement.IsWallSliding)airLean=-6f;
        if(wallLaunch)airLean=(outward?-13f:-5f)*(1-Mathf.Clamp01(launchAge/.32f));
        if(releasing&&hasPose)airLean=Mathf.LerpAngle(airLean,BridgeAngle(),Swing.Weight);
        pelvisY=0; torsoAngle=Mathf.SmoothDampAngle(torsoAngle,airLean+HurtAngle,ref airLeanVelocity,.08f,Mathf.Infinity,Time.deltaTime);
        headAngle=-torsoAngle*.65f*(releasing?1-Swing.Weight:1); StepWave=0;
        Vector3 center=transform.TransformPoint(new Vector3(pelvis.x*facing,0,0));
        transform.position+=new Vector3(movement.transform.position.x-center.x,0,0);
        if(releasing&&hasPose)airPoseOffset=(BridgeLocal(bridgeNeck)-Upper(neck))*Swing.Weight;
        for(int i=0;i<2;i++)
        {
            Leg leg=legs[i]; leg.stance=false;
            float tuck=airTuck/Mathf.Max(.01f,transform.lossyScale.y);
            float baseY=transform.InverseTransformPoint(new Vector3(movement.transform.position.x,RenderedSoleY,transform.position.z)).y;
            if(groundLegs.HasThreeQuarterArt)
            {
                float total=groundLegs.UpperLength+groundLegs.LowerLength;
                leg.posedHip=new Vector2(pelvis.x+(i==0?-.55f:.55f),baseY-groundLegs.SoleOffset.y+total*.96f);
                // Near knee leads the gather; far leg follows without changing anatomy.
                float gather=tuck*(i==0?.72f:1f);
                float stride=(i==0?-.22f:.22f)*Mathf.Clamp01(tuck);
                Vector2 sole=new Vector2(leg.posedHip.x+groundLegs.SoleOffset.x+airFootTrail+stride,baseY+gather);
                SolveGroundLeg(leg,sole);
                if(releasing&&hasPose)
                {
                    BlendBridgeLeg(leg,i,Swing.Weight);
                    Vector2 socket=Upper(pelvis+new Vector2(i==0?-.55f:.55f,-.85f));
                    Vector2 correction=(socket-leg.posedHip)*Swing.Weight;
                    leg.posedHip+=correction;leg.posedKnee+=correction;leg.posedAnkle+=correction;leg.posedSole+=correction;
                }
            }
            else
            {
                leg.posedHip=leg.hip;
                Vector2 sole=new Vector2(leg.sole.x+airFootTrail,baseY+tuck*(i==0?1f:.8f));
                SolveLeg(leg,sole);
            }
        }
        return true;
    }

    bool PrepareWallSlide(float facing)
    {
        // Reuse the approved raised-arm painting: the free hand braces above
        // the head while the other keeps the reedblade lowered beside the body.
        threadPose=BodyActive=WallPoseActive=true;GroundActive=false;
        tracking=false;turning=false;turnWidth=1;VisualFacing=desiredFacing=facing;
        material.mainTexture=threadTexture;State=movement.IsLedgeClimbing?"LedgeClimb":movement.IsLedgeHanging?"LedgeHang":"WallSlide";
        threadLoad=0;reachAngle=0;StepWave=0;
        float wallX=colliderBody.bounds.center.x+facing*(colliderBody.bounds.extents.x-.025f);
        // Keep the painted raised arm intact and align its knuckles with the
        // wall. Rotating only its exposed tip would split it at the head overlap.
        Vector3 hand=transform.TransformPoint(Mirror(Pixel(865,145)));
        transform.position+=new Vector3(wallX-hand.x,0,0);
        if(movement.IsLedgeHanging)
            transform.position+=new Vector3(0,Mathf.Clamp(movement.LedgePoint.y-hand.y,-.25f,.25f),0);
        float climb=movement.LedgeClimbProgress;
        if(movement.IsLedgeClimbing)
        {
            ledgeArmActive=true;
            ledgeBodyLean=-16f*Mathf.Sin(climb*Mathf.PI);
            ledgeShoulder=ThreadUpper(Pixel(798,510));
            Vector3 gripWorld=new Vector3(movement.LedgePoint.x,movement.LedgePoint.y+.02f,transform.position.z);
            Vector3 gripLocal=transform.InverseTransformPoint(gripWorld);gripLocal.x*=facing;
            float release=Smooth(.42f,.72f,climb);
            ledgeHand=Vector2.Lerp(gripLocal,ledgeShoulder+new Vector2(.65f,-1.7f),release);
            Vector2 delta=ledgeHand-ledgeShoulder;
            float upper=2.15f,lower=Vector2.Distance(Pixel(835,300),Pixel(865,145));
            upper=Mathf.Min(upper,Mathf.Max(lower*.8f,delta.magnitude+lower-.025f));
            float length=Mathf.Clamp(delta.magnitude,.12f,upper+lower-.015f);
            Vector2 axis=delta.sqrMagnitude>.0001f?delta.normalized:Vector2.up;
            float along=(upper*upper-lower*lower+length*length)/(2*length);
            Vector2 bendDirection=new Vector2(-axis.y,axis.x);
            if(bendDirection.x>0)bendDirection=-bendDirection;
            ledgeElbow=ledgeShoulder+axis*along+bendDirection*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            ledgeHand=ledgeShoulder+axis*length;
        }
        for(int i=0;i<2;i++)
        {
            Leg leg=threadLegs[i];leg.stance=false;leg.posedHip=ThreadUpper(leg.hip);
            // Offset the feet vertically so the silhouette reads as bracing,
            // with a small yielding motion as weight moves down the wall.
            float yield=Mathf.Sin(Time.time*3f+i*.8f)*.008f;
            float toeClearance=groundLegs.HasThreeQuarterArt?.10f:0;
            Vector3 soleWorld=new Vector3(wallX-facing*toeClearance,colliderBody.bounds.min.y+(i==0?.12f:.26f)+yield,transform.position.z);
            if(movement.IsLedgeClimbing)
            {
                float step=Smooth(i==0?.68f:.55f,i==0?.98f:.87f,climb);
                Vector3 top=new Vector3(movement.transform.position.x+facing*(i==0?-.1f:.12f),movement.LedgePoint.y+.025f,transform.position.z);
                soleWorld=Vector3.Lerp(soleWorld,top,step);
            }
            Vector3 local=transform.InverseTransformPoint(soleWorld);local.x*=facing;
            SolveLeg(leg,local);
        }
        return true;
    }

    void UpdateGroundLegs(float speed,float dt,bool reset)
    {
        bool moving=Mathf.Abs(speed)>.08f;
        groundBlend=Mathf.SmoothDamp(groundBlend,moving?1:0,ref groundBlendVelocity,.10f,Mathf.Infinity,dt);
        float scaleX=Mathf.Max(.001f,Mathf.Abs(transform.lossyScale.x));
        float floorLocal=transform.InverseTransformPoint(new Vector3(transform.position.x,FloorY,transform.position.z)).y;
        float total=groundLegs.UpperLength+groundLegs.LowerLength;
        float hipY=floorLocal-groundLegs.SoleOffset.y+total*.96f+pelvisY;
        float vertical=floorLocal-groundLegs.SoleOffset.y-hipY;
        float available=Mathf.Sqrt(Mathf.Max(0,(total-.004f)*(total-.004f)-vertical*vertical))*scaleX;
        // Use the available rigid-leg reach instead of clipping the stride to
        // a short fixed sweep. Keep a margin from full extension for the knee.
        float halfStride=available*.94f*groundBlend;
        float direction=moving?Mathf.Sign(speed):VisualFacing;
        for(int index=0;index<2;index++)
        {
            Leg leg=legs[index];
            float hipSpread=groundLegs.HasThreeQuarterArt?.55f:.24f;
            leg.posedHip=new Vector2(pelvis.x+(index==0?-hipSpread:hipSpread),hipY);
            Vector3 hipWorld=transform.TransformPoint(Mirror(leg.posedHip));
            float neutralX=hipWorld.x+groundLegs.SoleOffset.x*scaleX*VisualFacing;
            QoriGroundCycle.Sample sample=groundCycle.Evaluate(index,ground.supportFraction);
            bool crossed=(float)sample.phase<footPhase[index];
            bool planted=moving && sample.contact;
            Vector3 target=new Vector3(neutralX+direction*halfStride*(float)sample.horizontal,FloorY+(float)sample.lift*ground.footLift*Mathf.Lerp(1f,1.5f,runBlend)*groundBlend,transform.position.z);
            if(planted)
            {
                if(reset || !leg.stance || crossed)
                {
                    if(!reset){FootstepVersion++;FootstepPosition=new Vector3(target.x,FloorY,target.z);}
                }
                // A pose-driven support sweep preserves slow animation at the
                // unchanged controller speed. This is ground contact, not a
                // world-locked foot; do not report target accuracy as zero sliding.
                target.y=FloorY;
                leg.plant=target;
            }
            if(!moving)
            {
                Vector3 neutral=new Vector3(neutralX,FloorY,transform.position.z);
                if(!reset && leg.stance)
                {
                    // Keep an already supporting foot still at a stop. Clamp
                    // only when the recovering pelvis changes the reachable span.
                    target=new Vector3(Mathf.Clamp(leg.worldFoot.x,neutralX-available*.98f,neutralX+available*.98f),FloorY,transform.position.z);
                    planted=true;
                }
                else
                {
                    target=reset?neutral:Vector3.MoveTowards(leg.worldFoot,neutral,dt*2f);
                    planted=Vector3.SqrMagnitude(target-neutral)<.000001f;
                }
                leg.plant=target;
            }
            leg.stance=planted;footPhase[index]=(float)sample.phase;
            Vector3 local=transform.InverseTransformPoint(target);local.x*=VisualFacing;
            if(!moving&&groundLegs.HasThreeQuarterArt)
            {
                // A stopped foot must settle on its own side, rather than retain
                // a crossed walking plant and visually merge with the other leg.
                float centerX=pelvis.x+groundLegs.SoleOffset.x;
                float separated=index==0?Mathf.Min(local.x,centerX-.5f):Mathf.Max(local.x,centerX+.5f);
                local.x=Mathf.MoveTowards(local.x,separated,dt*4f);
                leg.plant=transform.TransformPoint(Mirror(local));
            }
            if(authoredWalk!=null && authoredWalk.Weight>0)
            {
                Vector3 key=authoredWalk.Foot(index);
                float reach=available/scaleX*.90f;
                Vector2 authoredSole=new Vector2(leg.posedHip.x+groundLegs.SoleOffset.x+key.x*reach,
                    floorLocal+key.y);
                local=Vector3.Lerp(local,authoredSole,authoredWalk.Weight);
            }
            SolveGroundLeg(leg,local);
        }
    }
    void SolveGroundLeg(Leg leg,Vector2 sole)
    {
        Vector2 ankle=sole-groundLegs.SoleOffset;
        Vector2 delta=ankle-leg.posedHip;
        float upper=groundLegs.UpperLength,lower=groundLegs.LowerLength;
        float length=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper-lower)+.01f,upper+lower-.001f);
        Vector2 axis=delta.sqrMagnitude>.0001f?delta.normalized:Vector2.down;
        float along=(upper*upper-lower*lower+length*length)/(2*length);
        float bend=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
        leg.posedKnee=leg.posedHip+axis*along+new Vector2(-axis.y,axis.x)*bend;
        leg.posedAnkle=leg.posedHip+axis*length;
        leg.posedSole=leg.posedAnkle+groundLegs.SoleOffset;
    }
    void SolveLeg(Leg leg,Vector2 sole)
    {
        Vector2 ankle=sole-(leg.sole-leg.ankle);
        Vector2 delta=ankle-leg.posedHip; float length=Mathf.Clamp(delta.magnitude,Mathf.Abs(leg.upperLength-leg.lowerLength)+.01f,leg.upperLength+leg.lowerLength-.001f);
        Vector2 axis=delta.sqrMagnitude>.0001f?delta.normalized:Vector2.down;
        float along=(leg.upperLength*leg.upperLength-leg.lowerLength*leg.lowerLength+length*length)/(2*length);
        float bend=Mathf.Sqrt(Mathf.Max(0,leg.upperLength*leg.upperLength-along*along));
        Vector2 perpendicular=new Vector2(-axis.y,axis.x);
        leg.posedKnee=leg.posedHip+axis*along+perpendicular*bend;
        leg.posedAnkle=leg.posedHip+axis*length;
        leg.posedSole=leg.posedAnkle+leg.sole-leg.ankle;
    }

    Vector2 Upper(Vector2 point)
    {
        float chest=(Secondary!=null?Secondary.ChestRise:0)*Smooth(pelvis.y,neck.y,point.y);
        return pelvis+new Vector2(0,pelvisY+chest)+Rotate(point-pelvis,torsoAngle)+airPoseOffset;
    }
    Vector2 Head(Vector2 point)
    {
        Vector2 n=Upper(neck); return n+Rotate(point-neck,torsoAngle+headAngle+(Secondary!=null?Secondary.AttentionAngle:0));
    }
    static Vector2 Bone(Vector2 point,Vector2 a,Vector2 b,Vector2 posedA,Vector2 posedB)
        => posedA+Rotate(point-a,Vector2.SignedAngle(b-a,posedB-posedA));
    Vector2 SkinLeg(Vector2 point,Vector2 pixel,int index)
    {
        Leg leg=legs[index];
        Vector2 upper=Bone(point,leg.hip,leg.knee,leg.posedHip,leg.posedKnee);
        Vector2 lower=Bone(point,leg.knee,leg.ankle,leg.posedKnee,leg.posedAnkle);
        Vector2 foot=point+leg.posedAnkle-leg.ankle;
        float knee=Smooth(leg.knee.y+.065f,leg.knee.y-.065f,point.y);
        float ankle=QoriSkinWeights.FootWeight(pixel,index);
        return Vector2.Lerp(Vector2.Lerp(upper,lower,knee),foot,ankle);
    }
    Vector2 Skin(Vector2 point,Vector2 pixel)
    {
        Vector2 upper=Arm(point,pixel);
        if(pixel.y<500)
        {
            Vector2 headPoint=point;
            if(Secondary!=null && pixel.y<370)
            {
                Vector2 root=pixel.y<290?Pixel(653,290):Pixel(624,337);
                float weight=1-Smooth(545,660,pixel.x);
                headPoint=Vector2.Lerp(point,root+Rotate(point-root,Secondary.EarAngle),weight);
            }
            return Vector2.Lerp(Head(headPoint),upper,Smooth(460,500,pixel.y));
        }
        var weights=QoriSkinWeights.Evaluate(pixel);
        return upper*weights.body+SkinLeg(point,pixel,0)*weights.left+SkinLeg(point,pixel,1)*weights.right;
    }
    Vector2 Arm(Vector2 point,Vector2 pixel) => Upper(weaponCarry.Skin(point));
    void CachePose()
    {
        torsoRotation=Rotation(torsoAngle);headRotation=Rotation(torsoAngle+headAngle+(Secondary!=null?Secondary.AttentionAngle:0));
        earRotation=Rotation(Secondary!=null?Secondary.EarAngle:0);posedNeck=Upper(neck);
        for(int i=0;i<2;i++)
        {
            Leg leg=legs[i];upperBones[i]=new BoneMap(leg.hip,leg.knee,leg.posedHip,leg.posedKnee);
            lowerBones[i]=new BoneMap(leg.knee,leg.ankle,leg.posedKnee,leg.posedAnkle);
        }
    }
    void CacheActionPose()
    {
        earRotation=Rotation(Secondary!=null?Secondary.EarAngle:0);
        actionRotation=Rotation(combatPose>=0?combatAngle:reachAngle);
        if(combatPose>=0){actionPivot=combatPose==1?CombatPoint(810,620):CombatPoint(615,595);return;}
        for(int i=0;i<2;i++)
        {
            Leg leg=threadLegs[i];upperBones[i]=new BoneMap(leg.hip,leg.knee,leg.posedHip,leg.posedKnee);
            lowerBones[i]=new BoneMap(leg.knee,leg.ankle,leg.posedKnee,leg.posedAnkle);
        }
    }
    Vector2 BoundCombat(int index)
    {
        Vector2 p=combatRest[combatPose][index];
        if(upwardArmAngle!=0)return CombatSkin(p,combatPixels[combatPose][index]);
        return Vector2.Lerp(p,actionPivot+ApplyRotation(p-actionPivot,actionRotation),1-Smooth(620,895,combatPixels[combatPose][index].y));
    }
    Vector2 BoundThreadLeg(Vector2 p,Vector2 pixel,int index)
    {
        Leg leg=threadLegs[index];
        return Vector2.Lerp(Vector2.Lerp(upperBones[index].Apply(p),lowerBones[index].Apply(p),Smooth(leg.knee.y+.2f,leg.knee.y-.2f,p.y)),p+leg.posedAnkle-leg.ankle,Smooth(1095,1140,pixel.y));
    }
    Vector2 BoundThread(int index)
    {
        Vector2 p=rest[index],pixel=pixelCoordinates[index],upper=ThreadUpper(p);
        if(!ledgeArmActive && pixel.x>808 && pixel.y<340)
        {Vector2 shoulder=Pixel(798,510);return shoulder+ApplyRotation(p-shoulder,actionRotation);}
        if(pixel.y<505)
        {
            if(pixel.x<680 && Secondary!=null)
            {Vector2 root=pixel.y<370?Pixel(684,292):Pixel(669,373);upper=ThreadUpper(root+ApplyRotation(p-root,earRotation));}
            return upper;
        }
        float left=1-Smooth(650,715,pixel.x);
        float weight=Smooth(900,1010,pixel.y)*(1-Smooth(772,822,pixel.x))*Smooth(22,45,Mathf.Abs(pixel.x-(550+(pixel.y-730)*.79f)));
        if(weight<=0)return WallWeapon(p,pixel,upper,true,index);
        return WallWeapon(p,pixel,Vector2.Lerp(upper,Vector2.Lerp(BoundThreadLeg(p,pixel,1),BoundThreadLeg(p,pixel,0),left),weight),true,index);
    }
    Vector2 BoundLeg(Vector2 p,int index,float knee,float foot)
    {
        Leg leg=legs[index];
        return Vector2.Lerp(Vector2.Lerp(upperBones[index].Apply(p),lowerBones[index].Apply(p),knee),p+leg.posedAnkle-leg.ankle,foot);
    }
    Vector2 BoundSkin(int index)
    {
        Vector2 p=rest[index];VertexBinding b=bindings[index];
        Vector2 carry=weaponCarry.Skin(p);
        float chest=(Secondary!=null?Secondary.ChestRise:0)*Smooth(pelvis.y,neck.y,carry.y);
        Vector2 upper=pelvis+new Vector2(0,pelvisY+chest)+ApplyRotation(carry-pelvis,torsoRotation)+airPoseOffset;
        if(pixelCoordinates[index].y<500)
        {
            Vector2 headPoint=Vector2.Lerp(p,b.earRoot+ApplyRotation(p-b.earRoot,earRotation),b.ear);
            return Vector2.Lerp(posedNeck+ApplyRotation(headPoint-neck,headRotation),upper,b.headBlend);
        }
        if(GroundActive)return upper;
        Vector2 result=upper*b.legs.body;
        if(b.legs.left>0)result+=BoundLeg(p,0,b.kneeLeft,b.footLeft)*b.legs.left;
        if(b.legs.right>0)result+=BoundLeg(p,1,b.kneeRight,b.footRight)*b.legs.right;
        return result;
    }
    Vector2 Turn(Vector2 p)
    {
        float weight=1-Smooth(-2.2f,-4.8f,p.y);
        p.x=pelvis.x+(p.x-pelvis.x)*Mathf.Lerp(1,turnWidth,weight);
        return p;
    }
    void Joint(string name,Vector2 p)
    {
        if (name != "CloakMount") p=Turn(p);
        joints[name].localPosition=new Vector3(p.x*VisualFacing,p.y,0);
    }
    public void Render(Color tint)
    {
        if(!Ready || !BodyActive) return;
        rendererMesh.enabled=true;
        bool customWeapon=combatSource!=null&&combatSource.PresentationWeapon!=null&&combatSource.PresentationWeapon.weaponArtwork!=null;
        bool detachedHeldWeapon=combatPose<0&&(!threadPose&&combatLayerWeight>0||customWeapon);
        if(detachedHeldWeapon&&heldWeaponMask==null){heldWeaponMask=new float[rest.Length];for(int i=0;i<rest.Length;i++)heldWeaponMask[i]=QoriWeaponCarry.WeaponMask(rest[i]);}
        if(combatPose<0&&!threadPose)
        {
            float speed=Mathf.Clamp01(Mathf.Abs(physicsBody.linearVelocity.x)/movement.WalkSpeed);
            float lift=GroundActive?Mathf.Lerp(.02f,.48f,speed)+runBlend*.3f:State=="Rise"||State=="Takeoff"?.72f:.42f;
            float angle=GroundActive?speed*10f+runBlend*6f:State=="Rise"||State=="Takeoff"?18f:8f;
            float bridgeWeight=Swing!=null&&Swing.Releasing&&hasPose?Swing.Weight:0f;
            Vector2 grip=pelvis+Rotate(BridgeLocal(bridgeWeaponGrip)-pelvis-new Vector2(0,pelvisY)-airPoseOffset,-torsoAngle);
            float shaftAngle=Vector2.SignedAngle(Vector2.right,Pixel(1180,943)-Pixel(647,824));
            float weaponAngle=Vector2.SignedAngle(Vector2.right,BridgeLocal(bridgeWeaponTip)-BridgeLocal(bridgeWeaponGrip))-shaftAngle-torsoAngle;
            if(combatLayerWeight>0)
            {
                grip=layeredCombatPose.leftGrip;weaponAngle=layeredCombatPose.weaponAngle-torsoAngle;
                bridgeWeight=combatLayerWeight;
                if(combatEntryValid&&combatSource.Phase==AttackPhase.Startup)
                {
                    Vector2 entryGrip=pelvis+Rotate(BridgeLocal(combatEntryGrip)-pelvis-new Vector2(0,pelvisY)-airPoseOffset,-torsoAngle);
                    float entryAngle=Vector2.SignedAngle(Vector2.right,BridgeLocal(combatEntryTip)-BridgeLocal(combatEntryGrip))-shaftAngle-torsoAngle;
                    grip=Vector2.Lerp(entryGrip,grip,combatLayerWeight);weaponAngle=Mathf.LerpAngle(entryAngle,weaponAngle,combatLayerWeight);bridgeWeight=1;
                }
            }
            weaponCarry.Step(lift,angle,grip,weaponAngle,bridgeWeight,layeredCombatPose.shoulderReach*combatLayerWeight,combatOneHanded?combatLayerWeight:0,layeredCombatPose.freeHand);
        }
        if(combatPose<0 && !threadPose)CachePose();else CacheActionPose();
        if(!GroundActive&&!groundLegs.HasThreeQuarterArt)groundLegs.Hide();
        for(int i=0;i<vertices.Length;i++)
        {
            Vector2 p=combatPose>=0?BoundCombat(i):threadPose?BoundThread(i):Turn(BoundSkin(i));
            vertices[i]=new Vector3(p.x*VisualFacing,p.y,0); colors[i]=tint;
            if(detachedHeldWeapon&&!threadPose)colors[i].a*=1-heldWeaponMask[i];
            if(GroundActive)colors[i].a*=bindings[i].legs.body;
            if(ledgeArmActive)colors[i].a*=1-QoriLedgeArm.Mask(pixelCoordinates[i]);
            if(threadPose&&!WallPoseActive)colors[i].a*=1-threadWeaponBindings[i].weapon;
            if(threadPose&&WallPoseActive&&customWeapon)colors[i].a*=1-ThreadWeaponMask(pixelCoordinates[i]);
            if(headArt.Ready)colors[i].a*=1-OriginalHeadMask(combatPose>=0?combatPixels[combatPose][i]:pixelCoordinates[i]);
            if(torsoArt.Ready)
            {
                float limb=torsoLimbMasks[combatPose>=0?combatPose+2:threadPose?1:0][i];
                if(!headArt.Ready)limb=Mathf.Max(limb,OriginalHeadMask(combatPose>=0?combatPixels[combatPose][i]:pixelCoordinates[i]));
                colors[i].a*=limb;
            }
            if(groundLegs.HasThreeQuarterArt&&!GroundActive)
            {
                Vector2 pixel=combatPose>=0?combatPixels[combatPose][i]:pixelCoordinates[i];
                float keep=combatPose>=0?1-Smooth(640,675,pixel.y):threadPose?
                    (WallPoseActive?Mathf.Max(1-Smooth(850,920,pixel.y),ThreadWeaponMask(pixel)):1-Smooth(850,920,pixel.y)):bindings[i].legs.body;
                colors[i].a*=keep;
            }
        }
        mesh.vertices=vertices; mesh.colors=colors; mesh.RecalculateBounds();
        heldWeaponArt.Show(detachedHeldWeapon,transform,material,rendererMesh,rest,heldWeaponArt.Ready?null:mesh.uv,heldWeaponArt.Ready?null:mesh.triangles,PoseHeldWeapon,VisualFacing,tint,combatSource!=null?combatSource.PresentationWeapon:null,threadPose?0:weaponCarry.OneHandWeight,PoseFreePalm,threadPose?0:layeredCombatPose.weaponYaw*combatLayerWeight,threadPose?0:layeredCombatPose.weaponRoll*combatLayerWeight);
        threadWeaponArt.Show(threadPose&&!WallPoseActive&&!customWeapon,transform,material,rendererMesh,rest,pixelCoordinates,
            threadWeaponArt.Ready?null:mesh.uv,threadWeaponArt.Ready?null:mesh.triangles,threadWeaponGrip,threadWeaponAngle,VisualFacing,tint);
        if(ledgeArmActive)ledgeArm.Show(true,transform,material,rendererMesh,rest,pixelCoordinates,ledgeArm.Ready?null:mesh.uv,ledgeArm.Ready?null:mesh.triangles,ledgeShoulder,ledgeElbow,ledgeHand,VisualFacing,tint);
        else ledgeArm.Hide();
        if(GroundActive)for(int i=0;i<2;i++)groundLegs.Render(i,legs[i].posedHip,legs[i].posedKnee,legs[i].posedAnkle,VisualFacing,tint);
        if(combatPose>=0)
        {
            PublishCombatJoints();ShowHead(tint);if(eyelid!=null)eyelid.enabled=false;return;
        }
        if(threadPose)
        {
            PublishThreadJoints();
            if(customWeapon)joints["WeaponTip"].position=heldWeaponArt.CustomTip(combatSource.PresentationWeapon);
            if(ledgeArmActive)
            {
                joints["RightShoulder"].localPosition=Mirror(ledgeShoulder);
                joints["RightElbow"].localPosition=Mirror(ledgeElbow);
                joints["RightHand"].localPosition=Mirror(ledgeHand);
            }
            if(eyelid!=null)eyelid.enabled=false;
            ShowHead(tint);
            return;
        }
        Joint("Root",Vector2.zero); Joint("Pelvis",pelvis+new Vector2(0,pelvisY)+airPoseOffset);
        for(int i=0;i<2;i++)
        {
            Joint(HipNames[i],legs[i].posedHip);Joint(KneeNames[i],legs[i].posedKnee);Joint(FootNames[i],legs[i].posedSole);
            legs[i].worldFoot=joints[FootNames[i]].position;
            joints[HipNames[i]].localRotation=Quaternion.Euler(0,0,Vector2.SignedAngle(legs[i].knee-legs[i].hip,legs[i].posedKnee-legs[i].posedHip)*VisualFacing);
            joints[KneeNames[i]].localRotation=Quaternion.Euler(0,0,Vector2.SignedAngle(legs[i].ankle-legs[i].knee,legs[i].posedAnkle-legs[i].posedKnee)*VisualFacing);
        }
        Joint("LeftShoulder",Upper(Pixel(624,578))); Joint("RightShoulder",Upper(Pixel(717,591)));
        Joint("LeftElbow",Arm(Pixel(588,697),new Vector2(588,697))); Joint("RightElbow",Arm(Pixel(756,747),new Vector2(756,747)));
        Joint("LeftHand",Arm(Pixel(647,824),new Vector2(647,824))); Joint("RightHand",Arm(Pixel(817,850),new Vector2(817,850)));
        Joint("Neck",Upper(neck)); Joint("Head",Head(Pixel(731,395)));
        Joint("LeftEar",Head(Pixel(653,290))); Joint("RightEar",Head(Pixel(624,337)));
        Joint("CloakMount",Brooch); Joint("WeaponMount",Arm(Pixel(817,850),new Vector2(817,850)));
        Joint("WeaponTip",Arm(Pixel(1180,943),new Vector2(1180,943)));
        if(detachedHeldWeapon)
        {
            Joint("WeaponMount",Upper(weaponCarry.Grip));
            Joint("WeaponTip",PoseHeldWeapon(Pixel(1180,943)));
            if(combatSource.PresentationWeapon!=null&&combatSource.PresentationWeapon.weaponArtwork!=null)joints["WeaponTip"].position=heldWeaponArt.CustomTip(combatSource.PresentationWeapon);
        }
        joints["Head"].localRotation=Quaternion.Euler(0,0,(torsoAngle+headAngle+(Secondary!=null?Secondary.AttentionAngle:0))*VisualFacing);
        joints["WeaponMount"].localRotation=joints["RightHand"].localRotation=Quaternion.Euler(0,0,torsoAngle*VisualFacing);
        joints["CloakMount"].localRotation=Quaternion.Euler(0,0,torsoAngle*VisualFacing);
        ShowHead(tint);
        if(eyelid!=null)
        {
            float blink=Secondary!=null?Secondary.Blink:0;
            eyelid.enabled=!headArt.Ready&&blink>.1f;
            if(eyelid.enabled)
            {
                Vector2 eye=Turn(Head(Pixel(735,397)));
                eyelid.transform.localPosition=new Vector3(eye.x*VisualFacing,eye.y,0);
                eyelid.transform.localRotation=Quaternion.Euler(0,0,(torsoAngle+headAngle+Secondary.AttentionAngle)*VisualFacing);
                float scale=1.7f/eyelid.sprite.bounds.size.y;
                eyelid.transform.localScale=new Vector3(scale*turnWidth,scale,1);
                eyelid.flipX=VisualFacing<0;
                Color eyeTint=tint;eyeTint.a*=Smooth(.1f,.65f,blink);eyelid.color=eyeTint;
            }
        }
    }
    void ThreadJoint(string name,float x,float y)
    {
        joints[name].localPosition=Mirror(ThreadSkin(Pixel(x,y),new Vector2(x,y)));
    }
    float OriginalHeadMask(Vector2 pixel)
    {
        if(combatPose==0)return 1-Smooth(440,458,pixel.y);
        if(combatPose==1)
        {
            float bottom=Mathf.Lerp(pixel.x*.5f+35,510,Smooth(810,900,pixel.x));
            return Smooth(435,465,pixel.x)*(1-Smooth(bottom-6,bottom+6,pixel.y));
        }
        if(threadPose)
        {
            float bottom=Mathf.Lerp(605,495,Smooth(590,690,pixel.x));
            return (1-Smooth(bottom-6,bottom+6,pixel.y))*(1-QoriLedgeArm.Mask(pixel));
        }
        return 1-Smooth(486,505,pixel.y);
    }
    void ShowHead(Color tint)
    {
        Vector2 anchor=joints["Neck"].localPosition;anchor.x*=VisualFacing;
        Vector2 hip=joints["Pelvis"].localPosition;hip.x*=VisualFacing;
        torsoArt.Show(anchor,hip,VisualFacing,tint);
        if(groundLegs.HasThreeQuarterArt&&!GroundActive)RenderActionLegs(tint);
        float angle=combatPose>=0?combatAngle:threadPose?ledgeBodyLean:torsoAngle+headAngle+(Secondary!=null?Secondary.AttentionAngle:0);
        // Hanging uses the torso's full rotation. Clamping just the head left
        // its painted neck behind when the collar tilted past twelve degrees.
        bool swinging=threadPose&&!WallPoseActive||Swing!=null&&Swing.Releasing&&!GroundActive;
        if(swinging)
        {
            float weight=threadPose?1:Swing.Weight;
            anchor-=(anchor-hip).normalized*(.16f*weight);
            if(Swing!=null)angle-=Mathf.Clamp(Swing.TorsoAngle*.18f,-5,5)*weight;
        }
        headArt.Show(anchor,VisualFacing,swinging?angle:Mathf.Clamp(angle,-12,12),tint,movement,combatSource,Secondary,movement.IsLedgeClimbing,swinging);
        CapturePose();
    }
    Vector2 BridgeLocal(Vector2 relative)
    {Vector3 p=transform.InverseTransformPoint(movement.transform.position+(Vector3)relative);p.x*=VisualFacing;return p;}
    float BridgeAngle()=>Vector2.SignedAngle(Vector2.up,BridgeLocal(bridgeNeck)-BridgeLocal(bridgePelvis));
    void SaveBridge()
    {
        bridgeNeck=lastNeck;bridgePelvis=lastPelvis;bridgeHand=lastHand;
        bridgeWeaponGrip=lastWeaponGrip;bridgeWeaponTip=lastWeaponTip;
        for(int i=0;i<2;i++){bridgeHips[i]=lastHips[i];bridgeKnees[i]=lastKnees[i];bridgeAnkles[i]=lastAnkles[i];}
    }
    void BlendBridgeLeg(Leg leg,int i,float weight)
    {
        Vector2 h=BridgeLocal(bridgeHips[i]),k=BridgeLocal(bridgeKnees[i]),a=BridgeLocal(bridgeAnkles[i]);
        float upper=Mathf.LerpAngle(Vector2.SignedAngle(Vector2.down,leg.posedKnee-leg.posedHip),Vector2.SignedAngle(Vector2.down,k-h),weight);
        float lower=Mathf.LerpAngle(Vector2.SignedAngle(Vector2.down,leg.posedAnkle-leg.posedKnee),Vector2.SignedAngle(Vector2.down,a-k),weight);
        // Blend limb rotations around the current torso socket; saved world hips can
        // drift away from the torso when release and re-grab overlap.
        leg.posedKnee=leg.posedHip+Rotate(Vector2.down*groundLegs.UpperLength,upper);
        leg.posedAnkle=leg.posedKnee+Rotate(Vector2.down*groundLegs.LowerLength,lower);
        leg.posedSole=leg.posedAnkle+groundLegs.SoleOffset;
    }
    void CapturePose()
    {
        Vector2 root=movement.transform.position;
        lastNeck=(Vector2)joints["Neck"].position-root;lastPelvis=(Vector2)joints["Pelvis"].position-root;lastHand=(Vector2)joints["RightHand"].position-root;
        lastWeaponGrip=(Vector2)joints["LeftHand"].position-root;lastWeaponTip=(Vector2)joints["WeaponTip"].position-root;
        for(int i=0;i<2;i++)
        {
            Leg leg=threadPose?threadLegs[i]:combatPose>=0?combatLegs[i]:legs[i];
            lastHips[i]=(Vector2)transform.TransformPoint(Mirror(leg.posedHip))-root;
            lastKnees[i]=(Vector2)transform.TransformPoint(Mirror(leg.posedKnee))-root;
            lastAnkles[i]=(Vector2)transform.TransformPoint(Mirror(leg.posedAnkle))-root;
        }
        hasPose=true;
    }
    float ThreadWeaponMask(Vector2 pixel)
    {
        float shaft=550+(pixel.y-730)*.79f;
        return Mathf.Max(1-Smooth(22,38,Mathf.Abs(pixel.x-shaft)),Smooth(950,1020,pixel.y)*Smooth(780,840,pixel.x));
    }
    void RenderActionLegs(Color tint)
    {
        for(int i=0;i<2;i++)
        {
            Leg leg=threadPose?threadLegs[i]:legs[i];
            if(combatPose>=0)
            {
                leg=combatLegs[i];Vector2 h=joints[HipNames[i]].localPosition,f=joints[FootNames[i]].localPosition;
                h.x*=VisualFacing;f.x*=VisualFacing;leg.posedHip=h;
                if(movement.IsGrounded)
                {
                    f.y=transform.InverseTransformPoint(new Vector3(transform.position.x,FloorY,transform.position.z)).y;
                    float length=groundLegs.UpperLength+groundLegs.LowerLength-.005f;
                    float vertical=f.y-groundLegs.SoleOffset.y-h.y;
                    float reach=Mathf.Sqrt(Mathf.Max(0,length*length-vertical*vertical));
                    f.x=h.x+groundLegs.SoleOffset.x+Mathf.Clamp(f.x-h.x-groundLegs.SoleOffset.x,-reach,reach);
                }
                SolveGroundLeg(leg,f);
            }
            float footAngle=!threadPose&&combatPose<0?(State=="Rise"?-12:State=="Apex"?-5:State=="PreLanding"?3:0):0;
            groundLegs.Render(i,leg.posedHip,leg.posedKnee,leg.posedAnkle,VisualFacing,tint,footAngle);
        }
    }
    void CombatJoint(string name,float x,float y)
    {
        joints[name].localPosition=Mirror(CombatSkin(CombatPoint(x,y),new Vector2(x,y)));
        joints[name].localRotation=Quaternion.Euler(0,0,combatAngle*VisualFacing);
    }
    void PublishCombatJoints()
    {
        bool windup=combatPose==1;
        CombatJoint("Root",windup?924:768,512);CombatJoint("Pelvis",windup?810:615,windup?620:595);
        CombatJoint("LeftHip",windup?727:535,711);CombatJoint("LeftKnee",windup?590:410,806);CombatJoint("LeftFoot",windup?440:295,990);
        CombatJoint("RightHip",windup?907:685,678);CombatJoint("RightKnee",windup?985:797,740);CombatJoint("RightFoot",windup?922:788,980);
        CombatJoint("LeftShoulder",windup?893:673,485);CombatJoint("LeftElbow",windup?679:799,500);CombatJoint("LeftHand",windup?595:943,windup?385:518);
        CombatJoint("RightShoulder",windup?913:701,503);CombatJoint("RightElbow",windup?843:863,windup?526:525);CombatJoint("RightHand",windup?710:995,windup?427:503);
        CombatJoint("Neck",windup?947:677,windup?480:425);CombatJoint("Head",windup?1030:753,windup?401:335);
        CombatJoint("LeftEar",windup?940:657,windup?293:252);CombatJoint("RightEar",windup?891:620,windup?317:310);
        joints["CloakMount"].localPosition=Mirror(Brooch);
        CombatJoint("WeaponMount",windup?710:995,windup?427:503);
        CombatJoint("WeaponTip",windup?60:1520,windup?110:502);
    }
    void PublishThreadJoints()
    {
        ThreadJoint("Root",591,665);ThreadJoint("Pelvis",744,790);
        ThreadJoint("LeftHip",719,875);ThreadJoint("LeftKnee",662,1010);ThreadJoint("LeftFoot",580,1260);
        ThreadJoint("RightHip",790,861);ThreadJoint("RightKnee",748,993);ThreadJoint("RightFoot",700,1250);
        ThreadJoint("LeftShoulder",703,564);ThreadJoint("LeftElbow",660,697);ThreadJoint("LeftHand",650,825);
        ThreadJoint("RightShoulder",798,510);ThreadJoint("RightElbow",835,300);ThreadJoint("RightHand",865,145);
        ThreadJoint("Neck",750,480);ThreadJoint("Head",780,370);ThreadJoint("LeftEar",684,292);ThreadJoint("RightEar",669,373);
        ThreadJoint("CloakMount",790,540);ThreadJoint("WeaponMount",650,825);
        ThreadJoint("WeaponTip",975,1260);
        for(int i=0;i<2;i++)
        {
            Joint(HipNames[i],threadLegs[i].posedHip);Joint(KneeNames[i],threadLegs[i].posedKnee);Joint(FootNames[i],threadLegs[i].posedSole);
        }
    }
    public void Hide(){BodyActive=GroundActive=false;tracking=false;groundLegs.Hide();ledgeArm.Hide();threadWeaponArt.Hide();heldWeaponArt.Hide();headArt.Hide();torsoArt.Hide();if(rendererMesh!=null)rendererMesh.enabled=false;if(eyelid!=null)eyelid.enabled=false;}
    void OnDisable()=>Hide();
    void OnDestroy()
    {
        ledgeArm.Dispose();
        threadWeaponArt.Dispose();
        heldWeaponArt.Dispose();
        headArt.Dispose();
        torsoArt.Dispose();
        groundLegs.Dispose();
        if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);
        if(rendererMesh!=null)Destroy(rendererMesh.gameObject);if(eyelid!=null)Destroy(eyelid.gameObject);if(rigRoot!=null)Destroy(rigRoot.gameObject);
    }
}

// A rigid cutout avoids stretching triangles between the shaft and the thigh
// where both were painted onto the same source canvas.
sealed class QoriThreadWeaponArt
{
    Mesh mesh;MeshRenderer renderer;Vector3[] source,vertices;Color[] colors;float[] alpha;
    public bool Ready=>mesh!=null;
    static float Smooth(float a,float b,float v)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));
    public static float Mask(Vector2 p)
    {
        float shaftX=650+(p.y-825)*.82f;
        float shaft=(1-Smooth(15,22,Mathf.Abs(p.x-shaftX)))*Smooth(697,709,p.y)*(1-Smooth(1038,1070,p.y));
        float blade=Smooth(801,815,p.x)*Smooth(1010,1030,p.y);
        float palm=1-Smooth(24,34,Vector2.Distance(p,new Vector2(650,825)));
        return Mathf.Max(Mathf.Max(shaft,blade),palm);
    }
    public void Show(bool active,Transform parent,Material material,MeshRenderer body,Vector3[] rest,Vector2[] pixels,Vector2[] uv,int[] triangles,Vector2 grip,float angle,float facing,Color tint)
    {
        if(!active){Hide();return;}
        if(mesh==null)
        {
            var ids=new List<int>();var map=new Dictionary<int,int>();var indices=new List<int>();
            for(int t=0;t<triangles.Length;t+=3)
            {
                if(Mask(pixels[triangles[t]])<=0&&Mask(pixels[triangles[t+1]])<=0&&Mask(pixels[triangles[t+2]])<=0)continue;
                for(int j=0;j<3;j++){int id=triangles[t+j];if(!map.TryGetValue(id,out int compact)){compact=ids.Count;map.Add(id,compact);ids.Add(id);}indices.Add(compact);}
            }
            source=new Vector3[ids.Count];vertices=new Vector3[ids.Count];colors=new Color[ids.Count];alpha=new float[ids.Count];var coords=new Vector2[ids.Count];
            for(int i=0;i<ids.Count;i++){source[i]=rest[ids[i]];coords[i]=uv[ids[i]];alpha[i]=Mask(pixels[ids[i]]);}
            mesh=new Mesh{name="Qori rigid thread weapon"};mesh.vertices=vertices;mesh.uv=coords;mesh.triangles=indices.ToArray();mesh.MarkDynamic();
            var obj=new GameObject(mesh.name);obj.transform.SetParent(parent,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerID=body.sortingLayerID;renderer.sortingOrder=body.sortingOrder+2;
        }
        float r=angle*Mathf.Deg2Rad,c=Mathf.Cos(r),s=Mathf.Sin(r);
        for(int i=0;i<vertices.Length;i++)
        {
            Vector2 p=(Vector2)source[i]-new Vector2(.59f,-1.60f);
            p=grip+new Vector2(p.x*c-p.y*s,p.x*s+p.y*c);
            vertices[i]=new Vector3(p.x*facing,p.y,0);colors[i]=tint;colors[i].a*=alpha[i];
        }
        mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();renderer.enabled=true;
    }
    public void Hide(){if(renderer!=null)renderer.enabled=false;}
    public void Dispose(){if(mesh!=null)UnityEngine.Object.Destroy(mesh);if(renderer!=null)UnityEngine.Object.Destroy(renderer.gameObject);}
}

// The exposed forearm and hand are cut from the approved raised-arm texture.
// A small wood-textured upper-arm strip fills the surface hidden behind the head.
sealed class QoriLedgeArm
{
    public bool Ready=>mesh!=null;
    public void Hide(){if(renderer!=null)renderer.enabled=false;}
    Mesh mesh;MeshRenderer renderer;Vector3[] source,vertices;Color[] colors;float[] alpha;
    static float Smooth(float a,float b,float v)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));
    public static float Mask(Vector2 pixel)
    {
        float edge=pixel.y<290?Mathf.Lerp(807,818,Smooth(220,290,pixel.y)):Mathf.Lerp(818,860,Smooth(290,350,pixel.y));
        return Smooth(edge-2,edge+2,pixel.x)*(1-Smooth(338,352,pixel.y));
    }
    public void Show(bool active,Transform parent,Material material,MeshRenderer body,Vector3[] rest,Vector2[] pixels,Vector2[] uv,int[] triangles,Vector2 shoulder,Vector2 elbow,Vector2 hand,float facing,Color tint)
    {
        if(!active){if(renderer!=null)renderer.enabled=false;return;}
        if(mesh==null)
        {
            var map=new Dictionary<int,int>();var ids=new List<int>();var indices=new List<int>();
            for(int t=0;t<triangles.Length;t+=3)
            {
                if(Mask(pixels[triangles[t]])<=0 && Mask(pixels[triangles[t+1]])<=0 && Mask(pixels[triangles[t+2]])<=0)continue;
                for(int j=0;j<3;j++){int id=triangles[t+j];if(!map.TryGetValue(id,out int compact)){compact=ids.Count;map.Add(id,compact);ids.Add(id);}indices.Add(compact);}
            }
            int count=ids.Count;source=new Vector3[count];vertices=new Vector3[count+4];colors=new Color[count+4];alpha=new float[count];var coords=new Vector2[count+4];
            for(int i=0;i<count;i++){source[i]=rest[ids[i]];coords[i]=uv[ids[i]];alpha[i]=Mask(pixels[ids[i]]);}
            coords[count]=new Vector2(836f/1182,1-225f/1330);coords[count+1]=new Vector2(850f/1182,1-225f/1330);
            coords[count+2]=new Vector2(850f/1182,1-275f/1330);coords[count+3]=new Vector2(836f/1182,1-275f/1330);
            indices.AddRange(new[]{count,count+1,count+2,count,count+2,count+3});
            mesh=new Mesh{name="Qori ledge pulling arm"};mesh.vertices=vertices;mesh.uv=coords;mesh.triangles=indices.ToArray();mesh.MarkDynamic();
            var obj=new GameObject(mesh.name);obj.transform.SetParent(parent,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerID=body.sortingLayerID;
        }
        renderer.enabled=true;
        // Keep the supporting arm behind the face, without tying its sorting order.
        renderer.sortingOrder=body.sortingOrder;
        Vector2 originalHand=new Vector2(2.74f,5.20f),originalElbow=new Vector2(2.44f,3.65f);
        float angle=Vector2.SignedAngle(originalHand-originalElbow,hand-elbow)*Mathf.Deg2Rad;
        float c=Mathf.Cos(angle),s=Mathf.Sin(angle);
        for(int i=0;i<source.Length;i++)
        {
            Vector2 p=(Vector2)source[i]-originalHand;
            p=hand+new Vector2(c*p.x-s*p.y,s*p.x+c*p.y);
            vertices[i]=new Vector3(p.x*facing,p.y,0);colors[i]=tint;colors[i].a*=alpha[i];
        }
        Vector2 normal=new Vector2(-(elbow-shoulder).y,(elbow-shoulder).x).normalized*.105f;
        Vector2[] strip={shoulder-normal,shoulder+normal,elbow+normal,elbow-normal};
        for(int j=0;j<4;j++){vertices[source.Length+j]=new Vector3(strip[j].x*facing,strip[j].y,0);colors[source.Length+j]=tint;}
        mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();
    }
    public void Dispose(){if(mesh!=null)UnityEngine.Object.Destroy(mesh);if(renderer!=null)UnityEngine.Object.Destroy(renderer.gameObject);}
}







