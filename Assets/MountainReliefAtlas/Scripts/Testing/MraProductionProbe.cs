#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

// Explicitly opted-in development-player smoke/measurement run. Ordinary launches do nothing.
public sealed class MraProductionProbe : MonoBehaviour
{
    [Serializable] public struct Sample { public int frame, gcCollections; public double seconds; public float frameMs; public double cpuMs, gpuMs; public long allocatedBytes, workingSetBytes; }
    [Serializable] public sealed class Phase { public string name; public int width,height; public float warmupSeconds; public List<Sample> samples=new List<Sample>(); }
    [Serializable] public sealed class Report
    {
        public string unity, cpu, gpu, graphicsApi, save, quality, result="running";
        public List<double> reportWriteMs=new List<double>(); public List<string> events=new List<string>(); public bool developmentBuild; public int vSync, targetFrameRate; public long peakAllocatedBytes, peakWorkingSetBytes;
        public List<string> checks=new List<string>(); public List<string> errors=new List<string>(); public List<Phase> phases=new List<Phase>();
    }
    readonly Report report=new Report(); readonly FrameTiming[] timing=new FrameTiming[1];
    Gamepad pad; string output; float started, nextMemorySample; int failures; bool extended;
    PlayerMovement Qori => FindAnyObjectByType<PlayerMovement>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()
    {
        if (!Environment.GetCommandLineArgs().Contains("-qolossalProductionProbe")) return;
        if (Path.GetFileName(GameSave.FilePath)!="qolossal_batch44_probe.json") { Debug.LogError("Probe requires isolated -saveFile qolossal_batch44_probe.json"); return; }
        Application.runInBackground=true;
        Debug.Log("[ProductionProbe] Launch enabled");
        if (FindAnyObjectByType<MraProductionProbe>()==null) new GameObject("Production Probe").AddComponent<MraProductionProbe>();
    }
    void Check(bool ok,string message) { report.checks.Add((ok?"PASS ":"FAIL ")+message); if(!ok)failures++; }
    void Save()
    {
        if(output==null)return;
        var clock=System.Diagnostics.Stopwatch.StartNew();
        File.WriteAllText(Path.Combine(output,"player_report.json"),JsonUtility.ToJson(report,true));
        report.reportWriteMs.Add(clock.Elapsed.TotalMilliseconds);
    }
    void Log(string message,string trace,LogType type) { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){report.errors.Add(message);} }
    void Update()
    {
        if(started>0 && Time.realtimeSinceStartup-started>900){report.result="watchdog_timeout";Save();Application.Quit(3);}
        if(Time.realtimeSinceStartup>=nextMemorySample){nextMemorySample=Time.realtimeSinceStartup+1;report.peakAllocatedBytes=Math.Max(report.peakAllocatedBytes,Profiler.GetTotalAllocatedMemoryLong());}
    }
    // Save resume can replace the launch scene before Start; survive that callback ordering.
    void Awake()=>DontDestroyOnLoad(gameObject);
    IEnumerator Start()
    {
        Debug.Log("[ProductionProbe] Start entered");
        DontDestroyOnLoad(gameObject);
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-probeOutput");
        output=at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.persistentDataPath,"Batch44Probe");Directory.CreateDirectory(output);
        Application.logMessageReceived+=Log;Application.runInBackground=true;QualitySettings.vSyncCount=0;int fpsAt=Array.IndexOf(args,"-probeFps");Application.targetFrameRate=fpsAt>=0&&fpsAt+1<args.Length?int.Parse(args[fpsAt+1]):Application.targetFrameRate;extended=args.Contains("-probeExtended");
        report.unity=Application.unityVersion;report.cpu=SystemInfo.processorType;report.gpu=SystemInfo.graphicsDeviceName;report.graphicsApi=SystemInfo.graphicsDeviceType.ToString();report.save=GameSave.FilePath;report.quality=QualitySettings.names[QualitySettings.GetQualityLevel()];report.developmentBuild=Debug.isDebugBuild;report.vSync=QualitySettings.vSyncCount;report.targetFrameRate=Application.targetFrameRate;
        GameSave.Clear();started=Time.realtimeSinceStartup;Save();
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        pad=InputSystem.AddDevice<Gamepad>("Production probe virtual controller");
        if(SceneManager.GetActiveScene().name!="MRAtlas_MR01_Cradle")yield return SceneManager.LoadSceneAsync("MRAtlas_MR01_Cradle");
        if(args.Contains("-probeArmorySmoke")){yield return new WaitForSecondsRealtime(2);yield return ArmoryCoverage();Finish();yield break;}
        yield return new WaitForSecondsRealtime(3);
        var initial=Qori.transform.position;float peak=initial.y,t=Time.realtimeSinceStartup;
        while(Time.realtimeSinceStartup-t<3){float e=Time.realtimeSinceStartup-t;Queue(e<1.5f?1:0,e>.7f&&e<1);peak=Mathf.Max(peak,Qori.transform.position.y);yield return null;}
        Queue(0);Check(Qori.transform.position.x-initial.x>3,"virtual-gamepad walk");Check(peak-initial.y>1,"virtual-gamepad jump");Check(ClearOfGround(),"Cradle landing clear of ground");
        yield return Measure("Cradle_start",8,20);
        Qori.PlaceAt(new Vector2(221.25f,14.9f),0,false);yield return new WaitForSecondsRealtime(1);
        t=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-t<1.1f){Queue(1);yield return null;}Queue(0);yield return new WaitForSecondsRealtime(1);
        Check(ClearOfGround(),"descending short-step traversal clear of ground");
        yield return Measure("Cradle_ravine",5,20);
        yield return SceneManager.LoadSceneAsync("MRAtlas_MR07_Summit");yield return new WaitForSecondsRealtime(2);
        Qori.PlaceAt(new Vector2(362.4f,149f),0,false);yield return new WaitForSecondsRealtime(1);
        var state=FindAnyObjectByType<MraEntranceState>();
        Check(SceneManager.GetActiveScene().name=="MRAtlas_MR07_Summit"&&!state.IsOpen&&state.artwork.sprite==state.sealedState,"sealed doorway refuses travel before reveal");
        var trigger=state.exit.GetComponent<BoxCollider2D>();Check(Vector2.Distance(trigger.size,new Vector2(1.2f,1.6f))<.0001f,"registered doorway trigger size");
        Qori.PlaceAt(new Vector2(360.7f,149f),0,false);
        yield return Measure("Summit_sealed",5,20);
        var reveal=FindAnyObjectByType<MraReveal>();Check(reveal.Play(Qori),"existing reveal sequence starts");
        t=Time.realtimeSinceStartup;while(MraReveal.IsPlaying&&Time.realtimeSinceStartup-t<60)yield return null;
        Check(!MraReveal.IsPlaying&&MraState.Revealed&&state.IsOpen&&state.artwork.sprite==state.open,"completed reveal selects open artwork");
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"Summit_open.png"));yield return null;
        yield return Measure("Summit_to_Descent_transition",0,10,e=>Queue(SceneManager.GetActiveScene().name=="MRAtlas_MR07_Summit"?1:0));
        yield return new WaitForSecondsRealtime(2);Check(SceneManager.GetActiveScene().name=="MRAtlas_MR08_Descent","walk through open entrance reaches Descent");Check(ClearOfGround(),"arrival clear of ground");
        if(SceneManager.GetActiveScene().name=="MRAtlas_MR08_Descent")yield return Measure("Descent_arrival",5,20);
        GameSave.Reload();Check(MraState.Revealed,"reveal unlock survives save reload");
        yield return SceneManager.LoadSceneAsync("MRAtlas_MR07_Summit");yield return new WaitForSecondsRealtime(2);
        state=FindAnyObjectByType<MraEntranceState>();Check(state!=null&&state.IsOpen&&state.artwork.sprite==state.open,"revisited Summit restores open state");
        if(extended)yield return ExtendedCoverage();
        Finish();
    }
    void Finish(){report.result=failures==0&&report.errors.Count==0?"passed":"failed";Save();InputSystem.RemoveDevice(pad);Application.Quit(report.result=="passed"?0:2);}
    void Queue(float move,bool jump=false)
    {var s=new GamepadState{leftStick=new Vector2(move,0)};if(jump)s=s.WithButton(GamepadButton.South);InputSystem.QueueStateEvent(pad,s);}
    bool ClearOfGround()
    {
        if(Qori==null)return false;var box=Qori.GetComponent<BoxCollider2D>();var body=Qori.GetComponent<Rigidbody2D>();
        return !Physics2D.OverlapBoxAll(box.bounds.center,box.bounds.size*.9f,0,LayerMask.GetMask("Ground")).Any(c=>!c.isTrigger&&c.attachedRigidbody!=body);
    }
    // Buffers and report serialization happen BEFORE warmup. No file I/O, process queries,
    // capacity growth or sample-object allocation is performed inside measured frames.
    IEnumerator Measure(string name,float warmup,float seconds,Action<float> drive=null)
    {
        var p=new Phase{name=name,width=Screen.width,height=Screen.height,warmupSeconds=warmup};
        p.samples.Capacity=Mathf.CeilToInt(seconds*(Application.targetFrameRate>0?Application.targetFrameRate*2:4000))+16;
        report.phases.Add(p);Save();
        yield return new WaitForSecondsRealtime(warmup);
        // Resume across two frames even for a zero-warmup event window: never include setup IO.
        yield return null;yield return null;
        double begin=Time.realtimeSinceStartupAsDouble,end=begin+seconds;
        while(Time.realtimeSinceStartupAsDouble<end)
        {
            drive?.Invoke((float)(Time.realtimeSinceStartupAsDouble-begin));
            FrameTimingManager.CaptureFrameTimings();yield return null;
            uint count=FrameTimingManager.GetLatestTimings(1,timing);
            if(p.samples.Count==p.samples.Capacity){Check(false,"measurement buffer exhausted: "+name);break;}
            p.samples.Add(new Sample{frame=Time.frameCount,seconds=Time.realtimeSinceStartupAsDouble,frameMs=Time.unscaledDeltaTime*1000,cpuMs=count>0?timing[0].cpuFrameTime:0,gpuMs=count>0?timing[0].gpuFrameTime:0,gcCollections=GC.CollectionCount(0)});
        }
        Queue(0);
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return null;Save();
    }
    IEnumerator Press(GamepadButton button)
    {
        InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));
        yield return null;yield return null;
        Queue(0);yield return null;yield return null;
    }
    string SelectedLabel()
    {
        var go=UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
        return go!=null?go.GetComponentInChildren<UnityEngine.UI.Text>()?.text:null;
    }
    IEnumerator ArmoryCoverage()
    {
        var armory=Qori.GetComponent<QoriArmory>();int selected=armory.Selected;
        // The weapon menu was removed (3 Nov): none of its inputs may do anything now.
        yield return Press(GamepadButton.RightStick);Check(armory.Selected==selected&&!MraChart.IsOpen&&!QoriArmory.BlocksAttackInput,"right-stick click opens nothing");
        var keyboard=InputSystem.AddDevice<Keyboard>("Production probe keyboard");
        foreach(var key in new[]{Key.Tab,Key.Digit1,Key.Digit4})
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
            Check(armory.Selected==selected&&!QoriArmory.BlocksAttackInput,"keyboard "+key+" opens and selects nothing");
        }
        Check(armory.Weapons[armory.Selected].weaponId==QoriArmoryFactory.StaffId,"Qori holds the Leaf Staff");InputSystem.RemoveDevice(keyboard);Queue(0);yield return null;
    }
    IEnumerator ExtendedCoverage()
    {
        // The input events go through the real Input System; no UI callbacks are invoked.
        yield return Press(GamepadButton.Select);
        Check(MraChart.IsOpen,"controller View opens Chart");
        var chart=MraChart.Instance;
        float z=chart.Zoom;Vector2 pan=chart.Pan;
        yield return Measure("Chart_pan_zoom",1,12,e=>InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=new Vector2(.65f,.3f),rightTrigger=e<1?.7f:0}));
        Check(chart.Zoom>z&&Vector2.Distance(chart.Pan,pan)>1,"controller Chart zoom and pan");
        string previous=chart.Viewing;yield return Press(GamepadButton.RightShoulder);
        Check(chart.Viewing!=previous,"controller Chart switches region");
        yield return Press(GamepadButton.North);
        yield return Press(GamepadButton.East);Check(!MraChart.IsOpen&&Time.timeScale>0,"controller B closes Chart and resumes");
        yield return ArmoryCoverage();
        yield return Press(GamepadButton.Start);Check(GamePauseMenu.IsPaused&&SelectedLabel()=="Resume","controller pause selects Resume");
        yield return Press(GamepadButton.DpadDown);Check(SelectedLabel()=="Chart","controller pause navigation selects Chart");
        yield return Press(GamepadButton.South);Check(MraChart.IsOpen&&!GamePauseMenu.IsPaused,"controller confirm opens Chart from pause");
        yield return Press(GamepadButton.East);
        yield return Press(GamepadButton.Start);
        InputSystem.RemoveDevice(pad);pad=null;yield return new WaitForSecondsRealtime(.25f);
        Check(GamePauseMenu.IsPaused,"virtual controller disconnect preserves paused menu");
        pad=InputSystem.AddDevice<Gamepad>("Production probe replacement controller");Queue(0);yield return null;
        yield return Press(GamepadButton.South);Check(!GamePauseMenu.IsPaused&&Time.timeScale>0,"replacement controller confirms Resume");

        var world=Mra.World.Load();
        foreach(string id in new[]{"MR02","MR04","MR05","MR06"})
        {
            var region=world.RegionById(id);bool requested=false;double loadStart=0;bool loaded=false;
            yield return Measure(id+"_transition",1,5,e=>
            {
                if(!requested){requested=true;loadStart=Time.realtimeSinceStartupAsDouble;SceneManager.LoadSceneAsync(region.scene);}
                if(!loaded&&SceneManager.GetActiveScene().name==region.scene){loaded=true;report.events.Add(id+" load ms="+((Time.realtimeSinceStartupAsDouble-loadStart)*1000).ToString("F2",System.Globalization.CultureInfo.InvariantCulture));}
            });
            Check(loaded,id+" async scene transition completed");
            var qori=Qori;var node=region.nodes[region.nodes.Length/2];qori.PlaceAt(node.spawn,0,false);
            float minX=float.PositiveInfinity,maxX=float.NegativeInfinity;
            yield return Measure(id+"_traversal",3,12,e=>{minX=Mathf.Min(minX,qori.transform.position.x);maxX=Mathf.Max(maxX,qori.transform.position.x);Queue(e%4<2?1:-1,e%3<.18f);});
            Check(maxX-minX>.5f,id+" traversal covers horizontal distance ("+(maxX-minX).ToString("F2")+"u)");
            if(id=="MR05")
            {
                var guardian=FindAnyObjectByType<KnucklebrambleGuardian>();
                Check(guardian!=null,"Ribwood guardian present");
                if(guardian!=null)
                {
                    qori.PlaceAt((Vector2)guardian.transform.position+new Vector2(-1.7f,1),0,false);
                    var combat=qori.GetComponent<PlayerCombat>();int attacks=0,hits=0;bool active=false;float initialHealth=guardian.Health;
                    Action<AttackDefinition> onAttack=a=>attacks++;Action<AttackHitResult> onHit=h=>hits++;
                    combat.OnAttackStarted+=onAttack;combat.OnAttackHit+=onHit;
                    yield return Measure("Ribwood_guardian_combat",2,20,e=>
                    {
                        var state=new GamepadState{leftStick=new Vector2(e%4<.12f?.3f:0,0)};
                        if(e%.75f<.09f)state=state.WithButton(GamepadButton.LeftShoulder);
                        InputSystem.QueueStateEvent(pad,state);active|=guardian.Current!=KnucklebrambleGuardian.State.Dormant;
                    });
                    combat.OnAttackStarted-=onAttack;combat.OnAttackHit-=onHit;
                    Check(attacks>=3,"controller attacks execute during guardian combat ("+attacks+")");
                    Check(active,"guardian attack cycle activates");
                    Check(hits>0&&guardian.Health<initialHealth,"guardian receives controller-driven combat hits");
                    report.events.Add("Guardian contact results="+hits+"; health="+guardian.Health+"; cycle="+guardian.Cycle);
                }
            }
        }
    }
    void OnDestroy(){Application.logMessageReceived-=Log;}
}
#endif
