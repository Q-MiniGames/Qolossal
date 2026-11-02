using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Qolossal.Atlas
{
    // Opened by MraChart: it owns pause/modal lifetime and preserves the established Waymark UI.
    public sealed class ConnectedAtlas : MonoBehaviour
    {
        public AtlasData Data { get; private set; }
        readonly HashSet<string> surfaceEntrances=new HashSet<string>();
        public bool HasRoomOutline(AtlasNode n)=>n.type!="surface"||surfaceEntrances.Contains(n.id);
        public readonly Dictionary<string,AtlasNode> Nodes=new Dictionary<string,AtlasNode>();
        public readonly List<(Vector2 from,Vector2 to,Color color)> LabelLeaders=new List<(Vector2,Vector2,Color)>();
        public string FocusedRegion { get; private set; }
        public string SelectedId { get; private set; }
        public bool IsShowing => canvas && canvas.enabled;
        public Rect ViewBounds => view;
        public int ActiveCaveCount => Data?.caves.Length ?? 0;
        public RectTransform Viewport => viewport;
        public Canvas Canvas => canvas;
        public int VisibleRoomCount => Data?.nodes.Count(n=>NodeVisible(n)) ?? 0;
        Canvas canvas;RectTransform board,viewport;AtlasGraphic graphic;MraChart owner;
        UnityEngine.UI.Text title,detail,hints,subtitle;
        UnityEngine.UI.Image wash;
        RectTransform terrainPainting;
        public RectTransform TerrainPainting => terrainPainting;
        readonly List<UnityEngine.UI.Button> tabs=new List<UnityEngine.UI.Button>();
        readonly Dictionary<string,UnityEngine.UI.Text> labels=new Dictionary<string,UnityEngine.UI.Text>();
        readonly List<(AtlasCave cave,UnityEngine.UI.Text text)> caveLabels=new List<(AtlasCave,UnityEngine.UI.Text)>();
        Rect view;int openedFrame,selectedTab;bool padMode;float nextNav;
        readonly Color ink=new Color32(239,224,190,255),dim=new Color32(167,190,183,255);
        GameObject previousSelection;
        InputActionAsset controls;
        public InputActionAsset Controls => controls;
        const string BindingKey="Qolossal.Atlas.BindingOverrides";
        InputAction Action(string name)=>controls.FindAction("Atlas/"+name,true);
        public void SaveBindingOverrides(){PlayerPrefs.SetString(BindingKey,controls.SaveBindingOverridesAsJson());PlayerPrefs.Save();}
        public void ResetBindingOverrides(){controls.RemoveAllBindingOverrides();PlayerPrefs.DeleteKey(BindingKey);}
        string Binding(string name,bool gamepad)
        {
            var action=Action(name);
            var control=action.controls.FirstOrDefault(c=>gamepad?c.device==Gamepad.current:c.device is Keyboard);
            return control!=null?control.shortDisplayName??control.displayName:action.GetBindingDisplayString(group:gamepad?"Gamepad":"KeyboardMouse");
        }
        void OnDestroy(){if(controls){controls.Disable();Destroy(controls);}}

        public bool Initialize(MraChart chart)
        {
            owner=chart;var text=Resources.Load<TextAsset>("QolossalAtlas/AtlasData");if(!text)return false;
            Data=JsonUtility.FromJson<AtlasData>(text.text);
            Data.caves=Data.caves.Where(c=>!CaveAccessPolicy.IsRetiredEntrance(c.lowerRoomId)).ToArray();
            var active=new HashSet<string>(Data.caves.Select(c=>c.connectionId));
            Data.edges=Data.edges.Where(e=>e.kind!="cave"||active.Contains(e.id)).ToArray();
            foreach(var n in Data.nodes)Nodes.Add(n.id,n);
            foreach(var c in Data.caves)surfaceEntrances.Add(c.upperAreaId);
            controls=Instantiate(Resources.Load<InputActionAsset>("QolossalAtlas/AtlasControls"));
            var overrides=PlayerPrefs.GetString(BindingKey,"");if(!string.IsNullOrEmpty(overrides)){try{controls.LoadBindingOverridesFromJson(overrides);}catch(ArgumentException){controls.RemoveAllBindingOverrides();}}
            Build();return true;
        }
        RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=position;rt.sizeDelta=size;return rt;
        }
        UnityEngine.UI.Text Text(Transform parent,string name,string value,Vector2 position,Vector2 size,int font,TextAnchor align=TextAnchor.MiddleCenter)
        {
            // Use the project's existing font/layout helpers; text is never baked into the artwork.
            var t=TownUi.Label(parent,name,new Vector2(.5f,.5f),position,size,font,align,ink);t.text=value;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        UnityEngine.UI.Button Button(string name,string caption,Vector2 pos,Vector2 size,Action action)
        {
            var rt=Rect(board,name,pos,size);var img=rt.gameObject.AddComponent<UnityEngine.UI.Image>();img.color=new Color(.06f,.11f,.12f,.96f);
            var b=rt.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=img;var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1.3f,1.25f,1.1f);c.pressedColor=new Color(.8f,.8f,.7f);b.colors=c;b.navigation=new Navigation{mode=Navigation.Mode.None};
            Text(rt,"Label",caption,Vector2.zero,size-new Vector2(12,2),20);b.onClick.AddListener(()=>{action();if(IsShowing)EventSystem.current?.SetSelectedGameObject(null);});return b;
        }
        void Build()
        {
            canvas=GameHud.CreateCanvas("Connected World Atlas",94);canvas.transform.SetParent(transform,false);
            var backdrop=GameHud.AddImage(canvas.transform,"Opaque atlas backdrop",null,new Vector2(.5f,.5f),Vector2.zero,new Vector2(10000,10000));backdrop.color=new Color(.025f,.055f,.065f,1);backdrop.raycastTarget=true;
            board=Rect(canvas.transform,"Safe composition",Vector2.zero,new Vector2(1920,1080));
            title=Text(board,"Title","Q O L O S S A L",new Vector2(-565,484),new Vector2(710,44),32,TextAnchor.MiddleLeft);
            subtitle=Text(board,"Subtitle","THE CONNECTED ATLAS  /  SURFACE & UNDERGROUND",new Vector2(-525,444),new Vector2(790,30),17,TextAnchor.MiddleLeft);subtitle.color=dim;
            Button("World","Whole map",new Vector2(250,478),new Vector2(160,44),ShowWorld);
            Button("ZoomOut","−",new Vector2(369,478),new Vector2(54,44),()=>Zoom(1/1.3f));
            Button("ZoomIn","+",new Vector2(433,478),new Vector2(54,44),()=>Zoom(1.3f));
            Button("Here","Qori",new Vector2(542,478),new Vector2(140,44),CentreOnPlayer);
            Button("Waymarks","Waymarks",new Vector2(703,478),new Vector2(160,44),()=>owner.ShowRegionalChart());
            Button("Close","Close",new Vector2(857,478),new Vector2(126,44),()=>owner.Hide());
            var frame=Rect(board,"Atlas brass frame",new Vector2(0,30),new Vector2(1840,770));
            var frameInk=frame.gameObject.AddComponent<UnityEngine.UI.Image>();frameInk.color=new Color(.67f,.56f,.35f,.65f);frameInk.raycastTarget=false;
            var inset=Rect(frame,"Dark inset",Vector2.zero,new Vector2(1836,766)).gameObject.AddComponent<UnityEngine.UI.Image>();inset.color=new Color(.025f,.055f,.06f,1);inset.raycastTarget=false;
            viewport=Rect(board,"Map viewport",new Vector2(0,20),new Vector2(1770,710));viewport.gameObject.AddComponent<RectMask2D>();
            var mapBase=viewport.gameObject.AddComponent<UnityEngine.UI.Image>();mapBase.color=new Color(.025f,.055f,.06f,1);mapBase.raycastTarget=false;
            terrainPainting=Rect(viewport,"Registered terrain artwork",Vector2.zero,viewport.sizeDelta);
            var painted=terrainPainting.gameObject.AddComponent<RawImage>();painted.texture=Resources.Load<Texture2D>("QolossalAtlas/AtlasTerrain");painted.raycastTarget=false;
            painted.enabled=!Data.sceneDerived; // The previous painting uses superseded schematic coordinates.
            wash=Rect(viewport,"Ink contrast veil",Vector2.zero,viewport.sizeDelta).gameObject.AddComponent<UnityEngine.UI.Image>();wash.color=new Color(.025f,.055f,.06f,.12f);wash.raycastTarget=false;
            var layer=Rect(viewport,"Precise room and passage geometry",Vector2.zero,viewport.sizeDelta);layer.gameObject.AddComponent<CanvasRenderer>();graphic=layer.gameObject.AddComponent<AtlasGraphic>();graphic.owner=this;
            foreach(var n in Data.nodes)labels[n.id]=Text(viewport,n.id,n.id.Replace('_','-'),Vector2.zero,new Vector2(160,26),18);
            foreach(var c in Data.caves)caveLabels.Add((c,Text(viewport,"Entrance "+c.lowerRoomId,c.lowerRoomId.Replace('_','-'),Vector2.zero,new Vector2(160,24),16)));
            detail=Text(board,"Selection details","Choose a region to explore its surface and underground together.",new Vector2(0,-378),new Vector2(1800,68),21);detail.color=ink;
            for(int i=0;i<Data.regions.Length;i++){int index=i;var r=Data.regions[i];var tab=Button(r.id,Data.illustratedMap?"":r.id+"\n"+r.name,new Vector2(-780+i*260,-459),new Vector2(248,70),()=>FocusRegion(Data.regions[index].id));tabs.Add(tab);if(Data.illustratedMap){var glyph=Rect(tab.transform,"Terrain silhouette",Vector2.zero,new Vector2(206,42)).gameObject.AddComponent<AtlasZoneGlyph>();glyph.color=RegionColor(r.id);glyph.raycastTarget=false;glyph.points=Data.contours.Where(c=>!c.underground&&c.region==r.id).SelectMany(c=>c.path).OrderBy(p=>p.x).ToArray();}}
            hints=Text(board,"Controls","",new Vector2(0,-516),new Vector2(1860,35),18);hints.color=dim;
            canvas.enabled=false;
        }
        public void Show()
        {
            previousSelection=EventSystem.current?EventSystem.current.currentSelectedGameObject:null;
            EventSystem.current?.SetSelectedGameObject(null);canvas.enabled=true;controls.Enable();openedFrame=Time.frameCount;
            selectedTab=Mathf.Max(0,Array.FindIndex(Data.regions,r=>r.id==MraSession.Current?.regionId));
            FitBoard();ShowWorld();
        }
        public void Hide()
        {
            if(!canvas)return;controls.Disable();canvas.enabled=false;
            if(EventSystem.current)EventSystem.current.SetSelectedGameObject(previousSelection&&previousSelection.activeInHierarchy?previousSelection:null);
        }
        void FitBoard()
        {
            var rt=canvas.transform as RectTransform;float scale=Mathf.Min(rt.rect.width/1920f,rt.rect.height/1080f);board.localScale=Vector3.one*scale;
        }
        public void ShowWorld()
        {
            FocusedRegion=null;SelectedId=null;Fit(new Rect(0,0,Data.width,Data.height),0);
            title.text="Q O L O S S A L";subtitle.text="THE CONNECTED ATLAS  /  SURFACE & UNDERGROUND";
            detail.text="Choose a level to see its upper areas and underground rooms.  •  Gold mouths mark active cave pairs.";Refresh();
        }
        public void FocusRegion(string id)
        {
            int index=Array.FindIndex(Data.regions,r=>r.id==id);if(index<0)return;selectedTab=index;FocusedRegion=id;SelectedId=null;
            var ns=Data.nodes.Where(n=>n.region==id).ToArray();float x=ns.Min(n=>n.Bounds.xMin),y=ns.Min(n=>n.Bounds.yMin),xx=ns.Max(n=>n.Bounds.xMax),yy=ns.Max(n=>n.Bounds.yMax);
            foreach(var cave in Data.caves.Where(CaveVisible)){x=Mathf.Min(x,cave.upperPoint.x,cave.lowerPoint.x);xx=Mathf.Max(xx,cave.upperPoint.x,cave.lowerPoint.x);y=Mathf.Min(y,cave.upperPoint.y,cave.lowerPoint.y);yy=Mathf.Max(yy,cave.upperPoint.y,cave.lowerPoint.y);}
            if(Data.illustratedMap)foreach(var edge in Data.edges.Where(e=>e.kind=="cave"&&Nodes[e.from].region==id))foreach(var p in edge.path){x=Mathf.Min(x,p.x);xx=Mathf.Max(xx,p.x);y=Mathf.Min(y,p.y);yy=Mathf.Max(yy,p.y);}
            Fit(new Rect(x,y,xx-x,yy-y),150);
            title.text=Data.illustratedMap?"SURFACE & DEPTHS":Data.regions[index].name.ToUpperInvariant();subtitle.text=Data.illustratedMap?"CAVE PASSAGES  /  EXPLORE THIS LANDSCAPE":id+"  /  UPPER GROUND + UNDERGROUND";
            detail.text=Data.illustratedMap?"Follow the carved tunnels from each cave mouth. Scroll to look closer.":$"{ns.Count(n=>n.type=="surface")} upper areas  •  {ns.Count(n=>n.type=="underground")} underground rooms  •  {Data.caves.Count(CaveVisible)} cave pairs.  Select a room for its connections.";Refresh();
        }
        void Fit(Rect bounds,float margin)
        {
            bounds.xMin-=margin;bounds.xMax+=margin;bounds.yMin-=margin;bounds.yMax+=margin;
            float ratio=viewport.rect.width/viewport.rect.height,w=Mathf.Max(bounds.width,bounds.height*ratio),h=w/ratio;
            view=new Rect(bounds.center-new Vector2(w,h)/2,new Vector2(w,h));
        }
        public void SelectRoom(string id)
        {
            if(!Nodes.TryGetValue(id,out var n))return;SelectedId=id;
            if(Data.illustratedMap){detail.text="Follow the cave mouth and its tunnel. Scroll to inspect the surrounding terrain.";Refresh();return;}
            var neighbours=Data.edges.Where(e=>e.from==id||e.to==id).Select(e=>(e.kind=="cave"?"Cave: ":"")+(e.from==id?e.to:e.from).Replace('_','-')).ToArray();
            var cave=Data.caves.FirstOrDefault(c=>c.lowerRoomId==id||c.upperAreaId==id);
            detail.text=n.id.Replace('_','-')+"  /  "+n.name+"\n"+string.Join("  ·  ",neighbours)+(cave!=null?"   |   "+cave.requirements:"");Refresh();
        }
        public Vector2 Project(Vector2 p)=>new Vector2((p.x-view.xMin)/view.width*viewport.rect.width-viewport.rect.width/2,viewport.rect.height/2-(p.y-view.yMin)/view.height*viewport.rect.height);
        Vector2 Unproject(Vector2 p)=>new Vector2(view.xMin+(p.x+viewport.rect.width/2)/viewport.rect.width*view.width,view.yMin+(viewport.rect.height/2-p.y)/viewport.rect.height*view.height);
        public Vector2 ScreenPoint(Vector2 p)=>RectTransformUtility.WorldToScreenPoint(null,viewport.TransformPoint(Project(p)));
        public void ClickScreen(Vector2 screen)
        {
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,screen,null,out var local))return;var p=Unproject(local);
            var node=Data.nodes.Where(NodeVisible).Where(n=>{var b=n.Bounds;b.xMin-=10;b.xMax+=10;b.yMin-=10;b.yMax+=10;return b.Contains(p);}).OrderBy(n=>n.Bounds.width*n.Bounds.height).FirstOrDefault();
            if(FocusedRegion==null){if(node!=null&&node.region!="shared")FocusRegion(node.region);else {var nearest=Data.nodes.Where(n=>n.type=="surface").OrderBy(n=>(n.point-p).sqrMagnitude).FirstOrDefault();if(nearest!=null)FocusRegion(nearest.region);}}
            else if(node!=null)SelectRoom(node.id);
        }
        public void DragScreen(Vector2 delta)
        {
            float s=canvas.scaleFactor*board.localScale.x;view.position+=new Vector2(-delta.x/s/viewport.rect.width*view.width,delta.y/s/viewport.rect.height*view.height);ClampView();Refresh();
        }
        public void Zoom(float factor){ZoomLocal(factor,Vector2.zero);}
        public void ZoomAt(float factor,Vector2 screen){RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,screen,null,out var p);ZoomLocal(factor,p);}
        void ZoomLocal(float factor,Vector2 local)
        {
            var pivot=Unproject(local);float w=Mathf.Clamp(view.width/factor,220,Data.width*1.3f),ratio=w/view.width;view=new Rect(pivot+(view.position-pivot)*ratio,view.size*ratio);ClampView();Refresh();
        }
        void ClampView(){view.center=new Vector2(Mathf.Clamp(view.center.x,-view.width*.1f,Data.width+view.width*.1f),Mathf.Clamp(view.center.y,-view.height*.1f,Data.height+view.height*.1f));}
        public Color RegionColor(string id){var r=Data.regions.FirstOrDefault(r=>r.id==id);return r!=null&&ColorUtility.TryParseHtmlString(r.color,out var c)?c:ink;}
        public bool NodeVisible(AtlasNode n)=>FocusedRegion==null||n.region==FocusedRegion;
        public bool EdgeVisible(AtlasEdge e)=>FocusedRegion==null||Nodes[e.from].region==FocusedRegion&&Nodes[e.to].region==FocusedRegion;
        public bool CaveVisible(AtlasCave c)=>FocusedRegion==null||Nodes[c.upperAreaId].region==FocusedRegion;
        public bool PlayerMapPoint(out Vector2 p)
        {
            p=Vector2.zero;var player=FindAnyObjectByType<PlayerMovement>();if(!player)return false;string scene=player.gameObject.scene.name;var at=(Vector2)player.transform.position;
            var n=Data.nodes.FirstOrDefault(n=>n.scene==scene&&new Rect(n.worldBounds[0],n.worldBounds[1],n.worldBounds[2],n.worldBounds[3]).Contains(at));
            if(n==null&&scene=="Qolossal_Underground")n=Data.nodes.Where(n=>n.scene==scene).OrderBy(n=>((Vector2)new Vector2(n.worldBounds[0]+n.worldBounds[2]/2,n.worldBounds[1]+n.worldBounds[3]/2)-at).sqrMagnitude).FirstOrDefault();
            if(n==null||!NodeVisible(n))return false;
            var mapping=Data.mappings?.FirstOrDefault(m=>m.id==(string.IsNullOrEmpty(n.mappingId)?scene:n.mappingId));
            if(mapping!=null){p=new Vector2(at.x*mapping.sx+mapping.ox,mapping.oy-at.y*mapping.sy);return true;}
            p=new Vector2(n.Bounds.x+n.Bounds.width*(at.x-n.worldBounds[0])/n.worldBounds[2],n.Bounds.yMax-n.Bounds.height*(at.y-n.worldBounds[1])/n.worldBounds[3]);return true;
        }
        public void CentreOnPlayer(){var player=FindAnyObjectByType<PlayerMovement>();var n=player?Data.nodes.Where(n=>n.scene==player.gameObject.scene.name).OrderBy(n=>Vector2.SqrMagnitude(new Vector2(n.worldBounds[0]+n.worldBounds[2]/2,n.worldBounds[1]+n.worldBounds[3]/2)-(Vector2)player.transform.position)).FirstOrDefault():null;FocusRegion(n!=null&&n.region!="shared"?n.region:MraSession.Current?.regionId??"MR01");if(PlayerMapPoint(out var p)){view.center=p;Refresh();}}
        Vector2 PlaceLabel(Vector2 preferred,Vector2 size,List<Rect> used)
        {
            var safe=viewport.rect;safe.xMin+=size.x/2+4;safe.xMax-=size.x/2+4;safe.yMin+=size.y/2+4;safe.yMax-=size.y/2+4;
            var candidates=new List<Vector2>();
            for(int x=-8;x<=8;x++)for(int y=-8;y<=8;y++)candidates.Add(preferred+new Vector2(x*(size.x/2+12),y*(size.y+5)));
            foreach(var candidate in candidates.OrderBy(p=>(p-preferred).sqrMagnitude)){
                if(!safe.Contains(candidate))continue;var rect=new Rect(candidate-size/2-Vector2.one*2,size+Vector2.one*4);
                if(used.Any(u=>u.Overlaps(rect)))continue;used.Add(rect);return candidate;
            }
            return new Vector2(Mathf.Clamp(preferred.x,safe.xMin,safe.xMax),Mathf.Clamp(preferred.y,safe.yMin,safe.yMax));
        }
        void Refresh()
        {
            if(!graphic)return;graphic.SetVerticesDirty();wash.color=new Color(.025f,.055f,.06f,FocusedRegion==null?.10f:.23f);
            terrainPainting.anchoredPosition=Project(new Vector2(Data.width/2,Data.height/2));
            terrainPainting.sizeDelta=new Vector2(Data.width/view.width*viewport.rect.width,Data.height/view.height*viewport.rect.height);
            var used=new List<Rect>();LabelLeaders.Clear();
            foreach(var item in caveLabels){
                var p=Project(item.cave.upperPoint);bool show=!Data.illustratedMap&&FocusedRegion!=null&&CaveVisible(item.cave)&&viewport.rect.Contains(p);item.text.gameObject.SetActive(show);
                if(show){string label=item.cave.lowerRoomId.StartsWith(FocusedRegion+"_")?item.cave.lowerRoomId.Substring(5):item.cave.lowerRoomId;item.text.text=label;var size=new Vector2(label.Length*11+18,24);var at=p+new Vector2(0,-30);
                    at=PlaceLabel(at,size,used);item.text.rectTransform.sizeDelta=size;
                    item.text.rectTransform.anchoredPosition=at;item.text.color=new Color32(231,202,143,255);LabelLeaders.Add((p,at,new Color(.8f,.68f,.4f,.3f)));}
            }
            foreach(var n in Data.nodes.OrderByDescending(n=>n.Bounds.width*n.Bounds.height)){
                var t=labels[n.id];var p=Project(n.point);bool visible=!Data.illustratedMap&&FocusedRegion!=null&&NodeVisible(n)&&viewport.rect.Contains(p);t.gameObject.SetActive(visible);if(!visible)continue;
                string display=n.type=="surface"?n.id.Substring(5):n.id.Replace('_','-');t.text=display;
                float width=Mathf.Max(36,display.Length*11),height=24;var b=n.Bounds;var top=Project(new Vector2(b.center.x,b.yMin));var bottom=Project(new Vector2(b.center.x,b.yMax));float screenWidth=b.width/view.width*viewport.rect.width;
                var candidates=new List<Vector2>();if(n.type!="surface"&&screenWidth>width+8&&top.y-bottom.y>24)candidates.Add(p);candidates.Add(top+Vector2.up*16);candidates.Add(bottom-Vector2.up*16);candidates.Add(top+Vector2.up*40);candidates.Add(bottom-Vector2.up*40);candidates.Add(top+Vector2.up*64);candidates.Add(bottom-Vector2.up*64);
                var size=new Vector2(width+12,height);var pos=PlaceLabel(candidates[0],size,used);
                t.rectTransform.anchoredPosition=pos;t.rectTransform.sizeDelta=size;t.color=SelectedId==n.id?Color.white:RegionColor(n.region);if((pos-p).sqrMagnitude>576){var col=t.color;col.a=.3f;LabelLeaders.Add((p,pos,col));}
            }
            for(int i=0;i<tabs.Count;i++){var image=tabs[i].GetComponent<UnityEngine.UI.Image>();image.color=i==selectedTab?new Color(.23f,.30f,.26f,.98f):new Color(.055f,.105f,.115f,.98f);}
        }
        void Update()
        {
            if(!IsShowing||Time.frameCount==openedFrame)return;FitBoard();var k=Keyboard.current;var pad=Gamepad.current;var mouse=Mouse.current;
            if(pad!=null&&(pad.dpad.ReadValue()!=Vector2.zero||pad.buttonSouth.wasPressedThisFrame||pad.leftShoulder.wasPressedThisFrame||pad.rightShoulder.wasPressedThisFrame||pad.leftStick.ReadValue().sqrMagnitude>.1f))padMode=true;
            if(k!=null&&k.anyKey.wasPressedThisFrame||mouse!=null&&(mouse.delta.ReadValue().sqrMagnitude>2||mouse.leftButton.wasPressedThisFrame))padMode=false;
            hints.text=padMode?$"D-pad ← / →: level  ·  ↑ / ↓: rooms  ·  {Binding("Confirm",true)}: open level  ·  Stick: pan  ·  Triggers: zoom  ·  {Binding("Center",true)}: Qori  ·  {Binding("Waymarks",true)}: Waymarks  ·  {Binding("Back",true)}: back":$"Click level / room  ·  Drag: pan  ·  Wheel / + / −: zoom  ·  {Binding("Previous",false)} / {Binding("Next",false)}: levels  ·  {Binding("Center",false)}: Qori  ·  {Binding("Back",false)}: back  ·  {Binding("Close",false)}: close";
            if(Action("Close").WasPressedThisFrame()){owner.Hide();return;}
            if(Action("Back").WasPressedThisFrame()){if(FocusedRegion!=null)ShowWorld();else owner.Hide();return;}
            int step=(Action("Next").WasPressedThisFrame()?1:0)-(Action("Previous").WasPressedThisFrame()?1:0);
            if(step!=0)FocusRegion(Data.regions[(selectedTab+step+7)%7].id);
            var nav=Action("Navigate").ReadValue<Vector2>();
            if(Time.unscaledTime>nextNav&&nav.sqrMagnitude>.25f){
                if(Mathf.Abs(nav.x)>.5f){selectedTab=(selectedTab+(nav.x>0?1:6))%7;Refresh();}
                else if(FocusedRegion!=null){var rooms=Data.nodes.Where(NodeVisible).OrderBy(n=>n.type=="surface"?0:1).ThenBy(n=>n.id).ToArray();int index=Array.FindIndex(rooms,n=>n.id==SelectedId);index=(index+(nav.y<0?1:-1)+rooms.Length)%rooms.Length;SelectRoom(rooms[index].id);if(!view.Contains(rooms[index].point)){view.center=rooms[index].point;Refresh();}}
                nextNav=Time.unscaledTime+.22f;
            }
            if(Action("Confirm").WasPressedThisFrame())FocusRegion(Data.regions[selectedTab].id);
            if(Action("Center").WasPressedThisFrame())CentreOnPlayer();
            if(Action("Waymarks").WasPressedThisFrame()){owner.ShowRegionalChart();return;}
            float z=Action("Zoom").ReadValue<float>();
            if(Mathf.Abs(z)>.05f)Zoom(Mathf.Pow(2,z*Time.unscaledDeltaTime));
            var pan=Action("Pan").ReadValue<Vector2>();
            if(pan.sqrMagnitude>.02f)DragScreen(-pan*500*Time.unscaledDeltaTime*canvas.scaleFactor*board.localScale.x);
        }
    }
}
