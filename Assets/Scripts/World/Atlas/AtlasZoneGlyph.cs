using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Qolossal.Atlas
{
    // Label-free zone selector: a miniature of the zone's measured surface profile.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AtlasZoneGlyph : MaskableGraphic
    {
        public Vector2[] points;
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();if(points==null||points.Length<2)return;
            float minX=points.Min(p=>p.x),maxX=points.Max(p=>p.x),minY=points.Min(p=>p.y),maxY=points.Max(p=>p.y);
            float scale=Mathf.Min(rectTransform.rect.width/Mathf.Max(1,maxX-minX),rectTransform.rect.height/Mathf.Max(1,maxY-minY));
            var centre=new Vector2((minX+maxX)/2,(minY+maxY)/2);
            for(int i=1;i<points.Length;i++){
                var a=(points[i-1]-centre)*scale;var b=(points[i]-centre)*scale;a.y=-a.y;b.y=-b.y;
                if((b-a).sqrMagnitude<.2f)continue;var dir=(b-a).normalized;var n=new Vector2(-dir.y,dir.x)*1.2f;int k=v.currentVertCount;
                v.AddVert(a+n,color,Vector2.zero);v.AddVert(b+n,color,Vector2.zero);v.AddVert(b-n,color,Vector2.zero);v.AddVert(a-n,color,Vector2.zero);v.AddTriangle(k,k+1,k+2);v.AddTriangle(k,k+2,k+3);
            }
        }
    }
}
