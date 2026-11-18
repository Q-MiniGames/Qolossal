# Codex brief: Batch 50b-r1, Keepers' Nursery B04 slice, revision

9 Nov 2026, Claude. Read `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_108.md` first. Its "Must fix" items 1-6 are this brief. Everything in `CODEX_PROMPT_BATCH50B_KEEPERS_NURSERY_B04_SLICE.md` still applies unless changed here. Route names A-D; B04 = internal C04.

Deliver into `Codex_v2_full\Batch50\50b-r1\`. Never overwrite a 50b file. Same rules: no scene edits, prototype folder only, mipmaps off, BC7, read back the imported format.

## Do

1. **Repaint `FAR_B04_Terraces`** (same name, size and tiling): legible receding terraces and drawer walls, warm lantern pools, a few lilac points; FAR P99.9 at 15-25% of the walk rim; no lit lips; no shelf on empty void.
2. **Re-dress B04's rear wall** to the approved concept's density: piers every 8-14 u, drawer banks, root curtains, lantern niches, bloom clusters, trellis fragments. At most about 30% bare masonry in any of the four camera frames. Optional new variant `NK_Wall_Masonry_02` (10 × 10 u, 2160 × 2160, seamless): larger, irregular blocks with root cracks; alternate it with `_01`. If you add it, rename the existing tile to `NK_Wall_Masonry_01` only in your preview; the 50b file stays as delivered.
3. **Close the vertical gap** between the masonry and the seed archive (upper-left view). No void may show between rear-wall placements anywhere in the slice.
4. **No floating slabs.** Every shelf and jar group sits in a niche or against masonry, never on the void.
5. **Granary tower:** no shelf top within 1 u vertically of any deck within 4 u horizontally, across the full camera range at 88/94 motion. Prove it with a sweep, or move the tower to a deck-free area.
6. **Light:** visible warm pools around lantern niches, soft glow at mint root tips and lilac clusters, through the light maps (repaint `LM_B04` and the passage maps if needed, same sizes).
7. Any other new kit variant the dressing needs: name it, give size and memory, and keep the route under the 222 MiB budget.

## Deliver

- The changed PNGs and `MANIFEST_50b-r1.json`.
- **Side-by-side sheets for the four B04 cameras: the approved 50a-r1 concept (or its framing at that camera) beside the new in-game capture.** For each, say where the result differs from the concept and why.
- All 50b QA again for the slice: value gate, reserved shapes with the gameplay-object overlay, mystery, seams, imported format and memory, captures at 3840 and 1920 with F8 on and off.
- `REVIEW_HANDOFF_50b-r1.md`: verdict per fix. Re-check the next free CLAUDE_REVIEW number; don't write a review.

The automated traversal walk isn't required this time. Leave the preview ready for the user to play B04 (menu and keys as in 50b).

**Stop.** Claude reviews and the user plays before 50c.
