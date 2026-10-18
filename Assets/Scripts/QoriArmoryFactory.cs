using System.Collections.Generic;
using UnityEngine;

// Runtime copies keep the approved Reedblade assets immutable.
public static class QoriArmoryFactory
{
    // The mace's world length, grip to the far side of its head (the sword is 7.4, the spear 7.5):
    // the heavy weapon is drawn the longest. Its art is the heavy leaf-plated maul the user chose
    // (Tools/ArtImport/install_weapon_art.py installs it and the spear).
    const float MaceLength=7.9f;
    // The Leaf Staff is held a hand-width up from its butt end; its grip-to-tip length is chosen to
    // draw it the same overall length as the sword: 7.4 / (1 - .17 sword grip) x (1 - .16 staff grip).
    const float StaffLength=7.489f;
    public const string StaffId="forest-staff";
    static T Copy<T>(T value,List<Object> owned) where T:Object{var copy=Object.Instantiate(value);owned.Add(copy);return copy;}
    public static WeaponDefinition[] Build(WeaponDefinition original,List<Object> owned,out WeaponDefinition slingWeapon,out AttackDefinition sling)
    {
        // The weapons, keyed by their permanent ids (mechanics and saves use them): the Leaf Sword,
        // the Seedpod Mace, the Thorn Spear and the Leaf Staff, Qori's starting weapon (QoriArmory
        // equips it). (forest-1, the Vine Whip, was removed.)
        string[] ids={"forest-0","forest-2","forest-3",StaffId},names={"Leaf Sword","Seedpod Mace","Thorn Spear","Leaf Staff"};
        int[] artRows={0,1,2,0};string[] cleanArt={"LeafSword","SeedpodMace","ThornSpear","LeafStaff"};float[] lengths={7.4f,MaceLength,7.5f,StaffLength};
        var result=new WeaponDefinition[ids.Length];
        for(int n=0;n<ids.Length;n++)
        {
            int i=n==0?0:n+1;   // 0 sword, 2 mace, 3 spear, 4 staff: the tuning below is keyed by the old slot numbers
            var w=Copy(original,owned);result[n]=w;w.name=w.displayName=names[n];w.weaponId=ids[n];w.damageMultiplier=1;
            ApplyArt(w,artRows[n],lengths[n],owned);
            ApplyCleanArt(w,cleanArt[n],lengths[n],owned);
            w.trailColor=i==2?new Color(.8f,.65f,.32f):new Color(.8f,.94f,.5f);
            w.moveSet=ScriptableObject.CreateInstance<CombatMoveSet>();owned.Add(w.moveSet);w.moveSet.moves=new CombatMoveSet.Entry[3];
            for(int a=0;a<3;a++)
            {
                var slot=a==0?CombatMoveSlot.Front:a==1?CombatMoveSlot.Upper:CombatMoveSlot.Lower;
                var move=Copy(original.moveSet.Find(slot),owned);move.name=move.attackId=names[n]+" "+(AttackAim)a;
                move.animation=Copy(move.animation,owned);move.followUps=new AttackDefinition[0];
                // Phases, damage, knockback and impact feedback come from WeaponTuning (ApplyTuning, below).
                move.bladeRadius=i==2?.24f:i==4?.14f:.10f;move.bladeStart=i==3?.6f:i==2||i==4?.7f:.15f;   // the staff hits with its leaf blade
                var p=move.animation;p.oneHanded=i==0||a!=0;p.straightTrail=i==3||a!=0;p.sweepThroughActive=!p.straightTrail;
                if(a==0)
                {
                    var wind=p.anticipation;var contact=p.contact;var follow=p.followThrough;
                    if(i==2){wind.weaponAngle=105;wind.leftGrip=new Vector2(.2f,.8f);wind.torsoAngle=12;contact.weaponAngle=-35;contact.leftGrip=new Vector2(2,-.6f);contact.compression=.25f;follow.weaponAngle=-75;follow.leftGrip=new Vector2(1.8f,-1.1f);follow.compression=.2f;}
                    if(i==3){wind.weaponAngle=contact.weaponAngle=follow.weaponAngle=0;wind.leftGrip=new Vector2(-.6f,-.15f);contact.leftGrip=new Vector2(2.65f,-.05f);follow.leftGrip=new Vector2(1.9f,-.05f);}
                    wind.freeHand=contact.freeHand=follow.freeHand=new Vector2(.5f,-.5f);
                    p.anticipation=wind;p.contact=contact;p.followThrough=follow;
                }
                w.moveSet.moves[a]=new CombatMoveSet.Entry{slot=slot,attack=move};
            }
        }
        var opener=result[0].moveSet.Find(CombatMoveSlot.Front);
        var reverse=Copy(opener,owned);reverse.animation=Copy(opener.animation,owned);
        var quick=Copy(opener,owned);quick.animation=Copy(opener.animation,owned);
        var finish=Copy(opener,owned);finish.animation=Copy(opener.animation,owned);
        reverse.name=reverse.attackId="Leaf Sword 2 - Quick front cut";
        finish.name=finish.attackId="Leaf Sword 4 - Depth sweep";
        quick.name=quick.attackId="Leaf Sword 3 - Quick return cut";
        opener.followUps=new[]{reverse};reverse.followUps=new[]{quick};quick.followUps=new[]{finish};finish.followUps=new AttackDefinition[0];
        RefreshSwordCombo(result[0]);
        BuildStaffMoves(result[3],owned);
        slingWeapon=Copy(result[0],owned);slingWeapon.name=slingWeapon.displayName="Leaf Bolt";slingWeapon.weaponId="resin-sling";
        sling=Copy(result[0].moveSet.Find(CombatMoveSlot.Front),owned);sling.name=sling.attackId="Resin seed";sling.slingProjectile=true;
        sling.followUps=new AttackDefinition[0];
        sling.impulse=Vector2.zero;sling.recoil=Vector2.zero;sling.lockFacing=true;
        sling.animation=Copy(sling.animation,owned);sling.animation.oneHanded=true;sling.animation.straightTrail=true;sling.animation.sweepThroughActive=false;
        // A compact cocked wrist, forward cast, then relaxed follow-through.
        sling.animation.contactFraction=.55f;sling.animation.secondaryImpulse=.1f;
        sling.animation.anticipation=new AttackAnimationProfile.Pose{weaponAngle=75,leftGrip=new Vector2(-.3f,.25f),freeHand=new Vector2(.7f,-.55f),torsoAngle=5,shoulderReach=new Vector2(-.08f,0)};
        sling.animation.contact=new AttackAnimationProfile.Pose{weaponAngle=5,leftGrip=new Vector2(2.35f,.15f),freeHand=new Vector2(.2f,-.7f),torsoAngle=-5,shoulderReach=new Vector2(.22f,.04f)};
        sling.animation.followThrough=new AttackAnimationProfile.Pose{weaponAngle=-25,leftGrip=new Vector2(1.5f,-.4f),freeHand=new Vector2(.35f,-.6f),torsoAngle=-2,shoulderReach=new Vector2(.08f,0)};
        slingWeapon.artworkScale*=.55f;
        ApplyTuning(result,sling);
        return result;
    }
    // Writes WeaponTuning's numbers into built weapons: every attack's phases, damage, knockback and
    // impact feedback, the Leaf Sword's combo follow-ups and the sling. Called after Build, and again by
    // QoriArmory when the asset is edited in Play mode.
    public static void ApplyTuning(WeaponDefinition[] weapons,AttackDefinition sling)
    {
        var tuning=WeaponTuning.Current;
        if(weapons!=null)
            foreach(var weapon in weapons)
            {
                var entry=weapon!=null&&weapon.moveSet!=null?tuning.Find(weapon.weaponId):null;
                if(entry==null)continue;
                bool staff=weapon.weaponId==StaffId;
                foreach(var slot in weapon.moveSet.moves)
                {
                    var move=slot.attack;if(move==null)continue;
                    Apply(move,entry,(staff?StaffPhases(tuning,slot.slot):null)??entry.phases,tuning.cooldownPad);
                    if(staff)StaffExtras(move,slot.slot,tuning);
                    if(slot.slot!=CombatMoveSlot.Front||move.followUps==null||move.followUps.Length==0)continue;
                    // The Leaf Sword's and the Leaf Staff's combos: the follow-ups hit like the opener, on their own phases.
                    var combo=staff?new[]{tuning.staffBackswing,tuning.staffSpin}:new[]{tuning.swordQuickCut,tuning.swordReturnCut,tuning.swordDepthSweep};
                    var next=move.followUps[0];
                    for(int i=0;next!=null&&i<combo.Length;i++)
                    {
                        Apply(next,entry,combo[i],tuning.cooldownPad);
                        if(staff){if(i==1)next.knockback*=tuning.staffSpinKnockback;StaffComboFeel(next,i==1,tuning);}
                        next=next.followUps!=null&&next.followUps.Length>0?next.followUps[0]:null;
                    }
                }
            }
        if(sling!=null)
        {
            Phases(sling,tuning.slingPhases,0f);sling.cooldown=tuning.slingCooldown;
            sling.damage=tuning.slingDamage;sling.knockback=tuning.slingKnockback;
        }
    }
    static void Apply(AttackDefinition move,WeaponTuning.Weapon entry,WeaponTuning.Phases phases,float pad)
    {
        Phases(move,phases,pad);
        move.damage=entry.damage;move.knockback=entry.knockback;
        move.hitStop=entry.hitStop;move.cameraShake=entry.cameraShake;move.rumble=entry.rumble;
    }
    static void Phases(AttackDefinition move,WeaponTuning.Phases phases,float pad)
    {
        move.startup=phases.startup;move.active=phases.active;move.recovery=phases.recovery;move.cooldown=move.TotalDuration+pad;
    }
    // The Leaf Staff's set, drawn frame by frame (Ludo attack clips A1-A4): a three-hit combo (the opener, a
    // backswing, a spin finisher that hits all round), a spinning lunge when attacking while running and the charged
    // sweep (hold attack). In the air the opener is used (the flip became the double jump). The rig plays the opener's
    // swing for the new ones.
    static void BuildStaffMoves(WeaponDefinition staff,List<Object> owned)
    {
        var front=staff.moveSet.Find(CombatMoveSlot.Front);
        AttackDefinition Make(string id)
        {
            var m=Copy(front,owned);m.name=m.attackId="Leaf Staff "+id;m.animation=Copy(front.animation,owned);m.followUps=new AttackDefinition[0];return m;
        }
        void Around(AttackDefinition m,CombatHitShape shape,Vector2 size,Vector2 offset){m.hitShape=shape;m.hitboxSize=size;m.hitboxOffset=offset;m.multipleTargets=true;}
        var back=Make("2 - Backswing");var spin=Make("3 - Spin finisher");
        Around(spin,CombatHitShape.Circle,new Vector2(3f,3f),new Vector2(0,.3f));
        front.followUps=new[]{back};back.followUps=new[]{spin};
        foreach(var m in new[]{front,back,spin})m.comboWindowStart=.25f;
        var sprint=Make("Sprint lunge");sprint.allowAir=false;
        Around(sprint,CombatHitShape.Circle,new Vector2(2.6f,2.6f),new Vector2(.9f,.3f));
        // Down in the air: the plunge, and the burst where it lands (not in the move set: the plunge starts it).
        var plunge=staff.moveSet.Find(CombatMoveSlot.Lower);
        var landing=Make("Plunge landing");landing.direction=AttackAim.Down;landing.allowAir=true;landing.lockFacing=true;
        Around(landing,CombatHitShape.Circle,new Vector2(2.8f,2.8f),new Vector2(0,-.2f));
        plunge.plungeLanding=landing;plunge.airControl=.3f;plunge.lockFacing=true;plunge.allowGround=false;
        Around(plunge,CombatHitShape.Box,new Vector2(1.3f,1.6f),new Vector2(0,-.5f));   // the staff's point, under him all the way down
        var charged=Make("Charged sweep");charged.allowAir=false;charged.breaksArmor=true;   // cracks the Shellback's shell and rubble
        Around(charged,CombatHitShape.Box,new Vector2(3.6f,1.8f),new Vector2(1.5f,.3f));
        var moves=new List<CombatMoveSet.Entry>(staff.moveSet.moves)
        {
            new CombatMoveSet.Entry{slot=CombatMoveSlot.Dash,attack=sprint},
            new CombatMoveSet.Entry{slot=CombatMoveSlot.Charged,attack=charged},
        };
        staff.moveSet.moves=moves.ToArray();
    }
    static WeaponTuning.Phases StaffPhases(WeaponTuning tuning,CombatMoveSlot slot)=>
        slot==CombatMoveSlot.Front?tuning.staffOpener:slot==CombatMoveSlot.Upper?tuning.staffUpper:slot==CombatMoveSlot.Lower?tuning.staffPlunge:slot==CombatMoveSlot.Dash?tuning.staffSprint:slot==CombatMoveSlot.Charged?tuning.staffCharged:null;
    // The combo's weight and agility: a step forward per hit while the stick pushes forward (the drawn frames no longer
    // drift, and standing still he stays put), a harder landing
    // (pause, freeze, shake, rumble), and moving during the follow-through ends it.
    static void StaffComboFeel(AttackDefinition move,bool finisher,WeaponTuning tuning)
    {
        move.impulse=new Vector2(finisher?tuning.staffFinisherStep:tuning.staffComboStep,0);move.impulseNeedsInput=true;   // steps only when pushed forward
        move.hitStop=finisher?tuning.staffFinisherHitStop:tuning.staffComboHitStop;move.hitFreeze=finisher?tuning.staffFinisherFreeze:tuning.staffComboFreeze;
        move.cameraShake=finisher?tuning.staffFinisherShake:tuning.staffComboShake;move.rumble=finisher?tuning.staffFinisherRumble:tuning.staffComboRumble;
        move.moveCancelsRecovery=true;
    }
    static void StaffExtras(AttackDefinition move,CombatMoveSlot slot,WeaponTuning tuning)
    {
        if(slot==CombatMoveSlot.Front)StaffComboFeel(move,false,tuning);
        if(slot==CombatMoveSlot.Dash){move.impulse=new Vector2(tuning.staffSprintPush,0);move.groundControl=tuning.staffSprintCarry;move.knockback*=1.4f;}
        if(slot==CombatMoveSlot.Lower)
        {
            move.plungeSpeed=tuning.staffPlungeSpeed;move.plungeBounce=tuning.staffPlungeBounce;
            var land=move.plungeLanding;
            if(land!=null)
            {
                Phases(land,tuning.staffPlungeLanding,0f);land.damage=tuning.staffPlungeLandingDamage;land.knockback=new Vector2(4f,3f);
                land.hitStop=.05f;land.cameraShake=.14f;land.rumble=.4f;
            }
        }
        if(slot==CombatMoveSlot.Charged)
        {
            move.damage*=tuning.staffChargedDamage;move.knockback*=2f;move.groundControl=0;
            move.hitStop=Mathf.Max(move.hitStop,.06f);move.cameraShake=Mathf.Max(move.cameraShake,.12f);move.rumble=Mathf.Max(move.rumble,.35f);
        }
    }
    public static void RefreshSwordCombo(WeaponDefinition weapon)
    {
        var first=weapon.moveSet.Find(CombatMoveSlot.Front);
        if(first.followUps.Length==0)return;
        var second=first.followUps[0];var third=second.followUps[0];var fourth=third.followUps[0];
        foreach(var move in new[]{second,third,fourth})
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(first.animation),move.animation);
        // Two compact cuts remain in front of the chest, never finishing overhead.
        var p=second.animation;
        p.anticipation=new AttackAnimationProfile.Pose{leftGrip=new Vector2(1.7f,-.75f),weaponAngle=-38,torsoAngle=-8,freeHand=new Vector2(.2f,-.6f)};
        p.contact=new AttackAnimationProfile.Pose{leftGrip=new Vector2(2.75f,.05f),weaponAngle=10,torsoAngle=5,shoulderReach=new Vector2(.45f,0),freeHand=new Vector2(.1f,-.6f)};
        p.followThrough=new AttackAnimationProfile.Pose{leftGrip=new Vector2(2.05f,.65f),weaponAngle=48,torsoAngle=7,freeHand=new Vector2(.1f,-.6f)};
        p.contactFraction=.5f;p.recoveryHold=.05f;
        p=third.animation;p.anticipation=second.animation.followThrough;
        p.contact=new AttackAnimationProfile.Pose{leftGrip=new Vector2(2.85f,.1f),weaponAngle=8,torsoAngle=-8,shoulderReach=new Vector2(.5f,0),freeHand=new Vector2(.2f,-.65f)};
        p.followThrough=new AttackAnimationProfile.Pose{leftGrip=new Vector2(1.9f,-.65f),weaponAngle=-38,torsoAngle=-9,freeHand=new Vector2(.2f,-.65f)};
        p.contactFraction=.5f;p.recoveryHold=.05f;
        p=fourth.animation;p.depthSweep=true;
        p.anticipation=new AttackAnimationProfile.Pose{leftGrip=new Vector2(.8f,.1f),weaponAngle=20,weaponYaw=72,weaponRoll=72,torsoAngle=12,compression=.20f,freeHand=new Vector2(.4f,-.5f)};
        p.contact=new AttackAnimationProfile.Pose{leftGrip=new Vector2(2.9f,.05f),weaponAngle=13,weaponYaw=0,weaponRoll=76,torsoAngle=-10,shoulderReach=new Vector2(.55f,0),compression=.10f,freeHand=new Vector2(-.2f,-.6f)};
        p.followThrough=new AttackAnimationProfile.Pose{leftGrip=new Vector2(1.2f,-.1f),weaponAngle=5,weaponYaw=-65,weaponRoll=70,torsoAngle=-13,compression=.15f,freeHand=new Vector2(.1f,-.6f)};
        p.contactFraction=.6f;p.recoveryHold=.3f;
        foreach(var move in new[]{first,second,third,fourth})move.comboWindowStart=.25f;
    }
    // One clean PNG per weapon (Resources/Armory/Weapons/<name>.png) with its grip in Grips.txt
    // ("name,gripX,gripY", pivot fractions, y from the bottom). Keeps the weapon's world length
    // (grip -> tip). Returns false (falls back to the old sheet) when the files are missing.
    static bool ApplyCleanArt(WeaponDefinition weapon,string name,float length,List<Object> owned)
    {
        var texture=Resources.Load<Texture2D>("Armory/Weapons/"+name);var grips=Resources.Load<TextAsset>("Armory/Weapons/Grips");
        if(texture==null||grips==null)return false;
        foreach(string line in grips.text.Split('\n'))
        {
            string[] f=line.Trim().Split(',');if(f.Length<3||f[0]!=name)continue;
            float gx=float.Parse(f[1],System.Globalization.CultureInfo.InvariantCulture),gy=float.Parse(f[2],System.Globalization.CultureInfo.InvariantCulture);
            var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(gx,gy),100,0,SpriteMeshType.FullRect);sprite.name=name;owned.Add(sprite);
            weapon.weaponArtwork=sprite;weapon.artworkTip=new Vector2(texture.width*(1-gx)/100,0);weapon.artworkScale=length/weapon.artworkTip.x;
            return true;
        }
        return false;
    }
    static void ApplyArt(WeaponDefinition weapon,int row,float length,List<Object> owned)
    {
        var texture=Resources.Load<Texture2D>("Armory/Weapons");if(texture==null)return;
        // Updated from the final sheet's measured sprite bounds during installation.
        var rows=Resources.Load<TextAsset>("Armory/WeaponRects");if(rows==null)return;
        string[] values=rows.text.Split('\n')[row].Trim().Split(',');
        float x=float.Parse(values[0],System.Globalization.CultureInfo.InvariantCulture),top=float.Parse(values[1],System.Globalization.CultureInfo.InvariantCulture),width=float.Parse(values[2],System.Globalization.CultureInfo.InvariantCulture),height=float.Parse(values[3],System.Globalization.CultureInfo.InvariantCulture),gripY=float.Parse(values[4],System.Globalization.CultureInfo.InvariantCulture);
        var sprite=Sprite.Create(texture,new Rect(x,texture.height-top-height,width,height),new Vector2(.12f,gripY),100,0,SpriteMeshType.FullRect);owned.Add(sprite);
        // Per-object convex mesh excludes neighbouring tips where rectangular
        // bounds overlap, without editing the generated texture's alpha.
        var hulls=Resources.Load<TextAsset>("Armory/WeaponHulls");
        if(hulls!=null)
        {
            var pairs=hulls.text.Split('\n')[row].Trim().Split(';');var vertices=new Vector2[pairs.Length];
            for(int i=0;i<pairs.Length;i++){var pair=pairs[i].Split(',');vertices[i]=new Vector2(float.Parse(pair[0],System.Globalization.CultureInfo.InvariantCulture)*100+width*.12f,float.Parse(pair[1],System.Globalization.CultureInfo.InvariantCulture)*100+height*gripY);vertices[i]=new Vector2(Mathf.Clamp(vertices[i].x,0,width),Mathf.Clamp(vertices[i].y,0,height));}
            var indices=new ushort[(pairs.Length-2)*3];for(int i=0;i<pairs.Length-2;i++){indices[i*3]=0;indices[i*3+1]=(ushort)(i+1);indices[i*3+2]=(ushort)(i+2);}sprite.OverrideGeometry(vertices,indices);
        }
        weapon.weaponArtwork=sprite;weapon.artworkTip=new Vector2(width*.88f/100,0);weapon.artworkScale=length/weapon.artworkTip.x;
    }
}



