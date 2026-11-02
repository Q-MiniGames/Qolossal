using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Opt-in convex apertures in terrain-local coordinates. The same polygons cut every painted
// terrain layer and the solid columns, so a visible passage is also physically open.
public sealed partial class MraTerrain
{
    [Serializable] public sealed class CaveOpening
    {
        public string roomId;
        [Tooltip("Convex, counter-clockwise outline, in this terrain's local coordinates.")]
        public Vector2[] outline = Array.Empty<Vector2>();
    }
    public CaveOpening[] caveOpenings = Array.Empty<CaveOpening>();
    [Tooltip("Rendering-only trims for old decorative wall fringes at fitted cave mouths. Does not affect collision.")] public CaveOpening[] caveFringeTrims = Array.Empty<CaveOpening>();
    IEnumerable<CaveOpening> PaintedCaves(bool fringe) { foreach(var h in caveOpenings ?? Array.Empty<CaveOpening>()) yield return h; if(fringe) foreach(var h in caveFringeTrims ?? Array.Empty<CaveOpening>()) yield return h; }
    struct CaveVertex
    {
        public Vector3 p; public Vector2 uv; public Color c;
        public static CaveVertex Mix(CaveVertex a, CaveVertex b, float t) => new CaveVertex
        { p = Vector3.LerpUnclamped(a.p,b.p,t), uv = Vector2.LerpUnclamped(a.uv,b.uv,t), c = Color.LerpUnclamped(a.c,b.c,t) };
    }
    static float Side(Vector2 a, Vector2 b, Vector3 p) => (b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
    static List<CaveVertex> HalfPlane(List<CaveVertex> input, Vector2 a, Vector2 b, bool inside)
    {
        var result = new List<CaveVertex>(); if (input.Count == 0) return result;
        var previous = input[input.Count-1]; float d0 = Side(a,b,previous.p);
        bool was = inside ? d0 >= 0 : d0 <= 0;
        foreach (var current in input)
        {
            float d1 = Side(a,b,current.p); bool now = inside ? d1 >= 0 : d1 <= 0;
            if (now != was) result.Add(CaveVertex.Mix(previous,current,d0/(d0-d1)));
            if (now) result.Add(current);
            previous=current;d0=d1;was=now;
        }
        return result;
    }
    static bool HasArea(List<CaveVertex> p)
    {
        if (p.Count < 3) return false;
        double area=0; for(int i=0;i<p.Count;i++){var a=p[i].p;var b=p[(i+1)%p.Count].p;area+=a.x*(double)b.y-b.x*(double)a.y;}
        return Math.Abs(area) > .000001;
    }
    List<List<CaveVertex>> OutsideCaves(List<CaveVertex> polygon, bool trimFringe = false)
    {
        var pieces = new List<List<CaveVertex>> {polygon};
        foreach(var hole in PaintedCaves(trimFringe))
        {
            if(hole == null || hole.outline == null || hole.outline.Length<3) continue;
            var next = new List<List<CaveVertex>>();
            foreach(var piece in pieces)
            {
                // Entirely outside one supporting edge: no clipping or extra vertices needed.
                bool disjoint=false;
                for(int e=0;e<hole.outline.Length&&!disjoint;e++)
                {
                    bool all=true;foreach(var v in piece)if(Side(hole.outline[e],hole.outline[(e+1)%hole.outline.Length],v.p)>0){all=false;break;}
                    disjoint=all;
                }
                if(disjoint){next.Add(piece);continue;}
                var remainder=piece;
                for(int e=0;e<hole.outline.Length && HasArea(remainder);e++)
                {
                    var a=hole.outline[e];var b=hole.outline[(e+1)%hole.outline.Length];
                    var outside=HalfPlane(remainder,a,b,false);if(HasArea(outside))next.Add(outside);
                    remainder=HalfPlane(remainder,a,b,true);
                }
            }
            pieces=next;
        }
        return pieces;
    }
    void ClipCaveMesh(Mesh mesh, bool trimFringe = false)
    {
        if(mesh.vertexCount==0 || ((caveOpenings == null || caveOpenings.Length==0) && (!trimFringe || caveFringeTrims==null || caveFringeTrims.Length==0)))return;
        var v=mesh.vertices;var u=mesh.uv;var c=mesh.colors;var tris=mesh.triangles;
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var indices=new List<int>();
        for(int t=0;t<tris.Length;t+=3)
        {
            var triangle=new List<CaveVertex>();
            for(int k=0;k<3;k++){int n=tris[t+k];triangle.Add(new CaveVertex{p=v[n],uv=n<u.Length?u[n]:Vector2.zero,c=n<c.Length?c[n]:Color.white});}
            foreach(var poly in OutsideCaves(triangle, trimFringe))
            {
                int first=vertices.Count;foreach(var p in poly){vertices.Add(p.p);uv.Add(p.uv);colors.Add(p.c);}
                for(int k=1;k<poly.Count-1;k++){indices.Add(first);indices.Add(first+k);indices.Add(first+k+1);}
            }
        }
        mesh.Clear();mesh.indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16;
        mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
    }
    bool BuildCaveCollision(PolygonCollider2D collider)
    {
        if(caveOpenings==null || caveOpenings.Length==0)return false;
        var paths=new List<Vector2[]>();
        // Each ground column is convex even when the complete route has steps and valleys.
        for(int i=0;i<surface.Length-1;i++)
        {
            Vector2 a=surface[i],b=surface[i+1];if(b.x-a.x<.00001f)continue;
            var column=new List<CaveVertex>{new CaveVertex{p=new Vector3(a.x,bottom)},new CaveVertex{p=new Vector3(b.x,bottom)},new CaveVertex{p=b},new CaveVertex{p=a}};
            foreach(var poly in OutsideCaves(column))if(HasArea(poly)){var path=new Vector2[poly.Count];for(int j=0;j<path.Length;j++)path[j]=poly[j].p;paths.Add(path);}
        }
        collider.pathCount=paths.Count;for(int i=0;i<paths.Count;i++)collider.SetPath(i,paths[i]);
        return true;
    }
}

