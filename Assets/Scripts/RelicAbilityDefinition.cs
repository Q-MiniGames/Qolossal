using System.Linq;
using UnityEngine;

// A relic that unlocks a movement ability the movement scripts already own (the thread,
// the wall cling, the dash, the glide). It does nothing itself; PlayerThread and PlayerMovement
// ask Relics.Has.
[CreateAssetMenu(menuName="Qolossal/Abilities/Relic")]
public sealed class RelicAbilityDefinition : AbilityDefinition
{
    public override bool Activate(PlayerAbilityController owner)=>false;
}

// The relic abilities found at ability shrines. Their definitions live in Resources/Relics
// (built by Qolossal > Relics > Build Relics).
public static class Relics
{
    public const string LivingThread="living-thread", ClimbingMoss="climbing-moss", Bloomfall="bloomfall",
        WindLeaf="wind-leaf", Glidecap="glidecap";

    static AbilityDefinition[] all;
    public static AbilityDefinition[] All=>all!=null&&all.All(a=>a!=null)?all:all=Resources.LoadAll<AbilityDefinition>("Relics");

    // A player without an ability controller keeps every ability (older scenes and checks).
    public static bool Has(PlayerAbilityController abilities,string id)=>abilities==null||abilities.HasAbility(id);
}
