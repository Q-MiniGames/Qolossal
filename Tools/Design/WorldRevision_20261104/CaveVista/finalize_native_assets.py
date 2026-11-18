"""Hash/dimension receipts only; never resizes or changes generated pixels."""
from pathlib import Path
import hashlib, json, shutil, struct

ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
STAGED=HERE/'Staged'
ART=ROOT/'Assets/Art/Prototypes/WorldRevision_20261104/CaveVista'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
records=read(HERE/'depth_generation.json')
records += [read(p) for p in sorted(STAGED.glob('*.generation.json'))]
for theme in ['MR01','H']:
    r=read(HERE/f'{theme}_generation.json')
    r['part']='Extension'
    shutil.copy2(ART/f'{theme}_Extension.png', STAGED/f'{theme}_Extension.png')
    records.append(r)
rows=[]
for r in sorted(records,key=lambda r:(r['set'],r['part'])):
    path=STAGED/f"{r['set']}_{r['part']}.png"
    source=Path(r['source'])
    width,height=struct.unpack('>II',path.read_bytes()[16:24])
    assert sha(path)==sha(source), path
    expected=(1024,1536) if r['part'] in ['Left','Right'] else (1774,887) if r['part']=='Vault' else (1254,1254)
    assert (width,height)==expected,(path,width,height)
    rows.append(dict(theme=r['set'],part=r['part'],staged=str(path),asset=str(ART/path.name),source=str(source),sha256=sha(path),width=width,height=height,nativePPU=216,worldWidth=width/216,worldHeight=height/216,resized=False,sourceBytesPreserved=True,prompt=r['prompt']))
assert len(rows)==48 and len({r['sha256'] for r in rows})==48
assert len({r['theme'] for r in rows})==12
receipt=dict(assetCount=48,themes=12,allUnique=True,allNativeUnmodified=True,closestCameraHalfHeight=5,targetHeightPixels=2160,requiredPPU=216,rows=rows)
(HERE/'native48_manifest.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
original=read(HERE/'original13_hash_preservation.json')
for r in original['rows']:
    r['afterSha256']=sha(ROOT/r['path'])
    assert r['beforeSha256']==r['afterSha256'],r['path']
original['allUnmodified']=True
(HERE/'original13_hash_preservation.json').write_text(json.dumps(original,indent=2),encoding='utf-8')
print(json.dumps(dict(nativeAssets=48,themes=12,uniqueHashes=48,unchangedOriginals=13)))
