using UnityEngine;

public sealed class QoriWalkMotion : MonoBehaviour
{
    private readonly QoriCloakMotion cloakMotion = new QoriCloakMotion();
    private Sprite[] sprites;
    private QoriRopeMotion[] drawings;
    private float worldTrail, trailVelocity, phase, verticalTrail, verticalTrailVelocity;
    private float airAmount, airAmountVelocity;

    public void Initialize(Sprite[] frames, Sprite idle, SpriteRenderer source, Sprite jump, Sprite fall)
    {
        sprites = new Sprite[frames.Length + 3];
        drawings = new QoriRopeMotion[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            sprites[i] = i < frames.Length ? frames[i] :
                i == frames.Length ? idle : i == frames.Length + 1 ? jump : fall;
            if (sprites[i] == null) continue;
            drawings[i] = gameObject.AddComponent<QoriRopeMotion>();
            drawings[i].Initialize(sprites[i], source);
        }
    }

    public bool Show(bool active, Sprite sprite, float speed, float referenceSpeed,
        float frameRate, bool flipped, Color tint, float verticalSpeed, bool grounded)
    {
        if (drawings == null) return false;
        if (!active) { Hide(); return false; }
        airAmount = Mathf.SmoothDamp(airAmount, grounded ? 0f : 1f,
            ref airAmountVelocity, .12f, Mathf.Infinity, Time.deltaTime);
        cloakMotion.Step(new Vector2(speed, grounded ? 0f : verticalSpeed), Time.deltaTime);
        float target = Mathf.Clamp(speed / Mathf.Max(.1f, referenceSpeed), -1f, 1f);
        worldTrail = Mathf.SmoothDamp(worldTrail, target, ref trailVelocity, .18f,
            Mathf.Infinity, Time.deltaTime);
        verticalTrail = Mathf.SmoothDamp(verticalTrail,
            grounded ? 0f : Mathf.Clamp(verticalSpeed / 12f, -1f, 1f),
            ref verticalTrailVelocity, .20f, Mathf.Infinity, Time.deltaTime);
        phase = (phase + Time.deltaTime * frameRate * Mathf.Abs(target) * Mathf.PI * 2f / 6f) % (Mathf.PI * 2f);
        float wave = grounded ? Mathf.Sin(phase - .45f) * Mathf.Abs(worldTrail) : 0f;
        int selected = -1;
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] == sprite && drawings[i] != null && drawings[i].Ready) { selected = i; break; }
        for (int i = 0; i < drawings.Length; i++)
            if (drawings[i] != null) drawings[i].ShowWalking(i == selected,
                worldTrail * (flipped ? -1f : 1f), wave, flipped, tint, verticalTrail, grounded ? 0f : airAmount, cloakMotion);
        return selected >= 0;
    }

    public void Hide()
    {
        cloakMotion.Reset();
        worldTrail = trailVelocity = phase = verticalTrail = verticalTrailVelocity = 0f;
        airAmount = airAmountVelocity = 0f;
        if (drawings == null) return;
        foreach (QoriRopeMotion drawing in drawings)
            if (drawing != null) drawing.ShowWalking(false, 0f, 0f, false, Color.white);
    }
    private void OnDisable() { Hide(); }
    private void OnDestroy()
    {
        if (drawings == null) return;
        foreach (QoriRopeMotion drawing in drawings) if (drawing != null) Destroy(drawing);
    }
}

