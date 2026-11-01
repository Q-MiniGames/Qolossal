using UnityEngine;
using UnityEngine.SceneManagement;

// The underground's surface entrances, added to their region scenes when those load (the first
// phase does this at run time, so the regional scenes and their builder stay untouched): each is a
// doorway on the ground (MraUgPortal, Up to go down) and the named landing beside it where every
// way back up from that route arrives (an MraArrival with the entrance's return id), both at the
// positions surveyed for it. Read from Resources/MRA/UgSurfaceEntrances (written by the builder).
public static class MraUgSurface
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
    }

    static MraUgSurfaceSet set;
    static MraUgSurfaceSet Set => set != null ? set : set = Resources.Load<MraUgSurfaceSet>("MRA/UgSurfaceEntrances");

    static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Set == null) return;
        foreach (var e in Set.entrances)
            if (e.scene == scene.name) Place(scene, e);
        foreach (var m in Set.caveMoves)
            if (m.scene == scene.name) Move(scene, m);
        foreach (var r in Set.redirects)
            if (r.scene == scene.name) Redirect(scene, r);
    }

    // A region exit or a side cave's way out, pointed at its new destination.
    static void Redirect(Scene scene, MraUgSurfaceSet.Redirect r)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            foreach (var exit in go.GetComponentsInChildren<MraExit>(true))
                if (exit.targetScene == r.fromScene) { exit.targetScene = r.toScene; exit.arrivalId = r.toArrival; if (!string.IsNullOrEmpty(r.label)) exit.destinationName = r.label; }
            foreach (var back in go.GetComponentsInChildren<MraChamberReturn>(true))
                if (back.parentScene == r.fromScene) { back.parentScene = r.toScene; back.returnId = r.toArrival; if (!string.IsNullOrEmpty(r.label)) back.label = r.label; }
        }
    }

    // The grant migration, in a region scene: the cave's door and return landing to their new pad,
    // the old shrine of the same ability out of the way. Identities are unchanged, so discovery,
    // rewards and old saves carry over; a relic already owned stays owned.
    static void Move(Scene scene, MraUgSurfaceSet.CaveMove m)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            foreach (var door in go.GetComponentsInChildren<MraChamberDoor>(true))
                if (door.chamberId == m.chamberId) door.transform.position = new Vector3(m.feet.x, m.feet.y, door.transform.position.z);
            foreach (var landing in go.GetComponentsInChildren<MraArrival>(true))
                if (landing.arrivalId == m.returnId) landing.transform.position = new Vector3(m.returnFeet.x, m.returnFeet.y + m.feetToCentre, landing.transform.position.z);
            foreach (var shrine in go.GetComponentsInChildren<AbilityShrine>(true))
                if (shrine.ability != null && shrine.ability.abilityId == m.ability) shrine.gameObject.SetActive(false);
        }
    }

    public const string RootName = "Underground entrances (run time)";

    static void Place(Scene scene, MraUgSurfaceSet.Entrance e)
    {
        GameObject root = null;
        foreach (var go in scene.GetRootGameObjects()) if (go.name == RootName) root = go;
        if (root == null) { root = new GameObject(RootName); SceneManager.MoveGameObjectToScene(root, scene); }

        var door = new GameObject("Entrance " + e.id);
        door.transform.SetParent(root.transform, false); door.transform.position = e.feet;
        var trigger = door.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true; trigger.size = e.triggerSize; trigger.offset = new Vector2(0f, e.triggerSize.y * .5f);
        var portal = door.AddComponent<MraUgPortal>();
        portal.targetScene = e.targetScene; portal.arrivalId = e.targetArrival;
        portal.label = e.label; portal.requires = e.requires; portal.closedText = e.closedText;
        portal.discoveredFlag = e.discoveredFlag; portal.discoveredText = e.discoveredText;
        portal.promptHeight = e.promptHeight;
        if (e.art != null)
        {
            var r = new GameObject("Entrance art").AddComponent<SpriteRenderer>();
            r.transform.SetParent(door.transform, false);
            r.transform.position = e.feet + e.artOffset; r.transform.localScale = new Vector3(e.artScale, e.artScale, 1f);
            r.sprite = e.art; r.sortingOrder = e.artOrder; r.color = e.artTint;
        }

        var landing = new GameObject("Landing " + e.returnId).AddComponent<MraArrival>();
        landing.transform.SetParent(root.transform, false);
        landing.transform.position = e.returnFeet + new Vector2(0f, e.feetToCentre);
        landing.arrivalId = e.returnId; landing.facing = e.returnFacing;
    }
}
