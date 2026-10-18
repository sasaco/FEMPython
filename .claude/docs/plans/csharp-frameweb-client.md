## Implementation Plan: C# FrameWeb Desktop Client

### Purpose

`FramePrintPDF/PDF_Manager` を中心に、`FrameWebforJS` の画面構成を画面・状態ごとに再現する Windows 専用の .NET 8 / WinForms デスクトップアプリを完成させる。受入対象は機能の意味だけではなく、shell hierarchy、navigationの配置と順序、route別の入力・結果・印刷画面、field/control、grouping、default visibility、viewport/table split、overlay、画面遷移を含む。

既存利用者、現行C# docking UI、旧印刷UIとの後方互換およびバックアップ作成は要件に含めない。Python FEM解析、型付き `ProjectDocument`、唯一の成功応答 `AnalysisResultSet v1`、private loopback process security、OpenGL描画、型付きPDF基盤は維持し、ユーザーに見える表示層を `FrameWebforJS` 準拠のroute/state shellへ載せ替える。作業ツリーには退避物や互換adapterを残さず、各vertical sliceの検証後に不要な旧UI surfaceを削除する。

### Scope

本計画は、C#デスクトップ製品の設計・実装・検証・切替までを対象とし、Python FEMの数値実装と旧クライアント互換は対象外とする。

#### Implementation Status (2026-09-21)

- Step 0〜8はtyped domain、解析、編集、描画、結果presentation、印刷の**機能基盤**として完了している。ただしStep 3/5/6/7/8のuser-visible構成はscreen-composition parity未達のため再開する。567件のgreen testsは機能非退行baselineであり、UI parityの完了証拠ではない。
- UI parityの調査、screen matrix、再利用／置換境界は `.agents/docs/research/csharp-frameweb-ui-parity.md` に記録した。現行C#は左Navigation・中央Document・右Editor・下Diagnosticsの4領域、Angularは上部menu/context header・順序付き左navigation・全面viewport・可動route panel・全面overlayであり、DockPanel配置調整では解消できない。
- `FrameWebforJS` は2026-09-21に `start:local` でcompile成功しHTTP 200を確認したが、利用可能なUI automation surfaceが無かったため新規live screenshotは未取得である。固定viewport/DPIの基準captureをUI remediation最初の必須gateとする。

- Step 0 の実装成果は commit `572a1d1` に保存済みである。`PDF_Manager.Core` と41件の契約テスト、実OpenGLを使う `PDF_Manager.RendererProbe`、依存関係・来歴・再配布可否のinventory、および `.agents/docs/DESIGN.md` の設計決定が含まれる。
- package-only の開発経路は **GO** である。DockPanelSuite 3.1.1、OpenTK.GLControl 4.0.2、OpenTK 4.9.4、および所有する最小renderer/shaderで Step 1 へ進める。
- 完成アプリの再配布は **NO-GO** のままである。MS Gothic、MS Mincho、SimSun、旧THREE shader/typeface/LTC textureは製品へコピー・同梱せず、PDF font strategy、PDF golden、publish成果物のforbidden-file/SBOM検査を後続gateで解決する。
- Step 1 は作業ツリーで完了した。`PDF_Manager` は `net8.0-windows` WinExeとなり、Core/Rendering/typed Printingを参照する空WinForms shell、4つの自動test project、両solutionへの登録が実装済みである。この記録以降にStep 2〜8も完了しており、現在の未完了範囲はscreen-composition parityとproduction cutoverである。
- Step 2 は作業ツリーで完了した。Coreへtyped `ProjectDocument v1`、厳格かつ決定的なJSON/atomic store、完全な`AnalysisResultSet v1` DTO/validator/index/commit boundary、static-only derived presentation、moving-load paging/envelope、およびtyped service boundaryを実装した。
- Step 3 は作業ツリーで完了した。localized WinForms shell、stable-key docking registry、bounded/atomic/transactional layout、serialized document transition、coalesced activation、dirty-close/cancellation/exception boundaryを実装した。
- Step 8 は作業ツリーで完了した。公式PDFsharp 6.2.4上のtyped printing、同一immutable planによるpreview/export、22入力表、model/load/result diagram、結果選択matrix、共有work budget、CJK installed-font解決、原子的PDF保存を実装した。次はStep 9のscreen-composition parity remediationであり、そのsign-off後にStep 10のproduction integration/cutoverへ進む。
- Step 1完了後にリポジトリ正規の全体検査 `& .agents/check.ps1 -AllowProductPath 'FramePrintPDF'` を再実行した。Agent系、scope isolation、`git diff --check`、`.NET build` はPASSしたが、全体は `overall=fail` である。Pythonは3,273件PASS・6件FAIL・4件ERROR（1:55:37）、Angular test/buildはTypeScript compilation、FontAwesome path、`environment.prod.ts` 不在でFAILした。詳細は `.agents/logs/check-20260920T035935489Z-32252.log` を参照する。いずれも既知のC#変更範囲外failureだが、リポジトリ全体をgreenとは報告しない。
- 独立レビューは品質PASS（Critical/Highなし）、テストPASS（Critical/Highなし）、セキュリティChanges requested（旧Azure/local print hostにHigh 2件）である。新desktop境界には旧印刷資産・制限font・既知脆弱packageは入っていない。一方、legacy hostの匿名endpointは脆弱なImageSharp 1.0.4へ到達でき、request/decompression/image/PDF workも無制限なので、公開・配布はNO-GOである。詳細は `.agents/docs/research/review-{quality,tests,security}-csharp-frameweb-client.md` を参照する。

#### Current State

- `PDF_Manager` は `net8.0-windows` WinExeであり、Core、Rendering、Printing、LocalRuntime、および自動test projectsを持つ。Python FEM、`AnalysisResultSet v1`、OpenGL、公式PDFsharp 6.2.4のtyped境界は実装済みである。
- Angularの基準は14 input routes、9 result routes、Start/Preset/Print named-outlet overlaysである（`FrameWebforJS/src/app/app-routing.module.ts:34-72`）。共通shellはmenu、optional header、ordered left navigation、full viewport、movable/resizable route panel、overlay hostで構成される（`app.component.html:1-18,958-980`）。
- 現行C# `MainForm` はNavigation=左、Editor=右、Diagnostics=下、Document=中央の4領域を初期表示する（`FramePrintPDF/PDF_Manager/Shell/MainForm.cs:102-138`）。`EditorContent` は21表を一つの`TabControl`へ生成し（`EditorContent.cs:25-81,91-115`）、`ProjectDocumentContent` は多数のselectorと常時viewport/result splitを持つ（`ProjectDocumentContent.cs:137-219`）。
- 現行UIのfunctional servicesは再利用可能だが、`NavigationContent`、generic `EditorContent`、persistent `DiagnosticsContent`、user-visible docking/layout restore、separate print dialogsはdefault UIから置換・削除する。
- 初回releaseはuser loginを持たない。private loopback bearerとWindows Job/listener ownershipは認証UIではなくlocal IPC securityとして維持する。

#### Target Architecture

```text
PDF_Manager (net8.0-windows WinForms executable / composition root)
  ├─ FrameWebforJS-parity screen composition
  │    ├─ top menu + contextual optional header
  │    ├─ ordered primary navigation + full workspace viewport
  │    ├─ route-specific movable/resizable input/result screen
  │    └─ Start/Preset/Print/operation overlays
  ├─ PDF_Manager.Core
  │    ├─ ProjectDocument + validation + undo/redo
  │    ├─ AnalysisResultSet DTO/validator/index
  │    ├─ ResultPresentationService
  │    └─ IAnalysisClient / IPrintExporter / IProjectStore
  ├─ PDF_Manager.Rendering
  │    ├─ THREE/OpenGL host + camera + hit testing
  │    ├─ model/load/result scene layers
  │    └─ deterministic viewport capture
  ├─ PDF_Manager.Printing
  │    └─ typed official PDFsharp document/layout/export pipeline
  └─ FrameWeb.LocalRuntime
       └─ Python process startup, readiness, cancellation, Job Object cleanup

PDF_Manager.Core --HTTP--> FrameWeb (Python FEM)
                         <-- AnalysisResultSet v1 only
```

依存方向は `ScreenComposition -> Core`、`ScreenComposition -> Rendering`、`ScreenComposition -> Printing` とし、`Core` はWinForms、OpenGL、PDFsharpを参照しない。route/state controllerはUI非依存とし、各WinForms controlは既存serviceへtypedに接続する。計算と印刷は別境界にし、旧印刷データを計算成功schemaとして復元しない。

#### Feature-Parity Boundary

| Area | C# implementation | Parity rule |
|---|---|---|
| Document | `ProjectDocument`、新規C#文書schema、open/save/save-as、4 presets | 旧保存形式migrationは行わない |
| Input | 14 Angular input routesへ21 typed tablesを明示配置し、route別field/control/order/group/read-only/default/2D-3D visibilityを再現 | Angular template/optionsとrunning referenceを正本にする。単一generic tab paneは不可 |
| Viewport | Z-up、2D orthographic、3D perspective、grid/axis/label、selection sync | Angular shell内のfull workspace、overlay、route panelとのsplit/重なり、control placementまで受入条件にする |
| Analysis | `IAnalysisClient` がPython APIを呼び、`AnalysisResultSet` を一度だけ厳格検証 | alternate success schema、旧worker schema、部分commitを禁止 |
| Results | static/nonlinear/modal、変位・反力・断面力、Basic/COMBINE/PICKUP、case/state/direction paging | Angularの3 category × 3 substate shellを再現し、Angularに専用routeがないnonlinear/modal/movingも同じshell内へ拡張する |
| Derived results | DEFINE/COMBINE/PICKUP、移動荷重page/envelope、max/min | immutable base resultからPresentation層で導出する |
| Printing | typed print job、preview、save、viewport captureを一つのPrint overlayへ投影 | Angularのselection-left / preview-right / actions-bottom構成を再現し、calculation transportとprint contractを共有しない |
| Shell | top menu、optional header、ordered left navigation、full viewport、movable route panel、overlay host、ja/en/zh | FrameWebforJS screen compositionがgolden。generic DockPanel、persistent diagnostics、21-tab editorをdefault UIに残さない |
| Peripheral | user login、authenticated MyPage/logout stateは初回release対象外。anonymous stateで見えるHelp、Contact/chat、language、file/action menu、その他のcontrolと遷移はparity対象 | login系だけをuser-approved exceptionとしてmanifestへ記録する。その他の可視controlは実装または追加のユーザー承認が必要で、黙って非表示・placeholder化しない |

#### Files and Projects

UI parity remediationの主要scope:

- New screen-composition files:
  - `FramePrintPDF/PDF_Manager/Shell/ScreenComposition/FrameWebShellControl.cs`
  - `HeaderBarControl.cs`, `OptionalHeaderControl.cs`, `PrimaryNavigationControl.cs`, `WorkspaceControl.cs`
  - `InputScreenHost.cs`, `ResultScreenHost.cs`
  - `StartOverlayControl.cs`, `PresetOverlayControl.cs`, `PrintOverlayControl.cs`, `OperationOverlayControl.cs`
  - `ScreenRouteState.cs`, `AngularScreenManifest.cs`
  - `FramePrintPDF/PDF_Manager.UiTests/UiParity/**`
- Modified/replaced UI files:
  - `FramePrintPDF/PDF_Manager/Shell/MainForm.cs`
  - `Shell/Contents/{NavigationContent,EditorContent,ProjectDocumentContent,DiagnosticsContent}.cs`
  - `Shell/Printing/PrintUiModels.cs`
  - `FramePrintPDF/PDF_Manager/Resources/Strings*.resx`
  - `.agents/docs/plans/csharp-frameweb-client.md`
  - `.agents/docs/research/csharp-frameweb-ui-parity.md`
- Reference-only source: `FrameWebforJS/src/app/app-routing.module.ts`, `app.component.*`, `components/{menu,optional-header,doc-layout,start-menu,preset,three,input,result,print}/**`.

The original project list below records the completed Step 0-8 foundations; it is retained as history, not as a list of missing files.

- New files/projects:
  - `FramePrintPDF/PDF_Manager/Program.cs`
  - `FramePrintPDF/PDF_Manager/Shell/MainForm.cs`
  - `FramePrintPDF/PDF_Manager/Shell/DocumentKey.cs`
  - `FramePrintPDF/PDF_Manager/Shell/DockContentRegistry.cs`
  - `FramePrintPDF/PDF_Manager/Shell/LayoutState.cs`
  - `FramePrintPDF/PDF_Manager/Resources/Strings*.resx`
  - `FramePrintPDF/PDF_Manager.Core/PDF_Manager.Core.csproj`
  - `FramePrintPDF/PDF_Manager.Core/Documents/ProjectDocument.cs`
  - `FramePrintPDF/PDF_Manager.Core/Documents/ProjectDocumentSerializer.cs`
  - `FramePrintPDF/PDF_Manager.Core/Analysis/AnalysisResultSet.cs`
  - `FramePrintPDF/PDF_Manager.Core/Analysis/AnalysisResultSetValidator.cs`
  - `FramePrintPDF/PDF_Manager.Core/Analysis/ResultIndex.cs`
  - `FramePrintPDF/PDF_Manager.Core/Analysis/FrameWebAnalysisClient.cs`
  - `FramePrintPDF/PDF_Manager.Core/Results/ResultPresentationService.cs`
  - `FramePrintPDF/PDF_Manager.Rendering/PDF_Manager.Rendering.csproj`
  - `FramePrintPDF/PDF_Manager.Rendering/Three/ThreeViewportHost.cs`
  - `FramePrintPDF/PDF_Manager.Rendering/Scene/*Layer.cs`
  - `FramePrintPDF/PDF_Manager.Printing/PDF_Manager.Printing.csproj`
  - `FramePrintPDF/PDF_Manager.Tests/PDF_Manager.Tests.csproj`
  - `FramePrintPDF/PDF_Manager.Rendering.Tests/PDF_Manager.Rendering.Tests.csproj`
  - `FramePrintPDF/PDF_Manager.UiTests/PDF_Manager.UiTests.csproj`
  - `tools/FrameWeb.LocalRuntime/FrameWeb.LocalRuntime.csproj`
- Modified files/projects:
  - `FramePrintPDF/PDF_Manager/PDF_Manager.csproj`
  - useful sources under `FramePrintPDF/PDF_Manager/Printing/**` while moving them behind typed APIs
  - `FramePrintPDF/FramePrintAzure/FramePrintAzure.csproj` if Azure printing remains in the product
  - `tools/FrameWeb.Startup/**`
  - `FrameWeb.sln` and `FramePrintPDF/FramePrintPDF.sln`
  - `.agents/docs/DESIGN.md`（Step 0で設計決定を記録済み）
- Shared contract inputs:
  - `FrameWeb/tests/data/contracts/analysis-result-set-v1.schema.json`
  - `FrameWeb/tests/data/contracts/positive/*.json`
  - `FrameWeb/tests/data/contracts/negative/*.json`
- Dependencies selected or inventoried in Step 0:
  - .NET 8 Windows Desktop SDK
  - a maintained DockPanelSuite package supporting .NET 8 WinForms
  - a maintained OpenTK/GLControl path and the reusable subset/provenance of `isasPrint/THREE`
  - PdfSharpCore or a compatible maintained PDF library selected after current PDF golden characterization
  - Microsoft.NET.Test.Sdk plus one repository-standard .NET test framework
- Explicitly excluded dependencies: ComponentOne and machine-absolute binary references from `isasPrint`.

### Implementation Steps

以下を依存順に実施し、各ステップの検証を通過してから次の広い機能面へ進む。

#### Step 0: Freeze the greenfield contract and clear feasibility blockers

- [x] Record in `.agents/docs/DESIGN.md` that the C# app replaces the frontend behavior only, retains Python FEM, consumes only `AnalysisResultSet v1`, requires no legacy compatibility, and keeps calculation/printing separate.
- [x] Produce a source/provenance inventory for the `isasPrint/THREE` subset, DockPanelSuite, OpenTK, shaders, fonts, textures, and transitive packages; decide what may be copied, rewritten, or consumed as a package.
- [x] Build a minimal .NET 8 WinForms probe that opens a DockingWindow-style document, creates a real GL context, draws one deterministic frame, resizes, captures a bitmap, closes, and reopens repeatedly.
- [x] Define the renderer lifetime contract: UI-thread-only GL calls; idempotent `Initialize`, `SetModel`, `Resize`, `Render`, `Capture`, `Dispose`; one context and subscription set per viewport; explicit disposal order; render-on-invalidation when idle.
- [x] Define versioned `DocumentKey` and layout DTOs, tool-window hide policy, document-window dispose policy, and whitelist-only restore without `Activator` or CLR type names.

**Verification result**: Core contract tests are 41/41 PASS. The real OpenGL 3.3 probe completed 100 create/resize/render/capture/close cycles, 600 frames, and 200 captures, ending with zero live contexts, subscriptions, and windows; it uses invalidation-only rendering and has no idle loop. Malformed/unknown layout data is covered by rejection tests. The package-only path is therefore cleared for Step 1. The full redistribution condition is not cleared: restricted or provenance-incomplete fonts and legacy assets must remain excluded, and the PDF font/publish verification described in the dependency inventory remains mandatory before shipping.

#### Step 1: Establish the solution and project boundaries

- [x] Retarget `FramePrintPDF/PDF_Manager/PDF_Manager.csproj` to a `net8.0-windows` WinExe with WinForms enabled and make it the composition root.
- [x] Add `PDF_Manager.Core`, `PDF_Manager.Rendering`, `PDF_Manager.Printing`, unit tests, rendering tests, and STA UI tests with the dependency direction described above.
- [x] Audit the existing `Printing/**` code before migration. No untyped source qualified for direct promotion before Step 8 characterization, so the new `PDF_Manager.Printing` contains only a typed, dependency-free page-layout contract; `PrintInput`, `PrintData`, legacy dictionaries, duplicate Function handlers, and restricted fonts remain outside the desktop boundary in the explicit non-packable `PDF_Manager.LegacyPrinting` bridge used only by the existing Azure/local print host.
- [x] Add all product/test projects to `FrameWeb.sln` and `FramePrintPDF/FramePrintPDF.sln`. `PDF_Test` is removed from the active `FramePrintPDF.sln` only; `PDF_Test` / `PDF_Test_CLI` source and fixtures remain intact for Step 8 characterization and are not treated as automated tests.
- [x] Prevent excluded files such as `KAJYU_ZU23.cs` from returning through wildcard moves.

**Verification result**: `dotnet build FrameWeb.sln -c Release` and `dotnet build FramePrintPDF/FramePrintPDF.sln -c Release --no-restore` pass. The newly added Step 1 projects build warning-free, while a clean/incrementally invalidated solution build emits 28 inherited warnings from the isolated `PDF_Manager.LegacyPrinting` source; an up-to-date incremental build can report 0 warnings and is not evidence of clean warning-free status. Both solution test runs pass 61/61: Core 41, composition/Printing 9, Rendering 10, STA UI smoke 1. Project-reference tests prove Core has no UI/render/PDF dependency and the desktop project excludes legacy printing sources and restricted fonts. The shell opens/closes on an STA thread without leaving forms. The production Rendering library passes the real OpenGL 3/20/100-cycle sequence; the final 100-cycle run created 100 contexts, rendered 600 frames, completed 200 captures, and ended with zero live contexts/subscriptions/windows. A teardown-order defect found during lead verification was fixed by disposing the renderer before WinForms removes its GLControl; verification-mode UI exceptions now exit nonzero instead of displaying a modal Continue dialog.

#### Step 2: Implement typed document and result foundations

- [x] Define one new `ProjectDocument` aggregate for model inputs, load definitions, derived-result definitions, metadata, selection, dirty state, and validation; keep runtime `AnalysisResultSet` outside the persisted input document unless a later persisted-result feature is approved.
- [x] Define the new C# project-file JSON schema and deterministic serializer using `System.Text.Json`; add open/save/save-as, atomic replace, UTF-8, finite-number, ID/reference, and unknown-field policies.
- [x] Implement immutable C# DTOs and strict validation/indexing for the exact `AnalysisResultSet v1` contract, keyed by `ResultCoordinate(caseId, stateKind, stateIndex)`.
- [x] Consume every shared positive and negative fixture under `FrameWeb/tests/data/contracts/` from C# tests; do not copy fixtures into a divergent C# directory.
- [x] Implement `ResultPresentationService` for static-only DEFINE/COMBINE/PICKUP and moving-load paging/envelopes without mutating base results.
- [x] Define `IAnalysisClient`, `IPrintExporter`, `IProjectStore`, and cancellation/error contracts without coupling Core to HTTP, WinForms, OpenGL, or PdfSharpCore.

**Verification result**: project JSON round-trips byte-stably with canonical entity ordering, strict unknown/duplicate/UTF-8/version/null checks, non-finite/reference rejection, transient selection/dirty exclusion, and same-directory atomic replace. C# reads all six shared positive fixtures and rejects all seven shared negative fixtures in place; Python contract tests pass 14/14. Core tests pass 75/75, both solution test runs pass 95/95, and both Release solution builds pass. `ResultCoordinate` ordering, semantic frame/station/segment/modal validation, failed-candidate state preservation, derived-result immutability, moving-load paging/envelope provenance and source-order rejection, cancellation-aware interfaces, and the dependency-free Core boundary are covered.

#### Step 3: Build the desktop shell and docking lifecycle

- [x] Implement `MainForm` with menu/commands, left navigation, central document viewport, right editor/tool panes, bottom diagnostics/progress pane, and optional floating panes.
- [x] Implement `DockContentRegistry<DocumentKey, Func<DockContent>>`; opening the same keyed tool reuses/activates it, while entity documents may coexist by key.
- [x] Implement versioned layout save/restore for keys, dock state, pane bounds/order, and active document using a whitelist registry.
- [x] Implement command state, dirty-document close confirmation, exception boundary, cancellation, and a pure activation reducer whose side effects are coalesced and cancellable.
- [x] Move all user-visible strings to `Strings.resx`, `Strings.ja.resx`, `Strings.en.resx`, and `Strings.zh.resx`; do not carry hardcoded Angular labels forward.

**Verification result**: STA tests cover create-once/reuse, multi-document identity, actual tool-hide/document-dispose close behavior, invalid factory fail-fast, bounded JSON/layout persistence, transactional rollback, live order/floating bounds, unknown-version/key rejection, active/null-document restore, creating-thread enforcement, language switching, serialized new/open/save/close races, expected versus unexpected cancellation, and bounded non-cooperative analysis/dirty-save/layout-save shutdown. UiTests pass 74/74; both solution test runs pass 168/168 (Core 75, composition/Printing 9, Rendering 10, UI 74); both Release solution builds and targeted `dotnet format` pass. Coverage percentage is not measured. LayoutState v1 intentionally does not persist docked pane proportions or auto-hide state.

#### Step 4: Deliver the first end-to-end vertical MVP

- [x] Implement new/open/save/save-as and at least one representative preset using the new document schema.
- [x] Implement the minimum node/member/support/load-case editors required by the representative preset, with validation and undo/redo.
- [x] Implement the first viewport slice: Z-up nodes, members, supports, loads, selection highlight, orthographic/perspective toggle, fit/home, resize, and table/viewport selection sync.
- [x] Implement `FrameWebAnalysisClient` using the current documented JSON request path and strict `AnalysisResultSet` validation; use cancellation and user-safe diagnostic mapping.
- [x] Extract or wrap the Python-only lifecycle from `FrameWeb.Startup` into `FrameWeb.LocalRuntime`, retaining readiness, timeout, stdout/stderr capture, Job Object cleanup, and parent-exit cleanup; do not start Angular for the desktop path.
- [x] Implement basic static displacement/reaction/member-force tables and one result scene layer.
- [x] Implement one typed PDF job with model summary, one result table, and a deterministic viewport capture.

**Verification result**: both Release solutions build with 0 warnings/errors and both solution test runs pass 253/253 (Core 112, composition/Printing 17, Rendering 27, LocalRuntime 14, UI 83). The real `uv --locked` Flask process is started through the production runtime, the representative preset is serialized, calculated through `FrameWebAnalysisClient`, validated, inspected, and shut down with no owned child process left. Targeted Python transport/runtime tests pass 165/165. RendererProbe passes 20 contexts, 200 frames, and 60 captures with all live counters zero; repeated and concurrent independent WGL review also passes. Typed PDF structural/text/page assertions and the independently rasterized 595x842 Gray8 golden pass, including fail-closed mutation tests. Cancellation/backend failures retain prior state. Coverage percentage is not measured; the known Python/Angular full baseline was not rerun.

#### Step 5: Complete all model input editors

- [x] Add typed editors and validation for nodes, members, rigid zones, supports, elements/materials/sections, panels, joints, notice points, member springs, load cases, load values, DEFINE, COMBINE, and PICKUP.
- [x] Add keyboard navigation, multi-row edit, copy/paste, insert/delete, selection synchronization, and undo/redo as shared grid behaviors rather than per-screen copies.
- [x] Add all four built-in presets as C# resources or typed fixture builders and validate them on load.
- [x] Enforce cross-table references, dimensional requirements, duplicate IDs, case limits, and calculation preconditions in Core before HTTP submission.

**Verification**: each input module has focused ViewModel/domain tests for create/edit/delete/paste/invalid references; every preset opens without warning, round-trips, and produces the expected request; the full editor matrix is navigable from the shell without orphaned dock panes.

**Verification result**: one descriptor-driven editor pane exposes 21 typed tables, including model dimension, four set managers, non-default set rows, prescribed displacements, every load table, and DEFINE/COMBINE/PICKUP. Shared bounded clipboard/keyboard/insert/delete/selection behavior commits multi-row edits as one validated undo item and preserves document/history on rejection. All four stable built-in presets use typed semantic builders, round-trip byte-stably, and have pinned topology/material/support/load/selector assertions plus canonical request hashes. Core enforces references, 2D/3D requirements, topology and panel geometry, duplicate identities, the 256-case limit, effective nonzero loads, member-load positions, and request limits before HTTP submission. Both Release solutions build with 0 warnings/errors and both solution test runs pass 401/401 (Core 243, composition/Printing 17, Rendering 27, LocalRuntime 14, UI 100). Final independent security, quality, and test reviews report no Critical/High/Medium Step 5 findings; coverage percentage is not measured. The known Python/Angular full baseline was not rerun.

#### Step 6: Complete rendering and interaction parity

- [x] Implement independent node, member, rigid-zone, support, spring, joint, panel, notice-point, load, displacement, reaction, and section-force scene layers.
- [x] Implement 2D orthographic and 3D perspective camera policies, grid/axis/labels, scale and color legends, hit testing, hover/selection policy, case/state paging, max/min selection, and PNG capture.
- [x] Keep the rendering engine behind `IViewportScene`, `ICameraController`, `IHitTestService`, and stable domain IDs; do not reproduce the Angular service graph.
- [x] Invalidate only affected layers after document/result changes and coalesce rapid edit/activation events.

**Verification**: layer-level scene snapshots or command-buffer assertions cover every input/result mode; real-context smoke tests draw at least one frame for 2D and 3D; selection is bidirectionally stable; resize/float/dock/close/reopen tests show no duplicate events or growing GPU resource counters; representative large presets remain responsive under a recorded performance baseline.

**Verification result**: all twelve typed model/load/result layers compile independently behind `IViewportScene`, `ICameraController`, and `IHitTestService`. The viewport applies explicit 2D XZ orthographic and 3D perspective policies, active-case load filtering, deterministic signed extrema shared by scene/legend/table, bidirectional hit/hover/selection, result paging, bounded PNG capture, and a bounded OpenGL texture overlay that renders grid, axes, labels, scale, and color legends identically in live Paint and capture. Node/member dependency closure prevents stale cached geometry; rapid invalidations coalesce and the 10,000-node/9,999-member masked update recompiles only Loads in 5.873 ms. Both Release solutions build with 0 warnings/errors and both solution test runs pass 463/463 (Core 243, composition/Printing 17, Rendering 56, LocalRuntime 14, UI 133). The real OpenGL probe passes 100 contexts, 1,700 frames, 600 captures, 200 PNG encodes, and final live context/subscription/window counters of zero. Final security, quality, and test reviews report no Critical/High/Medium Step 6 findings; coverage percentage is not measured. The full Python/Angular baseline was not completed or rebaselined, so the known 3,273 PASS / 6 FAIL / 4 ERROR baseline remains authoritative.

#### Step 7: Complete calculation and result presentation

- [x] Support ordered multi-case static results, every accepted nonlinear load step, and modal modes directly from `AnalysisResultSet`.
- [x] Implement displacement, support-reaction, and member-section-force tables and diagrams for basic results, with explicit case/state selectors.
- [x] Implement DEFINE/COMBINE/PICKUP only for static operands and show a visible domain error for nonlinear/modal attempts.
- [x] Implement moving-load parent/child paging, component max/min envelopes, reaction absolute maximum, member-force extrema, CSV/PICKUP export, and deterministic ordering.
- [x] Retain shell/solid data in the validated result model; dedicated shell/solid screens are outside initial parity because the current Angular app has no such result routes.

**Verification**: shared and Angular-derived fixtures produce identical order, coordinates, extrema, pages, and static-only rejection; Ct first/last cases and all accepted nonlinear steps are accessible; malformed or partial responses never enable result commands; no legacy `disg` / `reac` / `fsec` adapter exists in active C# code.

**Verification result**: Ctの全11ケース、ordered static/nonlinear/modal navigation、Angular由来のmoving `1, 2, 1.1, 1.2`順序、child-only reaction absolute projection、all-source signed envelope、DEFINE/COMBINE/PICKUPのstatic-only境界、3D CSV／2D fixed-width `.pik`、formula-safe text、失敗時のrich UI state保持、atomic overwriteをactual Core/Shell経路で検証した。presentation候補はpages、derived definitions、moving definitions、operands、output entities、scalar workを共有budgetでfail-fastし、exact/+1境界を網羅する。両Release solution buildは0 warnings/errors、両solution testsは501/501 PASS（Core 272、composition/Printing 17、Rendering 56、LocalRuntime 14、UI 142）。ownershipはoverlap 0 / unowned 0 / idle 0、AgentOnlyは`overall=pass`。最終reviewはsecurity Critical 0 / High 0 / Medium 0 / Low 1、quality 0 / 0 / 0 / Low 2、tests 0 / 0 / 0 / Low 2。coverage率は未計測で、Python/Angular全件は再baseline化せず既知の3,273 PASS / 6 FAIL / 4 ERRORを維持する。

#### Step 8: Replace the legacy print path with typed printing

- [x] Characterize the useful existing PDF behavior first: A3/A4, portrait/landscape, page numbers, Japanese/Chinese fonts, table pagination, diagram layouts, and representative existing fixtures.
- [x] Replace `PrintInput` / `PrintData` dictionaries with typed `PrintJob`, page-section, table, diagram, result, and viewport-capture models.
- [x] Replace the vulnerable legacy PdfSharpCore/ImageSharp graph, initialize the selected font resolver once, make export concurrency explicit, preserve original exception types/causes, and enforce encoded/decompressed/image-dimension/page/work limits before allocation.
- [x] Implement preview, page count, input tables, displacement/reaction/section-force tables, load/model/result diagrams, scale/layout options, and file export from the desktop app.
- [x] Decide whether `FramePrintAzure` is removed or rebuilt against `PDF_Manager.Printing`; do not let the desktop executable become an Azure dependency. Decision: retire it during the Step 10 cutover instead of rebuilding a second print API.

**Verification**: pure layout tests cover pagination and page geometry; generated PDFs pass parse/text/page assertions; representative rendered pages match approved goldens; repeated/parallel export follows the chosen concurrency policy; oversized/compression-amplified/image-bomb inputs fail before expensive work; shippable project graphs have no known High/Critical package advisory; no old base64/comma-byte print endpoint is required by the desktop app.

**Verification result**: immutable `PrintJob`/page/table/diagram/result/capture models produce one authoritative page plan consumed by preview and export. The desktop captures all 21 editor tables plus Moving Loads in stable order, separates model/load/result diagrams, and supports static/nonlinear/modal/derived/moving result selections. Official PDFsharp 6.2.4 replaces the typed product graph's legacy PdfSharpCore/ImageSharp path; process-wide export is serialized, installed Windows fonts are resolved lazily per language under bounded source-size checks, and no restricted font binary is bundled. A3/A4、portrait/landscape、margins、scale、page numbers、table pagination、grapheme-safe clipping、full selectable preview text、shared whole-document budgets、atomic save、exception provenanceを検証した。両Release solution buildは0 warnings/errors、両solution testsは567/567 PASS（Core 272、Printing 73、Rendering 56、LocalRuntime 14、UI 152）。ownershipはoverlap 0 / unowned 0 / idle 0、AgentOnlyは`overall=pass`。最終reviewはSecurity Critical 0 / High 0 / Medium 0 / Low 1、Quality 0 / 0 / 0 / Low 0、Tests 0 / 0 / 0 / Low 4。coverage率は未計測で、CJKの構造・抽出検証はPASSしたがGUI viewerによる実glyph目視証跡は環境制約で未取得である。legacy hostは既知High 2 / Medium 2と再配布NO-GOを維持し、Step 10で廃止する。

#### Step 9: Remediate screen-composition parity before production integration

This step reopens the visible portions of Steps 3/5/6/7/8. Their typed and lifecycle foundations remain complete; their screen-composition acceptance does not.

##### 9.0 Freeze the Angular manifest and reference captures

- [ ] Add the versioned manifest at `FramePrintPDF/PDF_Manager.UiTests/UiParity/Manifest/framewebforjs-screen-manifest.v1.json`, its schema beside it, canonical typed fixtures under `UiParity/Fixtures/`, and Angular references under `UiParity/References/Angular/`. Each scenario ID binds fixture, route/outlet/header state, selection, language, logical client size, DPI, theme/font metadata, and expected capture path.
- [ ] Cover all 14 input routes, 9 result routes, Start/Preset/Print overlays, shared shell, optional-header substates, nested print states, every visible menu/control, conditional field/control branch, and busy/confirm/error/cancel state. Record control/field order, grouping, default/read-only/conditional visibility, navigation enablement, transitions, actions, and source locations for every item.
- [ ] Independently extract the source inventory from Angular routing, shell/outlet/header declarations, route templates, shared Sheet and typed column descriptors, menu templates/actions, and nested Print components. Require exact key-set equality from every independently discovered route/state/outlet/header/menu/control/field/conditional branch to either one C# implementation item or one explicitly user-approved exception. Missing, duplicate, extra, and many-to-zero mappings fail; a manifest entry cannot prove its own completeness.
- [ ] Capture Angular and current C# references at 1200x800 logical client / 100% DPI. Add 1024x768 / 100% DPI and 1440x900 / 150% DPI during responsive closeout. Use Windows light theme and record the actual OS, scale, font, theme, fixture hash, route/state, and capture tool with each image.
- [ ] Compare client areas only. Exclude OS non-client chrome, pointer, caret blink, and animated timestamps. Require exact hierarchy, z-order, visible/enabled/text/state values and logical control bounds within 1 logical pixel for DPI rounding. For non-GL image regions allow at most 8/255 per-channel delta and 0.5% mismatched pixels after a one-physical-pixel text/edge antialiasing mask. Verify the GL scene with existing renderer goldens while parity captures assert its bounds, clipping, overlays, and surrounding composition; do not require Angular WebGL and OpenGL raster bytes to match.

**Verification**: schema validation, Angular-inventory/manifest bidirectional set equality at route/state and field/control granularity, fixture/hash validation, metadata validation, and reference-image existence all pass. Mutation tests prove that a removed/duplicated/extra route, conditional field, menu action, and nested-print control, an unapproved exclusion, and an over-threshold geometry/pixel change each fail. Missing browser/native capture capability is a failed gate, not a waived gate.

##### 9.1 Implement UI-independent route and screen state

- [ ] Add `ScreenRouteState`, `AngularScreenManifest`, and a deterministic route controller for navigation order, contextual header state, overlay state, calculation enablement, dimension, paging, and invalid transitions.
- [ ] Keep `ProjectDocument`, result coordinates, edit transactions, and services out of WinForms event handlers except through typed commands/state publication.

**Verification**: unit tests cover navigation order, results-disabled-before-calculation, 2D/3D state, all contextual header transitions, overlay exclusivity/focus, and rejection of invalid route/state combinations.

##### 9.2 Replace the common shell

- [ ] Build `FrameWebShellControl` with top menu, optional header, ordered left navigation, full viewport workspace, route-panel host, and overlay host; connect it from `MainForm`.
- [ ] Reproduce every control visible in the anonymous Angular menu, including Help, Contact/chat, language, and source-discovered file/action controls. Preserve their visible ordering, enabled state, focus/accessibility names, and transitions; external navigation uses one allow-listed typed launcher and surfaces cancellation/failure safely.
- [ ] Make `WorkspaceControl` the sole visible viewport and OpenGL-context owner. Refactor `ProjectDocumentContent` into non-visual typed coordination services or delete it; route panels and overlays must never create a second renderer, GL context, selection subscription, or result publisher.
- [ ] Connect the existing serialized document transition, captured-revision publication, dirty-close confirmation, cancellation, bounded shutdown, and exception mapping at the new composition root. Route changes and overlay open/close reuse the current viewport and cannot publish work for an obsolete document revision.
- [ ] Stop opening Navigation/Editor/Diagnostics dock panes in the default composition. Retain only non-visible internal lifecycle helpers that still have a matching responsibility.

**Verification**: STA UI tests assert hierarchy, z-order, menu/control order and actions, default visibility, focus/accessibility names, navigation enablement, and route-panel/viewport geometry. Help and Contact/chat success/cancel/failure paths use only allow-listed targets. Repeated route/overlay/document transitions prove one live viewport/context and one subscription/publication path, reject stale revisions, preserve cancellation semantics, and return renderer/subscription/resource counters to zero after close. Replace four-pane assertions with stronger parity assertions.

##### 9.3 Implement document commands plus Start and Preset overlays

- [ ] Reproduce New/Open/Preset tiles, the four typed presets, close/cancel transitions, keyboard focus, and failure preservation using existing document services.
- [ ] Wire top-menu New, Open, Save, Save As, and Close through the serialized transition boundary. Reproduce dirty confirmation choices, cancel, invalid/open/save failure preservation, atomic save, and close/shutdown behavior without exposing the old docking shell.

**Verification**: overlay structure, focus order, new/open/preset identity, Save/Save As/Close, clean and dirty paths, each confirmation choice, cancel, invalid file, save failure, revision race, and state-preservation tests pass at all reference sizes.

##### 9.4 Approve the representative input slice

- [ ] Implement Elements, Nodes, and Supports as route-specific screens over existing edit transactions, grid controller, clipboard, undo/redo, and selection sync.
- [ ] Match 2D/3D visibility, field order, group headers, units, defaults/read-only behavior, scroll, resize, drag, and viewport overlap.

**Verification**: structural tests, edit behavior tests, and same-size captures pass. A side-by-side Angular/C# review of this slice is required before propagating the pattern.

##### 9.5 Complete structural input routes

- [ ] Implement Members/Rigid Zone contextual switching plus Panel, Joints, Notice Points, Member Springs, and their set-management surfaces.
- [ ] Map every existing typed table to one explicit route/state; remove the generic 21-tab user surface when no longer referenced.

**Verification**: route-by-route field/group/visibility/pager tests and manifest coverage prove that no typed surface is orphaned and no extra default tab is exposed.

##### 9.6 Complete load and derived-definition inputs

- [ ] Implement Load Name/Load Strength, nodal/member/prescribed/moving loads, moving pitch, and DEFINE/COMBINE/PICKUP contextual navigation.

**Verification**: contextual transitions, paging, moving-only controls, atomic multi-row edits, validation failure preservation, keyboard, clipboard, and undo/redo pass.

##### 9.7 Implement basic result screen families

- [ ] Implement Displacement, Reaction, and Section Force categories with Basic/COMBINE/PICKUP substates, case/direction paging, result table, extrema, and viewport sync.

**Verification**: calculation precondition, first/last case, category/substate order, field/group order, selection/extrema sync, malformed result rejection, and route captures pass.

##### 9.8 Integrate nonlinear, modal, moving, and derived states

- [ ] Place nonlinear accepted steps, modal modes, moving parent/child pages, and derived results in the same reference result shell. Use the optional-header pager area and explicit state labels; do not introduce a second shell.

**Verification**: every accepted nonlinear/modal state, moving order/provenance/extrema, static-only derived rejection, and failure-state preservation pass against existing result fixtures.

##### 9.9 Replace print dialogs with the Print overlay

- [ ] Project the existing typed print plan into one selection-left / preview-right / actions-bottom overlay and retain the same authoritative preview/export plan, budgets, atomic save, and exception provenance.

**Verification**: selection → preview → page navigation → PDF and cancel/failure preservation pass together with existing PDF structural/text/image goldens.

##### 9.10 Complete operation overlays, localization, and responsive behavior

- [ ] Implement busy, confirm, error, and cancel overlays; close ja/en/zh clipping, keyboard/focus, accessibility names, resize/scroll, DPI, and route-panel drag/resize.
- [ ] Delete unreachable default docking/layout UI, persistent diagnostics UI, old print dialogs, obsolete resources, and composition-only tests. Do not create backup copies or compatibility shims.

**Verification**: three-language no-clipping captures, focus/keyboard tests, reference-size/DPI captures, invalid/empty/populated/error/cancel states, resource/lifecycle counters, and obsolete-composition non-reachability pass.

##### 9.11 Sign off screen parity

- [ ] Execute the complete source-derived matrix and obtain user visual approval. C# self-generated images alone are not an acceptable golden.
- [ ] Run the existing typed-domain, rendering, result, printing, local-runtime, and solution gates to prove the UI replacement did not regress the completed foundations.

**Verification**: both solutions build and test serially, the UI parity suite passes, relevant Python contract tests pass, `git diff --check` passes, and `& .agents/check.ps1` passes for the changed scope. Production integration remains blocked until this gate is explicitly signed off.

#### Step 10: Production integration and cutover

##### 10.0 Freeze release decisions

- [ ] Ship the first release without user login or an external identity provider. Retain only the private loopback bearer and Windows Job/listener ownership boundary.
- [ ] Before packaging changes, decide and record installer/update strategy, per-user settings location, crash/diagnostic policy, backend discovery, offline/local-mode behavior, signing ownership, and whether `FrameWebforJS` remains a read-only reference, is archived, or is deleted.

**Verification**: every release decision has one durable design record, owner, and executable acceptance check; packaging and cutover tasks remain blocked while any required decision is unset.

##### 10.1 Build and verify the release candidate

- [ ] Implement signed packaging and add clean-machine, upgrade/uninstall, SBOM, forbidden-file/font, dependency advisory, signed-artifact, Python lifecycle, offline/error, and orphan-process verification.

**Verification**: a signed clean-machine candidate launches without Node/Electron, manages Python lifecycle, completes the approved end-to-end matrix, contains no forbidden assets or legacy host graph, and leaves no orphan process.

##### 10.2 Switch the supported entrypoint and delete legacy hosts

- [ ] Only after Step 9 sign-off and Step 10.1 clean-machine PASS, change `FrameWeb.Startup` and release documentation to make the C# desktop the supported entrypoint and stop launching/redirecting to Angular.
- [ ] In the same cutover slice, retire `FramePrintAzure`, `PDF_Manager.LegacyPrinting`, the legacy local print endpoint, `PDF_Test` / `PDF_Test_CLI`, obsolete manual print APIs, and unreachable packaging paths rather than maintaining compatibility.

**Verification**: repository/solution/package scans prove the legacy host graph and forbidden artifacts are absent; Startup reaches only the C# supported path; `dotnet test FrameWeb.sln`, `dotnet build FrameWeb.sln`, relevant Python contract tests, and `& .agents/check.ps1` pass.

### Risks & Considerations

- **THREE provenance and modernization**: `isasPrint/THREE` records MIT provenance, but shader/font/texture/transitive licenses need separate verification. Gate source import on Step 0; prefer a minimal owned adapter over copying all 289 vendored files.
- **OpenTK generation gap**: sample code targets OpenTK 3.3.3/.NET Framework 4.8. A modern OpenTK port may change GLControl, context, input, shader, and disposal APIs. Prove it with a real-context spike before architecture lock-in.
- **Lifecycle leaks**: the sample's `Application.Idle` loop, repeated Load subscription, non-disposable controls, and incomplete teardown can cause duplicate work and GPU leaks. Render on invalidation and test explicit disposal order.
- **Obsolete layout tests**: current tests explicitly require the four dock regions and one 21-tab editor. Replace those composition assertions with manifest-backed parity assertions while retaining their lifecycle, cancellation, and state-preservation coverage; do not delete tests merely to obtain green results.
- **Scope size**: full FrameWebforJS parity spans 14 input routes, 9 result routes, overlays, three languages, and responsive states. The representative Elements/Nodes/Supports slice is the mandatory visual checkpoint before breadth expansion.
- **Reference capture availability**: the planning environment could start Angular but exposed no browser/native-app UI surface. Fixed-size/DPI capture is therefore Step 9.0's hard gate; source inspection cannot substitute for visual acceptance.
- **Visual determinism**: text rasterization and OpenGL anti-aliasing vary by DPI, font, and GPU. Require exact hierarchy/control geometry/state, source-derived content, bounded region/pixel comparison, and side-by-side human approval rather than a brittle whole-window byte hash.
- **Contract drift**: C# must consume shared `AnalysisResultSet` schema/fixtures directly. Do not fork DTO semantics or create a UI-specific backend response.
- **Input contract**: this plan creates a new persisted C# document but keeps the Python calculation input meaning. Request serialization needs golden tests against Python before UI breadth expands.
- **Printing presentation**: the typed PDF model is complete, but its current setup/preview dialogs are not composition-parity surfaces. Reuse the immutable plan and exporter behind one overlay; do not duplicate print state or preserve the old dialogs.
- **Global PDF state**: official PDFsharpのprocess-global font stateは一度だけ構成し、typed export/preview PDF workはprocess-wide semaphoreで直列化する。言語別fontは必要時だけboundedに解決する。
- **Startup ownership**: desktop, startup host, Azure print, and Python service currently overlap. Give the desktop path exactly one owner for process lifetime and leave cloud hosting behind explicit interfaces.
- **Canonical gate debt**: the post-Step-1 full repository check in `.agents/logs/check-20260920T035935489Z-32252.log` completed with Python and Angular failures outside the C# diff. Keep those failures separate from task-scoped validation, but do not claim repository-wide green until they are fixed or explicitly baselined.
- **Solution coverage**: Step 0/1のdesktop product、Core、Rendering、typed Printing、LegacyPrinting、probe、4 test projectsは両solutionへ登録済みである。以後はsolution-level build/testを受入証拠にする。
- **Temporary legacy print bridge**: desktopのtyped printing移行はStep 8で完了し、新製品graphは公式PDFsharp 6.2.4のみを使う。`PDF_Manager.LegacyPrinting` は既存Startup/Azure経路の隔離物としてだけ残り、desktop shellは参照しない。bridge DLLには制限fontと脆弱なImageSharp 1.0.4が残り、匿名かつ無制限のlegacy処理も未解消なので公開・配布は引き続きNO-GOである。Step 10 cutoverで `FramePrintAzure`、legacy local print surface、manual harness/APIとともに廃止する。
- **No compatibility or backups**: the user explicitly excludes backward compatibility and backup artifacts. Use version control and focused tests as recovery, delete obsolete UI only after its replacement slice passes, and never create `.bak`, duplicate legacy adapters, or hidden fallback screens.
- **Plan review**: the 2026-09-21 read-only Codex decomposition supports a manifest-first, shell-first, representative-slice delivery order. After two revision rounds closed visual-comparison, manifest-completeness, document-command, ownership/lifecycle, cutover-order, and visible-menu gaps, final read-only adequacy validation returned PASS with no remaining critical omission or ordering conflict.

### Open Questions

- Which signed installer/update channel should Step 10 use: MSIX, a traditional installer, or another managed channel? This does not block UI parity remediation.
- After parity sign-off, should `FrameWebforJS` remain as a read-only reference implementation, be archived outside the supported product, or be deleted? No decision is needed before Step 10.
- Every control visible in the reference anonymous Angular state, including Help and Contact/chat, is part of the first parity implementation with its visible state and transition. Source discovery decides whether cloud-document or update controls exist; if visible, they remain in scope unless the user explicitly approves a manifest exception. Production login and authenticated MyPage/logout are the only approved visual/behavioral exception for the first release.

Resolved for implementation: no production login; no backward compatibility or backup artifacts; primary comparison at 1200x800 logical client / 100% DPI; responsive checks at 1024x768 / 100% and 1440x900 / 150%; exact hierarchy, control geometry/state, and interaction parity with bounded visual comparison rather than brittle whole-window byte equality. Nonlinear/modal/moving states use the same optional-header/result shell and require approval with the first result slice.
