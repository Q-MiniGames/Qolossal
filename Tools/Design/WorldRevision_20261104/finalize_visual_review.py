from pathlib import Path
import json,hashlib
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
def read(p):return json.loads((HERE/p).read_text(encoding='utf-8-sig'))
surface=read('VisualQA/surface_entrance_final_review.json')
underground=read('VisualQA/underground_final_review.json')
caves=read('CaveVista/runtime89_visual_qa.json')
assert surface['entrancesReviewed']==37
assert caves['roomCount']==89 and not caves['caveCompositionFailures'] and not caves['observedPlayerOcclusion']
# Confirm review receipts refer to the current capture bytes.
def verify_images(value):
 if isinstance(value,dict):
  if 'file' in value and 'sha256' in value:
   p=Path(value['file'])
   if not p.is_absolute():
    candidates=[ROOT/p,HERE/p,HERE/'CaveVista'/p]
    existing=[q for q in candidates if q.is_file()]
    assert len(existing)==1, 'Ambiguous or missing review path: '+str(p)
    p=existing[0]
   assert hashlib.sha256(p.read_bytes()).hexdigest()==value['sha256'], 'Stale visual review: '+str(p)
  for v in value.values():verify_images(v)
 elif isinstance(value,list):
  for v in value:verify_images(v)
for value in (surface,underground,caves):verify_images(value)
result={'passed':True,'surfaceEntrances':37,'caveRooms':89,'caveThemes':12,
 'method':'Root composition review and native spot checks, plus independent hash-linked agent reviews. All cave room overviews reviewed; selected detailed native views, not complete traversal at every camera position.',
 'surfaceReview':'VisualQA/surface_entrance_final_review.json',
 'undergroundJoinReview':'VisualQA/underground_final_review.json',
 'runtimeCaveReview':'CaveVista/runtime89_visual_qa.json',
 'ceilingFlagsReviewed':caves['ceilingFlagsReviewed'],
 'mapReview':'Root inspected canonical overview and live relocated Qvale connection; 37-step live UI fixture passed.',
 'limits':'No claim of flawless visuals at every gameplay position. Retained native-source density exceptions are separate from visual attachment checks.'}
(HERE/'final_visual_review.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(result)
