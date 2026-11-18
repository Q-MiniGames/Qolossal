from pathlib import Path
import json,hashlib,runpy
p=Path(__file__).parent
runpy.run_path(str(p/'verify_scene_changes.py'))
r=json.loads((p/'Play/result.json').read_text())
rows={v['label']:v for v in r['rows']}
baseline=json.loads((p.parent/'CisternContinuous_20261106/scene_hashes_before.json').read_text())
edited={x.name for x in (p/'Before').glob('*.unity')}
others=all(hashlib.sha256(Path(k).read_bytes()).hexdigest()==v for k,v in baseline.items() if Path(k).name not in edited)
qa=dict(passed=r['complete'] and not r['errors'] and all(v['passed'] for v in rows.values()) and r['normalSavesPreserved'] and others,
    solidCaveLedgeFeet=rows['solid_ledge_idle']['last'][1],solidSurfaceLedgeFeet=rows['surface_ledge_idle']['last'][1],
    caveFloorFeet=rows['cave_main_floor']['last'][1],qvaleGroundFeet=rows['qvale_main_ground']['last'][1],
    surfaceLedgeAutomaticPreview=rows['surface_ledge_idle']['last'][12],
    jumpFramingRetained=all(v[15]==1 for name in ['solid_ledge_jump','platform_jump'] for v in rows[name]['samples']),
    frameCount=sum(v['frames'] for v in rows.values()),normalSavesPreserved=r['normalSavesPreserved'],
    unchangedScenes=others,cameraOnlySceneEdits=json.loads((p/'scene_changes.json').read_text()),errors=r['errors'])
(p/'QA.json').write_text(json.dumps(qa,indent=2));print(json.dumps(qa,indent=2));assert qa['passed']
s=(p.parent/'PlatformCamera_20261106/start_user_trial.cs').read_text().replace('PlatformCamera_20261106/','LedgeCamera_20261106/').replace('1630f','1610f')
(p/'start_user_trial.cs').write_text(s)
(p/'resume_save.json').write_bytes((p.parent/'PlatformCamera_20261106/resume_save.json').read_bytes())
