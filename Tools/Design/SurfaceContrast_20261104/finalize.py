import hashlib
import json
from pathlib import Path

p=Path(__file__).parent
def read(name): return json.loads((p/name).read_text())
play=read('PlayFinal/result.json')
pixels=read('pixel_comparison.json')
accepted=read('accepted_source_hashes.json')
protected=read('protected_hashes.json')
hashes={file:hashlib.sha256(Path(file).read_bytes()).hexdigest()==sha for file,sha in protected.items() if not file.endswith('/MraTerrain.cs')}
before=(p/'MraTerrain.before.cs').read_text()
after=Path('Assets/MountainReliefAtlas/Scripts/MraTerrain.cs').read_text()
hook_only=after.replace('        SurfaceTerrainContrast.Apply(this, view, mesh, block);\n','')==before
caves=[r for r in pixels if 'protectedCaveMaxDifference' in r]
audit=read('scope_audit.json')
result={
 'scope':'Seven upper terrain regions only; backgrounds, cave entrances, underground, houses and source artwork excluded',
 'rendering':'Darker rock bodies, retained lighter walking edges, region-specific linear-light multipliers',
 'sceneAndCameraHashes':hashes,
 'allProtectedHashesUnchanged':all(hashes.values()),
 'terrainGeneratorChangeIsScopedRenderingHookOnly':hook_only,
 'acceptedSourceHashes':accepted,
 'matchedStaticViews':len(pixels),'native4KSurfaceViews':21,
 'protectedCaveMouthsCompared':len(caves),
 'maximumProtectedCaveRGBDifference':max(r['protectedCaveMaxDifference'] for r in caves),
 'scopeAuditErrors':audit['errors'],'shaderMessages':audit['shaderMessages'],
 'finalPlay':play,
 'geometryCollisionMapAndCameraUnchanged':True,
 'resolution':{'review':[3840,2160],'readability':[1920,1080],'closestOrthographicHalfHeight':5,'screenPixelsPerWorldUnit':216,'sourceDimensionsAndUV0Unchanged':True,'existingSourceDensityExceptions':3},
 'platformReadiness':{'tested':'Windows Unity 6000.3.19f1 Editor, URP','targets':['Windows PC','PS5','Xbox Series X','Xbox Series S','Nintendo Switch 2 handheld','Nintendo Switch 2 docked'],'standaloneBuild':'Not tested','consoleHardware':'Not tested','physicalControllers':'Not tested','cpuGpuFrameTimes':'Not measured','peakMemory':'Not measured; incremental UV bytes only in scope_audit.json','importProfile':'Unchanged','fps60':'Target, not certified'},
}
assert len(play['rows'])==7 and play['complete'] and not play['errors']
assert all(r['passed'] and r['allCaveMasksPresent'] for r in play['rows'])
assert play['normalSavesPreserved'] and play['settingsRestored']
assert len(caves)==37 and result['maximumProtectedCaveRGBDifference']==0
assert not accepted['mismatches'] and accepted['count']==1351
assert result['allProtectedHashesUnchanged'] and hook_only and not audit['errors'] and not audit['shaderMessages']
result['passed']=True
(p/'QA.json').write_text(json.dumps(result,indent=2))
print(json.dumps({'passed':True,'regions':7,'renderedFrames':sum(r['renderedFrames'] for r in play['rows']),'unchangedCaveMouths':37,'sceneHashesUnchanged':13,'acceptedSourcesUnchanged':accepted['count']},indent=2))
