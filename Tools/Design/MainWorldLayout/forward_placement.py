"""Cartographic placement only: rigid connected caves, scene geometry unchanged."""
import copy

def place(nodes, edges, inventory, transforms, base, convert, bounds, surface_segments):
    underground = {n['id']: n for n in nodes if n['type'] == 'underground'}
    parent = {k:k for k in underground}
    def root(k):
        while parent[k] != k:
            parent[k] = parent[parent[k]]; k = parent[k]
        return k
    for e in edges:
        if e['kind'] == 'passage':
            parent[root(e['to'])] = root(e['from'])
    components = {}
    for k in underground: components.setdefault(root(k), []).append(k)
    components = {min(ids): sorted(ids) for ids in components.values()}
    component_of = {k:cid for cid,ids in components.items() for k in ids}
    upper = [c for c in inventory['caves'] if not c['undergroundSide']]
    lower = {c['connectionId']:c for c in inventory['caves'] if c['undergroundSide']}
    moved = []; occupied = []
    def intersects(a,b,pad=24):
        return a[0] < b[0]+b[2]+pad and a[0]+a[2]+pad > b[0] and a[1] < b[1]+b[3]+pad and a[1]+a[3]+pad > b[1]
    def box(ids):
        bs=[underground[k]['mapBounds'] for k in ids]
        x=min(b[0] for b in bs); y=min(b[1] for b in bs)
        return [x,y,max(b[0]+b[2] for b in bs)-x,max(b[1]+b[3] for b in bs)-y]
    def surface_y(x):
        hits=[a[1]+(b[1]-a[1])*(x-a[0])/(b[0]-a[0]) for a,b in surface_segments if min(a[0],b[0])<=x<=max(a[0],b[0]) and abs(b[0]-a[0])>1e-7]
        return max(hits) if hits else -1e6
    # The continuous deep galleries keep their existing relative registration.
    for cid,ids in components.items():
        if len(ids)>6:
            occupied.extend(underground[k]['mapBounds'] for k in ids)
    for cid,ids in sorted(components.items(),key=lambda item:(-len(item[1]), item[0])):
        mapping=copy.deepcopy(base); offset=[0.,0.]
        access=next((c for c in upper if c['lowerRoomId'] in ids),None)
        if access and len(ids)<=6:
            up=convert(transforms[access['upperScene']],access['entrancePosition'])
            old=box(ids); old_entry=convert(base,lower[access['connectionId']]['entrancePosition'])
            # Whole early networks move beyond the mouth; small caves sit in a
            # shallow forward band with spacing chosen to keep outlines distinct.
            candidates=([(up[0]+90,old[1]+dy) for dy in range(0,401,40)] if len(ids)>1 else
                        [(up[0]+dx,up[1]+depth-old[3]) for depth in range(150,391,30) for dx in range(80,381,30)])
            for x,y in candidates:
                target=[x,y,old[2],old[3]]
                if x+old[2]>max(max(a[0],b[0]) for a,b in surface_segments)-25:continue
                if any(intersects(target,b) for b in occupied):continue
                if any(y<surface_y(x+old[2]*t)+70 for t in [0,.25,.5,.75,1]):continue
                offset=[x-old[0],y-old[1]]; occupied.append(target);break
            else:raise AssertionError('No clear forward placement for '+cid)
            mapping['ox']+=offset[0]; mapping['oy']+=offset[1]
            moved.append(dict(component=cid,rooms=ids,offset=offset,upper=up,oldEntrance=old_entry))
        mid='underground:'+cid
        transforms[mid]=mapping
        for k in ids:
            n=underground[k];n['mappingId']=mid;n['mapBounds']=bounds(mapping,n['worldBounds'])
            x,y,w,h=n['mapBounds'];n['position']=[x+w/2,y+h/2];n['point']=dict(x=x+w/2,y=y+h/2)
    for e in edges:
        if e['kind']=='passage':assert component_of[e['from']]==component_of[e['to']]
    return moved
