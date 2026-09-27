using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public sealed class QoriArmory:MonoBehaviour
{
    public static bool BlocksAttackInput=>active!=null&&(active.menuOpen||active.IsAiming||Time.frameCount<=active.closeFrame);
    static QoriArmory active;
    public WeaponDefinition[] Weapons {get;private set;}
    public WeaponDefinition SlingWeapon {get;private set;}
    public QoriArmoryVisual Visual {get;private set;}
    public Vector2 SlingDirection {get;private set;}=Vector2.right;
    public bool IsAiming {get;private set;}
    bool controllerAim;int aimReset;
    public int Selected {get;private set;}
    public bool MenuOpen=>menuOpen;
    PlayerCombat combat;PlayerMovement movement;
    bool menuOpen;int closeFrame=-1,pending=-1;float nextNavigation;
    readonly List<Object> owned=new List<Object>();
    AttackDefinition sling;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics()=>active=null;
    void Start()
    {
        active=this;combat=GetComponent<PlayerCombat>();movement=GetComponent<PlayerMovement>();
        Weapons=QoriArmoryFactory.Build(combat.EquippedWeapon,owned,out WeaponDefinition ranged,out AttackDefinition shot);
        SlingWeapon=ranged;sling=shot;combat.EquipWeapon(Weapons[0]);
        Visual=gameObject.AddComponent<QoriArmoryVisual>();Visual.Initialize(this,combat);
    }
    public bool Select(int index)
    {
        if(Weapons==null||index<0||index>=Weapons.Length)return false;
        if(IsAiming)CancelAim();
        if(combat.IsAttackPoseActive){pending=index;return false;}
        combat.EquipWeapon(Weapons[index]);Selected=index;pending=-1;return true;
    }
    public bool Fire(Vector2 direction)
    {
        if(sling==null||menuOpen||GamePauseMenu.BlocksGameplayInput||Time.timeScale<=0||!movement.isActiveAndEnabled||combat.IsAttackPoseActive)return false;
        SlingDirection=direction.sqrMagnitude>.01f?direction.normalized:Vector2.right*movement.FacingDirection;
        return combat.RequestAttack(new AttackRequest(AttackAim.Front,Mathf.Abs(SlingDirection.x)>.05f?SlingDirection.x:movement.FacingDirection,CombatMoveSlot.Special),sling);
    }
    public bool BeginAim(Vector2 direction)
    {
        if(sling==null||menuOpen||Time.timeScale<=0||IsAiming)return false;
        SlingDirection=direction.sqrMagnitude>.01f?direction.normalized:Vector2.right*movement.FacingDirection;
        if(!combat.BeginSlingAim(sling,SlingDirection))return false;
        IsAiming=true;aimReset=movement.ResetVersion;return true;
    }
    public void ReleaseAim()
    {
        if(!IsAiming)return;
        IsAiming=false;combat.ReleaseSlingAim();closeFrame=Time.frameCount;
    }
    public void CancelAim()
    {
        if(IsAiming){IsAiming=false;combat.CancelAttack();}
    }
    Vector2 ReadAim(bool controller)
    {
        Vector2 aim=Vector2.right*movement.FacingDirection;
        if(controller)
        {
            var pad=Gamepad.current;
            return pad!=null&&pad.rightStick.ReadValue().sqrMagnitude>.16f?pad.rightStick.ReadValue().normalized:SlingDirection;
        }
        if(Mouse.current!=null&&Camera.main!=null)
        {
            var screen=Mouse.current.position.ReadValue();
            var world=Camera.main.ScreenToWorldPoint(new Vector3(screen.x,screen.y,Mathf.Abs(Camera.main.transform.position.z-transform.position.z)));
            aim=(Vector2)world-(Vector2)transform.position;
        }
        return aim.sqrMagnitude>.01f?aim.normalized:Vector2.right*movement.FacingDirection;
    }
    void Update()
    {
        if(Weapons==null)return;
        if(GamePauseMenu.BlocksGameplayInput||!movement.isActiveAndEnabled){CancelAim();menuOpen=false;return;}
        if(Time.timeScale<=0){CancelAim();return;}
        if(pending>=0&&!combat.IsAttackPoseActive)Select(pending);
        var key=Keyboard.current;var pad=Gamepad.current;
        if(key!=null&&key.tabKey.wasPressedThisFrame||pad!=null&&pad.selectButton.wasPressedThisFrame)
        {CancelAim();menuOpen=!menuOpen;closeFrame=Time.frameCount;}
        if(key!=null)
        {
            Key[] keys={Key.Digit1,Key.Digit2,Key.Digit3};
            for(int i=0;i<keys.Length;i++)if(key[keys[i]].wasPressedThisFrame){Select(i);menuOpen=false;closeFrame=Time.frameCount;}
        }
        if(menuOpen&&pad!=null&&Time.unscaledTime>nextNavigation&&Mathf.Abs(pad.rightStick.x.ReadValue())>.6f)
        {Select((Selected+(pad.rightStick.x.ReadValue()>0?1:Weapons.Length-1))%Weapons.Length);nextNavigation=Time.unscaledTime+.25f;}
        if(IsAiming)
        {
            if(aimReset!=movement.ResetVersion||combat.CurrentAttack!=sling||!combat.CanUse(sling)){CancelAim();return;}
            SlingDirection=ReadAim(controllerAim);
            bool held=controllerAim?pad!=null&&pad.buttonWest.isPressed:key!=null&&key.qKey.isPressed;
            if(!held)ReleaseAim();
        }
        else if(!BlocksAttackInput&&(key!=null&&key.qKey.wasPressedThisFrame||pad!=null&&pad.buttonWest.wasPressedThisFrame))
        {
            controllerAim=pad!=null&&pad.buttonWest.wasPressedThisFrame;
            if(controllerAim)SlingDirection=Vector2.right*movement.FacingDirection;
            BeginAim(ReadAim(controllerAim));
        }
    }

    void OnGUI()
    {
        if(Weapons==null||GamePauseMenu.IsPaused)return;
        var prior=GUI.matrix;float scale=Mathf.Min(Screen.width/1000f,Screen.height/650f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        float width=Screen.width/scale;
        GUI.Box(new Rect(width-328,18,310,58),Weapons[Selected].displayName+(pending>=0?" → "+Weapons[pending].displayName:"")+"\nTab / View: armory     Hold Q / X: aim; release: fire");
        if(menuOpen)
        {
            float x=(width-510)/2,y=96;
            GUI.Box(new Rect(x-12,y-42,534,218),"FOREST ARMORY - 1-3 or click - right stick cycles - Tab / View closes");
            string[] styles={"Balanced sweeping cuts","Heavy overhead impact","Long precise thrust"};
            for(int i=0;i<Weapons.Length;i++)
            {
                var rect=new Rect(x+i*170,y,160,125);var old=GUI.backgroundColor;
                GUI.backgroundColor=i==Selected?new Color(.6f,.8f,.35f):Color.white;
                if(GUI.Button(rect,(i+1)+"  "+Weapons[i].displayName+"\n\n"+styles[i])){Select(i);menuOpen=false;closeFrame=Time.frameCount;}
                GUI.backgroundColor=old;
            }
            GUI.Label(new Rect(x,y+132,830,32),"Resin sling is always equipped • unlimited seeds • 0.35 damage • mouse / right stick aims");
        }
        GUI.matrix=prior;
    }
    void OnApplicationFocus(bool focused){if(!focused)CancelAim();}
    void OnDisable(){CancelAim();menuOpen=false;if(active==this)active=null;}
    void OnEnable(){active=this;}
    void OnDestroy(){if(active==this)active=null;foreach(var item in owned)if(item!=null)Destroy(item);}
}
