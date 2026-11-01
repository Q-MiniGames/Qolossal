# Reach check for the authored layouts: which doors (and the arrival) connect, room by room.
# Jump envelope from a step simulation of PlayerMovement with the live tuning (it reproduces the
# phase 1 measurements: rise 2.39, flat 4.08 walking / 6.13 running), with margins:
# run speed x0.9 and peak heights cut by 0.45 u. Ignores ceilings and walls between surfaces;
# the in-engine pass covers those.
# usage: python validate.py rooms.json platforming_v1.json [room ids]
import json, sys

DT, G, BAND, APEX, FALLM, VMAX = 1 / 60, -9.81 * 3, 1.5, .55, 1.7, 22
VX = 7.5 * .9; MARGIN = .45

def arcs():
    out = []
    for second in [None] + [i * .05 for i in range(0, 40)]:
        y, vy, t, used, pts = 0., 12., 0., False, []
        while y > -60:
            vy += G * DT
            if abs(vy) <= BAND: vy += G * (APEX - 1) * DT
            elif vy < 0: vy += G * (1 + (FALLM - 1) * min(1, -vy / BAND) - 1) * DT
            vy = max(vy, -VMAX); y += vy * DT; t += DT
            if second is not None and not used and t >= second: vy, used = 11., True
            pts.append((t, y))
        out.append(pts)
    return out
ARCS = arcs()
PEAK = max(y for a in ARCS for _, y in a) - MARGIN

def reach(dx, dy, ab=frozenset(), bonus=0.):
    dy -= bonus
    if 'dash' in ab: dx = max(0., dx - 2.5)
    if 'glide' in ab and dy < 0 and dx <= 6 + 3.5 * -dy: return True
    if dy > PEAK: return False
    best = 0.
    for a in ARCS:
        top = max(y for _, y in a)
        if top - MARGIN < dy: continue
        t = max(t for t, y in a if y >= dy)   # last moment at or above dy
        best = max(best, VX * t)
    return dx <= best

def segments(r, spec):
    w, h = r['w'], r['h']; segs = []
    shapes = spec['shapes'] if spec else []
    blocked = [(s['local'][0], s['local'][0] + s['local'][2]) for s in shapes
               if s['kind'] == 'water' or s['kind'] == 'thorns' and s['local'][1] <= .1 and s['local'][2] > 1]
    blocked += [(d['x0'], d['x1']) for d in r['doors'] if d['side'] == 'B']
    xs = sorted({0., w} | {v for b in blocked for v in b})
    for a, b in zip(xs, xs[1:]):
        m = (a + b) / 2
        if 0 <= m <= w and not any(x0 <= m <= x1 for x0, x1 in blocked): segs.append(('floor', a, b, 0.))
    for s in shapes:
        x, y, sw, sh = s['local']
        if s['kind'] in ('solid', 'rock'): segs.append(('top', x, x + sw, y + sh))
        elif s['kind'] in ('deck', 'crumble'): segs.append((s['kind'], x, x + sw, y + sh))
        elif s['kind'] == 'mover':
            k = 'mover#%d' % len(segs)
            segs.append((k, x, x + sw, y + sh))
            segs.append((k, x + s['offset'][0], x + sw + s['offset'][0], y + sh + s['offset'][1]))
        elif s['kind'] == 'pod': segs.append(('pod', x, x + sw, y + sh))
        elif s['kind'] == 'reward': segs.append(('door:REWARD', x + sw / 2 - .3, x + sw / 2 + .3, y + sh / 2 - 1.))
        elif s['kind'] == 'anchor': segs.append(('anchor', x + sw / 2, x + sw / 2, y + sh / 2))
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

def gap(a, b):
    return max(0., b[1] - a[2], a[1] - b[2])

ROUTE_AB = {'A': set(), 'C': {'moss', 'thread', 'pod'}, 'D': {'moss', 'thread', 'pod', 'dash', 'glide'},
            'E': {'moss', 'thread', 'pod', 'dash', 'glide'}}

def edges(segs, r, spec):
    shapes = spec['shapes'] if spec else []
    ab = frozenset(ROUTE_AB.get(r['group'], {'moss', 'thread', 'pod', 'dash', 'glide'}))
    if spec and spec.get('skills') is not None: ab = frozenset(spec['skills'])
    w = r['w']
    gates = [s for s in shapes if s['kind'] == 'gate']
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
            if s['kind'] == 'thorns' or s['kind'] == 'rock': return True
        return False
    n = len(segs); adj = {i: [] for i in range(n)}
    for i in range(n):
        for j in range(n):
            if i == j: continue
            a, b = segs[i], segs[j]
            if a[0].startswith('door') and a[0] == b[0]: continue
            dy = b[3] - a[3]; dx = gap(a, b); ok = False
            if a[0].startswith('mover') and a[0] == b[0]: adj[i].append((j, [])); continue
            if a[0] == 'anchor' or b[0] == 'anchor':
                if 'thread' not in ab: continue
                if b[0] == 'anchor' and a[0] == 'anchor': ok = ((a[1] - b[1]) ** 2 + (a[3] - b[3]) ** 2) ** .5 <= 7.5
                elif b[0] == 'anchor':
                    cx = min(max(b[1], a[1]), a[2]); ok = ((cx - b[1]) ** 2 + (a[3] + .6 - b[3]) ** 2) ** .5 <= 4.0 or reach(dx, dy - .6, ab)
                else: ok = b[3] <= a[3] + 3 and dx <= 6.5
            else:
                if b[0] == 'top' and dy > 0 and dx == 0 and a[1] >= b[1] and a[2] <= b[2]: continue  # under a solid
                bonus = 2. if a[0] == 'pod' else 0.
                ok = reach(dx, dy, ab, bonus)
                if not ok and 'moss' in ab and dy > 0:
                    if b[0].startswith('door') and b[3] >= r['h'] - .1: ok = reach(dx, dy - .6, ab)
                    for side in wall(a) & wall(b):
                        if not cling_blocked(side, a[3], b[3]): ok = True
            if ok:
                gated = [g for g in gates if b[0].startswith('door') and abs(g['local'][1] - b[3]) < .6 and
                         min(abs(g['local'][0] - b[1]), abs(g['local'][0] + g['local'][2] - b[2])) < 2]
                adj[i].append((j, [f for g in gated for f in g['requires']]))
    return adj

def check(r, spec):
    segs = segments(r, spec); adj = edges(segs, r, spec)
    valves = {s['flag']: s['local'] for s in (spec['shapes'] if spec else []) if s['kind'] == 'valve'}
    def valve_seg(flag):
        vx, vy = valves[flag][0] + .6, valves[flag][1]
        return [i for i, s in enumerate(segs) if s[1] - .1 <= vx <= s[2] + .1 and abs(s[3] - vy) < .3]
    starts = [i for i, s in enumerate(segs) if s[0].startswith('door')]
    report = {}
    for st in starts:
        flags, seen = set(), set()
        while True:
            seen, stack = {st}, [st]
            while stack:
                i = stack.pop()
                for j, req in adj[i]:
                    if j not in seen and all(f in flags for f in req): seen.add(j); stack.append(j)
            got = {f for f in valves if any(i in seen for i in valve_seg(f))} - flags
            if not got: break
            flags |= got
        report[segs[st][0]] = sorted({segs[i][0] for i in seen if segs[i][0].startswith('door') and i != st})
    return report

if __name__ == '__main__':
    rooms = json.load(open(sys.argv[1]))['rooms']; plan = json.load(open(sys.argv[2]))['rooms']
    only = sys.argv[3:] or list(plan)
    print('peak %.2f (margin %.2f)' % (PEAK, MARGIN))
    for rid in only:
        r = rooms[rid]; rep = check(r, plan.get(rid))
        doors = sorted({'door:' + d['to'] for d in r['doors']} | {'door:' + t for m in r['markers'] for k, t in (('Arrival', 'ARRIVAL'), ('Reward', 'REWARD')) if m['kind'] == k} | ({'door:REWARD'} if any(s['kind'] == 'reward' for s in plan.get(rid, {}).get('shapes', [])) else set()))
        print(rid, r['name'])
        for k, v in rep.items():
            miss = [d for d in doors if d not in v and d != k]
            print('   from %-12s reaches %s%s' % (k, ', '.join(x[5:] for x in v) or '-', ('   MISSING ' + ', '.join(x[5:] for x in miss)) if miss else ''))

def overlaps(r, plan):
    """Markers buried in solids or standing in hazards, and solids blocking a door opening."""
    out = []
    for s in plan['shapes']:
        x, y, w, h = s['local']
        for m in r['markers']:
            mx, my = m['x'], m['y'] - .4
            inside = x < mx < x + w and y < my + .3 < y + h
            if inside and s['kind'] in ('solid', 'rock', 'gate', 'water', 'thorns'): out.append('%s %s at (%.1f,%.1f) in %s' % (r['id'], m['kind'], m['x'], m['y'], s['kind']))
        if s['kind'] in ('solid', 'rock'):
            for d in r['doors']:
                if d['side'] in 'LR':
                    dx = 0 if d['side'] == 'L' else r['w']
                    if x - .01 <= dx <= x + w + .01 and y < d['sill'] + d['width'] - .1 and y + h > d['sill'] + .05: out.append('%s %s blocks door %s' % (r['id'], s['kind'], d['to']))
                else:
                    dy = 0 if d['side'] == 'B' else r['h']
                    if x < d['x1'] and x + w > d['x0'] and abs((y if dy == 0 else y + h) - dy) < .05: out.append('%s %s covers floor/ceiling door %s' % (r['id'], s['kind'], d['to']))
    return out

if __name__ == '__main__':
    for rid in only:
        for line in overlaps(rooms[rid], plan[rid]): print('OVERLAP', line)
