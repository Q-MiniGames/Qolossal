using UnityEngine;

[CreateAssetMenu(menuName="Qolossal/Combat/Weapon")]
public sealed class WeaponDefinition : ScriptableObject
{
    public string weaponId="reedblade",displayName="Reedblade";
    public CombatMoveSet moveSet;
    [Min(0)] public float damageMultiplier=1;
    [Tooltip("Optional replacement cutout. Put its pivot at the rear hand grip; reuse Qori's two-hand rig.")]
    public Sprite weaponArtwork;
    public Vector2 artworkTip=new Vector2(5.33f,-1.19f);
    [Min(.01f)] public float artworkScale=1;
    public Color trailColor=new Color(.9f,.96f,.66f,1);
}
