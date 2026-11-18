"""Read-only shader-aware audit of retained Cradle paintings; no image/asset mutations."""
from pathlib import Path
import re,json
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[4]
OUT=Path(__file__).resolve().parent
def mesh(path):
    s=path.read_text(); block=s.split('m_Channels:')[1].split('m_DataSize:')[0]
    ch=[tuple(map(int,m)) for m in re.findall(r'stream: (\d+)\s+offset: (\d+)\s+format: (\d+)\s+dimension: (\d+)',block)]
    assert all(c[0]==0 and c[2]==0 for c in ch if c[3]),ch
    stride=max(c[1]+c[3]*4 for c in ch);n=int(re.search(r'm_VertexCount: (\d+)',s)[1]);raw=bytes.fromhex(re.search(r'_typelessdata: ([0-9a-f]+)',s)[1])
    attrs={i:np.ndarray((n,c[3]),dtype='<f4',buffer=raw,offset=c[1],strides=(stride,4)).astype(float) for i,c in enumerate(ch) if c[3]}
    tri=np.frombuffer(bytes.fromhex(re.search(r'm_IndexBuffer: ([0-9a-f]+)',s)[1]),dtype='<u2').reshape(-1,3)
    return attrs,tri
def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)
def sampler(path):
    a=np.array(Image.open(path).convert('RGBA'),dtype=float)[::-1]/255.;h,w=a.shape[:2]
    def sample(uv):
        xy=np.clip(uv,0,1)*[w,h]-.5;x=np.clip(xy[...,0],0,w-1);y=np.clip(xy[...,1],0,h-1);x0=x.astype(int);y0=y.astype(int);x1=np.minimum(x0+1,w-1);y1=np.minimum(y0+1,h-1);dx=(x-x0)[...,None];dy=(y-y0)[...,None]
        return (a[y0,x0]*(1-dx)+a[y0,x1]*dx)*(1-dy)+(a[y1,x0]*(1-dx)+a[y1,x1]*dx)*dy
    return sample,(w,h)
def audit(id,role,source):
    name='Registered native outer rock' if role=='rim' else 'Continuous themed inner wall'
    attrs,tri=mesh(ROOT/f'Assets/Development/Layouts/WorldRevision_20261104/Surface/MR01/{id}/{name}.asset')
    sample,size=sampler(ROOT/source);v=attrs[0][:,:2][tri];channel=5 if role=='rim' else 4;uv=attrs[channel][:,:2][tri].copy()
    if role=='rim':uv=uv*[-1,.77]+[1,.23]
    texel=uv*np.array(size);du=texel[:,1]-texel[:,0];dv=texel[:,2]-texel[:,0];p=v[:,1]-v[:,0];q=v[:,2]-v[:,0];det=du[:,0]*dv[:,1]-dv[:,0]*du[:,1]
    assert np.min(abs(det))>1e-10
    jac=np.stack(((p*dv[:,1,None]-q*du[:,1,None])/det[:,None],(-p*dv[:,0,None]+q*du[:,0,None])/det[:,None]),axis=2)
    scale=np.linalg.svd(jac,compute_uv=False)[:,0];ppu=1/scale
    # 28 regular barycentric samples per triangle, including all edges/vertices.
    bary=np.array([[a/6,b/6,1-(a+b)/6] for a in range(7) for b in range(7-a)])
    interp=lambda channel:np.einsum('sb,tbc->tsc',bary,attrs[channel][tri])
    edge=interp(5)[...,:2];boundary=interp(6)[...,0];color=interp(3);alpha=color[...,3]
    if role=='rim':
        mapped=edge*[-1,.77]+[1,.23];rim=sample(mapped);reach=np.zeros_like(alpha)
        for k in range(1,11):
            o=k*.022;hole=1-np.minimum(sample(mapped+[-o,0])[...,3],sample(mapped-[0,o*.77])[...,3]);reach=np.maximum(reach,hole*(1-smooth(.08,.22,o)))
        paint=np.clip(reach*1.35,0,1)*rim[...,3]*(1-smooth(.80,.88,edge[...,1]))
        width=np.ptp(attrs[0][:,0]);alpha*=np.maximum(rim[...,3],edge[...,1]>=.74)*(1-smooth(.98,1,edge[...,1]))*smooth(0,np.clip(.45/width,.01,.15),edge[...,0]);contribution=paint*alpha
    else:
        mat=(ROOT/f'Assets/Development/Layouts/WorldRevision_20261104/Surface/MR01/{id}/Inner wall.mat').read_text();guid=re.search(r'- _EdgeMask:\s+m_Texture: \{fileID: \d+, guid: ([^,]+)',mat)[1]
        paths=[p for p in (ROOT/'Assets/Art/Prototypes/CradleTerrainJoin_20261027').glob('*.png.meta') if f'guid: {guid}' in p.read_text()]
        assert len(paths)==1;edgeSample,_=sampler(Path(str(paths[0])[:-5]));contribution=alpha*np.where(edge[...,0]<1,edgeSample(edge)[...,3],1)*sample(interp(4)[...,:2])[...,3]
        # Account for the front painted rim occluding this inner painting.
        ra,_=mesh(ROOT/f'Assets/Development/Layouts/WorldRevision_20261104/Surface/MR01/{id}/Registered native outer rock.asset')
        positions=interp(0)[...,:2];xs=np.unique(ra[0][:,0]);floor=np.array([ra[0][ra[0][:,0]==x,1].min() for x in xs]);ceil=np.array([ra[0][ra[0][:,0]==x,1].max() for x in xs]);floorAt=np.interp(positions[...,0],xs,floor);heightAt=np.interp(positions[...,0],xs,ceil-floor)
        rimEdge=np.stack(((xs[-1]-positions[...,0])/(xs[-1]-xs[0]),(positions[...,1]-floorAt)/heightAt),axis=-1)
        rimSample,_=sampler(ROOT/'Assets/Art/Prototypes/CradleRockwork_20261027/MR01_C01_Rockwork.png');rimAlpha=rimSample(rimEdge*[-1,.77]+[1,.23])[...,3]
        front=np.maximum(rimAlpha,rimEdge[...,1]>=.74)*(1-smooth(.98,1,rimEdge[...,1]))*smooth(0,np.clip(.45/(xs[-1]-xs[0]),.01,.15),rimEdge[...,0])
        inside=((rimEdge>=0)&(rimEdge<=1)).all(axis=-1);contribution*=1-np.where(inside,front,0)
    contribution=np.where(boundary>=0,contribution,0);peak=contribution.max(axis=1)
    result={'id':id,'role':role,'source':source,'sourceSize':size,'allTriangleMinimumPPU':float(ppu.min()),'sampleCountPerTriangle':len(bary),'visibleThresholds':[]}
    for threshold in [.01,.05,.25,.5]:
        mask=peak>=threshold;idx=np.flatnonzero(mask)[np.argmin(ppu[mask])]
        result['visibleThresholds'].append({'minimumContribution':threshold,'triangles':int(mask.sum()),'minimumPPU':float(ppu[idx]),'worstTriangle':int(idx),'worldVertices':v[idx].tolist(),'edgeVertices':attrs[5][tri[idx],:2].tolist(),'maxContribution':float(peak[idx]),'requiredSourceSize':np.ceil(np.array(size)*216/ppu[idx]).astype(int).tolist()})
    return result
if __name__=='__main__':
    rows=json.loads((OUT/'MR01_approved_caves.json').read_text())['densityAudits'];results=[]
    for r in rows:
        if not r['layer']['nativePass']:results.append(audit(r['id'],'rim' if 'rim' in r['layer']['role'] else 'interior',r['layer']['asset']))
    (OUT/'cradle_visible_density.json').write_text(json.dumps(results,indent=2));print(json.dumps(results,indent=2))

