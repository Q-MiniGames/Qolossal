if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
string prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
var result=new System.Collections.Generic.List<object>();
try{
 foreach(string name in new[]{"MRAtlas_MR02_Causeway","MRAtlas_MR04_Qvale"}){
  UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+name+".unity");
  result.Add(new{scene=name,report=WorldRevisionEntranceMoves.ApplyCurrent()});
 }
 UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+RegionalSceneCatalog.Underground+".unity");
 result.Add(new{scene=RegionalSceneCatalog.Underground,report=WorldRevisionEntranceMoves.SyncA01UndergroundCurrent()});
 foreach(var name in RegionalSceneCatalog.Surface){
  UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+name+".unity");
  result.Add(new{scene=name,corners=WorldRevisionSurfaceJoins.RoundSlopedCornersCurrent(),joins=WorldRevisionSurfaceJoins.BlendWalkingEndsCurrent()});
 }
 return result;
}finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prior);}
