using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class MraTerrain
{
    public enum RegisteredTopJoinRole { None, FlatEnd, RisingStart }
    [Tooltip("Reviewed render-only ownership and strip registration at one paired flat-to-slope join. Off by default.")]
    public RegisteredTopJoinRole registeredTopJoinRole;
    public MraTerrain registeredTopJoinPeer;
    [Tooltip("Pinned accepted strip identity for this reviewed join. Changing the source disables the rule until reviewed again.")]
    public Sprite registeredTopJoinSource;

    [NonSerialized] MraTerrain registeredTopJoinLastPeer;

    public void NotifyRegisteredTopJoinConfiguration()
    {
        if (registeredTopJoinRole == RegisteredTopJoinRole.None && registeredTopJoinPeer == null && registeredTopJoinLastPeer == null) return;
        // Rebuild deliberately leaves LastPeer alone so removal/replacement can refresh the old side.
        if (isActiveAndEnabled) Rebuild();
        RefreshRegisteredTopJoinPeer();
    }

    void RefreshRegisteredTopJoinPeer()
    {
        // Notifications occur only at lifecycle/configuration boundaries, never inside Rebuild.
        var previous = registeredTopJoinLastPeer;
        var current = registeredTopJoinPeer;
        registeredTopJoinLastPeer = current;
        void Refresh(MraTerrain peer)
        {
            if (peer != null && peer != this && peer.isActiveAndEnabled && peer.registeredTopJoinPeer == this && peer.gameObject.scene == gameObject.scene)
                peer.Rebuild();
        }
        if (previous != current) Refresh(previous);
        if (registeredTopJoinRole != RegisteredTopJoinRole.None || current != null) Refresh(current);
    }

#if UNITY_EDITOR
    [NonSerialized] int registeredTopJoinValidateQueued;
    void OnValidate()
    {
        if (registeredTopJoinRole == RegisteredTopJoinRole.None && ReferenceEquals(registeredTopJoinPeer, null) && ReferenceEquals(registeredTopJoinLastPeer, null)) return;
        if (System.Threading.Interlocked.Exchange(ref registeredTopJoinValidateQueued, 1) != 0) return;
        // OnValidate may run off the main thread. Only queue managed work here.
        // update also advances in unfocused Editor sessions where inspector delayCall may stall.
        UnityEditor.EditorApplication.update += ProcessRegisteredTopJoinValidation;
    }

    void ProcessRegisteredTopJoinValidation()
    {
        if (this == null)
        {
            UnityEditor.EditorApplication.update -= ProcessRegisteredTopJoinValidation;
            System.Threading.Interlocked.Exchange(ref registeredTopJoinValidateQueued, 0);
            return;
        }
        // Retain the one pending notification through busy states; do not silently discard it.
        if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
        // Unsubscribe before touching objects, so a new validation can schedule a later update.
        UnityEditor.EditorApplication.update -= ProcessRegisteredTopJoinValidation;
        System.Threading.Interlocked.Exchange(ref registeredTopJoinValidateQueued, 0);
        NotifyRegisteredTopJoinConfiguration();
    }

    void OnDestroy()
    {
        if (System.Threading.Interlocked.Exchange(ref registeredTopJoinValidateQueued, 0) != 0)
            UnityEditor.EditorApplication.update -= ProcessRegisteredTopJoinValidation;
    }
#endif

    static void ReleaseJoinMesh(Mesh mesh)
    {
        if (mesh == null) return;
        if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
    }

    bool ResolveRegisteredTopJoin(out MraTerrain flat, out MraTerrain slope)
    {
        flat = slope = null;
        if (registeredTopJoinRole == RegisteredTopJoinRole.None || registeredTopJoinPeer == null || registeredTopJoinPeer == this) return false;
        flat = registeredTopJoinRole == RegisteredTopJoinRole.FlatEnd ? this : registeredTopJoinPeer;
        slope = registeredTopJoinRole == RegisteredTopJoinRole.RisingStart ? this : registeredTopJoinPeer;
        if (flat.registeredTopJoinRole != RegisteredTopJoinRole.FlatEnd || slope.registeredTopJoinRole != RegisteredTopJoinRole.RisingStart || flat.registeredTopJoinPeer != slope || slope.registeredTopJoinPeer != flat) return false;
        if (!flat.isActiveAndEnabled || !slope.isActiveAndEnabled || flat.gameObject.scene != slope.gameObject.scene || flat.transform.localToWorldMatrix != slope.transform.localToWorldMatrix) return false;
        if (flat.chamberBackdrop != null || slope.chamberBackdrop != null || flat.top == null || flat.top != slope.top || flat.registeredTopJoinSource != flat.top || slope.registeredTopJoinSource != flat.top) return false;
        if (!flat.HasStepSet || !slope.HasStepSet || !flat.stripDerivedRuns || !slope.stripDerivedRuns || flat.tint != slope.tint || flat.walkLinePx != slope.walkLinePx || flat.stripDepth != slope.stripDepth || flat.lipOverhang != slope.lipOverhang || flat.shortNaturalSoilTail || slope.shortNaturalSoilTail) return false;
        if (flat.surface == null || flat.surface.Length != 2 || slope.surface == null || slope.surface.Length < 3) return false;
        Vector2 j = flat.surface[1], end = slope.surface[1];
        if (flat.surface[0].x >= j.x || flat.surface[0].y != j.y || slope.surface[0] != j || end.x <= j.x || end.y <= j.y || slope.surface[2].x != end.x) return false;
        float above = flat.walkLinePx / StepPpu, below = Mathf.Min(flat.WorldSize(flat.top).y - above, flat.stripDepth);
        if (slope.surface[2].y <= end.y + above + below || (end.y-j.y)/(end.x-j.x) > slope.maxStripSlope || below <= 0 || above <= 0) return false;
        // The paired join must be the only top geometry intersecting this local-x boundary.
        for (int i=2; i<slope.surface.Length; i++) if (slope.surface[i].x <= j.x) return false;
        return true;
    }

    Mesh ApplyRegisteredTopJoin(Mesh ownRaw)
    {
        if (!ResolveRegisteredTopJoin(out var flat, out var slope)) return ownRaw;
        Mesh peerRaw = null; Mesh[] result = null;
        try
        {
            // Build the peer's RAW mathematical strip, never read its existing renderer or call Rebuild.
            // This makes load/rebuild order irrelevant and avoids recursively applying the join.
            registeredTopJoinPeer.risersCache = null;
            registeredTopJoinPeer.seamXs = null;
            peerRaw = registeredTopJoinPeer.TopMesh();
            var raw = this == flat ? new[] { ownRaw, peerRaw } : new[] { peerRaw, ownRaw };
            result = BuildRegisteredTopPair(flat, slope, raw);
            int take = this == flat ? 0 : 1;
            Mesh chosen = result[take]; result[take] = null;
            ReleaseJoinMesh(ownRaw);
            return chosen;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Registered terrain top join kept its original mapping: " + ex.Message, this);
            return ownRaw;
        }
        finally
        {
            ReleaseJoinMesh(peerRaw);
            if (result != null) foreach (var mesh in result) ReleaseJoinMesh(mesh);
        }
    }

    static Mesh[] BuildRegisteredTopPair(MraTerrain flat, MraTerrain slope, Mesh[] oldMeshes)
    {
        var results = new Mesh[2];
        var created = new List<Mesh>();
        try
        {
            float join=flat.surface[1].x, end=slope.surface[1].x, walk=flat.surface[1].y;
            float rise=slope.surface[1].y-walk, run=end-join, lip=flat.lipOverhang;
            float renderedStart=join-lip, renderedRun=run+lip, desiredSlope=rise/run;
            float above=flat.walkLinePx/StepPpu, maxFirstY=slope.surface[1].y+above+.0001f;
            float vWalk=1f-above/flat.WorldSize(flat.top).y;
            var ps=oldMeshes.Select(m=>m.vertices).ToArray();var us=oldMeshes.Select(m=>m.uv).ToArray();var cs=oldMeshes.Select(m=>m.colors).ToArray();var ts=oldMeshes.Select(m=>m.triangles).ToArray();
            if(cs.Where((c,i)=>c.Length!=ps[i].Length).Any())throw new Exception("Missing vertex-alpha data");
            double Area(Vector3 a,Vector3 b,Vector3 c)=>Math.Abs(((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x))*.5;
 float SeamU(int which){var values=new System.Collections.Generic.List<float>();for(int k=0;k<ts[which].Length;k+=3)for(int e=0;e<3;e++){int a=ts[which][k+e],b=ts[which][k+(e+1)%3];if(System.Math.Min(ps[which][a].x,ps[which][b].x)<=join&&System.Math.Max(ps[which][a].x,ps[which][b].x)>=join&&ps[which][a].x!=ps[which][b].x){float f=(join-ps[which][a].x)/(ps[which][b].x-ps[which][a].x);values.Add(UnityEngine.Mathf.LerpUnclamped(us[which][a].x,us[which][b].x,f));}}if(values.Count==0||values.Max()-values.Min()>2e-5f)throw new System.Exception("Ambiguous strip phase");return values.Average();}
 float flatU=SeamU(0),slopeU=SeamU(1),deltaU=flatU-slopeU,sizeY=flat.WorldSize(flat.top).y;
 // Common seam alpha is evaluated from both existing meshes, never from invented source opacity.
 // Endpoint ownership is geometric; this narrow seam profile prevents mismatched soil fades.
 var seamYs=new System.Collections.Generic.List<float>();
 for(int n=0;n<2;n++)for(int k=0;k<ts[n].Length;k+=3)for(int e=0;e<3;e++){
  var a=ps[n][ts[n][k+e]];var b=ps[n][ts[n][k+(e+1)%3]];
  if(a.x==join)seamYs.Add(a.y);
  if(System.Math.Min(a.x,b.x)<join&&System.Math.Max(a.x,b.x)>join)seamYs.Add(UnityEngine.Mathf.LerpUnclamped(a.y,b.y,(join-a.x)/(b.x-a.x)));
 }
 seamYs=seamYs.OrderBy(y=>y).Aggregate(new System.Collections.Generic.List<float>(),(list,y)=>{if(list.Count==0||y-list[list.Count-1]>1e-5f)list.Add(y);return list;});
 float OriginalAlpha(int n,float y){float result=0;for(int k=0;k<ts[n].Length;k+=3){int ia=ts[n][k],ib=ts[n][k+1],ic=ts[n][k+2];var a=ps[n][ia];var b=ps[n][ib];var c=ps[n][ic];if(join<System.Math.Min(a.x,System.Math.Min(b.x,c.x))||join>System.Math.Max(a.x,System.Math.Max(b.x,c.x))||y<System.Math.Min(a.y,System.Math.Min(b.y,c.y))-1e-5f||y>System.Math.Max(a.y,System.Math.Max(b.y,c.y))+1e-5f)continue;
  double d=((double)b.y-c.y)*(a.x-c.x)+((double)c.x-b.x)*(a.y-c.y);if(System.Math.Abs(d)<1e-12)continue;
  double wa=(((double)b.y-c.y)*(join-c.x)+((double)c.x-b.x)*(y-c.y))/d,wb=(((double)c.y-a.y)*(join-c.x)+((double)a.x-c.x)*(y-c.y))/d,wc=1-wa-wb;
  if(wa>=-1e-4&&wb>=-1e-4&&wc>=-1e-4)result=UnityEngine.Mathf.Max(result,(float)(wa*cs[n][ia].a+wb*cs[n][ib].a+wc*cs[n][ic].a));
 }return UnityEngine.Mathf.Clamp01(result);}
 var seamAlpha=seamYs.Select(y=>UnityEngine.Mathf.Min(OriginalAlpha(0,y),OriginalAlpha(1,y))).ToArray();
 float CommonAlpha(float y){if(y<seamYs[0]-1e-5f||y>seamYs[seamYs.Count-1]+1e-5f)return 0;for(int k=1;k<seamYs.Count;k++)if(y<=seamYs[k])return UnityEngine.Mathf.Lerp(seamAlpha[k-1],seamAlpha[k],UnityEngine.Mathf.InverseLerp(seamYs[k-1],seamYs[k],y));return seamAlpha[seamAlpha.Length-1];}
 for(int n=0;n<2;n++){
  var vv=new System.Collections.Generic.List<UnityEngine.Vector3>();var uu=new System.Collections.Generic.List<UnityEngine.Vector2>();var cc=new System.Collections.Generic.List<UnityEngine.Color>();var tt=new System.Collections.Generic.List<int>();
  double totalBefore=0,totalAfter=0,maxAreaDelta=0,maxPositionResidual=0;int splitParents=0;
  (UnityEngine.Vector3 p,UnityEngine.Vector2 uv,UnityEngine.Color c,UnityEngine.Vector3 bary) Lerp((UnityEngine.Vector3 p,UnityEngine.Vector2 uv,UnityEngine.Color c,UnityEngine.Vector3 bary) a,(UnityEngine.Vector3 p,UnityEngine.Vector2 uv,UnityEngine.Color c,UnityEngine.Vector3 bary) b,float f)=>(UnityEngine.Vector3.LerpUnclamped(a.p,b.p,f),UnityEngine.Vector2.LerpUnclamped(a.uv,b.uv,f),UnityEngine.Color.LerpUnclamped(a.c,b.c,f),UnityEngine.Vector3.LerpUnclamped(a.bary,b.bary,f));
  for(int k=0;k<ts[n].Length;k+=3){
   var original=new System.Collections.Generic.List<(UnityEngine.Vector3 p,UnityEngine.Vector2 uv,UnityEngine.Color c,UnityEngine.Vector3 bary)>();
   for(int e=0;e<3;e++){int id=ts[n][k+e];original.Add((ps[n][id],us[n][id],cs[n][id],e==0?UnityEngine.Vector3.right:e==1?UnityEngine.Vector3.up:UnityEngine.Vector3.forward));}
   double before=Area(original[0].p,original[1].p,original[2].p),after=0;totalBefore+=before;
   bool firstSlope=n==1&&original.All(v=>v.p.x<=end+.0001f&&v.p.y<maxFirstY);
   bool cross=original.Any(v=>v.p.x<join)&&original.Any(v=>v.p.x>join);if(cross)splitParents++;
   var polygons=new System.Collections.Generic.List<(System.Collections.Generic.List<(UnityEngine.Vector3 p,UnityEngine.Vector2 uv,UnityEngine.Color c,UnityEngine.Vector3 bary)> poly,bool left)>();
   if(!cross)polygons.Add((original,original.Average(v=>v.p.x)<=join));
   else for(int side=0;side<2;side++){
    bool left=side==0;var poly=new System.Collections.Generic.List<(UnityEngine.Vector3 p,UnityEngine.Vector2 uv,UnityEngine.Color c,UnityEngine.Vector3 bary)>();
    for(int e=0;e<3;e++){var a=original[e];var b=original[(e+1)%3];bool ai=left?a.p.x<=join:a.p.x>=join,bi=left?b.p.x<=join:b.p.x>=join;if(ai)poly.Add(a);if(ai!=bi){var v=Lerp(a,b,(join-a.p.x)/(b.p.x-a.p.x));v.p.x=join;poly.Add(v);}}
    polygons.Add((poly,left));
   }
   foreach(var entry in polygons){var poly=entry.poly;bool active=n==0?entry.left:!firstSlope||!entry.left;
    // Add the same seam row breaks to both meshes without moving the plane surface.
    var expanded=new System.Collections.Generic.List<(UnityEngine.Vector3 p,UnityEngine.Vector2 uv,UnityEngine.Color c,UnityEngine.Vector3 bary)>();
    for(int e=0;e<poly.Count;e++){var a=poly[e];var b=poly[(e+1)%poly.Count];expanded.Add(a);if(a.p.x==join&&b.p.x==join&&System.Math.Abs(b.p.y-a.p.y)>1e-6f){var ys=seamYs.Where(y=>y>System.Math.Min(a.p.y,b.p.y)+1e-5f&&y<System.Math.Max(a.p.y,b.p.y)-1e-5f);foreach(float y in b.p.y>a.p.y?ys.OrderBy(v=>v):ys.OrderByDescending(v=>v)){var v=Lerp(a,b,(y-a.p.y)/(b.p.y-a.p.y));v.p.y=y;expanded.Add(v);}}}
    int start=expanded.FindIndex(v=>v.p.x!=join);if(start<0)start=0;expanded=expanded.Skip(start).Concat(expanded.Take(start)).ToList();
    for(int e=1;e<expanded.Count-1;e++){
     var tri=new[]{expanded[0],expanded[e],expanded[e+1]};double area=Area(tri[0].p,tri[1].p,tri[2].p);if(area<1e-12)continue;after+=area;
     foreach(var item in tri){var v=item;var predicted=original[0].p*v.bary.x+original[1].p*v.bary.y+original[2].p*v.bary.z;maxPositionResidual=System.Math.Max(maxPositionResidual,(predicted-v.p).magnitude);
      if(v.bary.x<-.0001f||v.bary.y<-.0001f||v.bary.z<-.0001f)throw new System.Exception("Subdivision escaped original triangle");
      if(firstSlope){float renderedWalk=walk+(v.p.x-renderedStart)*(rise/renderedRun),desiredWalk=walk+(v.p.x-join)*desiredSlope;v.uv.x+=deltaU*(end-v.p.x)/(end-join);v.uv.y+=(renderedWalk-desiredWalk)/sizeY;}
      if(!active)v.c.a=0;else if(v.p.x==join&&(n==0||firstSlope)){v.uv=new UnityEngine.Vector2(flatU,vWalk+(v.p.y-walk)/sizeY);v.c.a=CommonAlpha(v.p.y);}
      tt.Add(vv.Count);vv.Add(v.p);uu.Add(v.uv);cc.Add(v.c);
     }
    }
   }
   double delta=System.Math.Abs(before-after);maxAreaDelta=System.Math.Max(maxAreaDelta,delta);totalAfter+=after;
   if(delta>System.Math.Max(2e-5,before*2e-4))throw new System.Exception("Original triangle coverage changed: "+k/3+" delta="+delta);
  }
  if(maxPositionResidual>6e-5)throw new System.Exception("Subdivision position residual beyond float allowance");
  var mesh=new UnityEngine.Mesh{name=oldMeshes[n].name,hideFlags=UnityEngine.HideFlags.HideAndDontSave,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};created.Add(mesh);mesh.SetVertices(vv);mesh.SetUVs(0,uu);mesh.SetColors(cc);mesh.SetTriangles(tt,0);mesh.RecalculateBounds();
  if((mesh.bounds.min-oldMeshes[n].bounds.min).magnitude>6e-5||(mesh.bounds.max-oldMeshes[n].bounds.max).magnitude>6e-5)throw new System.Exception("Surface bounds changed");mesh.bounds=oldMeshes[n].bounds;
  results[n]=mesh;
 }
            return results;
        }
        catch
        {
            foreach (var mesh in created) ReleaseJoinMesh(mesh);
            throw;
        }
    }
}
