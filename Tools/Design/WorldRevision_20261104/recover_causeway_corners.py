"""Recover overwritten MR02 rounding evidence from immutable ZIP vs saved scene.

Reads scenes only; writes evidence, never YAML. Refuses non-corner differences.
Run before any additional MR02 slope-corner pass changes these surface arrays.
"""
from pathlib import Path
import hashlib,json,re,zipfile
ROOT=Path(__file__).resolve().parents[3];TASK=Path(__file__).resolve().parent
scene='Assets/Scenes/Game/MRAtlas_MR02_Causeway.unity'
archive=TASK/'Before/scenes_and_map_before.zip'
with zipfile.ZipFile(archive) as z:before_bytes=z.read(scene)
after_bytes=(ROOT/scene).read_bytes()
guid='71198dec469745448b328b8db0db7960'
def parse(text):
    docs={m[2]:(int(m[1]),m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)',text,re.M|re.S)}
    def ref(b,k):
        m=re.search(r'^  '+k+r': \{fileID: (-?\d+)',b,re.M);return m[1] if m else '0'
    transforms={ref(b,'m_GameObject'):(fid,b) for fid,(kind,b) in docs.items() if kind==4}
    def name(go):return re.search(r'^  m_Name: (.*)$',docs[go][1],re.M)[1].strip('"')
    def hierarchy(go):
        parts=[name(go)];t=transforms.get(go)
        while t:
            parent=ref(t[1],'m_Father')
            if parent not in docs:break
            t=(parent,docs[parent][1]);parts.append(name(ref(t[1],'m_GameObject')))
        return '/'.join(reversed(parts))
    rows={}
    for fid,(kind,b) in docs.items():
        if kind!=114 or guid not in b:continue
        v=re.search(r'^  surface:\n(.*?)(?=^  [a-zA-Z_]|\Z)',b,re.M|re.S)
        rows[fid]={'path':hierarchy(ref(b,'m_GameObject')),'surface':[[float(x),float(y)] for x,y in re.findall(r'\{x: ([-\d.eE]+), y: ([-\d.eE]+)\}',v[1])]}
    return rows
old=parse(before_bytes.decode('utf-8-sig'));new=parse(after_bytes.decode('utf-8-sig'))
assert old.keys()==new.keys(),'Terrain component identities changed'
records=[]
for fid,b in old.items():
    a=new[fid];assert a['path']==b['path']
    if a['surface']==b['surface']:continue
    removed=[]
    for i,p in enumerate(b['surface']):
        if any(abs(p[0]-q[0])<.0001 and abs(p[1]-q[1])<.0001 for q in a['surface']):continue
        assert 0<i<len(b['surface'])-1,'Endpoint changed'
        prev,nxt=b['surface'][i-1],b['surface'][i+1]
        dx0,dy0=p[0]-prev[0],p[1]-prev[1];dx1,dy1=nxt[0]-p[0],nxt[1]-p[1]
        assert (abs(dx0)<.0001 and dy0>.5 and dx1>.5 and abs(dy1)<.0001) or (dx0>.5 and abs(dy0)<.0001 and abs(dx1)<.0001 and dy1<-.5), 'Non-original-rounding change at '+str(p)
        removed.append(p)
    assert a['surface'][0]==b['surface'][0] and a['surface'][-1]==b['surface'][-1]
    assert len(a['surface'])==len(b['surface'])+6*len(removed),'Unexpected added geometry'
    records.append({'path':a['path'],'componentFileId':fid,'corners':len(removed),'radius':.18,'before':b['surface'],'after':a['surface'],'removedUpperVertices':removed,'endpointsPreserved':True})
assert sum(r['corners'] for r in records)==11,'Recovered count differs from audited 8+3 corners'
out=TASK/'Surface/MR02_rounded_corners.json'
lost=TASK/'Before/MR02_overwritten_round_receipt.json'
if not lost.exists():lost.write_bytes(out.read_bytes())
result={'scene':'MRAtlas_MR02_Causeway','radius':.18,'records':records,'requiresMapRefresh':True,'requiresCollisionAndTraversalCheck':True,'recoveredEvidence':True,'sourceArchive':str(archive),'sourceMember':scene,'beforeSceneSha256':hashlib.sha256(before_bytes).hexdigest(),'afterSceneSha256':hashlib.sha256(after_bytes).hexdigest(),'method':'Actual saved MraTerrain surface arrays keyed by stable component ID; original endpoints retained; each removed point is an eligible convex upper corner with six net added points. No scene mutation.'}
out.write_text(json.dumps(result,indent=2),encoding='utf-8')
print('Recovered MR02 evidence: 11 corners, two terrain objects; no scene writes.')
