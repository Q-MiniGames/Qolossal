from pathlib import Path
import re,json
p=Path(__file__).parent
def blocks(text):
    return {m[1]:m[0] for m in re.findall(r'(^--- !u!\d+ &(\d+)\n.*?)(?=^--- !u!|\Z)',text,re.M|re.S)}
allowed_defaults={'platformFootViewport':'.4','groundedFootViewport':'.15','airborneCentreViewport':'.55','groundVerticalSmoothTime':'.24','downhillProbeDistance':'6','downhillMinimumDrop':'.9','maximumDownhillPreview':'.58','manualLookDown':'.58','previewSmoothTime':'.28','nativeStripEndBlend':'0'}
out=[]
for source in (p/'Before').glob('*.unity'):
    before=blocks(source.read_text()); after=blocks((Path('Assets/Scenes/Game')/source.name).read_text())
    added=after.keys()-before.keys()
    assert all('Assembly-CSharp::CameraPlatformSurface' in after[k] for k in added)
    assert not before.keys()-after.keys()
    for k,old in before.items():
        new=after[k]
        for marker in added:new=new.replace('  - component: {fileID: '+marker+'}\n','')
        for name,val in allowed_defaults.items():
            if not re.search(r'^  '+name+':',old,re.M):
                found=re.search(r'^  '+name+r': ([\d.]+)\n',new,re.M)
                if found:
                    assert float(found[1])==float(val),(source.name,name,found[1])
                    new=new.replace(found[0],'')
        assert old==new,(source.name,k,'Unexpected serialized change')
    out.append(dict(scene=source.name,markers=len(added),allExistingGeometryAndArtPreserved=True))
(p/'scene_changes.json').write_text(json.dumps(out,indent=2))
print(json.dumps(out))
