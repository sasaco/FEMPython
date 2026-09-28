# Security and Data Integrity Review: Load Sheet Fixed Rows

## Summary

Reviewed the approved plan, resolved review diff, service/persistence implementation, and relevant test changes. One P2 data-integrity finding was corrected and independently inspected. No unresolved security or data-integrity findings remain. The reviewer did not independently assess its own UI implementation.

## Findings

### P2: Direct replacement publishes a mixed load/layout snapshot

In the reviewed source, `InputLoadService.ReplaceLoad` publishes `LoadNames.ResetBindings` at `FrameWebforCS/components/input/InputLoadService.cs:792` before replacing the intensity layout at line 794. `DocumentReplacementNotifications.Defer` runs synchronously without an active document-replacement scope (`FrameWebforCS/providers/DocumentReplacementNotifications.cs:22`). Consequently, a `LoadNames.ListChanged` Reset observer during direct `setLoadJson` or `clear` sees the new `_load` and old `_assignedIndices`/intensity rows. Capturing saved load data plus the new persisted layout in that callback produces an inconsistent document; an observer that validates the snapshot can throw before the slot replacement occurs.

Normal `InputDataService.JsonDataOpen` has an outer notification scope and does not expose this order. Direct service replacement has no such scope. The older notification order becomes a persisted-reference integrity issue with the new layout metadata.

**Correction**: Publish the names Reset only after `ReplaceIntensityRows` installs the complete replacement. Keep both binding lists stable and avoid a nested notification scope.

**Status: Corrected; source and regression review PASS.** Independently inspected the correction: `InputLoadService.cs:793` now installs all intensity assignments before the names Reset at line 794. Both Reset notifications therefore execute after load/names/slot installation.

**Regression verification**: Independently inspected `InputLoadLayoutPersistenceTests.cs:242`: the test subscribes to both binding-list Reset streams during direct replacement and clear, serializes each callback snapshot, parses its load/layout, validates reverse lookup and slot identity, and asserts both snapshots match the fully installed final state. The final TRX records this regression as Passed. Three added whole-document rollback variants also pass (`InputLoadLayoutPersistenceTests.cs:291`).

## Review Evidence

- Load identities and aggregate capacity are validated before application: `InputLoadService.cs:236`, `:267`, `:287`.
- Metadata version/types, bounded one-based slot integers, canonical Case IDs, exact identity coverage, duplicate slots, duplicate identities, and unknown references are checked in `InputLoadService.cs:295`.
- Both direct load and whole-document open use `ParseLoadData`; document open stages later input/result sections before entering the apply phase (`InputLoadService.cs:220`; `InputDataService.cs:237`, `:245`, `:249`, `:319`).
- Insertion rejects an occupied final slot before mutation; multirow deletion validates the entire distinct index set before removing any records (`InputLoadService.cs:481`, `:499`).
- Case-local identities remain separate from display slots; row renumbering updates retained node/member identities and sparse lookup (`InputLoadService.cs:82`, `:499`, `:538`, `:592`).
- Sparse layout serialization enumerates assigned slots only. Ordinary saved documents include it while calculation snapshots do not (`InputLoadService.cs:337`; `InputDataService.cs:125`, `:393`).
- Existing tests retain valid load values, case moves, column widths, protection, and ordinary cell Delete assertions while replacing superseded starter-row/count assumptions. Added tests cover malformed metadata, state/revision preservation, exact capacity, assigned empty tail rows, mixed-case shifts, sparse identity allocation, saved layout separation, and late document parse failure (`InputLoadLayoutPersistenceTests.cs:11`, `:40`, `:100`, `:130`, `:159`; `InputLoadFixedRowsTests.cs:51`, `:126`, `:166`, `:189`).

## Validation and Limits

The lead reports product build success. This reviewer independently parsed `.tmp/InputLoadFixedRows/TestResults/fixed-rows.trx`: 96 executed, 96 passed, 0 failed, 0 notExecuted, including the direct-reset consistency regression and all three added document rollback variants. This reviewer ran no additional builds or tests. The existing solution test build's 12 LoadDisplayConversion CS0103 errors and the unrelated Sidebar trailing whitespace were excluded from this review; no product or test files were edited.
