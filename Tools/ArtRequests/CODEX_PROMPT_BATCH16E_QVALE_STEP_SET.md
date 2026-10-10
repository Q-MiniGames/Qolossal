Qolossal: Codex Batch 16e (Qvale street: a purpose-built step set, so no terrain edge shows a vertical or horizontal cut). Start now and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch16e\
Copy your context files into Batch16e\Integrator\Context\, as for Batch 16d.

WHY
Qvale's street now has its umber soil, darker kerb and step kit (Batches 16c and 16d), but its steps still show cuts. The user's screenshot is Tools\ArtRequests\Batch16e\user_screenshot_cuts.png; current captures are town_*.png beside it. The cause is the kit, not the painting quality. The 16d pieces are cliff pieces, 4.27 u wide, painted for tall rock walls, while Qvale's steps are short built town steps. To fit them the game has to cut them, and every cut shows:
1. Vertical cut where a lower tread's kerb starts at the foot of a step (the strip simply begins at the riser line).
2. Horizontal cut where a step corner is cropped at the lower walk line, because the riser is shorter than the corner.
3. Visible seam where a corner's kerb overlaps the ordinary strip (two copies of the kerb, out of phase).
4. A pale horizontal line under the kerb stones (inherited from the strip's transition rows).
5. On the two tall walls, a repeating stone pattern.

This batch replaces that with a step set designed for Qvale's actual steps. Every join between pieces, and between a piece and the strip or fill, must happen inside a painted alpha fade over soil, or under a planted grass and moss clump. The game will never crop a piece except inside a fade band named below.

READ FIRST
1. AGENTS.md and Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md. Mark every device test "not tested".
2. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_41.md, and your Batch16c and Batch16d deliveries.
3. Tools\ArtRequests\Batch16e\qvale_surface.json: the exact walkable line of the Qvale street (world units; 1 u = 120 px) and every riser in it, with the ground beside its top. There are eight risers: 1.04 u (twice), 1.63 to 1.88 u (four), 5.74 u and 6.25 u. Treads beside a riser top are flat or slope up to 0.75 rise per unit.
4. Assets\MountainReliefAtlas\Scripts\MraTerrain.cs: TopMesh (how the strip is laid and sheared along slopes), and Wall (how step pieces are placed today). The new assembly is described under ASSEMBLY below; Claude will implement it.
5. Accepted Qvale_Top_Strip_v2, Qvale_Fill and the Qvale corner pieces in Assets\Art\Codex\Terrain\Body\Qvale\. Match their scale, kerb stones, moss, grass and soil exactly.
6. Tools\ArtImport\codex_v2_accepted.json: the accepted manifest (1173 files today). Re-read it; don't trust the count.

ORIENTATION
_L: the step's open face looks left; the upper ground continues to the right of the riser. _R: the mirror image. Light is soft and from above; an R piece may be a mirror of its L piece if nothing reads as wrongly lit.

COLOURS AND MATERIAL
Exactly Batch 16d: soil mean about #7A5A3E (darks to #4A3426), kerb stones about #B39C78 with joints about #806C52, the same moss and grass. The step faces are dressed stone blocks stacked like the 16d corner's stone column, the same stone and mortar as the kerb.

WHAT TO MAKE (12 new PNGs under Terrain\Body\Qvale\, 120 PPU, RGBA, no mipmaps, no accepted file replaced; names checked free on 9 Oct)

Pixel rows and columns below are from the top-left of each canvas. "Riser line" is the column where the step's collision face is; "walk line" is the row where Qori's feet stand.

1. Qvale_Top_Strip_v3 (1024 x 512, Repeat U / Clamp V): Qvale_Top_Strip_v2 with the pale band directly under the kerb stones removed, so the stones sit straight in the soil with a soft dark joint shadow. Same alpha channel byte for byte, same grass and stones, walk line row 96.

2. Qvale_Step_Short_L and _R (768 x 512): one complete step for a riser of 1.04 u (125 px). Riser line column 256 (L; 512 for R). Upper walk line row 160, lower walk line row 285.
   - Upper side (right of the riser for L): the kerb and grass turn round the top edge and run 512 px into the upper tread. Its last 192 px are a planted moss and grass clump over the kerb that fades to alpha 0 at the canvas edge, so it can lie over the ordinary strip with no seam.
   - The face: dressed stone blocks from the upper walk line down to the lower walk line, overhanging the riser line by up to 40 px into the open side, as the 16d corner does.
   - Lower side (left for L): the lower tread's kerb starts against the foot of the face with a corner stone and a grass tuft (no straight start), runs 256 px and ends in the same kind of fading clump.
   - Below the kerbs: soil, fading to alpha 0 over the bottom 96 rows and over the 96 columns at each side.

3. Qvale_Step_Medium_L and _R (768 x 640): the same for a riser of 1.75 u (210 px). Upper walk line row 160, lower walk line row 370. The game may stretch it vertically by up to 8% to fit risers of 1.63 to 1.88 u, so keep the face's blocks free of fine horizontal detail that would show a stretch.

4. Qvale_Step_Outer_L and _R (768 x 512): the top of a tall wall. Upper walk line row 160; the upper kerb and fading clump as in the short step; the face's stone column from the walk line down to a mortar joint at row 448; rows 448 to 512 fade to alpha 0.

5. Qvale_Step_Wall_A, _B and _C (384 x 240, Repeat V, for both L and R by mirroring): 2.0 u of the same stone column. Row 0 and row 239 are mortar joints; any variant stacks on any other with no seam, and A, B and C differ enough that no repeat reads on a 6 u wall. The riser line is column 128 for L; the soil side fades to alpha 0 over its last 96 columns.

6. Qvale_Step_Inner_L and _R (768 x 512): the foot of a tall wall. The stone column comes down from a mortar joint at row 64 (rows 0 to 64 fade in from alpha 0) to the lower walk line at row 352, where the lower kerb starts against it with a corner stone and grass tuft and runs 256 px into the lower tread, ending in a fading clump. Soil below fades out as in the short step.

ASSEMBLY (what the game will do; your proofs must follow it)
- Strips: Qvale_Top_Strip_v3 along every tread and slope, unchanged otherwise. Every strip end at a riser lies under a step piece's fading clump.
- Riser up to 1.3 u: Step_Short, placed by its riser line and upper walk line.
- Riser 1.4 to 2.0 u: Step_Medium, placed by its riser line and upper walk line, stretched vertically to meet the lower walk line.
- Riser over 2.0 u: Step_Outer at the top, Step_Inner at the bottom, Step_Wall variants stacked between from the Outer's joint downwards; the last wall runs under the Inner's fade-in rows.
- Where the ground beside a step's top is a slope (up to 0.75 rise per unit), the upper kerb run and its clump are sheared vertically to follow it, as the strip is. So keep those 512 px free of strong vertical features that a shear would bend visibly.
- Draw order: fill, strip, then step pieces on top.

NON-NEGOTIABLES
- No piece edge may show as a straight line anywhere: every edge that meets another piece, the strip or the fill is either a painted alpha fade over soil or hidden under a clump.
- Kerb stones, grass, moss and soil match Qvale_Top_Strip_v2 and Qvale_Fill in scale (120 PPU), colour and texture.
- Nothing pale in the soil, no white cracks. Mystery rule: nothing body-like.
- Never write under C:\UnityProjects\Qolossal\Assets. Keep every accepted file byte-identical, with a preservation receipt.
- Paint at native size or downscale; never upscale or resample X and Y independently. Archive native outputs with prompts and settings in Batch16e\sources\<name>\.
- Run the global name check (case-insensitive, against accepted names, accepted sub-sprite names and every Assets\ stem).

PROOFS
- The whole Qvale street profile from qvale_surface.json, assembled exactly as ASSEMBLY describes, with Qvale_Fill below and the accepted sky, at 1920 x 1080 and 1280 x 720 for these windows: x 8 to 40 (the entry stairs and the 5.74 u wall), x 26 to 58 (the slope tops and the two 1.04 u steps), x 222 to 246 (the 6.25 u wall), and one camera-height crop around each of the eight risers. Show Qori (accepted 144 PPU composite) standing at the foot and top of two steps.
- The same windows with today's 16d assembly (from the town_*.png captures) beside them.
- A 4x crop of every join (piece to strip, piece to fill, wall to wall, outer to wall, wall to inner), showing no straight edge.
- Each piece over Qvale_Fill at 1:1, and the three walls stacked in every order.

DELIVERABLES
- The 12 PNGs with a REGISTRATION.json per piece (canvas, PPU, riser line column, walk line rows, joint rows, fade bands), Batch16e\MANIFEST.json (sha256, size, PPU, pivot, wrap), the QA sheet, the name-check log and the preservation receipt.
- REVIEW_HANDOFF.md, with a platform section that keeps source checks, imported-resource notes and device tests (all "not tested") separate.

If anything here is ambiguous or impossible, write it in the handoff instead of guessing.
