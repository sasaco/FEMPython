## Implementation Plan: Transverse Analysis Print Verification

### Purpose
Recalculate the supplied transverse-direction model through the current FrameWebforCS application pipeline, generate its three requested reports, and compare all reference values, entity/case associations, provenance, and page presentation. Deliver an evidence-backed verification report; correcting discrepancies is a separate task.

The user confirmed this scope on 2026-10-15. This document is a verification plan, not a claim that the analysis or PDFs already match. The primary target is the current .NET 10 FrameWebforCS application and its shared Python/printing implementation.

### Scope
- New files after approval: a private verification harness (`Verification.csproj`, `Program.cs`), PDF inventory/comparison script (`compare_pdfs.py`), manifests, mapping tables, canonical/derived results, three generated PDFs, row differences, and page images under `.tmp/transverse-analysis-print-verification/{run-id}/`; durable findings in `.agents/docs/research/transverse-analysis-print-verification-report.md`.
- Modified files: this plan during review. No product source, dependency manifest/lock, sample JSON, or reference PDF changes are required. The verification harness must call existing production methods rather than reproduce their calculation or print logic.
- Source files to reuse: `FrameWebforCS/calculation/{CalculationRequest,PythonCalculationRuntime,CalculationResultJson,CalculationDerivedPresenter}.cs`, `FrameWebforCS/components/input/InputCombineService.cs`, `FrameWebforCS/providers/printing/PrintProjection.cs`, and `FramePrintPDF/PDF_Manager/DirectPdfGenerator.cs`. Use `FrameWebforCS/Headless/HeadlessCalculation.cs` as the complete pipeline example.
- Dependencies: Windows x64, .NET 10 Desktop runtime/SDK, repository-managed `FrameWeb/.venv`, current desktop/PDF project references and installed Japanese print fonts. PDF inspection can use ephemeral `uv run --project FrameWeb --locked --extra dev --with pymupdf python ...`; this command was checked successfully without changing project dependencies. Run from the repository root in PowerShell and keep Japanese script files UTF-8.
- Out of scope: discrepancy fixes, solver/aggregation semantics changes, new production CLI options, Angular/converter changes, dependency upgrades, deployment, physical printing, and structural design certification.

#### Inputs and confirmed preflight

All four supplied files were accessible. Root: `K:\レールウェイコンサルタント\26_ピット\計算書`.

| Role | Relative path | Bytes | SHA-256 |
|---|---|---:|---|
| Saved input | `02_pre\05_線路直角方向の計算\線路直角方向.json` | 44097 | `c2798b3c8aa687f7b2220aa57f31dd6ad91f9f66a41685edae450386b7ae6553` |
| Section forces | `03_analysis\05_線路直角方向の計算\基本.pdf` | 39816 | `eff765fa119b2ab25f982355d9a77741a8fe3e1591b765e842bb217d38246727` |
| PICKUP section forces | `03_analysis\05_線路直角方向の計算\PICK UP.pdf` | 109457 | `6aed77f12d8d60ec8abc04ba79279d48966e84b7abe2157c2e66e6f6eeb438b8` |
| Base and COMBINE displacement | `03_analysis\05_線路直角方向の計算\変位量.pdf` | 16167 | `24d840047de627d0baa3f09776ff41ed4e8b30a52703e910e659f71c8049bf49` |

Preflight HEAD: `08aac8fd4b64b8513047adc575d87893a38fe818`; initial working tree was clean. The JSON has dimension 2, 12 input nodes, 11 members, 4 rigid-zone records, 3 notice-point records, 10 saved load cases, 10 DEFINEs, 10 COMBINEs, and 5 PICKUPs. These input counts are not assertions about solver-generated nodes or analyzed cases.

| Reference | Pages | Confirmed content |
|---|---:|---|
| `基本.pdf` | 15 | Basic CASE 1–10 in paired columns; M/S/N, DIST, I-TAN/J-TAN, rigid boundaries, notice points, and an M-MAX row |
| `PICK UP.pdf` | 45 | PICKUP 1–5, MOMENT/SHARE/AXIAL focus, signed maximum/minimum correlated M/S/N and COM:CASE source labels |
| `変位量.pdf` | 4 | Pages 1–2: CASE 1–10; pages 3–4: MIX 1–10; 12 nodes per case; X/Y displacement in mm and rotation in mRad |

All reference page rectangles are 842 × 595 pt (A4 landscape); the producer is Acrobat Distiller 5.0.5. Force and DIST displays use 2 and 3 decimal places, and displacement uses 4. The JSON was modified on October 15, after the October 14 PDFs: historical input identity is unproven. Reference inventory and first-page images were inspected during planning and are temporarily available in `.tmp/transverse-analysis-print-verification/preflight/`.

#### Current implementation evidence

- `FrameWebforCS/Headless/HeadlessCalculation.cs:27`–`:63` illustrates derived-input parsing, saved-document request projection, Python analysis, strict result parsing, and derived presentation. `Headless/README.md:61` documents fresh recalculation; `Headless/tests/RunnerTests.cs:74` covers stale saved results.
- `FrameWebforCS/components/menu/MenuComponent.cs:230` and `components/print/PrintDialogForm.cs:326` use the shared application calculation/printing path. `calculation/PythonCalculationRuntime.cs:233` invokes `build_analysis_result_set`.
- `Headless/RunOptions.cs:14` and `:40` do not accept COMBINE displacement; `HeadlessCalculation.cs:94` maps its limited selection. Therefore the existing CLI alone cannot cover this request.
- `providers/printing/PrintProjection.cs:13`, `:26`, and `:550` provide `CombinedDisplacement`, explicit paper/orientation/entity selection, and `disgCombine` projection. Defaults are A4 portrait; use `PrintPaper.A4` and `PrintOrientation.Horizontal` explicitly. The private harness can access existing internal APIs through the already declared `FrameWebforCS.Tests` friend assembly (`FrameWebforCS.csproj:33`).
- `FrameWeb/src/fem/result_topology.py:113` produces generated station/node identities. `result_projection.py:192` applies endpoint sign rules. `CalculationDerivedPresenter.cs:265` and `PrintProjection.cs:413` deduplicate stations, so discontinuity sides and row multiplicity need explicit checks.
- `CalculationDerivedPresenter.cs:138`, `:180`, and `:234` implement DEFINE, COMBINE, and PICKUP; strict extrema comparison preserves first candidate on ties. `PrintProjection.cs:618` emits source IDs and provenance. Printed numeric formatting is exercised by `FramePrintPDF/PDF_Manager.Tests/PrintNumericFormatOracleTests.cs` and `DisplacementNodeIdLayoutTests.cs`.

### Implementation Steps

Execute the following stages in order after approval. Each stage retains its evidence for the next stage and has an explicit completion check.

#### Step 1: Freeze evidence and build the reference inventory
- [x] Recheck git status/relevant diffs and record current HEAD, dirty-path hashes, SDK/Python versions, culture, and font environment. Create a unique private run directory; never reuse a previous run's outputs.
- [x] Rehash the four external files and compare with the planning hashes. If changed, record a new baseline explicitly and reassess the inventory; do not silently continue using the previous hashes. Capture modification/PDF creation timestamps without treating timestamps as proof of input equivalence.
- [x] Extract text with coordinates from every reference page. Inventory report/case/COMBINE/PICKUP/focus IDs, member/node IDs, station labels/distances, units, printed precision, column positions, and row counts. Retain original page/column/bounding-box evidence for every parsed row.
- [x] Establish a parser coverage ledger: parsed data rows, recognized headers/footers/non-numeric placeholders, and unresolved tokens/rows. Preserve repeated rows, signed zero, M-MAX, I/J labels, and discontinuity sides. Visually check column parsing against each distinct template and page transition.
**Verification**: Exactly 15/45/4 reference pages are inventoried under the recorded hashes. Base/COMBINE displacement each covers 10 cases × 12 nodes × 3 displayed components. All numeric rows are accounted for; unresolved extraction prevents a complete-match verdict.

#### Step 2: Prove the fresh application pipeline and entity mapping
- [x] Create a private `net10.0-windows` x64 console harness with `UseWindowsForms=true`, assembly name `FrameWebforCS.Tests`, and a project reference to the desktop project. Keep its executable beneath the repository so existing Python-environment discovery works. Isolate the output so it cannot collide with the actual test assembly.
- [x] Before running the client model, smoke-test the harness with the existing small known-displacement fixture used by `Headless/tests/RunnerTests.cs`. Confirm its independently known axial displacement and PDF output through the same shared path; an executed process or nonempty PDF alone is insufficient.
- [x] Call `CalculationRequestBuilder.FromSavedJson`, the shared Python runtime, `AnalysisResultSetJson.Deserialize`, `InputCombineService.ParseCombineJson`, `CalculationDerivedInputSnapshot.Capture`, and `CalculationDerivedPresenter.Build`; retain the resulting `CalculationResultPresentation`. Read the supplied JSON once and perform one fresh solve for all three reports. Dispose the runtime and report stage failures explicitly.
- [x] Persist the actual solver request, canonical AnalysisResultSet, derived definitions/combinations/pickups, skipped-case list and diagnostics. Distinguish 12 input nodes from generated topology nodes; map original member/station location, I/J direction, rigid boundaries, notice points, spring subdivisions, and any coincident discontinuity sides.
- [x] Audit 10 saved case IDs against projected/analyzed IDs. Explain every omission through the actual request rules and flag any absent reference case; do not synthesize zeros. Verify DEFINE/COMBINE references and all five PICKUP candidate sets.
- [x] Build an explicit unit/axis table from saved input, result metadata, shared print projection, and reference labels. Confirm how translations and rotations reach mm/mRad and M/S/N; do not infer undeclared raw units or apply a compensating multiplier merely to match the reference.
**Verification**: A real calculation succeeds with strict canonical parsing; every analyzed/omitted case is explained, every referenced original node/member maps unambiguously or is flagged, and no saved result is substituted. Run the existing stale-result regression in `Headless/tests/RunnerTests.cs` as a targeted smoke; inability to calculate is reported as blocked verification with its failing stage.

#### Step 3: Generate the three reports through the existing PDF path
- [x] Construct three `PrintSelection`s from the same fresh presentation: `[SectionForce]`, `[PickupSectionForce]`, and `[Displacement, CombinedDisplacement]`. Explicitly set Japanese language, title `線路直角方向`, A4 landscape, and record layout/scale/component selections.
- [x] Use original 12 node IDs for the reference displacement scope after Step 2 mapping. Record the unfiltered entity inventory and justify every output filter by the requested reference scope; never remove an input case, member, reference station, or discrepancy to force agreement. Keep generated topology and unfiltered results in evidence.
- [x] Call `PrintSnapshot` → `PrintProjection.Build` → `DirectPdfGenerator.Generate` for each selection and retain each projected print JSON and exact PDF bytes. Save privately as `section-force.pdf`, `pickup-section-force.pdf`, and `displacement-and-combine.pdf`.
- [x] Verify PDF signatures, readable page trees, section/case presence, no empty report, and correspondence between the shared fresh result, projected rows, and generated PDF. Record actual page geometry/count without forcing reference pagination by editing output or templates.
**Verification**: Three real product PDFs are generated; the third contains both basic and COMBINE displacement with no PICKUP displacement. Each uses the same calculation identity and explicit settings; hashes and byte counts are in the manifest. Page/content differences remain reportable findings.

#### Step 4: Compare every printed value and independently check derived results
- [x] Parse all generated PDF pages using the same coverage contract as the references. Join semantic row multisets by report/case or PICKUP/focus/extremum, original node/member, station identity/distance/side, and component. Trace all joins to page/column/row; report missing, extra, duplicate, ambiguous, and unsupported rows.
- [x] Compare base M/S/N and displacement displays, signs, station positions, IDs/names and component order. Compare PICKUP M/S/N as correlated vectors from the selected max/min COMBINE, including both COMBINE IDs and constituent-case/provenance labels. Treat the reference's extra M-MAX rows explicitly rather than dropping them.
- [x] Compare printed decimal tokens first. Use rounding intervals at the reference's precision only to explain raw-versus-printed differences (half of 0.01 force, 0.001 DIST, or 0.0001 mm/mRad display unit with a documented floating-point epsilon); do not use a broad relative tolerance to approve different printed values. Record signed-zero/formatting differences separately.
- [x] Independently recompute this sample's single-case DEFINEs and weighted COMBINE vectors from unrounded fresh base results, then independently choose PICKUP signed extrema and correlated vectors. Check all 10 COMBINEs and all 5 PICKUPs; do not sum rounded reference cells or call the production derived presenter as its own oracle. Record ties and source-selection differences.
- [x] Distinguish fresh-solver/reference differences, derived aggregation differences, print-projection/display differences, mapping/parser gaps, and historical input uncertainty using the saved intermediate stages. A mismatch does not by itself establish a code defect.
- [x] Prove the comparer detects failures using private extracted-row copies: change one numeric value and one source ID, remove/add/duplicate a row, and retain coincident stations on opposite sides. Require the expected specific differences without touching reference or generated PDFs.
**Verification**: Produce row-level CSV/JSON with compared counts, exact display matches, numeric differences, missing/extra/ambiguous rows, source mismatches, and examples linked to original evidence. All reference numeric rows and all generated numeric rows are accounted for. Any gap blocks an unqualified match statement.

#### Step 5: Compare page presentation and publish the verification result
- [x] Rasterize every reference/generated page at the same fixed resolution. Match report content before page alignment, especially if pagination differs. Review all pages for blank pages, clipping, overlaps, missing glyphs, unreadable values, and lost rows; inspect side-by-side each template plus every numeric/content/layout discrepancy page.
- [x] Report page sizes/counts, column/table composition, headings/units, row order, line/page breaks, source labels and fonts. Use overlays/crops as evidence where useful; no arbitrary pixel threshold or automatic font/pagination waiver is allowed. PDF metadata/byte differences alone do not prove a content mismatch.
- [x] Publish `.agents/docs/research/transverse-analysis-print-verification-report.md` with one result per reference: complete match, differences found, or verification incomplete. State numeric, mapping/provenance, and presentation outcomes separately, include totals and largest discrepancies, and identify any remaining input-history or extraction uncertainty.
- [x] Link the report to retained manifests, raw request/results/projections, generated PDFs, all row differences and relevant page images. Rehash the external originals and confirm they are unchanged; inspect final git status/diff and run the agent-infrastructure gate for the added documentation.
**Verification**: The durable report covers all three requested PDFs with reproducible commands and complete evidence. A full-match verdict requires complete row coverage, matching values and associations, and matching presentation within explicitly accepted tolerances; otherwise describe the actual differences or incomplete stage. Repairs are proposed separately.

#### Relevant commands and gates

```powershell
# Existing targeted coverage; select relevant methods rather than rerunning unrelated suites.
dotnet build FrameWebforCS/Headless/FrameWebforCS.Headless.csproj -c Release
dotnet test FrameWebforCS/Headless/tests/FrameWebforCS.Headless.Tests.csproj -c Release --filter FullyQualifiedName~RunnerTests
dotnet test FrameWebforCS/tests/PrintProjection.Tests/PrintProjection.Tests.csproj -c Release
dotnet test FramePrintPDF/PDF_Manager.Tests/PDF_Manager.Tests.csproj -c Release --filter "FullyQualifiedName~PrintNumericFormatOracleTests|FullyQualifiedName~DisplacementNodeIdLayoutTests|FullyQualifiedName~PrintLibraryTests"
# Use if station mapping/sign behavior needs clarification; no solver changes are planned.
uv --directory FrameWeb run --locked --extra dev python -m pytest tests/integration/test_result_projection.py -q
& .agents/check.ps1 -AgentOnly
```

The private harness and comparer must record their exact build/run commands in the run manifest once created. Build or test failures are reported separately from sample/reference mismatches. Solution tests alone do not cover the separate Headless and focused PrintProjection test projects.

### Risks & Considerations
- The latest JSON may differ materially from the input that produced the PDFs. Preserve the supplied version and compare it as requested; explain mismatches without asserting historic equivalence or silently restoring older files.
- Existing CLI options cannot select COMBINE displacement or all required print settings. A private friend-assembly harness uses existing APIs without widening product scope; a small known-case smoke and shared-pipeline trace guard against a harness-only result.
- Generated topology IDs, rigid zones, spring intervals, coincident station sides, and legacy M-MAX rows can cause omissions or ambiguous joins. Preserve row multiplicity and explicitly report unresolved mappings.
- A legacy PDF can contain rounded values only. Printed agreement cannot certify equal unrounded solver values. Raw independent derived checks and display comparisons answer separate questions.
- Current projection deduplication can discard same-station sides; the verifier must expose the difference rather than normalize it away. PICKUP must retain correlated vectors and provenance, including same-value ties.
- Reference PDFs use a legacy composer. A4 landscape settings can match paper geometry but do not guarantee identical typography or pagination. Record differences; do not modify product layout in this task.
- Machine culture and installed Japanese fonts affect rendering/formatting. Record and fix the verification environment, and distinguish environment failures from solver/print discrepancies.
- K: may be unavailable or files may change between runs. Stop dependent comparison on changed/missing inputs until a new baseline is explicitly recorded. Keep original hashes and all diagnostics.
- Client documents and detailed result artifacts stay in the private evidence directory; durable documentation should contain only the minimum evidence needed and links. Do not upload files or send them to third parties.
- Repository `PROGRESS.md` is absent. Existing historical state/design sections contain superseded architectures; current source and the later pythonnet/direct-PDF decisions govern this verification.

### Open Questions
- No additional purpose/scope answer is required: the user confirmed verification of values, associations, and presentation, with fixes separate. The user approved execution on 2026-10-15; verification was executed in run `20261015T182603`. See [the verification report](../research/transverse-analysis-print-verification-report.md) for differences and remaining mapping limits.
- The historical input/version used for the October 14 PDFs is unknown. This does not block comparison of the supplied files; it limits causal attribution if differences are found.
- No display/layout tolerance has been authorized. Report all observed differences, including rounding, fonts and pagination; do not waive them. If there are differences, obtain any acceptance decision against the concrete verification report.

