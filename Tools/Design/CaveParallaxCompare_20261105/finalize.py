from pathlib import Path
import json,hashlib
p=Path(__file__).parent
def read(f):return json.loads((p/f).read_text(encoding='utf-8-sig'))
render=read('render_checks.json'); motion=read('motion_measurements.json'); play=read('Play/result.json')
assert render['tested']==252 and not render['failures'] and render['restored'] and render['outsideUnchanged'] and render['missing']==0
assert motion['passed']
assert play['complete'] and not play['errors'] and play['normalSavesPreserved'] and play['settingsRestored'] and play['keyboardPassed'] and play['padPassed']
assert all(r['passed'] for r in play['rows'])
hashes=read('scene_hashes_before.json')['sceneHashes']
assert all(hashlib.sha256(Path('Assets/Scenes/Game',n+'.unity').read_bytes()).hexdigest()==h for n,h in hashes.items())
qa=dict(passed=True,comparisonRooms=['A04','A05'],materialOverrides=render['overrides'],seamChecks=render['tested'],outsideRoomsUnchanged=render['outsideUnchanged'],originalsRestored=render['restored'],motionMeasurements=motion,playSteps=play['steps'],renderedFrames=sum(r['frames'] for r in play['rows']),keyboardPassed=play['keyboardPassed'],controllerPassed=play['padPassed'],all13ScenesUnchanged=True,normalSavesPreserved=True,errors=[],scope='Temporary Windows Editor comparison only')
(p/'QA.json').write_text(json.dumps(qa,indent=2));print(json.dumps(qa,indent=2))
