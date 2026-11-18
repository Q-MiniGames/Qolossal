#if UNITY_EDITOR
using UnityEngine;
public sealed class KeepersNurseryGradeLabel:MonoBehaviour {
 public static string DisplayText="Brightness: measuring...";GUIStyle style;
 void OnGUI(){if(!Application.isPlaying)return;if(style==null)style=new GUIStyle(GUI.skin.box){fontSize=20,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(14,14,8,8)};GUI.Label(new Rect(Mathf.Max(16,Screen.width-610),24,590,82),DisplayText,style);}
}
#endif
