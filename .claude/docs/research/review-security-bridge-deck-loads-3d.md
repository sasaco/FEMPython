# Security and Data Integrity Review: 3D Bridge Deck Loads

## Summary
Reviewed the feature's data boundaries, solver audit provenance, display/print consistency, and resource guards. Five concrete issues were coordinated with the lead and owners. Their fixes are present in source; the lead owns the final combined regression and native smoke run.

## Review Scope
- FrameWeb normalized/legacy spatial adapters, 2D rejection, static audit projection, and Python result validation.
- FrameWebforCS audit DTO parsing/validation, result revision matching, bridge renderer/geometry, printing projection, and input validation/migration.
- FramePrintPDF bridge table layout, image handling, text limits, and cancellation behavior.
- Reviewed others' backend, PDF, and THREE work independently. Input implementation was authored by this reviewer; the input issues below were corrected after explicit lead authorization.

## Findings

### High / P1 — Invalid area geometry could be printed as valid loading

Original location: `FrameWebforCS/three/ThreeBridgeLoadsService.cs`, `DrawAreaLoad`, and `BridgeLoadGeometry.ClipToTriangle`.

The display clipped area strips against triangle half-planes without validating coplanarity, complete outer-boundary containment, or strip validity. Off-plane paths could produce a filled polygon with no arrows and no error. Exterior portions were silently clipped although the solver rejects extrapolation. `ApplyPrintView` accepted the resulting nonzero load count, so an input-only report could show loading the solver would reject.

Fix present: `BridgeLoadValidation.Validate` checks the relevant geometry before drawing; per-load scene objects and labels are removed on failure. The lead added geometry regressions and owns their final execution. A tolerance-parity correction was also requested: the solver uses `max(absolute, relative * scale)`.

### High / P1 — Independent loading-mesh winding reversed normal-force arrows

Original location: `FrameWebforCS/three/ThreeBridgeLoadsService.cs`, `Rebuild` / `GetTriangles`.

Normal direction was derived from the independent loading mesh when one existed. The solver derives it from the structural transfer cells. Independent loading triangles may have the opposite winding while still being valid, resulting in display arrows opposite to the applied solver force.

Fix present: normal direction now uses `GetTriangles(panel, structural: true)`; loading triangles remain the footprint domain. An independent Python probe confirmed a valid reversed loading mesh with solver normal `[0,0,1]` while the original display calculation returned `[0,0,-1]`. The lead added a reversed-mesh normal regression.

### Medium / P2 — PDF text wrapping bypassed resource and cancellation guards

Original location: `FramePrintPDF/PDF_Manager/Printing/PrintInput/BridgeLoadTables.cs`, `PrintInit` / `Wrap`.

A single large name cell below the 64 MB request cap could trigger millions of font measurements and allocate its wrapped lines before the expanded-row limit was checked. Cancellation was checked only before entering the row, making this work uninterruptible.

Fix inspected: bounded cell/header/document text, a shared wrapped-line budget, and cancellation checks inside the character loop and row-copy loop. Limits are checked before layout. PDF owner supplies focused oversized-text/cancellation test evidence; final combined verification remains with the lead.

### Medium / P2 — Independent hole IDs were checked in the structural namespace

Original location: `FrameWebforCS/components/input/InputBridgeLoadService.cs`, `Build`.

Hole rings were always checked against structural node IDs. The backend explicitly interprets holes in the independent loading-node namespace when that mesh is present. A valid independent mesh with hole nodes `105,108,107,106` and structural nodes `1,2,3` therefore failed import or remained an invalid draft.

Fix present: hole validation selects the loading-node universe when applicable. Regression `IndependentLoadingMeshHoleIdsRemainInTheirOwnNamespace` covers import, save/reload, and calculation projection.

### High / P1 — Root normalized shell IDs changed meaning during migration

Reported independently by the PDF/quality reviewer; input fix coordinated through the lead. Original location: `InputBridgeLoadService.MigrateSolverBridge`.

Root normalized `spatial_loads.panels[].elements` uses unified solver element IDs, while case-local `spatial_loads` uses public shell IDs. Copying those values unchanged made a valid root input fail or reference the wrong shell after conversion.

Fix present: root-only migration reconstructs the backend allocation rule (maximum public member/shell/solid ID plus one, shell insertion-order collision allocation) and maps unified references back to public shell IDs. Case-local records remain unchanged. Regression `RootNormalizedShellReferencesMigrateFromUnifiedToPublicIds` covers member/shell ID collisions.

## Validation and Positive Controls
- Independent Python geometry probe exited 0: reversed independent winding is valid and exposes the former sign mismatch; off-plane and exterior paths raise solver errors.
- Strict audit validation rejects non-finite vectors, invalid lengths/errors, duplicate load/node IDs, and unknown topology nodes.
- Audit values originate from the actual solver contribution and are immutable result data. They are not projected back into calculation input.
- Audit display/printing requires the calculation input revision to match the current document. The asynchronous calculation path checks the captured revision before committing results.
- 2D UI/print guards retain bridge input and reject incompatible calculation; no silent dimensional conversion was found.
- PDF text is drawn as data; no new executable command, remote resource fetch, credential, or unsafe path handling was introduced.
- No .NET commands were run during this review because the lead held the shared build lock. Earlier lead evidence: desktop 66 passed, native smoke passed, Python 189 passed, and PDF focused suites passed. Added defect regressions require the final lead run.

## Verdict
Concrete findings are addressed in source, with final combined test and native smoke confirmation pending at handoff. No additional unresolved security finding is asserted.

## Final Lead Verification

After handoff, the lead independently passed 108 desktop tests (including both namespace regressions and geometry fixes), 32 print projection tests, 22 bridge-case Python tests, the native save/reload/solver/OpenGL/PDF smoke, and the solution build. No unresolved finding remains from this review.
