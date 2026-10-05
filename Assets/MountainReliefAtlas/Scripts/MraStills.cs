using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Full-screen paintings shown in turn (the reveal, the ending): each fades in, holds until Confirm
// or its time is up, and fades out. A line of caption text can sit under each, in scalable UI
// text (nothing is baked into the paintings).
public static class MraStills
{
    public static IEnumerator Play(Sprite[] stills, string[] captions, float hold = 5f)
    {
        var canvas = GameHud.CreateCanvas("MRA Stills", 95);
        var black = GameHud.AddImage(canvas.transform, "Black", null, new Vector2(.5f, .5f), Vector2.zero, new Vector2(4000f, 4000f));
        black.color = new Color(0f, 0f, 0f, 0f); black.preserveAspect = false;
        var image = GameHud.AddImage(canvas.transform, "Still", null, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1920f, 1080f));
        image.preserveAspect = true;
        var caption = TownUi.Label(canvas.transform, "Caption", new Vector2(.5f, 0f), new Vector2(0f, 90f), new Vector2(1500f, 80f), 36, TextAnchor.MiddleCenter, Color.white);
        for (float t = 0f; t < .6f; t += Time.unscaledDeltaTime) { black.color = new Color(0f, 0f, 0f, t / .6f); yield return null; }
        black.color = Color.black;
        for (int i = 0; i < stills.Length; i++)
        {
            if (stills[i] == null) continue;
            image.sprite = stills[i];
            string line = captions != null && i < captions.Length ? captions[i] : "";
            for (float t = 0f; t < .8f; t += Time.unscaledDeltaTime) { Fade(image, caption, line, t / .8f); yield return null; }
            for (float t = 0f; t < hold; t += Time.unscaledDeltaTime)
            {
                Fade(image, caption, line, 1f);
                if (t > .4f && (TownInput.Confirm() || TownInput.Up())) break;
                yield return null;
            }
            for (float t = 0f; t < .6f; t += Time.unscaledDeltaTime) { Fade(image, caption, line, 1f - t / .6f); yield return null; }
        }
        for (float t = 0f; t < .6f; t += Time.unscaledDeltaTime) { black.color = new Color(0f, 0f, 0f, 1f - t / .6f); yield return null; }
        Object.Destroy(canvas.gameObject);
    }

    static void Fade(Image image, Text caption, string line, float a)
    {
        image.color = new Color(1f, 1f, 1f, a);
        caption.text = line; caption.color = new Color(1f, 1f, 1f, a);
    }
}
