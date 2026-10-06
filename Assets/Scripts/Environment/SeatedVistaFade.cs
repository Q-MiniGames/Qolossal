using UnityEngine;

// A view shown only while Qori sits at a listening spot (Qvale's overlook): the vista layer fades in
// over `seconds` as the seated camera eases out, and the `under` layers (the street's valley layers)
// fade out beneath it; standing up reverses it. Outside Play mode the vista stays hidden.
[DisallowMultipleComponent, DefaultExecutionOrder(9990)]
public sealed class SeatedVistaFade : MonoBehaviour
{
    public ParallaxLayer vista;
    public ParallaxLayer[] under = new ParallaxLayer[0];
    [Min(.01f)] public float seconds = 1.4f;

    float shown;
    float[] tintAlpha, bandAlpha;

    void Awake()
    {
        tintAlpha = new float[under.Length]; bandAlpha = new float[under.Length];
        for (int i = 0; i < under.Length; i++)
            if (under[i] != null) { tintAlpha[i] = under[i].tint.a; bandAlpha[i] = under[i].belowColor.a; }
        Apply();
    }

    void Update()
    {
        float target = ListeningSpot.IsSitting ? 1f : 0f;
        if (Mathf.Approximately(shown, target)) return;
        shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime / seconds);
        Apply();
    }

    void Apply()
    {
        float t = Mathf.SmoothStep(0f, 1f, shown);
        if (vista != null) { var c = vista.tint; c.a = t; vista.tint = c; vista.enabled = t > 0f; }
        for (int i = 0; i < under.Length; i++)
        {
            if (under[i] == null) continue;
            var c = under[i].tint; c.a = tintAlpha[i] * (1f - t); under[i].tint = c;
            var b = under[i].belowColor; b.a = bandAlpha[i] * (1f - t); under[i].belowColor = b;
        }
    }
}
