using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Qolossal.Atlas
{
    [Serializable] public sealed class AtlasData
    {
        public float width, height;
        public AtlasNode[] nodes;
        public AtlasEdge[] edges;
        public AtlasCave[] caves;
        public AtlasRegion[] regions;
        public AtlasMapping[] mappings;
        public AtlasSurfaceBand[] surfaceBands;
        public AtlasContour[] contours;
        public AtlasDoor[] caveDoors;
        public bool sceneDerived;
        public bool illustratedMap;
    }
    [Serializable] public sealed class AtlasContour { public string region, room, mappingId, kind; public bool underground, primary; public Vector2[] path; }
    [Serializable] public sealed class AtlasDoor { public string room, to, link; public float x, y, w, h; }
    [Serializable] public sealed class AtlasSurfaceBand { public string region; public bool connector; public Vector2[] path; }
    [Serializable] public sealed class AtlasNode
    {
        public string id, name, type, region, group, scene, mappingId;
        public Vector2 point;
        public float[] mapBounds, worldBounds;
        public Rect Bounds => new Rect(mapBounds[0], mapBounds[1], mapBounds[2], mapBounds[3]);
    }
    [Serializable] public sealed class AtlasEdge { public string id, from, to, kind, requirements; public Vector2[] path; public AtlasTunnelStroke[] strokes; }
    [Serializable] public sealed class AtlasTunnelStroke { public Vector2[] path; }
    [Serializable] public sealed class AtlasCave { public string connectionId, lowerRoomId, upperAreaId, upperRegion, lowerRoomName, requirements; public Vector2 upperPoint, lowerPoint; }
    [Serializable] public sealed class AtlasRegion { public string id, name, color; }
    [Serializable] public sealed class AtlasMapping { public string id; public float sx, sy, ox, oy; }

    // One batched vector graphic; outlines remain crisp at every zoom and carry no baked labels.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AtlasGraphic : MaskableGraphic, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public ConnectedAtlas owner;
        bool dragged;
        static readonly Color Gold = new Color32(224, 193, 130, 255);
        public void OnPointerClick(PointerEventData e) { if (!dragged) owner.ClickScreen(e.position); }
        public void OnBeginDrag(PointerEventData e) { dragged = true; }
        public void OnDrag(PointerEventData e) { owner.DragScreen(e.delta); }
        public void OnEndDrag(PointerEventData e) { dragged = true; }
        public void OnScroll(PointerEventData e) { owner.ZoomAt(Mathf.Pow(1.16f, e.scrollDelta.y), e.position); }
        public override bool Raycast(Vector2 p, Camera c) { return base.Raycast(p, c); }
        // EventSystem sends begin-drag only after its pixel threshold. Reset for every new press.
        public void ResetDrag() { dragged = false; }
        void LateUpdate() { if (UnityEngine.InputSystem.Mouse.current?.leftButton.wasPressedThisFrame == true) dragged = false; }
        Vector2 P(Vector2 p) => owner.Project(p);
        bool Visible(Vector2 a, Vector2 b) => new Rect(Mathf.Min(a.x,b.x)-5, Mathf.Min(a.y,b.y)-5, Mathf.Abs(a.x-b.x)+10, Mathf.Abs(a.y-b.y)+10).Overlaps(rectTransform.rect);
        void Quad(VertexHelper v, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int n=v.currentVertCount; v.AddVert(a,color,Vector2.zero);v.AddVert(b,color,Vector2.zero);v.AddVert(c,color,Vector2.zero);v.AddVert(d,color,Vector2.zero);v.AddTriangle(n,n+1,n+2);v.AddTriangle(n,n+2,n+3);
        }
        void Stroke(VertexHelper v, Vector2 a, Vector2 b, float width, Color color)
        {
            if (!Visible(a,b) || (b-a).sqrMagnitude < .001f) return;
            var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;Quad(v,a+n,b+n,b-n,a-n,color);
        }
        void Path(VertexHelper v, Vector2[] pts, float width, Color color, bool dashed=false)
        {
            for(int i=1;i<pts.Length;i++){
                var a=P(pts[i-1]);var b=P(pts[i]);if(!Visible(a,b))continue;
                if(!dashed)Stroke(v,a,b,width,color);
                else {float len=Vector2.Distance(a,b);int steps=Mathf.CeilToInt(len/13);for(int j=0;j<steps;j++)Stroke(v,Vector2.Lerp(a,b,j*13/len),Vector2.Lerp(a,b,Mathf.Min(1,(j*13+6)/len)),width,color);}
            }
        }
        void Room(VertexHelper v,AtlasNode n)
        {
            if(n.type=="surface")return; // Surface entrances use cave symbols on the painting, not room boxes.
            var b=n.Bounds;Vector2 a=P(new Vector2(b.xMin,b.yMin)),z=P(new Vector2(b.xMax,b.yMax));
            if(!Visible(a,z))return;float cut=Mathf.Min(6,Mathf.Abs(z.y-a.y)*.22f,Mathf.Abs(z.x-a.x)*.12f);
            var p=new[]{new Vector2(a.x+cut,a.y),new Vector2(z.x-cut,a.y),new Vector2(z.x,a.y-cut),new Vector2(z.x,z.y+cut),new Vector2(z.x-cut,z.y),new Vector2(a.x+cut,z.y),new Vector2(a.x,z.y+cut),new Vector2(a.x,a.y-cut)};
            var col=owner.RegionColor(n.region);if(owner.SelectedId==n.id)col=Gold;
            int start=v.currentVertCount;var fill=col;fill.a=owner.SelectedId==n.id?.38f:.13f;
            foreach(var q in p)v.AddVert(q,fill,Vector2.zero);for(int i=1;i<7;i++)v.AddTriangle(start,start+i,start+i+1);
            for(int i=0;i<8;i++)Stroke(v,p[i],p[(i+1)%8],owner.SelectedId==n.id?2.8f:1.5f,col);
        }
        void Mouth(VertexHelper v,Vector2 p)
        {
            p=P(p);float r=owner.FocusedRegion==null?5:8;
            int centre=v.currentVertCount;v.AddVert(p,new Color(.02f,.055f,.06f,.95f),Vector2.zero);
            for(int i=0;i<16;i++){float angle=i*Mathf.PI*2/16;v.AddVert(p+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*r,new Color(.02f,.055f,.06f,.95f),Vector2.zero);}
            for(int i=0;i<16;i++)v.AddTriangle(centre,centre+1+i,centre+1+(i+1)%16);
            for(int i=0;i<16;i++){float a=i*Mathf.PI*2/16,b=(i+1)*Mathf.PI*2/16;Stroke(v,p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,p+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r,1.3f,Gold);}
            Stroke(v,p+new Vector2(-r*.4f,-r*.45f),p+new Vector2(-r*.4f,r*.25f),1.2f,Gold);Stroke(v,p+new Vector2(r*.4f,-r*.45f),p+new Vector2(r*.4f,r*.25f),1.2f,Gold);Stroke(v,p+new Vector2(-r*.4f,r*.25f),p+new Vector2(0,r*.6f),1.2f,Gold);Stroke(v,p+new Vector2(0,r*.6f),p+new Vector2(r*.4f,r*.25f),1.2f,Gold);
        }
        void Tunnel(VertexHelper v,AtlasEdge edge,Color tint)
        {
            foreach(var path in edge.strokes?.Select(s=>s.path)??new[]{edge.path})TunnelStroke(v,path,tint,edge.id);
        }
        void TunnelStroke(VertexHelper v,Vector2[] path,Color tint,string id)
        {
            var pts=path.Select(P).ToArray();if(pts.Length<2)return;
            float width=Mathf.Clamp(18/owner.ViewBounds.width*rectTransform.rect.width,2.5f,14);
            int seed=id.Sum(c=>(int)c);var left=new Vector2[pts.Length];var right=new Vector2[pts.Length];
            for(int i=0;i<pts.Length;i++){
                var dir=(pts[Mathf.Min(i+1,pts.Length-1)]-pts[Mathf.Max(0,i-1)]).normalized;var n=new Vector2(-dir.y,dir.x);
                float rough=1+.13f*Mathf.Sin(i*2.31f+seed)+.07f*Mathf.Sin(i*.83f+seed*.3f);
                left[i]=pts[i]+n*width*rough*.5f;right[i]=pts[i]-n*width*rough*.5f;
            }
            var rock=Color.Lerp(new Color(.30f,.27f,.19f,1),tint,.18f);var shadow=new Color(.012f,.023f,.023f,1);var inner=new Color(.035f,.064f,.061f,1);
            for(int i=1;i<pts.Length;i++){
                Stroke(v,pts[i-1],pts[i],width+5,shadow);
                if(!Visible(pts[i-1],pts[i]))continue;
                Quad(v,left[i-1],left[i],right[i],right[i-1],rock);
                Stroke(v,pts[i-1],pts[i],width*.64f,inner);
                Stroke(v,left[i-1],left[i],1,new Color(.60f,.53f,.34f,.75f));
                Stroke(v,right[i-1],right[i],1.5f,new Color(.06f,.08f,.06f,.9f));
            }
        }
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();if(owner?.Data==null)return;
            if(owner.Data.illustratedMap){
                var surface=owner.Data.contours.Where(c=>!c.underground&&c.primary).SelectMany(c=>c.path).OrderBy(p=>p.x).ToArray();
                var earth=new Color(.09f,.17f,.145f,1);
                for(int i=1;i<surface.Length;i++){var a=surface[i-1];var b=surface[i];Quad(v,P(a),P(b),P(new Vector2(b.x,owner.Data.height-65)),P(new Vector2(a.x,owner.Data.height-65)),earth);}
            }
            foreach(var e in owner.Data.edges){
                if(owner.Data.sceneDerived&&e.kind=="surface")continue;
                if(!owner.EdgeVisible(e))continue;var col=owner.RegionColor(owner.Nodes[e.from].region);
                if(owner.Data.illustratedMap&&e.kind=="cave"){if(owner.FocusedRegion!=null)Tunnel(v,e,col);continue;}
                if(owner.Data.illustratedMap&&e.kind=="passage")continue; // Actual corridor walls already show the route.
                if(e.kind=="cave"){col=Gold;col.a=owner.FocusedRegion==null?.4f:.65f;Path(v,e.path,1.2f,col,true);}
                else if(e.kind=="scene-transition"){Path(v,e.path,7,new Color(.03f,.06f,.065f,.95f));Path(v,e.path,1.6f,Gold);}
                else {Path(v,e.path,e.kind=="surface"?4:7,new Color(.03f,.06f,.065f,.9f));col.a=e.kind=="surface"?.65f:.9f;Path(v,e.path,e.kind=="surface"?1.6f:3,col);}
            }
            if(owner.Data.sceneDerived&&owner.Data.contours!=null){
                for(int index=0;index<owner.Data.contours.Length;index++){var c=owner.Data.contours[index];
                    if(owner.FocusedRegion!=null&&c.region!=owner.FocusedRegion)continue;
                    if(owner.Data.illustratedMap&&owner.FocusedRegion==null&&c.underground&&!string.IsNullOrEmpty(c.room))continue;
                    var col=owner.RegionColor(c.region);if(c.room==owner.SelectedId)col=Gold;
                    // Collapse subpixel contour segments at world zoom, retaining full slopes at close zoom.
                    var pts=c.path;if(pts==null||pts.Length<2)continue;var a=P(pts[0]);
                    var known=owner.ContourKnown(index);for(int i=1;i<pts.Length;i++){var b=P(pts[i]);if(known!=null&&!known[i]){a=b;continue;}if(i<pts.Length-1&&(b-a).sqrMagnitude<.64f)continue;Stroke(v,a,b,c.underground?1.4f:2f,col);a=b;}
                }
            }else foreach(var n in owner.Data.nodes)if(owner.NodeVisible(n)&&owner.HasRoomOutline(n))Room(v,n);
            foreach(var c in owner.Data.caves)if(owner.CaveVisible(c)){bool known=owner.RoomKnown(c.lowerRoomId);if(known||GameSave.HasFlag("mra:"+c.lowerRoomId+":discovered"))Mouth(v,c.upperPoint);if(known)Mouth(v,c.lowerPoint);}
            foreach(var leader in owner.LabelLeaders)Stroke(v,leader.from,leader.to,1,leader.color);
            if(owner.PlayerMapPoint(out var player)){var p=P(player);var c=Color.white;Stroke(v,p+Vector2.up*8,p+Vector2.right*6,2,c);Stroke(v,p+Vector2.right*6,p-Vector2.up*8,2,c);Stroke(v,p-Vector2.up*8,p-Vector2.right*6,2,c);Stroke(v,p-Vector2.right*6,p+Vector2.up*8,2,c);}
        }
    }
}
