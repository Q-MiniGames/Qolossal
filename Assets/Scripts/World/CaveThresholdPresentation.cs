using System.Collections;
using UnityEngine;

// Opt-in presentation on the existing portal. IDs, unlock rules and collision stay authoritative.
public sealed class CaveThresholdPresentation : MonoBehaviour
{
    public CaveConnectionPoint connection;
    public Vector2 mouth;
    public float inward = 1f;
    [Tooltip("Distance inside the mouth used for the walk-out presentation.")]
    public float emergenceInset = .65f;
    public float emergenceSeconds = 2.5f;
    [Tooltip("Authored floor height change from the arrival to the concealed start, for sloped passages.")]
    public float emergenceFloorRise;
    public static int Departures { get; private set; }
    public static int Emergences { get; private set; }
    public static int ArrivalFallbacks { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCounts() { Departures=Emergences=ArrivalFallbacks=0; }

    public IEnumerator StepIn(PlayerMovement player)
    {
        if (player == null) yield break;
        Departures++;
        float start=player.transform.position.x;
        try {
            for(float t=0;t<.38f;t+=Time.unscaledDeltaTime) {
                player.TransitionWalkInput=(player.transform.position.x-start)*inward < .42f ? inward*.6f : 0f;
                yield return null;
            }
        } finally { if(player!=null)player.TransitionWalkInput=null; }
    }

    public static CaveThresholdPresentation AtArrival(IArrivalPoint arrival)
    {
        var component=arrival as Component;if(component==null)return null;
        foreach(var p in FindObjectsByType<CaveThresholdPresentation>(FindObjectsSortMode.None))
            if(p.connection!=null && p.connection.arrival==component)return p;
        return null;
    }

    // Start just inside the mouth, then use the normal movement/grounding code to emerge.
    public bool PrepareEmergence(PlayerMovement player,Vector2 destination)
    {
        float fromArrival=destination.x+inward*.8f;
        float fromMouth=mouth.x+inward*emergenceInset;
        float x=inward>0?Mathf.Max(fromArrival,fromMouth):Mathf.Min(fromArrival,fromMouth);
        if(Mathf.Abs(x-destination.x)>Mathf.Max(4f,emergenceInset+Mathf.Abs(mouth.x-destination.x)+.5f))return false;
        var hit=Physics2D.Raycast(new Vector2(x,destination.y+.15f+emergenceFloorRise),Vector2.down,3f+Mathf.Abs(emergenceFloorRise),LayerMask.GetMask("Ground"));
        if(hit.collider==null)return false;
        player.PlaceAt(hit.point+Vector2.up*.66f,-inward,false);
        return true;
    }

    public IEnumerator Emerge(PlayerMovement player,Vector2 destination)
    {
        Emergences++;
        try {
            for(float t=0;t<emergenceSeconds;t+=Time.unscaledDeltaTime) {
                if(player==null)yield break;
                float dx=destination.x-player.transform.position.x;
                if(Mathf.Abs(dx)<.12f && player.HasGroundContact)break;
                player.TransitionWalkInput=Mathf.Clamp(dx*2f,-.85f,.85f);
                yield return null;
            }
            if(player!=null && Vector2.Distance(player.transform.position,destination)>.65f) {
                ArrivalFallbacks++;
                player.PlaceAt(destination,-inward,false);
            }
        } finally { if(player!=null){player.TransitionWalkInput=null;player.GetComponent<PlayerHealth>()?.ProtectCaveExit();} }
    }
}


