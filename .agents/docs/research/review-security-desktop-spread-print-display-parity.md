# Security Review: Desktop Spread/PDF Numeric Display Parity

## Scope

Read-only review of the task-owned input and result formatting changes, their
format helpers, canonical result table writer, and focused desktop/PDF tests.
The pre-existing Headless changes and the approved plan were excluded.

## Findings

- **Low — verification gap:** `FrameWebforCS/components/input/PrintNumberCellType.cs:23`
  and `FrameWebforCS/tests/SpreadPrintNumericFormatTests.cs:201` — the new
  display formatter is exercised with a bound-float edit under `ja-JP`, but a
  comma-decimal edit and paste under `de-DE` are not covered. No data loss was
  demonstrated. Add a focused STA test that enters and pastes a comma-decimal
  value into a bound float cell, commits it, and checks both the bound float
  and saved JSON independently of the displayed text.

No confirmed security defect. The change adds display formatting without a
new external input, filesystem, process, network, SQL, permission, or
secret-handling path. The editable L1/L2 load cells retain string values;
result text remains protected; the canonical table writer retains the existing
numeric values and displacement scaling.

## Verification Notes

- Inspected `PrintNumberCellType.Format`, `InputLoadComponent.LoadLengthCellType`,
  `InputCombineComponent.OnChange`, and the changed input-column assignments
  for value mutation and current-culture conversion.
- Inspected the base, derived, and legacy result formatting paths for altered
  calculation/aggregation values, result provenance, and untrusted text handling.
- The focused tests cover unchanged edit commits, saved float values, L1/L2
  round trips, culture-specific display, and result raw-value preservation.

No tests were run for this read-only security review.
