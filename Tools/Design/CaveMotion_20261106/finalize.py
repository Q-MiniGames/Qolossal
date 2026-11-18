from pathlib import Path
import json,hashlib
p=Path(__file__).parent
play=json.loads((p/'Play/result.json').read_text())
render=json.loads((p/'render_checks.json').read_text())
motion=json.loads((p/'motion_measurements.json').read_text())
rows={r['label']:r for r in play['rows']}
glide=rows['cave_glide']['samples']
coverage=all(s[14]-s[3]*16/9>=1551 and s[14]+s[3]*16/9<=1659 and s[4]-s[3]>=82 and s[4]+s[3]<=118 for s in glide)
before=json.loads((p.parent/'CisternContinuous_20261106/scene_hashes_before.json').read_text())
unchanged=all(hashlib.sha256(Path(f).read_bytes()).hexdigest()==h for f,h in before.items())
qa=dict(passed=play['complete'] and not play['errors'] and all(r['passed'] for r in play['rows']) and not render['failures'] and motion['passed'] and coverage and unchanged,
 caveAutomaticPreviewMax=max(s[12] for s in rows['cave_edge_idle']['samples']),manualCavePreviewMax=max(s[12] for s in rows['manual_cave_look_down']['samples']),surfaceAutomaticPreviewMax=max(s[12] for s in rows['surface_edge']['samples']),glidingFrames=sum(s[13]>0 for s in glide),glideHorizontalTravel=max(s[5] for s in glide)-min(s[5] for s in glide),glideVerticalTravel=max(s[10] for s in glide)-min(s[10] for s in glide),glideViewWithinPainting=coverage,renderedFrames=sum(r['frames'] for r in play['rows']),seamSamples=render['tested'],normalSavesPreserved=play['normalSavesPreserved'],all13ScenesUnchanged=unchanged,native4KArtAccepted=False,errors=play['errors'])
(p/'QA.json').write_text(json.dumps(qa,indent=2));print(json.dumps(qa,indent=2));assert qa['passed']
resolution=dict(native=[2172,724],worldRect=[1605,100,108,36],requiredPixelsPerUnit=216,requiredNative=[23328,7776],sourcePixelsPerWorldUnit=2172/108,native4KPassed=False,sourceResized=False,scope='Expanded motion/composition draft for the tested ledge and glide; not all tall shafts or a final production canvas')
(p/'resolution.json').write_text(json.dumps(resolution,indent=2))
