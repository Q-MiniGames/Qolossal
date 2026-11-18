var output=new System.Collections.Generic.List<object>();
foreach(var path in RegionalSceneCatalog.BuildPaths){
 var sc=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
 try {
 var roots=sc.GetRootGameObjects();
 var cameras=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).Select(c=>new{c.name,c.orthographicSize}).ToArray();
 var terrain=roots.SelectMany(r=>r.GetComponentsInChildren<MraTerrain>(true)).Where(t=>t.isActiveAndEnabled).ToArray();
 var tops=terrain.Where(t=>t.top!=null).GroupBy(t=>t.top).Select(g=>new{asset=AssetDatabase.GetAssetPath(g.Key),ppu=g.Key.pixelsPerUnit,rect=new[]{g.Key.rect.width,g.Key.rect.height},placements=g.Count()}).ToArray();
 var skies=roots.SelectMany(r=>r.GetComponentsInChildren<ParallaxLayer>(true)).Where(p=>p.texture!=null||p.sprite!=null).Select(p=>new{p.name,active=p.isActiveAndEnabled,asset=AssetDatabase.GetAssetPath(p.texture!=null?p.texture:p.sprite.texture),p.height,p.depth,p.composedForSize,p.texturePixelsPerUnit,p.baseY,p.sortingOrder}).ToArray();
 output.Add(new{scene=sc.name,cameras,tops,skies});
 }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(sc);}
}
System.IO.File.WriteAllText("Tools/Design/WorldRefresh_20261104/asset_inventory.json",Newtonsoft.Json.JsonConvert.SerializeObject(output,Newtonsoft.Json.Formatting.Indented));
return output.Select(x=>x).ToArray();
