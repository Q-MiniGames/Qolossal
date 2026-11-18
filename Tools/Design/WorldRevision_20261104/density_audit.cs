if(EditorApplication.isPlaying)throw new Exception("Density audit requires Edit mode");
var rows=new System.Collections.Generic.List<object>();
var coverageRows=new System.Collections.Generic.List<object>();
var skipped=new System.Collections.Generic.List<string>();
var sourceInfo=new System.Collections.Generic.Dictionary<string,int[]>();
string Hier(Transform t){var a=new System.Collections.Generic.List<string>();while(t!=null){a.Add(t.name);t=t.parent;}a.Reverse();return string.Join("/",a);}
// Stable 2x2 largest singular value; avoids cancellation near isotropic maps.
double Norm(double a,double b,double c,double d)=>.5*(Math.Sqrt((a+d)*(a+d)+(c-b)*(c-b))+Math.Sqrt((a-d)*(a-d)+(c+b)*(c+b)));
// Subtract local coordinates in double, then apply only the linear world matrix.
// Subtracting two transformed float positions loses texel precision at x~2000.
(double x,double y) WorldEdge(Transform transform,Vector3 from,Vector3 to){var m=transform.localToWorldMatrix;double x=(double)to.x-from.x,y=(double)to.y-from.y,z=(double)to.z-from.z;return (m.m00*x+m.m01*y+m.m02*z,m.m10*x+m.m11*y+m.m12*z);}
// Match the reported ceil exactly: tolerate only .001 source pixels of arithmetic
// noise, not the previous .1% scale allowance (which could hide several pixels).
bool Fits(double scale,int w,int h)=>Math.Ceiling(w*scale-.001)<=w&&Math.Ceiling(h*scale-.001)<=h;
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
  string shaderName=renderer.sharedMaterial!=null?renderer.sharedMaterial.shader.name:"";
  bool regionalWorld=shaderName=="Qolossal/Review/RegionalUndergroundRock"||shaderName=="Qolossal/Prototypes/Cave Rollout Regional Rim";
  bool regionalEdgeNative=shaderName=="Qolossal/WorldRevision/Native Regional Painted Edge";
  bool regionalEdge=shaderName=="Qolossal/Review/RegionalPaintedEdge"||regionalEdgeNative;
  bool scope=terrain||layer!=null||hierarchy.Contains("UGP Platforming")||hierarchy.Contains("Cave")||hierarchy.Contains("Backdrop")||regionalWorld||regionalEdge;
  if(!scope)continue;
  // Regional border shaders have no _MainTex. UV0/UV1 encode eight blend
  // weights, not texture coordinates. Measure every channel actually used by
  // the mesh against the shader's world repeat, and UV2 for registered paint.
  if(regionalWorld||regionalEdge){
   var regionalFilter=renderer.GetComponent<MeshFilter>();
   if(regionalFilter==null||regionalFilter.sharedMesh==null){skipped.Add(hierarchy+": regional material has no mesh");continue;}
   var rm=regionalFilter.sharedMesh;var rp=rm.vertices;var rt=rm.triangles;var rc=rm.colors;
   var wa=new System.Collections.Generic.List<Vector4>();var wb=new System.Collections.Generic.List<Vector4>();var re=new System.Collections.Generic.List<Vector4>();
   rm.GetUVs(0,wa);rm.GetUVs(1,wb);rm.GetUVs(2,re);
   if(wa.Count!=rp.Length||wb.Count!=rp.Length){skipped.Add(hierarchy+": regional blend weight channels missing");continue;}
   var mat=renderer.sharedMaterial;var repeatA=mat.GetVector("_RepeatA");var repeatB=mat.GetVector("_RepeatB");
   double[] fixedCapRepeat={16,4,4.8984375,4,4.8984375,4.8984375,4.8984375,8};
   bool ChannelUsed(int channel,int index){
    double sum=0;for(int k=0;k<4;k++)sum+=Math.Max(0,wa[index][k])+Math.Max(0,wb[index][k]);
    double w=channel<4?wa[index][channel]:wb[index][channel-4];return Math.Max(0,w)/Math.Max(sum,.00001)>.0001;
   }
   void RegionalRow(Texture2D texture,string property,string kind,double worldPerSourceTexel,double worldPerImportedTexel,int measured,int invalid,double samplingScale=1){
    if(texture==null){skipped.Add(hierarchy+": missing regional "+property);return;}
    string ap=AssetDatabase.GetAssetPath(texture);var ai=AssetImporter.GetAtPath(ap) as TextureImporter;
    if(ai==null){skipped.Add(hierarchy+": missing regional importer "+property+" "+ap);return;}
    ai.GetSourceTextureWidthAndHeight(out int aw,out int ah);
    if(measured==0||worldPerSourceTexel<=0||double.IsNaN(worldPerSourceTexel)||double.IsInfinity(worldPerSourceTexel)){skipped.Add(hierarchy+": no measurable regional "+property+" mapping");return;}
    if(invalid>0)skipped.Add(hierarchy+": degenerate regional "+property+" UV triangles: "+invalid);
    double rs=worldPerSourceTexel*ppu;
    rows.Add(new{scene=scene.name,hierarchy,asset=ap,kind,textureProperty=property,samplingScale,minCameraSize=minSize,screenPixelsPerUnit=ppu,sourceWidth=aw,sourceHeight=ah,importedWidth=texture.width,importedHeight=texture.height,requiredWidth=(int)Math.Ceiling(aw*rs-.001),requiredHeight=(int)Math.Ceiling(ah*rs-.001),nativeMinimumPixelsPerUnit=1/worldPerSourceTexel,nativePass=invalid==0&&Fits(rs,aw,ah),importPass=invalid==0&&Fits(worldPerImportedTexel*ppu,texture.width,texture.height),compression=ai.textureCompression.ToString(),triangles=measured});
   }
   void RegionalUv(Texture2D texture,string property,int channel,double uDivisor,bool registered){
    if(texture==null){skipped.Add(hierarchy+": missing regional "+property);return;}
    if(re.Count!=rp.Length){skipped.Add(hierarchy+": missing regional UV2 for "+property);return;}
    var ai=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
    if(ai==null){skipped.Add(hierarchy+": missing regional importer "+property);return;}ai.GetSourceTextureWidthAndHeight(out int aw,out int ah);
    double nativeScale=regionalEdgeNative&&!registered?(channel<4?mat.GetVector("_NativeEdgeScaleA")[channel]:mat.GetVector("_NativeEdgeScaleB")[channel-4]):1;
    if(nativeScale<=0){skipped.Add(hierarchy+": invalid native edge scale "+property);return;}
    double worst=0,importedWorst=0;int measured=0,invalid=0,candidates=0;
    for(int t=0;t+2<rt.Length;t+=3){
     int j0=rt[t],j1=rt[t+1],j2=rt[t+2];
     if(rc.Length==rp.Length&&Math.Max(rc[j0].a,Math.Max(rc[j1].a,rc[j2].a))<=0)continue;
     if(channel>=0&&!ChannelUsed(channel,j0)&&!ChannelUsed(channel,j1)&&!ChannelUsed(channel,j2))continue;
     // lerp in this shader is not saturated: z>1 extrapolates, so only an
     // exactly cap-only triangle (all z==1) has no edge-paint RGB contribution.
     if(!registered&&re[j0].z==1&&re[j1].z==1&&re[j2].z==1)continue;
     // Revised shader saturates cap mix and replaces q.y<=0 with native rock.
     // Exclude only wholly cap-only triangles; crossing triangles retain the
     // same affine derivative wherever their source painting contributes.
     if(regionalEdgeNative&&(Math.Min(re[j0].z,Math.Min(re[j1].z,re[j2].z))>=1||1+(Math.Max(re[j0].y,Math.Max(re[j1].y,re[j2].y))-1)*nativeScale<=0))continue;
     // Registered rim RGB is used only below y=.88; outside the source rectangle
     // saturation produces an edge constant, so ignore wholly outlying triangles.
     if(registered&&(Math.Min(re[j0].y,Math.Min(re[j1].y,re[j2].y))>=.88||Math.Max(re[j0].y,Math.Max(re[j1].y,re[j2].y))<=0||Math.Max(re[j0].x,Math.Max(re[j1].x,re[j2].x))<=0||Math.Min(re[j0].x,Math.Min(re[j1].x,re[j2].x))>=1))continue;
     var b=WorldEdge(renderer.transform,rp[j0],rp[j1]);var c=WorldEdge(renderer.transform,rp[j0],rp[j2]);
     if(Math.Abs(b.x*c.y-b.y*c.x)<1e-10)continue;
     candidates++;
     double ux=((double)re[j1].x-re[j0].x)*aw/uDivisor*nativeScale,uy=((double)re[j1].y-re[j0].y)*ah*nativeScale,vx=((double)re[j2].x-re[j0].x)*aw/uDivisor*nativeScale,vy=((double)re[j2].y-re[j0].y)*ah*nativeScale,det=ux*vy-vx*uy;
     if(Math.Abs(det)<1e-10){invalid++;continue;}
     double aa=(b.x*vy-c.x*uy)/det,bb=(-b.x*vx+c.x*ux)/det,cc=(b.y*vy-c.y*uy)/det,dd=(-b.y*vx+c.y*ux)/det;
     worst=Math.Max(worst,Norm(aa,bb,cc,dd));importedWorst=Math.Max(importedWorst,Norm(aa*aw/texture.width,bb*ah/texture.height,cc*aw/texture.width,dd*ah/texture.height));measured++;
    }
    if(candidates>0)RegionalRow(texture,property,registered?"registered-regional-rim-uv2":"regional-painted-edge-uv2",worst,importedWorst,measured,invalid,nativeScale);
   }
   for(int channel=0;channel<8;channel++){
    bool used=false;for(int t=0;t<rt.Length&&!used;t++)used=ChannelUsed(channel,rt[t]);if(!used)continue;
    string property="_T"+channel;var texture=mat.GetTexture(property) as Texture2D;
    if(texture==null){skipped.Add(hierarchy+": missing used regional "+property);continue;}
    var ai=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
    if(ai==null){skipped.Add(hierarchy+": missing regional importer "+property);continue;}ai.GetSourceTextureWidthAndHeight(out int aw,out int ah);
    double repeat=regionalEdge?fixedCapRepeat[channel]:channel==0?16:channel<4?repeatA[channel]:repeatB[channel-4];
    if(repeat<=0){skipped.Add(hierarchy+": invalid regional world repeat "+property);continue;}
    RegionalRow(texture,property,"regional-rock-world-repeat",Math.Max(repeat/aw,repeat/ah),Math.Max(repeat/texture.width,repeat/texture.height),rt.Length/3,0);
    if(regionalEdge){double edgeRepeat=channel<4?repeatA[channel]:repeatB[channel-4];if(edgeRepeat<=0)skipped.Add(hierarchy+": invalid painted edge repeat "+channel);else RegionalUv(mat.GetTexture("_E"+channel) as Texture2D,"_E"+channel,channel,edgeRepeat,false);}
   }
   if(shaderName=="Qolossal/Prototypes/Cave Rollout Regional Rim")RegionalUv(mat.GetTexture("_Rim") as Texture2D,"_Rim",-1,1,true);
   continue;
  }
  var vista=renderer.GetComponent<WorldRefreshCaveVista>();var heart=renderer.GetComponent<WorldRefreshHeartSurface>();var rock=renderer.GetComponent<WorldRefreshCaveRockField>();
  if(vista!=null&&!vista.isActiveAndEnabled)vista=null;if(heart!=null&&!heart.isActiveAndEnabled)heart=null;if(rock!=null&&!rock.isActiveAndEnabled)rock=null;
  if((vista!=null&&vista.vista!=null)||(heart!=null&&heart.detail!=null)||rock!=null){
   var custom=new System.Collections.Generic.List<Texture2D>();
   // Vista, extension, leftDepth, rightDepth and upperVault all use imported
   // dimensions / max(216,PPU) in the live/staged component. Their shader UVs
   // translate only; blend masks do not magnify or warp painted source UVs.
   if(vista!=null){var serialized=new SerializedObject(vista);var it=serialized.GetIterator();while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue is Texture2D texture&&!custom.Contains(texture))custom.Add(texture);if(vista.extension==null&&renderer is SpriteRenderer vs&&vs.sprite!=null&&!custom.Contains(vs.sprite.texture))custom.Add(vs.sprite.texture);}
   else if(heart!=null)custom.Add(heart.detail);else if(renderer is SpriteRenderer rs&&rs.sprite!=null)custom.Add(rs.sprite.texture);
   foreach(var ct in custom){string cp=AssetDatabase.GetAssetPath(ct);var ci=AssetImporter.GetAtPath(cp) as TextureImporter;if(ci==null){skipped.Add(hierarchy+": missing source importer for world-mapped texture "+cp);continue;}ci.GetSourceTextureWidthAndHeight(out int cw,out int ch);double nativePpu=Math.Max(216,vista!=null?vista.pixelsPerUnit:heart!=null?heart.pixelsPerUnit:rock.pixelsPerUnit);
    double worldWidth=ct.width/nativePpu,worldHeight=ct.height/nativePpu;
    if(vista==null&&heart==null&&rock!=null&&renderer is SpriteRenderer rockArt){worldWidth=rockArt.sprite.rect.width/nativePpu;worldHeight=rockArt.sprite.rect.height/nativePpu;}
    double nativeDensity=Math.Min(cw/worldWidth,ch/worldHeight),customScale=ppu/nativeDensity,importScale=ppu*Math.Max(worldWidth/ct.width,worldHeight/ct.height);
    rows.Add(new{scene=scene.name,hierarchy,asset=cp,kind=vista!=null?"cave-vista-world-shader":heart!=null?"heart-world-shader":"cave-rock-world-shader",minCameraSize=minSize,screenPixelsPerUnit=ppu,sourceWidth=cw,sourceHeight=ch,importedWidth=ct.width,importedHeight=ct.height,requiredWidth=(int)System.Math.Ceiling(cw*customScale-.001),requiredHeight=(int)System.Math.Ceiling(ch*customScale-.001),nativeMinimumPixelsPerUnit=nativeDensity,nativePass=Fits(customScale,cw,ch),importPass=Fits(importScale,ct.width,ct.height),compression=ci.textureCompression.ToString(),triangles=0});
   }continue;
  }
  // The restored approved paint uses a separate registered UV channel.
  // Audit the live, possibly relocated/bent mesh rather than a stale receipt.
  if(renderer.sharedMaterial!=null&&renderer.sharedMaterial.shader.name=="Qolossal/WorldRevision/Approved Rim Native Attachment"){
   var mat=renderer.sharedMaterial;var edge=mat.GetTexture("_EdgeMask") as Texture2D;var mf=renderer.GetComponent<MeshFilter>();
   if(edge==null||mf==null)throw new Exception("Missing approved rim mapping: "+hierarchy);
   string ep=AssetDatabase.GetAssetPath(edge);var ei=(TextureImporter)AssetImporter.GetAtPath(ep);ei.GetSourceTextureWidthAndHeight(out int ew,out int eh);
   var mesh=mf.sharedMesh;var pos=mesh.vertices;var euv=new System.Collections.Generic.List<Vector4>();mesh.GetUVs(1,euv);var ets=mesh.triangles;var map=mat.GetVector("_RimUV");double worstEdge=0,worstEdgeImported=0;int measured=0,invalidEdge=0;
   if(euv.Count!=pos.Length)throw new Exception("Missing approved rim UV1: "+hierarchy);
   for(int i=0;i<ets.Length;i+=3){
    int i0=ets[i],i1=ets[i+1],i2=ets[i+2];var b=WorldEdge(mf.transform,pos[i0],pos[i1]);var c=WorldEdge(mf.transform,pos[i0],pos[i2]);
    double ux=((double)euv[i1].x-euv[i0].x)*map.x*ew,uy=((double)euv[i1].y-euv[i0].y)*map.y*eh,vx=((double)euv[i2].x-euv[i0].x)*map.x*ew,vy=((double)euv[i2].y-euv[i0].y)*map.y*eh,det=ux*vy-vx*uy;
    if(Math.Abs(b.x*c.y-b.y*c.x)<1e-10)continue;
    if(Math.Abs(det)<1e-10){invalidEdge++;continue;}
    double aa=(b.x*vy-c.x*uy)/det,bb=(-b.x*vx+c.x*ux)/det,cc=(b.y*vy-c.y*uy)/det,dd=(-b.y*vx+c.y*ux)/det;
    worstEdge=Math.Max(worstEdge,Norm(aa,bb,cc,dd));
    worstEdgeImported=Math.Max(worstEdgeImported,Norm(aa*ew/edge.width,bb*eh/edge.height,cc*ew/edge.width,dd*eh/edge.height));measured++;
   }
   double edgeScale=worstEdge*ppu;
   if(invalidEdge>0)skipped.Add(hierarchy+": "+invalidEdge+" nonzero-area painted triangles have degenerate UV1; no finite source canvas resolves a collapsed mapping");
   if(measured==0||worstEdge<=0)skipped.Add(hierarchy+": no measurable painted UV1 triangles; mapping rejected");
   else rows.Add(new{scene=scene.name,hierarchy,asset=ep,kind="approved-painted-rim-uv1",minCameraSize=minSize,screenPixelsPerUnit=ppu,sourceWidth=ew,sourceHeight=eh,importedWidth=edge.width,importedHeight=edge.height,requiredWidth=(int)Math.Ceiling(ew*edgeScale-.001),requiredHeight=(int)Math.Ceiling(eh*edgeScale-.001),nativeMinimumPixelsPerUnit=1/worstEdge,nativePass=invalidEdge==0&&Fits(edgeScale,ew,eh),importPass=invalidEdge==0&&Fits(worstEdgeImported*ppu,edge.width,edge.height),compression=ei.textureCompression.ToString(),triangles=measured});
   if(mat.HasProperty("_UseRoofClosure")&&mat.GetFloat("_UseRoofClosure")>0){
    var coverage=mat.GetTexture("_RoofClosure") as Texture2D;
    if(coverage==null)skipped.Add(hierarchy+": enabled roof coverage is missing");
    else{
     string cp=AssetDatabase.GetAssetPath(coverage);var ci=AssetImporter.GetAtPath(cp) as TextureImporter;
     if(ci==null)skipped.Add(hierarchy+": roof coverage importer missing");
     else{
      ci.GetSourceTextureWidthAndHeight(out int cw,out int ch);
      // Coverage is derived at the approved source's exact native dimensions.
      // It is measured explicitly but kept separate from painted RGB assets.
      if(cw!=ew||ch!=eh)skipped.Add(hierarchy+": closure dimensions differ from approved alpha registration");
      double cs=worstEdge*ppu,iscl=worstEdge*ppu*Math.Max((double)ew/coverage.width,(double)eh/coverage.height);
      coverageRows.Add(new{scene=scene.name,hierarchy,asset=cp,kind="derived-roof-coverage",approvedSource=ep,classification="Procedural alpha coverage above the connected painted rock; no new colour detail",sourceWidth=cw,sourceHeight=ch,importedWidth=coverage.width,importedHeight=coverage.height,requiredWidth=(int)Math.Ceiling(cw*cs-.001),requiredHeight=(int)Math.Ceiling(ch*cs-.001),nativeMinimumPixelsPerUnit=1/worstEdge,nativePass=invalidEdge==0&&Fits(cs,cw,ch),importPass=invalidEdge==0&&Fits(iscl,coverage.width,coverage.height),triangles=measured});
     }
    }
   }
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
  string path=AssetDatabase.GetAssetPath(tex);if(string.IsNullOrEmpty(path))continue;
  bool worldMapped=renderer.sharedMaterial!=null&&(renderer.sharedMaterial.shader.name=="Qolossal/WorldRefresh/Native Fitted Stone"||renderer.sharedMaterial.shader.name=="Qolossal/WorldRevision/Approved Rim Native Attachment");
  bool analytical=layer!=null||worldMapped;
  if(!analytical&&uv.Length!=verts.Length){skipped.Add(hierarchy+": UV0 count does not match mesh vertices");continue;}
  if(!sourceInfo.ContainsKey(path)){
   var imp=AssetImporter.GetAtPath(path) as TextureImporter;int w=tex.width,h=tex.height;if(imp!=null)imp.GetSourceTextureWidthAndHeight(out w,out h);sourceInfo[path]=new[]{w,h};
  }
  int sw=sourceInfo[path][0],sh=sourceInfo[path][1];double worst=0,worstImported=0;int useful=0,invalidUv=0;
  // World distance per native source texel: largest singular value of each affine triangle map.
  for(int i=0;!analytical&&i+2<tris.Length;i+=3){
   int i0=tris[i],i1=tris[i+1],i2=tris[i+2];
   var b=WorldEdge(renderer.transform,verts[i0],verts[i1]);var c=WorldEdge(renderer.transform,verts[i0],verts[i2]);
   double ux=((double)uv[i1].x-uv[i0].x)*sw,uy=((double)uv[i1].y-uv[i0].y)*sh,vx=((double)uv[i2].x-uv[i0].x)*sw,vy=((double)uv[i2].y-uv[i0].y)*sh,det=ux*vy-vx*uy;
   if(System.Math.Abs(b.x*c.y-b.y*c.x)<1e-10)continue;
   if(System.Math.Abs(det)<1e-10){invalidUv++;continue;}
   double aa=(b.x*vy-c.x*uy)/det,bb=(-b.x*vx+c.x*ux)/det,cc=(b.y*vy-c.y*uy)/det,dd=(-b.y*vx+c.y*ux)/det;
   worst=System.Math.Max(worst,Norm(aa,bb,cc,dd));
   worstImported=System.Math.Max(worstImported,Norm(aa*sw/tex.width,bb*sh/tex.height,cc*sw/tex.width,dd*sh/tex.height));useful++;
  }
  if(invalidUv>0)skipped.Add(hierarchy+": "+invalidUv+" nonzero-area textured triangles have degenerate UV0");
  // Background depth scaling is evaluated analytically at the closest camera size.
  if(layer!=null){
   double k=layer.composedForSize>0?minSize/layer.composedForSize:1;
   if(layer.depth>0)k=k*layer.depth/(layer.depth+k-1);
   double h=layer.height>0?layer.height:layer.sprite!=null?layer.sprite.rect.height/layer.sprite.pixelsPerUnit:layer.texture!=null?layer.texture.height/layer.texturePixelsPerUnit:0;
   double u=Math.Abs(layer.uvRect.width),v=Math.Abs(layer.uvRect.height);
   if(h<=0||u<=0||v<=0){skipped.Add(hierarchy+": invalid parallax height or cropped UV footprint");continue;}
   double aspect=layer.sprite!=null?layer.sprite.rect.width/layer.sprite.rect.height:(double)tex.width/tex.height;
   double width=h*aspect*u/v*k,height=h*k;
   var bx=renderer.transform.TransformVector(Vector3.right);var by=renderer.transform.TransformVector(Vector3.up);
   double dx=width/(sw*u),dy=height/(sh*v);
   worst=Norm(bx.x*dx,by.x*dy,bx.y*dx,by.y*dy);
   worstImported=Norm(bx.x*dx*sw/tex.width,by.x*dy*sh/tex.height,bx.y*dx*sw/tex.width,by.y*dy*sh/tex.height);
  }
  if(worldMapped){
   var mat=renderer.sharedMaterial;var size=mat.GetVector("_NativeTextureSize");double nativePpu=mat.GetFloat("_PixelsPerUnit");
   worst=System.Math.Max(size.x/(nativePpu*sw),size.y/(nativePpu*sh));
   worstImported=System.Math.Max(size.x/(nativePpu*tex.width),size.y/(nativePpu*tex.height));
  }
  if(worst<=0||double.IsInfinity(worst)||double.IsNaN(worst)){skipped.Add(hierarchy+": no finite measurable texture mapping");continue;}
  double scale=worst*ppu;
  var import=AssetImporter.GetAtPath(path) as TextureImporter;
  rows.Add(new{scene=scene.name,hierarchy,asset=path,kind=layer!=null?"background":terrain?"surface-terrain":"cave-environment",minCameraSize=minSize,screenPixelsPerUnit=ppu,sourceWidth=sw,sourceHeight=sh,importedWidth=tex.width,importedHeight=tex.height,requiredWidth=(int)System.Math.Ceiling(sw*scale-0.001),requiredHeight=(int)System.Math.Ceiling(sh*scale-0.001),nativeMinimumPixelsPerUnit=1/worst,nativePass=invalidUv==0&&Fits(scale,sw,sh),importPass=invalidUv==0&&Fits(worstImported*ppu,tex.width,tex.height),compression=import!=null?import.textureCompression.ToString():"unknown",triangles=useful});
 }
 }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
}
System.IO.File.WriteAllText("Tools/Design/WorldRevision_20261104/density_instances.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{targetWidth=3840,targetHeight=2160,method="All nondegenerate mesh triangles; largest affine world-per-source-texel singular value; ceil at closest authored camera. Environment only, not characters/UI. Derived coverage masks measured separately from colour artwork.",rows,coverageRows,skipped},Newtonsoft.Json.Formatting.Indented));
return new{placements=rows.Count,coveragePlacements=coverageRows.Count,skipped=skipped.Count};


