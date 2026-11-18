if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
var cam=Camera.main;
var rt=new RenderTexture(1920,1080,24);
var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;
try{cam.targetTexture=rt;CaveDepth.Apply(cam);cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();System.IO.File.WriteAllBytes("Tools/Design/CisternContinuous_20261106/current_game.png",tex.EncodeToPNG());}
finally{cam.targetTexture=oldTarget;RenderTexture.active=oldActive;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tex);}
var result=new{playing=EditorApplication.isPlaying,frame=Time.frameCount,enabled=CisternContinuousPreview.Enabled,controllers=Resources.FindObjectsOfTypeAll<CaveParallaxComparison>().Where(c=>c.gameObject.scene.IsValid()).Select(c=>new{bindings=c.BindingCount,continuous=c.HasContinuousPainting}).ToArray(),rooms=UnityEngine.Object.FindObjectsByType<WorldRefreshCaveVista>(FindObjectsSortMode.None).Where(v=>CaveParallaxComparison.Includes(v.GetComponentInParent<UndergroundRoom>().roomId)).Select(v=>new{id=v.GetComponentInParent<UndergroundRoom>().roomId,on=v.GetComponent<SpriteRenderer>().sharedMaterial.GetFloat("_ContinuousEnabled"),texture=v.GetComponent<SpriteRenderer>().sharedMaterial.GetTexture("_Continuous").name}).ToArray()};
System.IO.File.WriteAllText("Tools/Design/CisternContinuous_20261106/persistent_second_play.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
