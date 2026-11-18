using UnityEngine;

[ExecuteAlways, DefaultExecutionOrder(9999)]
public sealed class WorldRefreshWeather : MonoBehaviour
{
    public Camera targetCamera;
    public ParallaxLayer[] clouds = System.Array.Empty<ParallaxLayer>();
    public float[] authoredX = System.Array.Empty<float>();
    public float speed = .018f;
    public void Refresh(float elapsed)
    {
        var view=targetCamera!=null?targetCamera:Camera.main;if(view==null)return;
        for(int i=0;i<clouds.Length;i++)if(clouds[i]!=null){var p=clouds[i];float rate=1-p.FollowX;float origin=i<authoredX.Length?authoredX[i]:i*20;
            p.baseX=view.transform.position.x*rate+Mathf.Repeat(origin-view.transform.position.x*rate+elapsed*speed+40,80)-40;p.Refresh(view);}
    }
    void LateUpdate()=>Refresh(Application.isPlaying?Time.timeSinceLevelLoad:0);
}
