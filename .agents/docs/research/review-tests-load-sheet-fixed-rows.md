# Test Review: Load Sheet Fixed Rows

## Summary

PASS after follow-up. The regression suite covers fixed-slot behavior and preserves existing load-value and selection assertions. The final actual-product runner records 96 passed, 0 failed and 0 skipped, including all four follow-up cases. Coverage is not measured. Both medium findings and the low performance-evidence gap are resolved.
## Review Scope

Reviewed the approved plan and .agents/logs/review-diff-load-sheet-fixed-rows.patch, all new load-sheet test sources, modified unified/width/ThreeLoads tests, UI key/editor hooks, singleton collection configuration, isolated project source links, and the machine-readable TRX. No build or test execution was repeated during this review.

## Findings

- [Medium / P2, resolved] The provider boundary previously lacked invalid-load-candidate rollback coverage. InputLoadLayoutPersistenceTests.cs:291 now tests duplicate slots, missing mappings and aggregate overflow through JsonDataOpen with otherwise valid changed geometry. Every case asserts whole-document saved JSON, both revisions, selected Case and FileReplaced count remain unchanged. Independently inspected all three cases and their Passed TRX outcomes.
- [Medium / P2, resolved] Direct setLoadJson/clear previously published the LoadNames Reset before intensity slots/mappings were replaced. The service owner moved the names Reset after ReplaceIntensityRows. InputLoadLayoutPersistenceTests.cs:242 now subscribes to both Reset streams during direct replacement and clear, verifies fixed counts/list identity, parses every observed snapshot as a valid complete load/layout candidate, and confirms reverse mappings agree. Both operations publish two coherent snapshots; the final TRX records this case Passed.
- [Low / P3, resolved] Dedicated measurements now exist in .tmp/InputLoadFixedRows/Perf/metrics-empty.json, metrics-mixed.json and metrics-near-capacity.json. Inspected the actual STA probe and metric files. Each scenario runs separately against actual controls; its one-cell editor commit emits one ItemChanged and zero Reset events. Stage samples record time, allocated bytes, managed memory, private memory and working set. They are local stage-end samples, not a continuous peak or retained-memory measurement.
- No remaining blocking test-review findings. No coverage percentage inferred.
## Covered Requirements

- Fixed BindingList identity and count, anonymous LoadId, clear/reload/name changes: InputLoadFixedRowsTests.cs:19.
- Insertion at first/middle/gap/tail, full node/member payload shift, event count and revision increment: InputLoadFixedRowsTests.cs:56.
- Disjoint simultaneous deletion across cases with duplicate selected indices and assigned local-row renumbering: InputLoadFixedRowsTests.cs:92 and UnifiedLoadIntensitySheetTests.cs:283.
- Assigned empty or populated final slot rejects insertion without state/revision changes; real control key path preserves active cell and ranges: InputLoadFixedRowsTests.cs:129 and :322.
- First payload/LoadId assignment, zero, sparse maximum-row fallback, Case movement without display reorder: InputLoadFixedRowsTests.cs:167 and :191; InputLoadLayoutPersistenceTests.cs:219.
- Actual Spread editor commit/cancel, registered KeyDown chain, Oem5/Oem102 and modifier/edit guards, ordinary/protected-cell Delete and Sheet1 insertion guard: InputLoadColumnWidthTests.cs:24 and InputLoadFixedRowsTests.cs:216, :262, :291. KeyDown is injected through Control.OnKeyDown; row-header origin and ranges are set explicitly. These are real WinForms control/editor tests, not OS-level physical input automation.
- Sparse layout and assigned empty-row roundtrip including last slot, save/calculation separation, invalid metadata, later invalid section atomicity and exact 100000/100001 capacity: InputLoadLayoutPersistenceTests.cs:12, :41, :102, :131, :160.
- 2D/3D count, stale blue color removal, disposal/recreation, deferred move versus newer navigation and real ThreeService selection after display shift: InputLoadFixedRowsTests.cs:350, :388, :417.

## Test Quality and Isolation

DisplacementSingletonCollection.cs:5 disables parallel execution for the named singleton collection. All eight runner suites use that collection. New helpers clean load state in finally; full-document tests clear state and detach event subscribers in finally; STA execution propagates exceptions and has a bounded timeout. The runner references the actual product project and original test sources, retaining InternalsVisibleTo through the existing test assembly name. It neither replaces production behavior with mocks nor removes the unrelated failing test source from the tracked project.

Modified prior tests remove obsolete automatic starter-row/variable-count expectations. Node/member values, Case movement, sparse row identities, width/lock behavior, deletion and selection assertions remain. No weakening unrelated to the approved behavior change was found.

## Test Execution Results

- Independently parsed .tmp/InputLoadFixedRows/TestResults/fixed-rows.trx: total=96, executed=96, passed=96, failed=0, notExecuted=0. Explicitly verified all four added test outcomes Passed.
- Coverage: not measured; no percentage inferred.
- Lead-provided product build: exit 0. Normal solution/test project retains 12 preexisting LoadDisplayConversion CS0103 errors.
- No builds/tests repeated by this reviewer; inspected final test sources, TRX and performance evidence read-only.

## Performance Evidence

Values below come from the separate actual STA processes in .tmp/InputLoadFixedRows/Perf. Empty/mixed labels describe initial input; the editor stage later adds or updates a row. Near-capacity starts with 99,999 assignments.

| Scenario | Initialize/show | Front insert | Front delete | Reload | Highest sampled working set | Highest sampled managed memory |
|---|---:|---:|---:|---:|---:|---:|
| Empty | 1587.95 ms | 62.04 ms | 49.37 ms | 51.18 ms | 401.82 MiB | 277.14 MiB |
| Mixed | 1205.69 ms | 41.46 ms | 42.19 ms | 42.09 ms | 402.15 MiB | 277.17 MiB |
| Near capacity | 1444.29 ms | 822.07 ms | 536.02 ms | 1420.39 ms | 544.60 MiB | 407.68 MiB |

These are single local runs without an established performance threshold or baseline. GC is forced before each measured stage; memory is sampled immediately afterward, so the table does not claim peak usage or memory retained after a final GC. All three probes verify fixed service/sheet counts and one ItemChanged/zero Reset for a normal editor commit.