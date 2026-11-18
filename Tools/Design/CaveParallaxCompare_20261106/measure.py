from pathlib import Path
import cv2,json,numpy as np
p=Path(__file__).parent
def read(name):return cv2.imread(str(p/name),cv2.IMREAD_GRAYSCALE).astype(np.float32)
rows=[]
for mode in ('A','B'):
 a=read(f'{mode}_0_0.png')
 for dx,dy in ((2,0),(0,2)):
  b=read(f'{mode}_{dx}_{dy}.png')
  measured,correlation=cv2.phaseCorrelate(a,b)
  expected=(-108*dx*(1 if mode=='A' else .70),108*dy)
  rows.append(dict(mode=mode,cameraDelta=[dx,dy],expected=expected,measured=measured,correlation=correlation,passed=max(abs(measured[i]-expected[i]) for i in (0,1))<2))
same=int(np.abs(read('A_0_0.png')-read('B_0_0.png')).max())
out=dict(sameReferenceCompositionMaxDifference=same,rows=rows,passed=same==0 and all(r['passed'] for r in rows))
(p/'motion_measurements.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))

