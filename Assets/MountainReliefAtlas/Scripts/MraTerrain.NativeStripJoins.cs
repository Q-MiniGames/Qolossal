using UnityEngine;

public sealed partial class MraTerrain
{
    [Tooltip("Opt-in native top painting blend into Fill at exposed convex cliff ends. Does not alter collision or texture scale. Zero preserves legacy art.")]
    [Range(0f,.5f)] public float nativeStripEndBlend;

    bool NativeStripSegment(int i)
    {
        if(i<0||i>=surface.Length-1)return false;
        var d=surface[i+1]-surface[i];return d.x>=.0001f&&Mathf.Abs(d.y)/d.x<=maxStripSlope;
    }
    // Contiguous walkable strip extent, excluding concave floor/wall joins.
    // Bevels steeper than maxStripSlope use the Fill; the last shallow part
    // takes the native walking painting without a rectangular soil column.
    void NativeStripLimits(int segment,out float start,out float end,out float blend,out bool left,out bool right)
    {
        start=end=blend=0;left=right=false;
        if(nativeStripEndBlend<=0||HasStepSet)return;
        int first=segment,last=segment;
        while(NativeStripSegment(first-1))first--;
        while(NativeStripSegment(last+1))last++;
        start=surface[first].x;end=surface[last+1].x;
        left=first>0&&surface[first].y>surface[first-1].y;
        right=last+2<surface.Length&&surface[last+2].y<surface[last+1].y;
        if(first==0)left=!NeighbourGround(start,true,out float yL)||yL<surface[0].y-.4f;
        if(last==surface.Length-2)right=!NeighbourGround(end,false,out float yR)||yR<surface[last+1].y-.4f;
        blend=Mathf.Min(nativeStripEndBlend,(end-start)*.25f);
    }
    static float NativeStripAlpha(float x,float start,float end,float blend,bool left,bool right)
    {
        if(blend<=0)return 1;
        float a=1;if(left)a*=Mathf.SmoothStep(0,1,Mathf.Clamp01((x-start)/blend));if(right)a*=Mathf.SmoothStep(0,1,Mathf.Clamp01((end-x)/blend));return a;
    }
}
