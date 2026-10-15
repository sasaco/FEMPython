# Transverse Analysis and Print Corrections

Date: 2026-10-15  
Baseline HEAD: `08aac8fd4b64b8513047adc575d87893a38fe818`  
Status: code-proven corrections verified; **exact reference parity remains incomplete**.

## Authoritative inputs and outputs

The user approved implementation, selected the UPDATED K: PDFs, and confirmed identical
CASE 10 pile pressure distributions. Private evidence root (`RUN/` below):
`.tmp/transverse-analysis-print-corrections/20261015T193610/`. Preserve this gitignored directory and the original verification run.

- [Corrected input COPY](../../../.tmp/transverse-analysis-print-corrections/20261015T193610/residual-investigation/corrected-input-row-labels-layout.json)
- [Basic force PDF](../../../.tmp/transverse-analysis-print-corrections/20261015T193610/stages/final-9/output/section-force.pdf)
- [PICKUP force PDF](../../../.tmp/transverse-analysis-print-corrections/20261015T193610/stages/final-9/output/pickup-section-force.pdf)
- [Displacement + MIX PDF](../../../.tmp/transverse-analysis-print-corrections/20261015T193610/stages/final-9/output/displacement-and-combine.pdf)
- [Final manifest](../../../.tmp/transverse-analysis-print-corrections/20261015T193610/manifest.json)
- [Maintained implementation plan](../plans/transverse-analysis-print-corrections.md)

Original JSON SHA256: `c2798b3c8aa687f7b2220aa57f31dd6ad91f9f66a41685edae450386b7ae6553`.
Corrected COPY SHA256: `60dcb754088a58f2008696e76b982c787a61592fdd8fba426d47d40fc5751ad3`.
Exactly 11 CASE10 payload row IDs and 11 corresponding layout references changed.
Pressure values, array order, slots, other cases and all other input data are preserved.
Actual InputLoadService save/clear/reload and independent successor review passed.
The original K: JSON was not modified; no production automatic symmetry rule was added.

The PDFs changed externally during work. Both versions are retained. The user-selected
updated snapshots under `RUN/reference-refresh/snapshot/` are portrait A4, 30/60/2 pages.
Updated PICKUP has 29 changed numerical components among unambiguous old/new pairs;
it is not merely repaginated. The superseded M-MAX row is absent from the updated target.

## Implemented corrections and boundaries

- Notice arrays are retained and validated. All three sample records, including
  member5 at0.566, reach request, canonical topology and real PDF output.
- Lexical distributed `"-0"` continuation is preserved in request and independent
  load-display paths. Zero aliases, gaps, member changes, point/distributed/temperature
  marks, moving resets and malformed input have focused regressions.
- Distances now quantize to integer mm at the actual Angular stages BEFORE carry.
  Numeric raw-L2 carry stays separate from physical distributed-end carry; signed
  point rounding, absolute physical spans and negative clipping retain their contracts.
  Negative sub-half-mm relative lengths retain their original meaning when rounded
  to zero; literal `"-0"` stays distinct. An internal zero-span marker is cleared from
  cloned user input before derivation and removed before request emission.
- Physical print m/mm/cm and rad become mm/mrad at print projection only; unspecified
  legacy length uses the documented metre fallback and unknown physical units fail.
  Modal shapes remain unscaled; 3D signs are preserved. Explicit 2D Mz/Rz transforms
  remap reversed derived modes before filters and transform moving candidates before
  extrema selection, retaining correlated vectors, sources and strict first ties.
- Semantic ordinary-combination metadata checks all contributing DEFINE/LL sources
  and complete unfiltered vectors. Genuine envelopes/filters remain available.
  Proven weighted expressions remain text and preserve raw provenance. Canonical
  schemas, raw .pik/CSV values and global numeric-string formatting remain unchanged.
- The private 2D composer honors portrait/landscape orientation. Basic portrait
  flows retain whole members with a short-side-bounded viewport (an explicit inferred
  presentation policy); PICKUP uses six sequential focus/extremum flows; displacement
  pairs cases/MIX panels. Wrapped/repeated captions, impossible captions, oversized
  members, short IDs, filtered MIN continuations and atomic numeric coefficients
  have parsed-PDF regressions. Reference caption collisions are not reproduced.
- Explicit foundation interval joins gain two TABLE occurrences only after identical
  active-sheet join sets, EXACT declared-decimal/canonical station positions and EXACT
  equality of all six adjacent endpoint components in EVERY static case are proved.
  Unchanged-stiffness joins are included; unsupported/missing/unequal/mixed/3D/modal/LL
  proof falls back to unique rows with diagnostics. Canonical n/vectors/sources stay
  intact; optional occurrence1/2 distinguishes table copies. Notice/rigid labels stay
  protected by their existing label association contract, never by relaxed join proof.
- Shared 2D diagram rendering filters ONLY explicitly added occurrence>1 before
  scaling/drawing. Actual base/derived parsers and the renderer list boundary retain
  legacy unmarked repeats, discontinuities, both extrema and table lists. This does
  not establish general canonical diagram rendering correctness; see limits below.

Product changes stay inside FrameWebforCS and FramePrintPDF. Backend, Angular,
converter, dependency manifests and lockfiles were not changed.

## Complete updated-reference comparison

Final source-stable shared run: `RUN/stages/final-9/output/`; all10 cases solved once,
no solver warnings, 67 nodes/11 members, PDFs30/60/2 pages.
The rounding correction removes two spurious load-boundary nodes. Foundation proof
adds18 table occurrences per case, producing950/2850 force rows without row deletion.

| Report | Reference / product rows | Numeric vectors equal / paired | Differing numerical components | Missing / extra / ambiguous product rows |
|---|---:|---:|---:|---:|
| Basic forces | 950 / 950 | 904 / 950 | 56 | 0 / 0 / 0 |
| PICKUP forces | 2850 / 2850 | 2470 / 2850 | 553 | 0 / 0 / 0 |
| Displacement + MIX | 240 / 240 | 173 / 240 | 122 | 0 / 0 / 0 |

All 4040 rows have a complete association. 180 basic and540 PICKUP coincident groups
are associated only when multiplicities match and BOTH sides have complete unique
identical printed-label sets. Neither values nor sources choose partners. An explicit
resolution ledger and six adversarial controls are retained; historical ambiguity
reports remain intact. Label changes/reordering, missing/duplicate occurrences and
numeric/source mutations cannot silently improve matching.

All3800/14793/732 generated numeric tokens are accounted for, with zero unresolved
tokens. Five actual-reference parser, four actual-generated parser, six original
comparator and six label-association controls pass. Source hashes, exact parser inputs,
partitions and all differences are in `RUN/reference-refresh/comparison-final-9/`.
Do not confuse the increased paired denominator with numerical regression versus7.

Exact whole-row parity remains0/0/94. Missing source names affect all950/2850 force
rows; the JSON has no authoritative member/section names. Weighted coefficient
expressions intentionally remain correct even where the reference prints unweighted
membership. Selected source IDs also differ on215 PICKUP rows. Residual numerical,
source and formatting differences are **not waived or explained solely by historical
precision**. See final comparison residual ledgers for case/component magnitudes.

## Independent audits and reviews

Lead independently replayed `audit/checker.py --require-foundation-occurrences`:
PASS 140,400 components, zero input/mapping/projection/arithmetic/
occurrence errors; maximum arithmetic delta 1.1368683772161603e-13. Exact stored-PICKUP
full-vector/source/provenance checks pass AFTER independent upstream arithmetic.
No tolerance was loosened. Current fsum-selection diagnostics: 0.
The prior7 near-zero five-ULP oracle disagreement remains retained historically.
The independent occurrence auditor derives geometry and all six raw-side proofs
without trusting claimed metadata, checks counts/IDs/order/labels/clones/table modes,
and rejects missing/invented rows, false continuity and source/provenance mutation.
Near-notice matching is independently implemented for labels only.

Independent input, projection, composition, corrected-input, rounding and foundation
reviews fixed/rechecked concrete defects, including zero aliases, child-infrastructure
false Red, impossible captions, ID width, layout references, numeric-token wrapping,
sub-half-mm negative spans and internal-marker collision. No remaining actionable
finding from these bounded corrections is claimed to resolve the external residuals.

## Verification and cleanup

| Lead gate | Log label | Native passed |
|---|---|---:|
| Input/request/display | `transverse-lead-input-marker-final` | 78 |
| Projection/metadata/exports | `transverse-lead-projection-full-final9` | 73 |
| PDF composition and diagram occurrence boundary | `transverse-lead-pdf-full-final9` | 85 |
| Headless process/artifacts | `transverse-lead-headless-occurrence-final` | 1 |
| Total selected executions | | **237** |

All selected native tests executed with zero failures/skips. Coverage is **unmeasured**;
the full repository matrix is not claimed. Exact filters, named TRX, commands and
cleanup are under `.tmp/transverse-analysis-print-corrections/native/<label>-*/`;
bridge pytest success alone is not used as the native count. The final solution build
and AgentOnly/ownership payloads follow below. Native infrastructure controls reject
zero/skipped/build/collection/runtime/timeout failures as product Red.

All92 final pages were rasterized. Independent visual review inspected all24 contact
sheets and seven original-resolution pages; lead inspected basic30, PICKUP59 and MIX2.
Zero extracted words lie outside page bounds. Most pages received contact-sheet
inspection only. Final9 rasters must match the independently reviewed final8 set
byte-for-byte; evidence is under `RUN/visual/` and `portrait-review/`.

Owned Windows jobs serialize builds, terminate/reap descendants and retain cleanup
records. Temporary focused input project source, three generated C# leaves and its
main-project exclusion were removed. Ignored caches, failed attempts, TRX, protected
original313 artifacts+manifest and every verification stage are retained. Both frozen
reference sets and original input remain unchanged; current K: reference hashes are
verified against the selected snapshots. No original writes, uploads, staging or
commits were performed. Private .tmp evidence is not a durable archive.

## Open acceptance and existing limitations

1. Numerical vectors still differ on46/380/67 rows (56/553/122 components), despite
   complete row association. Investigate with exact inputs/independent solver evidence;
   do not hardcode values, replace strict selection with a tolerance or force ties.
2. Recover authoritative member/section names if caption parity is required. Do not
   synthesize reference-only names or remove correct weighted coefficients.
3. Existing active 2D vector-diagram defects were source-confirmed in baseline HEAD:
   DiagramResult converts canonical case keys such as1/Static/0 to int, and interprets
   canonical station n such asS0 as an input node ID. General diagram PDF correctness
   is unverified. This table correction preserves the existing route and fixes only
   newly added occurrence duplication; desktop3D uses captured images. These baseline
   defects are separate follow-up work, not a claimed passing diagram regression.
4. Keep exact reference parity visibly unfinished until numbers, sources, names and
   complete row text agree. The supplied PDFs do not establish historical software/input
   identity or full FEM correctness. Original and superseded-target reports are retained
   in the original verification run, report-first-reference-and-steering.md and
   report-before-final9.md. Current plan/HANDOFF/STATE carry continuation paths.

## Closeout gate evidence

overall: `pass`

Final solution build: exit0, **0 errors / 1487 warnings**. Warning elimination was not in scope; incremental warning counts are not a quality trend. Build command/log/owned-job cleanup: `RUN/gates/solution-build-final9/`.

Verbatim `tools` object from the final AgentOnly payload:

```json
{
  "scope-isolation": {
    "status": "pass",
    "exit_code": 0,
    "command": [],
    "detail": "checked 35 changed path(s); product scope is isolated",
    "changed_paths": [
      ".agents/docs/DESIGN.md",
      ".agents/docs/plans/transverse-analysis-print-corrections.md",
      ".agents/docs/plans/transverse-analysis-print-verification.md",
      ".agents/docs/research/handoff-pickup-node-result-exports-20261005.md",
      ".agents/docs/research/review-quality-transverse-analysis-print-verification.md",
      ".agents/docs/research/review-security-transverse-analysis-print-verification.md",
      ".agents/docs/research/review-tests-transverse-analysis-print-verification.md",
      ".agents/docs/research/transverse-analysis-print-corrections-report.md",
      ".agents/docs/research/transverse-analysis-print-verification-report.md",
      ".agents/STATE.md",
      "FramePrintPDF/PDF_Manager.Tests/TransverseFoundationDiagramTests.cs",
      "FramePrintPDF/PDF_Manager.Tests/TransverseReportCompositionTests.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintDiagram/DiagramResult.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ReferenceResultLayout.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultDisg.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultDisgCombine.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultFsec.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultFsecCombine.cs",
      "FrameWebforCS/calculation/CalculationRequest.cs",
      "FrameWebforCS/components/input/InputLoadService.cs",
      "FrameWebforCS/providers/printing/PrintFoundationOccurrences.cs",
      "FrameWebforCS/providers/printing/PrintProjection.cs",
      "FrameWebforCS/providers/printing/PrintResultMetadata.cs",
      "FrameWebforCS/tests/PrintProjection.Tests/PrintProjection.Tests.csproj",
      "FrameWebforCS/tests/PrintProjectionTests.cs",
      "FrameWebforCS/tests/TransverseDisplayProjectionTests.cs",
      "FrameWebforCS/tests/TransverseFoundationPrintTests.cs",
      "FrameWebforCS/tests/TransverseInputCorrectionTests.cs",
      "FrameWebforCS/tests/TransverseParity/native_runner.py",
      "FrameWebforCS/tests/TransverseParity/README.md",
      "FrameWebforCS/tests/TransverseParity/selfcheck.py",
      "FrameWebforCS/tests/TransverseParity/test_native.py",
      "FrameWebforCS/tests/TransversePrintMetadataTests.cs",
      "FrameWebforCS/three/LoadMemberExpansion.cs",
      "HANDOFF.md"
    ],
    "baseline_ref": null,
    "allowed_product_paths": [
      "FramePrintPDF/PDF_Manager.Tests/TransverseFoundationDiagramTests.cs",
      "FramePrintPDF/PDF_Manager.Tests/TransverseReportCompositionTests.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintDiagram/DiagramResult.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ReferenceResultLayout.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultDisg.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultDisgCombine.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultFsec.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultFsecCombine.cs",
      "FrameWebforCS/calculation/CalculationRequest.cs",
      "FrameWebforCS/components/input/InputLoadService.cs",
      "FrameWebforCS/providers/printing/PrintFoundationOccurrences.cs",
      "FrameWebforCS/providers/printing/PrintProjection.cs",
      "FrameWebforCS/providers/printing/PrintResultMetadata.cs",
      "FrameWebforCS/tests/FrameWebforCS.Tests.csproj",
      "FrameWebforCS/tests/PrintProjection.Tests/PrintProjection.Tests.csproj",
      "FrameWebforCS/tests/PrintProjectionTests.cs",
      "FrameWebforCS/tests/TransverseDisplayProjectionTests.cs",
      "FrameWebforCS/tests/TransverseFoundationPrintTests.cs",
      "FrameWebforCS/tests/TransverseInputCorrectionTests.cs",
      "FrameWebforCS/tests/TransverseParity/native_runner.py",
      "FrameWebforCS/tests/TransverseParity/README.md",
      "FrameWebforCS/tests/TransverseParity/selfcheck.py",
      "FrameWebforCS/tests/TransverseParity/test_native.py",
      "FrameWebforCS/tests/TransversePrintMetadataTests.cs",
      "FrameWebforCS/three/LoadMemberExpansion.cs"
    ],
    "product_paths": [
      "FramePrintPDF/PDF_Manager.Tests/TransverseFoundationDiagramTests.cs",
      "FramePrintPDF/PDF_Manager.Tests/TransverseReportCompositionTests.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintDiagram/DiagramResult.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ReferenceResultLayout.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultDisg.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultDisgCombine.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultFsec.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultFsecCombine.cs"
    ],
    "disallowed_product_paths": []
  },
  "agent pytest": {
    "status": "pass",
    "exit_code": 0,
    "command": [
      "uv",
      "run",
      "--project",
      "FrameWeb",
      "--locked",
      "--extra",
      "dev",
      "python",
      "-m",
      "pytest",
      ".agents/tests",
      "-q"
    ],
    "detail": ""
  },
  "repository detector": {
    "status": "pass",
    "exit_code": 0,
    "command": [
      "uv",
      "run",
      "--project",
      "FrameWeb",
      "--locked",
      "--extra",
      "dev",
      "python",
      ".agents/skills/init/detect_stack.py",
      "--project-root",
      "."
    ],
    "detail": ""
  },
  "STATE contract": {
    "status": "pass",
    "exit_code": 0,
    "command": [
      "uv",
      "run",
      "--project",
      "FrameWeb",
      "--locked",
      "--extra",
      "dev",
      "python",
      ".agents/skills/_shared/validate_doc.py",
      "--contract",
      "state-doc",
      "--file",
      ".agents/STATE.md"
    ],
    "detail": ""
  },
  "DESIGN contract": {
    "status": "pass",
    "exit_code": 0,
    "command": [
      "uv",
      "run",
      "--project",
      "FrameWeb",
      "--locked",
      "--extra",
      "dev",
      "python",
      ".agents/skills/_shared/validate_doc.py",
      "--contract",
      "design-doc",
      "--file",
      ".agents/docs/DESIGN.md"
    ],
    "detail": ""
  },
  "plan contract": {
    "status": "pass",
    "exit_code": 0,
    "command": [
      "uv",
      "run",
      "--project",
      "FrameWeb",
      "--locked",
      "--extra",
      "dev",
      "python",
      ".agents/skills/_shared/validate_doc.py",
      "--contract",
      "plan-doc",
      "--file",
      ".agents/docs/plans/agents-repository-alignment.md"
    ],
    "detail": ""
  },
  "live script references resolve": {
    "status": "pass",
    "exit_code": 0,
    "command": [],
    "detail": ""
  },
  "foreign project references absent": {
    "status": "pass",
    "exit_code": 0,
    "command": [],
    "detail": ""
  },
  "agent caches absent": {
    "status": "pass",
    "exit_code": 0,
    "command": [],
    "detail": ""
  },
  "git diff --check": {
    "status": "pass",
    "exit_code": 0,
    "command": [
      "git",
      "diff",
      "--check"
    ],
    "detail": ""
  },
  "product gates": {
    "status": "skipped",
    "exit_code": null,
    "command": [],
    "detail": "AgentOnly requested"
  }
}
```

AgentOnly log: `.agents/logs/transverse-corrections-final9-agent.log`. Payload: `RUN/gates/final9-closeout/agent-check.json`.

Ownership reconciliation: `overlaps=[]`, `unowned_changes=[]`, `idle_owners=[]`. Its sole warning states that coverage.json/coverage.xml is absent; coverage is explicitly unmeasured. The six preexisting untracked verification documents are preserved baseline paths. Payload: `RUN/gates/final9-closeout/ownership-reconcile.json`.

Additional residual evidence: `reference-refresh/comparison-final-9/residual-patterns.json`. All215 selected-source differences have equal PRINTED focus values, including80 printed zero pairs;136 have all three PRINTED components equal. This does not prove an unrounded tie or justify changing strict selection. All-paired IDs match2635/2850 and literal formula text1030/2850.
