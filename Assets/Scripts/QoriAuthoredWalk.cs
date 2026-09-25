using UnityEngine;

// Opt-in animation experiment. Only the comparison scene adds this component.
// Clip channels are normal transform curves, editable in Unity's Animation window.
public sealed class QoriAuthoredWalk : MonoBehaviour
{
    public AnimationClip walk;
    public bool useAuthoredWalk = true;
    public bool useAuthoredSword = true;
    AttackDefinition sword;
    AttackAnimationProfile originalSword, authoredSword;
    PlayerCombat combat;
    float originalStartup,originalActive,originalRecovery,originalCooldown;
    bool swordApplied;
    readonly AttackDefinition[] directional = new AttackDefinition[2];
    readonly AttackAnimationProfile[] originalDirectional = new AttackAnimationProfile[2], slashDirectional = new AttackAnimationProfile[2];
    readonly Vector4[] directionalTimes = new Vector4[2];
    [Range(.65f,1.4f)] public float cycleSeconds = .95f;
    Transform pose, foot, body;
    float clock, weight;
    public float Weight => weight;
    public Vector3 Body => body == null ? Vector3.zero : body.localPosition;

    System.Collections.IEnumerator Start()
    {
        yield return null; // Armory builds its private weapon copies in Start.
        var armory=GetComponent<QoriArmory>();combat=GetComponent<PlayerCombat>();
        if(armory==null || armory.Weapons==null)yield break;
        sword=armory.Weapons[0].moveSet.Find(CombatMoveSlot.Front);
        originalSword=sword.animation;originalStartup=sword.startup;originalActive=sword.active;
        originalRecovery=sword.recovery;originalCooldown=sword.cooldown;
        authoredSword=Instantiate(originalSword);authoredSword.name="Sword — broad committed cut prototype";
        authoredSword.anticipation=new AttackAnimationProfile.Pose{leftGrip=new Vector2(-.75f,.8f),weaponAngle=145,torsoAngle=18,compression=.24f,shoulderReach=new Vector2(-.22f,.1f),freeHand=new Vector2(.65f,-.25f)};
        authoredSword.contact=new AttackAnimationProfile.Pose{leftGrip=new Vector2(2.7f,.1f),weaponAngle=-5,torsoAngle=-18,compression=.08f,shoulderReach=new Vector2(.55f,.02f),freeHand=new Vector2(-.25f,-.65f)};
        authoredSword.followThrough=new AttackAnimationProfile.Pose{leftGrip=new Vector2(1.55f,-1.45f),weaponAngle=-72,torsoAngle=-12,compression=.20f,shoulderReach=new Vector2(.25f,-.1f),freeHand=new Vector2(.1f,-.7f)};
        authoredSword.contactFraction=.58f;authoredSword.recoveryHold=.34f;
        authoredSword.oneHanded=true;authoredSword.straightTrail=false;authoredSword.sweepThroughActive=true;
        authoredSword.swingTiming=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(.25f,.08f,.8f,.8f),new Keyframe(.6f,.7f,1.7f,1.7f),new Keyframe(1,1,0,0));
        for(int i=0;i<2;i++)
        {
            var move=armory.Weapons[0].moveSet.Find(i==0?CombatMoveSlot.Upper:CombatMoveSlot.Lower);
            directional[i]=move;originalDirectional[i]=move.animation;
            directionalTimes[i]=new Vector4(move.startup,move.active,move.recovery,move.cooldown);
            var slash=Instantiate(authoredSword);slashDirectional[i]=slash;
            slash.name=i==0?"Sword overhead crescent":"Sword descending crescent";
            slash.contactFraction=.55f;slash.recoveryHold=.28f;
            if(i==0)
            {
                slash.anticipation=new AttackAnimationProfile.Pose{leftGrip=new Vector2(2.3f,-.6f),weaponAngle=-10,torsoAngle=10,compression=.20f,freeHand=new Vector2(.2f,-.6f)};
                slash.contact=new AttackAnimationProfile.Pose{leftGrip=new Vector2(1.05f,4.5f),weaponAngle=104,torsoAngle=-3,compression=-.08f,shoulderReach=new Vector2(.1f,.95f),freeHand=new Vector2(-.35f,-.7f)};
                slash.followThrough=new AttackAnimationProfile.Pose{leftGrip=new Vector2(-1.1f,2.9f),weaponAngle=195,torsoAngle=9,compression=.03f,shoulderReach=new Vector2(-.2f,.35f),freeHand=new Vector2(.25f,-.6f)};
            }
            else
            {
                slash.anticipation=new AttackAnimationProfile.Pose{leftGrip=new Vector2(2.1f,1.2f),weaponAngle=25,torsoAngle=-8,compression=.05f,kneeBend=.4f,freeHand=new Vector2(.5f,.1f)};
                slash.contact=new AttackAnimationProfile.Pose{leftGrip=new Vector2(.5f,-2.95f),weaponAngle=-76,torsoAngle=15,compression=.04f,kneeBend=.35f,shoulderReach=new Vector2(.1f,-.7f),freeHand=new Vector2(.3f,1.1f)};
                slash.followThrough=new AttackAnimationProfile.Pose{leftGrip=new Vector2(-1.45f,-1.9f),weaponAngle=-172,torsoAngle=10,compression=.08f,kneeBend=.25f,shoulderReach=new Vector2(-.25f,-.35f),freeHand=new Vector2(.5f,.65f)};
            }
        }
        Update();
    }
    void Update()
    {
        if(sword==null || combat.IsAttackPoseActive || swordApplied==useAuthoredSword)return;
        swordApplied=useAuthoredSword;sword.animation=swordApplied?authoredSword:originalSword;
        sword.startup=swordApplied?.28f:originalStartup;sword.active=swordApplied?.23f:originalActive;
        sword.recovery=swordApplied?.38f:originalRecovery;sword.cooldown=swordApplied?.915f:originalCooldown;
        QoriArmoryFactory.RefreshSwordCombo(GetComponent<QoriArmory>().Weapons[0]);
        for(int i=0;i<2;i++)
        {
            var move=directional[i];var times=directionalTimes[i];
            move.animation=swordApplied?slashDirectional[i]:originalDirectional[i];
            move.startup=swordApplied?.23f:times.x;move.active=swordApplied?.34f:times.y;
            move.recovery=swordApplied?.32f:times.z;move.cooldown=swordApplied?.915f:times.w;
        }
    }
    void OnDestroy(){if(authoredSword!=null)Destroy(authoredSword);foreach(var slash in slashDirectional)if(slash!=null)Destroy(slash);}

    void EnsurePose()
    {
        if(pose != null) return;
        pose = new GameObject("Walk clip controls").transform;
        pose.SetParent(transform, false);
        foot = new GameObject("Foot").transform; foot.SetParent(pose,false);
        body = new GameObject("Body").transform; body.SetParent(pose,false);
    }
    public void Step(float speed, float dt, bool reset, bool eligible)
    {
        EnsurePose();
        if(reset) { clock=0; weight=0; }
        float target = useAuthoredWalk && walk != null && eligible ? Mathf.InverseLerp(.08f,2f,Mathf.Abs(speed)) : 0;
        weight = Mathf.MoveTowards(weight,target,dt/ .16f);
        if(target>0) clock=Mathf.Repeat(clock+dt/cycleSeconds,1);
        if(walk!=null) walk.SampleAnimation(pose.gameObject,clock*walk.length);
    }
    public Vector3 Foot(int leg)
    {
        if(walk==null) return Vector3.zero;
        walk.SampleAnimation(pose.gameObject,Mathf.Repeat(clock+(leg==0?0:.5f),1)*walk.length);
        Vector3 result=foot.localPosition;
        walk.SampleAnimation(pose.gameObject,clock*walk.length);
        return result;
    }
    void OnGUI()
    {
        GUI.Box(new Rect(12,12,520,144),"Qori walk + SWORD SWING comparison — prototype");
        useAuthoredWalk=GUI.Toggle(new Rect(26,38,385,24),useAuthoredWalk," Authored walk (untick for current animation)");
        GUI.Label(new Rect(26,65,390,24),"A/D: walk · Shift: existing run · Space: existing jump");
        GUI.Label(new Rect(26,89,480,24),"Click: slash · W+click: above · Air S+click: below · 1–4: weapon");
        useAuthoredSword=GUI.Toggle(new Rect(26,117,480,24),useAuthoredSword," New sword swing — select 1, then left click (toggle at rest)");
    }
}
