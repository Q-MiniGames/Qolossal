using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
/// <summary>Review-scene-only mode switch. Never changes the player prefab or movement tuning.</summary>
public sealed class QolossalRoomTraversalMode:MonoBehaviour
{
 public GameObject playerRoot,flightRoot;
 public Camera reviewCamera;
 public CameraFollow follow;
 public bool CharacterMode {get;private set;}=true;
 InputAction toggle,reset;
 UnityEngine.UI.Text guide;
 void Awake()
 {
  toggle=new InputAction("Room test mode",InputActionType.Button,"<Keyboard>/f2");toggle.AddBinding("<Gamepad>/select");
  reset=new InputAction("Room test reset",InputActionType.Button,"<Keyboard>/home");
  var c=new GameObject("Room test guide",typeof(Canvas),typeof(CanvasScaler));c.transform.SetParent(transform,false);c.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;c.GetComponent<Canvas>().sortingOrder=200;
  var scale=c.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=.5f;
  var p=new GameObject("Guide",typeof(RectTransform),typeof(UnityEngine.UI.Image));p.transform.SetParent(c.transform,false);p.GetComponent<UnityEngine.UI.Image>().color=new Color(.02f,.03f,.04f,.9f);p.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
  var r=p.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,0);r.anchoredPosition=new Vector2(20,20);r.sizeDelta=new Vector2(1180,65);
  var t=new GameObject("Controls",typeof(RectTransform),typeof(UnityEngine.UI.Text));t.transform.SetParent(p.transform,false);guide=t.GetComponent<UnityEngine.UI.Text>();guide.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");guide.fontSize=18;guide.color=Color.white;guide.raycastTarget=false;
  var tr=t.GetComponent<RectTransform>();tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(12,8);tr.offsetMax=new Vector2(-12,-8);
  SetCharacterMode(true);
 }
 void OnEnable(){toggle?.Enable();reset?.Enable();}
 void OnDisable(){toggle?.Disable();reset?.Disable();}
 void OnDestroy(){toggle?.Dispose();reset?.Dispose();}
 void Update(){if(toggle.WasPressedThisFrame())SetCharacterMode(!CharacterMode);if(CharacterMode&&reset.WasPressedThisFrame())playerRoot.GetComponent<PlayerMovement>().PlaceAt(new Vector2(52,-173.3f),1,true);}
 public void SetCharacterMode(bool character)
 {
  CharacterMode=character;flightRoot.SetActive(!character);playerRoot.SetActive(character);follow.enabled=character;
  if(character){reviewCamera.orthographicSize=5;follow.Configure(playerRoot.transform);}
  guide.text=character?"WARM CRACK / CHARACTER TEST   |   A/D or stick: move   |   Space / South button: jump   |   Shift: sprint\nF2 / Select button: inspect in flight   |   Home: reset   |   Room pilot only":"WARM CRACK / FLIGHT INSPECTION   |   F2 / Select button: return to character   |   R: gameplay zoom";
 }
}

