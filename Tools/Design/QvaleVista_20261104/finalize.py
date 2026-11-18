from pathlib import Path
import json, shutil, hashlib, struct
root=Path(__file__).resolve().parents[3]; task=Path(__file__).resolve().parent
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
before=read(task/'before/result.json'); after=read(task/'after/result.json'); integrity=read(task/'scene_integrity.json')
assert after['complete'] and after['steps']==49 and not after['errors']
assert len(after['rows'])==17 and all(r['passed'] for r in after['rows'])
assert after['normalSavesPreserved'] and after['settingsRestored'] and integrity['passed']
motion={}
for label in ('street_jump','street_high_launch'):
    row=next(r for r in after['rows'] if r['label']==label)
    feet=[s[4]+(s[1]-.5)*2*s[3] for s in row['samples']]
    motion[label]={'worldFootTravel':max(feet)-min(feet),'airborneRenderedFrames':sum(s[6]==0 for s in row['samples'])}
    assert motion[label]['worldFootTravel']>1.5 and motion[label]['airborneRenderedFrames']>8
out=root/'output/images';out.mkdir(parents=True,exist_ok=True)
for suffix,w,h in [('1080p',1920,1080),('4K',3840,2160)]:
    source=task/f'after/street_129_{suffix}.png'
    assert struct.unpack('>II',source.read_bytes()[16:24])==(w,h)
    shutil.copy2(source,out/f'Qvale_Camera_Fixed_{suffix}.png')
qa={'passed':True,'date':'2026-11-04','windowsEditor':True,'authoredQvaleZoomPreserved':7.6,'beforeFootViewport':before['rows'][0]['minFeet'],'afterSettledFootViewport':after['rows'][0]['last'][1],'cases':17,'steps':49,'renderedFrames':sum(r['frames'] for r in after['rows']),'allSixAuthoredVistasVerified':True,'jumpMotionVerified':motion,'normalSavesPreserved':True,'editorSettingsRestored':True,'all13SceneHashesUnchanged':True,'runtimeErrors':after['errors'],'codeHashes':{p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in ['Assets/Scripts/CameraFollow.cs','Assets/Scripts/Prototype/VistaZone.cs']},'untested':['Player builds','Console hardware','CPU/GPU performance','Peak memory','Physical controllers']}
(task/'QA.json').write_text(json.dumps(qa,indent=2),encoding='utf-8');print(json.dumps(qa,indent=2))
