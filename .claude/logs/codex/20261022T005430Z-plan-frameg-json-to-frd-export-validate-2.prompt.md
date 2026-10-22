Objective: Re-validate the REVISED implementation plan at .claude/docs/plans/frameg-json-to-frd-export.md. Read-only: do not edit files.

Your previous review (NEEDS_REVISION) raised: bridge_loads/spatial loads, m1/m2 remapping under renumbering, FrameWebforCS tests locating samples, direct CRC verification, gy/gx and fix_member.length being undecided, Headless protocol mapping (messages[], RunnerError, engineeringStatus), lzh.py reproducibility, and the FrameG checklist coverage. The plan has been revised for each. Note: FrameWebforCS tests locate samples with RepositoryRoot() walking up directories (FrameWebforCS/tests/InputFileLoaderTests.cs:81), so no csproj change is needed - verify this yourself.

Constraints:
- Check each previous finding is actually resolved in the plan text
- Check for any new missing edge cases, ordering problems, or verification gaps
- Check that nothing in Open Questions is a blocker in disguise

Output format:
## Validation Result (PASS / NEEDS_REVISION)
## Missing Coverage
## Ordering Problems
## Integration Risks
## Revised Steps (if NEEDS_REVISION)
