from pathlib import Path
p=Path('Tools/Design/UndergroundRelocation_20261030/map_playtest.cs');s=p.read_text(encoding='utf-8-sig').replace('Tools/UndergroundRelocation_20261030/','Tools/CompactTunnelMap_20261030/')
s=s.replace('System.Action labelCheck=()=>{','System.Action labelCheck=()=>{check(atlas().Viewport.GetComponentsInChildren<UnityEngine.UI.Text>().Length==0,"No visible area names or numbers");check(atlas().Canvas.GetComponentsInChildren<Qolossal.Atlas.AtlasZoneGlyph>().Length==7,"Seven terrain silhouettes replace named tabs");check(atlas().Data.edges.Where(e=>e.kind=="cave"&&atlas().Nodes[e.from].region==atlas().FocusedRegion).All(e=>e.path.All(p=>atlas().ViewBounds.Contains(p))),"Focused view contains complete tunnel routes");')
s=s.replace('check(atlas().Data.sceneDerived&&atlas().Data.contours.Length==949','check(atlas().Data.illustratedMap&&atlas().Data.sceneDerived&&atlas().Data.contours.Length==949')
s=s.replace('steps.Add((.5f,"Cradle capture",()=>capture("MR01_1080p")));','steps.Add((.5f,"Cradle capture",()=>{labelCheck();capture("MR01_1080p");}));')
Path('Tools/Design/MainWorldLayout/player_test.cs').write_text(s,encoding='utf-8')
print('Prepared compact-map UI tests.')
