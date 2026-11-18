# Qori's reach, by relic, for the ability access checker. Two envelopes, because one number can't
# prove both directions:
#   safe  the game's own design margins (run speed x0.9, peaks cut by 0.45, no sprint, no ledge grab,
#         same rules as UndergroundPlatforming_20261030/validate.py). Passing it proves "you can".
#   best  everything Qori can squeeze out (sprint, no margin, ledge grab, the air dash's reset of the
#         fall). Failing it proves "you can't".
# Anything between the two is grey and needs hands on it.
# Constants are the MovementTuning.cs defaults overridden by Resources/Tuning/MovementTuning.asset;
# read_tuning() re-reads both, so a tuning change shows up here without editing this file.
import re

ABILITIES = ('dj', 'moss', 'thread', 'pod', 'dash', 'glide')
DT = 1 / 60
GRAVITY = 9.81 * 3   # Physics2D gravity x the player's gravity scale; reproduces the measured rise of 2.39
GRAB = 1.42          # PlayerMovement.TryGrabLedge: ledge top up to 0.22 over the 1.2 u body's head
GLIDE_MEASURED = 4.46   # u forward per u dropped, measured on 17 Oct
GLIDE_SAFE = 3.5        # the platforming plan's sizing margin for glide spans
SAFE_RUN_FACTOR, SAFE_PEAK_CUT = .9, .45
TABLE_STEP = .05
TABLE_MIN, TABLE_MAX = -70., 12.


def read_tuning(cs_path, asset_path):
    cs = open(cs_path, encoding='utf-8').read()
    asset = open(asset_path, encoding='utf-8').read()
    def value(name):
        m = re.search(r'^\s*%s: ([-\d.e]+)' % name, asset, re.M)
        if m: return float(m.group(1))
        m = re.search(r'public float %s = ([-\d.]+)f?;' % name, cs)
        if not m: raise SystemExit('MovementTuning has no ' + name)
        return float(m.group(1))
    names = ('runSpeed sprintSpeed jumpSpeed doubleJumpSpeed maximumFallSpeed fallGravityMultiplier apexGravityMultiplier '
             'apexSpeedBand dashSpeed dashSeconds glideFallSpeed glideSpeed').split()
    return {n: value(n) for n in names}


class Reach:
    def __init__(self, tuning):
        self.t = t = tuning
        self.safe_vx = t['runSpeed'] * SAFE_RUN_FACTOR
        self.best_vx = max(t['runSpeed'], t['sprintSpeed'])
        self.dash_gain = t['dashSpeed'] * t['dashSeconds']
        self.tables = {}
        for dj in (False, True):
            self.tables['safe', dj] = self._table(self.safe_vx, dj, False, SAFE_PEAK_CUT)
            for dash in (False, True):
                self.tables['best', dj, dash] = self._table(self.best_vx, dj, dash, 0.)

    def _flight(self, vx, second, dash):
        """One airborne path from (0, 0): second = when the double jump is pressed, dash = when the air dash starts."""
        t = self.t
        x, y, vy, time = 0., 0., t['jumpSpeed'], 0.
        used = dashed = False
        pts = []
        while y > TABLE_MIN and time < 8:
            vy -= GRAVITY * DT
            if abs(vy) <= t['apexSpeedBand']: vy -= GRAVITY * (t['apexGravityMultiplier'] - 1) * DT
            elif vy < 0: vy -= GRAVITY * (t['fallGravityMultiplier'] - 1) * min(1, -vy / t['apexSpeedBand']) * DT
            vy = max(vy, -t['maximumFallSpeed'])
            y += vy * DT; x += vx * DT; time += DT
            if second is not None and not used and time >= second: vy, used = t['doubleJumpSpeed'], True
            if dash is not None and not dashed and time >= dash:
                dashed = True; vy = 0.; x += self.dash_gain; time += t['dashSeconds']
            pts.append((x, y))
        return pts

    def _table(self, vx, dj, dash, cut):
        """max horizontal distance by height difference, over every timing of the double jump and the dash."""
        n = int((TABLE_MAX - TABLE_MIN) / TABLE_STEP) + 1
        table = [float('-inf')] * n
        seconds = [None] + ([i * .05 for i in range(40)] if dj else [])
        dashes = [None] + ([i * .08 for i in range(25)] if dash else [])
        peak = float('-inf')
        for s in seconds:
            for d in dashes:
                pts = self._flight(vx, s, d)
                top = max(y for _, y in pts) - cut
                peak = max(peak, top)
                for x, y in pts:
                    i = max(0, int((min(y, top) - TABLE_MIN) / TABLE_STEP))
                    if x > table[i]: table[i] = x
        for i in range(n - 2, -1, -1):
            table[i] = max(table[i], table[i + 1])
        return table, peak

    def _dx(self, key, dy):
        table, peak = self.tables[key]
        if dy > peak: return float('-inf')
        return table[max(0, int((dy - TABLE_MIN) / TABLE_STEP))]

    def peak(self, mode, ab):
        key = ('safe', 'dj' in ab) if mode == 'safe' else ('best', 'dj' in ab, 'dash' in ab)
        return self.tables[key][1]

    def reach(self, mode, dx, dy, ab, bonus=0., grab=False):
        """Can Qori land dx across and dy up from a standing take-off, with these abilities?"""
        dy -= bonus
        if mode == 'safe':
            if 'dash' in ab: dx = max(0., dx - 2.5)
            key = ('safe', 'dj' in ab)
            if 'glide' in ab:
                peak = self.tables[key][1]
                if dy <= peak and dx <= self._dx(key, peak - .05) + GLIDE_SAFE * (peak - dy): return True
            return dx <= self._dx(key, dy)
        key = ('best', 'dj' in ab, 'dash' in ab)
        if dx <= self._dx(key, dy): return True
        if grab and dx <= self._dx(key, dy - GRAB) + .5: return True
        if 'glide' in ab:
            peak = self.tables[key][1]
            if dy <= peak:
                # Fly to the apex at full sprint, then open the cap and drift the rest of the way down.
                carry = self._dx(key, peak - .05)
                if dx <= carry + GLIDE_MEASURED * (peak - dy): return True
        return False
