# Regional edge density review

Native mesh analysis uses28 barycentric samples per triangle, actual scene transform, normalized region weights, source alpha, vertex alpha and shader RGB mixing. It diagnoses shader contribution before unrelated renderer occlusion; it is not an exhaustive visibility certificate.

| Source | Original PPU | PPU at >=1% contribution | PPU at >=50% contribution |
|---|---:|---:|---:|
| Cistern_EdgeNative.png | 132.163 | 133.651 | 211.473 |
| Roots_EdgeNative.png | 132.691 | 134.601 | 207.853 |
| Wind_EdgeNative.png | 83.852 | 83.852 | 83.852 |
| Descent_EdgeNative.png | 132.217 | 132.217 | 211.013 |
| Cradle_EdgeNative.png | 109.422 | 135.445 | 223.655 |
| Causeway_EdgeNative.png | 132.381 | 135.848 | 209.333 |
| Mill_EdgeNative.png | 134.512 | 134.512 | 220.010 |
| Heart_EdgeNative.png | 134.929 | 155.958 | 221.581 |

D01 Wind walking edge has a genuine83.852PPU deficit at98.8% source contribution near world(2016.64,437.51). Weak corner blends explain some conservative minima, but cannot dismiss all failures.

Correction is staged and copied for root: per-family uniform source-coordinate scaling with fixed outer y=1, plus a handoff to existing native cap texture below the smaller painted band. All materials share family scales and cap world mapping stays unchanged. No source enlargement, source replacement, or mesh/collider edit. Unity audit and visual captures remain required.

Legacy MR03 foreground hay tuft0 samples110x90sourcepixels over4.355x3.563worldunits; drygrass12 samples320x320over3.975x3.975worldunits. These are real deficits, delegated for footprint correction.
