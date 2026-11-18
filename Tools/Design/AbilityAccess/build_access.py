# Generates ABILITY_ACCESS.json: the one place that says which area, door and room needs which relic.
# Reads the game's real data and never writes to it (no Unity needed):
#   the scenes             what the doors, portals, gates and shrines actually enforce
#   mra_route.json         the surface route's walls and modules
#   AtlasData.json, mra_world.json   what the map and the older design data claim
#   rooms.json, platforming_v1.json  the underground rooms and their layouts
# Every room and door result carries a verdict from movesim's two envelopes: pass (proven possible),
# blocked (proven impossible), grey (needs hands on it). Run check_access.py for the rules and report.
# usage: python build_access.py
import hashlib, itertools, json, os, re, sys
from movesim import ABILITIES, Reach, read_tuning

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
PLATFORMING = 'Tools/Design/UndergroundPlatforming_20261030/'
SOURCES = dict(
    atlas='Assets/Resources/QolossalAtlas/AtlasData.json',
    world='Assets/MountainReliefAtlas/Resources/MRA/mra_world.json',
    route='Assets/MountainReliefAtlas/Resources/MRA/mra_route.json',
    rooms=PLATFORMING + 'rooms.json',
    platforming=PLATFORMING + 'platforming_v1.json',
    tuningCode='Assets/Scripts/Tuning/MovementTuning.cs',
    tuningAsset='Assets/Resources/Tuning/MovementTuning.asset',
)
SCENES = ['MR01_Cradle', 'MR02_Causeway', 'MR03_Terraces', 'MR04_Qvale', 'MR05_Ribwood', 'MR06_Heights', 'MR07_Summit']
RELICS = [  # the code's Relics ids, the checker's short names, and what each gives
    ('double-jump', 'dj', 'Leapseed', 'Double jump'),
    ('climbing-moss', 'moss', 'Climbing Moss', 'Wall slide and wall jump'),
    ('living-thread', 'thread', 'Living Thread', 'Grapple and swing, 4 u range'),
    ('bloomfall', 'pod', 'Bloomfall', 'Downward strike bounce (pods, enemies)'),
    ('wind-leaf', 'dash', 'Wind Leaf', 'Dash and air dash'),
    ('glidecap', 'glide', 'Glidecap', 'Glide'),
]
SHORT = {r[0]: r[1] for r in RELICS}
LONG = {r[1]: r[0] for r in RELICS}
NAME = {r[1]: r[2] for r in RELICS}
ALL = frozenset(ABILITIES)
KIND_USES = dict(anchor='thread', pod='pod', gust='gust')


def path(rel): return os.path.join(ROOT, rel.replace('/', os.sep))
def load(rel): return json.load(open(path(rel), encoding='utf-8-sig'))
def sha(rel): return hashlib.sha256(open(path(rel), 'rb').read()).hexdigest()


# ------------------------------------------------------------------ the scenes, read as text

def script_guids():
    out = {}
    for root in ('Assets/MountainReliefAtlas/Scripts', 'Assets/Scripts'):
        for dp, _, fs in os.walk(path(root)):
            for f in fs:
                if f.endswith('.cs.meta'):
                    m = re.search(r'guid: (\w+)', open(os.path.join(dp, f), encoding='utf-8').read())
                    out[m.group(1)] = f[:-8]
    return out

def relic_guids():
    out = {}
    folder = path('Assets/Resources/Relics')
    for f in os.listdir(folder):
        if f.endswith('.asset'):
            guid = re.search(r'guid: (\w+)', open(os.path.join(folder, f + '.meta'), encoding='utf-8').read()).group(1)
            out[guid] = re.search(r'abilityId: (\S+)', open(os.path.join(folder, f), encoding='utf-8').read()).group(1)
    return out

def yaml_list(doc, key):
    m = re.search(r'^  %s:(.*?)(?=^  [A-Za-z_]|\Z)' % key, doc, re.M | re.S)
    if not m: return []
    return [x.strip().strip("'\"") for x in re.findall(r'- (.+)', m.group(1))]

def read_scene(name, guids, relics):
    text = open(path('Assets/Scenes/Game/%s.unity' % name), encoding='utf-8', errors='replace').read()
    docs = re.split(r'^--- !u!', text, flags=re.M)
    names, active, tr, by_go = {}, {}, {}, {}
    for d in docs:
        m = re.match(r'1 &(\d+)', d)
        if m:
            n = re.search(r'm_Name: (.*)', d)
            names[m.group(1)] = n.group(1).strip().strip("'\"") if n else ''
            a = re.search(r'm_IsActive: (\d)', d)
            active[m.group(1)] = a is None or a.group(1) == '1'
        m = re.match(r'(?:4|224) &(\d+)', d)
        if m:
            go = re.search(r'm_GameObject: \{fileID: (\d+)\}', d)
            pos = re.search(r'm_LocalPosition: \{x: ([-\d.e]+), y: ([-\d.e]+)', d)
            scale = re.search(r'm_LocalScale: \{x: ([-\d.e]+), y: ([-\d.e]+)', d)
            father = re.search(r'm_Father: \{fileID: (\d+)\}', d)
            if go and pos:
                tr[m.group(1)] = dict(go=go.group(1), x=float(pos.group(1)), y=float(pos.group(2)),
                                      sx=float(scale.group(1)) if scale else 1., sy=float(scale.group(2)) if scale else 1.,
                                      father=father.group(1) if father else '0')
                by_go[go.group(1)] = m.group(1)
    def world(go):
        t = tr.get(by_go.get(go))
        chain = []
        while t: chain.append(t); t = tr.get(t['father'])
        x, y = 0., 0.
        for t in chain:
            x, y = t['x'] + x * t['sx'], t['y'] + y * t['sy']
        return round(x, 2), round(y, 2)
    out = dict(doors=[], portals=[], exits=[], gates=[], shrines=[], names=set(names.values()), portalCount=0)
    for d in docs:
        if not d.startswith('114 '): continue
        g = re.search(r'm_Script: \{fileID: \d+, guid: (\w+)', d)
        script = guids.get(g.group(1)) if g else None
        if script not in ('MraChamberDoor', 'MraUgPortal', 'MraExit', 'MraGate', 'AbilityShrine', 'Portal'): continue
        go = re.search(r'm_GameObject: \{fileID: (\d+)\}', d).group(1)
        field = lambda k: (re.search(r'^  %s: (.*)$' % k, d, re.M) or [0, ''])[1].strip().strip("'\"")
        rec = dict(name=names.get(go, '?'), at=world(go))
        if script == 'MraChamberDoor':
            rec.update(chamber=field('chamberId'), requires=['relic:' + field('prerequisite')] if field('prerequisite') else [])
            out['doors'].append(rec)
        elif script == 'MraUgPortal':
            rec.update(target=field('arrivalId'), requires=yaml_list(d, 'requires'), unlockFlag=field('unlockFlag'))
            out['portals'].append(rec)
        elif script == 'MraExit':
            rec.update(target=field('arrivalId'), scene=field('targetScene'), requires=yaml_list(d, 'requires'))
            out['exits'].append(rec)
        elif script == 'MraGate':
            rec.update(requires=yaml_list(d, 'requires'))
            out['gates'].append(rec)
        elif script == 'AbilityShrine':
            rec.update(active=active.get(go, True), relic=relics.get((re.search(r'ability: \{fileID: \d+, guid: (\w+)', d) or [0, ''])[1], '?'))
            out['shrines'].append(rec)
        elif script == 'Portal':
            out['portalCount'] += 1
    return out


# ------------------------------------------------------------------ the surface route

def risers(pieces):
    """The route's walls, as (x, y at the bottom, height, clingable); a tread under a body's width merges two walls."""
    out = []
    for p in pieces:
        pts = p['points']
        for a, b in zip(pts, pts[1:]):
            dy = b['y'] - a['y']
            if abs(b['x'] - a['x']) < .2 and abs(dy) > .3:
                if out and out[-1]['clingable'] == p['clingable'] and abs(out[-1]['top'] - a['y']) < .01 and \
                        abs(a['x'] - out[-1]['x']) < .8 and (dy > 0) == (out[-1]['h'] > 0):
                    out[-1]['h'] = round(out[-1]['h'] + dy, 3); out[-1]['top'] = b['y']
                else:
                    out.append(dict(x=round(a['x'], 2), y=round(a['y'], 2), h=round(dy, 3), top=b['y'], clingable=p['clingable']))
    return out

def route_length(pieces, x0, x1):
    x0, x1 = min(x0, x1), max(x0, x1)
    total = 0.
    for p in pieces:
        for a, b in zip(p['points'], p['points'][1:]):
            lo, hi = max(x0, min(a['x'], b['x'])), min(x1, max(a['x'], b['x']))
            if hi < lo: continue
            full = abs(b['x'] - a['x'])
            k = 1. if full < .01 else (hi - lo) / full
            if full < .01 and not (x0 <= a['x'] <= x1): continue
            total += k * ((b['x'] - a['x']) ** 2 + (b['y'] - a['y']) ** 2) ** .5
    return total


class Surface:
    def __init__(self, world, route, sim):
        self.sim = sim
        self.route = {r['id']: r for r in route['regions']}
        self.regions = world['regions']
        self.region = {r['id']: r for r in self.regions}
        self.nodes, self.edges = {}, []
        for r in self.regions:
            rt = self.route[r['id']]
            walls = risers(rt['pieces'])
            modules = {f['edge']: f['name'] for f in rt['features'] if f['kind'] == 'module'}
            for i, n in enumerate(r['nodes']):
                self.nodes[n['id']] = dict(n, region=r['id'], index=i)
            for e in r['edges']:
                a, b = r['nodes'][int(e['source'][-2:])], r['nodes'][int(e['target'][-2:])]
                x0, x1 = a['position']['x'], b['position']['x']
                self.edges.append(dict(id=e['id'], source=e['source'], target=e['target'], kind=e['kind'],
                                       requires=e['requires'], module=modules.get(e['id'], ''), x0=x0, x1=x1,
                                       walls=[w for w in walls if min(x0, x1) <= w['x'] <= max(x0, x1)], region=r['id']))
        self.links = world['links']
        self.gorge = None
        rt = self.route['MR06']
        launch = [p for p in rt['pieces'] if p['label'] == '' ][0]['points'][-1]
        land = [p for p in rt['pieces'] if p['label'] == 'glide landing'][0]['points'][0]
        self.gorge = dict(width=land['x'] - launch['x'], drop=land['y'] - launch['y'])

    def wall_ok(self, wall_h, clingable, ab, mode):
        if wall_h <= 0: return True
        if 'moss' in ab and clingable: return True
        return self.sim.reach(mode, 0., wall_h, ab, grab=clingable)

    def walk(self, edge, forward, ab, flags, mode):
        """Can this edge be walked in this direction? Returns (ok, what stops it)."""
        m = edge['module']
        if forward:
            if m == 'root-gate-with-fallen-crossing' and 'knot:grip' not in flags: return False, 'knot:grip'
            if m == 'flat-thread-gap' and 'thread' not in ab: return False, 'thread lift'
            if m == 'glide-drop' and not self.sim.reach(mode, self.gorge['width'], self.gorge['drop'], ab): return False, 'glide gorge'
        sign = 1 if forward == (edge['x1'] >= edge['x0']) else -1
        for w in edge['walls']:
            if not self.wall_ok(w['h'] * sign, w['clingable'], ab, mode):
                return False, 'wall %.1f u at x %.1f' % (abs(w['h']), w['x'])
        return True, ''


# ------------------------------------------------------------------ the rooms

class Rooms:
    def __init__(self, rooms, plan, sim):
        self.rooms, self.plan, self.sim = rooms, plan, sim
        self.cache = {}

    def segments(self, r, spec):
        w, h = r['w'], r['h']; segs = []
        shapes = spec['shapes'] if spec else []
        blocked = [(s['local'][0], s['local'][0] + s['local'][2]) for s in shapes
                   if s['kind'] == 'water' or s['kind'] == 'thorns' and s['local'][1] <= .1 and s['local'][2] > 1]
        blocked += [(d['x0'], d['x1']) for d in r['doors'] if d['side'] == 'B']
        blocked += [(s['local'][0], s['local'][0] + s['local'][2]) for s in shapes if s['kind'] == 'grille' and s['local'][1] <= 0 and s['local'][2] < 2]
        xs = sorted({0., w} | {v for b in blocked for v in b})
        for a, b in zip(xs, xs[1:]):
            m = (a + b) / 2
            if 0 <= m <= w and not any(x0 <= m <= x1 for x0, x1 in blocked): segs.append(('floor', a, b, 0.))
        for s in shapes:
            x, y, sw, sh = s['local']
            if s['kind'] == 'solid': segs.append(('top', x, x + sw, y + sh))
            elif s['kind'] == 'rock': segs.append(('rock', x, x + sw, y + sh))
            elif s['kind'] in ('deck', 'crumble'): segs.append((s['kind'], x, x + sw, y + sh))
            elif s['kind'] == 'mover':
                k = 'mover#%d' % len(segs)
                segs.append((k, x, x + sw, y + sh))
                segs.append((k, x + s['offset'][0], x + sw + s['offset'][0], y + sh + s['offset'][1]))
            elif s['kind'] == 'pod': segs.append(('pod', x, x + sw, y + sh))
            elif s['kind'] == 'reward': segs.append(('door:REWARD', x + sw / 2 - .3, x + sw / 2 + .3, y + sh / 2 - 1.))
            elif s['kind'] == 'anchor': segs.append(('anchor', x + sw / 2, x + sw / 2, y + sh / 2))
            elif s['kind'] == 'quarter': segs.append(('door:QUARTER_' + s['flag'], x, x + sw, y))
        for d in r['doors']:
            tag = 'door:' + d['to']
            if d['side'] == 'L': segs.append((tag, -1.5, 0., d['sill']))
            elif d['side'] == 'R': segs.append((tag, w, w + 1.5, d['sill']))
            elif d['side'] == 'T': segs.append((tag, d['x0'] + .4, d['x1'] - .4, h))
            else: segs.append((tag, d['x0'] + .4, d['x1'] - .4, -0.01))
        for m in r['markers']:
            if m['kind'] == 'Arrival': segs.append(('door:ARRIVAL', m['x'] - .1, m['x'] + .1, m['y'] - .66))
            if m['kind'] == 'Reward': segs.append(('door:REWARD', m['x'] - .3, m['x'] + .3, m['y'] - .66))
        return segs

    def graph(self, rid, ab, mode):
        key = (rid, ab, mode)
        if key in self.cache: return self.cache[key]
        r, spec = self.rooms[rid], self.plan.get(rid)
        shapes = spec['shapes'] if spec else []
        segs = self.segments(r, spec)
        w, sim = r['w'], self.sim
        gates = [s for s in shapes if s['kind'] == 'gate']
        grilles = [s['local'] for s in shapes if s['kind'] == 'grille']
        solids = [s['local'] for s in shapes if s['kind'] == 'solid']

        def gap(a, b): return max(0., b[1] - a[2], a[1] - b[2])
        def wall(seg):
            sides = set()
            if seg[1] <= .6: sides.add('L')
            if seg[2] >= w - .6: sides.add('R')
            return sides
        def cling_blocked(side, y0, y1):
            for s in shapes:
                x, y, sw, sh = s['local']
                on = x <= .6 if side == 'L' else x + sw >= w - .6
                if not on or y + sh < y0 or y > y1: continue
                if s['kind'] in ('thorns', 'rock', 'grille'): return True
            return False
        def through_grille(a, b):
            ax, bx = min(max(b[1], a[1]), a[2]), min(max(a[1], b[1]), b[2])
            for gx, gy, gw, gh in grilles:
                for k in range(61):
                    t = k / 60
                    px, py = ax + (bx - ax) * t, a[3] + .6 + (b[3] - a[3]) * t
                    if gx <= px <= gx + gw and gy <= py <= gy + gh: return True
            return False
        hard = [s['local'] for s in shapes if s['kind'] in ('solid', 'rock')]
        def inside(px, py):
            return any(x + .05 < px < x + sw - .05 and y + .05 < py < y + sh - .05 for x, y, sw, sh in hard)
        def line_clear(x0, y0, x1, y1):
            steps = max(1, int(max(abs(x1 - x0), abs(y1 - y0)) / .25))
            return not any(inside(x0 + (x1 - x0) * k / steps, y0 + (y1 - y0) * k / steps) for k in range(steps + 1))
        def walled_off(a, b):
            ax, bx = min(max(b[1], a[1]), a[2]), min(max(a[1], b[1]), b[2])
            ay, by = a[3] + .6, b[3] + .6
            return not (line_clear(ax, ay, bx, by) or (line_clear(ax, ay, ax, by) and line_clear(ax, by, bx, by)) or
                        (line_clear(ax, ay, bx, ay) and line_clear(bx, ay, bx, by)))
        def gated(seg):
            return [f for g in gates for f in g['requires'] if seg[0].startswith('door') and abs(g['local'][1] - seg[3]) < .6 and
                    min(abs(g['local'][0] - seg[1]), abs(g['local'][0] + g['local'][2] - seg[2])) < 2]
        def climb_best(a, b):
            # Any clingable face that both surfaces can reach: the room's walls, and the sides of solids.
            flat = sim._dx(('best', 'dj' in ab, 'dash' in ab), 0.)
            for side in ('L', 'R'):
                x = 0. if side == 'L' else w
                if abs(x - min(max(x, a[1]), a[2])) <= flat and abs(x - min(max(x, b[1]), b[2])) <= flat and not cling_blocked(side, a[3], b[3]): return True
            for x, y, sw, sh in solids:
                for fx in (x, x + sw):
                    if abs(fx - min(max(fx, a[1]), a[2])) <= flat and abs(fx - min(max(fx, b[1]), b[2])) <= 1.5 and b[3] <= y + sh + .1: return True
            return False

        n = len(segs); adj = {i: [] for i in range(n)}
        for i in range(n):
            for j in range(n):
                if i == j: continue
                a, b = segs[i], segs[j]
                if a[0].startswith('door') and a[0] == b[0]: continue
                dy = b[3] - a[3]; dx = gap(a, b); ok = False
                if a[0].startswith('mover') and a[0] == b[0]: adj[i].append((j, [])); continue
                if grilles and through_grille(a, b): continue
                if hard and walled_off(a, b): continue
                if a[0] == 'anchor' or b[0] == 'anchor':
                    if 'thread' not in ab: continue
                    if mode == 'safe':
                        if b[0] == 'anchor' and a[0] == 'anchor': ok = ((a[1] - b[1]) ** 2 + (a[3] - b[3]) ** 2) ** .5 <= 7.5
                        elif b[0] == 'anchor':
                            cx = min(max(b[1], a[1]), a[2]); ok = ((cx - b[1]) ** 2 + (a[3] + .6 - b[3]) ** 2) ** .5 <= 4.0 or sim.reach(mode, dx, dy - .6, ab)
                        else: ok = b[3] <= a[3] + 3 and dx <= 6.5
                    else:
                        if b[0] == 'anchor' and a[0] == 'anchor': ok = ((a[1] - b[1]) ** 2 + (a[3] - b[3]) ** 2) ** .5 <= 11.
                        elif b[0] == 'anchor': ok = sim.reach(mode, max(0., dx - 4.), dy - 4., ab)
                        else: ok = b[3] <= a[3] + 4.5 and dx <= 14.
                else:
                    if b[0] in ('top', 'rock') and dy > 0 and dx == 0 and a[1] >= b[1] and a[2] <= b[2]: continue   # under a solid
                    bonus = (2. if mode == 'safe' else 2.5) if a[0] == 'pod' and 'pod' in ab else 0.
                    ok = sim.reach(mode, dx, dy, ab, bonus, grab=b[0] != 'rock')
                    if not ok and 'moss' in ab and dy > 0:
                        if b[0].startswith('door') and b[3] >= r['h'] - .1: ok = sim.reach(mode, dx, dy - .6, ab)
                        for side in wall(a) & wall(b):
                            if not cling_blocked(side, a[3], b[3]): ok = True
                        if not ok and mode == 'best': ok = climb_best(a, b)
                if ok: adj[i].append((j, gated(b) + gated(a)))
        valves = [(s['flag'], s['local']) for s in shapes if s['kind'] in ('valve', 'boss')]
        tags = {}
        for i, s in enumerate(segs):
            tags.setdefault(s[0], []).append(i)
        def seg_of(local):
            vx, vy = local[0] + .6, local[1]
            return [i for i, s in enumerate(segs) if s[1] - .4 <= vx <= s[2] + .4 and abs(s[3] - vy) < .5]
        valve_at = {flag: seg_of(local) for flag, local in valves}
        self.cache[key] = (segs, adj, tags, valve_at)
        return self.cache[key]

    def explore(self, entries, ab, mode, flags=frozenset(), blocked_rooms=()):
        """From (room, tag) entries, everything reachable: rooms, tags reached and entered, flags thrown. Directed."""
        ab = frozenset(ab); flags = set(flags)
        reached, entered = {}, {}
        while True:
            grew = False
            stack = [e for e in entries]
            seen = set()
            while stack:
                rid, tag = stack.pop()
                if (rid, tag) in seen or rid not in self.rooms or rid in blocked_rooms: continue
                seen.add((rid, tag))
                segs, adj, tags, valve_at = self.graph(rid, ab, mode)
                start = tags.get(tag, [])
                if not start: continue
                entered.setdefault(rid, set()).add(tag)
                visit, todo = set(start), list(start)
                while todo:
                    i = todo.pop()
                    for j, req in adj[i]:
                        if j not in visit and all(f in flags for f in req): visit.add(j); todo.append(j)
                got = {segs[i][0] for i in visit if segs[i][0].startswith('door')}
                reached.setdefault(rid, set()).update(got)
                for flag, at in valve_at.items():
                    if flag not in flags and any(i in visit for i in at): flags.add(flag); grew = True
                for t in got:
                    other = t[5:]
                    if other in self.rooms: stack.append((other, 'door:' + rid))
            if not grew: break
        return dict(reached=reached, entered=entered, flags=flags)

    def inner(self, rid, entry_tags, ab, mode, flags):
        """Within one room, can Qori get from any entry to the reward (or, with no reward, to every other way out)?"""
        ab = frozenset(ab)
        segs, adj, tags, valve_at = self.graph(rid, ab, mode)
        flags = set(flags)
        goals = [t for t in tags if t == 'door:REWARD'] or [t for t in tags if t.startswith('door:') and t != 'door:ARRIVAL']
        got = set()
        for tag in entry_tags:
            start = tags.get(tag, [])
            while True:
                visit, todo = set(start), list(start)
                while todo:
                    i = todo.pop()
                    for j, req in adj[i]:
                        if j not in visit and all(f in flags for f in req): visit.add(j); todo.append(j)
                new = {f for f, at in valve_at.items() if f not in flags and any(i in visit for i in at)}
                if not new: break
                flags |= new
            got |= {segs[i][0] for i in visit}
        return [g for g in goals if g not in entry_tags and g not in got], goals


def minimal_sets(combos, rid, what):
    """The smallest relic sets that do it, per envelope, as relic ids."""
    out = {}
    for mode in ('safe', 'best'):
        sets = []
        for (m, subset), res in sorted(combos.items(), key=lambda kv: len(kv[0][1])):
            if m == mode and rid in res[what] and not any(set(x) <= set(subset) for x in sets): sets.append(subset)
        out[mode] = [sorted(LONG[x] for x in subset) for subset in sets]
    return out

def needs_from(sets):
    """blocked: no set in the best case does it without this relic. grey: only the design-margin envelope says so."""
    needs = {}
    common = lambda ss: set.intersection(*[set(x) for x in ss]) if ss else set()
    for rid in sorted(common(sets['safe']) | common(sets['best'])):
        needs[rid] = 'blocked' if rid in common(sets['best']) else 'grey'
    return needs

def verdict(safe_ok, best_ok):
    return 'pass' if safe_ok else 'blocked' if not best_ok else 'grey'


# ------------------------------------------------------------------ the world, step by step

def predicate_holds(p, ab, flags):
    if p.startswith('relic:'): return SHORT[p[6:]] in ab
    return p in flags or p.replace('flag:', '') in flags

def world_walk(surface, doors, rooms, shrine_nodes, exclude, mode):
    """A player finding everything from a fresh save, never given `exclude`. Returns what they can get to."""
    ab, flags = set(), set()
    reached = {'MR01_N00'}
    log = []
    under = dict(reached={}, entered={}, flags=set())
    while True:
        before = (len(ab), len(flags), len(reached))
        grew = True
        while grew:
            grew = False
            for e in surface.edges:
                for src, dst, fwd in ((e['source'], e['target'], True), (e['target'], e['source'], False)):
                    if src in reached and dst not in reached and surface.walk(e, fwd, frozenset(ab), flags, mode)[0]:
                        reached.add(dst); grew = True
            for l in surface.links:
                for src, dst in ((l['source'], l['target']), (l['target'], l['source'])):
                    if src in reached and dst not in reached: reached.add(dst); grew = True
        for n in sorted(reached):
            node = surface.nodes[n]
            if node.get('knot'): flags.add('knot:' + node['knot'])
        if 'MR07_N10' in reached: flags.add('mra:reveal-complete')
        for relic, node in shrine_nodes.items():
            s = SHORT[relic]
            if node in reached and s not in ab and s != exclude:
                ab.add(s); log.append(dict(relic=relic, node=node, owned=sorted(LONG[x] for x in ab)))
        entries = [(d['room'], 'door:ARRIVAL') for d in doors
                   if d['node'] in reached and all(predicate_holds(p, ab, flags) for p in d['requires'])]
        under = rooms.explore(entries, ab, mode, flags)
        if (len(ab), len(flags), len(reached)) == before: break
    return dict(ab=ab, flags=flags, reached=reached, log=log, under=under)


def main():
    atlas, world, route = load(SOURCES['atlas']), load(SOURCES['world']), load(SOURCES['route'])
    roomdata, plan = load(SOURCES['rooms'])['rooms'], load(SOURCES['platforming'])['rooms']
    tuning = read_tuning(path(SOURCES['tuningCode']), path(SOURCES['tuningAsset']))
    sim = Reach(tuning)
    guids, relics = script_guids(), relic_guids()
    scenes = {s: read_scene('MRAtlas_' + s, guids, relics) for s in SCENES}
    underground = read_scene('Qolossal_Underground', guids, relics)
    surface = Surface(world, route, sim)
    rooms = Rooms(roomdata, plan, sim)

    # Shrines: where the scene really has them, tied to the nearest route node.
    shrine_nodes, relic_rows, live_shrines = {}, [], {}
    for s, data in scenes.items():
        for sh in data['shrines']:
            live_shrines[sh['relic']] = dict(scene=s, name=sh['name'], at=sh['at'], active=sh['active'])
    for order, (rid, short, name, gives) in enumerate(RELICS, 1):
        sh = live_shrines.get(rid)
        node = next((n for n, d in surface.nodes.items() if d.get('ability') == rid), None)
        if sh and node is None:   # the Leapseed has no node of its own: use the last node at or before the shrine
            region = sh['scene'][:4]
            cands = [d for d in surface.nodes.values() if d['region'] == region and d['position']['x'] <= sh['at'][0]]
            node = max(cands, key=lambda d: d['position']['x'])['id']
        shrine_nodes[rid] = node
        relic_rows.append(dict(order=order, id=rid, short=short, name=name, gives=gives,
                               node=node, nodeName=surface.nodes[node]['name'], region=surface.nodes[node]['region'],
                               sceneObject=sh['name'] if sh else None, sceneAt=sh['at'] if sh else None, activeInScene=bool(sh and sh['active']),
                               inWorldData=any(r['grant'] == rid for r in world['regions'])))

    # Doors: scene first, because that's what the player meets.
    worldchambers = {c['id']: c for c in world['chambers']}
    doors = []
    for c in atlas['caves']:
        region = c['upperScene'].split('_')[1]
        scene = scenes[c['upperScene'].replace('MRAtlas_', '')]
        room = c['lowerRoomId']
        live = next((d for d in scene['doors'] if d['chamber'] == room), None)
        kind = 'MraChamberDoor'
        if live is None:
            live = next((p for p in scene['portals'] if p['target'] == 'room:' + room), None); kind = 'MraUgPortal'
        if live is None:
            live = next((x for x in scene['exits'] if x['target'] == 'room:' + room), None); kind = 'MraExit'
        if live is None: kind = None
        req = c['requirements']
        atlas_req = [] if req.startswith('No ') else [x.strip() for x in req.split(',')]
        doors.append(dict(id=c['connectionId'], room=room, roomName=c['lowerRoomName'].replace('\\u2019', "'"), region=region, node=c['upperAreaId'],
                          enforcedBy=kind, requires=live['requires'] if live else atlas_req, atlasRequires=atlas_req,
                          entrance=c['entrancePosition'],
                          worldPrerequisite=(worldchambers.get(room, {}).get('prerequisite') or None),
                          hasArrival=any(m['kind'] == 'Arrival' for m in roomdata[room]['markers']) if room in roomdata else False))

    # When the first-pass player owns each relic, in route order; a door's "late" gap in walking distance.
    def spot(node):
        d = surface.nodes[node]
        return (next(i for i, r in enumerate(world['regions']) if r['id'] == d['region']), d['index'])
    def walk_units(a, b):
        ra, rb = surface.nodes[a]['region'], surface.nodes[b]['region']
        ids = [r['id'] for r in world['regions']]
        ia, ib = ids.index(ra), ids.index(rb)
        if ia > ib: a, b, ra, rb, ia, ib = b, a, rb, ra, ib, ia
        x = lambda n: surface.nodes[n]['position']['x']
        if ia == ib: return route_length(surface.route[ra]['pieces'], x(a), x(b))
        total = route_length(surface.route[ra]['pieces'], x(a), surface.nodes[world['regions'][ia]['exit']]['position']['x'])
        for k in range(ia + 1, ib):
            r = world['regions'][k]
            total += route_length(surface.route[r['id']]['pieces'], surface.nodes[r['entry']]['position']['x'], surface.nodes[r['exit']]['position']['x'])
        total += route_length(surface.route[rb]['pieces'], surface.nodes[world['regions'][ib]['entry']]['position']['x'], x(b))
        return total
    for d in doors:
        owned = [rid for rid, n in shrine_nodes.items() if spot(n) <= spot(d['node'])]
        d['ownedOnFirstPass'] = owned
        late = [p[6:] for p in d['requires'] if p.startswith('relic:') and p[6:] not in owned]
        d['lateRelics'] = late
        if late:
            farthest = max(late, key=lambda rid: spot(shrine_nodes[rid]))
            d['walkBackUnits'] = round(walk_units(shrine_nodes[farthest], d['node']))
            d['walkBackSeconds'] = round(d['walkBackUnits'] / tuning['runSpeed'])
            way = next(r['primaryWaymark'] for r in world['regions'] if r['id'] == d['region'])
            d['fromWaymarkNode'] = way
            d['fromWaymarkSeconds'] = round(walk_units(way, d['node']) / tuning['runSpeed'])

    # The whole world, with everything, then without each relic in turn, in both envelopes.
    walks = {}
    for mode in ('safe', 'best'):
        walks[mode, None] = world_walk(surface, doors, rooms, shrine_nodes, None, mode)
        for short in ABILITIES:
            walks[mode, short] = world_walk(surface, doors, rooms, shrine_nodes, short, mode)
    full_safe, full_best = walks['safe', None], walks['best', None]

    # Main route, by region and edge: what blocks each edge going forward with only what's owned then.
    edges_out = []
    for e in surface.edges:
        needs = {}
        blockers = {}
        for short in ABILITIES:
            ab = ALL - {short}
            flags = {'knot:grip'}
            ok_s, why_s = surface.walk(e, True, ab, flags, 'safe')
            ok_b, why_b = surface.walk(e, True, ab, flags, 'best')
            v = verdict(ok_s, ok_b)
            if v != 'pass': needs[LONG[short]] = v; blockers[LONG[short]] = why_b or why_s
        tallest = max([w['h'] for w in e['walls']] + [0.])
        edges_out.append(dict(id=e['id'], region=e['region'], source=e['source'], target=e['target'], kind=e['kind'], module=e['module'],
                              atlasRequires=next((x.get('requires') or [] for x in atlas['edges'] if x['id'] == e['id']), []),
                              worldRequires=e['requires'], tallestWall=round(tallest, 2), needs=needs, why=blockers))
    main_route = {}
    goal = world['regions'][6]['exit']
    for short in ABILITIES:
        s, b = walks['safe', short], walks['best', short]
        first_block = None
        if goal not in b['reached']:
            for e in surface.edges:
                if e['source'] in b['reached'] and e['target'] not in b['reached']:
                    why = surface.walk(e, True, frozenset(b['ab']), b['flags'], 'best')[1]
                    first_block = dict(edge=e['id'], why=why); break
        main_route[LONG[short]] = dict(verdict=verdict(goal in s['reached'], goal in b['reached']), firstBlock=first_block)

    # Rooms: what each one uses, and what getting to its reward (or through it) really takes.
    groups = {}
    for rid, r in roomdata.items():
        groups.setdefault(r['group'], []).append(rid)
    rooms_out = []
    full_flags = full_best['under']['flags'] | {'mra:reveal-complete'}
    arrivals = [(d['room'], 'door:ARRIVAL') for d in doors if d['hasArrival']]
    combos = {}
    for size in range(len(ABILITIES) + 1):
        for subset in itertools.combinations(ABILITIES, size):
            for mode in ('safe', 'best'):
                res = rooms.explore(arrivals, subset, mode, {'knot:grip', 'mra:reveal-complete'})
                combos[mode, subset] = dict(entered=set(res['entered']), reward={k for k, v in res['reached'].items() if 'door:REWARD' in v})
    door_by_room = {d['room']: d for d in doors}
    entered_safe, entered_best = full_safe['under']['entered'], full_best['under']['entered']
    for rid, r in sorted(roomdata.items()):
        spec = plan.get(rid) or dict(shapes=[], note='')
        uses = {}
        for s in spec['shapes']:
            k = KIND_USES.get(s['kind'])
            if k: uses[k] = uses.get(k, 0) + 1
        row = dict(id=rid, name=r['name'], group=r['group'], kind=r['kind'], declaredSkills=spec.get('skills'), uses=uses, note=spec.get('note', ''),
                   chamberType=worldchambers.get(rid, {}).get('type'), worldPrerequisite=worldchambers.get(rid, {}).get('prerequisite') or None,
                   surfaceDoor=door_by_room.get(rid, {}).get('id'), reachedSafe=rid in entered_safe, reachedBest=rid in entered_best,
                   hasReward=any(s['kind'] == 'reward' for s in spec['shapes']) or any(m['kind'] == 'Reward' for m in r['markers']))
        # What it takes with the doors' relic checks ignored, from every real arrival: the room's own truth.
        row['enter'] = minimal_sets(combos, rid, 'entered')
        row['reward'] = minimal_sets(combos, rid, 'reward') if row['hasReward'] else None
        row['needs'] = needs_from(row['reward'] or row['enter'])
        rooms_out.append(row)
    room_row = {r['id']: r for r in rooms_out}

    # Each door, both ways. Lost without the relic: can the room still be reached some other way?
    for d in doors:
        relic_reqs = [p[6:] for p in d['requires'] if p.startswith('relic:')]
        proof = {}
        for rid in relic_reqs:
            w = walks['best', SHORT[rid]]
            proof[rid] = dict(refusedWithout=bool(d['enforcedBy']), roomStillReachableWithout=d['room'] in w['under']['entered'])
        d['proof'] = proof
        d['roomNeeds'] = room_row.get(d['room'], {}).get('needs', {})
        d['opensOnFirstPass'] = not d['lateRelics']
        owned = {SHORT[x] for x in d['ownedOnFirstPass']} | {SHORT[x] for x in relic_reqs}
        d['ownedWhenOpen'] = sorted(LONG[x] for x in owned)
        if d['hasArrival']:
            d['networkWhenOpen'] = sorted(rooms.explore([(d['room'], 'door:ARRIVAL')], owned, 'safe', full_flags)['entered'])
            d['networkEverything'] = sorted(rooms.explore([(d['room'], 'door:ARRIVAL')], ALL, 'best', full_flags)['entered'])
        else:
            d['networkWhenOpen'] = d['networkEverything'] = []

    # Each room with a reward: what the player owns the first time they can get in, and whether that's enough for the reward.
    def sets_cover(sets, owned): return any(set(x) <= owned for x in sets)
    for d in sorted(doors, key=lambda d: (spot(d['node']), len(d['ownedWhenOpen']))):
        owned = set(d['ownedWhenOpen'])
        for rid in d['networkEverything']:
            row = room_row[rid]
            if 'firstOwned' not in row or len(owned) < len(row['firstOwned']):
                row['firstOwned'] = sorted(owned); row['firstDoor'] = d['id']
    for row in rooms_out:
        if row.get('firstOwned') is not None and row['reward']:
            owned = set(row['firstOwned'])
            row['rewardWithFirstOwned'] = verdict(sets_cover(row['reward']['safe'], owned), sets_cover(row['reward']['best'], owned))

    # Networks: which door lets the player into which rooms, and the atlas's passages against what can be walked.
    passages = []
    for e in atlas['edges']:
        if e['kind'] != 'passage': continue
        a, b = e['from'], e['to']
        ra, rb = roomdata.get(a), roomdata.get(b)
        doors_ok = bool(ra and rb and any(x['to'] == b for x in ra['doors']) and any(x['to'] == a for x in rb['doors']))
        passages.append(dict(id=e['id'], a=a, b=b, doorsExist=doors_ok,
                             crossesSector=a == 'HEART' or b == 'HEART', fromA=doors_ok and a in full_best['under']['entered'] and ('door:' + b) in full_best['under']['reached'].get(a, set()),
                             fromB=doors_ok and b in full_best['under']['entered'] and ('door:' + a) in full_best['under']['reached'].get(b, set())))
    heart_tags = sorted(full_best['under']['reached'].get('HEART', set()))
    heart_from = {}
    for sector in ('A08', 'C08', 'D08', 'E08'):
        got = rooms.explore([('HEART', 'door:' + sector)], ALL, 'best', full_flags, blocked_rooms=())
        heart_from[sector] = sorted(t[5:] for t in got['reached'].get('HEART', set()))

    # What the code has for moving between regions: any way but walking?
    travel_callers = []
    for root in ('Assets/Scripts', 'Assets/MountainReliefAtlas/Scripts'):
        for dp, _, fs in os.walk(path(root)):
            if 'Testing' in dp: continue
            for f in fs:
                if f.endswith('.cs') and 'AreaTransition.Travel(' in open(os.path.join(dp, f), encoding='utf-8', errors='replace').read():
                    travel_callers.append(f[:-3])
    chart = open(path('Assets/MountainReliefAtlas/Scripts/MraChart.cs'), encoding='utf-8').read()
    waymark_levels = {}
    for s_name in SCENES:
        text = open(path('Assets/Scenes/Game/MRAtlas_%s.unity' % s_name), encoding='utf-8', errors='replace').read()
        waymark_levels[s_name[:4]] = 'mra:' + s_name[:4] in re.findall(r'levelId: (\S+)', text)
    world_sha = sha(SOURCES['world'])
    gorge = surface.gorge
    gorge_tests = []
    for label, ab in (('single jump, sprint', ()), ('double jump, sprint', ('dj',)), ('double jump, sprint, air dash', ('dj', 'dash')), ('with the Glidecap', ('dj', 'dash', 'glide'))):
        gorge_tests.append(dict(moves=label, best=sim.reach('best', gorge['width'], gorge['drop'], ab), safe=sim.reach('safe', gorge['width'], gorge['drop'], ab)))
    run_only = sim.reach('safe', gorge['width'], gorge['drop'], ('dj', 'dash'))
    gorge['bestCarryDoubleJump'] = round(sim._dx(('best', True, False), gorge['drop']), 2)
    gorge['bestCarrySingleJump'] = round(sim._dx(('best', False, False), gorge['drop']), 2)
    gorge['runningCarryDoubleJump'] = round(sim._dx(('safe', True), gorge['drop']), 2)

    out = dict(
        schema='qolossal.ability-access/1',
        generatedBy='Tools/Design/AbilityAccess/build_access.py',
        sources={k: dict(path=v, sha256=sha(v)) for k, v in SOURCES.items()},
        routeMatchesWorld=route.get('worldSha256') == world_sha,
        tuning=tuning,
        reachBounds=dict(safe={k: round(sim.peak('safe', v), 2) for k, v in (('single', ()), ('double', ('dj',)))},
                         best={k: round(sim.peak('best', v), 2) for k, v in (('single', ()), ('double', ('dj',)))},
                         bestWithGrab=dict(single=round(sim.peak('best', ()) + 1.42, 2), double=round(sim.peak('best', ('dj',)) + 1.42, 2)),
                         flatGap=dict(safeRunSingle=round(sim._dx(('safe', False), 0.), 2), safeRunDouble=round(sim._dx(('safe', True), 0.), 2),
                                      bestSprintSingle=round(sim._dx(('best', False, False), 0.), 2), bestSprintDouble=round(sim._dx(('best', True, False), 0.), 2),
                                      bestSprintDoubleDash=round(sim._dx(('best', True, True), 0.), 2))),
        relics=relic_rows,
        firstPassOrder=walks['safe', None]['log'],
        mainRouteNeeds=main_route,
        surfaceEdges=edges_out,
        glideGorge=dict(gorge, tests=gorge_tests, safeBreak=run_only),
        doors=doors,
        rooms=rooms_out,
        passages=passages,
        heartFromEachDoor=heart_from,
        heartTags=heart_tags,
        world=dict(finished=dict(safe=world['regions'][6]['exit'] in full_safe['reached'], best=world['regions'][6]['exit'] in full_best['reached']),
                   heartWakePoints=dict(safe=sorted(t[5:] for t in full_safe['under']['reached'].get('HEART', set()) if 'QUARTER' in t),
                                        best=sorted(t[5:] for t in full_best['under']['reached'].get('HEART', set()) if 'QUARTER' in t)),
                   withoutRelic={LONG[s]: dict(
                       roomsLostSafe=sorted(set(entered_safe) - set(walks['safe', s]['under']['entered'])),
                       roomsLostBest=sorted(set(entered_best) - set(walks['best', s]['under']['entered']))) for s in ABILITIES}),
        travel=dict(waymarkTravelInCode='public bool TryTravel()' in chart, regionWaymarkCharts=waymark_levels,
                    primaryWaymarks={r['id']: r['primaryWaymark'] for r in world['regions']},
                    callersOfAreaTransitionTravel=sorted(set(travel_callers)), legacyPortalsInScenes=sum(s['portalCount'] for s in scenes.values())),
        undergroundArrivalsMissingFromScene=sorted(r for r, x in roomdata.items() if any(m['kind'] == 'Arrival' for m in x['markers']) and ('Arrival room:' + r) not in underground['names']),
        undergroundSceneArrivalsMissingFromRooms=sorted(r for r in roomdata if ('Arrival room:' + r) in underground['names'] and not any(m['kind'] == 'Arrival' for m in roomdata[r]['markers'])),
        limits=['Room checks ignore ceilings and walls between surfaces, enemies, gust push and moving-platform timing; the surface check reads the route data, not scene colliders.',
                'A "blocked" result means no move in the best-case model gets there; a real-engine check in Unity stage still has to confirm it.',
                'Scripted checks prove logic, never feel.'],
    )
    with open(os.path.join(HERE, 'ABILITY_ACCESS.json'), 'w', encoding='utf-8', newline='\n') as f:
        json.dump(out, f, indent=1, sort_keys=False, default=lambda x: sorted(x) if isinstance(x, (set, frozenset)) else str(x))
        f.write('\n')
    print('wrote ABILITY_ACCESS.json:', len(doors), 'doors,', len(rooms_out), 'rooms,', len(edges_out), 'surface edges')


if __name__ == '__main__':
    main()
