from pathlib import Path
import json, csv, hashlib, collections
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
data=json.loads((OUT/'density_instances.json').read_text(encoding='utf-8-sig'))
grouped=collections.defaultdict(list)
for row in data['rows']: grouped[row['asset']].append(row)
assets=[]
for path,rows in sorted(grouped.items()):
    r=rows[0]
    assets.append(dict(asset=path,sha256=hashlib.sha256((ROOT/path).read_bytes()).hexdigest(),nativeWidth=r['sourceWidth'],nativeHeight=r['sourceHeight'],importedWidth=r['importedWidth'],importedHeight=r['importedHeight'],requiredWidth=max(x['requiredWidth'] for x in rows),requiredHeight=max(x['requiredHeight'] for x in rows),minimumPixelsPerUnit=round(min(x['nativeMinimumPixelsPerUnit'] for x in rows),5),placements=len(rows),scenes='; '.join(sorted({x['scene'] for x in rows})),nativePass=all(x['nativePass'] for x in rows),importPass=all(x['importPass'] for x in rows)))
failures=[a for a in assets if not a['nativePass'] or not a['importPass']]
coverage=[]
for path in sorted({r['asset'] for r in data.get('coverageRows',[])}):
    rows=[r for r in data['coverageRows'] if r['asset']==path];r=rows[0]
    coverage.append(dict(asset=path,kind='derived-roof-coverage',approvedSource=r['approvedSource'],sourceWidth=r['sourceWidth'],sourceHeight=r['sourceHeight'],importedWidth=r['importedWidth'],importedHeight=r['importedHeight'],requiredWidth=max(x['requiredWidth'] for x in rows),requiredHeight=max(x['requiredHeight'] for x in rows),minimumPixelsPerUnit=min(x['nativeMinimumPixelsPerUnit'] for x in rows),nativePass=all(x['nativePass'] for x in rows),importPass=all(x['importPass'] for x in rows),placements=len(rows),sha256=hashlib.sha256((ROOT/path).read_bytes()).hexdigest(),classification=r['classification']))
result=dict(display=[3840,2160],closestCameraSize=5,screenPixelsPerWorldUnit=216,placements=len(data['rows']),assetCount=len(assets),passed=not failures and not data['skipped'],failures=failures,skipped=data['skipped'],scope='Active environment across 13 shipping scenes, including approved rim UV1 and live relocated meshes. Excludes characters, UI and cinematics.',assets=assets)
result['derivedCoverage']=coverage
(OUT/'asset_resolution_requirements.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
with (OUT/'asset_resolution_requirements.csv').open('w',newline='',encoding='utf-8-sig') as f:
    writer=csv.DictWriter(f,fieldnames=list(assets[0]));writer.writeheader();writer.writerows(assets)
lines=['# Revised environment resolution audit','','Target: 3840 × 2160. Closest camera half-height: 5 world units. The display requires 216 pixels/world unit and spans 17.777778 × 10 world units.', '',f'Placements: {len(data["rows"])}. Distinct raster sources: {len(assets)}. Resolution exceptions: {len(failures)}. Skipped mappings: {len(data["skipped"])}.', '', 'Required dimensions are calculated from the largest affine world-distance-per-source-texel singular value of every nondegenerate mesh triangle. This includes the approved rim painting’s registered UV1 channel and the live relocated/bent entrance meshes. Native world-sampling shaders are audited from their explicit pixels-per-unit mapping. Parallax is evaluated at the closest authored zoom. Requirements are rounded upward per source canvas, preserving aspect ratio.', '', 'The CSV and JSON contain every source/import size, exact required canvas, usage and hash. A painting does not need to be a full 4K image if it occupies only part of the screen. No bitmap enlargement counts as new native detail.', '', '| Newly painted source | Native pixels | Required pixels | Native/import pass |','|---|---:|---:|---|']
for a in assets:
    if '/WorldRevision_20261104/' in a['asset']:
        lines.append(f'| {a["asset"].split("WorldRevision_20261104/")[1]} | {a["nativeWidth"]} × {a["nativeHeight"]} | {a["requiredWidth"]} × {a["requiredHeight"]} | {a["nativePass"]}/{a["importPass"]} |')
if failures:
    lines+=['','## Exceptions','']
    lines += [f'- {a["asset"]}: source {a["nativeWidth"]} × {a["nativeHeight"]}; required {a["requiredWidth"]} × {a["requiredHeight"]}.' for a in failures]
if coverage:
    lines+=['','## Derived coverage masks','','These masks close geometry above retained approved paint. They add no painted RGB detail. Their inherited source-boundary sampling is measured separately and is not counted as new native artwork.']
    for a in coverage:
        lines.append(f'- {a["asset"]}: {a["sourceWidth"]} × {a["sourceHeight"]}; measured requirement {a["requiredWidth"]} × {a["requiredHeight"]}; native/import pass {a["nativePass"]}/{a["importPass"]}. Derived from {a["approvedSource"]}.')
lines+=['','Windows Editor evidence is separate from platform certification. PS5, Xbox Series X, Xbox Series S, Nintendo Switch 2 handheld and docked builds/hardware were not tested. 60 FPS remains a target. Preserve native masters and derive runtime platform profiles from measured builds. Original Switch is excluded.']
(OUT/'RESOLUTION_4K.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print({k:result[k] for k in ('passed','placements','assetCount')}, 'failures',len(failures),'skipped',len(data['skipped']))
