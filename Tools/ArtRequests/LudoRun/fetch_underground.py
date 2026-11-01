"""Files the newest Ludo frames download under its clip's folder in Tools/IncomingArt/Ludo/Underground.
Not art: moves and unzips a download. The clip is named by its id in LUDO_PROMPTS_UNDERGROUND_ENEMIES.md.
Usage: python fetch_underground.py <clip ids in download order, e.g. G4-2 G4-3>   (or `list` to print id, folder and prompt)
"""
import os, re, sys, glob, zipfile, shutil, time

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
PROMPTS = os.path.join(ROOT, 'Tools', 'ArtRequests', 'LUDO_PROMPTS_UNDERGROUND_ENEMIES.md')
OUT = os.path.join(ROOT, 'Tools', 'IncomingArt', 'Ludo', 'Underground')
DOWNLOADS = os.path.join(os.environ['USERPROFILE'], 'Downloads')

def clips():
    rows = re.findall(r'^\| ([A-Z])(\d+)-(\d) \| ([^|]+?) \| `([^`]+)` \|', open(PROMPTS, encoding='utf-8').read(), re.M)
    return {f'{a}{n}-{k}': (f'{a}-{int(n):02d} - {name}', prompt) for a, n, k, name, prompt in rows}

if __name__ == '__main__':
    table = clips()
    if sys.argv[1] == 'list':
        for cid, (folder, prompt) in table.items(): print(cid, '|', folder, '|', prompt)
        sys.exit()
    # Several ids: the newest downloads, oldest first, in the order the ids were downloaded.
    ids = sys.argv[1:]
    for _ in range(30):
        zips = sorted(glob.glob(os.path.join(DOWNLOADS, 'sprite-*frames*.zip')), key=os.path.getmtime)
        fresh = [z for z in zips if time.time() - os.path.getmtime(z) < 900]
        if len(fresh) >= len(ids) and not glob.glob(os.path.join(DOWNLOADS, '*.crdownload')): break
        time.sleep(1)
    else: sys.exit(f'only {len(fresh)} fresh downloads for {len(ids)} clips')
    for cid, z in zip(ids, fresh[-len(ids):]):
        target = os.path.join(OUT, table[cid][0])
        os.makedirs(target, exist_ok=True)
        with zipfile.ZipFile(z) as f: f.extractall(target)
        shutil.move(z, os.path.join(target, os.path.basename(z)))
        print(cid, table[cid][0], len(glob.glob(os.path.join(target, '**', '*.png'), recursive=True)), 'frames')
