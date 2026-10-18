Objective: Validate the implementation plan at .agents/docs/plans/partial-member-spring-intervals.md for correctness, completeness, dependency order, and test quality.
Current requirements: cumulative member-spring intervals from I end; blank-only legacy rows cover full member; mixed blank row may appear only last and covers remainder; same-member rows auto-move to existing group end; backslash insert and row-header Delete; use FpSpread grouping and show geometric full length plus spring values *** when collapsed; PDF input printing must reflect lengths.
Current state: InputFixMemberComponent.cs has an unbound length column; CalculationRequest.cs drops length/zero spring rows; Python legacy_beam.py physically splits rigid/load intervals and presently applies full-member spring stiffness to every segment; FramePrintPDF/Printing/PrintInput/InputFixMember.cs lacks length. Spread 15.4 is installed; official spwin-group-interop.html warns of GroupDataModel and row-change limitations. Existing PDF and viewport changes belong to another worker.
Constraints: read only, no edits. Check missing edge cases and malformed-input handling, step order, whether verification detects failures, integration risks, and requirements disguised as questions. Pay attention to all-zero gap rows, multi-case topology, 100000-row grouping, collapsed per-column presentation, and preserving original member IDs.
Output format:
## Validation Result (PASS / NEEDS_REVISION)
## Missing Coverage
## Ordering Problems
## Integration Risks
## Revised Steps (if NEEDS_REVISION)
