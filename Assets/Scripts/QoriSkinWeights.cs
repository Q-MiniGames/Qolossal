using UnityEngine;

// Canonical 1182x1330 idle painting only. These are skin weights, not cutout contours.
public static class QoriSkinWeights
{
    public struct Legs
    {
        public float left;
        public float right;
        public float body => 1f - left - right;
    }

    // The irregular leaf hem must stay with the body. Values trace its lower tips;
    // the transition below each tip hides most of the thigh/body blend in dark wood.
    static readonly Vector2[] Hem =
    {
        new Vector2(430, 962), new Vector2(510, 967), new Vector2(545, 972),
        new Vector2(586, 984), new Vector2(608, 974), new Vector2(632, 940),
        new Vector2(658, 912), new Vector2(679, 891), new Vector2(701, 912),
        new Vector2(724, 935), new Vector2(748, 962), new Vector2(780, 967)
    };

    static float Smooth(float from, float to, float value)
        => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, value));

    static float HemHeight(float x)
    {
        for (int i = 1; i < Hem.Length; i++)
            if (x <= Hem[i].x)
                return Mathf.Lerp(Hem[i - 1].y, Hem[i].y,
                    Mathf.InverseLerp(Hem[i - 1].x, Hem[i].x, x));
        return Hem[Hem.Length - 1].y;
    }

    public static Legs Evaluate(Vector2 pixel)
    {
        if (pixel.y <= 885f) return default(Legs);
        float limb = Smooth(HemHeight(pixel.x), HemHeight(pixel.x) + 28f, pixel.y);
        // The blade extends down to about y950 on the far right; feet must not
        // inherit this exclusion, since the front toes extend right of the shin.
        float blade = Smooth(775f, 798f, pixel.x) * (1f - Smooth(968f, 1000f, pixel.y));
        limb *= 1f - blade;
        // Below the hem this narrow interval lies in the transparent inter-leg gap.
        // Never blend left/right bones through either visible foot.
        float right = Smooth(615f, 645f, pixel.x);
        return new Legs { left = limb * (1f - right), right = limb * right };
    }

    public static float FootWeight(Vector2 pixel, int legIndex)
    {
        // Complete the ankle transition above the leaf wrap. All visible foot/toe
        // vertices then receive exactly one rigid foot transform.
        return legIndex == 0 ? Smooth(1132f, 1144f, pixel.y)
                             : Smooth(1123f, 1135f, pixel.y);
    }

    public static float ArmWeight(Vector2 pixel)
    {
        // Both hands and the embedded reedblade share ONE translation. Full
        // weight along its painted shaft is essential: independent grip motion
        // would stretch the weapon over the tunic. Shoulder weights remain zero.
        float Stroke(Vector2 a, Vector2 b, float inner, float outer)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(pixel - a, ab) / ab.sqrMagnitude);
            return 1f - Smooth(inner, outer, Vector2.Distance(pixel, a + t * ab));
        }

        float shaft = Stroke(new Vector2(465f, 798f), new Vector2(953f, 868f), 19f, 28f);
        // Broad leaf-shaped blade: overlapping strokes stay inside its padded
        // silhouette, far from the face/tunic. Transparent padding is harmless.
        float blade = Mathf.Max(
            Stroke(new Vector2(953f, 867f), new Vector2(1088f, 886f), 48f, 57f),
            Stroke(new Vector2(1088f, 886f), new Vector2(1169f, 933f), 22f, 31f));
        float leftHand = 1f - Smooth(29f, 37f, Vector2.Distance(pixel, new Vector2(647f, 824f)));
        float rightHand = 1f - Smooth(29f, 37f, Vector2.Distance(pixel, new Vector2(817f, 850f)));
        float leftForearm = Stroke(new Vector2(588f, 697f), new Vector2(631f, 801f), 19f, 27f)
            * Smooth(678f, 786f, pixel.y);
        float rightForearm = Stroke(new Vector2(756f, 747f), new Vector2(802f, 822f), 18f, 26f)
            * Smooth(717f, 807f, pixel.y);
        return Mathf.Max(Mathf.Max(shaft, blade),
            Mathf.Max(Mathf.Max(leftHand, rightHand), Mathf.Max(leftForearm, rightForearm)));
    }
}
