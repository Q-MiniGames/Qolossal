using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Collision-free inspection cursor, confined to the dedicated layout scene.</summary>
[DisallowMultipleComponent]
public sealed class QolossalLayoutFlight : MonoBehaviour
{
    public Camera reviewCamera;
    public TextAsset layoutData;
    public Material markerMaterial;
    [Min(1)] public float normalSpeed=12;
    public float boostMultiplier=4, precisionMultiplier=.2f;
    [Serializable] public class Room { public string id,name; public float x,y,w,h; }
    [Serializable] class Layout { public Room[] rooms; }
    Room[] rooms;
    InputActionMap input;
    InputAction move,boost,precision,zoomAxis,scroll,normalZoom,map,heart,home,next,previous,help,click;
    LineRenderer[] borders;
    Transform marker;
    UnityEngine.UI.Text statusText,helpText;
    RectTransform helpPanel;
    Vector2 position;
    float zoom=5,lastStroke=-1,lastHud=-1;
    bool overview,showHelp=true;
    int roomIndex;
    public Vector2 Position => position;
    public float ViewSize => overview?760:zoom;
    public bool IsOverview => overview;
    public int RoomCount => rooms==null?0:rooms.Length;
    public int UpdateCount { get; private set; }
    public InputActionMap Actions => input;
    public float CurrentSpeed => Mathf.Min(2000,normalSpeed*Mathf.Max(1,ViewSize/5)*(boost.IsPressed()?boostMultiplier:1)*(precision.IsPressed()?precisionMultiplier:1));

    void Awake()
    {
        if(gameObject.scene.name!="Qolossal_WorldLayout_Flight" || reviewCamera==null || layoutData==null)
        { Debug.LogError("Layout flight needs its dedicated review scene, camera and data.");enabled=false;return; }
        rooms=JsonUtility.FromJson<Layout>(layoutData.text).rooms;
        borders=Array.FindAll(FindObjectsByType<LineRenderer>(FindObjectsSortMode.None),l=>l.gameObject.scene==gameObject.scene);
        ConfigureInput();CreateMarker();CreateHud();ReturnToStart();
    }
    InputAction Button(string name,params string[] bindings)
    {
        var a=input.AddAction(name,InputActionType.Button);
        foreach(var binding in bindings)a.AddBinding(binding);
        return a;
    }
    void ConfigureInput()
    {
        input=new InputActionMap("LayoutInspection");
        move=input.AddAction("Fly",InputActionType.Value);move.expectedControlType="Vector2";
        move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
        move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick");
        boost=Button("Fast","<Keyboard>/leftShift","<Keyboard>/rightShift","<Gamepad>/rightTrigger");
        precision=Button("Precise","<Keyboard>/leftCtrl","<Keyboard>/rightCtrl","<Gamepad>/leftTrigger");
        zoomAxis=input.AddAction("Zoom",InputActionType.Value);
        zoomAxis.AddCompositeBinding("1DAxis").With("Positive","<Keyboard>/equals").With("Negative","<Keyboard>/minus");
        zoomAxis.AddBinding("<Gamepad>/rightStick/y");
        scroll=input.AddAction("Scroll",InputActionType.Value,"<Mouse>/scroll/y");
        normalZoom=Button("NormalZoom","<Keyboard>/r","<Gamepad>/dpad/up");
        map=Button("Map","<Keyboard>/m","<Gamepad>/start");
        heart=Button("Heart","<Keyboard>/h","<Gamepad>/buttonNorth");
        home=Button("Start","<Keyboard>/home","<Gamepad>/buttonWest");
        next=Button("NextRoom","<Keyboard>/tab","<Gamepad>/dpad/right");
        previous=Button("PreviousRoom","<Keyboard>/backquote","<Gamepad>/dpad/left");
        help=Button("Help","<Keyboard>/f1","<Gamepad>/select");
        click=Button("MapTravel","<Mouse>/leftButton");
    }
    void OnEnable(){input?.Enable();}
    void OnDisable(){input?.Disable();}
    void OnDestroy(){input?.Dispose();}
    void Update()
    {
        UpdateCount++;
        float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
        if(map.WasPressedThisFrame())ToggleOverview();
        if(normalZoom.WasPressedThisFrame())SetZoom(5);
        if(heart.WasPressedThisFrame())VisitRoom("HEART");
        if(home.WasPressedThisFrame())ReturnToStart();
        if(next.WasPressedThisFrame())CycleRoom(boost.IsPressed()?-1:1);
        if(previous.WasPressedThisFrame())CycleRoom(-1);
        if(help.WasPressedThisFrame()){showHelp=!showHelp;helpText.gameObject.SetActive(showHelp);helpPanel.sizeDelta=new Vector2(1030,showHelp?132:40);}
        // Input System 1.19 normalizes a wheel notch to 1 by default.
        // Only legacy Windows platform-specific input uses 120 per notch.
        float wheel=scroll.ReadValue<float>();
        if(InputSystem.settings.scrollDeltaBehavior==InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange
            && (Application.platform==RuntimePlatform.WindowsEditor || Application.platform==RuntimePlatform.WindowsPlayer))wheel/=120f;
        float zoomSpeed=precision.IsPressed()?.25f:(boost.IsPressed()?2f:1f);
        float z=(zoomAxis.ReadValue<float>()*dt*3f+wheel*.32f)*zoomSpeed;
        if(Mathf.Abs(z)>.00001f)SetZoom(ViewSize*Mathf.Exp(-Mathf.Clamp(z,-1.5f,1.5f)));
        if(overview && click.WasPressedThisFrame() && Mouse.current!=null)
        {
            var target=reviewCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            TravelTo(target,5);
        }
        FlyStep(move.ReadValue<Vector2>(),dt,CurrentSpeed);
        ApplyView();
        if(Time.unscaledTime-lastHud>.12f){UpdateHud();lastHud=Time.unscaledTime;}
    }
    public void FlyStep(Vector2 direction,float dt,float speed)
    {
        position+=Vector2.ClampMagnitude(direction,1)*Mathf.Max(0,dt)*Mathf.Max(0,speed);
        position=new Vector2(Mathf.Clamp(position.x,-200,2700),Mathf.Clamp(position.y,-350,1000));
    }
    public void TravelTo(Vector2 point,float size)
    {
        position=new Vector2(Mathf.Clamp(point.x,-200,2700),Mathf.Clamp(point.y,-350,1000));
        SetZoom(size);ApplyView();
    }
    public void SetZoom(float size){overview=false;zoom=Mathf.Clamp(size,2.5f,900);ApplyView();}
    public void ToggleOverview(){overview=!overview;ApplyView();}
    public void ReturnToStart()
    {
        roomIndex=Array.FindIndex(rooms,r=>r.id=="MR01_C01");
        var r=rooms[roomIndex];TravelTo(new Vector2(r.x+7,r.y+4),5);
    }
    public void VisitRoom(string id)
    {
        int index=Array.FindIndex(rooms,r=>r.id==id);
        if(index<0)throw new ArgumentException("Unknown layout room: "+id);
        roomIndex=index;var r=rooms[index];
        TravelTo(new Vector2(r.x+r.w/2,r.y+r.h/2),Mathf.Max(r.h/2+3,(r.w/2+3)/Mathf.Max(.5f,reviewCamera.aspect)));
    }
    public void CycleRoom(int direction){VisitRoom(rooms[(roomIndex+direction%rooms.Length+rooms.Length)%rooms.Length].id);}
    void ApplyView()
    {
        if(reviewCamera==null)return;
        var p=overview?new Vector2(1240,290):position;
        reviewCamera.transform.position=new Vector3(p.x,p.y,-10);reviewCamera.orthographicSize=ViewSize;
        if(marker!=null){marker.position=new Vector3(position.x,position.y,-1);marker.localScale=Vector3.one*Mathf.Max(1,ViewSize/90);}
        float stroke=Mathf.Max(.12f,ViewSize/540);
        if(borders!=null && Mathf.Abs(stroke-lastStroke)>.001f){foreach(var b in borders)if(b!=null)b.startWidth=b.endWidth=stroke;lastStroke=stroke;}
    }
    void CreateMarker()
    {
        var go=new GameObject("Flight cursor - no collision or gravity");go.transform.SetParent(transform);marker=go.transform;
        var line=go.AddComponent<LineRenderer>();line.sharedMaterial=markerMaterial;line.useWorldSpace=false;
        line.positionCount=5;line.SetPositions(new[]{new Vector3(0,.55f),new Vector3(.32f,0),new Vector3(0,-.55f),new Vector3(-.32f,0),new Vector3(0,.55f)});
        line.startWidth=line.endWidth=.055f;line.startColor=line.endColor=new Color(.95f,.98f,1);line.sortingOrder=30;
    }
    static RectTransform RectChild(string name,Transform parent)
    {
        var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(parent,false);return rt;
    }
    void CreateHud()
    {
        var canvasObject=new GameObject("Layout Flight Controls",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));canvasObject.transform.SetParent(transform,false);
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
        var scaler=canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        helpPanel=RectChild("Control guide",canvas.transform);helpPanel.anchorMin=helpPanel.anchorMax=helpPanel.pivot=new Vector2(0,1);
        helpPanel.anchoredPosition=new Vector2(20,-20);helpPanel.sizeDelta=new Vector2(1030,132);
        var bg=helpPanel.gameObject.AddComponent<UnityEngine.UI.Image>();bg.color=new Color(.025f,.045f,.07f,.92f);bg.raycastTarget=false;
        statusText=Label("Status",helpPanel,14,10,1000,26,19);
        helpText=Label("Controls",helpPanel,14,43,1000,80,17);
        helpText.text="WASD / arrows: fly   |   Shift: fast   |   Ctrl: precise   |   Wheel or +/-: zoom   |   R: normal zoom\nM: whole map (click to travel)   |   H: heart   |   Home: start   |   Tab / Shift+Tab: rooms   |   F1: hide guide\nPad: left stick fly, RT fast, LT precise, right stick zoom, Start map, D-pad rooms / up normal, Y heart, X start";
        UpdateHud();
    }
    static UnityEngine.UI.Text Label(string name,Transform parent,float x,float y,float w,float h,int fontSize)
    {
        var rt=RectChild(name,parent);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);
        var t=rt.gameObject.AddComponent<UnityEngine.UI.Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=fontSize;
        t.color=new Color(.9f,.94f,1);t.raycastTarget=false;t.alignment=TextAnchor.UpperLeft;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;
        return t;
    }
    void UpdateHud()
    {
        if(statusText==null || rooms==null)return;
        var room=Array.Find(rooms,r=>position.x>=r.x && position.x<=r.x+r.w && position.y>=r.y && position.y<=r.y+r.h);
        string location=room==null?"Between rooms":room.id+" - "+room.name;
        statusText.text=$"LAYOUT FLIGHT  |  {location}  |  {CurrentSpeed:0.#} u/s  |  View {ViewSize*2*reviewCamera.aspect:0.#} x {ViewSize*2:0.#} u";
    }
}
