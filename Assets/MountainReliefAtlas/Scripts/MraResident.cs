using System.Collections.Generic;
using UnityEngine;

// Someone trapped in a side chamber (the smith, the farmer, the lantern keeper, a traveller, the last
// resident). Talking to them with Up rescues them: the rescue is saved once (its reward id), the
// town flag resident:<role> is set, and their home and service open in Qvale (MraVariant / the
// shop's availability). After that they only say goodbye.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraResident : MonoBehaviour
{
    public string rewardId, role, displayName;
    [TextArea] public string[] rescueLines = new string[0];
    [TextArea] public string[] laterLines = { "I'll see you in Qvale." };
    public Transform figure;

    bool near; Transform qori;
    public bool Rescued => GameSave.HasFlag(rewardId);

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    void OnTriggerEnter2D(Collider2D other) { if (MraState.IsQori(other)) { near = true; qori = other.attachedRigidbody.transform; } }
    void OnTriggerExit2D(Collider2D other) { if (MraState.IsQori(other)) near = false; }

    void Update()
    {
        if (figure != null && qori != null && near)
        {
            float side = Mathf.Sign(qori.position.x - transform.position.x);
            figure.localScale = new Vector3(Mathf.Abs(figure.localScale.x) * side, figure.localScale.y, figure.localScale.z);
        }
        if (near && !MraState.Busy && !GamePauseMenu.BlocksGameplayInput && TownInput.Up()) Talk();
    }

    // Up calls this; so can tests (Rescue applies at once, without waiting for the dialogue).
    public void Talk()
    {
        if (Rescued) { DialogueBox.Show(displayName, laterLines); return; }
        DialogueBox.Show(displayName, rescueLines, () => Rescue());
    }

    public bool Rescue()
    {
        if (!GameSave.SetFlag(rewardId)) return false;
        TownState.Set("resident:" + role);
        TownHud.Toast($"{displayName} is heading home to Qvale.", 5f);
        return true;
    }

    void OnGUI()
    {
        if (!MraState.Busy) MraState.Prompt(transform.position + Vector3.up * 2.6f, near ? displayName + "\n▲ Talk" : displayName, near);
    }
}
