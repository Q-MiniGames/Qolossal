"""Run only after fresh Unity extraction and refresh_map_sources.py --apply.

Without --publish, rebuilds/validates review only. --publish exports to Unity
only after the revision's unchanged-underground and changed-mouth checks pass.
"""
import argparse, subprocess, sys
from refresh_map_sources import ROOT,TASK,MAP,CANON,read,validate
from verify_revision_map import verify

p=argparse.ArgumentParser();p.add_argument('--publish',action='store_true');args=p.parse_args()
validate()
assert read(MAP/'source_refresh.json')['canonicalInputsWritten'], 'Run source preflight with --apply first'
assert (CANON/'component-placement-lock.json').exists(), 'Approved underground placement lock required'
for script in ('build_layout.py','viewer.py'):
    subprocess.run([sys.executable,str(CANON/script)],cwd=ROOT,check=True)
verify(False)
if args.publish:
    subprocess.run([sys.executable,str(CANON/'export_runtime.py')],cwd=ROOT,check=True)
    verify(True)
    subprocess.run([sys.executable,str(CANON/'verify.py')],cwd=ROOT,check=True)
print('Map review refreshed.'+(' Runtime export verified; live UI test still required.' if args.publish else ' Runtime was not published.'))
