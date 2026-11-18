if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
var comparison=Resources.FindObjectsOfTypeAll<CaveParallaxComparison>().First(c=>c.gameObject.scene.IsValid()&&c.BindingCount==7);
var current=AssetDatabase.LoadAssetAtPath<Texture2D>(CisternContinuousPreview.ArtPath);
var old=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Prototypes/CisternContinuous_20261106/CisternPanorama.png");
var cam=Camera.main;var target=cam.targetTexture;var active=RenderTexture.active;
try{foreach(bool after in new[]{false,true}){
 comparison.SetContinuousPainting(after?current:old,CaveParallaxComparison.ContinuousReviewRect);
 var rt=new RenderTexture(1920,1080,24);var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
 try{cam.targetTexture=rt;CaveDepth.Apply(cam);cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();System.IO.File.WriteAllBytes("Tools/Design/CisternDistance_20261106/"+(after?"After":"Before")+".png",tex.EncodeToPNG());}
 finally{cam.targetTexture=target;RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tex);}
}}finally{comparison.SetContinuousPainting(current,CaveParallaxComparison.ContinuousReviewRect);}
var report=new{playing=EditorApplication.isPlaying,frame=Time.frameCount,bindings=comparison.BindingCount,mode=comparison.Gentle?"B":"A",painting=current.name,camera=cam.transform.position.ToString(),zoom=cam.orthographicSize,edgePreview=cam.GetComponent<CameraFollow>().DownwardPreview,persistent=CisternContinuousPreview.Enabled};
System.IO.File.WriteAllText("Tools/Design/CisternDistance_20261106/live_handoff.json",Newtonsoft.Json.JsonConvert.SerializeObject(report));return report;
