"""Writes a .meta with a deterministic GUID for new Qolossal audio scripts/folders, so the scratch
project and the open editor give each the same identity.  python write_script_meta.py <path>..."""
import hashlib, os, sys
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
for path in sys.argv[1:]:
    path = os.path.abspath(path); m = path + ".meta"
    if os.path.exists(m): continue
    g = hashlib.md5(("qolossal-sfx:" + os.path.relpath(path, ROOT).replace("\\", "/")).encode()).hexdigest()
    if os.path.isdir(path):
        body = f"fileFormatVersion: 2\nguid: {g}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    else:
        body = f"fileFormatVersion: 2\nguid: {g}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {{instanceID: 0}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    open(m, "w", newline="\n").write(body); print("wrote", m)
