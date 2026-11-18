"""Build the review from current source metadata; never infer an integration pass."""
from pathlib import Path
import argparse
import html
import json

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--status', default='Regional art and terrain are integrated. Validation status below is read from saved receipts.')
args = parser.parse_args()
regions = [
 ('MR01','Cradle','Toes, foot hollows and ankle','Sheltered mossy spring basin, pale rounded limestone, fern banks and tiny woodland shelters','Grip shrine, quake crossing and catching floor'),
 ('MR02','Long Causeway','Ascending shin','Weathered road-stone over an aqueduct gorge, rooted waystation ruins and high cisterns','Living Thread lift wall and its approach'),
 ('MR03','Terraces','Kneecap crest and descending thigh','Honey limestone orchard shelves, irrigation channels, old mills and cascading sluices','Sluice gate and lintel; Bloomfall progression'),
 ('MR04','Qvale','Sheltered lap basin','A warm-spring river town, flowering gardens and the listening tree','Buildings, shops, doorways and town services'),
 ('MR05','Ribwood','Belly rise and rib ridges','Ancient root bridges, fern gardens and forest canopy beneath sweeping stone arches','Wind Leaf shrine and deliberate climbing walls'),
 ('MR06','Windward Heights','Shoulder crest, collarbone and neck dip','The approved river valley: wooded islands, waterfalls, weathered arches and wind-worn ivory limestone','Glidecap shrine, glide gorge and catching floor'),
 ('MR07','Summit','Chin, face, brow and crown','Pearl limestone, pale alpine flowers, reeds, turquoise high lakes and waterfall shelves','Reveal ledge, lake shore and ending approach'),
]
native = json.loads((HERE/'Underground/native_assets.json').read_text(encoding='utf-8-sig'))
caves = [x for x in native if 'title' in x]
assert len(caves) == 12
plans = {}
for rid,*_ in regions:
 p = HERE/'Surface'/f'{rid}_plan.json'
 data = json.loads(p.read_text()) if p.exists() else []
 plans[rid] = sum(len(x.get('edits', [])) for x in data)

def receipt(path):
 p=HERE/path
 return json.loads(p.read_text(encoding='utf-8-sig')) if p.exists() else None

applied={r[0]:receipt(f'Surface/{r[0]}_applied.json') for r in regions}
validated={r[0]:receipt(f'Surface/{r[0]}_validation.json') for r in regions}
all_applied=all(isinstance(x,dict) for x in applied.values())
all_validated=all(isinstance(x,dict) and 'errors' in x for x in validated.values())
applied_count=sum(x.get('edits',0) for x in applied.values() if x)
error_count=sum(len(x.get('errors',[])) for x in validated.values() if x)
application_summary=(f'{applied_count} connectors applied across all seven regions; static validation reports {error_count} errors.' if all_applied and all_validated else 'Application and validation receipts incomplete; inspect individual regions.')
ug_application=receipt('Underground/application.json') or []
ug_summary=f"{len(ug_application)} room records, {sum(x.get('rounded',0) for x in ug_application)} rounded terrain pieces and {sum(x.get('painted',0) for x in ug_application)} painted pieces in the underground application receipt."
res=receipt('asset_resolution_requirements.json') or {}
assets=res.get('assets',[])
resolution_summary=(f"{res.get('placements',0):,} active placements, {len(assets)} raster assets; {sum(not x.get('nativePass',False) for x in assets)} native and {sum(not x.get('importPass',False) for x in assets)} imported resolution failures." if assets else 'Resolution audit pending.')
checks=[('Playtest/surface_playtest_result.json','Surface live connector smoke'),('Underground/settle_result.json','Underground live placement/settling'),('Map/ui_tests.json','Unity map UI tests'),('QA.json','Final QA')]
for p in sorted(HERE.rglob('*.json')):
 if ('runtime' in p.name.lower() or p.name in ('final_result.json','final_validation.json')) and 'Before' not in p.parts:
  checks.append((p.relative_to(HERE).as_posix(),p.stem.replace('_',' ')))
evidence=[('RESOLUTION_4K.md','Exact 4K audit'),('asset_resolution_requirements.csv','Per-asset requirements CSV'),('asset_resolution_requirements.json','Per-asset requirements JSON'),('density_instances.json','Per-placement measurements'),('accepted_source_integrity.json','Accepted-source integrity'),('topology_refresh.json','Map/topology refresh'),('Underground/audit.json','Underground static audit'),('Underground/visual_qa.json','Underground visual QA'),('Underground/landing_repairs.json','Landing repairs'),('VISUAL_QA.md','Surface visual QA'),('Surface/CAP_JOIN_QA.md','Cap join QA'),('Playtest/surface_playtest_cases.json','Durable surface cases'),('Playtest/surface_playtest_captures.json','Play-mode capture metadata'),('Underground/Captures/capture_manifest.json','Underground capture modes')]

def test_status(path):
 d=receipt(path)
 if d is None:return 'Pending — result file not available'
 if not isinstance(d,dict):return 'Recorded — inspect receipt'
 flag=d.get('passed',d.get('success',d.get('pass')))
 status='PASS' if flag is True else 'FAIL — see receipt' if flag is False else 'Recorded — no explicit overall pass flag'
 if d.get('complete') is False:status='Incomplete — '+status
 details=[]
 for key,label in [('caseCount','connector cases'),('checkedCount','placements checked'),('checkpointCount','checkpoints'),('pairCount','return pairs')]:
  if key in d:details.append(f'{d[key]} {label}')
 for key,label in [('errors','errors'),('consoleErrors','console errors')]:
  if isinstance(d.get(key),list):details.append(f'{len(d[key])} {label}')
 return status+(' · '+', '.join(details) if details else '')

md = ['# Qolossal world refresh — 4 November 2026','',args.status,'',
 'This review covers seven surface regions, their 84 zones and the 89-room underground network. Applied counts and test statuses come from saved operation and validation receipts.', '', application_summary, ug_summary,
 '', '## Regional themes', '', '| Region | Titan structure | Environment | Protected gameplay |', '|---|---|---|---|']
for rid,name,body,theme,challenge in regions:
 md.append(f'| {rid} · {name} | {body} | {theme} | {challenge} |')
md += ['', 'The Titan relationship is expressed through the real route, stone folds and natural arches. It does not restore the superseded illustrated Titan contour or place a literal giant portrait behind every level.', '',
 '## Terrain and cave approaches', '',
 'The surface design eases ordinary repeated risers into connectors where space and protected footprints allow. The source plan caps new grade at 1.0 and displacement from the previous local walkline at 3.2 units. Deliberate climbing walls, ability gates, beat pads, entrances, arrivals and encounters remain protected. Cave placement changes must be supported by actual clearance evidence; moving a mouth merely for visual symmetry is not part of the plan.', '',
 '| Region | Applied connectors | Static errors | Evidence |', '|---|---:|---:|---|']
for rid,name,*_ in regions:
 md.append(f'| {rid} · {name} | {applied[rid]["edits"] if applied[rid] else "Pending"} | {len(validated[rid]["errors"]) if validated[rid] else "Pending"} | [applied](Surface/{rid}_applied.json) · [checks](Surface/{rid}_validation.json) · [plan](Surface/{rid}_plan.json) |')
md += ['', 'These counts are application receipts. See [surface design](Surface/DESIGN.md). Underground work retains the 89-room physical network and 63 passages; recorded arrival/checkpoint adjustments are in [landing repairs](Underground/landing_repairs.json). Static collision screening remains separate from live settling and full traversal.', '',
 '## Underground themes', '', '| Set | Theme | Environment |', '|---|---|---|']
for x in caves:
 md.append(f"| {x['set']} | {x['title']} | {x['theme']} |")
md += ['', 'See the [room manifest](Underground/room_manifest.json), [underground handoff](Underground/README.md) and [native asset records](Underground/native_assets.json). Great Heart uses a separate native detail tile; changes to its appearance and state animation require rendered review.', '',
 '## Exact 4K resolution basis', '',
 'At 3840 × 2160 and orthographic half-height 5, the frame covers 17.777778 × 10 world units, so the requirement is 216 display pixels per world unit. For an axis-aligned asset: `required width = ceil(world width × 216)` and `required height = ceil(world height × 216)`. For a repeating material, evaluate one tile and its UV/world mapping, not the full wall bounds. Sheared terrain needs the weakest directional sampling rate. The calculation must use imported dimensions and the closest actual supported zoom.', '',
 '| Asset group | Native dimensions | Intended footprint / mapping | 4K requirement |', '|---|---|---|---|',
 '| 12 new surface Far/Mid layers | 2172 × 724 each | 10 × 3⅓ units | 2160 × 720 each |',
 '| Six new terrain top strips, MR02–MR07 | 2172 × 724 each | 360 PPU, 6.033333 × 2.011111-unit tile; maximum slope 1 | Weakest rate 222.49 PPU, above 216 PPU |',
 '| Twelve underground vistas | 1774 × 887 each | 8.212963 × 4.106481-unit tile at 216 PPU | 1774 × 887 each |',
 '| Heart detail tile | 1254 × 1254 | 5.805556 × 5.805556 units at 216 PPU | 1254 × 1254 |',
 '| Accepted sky masters MR01–MR05 | 3840 × 3240 | At most 17.777778 × 15 units at size 5 | 3840 × 3240 at that footprint |', '',
 'The top-strip shear bound is `360 × (sqrt(5) − 1) / 2 = 222.49` pixels per unit at slope 1. Other fill, cloud and cave assets must use the final per-instance density audit; a filename containing “4K” is not evidence. The original 1920 × 1080 Summit sky cannot be enlarged to pass a native 4K requirement. A resolution-independent gradient and separately sized native clouds are the proposed replacement.', '',
 'No generated source was enlarged to manufacture detail. Native pixel counts do not by themselves guarantee good composition, seam quality, correct import settings or a sharp render. [Surface source manifest](Art/asset_manifest.json), [terrain top QA](Surface/top_source_qa.json), [underground source metadata](Underground/native_assets.json), [instance audit input](density_instances.json).', '',
 '## Review images and evidence', '',
 'Open [the whole-world gallery](index.html). Its screenshot slots load from `Captures/<region>_entry_4K.png`, `_mid_4K.png` and `_cave_4K.png` when those files exist. A loaded image is available for inspection; it is not automatically marked as passing. [Surface painting gallery](Art/index.html).', '',
 'Generation used the built-in image tool. Exact [far prompts](Art/prompts_far.json), [middle prompts](Art/prompts_mid.json), [terrain top prompts](Surface/top_prompts.json), [fill prompts](Art/prompts_fill.json) and [underground prompts/provenance](Underground/native_assets.json) remain reviewable. Accepted sources are retained; new paintings are siblings under `Assets/Art/Prototypes/WorldRefresh_20261104`.', '',
 'Before delivery, verify the refreshed map against actual final scene extraction, protected cave/arrival registrations, all room IDs, rendered seams, 4K density shortfalls and live movement through changed connectors. Test smoothness with the real character; static clearance alone is not enough.', '',
 '## Platform limits', '',
 'Targets remain Windows PC, PS5, Xbox Series X and Series S, and Nintendo Switch 2 handheld/docked. Editor screenshots and source-density calculations do not certify 4K output, 60 FPS or console readiness. Standalone builds, physical controller coverage, CPU/GPU frame time, peak memory and console hardware testing remain separate validation gates. Report final tested modes and unresolved items explicitly.', '']
# Replace design-stage paragraphs with the final measured deliverable and linked evidence.
md=[line for line in md if not line.startswith(('| Accepted sky masters','The top-strip shear bound is','Before delivery, verify','Open [the whole-world gallery]','No generated source was enlarged'))]
md[4:4]=['## Actual Titan profile','','![Measured revised surface contours](TitanProfile.svg)','','The profile uses the actual revised upper terrain with equal X/Y scale and seven anatomy labels. It is a review illustration, not a map geometry replacement. [Profile generator](titan_profile.py).','']
md+=['## Measured environment resolution','',resolution_summary,'','[Exact 4K audit](RESOLUTION_4K.md) · [Per-asset CSV](asset_resolution_requirements.csv) · [Per-asset JSON](asset_resolution_requirements.json). The full audit gives native/imported dimensions and the exact required canvas using weakest-direction mesh sampling. Sky gradients and geometric doorway outlines rasterize at output resolution. No source bitmap was enlarged to manufacture detail. The environment audit excludes characters, UI, cinematics and disabled alternatives.','','## Recorded validation','','| Check | Result | Evidence |','|---|---|---|']
for path,label in checks:
 md.append(f'| {label} | {test_status(path)} | '+(f'[receipt]({path})' if (HERE/path).exists() else 'Pending')+' |')
md+=['','Missing result files remain pending. The review reads results when regenerated. Image presence is not a test pass.','','## Capture galleries and source evidence','','[Open the gallery](index.html): 21 surface Editor views, seven surface Play-mode entry captures, and 15 underground saved-scene/state-fixture views. Underground `_play` filenames are labeled by their capture manifest and are not runtime proof.','']
for path,label in evidence:
 md.append(f'- [{label}]({path})' if (HERE/path).exists() else f'- {label}: pending')
(HERE/'REVIEW.md').write_text('\n'.join(md), encoding='utf-8')

def esc(s): return html.escape(str(s), quote=True)
def source_img(path): return '../../../'+path.replace('\\','/')
cards=[]
for rid,name,body,theme,challenge in regions:
 figures=[]
 for kind,label in [('entry','Entry'),('mid','Mid-region'),('cave','Cave approach')]:
  path=f'Captures/{rid}_{kind}_4K.png'
  figures.append(f'<figure class="capture"><a href="{path}"><img data-src="{path}" alt="{esc(name)} {label} Unity capture" loading="lazy"></a><figcaption>{label} · 3840 × 2160 <span class="availability">Capture pending</span></figcaption><div class="placeholder">Unity capture not available yet</div></figure>')
 assetlinks = f'<a href="../../../Assets/Art/Prototypes/HeightsRiverDetail_20261104/Far_River.png">Approved river painting</a>' if rid=='MR06' else f'<a href="../../../Assets/Art/Prototypes/WorldRefresh_20261104/{rid}/Far.png">Far source</a> · <a href="../../../Assets/Art/Prototypes/WorldRefresh_20261104/{rid}/Mid.png">Middle source</a>'
 cards.append(f'<section id="{rid}"><h2>{esc(name)} <small>{rid}</small></h2><p class="body">{esc(body)}</p><p>{esc(theme)}</p><p class="quiet">Protected: {esc(challenge)}. Source plan: {plans[rid]} proposed connectors; application results are separate.</p><p>{assetlinks} · <a href="Surface/{rid}_plan.json">Terrain plan</a></p><div class="captures">'+''.join(figures)+'</div></section>')
cavecards=[]
for x in caves:
 cavecards.append(f'<article><h3>{esc(x["title"])} <small>{esc(x["set"])}</small></h3><a href="{source_img(x["path"])}"><img src="{source_img(x["path"])}" loading="lazy" alt="{esc(x["title"])} source painting"></a><p>{esc(x["theme"])}</p><p class="quiet">Native {x["width"]} × {x["height"]}; {x["worldWidth"]:.6f} × {x["worldHeight"]:.6f} units at 216 PPU. Source preview only.</p></article>')
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Qolossal · World refresh review</title>
<style>:root{color-scheme:dark}*{box-sizing:border-box}body{margin:0;background:#171d22;color:#e9e6de;font:17px/1.55 system-ui}header,main,footer{max-width:1720px;margin:auto;padding:28px 36px}header{padding-top:50px}h1{font-size:42px;line-height:1.12}h2{font-size:30px;margin-bottom:2px}h3{font-size:22px}small,.quiet{color:#a8b5bc}small{font-size:.55em;font-weight:500}a{color:#b8d6d2}nav{display:flex;gap:12px;flex-wrap:wrap}nav a,button{background:#29363d;border:1px solid #43535a;border-radius:6px;color:#e9e6de;padding:9px 14px;text-decoration:none;font:inherit}button{cursor:pointer}.status{padding:16px 20px;border-left:4px solid #c2a572;background:#262d31}.body{color:#d3b889;margin-top:0}section{padding:22px 0;border-top:1px solid #3b464d;margin-top:25px}p{max-width:1100px}.captures{display:grid;grid-template-columns:repeat(3,1fr);gap:16px}figure{margin:0;background:#222b31;border:1px solid #3b464d;border-radius:8px;overflow:hidden;position:relative;min-width:0}.capture img{display:none;width:100%;aspect-ratio:16/9;object-fit:contain}.capture.ready img{display:block}.placeholder{height:180px;display:grid;place-items:center;color:#87959e;font-size:14px}.capture.ready .placeholder{display:none}figcaption{padding:12px;font-size:14px}.availability{display:block;color:#b6a074}.ready .availability{color:#9fc6aa}.caves{display:grid;grid-template-columns:repeat(2,1fr);gap:24px}.caves article{background:#222b31;padding:18px;border-radius:8px}.caves img{width:100%;display:block}.caves p{font-size:15px}table{width:100%;border-collapse:collapse;font-size:15px}td,th{text-align:left;padding:10px;border-bottom:1px solid #3b464d}code{background:#29363d;padding:3px 6px}footer{color:#acb6bb;font-size:14px}@media(max-width:1000px){.captures{grid-template-columns:1fr}.caves{grid-template-columns:1fr}header,main,footer{padding:22px}h1{font-size:34px}}</style>
<header><p class="body">QOLOSSAL · 4 NOVEMBER 2026</p><h1>A living landscape across the Titan</h1><p>Seven regional themes, terrain refinements and twelve underground environments. The approved Heights river valley sets the level of scenic detail.</p><p class="status">'''+esc(args.status)+'''</p><nav>'''+''.join(f'<a href="#{r[0]}">{esc(r[1])}</a>' for r in regions)+'''<a href="#underground">Underground</a><a href="#resolution">4K resolution</a></nav><p><a href="REVIEW.md">Full review notes</a> · <a href="Art/index.html">All surface painting sources</a> · <button id="refresh">Reload captures</button> <span id="capture-count" class="quiet"></span></p></header><main>'''+''.join(cards)+'''<section id="underground"><h2>Twelve underground themes</h2><p>The source paintings below describe the twelve interior families. Their presence does not prove that all 89 rooms have been rendered or played. The established room network, puzzle routes and passage identities remain the design constraints.</p><p><a href="Underground/room_manifest.json">Room manifest</a> · <a href="Underground/native_assets.json">Exact source dimensions, themes and prompts</a> · <a href="Underground/README.md">Underground implementation notes</a></p><div class="caves">'''+''.join(cavecards)+'''</div></section><section id="resolution"><h2>Resolution from the actual footprint</h2><p>A 3840 × 2160 view at camera half-height 5 covers 10 world units vertically: <strong>216 display pixels per world unit</strong>. Required source width and height are the respective world dimensions multiplied by 216, rounded up. Repeating textures are measured per UV tile; sheared terrain needs its weakest directional density.</p><table><thead><tr><th>Asset</th><th>Native pixels</th><th>Placement</th><th>Required pixels</th></tr></thead><tbody><tr><td>Surface far / middle</td><td>2172 × 724</td><td>10 × 3⅓ units</td><td>2160 × 720</td></tr><tr><td>Underground vista</td><td>1774 × 887</td><td>8.212963 × 4.106481 units</td><td>1774 × 887</td></tr><tr><td>Heart detail tile</td><td>1254 × 1254</td><td>5.805556 × 5.805556 units</td><td>1254 × 1254</td></tr><tr><td>New terrain tops</td><td>2172 × 724</td><td>360 PPU, max slope 1</td><td>Weakest 222.49 PPU ≥ 216</td></tr></tbody></table><p>Source images were not enlarged to manufacture detail. Imported dimensions, camera limits and shader sampling still need verification. A passed pixel count also needs a good-looking composition.</p><p><a href="Art/asset_manifest.json">Surface manifest</a> · <a href="Surface/top_source_qa.json">Terrain source QA</a> · <a href="density_instances.json">Per-instance audit input</a> · <a href="Art/prompts_far.json">Far prompts</a> · <a href="Art/prompts_mid.json">Middle prompts</a></p></section></main><footer>Windows PC, PS5, Xbox Series X/S and Nintendo Switch 2 handheld/docked remain the targets. Source checks and Editor renders do not certify 4K output or 60 FPS on hardware. Standalone, controller, memory, frame-time and console tests must be reported separately. Generated with the built-in image tool; accepted originals retained.</footer>
<script>const imgs=[...document.querySelectorAll('.capture img')];function count(){document.getElementById('capture-count').textContent=imgs.filter(i=>i.closest('figure').classList.contains('ready')).length+' / '+imgs.length+' captures available for review'}function load(){for(const i of imgs){const f=i.closest('figure');i.onload=()=>{f.classList.add('ready');f.querySelector('.availability').textContent='Available · inspect at full size';count()};i.onerror=()=>{f.classList.remove('ready');f.querySelector('.availability').textContent='Capture pending';count()};i.src=i.dataset.src+'?review='+Date.now()}}document.getElementById('refresh').onclick=load;load();</script></html>'''
def evidence_link(path,label):
 return f'<a href="{esc(path)}">{esc(label)}</a>' if (HERE/path).exists() else f'<span class="quiet">{esc(label)}: pending</span>'

def capture_figure(path,label,mode):
 return f'<figure class="capture"><a href="{esc(path)}"><img data-src="{esc(path)}" alt="{esc(label)}" loading="lazy"></a><figcaption>{esc(label)} · 3840 × 2160<br><span class="quiet">{esc(mode)}</span><span class="availability">Capture pending</span></figcaption><div class="placeholder">Capture not available yet</div></figure>'

for rid,*_ in regions:
 old=f'Source plan: {plans[rid]} proposed connectors; application results are separate.'
 new=(f'{applied[rid]["edits"]} applied connectors; {len(validated[rid]["errors"])} static validation errors.' if applied[rid] and validated[rid] else 'Application / static validation pending.')
 page=page.replace(old,new)
page=page.replace('Unity capture"','Editor saved-scene capture"')
profile='<figure style="margin-top:24px"><a href="TitanProfile.svg"><img src="TitanProfile.svg" style="width:100%;display:block" alt="Actual measured surface contours with Titan anatomy labels"></a><figcaption>Actual revised upper terrain, equal X/Y scale. Review illustration; the diagram does not replace map geometry.</figcaption></figure>'
rows=''.join(f'<tr><td>{esc(label)}</td><td>{esc(test_status(path))}</td><td>{evidence_link(path,"Receipt")}</td></tr>' for path,label in checks)
results='<section id="validation"><h2>Recorded validation</h2><p>'+esc(application_summary)+' '+esc(ug_summary)+'</p><table><thead><tr><th>Check</th><th>Recorded result</th><th>Evidence</th></tr></thead><tbody>'+rows+'</tbody></table><p class="quiet">Results read when this review is generated. Missing receipts remain pending. Reload captures updates images only. The 21 regional frames below are Editor saved-scene renders.</p></section>'
page=page.replace('</header><main>',profile+'</header><main>'+results)
playfigs=''.join(capture_figure(f'Playtest/{rid}_entry_play_4K.png',name+' entry','Play-mode native camera render with real player') for rid,name,*_ in regions)
playsection='<section id="playmode"><h2>Surface Play-mode entry captures</h2><p>'+esc(test_status('Playtest/surface_playtest_result.json'))+' · '+evidence_link('Playtest/surface_playtest_result.json','Runtime result')+'</p><p>Seven native entry views. The separate movement fixture checks controlled walking, ground contact, falls, respawns and console errors.</p><div class="captures">'+playfigs+'</div></section>'
manifest=receipt('Underground/Captures/capture_manifest.json') or []
ugfigs=''.join(capture_figure('Underground/Captures/'+Path(x['file']).name,x['view'].replace('_',' '),x.get('mode','Mode not recorded')) for x in manifest)
ugsection='<section id="underground-captures"><h2>Underground scene captures</h2><p>Fifteen views covering twelve environment families and Heart detail/state fixtures. Capture modes follow the manifest; filenames containing _play are not treated as evidence of runtime execution.</p><p>'+esc(test_status('Underground/settle_result.json'))+' · '+evidence_link('Underground/settle_result.json','Live settling result')+'</p><div class="captures">'+ugfigs+'</div></section>'
page=page.replace('<section id="underground">',playsection+ugsection+'<section id="underground">')
page=page.replace('<a href="#underground">Underground</a>','<a href="#playmode">Surface Play-mode</a><a href="#underground-captures">Underground captures</a><a href="#underground">Painting sources</a>')
page=page.replace('Source images were not enlarged to manufacture detail. Imported dimensions, camera limits and shader sampling still need verification. A passed pixel count also needs a good-looking composition.',esc(resolution_summary)+' The exact per-asset audit covers imported dimensions, closest camera and weakest-direction mesh sampling. Sources were not enlarged. Sky gradients and geometric silhouettes rasterize at output resolution. Density and visual quality are separate checks.')
evidencesection='<section id="evidence"><h2>Final evidence and exact requirements</h2><p>'+' · '.join(evidence_link(path,label) for path,label in evidence)+'</p><p>The environment density audit excludes characters, UI, cinematics and disabled alternative artwork. Consult visual findings and runtime receipts alongside pixel counts.</p></section>'
page=page.replace('</main>',evidencesection+'</main>')
(HERE/'index.html').write_text(page,encoding='utf-8')
print(f'Wrote REVIEW.md and index.html: {applied_count} applied connectors; {error_count} static errors; 21 Editor + 7 Play-mode + {len(manifest)} underground capture slots. Status: {args.status}')
