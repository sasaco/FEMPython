# Work Log: PDF Comparison

## Summary

Extracted every numeric row from all 64 reference pages and 124 freshly generated pages, retained semantic identities and visual coordinates, and compared printed tokens without numerical normalization. Differences are established for all three reports; coincident-side mapping ambiguities are retained explicitly.

## Tasks Completed

- [x] Loaded all 10 rule files, STATE, DESIGN, approved plan and INDEX; context reports missing `["PROGRESS.md"]`, unreadable `[]`, warnings `[]`, design.placeholder false, progress.entries 0.
- [x] Read PDF and linksee-memory skills; recalled relevant historical printing caveats. Used the assigned PDF comparison workstream directly, with no extra agent or nested CLI.
- [x] Extracted reference rows and coordinates: base 951, PICKUP 2850, displacement 240. All decimal and placeholder tokens accounted for.
- [x] Extracted current rows and coordinates: base 680, PICKUP 2040, displacement 840. All decimal and placeholder tokens accounted for.
- [x] Joined unique semantic station/node identities, preserved duplicate station sides, and reported missing/extra/ambiguous rows instead of forced side matches.
- [x] Compared every unambiguous M/S/N or displacement vector, PICKUP source ID / CASE formula, names, endpoint labels, and signed-zero/formatting tokens.
- [x] Proved the 600 COMBINE extra rows are six identical vectors per node (120 groups), not leaked generated nodes.
- [x] Ran six specific negative controls for numeric/source/missing/add/duplicate/coincident-side mutations; all passed.
- [x] Retained reproducible script, JSON/CSV differences, coverage, entity inventory, repetition audit, and detailed audit notes under the exclusive comparison directory.

## Files Modified

- `.tmp/transverse-analysis-print-verification/20261015T182603/comparison/compare_pdfs.py` and generated private evidence only.
- This resolved work log. No product source, input JSON, or reference/generated PDF was edited.

## Verification

- `uv run --project FrameWeb --locked --extra dev --with pymupdf python .tmp/transverse-analysis-print-verification/20261015T182603/comparison/compare_pdfs.py`: exit 0, unaccounted numeric tokens 0 for all six PDFs.
- All 6 comparer negative controls PASS with the expected specific difference category.
- Base reference/current row accounting: 951 = 300 paired + 291 missing + 360 ambiguous; 680 = 300 paired + 200 extra + 180 ambiguous.
- PICKUP accounting: 2850 = 900 paired + 870 missing + 1080 ambiguous; 2040 = 900 paired + 600 extra + 540 ambiguous.
- Displacement accounting: 240 paired reference rows and 840 current rows = 240 paired + 600 extra.
- `coverage.json`, `summary.json`, `row-differences.csv/json`, `audit-notes.md` contain complete evidence and limitations.
- Work-log document contract PASS; `.agents/check.ps1 -AgentOnly` overall PASS, no warnings, log `.agents/logs/check-20261015T093506963Z-6692.log`.

## Communication with Teammates

- → Lead: reference coverage, generated page counts, row accounting, largest differences, PICKUP provenance differences and page coordinates.
- ← Lead: frozen reference input paths and hash confirmation; all-page raster review is lead-owned.
- ↔ Analysis harness: fresh pipeline/output readiness, exact print projections, COMBINE six-mode interpretation; no duplicate build/analysis was run.
- → Derived audit: unique force differences, displacement scale/sign patterns, COMBINE formula formatting and provenance counts for independent attribution.

## Issues Encountered

- Reference page rotation is 90 degrees: resolved by applying `page.rotation_matrix` before coordinate grouping. No raw unrotated clipping conclusion was used.
- Current S station labels cannot identify reference coincident side labels: preserved ambiguous groups rather than forcing a match. This is a documented verification limitation and reported presentation/mapping difference.
- Current and reference column orders differ: mapped by semantic component, preserving actual tokens.
- Current COMBINE report repeats each vector six times with an altered source formula literal: primary vector compared and other 600 rows retained as extra; all repetitions independently checked identical.
- Large differences and missing reference stations remain findings; fixes and historic-input reconstruction are outside scope.
