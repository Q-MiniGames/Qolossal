"""Draw the reclining-titan route map (designer spoilers) as SVG, and render a PNG with Edge.

Usage: python Tools/Design/reclining_route_map.py
Writes QOLOSSAL_RECLINING_ROUTE_MAP_DESIGNER_SPOILERS.svg/.png next to this script.
Cave markers are placed from the chambers' entrance nodes in mra_world.json.
"""
import json
import os
import subprocess

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(os.path.dirname(HERE))
WORLD = os.path.join(PROJECT, "Assets", "MountainReliefAtlas", "Resources", "MRA", "mra_world.json")
OUT = os.path.join(HERE, "QOLOSSAL_RECLINING_ROUTE_MAP_DESIGNER_SPOILERS")
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
W, H = 1920, 1080
GROUND = 930

# The walkable top of the body, feet (left) to crown (right).
TOP = [(250, 930), (196, 905), (160, 860), (140, 780), (138, 700), (142, 655), (152, 632),
       (166, 626), (180, 636), (194, 630), (208, 642), (222, 640), (236, 656), (250, 660),
       (290, 728), (330, 770), (420, 690), (540, 565), (650, 460), (720, 412), (765, 398),
       (810, 415), (880, 495), (970, 610), (1050, 715), (1110, 752), (1170, 742), (1250, 696),
       (1340, 630), (1440, 572), (1530, 540), (1590, 530), (1640, 565), (1640, 535),
       (1630, 505), (1648, 482), (1650, 462), (1622, 430), (1660, 400), (1672, 392),
       (1668, 360), (1700, 310), (1780, 272), (1860, 300), (1900, 380), (1905, 460)]
ROUTE_FROM, ROUTE_TO = 6, TOP.index((1780, 272))  # toe tip to crown (indices into TOP)
# The underside: back on the rising ground, then under the thigh, knee and calf.
UNDER = [(1905, 610), (1700, 700), (1450, 800), (1250, 880), (1120, 930), (1060, 905),
         (980, 800), (880, 650), (790, 590), (700, 615), (560, 725), (430, 845), (360, 905),
         (300, 930)]

REGIONS = [  # id, label, real part, route x span
    ("MR01", "The Cradle", "feet and toes", 128, 322),
    ("MR02", "The Long Causeway", "shin", 322, 700),
    ("MR03", "The Terraces", "knee, then down the thigh", 700, 1075),
    ("MR04", "Qvale", "lap", 1075, 1185),
    ("MR05", "The Ribwood", "belly and ribs", 1185, 1560),
    ("MR06", "The Windward Heights", "shoulder, collarbone, neck", 1560, 1655),
    ("MR07", "The Summit", "chin, mouth, nose, eye, brow", 1655, 1905),
]
COLORS = ["#c9a46a", "#b8a27a", "#a8b06c", "#e0a050", "#8fae78", "#9fb2c0", "#d6c7a8"]

DRIFTS = [  # name, cluster centre, slabs (dx, dy, w), launch point on the route (x)
    ("P1 Stepping Stones", (470, 420), [(-90, 70, 60), (-30, 20, 50), (40, -20, 56), (110, -60, 46), (60, -120, 70)], 470),
    ("P2 Lantern Drift", (1080, 470), [(-130, 40, 54), (-60, -10, 48), (10, -50, 60), (90, -20, 50), (150, -70, 56)], 960),
    ("P3 The Swell Isles", (1430, 330), [(-110, 60, 56), (-40, 10, 46), (30, -30, 60), (100, 0, 46), (60, -80, 64)], 1340),
    ("P4 Updraft Stair", (1560, 200), [(-60, 120, 50), (0, 70, 56), (60, 20, 46), (130, -20, 60), (210, -50, 50)], 1600),
]


def catmull(points, closed=False, tension=0.5):
    """Smooth path through points as cubic Beziers (SVG d string + a sampled polyline)."""
    pts = points + points[:3] if closed else [points[0]] + points + [points[-1]]
    d = f"M{pts[1][0]:.1f},{pts[1][1]:.1f}"
    poly = []
    for i in range(1, len(pts) - 2):
        p0, p1, p2, p3 = pts[i - 1], pts[i], pts[i + 1], pts[i + 2]
        c1 = (p1[0] + (p2[0] - p0[0]) * tension / 3, p1[1] + (p2[1] - p0[1]) * tension / 3)
        c2 = (p2[0] - (p3[0] - p1[0]) * tension / 3, p2[1] - (p3[1] - p1[1]) * tension / 3)
        d += f" C{c1[0]:.1f},{c1[1]:.1f} {c2[0]:.1f},{c2[1]:.1f} {p2[0]:.1f},{p2[1]:.1f}"
        for k in range(20):
            t = k / 20
            mt = 1 - t
            poly.append((mt**3 * p1[0] + 3 * mt * mt * t * c1[0] + 3 * mt * t * t * c2[0] + t**3 * p2[0],
                         mt**3 * p1[1] + 3 * mt * mt * t * c1[1] + 3 * mt * t * t * c2[1] + t**3 * p2[1]))
    poly.append(points[-1])
    return d, poly


def surface_at(poly, x):
    """Top-surface y at x (first crossing from the left, ignoring the vertical foot)."""
    best = min((p for p in poly if p[0] >= 128 and (p[0] > 200 or p[1] < 700)), key=lambda p: abs(p[0] - x))
    return best[1]


def main():
    world = json.load(open(WORLD, encoding="utf-8"))
    top_d, top_poly = catmull(TOP)
    under_d, _ = catmull(UNDER)
    body_d = top_d + " L" + under_d[1:] + " Z"
    route_poly = [(x, y - 14) for x, y in top_poly[20 * ROUTE_FROM:20 * ROUTE_TO]]
    route_d = "M" + " L".join(f"{x:.1f},{y:.1f}" for x, y in route_poly)

    s = []
    a = s.append
    a(f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}" font-family="Segoe UI, Arial, sans-serif">')
    a("""<defs>
<linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#f3e3c4"/><stop offset="0.7" stop-color="#e9d9bd"/><stop offset="1" stop-color="#d9c9a8"/></linearGradient>
<linearGradient id="body" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#e8e0cc"/><stop offset="0.25" stop-color="#cfc6ae"/><stop offset="1" stop-color="#a69b82"/></linearGradient>
<linearGradient id="moss" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#7f9152"/><stop offset="1" stop-color="#5f7140"/></linearGradient>
<filter id="soft"><feGaussianBlur stdDeviation="1.2"/></filter>
<marker id="arrow" viewBox="0 0 10 10" refX="6" refY="5" markerWidth="7" markerHeight="7" orient="auto"><path d="M0,0 L10,5 L0,10 z" fill="#7a2f1d"/></marker>
</defs>""")
    a(f'<rect width="{W}" height="{H}" fill="url(#sky)"/>')
    # distant valley wall and floor
    a(f'<path d="M0,{GROUND-120} C300,{GROUND-170} 600,{GROUND-110} 900,{GROUND-150} S1500,{GROUND-260} 1920,{GROUND-330} L1920,{H} L0,{H} Z" fill="#cdbf9f" opacity="0.55"/>')
    a(f'<path d="M0,{GROUND} L1100,{GROUND} C1400,{GROUND-40} 1700,{GROUND-180} 1920,{GROUND-330} L1920,{H} L0,{H} Z" fill="#9d9270"/>')
    a(f'<path d="M0,{GROUND} L1100,{GROUND}" stroke="#7d7356" stroke-width="3"/>')

    # region bands behind the body
    for i, (rid, name, part, x0, x1) in enumerate(REGIONS):
        a(f'<rect x="{x0}" y="70" width="{x1-x0}" height="{GROUND-70}" fill="{COLORS[i]}" opacity="0.10"/>')
        a(f'<line x1="{x0}" y1="70" x2="{x0}" y2="{GROUND}" stroke="#8a7a5a" stroke-width="1" stroke-dasharray="3 6" opacity="0.6"/>')

    # the body
    a(f'<path d="{body_d}" fill="url(#body)" stroke="#6d6450" stroke-width="3"/>')
    a(f'<path d="{top_d}" fill="none" stroke="url(#moss)" stroke-width="12" stroke-linecap="round" opacity="0.85"/>')
    # under-knee arch shading, the great arch
    # designer hints: kneecap, near arm with its hand beside Qvale, rib walls, navel
    a('<path d="M735,420 Q765,395 800,425" fill="none" stroke="#8a7f66" stroke-width="3"/>')
    a('<path d="M1585,575 C1520,690 1470,780 1420,800 C1340,812 1270,806 1215,792 C1190,786 1172,774 1168,762" fill="none" stroke="#857a62" stroke-width="16" stroke-linecap="round" opacity="0.3"/>')
    for fx in (1172, 1186, 1200, 1214, 1228):
        a(f'<path d="M{fx},770 q-4,-14 2,-22" fill="none" stroke="#857a62" stroke-width="6" stroke-linecap="round" opacity="0.5"/>')
    a('<text x="1250" y="830" font-size="13" fill="#5a503c" font-style="italic">near arm; its hand rests beside Qvale</text>')
    for rx in (1370, 1410, 1450, 1490, 1530):
        a(f'<path d="M{rx},{632 - (rx-1370)*0.45:.0f} l-18,70" stroke="#8a7f66" stroke-width="3" opacity="0.6"/>')
    a('<circle cx="1262" cy="720" r="7" fill="none" stroke="#8a7f66" stroke-width="3"/>')
    a('<text x="1262" y="760" font-size="13" fill="#6e1712" text-anchor="middle">Navel Well</text>')
    a('<path d="M1664,382 q10,8 20,0" fill="none" stroke="#4d6f8a" stroke-width="4"/>')

    # MR08: the descent inside, crown to the chest
    a('<path d="M1800,420 C1760,520 1700,600 1640,620 C1580,640 1520,650 1460,660" fill="none" stroke="#7a2f1d" stroke-width="5" stroke-dasharray="10 8" marker-end="url(#arrow)"/>')
    a('<circle cx="1445" cy="662" r="16" fill="#b5462e" stroke="#7a2f1d" stroke-width="3"/>')
    a('<text x="1445" y="707" font-size="17" fill="#7a2f1d" text-anchor="middle" font-weight="600">MR08 The Descent</text>')
    a('<text x="1445" y="727" font-size="14" fill="#7a2f1d" text-anchor="middle">after the reveal: inside, to the heart</text>')

    # the main route
    a(f'<path d="{route_d}" fill="none" stroke="#fffaf0" stroke-width="9" stroke-linecap="round"/>')
    a(f'<path d="{route_d}" fill="none" stroke="#2f6b8a" stroke-width="5" stroke-linecap="round" stroke-dasharray="14 9"/>')
    a('<circle cx="166" cy="606" r="9" fill="#2f6b8a"/>')
    a('<text x="130" y="560" font-size="16" fill="#2f4f5f" font-weight="600">START</text>')
    a('<text x="130" y="578" font-size="13" fill="#2f4f5f">Qori sprouts among the toes</text>')

    # Qvale houses in the lap
    for hx in (1092, 1118, 1146, 1170):
        hy = surface_at(top_poly, hx) - 2
        a(f'<path d="M{hx-9},{hy} L{hx-9},{hy-14} L{hx},{hy-23} L{hx+9},{hy-14} L{hx+9},{hy} Z" fill="#e7a24a" stroke="#7a4e1d" stroke-width="1.5"/>')

    # caves from the chamber data
    regions = {r[0]: r for r in REGIONS}
    for c in world["chambers"]:
        rid = c["region"]
        if rid not in regions or rid == "MR04":
            continue
        same = sorted((d for d in world["chambers"] if d["region"] == rid), key=lambda d: (d["entranceNode"], d["id"]))
        k = same.index(c)
        x0, x1 = regions[rid][3], regions[rid][4]
        x = x0 + (x1 - x0) * (k + 0.5) / len(same)
        y = surface_at(top_poly, x) + 40
        secret = c["type"] == "secret"
        a(f'<path d="M{x-9:.1f},{y+7:.1f} Q{x:.1f},{y-12:.1f} {x+9:.1f},{y+7:.1f} Z" fill="#c8322a" stroke="#6e1712" stroke-width="1.5"{" stroke-dasharray=\"2 2\"" if secret else ""}/>')
    # body-place caves called out in the proposal
    a('<path d="M860,820 Q900,760 945,820 Z" fill="#c8322a" stroke="#6e1712" stroke-width="2"/>')
    a('<text x="902" y="850" font-size="14" fill="#6e1712" text-anchor="middle" font-weight="600">The Under-Arch</text>')
    a('<text x="902" y="866" font-size="12" fill="#6e1712" text-anchor="middle">(behind the knee)</text>')

    # floating platform levels
    for name, (cx, cy), slabs, launch_x in DRIFTS:
        ly = surface_at(top_poly, launch_x) - 16
        fx, fy = cx + slabs[0][0], cy + slabs[0][1]
        a(f'<path d="M{launch_x},{ly} Q{(launch_x+fx)/2},{min(ly,fy)-30} {fx},{fy+6}" fill="none" stroke="#3f7f3a" stroke-width="2.5" stroke-dasharray="4 5"/>')
        for dx, dy, w in slabs:
            x, y = cx + dx, cy + dy
            a(f'<rect x="{x-w/2}" y="{y}" width="{w}" height="11" rx="5" fill="#5fa04e" stroke="#2f5f28" stroke-width="2"/>')
            a(f'<path d="M{x-w/2+6},{y+11} L{x},{y+22} L{x+w/2-6},{y+11} Z" fill="#9b8f72" opacity="0.8"/>')
        a(f'<text x="{cx}" y="{cy + min(d[1] for d in slabs) - 14}" font-size="16" fill="#2f5f28" text-anchor="middle" font-weight="600">{name}</text>')

    # region labels along the bottom
    for i, (rid, name, part, x0, x1) in enumerate(REGIONS):
        cx = (x0 + x1) / 2
        y = 968 if i % 2 == 0 else 1018
        a(f'<text x="{cx}" y="{y}" font-size="17" fill="#2b2418" text-anchor="middle" font-weight="700">{rid} {name}</text>')
        a(f'<text x="{cx}" y="{y+19}" font-size="14" fill="#4b4030" text-anchor="middle" font-style="italic">{part}</text>')

    # title and legend
    a('<text x="40" y="52" font-size="30" fill="#2b2418" font-weight="700">Qolossal: the reclining titan route</text>')
    a('<text x="40" y="80" font-size="16" fill="#6e1712" font-weight="600">DESIGNER SPOILERS. Never shown in game before the Summit reveal.</text>')
    lx, ly = 40, 110
    a(f'<rect x="{lx-12}" y="{ly-22}" width="330" height="150" rx="10" fill="#fffaf0" opacity="0.85" stroke="#b9a885"/>')
    a(f'<path d="M{lx},{ly} L{lx+40},{ly}" stroke="#2f6b8a" stroke-width="5" stroke-dasharray="14 9"/><text x="{lx+52}" y="{ly+5}" font-size="15" fill="#2b2418">main route, walked on the body</text>')
    a(f'<rect x="{lx}" y="{ly+20}" width="40" height="11" rx="5" fill="#5fa04e" stroke="#2f5f28" stroke-width="2"/><text x="{lx+52}" y="{ly+31}" font-size="15" fill="#2b2418">floating-platform level (optional)</text>')
    a(f'<path d="M{lx+11},{ly+63} Q{lx+20},{ly+44} {lx+29},{ly+63} Z" fill="#c8322a" stroke="#6e1712" stroke-width="1.5"/><text x="{lx+52}" y="{ly+60}" font-size="15" fill="#2b2418">cave / side chamber (dashed: secret)</text>')
    a(f'<path d="M{lx},{ly+85} L{lx+40},{ly+85}" stroke="#7a2f1d" stroke-width="5" stroke-dasharray="10 8"/><text x="{lx+52}" y="{ly+90}" font-size="15" fill="#2b2418">the descent, after the reveal</text>')
    a(f'<path d="M{lx+8},{ly+115} L{lx+8},{ly+103} L{lx+16},{ly+96} L{lx+24},{ly+103} L{lx+24},{ly+115} Z" fill="#e7a24a" stroke="#7a4e1d"/><text x="{lx+52}" y="{ly+115}" font-size="15" fill="#2b2418">Qvale, the hub</text>')
    a("</svg>")

    with open(OUT + ".svg", "w", encoding="utf-8") as f:
        f.write("\n".join(s))
    if os.path.exists(EDGE):  # --headless=new needs its own profile folder
        subprocess.run([EDGE, "--headless=new", "--user-data-dir=" + os.path.join(os.environ.get("TEMP", HERE), "qolossal_edge"), "--disable-gpu", "--hide-scrollbars", f"--window-size={W},{H}",
                        f"--screenshot={OUT}.png", "file:///" + (OUT + ".svg").replace("\\", "/")],
                       check=False, capture_output=True, timeout=60)
    print(OUT + ".svg", os.path.exists(OUT + ".png"))


if __name__ == "__main__":
    main()
