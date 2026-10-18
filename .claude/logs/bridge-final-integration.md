# Integration: 3D Bridge Deck Loads

## Quality Checks

overall: pass

```json
{
  "scope-isolation": {
    "status": "pass",
    "exit_code": 0,
    "command": [],
    "detail": "checked 52 changed path(s); product scope is isolated",
    "changed_paths": [
      ".agents/docs/DESIGN.md",
      ".agents/docs/plans/bridge-deck-loads-3d.md",
      ".agents/docs/research/review-quality-bridge-deck-loads-3d.md",
      ".agents/docs/research/review-security-bridge-deck-loads-3d.md",
      ".agents/docs/research/review-tests-bridge-deck-loads-3d.md",
      ".agents/STATE.md",
      "FramePrintPDF/PDF_Manager.Tests/PrintLibraryTests.cs",
      "FramePrintPDF/PDF_Manager/PrintData.cs",
      "FramePrintPDF/PDF_Manager/Printing/Diagram3D/PrintBase3dDiagram.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintInput/BridgeLoadTables.cs",
      "FramePrintPDF/PDF_Manager/PrintInput.cs",
      "FrameWeb/docs/wiki/results.md",
      "FrameWeb/docs/wiki/spatial-loads.md",
      "FrameWeb/src/fem/file_io.py",
      "FrameWeb/src/fem/result_contracts.py",
      "FrameWeb/src/fem/result_projection.py",
      "FrameWeb/src/fem/spatial_loads/serialization.py",
      "FrameWeb/tests/data/contracts/analysis-result-set-v1.schema.json",
      "FrameWeb/tests/data/contracts/positive/bridge-static.json",
      "FrameWeb/tests/io/test_bridge_load_cases.py",
      "FrameWebforCS/AppRoutingModule.cs",
      "FrameWebforCS/calculation/CalculationRequest.cs",
      "FrameWebforCS/calculation/CalculationResultJson.cs",
      "FrameWebforCS/calculation/CalculationResultModels.cs",
      "FrameWebforCS/calculation/CalculationResultPresentation.cs",
      "FrameWebforCS/calculation/CalculationResultValidator.cs",
      "FrameWebforCS/components/input/InputBridgeLoadComponent.cs",
      "FrameWebforCS/components/input/InputBridgeLoadService.cs",
      "FrameWebforCS/components/input/InputLoadService.cs",
      "FrameWebforCS/components/menu/SidebarComponent.cs",
      "FrameWebforCS/components/print/PrintDialogForm.cs",
      "FrameWebforCS/providers/InputDataService.cs",
      "FrameWebforCS/providers/printing/BridgePrintProjection.cs",
      "FrameWebforCS/providers/printing/PrintProjection.cs",
      "FrameWebforCS/tests/BridgeLoadAuditTests.cs",
      "FrameWebforCS/tests/BridgeLoadGeometryValidationTests.cs",
      "FrameWebforCS/tests/BridgeLoadInputTests.cs",
      "FrameWebforCS/tests/BridgeLoadPrintTests.cs",
      "FrameWebforCS/tests/BridgeLoadSmoke/BridgeLoadSmoke.csproj",
      "FrameWebforCS/tests/BridgeLoadSmoke/Program.cs",
      "FrameWebforCS/tests/BridgeLoadThreeTests.cs",
      "FrameWebforCS/tests/BridgeLoadUiTests.cs",
      "FrameWebforCS/tests/FrameWebforCS.Tests.csproj",
      "FrameWebforCS/tests/PrintProjection.Tests/PrintProjection.Tests.csproj",
      "FrameWebforCS/tests/PrintUi.Tests/PrintDialogFormTests.cs",
      "FrameWebforCS/three/BridgeLoadGeometry.cs",
      "FrameWebforCS/three/BridgeLoadValidation.cs",
      "FrameWebforCS/three/ThreeBridgeLoadsService.cs",
      "FrameWebforCS/three/ThreeService.cs",
      "FrameWebforCS/three/ViewportTextLabels.cs",
      "FrameWebforJS/src/app/providers/analysis-result-set.spec.ts",
      "FrameWebforJS/src/app/providers/analysis-result-set.ts"
    ],
    "baseline_ref": null,
    "allowed_product_paths": [
      "FramePrintPDF",
      "FrameWeb",
      "FrameWebforJS"
    ],
    "product_paths": [
      "FramePrintPDF/PDF_Manager.Tests/PrintLibraryTests.cs",
      "FramePrintPDF/PDF_Manager/PrintData.cs",
      "FramePrintPDF/PDF_Manager/Printing/Diagram3D/PrintBase3dDiagram.cs",
      "FramePrintPDF/PDF_Manager/Printing/PrintInput/BridgeLoadTables.cs",
      "FramePrintPDF/PDF_Manager/PrintInput.cs",
      "FrameWeb/docs/wiki/results.md",
      "FrameWeb/docs/wiki/spatial-loads.md",
      "FrameWeb/src/fem/file_io.py",
      "FrameWeb/src/fem/result_contracts.py",
      "FrameWeb/src/fem/result_projection.py",
      "FrameWeb/src/fem/spatial_loads/serialization.py",
      "FrameWeb/tests/data/contracts/analysis-result-set-v1.schema.json",
      "FrameWeb/tests/data/contracts/positive/bridge-static.json",
      "FrameWeb/tests/io/test_bridge_load_cases.py",
      "FrameWebforJS/src/app/providers/analysis-result-set.spec.ts",
      "FrameWebforJS/src/app/providers/analysis-result-set.ts"
    ],
    "disallowed_product_paths": []
  },
  "agent pytest": {
    "status": "pass",
    "exit_code": 0,
    "command": [
      "uv",
      "run",
      "--project",
      "FrameWeb",
      "--locked",
      "--extra",
      "dev",
      "python",
      "-m",
      "pytest",
      ".agents/tests",
      "-q"
    ],
    "detail": ""
  },
  "repository detector": {
    "status": "pass",
    "exit_code": 0,
    "command": [
      "uv",
      "run",
      "--project",
      "FrameWeb",
      "--locked",
      "--extra",
      "dev",
      "python",
      ".agents/skills/init/detect_stack.py",
      "--project-root",
      "."
    ],
    "detail": ""
  },
  "STATE contract": {
    "status": "pass",
    "exit_code": 0,
    "command": [
      "uv",
      "run",
      "--project",
      "FrameWeb",
      "--locked",
      "--extra",
      "dev",
      "python",
      ".agents/skills/_shared/validate_doc.py",
      "--contract",
      "state-doc",
      "--file",
      ".agents/STATE.md"
    ],
    "detail": ""
  },
  "DESIGN contract": {
    "status": "pass",
    "exit_code": 0,
    "command": [
      "uv",
      "run",
      "--project",
      "FrameWeb",
      "--locked",
      "--extra",
      "dev",
      "python",
      ".agents/skills/_shared/validate_doc.py",
      "--contract",
      "design-doc",
      "--file",
      ".agents/docs/DESIGN.md"
    ],
    "detail": ""
  },
  "plan contract": {
    "status": "pass",
    "exit_code": 0,
    "command": [
      "uv",
      "run",
      "--project",
      "FrameWeb",
      "--locked",
      "--extra",
      "dev",
      "python",
      ".agents/skills/_shared/validate_doc.py",
      "--contract",
      "plan-doc",
      "--file",
      ".agents/docs/plans/agents-repository-alignment.md"
    ],
    "detail": ""
  },
  "live script references resolve": {
    "status": "pass",
    "exit_code": 0,
    "command": [],
    "detail": ""
  },
  "foreign project references absent": {
    "status": "pass",
    "exit_code": 0,
    "command": [],
    "detail": ""
  },
  "agent caches absent": {
    "status": "pass",
    "exit_code": 0,
    "command": [],
    "detail": ""
  },
  "git diff --check": {
    "status": "pass",
    "exit_code": 0,
    "command": [
      "git",
      "diff",
      "--check"
    ],
    "detail": ""
  },
  "product gates": {
    "status": "skipped",
    "exit_code": null,
    "command": [],
    "detail": "AgentOnly requested"
  }
}
```

## Product Verification

- Desktop: 108 passed.
- Print projection: 32 passed; PDF library: 55 passed; print UI: 8 passed.
- Python focused: 189 passed; lead bridge cases: 22 passed.
- Native GL/pythonnet/save/reload/PDF smoke: passed.
- Solution build: passed with existing warnings.
- Six-page PDF rendered and inspected after all fixes.
- Ownership: no overlaps, unowned paths or idle owners. Seven work logs validated.
- Broad Python suite interrupted after six pre-existing outdated benchmark-consumer failures. Angular full gate unavailable without dependencies/lockfile; actual TS validator19 runtime checks passed. No full-suite pass claimed.

## Review

Security, quality and test review findings resolved. See .agents/docs/research/review-{security,quality,tests}-bridge-deck-loads-3d.md.
