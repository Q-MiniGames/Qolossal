using UnityEngine;

// Qvale prototype: stand-in tracks for the listening spot until the Suno music arrives. Each is a
// slow, looping pentatonic piece of soft plucked tones over a low drone, generated from a seed.
public static class PlaceholderMusic
{
    const int Rate = 22050;

    public static AudioClip Make(int seed, float bpm, float seconds = 32f)
    {
        var random = new System.Random(seed);
        int length = Mathf.RoundToInt(seconds * Rate);
        var data = new float[length];
        float root = 196f * Mathf.Pow(2f, (seed % 5 - 2) / 12f);   // around G3
        int[] scale = { 0, 2, 4, 7, 9, 12, 14, 16 };
        float beat = 60f / bpm;
        int note = 2;
        for (float t = 0f; t < seconds - 1.5f; t += beat * (random.Next(4) == 0 ? 2f : 1f))
        {
            note = Mathf.Clamp(note + random.Next(-2, 3), 0, scale.Length - 1);
            float f = root * Mathf.Pow(2f, scale[note] / 12f) * (random.Next(5) == 0 ? .5f : 1f);
            Pluck(data, t, f, .18f, 2.6f);
            if (random.Next(3) == 0) Pluck(data, t + beat * .5f, f * 1.5f, .07f, 1.6f);   // a quiet fifth
        }
        // A soft drone on the root and fifth, swelling slowly, so the loop never falls silent.
        for (int i = 0; i < length; i++)
        {
            float t = i / (float)Rate;
            float swell = .5f + .5f * Mathf.Sin(2f * Mathf.PI * t / seconds);
            data[i] += .035f * swell * (Mathf.Sin(2f * Mathf.PI * root * .5f * t) + .6f * Mathf.Sin(2f * Mathf.PI * root * .75f * t));
        }
        // A simple echo for space, then fade the seam so the loop is smooth.
        int delay = Mathf.RoundToInt(beat * .75f * Rate);
        for (int i = length - 1; i >= delay; i--) data[i] += data[i - delay] * .3f;
        int fade = Rate / 2;
        for (int i = 0; i < fade; i++) { float k = i / (float)fade; data[i] *= k; data[length - 1 - i] *= k; }
        float peak = 0f; foreach (float v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
        if (peak > 0f) for (int i = 0; i < length; i++) data[i] *= .5f / peak;
        var clip = AudioClip.Create("Placeholder track " + seed, length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static void Pluck(float[] data, float start, float frequency, float level, float decay)
    {
        int from = Mathf.RoundToInt(start * Rate), to = Mathf.Min(data.Length, from + Mathf.RoundToInt(decay * 1.6f * Rate));
        for (int i = from; i < to; i++)
        {
            float t = (i - from) / (float)Rate;
            float env = Mathf.Exp(-t * 3f / decay) * Mathf.Min(1f, t * 200f);
            float w = 2f * Mathf.PI * frequency * t;
            data[i] += level * env * (Mathf.Sin(w) + .3f * Mathf.Sin(2f * w) + .1f * Mathf.Sin(3f * w));
        }
    }
}
