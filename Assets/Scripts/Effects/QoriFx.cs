using UnityEngine;
using UnityEngine.SceneManagement;

// Qori's movement and combat effects: landing and running dust, wall-slide scrape, hit sparks
// and blocked-hit chips. Added automatically to the player in any scene.
[DisallowMultipleComponent]
public sealed class QoriFx : MonoBehaviour
{
    PlayerMovement movement; PlayerCombat combat; Collider2D body;
    int landing, reset; float nextRunDust, nextScrape; bool wasRunning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= Attach; SceneManager.sceneLoaded += Attach;
        Attach(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void Attach(Scene scene, LoadSceneMode mode)
    {
        var player = Object.FindFirstObjectByType<PlayerMovement>();
        if (player != null && player.GetComponent<QoriFx>() == null && Fx.Library != null) player.gameObject.AddComponent<QoriFx>();
    }

    void Awake()
    {
        movement = GetComponent<PlayerMovement>(); combat = GetComponent<PlayerCombat>(); body = GetComponent<Collider2D>();
        landing = movement.LandingVersion; reset = movement.ResetVersion;
    }

    void OnEnable() { if (combat != null) combat.OnAttackHit += Hit; }
    void OnDisable() { if (combat != null) combat.OnAttackHit -= Hit; }

    Vector2 Feet => new Vector2(body.bounds.center.x, body.bounds.min.y);

    void LateUpdate()
    {
        var lib = Fx.Library; if (lib == null) return;
        if (reset != movement.ResetVersion) { reset = movement.ResetVersion; landing = movement.LandingVersion; return; }
        if (landing != movement.LandingVersion)
        {
            landing = movement.LandingVersion;
            float strength = Mathf.InverseLerp(4f, 20f, movement.LastLandingSpeed);
            if (movement.LastLandingSpeed > 4f)
            {
                Fx.Play(lib.dustLand, Feet + Vector2.up * .1f, 14f, .9f + .6f * strength, 30);
                if (strength > .5f) Fx.Leaves(Feet, 2, strength);
            }
        }
        bool running = movement.IsGrounded && Mathf.Abs(movement.ObservedVelocity.x) > 5f;
        if (running && (!wasRunning || Time.time >= nextRunDust))
        {
            nextRunDust = Time.time + .28f;
            bool left = movement.ObservedVelocity.x > 0f;   // puff kicks up behind him
            Fx.Play(lib.dustRun, Feet + new Vector2(left ? -.2f : .2f, .08f), 14f, .65f, 30, left);
        }
        wasRunning = running;
        if (movement.IsWallSliding && Time.time >= nextScrape)
        {
            nextScrape = Time.time + .12f;
            float side = movement.WallDirection;
            var bounds = body.bounds;
            // The art streaks from a dense lower-left end up to the right. Put the dense end at the
            // contact point and point the streak away from the wall so it stays in open air.
            const float size = .45f;
            Vector2 contact = new Vector2(side > 0 ? bounds.max.x : bounds.min.x, bounds.center.y - .15f);
            Vector2 at = contact + new Vector2(-side * size * .42f, size * .3f);
            var fb = Fx.Pop(lib.wallScrape, at, size, .3f, 30, 0f, side > 0);
            if (fb != null) { fb.velocity = new Vector2(-side * .9f, .2f); fb.gravity = 3f; }
        }
    }

    void Hit(AttackHitResult hit)
    {
        var lib = Fx.Library; if (lib == null) return;
        Vector2 at = hit.Hit.Point;
        if (hit.Response.Disposition == CombatHitDisposition.Blocked)
            Fx.Pop(lib.hitBlock, at, .55f, .2f, 40, Random.Range(0f, 360f));
        else if (hit.Response.Disposition == CombatHitDisposition.Damaged && lib.hitSparks.Length > 0)
            Fx.Pop(lib.hitSparks[Random.Range(0, lib.hitSparks.Length)], at, .7f, .18f, 40, Random.Range(0f, 360f));
    }
}
