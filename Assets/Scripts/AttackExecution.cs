using UnityEngine;

// One attack's clock; asset definitions remain immutable at runtime.
public sealed class AttackExecution
{
    public AttackDefinition Definition {get;private set;}
    public float Elapsed {get;private set;}
    public AttackPhase Phase {get;private set;}
    public float Progress {get;private set;}
    public bool Finished=>Definition!=null&&Elapsed>=Definition.TotalDuration;
    public void Begin(AttackDefinition attack){Definition=attack;Elapsed=0;Resolve();}
    public void Advance(float dt){Elapsed+=Mathf.Max(0,dt);Resolve();}
    void Resolve()
    {
        float startup=Mathf.Max(.001f,Definition.startup),active=Mathf.Max(.001f,Definition.active),recovery=Mathf.Max(.001f,Definition.recovery);
        if(Elapsed<startup){Phase=AttackPhase.Startup;Progress=Elapsed/startup;}
        else if(Elapsed<startup+active){Phase=AttackPhase.Active;Progress=(Elapsed-startup)/active;}
        else {Phase=AttackPhase.Recovery;Progress=Mathf.Clamp01((Elapsed-startup-active)/recovery);}
    }
}
