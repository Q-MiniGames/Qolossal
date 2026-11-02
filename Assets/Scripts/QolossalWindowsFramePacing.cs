using UnityEngine;

// The shared game's console presentation remains platform-owned. Windows runs at the
// approved 60 FPS target; the opt-in development probe can override this for profiling.
public static class QolossalWindowsFramePacing
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
#endif
    }
}
