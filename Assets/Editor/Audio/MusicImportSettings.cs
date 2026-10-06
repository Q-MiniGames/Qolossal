using UnityEditor;
using UnityEngine;

// Import settings for the soundtrack (Assets/Audio/Music): long stereo tracks, so they stream from
// disk instead of being decompressed into memory, compressed with Vorbis. Sources stay lossless WAV
// (48 kHz, 16-bit) in the project. These are the default settings; per-platform overrides are chosen
// later from measured builds (platform direction), so none are set here.
public sealed class MusicImportSettings : AssetPostprocessor
{
    public const string Folder = "Assets/Audio/Music/";

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(Folder)) return;
        var importer = (AudioImporter)assetImporter;
        importer.forceToMono = false;
        importer.loadInBackground = true;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = .6f;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData = false;
        importer.defaultSampleSettings = settings;
    }
}
