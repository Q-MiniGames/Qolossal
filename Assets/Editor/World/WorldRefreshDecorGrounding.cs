using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WorldRefreshDecorGrounding
{
    sealed class OriginalTransform {public long parent;public Matrix4x4 local;}
    static string Hier(Transform t){var p=new List<string>();while(t!=null){p.Add(t.name);t=t.parent;}p.Reverse();return string.Join("/",p);}
    static IEnumerable<T> All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true));
    static float Y(Vector2[] p,float x){for(int i=0;i<p.Length-1;i++)if(p[i+1].x>p[i].x+.0001f&&x>=p[i].x-.00001f&&x<=p[i+1].x+.00001f)return Mathf.Lerp(p[i].y,p[i+1].y,(x-p[i].x)/(p[i+1].x-p[i].x));return x<p[0].x?p[0].y:p.Last().y;}
    static float F(string text)=>float.Parse(text,CultureInfo.InvariantCulture);
    static Dictionary<long,OriginalTransform> ReadOriginal(string file)
    {
        var result=new Dictionary<long,OriginalTransform>();string text=File.ReadAllText(file);
        foreach(Match block in Regex.Matches(text,@"(?ms)^--- !u!4 &(\d+)\r?\n(.*?)(?=^--- !u!|\z)"))
        {
            string b=block.Groups[2].Value;Vector3 Vec(string field){var m=Regex.Match(b,field+@": \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}");if(!m.Success)throw new Exception("Missing original transform "+field);return new Vector3(F(m.Groups[1].Value),F(m.Groups[2].Value),F(m.Groups[3].Value));}
            var q=Regex.Match(b,@"m_LocalRotation: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}");var parent=Regex.Match(b,@"m_Father: \{fileID: (\d+)\}");
            result.Add(long.Parse(block.Groups[1].Value),new OriginalTransform{parent=long.Parse(parent.Groups[1].Value),local=Matrix4x4.TRS(Vec("m_LocalPosition"),new Quaternion(F(q.Groups[1].Value),F(q.Groups[2].Value),F(q.Groups[3].Value),F(q.Groups[4].Value)),Vec("m_LocalScale"))});
        }
        return result;
    }
    static Bounds OriginalBounds(SpriteRenderer sr,Matrix4x4 matrix)
    {
        var b=sr.sprite.bounds;var result=new Bounds();bool first=true;
        foreach(float x in new[]{b.min.x,b.max.x})foreach(float y in new[]{b.min.y,b.max.y})
        {var p=matrix.MultiplyPoint3x4(new Vector3(sr.flipX?-x:x,sr.flipY?-y:y,0));if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);}
        return result;
    }
    public static string ApplyCurrent()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Save unrelated changes first");
        string id=scene.name.Substring(8,4),evidence=WorldRefreshSurface.Evidence;var report=JObject.Parse(File.ReadAllText(evidence+"/"+id+"_applied.json"));
        var original=ReadOriginal(evidence+"/Before/"+scene.name+".unity");var matrices=new Dictionary<long,Matrix4x4>();
        Matrix4x4 Matrix(long key){if(key==0)return Matrix4x4.identity;if(matrices.TryGetValue(key,out var m))return m;if(!original.TryGetValue(key,out var t))throw new Exception("Original transform missing "+key);m=Matrix(t.parent)*t.local;matrices[key]=m;return m;}
        var terrains=All<MraTerrain>(scene).ToDictionary(t=>Hier(t.transform));var changed=report["terrains"].Where(t=>t["changedSpans"].Any()).ToArray();var records=new List<object>();int candidates=0,moved=0,alreadyAligned=0;
        string CollisionState()=>string.Join("\n",All<Collider2D>(scene).Select(c=>Hier(c.transform)+EditorJsonUtility.ToJson(c)));string beforeCollision=CollisionState();
        foreach(var sr in All<SpriteRenderer>(scene).Where(s=>s.gameObject.activeInHierarchy&&s.enabled&&s.sprite!=null&&s.GetComponentInParent<MraTerrain>()==null&&s.GetComponentInParent<Collider2D>()==null))
        {
            string hierarchy=Hier(sr.transform);bool hanging=sr.name.StartsWith("Hanging ",StringComparison.Ordinal);bool decor=sr.name.Contains("Decor_")||hierarchy.Contains("/Decor/")||hierarchy.Contains("/Cliff dressing/");if(!hanging&&!decor)continue;
            if(sr.GetComponentsInParent<MonoBehaviour>().Any(c=>c!=null)||sr.GetComponentsInChildren<Collider2D>(true).Any())continue;
            long key=(long)GlobalObjectId.GetGlobalObjectIdSlow(sr.transform).targetObjectId;if(!original.ContainsKey(key))continue;var oldMatrix=Matrix(key);var oldPosition=oldMatrix.MultiplyPoint3x4(Vector3.zero);var oldBounds=OriginalBounds(sr,oldMatrix);
            float bestScore=float.MaxValue,delta=0,oldAttach=0,newAttach=0;string terrainPath=null;bool inChangedSpan=false;
            foreach(var item in changed)
            {
                string path=(string)item["path"];if(!terrains.TryGetValue(path,out var t))continue;float x=t.transform.InverseTransformPoint(oldPosition).x;
                if(!item["changedSpans"].Any(s=>x>=(float)s["a"]-.3f&&x<=(float)s["b"]+.3f))continue;inChangedSpan=true;
                var points=item["before"].Select(p=>new Vector2((float)p[0],(float)p[1])).ToArray();float oldY=0,score=float.MaxValue;
                if(hanging)
                {
                    // Original authoring anchors the top .06 u below a riser lip,
                    // with x offset .12 u toward the open (lower) side of the cliff.
                    for(int k=0;k<points.Length-1;k++)if(Mathf.Abs(points[k+1].x-points[k].x)<.0001f&&Mathf.Abs(points[k].x-x)<=.5f)
                    {float top=t.transform.TransformPoint(new Vector3(points[k].x,Mathf.Max(points[k].y,points[k+1].y),0)).y;float error=Mathf.Abs(oldBounds.max.y-(top-.06f));if(error<score){score=error;oldY=top;}}
                }
                else{oldY=t.transform.TransformPoint(new Vector3(x,Y(points,x),0)).y;score=Mathf.Abs(oldBounds.min.y-oldY);}
                if(score>(hanging?.4f:.65f)||score>=bestScore)continue;
                float newY=t.transform.TransformPoint(new Vector3(x,Y(t.surface,x),0)).y;bestScore=score;delta=newY-oldY;oldAttach=oldY;newAttach=newY;terrainPath=path;
            }
            if(!inChangedSpan)continue;candidates++;
            if(terrainPath==null){records.Add(new{hierarchy,transformFileId=key,hanging,disposition="Preserved: no original ground/lip attachment match; may be intentional scenery"});continue;}
            Vector3 target=oldPosition+Vector3.up*delta;float correction=target.y-sr.transform.position.y;
            if(Mathf.Abs(correction)>.001f){var p=sr.transform.position;p.y=target.y;sr.transform.position=p;EditorUtility.SetDirty(sr.transform);moved++;}else alreadyAligned++;
            records.Add(new{hierarchy,transformFileId=key,hanging,terrain=terrainPath,originalAttachment=oldAttach,newAttachment=newAttach,groundDelta=delta,appliedCorrection=correction,originalY=oldPosition.y,finalY=sr.transform.position.y,disposition=hanging?"Top follows smoothed terrain; original hanging orientation and inset retained":"Grounded decoration follows smoothed terrain"});
        }
        bool collisionPreserved=beforeCollision==CollisionState();if(!collisionPreserved)throw new Exception("Unexpected collision mutation");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        string output=evidence+"/"+id+"_decor_grounding.json";File.WriteAllText(output,JsonConvert.SerializeObject(new{scene=scene.name,candidates,moved,alreadyAligned,collisionPreserved,records},Formatting.Indented));return JsonConvert.SerializeObject(new{scene=scene.name,candidates,moved,alreadyAligned,collisionPreserved,output});
    }
}
