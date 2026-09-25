using System;
using UnityEngine;

public sealed class QoriResinShot:MonoBehaviour
{
    public const float Radius=.07f;
    PlayerCombat owner;AttackDefinition attack;WeaponDefinition weapon;Vector2 velocity;float remaining;int reset;
    Material material;PlayerMovement movement;
    public static QoriResinShot Launch(PlayerCombat owner,Vector2 origin,AttackDefinition attack,WeaponDefinition weapon,Vector2 direction)
    {
        var obstruction=Physics2D.Linecast(owner.transform.position,origin,LayerMask.GetMask("Ground"));
        if(obstruction.collider!=null)return null;
        var obj=new GameObject("Resin seed");obj.transform.position=origin;
        var shot=obj.AddComponent<QoriResinShot>();shot.owner=owner;shot.attack=attack;shot.weapon=weapon;
        shot.velocity=direction.normalized*attack.projectileSpeed;shot.remaining=attack.projectileLifetime;
        shot.movement=owner.GetComponent<PlayerMovement>();shot.reset=shot.movement.ResetVersion;
        QoriArmoryArt.Create("ResinSeed",obj.transform,.19f,14);
        obj.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        var trail=obj.AddComponent<TrailRenderer>();trail.time=.065f;trail.startWidth=.035f;trail.endWidth=0;trail.minVertexDistance=.04f;trail.sortingOrder=13;
        shot.material=new Material(Shader.Find("Sprites/Default"));trail.sharedMaterial=shot.material;
        trail.startColor=new Color(1,.73f,.27f,.45f);trail.endColor=new Color(1,.73f,.27f,0);
        return shot;
    }
    void Update()
    {
        if(owner==null||movement==null||!movement.isActiveAndEnabled||movement.ResetVersion!=reset){Destroy(gameObject);return;}
        if(GamePauseMenu.BlocksGameplayInput||Time.deltaTime<=0)return;
        remaining-=Time.deltaTime;if(remaining<=0){Destroy(gameObject);return;}
        Vector2 origin=transform.position,delta=velocity*Time.deltaTime;
        var hits=Physics2D.CircleCastAll(origin,Radius,delta.normalized,delta.magnitude);
        Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in hits)
        {
            var collider=hit.collider;if(collider==null||collider.transform.IsChildOf(owner.transform))continue;
            var receiver=collider.GetComponentInParent<ICombatDamageReceiver>();
            if(receiver!=null)
            {
                var response=receiver.ReceiveCombatHit(new CombatDamage{Source=owner.gameObject,Attack=attack,Weapon=weapon,Point=hit.point,Normal=hit.normal,Direction=velocity.normalized,Damage=attack.damage,Knockback=velocity.normalized*attack.knockback.x+Vector2.up*attack.knockback.y});
                if(response.Disposition==CombatHitDisposition.Ignored)continue;
                Destroy(gameObject);return;
            }
            if(!collider.isTrigger){Destroy(gameObject);return;}
        }
        transform.position+=(Vector3)delta;
    }
    void OnDestroy(){if(material!=null)Destroy(material);}
}
