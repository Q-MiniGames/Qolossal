# Terrain correction review — 4 November 2026

This revision responds to the user's rejection of the earlier world-refresh terrain presentation. The earlier gallery is historical evidence, not approval of the revised work.

The grounded camera places the player's feet at 15% of viewport height on surfaces and underground. Jump framing retains vertical room to rise before following upward. The closest camera half-height remains five world units, requiring 216 native source pixels per world unit at 3840 × 2160.

Approved cave paintings, their inner facets and dark recesses are restored. Native surface fill attaches outside their painted aperture. Rounded exposed terrain corners and tapered strip ends remove abrupt top-cap joins. The CaveRollout source paintings remain unchanged. A small set of older approved Cradle paintings has insufficient native density at its existing registered scale; these exceptions are recorded instead of replacing the approved appearance or enlarging source bitmaps.

PC04 now enters the actual Causeway cliff rather than an isolated mound. Qvale A01 now enters the western mountainside, outside the city. Persistent connection IDs, destinations and save keys are preserved. Their travel and checkpoint reloads have separate runtime evidence.

Underground scenic paintings are composed into one continuous distant cavern for each of twelve themes. Forty-eight native lateral, vault and peripheral paintings extend the preserved central scenes. Narrative scenes do not repeat in rectangular panels. Actual rooms, collision shapes and passage slopes remain intact.

Final evidence is collected in `QA.json`. Surface images in `Captures` are saved-scene Editor renders; the `Camera`, `Portals`, `Corners`, `CaveVista/Play` and `Map` directories identify their actual runtime test scope. Source integrity and exact per-asset resolution calculations are separate receipts. The mobile PDF uses these images with explicit captions; preview image compression does not alter game assets.

The canonical map must be regenerated from the final saved scenes. Its existing underground component positions remain locked to the approved cartography while the two surface mouth connections and rounded surface contours update. Never run `connect_titan.py` to restore the superseded illustration.

These checks concern Windows Unity Editor behavior and visual presentation. Standalone builds, console hardware, memory budgets and 60 FPS remain unverified; Xbox Series S and Nintendo Switch 2 handheld/docked require explicit platform testing.
