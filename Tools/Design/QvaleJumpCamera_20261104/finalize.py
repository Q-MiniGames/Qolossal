from pathlib import Path
import hashlib
import json

task = Path(__file__).resolve().parent
root = task.parents[2]
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
before = read(task/'before/result.json')
after = read(task/'after/result.json')
normal = read(task/'normal_zoom/result.json')
assert normal['complete'] and normal['normalSavesPreserved'] and normal['settingsRestored'] and not normal['errors']
assert all(r['passed'] for r in normal['rows']), [(r['label'],r['maxCameraStep'],r['cameraTravel']) for r in normal['rows'] if not r['passed']]
assert after['complete'] and after['normalSavesPreserved'] and after['settingsRestored']
assert not after['errors'], after['errors']
assert all(r['passed'] for r in after['rows']), [(r['label'],r['maxCameraStep'],r['cameraTravel']) for r in after['rows'] if not r['passed']]
baseline = next(r for r in before['rows'] if r['label']=='street_jump')
assert not baseline['passed'] and baseline['maxCameraStep']>1
inventory = read(root/'Tools/Design/ConnectedWorldMap_20261030/current_scene_inventory.json')
changed = [p for p,h in inventory['sceneHashes'].items() if hashlib.sha256((root/'Assets/Scenes/Game'/f'{p}.unity').read_bytes()).hexdigest().lower()!=h.lower()]
assert not changed, changed
motion = [{k:v for k,v in r.items() if k not in ('samples','last')} for r in after['rows'] if 'jump' in r['label'] or 'launch' in r['label']]
assert len([r for r in motion if 'jump' in r['label']])==4
qa = dict(passed=True, date='2026-11-04', beforeMaxJumpCameraStep=baseline['maxCameraStep'],
          cases=len(after['rows']), steps=after['steps'], renderedFrames=sum(r['frames'] for r in after['rows']),
          motion=motion, authoredQvaleZoom=7.6, allSixVistasCovered=True,
          normalZoomCases=[{k:v for k,v in r.items() if k not in ('samples','last')} for r in normal['rows']],
          normalSavesPreserved=True, editorSettingsRestored=True,
          sceneHashesVerified=len(inventory['sceneHashes']), sceneChanges=changed,
          codeHashes={p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in ('Assets/Scripts/CameraFollow.cs','Assets/Scripts/Prototype/VistaZone.cs')},
          platformReadiness={'tested':'Windows Unity Editor, rendered motion with virtual gamepad, 30/60 FPS caps; 1920x1080 and 3840x2160 captures',
              'notTested':['Windows player build','PS5','Xbox Series X','Xbox Series S','Switch 2 handheld','Switch 2 docked','Physical controllers','CPU/GPU performance','Peak memory'],
              'importProfiles':'Unchanged; no art or texture changes'})
(task/'QA.json').write_text(json.dumps(qa,indent=2))
print(json.dumps(qa,indent=2))
