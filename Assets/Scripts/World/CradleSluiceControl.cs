using UnityEngine;

public sealed class CradleSluiceControl : MraUgInteract
{
    public CradleSluicePuzzle puzzle;
    public bool inlet;
    public Transform handle;
    public override bool Use() => puzzle && !LockedOut && !MraState.Busy && !GamePauseMenu.BlocksGameplayInput && puzzle.Turn(inlet);
    protected override string PromptText => !puzzle ? "" : puzzle.Solved ? "The pool is restored" : inlet
        ? (puzzle.InletOpen ? "▲ Close inlet" : "▲ Open inlet")
        : (puzzle.SpillwayClosed ? "▲ Open spillway" : "▲ Close spillway");
    protected override void Update()
    {
        base.Update();
        if (handle && puzzle) handle.localRotation = Quaternion.Euler(0, 0,
            (inlet ? puzzle.InletOpen : puzzle.SpillwayClosed) ? -25 : 25);
    }
}
