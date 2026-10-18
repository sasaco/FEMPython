Objective: Validate one specific implementation plan for completeness, correctness, and risk. Read `.agents/docs/plans/rail-transverse-print-parity.md` from this repository. This is a verification-only plan, not a request for generic repository test commands. Return PASS or NEEDS_REVISION with concrete file/step evidence; do not ask for a target or edit files.

Purpose and scope: Rerun the exact original K:\レールウェイコンサルタント\26_ピット\計算書\02_pre\05_線路直角方向の計算\線路直角方向.json with current FrameWebforCS calculation and printing and compare the three current K: PDFs (基本.pdf, PICK UP.pdf, 変位量.pdf) including visual appearance. Preserve originals, reuse existing private harness/comparer in a new private run, and produce an evidence-backed report. No product fix is in scope.

Current state: K: input/reference SHA256 hashes match the frozen updated snapshots recorded in the plan. Prior report `.agents/docs/research/transverse-analysis-print-corrections-report.md:75-106` used a corrected private COPY of the input, not the original K: JSON; it found numerical and provenance differences. Original-input verdict needs a fresh run. `FrameWebforCS/Headless/HeadlessCalculation.cs:35-108` is the real path; `FrameWebforCS/components/print/PrintDialogForm.cs:330-334` shares PDF projection/generation. Previous harness and comparator paths are in the plan Scope. The user explicitly requires PDF visual appearance to be included.

Check for missing edge cases/error handling, dependency order, whether each verification would detect failure, integration risks, current project conventions, and whether Open Questions conceal blockers. Reject any step that silently substitutes the corrected COPY for the original, ignores all-page visual comparison, weakens row/source matching, or requires full Angular/Python/converter gates for read-only C#/PDF verification. When possible cite the plan step number and relevant repository path.

Output exactly:
## Validation Result (PASS / NEEDS_REVISION)
## Missing Coverage
## Ordering Problems
## Integration Risks
## Revised Steps (if NEEDS_REVISION)
