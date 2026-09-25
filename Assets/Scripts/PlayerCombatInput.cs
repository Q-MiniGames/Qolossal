using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerCombatInput : MonoBehaviour
{
    [Range(.2f,1)] public float verticalThreshold=.55f;
    InputAction attack;PlayerCombat combat;PlayerMovement movement;
    QoriBodyRig rig;
    void Awake()
    {
        combat=GetComponent<PlayerCombat>();movement=GetComponent<PlayerMovement>();
        attack=new InputAction("Attack",InputActionType.Button);
        attack.AddBinding("<Mouse>/leftButton");attack.AddBinding("<Gamepad>/leftShoulder");
    }
    void OnEnable()=>attack.Enable();
    void OnDisable()=>attack.Disable();
    void OnDestroy()=>attack.Dispose();
    void Update()
    {
        if(GamePauseMenu.BlocksGameplayInput||QoriArmory.BlocksAttackInput||Time.timeScale<=0||!attack.WasPressedThisFrame())return;
        Keyboard key=Keyboard.current;Gamepad pad=Gamepad.current;
        bool up=key!=null&&(key.wKey.isPressed||key.upArrowKey.isPressed);
        bool down=key!=null&&(key.sKey.isPressed||key.downArrowKey.isPressed);
        if(pad!=null){float y=pad.leftStick.y.ReadValue();up|=pad.dpad.up.isPressed||y>=verticalThreshold;down|=pad.dpad.down.isPressed||y<=-verticalThreshold;}
        AttackAim aim=up&&!down?AttackAim.Up:down&&!up&&!movement.IsGrounded?AttackAim.Down:AttackAim.Front;
        float horizontal=key==null?0:(key.dKey.isPressed||key.rightArrowKey.isPressed?1:0)-(key.aKey.isPressed||key.leftArrowKey.isPressed?1:0);
        if(horizontal==0&&pad!=null)horizontal=Mathf.Abs(pad.dpad.x.ReadValue())>.2f?pad.dpad.x.ReadValue():pad.leftStick.x.ReadValue();
        if(rig==null)rig=GetComponentInChildren<QoriBodyRig>();
        float neutralFacing=rig!=null&&rig.BodyActive&&rig.Swing!=null&&rig.Swing.PreserveReleaseFacing?rig.VisualFacing:movement.FacingDirection;
        float facing=Mathf.Abs(horizontal)>.2f?Mathf.Sign(horizontal):neutralFacing;
        combat.RequestAttack(new AttackRequest(aim,facing,CombatMoveSlot.Front));
    }
}

