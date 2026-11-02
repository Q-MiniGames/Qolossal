using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Authoring only. Copies affected baked meshes; original gallery assets remain recoverable.
public static class RegionalCaveRolloutCarver
{
    public sealed class Plan
    {
        public string id;
        public bool right;
        public float wall, floor, rise;
        public Vector2[] outline;
        public float Sign => right ? 1 : -1;
        public Vector2 At(float u, float v) => new Vector2(wall + Sign * Mathf.Lerp(-.75f, 3.4f, u), floor + rise * u + v);
    }
    struct Vertex
    {
        public Vector3 p;
        public Vector4 u0, u1, u2;
        public Color c;
        public static Vertex Mix(Vertex a, Vertex b, float t) => new Vertex {
            p=Vector3.LerpUnclamped(a.p,b.p,t), u0=Vector4.LerpUnclamped(a.u0,b.u0,t),
            u1=Vector4.LerpUnclamped(a.u1,b.u1,t), u2=Vector4.LerpUnclamped(a.u2,b.u2,t), c=Color.LerpUnclamped(a.c,b.c,t)
        };
    }
    static float Side(Vector2 a, Vector2 b, Vector3 p) => (b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
    static List<Vertex> Half(List<Vertex> input, Vector2 a, Vector2 b, bool inside)
    {
        var output=new List<Vertex>(); if(input.Count==0)return output;
        var prev=input[input.Count-1]; float d0=Side(a,b,prev.p);
        foreach(var curr in input) {
            float d1=Side(a,b,curr.p);bool was=inside?d0>=0:d0<=0, now=inside?d1>=0:d1<=0;
            if(was!=now)output.Add(Vertex.Mix(prev,curr,d0/(d0-d1)));
            if(now)output.Add(curr);prev=curr;d0=d1;
        }
        return output;
    }
    static bool Area(List<Vertex> p)
    {
        double a=0;for(int i=0;i<p.Count;i++){var x=p[i].p;var y=p[(i+1)%p.Count].p;a+=(double)x.x*y.y-(double)y.x*x.y;}
        return Math.Abs(a)>.000001;
    }
    static List<List<Vertex>> Subtract(List<Vertex> triangle, Vector2[] hole, ref bool changed)
    {
        for(int i=0;i<hole.Length;i++)if(triangle.All(v=>Side(hole[i],hole[(i+1)%hole.Length],v.p)<=0))return new List<List<Vertex>>{triangle};
        var inside=triangle;
        for(int i=0;i<hole.Length&&Area(inside);i++)inside=Half(inside,hole[i],hole[(i+1)%hole.Length],true);
        if(!Area(inside))return new List<List<Vertex>>{triangle};
        changed=true;var pieces=new List<List<Vertex>>();var rem=triangle;
        for(int i=0;i<hole.Length&&Area(rem);i++) {
            var outside=Half(rem,hole[i],hole[(i+1)%hole.Length],false);if(Area(outside))pieces.Add(outside);
            rem=Half(rem,hole[i],hole[(i+1)%hole.Length],true);
        }
        return pieces;
    }
    static Mesh MeshOf(List<Vertex> vertices, List<int> indices, Transform transform=null)
    {
        var mesh=new Mesh{indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
        mesh.SetVertices(vertices.Select(v=>transform==null?v.p:transform.InverseTransformPoint(v.p)).ToList());
        mesh.SetUVs(0,vertices.Select(v=>v.u0).ToList());mesh.SetUVs(1,vertices.Select(v=>v.u1).ToList());mesh.SetUVs(2,vertices.Select(v=>v.u2).ToList());
        mesh.SetColors(vertices.Select(v=>v.c).ToList());mesh.SetTriangles(indices,0);mesh.RecalculateBounds();return mesh;
    }
    static void Append(List<Vertex> poly,List<Vertex> vertices,List<int> indices)
    {
        int first=vertices.Count;vertices.AddRange(poly);for(int i=1;i<poly.Count-1;i++)indices.AddRange(new[]{first,first+i,first+i+1});
    }
    static bool Interval(Vector2 a,Vector2 b,Vector2[] hole,out float lo,out float hi)
    {
        lo=0;hi=1;
        for(int i=0;i<hole.Length;i++) {
            float x=Side(hole[i],hole[(i+1)%hole.Length],a), y=Side(hole[i],hole[(i+1)%hole.Length],b), delta=y-x;
            if(Mathf.Abs(delta)<.000001f){if(x<0)return false;continue;}
            float t=-x/delta;if(delta>0)lo=Mathf.Max(lo,t);else hi=Mathf.Min(hi,t);if(lo>hi)return false;
        }
        return hi-lo>.00001f;
    }
    static Bounds BoundsOf(Vector2[] poly)
    {
        var b=new Bounds(poly[0],Vector3.zero);foreach(var p in poly)b.Encapsulate(p);b.Expand(new Vector3(.3f,.3f,10));return b;
    }
    public static object Build(string folder, Plan[] plans, bool approaches=false, string operationName=null)
    {
        if (plans.Any(p=>CaveAccessPolicy.IsRetiredEntrance(p.id))) throw new Exception("C01-C05 surface access in MR01-MR03 was retired; use C06. Rooms and internal passages remain active.");
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!=RegionalSceneCatalog.Underground)throw new Exception("Open consolidated underground");
        string rootName=operationName??(approaches?"Regional cave approaches | shared cuts":"Regional cave returns | shared cuts");if(GameObject.Find(rootName)!=null)throw new Exception("Already carved; reuse saved scene cuts");
        System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var points=UnityEngine.Object.FindObjectsByType<CaveConnectionPoint>(FindObjectsSortMode.None).Where(p=>plans.Any(q=>q.id==p.lowerRoomId)).ToArray();
        foreach(var plan in plans){plan.outline=new[]{plan.At(0,-.12f),plan.At(1,-.12f),plan.At(1,1.6f),plan.At(.95f,2.4f),plan.At(.8f,2.95f),plan.At(.15f,3),plan.At(0,2.85f)};if(!plan.right)Array.Reverse(plan.outline);if(approaches){float a=plan.wall-plan.Sign*.65f,b=plan.wall-plan.Sign*2.6f,l=Mathf.Min(a,b),r=Mathf.Max(a,b);plan.outline=new[]{new Vector2(l,plan.floor-.12f),new Vector2(r,plan.floor-.12f),new Vector2(r,plan.floor+3.05f),new Vector2(l,plan.floor+3.05f)};}}
        foreach(var point in points) {
            foreach(var t in point.GetComponentsInChildren<Transform>(true))if(t.name.StartsWith("Entrance prototype |")||t.name.StartsWith("Painted stair tread"))t.gameObject.SetActive(false);
            foreach(var r in point.GetComponentsInChildren<SpriteRenderer>(true))if(r.name=="Entrance"||r.name.StartsWith("Dressing ("))r.enabled=false;
        }
        var cutsRoot=new GameObject(rootName);
        int changedMeshes=0,changedEdges=0;
        var replacements=new List<object>();
        foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) {
            var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled||filter.sharedMesh==null)continue;
            var holes=plans.Where(p=>renderer.bounds.Intersects(BoundsOf(p.outline))).ToArray();if(holes.Length==0)continue;
            if(!(filter.name.StartsWith("Rock_")||filter.name.StartsWith("Rim_")||filter.name.StartsWith("Natural_Rim_")||filter.name.StartsWith("Continuous_")))continue;
            var source=filter.sharedMesh;if(source.subMeshCount!=1)throw new Exception("Unexpected multi-material terrain "+filter.name);
            var positions=source.vertices;var colors=source.colors;var uv=new List<Vector4>[3];for(int i=0;i<3;i++){uv[i]=new List<Vector4>();source.GetUVs(i,uv[i]);}
            var original=positions.Select((p,i)=>new Vertex{p=filter.transform.TransformPoint(p),c=colors.Length>i?colors[i]:Color.white,u0=uv[0].Count>i?uv[0][i]:Vector4.zero,u1=uv[1].Count>i?uv[1][i]:Vector4.zero,u2=uv[2].Count>i?uv[2][i]:Vector4.zero}).ToArray();
            var vertices=new List<Vertex>();var indices=new List<int>();bool changed=false;var triangles=source.triangles;
            for(int i=0;i<triangles.Length;i+=3) {
                var pieces=new List<List<Vertex>>{new List<Vertex>{original[triangles[i]],original[triangles[i+1]],original[triangles[i+2]]}};
                foreach(var hole in holes){var next=new List<List<Vertex>>();foreach(var poly in pieces)next.AddRange(Subtract(poly,hole.outline,ref changed));pieces=next;}
                foreach(var poly in pieces)Append(poly,vertices,indices);
            }
            if(!changed)continue;
            var mesh=MeshOf(vertices,indices,filter.transform);mesh.name=source.name+" | embedded returns";
            string path=folder+"/Cut_"+(changedMeshes++).ToString("D3")+".asset";AssetDatabase.CreateAsset(mesh,path);
            replacements.Add(new{objectName=filter.name,source=AssetDatabase.GetAssetPath(source),replacement=path});filter.sharedMesh=mesh;EditorUtility.SetDirty(filter);
        }
        foreach(var edge in UnityEngine.Object.FindObjectsByType<EdgeCollider2D>(FindObjectsSortMode.None)) {
            if(!edge.enabled||edge.isTrigger||(approaches&&edge.name.StartsWith("Embedded return |")))continue;
            var holes=plans.Where(p=>edge.bounds.Intersects(BoundsOf(p.outline))).ToArray();if(holes.Length==0)continue;
            var pts=edge.points;var paths=new List<List<Vector2>>();List<Vector2> current=null;bool changed=false;
            for(int i=0;i<pts.Length-1;i++) {
                Vector2 a=edge.transform.TransformPoint(pts[i]+edge.offset),b=edge.transform.TransformPoint(pts[i+1]+edge.offset);
                var parts=new List<(float lo,float hi)>{(0,1)};
                foreach(var hole in holes)if(Interval(a,b,hole.outline,out float lo,out float hi)) {
                    var next=new List<(float,float)>();foreach(var part in parts){float l=Mathf.Max(part.lo,lo),h=Mathf.Min(part.hi,hi);if(h-l<.00001f){next.Add(part);continue;}changed=true;if(l>part.lo+.00001f)next.Add((part.lo,l));if(h<part.hi-.00001f)next.Add((h,part.hi));}parts=next;
                }
                foreach(var part in parts) {
                    var start=Vector2.Lerp(a,b,part.lo);var end=Vector2.Lerp(a,b,part.hi);
                    if(current==null||Vector2.Distance(current[current.Count-1],start)>.0001f){current=new List<Vector2>{start};paths.Add(current);}current.Add(end);
                }
            }
            if(!changed)continue;changedEdges++;edge.enabled=false;EditorUtility.SetDirty(edge);
            for(int i=0;i<paths.Count;i++){var go=new GameObject(edge.name+" | preserved segment "+i);go.transform.SetParent(cutsRoot.transform,false);go.layer=edge.gameObject.layer;var c=go.AddComponent<EdgeCollider2D>();c.points=paths[i].ToArray();c.edgeRadius=edge.edgeRadius;c.sharedMaterial=edge.sharedMaterial;}
        }
        var rock=AssetDatabase.LoadAssetAtPath<Material>("Assets/Development/Layouts/UndergroundRegional_20261023/Regional_Rock_Blend.mat");
        var edgeMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Development/Layouts/UndergroundRegional_20261023/Regional_Painted_Edges.mat");
        if(rock==null||edgeMaterial==null)throw new Exception("Native materials missing");
        foreach(var plan in plans) {
            var point=points.Single(p=>p.lowerRoomId==plan.id);if(approaches){var e=point.GetComponentsInChildren<EdgeCollider2D>().Single(e=>e.name.StartsWith("Embedded return |"));var a=e.points;a[0]=new Vector2(plan.wall-plan.Sign*2.6f,plan.floor);e.points=a;EditorUtility.SetDirty(e);continue;}var root=new GameObject("Embedded return | "+plan.id+" | "+(plan.rise>0?"UP LEFT":plan.right?"RIGHT":"LEFT"));root.transform.SetParent(point.transform,true);
            var path=new[]{plan.At(0,0),plan.At(1,0),plan.At(1,1.6f),plan.At(.95f,2.4f),plan.At(.8f,2.95f),plan.At(.15f,3),plan.At(0,2.85f)};
            var collision=root.AddComponent<EdgeCollider2D>();root.layer=LayerMask.NameToLayer("Ground");collision.points=path;
            var vertices=plan.outline.Select(p=>{float u=Mathf.InverseLerp(plan.At(0,0).x,plan.At(1,0).x,p.x);float shade=Mathf.Lerp(.32f,.07f,u);return new Vertex{p=p,u0=new Vector4(1,0,0,0),c=new Color(shade,shade,shade,Mathf.Clamp01(u/.18f))};}).ToList();
            var inds=new List<int>();for(int i=1;i<vertices.Count-1;i++)inds.AddRange(new[]{0,i,i+1});
            CreatePart(root,"Recess backing",MeshOf(vertices,inds),rock,-1,folder+"/"+plan.id+"_Backing.asset");
            vertices=new List<Vertex>();inds=new List<int>();float distance=0;
            for(int i=0;i<path.Length-1;i++) {
                var a=path[i];var b=path[i+1];var dir=(b-a).normalized;var normal=new Vector2(-dir.y,dir.x)*(plan.right?-1:1);float length=Vector2.Distance(a,b);
                var poly=new List<Vertex>();foreach(var item in new[]{(a,0f,0f),(b,length,0f),(b+normal*.28f,length,1f),(a+normal*.28f,0f,1f)})poly.Add(new Vertex{p=item.Item1,u0=new Vector4(1,0,0,0),u2=new Vector4(distance+item.Item2,item.Item3,0,0),c=Color.white});
                Append(poly,vertices,inds);distance+=length;
            }
            CreatePart(root,"Native painted rim",MeshOf(vertices,inds),edgeMaterial,3,folder+"/"+plan.id+"_Rim.asset");
            point.entrance.transform.position=plan.At(.83f,0);
            point.arrival.transform.position=new Vector3(plan.wall-plan.Sign*1.8f,plan.floor+.66f,0);
            if(point.arrival is MraArrival arrival)arrival.facing=plan.right?-1:1;
            EditorUtility.SetDirty(point.entrance);EditorUtility.SetDirty(point.arrival);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return new {changedMeshes,changedEdges,replacements,plans=plans.Select(p=>new{p.id,p.right,p.wall,p.floor,p.rise,outline=p.outline.Select(v=>new[]{v.x,v.y}).ToArray()})};
    }
    // Restore only the old opening's intersection with the original gallery geometry.
    // Other openings in a shared mesh or collision loop remain intact.
    public static object RestoreOpening(string folder,string id,string receiptPath)
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit required");
        string rootName="Restored original divider | "+id;
        if(GameObject.Find(rootName)!=null)throw new Exception("Opening already restored");
        var receipt=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(receiptPath));
        var plan=receipt["plans"].Single(p=>(string)p["id"]==id);
        var hole=plan["outline"].Select(v=>new Vector2((float)v[0],(float)v[1])).ToArray();
        var bounds=BoundsOf(hole);System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var root=new GameObject(rootName);var restored=new List<object>();
        Func<Mesh,Transform,Vertex[]> read=(mesh,t)=>{
            var pos=mesh.vertices;var col=mesh.colors;var uv=new List<Vector4>[3];for(int c=0;c<3;c++){uv[c]=new List<Vector4>();mesh.GetUVs(c,uv[c]);}
            return pos.Select((v,i)=>new Vertex{p=t.TransformPoint(v),c=col.Length>i?col[i]:Color.white,u0=uv[0].Count>i?uv[0][i]:Vector4.zero,u1=uv[1].Count>i?uv[1][i]:Vector4.zero,u2=uv[2].Count>i?uv[2][i]:Vector4.zero}).ToArray();
        };
        foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
            var renderer=f.GetComponent<Renderer>();if(renderer==null||!renderer.enabled||!renderer.bounds.Intersects(bounds))continue;
            var entry=receipt["replacements"].FirstOrDefault(r=>(string)r["objectName"]==f.name);if(entry==null)continue;
            var original=AssetDatabase.LoadAssetAtPath<Mesh>((string)entry["source"]);if(original==null)throw new Exception("Missing original "+f.name);
            var vv=read(original,f.transform);var ti=original.triangles;var patches=new List<List<Vertex>>();
            for(int i=0;i<ti.Length;i+=3){var poly=new List<Vertex>{vv[ti[i]],vv[ti[i+1]],vv[ti[i+2]]};for(int h=0;h<hole.Length&&Area(poly);h++)poly=Half(poly,hole[h],hole[(h+1)%hole.Length],true);if(Area(poly))patches.Add(poly);}
            if(patches.Count==0)continue;
            var vertices=read(f.sharedMesh,f.transform).ToList();var indices=f.sharedMesh.triangles.ToList();foreach(var poly in patches)Append(poly,vertices,indices);
            var restoredMesh=MeshOf(vertices,indices,f.transform);string path=folder+"/Restored_"+restored.Count.ToString("D2")+".asset";AssetDatabase.CreateAsset(restoredMesh,path);f.sharedMesh=restoredMesh;EditorUtility.SetDirty(f);restored.Add(new{f.name,source=(string)entry["source"],path,patches=patches.Count});
        }
        int edges=0;
        foreach(var e in UnityEngine.Object.FindObjectsByType<EdgeCollider2D>(FindObjectsSortMode.None).Where(e=>!e.enabled&&e.name.StartsWith("Continuous_Collision_")).ToArray()){
            var pts=e.points.Select(q=>(Vector2)e.transform.TransformPoint(q+e.offset)).ToArray();var paths=new List<List<Vector2>>();List<Vector2> current=null;
            for(int i=0;i<pts.Length-1;i++)if(Interval(pts[i],pts[i+1],hole,out float lo,out float hi)){
                var a=Vector2.Lerp(pts[i],pts[i+1],lo);var b=Vector2.Lerp(pts[i],pts[i+1],hi);if(current==null||Vector2.Distance(current.Last(),a)>.001f){current=new List<Vector2>{a};paths.Add(current);}current.Add(b);
            }
            foreach(var path in paths){var go=new GameObject(e.name+" | restored original segment");go.transform.SetParent(root.transform,false);go.layer=e.gameObject.layer;var c=go.AddComponent<EdgeCollider2D>();c.points=path.ToArray();c.edgeRadius=e.edgeRadius;c.sharedMaterial=e.sharedMaterial;edges++;}
        }
        var point=UnityEngine.Object.FindObjectsByType<CaveConnectionPoint>(FindObjectsSortMode.None).Single(p=>p.lowerRoomId==id);
        foreach(var t in point.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Embedded return | "+id)).ToArray()){t.gameObject.SetActive(false);t.name="Retired "+t.name;}
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        return new{restoredMeshes=restored,restoredColliderSegments=edges,id};
    }
    static void CreatePart(GameObject parent,string name,Mesh mesh,Material material,int order,string path)
    {
        AssetDatabase.CreateAsset(mesh,path);var go=new GameObject(name);go.transform.SetParent(parent.transform,true);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=order;
    }
}



