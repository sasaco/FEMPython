Objective: Validate this implementation plan for completeness, correctness, and risk. Read-only: do not edit files.

Context:
- Purpose and scope: reverse of FrameGConverter/Convert_Manager/ConvertManager.cs — any 2D FrameWeb input JSON (FrameWebforCS save format) -> FrameG .frd (level-0 LZH of Shift-JIS files). User decisions: expose as Convert_Manager API + FrameWebforCS/Headless `export-frd` + Convert_Test button; arbitrary 2D JSON with errors for unrepresentable input; own lh5 encoder; FrameG itself is unavailable, the user checks the output in FrameG later.
- Current state: see the earlier step-decomposition prompt at .claude/logs/codex/prompt-plan-frameg-json-to-frd-export-steps.md (facts on file layouts, LZH headers, forward mappings, FrameWeb JSON, Headless runner). Verify claims against the code yourself where it matters (FrameGConverter/Convert_Manager/FrameWebForJS/*.cs, FrameWebforCS/providers/InputFileLoader.cs, FrameWebforCS/providers/InputDataService.cs, FrameWebforCS/calculation/CalculationRequest.cs, FrameWebforCS/Headless/*.cs, sample archives in FrameGConverter/Convert_Test/TestData).
- Implementation plan: the contents of .claude/docs/plans/frameg-json-to-frd-export.md (read it).

Constraints:
- Check for missing edge cases or error handling
- Verify the step order is actually dependency-correct
- Ensure each step's stated verification would really detect a failure (in particular: is the forward converter + SevenZipExtractor a sufficient oracle where it is used, and where is it blind?)
- Identify integration risks and convention violations (netcoreapp3.1/C# 8 in Convert_Manager, shared Headless exe, test projects' references)
- Check that nothing in Open Questions is in fact a blocker disguised as a question
- Note specifically whether the proposed load sign table, the m1/m2 handling, renumbering, material expansion, and the decision to keep the forward converter unchanged are consistent with the code.

Output format:
## Validation Result (PASS / NEEDS_REVISION)
## Missing Coverage
## Ordering Problems
## Integration Risks
## Revised Steps (if NEEDS_REVISION)
