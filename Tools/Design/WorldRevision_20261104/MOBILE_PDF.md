# Mobile review script handoff

`build_mobile_pdf.py` is prepared only. No PDF has been generated or visually accepted.

It creates exactly 18 phone pages: cover with internal navigation, seven surface region pages, the two relocated entrances, six pages covering twelve cave themes, actual jump/launch camera evidence, new-source 4K calculations, and QA/retained limits. Surface idle, cave idle and jump frames are labeled runtime; saved cave-mouth renders are labeled Editor. It embeds 31 image panels as compressed previews while leaving native captures untouched.

## Gates and configuration

Run `python Tools/Design/WorldRevision_20261104/build_mobile_pdf.py --check` for a read-only preflight. It checks every required image is a readable native 3840x2160 file, all 89 canonical underground room captures are present, relocation/density receipts exist, and final QA explicitly permits publication. Missing files are listed together.

By default `QA.json` must contain literal `passed: true`, `readyForPdf: true`, and a nonempty `checks` list (or dictionary). Each check group should contain `name`, explicit Boolean `passed`, and a concise `summary`. Supply at most six groups and keep summaries around 120 characters for phone readability. Use actual final receipt outcomes, not anticipated passes.

`--print-config` prints editable defaults. An optional `--config path.json` can change QA dotted-field bindings, chosen representative cave IDs, two jump panels, density/CSV paths, and `minimum_capture_mtime_utc` to reject stale frames. Set that timestamp after the final scene/art revision if all selected captures are recaptured together. `full_csv_url` may point at a shareable CSV companion URL; otherwise the link targets the source workspace CSV. The complete CSV, JSON density report and final QA are also embedded as attachments inside the PDF, so the underlying data travels with it.

The resolution page groups every final report exception by source category, with counts and minimum PPU. It does not discard near-threshold strict failures. The five retained approved Cradle placements are listed separately. No universal 4K pass is asserted, and no console/60 FPS certification is implied.

## Final authoring and verification

Root owns final QA and artifact generation. Immediately before the first `--build`, run the PDF skill's `mark_artifact_operation_started.mjs` command successfully for one new PDF. Then run:

`python Tools/Design/WorldRevision_20261104/build_mobile_pdf.py --build`

The output is `output/pdf/Qolossal_Terrain_Revision_Mobile.pdf`. The script checks 18 nonempty pages and three embedded evidence attachments. It also rejects text overflowing the phone page. These checks do not replace visual inspection: render every page with Poppler, inspect all page images for readability, overlap and correct captions, verify the navigation links and attachments, and only then deliver the PDF. No PDF artifact marker has been run by the script author.
