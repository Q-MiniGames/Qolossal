if(EditorApplication.isPlaying)throw new Exception("Density audit requires Edit mode");
var rows=new System.Collections.Generic.List<object>();
var skipped=new System.Collections.Generic.List<string>();
var sourceInfo=new System.Collections.Generic.Dictionary<string,int[]>();
string Hier(Transform t){var a=new System.Collections.Generic.List<string>();while(t!=null){a.Add(t.name);t=t.parent;}a.Reverse();return string.Join("/",a);}
double Norm(double a,double b,double c,double d){double s=a*a+b*b+c*c+d*d,det=a*d-b*c;return System.Math.Sqrt(System.Math.Max(0,(s+System.Math.Sqrt(System.Math.Max(0,s*s-4*det*det)))/2));}
foreach(string scenePath in RegionalSceneCatalog.BuildPaths){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(scenePath);
 try{
 var roots=scene.GetRootGameObjects();var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First(c=>c.orthographic);
 float minSize=camera.orthographicSize;
 foreach(var v in roots.SelectMany(r=>r.GetComponentsInChildren<VistaZone>(true)))minSize=Mathf.Min(minSize,v.size);
 double ppu=2160.0/(2*minSize);
 foreach(var renderer in roots.SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).Where(r=>r.enabled&&r.gameObject.activeInHierarchy)){
  string hierarchy=Hier(renderer.transform);
  bool terrain=renderer.GetComponentInParent<MraTerrain>()!=null;
  var layer=renderer.GetComponentInParent<ParallaxLayer>();
  bool scope=terrain||layer!=null||hierarchy.Contains("UGP Platforming")||hierarchy.Contains("Cave")||hierarchy.Contains("Backdrop");
  if(!scope)continue;
  var vista=renderer.GetComponent<WorldRefreshCaveVista>();var heart=renderer.GetComponent<WorldRefreshHeartSurface>();var rock=renderer.GetComponent<WorldRefreshCaveRockField>();
  if((vista!=null&&vista.vista!=null)||(heart!=null&&heart.detail!=null)||rock!=null){
   var custom=new System.Collections.Generic.List<Texture2D>();
   if(vista!=null){custom.Add(vista.vista);if(renderer is SpriteRenderer vs&&vs.sprite!=null)custom.Add(vs.sprite.texture);}
   else if(heart!=null)custom.Add(heart.detail);else if(renderer is SpriteRenderer rs&&rs.sprite!=null)custom.Add(rs.sprite.texture);
   foreach(var ct in custom){string cp=AssetDatabase.GetAssetPath(ct);var ci=(TextureImporter)AssetImporter.GetAtPath(cp);ci.GetSourceTextureWidthAndHeight(out int cw,out int ch);double nativePpu=vista!=null?vista.pixelsPerUnit:heart!=null?heart.pixelsPerUnit:rock.pixelsPerUnit;double customScale=ppu/nativePpu;
    rows.Add(new{scene=scene.name,hierarchy,asset=cp,kind=vista!=null?"cave-vista-world-shader":heart!=null?"heart-world-shader":"cave-rock-world-shader",minCameraSize=minSize,screenPixelsPerUnit=ppu,sourceWidth=cw,sourceHeight=ch,importedWidth=ct.width,importedHeight=ct.height,requiredWidth=(int)System.Math.Ceiling(ct.width*customScale-.001),requiredHeight=(int)System.Math.Ceiling(ct.height*customScale-.001),nativeMinimumPixelsPerUnit=nativePpu*System.Math.Min((double)cw/ct.width,(double)ch/ct.height),nativePass=customScale<=System.Math.Min((double)cw/ct.width,(double)ch/ct.height)+.001,importPass=customScale<=1.001,compression=ci.textureCompression.ToString(),triangles=0});
   }continue;
  }
  Texture2D tex=null;Vector3[] verts=null;Vector2[] uv=null;int[] tris=null;
  if(renderer is SpriteRenderer sr){
   if(sr.sprite==null)continue;tex=sr.sprite.texture;verts=sr.sprite.vertices.Select(v=>(Vector3)v).ToArray();uv=sr.sprite.uv;tris=sr.sprite.triangles.Select(v=>(int)v).ToArray();
   if(sr.drawMode==SpriteDrawMode.Sliced){var sz=sr.sprite.bounds.size;for(int v=0;v<verts.Length;v++)verts[v]=Vector3.Scale(verts[v],new Vector3(sr.size.x/sz.x,sr.size.y/sz.y,1));}
   if(sr.drawMode==SpriteDrawMode.Tiled&&sr.tileMode!=SpriteTileMode.Continuous)skipped.Add(hierarchy+": adaptive tiled sprite requires mesh review");
  }else{
   var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null||filter.sharedMesh.vertexCount==0)continue;
   var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);tex=block.GetTexture("_MainTex") as Texture2D;
   if(tex==null&&renderer.sharedMaterial!=null)tex=renderer.sharedMaterial.mainTexture as Texture2D;
   if(tex==null)continue;
   try{verts=filter.sharedMesh.vertices;uv=filter.sharedMesh.uv;tris=filter.sharedMesh.triangles;}catch(Exception ex){skipped.Add(hierarchy+": "+ex.Message);continue;}
  }
  string path=AssetDatabase.GetAssetPath(tex);if(string.IsNullOrEmpty(path)||uv.Length!=verts.Length)continue;
  if(!sourceInfo.ContainsKey(path)){
   var imp=AssetImporter.GetAtPath(path) as TextureImporter;int w=tex.width,h=tex.height;if(imp!=null)imp.GetSourceTextureWidthAndHeight(out w,out h);sourceInfo[path]=new[]{w,h};
  }
  int sw=sourceInfo[path][0],sh=sourceInfo[path][1];double worst=0;int useful=0;
  // World distance per native source texel: largest singular value of each affine triangle map.
  for(int i=0;i+2<tris.Length;i+=3){
   int i0=tris[i],i1=tris[i+1],i2=tris[i+2];
   var a=renderer.transform.TransformPoint(verts[i0]);var b=renderer.transform.TransformPoint(verts[i1])-a;var c=renderer.transform.TransformPoint(verts[i2])-a;
   var ub=uv[i1]-uv[i0];var uc=uv[i2]-uv[i0];double ux=ub.x*sw,uy=ub.y*sh,vx=uc.x*sw,vy=uc.y*sh,det=ux*vy-vx*uy;
   if(System.Math.Abs(det)<1e-10||System.Math.Abs(b.x*c.y-b.y*c.x)<1e-10)continue;
   double aa=(b.x*vy-c.x*uy)/det,bb=(-b.x*vx+c.x*ux)/det,cc=(b.y*vy-c.y*uy)/det,dd=(-b.y*vx+c.y*ux)/det;
   worst=System.Math.Max(worst,Norm(aa,bb,cc,dd));useful++;
  }
  if(useful==0)continue;
  // Background depth scaling is evaluated analytically at the closest camera size.
  if(layer!=null){
   double k=layer.composedForSize>0?minSize/layer.composedForSize:1;
   if(layer.depth>0)k=k*layer.depth/(layer.depth+k-1);
   double width=layer.height*(double)tex.width/tex.height*k;
   worst=System.Math.Max(width/sw,layer.height*k/sh);
  }
  if(renderer.sharedMaterial!=null&&renderer.sharedMaterial.shader.name=="Qolossal/WorldRefresh/Native Fitted Stone"){
   var mat=renderer.sharedMaterial;var size=mat.GetVector("_NativeTextureSize");double nativePpu=mat.GetFloat("_PixelsPerUnit");
   worst=System.Math.Max(size.x/(nativePpu*sw),size.y/(nativePpu*sh));
  }
  double scale=worst*ppu;
  var import=AssetImporter.GetAtPath(path) as TextureImporter;
  rows.Add(new{scene=scene.name,hierarchy,asset=path,kind=layer!=null?"background":terrain?"surface-terrain":"cave-environment",minCameraSize=minSize,screenPixelsPerUnit=ppu,sourceWidth=sw,sourceHeight=sh,importedWidth=tex.width,importedHeight=tex.height,requiredWidth=(int)System.Math.Ceiling(sw*scale-0.001),requiredHeight=(int)System.Math.Ceiling(sh*scale-0.001),nativeMinimumPixelsPerUnit=1/worst,nativePass=scale<=1.001,importPass=scale<=System.Math.Min((double)tex.width/sw,(double)tex.height/sh)+.001,compression=import!=null?import.textureCompression.ToString():"unknown",triangles=useful});
 }
 }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
}
System.IO.File.WriteAllText("Tools/Design/WorldRefresh_20261104/density_instances.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{targetWidth=3840,targetHeight=2160,method="All nondegenerate mesh triangles; largest affine world-per-source-texel singular value; ceil at closest authored camera. Environment only, not characters/UI.",rows,skipped},Newtonsoft.Json.Formatting.Indented));
return new{placements=rows.Count,skipped=skipped.Count};

