# Transverse Section Diagram Correction

Date: 2026-10-16  
Authorization: user requested correction with `$tdd`.  
Plan: [transverse-section-diagram-correction.md](../plans/transverse-section-diagram-correction.md)  
Investigation: [transverse-section-diagram-investigation-20261016.md](transverse-section-diagram-investigation-20261016.md)

## Result

The original K: transverse JSON now calculates and produces section-diagram PDFs through the production desktop calculation, projection and FramePrintPDF classes without the reproduced exception. Basic, PICKUP and COMBINE diagrams contain curves and annotations. The delivered basic+PICKUP PDF has 30 portrait pages, matching the reference's case order and two-panel organization.

Delivery, relative to repository root:

`.tmp/section-diagram-correction/verified-output/線路直角方向_応力図_修正版.pdf`

This is byte-identical to `base-and-pickup.pdf` in the same directory (309,482 bytes, SHA-256 `7d7fd56a40c24374294ad37a642ee12b9430d5ba1c2fd650b33b27771d71d9a9`). Exact visual/numerical reference parity is not claimed; limits are recorded below. No GUI clicking or physical printer run was performed.

## Causes and changes

| Cause | Correction |
|---|---|
| Legacy renderer converts canonical case key `1/Static/0` to an integer. | Explicit private canonical diagram path retains the full case/state identity and separate display captions. |
| Canonical member-distance stations are interpreted as input node IDs, omitting curves. | Draw continuous paths from original member coordinates and station distances. Preserve both segment endpoints at discontinuities. |
| Derived min/max lists and child result data are mishandled. | Read diagram-local result data and retain independent immutable envelope sides. |
| Piles/layout use the wrong display orientation and equal X/Y fit. | Y-up canonical projection, downward negative-Y piles, independent automatic fitting, and explicit manual scales. Untouched 2D diagram selections use a two-panel default. |
| Endpoint-only displacement geometry omits solved internal pile deformation. | Match internal stations to unique canonical topology nodes and use piecewise raw displacement geometry; convert labels separately to mm. |
| Pagination/caption state causes blanks, overlap and stale headings. | Correct occupied-page transitions, repeated headings and mixed multi-table/diagram boundaries. |
| Force offsets and dense labels differ from the supplied reference. | Correct M/V/N offset directions, black curves, blue oriented labels, meaningful endpoints/extrema/notice values, and proximity/orientation-aware duplicate suppression. |

Desktop changes are confined to print projection and print-dialog defaults. PDF changes are isolated behind the canonical diagram flag; legacy/unflagged and 3D routing remains available. Shared table payloads, canonical calculation output, exports, solver and legacy converter were not changed.

Moment curves use endpoint shear for Hermite interpolation. Derived intervals require matching source/combination provenance and consistency between moment change and the linear-shear integral; otherwise they connect existing stations directly. This avoids inventing a curved envelope from incompatible extrema. Internal station/node associations require unique coordinate/frame/length evidence using the producer's tolerance, not generated node ID parsing. Node label filtering does not remove displacement geometry endpoints.

The existing foundation print test fixture was completed with three missing internal nodes and displacement vectors. Existing assertions were retained; production validation was not relaxed.

## TDD and verification

The lead executed Red/Green gates through `.agents/skills/_shared/run_tests.py` and the existing `FrameWebforCS/tests/TransverseParity/test_native.py` bridge. Completed xUnit assertions and TRX observations establish native results; the single Python bridge test is not reported as the C# case count.

| Final native suite | Passed | Evidence under `.agents/logs/` |
|---|---:|---|
| PrintProjection.Tests | 109 | `diagram-projection-full-green4-valid-fixture.log` |
| PDF_Manager.Tests | 123 | `diagram-pdf-final-full.log` |
| PrintUi.Tests | 29 | `diagram-ui-full-verified.log` |
| **Total** | **261** | Zero failures or skips |

Focused new cases total 80 executions: projection 35, renderer 24, UI 21. Native coverage is not measured; no coverage percentage is claimed. The final label cycle first failed the intended assertion (23 passed, 1 failed), then passed in the final 123-case PDF suite. It verifies that all 20 strokes and distinct/distant/perpendicular labels survive suppression of nearby identical parallel text.

Intermediate infrastructure/fixture failures were not accepted as valid Red: an incomplete concurrent renderer build, invalid blank-name fixture, and incorrectly comma-separated UI expected-class configuration were corrected and rerun. One full PDF attempt encountered a transient existing WebView2 profile cleanup IOException; subsequent full runs passed, including the actual WebView2 navigation/print probe. The first full projection run exposed the incomplete foundation fixture described above; its completed fixture passed the full suite.

`dotnet build FrameWeb.sln` succeeded with **0 errors** and 2,739 warnings. Evidence: `.agents/logs/diagram-verified-build.log`. No warning-free or whole-repository product-suite claim is made. Independent review identified and verified fixes for multi-table pagination, filtered displacement endpoints and point-load shear discontinuities; no blocking findings remain in that reviewed scope.

Agent-infrastructure gate passed: `.agents/check.ps1 -AgentOnly` with the five authorized FramePrintPDF source/test paths explicitly supplied to `-AllowProductPath`; log `.agents/logs/diagram-agent-final-scoped.log`. The initial run with an empty allowlist failed scope isolation only; all its infrastructure tests/contracts passed. Both observations are retained. The correction plan's own `plan-doc` validation passes after completing its required headings, and final `git diff --check` passes. Product sources were unchanged after the final build/tests/PDF rendering.

## Fresh original-file acceptance

The fresh acceptance run reads the actual file:

`K:\レールウェイコンサルタント\26_ピット\計算書\02_pre\05_線路直角方向の計算\線路直角方向.json`

Pipeline: `CalculationRequestBuilder.FromSavedJson` -> `PythonCalculationRuntime.CalculateAsync` -> `AnalysisResultSetJson` -> derived presenter -> `PrintProjection.Build` -> `DirectPdfGenerator.Generate`.

All 10 cases solved with 67 topology nodes, 11 members and zero solver warnings. Fresh solve evidence: `.tmp/section-diagram-correction/acceptance-final/` and `.agents/logs/diagram-acceptance-final.log`. The final annotation-only correction rerendered those same print payloads with the latest renderer into `verified-output/`; log: `.agents/logs/diagram-verified-render.log`.

| Job | Pages | Bytes |
|---|---:|---:|
| Basic | 20 | 261,707 |
| PICKUP | 10 | 92,805 |
| COMBINE | 20 | 114,897 |
| Basic + PICKUP / delivery | 30 | 309,482 |

The final `verified-output/artifact-audit.json` reports every check true: all jobs succeeded; expected page counts; vector geometry on every page; exactly one title and case caption per page; no extracted text outside page bounds; unchanged shared root payloads for all four jobs; unchanged originals; and identical current request/canonical bytes to the investigation baseline.

| Protected artifact | SHA-256 |
|---|---|
| Original JSON | `0fa811b08228f32af2fdf9dcb2b4d255452653662a3d4b3d6d6c873de64255d4` |
| Original reference PDF | `27339edacd427c8f73a6458ffbb72101e49da728af0cdee8c038838c8c57335c` |
| Canonical result, unchanged from investigation | `e8ae4a8fc6f4bb7c81e31cb70c6d0fc1a59abc6f0180d5554b260dc4f3cedc27` |

Audit scripts: `.tmp/section-diagram-correction/audit_artifacts.py` and `inspect_diagrams.py`. Evidence directories are gitignored; preserve them if the generated PDFs and raster checks are needed later.

## Visual review and reference limits

The lead viewed all 30 final delivery pages in three contact sheets after the final annotation correction, plus final page 2 at full size. Before that annotation-only correction, the fresh acceptance PDF was also checked on all contact sheets and full pages 1, 2, 16, 20, 21 and 29. All reference pages were viewed in contact sheets, with full-page inspection including 1, 2, 20, 21 and 22. No empty output pages were observed; piles point down, force paths are continuous where appropriate, and case order/layout is consistent.

Remaining differences are explicit:

- Caption styling and annotation placement differ from the historical reference; its title is blue while the current title is black. Pixel equality is not an acceptance result.
- Reference page 20 contains CASE 10 axial force only. The selected current four-component output also includes its valid calculated displacement in the lower panel. The renderer does not hard-code a case-specific omission.
- This task did not perform a new all-label numerical/source parity audit against the historical PDF. Previously documented numerical/provenance differences remain separate. Values were not forced to match the reference; unchanged raw/shared output is independently checked.
- Hermite interpolation is applied only to the intervals justified above. An exact continuous envelope between unproved stations is not asserted.

## Closeout

The private canonical diagram boundary was recorded once in `.agents/docs/DESIGN.md` using the typed design writer. Existing foreign edits in `HANDOFF.md` and unrelated transverse parity plans/reports were preserved. Original external files were not overwritten; no upload, staging, commit or deployment was performed.
