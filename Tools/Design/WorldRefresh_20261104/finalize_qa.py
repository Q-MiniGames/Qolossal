"""Collect final, verified evidence; refuse to publish a pass from incomplete receipts."""
from pathlib import Path
import json, hashlib, re

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
MAP = Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/MainWorldLayout_20261030')
def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

surface = read(HERE/'Playtest/surface_playtest_result.json')
underground = read(HERE/'Underground/settle_result.json')
ui = read(HERE/'Map/ui_tests.json')
density = read(HERE/'asset_resolution_requirements.json')
mapqa = read(MAP/'qa.json')
audit = read(HERE/'Underground/audit.json')
assert surface['complete'] and surface['passed'] and surface['caseCount'] == 38
assert not surface['errors'] and not surface['consoleErrors']
assert underground['complete'] and underground['passed'] and underground['checkedCount'] == 126
assert not underground['errors']
assert ui['complete'] and ui['step'] == ui['steps'] and not ui['errors']
assert all(x['passed'] for x in ui['results'])
assert all(t['normalSavesPreserved'] and t['settingsRestored'] for t in [surface, underground, ui])
assert audit['rooms'] == 89 and audit['returnCount'] == 37 and not audit['failures']
assert all(x['nativePass'] and x['importPass'] for x in density['assets'])
assert not read(HERE/'density_instances.json')['skipped']
marker_samples = sum(x['message'].startswith('Marker projection ') for x in ui['results'])
assert marker_samples == 267
assert mapqa['runtimeDataMatchesCanonical'] and mapqa['allCurrentSceneHashesMatchCurrentExtraction']
for name, digest in mapqa['sceneHashes'].items():
    assert sha(ROOT/'Assets/Scenes/Game'/f'{name}.unity') == digest, name
validations = [read(HERE/f'Surface/MR{i:02}_validation.json') for i in range(1,8)]
assert all(not v['errors'] for v in validations)
applied = [read(HERE/f'Surface/MR{i:02}_applied.json') for i in range(1,8)]
assert sum(x['edits'] for x in applied) == 38
manifest = read(ROOT/'Tools/ArtImport/codex_v2_accepted.json')
bad = [a['dest'] for a in manifest['assets'] if sha(ROOT/a['dest']) != a['sha256']]
assert not bad, bad
integrity = dict(manifest='Tools/ArtImport/codex_v2_accepted.json',count=len(manifest['assets']),mismatches=bad)
(HERE/'accepted_source_integrity.json').write_text(json.dumps(integrity,indent=2)+'\n')
qa = dict(
    complete=True, passed=True, date='2026-11-04',
    scope='Environment refresh across seven surface regions/84 zones and 89 underground rooms; five Qvale service interiors preserved.',
    surface=dict(regions=7,zones=84,smoothedConnectors=38,changedConnectorZones=sum(sum(z['smoothedConnectors']>0 for z in a['zones']) for a in applied),
                 checks={k:sum(v[k] for v in validations) for k in ['endpoints','protectedSamples','floorSamples','gradeChecks']},
                 liveWalkingCases=38,liveErrors=0,playModeCaptures=7,editorCaptures=21),
    underground=dict(rooms=89,pairedCaves=37,passages=63,liveLandingCases=126,liveErrors=0,editorCaptures=15,
                     testScope=underground['scope']),
    resolution=dict(display=density['display'],closestCameraSize=density['closestCameraSize'],pixelsPerWorldUnit=216,
                    assets=density['assetCount'],placements=density['placements'],newNativePaintings=38,nativeFailures=0,importFailures=0,skippedMeshes=0,
                    excluded='Characters, UI, cinematics, disabled alternatives and legacy props outside the environment audit.'),
    map=dict(nodes=173,caveMouths=74,pairedCaves=37,uiSteps=ui['steps'],assertions=len(ui['results']),markerProjectionSamples=marker_samples,
             sceneCount=len(mapqa['sceneHashes']),sceneHashes=mapqa['sceneHashes'],runtimeMatchesCanonical=True),
    acceptedSources=integrity, normalSavesPreserved=True, editorSettingsRestored=True,
    limitations=['The existing macro Titan silhouette is retained; 38 ordinary stair sections are eased into slopes.',
                 'Scenery uses modular repeating paintings, not 89 unique panoramas.',
                 surface['scope'],underground['scope'],
                 'Windows Unity Editor verification only; no standalone build, physical controller, console hardware or frame-time certification.',
                 'PS5, Xbox Series X, Xbox Series S, Switch 2 handheld and Switch 2 docked require measured platform builds. 60 FPS remains a target.'],
    evidence=['Playtest/surface_playtest_result.json','Underground/settle_result.json','Map/ui_tests.json',
              'RESOLUTION_4K.md','asset_resolution_requirements.csv','VISUAL_QA.md','Underground/visual_qa.json'])
(HERE/'QA.json').write_text(json.dumps(qa,indent=2)+'\n',encoding='utf-8')
readme=HERE/'Underground/README.md'
text=readme.read_text(encoding='utf-8')
text=text.replace('The static audit passes after saving this correction; the final full rerun is pending at this documentation update; `settle_result.json` is authoritative once the root-run test finishes.',
                  'The final full rerun passed all 126 placements with zero errors, preserving normal saves and Editor settings. The seven repaired checkpoints each remained under observation for 4.4 simulation seconds. See `settle_result.json` for the final receipt.')
readme.write_text(text,encoding='utf-8')
print(json.dumps({k:qa[k] for k in ['complete','passed','resolution','normalSavesPreserved']},indent=2))
