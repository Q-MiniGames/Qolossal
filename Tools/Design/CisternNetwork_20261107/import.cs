AssetDatabase.Refresh();
foreach(var path in System.IO.Directory.GetFiles(CisternNetworkReview.Folder,"*.png")){
 var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)throw new Exception(path);
 importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=path.Contains("Middle");
 importer.mipmapEnabled=true;importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=path.Contains("PassageStone")?TextureWrapMode.Repeat:TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();
}
return new{imported=true};
