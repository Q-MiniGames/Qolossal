# The ability access checker. Rebuilds nothing: it reads ABILITY_ACCESS.json (run build_access.py first),
# applies the rules below, writes ACCESS_REPORT.md and exits 1 while any ERROR stands.
#   ERROR  the game is wrong or the data lies about it; blocks WS3's exit check.
#   WARN   a design problem, or a question with a decision attached (D3, D4 in the master plan).
#   INFO   worth knowing; needs no action.
# usage: python check_access.py
import hashlib, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
data = json.load(open(os.path.join(HERE, 'ABILITY_ACCESS.json'), encoding='utf-8'))
RELIC = {r['id']: r for r in data['relics']}
ROOMS = {r['id']: r for r in data['rooms']}
DOORS = data['doors']
THEMES = {'thread-trial': 'living-thread', 'wall-trial': 'climbing-moss', 'glide-trial': 'glidecap', 'pogo-trial': 'bloomfall', 'dash-trial': 'wind-leaf'}
LESSON_ROOMS = ('MR01_C02', 'MR02_C03', 'MR03_C02')   # the master plan's lesson caves: Moss Pocket, Echo Vault, Grain Store
issues = []


def name(rid): return RELIC[rid]['name'] if rid in RELIC else rid
def names(ids): return ', '.join(name(i) for i in ids) or 'nothing'
def add(level, rule, text, where=''): issues.append(dict(level=level, rule=rule, where=where, text=text))
def predicate(p): return name(p[6:]) if p.startswith('relic:') else 'the Summit reveal' if 'reveal' in p else p


# ---------------------------------------------------------------- is the data current?
stale = [k for k, v in data['sources'].items() if hashlib.sha256(open(os.path.join(ROOT, v['path'].replace('/', os.sep)), 'rb').read()).hexdigest() != v['sha256']]
if stale: add('ERROR', 'STALE_DATA', 'ABILITY_ACCESS.json is older than ' + ', '.join(stale) + '. Run build_access.py.')
if not data['routeMatchesWorld']:
    add('WARN', 'ROUTE_OLDER_THAN_WORLD', 'mra_route.json was generated from an older mra_world.json. Surface walls are read from the route, so the Unity stage should confirm them against the scene colliders.')
for r in data['undergroundSceneArrivalsMissingFromRooms']:
    add('INFO', 'ROOMS_JSON_BEHIND_SCENE', 'The scene has an arrival for %s that rooms.json lacks. Re-run extract_rooms.py after the art thread closes, then rebuild this data.' % r, r)
for r in data['undergroundArrivalsMissingFromScene']:
    add('ERROR', 'ROOMS_JSON_AHEAD_OF_SCENE', 'rooms.json has an arrival for %s that the scene lacks.' % r, r)

# ---------------------------------------------------------------- can the game be played?
w = data['world']
if not w['finished']['best']: add('ERROR', 'CANT_FINISH', 'No move set reaches the end of the Summit.')
elif not w['finished']['safe']: add('WARN', 'CANT_FINISH_SAFELY', 'The Summit is reached only by a best-case run, never within the design margins.')
for sector in 'ACDE':
    if 'QUARTER_' + sector not in w['heartWakePoints']['safe']:
        add('ERROR', 'HEART_WAKE_MISSING', 'The Heart wake point for Route %s is not reachable within the design margins.' % sector, 'HEART')
for r in data['relics']:
    if not r['activeInScene']: add('ERROR', 'RELIC_NOT_IN_SCENE', 'No active shrine for the %s in any region scene.' % r['name'], r['node'])
    if not r['inWorldData']: add('WARN', 'RELIC_NOT_IN_WORLD_DATA', 'The %s has a shrine in %s but no grant in mra_world.json, so the design data never mentions it.' % (r['name'], r['region']), r['node'])
for door in DOORS:
    if not door['hasArrival']:
        add('ERROR', 'NO_WAY_IN', '%s: the surface door leads to room:%s, but that room has no arrival or doors in rooms.json, so Qori would land nowhere.' % (door['roomName'], door['room']), door['id'])
    if door['enforcedBy'] is None:
        add('ERROR', 'DOOR_NOT_IN_SCENE', 'The atlas lists door %s, but %s has no door to %s.' % (door['id'], door['region'], door['room']), door['id'])
for r in data['rooms']:
    if not r['reachedBest'] and r['id'] not in [d['room'] for d in DOORS if not d['hasArrival']]:
        add('ERROR', 'ROOM_UNREACHABLE', '%s %s cannot be reached by any move set.' % (r['id'], r['name']), r['id'])
    elif r['reachedBest'] and r['hasReward'] and not r['reward']['best']:
        add('ERROR', 'REWARD_UNREACHABLE', '%s %s: its reward cannot be reached by any move set.' % (r['id'], r['name']), r['id'])
    elif r['reachedBest'] and r['hasReward'] and not r['reward']['safe']:
        add('WARN', 'REWARD_ONLY_BEST_CASE', '%s %s: its reward is reached only by a best-case run, never within the design margins.' % (r['id'], r['name']), r['id'])

# ---------------------------------------------------------------- does the data tell the truth about each door?
for d in DOORS:
    if d['enforcedBy'] and sorted(d['requires']) != sorted(d['atlasRequires']):
        add('ERROR', 'DOOR_DISAGREES', '%s: the scene asks for [%s], the atlas says [%s].' % (d['id'], ', '.join(d['requires']), ', '.join(d['atlasRequires'])), d['id'])
    for rid, proof in d['proof'].items():
        if proof['roomStillReachableWithout']:
            add('ERROR', 'DOOR_BYPASSED', '%s asks for the %s, but %s can still be reached without it another way.' % (d['id'], name(rid), d['room']), d['id'])
conflicts = []
for r in data['rooms']:
    pre = r['worldPrerequisite']
    if not pre: continue
    d = next((x for x in DOORS if x['room'] == r['id']), None)
    live = None
    if d: live = [p[6:] for p in d['requires'] if p.startswith('relic:')]
    if d and pre in live: continue
    door = r.get('firstDoor')
    gate = next((x for x in DOORS if x['id'] == door), None)
    conflicts.append(dict(room=r['id'], name=r['name'], old=pre, door=door, doorAsks=[p[6:] for p in gate['requires'] if p.startswith('relic:')] if gate else []))
for c in conflicts:
    add('WARN', 'OLD_PREREQUISITE', '%s %s: mra_world.json says %s, which no door enforces. The way in is %s, which asks for %s.' % (c['room'], c['name'], name(c['old']), c['door'], names(c['doorAsks'])), c['room'])
edges_with_requires = [e for e in data['surfaceEdges'] if e['atlasRequires'] or e['worldRequires']]
for e in edges_with_requires:
    relics = [p[6:] for p in e['atlasRequires'] + e['worldRequires'] if p.startswith('relic:')]
    if relics and not e['module']:
        add('ERROR', 'GATE_IN_THE_WRONG_PLACE', '%s: the atlas and mra_world.json say this edge needs the %s, but it has no gate there. The real module is on another edge (the route and the scene both say MR06_E10).' % (e['id'], name(relics[0])), e['id'])
stale_modules = [e for e in data['surfaceEdges'] if e['module'] == 'glide-drop' and not e['atlasRequires']]
for e in stale_modules:
    add('WARN', 'GATE_NOT_IN_ATLAS', '%s holds the glide gorge, but the atlas lists no requirement on it, so the Chart can\'t show it.' % e['id'], e['id'])

# ---------------------------------------------------------------- do the gates hold?
g = data['glideGorge']
broken = [t for t in g['tests'] if t['best'] and 'Glidecap' not in t['moves']]
if broken:
    add('WARN', 'GATE_BREAKABLE', 'The Heights glide gorge (%.0f u wide, %.0f u drop) has no lock but the gap, and a sprinting double jump carries %.1f u without the Glidecap. A running one carries %.1f u, and MraTestDriver\'s no-Glidecap try is a jump and an air dash, with no double jump. Test it in the engine, then widen the gorge or accept the skip.'
        % (g['width'], -g['drop'], g['bestCarryDoubleJump'], g['runningCarryDoubleJump']), 'MR06_E10')
for rid, m in data['mainRouteNeeds'].items():
    r = RELIC[rid]
    if m['verdict'] == 'pass':
        add('WARN', 'NEVER_REQUIRED', 'The %s is never needed on the main route, so a player can walk past it (D4).' % r['name'], r['node'])
    elif m['verdict'] == 'grey':
        add('WARN', 'NOT_PROVEN_REQUIRED', 'The %s: the main route can be finished without it in a best-case run, but not within the design margins.' % r['name'], r['node'])
    else:
        add('INFO', 'REQUIRED_ON_ROUTE', 'The %s is required on the main route, first at %s (%s).' % (r['name'], m['firstBlock']['edge'], m['firstBlock']['why']), r['node'])

# ---------------------------------------------------------------- lock or lesson?
late = [d for d in DOORS if d['lateRelics']]
for d in late:
    rooms = d['networkEverything']
    add('WARN', 'LATE_DOOR', '%s in %s needs the %s, found later. It opens %d room%s on a return trip: %d s on foot from the shrine, or %d s from the region\'s Waymark (D3).'
        % (d['id'], d['region'], names(d['lateRelics']), len(rooms), '' if len(rooms) == 1 else 's', d['walkBackSeconds'], d['fromWaymarkSeconds']), d['id'])
lessons = []
for d in late:
    for rid in d['networkEverything']:
        r = ROOMS[rid]
        note = r['note'].lower()
        theme = THEMES.get(r['chamberType'] or '')
        early_skill = theme and theme not in d['lateRelics'] and theme in d['ownedOnFirstPass']
        if 'teach' in note or 'lesson' in note or early_skill or rid in LESSON_ROOMS:
            lessons.append(dict(room=rid, name=r['name'], note=r['note'], door=d['id'], region=d['region'], lateRelics=d['lateRelics'], owned=d['ownedOnFirstPass']))
            add('WARN', 'LESSON_BEHIND_LATE_DOOR', '%s %s (%s) sits behind %s, so a player can\'t meet it until they own the %s.' % (rid, r['name'], d['region'], d['id'], names(d['lateRelics'])), rid)
lock_only = []
for d in DOORS:
    for rid in d['proof']:
        needed = any(rid in ROOMS[x]['needs'] for x in d['networkEverything'])
        safe_free = all(any(rid not in s for s in (ROOMS[x]['reward'] or ROOMS[x]['enter'])['safe']) for x in d['networkEverything'] if (ROOMS[x]['reward'] or ROOMS[x]['enter'])['safe'])
        if not needed and safe_free:
            lock_only.append(dict(door=d['id'], relic=rid, room=d['room']))
for row in lock_only:
    add('WARN', 'LOCK_NOT_LESSON', '%s asks for the %s, but what lies behind it can be done without it, within the design margins. The relic is a key, not a skill test.' % (row['door'], name(row['relic'])), row['door'])
for r in data['rooms']:
    theme = THEMES.get(r['chamberType'] or '')
    sets = (r['reward'] or r['enter'])['safe']
    if theme and sets and any(theme not in s for s in sets):
        add('WARN', 'TRIAL_WITHOUT_ITS_RELIC', '%s %s is a %s in mra_world.json, but the current layout can be finished without the %s.' % (r['id'], r['name'], r['chamberType'], name(theme)), r['id'])
    if r.get('rewardWithFirstOwned') == 'blocked':
        add('INFO', 'REWARD_NEEDS_RETURN', '%s %s: the reward needs a relic the player lacks on first entry, so it is a return trip.' % (r['id'], r['name']), r['id'])
    elif r.get('rewardWithFirstOwned') == 'grey':
        add('INFO', 'REWARD_TIGHT_ON_FIRST_ENTRY', '%s %s: with what\'s owned on first entry, the reward takes a best-case run.' % (r['id'], r['name']), r['id'])
route_use = {}
for r in data['rooms']:
    if r['group'] in 'ACDE':
        u = route_use.setdefault(r['group'], dict(rooms=0, anchors=0, pods=0, gusts=0, needs={}))
        u['rooms'] += 1; u['anchors'] += r['uses'].get('thread', 0); u['pods'] += r['uses'].get('pod', 0); u['gusts'] += r['uses'].get('gust', 0)
        for k, v in r['needs'].items(): u['needs'].setdefault(k, set()).add(r['id'])
a = route_use['A']
if not a['anchors'] and not a['pods']:
    add('WARN', 'ROUTE_IGNORES_RELICS', 'Route A has no Thread anchors or pods, and needs nothing but the double jump, for a player who already owns four relics when it opens.', 'A')
for route, door_id in (('C', 'CAVE_C01'), ('D', 'CAVE_D01')):
    d = next(x for x in DOORS if x['id'] == door_id)
    for rid in d['proof']:
        if not route_use[route]['needs'].get(rid):
            add('WARN', 'ROUTE_DOOR_NOT_TESTED_INSIDE', 'Route %s\'s door asks for the %s, but no room inside proves it within the design margins.' % (route, name(rid)), route)

# ---------------------------------------------------------------- the map against what can be walked
leaks = {k: [t for t in v if not t.startswith('QUARTER') and t != k and t != 'ARRIVAL'] for k, v in data['heartFromEachDoor'].items()}
if any(leaks.values()):
    add('ERROR', 'HEART_LEAK', 'A sector of the Heart leads into another: %s.' % leaks)
else:
    add('WARN', 'MAP_SHOWS_SEALED_LINKS', 'The atlas joins A, C, D and E through HEART, but its grilles seal each sector, so none reaches another. The Chart and minimap must not draw that as a route.', 'HEART')
for p in data['passages']:
    if not p['doorsExist']: add('ERROR', 'PASSAGE_WITHOUT_DOORS', 'The atlas passage %s has no matching door in rooms.json.' % p['id'], p['id'])

# ---------------------------------------------------------------- getting back
t = data['travel']
if t['waymarkTravelInCode'] and all(t['regionWaymarkCharts'].values()):
    add('INFO', 'WAYMARK_TRAVEL_EXISTS', 'Waymark travel is already in the game (MraChart.TryTravel, tested in MraTestDriver): stand at a lit Waymark, open the Chart, pick another charted region. All seven regions chart at their Waymark. The master plan\'s "no fast travel" is wrong.')
else:
    add('ERROR', 'NO_WAY_BACK', 'Waymark travel or a region\'s Waymark is missing: %s.' % t['regionWaymarkCharts'])


# ---------------------------------------------------------------- the report
ORDER = dict(ERROR=0, WARN=1, INFO=2)
issues.sort(key=lambda i: (ORDER[i['level']], i['rule'], i['where']))
counts = {k: sum(1 for i in issues if i['level'] == k) for k in ORDER}
lines = ['# Ability access report', '',
         'Generated by `check_access.py` from `ABILITY_ACCESS.json`. Do not edit by hand.', '',
         '**%d errors, %d warnings, %d notes.**' % (counts['ERROR'], counts['WARN'], counts['INFO']), '',
         'Reach bounds, in units. Jump height: design margin %.2f single, %.2f double; best case %.2f / %.2f, or %.2f / %.2f with a ledge grab. Flat gap: running %.1f / %.1f, sprinting %.1f / %.1f, plus an air dash %.1f.'
         % (data['reachBounds']['safe']['single'], data['reachBounds']['safe']['double'], data['reachBounds']['best']['single'], data['reachBounds']['best']['double'],
            data['reachBounds']['bestWithGrab']['single'], data['reachBounds']['bestWithGrab']['double'],
            data['reachBounds']['flatGap']['safeRunSingle'], data['reachBounds']['flatGap']['safeRunDouble'], data['reachBounds']['flatGap']['bestSprintSingle'],
            data['reachBounds']['flatGap']['bestSprintDouble'], data['reachBounds']['flatGap']['bestSprintDoubleDash']), '',
         '**Verdicts.** *pass*: proven possible within the design margins. *blocked*: no move in the best-case model gets there. *grey*: in between, needs hands on it.', '',
         '**Limits.** ' + ' '.join(data['limits']), '']

lines += ['## Relics in the order a first-time player finds them', '', '| # | Relic | Gives | Found | Main route |', '|---|---|---|---|---|']
for r in data['relics']:
    m = data['mainRouteNeeds'][r['id']]
    route = {'pass': 'never needed', 'grey': 'not proven'}.get(m['verdict']) or 'required from %s (%s)' % (m['firstBlock']['edge'], m['firstBlock']['why'])
    lines.append('| %d | %s | %s | %s %s | %s |' % (r['order'], r['name'], r['gives'], r['region'], r['nodeName'], route))

lines += ['', '## Doors', '', '| Door | Region | Leads to | Scene asks | Enforced by | Rooms behind it | Opens | Inside needs |', '|---|---|---|---|---|---|---|---|']
for d in DOORS:
    opens = 'first pass' if d['opensOnFirstPass'] else 'return trip, %d s from Waymark' % d['fromWaymarkSeconds']
    inside = ', '.join('%s (%s)' % (name(k), v) for k, v in d['roomNeeds'].items()) or 'nothing proven'
    lines.append('| %s | %s | %s | %s | %s | %d | %s | %s |' % (d['id'], d['region'], d['roomName'], ', '.join(predicate(p) for p in d['requires']) or 'nothing', d['enforcedBy'] or 'nothing', len(d['networkEverything']), opens, inside))

lines += ['', '## Main route walls and gates', '', 'Edges where a relic matters. Everything else on the route is free.', '', '| Edge | Tallest wall | Module | Without it |', '|---|---|---|---|']
for e in data['surfaceEdges']:
    if e['needs']:
        lines.append('| %s | %.1f | %s | %s |' % (e['id'], e['tallestWall'], e['module'] or '', ', '.join('%s: %s' % (name(k), v) for k, v in e['needs'].items())))

lines += ['', '## Rooms', '', 'Door checks ignored, starting from every real arrival. "Smallest sets" is the fewest relics that get the reward (or get in, if the room has none) within the design margins.', '', '| Room | Name | Uses | Needs | Owned at first entry | Reward then | Smallest sets |', '|---|---|---|---|---|---|---|']
for r in data['rooms']:
    uses = ', '.join('%d %s' % (v, {'thread': 'anchors', 'pod': 'pods', 'gust': 'gusts'}[k]) for k, v in sorted(r['uses'].items())) or ''
    needs = ', '.join('%s (%s)' % (name(k), v) for k, v in r['needs'].items()) or ''
    sets = '; '.join(' + '.join(name(x) for x in s) or 'none' for s in (r['reward'] or r['enter'])['safe'][:3]) or 'not reachable'
    lines.append('| %s | %s | %s | %s | %s | %s | %s |' % (r['id'], r['name'], uses, needs, len(r.get('firstOwned', [])) if r.get('firstOwned') is not None else '-', r.get('rewardWithFirstOwned') or '-', sets))

for level, title in (('ERROR', 'Errors'), ('WARN', 'Warnings'), ('INFO', 'Notes')):
    rows = [i for i in issues if i['level'] == level]
    lines += ['', '## %s (%d)' % (title, len(rows)), '']
    rule = None
    for i in rows:
        if i['rule'] != rule:
            rule = i['rule']; lines += ['', '**%s**' % rule, '']
        lines.append('- ' + i['text'])
with open(os.path.join(HERE, 'ACCESS_REPORT.md'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('\n'.join(lines).replace('\n\n\n', '\n\n') + '\n')

print('%d errors, %d warnings, %d notes. Report: ACCESS_REPORT.md' % (counts['ERROR'], counts['WARN'], counts['INFO']))
summary = {}
for i in issues: summary.setdefault((i['level'], i['rule']), []).append(i['where'])
for (level, rule), where in sorted(summary.items(), key=lambda kv: (ORDER[kv[0][0]], kv[0][1])):
    print('  %-5s %-30s %3d  %s' % (level, rule, len(where), ', '.join(x for x in where if x)[:90]))
sys.exit(1 if counts['ERROR'] else 0)
