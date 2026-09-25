using System.Collections.Generic;
using UnityEngine;

// Runtime copies keep the approved Reedblade assets immutable.
public static class QoriArmoryFactory
{
    static T Copy<T>(T value,List<Object> owned) where T:Object{var copy=Object.Instantiate(value);owned.Add(copy);return copy;}
    public static WeaponDefinition[] Build(WeaponDefinition original,List<Object> owned,out WeaponDefinition slingWeapon,out AttackDefinition sling)
    {
        var result=new WeaponDefinition[4];string[] names={"Leaf Sword","Vine Whip","Seedpod Mace","Thorn Spear"};
        float[] damage={1.2f,.8f,2f,1.1f},startup={.16f,.24f,.34f,.17f},active={.18f,.18f,.20f,.12f},recovery={.23f,.29f,.42f,.23f};
        int[] artRows={0,4,1,2};string[] cleanArt={"LeafSword","WhipHandle","SeedpodMace","ThornSpear"};float[] lengths={7.4f,1.6f,6.5f,7.5f};
        for(int i=0;i<4;i++)
        {
            var w=Copy(original,owned);result[i]=w;w.name=w.displayName=names[i];w.weaponId="forest-"+i;w.damageMultiplier=1;w.flexibleWhip=i==1;
            ApplyArt(w,artRows[i],lengths[i],owned);
            bool clean=ApplyCleanArt(w,cleanArt[i],i==1?1.35f:lengths[i],owned);
            if(i==1&&!clean)
            {
                var lash=QoriArmoryArt.Get("ThornLash");
                if(lash!=null)
                {
                    var rect=lash.rect;rect.width*=.14f;
                    var grip=Sprite.Create(lash.texture,rect,new Vector2(.1f,.5f),100,0,SpriteMeshType.FullRect);owned.Add(grip);
                    w.weaponArtwork=grip;w.artworkTip=new Vector2(rect.width*.9f/100,0);w.artworkScale=1.35f/w.artworkTip.x;
                }
            }
            w.trailColor=i==2?new Color(.8f,.65f,.32f):new Color(.8f,.94f,.5f);
            w.moveSet=ScriptableObject.CreateInstance<CombatMoveSet>();owned.Add(w.moveSet);w.moveSet.moves=new CombatMoveSet.Entry[3];
            for(int a=0;a<3;a++)
            {
                var slot=a==0?CombatMoveSlot.Front:a==1?CombatMoveSlot.Upper:CombatMoveSlot.Lower;
                var move=Copy(original.moveSet.Find(slot),owned);move.name=move.attackId=names[i]+" "+(AttackAim)a;
                move.animation=Copy(move.animation,owned);move.followUps=new AttackDefinition[0];move.damage=damage[i];
                move.startup=startup[i];move.active=active[i];move.recovery=recovery[i];move.cooldown=move.TotalDuration+.025f;
                move.knockback=new Vector2(i==2?5:i==1?1.3f:2, i==2?2:1);
                move.hitStop=i==2?.06f:.025f;move.bladeRadius=i==2?.18f:i==1?.065f:.10f;move.bladeStart=i==3?.6f:i==2?.7f:.15f;
                var p=move.animation;p.oneHanded=i==0||i==1||a!=0;p.straightTrail=i==3||a!=0;p.sweepThroughActive=!p.straightTrail;
                if(a==0)
                {
                    var wind=p.anticipation;var contact=p.contact;var follow=p.followThrough;
                    if(i==1){wind.weaponAngle=145;wind.leftGrip=new Vector2(-.5f,.1f);contact.weaponAngle=5;contact.leftGrip=new Vector2(2.4f,-.1f);follow.weaponAngle=-35;follow.leftGrip=new Vector2(1.7f,-.5f);}
                    if(i==2){wind.weaponAngle=105;wind.leftGrip=new Vector2(.2f,.8f);wind.torsoAngle=12;contact.weaponAngle=-35;contact.leftGrip=new Vector2(2,-.6f);contact.compression=.25f;follow.weaponAngle=-75;follow.leftGrip=new Vector2(1.8f,-1.1f);follow.compression=.2f;}
                    if(i==3){wind.weaponAngle=contact.weaponAngle=follow.weaponAngle=0;wind.leftGrip=new Vector2(-.6f,-.15f);contact.leftGrip=new Vector2(2.65f,-.05f);follow.leftGrip=new Vector2(1.9f,-.05f);}
                    wind.freeHand=contact.freeHand=follow.freeHand=new Vector2(.5f,-.5f);
                    p.anticipation=wind;p.contact=contact;p.followThrough=follow;
                }
                w.moveSet.moves[a]=new CombatMoveSet.Entry{slot=slot,attack=move};
            }
        }
        BuildWhipCombo(result[1],owned);
        var opener=result[0].moveSet.Find(CombatMoveSlot.Front);
        var reverse=Copy(opener,owned);reverse.animation=Copy(opener.animation,owned);
        var quick=Copy(opener,owned);quick.animation=Copy(opener.animation,owned);
        var finish=Copy(opener,owned);finish.animation=Copy(opener.animation,owned);
        reverse.name=reverse.attackId="Leaf Sword 2 - Quick front cut";
        finish.name=finish.attackId="Leaf Sword 4 - Depth sweep";
        quick.name=quick.attackId="Leaf Sword 3 - Quick return cut";
        opener.followUps=new[]{reverse};reverse.followUps=new[]{quick};quick.followUps=new[]{finish};finish.followUps=new AttackDefinition[0];
        RefreshSwordCombo(result[0]);
        slingWeapon=Copy(result[1],owned);slingWeapon.name=slingWeapon.displayName="Resin Sling";slingWeapon.weaponId="resin-sling";slingWeapon.flexibleWhip=false;
        sling=Copy(result[0].moveSet.Find(CombatMoveSlot.Front),owned);sling.name=sling.attackId="Resin seed";sling.slingProjectile=true;sling.damage=.35f;sling.startup=.28f;sling.active=.18f;sling.recovery=.24f;sling.cooldown=.72f;sling.knockback=new Vector2(.5f,.15f);
        sling.followUps=new AttackDefinition[0];
        sling.impulse=Vector2.zero;sling.recoil=Vector2.zero;sling.lockFacing=true;
        sling.animation=Copy(sling.animation,owned);sling.animation.oneHanded=true;sling.animation.straightTrail=true;sling.animation.sweepThroughActive=false;
        // A compact cocked wrist, forward cast, then relaxed follow-through.
        sling.animation.contactFraction=.55f;sling.animation.secondaryImpulse=.1f;
        sling.animation.anticipation=new AttackAnimationProfile.Pose{weaponAngle=75,leftGrip=new Vector2(-.3f,.25f),freeHand=new Vector2(.7f,-.55f),torsoAngle=5,shoulderReach=new Vector2(-.08f,0)};
        sling.animation.contact=new AttackAnimationProfile.Pose{weaponAngle=5,leftGrip=new Vector2(2.35f,.15f),freeHand=new Vector2(.2f,-.7f),torsoAngle=-5,shoulderReach=new Vector2(.22f,.04f)};
        sling.animation.followThrough=new AttackAnimationProfile.Pose{weaponAngle=-25,leftGrip=new Vector2(1.5f,-.4f),freeHand=new Vector2(.35f,-.6f),torsoAngle=-2,shoulderReach=new Vector2(.08f,0)};
        slingWeapon.artworkScale*=.55f;
        return result;
    }
    static void BuildWhipCombo(WeaponDefinition weapon,List<Object> owned)
    {
        var first=weapon.moveSet.Find(CombatMoveSlot.Front);
        var back=Copy(first,owned);back.animation=Copy(first.animation,owned);
        var finish=Copy(first,owned);finish.animation=Copy(first.animation,owned);
        first.name=first.attackId="Vine Whip 1 - Forward crack";
        back.name=back.attackId="Vine Whip 2 - Returning lash";
        finish.name=finish.attackId="Vine Whip 3 - Broad sweep";
        first.followUps=new[]{back};back.followUps=new[]{finish};finish.followUps=new AttackDefinition[0];
        var up=weapon.moveSet.Find(CombatMoveSlot.Upper);var down=weapon.moveSet.Find(CombatMoveSlot.Lower);
        var moves=new[]{first,back,finish,up,down};
        for(int i=0;i<moves.Length;i++)
        {
            var move=moves[i];var p=move.animation;
            p.oneHanded=true;p.straightTrail=false;p.sweepThroughActive=true;p.depthSweep=false;
            p.contactFraction=.6f;p.recoveryHold=.12f;
            p.swingTiming=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(.55f,.55f,1.6f,1.6f),new Keyframe(1,1,0,0));
            move.startup=i==1?.12f:i==2?.28f:.23f;move.active=i==2?.34f:.28f;move.recovery=i==1?.18f:.30f;
            move.cooldown=move.TotalDuration+.025f;move.comboWindowStart=.25f;move.whipSweepSign=i==1?-1:1;
            float wind=i==3?20:i==4?70:i==1?-40:135;
            float contact=i==3?100:i==4?-85:i==1?18:0;
            float end=i==3?155:i==4?-140:i==1?58:i==2?-65:-30;
            Vector2 grip=i==3?new Vector2(1.2f,3.5f):i==4?new Vector2(.8f,-2.6f):new Vector2(2.7f,.1f);
            p.anticipation=new AttackAnimationProfile.Pose{leftGrip=i==1?new Vector2(1.7f,-.7f):i==4?new Vector2(1.8f,.8f):new Vector2(-.4f,.65f),weaponAngle=wind,torsoAngle=i==1?-8:12,compression=.15f,freeHand=new Vector2(.4f,-.6f)};
            p.contact=new AttackAnimationProfile.Pose{leftGrip=grip,weaponAngle=contact,torsoAngle=i==3?-4:i==4?12:-12,shoulderReach=i==3?new Vector2(.1f,.5f):i==4?new Vector2(.1f,-.55f):new Vector2(.45f,0),freeHand=new Vector2(-.15f,-.6f),kneeBend=i==4?.3f:0};
            p.followThrough=new AttackAnimationProfile.Pose{leftGrip=grip+new Vector2(-.55f,i==3?-.3f:i==4?.4f:-.35f),weaponAngle=end,torsoAngle=i==1?7:-7,compression=.10f,freeHand=new Vector2(.2f,-.6f),kneeBend=i==4?.2f:0};
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
        second.startup=.09f;second.active=.13f;second.recovery=.12f;second.cooldown=.365f;
        third.startup=.07f;third.active=.13f;third.recovery=.14f;third.cooldown=.365f;
        fourth.startup=.22f;fourth.active=.30f;fourth.recovery=.38f;fourth.cooldown=.925f;
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



