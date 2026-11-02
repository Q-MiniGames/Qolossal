using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor integration only. Runtime behavior is selected through reciprocal serialized references.
public static class MraCausewayLiftJoin
{
    public const string TargetPath = "Assets/Scenes/Game/MRAtlas_MR02_Causeway.unity";
    const string SourcePath = "Assets/Art/Codex/Terrain/Body/Causeway/Causeway_Top_Strip.png";
    public static void Apply(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.path != TargetPath)
            throw new InvalidOperationException("Lift join requires the saved canonical MR02 main scene.");
        Configure(scene);
    }
    public static void ApplyForBuild(Scene scene, string targetPath)
    {
        if (targetPath != TargetPath || !scene.IsValid() || !scene.isLoaded || (!string.IsNullOrEmpty(scene.path) && scene.path != targetPath))
            throw new InvalidOperationException("Wrong lift-join build target or source scene.");
        Configure(scene);
    }
    static void Configure(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play is protected.");
        var roots=scene.GetRootGameObjects().Where(r=>r.name=="Generated (MRA builder)").ToArray();
        if (roots.Length!=1) throw new InvalidOperationException("Expected one generated root.");
        var all=roots[0].GetComponentsInChildren<MraTerrain>(true);
        bool IsFlat(MraTerrain t)=>t.surface!=null&&t.surface.Length==2&&t.surface[0]==new Vector2(261,121)&&t.surface[1]==new Vector2(267,121);
        bool IsSlope(MraTerrain t)=>t.surface!=null&&t.surface.Length>=3&&t.surface[0]==new Vector2(267,121)&&t.surface[1]==new Vector2(270,123.25f)&&t.surface[2]==new Vector2(270,130.75f);
        var flats=all.Where(IsFlat).ToArray();var slopes=all.Where(IsSlope).ToArray();
        if(flats.Length!=1||slopes.Length!=1)throw new InvalidOperationException("Reviewed endpoint pair is missing or ambiguous.");
        var flat=flats[0];var slope=slopes[0];var source=AssetDatabase.LoadAssetAtPath<Sprite>(SourcePath);
        if(source==null||flat.top!=source||slope.top!=source||!flat.isActiveAndEnabled||!slope.isActiveAndEnabled||flat.transform.localToWorldMatrix!=Matrix4x4.identity||slope.transform.localToWorldMatrix!=Matrix4x4.identity||source.texture.width!=1024||source.texture.height!=512||source.pixelsPerUnit!=120)
            throw new InvalidOperationException("Reviewed source, import, activation or transform changed.");
        if(flat.walkLinePx!=96||slope.walkLinePx!=96||flat.lipOverhang!=.22f||slope.lipOverhang!=.22f||flat.stripDepth!=1.4f||slope.stripDepth!=1.4f||flat.chamberBackdrop!=null||slope.chamberBackdrop!=null||flat.shortNaturalSoilTail||slope.shortNaturalSoilTail||!flat.HasStepSet||!slope.HasStepSet||!flat.stripDerivedRuns||!slope.stripDerivedRuns||flat.tint!=slope.tint)
            throw new InvalidOperationException("Reviewed strip settings changed.");
        foreach(var t in all.Where(t=>t!=flat&&t!=slope))
            if(t.registeredTopJoinRole!=MraTerrain.RegisteredTopJoinRole.None||t.registeredTopJoinPeer!=null||t.registeredTopJoinSource!=null)
                throw new InvalidOperationException("Unexpected other configured join; do not overwrite it.");
        void Validate(MraTerrain t,MraTerrain peer,MraTerrain.RegisteredTopJoinRole role)
        {
            bool empty=t.registeredTopJoinRole==MraTerrain.RegisteredTopJoinRole.None&&t.registeredTopJoinPeer==null&&t.registeredTopJoinSource==null;
            bool same=t.registeredTopJoinRole==role&&t.registeredTopJoinPeer==peer&&t.registeredTopJoinSource==source;
            if(!empty&&!same)throw new InvalidOperationException("Existing pair configuration conflicts with this review.");
        }
        Validate(flat,slope,MraTerrain.RegisteredTopJoinRole.FlatEnd);Validate(slope,flat,MraTerrain.RegisteredTopJoinRole.RisingStart);
        // Configure both sides before rebuilding either. No terrain points, collider fields or orders change.
        flat.registeredTopJoinRole=MraTerrain.RegisteredTopJoinRole.FlatEnd;flat.registeredTopJoinPeer=slope;flat.registeredTopJoinSource=source;
        slope.registeredTopJoinRole=MraTerrain.RegisteredTopJoinRole.RisingStart;slope.registeredTopJoinPeer=flat;slope.registeredTopJoinSource=source;
        flat.NotifyRegisteredTopJoinConfiguration();slope.NotifyRegisteredTopJoinConfiguration();EditorUtility.SetDirty(flat);EditorUtility.SetDirty(slope);EditorSceneManager.MarkSceneDirty(scene);
    }
}
