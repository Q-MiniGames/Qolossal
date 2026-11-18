"""Read-only native mesh/shader review of the worst regional-edge placements.

28 barycentric probes per triangle diagnose visible failures; this is not an
occlusion-complete proof that unsampled artwork is invisible.
"""
from pathlib import Path
import re, json
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent

def scene_objects():
    text=(ROOT/'Assets/Scenes/Game/Qolossal_Underground.unity').read_text()
    docs={}
    for d in re.split(r'(?m)^--- ',text):
        m=re.match(r'!u!(\d+) &(\d+)',d)
        if m: docs[int(m[2])]=(int(m[1]),d)
    names={i:re.search(r'  m_Name: (.*)',d)[1].strip("'\"") for i,(t,d) in docs.items() if t==1 and '  m_Name:' in d}
    trans={}
    for i,(t,d) in docs.items():
        if t!=4 or 'm_Father:' not in d: continue
        go=int(re.search(r'm_GameObject: \{fileID: (\d+)',d)[1]);parent=int(re.search(r'm_Father: \{fileID: (\d+)',d)[1])
        def vec(key,axis):
            st=re.search(key+r': \{([^}]+)',d)[1]
            return [float(re.search(a+r': ([^,}]+)',st)[1]) for a in axis]
        x,y,z,w=vec('m_LocalRotation','xyzw');sc=vec('m_LocalScale','xyz');pos=vec('m_LocalPosition','xyz')
        rot=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
        mat=np.eye(4);mat[:3,:3]=rot@np.diag(sc);mat[:3,3]=pos;trans[i]=(go,parent,mat)
    def hierarchy(t):
        if t not in trans: return 'stripped'
        go,pa,m=trans[t]
        return hierarchy(pa)+'/'+names.get(go,'stripped') if pa else names.get(go,'stripped')
    def world(t):
        go,pa,m=trans[t]
        return world(pa)@m if pa else m
    return docs,trans,{hierarchy(t):t for t in trans},world

def inputs():
    docs,trans,lookup,world=scene_objects()
    rows=json.loads((OUT/'density_instances.json').read_text())['rows'];worst={}
    for a in rows:
        if a['kind']=='regional-painted-edge-uv2' and (a['asset'] not in worst or a['nativeMinimumPixelsPerUnit']<worst[a['asset']]['nativeMinimumPixelsPerUnit']): worst[a['asset']]=a
    metas={}
    for p in (ROOT/'Assets/Development/Layouts').rglob('*.asset.meta'):
        m=re.search(r'guid: (\w+)',p.read_text())
        if m: metas[m[1]]=str(p.relative_to(ROOT))[:-5]
    out=[]
    for row in worst.values():
        t=lookup[row['hierarchy']];go=trans[t][0]
        fd=next(d for typ,d in docs.values() if typ==33 and f'm_GameObject: {{fileID: {go}}}' in d)
        guid=re.search(r'm_Mesh: \{fileID: \d+, guid: ([^,]+)',fd)[1]
        out.append(dict(row=row,mesh=metas[guid],worldMatrix=world(t).tolist()))
    (OUT/'regional_edge_review_inputs.json').write_text(json.dumps(out,indent=2))
    return out

def mesh(path):
    s=path.read_text();block=s.split('m_Channels:')[1].split('m_DataSize:')[0]
    ch=[tuple(map(int,m)) for m in re.findall(r'stream: (\d+)\s+offset: (\d+)\s+format: (\d+)\s+dimension: (\d+)',block)]
    assert all(c[0]==0 and c[2]==0 for c in ch if c[3])
    stride=max(c[1]+c[3]*4 for c in ch);n=int(re.search(r'm_VertexCount: (\d+)',s)[1]);raw=bytes.fromhex(re.search(r'_typelessdata: ([0-9a-f]+)',s)[1])
    attrs={i:np.ndarray((n,c[3]),dtype='<f4',buffer=raw,offset=c[1],strides=(stride,4)).astype(float) for i,c in enumerate(ch) if c[3]}
    fmt=int(re.search(r'm_IndexFormat: (\d+)',s)[1]);tri=np.frombuffer(bytes.fromhex(re.search(r'm_IndexBuffer: ([0-9a-f]+)',s)[1]),dtype='<u4' if fmt else '<u2').reshape(-1,3)
    return attrs,tri

def sampler(path):
    a=np.array(Image.open(path).convert('RGBA'),dtype=float)[::-1]/255.;h,w=a.shape[:2]
    meta=Path(str(path)+'.meta').read_text()
    wrap=[int(re.search(r'wrap'+axis+r': (\d+)',meta)[1]) for axis in ['U','V']]
    def sample(uv):
        xy=uv.copy()
        for k in range(2): xy[...,k]=np.mod(xy[...,k],1) if wrap[k]==0 else np.clip(xy[...,k],0,1)
        xy=xy*[w,h]-.5;x0=np.floor(xy[...,0]).astype(int);y0=np.floor(xy[...,1]).astype(int);dx=xy[...,0]-x0;dy=xy[...,1]-y0
        x1=x0+1;y1=y0+1
        if wrap[0]==0:x0%=w;x1%=w
        else:x0=np.clip(x0,0,w-1);x1=np.clip(x1,0,w-1)
        if wrap[1]==0:y0%=h;y1%=h
        else:y0=np.clip(y0,0,h-1);y1=np.clip(y1,0,h-1)
        return (a[y0,x0,3]*(1-dx)+a[y0,x1,3]*dx)*(1-dy)+(a[y1,x0,3]*(1-dx)+a[y1,x1,3]*dx)*dy
    return sample,(w,h)

def audit(item):
    attrs,tri=mesh(ROOT/item['mesh']);channel=int(item['row']['textureProperty'][-1]);repeat=[8,4,8,4,8,8,8,8][channel]
    _,size=sampler(ROOT/item['row']['asset'])
    local=attrs[0][tri];linear=np.array(item['worldMatrix'])[:2,:3]
    p=(local[:,1]-local[:,0])@linear.T;q=(local[:,2]-local[:,0])@linear.T
    uv=attrs[6][tri,:2].copy();uv[:,:,0]/=repeat;texel=uv*np.array(size)
    du=texel[:,1]-texel[:,0];dv=texel[:,2]-texel[:,0];det=du[:,0]*dv[:,1]-dv[:,0]*du[:,1]
    valid=(abs(det)>1e-10)&(abs(p[:,0]*q[:,1]-p[:,1]*q[:,0])>1e-10)
    ids=np.flatnonzero(valid);tri=tri[valid];p=p[valid];q=q[valid];du=du[valid];dv=dv[valid];det=det[valid]
    jac=np.stack(((p*dv[:,1,None]-q*du[:,1,None])/det[:,None],(-p*dv[:,0,None]+q*du[:,0,None])/det[:,None]),axis=2)
    ppu=1/np.linalg.svd(jac,compute_uv=False)[:,0]
    bary=np.array([[a/6,b/6,1-(a+b)/6] for a in range(7) for b in range(7-a)])
    families=['Cradle','Causeway','Mill','Cistern','Roots','Wind','Descent','Heart']
    samples=[sampler(ROOT/f'Assets/Development/Layouts/UndergroundRegional_20261023/Art/{f}_EdgeNative.png')[0] for f in families]
    peaks=np.zeros(len(tri))
    for start in range(0,len(tri),2000):
        tt=tri[start:start+2000];interp=lambda c:np.einsum('sb,tbc->tsc',bary,attrs[c][tt])
        edge=interp(6);weights=np.maximum(np.concatenate((interp(4),interp(5)),axis=2),0);weights/=np.maximum(weights.sum(axis=2,keepdims=True),.00001)
        compositeAlpha=np.zeros(edge.shape[:2])
        for k,sample in enumerate(samples):
            u=edge[:,:,:2].copy();u[:,:,0]/=[8,4,8,4,8,8,8,8][k]
            compositeAlpha+=weights[:,:,k]*sample(u)
        # Fragment RGB is lerp(edgeRGB, capRGB, edge.z), then multiplied by vertex
        # colour and standard alpha blending. The shader does not saturate z.
        contribution=abs(1-edge[:,:,2])*weights[:,:,channel]*interp(3)[:,:,3]*compositeAlpha
        peaks[start:start+len(tt)]=contribution.max(axis=1)
    thresholds=[]
    for threshold in [.001,.01,.05,.25,.5]:
        mask=peaks>=threshold
        if not mask.any():thresholds.append(dict(minimumContribution=threshold,triangles=0));continue
        idx=np.flatnonzero(mask)[np.argmin(ppu[mask])]
        thresholds.append(dict(minimumContribution=threshold,triangles=int(mask.sum()),minimumPPU=float(ppu[idx]),maxContribution=float(peaks[idx]),worstTriangle=int(ids[idx]),edgeVertices=attrs[6][tri[idx]].tolist(),worldVertices=(attrs[0][tri[idx]]@linear.T+np.array(item['worldMatrix'])[:2,3]).tolist(),requiredSourceSize=np.ceil(np.array(size)*216/ppu[idx]-.001).astype(int).tolist()))
    return dict(asset=item['row']['asset'],mesh=item['mesh'],hierarchy=item['row']['hierarchy'],originalReportedPPU=item['row']['nativeMinimumPixelsPerUnit'],allTriangleDoublePPU=float(ppu.min()),sampleCountPerTriangle=len(bary),visibleThresholds=thresholds)

if __name__=='__main__':
    data=inputs();results=[]
    for item in data:
        result=audit(item);results.append(result)
        print(result['asset'].split('/')[-1],result['visibleThresholds'],flush=True)
    (OUT/'regional_edge_visible_density.json').write_text(json.dumps(dict(method=__doc__,results=results),indent=2))
