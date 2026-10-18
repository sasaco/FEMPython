# Work Log: backend

## Summary

Implemented case-local normalized bridge loading for legacy desktop requests, with 3D gating and public shell-ID remapping. Added solver-originated static load audits to the Python, C#, TypeScript and JSON schema result contracts.

## Tasks Completed

- [x] Read context-loader, 10 rules, STATE, design, approved plan and ownership map. Context report: missing ["PROGRESS.md"], unreadable [], warnings [], design.placeholder false, progress.entries 0. Recalled spatial-load and result-contract memory before editing.
- [x] Accept selected-case spatial_loads and reject ambiguous root/case/legacy representations. Preserve existing root normalized semantics and legacy load_inf behavior.
- [x] Reject explicitly dimension-2 JSON with bridge definitions before loading the model.
- [x] Map case-local panel elements from legacy shell IDs to solver elements without mutating input.
- [x] Project actual solver SpatialLoadContribution into optional static diagnostics.spatial_loads, remapping all node IDs through canonical topology.
- [x] Add immutable C# SpatialLoadAudit/SpatialLoadItemAudit/SpatialNodalLoad DTOs and strict JSON/semantic validation. Extend Angular validation and the shared JSON schema with the same optional static-only contract.
- [x] Add an actual Python-solver bridge-static fixture, case-isolation and inclined/public-node regression tests, malformed-audit coverage and C# immutability tests.
- [x] Update Japanese spatial input and results documentation.
- [x] Implement the assigned post-review pure geometry validator and eight regressions; coordinate its THREE integration and case/hole fixes with the lead and input owner.

## Files Modified

- FrameWeb/src/fem/file_io.py: explicit 2D request guard.
- FrameWeb/src/fem/spatial_loads/serialization.py: selected-case normalized boundary and legacy shell namespace mapping.
- FrameWeb/src/fem/result_projection.py: immutable assembly contribution to public audit projection.
- FrameWeb/src/fem/result_contracts.py: strict optional static audit validation.
- FrameWeb/tests/io/test_bridge_load_cases.py: 22 targeted bridge contract tests.
- FrameWeb/tests/data/contracts/analysis-result-set-v1.schema.json: optional static audit schema.
- FrameWeb/tests/data/contracts/positive/bridge-static.json: actual area-load solution with audit.
- FrameWeb/docs/wiki/spatial-loads.md and results.md: input/output contract documentation.
- FrameWebforCS/calculation/CalculationResultModels.cs, CalculationResultJson.cs, CalculationResultValidator.cs: immutable DTO, mapping, validation.
- FrameWebforCS/tests/BridgeLoadAuditTests.cs: fixture, malformed data, nonstatic rejection and immutability tests.
- FrameWebforJS/src/app/providers/analysis-result-set.ts and analysis-result-set.spec.ts: compatible strict validator/type and regression cases.
- FrameWebforCS/three/BridgeLoadValidation.cs and tests/BridgeLoadGeometryValidationTests.cs: plane/domain/hole/strip validation before display or printing, plus eight regressions.

## Key Decisions

- Case-local panel elements are legacy shell IDs; root normalized panel elements retain existing internal element semantics.
- Audits use existing feature values spatial_line/spatial_area, Vector3Value / {x,y,z} vectors, global force/moment coordinates and public topology node IDs.
- Audit values come from the solver assembly snapshot, exclude ordinary loads, and are never reintroduced into input.
- Missing audit is omitted, explicit null is rejected; nonlinear/modal schemas remain unchanged.
- Avoid dependency upgrades or lockfile creation to repair an unrelated missing Angular environment.

## Validation

- Python focused IO/spatial/general-plane/result-contract/solver integration: 139 passed, exit 0.
- Python result projection and physical spatial acceptance: 50 passed, exit 0.
- After strengthening distinct-case signed intensities: bridge regressions 22 passed, exit 0.
- C#: dotnet test FrameWebforCS/tests/FrameWebforCS.Tests.csproj --filter 'FullyQualifiedName~BridgeLoadAuditTests|FullyQualifiedName~CalculationResultContractTests' --no-restore --verbosity minimal: 34 passed, exit 0; build succeeded with existing warnings.
- TypeScript: actual edited validator executed via Node 24 --experimental-transform-types against 7 shared positive fixtures and 12 malformed/nonstatic audits: 19 passed, exit 0. This is runtime contract verification, not Angular compilation.
- Canonical Angular focused test could not start because ng/node_modules are absent; locked npm ci also failed because FrameWebforJS/package-lock.json is absent. No dependency or manifest changes made.
- .agents/check.ps1 -AgentOnly -AllowProductPath 'FrameWeb;FrameWebforJS;FramePrintPDF': overall "pass", exit 0. Log: .agents/logs/check-20261006T152551421Z-43000.log. Product gates intentionally skipped in this scoped infrastructure run; focused product checks are listed above.
- git diff --check for owned files passed.
- Geometry helper plus THREE regression filter after review fixes: 17 passed, exit 0. The .NET slot was released to PDF/lead afterward.
- The earlier unscoped .agents/check.ps1 was stopped at the lead's request after more than 15 minutes and approximately 92% Python progress, by terminating only the verified child Python process. Session 90329 completed exit 1; full Python suite is not claimed as passing. Log: .agents/logs/check-20261006T152149520Z-45708.log. Wrapper solution build passed; initial scope and unavailable Angular gates failed.
- Pre-interruption Python errors/failures map to four mesh-convergence tests and two performance tests. Narrow reproductions show KeyError node_displacements at mesh_convergence.py:67 and KeyError metadata at performance.py:386. Those unchanged benchmark consumers expect the former result format, while HEAD FemModel.run already returns AnalysisResultSet. Full details are in review-tests-bridge-deck-loads-3d.md; no broad rerun or unrelated benchmark edit.

## Communication with Teammates

- Lead: agreed exact audit DTO/schema, additive compatibility, case boundary and validation evidence; released .NET build slot after 34 passing tests for the lead's native smoke.
- Input: confirmed case-local normalized input and direction modes global/normal, with shell remapping and explicit 2D rejection.
- PDF: provided audit DTOs and actual bridge-static fixture; coordinated exclusive .NET build/test slot.

## Issues Encountered

- An inclined regression initially guessed direction mode panel_normal; established mode is normal. Corrected the test and recorded memory. All 22 bridge regressions pass.
- Native Node strip-only mode cannot parse existing TypeScript constructor parameter properties. The built-in --experimental-transform-types mode successfully executed the unchanged source.
- Angular dependency/lockfile absence prevents its canonical build/test gates in this checkout; independent actual TS runtime contract checks passed.
- Default agent checker needs product scope flags and runs the full Python suite. Corrected scoped agent-only command passed; the original long full run was stopped and its limitations reported, without a duplicate broad run.
