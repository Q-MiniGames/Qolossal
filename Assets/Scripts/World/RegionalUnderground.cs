using System;
using UnityEngine;

// Normal gameplay in the approved continuous underground. No review-flight input is installed.
[DefaultExecutionOrder(-1500)]
public sealed class RegionalUnderground : MonoBehaviour
{
    [Serializable] public sealed class Room { public string id, name, group; public float x, y, w, h; }
    [Serializable] sealed class Layout { public Room[] rooms; }
    public TextAsset layout;
    public PlayerMovement player;
    public Camera gameplayCamera;
    public MraEnding ending;
    public Room CurrentRoom { get; private set; }
    Room[] rooms = Array.Empty<Room>();
    float nextCheck;

    void Awake()
    {
        if (layout != null) rooms = JsonUtility.FromJson<Layout>(layout.text).rooms;
        if (player == null) player = FindAnyObjectByType<PlayerMovement>();
        if (gameplayCamera == null) gameplayCamera = Camera.main;
        if (gameplayCamera != null)
        {
            gameplayCamera.orthographicSize = 5f;
            var follow = gameplayCamera.GetComponent<CameraFollow>();
            if (follow != null && player != null) follow.Configure(player.transform);
        }
    }

    void Update()
    {
        // The new hall has four real approaches; the story ending still follows the Summit reveal.
        if (ending != null) ending.enabled = MraState.Revealed;
        if (player == null || Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + .2f;
        Vector2 p = player.transform.position;
        foreach (var room in rooms)
        {
            if (!new Rect(room.x, room.y, room.w, room.h).Contains(p)) continue;
            if (CurrentRoom == room) return;
            CurrentRoom = room;
            string region = room.group == "A" ? "MR04" : room.group == "C" ? "MR05" :
                room.group == "D" ? "MR06" : room.group == "E" || room.group == "H" ? "MR07" : room.group;
            if (MraSession.Current != null) MraSession.Current.SetUndergroundRegion(region);
            GameSave.SetFlag("regional:room:" + room.id);
            if (room.id.StartsWith("MR", StringComparison.Ordinal)) GameSave.SetFlag("mra:" + room.id + ":discovered");
            return;
        }
    }
}
