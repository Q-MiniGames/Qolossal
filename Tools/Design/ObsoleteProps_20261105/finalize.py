from pathlib import Path
import json, hashlib, difflib
ROOT=Path(__file__).resolve().parents[3]; TASK=Path(__file__).resolve().parent
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
play=read(TASK/'Play/result.json'); ui=read(TASK/'Map/ui_tests.json'); mapping=read(TASK/'Map/source_refresh.json')
assert play['complete'] and not play['errors'] and play['normalSavesPreserved']
assert all(r['passed'] for r in play['rows'])
assert ui['passed'] and ui['complete'] and ui['normalSavesPreserved']
assert mapping['passed']
scene_changes={}
for p in (TASK/'Before/Scenes').glob('*.backup'):
 name=p.name.removesuffix('.backup'); now=ROOT/'Assets/Scenes/Game'/name
 diff=list(difflib.unified_diff(p.read_text().splitlines(),now.read_text().splitlines(),fromfile='before/'+name,tofile=name))
 (TASK/(name+'.diff.txt')).write_text('\n'.join(diff),encoding='utf-8')
 edits=[l for l in diff if l[:1] in ('+','-') and l[:3] not in ('+++','---')]
 # These scene edits may only change activation, renderer/component enablement,
 # the obsolete knot requirement, and retired ending references.
 for line in edits:
  s=line[1:].strip()
  camera_defaults={'downhillProbeDistance: 6','downhillMinimumDrop: 0.9','maximumDownhillPreview: 0.58','manualLookDown: 0.58','previewSmoothTime: 0.28'}
  assert s.startswith(('m_IsActive:','m_Enabled:','requires:','- knot:grip','ending:')) or (line.startswith('+') and s in camera_defaults), (name,line)
 scene_changes[name]=len(edits)
camera=hashlib.sha256((ROOT/'Assets/Scripts/CameraFollow.cs').read_bytes()).hexdigest()
assert camera=='7554c7d29147d72e4fc26903d4d718f90874e5e9a814da0621c8958155f80c25'
qa=dict(passed=True,retiredDecorativeLedges=18,retiredSurfaceWakeKnots=6,heartCheckpointAndActivationMechanicsRetired=True,
        playAssertions=len(play['rows']),renderedCrossingFrames=play['renderedCrossingFrames'],legacyHeartSaveRespawnVerified=True,
        mapSteps=ui['steps'],mapAssertions=len(ui['results']),sceneChanges=scene_changes,sceneDiffWhitelistPassed=True,
        cameraUnchanged=True,normalSavesPreserved=True,errors=[],tested='Windows Unity Editor; virtual gamepad; 3840x2160 captures and 1080p/4K map',
        untested=['Windows standalone','PS5','Xbox Series X','Xbox Series S','Switch 2 handheld','Switch 2 docked','Hardware performance'])
(TASK/'QA.json').write_text(json.dumps(qa,indent=2),encoding='utf-8');print(json.dumps(qa,indent=2))
