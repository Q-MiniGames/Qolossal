from pathlib import Path
import json, subprocess, sys, difflib
ROOT=Path(__file__).resolve().parents[3]; TASK=Path(__file__).resolve().parent
REVIEW=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview')
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
old=read(TASK/'Before/inventory.json'); new=read(TASK/'Map/inventory_fresh.json')
for k in ('areas','rooms','caves'):assert old[k]==new[k]
changed=[s for s in new['sceneHashes'] if old['sceneHashes'][s]!=new['sceneHashes'][s]]
assert changed==['MRAtlas_MR05_Ribwood'],changed
old_s=read(TASK/'Before/surface.json'); new_s=read(REVIEW/'ActualSlopeComparison_20261030/scene_geometry.json')
for a,b in zip(old_s['scenes'],new_s['scenes']):
 for k in a:
  if k!='sha256':assert a[k]==b[k],(a['scene'],k)
a=(TASK/'Before/MRAtlas_MR05_Ribwood.unity.backup').read_text().splitlines();b=(ROOT/'Assets/Scenes/Game/MRAtlas_MR05_Ribwood.unity').read_text().splitlines()
diff=list(difflib.unified_diff(a,b));edits=[l for l in diff if l[:1] in ('+','-') and l[:3] not in ('+++','---')]
assert len(edits)==38 and all(l in ('-  m_IsActive: 1','+  m_IsActive: 0') for l in edits),edits
(TASK/'scene.diff.txt').write_text('\n'.join(diff))
(ROOT/'Tools/Design/ConnectedWorldMap_20261030/current_scene_inventory.json').write_text(json.dumps(new,indent=2),encoding='utf-8')
for script in ('build_layout.py','viewer.py','export_runtime.py','verify.py'):
 subprocess.run([sys.executable,str(ROOT/'Tools/Design/MainWorldLayout'/script)],cwd=ROOT,check=True)
before=read(TASK/'Before/AtlasData.json');after=read(ROOT/'Assets/Resources/QolossalAtlas/AtlasData.json')
for k in ('nodes','edges','caves','transforms','contours'): 
 if k in before:assert before[k]==after[k],k
qa=dict(passed=True,removedMatchingDecorations=19,scene='MRAtlas_MR05_Ribwood',onlySceneChanges='19 decorative GameObjects disabled',allTerrainAndCollisionUnchanged=True,allOtherScenesUnchanged=True,mapGeometryUnchanged=True)
(TASK/'QA.json').write_text(json.dumps(qa,indent=2));print(json.dumps(qa))
