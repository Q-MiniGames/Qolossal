from pathlib import Path
import hashlib,json,struct
here=Path(__file__).resolve().parent
manifest=json.loads((here/'AllThemes/capture_manifest.json').read_text(encoding='utf-8-sig'))
notes={
 'A01':'Cistern vault/side channels read as one cavern; crisp foreground block and water; no mirror seams.',
 'C01':'Root shelf depths blend into central tree; foreground wall, spikes and ring remain distinct.',
 'D01':'Cool wind-carved vault has continuous painted depth; safe floor and checkpoint read clearly.',
 'E05':'Rose sanctuary arches span the full view; gust lanes and golden platforms remain distinct.',
 'HEART':'Fossil/red vault fills the view; no mirrored diamonds; Heart root and ladder remain readable.',
 'MR01_C06':'Olive root cavern fills view; original stream and adjoining depths blend without window framing.',
 'MR02_C04':'Stone gallery flows into side reaches; physical pale terrain is distinct from far cave.',
 'MR03_C06':'Warm mill galleries read across viewport; rings, spikes and terrain silhouette remain distinct.',
 'MR05_C01':'Emerald root vault has broad atmospheric depth; small platforms remain foreground objects.',
 'MR06_C01':'Lavender wind chamber remains coherent; gust strokes are existing gameplay visualization.',
 'MR07_C01':'Pale mineral cavern remains distant behind warm foreground platforms and spikes.',
 'PC06':'Complete P-theme depth is visible within the physical room aperture; surrounding pale rock is terrain, not a repeated scenic-panel frame.'}
rows=[]
for r in manifest:
    path=Path(r['file']);dims=struct.unpack('>II',path.read_bytes()[16:24]);assert dims==(3840,2160)
    rows.append(dict(room=r['room'],theme=r['theme'],file=str(path),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),width=dims[0],height=dims[1],nativeImageViewed=True,caveCompositionPass=True,notes=notes[r['room']]))
assert len(rows)==12
result=dict(review='Independent native3840x2160 saved-scene Editor capture inspection, cave_network',count=12,themeCount=12,caveCompositionFailures=[],criteria=['No black horizontal/vertical scenic grid','No mirrored waterfalls or diamond seams','No tiny central painting against flat surround','Continuous painted side/vault depth','Foreground terrain and hazards remain distinct'],limitations=['Static Editor images only; full89-room grounded runtime framing review follows separately','Physical terrain borders and existing gameplay wind effects are outside this cave-background change','No hardware performance or console readiness claim'],rows=rows)
(here/'theme12_visual_qa.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(dict(reviewed=12,caveCompositionFailures=0)))
