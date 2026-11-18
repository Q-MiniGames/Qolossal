from pathlib import Path
import cv2,json,numpy as np
p=Path(__file__).parent
rows=[]
for layer,rates in [('far',(.45,.55)),('middle',(.88,.94))]:
 a=cv2.imread(str(p/f'{layer}_0_0.png'),0).astype(np.float32)
 for dx,dy in [(2,0),(0,2)]:
  b=cv2.imread(str(p/f'{layer}_{dx}_{dy}.png'),0).astype(np.float32)
  measured,correlation=cv2.phaseCorrelate(a,b)
  expected=(-108*dx*rates[0],108*dy*rates[1])
  rows.append(dict(layer=layer,cameraDelta=[dx,dy],expected=expected,measured=measured,correlation=correlation,passed=max(abs(measured[i]-expected[i]) for i in (0,1))<2))
out=dict(passed=all(x['passed'] for x in rows),rows=rows)
(p/'motion_measurements.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))
