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
    static readonly Key[] SelectionKeys={Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4};
    static string WeaponStyle(string id)=>id switch
    {
        "forest-0"=>"Balanced sweeping cuts",
        "forest-2"=>"Heavy overhead impact",
        "forest-3"=>"Long precise thrust",
        QoriArmoryFactory.StaffId=>"Charged sweeping cuts",
        _=>""
    };
    AttackDefinition sling;
    readonly InputAction toggleMenu=new InputAction("Armory",InputActionType.Button);
    void Awake(){toggleMenu.AddBinding("<Keyboard>/tab");toggleMenu.AddBinding("<Gamepad>/rightStickPress");}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics()=>active=null;
    void Start()
    {
        active=this;combat=GetComponent<PlayerCombat>();movement=GetComponent<PlayerMovement>();
        Weapons=QoriArmoryFactory.Build(combat.EquippedWeapon,owned,out WeaponDefinition ranged,out AttackDefinition shot);
        SlingWeapon=ranged;sling=shot;
        // Qori starts with the Leaf Staff.
        int staff=System.Array.FindIndex(Weapons,w=>w.weaponId==QoriArmoryFactory.StaffId);
        Selected=Mathf.Max(0,staff);combat.EquipWeapon(Weapons[Selected]);
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
        if(toggleMenu.WasPressedThisFrame())
        {CancelAim();menuOpen=!menuOpen;closeFrame=Time.frameCount;}
        if(key!=null)
        {
            for(int i=0;i<SelectionKeys.Length&&i<Weapons.Length;i++)if(key[SelectionKeys[i]].wasPressedThisFrame){Select(i);menuOpen=false;closeFrame=Time.frameCount;}
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
        if(Weapons==null||GamePauseMenu.IsPaused||ModalUi.IsOpen||ListeningSpot.IsSitting)return;   // the listening retreat shows only its track list
        var prior=GUI.matrix;float scale=Mathf.Min(Screen.width/1000f,Screen.height/650f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        float width=Screen.width/scale;
        var safe = Screen.safeArea;
        var hint = new Rect(safe.xMax / scale - 328f, (Screen.height - safe.yMax) / scale + 18f, 310f, 58f);
        var route = MraRouteBar.Current;
        if (route != null && route.Showing)
        {
            Rect routePixels = route.ScreenBounds;
            var hintPixels = new Rect(hint.x * scale, hint.y * scale, hint.width * scale, hint.height * scale);
            if (hintPixels.Overlaps(routePixels)) hint.y = routePixels.yMax / scale + 12f;
        }
        if (!menuOpen) GUI.Box(hint,Weapons[Selected].displayName+(pending>=0?" → "+Weapons[pending].displayName:"")+"\nTab / RS click: armory     Hold Q / X: aim; release: fire");
        if(menuOpen)
        {
            float menuWidth=Weapons.Length*170f;float x=(width-menuWidth)/2,y=96;
            if(route!=null&&route.Showing)y=Mathf.Max(y,route.ScreenBounds.yMax/scale+54f);
            GUI.Box(new Rect(x-12,y-42,menuWidth+24,218),"FOREST ARMORY - 1-4 or click - right stick cycles - Tab / RS click closes");
            for(int i=0;i<Weapons.Length;i++)
            {
                var rect=new Rect(x+i*170,y,160,125);var old=GUI.backgroundColor;
                GUI.backgroundColor=i==Selected?new Color(.6f,.8f,.35f):Color.white;
                if(GUI.Button(rect,(i+1)+"  "+Weapons[i].displayName+"\n\n"+WeaponStyle(Weapons[i].weaponId))){Select(i);menuOpen=false;closeFrame=Time.frameCount;}
                GUI.backgroundColor=old;
            }
            GUI.Label(new Rect(x,y+132,830,32),"Resin sling is always equipped • unlimited seeds • 0.35 damage • mouse / right stick aims");
        }
        GUI.matrix=prior;
    }
    void OnApplicationFocus(bool focused){if(!focused)CancelAim();}
    void OnDisable(){toggleMenu.Disable();CancelAim();menuOpen=false;if(active==this)active=null;WeaponTuning.Changed-=Retune;}
    void OnEnable(){toggleMenu.Enable();active=this;WeaponTuning.Changed+=Retune;}
    // The tuning asset was edited (Inspector, Play mode): the weapons in hand take the new numbers.
    void Retune()=>QoriArmoryFactory.ApplyTuning(Weapons,sling);
    void OnDestroy(){toggleMenu.Dispose();if(active==this)active=null;foreach(var item in owned)if(item!=null)Destroy(item);}
}
