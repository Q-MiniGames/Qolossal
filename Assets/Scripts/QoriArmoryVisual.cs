using UnityEngine;

[DefaultExecutionOrder(15)]
public sealed class QoriArmoryVisual:MonoBehaviour
{
    QoriArmory armory;PlayerCombat combat;QoriBodyRig rig;QoriAnimator qoriRig;LineRenderer whip,sling,aimLine,reticle;SpriteRenderer pouch,seed;QoriVineLash vine;Material material;
    public Vector2 SlingReleasePosition {get;private set;}
    public Vector3[] WhipPoints {get;private set;}
    readonly float[] whipAngles=new float[18],whipAngularVelocity=new float[18];
    bool whipInitialized;float whipClock;
    public void Initialize(QoriArmory inventory,PlayerCombat owner)
    {
        armory=inventory;combat=owner;material=new Material(Shader.Find("Sprites/Default"));
        whip=Make("Living thorn lash",new Color(.44f,.55f,.19f),.055f,19);
        sling=Make("Sling winding cord",new Color(.68f,.73f,.40f),.025f,17);
        pouch=QoriArmoryArt.Create("SlingPouch",transform,.24f,13);
        seed=QoriArmoryArt.Create("ResinSeed",transform,.13f,14);
        vine=new QoriVineLash(transform);
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
        whip.enabled=!shooting&&combat.EquippedWeapon!=null&&combat.EquippedWeapon.flexibleWhip;
        sling.enabled=shooting;pouch.enabled=shooting;seed.enabled=false;aimLine.enabled=reticle.enabled=false;
        if(whip.enabled)
        {
            if(WhipPoints==null)WhipPoints=new Vector3[19];
            Vector3 axis=(tip.position-mount.position).normalized;
            float dt=combat.IsHitStopped?0:Time.deltaTime;if(dt>0)whipClock+=dt;
            float stretch=0,polarity=1;
            if(combat.IsAttackPoseActive)
            {
                float p=combat.PhaseProgress;
                polarity=combat.CurrentAttack.whipSweepSign<0?-1:1;
                stretch=combat.Phase==AttackPhase.Startup?.15f*Mathf.SmoothStep(0,1,p):
                    combat.Phase==AttackPhase.Active?.15f+.85f*Mathf.Sin(p*Mathf.PI*.8f):
                    (.15f+.85f*Mathf.Sin(Mathf.PI*.8f))*(1-Mathf.SmoothStep(0,1,p));
            }
            float facing=visualFacing,heading=Mathf.Atan2(axis.y,axis.x*facing)*Mathf.Rad2Deg;
            WhipPoints[0]=tip.position;
            for(int i=0;i<18;i++)
            {
                float t=(i+.5f)/18;
                float coil=-80+260*t;
                float wave=Mathf.Sin(t*6.8f-whipClock*19)*65*(1-stretch)*t*polarity;
                float target=Mathf.LerpAngle(coil,heading+wave,stretch);
                if(!whipInitialized)whipAngles[i]=target;
                else if(dt>0)whipAngles[i]=Mathf.SmoothDampAngle(whipAngles[i],target,ref whipAngularVelocity[i],.025f+t*.045f,Mathf.Infinity,dt);
                float radians=whipAngles[i]*Mathf.Deg2Rad;
                WhipPoints[i+1]=WhipPoints[i]+new Vector3(Mathf.Cos(radians)*facing,Mathf.Sin(radians),0)*(2.6f/18);
            }
            whipInitialized=true;
        }
        else {WhipPoints=null;whipInitialized=false;}
        vine.Show(WhipPoints);whip.enabled=false;
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
    void OnDisable(){if(whip!=null)whip.enabled=false;if(sling!=null)sling.enabled=false;if(pouch!=null)pouch.enabled=false;if(seed!=null)seed.enabled=false;if(aimLine!=null)aimLine.enabled=false;if(reticle!=null)reticle.enabled=false;if(vine!=null)vine.Show(null);WhipPoints=null;}
    void OnDestroy(){if(vine!=null)vine.Dispose();if(material!=null)Destroy(material);}
}
