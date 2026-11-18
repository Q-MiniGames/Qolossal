from pathlib import Path
import csv, hashlib, json, shutil, struct

task=Path(__file__).resolve().parent
root=task.parents[2]
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
seams=read(task/'seams.json');pan=read(task/'parallax.json');play=read(task/'Play/result.json')
density=read(task/'density.json');scenes=read(task/'scene_integrity.json');captures=read(task/'Captures/result.json')
assert seams['tested']==126 and not seams['failures'] and seams['connected']==63 and seams['missing']==0
assert abs(pan['measuredPixelShift'][0]-pan['expectedShift'])<1 and pan['correlation']>.9
assert play['complete'] and not play['errors'] and all(r['passed'] for r in play['rows'])
assert play['normalSavesPreserved'] and play['settingsRestored']
assert not scenes['changed'] and scenes['sceneCount']==13
assert density['rooms']==89 and not density['errors'] and not captures['shaderErrors']
unique={r['path']:r for r in density['rows']}
assert len(unique)==60
with (task/'RESOLUTION_4K.csv').open('w',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=list(next(iter(unique.values())).keys()))
    writer.writeheader();writer.writerows(unique.values())
# Camera behavior remains exactly the completed Qvale jump fix.
camera=read(root/'Tools/Design/QvaleJumpCamera_20261104/QA.json')['codeHashes']
assert all(hashlib.sha256((root/p).read_bytes()).hexdigest()==h for p,h in camera.items())
output=root/'output/images';output.mkdir(parents=True,exist_ok=True)
for suffix,w,h in [('1080p',1920,1080),('4K',3840,2160)]:
    source=task/f'Play/walk_into_passage_{suffix}.png'
    assert struct.unpack('>II',source.read_bytes()[16:24])==(w,h)
    shutil.copy2(source,output/f'Cave_Passage_Continuous_{suffix}.png')
qa=dict(passed=True,date='2026-11-04',rooms=89,passages=63,roomMouthChecks=126,
        maxMouthColorError=max(r['error'] for r in seams['rows']),measuredParallax=pan,
        playModeCases=len(play['rows']),renderedPlayFrames=sum(r['frames'] for r in play['rows']),
        normalSavesPreserved=True,editorSettingsRestored=True,unchangedScenes=13,cameraCodeUnchanged=True,
        nativeColorSources=60,closest4KPixelsPerWorldUnit=216,sourceArtHashes={p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in unique},
        platforms={'tested':'Windows Unity Editor; virtual gamepad passage walk in both directions; native 3840x2160 and 1920x1080 captures',
          'notTested':['Windows player build','PS5','Xbox Series X','Xbox Series S','Switch 2 handheld','Switch 2 docked','Physical controllers','CPU/GPU performance','Peak memory'],
          'textureImportProfiles':'Unchanged'},
        limitations=['A shared native perimeter texture remains outside the unique landmark composition; this change removes screen locking and dark passage cuts, not all theme reuse.'])
(task/'QA.json').write_text(json.dumps(qa,indent=2))
print(json.dumps({k:v for k,v in qa.items() if k not in ('sourceArtHashes','platforms')},indent=2))
