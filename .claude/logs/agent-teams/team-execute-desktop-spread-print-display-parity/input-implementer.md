# Work Log: Input Implementer

## Summary

Aligned PDF-backed numeric displays in the desktop input sheets with the printer's decimal and exponential formats. Bound numeric and string values remain unchanged.

## Tasks Completed

- [x] Mapped input columns to PDF format calls, including 2D/3D element branches and dynamic COMBINE columns.
- [x] Added a shared print-aware numeric cell type for float-backed columns, using the float's saved JSON decimal representation before PDF-style double formatting.
- [x] Preserved string-backed load lengths and spring outline `***` while applying the numeric display formats.
- [x] Verified focused STA cell text, edit preservation, and Release build with the tester.
- [x] Reset the moving COMBINE Name column to a default GeneralCellType after every grow/shrink rebuild.

## Files Modified

- `FrameWebforCS/components/input/PrintNumberCellType.cs` (new)
- `FrameWebforCS/components/input/InputNodesComponent.cs`
- `FrameWebforCS/components/input/InputMembersComponent.cs`
- `FrameWebforCS/components/input/InputElementsComponent.cs`
- `FrameWebforCS/components/input/InputFixMemberComponent.cs`
- `FrameWebforCS/components/input/InputLoadComponent.cs`
- `FrameWebforCS/components/input/InputNoticePointsComponent.cs`
- `FrameWebforCS/components/input/InputCombineComponent.cs`

## Key Decisions

- Format only numeric values at the column cell-type level. No 100,000-row style scan or per-row formatter allocation was added.
- Use `float.ToString("R", InvariantCulture)` followed by double parse for display, matching the decimal value serialized to JSON and read by the printer. A direct float F3 display differed at 1.2345 (1.235 versus the printer's 1.234).
- Apply the PDF's strict 3D element `>= 999` default format branch. Keep 2D area and inertia in fixed F4/F6 even at 999.
- Keep `L1`/`L2` bound as their original strings. Their cell type formats valid invariant decimal text but returns the editor text as a string on commit.
- Keep `LL_pitch`, IDs, flags, and default-formatted nodal loads outside the mapped format changes.
- A former coefficient column can become the trailing Name column after COMBINE shrinks. Explicitly restore the Name cell type during each rebuild so numeric formatting does not leak into editable names.

## Validation

- `dotnet build FrameWebforCS/FrameWebforCS.csproj -c Release --no-restore -clp:ErrorsOnly`: exit 0; 0 errors (1376 existing warnings).
- `& .agents/check.ps1 -AgentOnly -AllowProductPath 'FramePrintPDF/PDF_Manager.Tests/PrintNumericFormatOracleTests.cs'`: exit 0, overall pass.
- `git diff --check -- FrameWebforCS/components/input`: exit 0.
- After the COMBINE Name fix, the Release product build and `git diff --check -- FrameWebforCS/components/input/InputCombineComponent.cs` both exited 0.
- Tester reports expanded focused `SpreadPrintNumericFormatTests` pass for node negative zero/midpoint, member/rigid/spring/notice near-midpoint values, spring `***`, load values, 2D/3D element boundary, and unchanged/raw load edits.
- Tester reports the COMBINE 5→6→5 STA regression passes: Name text and edit/save behavior survive both moves, the trailing cell type resets to GeneralCellType, and C5 coefficient precision remains.
- The existing STA fixtures use 100,000-row sheets, but no reusable same-machine before/after timing harness exists. A quantitative 20% timing/memory comparison was not performed. Visual clipping at 125% DPI was not inspected in this headless test environment.

## Communication with Teammates

- → lead: Reported mapped input formats, source proof of column-only formatting, float midpoint defect and fix, build/check results, and performance/visual verification limits.
- → tester: Requested actual Spread cell-text, midpoint, string-edit, spring-summary, and node tests; received passing focused test report.

## Issues Encountered

- Running a desktop test simultaneously with another agent's testhost caused a Release DLL copy lock; subsequent test runs were coordinated with the tester.
- Direct NumberCellType/GeneralCellType formatting of bound floats diverged from the printer near decimal midpoints or with six fixed digits. The shared print-aware cell type resolved the measured cases.
- Review found a grow/shrink COMBINE column reuse path that could assign the former coefficient formatter to the Name column. The rebuild now explicitly resets that column's cell type; the focused regression passes.
