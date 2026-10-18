# Task List: Desktop Direct PDF Printing

Approved plan: `.agents/docs/plans/desktop-direct-pdf-printing.md`.

## Step 1: Feasibility and boundaries

- [x] Print library owner: prove direct `net10.0-windows` reference and PDF byte generation; audit PDF/font dependencies and add focused library tests. Isolated tests 7/7; WebView2 STA PDF navigation passed; font redistribution still requires release review.
- [x] Print viewport owner: prove GL capture with a modal fully covering the viewport; define and test occlusion-independent composition. Real GL smoke passed; focused tests 9/9.
- [x] Lead: verify the in-app preview and STA path. WebView2 loads generated PDF, PrintToPdfAsync produces a valid page, and PrintAsync reports unavailable printers; installed virtual-printer spooling remains unverified.

## Step 2-3: Parity inventory and projection

- [x] Projection owner: inventory all legacy print controls and payload keys with C# data sources and fixtures.
- [x] Projection owner: implement atomic input/result/derived projection, validation, stale revision handling, and focused tests. Focused tests 13/13.

## Step 4-5: PDF invocation and diagrams

- [x] Print library owner: implement bounded direct PDF bytes and validate structure, errors, and shared font state.
- [x] Print viewport owner: implement per-mode, ordered, labeled/legend diagram captures with UI-state restoration and tests.

## Step 6: Modal UI

- [x] UI owner: implement one owner-bound `ShowDialog(owner)` print window, all legacy selection controls, preview, save, system printer dialog, and lifecycle tests. Focused tests 5/5; actual printer spool remains unverified.

## Step 7: Integration and review

- [ ] Lead: run focused tests, root solution build/test, actual modal/preview/save/virtual-printer/GL smoke checks, ownership reconcile, and diff inspection. All completed except actual virtual-printer spool and full rendered-page parity; root solution retains 12 pre-existing LoadDisplayConversionTests compilation errors.
- [x] Security, quality, and test reviewers: review the completed scoped diff and report actionable findings. Security and quality defects were remediated and re-reviewed; printer and rendered-parity sign-off gaps remain documented.
