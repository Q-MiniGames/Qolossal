using UnityEngine;

// A fading ribbon sampled from the actual blade path, never a preset decorative arc.
[DefaultExecutionOrder(30)]
public sealed class QoriSlashTrail : MonoBehaviour
{
    private const int Segments = 24;
    private PlayerCombat combat;
    readonly Vector3[] tips=new Vector3[Segments+1],grips=new Vector3[Segments+1];
    readonly float[] stamps=new float[Segments+1];
    int count,execution=-1;float trailClock;
    const float Lifetime=.12f;
    private Mesh mesh;
    private Material material;
    private MeshRenderer display;
    private QoriBodyRig rig;
    private QoriAnimator qoriRig;
    private readonly Vector3[] vertices = new Vector3[(Segments + 1) * 2];
    private readonly Color[] colors = new Color[(Segments + 1) * 2];

    public void Initialize(PlayerCombat owner)
    {
        combat = owner;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        GameObject artwork = new GameObject("Reedblade Slash Arc");
        artwork.transform.SetParent(transform, false);
        display = artwork.AddComponent<MeshRenderer>();
        material = new Material(shader);
        display.sharedMaterial = material;
        display.enabled = false;
        mesh = new Mesh { name = "Reedblade slash arc" };
        mesh.MarkDynamic();
        int[] triangles = new int[Segments * 6];
        for (int i = 0; i < Segments; i++)
        {
            int v = i * 2, t = i * 6;
            triangles[t] = v; triangles[t+1] = v+1; triangles[t+2] = v+2;
            triangles[t+3] = v+1; triangles[t+4] = v+3; triangles[t+5] = v+2;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = new Vector2[vertices.Length];
        artwork.AddComponent<MeshFilter>().sharedMesh = mesh;
    }

    private void Start()
    {
        QoriVisual qori = GetComponentInChildren<QoriVisual>();
        rig = GetComponentInChildren<QoriBodyRig>();
        SpriteRenderer source = qori != null ? qori.GetComponent<SpriteRenderer>() : null;
        if (display == null) return;
        display.sortingLayerID = source != null ? source.sortingLayerID : 0;
        display.sortingOrder = source != null ? source.sortingOrder + 4 : 20;
    }

    private void LateUpdate()
    {
        if (display == null) return;
        if(Time.deltaTime<=0)return;
        bool posed=QoriPoseLookup.TryGetWeapon(this,ref qoriRig,ref rig,out Transform hand,out Transform tip,out _);
        if(combat==null||!combat.IsAttackPoseActive||combat.CurrentAttack.slingProjectile||combat.EquippedWeapon.flexibleWhip||!posed)
        {count=0;display.enabled=false;return;}
        if(execution!=combat.ExecutionId){execution=combat.ExecutionId;count=0;trailClock=0;}
        if(!combat.IsHitStopped)trailClock+=Time.deltaTime;
        if(hand==null||tip==null){display.enabled=false;return;}
        bool active=combat.Phase==AttackPhase.Active;
        if(combat.Phase==AttackPhase.Startup)count=0;
        while(count>0&&trailClock-stamps[0]>Lifetime)RemoveOldest();
        // Retain only the last preparation sample as the start of the first sweep.
        if(active||combat.Phase==AttackPhase.Startup)
        {
            if(count==Segments+1)RemoveOldest();
            tips[count]=tip.position;grips[count]=hand.position;stamps[count]=trailClock;count++;
        }
        display.enabled=count>=2&&combat.Phase!=AttackPhase.Startup;
        if(!display.enabled)return;
        bool straight=combat.CurrentAttack.animation.straightTrail;
        Color color=combat.EquippedWeapon!=null?combat.EquippedWeapon.trailColor:new Color(.9f,.96f,.66f,1);
        for(int i=0;i<=Segments;i++)
        {
            float u=i/(float)Segments,index=u*(count-1);int lo=Mathf.FloorToInt(index),hi=Mathf.Min(count-1,lo+1);float t=index-lo;
            Vector3 grip=Vector3.Lerp(grips[lo],grips[hi],t);
            Vector3 from=tips[lo]-grips[lo],to=tips[hi]-grips[hi];
            float angle=Mathf.LerpAngle(Mathf.Atan2(from.y,from.x)*Mathf.Rad2Deg,Mathf.Atan2(to.y,to.x)*Mathf.Rad2Deg,t)*Mathf.Deg2Rad;
            Vector3 end=grip+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*Mathf.Lerp(((Vector2)from).magnitude,((Vector2)to).magnitude,t);
            float age=trailClock-Mathf.Lerp(stamps[lo],stamps[hi],t);
            float fade=Mathf.Clamp01(1-age/Lifetime)*Mathf.SmoothStep(0,1,u)*.8f;
            Vector3 inner=Vector3.Lerp(end,grip,.32f);
            // A depth-facing sweep projects almost to a line. Give its fading
            // ribbon a shallow crescent so the horizontal cutting plane reads.
            if(combat.CurrentAttack.animation.depthSweep)
                inner=end+Vector3.down*(.16f*Mathf.Sin(u*Mathf.PI));
            if(straight)
            {
                Vector3 axis=(end-grip).normalized,normal=new Vector3(-axis.y,axis.x,0);
                inner=end-normal*(Mathf.Sin(u*Mathf.PI)*.05f);
            }
            vertices[i*2]=transform.InverseTransformPoint(inner);
            vertices[i*2+1]=transform.InverseTransformPoint(end);
            colors[i*2]=new Color(color.r,color.g,color.b,0);
            colors[i*2+1]=new Color(color.r,color.g,color.b,color.a*fade);
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.RecalculateBounds();
    }

    void RemoveOldest()
    {for(int i=1;i<count;i++){tips[i-1]=tips[i];grips[i-1]=grips[i];stamps[i-1]=stamps[i];}count--;}
    private void OnDisable() { count=0;if (display != null) display.enabled = false; }
    private void OnDestroy()
    {
        if (display != null) Destroy(display.gameObject);
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }
}
