from pathlib import Path
import hashlib,json
p=Path(__file__).parent
render=json.loads((p/'render_checks.json').read_text())
play=json.loads((p/'Play/result.json').read_text())
before=json.loads((p/'scene_hashes_before.json').read_text())
unchanged=all(hashlib.sha256(Path(name).read_bytes()).hexdigest()==digest for name,digest in before.items())
composition=not render['failures'] and render['restored'] and render['outsideUnchanged'] and play['complete'] and not play['errors'] and all(r['passed'] for r in play['rows']) and play['normalSavesPreserved'] and unchanged
asset=Path('Assets/Art/Prototypes/CisternContinuous_20261106/CisternPanorama.png')
qa=dict(compositionTrialPassed=composition,productionArtReady=False,native4KPassed=False,remaining='New native detail at 10368x3456 for this placement; full cave coverage beyond the local route is not authored',seamChecks=render['tested'],playSteps=play['steps'],renderedFrames=sum(r['frames'] for r in play['rows']),keyboardPassed=play['keyboardPassed'],controllerPassed=play['padPassed'],normalSavesPreserved=play['normalSavesPreserved'],allScenesUnchanged=unchanged,sceneCount=len(before),artSha256=hashlib.sha256(asset.read_bytes()).hexdigest(),errors=play['errors'],platform='Windows Unity Editor only; other supported hardware and performance not tested')
(p/'QA.json').write_text(json.dumps(qa,indent=2))
print(json.dumps(qa,indent=2))
assert composition
