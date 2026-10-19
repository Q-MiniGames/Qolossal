using UnityEngine;

// Fades a background layer in over the top of its climb (the Terraces' cloud sea, Batch 28d): its
// opacity follows the same smoothed camera height the layers sink by, from 0 at `from` to 1 at `to`
// (shares of the layer's climbRange). Runs before ParallaxLayer, which draws the tint.
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(9000), RequireComponent(typeof(ParallaxLayer))]
public sealed class MraClimbFade : MonoBehaviour
{
    [Range(0f, 1f)] public float from = .7f, to = 1f;

    ParallaxLayer layer;

    void LateUpdate() => Apply();

    public void Apply()
    {
        if (layer == null) layer = GetComponent<ParallaxLayer>();
        var view = layer.targetCamera != null ? layer.targetCamera : Camera.main;
        if (view == null) return;
        float t = Mathf.InverseLerp(layer.climbRange.x, layer.climbRange.y, ParallaxLayer.SmoothedClimbHeight(view));
        var tint = layer.tint;
        tint.a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, t));
        layer.tint = tint;
    }
}
