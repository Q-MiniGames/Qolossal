using UnityEngine;

// Burrow Grub (E-06): lives under the soil. When Qori is near, it tunnels toward him, kicking up
// little bursts of soil, then the ground heaves (the cue: bigger bursts and a glint) and it
// erupts with its mouth open, biting. It stays up for a moment (the chance to hit or pogo it)
// and burrows again. Underground it can't be touched.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class GrubEnemy : EnemyBase
{
    [SerializeField] float detectRange = 6f, detectHeight = 2.5f, roamHalfWidth = 5f, tunnelSpeed = 2.4f;
    [SerializeField] float telegraphTime = .6f, riseTime = .22f, upTime = 1.6f, sinkTime = .35f, cooldown = 1.4f;
    [SerializeField] Sprite soilBurst, mouthOpen;

    enum State { Buried, Tunnel, Telegraph, Rise, Up, Sink }
    State state; float since, nextPuff, facing = -1f, homeX;
    Collider2D hitbox; SpriteRenderer mouth;

    public bool IsBuried => state == State.Buried || state == State.Tunnel || state == State.Telegraph;
    public override bool HurtsOnContact => IsAlive && (state == State.Rise || state == State.Up);

    protected override void Awake()
    {
        base.Awake();
        body.bodyType = RigidbodyType2D.Kinematic; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        hitbox = GetComponent<Collider2D>(); hitbox.isTrigger = true;
        homeX = body.position.x;
        // Grub_Mouth_Open is an overlay registered to the head's canvas: drawn on top of it while biting.
        if (rig != null && mouthOpen != null)
        {
            var head = rig.Bone("Head").GetComponentInChildren<SpriteRenderer>();
            mouth = new GameObject("Mouth Open").AddComponent<SpriteRenderer>();
            mouth.transform.SetParent(head.transform, false);
            mouth.sprite = mouthOpen; mouth.sortingOrder = head.sortingOrder + 1; mouth.enabled = false;
            var list = new System.Collections.Generic.List<SpriteRenderer>(rig.renderers) { mouth };
            rig.renderers = list.ToArray();   // tinted (hit flash, death) with the rest
        }
        Enter(State.Buried);
    }

    SfxEmitter burrow;
    void Enter(State s)
    {
        if (burrow == null) burrow = SfxEmitter.Attach(gameObject, "Grub_Burrow", false);
        burrow.Playing = s == State.Tunnel || s == State.Telegraph;
        if (s == State.Rise) Sfx.Play("Grub_Erupt", body.position);
        state = s; since = Time.time;
        hitbox.enabled = !IsBuried;
        if (rig != null) foreach (var r in rig.renderers) if (r != null) r.enabled = !IsBuried;
        if (mouth != null) mouth.enabled = s == State.Rise || s == State.Up;   // closes .5 s after it is up
    }

    public override CombatDamageResponse ReceiveCombatHit(CombatDamage hit) =>
        IsBuried ? new CombatDamageResponse { Disposition = CombatHitDisposition.Ignored } : base.ReceiveCombatHit(hit);

    void Soil(float size) { if (soilBurst != null) Fx.Pop(soilBurst, body.position + new Vector2(0f, size * .3f), size, .4f, 31, Random.Range(-10f, 10f)); }

    void FixedUpdate()
    {
        if (!IsAlive) return;
        float t = Time.time - since;
        Vector2 pos = body.position;
        bool sees = CanSee(pos + Vector2.up * .6f, detectRange, detectHeight, out Vector2 toPlayer);
        switch (state)
        {
            case State.Buried:
                if (t >= cooldown && sees) Enter(State.Tunnel);
                break;
            case State.Tunnel:
            {
                if (!sees) { Enter(State.Buried); break; }
                float targetX = Mathf.Clamp(pos.x + toPlayer.x, homeX - roamHalfWidth, homeX + roamHalfWidth);
                float x = Mathf.MoveTowards(pos.x, targetX, tunnelSpeed * Time.fixedDeltaTime);
                facing = toPlayer.x >= 0f ? 1f : -1f;
                body.MovePosition(new Vector2(x, pos.y));
                if (Time.time >= nextPuff) { nextPuff = Time.time + .35f; Soil(.45f); }
                if (Mathf.Abs(targetX - x) < .3f || t > 2.5f) { Enter(State.Telegraph); Fx.Glint(pos + new Vector2(0f, .6f)); }
                break;
            }
            case State.Telegraph:
                if (Time.time >= nextPuff) { nextPuff = Time.time + .15f; Soil(.7f); }
                if (t >= telegraphTime) { Soil(1.5f); Enter(State.Rise); }
                break;
            case State.Rise:
                if (t >= riseTime) Enter(State.Up);
                break;
            case State.Up:
                if (mouth != null && t > .5f) mouth.enabled = false;
                if (t >= upTime) Enter(State.Sink);
                break;
            case State.Sink:
                if (t >= sinkTime) { Soil(.9f); Enter(State.Buried); }
                break;
        }
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (rig == null) return;
        rig.Face(facing);
        float t = Time.time - since;
        // Erupts and burrows by growing from / shrinking into the ground (the rig stands on y = 0).
        float up = state == State.Rise ? Mathf.Clamp01(t / riseTime) : state == State.Sink ? 1f - Mathf.Clamp01(t / sinkTime) : 1f;
        float overshoot = state == State.Up ? 1f + .08f * Mathf.Exp(-t * 8f) * Mathf.Sin(t * 30f) : 1f;
        rig.transform.localScale = new Vector3(1f, Mathf.Max(.01f, up * overshoot), 1f);
        float wave = Time.time * 7f;
        for (int i = 0; i < 5; i++) rig.Pose("Seg" + i, Mathf.Sin(wave - i * .9f) * (state == State.Up ? 6f : 2f));
        rig.Pose("Head", state == State.Rise || state == State.Up && t < .5f ? 14f : Mathf.Sin(wave) * 4f);
    }
}
