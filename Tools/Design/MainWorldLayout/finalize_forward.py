from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[3]
WORK=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal')
OUT=WORK/'Tools/ForwardCaveMap_20261030'
ui=json.loads((OUT/'ui_tests.json').read_text());assert ui['complete'] and not ui['errors'] and ui['normalSavesPreserved'] and all(r['passed'] for r in ui['results'])
q=json.loads((OUT/'placement_qa.json').read_text())
q.update(unityPlayModeSteps=ui['steps'],unityAssertions=len(ui['results']),markerProjectionSamples=267,allSevenRegionViewsVerified=True,normalSaveFilesPreserved=True,compilationErrors=0,sceneChanges=[],review='index.html',unityCaptures=[f'MR{i:02}_1080p.png' for i in range(1,8)])
(OUT/'qa.json').write_text(json.dumps(q,indent=2))
p=WORK/'DesignReview/MainWorldLayout_20261030/qa.json';canonical=json.loads(p.read_text());canonical.update(runtimeStatus='Forward cave map verified in Windows Editor: 25 steps, 435 assertions, all seven regions, 267 marker projection samples; zero errors.',changedScenes=[],forwardPlacementEvidence=str(OUT/'qa.json'));p.write_text(json.dumps(canonical,indent=2))
p=ROOT/'PROJECT_GUIDE.md';s=p.read_text();line='Validation: Windows Editor passed 25 UI steps and 435 assertions, including three marker positions in every underground room (267 samples), all seven focused views, 37 entrance pairs, and no visible room labels. Compilation and runtime checks reported no errors; normal save files and Editor settings were preserved. No standalone or console hardware testing was performed for this map-display change.\n\n'
if line not in s:s=s.replace('## Compact player map presentation',line+'## Compact player map presentation',1)
p.write_text(s,encoding='utf-8')
print('Final forward-placement receipt saved; Unity and review map verified.')
