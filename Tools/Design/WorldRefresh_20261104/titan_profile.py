"""A review-only silhouette drawn from the measured playable contour, not a new map transform."""
from pathlib import Path
import json,html
OUT=Path(__file__).resolve().parent
d=json.loads(Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/MainWorldLayout_20261030/world-map-data.json').read_text())
contours=[c for c in d['surfaceContours'] if c.get('primary')]
points=[p for c in contours for p in c['points']]
x0=min(p[0] for p in points);x1=max(p[0] for p in points)
y0=min(p[1] for p in points);y1=max(p[1] for p in points)
scale=1600/(x1-x0);base=(y1-y0)*scale+130;height=base+150
colors=['#aac99b','#ddc089','#c4c99a','#dcbaa1','#8da97b','#a3bfcd','#c4c8dc']
anatomy=['Toes · ankle','Shin','Knee · thigh','Lap basin','Belly · ribs','Shoulder · neck','Face · crown']
svg=[f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1720 {height:.2f}" role="img" aria-label="Measured playable Titan profile"><rect width="1720" height="{height}" fill="#171d24"/><style>text{{font-family:Segoe UI,sans-serif;fill:#e8e0d5}}.small{{font-size:14px;fill:#b5bec8}}</style><text x="60" y="44" font-size="25">Qolossal · the playable Titan</text><text x="60" y="71" class="small">Actual revised terrain contours; seven separately loaded regions assembled without stretching slopes.</text>']
for i,r in enumerate(d['regions']):
    cs=[c for c in contours if c['region']==r['id']]
    for c in cs:
        ps=[(60+(x-x0)*scale,110+(y-y0)*scale) for x,y in c['points']]
        if len(ps)<2:continue
        path='M'+' L'.join(f'{x:.2f},{y:.2f}' for x,y in ps)
        svg.append(f'<path d="{path} L{ps[-1][0]:.2f},{base:.2f} L{ps[0][0]:.2f},{base:.2f} Z" fill="{colors[i]}" fill-opacity=".32"/><path d="{path}" fill="none" stroke="{colors[i]}" stroke-width="2.8"/>')
    cx=60+((r['left']+r['right'])/2-x0)*scale
    svg.append(f'<text x="{cx:.2f}" y="{base+36:.2f}" font-size="16" text-anchor="middle">{html.escape(anatomy[i])}</text><text x="{cx:.2f}" y="{base+61:.2f}" class="small" text-anchor="middle">{html.escape(r["name"])}</text>')
svg.append(f'<text x="60" y="{height-30}" class="small">38 ordinary stair stretches eased. Landmark pads, cave mouths, ability walls and region endpoints remain protected.</text></svg>')
(OUT/'TitanProfile.svg').write_text('\n'.join(svg),encoding='utf-8')
print('TitanProfile.svg generated from current measured terrain')
