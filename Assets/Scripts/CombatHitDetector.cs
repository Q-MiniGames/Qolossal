using System.Collections.Generic;
using UnityEngine;

// Queries only the active window; sweeps cover gaps between rendered blade samples.
public sealed class CombatHitDetector
{
    readonly List<Collider2D> hits=new List<Collider2D>();
    readonly Dictionary<Component,float> lastHit=new Dictionary<Component,float>();
    Vector2 previousA,previousB;bool sampled,hitAny;
    Vector3[] previousLine;
    public Vector2 LastA {get;private set;}public Vector2 LastB {get;private set;}
    public void Reset(){lastHit.Clear();sampled=hitAny=false;previousLine=null;}
    public void SamplePolyline(PlayerCombat owner,Vector3[] points)
    {
        if(points==null||points.Length<2)return;
        for(int i=0;i<points.Length-1;i++)
        {
            ClearSweep();
            if(previousLine!=null)Sample(owner,previousLine[i],previousLine[i+1]);
            Sample(owner,points[i],points[i+1]);
        }
        if(previousLine==null)previousLine=new Vector3[points.Length];
        System.Array.Copy(points,previousLine,points.Length);
    }
    public void ClearSweep()=>sampled=false;
    public void Sample(PlayerCombat owner,Vector2 a,Vector2 b)
    {
        var attack=owner.CurrentAttack;if(attack==null||!attack.multipleTargets&&hitAny)return;
        int executionId=owner.ExecutionId;
        LastA=a;LastB=b;
        if(!sampled){previousA=a;previousB=b;sampled=true;}
        int steps=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(Vector2.Distance(previousA,a),Vector2.Distance(previousB,b))/.08f),1,24);
        for(int i=1;i<=steps;i++)
        {
            Vector2 start=Vector2.Lerp(previousA,a,(float)i/steps),end=Vector2.Lerp(previousB,b,(float)i/steps);
            var filter=new ContactFilter2D{useTriggers=true};filter.SetLayerMask(attack.targetLayers);hits.Clear();
            if(attack.hitShape==CombatHitShape.WeaponCapsule)
            {
                start=Vector2.Lerp(start,end,attack.bladeStart);
                float length=Vector2.Distance(start,end),angle=Vector2.SignedAngle(Vector2.right,end-start);
                Physics2D.OverlapCapsule((start+end)*.5f,new Vector2(length+attack.bladeRadius*2,attack.bladeRadius*2),CapsuleDirection2D.Horizontal,angle,filter,hits);
            }
            else
            {
                Vector2 offset=attack.hitboxOffset;offset.x*=owner.AttackDirection;
                Vector2 center=(Vector2)owner.transform.position+offset;
                if(attack.hitShape==CombatHitShape.Circle)Physics2D.OverlapCircle(center,Mathf.Max(.01f,attack.hitboxSize.x*.5f),filter,hits);
                else Physics2D.OverlapBox(center,attack.hitboxSize,0,filter,hits);
            }
            foreach(var collider in hits)
            {
                if(collider==null||collider.transform.IsChildOf(owner.transform))continue;
                Component receiver=collider.GetComponentInParent<ICombatDamageReceiver>() as Component;
                if(receiver==null)receiver=collider.GetComponentInParent<IReedbladeTarget>() as Component;
                if(receiver==null||receiver is Behaviour behaviour&&!behaviour.isActiveAndEnabled)continue;
                if(lastHit.TryGetValue(receiver,out float at)&&(!attack.repeatTarget||owner.AttackElapsed-at<attack.repeatInterval))continue;
                Vector2 point=collider.ClosestPoint(end),origin=owner.transform.position;
                var obstacle=Physics2D.Linecast(origin,point,LayerMask.GetMask("Ground")).collider;
                if(obstacle!=null&&obstacle!=collider&&!obstacle.transform.IsChildOf(receiver.transform))continue;
                lastHit[receiver]=owner.AttackElapsed;
                Vector2 axis=attack.direction==AttackAim.Up?Vector2.up:attack.direction==AttackAim.Down?Vector2.down:Vector2.right*owner.AttackDirection;
                var damage=new CombatDamage{Source=owner.gameObject,Attack=attack,Weapon=owner.EquippedWeapon,Point=point,Normal=(origin-point).normalized,Direction=axis,
                    Damage=attack.damage*(owner.EquippedWeapon!=null?owner.EquippedWeapon.damageMultiplier:1),Knockback=new Vector2(attack.knockback.x*axis.x,attack.knockback.y+attack.knockback.x*axis.y)};
                CombatDamageResponse response;
                if(receiver is ICombatDamageReceiver modern)response=modern.ReceiveCombatHit(damage);
                else{((IReedbladeTarget)receiver).TakeHit();response=CombatDamageResponse.Applied(damage.Damage);}
                owner.PublishHit(new AttackHitResult{Target=receiver,Collider=collider,Hit=damage,Response=response,ExecutionId=owner.ExecutionId});
                if(owner.ExecutionId!=executionId||owner.CurrentAttack!=attack)return;
                if(response.Disposition!=CombatHitDisposition.Ignored)hitAny=true;
                if(!attack.multipleTargets&&hitAny)break;
            }
            if(!attack.multipleTargets&&hitAny)break;
        }
        previousA=a;previousB=b;
    }
}
