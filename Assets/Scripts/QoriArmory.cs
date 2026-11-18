using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public sealed class QoriArmory:MonoBehaviour
{
    public static bool BlocksAttackInput=>active!=null&&(active.IsAiming||Time.frameCount<=active.closeFrame);
    static QoriArmory active;
    public WeaponDefinition[] Weapons {get;private set;}
    public WeaponDefinition SlingWeapon {get;private set;}
    public QoriArmoryVisual Visual {get;private set;}
    public Vector2 SlingDirection {get;private set;}=Vector2.right;
    public bool IsAiming {get;private set;}
    bool controllerAim;int aimReset;
    public int Selected {get;private set;}
    PlayerCombat combat;PlayerMovement movement;
    int closeFrame=-1,pending=-1;
    readonly List<Object> owned=new List<Object>();
    AttackDefinition sling;
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
    // Qori carries only the staff in the game (the weapon menu was removed, 3 Nov); the other weapons stay as data for tests.
    public bool Select(int index)
    {
        if(Weapons==null||index<0||index>=Weapons.Length)return false;
        if(IsAiming)CancelAim();
        if(combat.IsAttackPoseActive){pending=index;return false;}
        combat.EquipWeapon(Weapons[index]);Selected=index;pending=-1;return true;
    }
    public bool Fire(Vector2 direction)
    {
        if(sling==null||GamePauseMenu.BlocksGameplayInput||Time.timeScale<=0||!movement.isActiveAndEnabled||combat.IsAttackPoseActive)return false;
        SlingDirection=direction.sqrMagnitude>.01f?direction.normalized:Vector2.right*movement.FacingDirection;
        return combat.RequestAttack(new AttackRequest(AttackAim.Front,Mathf.Abs(SlingDirection.x)>.05f?SlingDirection.x:movement.FacingDirection,CombatMoveSlot.Special),sling);
    }
    public bool BeginAim(Vector2 direction)
    {
        if(sling==null||Time.timeScale<=0||IsAiming)return false;
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
        if(GamePauseMenu.BlocksGameplayInput||!movement.isActiveAndEnabled){CancelAim();return;}
        if(Time.timeScale<=0){CancelAim();return;}
        if(pending>=0&&!combat.IsAttackPoseActive)Select(pending);
        var key=Keyboard.current;var pad=Gamepad.current;
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

    void OnApplicationFocus(bool focused){if(!focused)CancelAim();}
    void OnDisable(){CancelAim();if(active==this)active=null;WeaponTuning.Changed-=Retune;}
    void OnEnable(){active=this;WeaponTuning.Changed+=Retune;}
    // The tuning asset was edited (Inspector, Play mode): the weapons in hand take the new numbers.
    void Retune()=>QoriArmoryFactory.ApplyTuning(Weapons,sling);
    void OnDestroy(){if(active==this)active=null;foreach(var item in owned)if(item!=null)Destroy(item);}
}
