#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

// Reversible Editor art review. No saved scene objects or player-build hook.
[ExecuteAlways]
public sealed class CisternNetworkReview : MonoBehaviour
{
    public const string Folder = "Assets/Art/Prototypes/CisternNetwork_20261107/";
    public const int ExpectedBindings = 27;
    public static readonly string[] Rooms = { "A01", "A02", "A03", "A04", "A05", "A06", "A07", "A08" };
    public static bool Includes(string id) => Array.IndexOf(Rooms, id) >= 0;
    public static readonly Vector2 Pivot = new Vector2(1600, 140);
    // Artwork coordinates use one common depth transform, independently of
    // room activation. Each narrative panel occurs once in the entire network.
    public static readonly Vector4[] FarRects = {
        new Vector4(1552,160.8f,44,44f/3), new Vector4(1570,146,22,44),
        new Vector4(1602.5f,124.625f,72,24), new Vector4(1617,144,22,44),
        new Vector4(1592,153.5f,60,20), new Vector4(1567,111,64,64f/3),
        new Vector4(1622,160,40,40f/3), new Vector4(1650,158,46,46f/3)
    };
    public static readonly Vector4[] FarDomains = {
        new Vector4(1480,170,1516,192),new Vector4(1528,126,1544,186),
        new Vector4(1552,100,1644,120),new Vector4(1628,122,1644,163),
        new Vector4(1554,154,1614,186),new Vector4(1490,84,1616,100),
        new Vector4(1628,170,1670,184),new Vector4(1684,168,1736,196)
    };
    public static readonly Vector4[] MiddleRects = {
        new Vector4(1616.8f,108.15f,6.8f,6.8f*2/3),
        new Vector4(1582.4f,157.5f,6.8f,6.8f*2/3),
        new Vector4(1642.24f,168.9f,6.8f,6.8f*2/3),
        new Vector4(1541.92f,162.56f,6.8f,6.8f*2/3)
    };
    public static readonly string[] FarPaths = {
        Folder+"SluiceArrival_Far.png", Folder+"DrainWell_Far.png",
        "Assets/Art/Prototypes/CisternLayers_20261107/Reservoir_Far.png",
        Folder+"Spillway_Far.png", Folder+"PumpVault_Far.png", Folder+"SiltWalk_Far.png",
        Folder+"KeeperGallery_Far.png", Folder+"GuardianSluice_Far.png", Folder+"PassageStone.png"
    };
    public static readonly string[] MiddlePaths = {
        "Assets/Art/Prototypes/CisternLayers_20261107/Waterworks_Middle_v2.png",
        Folder+"PumpWheel_Middle.png", Folder+"KeeperRefuge_Middle.png", Folder+"DrainMechanism_Middle.png"
    };
    [Serializable] sealed class Point { public float x,y; }
    [Serializable] sealed class Connection { public string id,fromRoom,toRoom; public Point[] points; }
    [Serializable] sealed class Connections { public Connection[] connections; }
    struct Binding { public SpriteRenderer renderer; public Material material; public string property; public float original; }
    readonly List<Binding> bindings = new List<Binding>();
    RenderTexture far, middle;
    public bool Layered { get; private set; } = true;
    public int BindingCount => bindings.Count;
    public bool Healthy => bindings.Count == ExpectedBindings && far != null && middle != null &&
        bindings.All(b => b.renderer != null && b.material != null && b.renderer.sharedMaterial == b.material && b.material.GetFloat(b.property) > .5f);
    public long ArrayBytes => (far != null ? UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(far) : 0) +
        (middle != null ? UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(middle) : 0);
    public Vector4[] Enclosures { get; private set; }
    int roomIndex = 3;

    public void Configure()
    {
        if (bindings.Count != 0) throw new InvalidOperationException("Already configured");
        try {
            far = MakeArray(FarPaths, "Cistern far review array");
            middle = MakeArray(MiddlePaths, "Cistern middle review array");
            Shader.SetGlobalTexture("_CisternFar", far); Shader.SetGlobalTexture("_CisternMiddle", middle);
            Shader.SetGlobalVector("_CisternPivot", Pivot);
            var farRectsPadded=new Vector4[16];Array.Copy(FarRects,farRectsPadded,FarRects.Length);
            Shader.SetGlobalVectorArray("_CisternNetworkFarRects", farRectsPadded);
            var domainsPadded=new Vector4[16];Array.Copy(FarDomains,domainsPadded,FarDomains.Length);
            Shader.SetGlobalVectorArray("_CisternNetworkDomains",domainsPadded);
            Shader.SetGlobalVectorArray("_CisternMiddleRects", MiddleRects);
            CavePassagePresentation.Create();
            foreach (var v in FindObjectsByType<WorldRefreshCaveVista>(FindObjectsSortMode.None))
                if (Includes(v.GetComponentInParent<UndergroundRoom>().roomId)) Add(v.GetComponent<SpriteRenderer>(), "_NetworkEnabled");
            var links = JsonUtility.FromJson<Connections>(Resources.Load<TextAsset>("CavePassageConnections").text).connections;
            var lookup = links.ToDictionary(c => "Continuous passage " + c.id);
            foreach (var renderer in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.sharedMaterial == null || !lookup.TryGetValue(renderer.sharedMaterial.name, out var link)) continue;
                if (Includes(link.fromRoom)) Add(renderer,"_NetworkEnabled");
                if (Includes(link.toRoom)) Add(renderer,"_B_NetworkEnabled");
            }
            // Only enclosed access tunnels; open gallery connectors retain the
            // uninterrupted far and middle view. End caps extend naturally into
            // the room, so no material seam is used as a transition boundary.
            var closed = new HashSet<string>{"A01_A02_main","A02_A03_main","A03_A04_main","A05_A06_main","A06_A02_shortcut"};
            var walls = new List<Vector4>();
            foreach (var link in links.Where(c=>closed.Contains(c.id)))
                for (int i=1;i<link.points.Length;i++) {
                    var a=link.points[i-1];var b=link.points[i];
                    walls.Add(new Vector4(a.x,a.y,b.x,b.y));
                }
            Enclosures=walls.ToArray();
            var padded=new Vector4[16];Array.Copy(Enclosures,padded,Enclosures.Length);
            Shader.SetGlobalVectorArray("_CisternEnclosures",padded);
            Shader.SetGlobalFloat("_CisternEnclosureCount",Enclosures.Length);
            SetLayered(true,false);
            if (BindingCount != ExpectedBindings) throw new InvalidOperationException("Expected 27 Cistern bindings, found "+BindingCount);
        } catch { Restore(); throw; }
    }
    void Add(SpriteRenderer renderer,string property)
    {
        var material=renderer.sharedMaterial;
        if(!material.HasProperty(property))throw new InvalidOperationException(material.name+" lacks network property");
        bindings.Add(new Binding {renderer=renderer,material=material,property=property,original=material.GetFloat(property)});
        material.SetFloat(property,1);
    }
    static RenderTexture MakeArray(string[] paths,string label)
    {
        var sources=paths.Select(p=>AssetDatabase.LoadAssetAtPath<Texture2D>(p) ?? throw new InvalidOperationException("Missing "+p)).ToArray();
        int width=sources.Max(t=>t.width),height=sources.Max(t=>t.height);
        var result=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB) {
            name=label, dimension=UnityEngine.Rendering.TextureDimension.Tex2DArray,volumeDepth=paths.Length,
            useMipMap=true,autoGenerateMips=false,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,hideFlags=HideFlags.HideAndDontSave
        };
        result.Create();
        var temp=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        var active=RenderTexture.active;
        try {
            for(int i=0;i<sources.Length;i++) { Graphics.Blit(sources[i],temp); Graphics.CopyTexture(temp,0,0,result,i,0); }
            result.GenerateMips();
        } catch {result.Release();DestroyImmediate(result);throw;}
        finally {RenderTexture.active=active;RenderTexture.ReleaseTemporary(temp);}
        return result;
    }
    public void SetLayered(bool enabled,bool announce=true)
    {
        Layered=enabled;Shader.SetGlobalFloat("_CisternLayered",enabled?1:0);
        if(announce)TownHud.Toast((enabled?"B: Connected layered Cistern":"A: Far scenery only")+" — F8 / left stick click: compare. F7: next room. F6: Cistern approach.",12);
    }
    public void VisitRoom(int index)
    {
        roomIndex=(index+Rooms.Length)%Rooms.Length;
        var room=FindObjectsByType<UndergroundRoom>(FindObjectsSortMode.None).FirstOrDefault(r=>r.roomId==Rooms[roomIndex]);
        var player=FindAnyObjectByType<PlayerMovement>();var checkpoint=room!=null?room.GetComponentInChildren<Checkpoint>():null;
        if(player==null||checkpoint==null)return;
        player.PlaceAt(checkpoint.SpawnPosition,1,true);Physics2D.SyncTransforms();
        TownHud.Toast(room.roomName+" — F7: next review room",8);
    }
    public void ReturnToStart()
    {
        var player=FindAnyObjectByType<PlayerMovement>();if(player==null)return;
        foreach(var hit in Physics2D.RaycastAll(new Vector2(1610,109),Vector2.down,8,LayerMask.GetMask("Ground")))
            if(hit.collider!=null&&!hit.collider.isTrigger&&hit.normal.y>.6f) {
                player.PlaceAt(new Vector2(1610,hit.point.y+.66f),1,true);Physics2D.SyncTransforms();roomIndex=3;return;
            }
    }
    void Update()
    {
        if(!Application.isPlaying||GamePauseMenu.BlocksGameplayInput||MraState.Busy)return;
        var keys=Keyboard.current;var pad=Gamepad.current;
        if((keys!=null&&keys.f8Key.wasPressedThisFrame)||(pad!=null&&pad.leftStickButton.wasPressedThisFrame))SetLayered(!Layered);
        if(keys!=null&&keys.f6Key.wasPressedThisFrame)ReturnToStart();
        if(keys!=null&&keys.f7Key.wasPressedThisFrame)VisitRoom(roomIndex+1);
    }
    void OnDisable()=>Restore();
    void OnDestroy()=>Restore();
    void Restore()
    {
        foreach(var b in bindings)if(b.material!=null)b.material.SetFloat(b.property,b.original);
        bindings.Clear();
        foreach(var texture in new[]{far,middle})if(texture!=null){texture.Release();DestroyImmediate(texture);}
        far=null;middle=null;
    }
}
#endif
