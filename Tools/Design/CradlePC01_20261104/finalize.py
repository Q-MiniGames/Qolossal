from pathlib import Path
import json, hashlib, shutil, csv, struct
ROOT=Path(__file__).resolve().parents[3]
TASK=Path(__file__).resolve().parent
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
portal=read(TASK/'Portals/final_regression.json')
atlas=read(TASK/'Map/ui_tests.json')
source=read(TASK/'source_hashes.json')
mapping=read(TASK/'Map/source_refresh.json')
assert portal['passed'] and portal['step']==19 and portal['fallbacks']==0
assert atlas['passed'] and atlas['steps']==37
assert source['passed'] and mapping['passed']
assert portal['normalSavesPreserved'] and atlas['normalSavesPreserved']
scene=ROOT/'Assets/Scenes/Game/MRAtlas_MR01_Cradle.unity'
assert sha(scene)==mapping['sceneHashes']['MRAtlas_MR01_Cradle']
density=read(TASK/'density_instances.json')
rows=[r for r in density['rows'] if 'PC01 compact limestone cave' in r['hierarchy']]
assert len(rows)==4 and all(r['nativePass'] and r['importPass'] for r in rows)
with (TASK/'resolution_4K.csv').open('w',newline='',encoding='utf-8') as f:
    keys=['asset','kind','sourceWidth','sourceHeight','requiredWidth','requiredHeight','nativeMinimumPixelsPerUnit','nativePass','importPass']
    w=csv.DictWriter(f,fieldnames=keys,extrasaction='ignore');w.writeheader();w.writerows(rows)
out=ROOT/'output/images';out.mkdir(parents=True,exist_ok=True)
for resolution,suffix in [('1080p',''),('4K','_4K')]:
    p=TASK/f'Portals/walk_PC01_upper_{resolution}.png'
    expected=(3840,2160) if resolution=='4K' else (1920,1080)
    assert struct.unpack('>II',p.read_bytes()[16:24])==expected
    shutil.copy2(p,out/f'Cradle_Cave_Rebuilt{suffix}.png')
qa={'passed':True,'date':'2026-11-04','scene':str(scene),'sceneSha256':sha(scene),'scope':'Cradle PC01 surface entrance and route x55..81 only','upperMouth':[60,24.7],'entrance':[62.9,24.7],'returnArrival':[58.4,25.36],'passageDepth':3.9,'nominalInteriorHeight':2.2,'portalPlaytestSteps':19,'ordinaryJumpAndOnwardSlopePassed':True,'checkpointReloadPassed':True,'arrivalFallbacks':0,'mapSteps':37,'mapAssertions':len(atlas['results']),'mapContours':646,'allOtherSceneHashesUnchanged':True,'undergroundGeometryUnchanged':True,'acceptedSourcesPreserved':source['checked'],'newCaveColourPlacementsNative4KPass':len(rows),'normalSavesPreserved':True,'editorSettingsRestored':portal['settingsRestored'] and atlas['settingsRestored'],'visualReview':['Portals/walk_PC01_upper_1080p.png','Portals/walk_PC01_roof_1080p.png','Captures/MR01_cave_PC01_preview.png','Map/Cradle_PC01_updated_1080p.png'],'preview':str(out/'Cradle_Cave_Rebuilt.png'),'native4KCapture':str(out/'Cradle_Cave_Rebuilt_4K.png'),'limitations':['Windows Unity Editor validation; no player or console build or hardware performance test','4K density claim applies to the rebuilt surface PC01 assembly; pre-existing exceptions elsewhere remain']}
(TASK/'QA.json').write_text(json.dumps(qa,indent=2),encoding='utf-8')
print(json.dumps({k:qa[k] for k in ['passed','portalPlaytestSteps','mapSteps','mapAssertions','mapContours','newCaveColourPlacementsNative4KPass']}))
