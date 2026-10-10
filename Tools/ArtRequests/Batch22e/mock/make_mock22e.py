# Feasibility mock for the Batch 22e checker (not art): 22d rows 0-179 and wrap leaves kept; bark below row 179
# rebuilt from real arm bark (mirrored down from row 178) cross-fading into the 22c leg's bark for flare and cover.
import numpy as np, sys
from PIL import Image
IN='C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Art/Codex_v2_full/Batch22e_Input/'
A=np.array(Image.open(IN+'reference_22d/Qori_v3_Arm.png').convert('RGBA')).astype(float)
G=np.array(Image.open('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Art/Codex_v2_full/Batch22d_Input/reference_22c/Qori_v3_Leg.png').convert('RGBA')).astype(float)
outer=np.array(Image.open(IN+'Qori_v3_Arm_Mask_Outer.png'))>127
inner=np.array(Image.open(IN+'Qori_v3_Arm_Mask_Inner.png'))>127
cover=np.array(Image.open(IN+'Qori_v3_Arm_Mask_HandCover.png'))>127
H,W=A.shape[:2]; out=A.copy()
L=lambda a:0.299*a[...,0]+0.587*a[...,1]+0.114*a[...,2]
ol=(A[...,3]>=128)&(A[...,1]>=A[...,0]*0.86)&(A[...,1]>=A[...,2]*1.35)
y,x=231,58; out[y,x]=np.mean([A[y+dy,x+dx] for dy in(-1,0,1) for dx in(-1,0,1) if (dy or dx) and not ol[y+dy,x+dx]],0)
tl,tr=56,96
def armsrc(y,x):
    k=(x-tl)%(2*(tr-tl)); sx=tl+(k if k<=tr-tl else 2*(tr-tl)-k); return A[178-(y-180),int(sx),:3]
def legsrc(y,x,l,r):
    ly=200+(y-180); xs=np.nonzero(G[ly,:,3]>=250)[0]; ll,lr=xs.min()+2,xs.max()-2
    return G[ly,int(round(ll+(x-l)/max(1,r-l)*(lr-ll))),:3]
for y in range(180,259):
    if y<236: shape=(A[y,:,3]>=128)&~ol[y]; alpha=None
    elif y<=242:
        shape=(A[y,:,3]>=128)&outer[y]; lim=118-(y-238)*2.25 if y>=239 else 999
        shape&=np.arange(W)<=lim; shape|=inner[y]; out[y]=0; alpha=255.0
    else:
        out[y]=0; shape=cover[y]; alpha=255.0 if y<=245 else 255.0*(256-y)/11
    xs=np.nonzero(shape)[0]
    if not len(xs): continue
    l,r=xs.min(),xs.max(); w=0.0; f=(1.0-0.05*min(1,(y-180)/20))*(1.0+0.08*min(1,max(0,(y-236)/4)))
    for x in xs:
        out[y,x,:3]=f*armsrc(y,x)
        if alpha is not None: out[y,x,3]=alpha if (x>l and x<r) or y>245 else min(alpha,200)
def smooth(v,k=7): return np.convolve(np.pad(v,k,mode='edge'),np.ones(2*k+1)/(2*k+1),'valid')
ok235=out[235,:,3]>=250; ref=L(out[235])
for y in range(236,257):
    m=out[y,:,3]>0
    if m.sum()<3: continue
    tgt=np.where(ok235,ref,np.nan); cur=np.where(m,L(out[y]),np.nan)
    for v in (tgt,cur):
        idx=np.arange(W); g=~np.isnan(v); v[~g]=np.interp(idx[~g],idx[g],v[g])
    wgt=max(0.0,1-(y-236)/6); out[y,:,:3]*=(1-wgt+wgt*smooth(tgt)/np.maximum(smooth(cur),1))[:,None]
out[...,:3][out[...,3]==0]=0
Image.fromarray(np.clip(out,0,255).round().astype(np.uint8),'RGBA').save(sys.argv[1])
