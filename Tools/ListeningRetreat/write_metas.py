# Writes .meta files with deterministic GUIDs for the new Assets/ListeningRetreat folders, scripts and
# art, so the scratch project and the open editor give each the same identity. Importer settings are
# then applied by ListeningRetreatBuilder and copied back.
import hashlib, os
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BASE = os.path.join(ROOT, "Assets", "ListeningRetreat")
def guid(path): return hashlib.md5(("qolossal-retreat:" + os.path.relpath(path, ROOT).replace("\\", "/")).encode()).hexdigest()
def write(path, body):
    m = path + ".meta"
    if os.path.exists(m): return
    open(m, "w", newline="\n").write(f"fileFormatVersion: 2\nguid: {guid(path)}\n" + body); print("wrote", os.path.relpath(m, ROOT))
paths = [BASE]
for d, dirs, files in os.walk(BASE):
    paths += [os.path.join(d, x) for x in dirs] + [os.path.join(d, x) for x in files if not x.endswith(".meta")]
for p in paths:
    if os.path.isdir(p): write(p, "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    elif p.endswith(".cs"): write(p, "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    elif p.endswith(".json"): write(p, "TextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    elif p.endswith(".png"): write(p, "")
