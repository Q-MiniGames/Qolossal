using UnityEngine;

// The Great Heart Hall: each sector's balcony has a root-knot Qori wakes with Up, and one quarter of the heart
// comes alive (saved as cw:heart:quarter:<sector>). Nobody reaches the heart itself; it is only seen. When the
// fourth quarter wakes after the Summit's reveal (in practice from E, the Descent), the careful ending plays.
public sealed class MraHeartQuarter : MraUgInteract
{
    public const string FlagPrefix = "cw:heart:quarter:";
    public static readonly string[] Sectors = { "A", "C", "D", "E" };

    public string sector = "A";
    [Tooltip("The heart's four quarter overlays, in Sectors order: dim while asleep, lit once awake.")]
    public SpriteRenderer[] quarters = new SpriteRenderer[4];
    public MraEnding ending;
    public Color asleep = new Color(.35f, .3f, .3f, .35f), awake = new Color(1f, .78f, .35f, .9f);

    public string Flag => FlagPrefix + sector;
    public bool IsAwake => GameSave.HasFlag(Flag);
    public static int AwakeCount() { int n = 0; foreach (var s in Sectors) if (GameSave.HasFlag(FlagPrefix + s)) n++; return n; }

    void Start() => Paint();

    public override bool Use()
    {
        if (IsAwake) return false;
        GameSave.SetFlag(Flag);
        Sfx.Play("Wheel_Turn", transform.position);
        if (Fx.Library != null) Fx.Leaves(transform.position + Vector3.up, 6, 1f);
        foreach (var q in FindObjectsByType<MraHeartQuarter>(FindObjectsSortMode.None)) q.Paint();
        int count = AwakeCount();
        if (count < Sectors.Length) TownHud.Toast(count == 1 ? "Far off, a quarter of the heart stirs." : $"{count} of the heart's four quarters are awake.");
        else if (ending != null && MraState.Revealed && !ending.Done) ending.Play();
        return true;
    }

    // Light each awake quarter; the whole heart pulses once all four are.
    void Paint()
    {
        for (int i = 0; i < quarters.Length && i < Sectors.Length; i++)
            if (quarters[i] != null) quarters[i].color = GameSave.HasFlag(FlagPrefix + Sectors[i]) ? awake : asleep;
    }

    protected override void Update()
    {
        base.Update();
        if (AwakeCount() < Sectors.Length || sector != "A") return;
        float k = .75f + .25f * Mathf.Sin(Time.time * 2.2f);
        foreach (var q in quarters) if (q != null) q.color = new Color(awake.r, awake.g, awake.b, awake.a * k);
    }

    protected override string PromptText => IsAwake ? "" : "▲ Wake the heart";
}
