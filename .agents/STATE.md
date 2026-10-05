# Agent State

## Main Agent

Codex

## Repository Identity

FrameWeb3 is a Windows-oriented monorepo for web-based structural frame analysis.

- `FrameWeb/`: Python FEM engine and Flask/functions-framework calculation API, managed with `uv`.
- `FrameWebforJS/`: Angular 15 browser/Electron client, managed with npm.
- `tools/FrameWeb.Startup/`: .NET 8 local launcher and print HTTP host.
- `FramePrintPDF/`: .NET printing and PDF projects.
- `FrameGConverter/`: separate conversion utility.

Windows PowerShell is the canonical development shell. Macro requirements and durable design decisions live in [docs/DESIGN.md](docs/DESIGN.md).

## Progress Tracker

Rolling progress summary (latest 5 checkpoints): [PROGRESS.md](../PROGRESS.md)

<!-- Working state below is maintained by workflow skills and manual notes. -->

## Current Bug Fix: FrameGConverter Pile Springs
<!-- orchestra:block-id: framegconverter-pile-springs -->

### Context

- Implemented the requested base `$4.txt` interval lengths, `$5.txt` transverse/axial TYPE pairs, and `$6.txt` J-end support TYPE rows for member names containing `杭`.
- Shared conversion code lives in `FrameGConverter/Convert_Manager`; interval rows serialize optional `fix_member.length`, with axial `tx` and transverse `ty`. Existing full-member springs are superposed and zero intervals retain their positions.
- Apply pile springs before rigid-zone splitting. Splits partition intervals, retain J-end supports, preserve coordinate precision for interval springs, and re-fetch geometry before splitting the opposite end.
- Files without pile spring tables remain convertible. Indexed alternate pile profiles are outside this request; current Angular import drops interval length, while the desktop consumer supports it.

### Validation

- `dotnet test FrameGConverter/Convert_Manager.Tests/Convert_Manager.Tests.csproj -c Release --no-restore`: 19 passed, including real LZH FRD, four TYPEs, malformed tables, zero intervals, existing springs, and 45-degree piles with rigid zones at both ends.
- `dotnet build FrameGConverter/FrameGConverter.sln -c Release --no-restore`: passed, with the existing NETSDK1138 warning for the legacy netcoreapp3.1 GUI.
- Independent review identified coordinate rounding versus interval length; fixed and covered by the 45-degree regression.

---

## Current Feature: Load Sheet Fixed Rows
<!-- orchestra:block-id: load-sheet-fixed-rows -->

### Context

- User approved `.agents/docs/plans/load-sheet-fixed-rows.md` on 2026-09-28. All questions resolved; row-header multirow Del removes the selected index set simultaneously and compacts survivors in display order.
- Sheet2 and IntensityRows stay at exactly MaxNodeId (100,000); anonymous slots have empty LoadId. Backslash shifts all following display slots across Cases, rejects an assigned final slot, and preserves blank positions through save/reload.
- Owned work: service (`InputLoadService.cs`, `InputDataService.cs`), UI (`InputLoadComponent.cs`, existing shared keyboard hook if needed), focused tests. Task list: `.agents/logs/task-list-team-execute-load-sheet-fixed-rows.md`.
- User/concurrent InputLoadComponent auto-width comment and SidebarComponent changes are preserved.

### Validation

- Implementation complete. Actual-product isolated runner: 96 passed, 0 failed, 0 skipped; final TRX `.tmp/InputLoadFixedRows/TestResults/fixed-rows.trx`. Product build passes with 0 errors.
- Independent security/quality/test reviews found a direct replacement Reset ordering defect; corrected and verified by both-list observer regression. Whole-document malformed layout/capacity rollback regressions pass. Review artifacts: `.agents/docs/research/review-{security,quality,tests}-load-sheet-fixed-rows.md`.
- STA performance samples for empty/mixed/99,999 assigned rows: display 1.588/1.206/1.444 seconds, insertion 62/41/822 ms, deletion 49/42/536 ms, reload 51/42/1,420 ms. Post-stage working-set samples 402/402/545 MiB (not peak or retained memory); raw evidence and limits are in the quality review.
- Full solution build remains blocked by 12 preexisting LoadDisplayConversion CS0103 errors. AgentOnly substantive gates pass; overall fails only concurrent SidebarComponent trailing whitespace. Coverage not measured. Bounded nested quality CLI timed out; its partial LoadId display hypothesis is disproved by five actual editor/display regressions, and native independent review completed.

### Decisions

- Keep Case-local row identities separate from global display slots. Persist optional sparse load_intensity_layout metadata only in saved inputs, exclude it from calculation snapshots.
- Do not add an analysis Case-count ceiling or modify backend/Angular/converter behavior.

---

## Current Bug Fix: framewebforjs-calculation-communication-error
<!-- orchestra:block-id: framewebforjs-calculation-communication-error -->

### Context

- Error: Ct桁プリセットの計算で汎用通信エラー。ブラウザーと直接HTTPで再現し、POSTはHTTP 400 invalid_input / Extra data: line 1 column 3を返す。
- Root cause: FrameWebforJSはgzip Uint8Arrayをbtoaへ直接渡してBase64(括弧なしCSV)を送るが、29df328以降のFrameWebは安全なjson.loadsでBase64(JSON整数配列)を要求する。失敗はモデル解析前。
- Affected files: FrameWebforJS/src/app/app.component.ts、FrameWeb/main.py。印刷側は別C#契約で括弧なしCSVが正規形式。
- Separate blocker: transport修復後もバックエンドのflat結果とフロントのcase別disg/reac/fsec契約が不一致の疑いがあり、Ctの複数荷重ケース仕様決定が必要。

### Fix Approach

- Milestone T: 計算専用の純粋エンコーダーを抽出し、btoa(`[${compressed.join(',')}]`)相当で現行JSON整数配列契約へ適合。バックエンド、ヘッダー、印刷エンコーダーは変更しない。実装結果は transport repaired とだけ報告する。
- Milestone U: 荷重ケース数・ID/順序・変位/反力/断面力の単位/符号/キー/変換所有者を先に決定し、case別disg/reac/fsecの失敗テストとアダプター/応答変換を別承認で実装する。Ct E2E通過のみをユーザー向け修復完了とする。
- 旧配布済み計算クライアント対応が明示的に必要な場合だけ、厳格制限・計測・削除条件付きCSV互換分岐を一時導入する。eval/literal_evalは禁止。

### Codex Validation

- 初回計画検証はNEEDS_REVISION: transport成功とuser成功を分離し、複数荷重ケース結果契約を実装前ゲートにするよう指摘。
- 改訂二段階計画はPASS。Milestone T PASS=transport repaired、Milestone U未承認/未通過=全体未完了、Milestone U E2E PASS=ユーザー向け修復完了。
- 追加必須: 実HTTPヘッダー検証、flat結果を成功扱いしない回帰、結果契約決定時の具体的ランナー/サービス起動コマンド。

### Regression Risks

- 計算専用正規化は低リスク。共有エンコーダーで印刷まで変更するとC# Convert.ToByte契約を壊すため高リスク。
- 大型ラーメン高架橋プリセットはgzip約2,025,295 bytes、Base64約9.65 MB。Array.fromで約202万要素をBox化せずjoinを使い、Chrome完走と性能を確認する。
- HTTP 200、ダイアログ消失、isCalculated=true、空worker結果は完了根拠にならない。
- 互換サーバー分岐は入力面とgzip bomb面を広げるため、必要性が証明された場合のみ。

### Decisions

- 29df328のeval除去は正しいセキュリティ修正であり、戻さない。
- 今回の観測エラーは全圧縮計算に共通するtransport契約不一致で、Ct固有データは直接原因ではない。
- 製品コードは調査段階では変更しない。実装は二段階計画の承認後に行う。

---

## Current Bug Fix: framewebforjs-results-not-displayed
<!-- orchestra:block-id: framewebforjs-results-not-displayed -->

### Context

- Symptom: Ct桁プリセットはHTTP 200で計算できるようになったが、FrameWebforJSの変位・反力・断面力が表示されない。
- Direct reproduction: 現行backendの復号結果はnode_displacements/reaction_forces/element_stresses等のflat schemaで、frontend互換のdisg/reac/fsec caseは0件。
- Root cause: 旧backendは全load caseを入力順で解いて{caseId: {disg,reac,fsec,shell_fsec,size}}を返す。現行backendはselect_caseで先頭caseだけを選び、1回のFemModel.run()のflat resultを返す。frontend workerはfield不一致を黙ってskipし、空mapをerror:nullで成功扱いする。
- Scope: Ct固有ではなくFrameWebforJSから送る旧node/load形式全体。Ctはcase 1～11のため、単なるshape wrapperではcase 2～11が欠落する。

### Fix Approach

- 既定の現行flat HTTP/Python contractは維持する。FrameWebforJSが明示的に要求するversioned compatibility representation（仮称legacy-cases-v1）をbackend HTTP境界へ追加する。
- compatibility pathでは全load caseを入力順にfresh modelで解析し、旧disg/reac/fsec/shell_fsec/sizeへproduction projectorで変換する。単位、符号、rate、member P1..Pn順序は旧backend goldenで固定する。
- FrameWebforJSはversionを明示し、worker起動前にnon-empty case-mapと必須3 fieldを検証する。不適合時はisCalculatedをtrueにせず、明示errorを表示する。
- test-firstで既定flat不変、Ct 11 case、case固有definition、rate、途中失敗の原子性、browser表示を検証する。

### Codex Validation

- UNAVAILABLE: lead 2回、root-cause analyst 1回、impact investigator 1回のbounded read-only相談はいずれもtimeoutまたは空responseで、正式なverdictなし。
- 診断はdirect HTTP reproduction、旧/current/frontend code、git history、既存testsを突合して確定。Codex outputを根拠として使用していない。

### Regression Risks

- 既定HTTP responseを旧case-mapへ置換すると文書化済みflat contract、現行client、広範なbackend testsを破壊するため禁止。
- frontend-only変換では解析されていないcase 2～11を復元できず、mesh由来のsign/order規則をTypeScriptへ重複するため不採用。
- Ctは11 sequential solveとなるためruntime/memory/timeoutを測定し、case間で可変FemModel stateを共有しない。
- shell_fsecはCtに含まれず現行helperともschemaが違う。旧契約互換を名乗る前に別oracleが必要。
- HTTP 200、transport test、success alert、isCalculated=trueだけでは表示修正完了と判定しない。

### Decisions

- 推奨案は、既定flatを保持した明示的/versioned backend compatibility adapterとfrontend fail-fastの組み合わせ。
- transport/input形式からresponse versionを暗黙推測しない。plain/canonical compressed/legacy compressedはいずれもtransportでありschema selectorではない。
- 本セッションは診断とhandoffまで。表示互換adapterは未実装。次セッションはlegacy-cases-v1の選択方法を確定し、contract testから開始する。
- 詳細は.agents/logs/troubleshoot-framewebforjs-results-not-displayed-diagnosis.md、root-cause/impact reportを参照。

---

## Current Feature: C# FrameWeb Desktop Client Step 2
<!-- orchestra:block-id: c-frameweb-desktop-client-step-2 -->

### Context

- Goal: Complete typed document/result foundations before docking UI work.
- Key files: FramePrintPDF/PDF_Manager.Core/{Documents,Analysis,Results,Abstractions}/** and matching Core.Tests paths.
- Dependencies: package-free .NET 8 Core plus shared FrameWeb AnalysisResultSet fixtures.
- Complexity: COMPLEX

### Architecture

- ProjectDocument v1 persists validated inputs only; runtime result, selection, and dirty state are excluded.
- AnalysisResultSet v1 uses immutable DTOs, strict parsing/semantic validation, ResultCoordinate indexing, and validate-before-commit state.
- Derived and moving-load behavior is presentation-only and cannot mutate base results.

### Codex Validation

- Core tests 75/75; both solutions 95/95; shared Python contract tests 14/14.
- Both Release solution builds pass; clean root build exposes only the 28 known LegacyPrinting warnings.
- Repository-wide Python/Angular known failures remain separate and are not reported green.

### Integration Points

- Step 3 shell consumes ProjectDocument dirty/selection state and typed operation errors.
- Step 4 FrameWebAnalysisClient implements IAnalysisClient and commits only fully validated results.
- Step 8 typed printing implements IPrintExporter without exposing legacy dictionaries.

### Decisions

- DEFINE/COMBINE use weighted linear combination; PICKUP chooses signed greatest-absolute component; moving-load envelopes retain source-case provenance.
- The next implementation step is Step 3 desktop shell/docking lifecycle.

---

## Current Feature: C# FrameWeb Desktop Client Step 3
<!-- orchestra:block-id: c-frameweb-desktop-client-step-3 -->

### Context

- Goal: Complete the WinForms desktop shell and docking lifecycle before the vertical MVP.
- Key files: FramePrintPDF/PDF_Manager/Shell/{MainForm.cs,Contents/**,Docking/**,Lifecycle/**}, Resources/**, and PDF_Manager.UiTests/**.
- Dependencies: DockPanelSuite 3.1.1, typed Step 2 Core contracts, and WinForms STA execution.
- Complexity: COMPLEX

### Architecture

- Stable DocumentKey identities feed a whitelist-only creating-thread DockContentRegistry; tools hide/reuse and documents dispose/remove.
- LayoutState v1 is size/count bounded, strict UTF-8, atomically stored, and transactionally restored with live tab order and floating bounds; docked ratios/auto-hide remain unsupported.
- Document-changing commands are serialized and revision checked; activation is coalesced/cancellable; active work, dirty save, and layout persistence use bounded shutdown with late publication rejection.

### Codex Validation

- UiTests 74/74; both solutions 168/168 (Core 75, composition/Printing 9, Rendering 10, UI 74).
- Both Release solution builds, targeted shell/UI formatting, ownership reconcile, delegated guardrails, work-log/document contracts, git diff check, and AgentOnly gate pass.
- Coverage percentage is not measured; unchanged Python/Angular full suites were not rerun and retain the known 3,273 PASS / 6 FAIL / 4 ERROR baseline.

### Integration Points

- Step 4 implements FrameWebAnalysisClient behind IAnalysisClient with JSON byte/entity/result limits, cancellation, transport timeout, and strict AnalysisResultSet validation before commit.
- Step 4 replaces the placeholder document viewport with the first OpenGL model/result slice and adds minimum editors plus FrameWeb.LocalRuntime.
- Step 8 typed printing implements IPrintExporter; the isolated legacy Azure/local print host remains outside the desktop boundary.

### Decisions

- Dirty-save timeout aborts close; layout-save timeout is diagnosed but permits close because layout can fall back to defaults.
- Unexpected OperationCanceledException is handled as a normal failure unless the owned token is actually canceled.
- The next implementation step is Step 4 end-to-end vertical MVP; legacy host publication and completed-app redistribution remain NO-GO.

---

## Current Feature: C# FrameWeb Desktop Client Step 4
<!-- orchestra:block-id: c-frameweb-desktop-client-step-4 -->

### Context

- Goal: Complete the first end-to-end desktop vertical MVP from typed project editing through local Python analysis, live OpenGL inspection, and typed PDF export.
- Key files: FramePrintPDF/PDF_Manager{.Core,.Rendering,.Printing}/**, FramePrintPDF/PDF_Manager.UiTests/**, tools/FrameWeb.LocalRuntime/**, and FrameWeb/main.py.
- Dependencies: .NET 8 WinForms, OpenTK 4, uv-managed Python/Flask, AnalysisResultSet v1, and Windows Job Objects.
- Complexity: COMPLEX

### Architecture

- ProjectDocument edits use a bounded undo/redo session and deterministic request serialization; FrameWebAnalysisClient applies byte/entity/result/time/concurrency bounds and commits only a fully validated AnalysisResultSet.
- FrameWeb.LocalRuntime starts only Python, keeps a per-launch secret private, verifies that the loopback listener belongs to its Job, and exposes a preconfigured HttpClient only after readiness.
- The UI owns a typed Z-up scene, live UI-thread viewport capture, static result tables/layer, and a dependency-free typed one-page PDF path with an independent fail-closed raster golden.

### Codex Validation

- Both Release solutions build with 0 warnings/errors and both solution test runs pass 253/253 (Core 112, composition/Printing 17, Rendering 27, LocalRuntime 14, UI 83).
- Targeted Python local-runtime/result transport tests pass 165/165; RendererProbe passes 20 contexts, 200 frames, 60 captures with all live counters zero, and independent repeated/concurrent WGL review also passes.
- Independent Step 4 security, quality, and test reviews pass with no Critical/High/Medium Step 4 findings; coverage percentage remains unmeasured.

### Integration Points

- Step 5 expands the representative editor slice into the complete model-input matrix and shared grid behaviors.
- Steps 6 and 7 extend the typed scene/result boundaries rather than adding a legacy adapter.
- Step 8 replaces the remaining legacy print host only after CJK licensing, pagination, dependency, golden, and publish decisions are complete.

### Decisions

- The desktop runtime never starts Angular and never exposes its local bearer token; listener Job ownership is part of readiness and request acceptance.
- Live viewport capture is the single diagram source for the vertical PDF slice and must run inside the UI exception boundary.
- Step 4 is complete; next is Step 5. Legacy-host publication and completed-app redistribution remain NO-GO.

---

## Current Feature: C# FrameWeb Desktop Client Step 5
<!-- orchestra:block-id: c-frameweb-desktop-client-step-5 -->

### Context

- Goal: Complete the full typed model-input editor matrix, shared editing behavior, all four built-in presets, and Core validation before HTTP submission.
- Key files: FramePrintPDF/PDF_Manager.Core/Documents/**, FramePrintPDF/PDF_Manager.Core/Analysis/FrameWebAnalysisRequestJson.cs, FramePrintPDF/PDF_Manager/Shell/{Contents,Editing}/**, Resources/**, and matching Core/UI tests.
- Dependencies: Step 4 ProjectDocument v1, WinForms/DockPanelSuite shell, typed Z-up viewport, and the Python legacy analysis-input meaning.
- Complexity: COMPLEX

### Architecture

- ProjectDocument v1 remains additive, strict, deterministic, and input-only; new dimension, property/support/joint/spring sets, rigid zones, panels, notice points, prescribed displacements, and member loads are validated before commit. JSON input is bounded to 16 MiB and 100,000 aggregate entities/rows.
- One descriptor-driven EditorContent hosts 21 typed tables. Shared controller behavior owns bounded clipboard, keyboard routing, multi-row paste, insert/delete, stable selection, lifecycle, and one-batch/one-undo semantics. Dedicated tables expose model dimension, four set managers, and prescribed displacements.
- Four stable built-in presets are typed semantic builders with explicit topology/material/support/load/selector assertions and pinned canonical request hashes; no public legacy Angular importer is part of the contract.
- The Python request boundary checks effective nonzero loads, table selectors, member-load positions, case/entity limits, and numeric legacy IDs. DEFINE/COMBINE/PICKUP remain client-side and are not sent.
- Member-load rows now project to stable representative SceneMemberLoad glyphs for bidirectional table/viewport selection; full load-shape rendering remains Step 6.

### Codex Validation

- Both Release solutions build with 0 warnings/errors; both solution test runs pass 401/401 (Core 243, composition/Printing 17, Rendering 27, LocalRuntime 14, UI 100). Focused domain/request tests pass 53/53, persistence/preset tests 78/78, and Step 5 UI tests 17/17.
- Ownership reconcile reports overlap 0, unowned 0, idle 0; all team work logs validate; final AgentOnly is overall=pass at .agents/logs/check-20260920T154552806Z-28216.log.
- Final security, quality, and test closeout reviews each report Critical 0 / High 0 / Medium 0 / Low 3. Coverage percentage is not measured.
- Full Python/Angular suites were not rerun; retain the known 3,273 PASS / 6 FAIL / 4 ERROR baseline.

### Integration Points

- Step 6 extends the typed scene with complete independent input/result layers and full load-shape rendering without changing the editor document contract.
- Step 7 consumes the existing load-case, DEFINE/COMBINE/PICKUP, and request/result boundaries for complete result presentation.
- Step 8 still replaces the isolated legacy print path; legacy host publication and completed-app redistribution remain NO-GO.

### Decisions

- Keep schema version 1 and make the complete input surface additive rather than introduce a parallel document format.
- Keep persisted stable alphanumeric IDs; validate the Python service's positive-integer ID requirement only at request projection.
- Treat one final validated batch as the edit transaction and one undo entry; rejected edits preserve document, undo, and redo exactly.
- Step 5 is complete; next is Step 6 rendering and interaction parity.

---

## Current Feature: C# FrameWeb Desktop Client Step 6
<!-- orchestra:block-id: c-frameweb-desktop-client-step-6 -->

### Context

- Goal: Complete rendering and interaction parity for every typed model, load, and result layer while preserving deterministic UI/GL lifecycle behavior.
- Key files: FramePrintPDF/PDF_Manager.Rendering/Scene/ViewportSceneContracts.cs, ViewportSceneModel.cs, ViewportSceneCompiler.cs, OpenGlViewportLifecycle.cs, PDF_Manager/Shell/Viewport/**, ProjectDocumentContent.cs, RendererProbe, and Step6 Rendering/UI tests.
- Dependencies: Step 5 ProjectDocument v1 inputs and presets, AnalysisResultSet v1, OpenTK GLControl, WinForms shell.
- Complexity: COMPLEX

### Architecture

- Twelve independent stable-ID scene layers are exposed behind IViewportScene, ICameraController, and IHitTestService.
- Node/member dependency closure expands shallow diffs before cached layer reuse; invalidations coalesce and only affected render buffers are replaced.
- Scene materialization, compiled vertices/batches/hit targets/decorations, result-grid rows, PNG dimensions/pixels/bytes, and topology exploration have explicit limits.
- Grid, axes, labels, scale, and color legends are rasterized to a bounded transparent texture and alpha-composited by the same OpenGL path used for Paint and capture.

### Codex Validation

- Both Release solutions build with 0 warnings/errors and both solution test runs pass 463/463: Core 243, composition/Printing 17, Rendering 56, LocalRuntime 14, UI 133.
- RendererProbe passes 100 contexts, 1,700 frames, 600 captures, 200 PNG encodes, and final live contexts/subscriptions/windows of zero.
- The 10,000-node/9,999-member masked update recompiles only Loads and completes in 5.873 ms; all budgets and exact/+1 boundaries are covered.
- Final security, quality, and test reviews each report Critical 0 / High 0 / Medium 0. Coverage percentage remains unmeasured.

### Integration Points

- Step 7 consumes the existing active-case filtering, ordered case/state navigator, signed extrema helper, result tables, typed result layers, and stable bidirectional selection.
- Step 8 continues to capture the live decorated viewport rather than reconstructing a second diagram and must retain bounded capture/resource semantics.
- Full Python/Angular validation was not completed or rebaselined; retain the known 3,273 PASS / 6 FAIL / 4 ERROR baseline.

### Decisions

- Keep screen and PNG decorations on one bounded OpenGL overlay path; do not introduce a separate GDI export path.
- Preserve camera on ordinary document edits, reset it only after replacement scene installation, and avoid creating a GL context for an empty workspace.
- Step 6 is complete; next is Step 7 calculation and result presentation. Legacy-host publication and completed-app redistribution remain NO-GO.

---

## Current Feature: C# FrameWeb Desktop Client Step 7
<!-- orchestra:block-id: c-frameweb-desktop-client-step-7 -->

### Context

- Goal: Complete calculation result navigation, presentation, derived/moving semantics, and deterministic exports from the canonical AnalysisResultSet v1 boundary.
- Key files: FramePrintPDF/PDF_Manager.Core/Results/**, PDF_Manager/Shell/Contents/ProjectDocumentContent.cs, PDF_Manager/Shell/Viewport/**, and Step7 Core/UI tests.
- Dependencies: Step 6 typed scene/result layers, ProjectDocument derived definitions, AnalysisResultSet v1, and the private FrameWeb.LocalRuntime HTTP boundary.
- Complexity: COMPLEX

### Architecture

- Ordered static, nonlinear-step, modal-mode, and moving parent/child pages are materialized into an immutable candidate before any shell state is committed.
- DEFINE and COMBINE use weighted static operands; generic PICKUP display keeps signed greatest-absolute values, while a distinct engineering envelope retains per-focus maximum/minimum provenance and correlated force vectors for 3D CSV and 2D fixed-width export.
- Moving signed extrema use all sources, while reaction absolute projection uses child cases when present and falls back to the parent only when no child exists.
- One checked ResultPresentationBudget bounds pages, derived and moving definitions, operands, output entities, and scalar work before UI publication.

### Codex Validation

- Both Release solutions build with 0 warnings/errors and both solution test runs pass 501/501: Core 272, composition/Printing 17, Rendering 56, LocalRuntime 14, UI 142.
- Angular-derived moving ordering/reaction semantics, Ct 11-case first/last access, nonlinear/modal scenes, static-only rejection, formula-safe CSV, 3D PICKUP CSV, 2D .pik, presentation limits, invalid-state preservation, and atomic export are covered.
- Ownership reconcile reports overlap 0, unowned 0, and idle 0; AgentOnly reports overall=pass. Final security, quality, and test reviews have no Critical, High, or Medium Step 7 findings. Coverage percentage remains unmeasured.

### Integration Points

- Step 8 consumes typed result tables, engineering PICKUP envelopes, and live viewport capture through the typed Printing boundary; it must not reintroduce legacy dictionaries or alternate calculation schemas.
- Python remains the FEM implementation and the desktop continues to call it through the existing private loopback HTTP runtime; changing transport is a separate architecture task.
- Full Python/Angular validation was not rebaselined; retain the known 3,273 PASS / 6 FAIL / 4 ERROR baseline.

### Decisions

- Neutralize only untrusted text cells in spreadsheet-oriented CSV so negative engineering numbers remain numeric.
- Preserve the original exception as InnerException on export failures and keep a cleanup failure secondary to the primary operation failure.
- Step 7 is complete; next is Step 8 typed printing. Legacy-host publication and completed-app redistribution remain NO-GO.

---

## Current Feature: C# FrameWeb Desktop Client Step 8
<!-- orchestra:block-id: c-frameweb-desktop-client-step-8 -->

### Context

- Goal: Replace the legacy print path with bounded typed printing whose preview and PDF export share one authoritative immutable plan.
- Key files: FramePrintPDF/PDF_Manager.Printing/**, PDF_Manager/Shell/Printing/**, PDF_Manager/Shell/Contents/**, and Printing/UI acceptance tests.
- Dependencies: Step 7 presentation models, deterministic viewport capture, ProjectDocument v1, and AnalysisResultSet v1.
- Complexity: COMPLEX

### Architecture

- Official PDFsharp 6.2.4 sits only behind PDF_Manager.Printing; the typed desktop graph does not depend on legacy PdfSharpCore/ImageSharp or embedded restricted fonts.
- Preview and export consume the same immutable page plan, including authoritative full/display text runs, clip bounds, font metrics, diagram geometry, and shared whole-document work budgets.
- PDF/font work is process-wide serialized; installed Windows fonts are resolved lazily per requested language and rejected before allocation when the source exceeds the configured bound.
- The shell captures 21 editor tables plus Moving Loads, distinct model/load/result diagrams, and the selected static/nonlinear/modal/derived/moving result set before publishing any new preview state.

### Codex Validation

- Both Release solutions build with 0 warnings/errors and both solution test runs pass 567/567: Core 272, Printing 73, Rendering 56, LocalRuntime 14, UI 152.
- A3/A4 portrait/landscape geometry, margins, scale, page numbers, grapheme-safe table pagination/clipping, full selectable preview text, golden rendering, exact/+1 budgets, parallel serialization, atomic save, exception provenance, and failed-candidate state preservation are covered.
- Ownership reconcile reports overlap 0, unowned 0, and idle 0; AgentOnly reports overall=pass. Final reviews report Security 0/0/0/Low 1, Quality 0/0/0/Low 0, and Tests 0/0/0/Low 4. Coverage remains unmeasured, and GUI-viewer CJK glyph evidence could not be captured in the available environment.

### Integration Points

- Step 9 must complete authentication, packaging/signing/update policy, endpoint discovery, clean-machine parity, Startup cutover, and publish SBOM/forbidden-content checks.
- FramePrintAzure and the isolated legacy local print bridge remain redistribution NO-GO and are to be retired during Step 9 rather than rebuilt.
- Python remains the FEM implementation behind the private loopback HTTP/Job boundary and AnalysisResultSet v1; full Python/Angular validation retains the known 3,273 PASS / 6 FAIL / 4 ERROR baseline.

### Decisions

- Use installed per-language Windows fonts without bundling font binaries; English output does not require CJK font availability.
- Serialize process-global PDFsharp work and share one budget across whole-document preview/export.
- Retire FramePrintAzure at the Step 9 cutover instead of preserving a second printing transport and presentation contract.
- Step 8 is complete; completed-app redistribution remains NO-GO until Step 9 production gates pass.
