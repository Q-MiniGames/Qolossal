#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public sealed class BackgroundJourneyReview:MonoBehaviour
{
    const string Key="Qori.BackgroundJourneyReview";
    const string Dir="C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Art/BackgroundJourney/Review";
    static BackgroundJourneyReview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("Background route review").AddComponent<BackgroundJourneyReview>();}};}
    [MenuItem("Qolossal/Background/Review route")]
    static void Run()
    {
        if(EditorApplication.isPlaying||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        foreach(string name in new[]{"Aqueduct","AncientGrove","FallsSanctuary"})
        {
            var art=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/WorldBackground/"+name+".png");
            art.textureType=TextureImporterType.Default;art.mipmapEnabled=false;art.npotScale=TextureImporterNPOTScale.None;art.maxTextureSize=2048;art.textureCompression=TextureImporterCompression.CompressedHQ;art.SaveAndReimport();
        }
        EditorSceneManager.OpenScene("Assets/Scenes/OpeningLevel.unity");SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
    }
    IEnumerator Start()
    {
        yield return new WaitForSeconds(.5f);Directory.CreateDirectory(Dir);
        var camera=Camera.main;var follow=camera.GetComponent<CameraFollow>();if(follow!=null)follow.enabled=false;
        var background=FindFirstObjectByType<CameraBackground>();var player=FindFirstObjectByType<PlayerMovement>();
        player.enabled=false;var thread=player.GetComponent<PlayerThread>();if(thread!=null)thread.enabled=false;
        var body=player.GetComponent<Rigidbody2D>();body.linearVelocity=Vector2.zero;body.simulated=false;
        var results=new List<string>();var positions=new[]{new Vector3(-5,-1.9f,0),new Vector3(14,-1.9f,0),new Vector3(34,2.4f,0),new Vector3(51,6.9f,0)};
        for(int i=0;i<positions.Length;i++)
        {
            player.transform.position=positions[i];camera.transform.position=positions[i]+new Vector3(0,2,-10);camera.aspect=16f/9f;
            yield return null;yield return new WaitForEndOfFrame();
            Capture(camera,"World-Zone-"+i);
        }
        camera.aspect=16f/9f;camera.transform.position=new Vector3(0,0,-10);yield return null;yield return new WaitForEndOfFrame();
        Vector3 original=background.LandmarkPosition;float startX=camera.WorldToViewportPoint(original).x;Capture(camera,"Landmark-Before");
        camera.transform.position=new Vector3(30,0,-10);yield return null;yield return new WaitForEndOfFrame();
        Vector3 advanced=background.LandmarkPosition;float endX=camera.WorldToViewportPoint(advanced).x;Capture(camera,"Landmark-After");
        results.Add((startX>0&&startX<1&&endX<0?"PASS ":"FAIL ")+"Previously visible landmark exits left after advancing");
        results.Add((Mathf.Abs((advanced.x-original.x)-3)<.001f?"PASS ":"FAIL ")+"Landmark travels 90 percent of camera distance across the screen");
        yield return new WaitForSeconds(.5f);results.Add((Vector3.Distance(advanced,background.LandmarkPosition)<.001f?"PASS ":"FAIL ")+"Stopped camera does not recenter scenery");
        camera.transform.position=new Vector3(0,0,-10);yield return null;yield return new WaitForEndOfFrame();
        results.Add((Vector3.Distance(original,background.LandmarkPosition)<.001f?"PASS ":"FAIL ")+"Backtracking returns to exactly the same landmark position");
        bool covered=true;
        foreach(float aspect in new[]{.75f,16f/9f,2.39f})
        foreach(float x in new[]{-7f,8f,26f,44f,55f})
        {
            camera.aspect=aspect;camera.transform.position=new Vector3(x,8,-10);yield return null;yield return new WaitForEndOfFrame();
            Bounds union=new Bounds();bool first=true;
            foreach(var renderer in GameObject.Find("World landscape - fixed landmarks").GetComponentsInChildren<MeshRenderer>())
            {if(first){union=renderer.bounds;first=false;}else union.Encapsulate(renderer.bounds);}
            float h=camera.orthographicSize,w=h*aspect;covered&=union.min.x<=x-w&&union.max.x>=x+w&&union.min.y<=8-h&&union.max.y>=8+h;
        }
        results.Add((covered?"PASS ":"FAIL ")+"Landscape and distant atmosphere cover tested viewports");
        File.WriteAllLines(Path.Combine(Dir,"Validation.txt"),results);Debug.Log("BACKGROUND REVIEW FINISHED: "+string.Join(" | ",results));
        EditorApplication.isPlaying=false;
    }
    void Capture(Camera camera,string name)
    {
        var rt=new RenderTexture(1600,900,24);rt.Create();
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
        var old=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();RenderTexture.active=old;
        File.WriteAllBytes(Path.Combine(Dir,name+".png"),texture.EncodeToPNG());Destroy(texture);rt.Release();Destroy(rt);
    }
}
#endif
