using UnityEngine;

[DefaultExecutionOrder(15)]
public sealed class QoriArmoryVisual:MonoBehaviour
{
    QoriArmory armory;PlayerCombat combat;QoriBodyRig rig;QoriAnimator qoriRig;LineRenderer sling,aimLine,reticle;SpriteRenderer pouch,seed;Material material;
    public Vector2 SlingReleasePosition {get;private set;}
    public void Initialize(QoriArmory inventory,PlayerCombat owner)
    {
        armory=inventory;combat=owner;material=new Material(Shader.Find("Sprites/Default"));
        sling=Make("Sling winding cord",new Color(.68f,.73f,.40f),.025f,17);
        pouch=QoriArmoryArt.Create("SlingPouch",transform,.24f,13);
        seed=QoriArmoryArt.Create("ResinSeed",transform,.13f,14);
        aimLine=Make("Sling aim guide",new Color(.9f,.85f,.5f,.4f),.012f,2);
        reticle=Make("Sling aim target",new Color(1,.86f,.44f,.85f),.015f,25);
    }
    LineRenderer Make(string label,Color color,float width,int count)
    {
        var obj=new GameObject(label);obj.transform.SetParent(transform,false);var line=obj.AddComponent<LineRenderer>();
        line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=count;line.startColor=line.endColor=color;line.startWidth=width;line.endWidth=width*.45f;line.numCapVertices=3;line.sortingOrder=12;line.enabled=false;return line;
    }
    void LateUpdate()
    {
        if(!QoriPoseLookup.TryGetWeapon(this,ref qoriRig,ref rig,out Transform mount,out Transform tip,out float visualFacing)){OnDisable();return;}
        bool shooting=combat.CurrentAttack!=null&&combat.CurrentAttack.slingProjectile;
        sling.enabled=shooting;pouch.enabled=shooting;seed.enabled=false;aimLine.enabled=reticle.enabled=false;
        if(shooting)
        {
            Vector3 aim=armory.SlingDirection,normal=new Vector3(-aim.y,aim.x,0);
            float contact=combat.CurrentAttack.animation.contactFraction;
            bool loaded=combat.Phase==AttackPhase.Startup||(combat.Phase==AttackPhase.Active&&combat.PhaseProgress<contact);
            // The orbit ends at the release point; the projectile uses this same point.
            float turn=armory.IsAiming?-Mathf.PI*.5f:combat.Phase==AttackPhase.Startup?Mathf.Lerp(-Mathf.PI*2,-Mathf.PI*.5f,combat.PhaseProgress):Mathf.Lerp(-Mathf.PI*.5f,0,Mathf.Clamp01(combat.PhaseProgress/contact));
            Vector3 release=mount.position+aim*.32f;
            SlingReleasePosition=release;
            Vector3 end=loaded?mount.position+(aim*Mathf.Cos(turn)+normal*Mathf.Sin(turn))*.32f:Vector3.Lerp(release,mount.position+Vector3.down*.32f,combat.Phase==AttackPhase.Recovery?Mathf.SmoothStep(0,1,combat.PhaseProgress):0);
            for(int i=0;i<17;i++)
            {
                float t=i<=8?i/8f:(16-i)/8f;
                Vector3 spread=normal*t*.085f*(i<=8?1:-1);
                sling.SetPosition(i,Vector3.Lerp(mount.position,end,t)+spread);
            }
                        float angle=Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg;
            pouch.transform.position=end;pouch.transform.rotation=Quaternion.Euler(0,0,angle+90);
            seed.enabled=loaded;seed.transform.position=end;seed.transform.rotation=Quaternion.Euler(0,0,angle);
            if(armory.IsAiming)
            {
                aimLine.enabled=reticle.enabled=true;
                float distance=Mathf.Min(6,combat.CurrentAttack.projectileSpeed*combat.CurrentAttack.projectileLifetime);
                var hit=Physics2D.Raycast(end,aim,distance,LayerMask.GetMask("Ground"));
                Vector3 target=hit.collider!=null?(Vector3)hit.point:end+aim*distance;
                aimLine.SetPosition(0,end);aimLine.SetPosition(1,target);
                for(int i=0;i<25;i++){float a=i/24f*Mathf.PI*2;reticle.SetPosition(i,target+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.075f);}
            }

        }
    }
    void OnDisable(){if(sling!=null)sling.enabled=false;if(pouch!=null)pouch.enabled=false;if(seed!=null)seed.enabled=false;if(aimLine!=null)aimLine.enabled=false;if(reticle!=null)reticle.enabled=false;}
    void OnDestroy(){if(material!=null)Destroy(material);}
}
