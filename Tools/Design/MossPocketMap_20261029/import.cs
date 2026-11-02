if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Stop Play before importing map resources");
UnityEditor.AssetDatabase.Refresh();
string folder="Assets/Art/Prototypes/MossPocketMap_20261029";
var records=new System.Collections.Generic.List<object>();
foreach(var file in System.IO.Directory.GetFiles(folder,"*.png")){
 string path=file.Replace('\\','/');var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
 importer.textureType=UnityEditor.TextureImporterType.Sprite;importer.spriteImportMode=UnityEditor.SpriteImportMode.Single;importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=2048;importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
 var tex=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);records.Add(new{path,width=tex.width,height=tex.height,format=tex.format.ToString(),runtimeBytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex)});
}
System.IO.File.WriteAllText("C:/UnityProjects/Qolossal/Tools/Design/MossPocketMap_20261029/import_qa.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{records,standaloneTested=false,consoleTested=false,runtimeChartIntegrated=false},Newtonsoft.Json.Formatting.Indented));return records;
