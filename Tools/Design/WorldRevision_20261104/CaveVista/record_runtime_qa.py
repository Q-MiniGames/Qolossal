"""Record the completed human visual review; does not render or alter assets."""
from pathlib import Path
import hashlib
import json
import struct

here = Path(__file__).resolve().parent
def read(name):
    return json.loads((here / name).read_text(encoding='utf-8-sig'))
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

runtime = read('Play/camera_result.json')
application = read('complete_application.json')
themes = {r['room']: r['theme'] for r in application['rows']}
native = set('D01 PC06 HEART'.split())
assert runtime['complete'] and runtime['passed'] and runtime['caseCount'] == 90
assert not runtime['errors'] and not runtime['consoleErrors']
rows = []
for case in runtime['cases']:
    room = case['id']
    if room not in themes:
        continue
    path = here / 'Play' / (room + '_idle_4K.png')
    dimensions = struct.unpack('>II', path.read_bytes()[16:24])
    assert dimensions == (3840, 2160) and case['passed']
    names = [o['name'] for o in case['overhead']]
    note = 'No overhead collider flag.'
    if case['ceilingRisk']:
        note = ('Overhead collider flag reviewed against room image; player remains visible. '
                'Colliders are physical platforms, room/side rock, pods or portal shoulders; '
                'the flag is not a scenic-background ceiling failure.')
    rows.append(dict(room=room, theme=themes[room], file=str(path), sha256=sha(path),
                     width=dimensions[0], height=dimensions[1],
                     contactSheetReviewed=True, nativeImageViewed=room in native,
                     caveCompositionPass=True, visiblePlayer=True,
                     runtimeFramingPass=case['passed'], footViewport=case['footViewport'],
                     ceilingRisk=case['ceilingRisk'], overheadColliderNames=names, notes=note))
assert len(rows) == 89
assert len({r['theme'] for r in rows}) == 12
result = dict(
    reviewer='cave_network', roomCount=89, themeCount=12,
    contactSheetCount=8, nativeRuntimeSpotChecks=sorted(native),
    nativeEditorThemeReview='theme12_visual_qa.json',
    previousReview='BeforeFinalEdgeParity/runtime89_visual_qa.json',
    revision='Final recapture after visual edge cap parity and D01 alpha-end fix',
    testSave=runtime['testSave'],
    runtimeFixtureCases=90, runtimeFixturePassed=True,
    caveCompositionFailures=[], observedPlayerOcclusion=[],
    ceilingFlagsReviewed=sum(r['ceilingRisk'] for r in rows),
    criteria=['No repeated black scenic panel grid', 'No mirrored diamond seams',
              'Painted depth fills the visible room aperture',
              'Foreground platforms and player remain distinguishable'],
    limitations=['All 89 final room views reviewed as contact-sheet overviews; the listed 3 final runtime images were additionally opened at native resolution. The prior pass with 12 native runtime checks is archived separately and is not claimed as inspection of these new PNG bytes.',
                 'All 12 themes additionally reviewed at native 4K in saved-scene Editor captures.',
                 'These are grounded placement samples, not a complete visual traversal of every room or all parallax extremes.',
                 'Enemy damage was suppressed by the camera fixture; combat safety is not established by these captures.',
                 'Physical terrain textures, borders and existing gameplay effects are separate from this cave-background review.',
                 'No console hardware or 60 FPS performance claim.'],
    evidence=[dict(file=n,sha256=sha(here/n)) for n in
              ['Play/camera_result.json','complete_application.json','theme12_visual_qa.json',
               'native48_manifest.json','original13_hash_preservation.json']],
    rows=rows)
(here/'runtime89_visual_qa.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(dict(rooms=89,themes=12,nativeRuntimeSpotChecks=len(native),
                     ceilingFlagsReviewed=result['ceilingFlagsReviewed'],failures=0)))
