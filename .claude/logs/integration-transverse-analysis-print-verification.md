# Integration: Transverse Analysis Print Verification

## Completed Tasks

- Fresh sample solve and three real PDFs generated.
- All-row comparison, independent derived checks, all-page visual review completed.
- Three independent evidence reviews PASS.

## Quality Checks

overall: pass

```json
{"scope-isolation":{"status":"pass","exit_code":0,"command":[],"detail":"checked 6 changed path(s); product scope is isolated","changed_paths":[".agents/docs/plans/transverse-analysis-print-verification.md",".agents/docs/research/review-quality-transverse-analysis-print-verification.md",".agents/docs/research/review-security-transverse-analysis-print-verification.md",".agents/docs/research/review-tests-transverse-analysis-print-verification.md",".agents/docs/research/transverse-analysis-print-verification-report.md",".agents/STATE.md"],"baseline_ref":null,"allowed_product_paths":[],"product_paths":[],"disallowed_product_paths":[]},"agent pytest":{"status":"pass","exit_code":0,"command":["uv","run","--project","FrameWeb","--locked","--extra","dev","python","-m","pytest",".agents/tests","-q"],"detail":""},"repository detector":{"status":"pass","exit_code":0,"command":["uv","run","--project","FrameWeb","--locked","--extra","dev","python",".agents/skills/init/detect_stack.py","--project-root","."],"detail":""},"STATE contract":{"status":"pass","exit_code":0,"command":["uv","run","--project","FrameWeb","--locked","--extra","dev","python",".agents/skills/_shared/validate_doc.py","--contract","state-doc","--file",".agents/STATE.md"],"detail":""},"DESIGN contract":{"status":"pass","exit_code":0,"command":["uv","run","--project","FrameWeb","--locked","--extra","dev","python",".agents/skills/_shared/validate_doc.py","--contract","design-doc","--file",".agents/docs/DESIGN.md"],"detail":""},"plan contract":{"status":"pass","exit_code":0,"command":["uv","run","--project","FrameWeb","--locked","--extra","dev","python",".agents/skills/_shared/validate_doc.py","--contract","plan-doc","--file",".agents/docs/plans/agents-repository-alignment.md"],"detail":""},"live script references resolve":{"status":"pass","exit_code":0,"command":[],"detail":""},"foreign project references absent":{"status":"pass","exit_code":0,"command":[],"detail":""},"agent caches absent":{"status":"pass","exit_code":0,"command":[],"detail":""},"git diff --check":{"status":"pass","exit_code":0,"command":["git","diff","--check"],"detail":""},"product gates":{"status":"skipped","exit_code":null,"command":[],"detail":"AgentOnly requested"}}
```

## Ownership Reconcile

- overlaps: 0; unowned: 0; idle implementation owners are expected because private implementation files are gitignored. Their artifacts and validated logs confirm execution.
- Six work logs validated; all review artifacts present/nonempty.

## Next Steps

Verification complete with differences. Repairs are a separate task.
