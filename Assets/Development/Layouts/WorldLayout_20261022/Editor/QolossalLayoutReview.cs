// Room-shell review only. No gameplay scene, save data or build-list changes.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class QolossalLayoutReview
{
    public const string Root = "Assets/Development/Layouts/WorldLayout_20261022";
    public const string ScenePath = Root + "/Qolossal_WorldLayout_Borders.unity";
    const string Evidence = "C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/UnityLayout_Blockout_20261022";
    [Serializable] public class V { public float x,y; public Vector2 P => new Vector2(x,y); public Vector3 P3 => new Vector3(x,y,0); }
    [Serializable] public class Room { public string id,name,kind,group,color; public float x,y,w,h; public Bounds Bounds => new Bounds(new Vector3(x+w/2,y+h/2,0),new Vector3(w,h,1)); }
    [Serializable] public class Wall { public V a,b; public string color,owner; }
    [Serializable] public class Path { public V[] points; }
    [Serializable] public class Surface { public string id,color; public V[] points; }
    [Serializable] public class Portal { public string id,room,color,kind; public V surface,arrival; }
    [Serializable] public class Connection { public string id,fromRoom,toRoom; public float width; public V[] points; }
    [Serializable] public class Data { public string source,sourceSha256; public Room[] rooms; public Wall[] walls; public Path[] collisionLoops; public Surface[] surfaces; public Portal[] portals; public Connection[] connections; }
    static Data Read() => JsonUtility.FromJson<Data>(File.ReadAllText(Root+"/layout_geometry.json"));
    static Color ColorOf(string hex) { ColorUtility.TryParseHtmlString(hex,out var c); return c; }
    static GameObject Child(string name,Transform parent) { var go=new GameObject(name); if(parent!=null)go.transform.SetParent(parent,false); return go; }
    static LineRenderer Line(string name,Transform parent,Vector3[] points,Color c,Material material,float width=.35f)
    {
        var go=Child(name,parent); var lr=go.AddComponent<LineRenderer>(); lr.useWorldSpace=true;
        lr.positionCount=points.Length;lr.SetPositions(points);lr.startWidth=lr.endWidth=width;
        lr.startColor=lr.endColor=c;lr.sharedMaterial=material;lr.numCapVertices=2;lr.numCornerVertices=2;
        lr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;lr.receiveShadows=false;
        lr.sortingOrder=2;return lr;
    }
    static void CrossMarker(string name,Transform parent,Vector3 point,Color color,Material material)
    {
        var g=Child(name,parent);g.transform.position=point;
        Line("Door position",g.transform,new[]{point+Vector3.left*.8f,point+Vector3.right*.8f},color,material,.25f);
        Line("Door position",g.transform,new[]{point+Vector3.down*.8f,point+Vector3.up*.8f},color,material,.25f);
    }
    public static object Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play mode before building.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("An existing scene has unsaved changes; leave it untouched.");
        if(File.Exists(ScenePath))throw new InvalidOperationException("Review scene already exists; refusing to overwrite review history.");
        var d=Read();
        var prior=EditorSceneManager.GetSceneManagerSetup();
        File.WriteAllText(Evidence+"/Previous_Editor_Scenes.json",Newtonsoft.Json.JsonConvert.SerializeObject(prior,Newtonsoft.Json.Formatting.Indented));
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=Child("QOLOSSAL | ROOM BORDERS | 1 Unity unit = 1 m",null);
        var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if(shader==null)throw new InvalidOperationException("URP 2D unlit shader unavailable.");
        var material=new Material(shader){name="Layout Border Unlit"};
        AssetDatabase.CreateAsset(material,Root+"/LayoutBorder.mat");
        var shell=Child("01 Borders - rooms and open connecting passages",root.transform);
        var groups=new Dictionary<string,Transform>();
        foreach(var w in d.walls)
        {
            if(!groups.TryGetValue(w.owner,out var parent)){parent=Child(w.owner,shell.transform).transform;groups.Add(w.owner,parent);}
            Line("Boundary",parent,new[]{w.a.P3,w.b.P3},ColorOf(w.color),material);
        }
        var collision=Child("02 Collision - continuous perimeter loops",root.transform);
        int n=0;
        foreach(var p in d.collisionLoops)
        {
            var go=Child("Perimeter "+(++n).ToString("D2"),collision.transform);
            var edge=go.AddComponent<EdgeCollider2D>();edge.points=p.points.Select(v=>v.P).ToArray();edge.edgeRadius=0;
        }
        var rooms=Child("03 Rooms - select then Qolossal Layout Review Fit Selected Room",root.transform);
        foreach(var r in d.rooms)
        {
            var g=Child(r.id+" | "+r.name+" | "+r.w+" x "+r.h+" u",rooms.transform);g.transform.position=r.Bounds.center;
        }
        var surface=Child("04 Upper ground - authored surface outlines",root.transform);
        foreach(var s in d.surfaces)
        {
            var lr=Line(s.id,surface.transform,s.points.Select(v=>v.P3).ToArray(),ColorOf(s.color),material,.45f);
            var ec=lr.gameObject.AddComponent<EdgeCollider2D>();ec.points=s.points.Select(v=>v.P).ToArray();
        }
        var transitions=Child("05 Paired surface doors - scene transitions, not shafts",root.transform);
        foreach(var p in d.portals)
        {
            var g=Child(p.id+" -> "+p.room,transitions.transform);var c=ColorOf(p.color);
            CrossMarker("SURFACE | "+p.id,g.transform,p.surface.P3,c,material);
            CrossMarker("ROOM | "+p.id,g.transform,p.arrival.P3,c,material);
        }
        var anchors=Child("06 Passage centerlines - editor route references only",root.transform);
        foreach(var p in d.connections)
        {
            var g=Child(p.id+" | clear width "+p.width+" u",anchors.transform);
            foreach(var v in p.points)Child("Waypoint",g.transform).transform.position=v.P3;
        }
        var cameras=Child("07 Review cameras",root.transform);
        var overview=Child("Layout Overview Camera - inspection zoom only",cameras.transform).AddComponent<Camera>();
        overview.tag="MainCamera";overview.orthographic=true;overview.orthographicSize=760;
        overview.transform.position=new Vector3(1240,290,-10);overview.clearFlags=CameraClearFlags.SolidColor;
        overview.backgroundColor=new Color(.025f,.04f,.06f);overview.nearClipPlane=.1f;overview.farClipPlane=100;
        overview.allowHDR=false;overview.allowMSAA=true;
        var normal=Child("Normal Gameplay Scale Reference - 17.78 x 10 at 16 by 9",cameras.transform).AddComponent<Camera>();
        normal.CopyFrom(overview);normal.tag="Untagged";normal.orthographicSize=5;normal.enabled=false;
        normal.transform.position=new Vector3(51,-169,-10);
        var heartPorts=Child("08 Heart - four open entrances",root.transform);
        foreach(var p in d.connections.Where(c=>c.toRoom=="HEART"))Child(p.id,heartPorts.transform).transform.position=p.points.Last().P3;
        SetOverviewWidths(scene,760);
        EditorSceneManager.SaveScene(scene,ScenePath);
        Selection.activeGameObject=root;
        WorldView();
        var result=new {scene=ScenePath,sourceSha256=d.sourceSha256,rooms=d.rooms.Length,passages=d.connections.Length,heartEntrances=4,pairedSurfaceDoors=d.portals.Length,borders=d.walls.Length,collisionLoops=d.collisionLoops.Length,interiorDetails=0,flightComponents=0,originalScenesChanged=false,normalCameraSize=5};
        File.WriteAllText(Evidence+"/Unity_Build_Receipt.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
    static void RequireScene()
    {
        if(SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Open the Qolossal WorldLayout Borders review scene first.");
    }
    static void View(Vector3 center,Vector3 size)
    {
        RequireScene();var view=SceneView.lastActiveSceneView??EditorWindow.GetWindow<SceneView>();view.in2DMode=true;
        // Bounds.Frame adds excessive spherical padding to a long 2D world.
        float aspect=Mathf.Max(.5f,view.camera.aspect);
        float halfHeight=Mathf.Max(size.y*.5f,size.x*.5f/aspect)*1.03f;
        view.pivot=center;view.rotation=Quaternion.identity;view.size=halfHeight;view.orthographic=true;view.Repaint();view.Focus();
    }
    [MenuItem("Qolossal/Layout Review/01 Whole World")]
    public static void WorldView()=>View(new Vector3(1240,290,0),new Vector3(2630,1080,1));
    [MenuItem("Qolossal/Layout Review/02 Heart and Four Routes")]
    public static void NetworkView()=>View(new Vector3(1800,255,0),new Vector3(710,460,1));
    [MenuItem("Qolossal/Layout Review/03 Heart Hall")]
    public static void HeartView()=>View(new Vector3(1796,194,0),new Vector3(100,60,1));
    [MenuItem("Qolossal/Layout Review/04 Early Cave Networks")]
    public static void EarlyView()=>View(new Vector3(490,-8,0),new Vector3(980,400,1));
    static Room SelectedRoom()
    {
        var go=Selection.activeGameObject;if(go==null)throw new InvalidOperationException("Select a room under 03 Rooms.");
        return Read().rooms.FirstOrDefault(r=>go.name.StartsWith(r.id+" |",StringComparison.Ordinal))??throw new InvalidOperationException("Select a room under 03 Rooms.");
    }
    [MenuItem("Qolossal/Layout Review/05 Fit Selected Room")]
    public static void FitSelected(){var r=SelectedRoom();View(r.Bounds.center,r.Bounds.size+new Vector3(4,4,0));}
    [MenuItem("Qolossal/Layout Review/06 Normal Camera Scale at Selected Room")]
    public static void NormalSelected()
    {
        RequireScene();var r=SelectedRoom();var cam=ReviewCamera();cam.transform.position=new Vector3(r.x+r.w/2,r.y+5,-10);cam.orthographicSize=5;
        SetOverviewWidths(SceneManager.GetActiveScene(),5);SceneView.RepaintAll();
    }
    [MenuItem("Qolossal/Layout Review/07 Restore Overview Camera")]
    public static void RestoreCamera()
    {
        RequireScene();var cam=ReviewCamera();cam.transform.position=new Vector3(1240,290,-10);cam.orthographicSize=760;
        SetOverviewWidths(SceneManager.GetActiveScene(),760);SceneView.RepaintAll();
    }
    static Camera ReviewCamera()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>()).First(c=>c.enabled);
    static void SetOverviewWidths(Scene scene,float size)
    {
        // A review-only minimum stroke keeps the huge overview readable. Collider positions never change.
        float width=Mathf.Max(.18f,size/540f);
        foreach(var lr in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LineRenderer>()))lr.startWidth=lr.endWidth=width;
    }
    public static object Validate()
    {
        RequireScene();var scene=SceneManager.GetActiveScene();var d=Read();Physics2D.SyncTransforms();
        var roots=scene.GetRootGameObjects();var cs=roots.SelectMany(g=>g.GetComponentsInChildren<Collider2D>()).ToArray();
        var failures=new List<string>();int samples=0;
        foreach(var link in d.connections)
        {
            for(int i=1;i<link.points.Length;i++)
            {
                Vector2 a=link.points[i-1].P,b=link.points[i].P;int count=Mathf.Max(2,Mathf.CeilToInt(Vector2.Distance(a,b)*2));
                for(int j=0;j<=count;j++)
                {
                    var p=Vector2.Lerp(a,b,j/(float)count);samples++;
                    var hits=Physics2D.OverlapCircleAll(p,link.width*.5f-.08f);
                    if(hits.Any(c=>c.gameObject.scene==scene)){failures.Add(link.id+" blocked at "+p);break;}
                }
            }
        }
        foreach(var portal in d.portals)
            if(Physics2D.OverlapCircleAll(portal.arrival.P,.45f).Any(c=>c.gameObject.scene==scene))failures.Add(portal.id+" arrival touches a boundary");
        var missing=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        if(missing>0)failures.Add("Missing scripts: "+missing);
        var scripts=roots.SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Where(m=>m!=null).Select(m=>m.GetType().Name).ToArray();
        var report=new {pass=failures.Count==0,scene=scene.path,roomCount=d.rooms.Length,passageCount=d.connections.Length,physicsClearanceSamples=samples,portalArrivalChecks=d.portals.Length,colliders=cs.Length,heartEntrances=4,missingScripts=missing,components=scripts,failures=failures.ToArray()};
        File.WriteAllText(Evidence+"/Unity_Physics_QA.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return report;
    }
    public static object Capture(string name,float x,float y,float size)
    {
        RequireScene();var cam=ReviewCamera();var oldPosition=cam.transform.position;float oldSize=cam.orthographicSize;
        var lines=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LineRenderer>()).ToArray();var widths=lines.Select(l=>l.startWidth).ToArray();
        var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;RenderTexture rt=null;Texture2D image=null;
        try
        {
            cam.transform.position=new Vector3(x,y,-10);cam.orthographicSize=size;SetOverviewWidths(SceneManager.GetActiveScene(),size);
            rt=new RenderTexture(3840,2160,24){antiAliasing=4};rt.Create();cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            image=new Texture2D(3840,2160,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,3840,2160),0,0);image.Apply();
            var path=Evidence+"/"+name+".png";File.WriteAllBytes(path,image.EncodeToPNG());return new {path,width=3840,height=2160,orthographicSize=size};
        }
        finally
        {
            cam.targetTexture=oldTarget;cam.transform.position=oldPosition;cam.orthographicSize=oldSize;RenderTexture.active=oldActive;
            for(int i=0;i<lines.Length;i++)lines[i].startWidth=lines[i].endWidth=widths[i];
            if(image!=null)UnityEngine.Object.DestroyImmediate(image);if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        }
    }
}
