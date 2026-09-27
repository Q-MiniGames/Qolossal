#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed class QoriArmoryValidation:MonoBehaviour
{
    const string Dir="C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Armory/Review";
    [MenuItem("Qolossal/Armory/Run validation")]
    static void Run()
    {
        if(EditorApplication.isPlaying||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        foreach(string name in new[]{"SlingPouch","ResinSeed"})
        {
            var art=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Armory/"+name+".png");art.textureType=TextureImporterType.Default;art.alphaIsTransparency=true;art.mipmapEnabled=false;art.isReadable=true;art.npotScale=TextureImporterNPOTScale.None;art.textureCompression=TextureImporterCompression.Uncompressed;art.maxTextureSize=4096;art.SaveAndReimport();
        }
        var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Armory/Weapons.png");
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"));player.transform.position=new Vector3(0,1,0);
        var floor=new GameObject("Test floor");floor.layer=LayerMask.NameToLayer("Ground");floor.transform.position=new Vector3(0,-1.5f,0);floor.AddComponent<BoxCollider2D>().size=new Vector2(100,1);
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.gameObject.AddComponent<AudioListener>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=2.2f;camera.transform.position=new Vector3(0,.3f,-10);camera.backgroundColor=new Color(.12f,.18f,.16f);camera.clearFlags=CameraClearFlags.SolidColor;
        new GameObject("Armory validation").AddComponent<QoriArmoryValidation>();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/ArmoryValidation.unity");EditorApplication.EnterPlaymode();
    }
    [MenuItem("Qolossal/Armory/Open game")]
    static void Open(){if(!EditorApplication.isPlaying&&EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene("Assets/Scenes/OpeningLevel.unity");}
    readonly List<string> results=new List<string>();
    void Check(bool value,string name)=>results.Add((value?"PASS ":"FAIL ")+name);
    IEnumerator Start()
    {
        Directory.CreateDirectory(Dir);yield return new WaitForSeconds(1);
        var combat=FindFirstObjectByType<PlayerCombat>();var armory=combat.GetComponent<QoriArmory>();var rig=combat.GetComponentInChildren<QoriBodyRig>();
        combat.GetComponent<PlayerCombatInput>().enabled=false;armory.enabled=false;
        Check(armory.Weapons.Length==3,"Three melee weapons initialized");
        for(int i=0;i<3;i++)
        {
            combat.CancelAttack();armory.Select(i);yield return null;
            Check(combat.EquippedWeapon.weaponArtwork!=null,"Artwork loaded: "+combat.EquippedWeapon.displayName);
            Check(combat.EquippedWeapon.moveSet.Find(CombatMoveSlot.Front).damage>.35f,"Melee exceeds sling damage: "+i);
            for(int side=0;side<2;side++)
            {
                var victim=new GameObject("Weapon test target").AddComponent<ArmoryProbe>();victim.combat=combat;victim.rig=rig;victim.armory=armory;
                victim.gameObject.AddComponent<CircleCollider2D>().isTrigger=true;victim.gameObject.AddComponent<CircleCollider2D>().isTrigger=true;
                foreach(var c in victim.GetComponents<CircleCollider2D>())c.radius=.09f;
                combat.RequestAttack(new AttackRequest(AttackAim.Front,side==0?1:-1,CombatMoveSlot.Front));
                float deadline=Time.time+2;while(!combat.IsAttackPoseActive&&Time.time<deadline)yield return null;
                bool active=false;while(combat.IsAttackPoseActive&&Time.time<deadline)
                {
                    if(combat.Phase==AttackPhase.Startup)Check(victim.hits==0,"No startup damage "+i+" / "+side);
                    if(!active&&combat.Phase==AttackPhase.Active&&combat.PhaseProgress>.35f){active=true;Capture("Weapon-"+i+"-"+side);}
                    yield return null;
                }
                Check(active,"Reached active pose "+i+" / "+side);Check(victim.hits==1,"One hit across duplicate colliders "+i+" / "+side+": "+victim.hits);
                Check(Mathf.Abs(victim.damage-combat.EquippedWeapon.moveSet.Find(CombatMoveSlot.Front).damage)<.001f,"Correct melee damage "+i+" / "+side);
                Destroy(victim.gameObject);yield return new WaitForSeconds(.1f);
            }
            combat.CancelAttack();var equipped=combat.EquippedWeapon;
            Check(armory.Fire(Vector2.right),"Sling request with weapon "+i);
            yield return new WaitForSeconds(.42f);Check(FindObjectsByType<QoriResinShot>(FindObjectsSortMode.None).Length>0,"Sling launched with weapon "+i);
            Check(combat.EquippedWeapon==equipped,"Sling preserves melee selection "+i);Capture("Sling-"+i);
            yield return new WaitForSeconds(1.3f);
        }
        for(int i=0;i<3;i++)
        {
            combat.CancelAttack();armory.Select(i);combat.RequestAttack(new AttackRequest(AttackAim.Up,1,CombatMoveSlot.Upper));
            yield return new WaitForSeconds(.15f);Check(combat.CurrentAttack!=null&&combat.CurrentAttack.direction==AttackAim.Up,"Upper attack selection "+i);
            yield return new WaitForSeconds(1.2f);
            var body=combat.GetComponent<Rigidbody2D>();body.position=new Vector2(0,5);body.linearVelocity=Vector2.zero;yield return new WaitForFixedUpdate();yield return null;
            combat.CancelAttack();combat.RequestAttack(new AttackRequest(AttackAim.Down,-1,CombatMoveSlot.Lower));yield return new WaitForSeconds(.15f);
            Check(combat.CurrentAttack!=null&&combat.CurrentAttack.direction==AttackAim.Down,"Airborne lower selection "+i);
            yield return new WaitForSeconds(1.3f);
        }
        combat.CancelAttack();armory.Select(1);combat.RequestAttack(new AttackRequest(AttackAim.Front,1,CombatMoveSlot.Front));yield return new WaitForSeconds(.1f);
        Check(!armory.Select(2)&&combat.EquippedWeapon==armory.Weapons[1],"Selection defers through current attack");
        armory.enabled=true;yield return new WaitForSeconds(1.3f);Check(armory.Selected==2,"Deferred selection applies after recovery");
        var keyboard=InputSystem.AddDevice<Keyboard>();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));yield return null;yield return null;
        Check(armory.MenuOpen&&QoriArmory.BlocksAttackInput,"Tab opens menu and blocks attack input");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Digit2));yield return null;yield return null;
        Check(armory.Selected==1&&!armory.MenuOpen,"Number key selects mace and closes menu");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return new WaitForSeconds(1.3f);
        combat.CancelAttack();
        Check(QoriArmoryArt.Get("ResinSeed")!=null&&QoriArmoryArt.Get("SlingPouch")!=null,"All painted ranged assets load");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q));yield return null;yield return new WaitForSeconds(1.1f);
        Check(armory.IsAiming&&combat.Phase==AttackPhase.Startup&&QoriArmory.BlocksAttackInput,"Q holds loaded pose beyond normal windup");
        Check(FindObjectsByType<QoriResinShot>(FindObjectsSortMode.None).Length==0,"Holding aim never fires a seed");Capture("Sling-Hold");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return new WaitForSeconds(.16f);
        Check(!armory.IsAiming&&FindObjectsByType<QoriResinShot>(FindObjectsSortMode.None).Length==1,"Releasing Q fires exactly one seed");Capture("Sling-Release");
        yield return new WaitForSeconds(1.4f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q));yield return null;yield return new WaitForSeconds(.35f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q,Key.Tab));yield return null;yield return null;
        Check(!armory.IsAiming&&armory.MenuOpen&&combat.CurrentAttack==null,"Opening armory cancels held aim");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));yield return null;yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q));yield return null;yield return new WaitForSeconds(.35f);
        Time.timeScale=0;yield return null;yield return null;Check(!armory.IsAiming,"Pause cancels held aim");Time.timeScale=1;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return new WaitForSeconds(.5f);
        Check(FindObjectsByType<QoriResinShot>(FindObjectsSortMode.None).Length==0,"Cancelled aim does not fire on later release");
        InputSystem.RemoveDevice(keyboard);armory.enabled=false;
        var target=new GameObject("Sling target").AddComponent<ArmoryProbe>();target.transform.position=new Vector3(2,2,0);
        target.gameObject.AddComponent<CircleCollider2D>().radius=.3f;target.gameObject.AddComponent<CircleCollider2D>().radius=.3f;
        var shot=armory.SlingWeapon.moveSet.Find(CombatMoveSlot.Front);
        var ranged=ScriptableObject.CreateInstance<AttackDefinition>();ranged.damage=.35f;ranged.projectileSpeed=25;ranged.projectileLifetime=1;
        QoriResinShot.Launch(combat,new Vector2(0,2),ranged,armory.SlingWeapon,Vector2.right);yield return new WaitForSeconds(.2f);
        Check(target.hits==1&&Mathf.Abs(target.damage-.35f)<.001f,"Fractional ranged damage; duplicate colliders hit once");
        target.hits=0;target.damage=0;var wall=new GameObject("Projectile wall");wall.transform.position=new Vector3(1,2,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(.05f,2);Physics2D.SyncTransforms();
        QoriResinShot.Launch(combat,new Vector2(0,2),ranged,armory.SlingWeapon,Vector2.right);yield return new WaitForSeconds(.25f);Check(target.hits==0,"Fast projectile blocked by thin terrain");
        Destroy(wall);Destroy(target.gameObject);Destroy(ranged);
        combat.CancelAttack();armory.Select(0);armory.enabled=true;combat.GetComponent<PlayerCombatInput>().enabled=true;
        File.WriteAllLines(Path.Combine(Dir,"Validation.txt"),results);Debug.Log("ARMORY VALIDATION FINISHED: "+string.Join(" | ",results.FindAll(x=>x.StartsWith("FAIL"))));
    }
    void Capture(string name)
    {
        var cam=Camera.main;var rt=new RenderTexture(800,500,24);rt.Create();
        RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
        var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(800,500,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,500),0,0);image.Apply();RenderTexture.active=previous;File.WriteAllBytes(Path.Combine(Dir,name+".png"),image.EncodeToPNG());Destroy(image);rt.Release();Destroy(rt);
    }
}
[DefaultExecutionOrder(17)]public sealed class ArmoryProbe:MonoBehaviour,ICombatDamageReceiver
{
    public PlayerCombat combat;public QoriBodyRig rig;public QoriArmory armory;public int hits;public float damage;
    void LateUpdate(){if(rig==null)return;transform.position=rig.GetJoint("WeaponTip").position;Physics2D.SyncTransforms();}
    public CombatDamageResponse ReceiveCombatHit(CombatDamage hit){hits++;damage+=hit.Damage;return CombatDamageResponse.Applied(hit.Damage);}
}
#endif
