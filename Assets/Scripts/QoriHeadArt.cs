using UnityEngine;
using UnityEngine.InputSystem;

// Shared three-quarter head, independently attached to every body painting.
public sealed class QoriHeadArt
{
    const int Grid=64;
    readonly Mesh[] meshes=new Mesh[4];
    readonly MeshRenderer[] displays=new MeshRenderer[4];
    readonly float[] weights=new float[4];
    Material material;float shownAngle,angleVelocity;bool initialized;int resetVersion=-1;
    readonly Vector3[] vertices=new Vector3[(Grid+1)*(Grid+1)];
    readonly Vector2[] uv=new Vector2[(Grid+1)*(Grid+1)];
    readonly Color[] colors=new Color[(Grid+1)*(Grid+1)];
    Texture2D atlas;
    public int Pose { get; private set; }
    public bool Ready=>displays[0]!=null;
    // Atlas cells have a measured gutter at x=640, rather than exactly half-width.
    static readonly Rect[] Cells={new Rect(0,627,640,627),new Rect(640,627,614,627),new Rect(0,0,640,627),new Rect(640,0,614,627)};
    static readonly Vector2[] Pivots={new Vector2(480,105),new Vector2(435,115),new Vector2(475,120),new Vector2(440,135)};
    public void Initialize(Transform parent,SpriteRenderer source)
    {
        atlas=Resources.Load<Texture2D>("QoriRig/Qori_HeadAtlas_v1");
        Shader shader=Shader.Find("Sprites/Default");
        if(atlas==null||shader==null)return;
        material=new Material(shader){mainTexture=atlas};
        var triangles=new int[Grid*Grid*6];
        for(int y=0;y<Grid;y++)for(int x=0;x<Grid;x++)
        {int i=y*(Grid+1)+x,t=(y*Grid+x)*6;triangles[t]=i;triangles[t+1]=i+Grid+1;triangles[t+2]=i+1;triangles[t+3]=i+1;triangles[t+4]=i+Grid+1;triangles[t+5]=i+Grid+2;}
        for(int pose=0;pose<4;pose++)
        {
            var obj=new GameObject(pose==0?"Qori three-quarter head":"Qori head transition "+pose);obj.transform.SetParent(parent,false);
            var display=displays[pose]=obj.AddComponent<MeshRenderer>();display.sortingLayerID=source.sortingLayerID;display.sortingOrder=source.sortingOrder+2;display.sharedMaterial=material;
            var mesh=meshes[pose]=new Mesh{name="Qori expressive head "+pose};mesh.MarkDynamic();
            Rect cell=Cells[pose];
            for(int y=0;y<=Grid;y++)for(int x=0;x<=Grid;x++)uv[y*(Grid+1)+x]=new Vector2((cell.x+cell.width*x/Grid)/atlas.width,(cell.y+cell.height*y/Grid)/atlas.height);
            mesh.vertices=vertices;mesh.triangles=triangles;mesh.uv=uv;obj.AddComponent<MeshFilter>().sharedMesh=mesh;display.enabled=false;
        }
    }
    static float Smooth(float a,float b,float v)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));
    static Vector2 Rotate(Vector2 p,float angle){float r=angle*Mathf.Deg2Rad;return new Vector2(p.x*Mathf.Cos(r)-p.y*Mathf.Sin(r),p.x*Mathf.Sin(r)+p.y*Mathf.Cos(r));}
    public void Show(Vector2 neck,float facing,float angle,Color tint,PlayerMovement movement,PlayerCombat combat,QoriSecondaryMotion secondary,bool climbing,bool swinging=false)
    {
        if(!Ready)return;
        var keyboard=Keyboard.current;var pad=Gamepad.current;
        bool down=!GamePauseMenu.BlocksGameplayInput&&((keyboard!=null&&(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed))||(pad!=null&&(pad.leftStick.y.ReadValue()<-.5f||pad.dpad.down.isPressed)));
        bool up=combat!=null&&combat.IsAttackPoseActive&&combat.IsUpwardAttack;
        down|=combat!=null&&combat.IsAttackPoseActive&&combat.IsDownwardAttack;
        int pose=up||climbing?1:down||(!movement.IsGrounded&&movement.ObservedVelocity.y< -2)?2:Mathf.Abs(movement.ObservedVelocity.x)>1?3:0;
        // Pendulum velocity crosses the walk/fall thresholds twice per arc.
        // Keep one painted silhouette on the thread; neck rotation supplies motion.
        if(swinging)pose=3;
        Pose=pose;
        if(!initialized||resetVersion!=movement.ResetVersion)
        {for(int i=0;i<4;i++)weights[i]=i==pose?1:0;shownAngle=angle;angleVelocity=0;initialized=true;resetVersion=movement.ResetVersion;}
        shownAngle=Mathf.SmoothDampAngle(shownAngle,angle,ref angleVelocity,.055f,Mathf.Infinity,Time.deltaTime);
        float blend=1-Mathf.Exp(-Time.deltaTime/.025f);
        // Blend registered paintings at the same neck anchor, without an attack-only
        // front-face swap. Gaze changes no longer replace the silhouette in one frame.
        for(int layer=0;layer<4;layer++)
        {
        weights[layer]=Mathf.Lerp(weights[layer],layer==pose?1:0,blend);
        var display=displays[layer];display.enabled=weights[layer]>.005f;if(!display.enabled)continue;
        var mesh=meshes[layer];Rect cell=Cells[layer];Vector2 pivot=Pivots[layer];
        float blink=layer==0&&secondary!=null?secondary.Blink:0;
        float ear=secondary!=null?secondary.EarAngle:0;
        for(int y=0;y<=Grid;y++)for(int x=0;x<=Grid;x++)
        {
            int i=y*(Grid+1)+x;Vector2 pixel=new Vector2(cell.width*x/Grid,cell.height*y/Grid);
            Vector2 p=(pixel-pivot)*.01f;
            float earWeight=1-Smooth(330,450,pixel.x);
            Vector2 earPivot=new Vector2(-.65f,1.8f);
            p=Vector2.Lerp(p,earPivot+Rotate(p-earPivot,ear),earWeight);
            // Compress the two painted eyes independently into a closed line at idle.
            if(blink>0)
            {
                foreach(float eyeX in EyeXs)
                {
                    float weight=(1-Smooth(32,56,Mathf.Abs(pixel.x-eyeX)))*(1-Smooth(40,70,Mathf.Abs(pixel.y-210)));
                    p.y-=((pixel.y-210)*.01f)*blink*.94f*weight;
                }
            }
            p=neck+Rotate(p,shownAngle);vertices[i]=new Vector3(p.x*facing,p.y,0);colors[i]=tint;colors[i].a*=weights[layer];
        }
        mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();
        }
    }
    static readonly float[] EyeXs={505,620};
    public void Hide(){foreach(var display in displays)if(display!=null)display.enabled=false;}
    public void Dispose(){foreach(var display in displays)if(display!=null)Object.Destroy(display.gameObject);foreach(var mesh in meshes)if(mesh!=null)Object.Destroy(mesh);if(material!=null)Object.Destroy(material);}
}
