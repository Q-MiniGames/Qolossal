from pathlib import Path
p=Path('Tools/Design/MainWorldLayout/build_layout.py');s=p.read_text(encoding='utf-8')
start=s.index(' base=max(p[1]');end=s.index('for e in old[\'edges\']:',start)
s=s[:start]+''' regionInfo.append(dict(id=r['id'],name=r['name'],color=r['color'],left=left,right=right,labelY=min(p[1] for c in contours if c['region']==r['id'] for p in c['points'])-100,netRise=r['exitY']-r['entryY']))
# One affine transform for the entire underground scene. No network packing or invented corridors.
ug=read(REVIEW/'ActualUndergroundComparison_20261030/scene_geometry.json')
assert ug['sha256']==inv['sceneHashes']['Qolossal_Underground']
ground=[c for c in ug['colliders'] if c['layer']=='Ground' and not c['dynamic']]
upoints=[p for c in ground for ps in c['paths'] for p in ps]
ugTop=max(p[1] for p in contours for p in p['points'])+230
um=dict(sx=SCALE,sy=SCALE,ox=LEFT-min(p[0] for p in upoints)*SCALE,oy=ugTop+max(p[1] for p in upoints)*SCALE,yFlip=True)
transforms['Qolossal_Underground']=um
for n in old['nodes']:
 if n['type']!='underground':continue
 q=copy.deepcopy(n);q['mapBounds']=bounds(um,q['worldBounds']);x,y,w,h=q['mapBounds'];q['position']=[x+w/2,y+h/2];q['point']=point(q['position']);nodes.append(q)
byid={n['id']:n for n in nodes}
for g in sorted({n['group'] for n in nodes if n['type']=='underground'}):
 ns=[n for n in nodes if n.get('group')==g];bs=[n['mapBounds'] for n in ns];xmin=min(b[0] for b in bs);ymin=min(b[1] for b in bs);xmax=max(b[0]+b[2] for b in bs);ymax=max(b[1]+b[3] for b in bs)
 groups[g]=dict(id=g,region=ns[0]['region'],bounds=[xmin,ymin,xmax-xmin,ymax-ymin])
undergroundContours=[]
for c in ground:
 for ps in c['paths']:
  pts=ps+([ps[0]] if c['closed'] else [])
  region=byid[c['room']]['region'] if c['room'] else 'shared'
  if not c['room']:
   center=[sum(p[0] for p in ps)/len(ps),sum(p[1] for p in ps)/len(ps)]
   near=min((n for n in nodes if n['type']=='underground'),key=lambda n:(n['worldBounds'][0]+n['worldBounds'][2]/2-center[0])**2+(n['worldBounds'][1]+n['worldBounds'][3]/2-center[1])**2)
   region=near['region']
  undergroundContours.append(dict(region=region,source=c['path'],room=c['room'],closed=c['closed'],points=[convert(um,p) for p in pts]))
W=max(W,max(convert(um,p)[0] for p in upoints)+180)
H=max(convert(um,p)[1] for p in upoints)+320
def roompoint(id,p):return convert(um,p)

'''+s[end:]
start=s.index("  if byid[a]['group']==byid[b]['group']:");end=s.index("  q['points']=pts;edges.append(q)",start)
s=s[:start]+"  pts=[roompoint(a,p) for p in wp]\n"+s[end:]
s=s.replace("undergroundAssembly='Preserve scene room bounds and passage centerlines within each network using uniform scale; translate groups below their access region. Inter-group passages rerouted only on the diagram.'","undergroundAssembly='One equal-X/Y affine transform for the complete actual underground scene, including every physical corridor. A common vertical display offset places it below the assembled upper scenes; cave links are transitions, not measured shafts.'")
s=s.replace("currentStage='Canonical connected geometry for artwork; current Unity painted atlas awaits migration to this approved layout.'","currentStage='Canonical scene-derived layout exported to Unity; prior unregistered terrain painting hidden until replacement artwork follows this geometry.'")
s=s.replace('surfaceContours=contours,groups=groups','surfaceContours=contours,undergroundContours=undergroundContours,groups=groups')
s=s.replace(" rooms.append(f'<g class=\"node room\" data-id=\"{id}\"><title>{esc(id+\" / \"+n[\"name\"])}</title><path d=\"{d}\" fill=\"#152b35\" stroke=\"{color}\" stroke-width=\"3.5\"/></g>')"," rooms.append(f'<g class=\"node room\" data-id=\"{id}\"><title>{esc(id+\" / \"+n[\"name\"])}</title><rect x=\"{x}\" y=\"{y}\" width=\"{w}\" height=\"{h}\" fill=\"transparent\"/></g>')")
s=s.replace("for g,group in groups.items():", "# Exact collider outlines and platforms replace the former schematic room boxes.\nfor c in undergroundContours:\n rooms.append(f'<path d=\"{line(c[\"points\"])}\" fill=\"none\" stroke=\"{colors[c[\"region\"]]}\" stroke-width=\"2.4\" stroke-linejoin=\"round\"/>')\nfor g,group in []:")
s=s.replace("runtimeIntegrated=False,runtimeStatus='Existing painted Unity atlas awaits this layout/artwork migration; this step creates the canonical review and art foundation.'","runtimeIntegrated=True,runtimeStatus='Exact scene-derived contours exported to Unity; live UI verification recorded in UndergroundRelocation_20261030.'")
s=s.replace('changedScenes=False','changedScenes=[\'Qolossal_Underground\']')
p.write_text(s,encoding='utf-8')
