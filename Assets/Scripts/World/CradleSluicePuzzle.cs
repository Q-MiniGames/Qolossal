using UnityEngine;

// MR01_C02: close the spillway, then feed the pool. A solved pool stays filled on return.
public sealed class CradleSluicePuzzle : MonoBehaviour
{
    public const string SolvedFlag = "cradle:MR01_C02:pool-filled";
    public const string InletFlag = "cradle:MR01_C02:inlet-open";
    public const string DrainFlag = "cradle:MR01_C02:spillway-closed";
    public Transform pool;
    public Vector3 emptyScale = new Vector3(1, .12f, 1), fullScale = Vector3.one;
    public GameObject inletFlow, spillwayFlow;
    public SpriteRenderer inletImage, spillwayImage;
    public Sprite gateOpen, gateClosed;
    [Min(.2f)] public float fillSeconds = 3f;
    public SfxEmitter waterSound;
    public float Fill01 { get; private set; }
    public bool Filling => !Solved && InletOpen && SpillwayClosed;
    public bool Solved => GameSave.HasFlag(SolvedFlag);
    public bool InletOpen => GameSave.HasFlag(InletFlag);
    public bool SpillwayClosed => GameSave.HasFlag(DrainFlag);

    void OnEnable() { GameSave.FlagsChanged += Refresh; GameSave.WorldChanged += Reload; Reload(); }
    void OnDisable() { GameSave.FlagsChanged -= Refresh; GameSave.WorldChanged -= Reload; }

    void Reload()
    {
        Fill01 = Solved ? 1f : 0f;
        if (pool) pool.localScale = Vector3.Lerp(emptyScale, fullScale, Fill01);
        Refresh();
    }

    public bool Turn(bool inlet)
    {
        if (Solved) return false;
        string flag = inlet ? InletFlag : DrainFlag;
        if (GameSave.HasFlag(flag)) GameSave.ClearFlag(flag); else GameSave.SetFlag(flag);
        var source = inlet ? inletImage : spillwayImage;
        Sfx.Play("Lever_Pull", source ? source.transform.position : transform.position);
        if (Filling) TownHud.Toast("The water is rising. Follow the mossy ledges to the Heart Seed.", 5);
        else TownHud.Toast(InletOpen ? "The water escapes. Close the spillway on the ledge above." : "The inlet is closed. The pool needs a flowing inlet and a closed spillway.", 5);
        return true;
    }

    void Refresh()
    {
        if (inletFlow) inletFlow.SetActive(InletOpen || Solved);
        if (spillwayFlow) spillwayFlow.SetActive(InletOpen && !SpillwayClosed && !Solved);
        if (inletImage) inletImage.sprite = InletOpen || Solved ? gateOpen : gateClosed;
        if (spillwayImage) spillwayImage.sprite = SpillwayClosed || Solved ? gateClosed : gateOpen;
        if (waterSound) waterSound.Playing = InletOpen || Solved;
    }

    void Update()
    {
        if (GamePauseMenu.BlocksGameplayInput || AreaTransition.IsTransitioning) return;
        Fill01 = Mathf.MoveTowards(Fill01, Solved || Filling ? 1f : 0f, Time.deltaTime / Mathf.Max(.2f, fillSeconds));
        if (pool) pool.localScale = Vector3.Lerp(emptyScale, fullScale, Fill01);
        if (!Solved && Filling && Fill01 >= 1f)
        {
            GameSave.SetFlag(SolvedFlag);
            Sfx.Play("Gate_Open", pool ? pool.position : transform.position);
            TownHud.Toast("Moss Pocket restored. The Heart Seed is free on the highest ledge.", 5);
        }
    }
}
