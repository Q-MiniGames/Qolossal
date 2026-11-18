# Rounded corner traversal criteria

The first run completed all 130 cases with no errors, respawns, or changed normal saves. Its original verdict remains failed: 123 cases exceeded the fixture's arbitrary ±0.4u endpoint tolerance. Original fixture/results/cases/progress/capture metadata are copied with hashes under `Before/CornerFirstRun`.

Independent analysis of the original 0.1s movement samples found all 130 started grounded, contacted ground before the lip, and crossed at least 0.6u beyond its original x. Final centre clearance was 1.175–1.763u; the slowest driven interval after acceleration and before clearance was 4.895u/s. These traces show outward crossing with expected falling/airborne deceleration, not a corner snag. They cannot prove absence of a sub-sample pause or directly establish collider-edge clearance because that was not recorded.

The revised fixture checks the gameplay obligation: begin grounded, retain evidence of pre-lip ground contact, move the actual BoxCollider2D trailing edge at least 0.05u beyond the corner, and finish with that clearance without respawn. While outward input exceeds 0.5 and before first whole-body clearance, a sustained 0.15s window with less than 0.02u forward movement fails as a snag; a 0.12s initial acceleration grace avoids testing immediate full speed. Input-neutral airborne deceleration is excluded. It records each criterion and failure reason separately. No endpoint proximity target determines pass.

Rerun all 130 cases through root's Unity owner to collect the missing full-frame stall and actual collider evidence. The first run is supporting diagnosis, not a retroactive passing test. Upward ledge climbing, safe lower landing, hardware controls, performance, and console certification are outside this fixture.
