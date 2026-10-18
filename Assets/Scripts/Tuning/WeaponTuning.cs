using System;
using UnityEngine;

// Qori's weapons in one asset (Resources/Tuning/WeaponTuning): each attack's phases, damage,
// knockback and impact feedback. QoriArmoryFactory builds the weapons from it, and an edit in the
// Inspector during Play mode is applied to the weapons in hand at once (and kept afterwards).
// The defaults are the values the game shipped with before the asset existed.
[CreateAssetMenu(menuName = "Qolossal/Tuning/Weapons", fileName = "WeaponTuning")]
public sealed class WeaponTuning : ScriptableObject
{
    [Serializable] public sealed class Phases
    {
        [Tooltip("Seconds of wind-up before the blade can hit.")] [Min(.001f)] public float startup;
        [Tooltip("Seconds the blade hits.")] [Min(.001f)] public float active;
        [Tooltip("Seconds of follow-through before Qori is free.")] [Min(.001f)] public float recovery;
        public Phases(float startup, float active, float recovery) { this.startup = startup; this.active = active; this.recovery = recovery; }
        public float Total => startup + active + recovery;
    }

    [Serializable] public sealed class Weapon
    {
        [Tooltip("The weapon's permanent id (saves and mechanics use it); don't change it.")] public string weaponId;
        public string displayName;
        [Min(0f)] public float damage = 1f;
        public Phases phases = new Phases(.2f, .2f, .26f);
        [Tooltip("Push given to what it hits (x away from Qori, y up).")] public Vector2 knockback = new Vector2(2f, 1f);
        [Tooltip("Qori's own brief pause on a hit (seconds; the whole-game freeze is in FeedbackTuning).")] [Range(0f, .1f)] public float hitStop = .025f;
        [Tooltip("Camera shake on a hit: the peak offset in world units.")] [Range(0f, .3f)] public float cameraShake = .025f;
        [Tooltip("Gamepad rumble on a hit (0..1).")] [Range(0f, 1f)] public float rumble = .1f;
    }

    public Weapon[] weapons =
    {
        new Weapon { weaponId = "forest-staff", displayName = "Leaf Staff", damage = 1f, phases = new Phases(.2f, .2f, .26f) },
        new Weapon { weaponId = "forest-0", displayName = "Leaf Sword", damage = 1.2f, phases = new Phases(.16f, .18f, .23f) },
        new Weapon { weaponId = "forest-2", displayName = "Seedpod Mace", damage = 2f, phases = new Phases(.34f, .2f, .42f), knockback = new Vector2(5f, 2f), hitStop = .06f },
        new Weapon { weaponId = "forest-3", displayName = "Thorn Spear", damage = 1.1f, phases = new Phases(.17f, .12f, .23f) },
    };

    [Header("Leaf Sword combo (after the opener above)")]
    public Phases swordQuickCut = new Phases(.09f, .13f, .12f);
    public Phases swordReturnCut = new Phases(.07f, .13f, .14f);
    public Phases swordDepthSweep = new Phases(.22f, .3f, .38f);

    [Header("Leaf Staff moves (after the opener above)")]
    [Tooltip("Combo hit 1: the sweep. A short wind-up so the hit lands almost on the press.")] public Phases staffOpener = new Phases(.06f, .1f, .18f);
    [Tooltip("Combo hit 2: the backswing.")] public Phases staffBackswing = new Phases(.05f, .1f, .16f);
    [Tooltip("Combo hit 3: the spin finisher (hits all round Qori).")] public Phases staffSpin = new Phases(.08f, .16f, .24f);
    [Tooltip("The step forward each combo hit takes while the stick pushes forward (u/s push; the third hit's is the larger).")] public float staffComboStep = 3.5f, staffFinisherStep = 5f;
    [Tooltip("Weight on a landed combo hit: Qori's own pause, a whole-game freeze, camera shake and rumble (hits 1-2, then the finisher).")]
    [Range(0f, .1f)] public float staffComboHitStop = .06f, staffFinisherHitStop = .09f;
    [Range(0f, .2f)] public float staffComboFreeze = .035f, staffFinisherFreeze = .06f;
    [Range(0f, .3f)] public float staffComboShake = .06f, staffFinisherShake = .14f;
    [Range(0f, 1f)] public float staffComboRumble = .2f, staffFinisherRumble = .45f;
    [Tooltip("The spin finisher's push, against the opener's.")] [Min(1f)] public float staffSpinKnockback = 1.8f;
    [Tooltip("Attack while running: the spinning lunge.")] public Phases staffSprint = new Phases(.1f, .24f, .26f);
    [Tooltip("The lunge's forward push (u/s) and how much of the run it keeps.")] public float staffSprintPush = 4f;
    [Range(0f, 2f)] public float staffSprintCarry = 1.3f;
    [Tooltip("Up + attack: the upward swing (quicker than the opener).")] public Phases staffUpper = new Phases(.04f, .1f, .12f);
    [Tooltip("Down + attack in the air: the plunge. Startup hangs Qori in the air, active is the longest dive (it ends on landing), recovery follows a dive that never lands.")]
    public Phases staffPlunge = new Phases(.1f, 1.5f, .15f);
    [Tooltip("The plunge's dive speed (u/s) and its bounce off whatever it hits.")] [Min(1f)] public float staffPlungeSpeed = 24f;
    [Min(0f)] public float staffPlungeBounce = 10f;
    [Tooltip("The burst where the plunge lands: hits round Qori's feet.")] public Phases staffPlungeLanding = new Phases(.01f, .08f, .22f);
    [Min(0f)] public float staffPlungeLandingDamage = 1f;
    [Tooltip("Hold attack: the release sweep.")] public Phases staffCharged = new Phases(.08f, .2f, .36f);
    [Tooltip("Seconds attack is held (after the press's own swing) before the charge starts.")] [Min(0f)] public float staffChargeDelay = .35f;
    [Tooltip("Seconds of charging before a release sweeps; let go sooner and nothing happens.")] [Min(0f)] public float staffChargeTime = .6f;
    [Tooltip("The charged sweep's damage, against the opener's.")] [Min(1f)] public float staffChargedDamage = 2f;

    [Header("Resin Sling")]
    [Min(0f)] public float slingDamage = .35f;
    public Phases slingPhases = new Phases(.28f, .18f, .24f);
    [Tooltip("Seconds between shots.")] [Min(0f)] public float slingCooldown = .72f;
    public Vector2 slingKnockback = new Vector2(.5f, .15f);

    [Tooltip("Rest after an attack's own phases before the next can start.")] [Min(0f)] public float cooldownPad = .025f;

    public Weapon Find(string weaponId) => Array.Find(weapons, w => w != null && w.weaponId == weaponId);

    // Raised when the asset is edited in the Inspector, so the weapons in hand pick it up.
    public static event Action Changed;
    void OnValidate() => Changed?.Invoke();

    static WeaponTuning current;
    // The asset in Resources/Tuning, or the defaults if it's missing.
    public static WeaponTuning Current
    {
        get
        {
            if (current != null) return current;
            current = Resources.Load<WeaponTuning>("Tuning/WeaponTuning");
            if (current == null) { current = CreateInstance<WeaponTuning>(); current.hideFlags = HideFlags.DontSave; }
            return current;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { current = null; Changed = null; }
}
