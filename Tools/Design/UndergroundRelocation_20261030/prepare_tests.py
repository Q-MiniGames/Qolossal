from pathlib import Path
W=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal');D=Path(__file__).parent
s=(W/'Tools/SummitCaves_20261028/regression.cs').read_text(encoding='utf-8-sig')
s=s.replace('Tools/SummitCaves_20261028/','Tools/UndergroundRelocation_20261030/').replace('{"count",8}','{"count",3}')
s=s.replace('new[]{"MR07_C01","MR07_C02","MR07_C03","MR07_C04","MR07_C05","MR07_C06","PC12","E01"}','new[]{"MR06_C01","PC11","PC12"}')
s=s.replace('string output=', 'string originalScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;\nstring output=',1)
anchor='fileField.SetValue(null,testSave);dataField.SetValue(null,null);UnityEditor.EditorSettings'
extra='''// Verify durable checkpoint IDs resolve after a fresh scene load at the new positions.
foreach(var id in new[]{"MR06_C01","PC11","PC12"}){
 steps.Add((id+" load checkpoint room",null,1,()=>{neutral();UnityEngine.SceneManagement.SceneManager.LoadScene(RegionalSceneCatalog.Underground);}));
 steps.Add((id+" save checkpoint ID",RegionalSceneCatalog.Underground,.5,()=>{var room=UnityEngine.Object.FindObjectsByType<UndergroundRoom>(FindObjectsSortMode.None).Single(r=>r.roomId==id);var cp=room.GetComponentsInChildren<Checkpoint>().First();GameSave.SetCheckpoint(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,cp.CheckpointId);dataField.SetValue(null,null);UnityEngine.SceneManagement.SceneManager.LoadScene(RegionalSceneCatalog.Underground);}));
 steps.Add((id+" restored checkpoint",RegionalSceneCatalog.Underground,1,()=>{var room=UnityEngine.Object.FindObjectsByType<UndergroundRoom>(FindObjectsSortMode.None).Single(r=>r.roomId==id);var cp=room.GetComponentsInChildren<Checkpoint>().First();check(Vector2.Distance(player().transform.position,cp.SpawnPosition)<1.2f&&player().HasGroundContact,"Checkpoint restore mismatch "+id+" actual="+player().transform.position+" expected="+cp.SpawnPosition);}));
}
'''
s=s.replace(anchor,extra+anchor)
s=s.replace('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/"+pairs[0].scene+".unity");System.IO.File.WriteAllText', 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene(originalScene);System.IO.File.WriteAllText')
(D/'playtest.cs').write_text(s,encoding='utf-8')
s=(W/'Tools/AtlasArtwork_20261030/Final/capture.cs').read_text(encoding='utf-8-sig').replace('Tools/AtlasArtwork_20261030/Final/','Tools/UndergroundRelocation_20261030/')
s=s.replace('steps.Add((.5f,"close"', '''foreach(var rid in new[]{"MR02","MR03","MR04","MR05","MR06","MR07"}){
 steps.Add((.25f,"focus "+rid,()=>atlas().FocusRegion(rid)));
 steps.Add((.35f,"verify "+rid,()=>{check(atlas().Data.nodes.Where(n=>n.region==rid).All(n=>atlas().ViewBounds.Contains(n.Bounds.min)&&atlas().ViewBounds.Contains(n.Bounds.max)),rid+" contains upper and underground");capture(rid+"_1080p");}));
}
steps.Add((.3f,"actual geometry",()=>{check(atlas().Data.sceneDerived&&atlas().Data.contours.Length>900,"Actual terrain contours loaded");check(!atlas().Canvas.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).Single().enabled,"Old unregistered painting hidden");}));
steps.Add((.5f,"close"''')
(D/'map_playtest.cs').write_text(s,encoding='utf-8')
print('Prepared traversal, checkpoint reload and map UI verification.')
