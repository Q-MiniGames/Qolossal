var camera=Camera.main;
camera.transform.position=new Vector3(129,11.5f,-50);camera.orthographicSize=5;camera.aspect=16f/9;
foreach(var sky in UnityEngine.Object.FindObjectsByType<WorldRefreshSky>(FindObjectsSortMode.None))sky.Refresh();
foreach(var weather in UnityEngine.Object.FindObjectsByType<WorldRefreshWeather>(FindObjectsSortMode.None))weather.Refresh(0);
foreach(var layer in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None))layer.Refresh(camera);
foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="Qolossal/Upper Terrain Contrast")){
 var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);b.SetColor("_Color",Color.white);b.SetColor("_RendererColor",Color.white);r.SetPropertyBlock(b);
}
var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);var active=RenderTexture.active;
try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();System.IO.File.WriteAllBytes("Tools/Design/SurfaceContrast_20261104/debug.png",tex.EncodeToPNG());}finally{camera.targetTexture=null;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
return "captured";
