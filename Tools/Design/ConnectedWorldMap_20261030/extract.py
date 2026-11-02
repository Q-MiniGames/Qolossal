from pathlib import Path
import re,json,hashlib
root=Path(r'C:/UnityProjects/Qolossal')
out=root/'Tools/Design/ConnectedWorldMap_20261030'
guid={'area':'da2d6b9170c8bde48b375595cbb5e786','room':'bf5c47fd3e134a04bbd050148a4d7cba','cave':'edb41d2bf64709c45854807be3411adb'}
result={'areas':[],'rooms':[],'caves':[],'sceneHashes':{}}
def field(b,k):
 m=re.search(r'^  '+re.escape(k)+r': *(.*)$',b,re.M)
 return m.group(1).strip().strip('"') if m else ''
def ref(b,k):
 m=re.search(r'^  '+re.escape(k)+r': \{fileID: (-?\d+)',b,re.M);return m.group(1) if m else '0'
def vec(b,k):
 m=re.search(re.escape(k)+r': \{x: ([-\d.eE]+), y: ([-\d.eE]+), z: ([-\d.eE]+)',b)
 return [float(n) for n in m.groups()] if m else [0,0,0]
for path in sorted((root/'Assets/Scenes/Game').glob('*.unity')):
 raw=path.read_text(encoding='utf-8-sig');result['sceneHashes'][path.stem]=hashlib.sha256(path.read_bytes()).hexdigest()
 docs={m.group(2):(int(m.group(1)),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)',raw,re.M|re.S)}
 transforms={ref(b,'m_GameObject'):(key,b) for key,(typ,b) in docs.items() if typ==4}
 def position(component):
  b=docs[component][1];go=ref(b,'m_GameObject');t=transforms.get(go);p=[0.,0.,0.];seen=set()
  while t and t[0] not in seen:
   seen.add(t[0]);q=t[1];v=vec(q,'m_LocalPosition');sc=vec(q,'m_LocalScale');p=[p[i]*sc[i]+v[i] for i in range(3)];parent=ref(q,'m_Father');t=(parent,docs[parent][1]) if parent in docs else None
  return p[:2]
 for id,(typ,b) in docs.items():
  if typ!=114:continue
  kind=next((k for k,g in guid.items() if 'guid: '+g in b),None)
  if kind in ('area','room'):
   key='areaId' if kind=='area' else 'roomId';name='areaName' if kind=='area' else 'roomName';c=vec(b,'m_Center');e=vec(b,'m_Extent');p=position(id)
   result['areas' if kind=='area' else 'rooms'].append({'id':field(b,key),'name':field(b,name),'scene':path.stem,'bounds':[c[0]+p[0]-e[0],c[1]+p[1]-e[1],e[0]*2,e[1]*2]})
  elif kind=='cave':
   r={k:field(b,k) for k in ['connectionId','upperScene','upperRegion','upperAreaId','upperAreaName','lowerRoomId','lowerRoomName','requirements','activation']}
   r['undergroundSide']=field(b,'undergroundSide')=='1';r['scene']=path.stem
   r['entrancePosition']=position(ref(b,'entrance'));r['arrivalPosition']=position(ref(b,'arrival'));result['caves'].append(r)
assert len(result['areas'])==84 and len(result['rooms'])==89 and len(result['caves'])==74
(out/'current_scene_inventory.json').write_text(json.dumps(result,indent=2))
print({k:len(v) for k,v in result.items()})
print([c for c in result['caves'] if c['lowerRoomId']=='MR07_C06' and c['undergroundSide']])
