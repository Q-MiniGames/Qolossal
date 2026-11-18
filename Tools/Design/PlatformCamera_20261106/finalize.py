from pathlib import Path
import json,hashlib
p=Path(__file__).parent
r=json.loads((p/'Play/result.json').read_text()); rows={v['label']:v for v in r['rows']}
before=json.loads((p.parent/'CisternContinuous_20261106/scene_hashes_before.json').read_text())
unchanged=all(hashlib.sha256(Path(k).read_bytes()).hexdigest()==v for k,v in before.items())
qa=dict(passed=r['complete'] and not r['errors'] and all(v['passed'] for v in r['rows']) and r['normalSavesPreserved'] and unchanged,
groundFootViewport=rows['ground_idle']['last'][1],platformFootViewport=rows['platform_idle']['last'][1],platformFramingRetainedThroughJump=all(v[15]==1 for v in rows['platform_jump']['samples']),platformJumpMaxCameraStep=rows['platform_jump']['maxCameraStep'],qvaleJumpCameraTravel=rows['qvale_jump']['cameraTravel'],renderedFrames=sum(v['frames'] for v in r['rows']),classification=json.loads((p/'Play/classification.json').read_text()),normalSavesPreserved=r['normalSavesPreserved'],all13ScenesUnchanged=unchanged,errors=r['errors'])
(p/'QA.json').write_text(json.dumps(qa,indent=2));print(json.dumps(qa,indent=2));assert qa['passed']
