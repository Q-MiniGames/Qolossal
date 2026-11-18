# Final live atlas verification

The current saved scene extraction, topology inventory, canonical layout and Unity export were regenerated together after the last checkpoint repair. All 13 scene hashes match. The map contains 173 areas/rooms, 37 paired cave connections, 63 passages and 650 actual terrain contours. The 108 shared passage collision paths retain their previous coordinates.

`ui_tests.json` records 36 successful steps and 506 assertions at 1920 × 1080 and 3840 × 2160, including 267 marker projection samples (three per underground room), seven focused regions, mouse selection, virtual-gamepad navigation/back, silhouette selectors and no visible area labels. The full runtime contour coordinates are compared with the canonical export.

The fixture first checks that a new isolated save hides undiscovered caves. It then marks all 89 rooms and the reveal flag inside that temporary save to inspect the complete map. The discovered views are QA fixtures, not changes to normal progression. Normal saves and Editor settings were restored. The final Heights 4K and Cradle 1080p captures were visually inspected for visible rooms, complete tunnel connections, label-free presentation and framing.

This is Windows Unity Editor evidence. It does not establish standalone, physical-controller or console-hardware readiness. The retained historical-count failure receipt documents the obsolete 949-contour assumption; the final test checks the current extracted coordinates directly.
