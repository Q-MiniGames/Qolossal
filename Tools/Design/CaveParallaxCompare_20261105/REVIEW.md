# Cave background comparison

The user wants to assess fixed backgrounds versus gentle horizontal parallax in a cavern and adjoining passage before changing all caves. This temporary review uses Cistern rooms A04/A05 and their shared route, starting at x1610 on the existing walkway. Walk right through the passage, walk back, then switch modes to compare.

- **A — Fixed:** the background moves with the terrain (zero camera-follow offset).
- **B — Gentle:** the scenery scrolls at 88% of terrain speed horizontally (12% camera-follow).
- Both modes scroll with terrain vertically: no vertical parallax.
- **F8** or **left stick click** toggles A/B; **F6** returns to the start. The current mode appears through the existing HUD toast.

The composition is registered to the same reference camera (1610,107.5), so A and B are pixel-identical at the comparison origin. No camera movement or zoom settings change. Existing paintings, density, grading, room bounds and collision remain intact. Nearby gameplay terrain and framing do not parallax. The low-contrast extension field remains part of the existing composition; this experiment does not redesign the artwork.

`CaveParallaxComparison` exists only in Editor builds and is explicitly created by the launcher, never saved in scenes or automatically installed. It binds two room materials and the relevant room-side samples in their connecting passages: seven overrides total. Other room samples keep their previous settings, including the opposite end of a passage leading out of the review area. Shader default override flags are zero, preserving the existing behavior when the tool is absent. Disable/destroy restores all original vectors; cleanup was checked in Edit mode as well as normal isolated-save Play teardown.

`verify_render.cs` compares both ends of all 63 passages in both modes: 252 samples, no failures at 216 pixels/world unit. `measure.py` measures a two-unit pan: fixed -215.99px, gentle -190.02px at 108px/world unit; both vertical pans measure 216.00px. The two modes match exactly at the reference camera. The untouched 87 room materials keep their default parameters.

`playtest.cs` verifies real rendering while walking through and back in both modes, with native 4K/1080p captures. It also injects F8 and left-stick click to verify switching. Review-room gates are opened through their public controls in the isolated test only. The launcher uses a separate copy of the user's testing save, opens only A04/A05 gates for comparison, and restores normal save routing and Editor options after Play ends. No normal save is rewritten.

Final receipts: `render_checks.json`, `motion_measurements.json`, `Play/result.json`, `QA.json`. All 13 scene hashes remain unchanged. This is Windows Editor verification with synthetic keyboard/gamepad input. Standalone, physical-controller, PS5, Xbox Series X/S, Switch 2 handheld/docked and hardware performance are not tested. No new final cave-parallax policy is selected by this comparison.
