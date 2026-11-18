using System;
using System.IO;
using System.Linq;

// The shipping scene list. The review galleries and retired prototypes never enter a build.
public static class RegionalSceneCatalog
{
    public const string Underground = "Qolossal_Underground";
    public const string Folder = "Assets/Scenes/Game/";
    public static readonly string[] Surface = {
        "MRAtlas_MR01_Cradle", "MRAtlas_MR02_Causeway", "MRAtlas_MR03_Terraces",
        "MRAtlas_MR04_Qvale", "MRAtlas_MR05_Ribwood", "MRAtlas_MR06_Heights", "MRAtlas_MR07_Summit"
    };
    // Qvale's homes (MraWorldBuilder.RestoreQvaleHouses). A save made inside one resumes on Qvale's street.
    public static readonly string[] Interiors = { "MRAtlas_MR04_C02", "MRAtlas_MR04_C03", "MRAtlas_MR04_C04", "MRAtlas_MR04_C05", "MRAtlas_MR04_C06" };
    public static string[] BuildPaths => Surface.Concat(new[] { Underground }).Concat(Interiors).Select(n => Folder + n + ".unity").ToArray();

    // The Heart checkpoint and wake controls were retired. Older saves resume at
    // the existing Cistern approach checkpoint; reading does not rewrite saves.
    public static string ResolveSavedCheckpoint(string scenePath, string id)
    {
        return id == "regional:HEART" && ResolveSavedScene(scenePath) == Folder + Underground + ".unity"
            ? "regional:A08" : id;
    }

    // Old checkpoints keep their inventory and flags. Retired scenes resume safely on their surface.
    // This does not rewrite the user's save merely because it was read.
    public static string ResolveSavedScene(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        string name = Path.GetFileNameWithoutExtension(path.Replace('\\', '/'));
        if (name == Underground) return Folder + name + ".unity";
        for (int i = 0; i < Surface.Length; i++)
            if (name == Surface[i] || name.StartsWith("MRAtlas_MR0" + (i + 1) + "_", StringComparison.Ordinal))
                return Folder + Surface[i] + ".unity";
        if (name.StartsWith("MRAtlas_MR08_", StringComparison.Ordinal) || name.StartsWith("MRAtlas_UG_E", StringComparison.Ordinal) || name == "MRAtlas_UG_HEART")
            return Folder + Surface[6] + ".unity";
        if (name.StartsWith("MRAtlas_UG_", StringComparison.Ordinal))
        {
            int region = name.StartsWith("MRAtlas_UG_A", StringComparison.Ordinal) ? 3 :
                name.StartsWith("MRAtlas_UG_B", StringComparison.Ordinal) ? 2 :
                name.StartsWith("MRAtlas_UG_C", StringComparison.Ordinal) ? 4 : 5;
            return Folder + Surface[region] + ".unity";
        }
        return path;
    }
}
