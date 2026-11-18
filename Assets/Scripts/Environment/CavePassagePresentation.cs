using System;
using System.Collections.Generic;
using UnityEngine;

// Rendering-only runtime bridge over the 63 authored passage boxes. References
// actual room paintings/bounds; never modifies terrain, traversal or discovery.
public sealed class CavePassagePresentation : MonoBehaviour
{
    [Serializable] sealed class Connection { public string id,fromRoom,toRoom; public float[] rect; }
    [Serializable] sealed class Connections { public Connection[] connections; }
    struct Binding { public SpriteRenderer art; public Material previous,material; }
    readonly List<Binding> bindings=new List<Binding>();
    readonly List<SpriteRenderer> hiddenNear=new List<SpriteRenderer>();
    public static CavePassagePresentation Instance { get; private set; }
    public int ConnectedCount => bindings.Count;
    public int MissingCount { get; private set; }

    public static CavePassagePresentation Create()
    {
        // DontSave presentation objects are excluded from FindObjectsByType.
        if(Instance!=null)
        {
            // ExecuteAlways authoring components can refresh their rock material
            // after this bridge was installed (including Editor play transitions).
            // Reassert ownership when a caller requests the active presentation.
            bool valid=Instance.bindings.Count>0;
            foreach(var b in Instance.bindings) if(b.art==null||b.material==null) valid=false;
            if(valid)
            {
                foreach(var b in Instance.bindings) b.art.sharedMaterial=b.material;
                foreach(var art in Instance.hiddenNear) if(art!=null) art.enabled=false;
                return Instance;
            }
            // A cached Editor instance may refer to a scene that has been reloaded.
            var stale=Instance;Instance=null;
            stale.Release();
            if(Application.isPlaying) Destroy(stale.gameObject);else DestroyImmediate(stale.gameObject);
        }
        var go=new GameObject("Continuous cave passage presentation") {hideFlags=HideFlags.DontSave};
        var result=go.AddComponent<CavePassagePresentation>();
        Instance=result;
        result.Configure();
        return result;
    }

    void Configure()
    {
        var source=Resources.Load<TextAsset>("CavePassageConnections");
        var shader=Shader.Find("Qolossal/CavePassageVista");
        if(source==null||shader==null){Debug.LogError("Cave passage presentation resources missing");return;}
        var connections=JsonUtility.FromJson<Connections>(source.text).connections;
        var rooms=new Dictionary<string,WorldRefreshCaveVista>();
        foreach(var vista in FindObjectsByType<WorldRefreshCaveVista>(FindObjectsSortMode.None))
        {
            var room=vista.GetComponentInParent<UndergroundRoom>();
            if(room!=null&&vista.vista!=null)rooms[room.roomId]=vista;
        }
        foreach(var art in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            // The old translucent mineral overlay ended at each rectangular room
            // edge. Its detail is already in the continuous painted composition.
            if(art.name=="UGP backdrop_near"&&art.enabled)
            {
                var room=art.GetComponentInParent<UndergroundRoom>();
                if(room!=null&&rooms.ContainsKey(room.roomId)){hiddenNear.Add(art);art.enabled=false;}
            }
            if(art.name!="UGP passage backdrop"||art.sortingOrder!=-105)continue;
            var b=art.bounds;
            Connection match=null;
            foreach(var c in connections)
            {
                if(c.rect==null||c.rect.Length!=4)continue;
                if(Mathf.Abs(b.min.x-c.rect[0])<.03f&&Mathf.Abs(b.min.y-c.rect[1])<.03f&&
                   Mathf.Abs(b.size.x-c.rect[2])<.03f&&Mathf.Abs(b.size.y-c.rect[3])<.03f){match=c;break;}
            }
            if(match==null||!rooms.TryGetValue(match.fromRoom,out var from)||!rooms.TryGetValue(match.toRoom,out var to))
            {MissingCount++;Debug.LogError("Unmatched cave passage backdrop: "+art.transform.position);continue;}
            var material=new Material(shader){name="Continuous passage "+match.id,hideFlags=HideFlags.HideAndDontSave};
            from.ApplyTo(material);to.ApplyTo(material,"_B_");
            bindings.Add(new Binding{art=art,previous=art.sharedMaterial,material=material});
            art.sharedMaterial=material;
        }
    }

    void OnDestroy() => Release();

    void Release()
    {
        if(Instance==this)Instance=null;
        foreach(var binding in bindings)
        {
            if(binding.art!=null&&binding.art.sharedMaterial==binding.material)binding.art.sharedMaterial=binding.previous;
            if(binding.material!=null){if(Application.isPlaying)Destroy(binding.material);else DestroyImmediate(binding.material);}
        }
        foreach(var art in hiddenNear)if(art!=null)art.enabled=true;
        bindings.Clear();hiddenNear.Clear();
    }
}
