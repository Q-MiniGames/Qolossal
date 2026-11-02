using UnityEngine;

// The exit's existing save predicates also select its painted state.
[DisallowMultipleComponent]
public sealed class MraEntranceState : MonoBehaviour
{
    public MraExit exit;
    public SpriteRenderer artwork;
    public Sprite open, sealedState;
    public bool IsOpen => exit != null && MraState.Holds(exit.requires);

    void OnEnable() { GameSave.FlagsChanged += Refresh; GameSave.WorldChanged += Refresh; Refresh(); }
    void OnDisable() { GameSave.FlagsChanged -= Refresh; GameSave.WorldChanged -= Refresh; }
    public void Refresh()
    {
        if (artwork != null && open != null && sealedState != null)
            artwork.sprite = IsOpen ? open : sealedState;
    }
}
