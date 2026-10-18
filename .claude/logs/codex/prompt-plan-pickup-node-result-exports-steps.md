Objective: Decompose PICKUP displacement and reaction node CSV exports into an ordered, independently testable implementation plan.
Context:
- Purpose: From FrameWebforCS desktop, export distinct PICKUP displacement and support-reaction files for 2D/3D, with each node/focus max and min selected COMBINE ID and full correlated six-component vectors.
- Scope in: FrameWebforCS/calculation/ new node formatter; FrameWebforCS/components/menu/MenuComponent.cs; focused tests; plan document. Preserve existing section-force exporter. Out: Angular, Python FEM/result contract, legacy display aggregators, concurrent Headless/Python runtime changes.
- Constraints: PowerShell, net10 desktop, CSV in both dimensions unless user specifies an external schema. Distinct default filenames; invariant full-precision numbers and explicit units from AnalysisUnits (displacement translations length unit, rotations rad; reaction forces force unit, moments force*length); six vector components even in 2D; topology node order; max/min paired per node; first tie winner from presenter; spreadsheet-safe text; reject malformed/nonfinite data and avoid partial files; UTF-8 without BOM. Empty definitions should not present a misleading file.
- Current state: CalculationResultPresentation.cs:86-130 Current.Derived lifecycle with invalidation/generation. CalculationDerivedPresentation.cs:5-51 rows and quantity maps. CalculationDerivedPresenter.cs:90-99 maps displacement/reaction node results; :130-134 2D indices 0,1,5 and 3D all; :234-259 selects whole COMBINE row and SourceCaseId using strict >/<. PickupExportFormatter.cs:18-86 is member/station section-force exporter only; :89-105 number/CSV text patterns. MenuComponent.cs:24-44,75-103 has one section-force item with SaveFileDialog, Changed gating, UTF-8 direct write. CalculationDerivedViewRenderer.cs:111-127,166-177 displays mm/rad and rounds; raw data should be used for file. Existing tests PickupExportFormatterTests.cs:8-99 and CalculationDerivedPresenterTests.cs:102-135. HANDOFF.md documents known 364/368 full-suite baseline. Working tree has user/concurrent changes in HANDOFF.md, FrameWebforCS.csproj, PythonCalculationRuntime.cs, Headless/; do not touch.
Constraints:
- Order steps by dependency and make each independently testable.
- Put high-risk and high-uncertainty steps first.
- Keep steps small and flag genuine blockers.
Output format:
## Implementation Steps (ordered by dependency)
## Verification per Step
## Risks and Mitigations
## Open Questions
