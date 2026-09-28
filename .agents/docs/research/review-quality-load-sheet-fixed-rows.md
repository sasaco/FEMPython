# Quality Review: Load Sheet Fixed Rows

## Summary

**PASS — no unresolved concrete quality defect.** Independently reviewed the product implementation and approved plan. The shared P2 direct-replacement notification issue is fixed and verified by a new observer regression; the final actual-product acceptance runner passes all 96 cases.

## Review Scope

- `FrameWebforCS/components/input/InputLoadService.cs`: fixed slots, anonymous assignment, global insertion/deletion, Case-local renumbering, reverse lookup, notification/revision behavior, sparse persistence and capacity.
- `FrameWebforCS/components/input/InputLoadComponent.cs`: key provenance, ordinary cell Delete, anonymous selection, deferred callbacks, columns and bounded color updates.
- `FrameWebforCS/providers/InputDataService.cs`: whole-document candidate staging, save metadata and calculation snapshot separation.
- Approved plan `.agents/docs/plans/load-sheet-fixed-rows.md` and diff `.agents/logs/review-diff-load-sheet-fixed-rows.patch`.

## Findings

- **Medium/P2, resolved** — `InputLoadService.cs:793`: direct replacement previously published the names Reset before intensity slot installation. It now installs assignments before either binding-list observer can inspect the replacement. `BothResetObserversSeeFullyInstalledLoadLayoutAndReverseMappingOnDirectReplacementAndClear` validates saved load/layout as a candidate from both callbacks during direct replacement and clear. Counts and reverse mapping are coherent in each callback.
- **No further actionable finding.** Case-local identities are updated consistently with node/member payloads and lookup maps (`InputLoadService.cs:499`, `:538`, `:592`). Slot replacement changes neither list identity nor count and suppresses intermediate notifications (`:599`). Full candidate parsing rejects overflow and incomplete/duplicate metadata before application (`:287`, `:295`). Normal cell editing remains local (`:734`). Deferred selection checks both data and navigation revisions (`InputLoadComponent.cs:95`, `:192`); color work visits occupied or previously colored indices (`:402`). Save output includes sparse layout while calculation snapshots exclude it (`InputDataService.cs:125`, `:393`).

## Codex Consultations

A single read-only nested consultation used a short single-line prompt pointing to the plan, diff and product sources, with medium reasoning and a 120-second configured timeout. **It did not complete successfully**: wrapper `timed_out=true`, reported duration 195.735 seconds, partial response 780 characters. No retry was performed.

The partial response hypothesized that value-first allocation might leave Spread's LoadId cell blank because only the edited payload property is notified. This UI hypothesis is contradicted by all five actual editor width regressions: each asserts `sheet.Cells[committedIndex,0].Text == caseId` after commit (`InputLoadColumnWidthTests.cs:83`). No product property-specific LoadId consumer was found. The partial response is preserved as consultation evidence, not treated as a successful CLI review or a verified defect. Native source review remains the adequacy decision.

- Prompt: `.agents/logs/codex/20260928T025632Z-quality-load-sheet-fixed-rows.prompt.md`
- Partial response: `.agents/logs/codex/20260928T025632Z-quality-load-sheet-fixed-rows.md`
- Stderr: `.agents/logs/codex/20260928T025632Z-quality-load-sheet-fixed-rows.err.log`

## Observed Performance

Executed a narrow temporary STA console probe using a real product ProjectReference, the existing friend assembly name, actual displayed WinForms/Spread controls and the real editor. No dependencies or product instrumentation were added. Each scenario ran in a separate fresh process on Windows 10.0.26200, .NET 10.0.12, 20 logical processors. The near-capacity input had 99,999 assigned empty rows split between Cases 1 and 2, leaving one free final slot for insertion; the mixed input had four assigned rows, two Cases with values plus one member-load Case and anonymous gaps.

| Stage (milliseconds) | Empty | Mixed | Near capacity |
|---|---:|---:|---:|
| Cold service initialization | 35.41 | 37.82 | 31.18 |
| Initial JSON load | 19.60 | 32.21 | 169.02 |
| Construct and show Sheet2 | 1587.95 | 1205.69 | 1444.29 |
| Front insertion, including UI update | 62.04 | 41.46 | 822.07 |
| Front deletion, including UI update | 49.37 | 42.19 | 536.02 |
| First real single-cell editor commit | 184.78 | 133.30 | 276.20 |
| Serialize loads and layout | 33.03 | 30.74 | 522.67 |
| Reload saved loads/layout with UI update | 51.18 | 42.09 | 1420.39 |

| Sampled memory / edit work | Empty | Mixed | Near capacity |
|---|---:|---:|---:|
| Service initialization allocations (MiB) | 29.62 | 29.62 | 29.62 |
| Sheet construction/show allocations (MiB) | 284.40 | 284.43 | 391.98 |
| Single-cell commit allocations (MiB) | 1.70 | 1.72 | 1.69 |
| Single-cell ItemChanged / Reset events | 1 / 0 | 1 / 0 | 1 / 0 |
| Managed sample after reload (MiB) | 277.14 | 277.17 | 407.68 |
| Working-set sample after reload (MiB) | 401.82 | 402.15 | 544.59 |
| Private-byte sample after reload (MiB) | 331.91 | 331.82 | 490.11 |
| Saved UTF-8 bytes | 290 | 828 | 4,644,820 |

The empty scenario gains one assigned row from the measured first cell edit before save. These are one-run observations, not statistical benchmarks or thresholds. Each stage forces GC before starting the timer, then records allocations and process/managed samples after the action and `Application.DoEvents`. Samples are **neither peak usage nor post-GC retained-memory measurements**; UI/library startup and the first editor setup are included. The near-capacity workload is assigned-empty layout and color/binding work, not 99,999 complete payloads. It visibly costs more time and memory, but all operations completed, counts stayed 100,000, and ordinary edits emitted one local item notification without any Reset. The required fixed-capacity UI itself has a substantial baseline memory cost.

Raw evidence and reproducible probe:

- `.tmp/InputLoadFixedRows/Perf/LoadSheetPerf.csproj`
- `.tmp/InputLoadFixedRows/Perf/Program.cs`
- `.tmp/InputLoadFixedRows/Perf/metrics-empty.json`
- `.tmp/InputLoadFixedRows/Perf/metrics-mixed.json`
- `.tmp/InputLoadFixedRows/Perf/metrics-near-capacity.json`

## Validation and Limits

- Actual-product isolated runner: 96 passed, zero failed/skipped; `.tmp/InputLoadFixedRows/TestResults/fixed-rows.trx`.
- Probe build: zero errors; all three process runs exited zero.
- Test-file `git diff --check` passed.
- Existing solution compilation failures in LoadDisplayConversionTests and the unrelated Sidebar whitespace were preserved and excluded from this review.
- No product source was changed by this reviewer. Only temporary probe files, requested regression tests and review/work-log artifacts were written.
