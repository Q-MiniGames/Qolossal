using UnityEditor;
using UnityEngine;
using System.IO;
public sealed class KeepersCisternImport:AssetPostprocessor {
 void OnPreprocessTexture(){if(!assetPath.StartsWith(KeepersCisternPreview.Art)||!assetPath.EndsWith(".png"))return;
 var t=(TextureImporter)assetImporter;string n=Path.GetFileNameWithoutExtension(assetPath);
 t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;t.mipmapEnabled=true;t.npotScale=TextureImporterNPOTScale.None;t.maxTextureSize=4096;t.filterMode=FilterMode.Bilinear;t.textureCompression=TextureImporterCompression.CompressedHQ;
 bool x=n.StartsWith("CK_Wall_")||n.StartsWith("CK_Band_")||n.StartsWith("CK_Basin_")||n=="CK_Pipe_H",y=n.StartsWith("CK_Wall_")||n=="CK_Pipe_V"||n=="CK_Chain_V"||n=="CK_Water_Sheet_V";
 t.wrapModeU=x?TextureWrapMode.Repeat:TextureWrapMode.Clamp;t.wrapModeV=y?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
 foreach(string p in new[]{"Standalone","PS5","GameCoreXboxOne","GameCoreScarlett"})t.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name=p,overridden=true,maxTextureSize=4096,format=TextureImporterFormat.BC7,compressionQuality=100});
 if(n.StartsWith("LM_")){t.sRGBTexture=false;t.alphaIsTransparency=false;t.mipmapEnabled=false;t.wrapMode=TextureWrapMode.Clamp;}
 }
}
