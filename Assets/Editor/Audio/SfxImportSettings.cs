using System.IO;
using UnityEditor;
using UnityEngine;

// Import settings for the sound effects (Assets/Audio/SFX/<Bus>/ and Loops/). The WAV sources stay
// untouched (48 kHz, 16-bit stereo); these settings only shape the imported runtime clips. They are
// defaults, chosen by length and use; per-platform overrides wait for measured builds (platform
// direction), so none are set here.
//   - Short one-shots (under 1.2 s): decompressed on load (ADPCM), preloaded: no decode on play.
//   - Longer one-shots: Vorbis, compressed in memory, decoded as they play.
//   - Loops on objects (Loops/ derivatives and short loop sources): ADPCM compressed in memory,
//     which loops sample-accurately.
//   - Ambience beds and story moments: streamed Vorbis.
//   - Gameplay buses are forced to mono (the game pans them itself); Ambience, Story, Town and UI
//     keep their stereo image.
public sealed class SfxImportSettings : AssetPostprocessor
{
    public const string Folder = "Assets/Audio/SFX/";
    const float BytesPerSecond = 48000f * 2f * 2f;

    static bool Under(string path, string sub) => path.StartsWith(Folder + sub + "/");

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(Folder)) return;
        var importer = (AudioImporter)assetImporter;
        float seconds = File.Exists(assetPath) ? (new FileInfo(assetPath).Length - 44) / BytesPerSecond : 1f;
        string name = Path.GetFileName(assetPath);
        bool loopDerivative = Under(assetPath, "Loops");
        bool bed = Under(assetPath, "Ambience") || loopDerivative && (name.StartsWith("SFX_Amb_") || name.StartsWith("SFX_Water") || name.StartsWith("SFX_Night"));
        bool story = Under(assetPath, "Story");
        bool stereo = bed || story || Under(assetPath, "Town") || Under(assetPath, "UI");
        bool loopSource = name.Contains("_Loop_") || name.Contains("_Idle_") || name.Contains("_Hover_") || name.Contains("_Walk_")
                          || name.Contains("_Burrow_") || name.Contains("_Ripple_") || name.Contains("_Wings_") || name.Contains("_Move_")
                          || name.StartsWith("SFX_Spring_Pulse") || name.StartsWith("SFX_Combat_ChargeUp");

        importer.forceToMono = !stereo;
        importer.loadInBackground = bed || story || seconds >= 1.2f;
        var s = importer.defaultSampleSettings;
        s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        if (bed || story)
        {
            s.loadType = AudioClipLoadType.Streaming; s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = story ? .7f : .6f; s.preloadAudioData = false;
        }
        else if (loopDerivative || loopSource)
        {
            s.loadType = AudioClipLoadType.CompressedInMemory; s.compressionFormat = AudioCompressionFormat.ADPCM;
            s.preloadAudioData = true;
        }
        else if (seconds < 1.2f)
        {
            s.loadType = AudioClipLoadType.DecompressOnLoad; s.compressionFormat = AudioCompressionFormat.ADPCM;
            s.preloadAudioData = true;
        }
        else
        {
            s.loadType = AudioClipLoadType.CompressedInMemory; s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = .7f; s.preloadAudioData = true;
        }
        importer.defaultSampleSettings = s;
    }
}
