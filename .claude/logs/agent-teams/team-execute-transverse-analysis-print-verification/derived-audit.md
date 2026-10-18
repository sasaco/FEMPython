# Work Log: Derived arithmetic, topology, and unit audit

## Summary

Completed independent recomputation of every sample DEFINE, COMBINE, and PICKUP from the fresh unrounded canonical result without production imports. Arithmetic/selection and print projection pass; the input/station audit identifies a dropped notice station and a CASE 10 negative-zero relative-load conversion difference.

## Tasks Completed

- [x] Loaded the 10 repository rule files, STATE, DESIGN, INDEX, approved plan, shared work-log contract, and memory recall. Context report: missing `["PROGRESS.md"]`, unreadable `[]`, warnings `[]`, design.placeholder false, progress.entries 0. Executed the explicitly delegated owned workstream directly; no further agent or nested CLI.
- [x] Created independent private checker and complete JSON ledgers under `.tmp/transverse-analysis-print-verification/20261015T182603/audit/`.
- [x] Compared 124,200 all-six-component quantities across 20,700 correlated vectors, including 20,700 source IDs and 20,700 full provenance labels. No errors over arithmetic diagnostic threshold 1e-10; maximum absolute arithmetic difference 2.2737367544323206e-13.
- [x] Checked all 10 saved/request/canonical case IDs, original 12 node coordinates, 46 generated node interpolations, 11 member directions/local x axes, and 68 stations. Recorded four rigid boundaries, three requested notice points, and spring interval boundaries across all three sheets.
- [x] Recorded the missing member 5 notice point at distance 0.566; traced saved notice array omission through JsonObject-only optional section projection.
- [x] Preserved and compared all 460 discarded same-station side occurrences; maximum difference 8.881784197001252e-16, none above 1e-10.
- [x] Checked 21,360 projected components plus derived source/provenance labels and station positions; projection passes and performs no displacement scaling.
- [x] Traced unspecified canonical unit metadata, legacy kN/m/rad interpretation, unchanged raw print values under mm/mmrad headers, M/S/N component mapping, and explicit Python I/J canonical signs.
- [x] Recorded every effective CASE 10 load extent. Each 23.95-long pile has 23 effective patches, 22 original textual -0 continuation starts, all projected starts zero, maximum extent 2.2. Independently integrated scalar intensity 1322.410580000004 and first moment about I 675.7970625266707 per pile. No alternate solve or fixes.
- [x] Validated checker with known vector, numeric/source mutations, and coefficient mutation; all detect expected errors.

## Validation and Artifacts

Command: `uv run --project FrameWeb --locked --extra dev python .tmp/transverse-analysis-print-verification/20261015T182603/audit/checker.py` — exit 0. `arithmeticVerdict: PASS`; overall `verdict: DIFFERENCES` due missing saved feature. `derived-mismatches.json` is empty. This is independent arithmetic/input audit, not reference PDF equivalence.

Command: `& .agents/check.ps1 -AgentOnly` — exit 0, overall pass. Log: `.agents/logs/check-20261015T093553843Z-18584.log`. Product gates skipped as requested. No .NET build/process or solve was launched by this workstream.

Artifact entry point: `.tmp/transverse-analysis-print-verification/20261015T182603/audit/mapping-unit-audit.md`; machine summary: `summary.json`; executable checker: `checker.py`. Additional private ledgers: `node-mapping.json`, `station-mapping.json`, `feature-stations.json`, `same-station-sides.json`, `case10-load-extents.json`, and empty `derived-mismatches.json`.

## Communication with Teammates

- → analysis_harness: requested exact export schema and artifacts; clarified canonical snake_case versus derived camelCase.
- ← analysis_harness: received fresh single-solve export paths, all10 IDs, no skipped cases, unspecified units, and successful known-displacement smoke.
- → lead: reported early unit/sign/dedup risks, arithmetic totals, missing notice station, source evidence for textual -0 conversion, exact CASE 10 extents, and final artifact paths.
- ← lead: requested bounded read-only CASE 10 source triage and caution concerning historical reference input identity; incorporated both.
- → pdf_comparison: shared fresh-derived arithmetic correctness, missing notice mapping, unscaled print chain, and concrete CASE 10 input issue to distinguish reference differences from derived errors.

## Issues Encountered

- Initial checker assumed print page ID equals case ID. Corrected the audit to explicitly decode and validate `/Static/0`; no product change.
- One PowerShell inline Python quoting attempt failed before execution; replaced it with direct PowerShell JSON inspection. All durable checker execution uses a file.
- Canonical metadata has unspecified units. Report retains this limitation and labels any m/rad-to-mm/mRad example as conditional legacy interpretation.
- The historical input producing reference PDFs remains unproven. Code-proven current projection differences are stated separately; no corrected historical response is claimed.
