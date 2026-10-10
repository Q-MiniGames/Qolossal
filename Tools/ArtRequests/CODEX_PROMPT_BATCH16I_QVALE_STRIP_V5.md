Qolossal: Codex Batch 16i (Qvale kerb strip v5: keep 16h's stones, restore the soil under them). Start now and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch16i\
Copy your context files into Batch16i\Integrator\Context\, as for Batch 16h.

WHY
Read Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_45.md. Your 16h grade of the kerb stones is right: measured on stone pixels only, they now match the step pieces. The band under the kerb is not: the 16h targets for it were measured over moss and stone undersides as well as soil (Claude's error in the brief), and meeting them lifted the soil grain into bright orange speckle that reads as a different soil (CLAUDE_REVIEW_45_v3_v4_2x.png and CLAUDE_REVIEW_45_Seam.png beside the review).

WHAT TO MAKE (1 new PNG under Terrain\Body\Qvale\, name checked free on 9 Oct)
Qvale_Top_Strip_v5:
- Rows 0-145: byte-identical to Qvale_Top_Strip_v4 (16h).
- Rows 157-511: byte-identical to Qvale_Top_Strip_v3.
- Rows 146-156: a smooth per-row blend from v4 to v3, so the stones' undersides keep their graded colour and run into the original soil with no visible line.
- Alpha byte-identical to v3 and v4; seamless in U; no geometry change.
That's a grade-only composite of two of your own deliveries; no repaint.

NON-NEGOTIABLES
- Never write under C:\UnityProjects\Qolossal\Assets. Keep every accepted file byte-identical, with a preservation receipt (1191 accepted today; re-read the manifest).
- Global name check as before.

PROOFS
- The same street windows as 16h (risers 5-6 and 3-4) with v3, v4 and v5 side by side, at 1920 x 1080 and 1280 x 720.
- The same four 4x seam crops, v3 / v4 / v5.
- Stone-only percentiles of v5 rows 100-147 (exclude moss: keep pixels with G <= R - 8 and B <= G) next to the step pieces' upper kerbs measured the same way, and the per-row identity checks above.

DELIVERABLES
- The PNG, Batch16i\MANIFEST.json (sha256, size, PPU 120, pivot [0.5, 0], Repeat U / Clamp V), the QA sheet, the name-check log, the preservation receipt and REVIEW_HANDOFF.md with the usual platform section (all device tests "not tested").

If anything here is ambiguous or impossible, write it in the handoff instead of guessing.
