from pathlib import Path
import shutil,json,hashlib
ROOT=Path(__file__).resolve().parents[3];WORK=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal');OUT=WORK/'Tools/CompactTunnelMap_20261030';OUT.mkdir(parents=True,exist_ok=True)
paths=[ROOT/p for p in ['Assets/Resources/QolossalAtlas/AtlasData.json','Assets/Scripts/World/Atlas/AtlasGraphic.cs','Assets/Scripts/World/Atlas/ConnectedAtlas.cs','Tools/Design/MainWorldLayout/build_layout.py','Tools/Design/MainWorldLayout/viewer.py','Tools/Design/MainWorldLayout/export_runtime.py','PROJECT_GUIDE.md','AGENTS.md']]+list((WORK/'DesignReview/MainWorldLayout_20261030').glob('*'))
receipt=[]
for p in paths:
 if not p.is_file():continue
 base=ROOT if p.is_relative_to(ROOT) else WORK;q=OUT/'Before'/('Project' if base==ROOT else 'Artifacts')/p.relative_to(base);q.parent.mkdir(parents=True,exist_ok=True)
 if not q.exists():shutil.copy2(p,q)
 receipt.append(dict(source=str(p),backup=str(q),sha256=hashlib.sha256(q.read_bytes()).hexdigest()))
(OUT/'backup.json').write_text(json.dumps(receipt,indent=2));print('Backed up',len(receipt),'files')
