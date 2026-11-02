# Authored platforming for the connected underground (Qolossal_Underground), room by room.
# Coordinates are room-local (origin at the room's bottom-left, floor y 0); write() converts to world.
# Kit: solid (clingable block), rock (no cling), deck (one-way, jump up through; no drop-through exists),
# crumble, mover, water (respawn), thorns (heart + bounce), spill (timed thorns), safe (respawn point),
# valve (Up saves a flag), gate (opens on flags), anchor (Living Thread), pod (Bloomfall pogo),
# gust (timed push back to the last shelter), shelter.
# usage: python layouts.py rooms.json platforming_v1.json
import json, sys

ROOMS = {}
cur = None

def room(rid, replace=True, note='', skills=None):
    global cur
    cur = ROOMS.setdefault(rid, dict(replace=replace, note=note, shapes=[], skills=skills))

# Skills a room may require, by when the player can first reach it (Leapseed, the double jump, comes at the
# Cradle's first checkpoint; Climbing Moss in the Cradle, the Thread in the Causeway, Bloomfall in the Terraces,
# Wind Leaf in Ribwood, the Glidecap in the Heights). None means every skill.
S0, S1, S2, S3, S4 = ['dj'], ['dj', 'moss'], ['dj', 'moss', 'thread'], ['dj', 'moss', 'thread', 'pod'], ['dj', 'moss', 'thread', 'pod', 'dash']

def add(kind, **k): cur['shapes'].append(dict(kind=kind, **k))
def solid(x0, x1, top, bottom=0., cling=True): add('solid' if cling else 'rock', rect=[x0, bottom, x1 - x0, top - bottom])
def deck(x0, x1, y): add('deck', rect=[x0, y - .3, x1 - x0, .3])
def crumble(x0, x1, y): add('crumble', rect=[x0, y - .3, x1 - x0, .3])
def mover(x0, x1, y, dx, dy, t=3., pause=.6): add('mover', rect=[x0, y - .3, x1 - x0, .3], offset=[dx, dy], seconds=t, pause=pause)
def water(x0, x1, top=.6, bottom=0.): add('water', rect=[x0, bottom, x1 - x0, top - bottom])
def thorns(x0, x1, y0, y1): add('thorns', rect=[x0, y0, x1 - x0, y1 - y0])
def spill(x0, x1, y0, y1, up=1.6, safe=1.2, phase=0.): add('spill', rect=[x0, y0, x1 - x0, y1 - y0], up=up, safe=safe, phase=phase)
def safe(x, y, w=3., h=2.5): add('safe', rect=[x - w / 2, y, w, h], feet=[x, y])
def valve(x, y, flag, label='Turn the wheel', done=''): add('valve', rect=[x - .6, y, 1.2, 2.], flag=flag, label=label, done=done)
def gate(x0, x1, y0, y1, requires): add('gate', rect=[x0, y0, x1 - x0, y1 - y0], requires=requires)
def anchor(x, y): add('anchor', rect=[x - .3, y - .3, .6, .6])
def pod(x, y): add('pod', rect=[x - .5, y - .4, 1., .8])
def gust(x0, x1, y0, y1, phase=0.): add('gust', rect=[x0, y0, x1 - x0, y1 - y0], phase=phase)
def shelter(x0, x1, y0, y1, fx, fy): add('shelter', rect=[x0, y0, x1 - x0, y1 - y0], feet=[fx, fy])

def grille(x0, x1, y0, y1): add('grille', rect=[x0, y0, x1 - x0, y1 - y0])   # see-through bars: block Qori, the thread and attacks
def look(x0, x1, y, cx, cy, half): add('look', rect=[x0, y, x1 - x0, 2.5], feet=[cx, cy], half=half)   # hold Up: camera shows (cx, cy)
# Creatures: stand-ins from Assets/Prefabs until the approved underground designs (Tools/IncomingArt/Ludo/
# EnemyConcepts/Underground_20261031) are built; `design` names the creature each will become. (x, y) is the
# ground (or, for fliers, the perch) under it.
def enemy(prefab, x, y, design, flying=False): add('enemy', rect=[x - .4, y, .8, .8], prefab=prefab, design=design, phase=2.5 if flying else .6)
def boss(prefab, x, y, flag, design): add('boss', rect=[x - .4, y, .8, .8], prefab=prefab, design=design, flag=flag, phase=.6)
def reward(x, y, amber=20): add('reward', rect=[x - .4, y - .4, .8, .8], amber=amber)   # an Amber find; (x, y) is its centre

def zigzag(xa, xb, y0, y1, step=3.5, w=3., kind=deck):
    """Alternating ledges from y0 (exclusive) up to y1 (inclusive), left edge xa / xb."""
    y, i = y0 + step, 0
    while y <= y1 + 1e-6:
        x = xa if i % 2 == 0 else xb
        kind(x, x + w, round(y, 2)); y += step; i += 1

# ---------------------------------------------------------------- Route A: the Cistern (no skills needed)
# Identity: run, jump, double jump; water as the hazard; a valve-and-gate puzzle; pumps that move.

room('A01', note='Teach: single jump, running gap over water, double jump to the door.')
solid(16, 19, 2)            # first step: a single jump
water(19, 24)               # first water: running gap (5 u at the same height)
solid(24, 29, 2)
deck(29, 36, 5)            # double jump up, then again into the door (sill 8)
safe(17.5, 2); safe(26.5, 2)

room('A02', note='Drain Well: shortcut ledges at the top, a one-way zigzag descent to the floor hole into A03.')
deck(0, 6, 52)              # A01 door shelf
deck(6.5, 9.5, 49)          # links the A06 shortcut shelf and the A01 shelf
deck(10, 16, 46)            # A06 shortcut shelf
deck(3, 10, 36.4)           # rest ledge (existing Rest marker)
deck(9, 14, 30); deck(2, 7, 24); crumble(9, 14, 18); deck(2, 7, 12); deck(9, 13, 6)
water(9, 16)                # pool beside the exit hole (4-8)
safe(6.5, 36.4); safe(2, 0, 2.5)

room('A03', note='Silt Walk: dropped in beside a shut sluice gate; run the silt channels to the wheel at the far end.')
solid(40, 44, 1); solid(44, 48, 4)          # landing island and the step to the door (sill 4)
gate(47, 48, 4, 8, ['cw:A:silt_gate'])
water(30, 40); solid(34, 36, 2)              # two 4 u running gaps with a pillar between
water(8, 22); deck(17, 19.5, 2.5); crumble(12, 14.5, 3.5)
valve(4, 0, 'cw:A:silt_gate', 'Turn the sluice wheel', 'Far off, a gate grinds open.')
safe(42, 1); safe(25, 0); safe(4, 0)

room('A04', note='Cistern Court: cross the high line (decks, a moving barge, crumbles) over the basin; a zigzag climbs back.')
deck(0, 8, 22)              # A03 door shelf (sill 22)
solid(55, 64, 22, 19)       # A05 door shelf
deck(11, 14, 22); deck(17, 20, 23.5)
mover(23, 26, 21, 12, 0, 3.2)
deck(41, 44, 22.5); crumble(47, 50, 23)
water(30, 55, 1.)           # the basin; the floor 8-30 stays dry (rest marker 24.3, A09 hole 14.5-17.5)
for x, y in [(9, 3.7), (4, 7.4), (9, 11.1), (4, 14.8), (9, 18.5)]: deck(x, x + 3, y)   # climb back from the floor
safe(4, 22); safe(24, 0); safe(59, 22)

room('A05', note='Spillway Rise: a 70 u climb (room + shaft); timed spill sheets between the columns.')
deck(0, 5, 14)              # A04 door shelf
solid(3, 9, 2.8); deck(11, 14, 6.4); deck(6.5, 9.5, 10.2)   # rest step (existing marker) and the climb back to the shelf
deck(6.5, 9.5, 17.5); deck(11, 14, 21); crumble(6.5, 9.5, 24.5); deck(2, 5, 28)
deck(6.5, 9.5, 31.5); deck(11, 14, 35); deck(6.5, 9.5, 38.5); deck(2, 5, 42); deck(6, 10, 45.5)
spill(9.7, 10.8, 19, 26, phase=0.); spill(5.3, 6.4, 27, 33, phase=1.4); spill(9.7, 10.8, 33, 40, phase=.7)
for i, y in enumerate([49, 52.5, 56, 59.5, 63, 66.5]):   # the 4 u shaft above the room (x 6-10)
    deck(6, 8, y) if i % 2 == 0 else deck(8, 10, y)
safe(2.5, 14); safe(8, 31.5); safe(8, 45.5)

room('A06', note='Pump Gallery: pistons over the flood going left, a climb, then the high line back right; a wheel opens the A02 shortcut.')
deck(54, 60, 6)            # arrival shelf (door sill 6)
water(30, 54)
mover(47, 50, 4, 0, 6, 2.6); mover(40, 43, 10, 0, -5, 2.6, .8); mover(33, 36, 4, 0, 6, 2.2)
zigzag(10, 6, 0, 14.4, step=3.6)               # floor to the left shelf
deck(0, 5, 18)             # A02 shortcut shelf (sill 18)
gate(0, 1, 18, 22, ['cw:A:pump_shortcut'])
valve(3, 18, 'cw:A:pump_shortcut', 'Open the shortcut', 'The way back to the Drain Well is open.')
deck(9, 12, 20); crumble(16, 19, 21); deck(23, 26, 22)
mover(29, 32, 22, 10, 0, 3.)
deck(45, 48, 23); crumble(51, 54, 22.5)
solid(56, 60, 22, 20)       # A07 door shelf (sill 22)
safe(57, 6); safe(20, 0); safe(3, 18)

room('A07', note='Keeper Refuge: rest. Steps down from both doors, flat floor.')
deck(0, 5, 6); solid(5, 8, 3); deck(37, 42, 6); solid(34, 37, 3)

room('A08', note='Sluice Guardian Arena: flat floor, two fight ledges, a zigzag to the Heart door; floor hole to A10.')
deck(0, 4, 8); deck(4, 7, 5); solid(7, 10, 2.5)
deck(10, 16, 4); deck(30, 36, 4)
deck(41, 44, 3.5); deck(36, 39, 7); deck(41, 44, 10.5)
deck(46, 52, 14)
safe(3, 8)

room('A09', note='Settling Cache (optional): a zigzag climb back up through the 16 u shaft to A04.')
deck(9, 12, 3.5); deck(16, 19, 7); deck(10, 13, 10.5); deck(15, 18, 14); deck(12.5, 14, 17.5)
for i, y in enumerate([21, 24.5, 28, 31.5, 35]):           # the 3 u shaft (x 12.5-15.5)
    deck(14, 15.5, y) if i % 2 == 0 else deck(12.5, 14, y)

room('A10', note='Keeper Archive (optional): the long climb back through the 30 u shaft, crumbles in the upper half.')
deck(10, 13, 3.5); deck(17, 20, 7); deck(11, 14, 10.5); deck(16, 19, 14); deck(14.5, 16, 17.5)
for i, y in enumerate([21, 24.5, 28, 31.5, 35, 38.5, 42, 45.5, 49]):   # the 3 u shaft (x 14.5-17.5)
    (crumble if y > 33 else deck)(*((16, 17.5) if i % 2 == 0 else (14.5, 16)), y)

# ---------------------------------------------------------------- Route C: the Root Gardens (Living Thread, Bloomfall; Climbing Moss owned)
# Identity: swings over thorns, pod bounces, crumbling roots, moss walls for every climb back.

room('C01', note='Garden Arrival: teach the swing over a thorn bed (a running double jump also clears it), then up to the ceiling shaft.')
solid(4, 8, 3.5); anchor(10.5, 6.5); thorns(9, 12, 0, .5); thorns(15.5, 17, 0, .5); solid(17, 24, 3.5)   # checkpoint floor 12-15.5 stays dry
deck(22, 25, 7); deck(27, 30, 10.5); deck(22, 25, 14); deck(26, 30, 17.5); deck(26, 30, 21)   # then wall-jump the shaft
safe(2, 0); safe(19, 3.5)

room('C02', note='Canopy Walk: two swings over thorns, then a Bloomfall pod bounce up to the door shelf.')
solid(0, 5, 4); anchor(7, 7); thorns(5, 14, 0, .5)
solid(14, 20, 1.2)          # catch island (existing arrival marker)
deck(18.5, 21, 3.5); anchor(23.5, 6.8); thorns(20, 38, 0, .5)
solid(30, 33, 3); pod(36, 2.5); solid(38, 42, 4)
safe(2.5, 4); safe(16, 1.2); safe(31.5, 3)

room('C03', note='Root Ladder: drop down the ladder; a thorny right wall up high keeps climbs to the left wall; C10 and the C06 shortcut open off the right.')
deck(0, 5, 50); deck(8, 12, 45); thorns(15.5, 16, 36, 48)
deck(3, 10, 36.4)           # rest ledge (existing marker)
deck(12, 16, 30.5)          # C10 door shelf
crumble(2, 6, 26); pod(11, 20); deck(11, 16, 14)    # C06 shortcut shelf
deck(1, 5, 8)
safe(6.5, 36.4)

room('C04', note='Seed Cradle: the exit gate is shut; climb the left side to the latch by the C09 door, then swing across to the exit.')
deck(35, 45, 6)             # catches the drop from the ceiling shaft
for x, y in [(8, 3.7), (0, 7.4), (8, 11.1), (0, 14.8), (8, 18.5)]: deck(x, x + (4 if x == 0 else 3), y)
thorns(0, .5, 2, 7)         # no wall climb from the floor: use the ledges
deck(0, 8, 22.5)            # C09 door shelf
valve(4, 22.5, 'cw:C:cradle_gate', 'Release the cradle latch', 'Far below, the root gate drops open.')
thorns(46, 56, 0, .5)
deck(46, 49, 9.5); anchor(51, 12.5); deck(56, 64, 14)
gate(63, 64, 14, 18, ['cw:C:cradle_gate'])
safe(24, 0); safe(4, 22.5); safe(60, 14)

room('C05', note='Rootfall Well: crumbling roots down to the C06 door; moss walls for the way back.')
deck(0, 5, 36); crumble(7, 11, 31); crumble(2, 6, 26); crumble(9, 13, 21); crumble(3, 7, 16)
deck(11, 16, 10)            # C06 door shelf
solid(3, 9, 2.8); thorns(9, 16, 0, .5)
safe(2.5, 36); safe(13.5, 10)

room('C06', note='Bloom Court: a three-anchor swing chain over thorns to the C07 door; a latch opens the C03 shortcut.')
thorns(30, 48, 0, .5)
deck(29, 32, 3.8); anchor(34, 7.2); anchor(40, 8.5); anchor(45.5, 9.5)
deck(48, 54, 12)
deck(0, 5, 20); gate(0, 1, 20, 24, ['cw:C:root_shortcut'])
valve(3, 20, 'cw:C:root_shortcut', 'Open the shortcut', 'The way back to the Root Ladder is open.')
safe(20, 0); safe(51, 12)

room('C07', note='Gardener Rest: steps down from the door; the floor shaft drops into the arena.')
deck(0, 4, 6); solid(4, 7, 3)

room('C08', note='Root Guardian Arena: flat floor, fight ledges, a ledge ladder back up to the C07 shaft; floor hole to the Heart.')
deck(4, 10, 5); deck(20, 26, 5)
for x, y in [(17, 3.5), (10, 7), (17, 10.5), (10, 14), (17, 17.5), (10, 21)]: deck(x, x + 3, y)
deck(12, 16, 24.5)

room('C09', note='Hidden Nursery (optional): a four-anchor swing over thorns with one island to rest on.')
deck(38, 44, 6.5); thorns(8, 14, 0, .5); thorns(19.5, 38, 0, .5)
anchor(34.5, 9.5); anchor(28, 10); anchor(21.5, 10); anchor(15, 9.5); anchor(8.5, 9)
deck(14.5, 19, 2); deck(14, 17, 5.5)   # rest island, and a step up to the chain
deck(0, 8, 5)
safe(41, 6.5); safe(16.5, 2); safe(3, 5)

room('C10', note='Hanging Garden (optional): crumbling roots and a pod over thorns to the high ledge.')
deck(0, 5, 6.5); thorns(5, 10.5, 0, .5); thorns(14, 26, 0, .5)   # checkpoint floor stays dry
crumble(8, 11, 7); crumble(14, 17, 8.5); pod(20, 8); crumble(23, 26, 10); deck(26, 32, 12)
safe(2.5, 6.5)

# ---------------------------------------------------------------- Route D: the Wind (all of the above + Wind Leaf, Glidecap)
# Identity: gust lanes crossed in their calm, glides, non-cling rock that forces the long way round.

room('D01', note='Windward Shelter: rock below the door; climb on the right, then cross the gust lane in its calm.')
solid(0, 7, 10, 8.5, cling=False)           # door shelf (sill 10); its rock underside stops a wall climb
for x, y in [(34, 3.5), (28, 7), (34, 10.5)]: deck(x, x + 4, y)
deck(20, 26, 11); deck(10, 16, 11.5)
gust(9, 21, 11.2, 15.5); shelter(33, 39, 10.5, 13, 36, 10.5)
safe(2, 0)

room('D02', note='Leeward Well: descend through two gust bands to the floor shaft; the D04 shortcut joins low.')
deck(11, 16, 54); deck(4, 9, 49); deck(3, 10, 36.4)
gust(0, 16, 40, 44); shelter(3, 10, 36.4, 39, 6.5, 36.4)
deck(9, 14, 30); shelter(9, 14, 30, 32.5, 11.5, 30); gust(0, 16, 22, 26, 1.5)
deck(0, 4, 18)              # D04 shortcut shelf
deck(9, 14, 10)
safe(13.5, 54)

room('D03', note='Vane Court: glide from the door down to the floor shaft; the optional D09 lies beyond three timed gusts.')
deck(0, 6, 16); thorns(6, 18, 0, .5); thorns(34, 56, 0, .5)
deck(13, 17, 15.5); deck(25, 29, 16); deck(37, 41, 16.5); deck(50, 56, 16.5)
gust(18, 24, 15, 22, 0); gust(30, 36, 15, 22, 1.4); gust(42, 49, 15, 22, 2.8)
shelter(13, 17, 15.5, 18, 15, 15.5); shelter(25, 29, 16, 18.5, 27, 16); shelter(37, 41, 16.5, 19, 39, 16.5)
safe(3, 16); safe(21, 0)

room('D04', note='Wind Bridge: 36 u over thorns: crumbles, a gust and an island; the D02 shortcut climbs from the island.')
solid(40, 44, 4); solid(0, 4, 4); thorns(4, 17, 0, .5); thorns(24, 40, 0, .5)
crumble(31, 34, 4.5); solid(17, 24, 1.2); crumble(8, 11, 4.5)
gust(24, 30, 1, 8); shelter(17, 24, 1.2, 4, 20.5, 1.2)
deck(21, 24, 5); deck(18, 21, 8.5)          # up into the shortcut shaft
safe(42, 4); safe(2, 4)

room('D05', note='Updraft Hall: glide left across the hall to the D06 door; a lift and ledges rise to the D10 shaft.')
deck(66, 72, 14); thorns(8, 24, 0, .5); thorns(30, 64, 0, .5)
deck(48, 52, 11); deck(23, 26, 3.8); deck(25, 33, 7.5); deck(14, 18, 8); deck(0, 6, 6)
mover(36, 39, 8, 0, 18, 3.); deck(28, 32, 29.5); deck(22.5, 25.5, 33); deck(22.5, 25.5, 36.5)
deck(0, 5, 18)              # D08 shortcut shelf
safe(69, 14); safe(27, 0); safe(3, 6)

room('D06', note='Baffled Drop: glide down past alternating rock baffles to the low D07 door.')
deck(11, 16, 60)
for i, y in enumerate([52, 42, 32, 22, 12]): solid(*((0, 10) if i % 2 == 0 else (6, 16)), y + 1, y, cling=False)
deck(10, 16, 6); solid(3, 9, 2.8)
safe(13.5, 60); safe(13, 6)

room('D07', note='Still-Air Rest.')
deck(0, 5, 12); deck(5, 9, 8.5); deck(9, 13, 5)

room('D08', note='Wind Guardian Arena: flat floor, fight ledges, a ledge ladder to the D05 shortcut shaft; floor hole to the Heart.')
deck(34, 40, 12); deck(30, 34, 8.5); deck(26, 30, 5); deck(4, 10, 5)
for x, y in [(20, 3.5), (24, 7), (20, 10.5), (15, 14), (20, 17.5), (15, 21)]: deck(x, x + 3, y)
deck(14, 18, 24.5)

room('D09', note='Glider Roost (optional): glide and dash over thorns with one crumbling perch.')
deck(0, 6, 10.5); thorns(6, 12, 0, .5); thorns(15.5, 30, 0, .5); crumble(16, 19, 8); deck(19, 24, 8)   # checkpoint floor stays dry; solid(30, 36, 6)
safe(3, 10.5); safe(33, 6)

room('D10', note='Cloud Archive (optional): a lift, then a gust lane to the far ledge.')
mover(4, 7, 3, 0, 9, 3.); deck(8, 12, 14); gust(12, 24, 12, 18); shelter(8, 12, 14, 16.5, 10, 14); deck(26, 32, 15)
safe(13, 0)

# ---------------------------------------------------------------- Route E: the Descent (every skill, gently, downward)

room('E01', note='Quiet Arrival: steps up to both doors.')
deck(6, 9, 4); deck(0, 5, 8); solid(29, 32, 4); deck(32, 36, 8)

room('E02', note='Memory Well: down the well, a thread catch on the way; moss walls back up.')
deck(0, 5, 58); anchor(8, 50); deck(10, 16, 46); deck(3, 10, 39.2)
anchor(8, 30); deck(9, 14, 24); deck(2, 7, 14)
safe(6.5, 39.2)

room('E03', note='First Memory: caught under the shaft, then glide (or pod-bounce) across the thorns to the E04 door.')
deck(16, 24, 17); thorns(4, 12, 0, .5); thorns(28, 56, 0, .5)
pod(9, 6); deck(0, 4, 14)
safe(20, 17); safe(2, 14); safe(14, 0)

room('E04', note='Processional Walk: a slow, flat walk.')
solid(0, 4, 4); solid(4, 7, 2.4); solid(7, 37, 1.2); solid(37, 40, 2.4); solid(40, 44, 4)

room('E05', note='The Wilted Sanctuary: right to left, one of each: glide, swing, gust in its calm, pod bounce, a long jump.')
solid(66, 72, 24, 22)
deck(52, 56, 20); anchor(50, 23); deck(38, 44, 21)
gust(31, 38, 19, 26); shelter(40, 44, 21, 23.5, 42, 21)
deck(28, 31, 21); pod(23, 17); deck(16, 20, 22.5)
solid(0, 6, 24, 22)
deck(62, 65, 27.5); deck(67, 70, 31); deck(62, 65, 34.5); deck(66.5, 69.5, 38)     # optional climb to E10
safe(69, 24); safe(54, 20); safe(29.5, 21); safe(3, 24)

room('E06', note='Return Stair: wall-jump climb between alternating ledges to the ceiling shaft.')
deck(11, 16, 8); solid(3, 9, 2.8)
for i, y in enumerate(range(13, 60, 5)): deck(*((0, 4) if i % 2 == 0 else (12, 16)), y)
deck(5, 11, 60.5)

room('E07', note='Last Refuge: rest, the reward on its pedestal, steps to all three doors.')
solid(20, 25, 1.2); deck(21, 25, 4.5); deck(25, 30, 8)
deck(9, 12, 5); deck(5, 8, 8.5); deck(0, 5, 12)

room('E08', note='Final Threshold: three steps up to the Heart.')
deck(24, 32, 8); deck(14, 20, 10); deck(0, 8, 12)

room('E09', note='Lost Seed Archive (optional): crumbles over thorns up to the shaft.')
thorns(0, 8, 0, .5); thorns(16, 32, 0, .5)
crumble(17, 20, 3.6); crumble(22, 25, 7.2); crumble(17, 20, 10.8); deck(13, 17, 14.4); deck(14.5, 17.5, 18)
safe(12, 0)

room('E10', note='Sanctuary Alcove (optional): cross a gust over thorns to the reward pedestal.')
thorns(14, 20, 0, .5); solid(20, 27, 2.8); thorns(27, 32, 0, .5)
gust(12, 20, .5, 8); shelter(2, 6, 0, 3, 4, 0)
safe(4, 0)

# ---------------------------------------------------------------- Region cave networks MR01-MR03 (six linked rooms each)
# Their surface way in (C06) needs the Glidecap, so every skill is available; each network keeps its region's
# flavour: the Cradle's moss walls, the Causeway's thread swings over water, the Terraces' pods and crumbling grain.

room('MR01_C06', note='Hanging Garden (way in): hanging ledges over thorns, the reward floor, a moss climb to the High Crevice door.')
thorns(8, 22.5, 0, .5); thorns(26, 33, 0, .5)
deck(9, 12, 3.5); deck(14, 17, 5); deck(19, 22, 3.5); deck(27, 30, 3.5)
deck(42, 48, 11.5)          # MR01_C05 door shelf (sill 11.5), reached up the right wall
safe(2, 0); safe(35.5, 0)

room('MR01_C01', note='Warm Crack: the reward on its step, a step up to the Moss Pocket door, a ladder to the ceiling shaft.')
solid(21, 26, 1.2); deck(25, 28, 4.5); solid(28, 32, 7.5)
for x, y in [(12, 3.6), (17, 7.2), (12, 10.8), (14.5, 14.4)]: deck(x, x + 3, y)

room('MR01_C02', note='Moss Pocket: crumbling roots down over thorns; the heart seed sits high on the right, up the moss wall.')
deck(0, 5, 19.5); crumble(8, 11, 16); crumble(14, 17, 13); crumble(20, 23, 10)
thorns(5, 10, 0, .5); deck(27, 30, 8); deck(30, 33, 3.5); solid(34, 40, 5.5)   # the floor east of 10 is the Matriarch's arena
deck(32, 40, 11.6)          # the heart seed's ledge (existing reward)
safe(2.5, 19.5); safe(37, 5.5)

room('MR01_C03', note='Pale Alcove: the lore stone below, ledges up to the ceiling shaft.')
solid(0, 4, 5.5); solid(15, 20, 1.2); deck(14, 17, 4.8); deck(9, 12, 8.4); deck(10.5, 13.5, 11.6)

room('MR01_C04', note='Root Gallery: a rock slab over the floor shaft sends the climb round the left side.')
solid(17, 27, 7, 6, cling=False)
for x, y in [(11, 3.6), (6, 7.2), (11, 10.8)]: deck(x, x + 3, y)
crumble(16, 19, 14.4); deck(20.5, 23.5, 18)

room('MR01_C05', note='High Crevice: the amber at the foot, thorns on the left floor; climb the right wall back up.')
deck(9, 14, 14.5); deck(0, 5, 11.5); deck(6, 9, 17); thorns(0, 7, 0, .5)

# Causeway: thread swings over water.
room('MR02_C06', note='High Arch (way in): an eight-anchor arch over thorns, the reward floor, the floor shaft to the Workshop.')
deck(0, 5, 13.5)            # MR02_C05 door shelf, up the left wall from the arrival
thorns(6, 28, 0, .5); thorns(32, 42, 0, .5); thorns(50, 60, 0, .5)
for x, y in [(7.5, 16.5), (14, 18), (20.5, 18), (27, 18), (33.5, 18), (40, 18), (44, 12.5), (46.5, 7.5)]: anchor(x, y)
deck(46, 49, 3.5)
safe(2, 0); safe(44.5, 0)

room('MR02_C01', note='Underarch Workshop: ledges from the Silt Cistern door up to the ceiling shaft.')
solid(0, 5, 8.5); deck(9, 12, 11); deck(14, 17, 14.5); deck(16.5, 19.5, 17.5)

room('MR02_C02', note='Silt Cistern: swing the anchor line over the water; the heart seed on its island, a ledge ladder back up.')
water(4, 34); water(42, 52)
solid(46, 52, 20.5, 19.5); solid(0, 6, 9.5)
for x, y in [(44, 23.5), (37.5, 24.5), (31, 24.5), (24.5, 24), (18, 23), (11.5, 21)]: anchor(x, y)
deck(9, 12, 12.5); deck(13, 16, 15.5); deck(16, 19, 18.5)
for x, y in [(43, 3.5), (47, 7), (43, 10.5), (47, 14), (43, 17.5)]: deck(x, x + 3, y)
safe(3, 9.5); safe(38, 0); safe(49, 20.5)

room('MR02_C03', note='Echo Vault: anchors from the door shelf up to the ceiling shaft; the reward below on its pedestal.')
solid(31, 36, 9.5); solid(24, 29, 1.2)   # the floor west of the pedestal is the Warden's arena
anchor(29, 13); anchor(23, 15.5); anchor(18, 19.5)
deck(29, 31, 4.5); deck(25, 28, 7.8)
safe(33.5, 9.5); safe(26.5, 1.2)

room('MR02_C04', note='Ring Gallery: a lift on the left, then ledges round a rock slab to the ceiling shaft.')
solid(20, 30, 6, 5, cling=False)
mover(8, 11, 2, 0, 14, 3.); deck(13, 18, 16.5); deck(23.5, 26.5, 20)

room('MR02_C05', note='Moss Tower: the amber at the foot, a thorny low left wall; climb the right wall.')
deck(0, 5, 20.5); deck(11, 16, 13.5); deck(6, 9, 17); thorns(0, .5, 4, 14)

# Terraces: pods and crumbling grain.
room('MR03_C06', note='Root Hanger (way in): a swing line to the floor shaft, pods to the reward, a ledge to the Wind Shelf door.')
thorns(6, 28, 0, .5); thorns(32, 42, 0, .5); thorns(47, 54, 0, .5)
solid(3, 6, 3.5)
for x, y in [(8, 7), (14.5, 8), (21, 8), (27, 7.5), (33.5, 9), (39, 10), (45.5, 10.5), (52, 10.5)]: anchor(x, y)
pod(36, 2.5); pod(40.5, 3)
deck(48, 51, 3.8); solid(54, 60, 7.5)
safe(1.5, 0); safe(44.5, 0); safe(57, 7.5)

room('MR03_C01', note='Sluice Cellar: water floor, the heart seed on the dry floor; a pod up to the door shelf, crumbling grain to the ceiling shaft.')
water(4, 30); water(36, 44)
pod(33, 2.5); deck(35, 38, 7.5); solid(39, 44, 10.5)
crumble(33, 36, 13.5); crumble(28, 31, 16.5); crumble(24, 27, 19.5); deck(20, 23, 22.5); deck(20.5, 23.5, 25)
safe(32, 0); safe(41.5, 10.5)

room('MR03_C02', note='Grain Store: crumbling grain steps down from the left door to the right one over thorns; the amber on the dry floor.')
deck(0, 5, 22.5)
for x, y in [(8, 19), (13, 15.5), (18, 12), (23, 8.5)]: crumble(x, x + 3, y)
solid(35, 40, 7.5); thorns(4, 27, 0, .5)
deck(30, 33, 4)
safe(2.5, 22.5); safe(29.5, 0); safe(37.5, 7.5)

room('MR03_C03', note='Farmer Shelter: a few ledges up to the ceiling shaft.')
solid(0, 4, 7.5); deck(7, 10, 10); deck(11, 14, 13); deck(14.5, 17.5, 15.5)

room('MR03_C04', note='Mill Undercroft: a rock slab over the floor shaft; a piston lift and ledges up to the ceiling shaft.')
solid(19, 29, 7, 6, cling=False)
mover(12, 15, 3, 0, 14, 2.8); deck(16, 20, 18.5); deck(20.5, 23.5, 21.5); deck(22.5, 25.5, 22.5)

room('MR03_C05', note='Wind Shelf: a ledge walk through timed wind shears between the doors; the reward below the right shelf.')
solid(0, 5, 7.5); solid(37, 42, 7.5)
deck(9, 12, 7.5); deck(16, 19, 8); deck(23, 26, 7.5); deck(30, 33, 8)
spill(12.6, 15.4, 7.5, 12, phase=0.); spill(19.6, 22.4, 7.5, 12, phase=.9); spill(26.6, 29.4, 7.5, 12, phase=1.8)
thorns(5, 31, 0, .5); solid(31, 35, 1.2); deck(35, 37, 4.5)
safe(2.5, 7.5); safe(39.5, 7.5); safe(33, 1.2)

# ---------------------------------------------------------------- Single-room caves (MR05-MR07): one challenge to the reward and back
# Required skills: what the cave's door demands plus what the region assumes.

room('MR05_C02', note='Root Loom: a four-anchor swing over thorns to the heart seed and back.', skills=S3)
solid(2, 5, 3.5); thorns(5, 21, 0, .5); thorns(32, 32, 0, 0)
anchor(7.5, 7); anchor(13.5, 8); anchor(19.5, 8); anchor(24.5, 8)
solid(21, 27, 1.2); solid(27, 32, 4.5)
safe(1.5, 0); safe(24, 1.2)

room('MR05_C03', note='Moss Well: a non-cling rock wall splits the well; climb the moss walls up and over to the reward.', skills=S3)
solid(12, 13, 13, 0, cling=False); deck(0, 3, 12); deck(23, 26, 12); solid(16, 23, 2.8)

room('MR05_C04', note='Seed Choir: three pods over thorns, struck one after another, to the reward.', skills=S3)
thorns(4, 27, 0, .5); pod(9, 3); pod(16, 4); pod(23, 3); solid(27, 33, 1.2)
safe(2, 0); safe(30, 1.2)

room('MR05_C05', note='Stone Drum: a non-cling wall too high for the double jump; bounce off the drum pod to clear it.', skills=S3)
solid(14, 15, 5.5, 0, cling=False); pod(11, 2); solid(19, 25, 2.8)

room('MR05_C06', note='Canopy Pocket: up the left wall, glide over the thorns to the reward; swing back along the canopy.', skills=None)
deck(0, 6, 16); thorns(6, 24, 0, .5); thorns(29, 33, 0, .5); solid(24, 29, 1.2)
deck(31, 36, 14); anchor(26, 17); anchor(19, 17); anchor(12, 17)
safe(2, 0); safe(26.5, 1.2)

room('MR06_C01', note='Sheltered Nest: three timed gust lanes along the floor; dash through each calm to the heart seed.', skills=S4)
gust(8, 13, 0, 7, 0); gust(17, 22, 0, 7, 1.4); gust(25, 29, 0, 7, 2.8)
shelter(3, 7, 0, 3, 5, 0); shelter(13, 17, 0, 3, 15, 0); shelter(22, 25, 0, 3, 23.5, 0)

room('MR06_C02', note='Clasp Hollow: swing over thorns to the reward; a ledge on the right for the swing back.', skills=S4)
solid(2, 5, 3.5); thorns(5, 22, 0, .5); anchor(7.5, 7); anchor(13.5, 8); anchor(19, 7.5); anchor(24, 7.5)
solid(22, 26, 1.2); solid(26, 32, 4)
safe(1.5, 0); safe(24, 1.2)

room('MR06_C03', note='Wind Window: two full-height gust walls; dash through in their calm.', skills=S4)
gust(12, 16, 0, 12, 0); gust(20, 23, 0, 12, 1.6)
shelter(4, 10, 0, 3, 7, 0); shelter(16.5, 19.5, 0, 3, 18, 0)

room('MR06_C04', note='Echo Steps: crumbling steps over thorns to the reward.', skills=S4)
thorns(4, 21, 0, .5)
for x, y in [(6, 3), (10, 4.5), (14, 3.5), (18, 4.5)]: crumble(x, x + 2, y)
solid(21, 26, 1.2)
safe(2, 0); safe(23.5, 1.2)

room('MR06_C05', note='High Drift: up the left wall and glide down to the reward; climb the right wall and swing back.', skills=None)
deck(0, 6, 17); thorns(6, 24, 0, .5); thorns(30, 36, 0, .5)
deck(30, 33, 3.5); deck(31, 36, 15); anchor(25, 18); anchor(18, 18); anchor(11, 18)
safe(2, 0); safe(27, 0)

room('MR07_C01', note='Reed Shelter: crumbling reeds over thorns to the lore stone.', skills=None)
thorns(4, 17, 0, .5); crumble(6, 8.5, 2.5); crumble(11, 13.5, 3.5); solid(17, 22, 2.4)
safe(2, 0); safe(19.5, 2.4)

room('MR07_C02', note='Falls Pocket: swing over the pool through two falling sheets to the heart seed.', skills=None)
solid(2, 5, 3.5); water(5, 21); anchor(7.5, 7); anchor(13.5, 8); anchor(19, 7.5); anchor(24, 7.5)
spill(10, 11.2, 4, 14, phase=0.); spill(16, 17.2, 4, 14, phase=1.)
solid(21, 26, 1.2); solid(26, 32, 4)
safe(1.5, 0); safe(23.5, 1.2)

room('MR07_C03', note='Pale Archive: ride the shelf across the thorns to the reward.', skills=None)
thorns(5, 20, 0, .5); mover(6, 9, 3, 8, 0, 2.5); solid(20, 27, 2.8)
safe(2, 0); safe(23.5, 2.8)

room('MR07_C04', note='Quiet Terrace: glide over thorns to the reward; up the right wall and swing back.', skills=None)
deck(0, 6, 16); thorns(6, 24, 0, .5); thorns(29, 36, 0, .5); solid(24, 29, 1.2)
deck(29, 32, 4.5); deck(31, 36, 15); anchor(26, 17); anchor(19, 17); anchor(12, 17)
safe(2, 0); safe(26.5, 1.2)

room('MR07_C06', note='Upper Cleft (entered on the right): up the moss wall and over a non-cling rock to the amber; a ladder back.', skills=None)
solid(21, 22, 13, 0, cling=False); deck(23, 26, 12); solid(16, 21, 1.2)
deck(17, 20, 4.2); deck(17, 20, 7.9); deck(17, 20, 11.6)

# ---------------------------------------------------------------- Pockets with a reward (PC): one trick each

room('PC01', note='Firefly Nook: a rock rise too high for one jump; the first place to use the Leapseed.', skills=S0)
solid(5, 6, 3.5, 0, cling=False); solid(7.5, 10, 1)

room('PC08', note='Millstone Pocket: climb the moss wall to a perch and drop over the millstone to the heart seed.', skills=S2)
solid(8, 9, 6, 0, cling=False); deck(0, 2.5, 5.5); solid(10, 14, 1)

room('PC10', note='Rootlight Niche: bounce off the pod to clear the root wall.', skills=S3)
pod(5.7, 1.5); solid(6.5, 7.5, 5, 0, cling=False); solid(7.5, 10.5, 1)

room('PC12', note='Last Cairn: a gust over thorns guards the lore stone; dash through the calm.', skills=None)
thorns(6.4, 7.5, 0, .5); gust(6.4, 7.5, .5, 8); shelter(0, 6.3, 0, 3, 4.5, 0); solid(7.5, 10.5, 1)   # checkpoint at 4.6 stays calm

# ---------------------------------------------------------------- The Great Heart Hall (user, 31 Oct): one big room, four entrances,
# the heart in the middle. Each sector sees the heart (a look-out zooms out to the whole hall) but can't reach
# another sector: see-through grilles seal them. A (left, low), C (left, high), D (right, high) are balconies;
# E (right, low) is a balcony too: nobody reaches the heart, it's only seen (user, 31 Oct). No reward here.
room('HEART', note='Great Heart Hall: four sealed sector balconies around the heart; none reaches it (user, 31 Oct).')
grille(28, 29, 0, 48); grille(55, 56, 0, 48); grille(0, 28, 23, 24); grille(56, 84, 23, 24)
deck(0, 24, 12)                                 # A balcony (door sill 12)
for x, y in [(19, 3.6), (14, 7.2), (19, 10.8)]: deck(x, x + 3, y)   # back up from A's floor
deck(0, 26, 36)                                 # C balcony, under its ceiling shaft
deck(15.5, 18.5, 39.5); deck(19, 22, 43); deck(20, 24, 46.5)
deck(58, 84, 36)                                # D balcony
deck(64.5, 67.5, 39.5); deck(68, 71, 43); deck(68, 72, 46.5)
deck(56, 84, 12)                                # E balcony, level with A's balcony and the heart's plinth (user, 31 Oct)
deck(80, 84, 14)                                # a step up to E's door (sill 16)
for x0, x1, y in [(14, 19, 12), (16, 21, 36), (64, 69, 36), (66, 71, 12)]: look(x0, x1, y, 42, 24, 25)
# Each balcony's wake point by its grille; the heart's four quarters light up one by one (user, 31 Oct).
for sector, x, y in [('A', 22.5, 12), ('C', 24.5, 36), ('D', 59.5, 36), ('E', 58.5, 12)]: add('quarter', rect=[x - .7, y, 1.4, 2.2], flag=sector)
# The heart fills the middle of the hall (user, 31 Oct): a tall oval, one quarter facing each sector.
for sector, x, y in [('A', 29.5, 1), ('C', 29.5, 24), ('D', 42, 24), ('E', 42, 1)]: add('quarterlight', rect=[x, y, 12.5, 23], flag=sector)
safe(10, 12); safe(16, 36); safe(70, 36); safe(74, 12)

# ---------------------------------------------------------------- Rooms that had nothing to find (user, 31 Oct: every room has a reward)
room('PC02', note='Old Footprints: a double-jump shelf.', skills=S0)
deck(6, 9, 3); reward(7.5, 4.2, 10)
room('PC03', note='Fern Pocket: a rock rise too high for one jump.', skills=S0)
solid(7, 8, 3.5, 0, cling=False); reward(11, 1, 10)
room('PC04', note='Mason\'s Recess: a find on the floor (this pocket has no way in yet).', skills=S1)
reward(6, 1, 10)
room('PC05', note='Dripstone Hollow: up the right wall to the shelf.', skills=S1)
deck(10, 14, 6); reward(12, 7.2, 10)
room('PC06', note='Forgotten Satchel: a crumbling step over thorns.', skills=S1)
thorns(4, 10, 0, .5); crumble(5, 7, 2.5); reward(6, 3.7, 10)
room('PC07', note='Seed Shelf: swing over the thorns to the find.', skills=S2)
thorns(3, 12, 0, .5); anchor(6, 5.5); reward(9, 3, 10)
room('PC09', note='Lantern Rest: through a timed drip.', skills=S2)
spill(6, 7, 0, 8); reward(10.5, 1, 10)
room('PC11', note='Quiet Wind Pocket: through a gust in its calm.', skills=S4)
gust(5, 9, 0, 9); shelter(0, 4.5, 0, 3, 2, 0); reward(11.5, 1, 10)
room('MR05_C01', note='Lost Lantern: crumbling stones over thorns to the find.', skills=S3)
thorns(4, 28, 0, .5); crumble(6, 8, 2.5); crumble(11, 13, 3.5); crumble(16, 18, 2.5); crumble(21, 23, 3.5)
solid(28, 32, 1.2); reward(30, 2.4, 30)
room('MR06_C06', note='Weather Cave: crumbling steps through two gust walls over thorns.', skills=S4)
thorns(5, 24, 0, .5); gust(8, 13, .5, 12); gust(17, 21, .5, 12, 1.6)
crumble(6, 8, 3); crumble(14, 16, 3.5); crumble(22, 24, 3); solid(24, 32, 1.2); reward(28, 2.4, 30)
room('MR07_C05', note='Watcher\'s Hollow: up the left wall, glide over thorns to the find; the right wall and a glide back.', skills=None)
deck(0, 4, 9); thorns(4, 26, 0, .5); solid(26, 32, 1.2); deck(28, 32, 9); reward(29, 2.4, 30)

# Rewards for rooms laid out earlier: on the hardest beat or a short detour. Amber: 20 on the routes, 40 in the
# optional rooms, 30 in the region caves, 10 in pockets.
for rid, (x, y, amber) in {
    'A01': (21.5, 4, 20), 'A02': (11.5, 31.2, 20), 'A03': (13.25, 5.2, 20), 'A04': (30.5, 23, 20), 'A05': (12.5, 36.2, 20),
    'A06': (41.5, 11.5, 20), 'A07': (21, 3.2, 20), 'A08': (13, 5.2, 20), 'A09': (17.5, 8.2, 40), 'A10': (18.5, 15.2, 40),
    'C01': (13.5, 5.5, 20), 'C02': (36, 5, 20), 'C03': (14, 31.7, 20), 'C04': (51, 10, 20), 'C05': (11, 22.2, 20),
    'C06': (40, 6.5, 20), 'C07': (8, 1, 20), 'C08': (23, 6.2, 20), 'C09': (21.5, 7.5, 40), 'C10': (20, 9.5, 40),
    'D01': (15.5, 13, 20), 'D02': (6.5, 42, 20), 'D03': (27, 17.6, 20), 'D04': (27, 6, 20), 'D05': (37.5, 27.6, 20),
    'D06': (13, 47, 20), 'D07': (7, 9.7, 20), 'D08': (7, 6.2, 20), 'D09': (17.5, 9.5, 40), 'D10': (18, 15, 40),
    'E01': (34, 9.2, 20), 'E02': (8, 27, 20), 'E03': (9, 8, 20), 'E04': (22, 2.6, 20), 'E05': (23, 19.5, 20),
    'E06': (2, 34.2, 20), 'E08': (17, 11.2, 20), 'E09': (23.5, 8.4, 40),
    'MR02_C01': (15.5, 15.7, 30), 'MR03_C03': (12.5, 14.2, 30),
}.items():
    room(rid); reward(x, y, amber)


# ---------------------------------------------------------------- Creatures (user, 31 Oct: "start placing")
# Never on a required jump: on landings, floors and corridors. Rest rooms, pockets, single caves and the Heart stay quiet.
GRUB, NEWT, SHELL, SPIT, SENT, WING, MOTH, KNUCK = ('Assets/Prefabs/%s.prefab' % n for n in
    ('Grub01', 'Newt01', 'Shellback01', 'PodSpitter01', 'Sentinel01', 'Thornwing01', 'GustMoth01', 'Knucklebramble01'))
for rid, placements in {
    'A01': [(NEWT, 21.5, 0, 'E-07 Ripple Newt')],
    'A02': [(WING, 4.5, 35.5, 'E-10 Silt Leech')],
    'A03': [(WING, 5, 11.2, 'E-10 Silt Leech'), (SENT, 26, 0, 'E-11 Sluice Crab'), (NEWT, 32, 0, 'E-07 Ripple Newt')],
    'A04': [(NEWT, 42, 0, 'E-07 Ripple Newt'), (MOTH, 22, 0, 'E-12 Pump Bloater', True)],
    'A06': [(SENT, 16, 0, 'E-11 Sluice Crab')],
    'A09': [(GRUB, 22, 0, 'E-06 Burrow Grub')],
    'A10': [(GRUB, 6, 0, 'E-06 Burrow Grub')],
    'C01': [(SPIT, 22, 3.5, 'E-13 Root Snapper')],
    'C02': [(WING, 17, 1.2, 'E-14 Seed Swarm', True)],
    'C04': [(WING, 40, 6, 'E-14 Seed Swarm', True), (SHELL, 28, 0, 'E-15 Bramble Creeper')],
    'C06': [(SPIT, 17, 0, 'E-13 Root Snapper')],
    'C09': [(SPIT, 2.5, 5, 'E-13 Root Snapper')],
    'C10': [(WING, 29, 12, 'E-14 Seed Swarm', True)],
    'D01': [(MOTH, 24, 0, 'E-17 Gale Wisp', True)],
    'D03': [(WING, 22, 3.5, 'E-16 Kite Ray', True)],
    'D04': [(WING, 14, 4, 'E-16 Kite Ray', True)],
    'D05': [(SPIT, 29, 7.5, 'E-18 Hail Sentry')],
    'D09': [(WING, 21.5, 8, 'E-16 Kite Ray', True)],
    'E03': [(GRUB, 25, 0, 'E-20 Wilted Husk')],
    'E04': [(GRUB, 16, 1.2, 'E-20 Wilted Husk'), (GRUB, 28, 1.2, 'E-20 Wilted Husk')],
    'E05': [(WING, 36, 22, 'E-16 Kite Ray', True)],
    'E09': [(GRUB, 12, 0, 'E-20 Wilted Husk')],
    'MR01_C06': [(GRUB, 40, 0, 'E-06 Burrow Grub')],
    'MR02_C06': [(WING, 46, 0, 'E-14 Seed Swarm', True)],
    'MR03_C02': [(SHELL, 31, 0, 'E-15 Bramble Creeper')],
}.items():
    room(rid)
    for pl in placements: enemy(*pl)

# Guardians: each arena's way onward stays shut until its guardian falls (STATE_CONTRACT's ug:BOSS_<route>:open).
# The guardians are their own code now (UndergroundGuardian.cs and its subclasses); the mini-bosses are still stand-ins.
room('A08'); boss(KNUCK, 30, 0, 'ug:BOSS_A:open', 'G-03 Sluice Warden'); gate(51, 52, 14, 18, ['ug:BOSS_A:open'])
room('C08'); anchor(23, 11); anchor(14, 11); boss(KNUCK, 25.5, 0, 'ug:BOSS_C:open', 'G-04 Root Matron'); gate(32.05, 35.95, -.6, 0, ['ug:BOSS_C:open'])
room('D08'); boss(KNUCK, 24, 0, 'ug:BOSS_D:open', 'G-05 Tempest Owl'); gate(14.05, 17.95, -.6, 0, ['ug:BOSS_D:open'])
room('MR01_C02'); enemy(SHELL, 24, 0, 'M-01 Moss Matriarch (mini-boss)')
room('MR02_C03'); enemy(SENT, 14, 0, 'M-02 Echo Warden (mini-boss)')
room('MR03_C04'); enemy(SHELL, 37, 0, 'M-03 Grain Golem (mini-boss)')

def dry_checkpoints(r, spec, half=1.75):
    """Each floor checkpoint (the scene's Rest markers) keeps a dry patch: floor hazards are split around it."""
    for m in r['markers']:
        if m['kind'] != 'Rest' or m['y'] > 1: continue
        out = []
        for s in spec['shapes']:
            x, y, w, h = s['rect']
            if s['kind'] in ('water', 'thorns') and y <= .1 and x < m['x'] + half and x + w > m['x'] - half:
                if m['x'] - half > x: out.append(dict(s, rect=[x, y, m['x'] - half - x, h]))
                if x + w > m['x'] + half: out.append(dict(s, rect=[m['x'] + half, y, x + w - m['x'] - half, h]))
            else: out.append(s)
        spec['shapes'] = out

def seals(r, spec):
    # A boss's or mini-boss's room shuts its doors while it fights, except a door already behind a gate.
    if not any(s['kind'] == 'boss' or s.get('design', '').startswith('M-') for s in spec['shapes']): return
    gates = [s['rect'] for s in spec['shapes'] if s['kind'] == 'gate']
    for d in r['doors']:
        w = d['width']
        if d['side'] in 'LR':
            rect = [0 if d['side'] == 'L' else r['w'] - .6, d['sill'], .6, w + .4]
        else:
            rect = [d['x0'], -.6 if d['side'] == 'B' else r['h'] - .6, w, .6]
        if any(rect[0] < g[0] + g[2] and g[0] < rect[0] + rect[2] and rect[1] < g[1] + g[3] and g[1] < rect[1] + rect[3] for g in gates): continue
        spec['shapes'].append(dict(kind='seal', rect=rect))

def write(rooms_path, out_path):
    rooms = json.load(open(rooms_path))['rooms']
    out = dict(schema='qolossal.underground.platforming/1', rooms={})
    for rid, spec in ROOMS.items():
        r = rooms[rid]; ox, oy = r['x'], r['y']; shapes = []
        dry_checkpoints(r, spec)
        seals(r, spec)
        for s in spec['shapes']:
            s = dict(s); x, y, w, h = s['rect']; s['local'] = [x, y, w, h]; s['rect'] = [round(ox + x, 3), round(oy + y, 3), w, h]
            if 'feet' in s: s['feet'] = [round(ox + s['feet'][0], 3), round(oy + s['feet'][1], 3)]
            shapes.append(s)
        out['rooms'][rid] = dict(replace=spec['replace'], note=spec['note'], origin=[ox, oy], shapes=shapes, skills=spec['skills'])
    out['roomList'] = [dict(id=k, **v) for k, v in out['rooms'].items()]   # for Unity's JsonUtility
    json.dump(out, open(out_path, 'w'), indent=1)
    print(len(out['rooms']), 'rooms,', sum(len(v['shapes']) for v in out['rooms'].values()), 'shapes')

if __name__ == '__main__':
    write(sys.argv[1], sys.argv[2])
