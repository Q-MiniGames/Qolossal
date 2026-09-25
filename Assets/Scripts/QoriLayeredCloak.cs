using UnityEngine;

// Shared walking, airborne and rope cloak. Panel geometry is separate from the body and collar.
public sealed class QoriLayeredCloak : MonoBehaviour
{
    private sealed class Panel
    {
        public Mesh mesh;
        public Material material;
        public MeshRenderer renderer;
        public Vector3[] vertices;
        public Vector2[] shape;
        public Color[] colors;
        public float angle, angularVelocity, tip, tipVelocity;
    }
    private readonly Panel[] panels = new Panel[3];
    private readonly QoriRopeMotion[] bodyDrawings = new QoriRopeMotion[11];
    private QoriRopeMotion bodyDrawing;
    private SpriteRenderer mantle;
    public bool CanShow(int pose) => Ready && pose >= 0 && pose < bodyDrawings.Length && bodyDrawings[pose] != null && bodyDrawings[pose].Ready;
    private bool activeLastFrame;
    private float trailing, trailVelocity, vertical, verticalVelocity;
    private const int Columns = 10, Rows = 32;
    public bool Ready { get; private set; }
    public void Impulse(float impulse)
    {
        foreach(Panel panel in panels)
        {
            if(panel==null)continue;
            panel.angularVelocity=Mathf.Clamp(panel.angularVelocity+impulse,-160,160);
            panel.tipVelocity=Mathf.Clamp(panel.tipVelocity+impulse*.65f,-160,160);
        }
    }

    public void Initialize(SpriteRenderer source)
    {
        Sprite bodySprite = Resources.Load<Sprite>("QoriCloakPrototype/Qori_AirBody_v1");
        Sprite mantleSprite = Resources.Load<Sprite>("QoriCloakPrototype/Qori_ShoulderMantle_v1");
        Shader shader = Shader.Find("Sprites/Default");
        Sprite[] assets = new Sprite[3];
        for (int i = 0; i < 3; i++) assets[i] = Resources.Load<Sprite>("QoriCloakPrototype/Qori_CloakPanel_" + (i+1));
        if (mantleSprite == null || bodySprite == null || shader == null || bodySprite.packed) return;
        for (int i = 0; i < 3; i++) if (assets[i] == null || assets[i].packed) return;
        bodyDrawing = gameObject.AddComponent<QoriRopeMotion>();
        bodyDrawing.Initialize(bodySprite, source);
        if (!bodyDrawing.Ready) return;
        bodyDrawings[0] = bodyDrawing;
        for (int i = 1; i < bodyDrawings.Length; i++)
        {
            string name = i == 10 ? "Qori_AttackWindupBody_v1" : i == 9 ? "Qori_AttackBody_v1" : i == 8 ? "Qori_IdleBody_v1" : i == 7 ? "Qori_RopeBody_v1" : "Qori_WalkBody_" + i.ToString("00");
            Sprite sprite = Resources.Load<Sprite>("QoriCloakPrototype/" + name);
            if (sprite == null || sprite.packed) continue;
            bodyDrawings[i] = gameObject.AddComponent<QoriRopeMotion>();
            bodyDrawings[i].Initialize(sprite, source);
        }
        Vector2[] anchors = {new Vector2(365,40),new Vector2(195,42),new Vector2(138,40)};
        float[] scales = {.69f,.76f,.87f};
        for (int n = 0; n < 3; n++)
        {
            Panel p = panels[n] = new Panel();
            Sprite sprite = assets[n]; Rect rect = sprite.rect;
            int count = (Columns+1)*(Rows+1);
            p.vertices = new Vector3[count]; p.shape = new Vector2[count]; p.colors = new Color[count];
            Vector2[] uv = new Vector2[count]; int[] triangles = new int[Columns*Rows*6];
            for (int row = 0; row <= Rows; row++)
            for (int col = 0; col <= Columns; col++)
            {
                int index = row*(Columns+1)+col;
                float x = rect.width*col/Columns, y = rect.height*row/Rows;
                p.shape[index] = new Vector2((x-anchors[n].x)*scales[n]*1.10f/100f,(y-anchors[n].y)*scales[n]/100f);
                p.vertices[index] = new Vector3(p.shape[index].x,-p.shape[index].y);
                uv[index] = new Vector2((rect.x+x)/sprite.texture.width,(rect.y+rect.height-y)/sprite.texture.height);
                p.colors[index] = Color.white;
                if (row == Rows || col == Columns) continue;
                int k = (row*Columns+col)*6;
                triangles[k]=index;triangles[k+1]=index+1;triangles[k+2]=index+Columns+1;
                triangles[k+3]=index+1;triangles[k+4]=index+Columns+2;triangles[k+5]=index+Columns+1;
            }
            p.mesh = new Mesh { name = "Qori separate cloak panel " + (n+1) };
            p.mesh.vertices=p.vertices;p.mesh.uv=uv;p.mesh.triangles=triangles;p.mesh.colors=p.colors;p.mesh.MarkDynamic();
            GameObject display = new GameObject(p.mesh.name);display.transform.SetParent(transform,false);
            display.AddComponent<MeshFilter>().sharedMesh=p.mesh;
            p.renderer=display.AddComponent<MeshRenderer>();p.material=new Material(shader){mainTexture=sprite.texture};
            p.renderer.sharedMaterial=p.material;p.renderer.sortingLayerID=source.sortingLayerID;
            // Reserve -2/-1 for the two legs; cloak panels must never tie with
            // their transparent meshes and switch order as bounds move.
            p.renderer.sortingOrder=source.sortingOrder-6+n;p.renderer.enabled=false;
            p.angle=-40f+n*6f;p.tip=p.angle;
        }
        GameObject collar = new GameObject("Qori shared original shoulder mantle");
        collar.transform.SetParent(transform,false);
        mantle = collar.AddComponent<SpriteRenderer>();
        mantle.sprite = mantleSprite;
        mantle.sortingLayerID = source.sortingLayerID;
        mantle.sortingOrder = source.sortingOrder + 1;
        mantle.enabled = false;
        Ready=true;
    }

    private static void Spring(ref float value, ref float velocity, float target, float stiffness, float dt)
    {
        velocity+=((target-value)*stiffness-velocity*12f)*dt;
        value+=velocity*dt;
    }

    public bool Show(bool active, Vector2 velocity, bool flipped, Color tint, int pose = 0, float artworkScale = 1f,
        float stepWave = 0f, QoriBodyRig rig = null, QoriSwingAnimation swing = null)
    {
        if (!Ready) return false;
        if (!active || !CanShow(pose)) { Hide(); return false; }
        float sign=flipped?-1f:1f;
        float swingWeight=swing!=null?Mathf.Clamp01(swing.Weight):0f;
        float compensation = 1f / Mathf.Max(.01f, artworkScale);
        // Brooch positions are measured in each body sprite; the shared art stays the same size.
        Vector2 brooch = pose == 10 ? new Vector2(.24f,-.10f) :
            pose == 9 ? new Vector2(-.40f,.64f) :
            pose == 7 ? new Vector2(1.99f,1.25f) :
            pose == 8 ? new Vector2(1.16f,1f) :
            pose == 0 ? new Vector2(1.12f,1.15f) : new Vector2(1.13f,.65f);
        if (rig != null) brooch = rig.Brooch;
        Vector2 mantleCenter = brooch + new Vector2(-1.07f,-.45f)*compensation;
        mantle.transform.localPosition = new Vector3(mantleCenter.x*sign,mantleCenter.y,0f);
        mantle.transform.localScale = Vector3.one*compensation;
        mantle.flipX = flipped;
        mantle.color = tint;
        mantle.enabled = rig==null || !rig.HasThreeQuarterTorso;
        float forward=Mathf.Clamp(velocity.x*sign/9f,-1f,1f);
        float rise=Mathf.Clamp(velocity.y/12f,-1f,1f);
        trailing=Mathf.SmoothDamp(trailing,forward,ref trailVelocity,.18f,Mathf.Infinity,Time.deltaTime);
        vertical=Mathf.SmoothDamp(vertical,rise,ref verticalVelocity,.2f,Mathf.Infinity,Time.deltaTime);
        for (int i = 0; i < bodyDrawings.Length; i++)
        {
            QoriRopeMotion drawing = bodyDrawings[i];
            if (drawing == null) continue;
            bool visible = i == pose && rig == null;
            if (i >= 9) drawing.ShowWalking(visible,0f,0f,flipped,tint,0f,0f,null,true);
            else if (i == 7) drawing.Show(visible,velocity,flipped,tint,false,true);
            else drawing.ShowWalking(visible,trailing,stepWave,flipped,tint,vertical,pose == 0 ? 1f : 0f,null,true);
        }
        for(int n=0;n<3;n++)
        {
            Panel p=panels[n];p.renderer.enabled=true;
            bool walkingPose = (pose >= 1 && pose <= 6) || pose == 8;
            // A more vertical drape on the ground; airborne and rope angles retain their sweep.
            float restingAngle = (pose == 8 ? -12f : walkingPose ? -26f : -40f) + n * 6f;
            float swingForce=swing!=null?Mathf.Clamp(swing.SecondaryForce,-5f,5f)*swingWeight:0f;
            float target=Mathf.Clamp(restingAngle-forward*22f+Mathf.Max(0f,rise)*8f+Mathf.Min(0f,rise)*48f+swingForce,-100f,20f);
            if(!activeLastFrame) {p.angle=restingAngle;p.tip=p.angle;p.angularVelocity=p.tipVelocity=0f;}
            float remaining=Mathf.Min(Time.deltaTime,.05f);
            while(remaining>0f)
            {
                float dt=Mathf.Min(remaining,1f/120f);
                Spring(ref p.angle,ref p.angularVelocity,target,100f-n*15f,dt);
                Spring(ref p.tip,ref p.tipVelocity,p.angle,65f-n*10f,dt);
                remaining-=dt;
            }
            float maxDepth=p.shape[p.shape.Length-1].y;
            // Integrate a curved centerline in short equal-length segments; width remains fixed.
            for(int index=0;index<p.vertices.Length;index++)
            {
                Vector2 shape=p.shape[index];float depth=shape.y;
                float cx=0f,cy=0f;
                const int subdivisions=12;
                for(int s=0;s<subdivisions;s++)
                {
                    float d=depth*(s+.5f)/subdivisions;
                    float angle=Mathf.Lerp(p.angle,p.tip,Mathf.Clamp01(d/maxDepth))*Mathf.Deg2Rad;
                    cx+=Mathf.Sin(angle)*depth/subdivisions;cy-=Mathf.Cos(angle)*depth/subdivisions;
                }
                float localAngle=Mathf.Lerp(p.angle,p.tip,Mathf.Clamp01(depth/maxDepth))*Mathf.Deg2Rad;
                p.vertices[index]=new Vector3((cx+shape.x*Mathf.Cos(localAngle))*sign,
                    cy+shape.x*Mathf.Sin(localAngle),0f);
                p.colors[index]=tint;
            }
            Vector2 mountOffset=new Vector2(-1.16f,.50f)*compensation;
            if(swingWeight>0f)
            {
                // Follow the collar at the mount only; the hanging panels retain
                // their gravity-relative drape and their existing spring history.
                float angle=swing.TorsoAngle*swingWeight*Mathf.Deg2Rad;
                mountOffset=new Vector2(mountOffset.x*Mathf.Cos(angle)-mountOffset.y*Mathf.Sin(angle),
                    mountOffset.x*Mathf.Sin(angle)+mountOffset.y*Mathf.Cos(angle));
            }
            Vector2 shoulder = brooch + mountOffset;
            p.renderer.transform.localScale=Vector3.one*compensation;
            p.renderer.transform.localPosition=new Vector3((shoulder.x+n*.08f*compensation)*sign,shoulder.y-n*.035f*compensation,0f);
            p.mesh.vertices=p.vertices;p.mesh.colors=p.colors;p.mesh.RecalculateBounds();
        }
        activeLastFrame=true;
        return true;
    }
    public void Hide()
    {
        if(mantle!=null) mantle.enabled=false;
        activeLastFrame=false;trailing=trailVelocity=vertical=verticalVelocity=0f;
        foreach(QoriRopeMotion drawing in bodyDrawings)
            if(drawing!=null)drawing.ShowWalking(false,0f,0f,false,Color.white);
        foreach(Panel p in panels) if(p!=null && p.renderer!=null)p.renderer.enabled=false;
    }
    private void OnDisable(){Hide();}
    private void OnDestroy()
    {
        if(mantle!=null) Destroy(mantle.gameObject);
        foreach(QoriRopeMotion drawing in bodyDrawings) if(drawing!=null)Destroy(drawing);
        foreach(Panel p in panels)
        {
            if(p==null)continue;
            if(p.renderer!=null)Destroy(p.renderer.gameObject);
            if(p.mesh!=null)Destroy(p.mesh);
            if(p.material!=null)Destroy(p.material);
        }
    }
}

