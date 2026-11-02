# Still composite of a v2 retreat place from its Unity registration (Art/v2/<Scene>), with Ludo seated frame 001
# between BenchBack and BenchFront, for checking layout before a Unity build:
#   python mock_retreat_v2.py <Scene> <out.jpg>
import json, os, sys
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(HERE))
scene, out = sys.argv[1], sys.argv[2]
D = os.path.join(ROOT, "Assets", "ListeningRetreat", "Art", "v2", scene)
reg = json.load(open(os.path.join(D, "Retreat_" + scene + "_Registration.json")))
info = json.load(open(os.path.join(ROOT, "Assets", "ListeningRetreat", "Art", "LudoSeated", "Retreat_QoriSeated_info.json")))
q = Image.open(os.path.join(ROOT, "Assets", "ListeningRetreat", "Art", "LudoSeated", "Retreat_QoriSeated_001.png")).convert("RGBA")
seat = reg["qori_seat"]; k = seat["figure_height_px"] / (info["figureBottomPx"] - info["figureTopPx"])
qs = q.resize((round(q.width * k), round(q.height * k)), Image.LANCZOS)
can = Image.new("RGBA", (3840, 2160))
def put(im, x, y):
    global can
    lay = Image.new("RGBA", (3840, 2160)); lay.paste(im, (int(round(x)), int(round(y))), im); can = Image.alpha_composite(can, lay)
for l in reg["layers"]:
    if l["name"] == seat["insert_before"]:
        put(qs, seat["anchor_px"][0] - info["cloakCentrePx"] * k, seat["anchor_px"][1] - info["figureBottomPx"] * k)
    im = Image.open(os.path.join(D, os.path.basename(l["file"]))).convert("RGBA")
    if l.get("wrap") == "vertical":
        for y in (0, l["period_px"]): put(im, 0, y)
    else: put(im, *l["offset"])
can.convert("RGB").resize((1920, 1080), Image.LANCZOS).save(out, quality=88)
