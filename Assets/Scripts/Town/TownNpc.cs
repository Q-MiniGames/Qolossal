using System;
using UnityEngine;

// A townsperson Qori can talk to: stand near and press Up. Their conversations are tried in order,
// and the first whose condition holds is spoken (TownState.Check: "calm", "quake", "flag:x" ...).
// A conversation marked `once` is spoken one time and then skipped, so later entries can be the
// repeat lines. A TownShop on the same object opens after the conversation.
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class TownNpc : MonoBehaviour
{
    [Serializable]
    public sealed class Talk
    {
        [Tooltip("TownState.Check condition; empty for always.")] public string when = "";
        [Tooltip("Spoken only the first time its condition holds.")] public bool once;
        [TextArea] public string[] lines = new string[0];
    }

    public string id = "npc";
    public string displayName = "Someone";
    public Talk[] talks = new Talk[0];
    [Tooltip("The figure, flipped to face Qori.")] public Transform figure;
    [Tooltip("Height of the name label above the feet.")] public float labelHeight = 2.2f;

    public int LastTalk { get; private set; } = -1;
    bool near; Transform qori;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    static Rigidbody2D QoriOf(Collider2D other) =>
        other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<PlayerMovement>() != null ? other.attachedRigidbody : null;
    void OnTriggerEnter2D(Collider2D other) { var q = QoriOf(other); if (q != null) { near = true; qori = q.transform; } }
    void OnTriggerExit2D(Collider2D other) { if (QoriOf(other) != null) near = false; }

    void Update()
    {
        if (figure != null && qori != null && near)
        {
            float side = Mathf.Sign(qori.position.x - transform.position.x);
            figure.localScale = new Vector3(Mathf.Abs(figure.localScale.x) * side, figure.localScale.y, 1f);
        }
        if (near && !ModalUi.IsOpen && !GamePauseMenu.IsPaused && TownInput.Up()) Interact();
    }

    // Speaks the current conversation (the Up key calls this; so can tests).
    public void Interact()
    {
        int pick = -1;
        for (int i = 0; i < talks.Length; i++)
        {
            var talk = talks[i];
            if (!TownState.Check(talk.when)) continue;
            if (talk.once && TownState.Has($"talk:{id}:{i}")) continue;
            pick = i; break;
        }
        var shop = GetComponent<TownShop>();
        if (pick < 0) { if (shop != null) shop.Open(); return; }
        LastTalk = pick;
        TownState.Set($"talk:{id}:{pick}");
        DialogueBox.Show(displayName, talks[pick].lines, shop != null ? (Action)shop.Open : null);
    }

    void OnGUI()
    {
        if (ModalUi.IsOpen || GamePauseMenu.IsPaused || ListeningSpot.IsSitting) return;
        var view = Camera.main; if (view == null) return;
        Vector3 p = view.WorldToScreenPoint(transform.position + Vector3.up * labelHeight);
        if (p.z < 0f) return;
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(Screen.height * .022f), fontStyle = FontStyle.Bold };
        style.normal.textColor = near ? new Color(1f, .9f, .6f) : new Color(1f, 1f, 1f, .75f);
        string text = near ? displayName + "\n▲ Talk" : displayName;
        GUI.Label(new Rect(p.x - 150f, Screen.height - p.y - 40f, 300f, 60f), text, style);
    }
}
