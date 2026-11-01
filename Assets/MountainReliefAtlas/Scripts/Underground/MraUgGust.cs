using UnityEngine;

// A gust lane (Seed Choir, Windbreak, the chimney's vents): a steady rhythm of tell, pulse and
// calm, always signalled. The tell flickers the lane and plays a warning; during the pulse, Qori
// inside it is blown back to the last shelter he reached (no heart is lost); the calm is safe.
// Timings are the package's starting values (1.2 s tell, 1.0 s pulse, 2.0 s calm).
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraUgGust : MonoBehaviour
{
    public float tellSeconds = 1.2f, pulseSeconds = 1f, calmSeconds = 2f, offset;
    public SpriteRenderer art;
    public Color calm = new Color(.8f, .9f, 1f, .12f), tell = new Color(.9f, .95f, 1f, .35f), pulse = new Color(1f, 1f, 1f, .75f);
    public enum Phase { Calm, Tell, Pulse }
    public Phase Now { get; private set; }
    Phase was; float pushedAt = -10f;
    public int Pushes { get; private set; }

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void Update()
    {
        float cycle = tellSeconds + pulseSeconds + calmSeconds, t = Mathf.Repeat(Time.time + offset, cycle);
        Now = t < tellSeconds ? Phase.Tell : t < tellSeconds + pulseSeconds ? Phase.Pulse : Phase.Calm;
        if (Now != was)
        {
            if (Now == Phase.Tell) Sfx.Play("Enemy_Telegraph", transform.position, .6f);
            if (Now == Phase.Pulse) Sfx.Play("GustMoth_Blast", transform.position, .7f);
            was = Now;
        }
        if (art == null) return;
        art.color = Now == Phase.Pulse ? pulse : Now == Phase.Tell ? Color.Lerp(calm, tell, .5f + .5f * Mathf.Sin(Time.time * 20f)) : calm;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (Now != Phase.Pulse || Time.time - pushedAt < .5f) return;
        var q = MraState.QoriOf(other); if (q == null || AreaTransition.IsTransitioning) return;
        pushedAt = Time.time; Pushes++;
        q.Respawn();
    }
}
