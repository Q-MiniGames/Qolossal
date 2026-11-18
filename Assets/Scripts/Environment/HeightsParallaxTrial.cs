using System;
using UnityEngine;

// Reversible Heights art trial. The original background remains serialized in the scene.
// Weather crosses the frame at its own depth; no rock painting needs to span the climb.
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(9999)]
public sealed class HeightsParallaxTrial : MonoBehaviour
{
    [Serializable]
    public sealed class Weather
    {
        public ParallaxLayer layer;
        public float authoredBaseX;
    }

    public bool useOptionA = true;
    public GameObject optionARoot;
    public GameObject singleValley;
    public GameObject threeLayerRoot;
    public bool useThreeLandLayers;
    public GameObject[] originalLayers = Array.Empty<GameObject>();
    public bool[] originalActive = Array.Empty<bool>();
    public Weather[] weather = Array.Empty<Weather>();
    public Camera targetCamera;
    public float windSpeed = .035f;
    [Min(40f)] public float horizontalPeriod = 80f;
    bool modeApplied, appliedMode, appliedThree;

    void OnEnable() { modeApplied = false; ApplyMode(); }
    void OnValidate() { modeApplied = false; }
    void OnDisable()
    {
        if (optionARoot != null) optionARoot.SetActive(false);
        RestoreOriginals();
        modeApplied = false;
    }

    void LateUpdate()
    {
        ApplyMode();
        UpdateWeather(targetCamera != null ? targetCamera : Camera.main,
            Application.isPlaying ? Time.timeSinceLevelLoad : 0f, false);
    }

    public void SetOptionA(bool value) { useOptionA = value; modeApplied = false; ApplyMode(); }
    public void SetLandLayers(bool three) { useThreeLandLayers = three; useOptionA = true; modeApplied = false; ApplyMode(); }

    void RestoreOriginals()
    {
        for (int i = 0; i < originalLayers.Length; i++)
            if (originalLayers[i] != null)
                originalLayers[i].SetActive(i < originalActive.Length && originalActive[i]);
    }

    void ApplyMode()
    {
        if (modeApplied && appliedMode == useOptionA && appliedThree == useThreeLandLayers) return;
        if (optionARoot != null) optionARoot.SetActive(useOptionA);
        bool three = useThreeLandLayers && threeLayerRoot != null;
        if (singleValley != null) singleValley.SetActive(!three);
        if (threeLayerRoot != null) threeLayerRoot.SetActive(three);
        if (useOptionA)
        {
            foreach (var layer in originalLayers)
                if (layer != null) layer.SetActive(false);
        }
        else RestoreOriginals();
        appliedMode = useOptionA; appliedThree = useThreeLandLayers; modeApplied = true;
    }

    // Fixed-period wrapping occurs far outside the largest tested view. The period never
    // changes with zoom, so zooming cannot relocate a visible cloud. No visible recycling in Y.
    public void UpdateWeather(Camera view, float elapsed, bool refreshLayers = true)
    {
        if (!useOptionA || view == null) return;
        foreach (var item in weather)
        {
            if (item == null || item.layer == null) continue;
            float rate = 1f - item.layer.FollowX;
            float offset = item.authoredBaseX - view.transform.position.x * rate + elapsed * windSpeed;
            offset = Mathf.Repeat(offset + horizontalPeriod * .5f, horizontalPeriod) - horizontalPeriod * .5f;
            item.layer.baseX = view.transform.position.x * rate + offset;
            if (refreshLayers) item.layer.Refresh(view);
        }
    }
}
