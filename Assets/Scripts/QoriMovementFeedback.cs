using UnityEngine;

// Restrained synthesized rustles until a recorded surface-specific sound set is authored.
[DefaultExecutionOrder(20)]
public sealed class QoriMovementFeedback : MonoBehaviour
{
    [System.Serializable] public sealed class Tuning
    {
        [Range(0,1)] public float volume=.18f;
        public bool sounds=true,leafParticles=true;
        public AudioClip footstepSound,landingSound,threadSound;
    }
    public Tuning tuning=new Tuning();
    PlayerMovement movement;
    PlayerThread thread;
    QoriBodyRig rig;
    AudioSource audioSource;
    AudioClip footClip,landClip,threadClip;
    ParticleSystem leaves;
    Material material;
    Mesh leafMesh;
    int reset=-1,foot=-1,landing=-1,attached=-1,released=-1;
    float nextFoot;
    bool ownsFoot,ownsLand,ownsThread;
    public int FootSounds { get; private set; }
    public int LandingSounds { get; private set; }
    public int ThreadSounds { get; private set; }
    public void Initialize(PlayerMovement owner,QoriBodyRig bodyRig)
    {
        movement=owner;rig=bodyRig;if(owner==null)return;
        // The recorded sounds (QoriSounds) replace these rustles wherever the player has them.
        if(owner.GetComponent<QoriSounds>()!=null)tuning.sounds=false;
        thread=owner.GetComponent<PlayerThread>();
        audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;
        ownsFoot=tuning.footstepSound==null;ownsLand=tuning.landingSound==null;ownsThread=tuning.threadSound==null;
        footClip=ownsFoot?Rustle("Qori soft leaf step",.085f,.12f,31):tuning.footstepSound;
        landClip=ownsLand?Rustle("Qori soft landing",.18f,.3f,57):tuning.landingSound;
        threadClip=ownsThread?Rustle("Qori thread fibre",.12f,.04f,89):tuning.threadSound;
        GameObject obj=new GameObject("Qori contact leaves");obj.transform.SetParent(owner.transform,false);
        leaves=obj.AddComponent<ParticleSystem>();leaves.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=leaves.main;main.playOnAwake=false;main.loop=false;main.maxParticles=40;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=.28f;main.startSize=.065f;main.gravityModifier=.22f;
        var emission=leaves.emission;emission.enabled=false;var shape=leaves.shape;shape.enabled=false;
        var renderer=obj.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;
        leafMesh=new Mesh{name="Qori small contact leaf"};leafMesh.vertices=new[]{new Vector3(0,.5f),new Vector3(.22f,0),new Vector3(0,-.5f),new Vector3(-.22f,0)};
        leafMesh.triangles=new[]{0,1,2,0,2,3};leafMesh.RecalculateNormals();renderer.mesh=leafMesh;
        Shader shader=Shader.Find("Sprites/Default");if(shader!=null){material=new Material(shader);renderer.sharedMaterial=material;}
        renderer.sortingOrder=5;
    }
    static AudioClip Rustle(string name,float duration,float bodyAmount,int seed)
    {
        const int rate=22050;float[] samples=new float[Mathf.CeilToInt(rate*duration)];
        System.Random random=new System.Random(seed);float low=0;
        for(int i=0;i<samples.Length;i++)
        {
            float t=(float)i/rate,p=(float)i/samples.Length;
            float noise=(float)random.NextDouble()*2-1;low=Mathf.Lerp(low,noise,.16f);
            float envelope=Mathf.Min(1,p*12)*Mathf.Exp(-p*5)*(1-p);
            samples[i]=Mathf.Clamp(((noise-low)*.12f+low*.32f+Mathf.Sin(t*Mathf.PI*2*95)*bodyAmount)*envelope,-1,1);
        }
        AudioClip clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
    }
    void Sound(AudioClip clip,float volume)
    {
        if(!tuning.sounds || audioSource==null)return;
        audioSource.pitch=1f+(FootSounds%3-1)*.035f;
        audioSource.PlayOneShot(clip,tuning.volume*volume);
    }
    void Emit(Vector3 position,int count,float strength)
    {
        if(!tuning.leafParticles || leaves==null)return;
        if(!leaves.isPlaying)leaves.Play();
        for(int i=0;i<count;i++)
        {
            var particle=new ParticleSystem.EmitParams();particle.position=position+Vector3.up*.025f;
            particle.velocity=new Vector3((i%2==0?-1:1)*(.15f+.1f*i),.5f+strength*.5f,0);
            particle.startColor=new Color(.43f,.49f,.25f,.6f);particle.startSize=.04f+strength*.025f;
            particle.rotation=i*73f;particle.startLifetime=.2f+strength*.15f;leaves.Emit(particle,1);
        }
    }
    void LateUpdate()
    {
        if(movement==null || rig==null)return;
        if(reset!=movement.ResetVersion)
        {
            reset=movement.ResetVersion;foot=rig.FootstepVersion;landing=movement.LandingVersion;
            attached=thread!=null?thread.AttachmentVersion:0;released=thread!=null?thread.ReleaseVersion:0;
            if(leaves!=null)leaves.Clear();if(audioSource!=null)audioSource.Stop();return;
        }
        if(foot!=rig.FootstepVersion)
        {
            foot=rig.FootstepVersion;
            if(Time.time>=nextFoot && movement.IsGrounded && Mathf.Abs(movement.ObservedVelocity.x)>.3f)
            {
                nextFoot=Time.time+.085f;FootSounds++;Sound(footClip,.6f);Emit(rig.FootstepPosition,1,.2f);
            }
        }
        if(landing!=movement.LandingVersion)
        {
            landing=movement.LandingVersion;float strength=Mathf.InverseLerp(2,22,movement.LastLandingSpeed);
            if(strength>.05f)
            {
                LandingSounds++;Sound(landClip,.4f+strength*.6f);
                Vector3 point=movement.transform.position;point.y=movement.GetComponent<Collider2D>().bounds.min.y;
                Emit(point,Mathf.CeilToInt(strength*3),strength);
            }
        }
        if(thread!=null && (attached!=thread.AttachmentVersion || released!=thread.ReleaseVersion))
        {
            attached=thread.AttachmentVersion;released=thread.ReleaseVersion;ThreadSounds++;Sound(threadClip,.5f);
        }
    }
    void OnDestroy()
    {
        if(audioSource!=null)Destroy(audioSource);if(leaves!=null)Destroy(leaves.gameObject);
        if(ownsFoot && footClip!=null)Destroy(footClip);if(ownsLand && landClip!=null)Destroy(landClip);if(ownsThread && threadClip!=null)Destroy(threadClip);
        if(material!=null)Destroy(material);if(leafMesh!=null)Destroy(leafMesh);
    }
}
