Objective: Validate the implementation plan in `.agents/docs/plans/desktop-spread-print-display-parity.md` for completeness, correctness, and risk. Read the plan file. This is a read-only review; do not modify files.

Critical direction of change: The user explicitly wants the existing `FrameWebforCS/components/input/*Component.cs` and `FrameWebforCS/components/result/*Component.cs` numeric displays to match `FramePrintPDF`, and clarified that numeric formatting alone is in scope. FramePrintPDF is the reference and must not be changed. Headings, units, columns, currently blank computed data, persisted numbers, calculated numbers, and unrelated Headless work are out of scope. The prior Codex step-decomposition attempt timed out and incorrectly proposed changing FramePrintPDF to match Spread; do not repeat that reversal. In particular, canonical base result G10 is a known mismatch, not an alternate target standard: corresponding PDF rows use F4/F2/F3/F2.

Current state evidence:
- 10 input components, 9 result components audited. Input counterparts: PrintInput/InputNode.cs:273-312 F3; InputMember.cs:241-253 Cg F3; InputRigid.cs:147-163 rigid I/J F2; InputFixMember.cs:250-262,320-324 mixed F3/E2/F2; InputLoad.cs:358-367 L1/L2 F3 P1/P2 F2, 415-456 default nodal; InputNoticePoints.cs:177 F3; InputCombine.cs:215-232 F2; InputElement.cs:253-271 E2/F4/F6 with 3D >=999 default and :290-299 2D F4/F6. InputLoadComponent.LL_pitch has no proven counterpart: the PDF InputLoadName.cs:169 prints `rate`, while the desktop persists `LL_pitch` without an established projection to `rate`; exclude pitch. InputNodes already F3. Spring outline marker `***` and string L1/L2 need preservation. Dynamic column configurations refresh on dimension changes and COMBINE column changes.
- Result PDF counterparts: PrintResult/ResultDisg.cs:396-414 and ResultDisgCombine.cs:494-512 F4; ResultReac.cs:357-375 and ResultReacCombine.cs:470-488 F2; ResultFsec.cs:401-425 and ResultFsecCombine.cs:445-469 station F3, force/moment F2; PickUp inherits Combine. Legacy result screens already show those digit counts, but may pre-round differently at midpoints. Canonical CalculationResultTableWriter.cs:45-54,85-94,151-168,202-205 uses G10 for all base and unrelated modal/moving values; only corresponding base rows should change. Canonical derived renderer uses F4/F2/F3.
- PDF printManager.toString at Printing/Comon/printManager.cs:58-78 uses culture-sensitive double.ToString("F{n}"/"E{n}"). Existing input Elements has dirty approved E2/F4/F6 changes, and four Headless files are dirty from unrelated work. No product files may be edited in this planning phase.
- The first validation pass found four issues, now revised in the plan: existing displacement value scaling is preserved and the oracle compares formatting on the same numeric value; LL_pitch is excluded; current-culture decimal separators are required to match PDF; Release build explicitly uses `-c Release`. Reassess the revised text, not the earlier draft.

Review requirements:
- Check missing numeric columns, 2D/3D branches, no-counterpart paths, input string/summary values, rounding boundaries, culture, data preservation, and performance on 100,000-row sheets.
- Check step order and whether each verification catches the likely failure.
- Challenge any speculative file/test path or invalid build command; suggest precise corrections.
- Decide whether any Open Question is a true blocker versus a resolved scope rule.
- Give a clear PASS or NEEDS_REVISION verdict. Keep output concise and evidence-based; do not investigate unrelated architecture or plan PDF modifications.

Output format:
## Validation Result (PASS / NEEDS_REVISION)
## Missing Coverage
## Ordering Problems
## Integration Risks
## Revised Steps (if NEEDS_REVISION)
