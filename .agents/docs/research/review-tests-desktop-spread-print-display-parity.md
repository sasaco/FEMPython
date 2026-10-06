# Test Review: Desktop Spread and PDF Numeric Display Parity

## Scope and evidence

Reviewed the approved plan, the task diff, the new PDF and desktop numeric-format suites, affected result-format assertions, and existing COMBINE/PICKUP and dynamic-column tests. Unrelated pre-existing Headless changes were excluded. Coverage not measured.

The lead reported PDF project tests 52/52 passing and focused desktop numeric/contract tests 35/35 passing. The full desktop project reported 396/399 passing, with the same three failures observed before this implementation. I did not rerun those commands in this read-only review.

## Findings

1. **High — COMBINE Name column after shrinking coefficient count.** `InputCombineComponent.RefreshCombineColumns()` sets `PrintNumberCellType("F2")` on every coefficient column, but does not reset the trailing Name column's `CellType` (`FrameWebforCS/components/input/InputCombineComponent.cs:99-109`). After a 6-to-5 case shrink, the former C6 column becomes Name while retaining the numeric formatter. `CombineCoefficientRetainsPrecisionAcrossDynamicColumns` only tests growth (`SpreadPrintNumericFormatTests.cs:171`), and `InputCombineColumnsTests` tests shrinking text but neither the cell type nor an actual Name edit in that state. Add a grow → shrink regression that checks Name text, cell type, edit/commit, and saved name, then reset the Name cell type if it fails.
2. **High — Derived result midpoint and culture paths lack direct screen assertions.** `CalculationDerivedViewRenderer` and legacy COMBINE/PICKUP formatters changed rounding and culture (`CalculationDerivedViewRenderer.cs:118-126` and related components/aggregators). The new suite verifies current culture only for canonical base tables (`SpreadPrintNumericFormatTests.cs:293`); existing derived tests mostly pin ordinary ja-JP values. Add 2D/3D COMBINE and PICKUP `Cell.Text` cases for F4/F2/F3 positive and negative midpoints and `de-DE`, with unchanged post-aggregation numeric value and provenance assertions. Include legacy result controls and canonical-derived projection where each is used.
3. **Medium — L1/L2 lifecycle stops before reload and calculation request.** `LoadStringLengthsKeepRawValuesAfterCultureSensitiveDisplayAndEdit` (`SpreadPrintNumericFormatTests.cs:108`) checks immediate model and save data in `de-DE`, but does not reload saved JSON or build the calculation request. Add those steps after a high-precision edit, plus empty and invalid string cases, to ensure the formatting layer never changes the existing invariant parser contract.
4. **Medium — Missing and blank source values have no desktop JSON-path regression.** The PDF suite covers `printManager.toString(null)`/NaN (`PrintNumericFormatOracleTests.cs:30`), but the desktop suite does not load missing input or result components and inspect the resulting Spread cells. Add one blank input and one legacy result case through JSON load to pin existing blank/zero behavior; avoid asserting printer-style blank where the desktop intentionally uses zero.
5. **Medium — PDF result oracle is at formatter level only.** The printer suite checks `printManager` and actual input element/load tables, but no actual PDF result table. Add representative displacement, reaction, and section-force table cases for 2D/3D or explicitly link each desktop expected string to a printer result-table fixture. This protects against future print-row format divergence beyond the shared helper.
6. **Low — Non-parity routes and performance lack targeted evidence.** The changed canonical writer guards modal formatting, but there is no explicit G10 modal/moving snapshot around this diff. No 100,000-row timed or visual evidence appears in the tests; the plan calls for separate measurement and normal/125% DPI inspection. Record those manual results and add a modal/moving assertion if those paths are cheap to exercise.

## Test design notes

The new STA helper restores both cultures and the desktop suite uses the serialized singleton collection. Direct float-to-saved-decimal checks, edit-without-change checks, E2 and 998.999/999 checks are useful. The `PrinterFloat` helper mirrors the new cell formatter, so independent PDF table examples are especially valuable for fields currently checked only through that helper. No coverage percentage was measured.

## Recommendations

Address findings 1 and 2 before treating the requested numeric parity as verified. Then add the lifecycle/null regressions and record manual performance and visual evidence. Keep unrelated full-suite failures separate from this task.
