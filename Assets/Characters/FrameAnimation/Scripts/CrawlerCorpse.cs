using UnityEngine;

// Test-only. An enemy's collapse, left where it died (the enemy itself vanishes at once): the death frames once, then
// it lies there and fades. A flyer's body first drops to the ground below it.
public sealed class CrawlerCorpse : MonoBehaviour
{
    Sprite[] frames; float seconds, born, fallSpeed; SpriteRenderer view; bool falling;

    public static void Leave(SpriteRenderer view, Sprite[] frames, float seconds, float facing, bool falls = false)
    {
        if (view == null) return;
        if (frames == null || frames.Length == 0) { Destroy(view.gameObject); return; }
        var c = view.gameObject.AddComponent<CrawlerCorpse>();
        c.view = view; c.frames = frames; c.seconds = seconds; c.born = Time.time; c.falling = falls;
        view.flipX = facing < 0f; view.color = Color.white; view.enabled = true;
        view.transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        float t = Time.time - born;
        view.sprite = frames[Mathf.Min(frames.Length - 1, (int)(t / seconds * frames.Length))];
        if (falling)
        {
            fallSpeed += 30f * Time.deltaTime;
            float step = fallSpeed * Time.deltaTime;
            Vector2 at = transform.position;
            var hit = Physics2D.Raycast(at, Vector2.down, step + .35f, GroundLayer.Mask);
            if (hit) { transform.position = new Vector3(at.x, hit.point.y + .35f, transform.position.z); falling = false; }
            else transform.position += Vector3.down * step;
        }
        if (t > seconds + 2.5f) view.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - (t - seconds - 2.5f)));
        if (t > seconds + 3.5f) Destroy(gameObject);
    }
}
