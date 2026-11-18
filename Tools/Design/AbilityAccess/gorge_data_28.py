# Run once, straight after MraGorgeWiden.Run has saved the Heights scene: moves the builder, the route
# generator, the route data and the gorge test from the 18 u gorge to the scene's new 28 u.
# Then re-run build_access.py and check_access.py, and the test (MraPlayTest.Run -mraGorgeOnly).
import os, re

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))

def sub(path, a, b):
    p = os.path.join(ROOT, path.replace('/', os.sep))
    raw = open(p, 'rb').read().decode('utf-8')
    if a not in raw: raise SystemExit('not found in %s: %s' % (path, a))
    open(p, 'wb').write(raw.replace(a, b).encode('utf-8'))

sub('Assets/MountainReliefAtlas/Editor/MraWorldBuilder.cs', 'public const float GlideGap = 18f,', 'public const float GlideGap = 28f,')
sub('Tools/MountainReliefAtlas/design_route.py', 'GLIDE_GAP, GLIDE_DROP, LIFT_HEIGHT = 18.0, 3.0, 5.0', 'GLIDE_GAP, GLIDE_DROP, LIFT_HEIGHT = 28.0, 3.0, 5.0')
sub('Assets/MountainReliefAtlas/Scripts/Testing/MraGorgeTest.cs', 'GorgeWidth = 18f;', 'GorgeWidth = 28f;')

# Only the two pieces change, as text, so the file keeps its formatting.
path = 'Assets/MountainReliefAtlas/Resources/MRA/mra_route.json'
p = os.path.join(ROOT, path.replace('/', os.sep))
t = open(p, 'rb').read().decode('utf-8')
nl = '\r\n' if '\r\n' in t else '\n'
start = t.index('"id": "MR06"')
land = t.index('"label": "glide landing"', start)
def point_pattern(x, y): return r'"x":\s*%s,\s*"y":\s*%s' % (re.escape(x), re.escape(y))
a = re.compile(point_pattern('412.0', '221.0')); b = re.compile(point_pattern('418.0', '221.0'))
m = a.search(t, land); t = t[:m.start()] + m.group(0).replace('412.0', '422.0') + t[m.end():]
m = b.search(t, land); t = t[:m.start()] + m.group(0).replace('418.0', '428.0') + t[m.end():]
# The next piece starts at the old landing's end: (418, 221) then (427.5, 213.9) and (427.5, 209.1).
m = b.search(t, m.end())
if not m: raise SystemExit('piece 02 not found')
t = t[:m.start()] + m.group(0).replace('418.0', '428.0') + t[m.end():]
s = re.compile(r'"x":\s*427\.5[\d]*,(\s*)"y":\s*213\.\d+')
m = s.search(t, m.end()); t = t[:m.start()] + '"x": 428.0,' + m.group(1) + '"y": 208.75' + t[m.end():]
s = re.compile(r'\{\s*"x":\s*427\.5[\d]*,\s*"y":\s*209\.\d+\s*\},?\s*')
m = s.search(t, m.end() if m else start)
t = t[:m.start()] + t[m.end():]
open(p, 'wb').write(t.encode('utf-8'))
print('gorge data now 28 u; rebuild ABILITY_ACCESS.json next')
