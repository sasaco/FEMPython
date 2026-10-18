## Implementation Plan: Transverse Section Diagram Correction

### Purpose

Repair the observed basic diagram PDF exception and missing PICKUP/COMBINE force curves, then reproduce the supplied 30-page portrait diagram organization with readable force/displacement diagrams. User authorized implementation with `$tdd` on 2026-10-16.

### Scope

- Desktop private diagram projection in `FrameWebforCS/providers/printing/` and focused projection tests.
- PDF diagram interpretation, coordinates, paths, headings, labels and page lifecycle in `FramePrintPDF/PDF_Manager/`, with real PDF regression tests.
- Desktop 2D section-diagram layout/automatic-scale defaults and focused UI tests; preserve manual choices. Complete the existing foundation projection test fixture with its missing internal topology nodes/displacements, keeping its assertions unchanged.
- Preserve canonical analysis values, table/export contracts, external input/reference files, and all preexisting dirty documents. No solver or converter changes.
- Investigation: `../research/transverse-section-diagram-investigation-20261016.md`; baseline evidence `.tmp/section-diagram-investigation/20261016T180325/`.
- Current original input SHA-256: `0fa811b08228f32af2fdf9dcb2b4d255452653662a3d4b3d6d6c873de64255d4`; reference: `27339edacd427c8f73a6458ffbb72101e49da728af0cdee8c038838c8c57335c`.

### Implementation Steps

The four steps below cover the private projection contract, rendering, layout and actual-file acceptance. Checked items have recorded test or artifact evidence.

#### Step 1: Diagram projection contract

- [x] Base/combined/pickup diagram-only metadata retains full case/state IDs and friendly case labels.
- [x] Canonical member/station rows remain unchanged in shared table payloads; renderer receives an explicit canonical-position convention.
- [x] Base displacement diagram receives raw model-unit translations and rad rotations; labels have an explicit conversion to mm. Cover m/mm/cm and unspecified legacy metres.
- [x] Multiple state identities, arbitrary string IDs, selection filters, 3D and table-only compatibility remain valid.

#### Step 2: Force and displacement rendering

- [x] Real PDF generation handles composite case keys and original/interior/end stations without exceptions.
- [x] Continuous member paths are drawn from member distance; both envelope sides remain separate and source lists are unchanged.
- [x] Zero-force diagrams remain finite; table-only duplicate occurrences are excluded; visible force labels/curves are asserted from rendered PDF content.
- [x] Raw displacement interpolation and mm labels agree without a second unit/sign conversion.

#### Step 3: Layout and page lifecycle

- [x] Negative-Y piles point downward; explicit scales are honored and default independent fitting yields a broad beam like the supplied reference.
- [x] Portrait two-panel jobs produce two pages per four-component basic case and per three-component PICKUP case, with no extra blank pages.
- [x] Title, case and component captions repeat correctly without collisions or stale positions.
- [x] Endpoint/extremum/notice labels are readable, duplicate labels are suppressed, and annotation placement stays inside the page.
- [x] Legacy/unflagged, landscape, 3D and mixed table+diagram jobs preserve their contracts.

#### Step 4: Real-file acceptance and closeout

- [x] Fresh current-input production calculation succeeds for all 10 cases; combined basic+PICKUP diagram PDF is generated.
- [x] Inspect all 30 output/reference pages in contact sheets and selected pages at full size for content, direction, geometry, labels, colors, case order and empty pages. Record reference differences without altering results; this is not an all-label numerical parity audit.
- [x] Lead reruns relevant native suites through `run_tests.py`, reviews all diffs, runs relevant .NET build and agent gates, and obtains independent review.
- [x] External hashes and raw/shared output preservation verified; save corrected PDF in a new local evidence/output location and record remaining limits.

### Verification

Strict Red/Green runs use the existing `FrameWebforCS/tests/TransverseParity/test_native.py` bridge through `.agents/skills/_shared/run_tests.py`. The bridge validates completed native xUnit assertions/TRX; build/runtime/collection failures are not accepted as Red. Lead runs every gate and releases each worker to implement only after a valid Red. The project has no native coverage threshold; Python bridge coverage does not measure C# product coverage, so no invented percentage is reported.

Ownership: projection worker owns desktop projection and projection tests; renderer worker owns PDF renderer/lifecycle and new PDF tests; UI worker owns print-dialog defaults and UI tests and independently reviews integration; lead owns this plan, evidence, integration checks and final report. Builds/tests are serialized. Existing dirty `HANDOFF.md` and unrelated parity reports/plans are preserved.

### Progress

- Investigation reproduced the production exception and empty-curve outputs; authorized implementation started.
- Loaded context: 10 rule files, STATE and DESIGN retained from investigation; missing `PROGRESS.md`, no unreadable files or warnings.
- Initial private contract: `diagramResult.canonicalStations`, `yUp`, `referenceLayout`, `caseLabels`, `displacementLabelScale`; table and canonical data retain their existing identity/values.
- First valid Red/Green cycles: projection 7 native cases and PDF renderer 7 native cases passed. UI cycle 2 passed all 21 native cases, including manual settings and legacy defaults. All observations were made by the lead through the validated native bridge. Transient build failures and an invalid blank-name fixture were rejected as Red evidence and corrected separately.
- First real-file acceptance generated all four jobs successfully (basic, PICKUP, COMBINE, basic+PICKUP). Combined basic+PICKUP is 30 pages, all pages have vector paths and force labels, and no text lies outside the page. Fresh canonical JSON is byte-identical to investigation output (SHA-256 `e8ae4a8fc6f4bb7c81e31cb70c6d0fc1a59abc6f0180d5554b260dc4f3cedc27`). Evidence: `.tmp/section-diagram-correction/acceptance-first/`.
- Visual comparison found PICKUP label crowding and the axial force offset side opposite to the supplied reference; add focused regressions before corrections. Preserve signed axial values and both envelope curves.
- Independent review found three additional correctness risks in the new rendering path: multi-table pagination can reset the content flag and overlay a diagram; node filtering can remove a geometric endpoint and silently suppress deformation; station deduplication can discard a point-load shear jump and distort Hermite moment interpolation. Diagram-local geometry must preserve one-sided segment endpoints while shared table values remain unchanged.
- Actual solved internal pile displacements show that whole-member endpoint interpolation is insufficient (CASE 8/10). Add a verified private station/node association and piecewise raw displacement geometry, without changing solver or public result schema.
- Verified completion: all three review findings are corrected with regression tests. Interior displacement stations require unique canonical topology matches at the producer's coordinate tolerance; generated ID spelling is never used as evidence. Node selections affect annotations independently of geometric endpoints.
- Derived moment interpolation uses Hermite slopes only when provenance agrees and the endpoint moment change matches the trapezoidal shear integral. Unproved intervals retain straight station connections. Basic one-sided segment endpoints remain available, preserving point-load shear jumps.
- Final TDD gates: projection 109, PDF 123 and UI 29 native cases passed (261 total, zero failures/skips). The last label Red had one expected failure; Green preserves all 20 diagram strokes while suppressing only nearby identical parallel labels. Solution build passed with 0 errors; warnings remain.
- Final fresh calculation read the original K: input directly: all 10 cases, 67 nodes, 11 members, zero solver warnings. All four PDF jobs succeeded: basic 20, PICKUP 10, COMBINE 20, basic+PICKUP 30 pages. Final renderer output is in `.tmp/section-diagram-correction/verified-output/`; artifact audit passes all checks.
- Final delivery: `verified-output/線路直角方向_応力図_修正版.pdf`. All 30 final pages were inspected in contact sheets after the last label correction, with page 2 also inspected at full size. Earlier final-calculation full-page checks covered 1, 2, 16, 20, 21 and 29. Report: [transverse-section-diagram-correction-20261016.md](../research/transverse-section-diagram-correction-20261016.md).
- ✅ Closeout verified: `check.ps1 -AgentOnly` passes with the five authorized PDF source/test files explicitly allowlisted (`.agents/logs/diagram-agent-final-scoped.log`). The initial unscoped run passed all infrastructure tests/contracts but failed scope isolation because its allowlist was empty; no permission or source-policy relaxation was made. This plan's own document contract and `git diff --check` pass.

### Risks & Considerations

- Historical numerical/reference differences are distinct from renderer defects; do not force values, extrema ties or source choices to match a PDF.
- The reference uses different X/Y display scales, whose historical settings are unavailable; generic fit policy and explicit scale overrides need visual verification.
- Native coverage is not provided by the existing test projects. Executed cases and actual PDF evidence will be reported explicitly.
- Exact reference parity is not claimed. Caption styling and label placement differ; reference page 20 has axial force only, while the selected four-component current output also includes valid calculated CASE 10 displacement. Historical numerical/source differences remain separate from this rendering correction.

### Open Questions

No unresolved issue blocks the reproduced exception/curve correction. The historical reference's original generation settings and numerical/source provenance remain unverified; the completion report states the resulting limits without assuming them.
