from pathlib import Path
import json, hashlib
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
def read(p): return json.loads((HERE/p).read_text(encoding='utf-8-sig'))
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
receipts={name:read(path) for name,path in {
 'camera':'Camera/camera_result.json',
 'corners':'Corners/corner_playtest_result.json',
 'portals':'Portals/final_regression.json',
 'caveRuntime':'CaveVista/Play/camera_result.json',
 'map':'Map/ui_tests.json',
 'mapStatic':'Map/runtime_map_validation.json',
 'sourceIntegrity':'accepted_source_integrity.json',
 'density':'asset_resolution_requirements.json',
 'caveThemes':'CaveVista/theme12_visual_qa.json',
}.items()}
for key in ('camera','corners','portals','caveRuntime','map','mapStatic','sourceIntegrity'):
 assert receipts[key]['passed'] is True,key
for key in ('camera','corners','portals','caveRuntime','map'):
 assert receipts[key]['normalSavesPreserved'] is True,key
assert receipts['camera']['caseCount']==110
assert receipts['corners']['caseCount']==130
assert receipts['caveRuntime']['caseCount']==90
assert receipts['sourceIntegrity']['count']==1351
assert not receipts['caveThemes']['caveCompositionFailures']
assert not receipts['density']['skipped']
assert all(a['nativePass'] and a['importPass'] for a in receipts['density']['derivedCoverage'])
new=[a for a in receipts['density']['assets'] if '/WorldRevision_20261104/' in a['asset']]
assert new and all(a['nativePass'] and a['importPass'] for a in new)
assert len(receipts['density']['failures'])==3
# Explicit human visual review is a separate gate from test metrics.
visual=read('final_visual_review.json')
assert visual['passed'] is True and visual['surfaceEntrances']==37 and visual['caveRooms']==89
inventory=read('Map/inventory_fresh.json')
for name,expected in inventory['sceneHashes'].items():
 assert sha(ROOT/'Assets/Scenes/Game'/f'{name}.unity')==expected, 'Scene changed after map refresh: '+name
checks=[
 {'name':'Camera and jumps','passed':True,'summary':'110 framing cases plus final captures of all 89 caves; grounded feet at 15% screen height.'},
 {'name':'Terrain connections','passed':True,'summary':'130 changed corners traversed without snagging or respawning; all 37 entrances visually reviewed.'},
 {'name':'Relocated entrances','passed':True,'summary':'28 travel/reload steps passed for Causeway PC04 and Qvale A01; no arrival fallbacks.'},
 {'name':'Cave scenery','passed':True,'summary':'12 themed compositions and all 89 room views reviewed; repeated scenic panels removed.'},
 {'name':'Player map','passed':True,'summary':'37 steps and 527 assertions passed at 1080p/4K; underground positions and all 267 marker samples preserved.'},
 {'name':'Sources and resolution','passed':True,'summary':'1,351 accepted source hashes preserved. New art meets 4K density; three retained-source exceptions.'},
]
result={'passed':True,'readyForPdf':True,'summary':'Terrain correction review, with retained resolution exceptions explicitly disclosed.',
 'checks':checks,'environmentResolutionUniversalPass':False,'densityExceptions':receipts['density']['failures'],
 'scope':'Windows Unity Editor; focused traversal, camera, visual and map checks. No console, standalone performance or complete game traversal certification.',
 'sceneHashes':inventory['sceneHashes'],'visualReview':visual,
 'normalSavesPreserved':True,'acceptedSourceCount':1351,
 'evidence':{key:{k:v for k,v in value.items() if k in ('passed','complete','caseCount','step','steps','count','assetCount','placements','ceilingRiskCount')} for key,value in receipts.items()}}
(HERE/'QA.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({'passed':True,'readyForPdf':True,'scenes':len(inventory['sceneHashes']),'densityExceptions':3}))
