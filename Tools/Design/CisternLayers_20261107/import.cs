AssetDatabase.Refresh();
foreach(var path in new[]{CisternContinuousPreview.ArtPath,CisternContinuousPreview.MiddleArtPath}){
 var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)throw new Exception(path);
 importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=path==CisternContinuousPreview.MiddleArtPath;
 importer.mipmapEnabled=true;importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();
}
return new{imported=true};
