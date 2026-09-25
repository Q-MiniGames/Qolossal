using UnityEngine;

// Adds a small presentation offset after CameraFollow, restoring it before the next follow update.
[DefaultExecutionOrder(1000),DisallowMultipleComponent]
public sealed class CombatCameraShake : MonoBehaviour
{
    PlayerCombatFeedback feedback;Vector3 applied;float until,amplitude;
    public void Initialize(PlayerCombatFeedback source)
    {
        if(feedback!=null)feedback.OnCameraImpulse-=Impulse;feedback=source;if(feedback!=null)feedback.OnCameraImpulse+=Impulse;
    }
    void Impulse(Vector2 point,float strength){amplitude=Mathf.Max(amplitude,Mathf.Clamp(strength,0,.08f));until=Time.unscaledTime+.09f;}
    void Update(){transform.position-=applied;applied=Vector3.zero;if(Time.timeScale<=0)until=0;}
    void LateUpdate()
    {
        float remaining=Mathf.Clamp01((until-Time.unscaledTime)/.09f);if(remaining<=0){amplitude=0;return;}
        float phase=Time.unscaledTime*150;applied=new Vector3(Mathf.Sin(phase),Mathf.Sin(phase*1.31f),0)*amplitude*remaining;transform.position+=applied;
    }
    void OnDisable(){transform.position-=applied;applied=Vector3.zero;until=amplitude=0;if(feedback!=null)feedback.OnCameraImpulse-=Impulse;}
}
