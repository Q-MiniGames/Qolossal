from pathlib import Path
import hashlib,json,shutil,struct

task=Path(__file__).resolve().parent
root=task.parents[2]
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
play=read(task/'Play/result.json');extra=read(task/'Extra/result.json')
for result in (play,extra):
    assert result['complete'] and not result['errors'],result['errors']
    assert all(r['passed'] for r in result['rows'])
    assert result['normalSavesPreserved'] and result['settingsRestored']
rows={r['label']:r for result in (play,extra) for r in result['rows']}
visibility={}
for name in ('running_approach','sprint_approach'):
    before=[s for s in rows[name]['samples'] if s[6]==1 and 182<s[5]<183]
    visibility[name]=max(s[13] for s in before)
    assert visibility[name]>=.05
assert rows['automatic_ledge']['last'][13]>=.08
assert rows['qvale_jump']['maxCameraStep']<.1 and rows['qvale_jump']['cameraTravel']<.3
for name in ('keyboard_look_down','right_stick_look_down','dpad_look_down'):
    assert rows[name]['last'][12]>3
for name in ('flat_idle','release_recovery','modal_blocks_peek','sling_aim_blocks_peek','walk_cancels_manual'):
    assert rows[name]['last'][12]<.06
inventory=read(root/'Tools/Design/ConnectedWorldMap_20261030/current_scene_inventory.json')
changed=[name for name,h in inventory['sceneHashes'].items() if hashlib.sha256((root/'Assets/Scenes/Game'/f'{name}.unity').read_bytes()).hexdigest()!=h]
assert not changed,changed
output=root/'output/images';output.mkdir(exist_ok=True,parents=True)
for suffix,w,h in [('1080p',1920,1080),('4K',3840,2160)]:
    source=task/f'Play/automatic_ledge_{suffix}.png'
    assert struct.unpack('>II',source.read_bytes()[16:24])==(w,h)
    shutil.copy2(source,output/f'Downhill_Preview_{suffix}.png')
qa=dict(passed=True,date='2026-11-04',cases=len(rows),renderedFrames=sum(r['frames'] for r in rows.values()),
        landingBeforeDeparture=rows['automatic_ledge']['last'][13],landingBeforeMovingEdge=visibility,
        ordinaryJumpMaxCameraStep=rows['qvale_jump']['maxCameraStep'],
        manualInputs=['Keyboard S (Down Arrow also bound)','Right stick down','D-pad down'],
        inputOwnershipChecked=['Modal UI','Held staff-shot aim','Movement cancels manual peek'],
        normalSavesPreserved=True,editorSettingsRestored=True,unchangedSceneHashes=len(inventory['sceneHashes']),
        codeSHA256=hashlib.sha256((root/'Assets/Scripts/CameraFollow.cs').read_bytes()).hexdigest(),
        platformReadiness={'tested':'Windows Unity Editor; virtual keyboard and gamepad; full-stick running, sprint and jump trials; 1920x1080 and native 3840x2160 captures',
        'notTested':['Windows player build','PS5','Xbox Series X','Xbox Series S','Switch 2 handheld','Switch 2 docked','Physical controllers','CPU/GPU frame time','Peak memory'],
        'textureImportProfile':'Unchanged; camera zoom, artwork and source resolution unchanged'},
        limitation='Preview is bounded to keep Qori visible. Arbitrarily deep drops can still extend beyond it.')
(task/'QA.json').write_text(json.dumps(qa,indent=2))
print(json.dumps(qa,indent=2))
