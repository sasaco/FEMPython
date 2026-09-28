# Design Document — 要件定義書 (Requirements & Macro Design)

> **Role:** Macro-level requirements and design — *what* this project builds and *why*.
> Kept current by `/init`, `/design-tracker`, and `/checkpointing`.
>
> **Document map:** Shared rules → [rules/](../rules/) ·
> Shared bootstrap → [AGENTS.md](../../AGENTS.md) · State → [STATE.md](../STATE.md) ·
> Micro work progress (latest 5 checkpoints) → [PROGRESS.md](../../PROGRESS.md)

## 背景・目的 (Background & Purpose)

FrameWeb3は、構造モデルの編集、骨組有限要素解析、結果確認、印刷・PDF出力を一つのリポジトリで提供するWeb構造解析システムである。Python解析サービス、Angularクライアント、.NETのローカル起動・印刷ホストを組み合わせ、各コンポーネントの公開契約を明示的に分離する。

ローカル開発はWindowsを主対象とし、PowerShell、各コンポーネントのmanifest、commit済みlockfileを再現可能な正規経路とする。

## スコープ (Scope)

対象範囲は製品コンポーネントとそれらの明示的な境界であり、生成物や運用secretを含めない。

### In Scope

- `FrameWeb/`のPython骨組FEM解析とFlask/functions-framework HTTP境界。
- `FrameWebforJS/`のAngularブラウザ/Electronモデル編集、計算要求、結果表示。
- `tools/FrameWeb.Startup/`のローカルセットアップ、解析・フロント起動、readiness、印刷HTTPホスト。
- `FramePrintPDF/`のローカル/Azure印刷およびPDF生成。
- `FrameGConverter/`の独立した変換機能と、`FrameWeb.sln`を中心とするVisual Studio開発経路。
- 単一・複数ケース、静的・非線形・モーダル解析の成功出力を共通化する`AnalysisResultSet`の単一契約。

### Out of Scope

- 本文書への本番認証情報、クラウドsecret、デプロイ資格情報の記載。
- versioningまたは明示的な移行なしでの公開解析・印刷契約の変更。
- 生成済み依存関係、build出力、cache、vendor資産を製品コンポーネントとして扱うこと。
- `.agents`整備に伴う製品コード、製品依存関係、API挙動の変更。

## 機能要件 (Functional Requirements)

| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| FR-FRAMEWEB-2 | Let the Angular client edit models, request calculations, and display calculation results and actionable errors. | High | The client validates required result fields before starting result workers. |
| FR-FRAMEWEB-3 | Start the analysis API, Angular development server, and local print host through the .NET startup project. | High | Visual Studio F5 and `dotnet run --project tools/FrameWeb.Startup` are supported local entry points. |
| FR-FRAMEWEB-4 | Produce print/PDF output through the existing .NET printing handlers without conflating their transport contract with the calculation API. | High | Azure deployment continues to use the dedicated printing project. |
| FR-FRAMEWEB-RESULT-SET-2 | Return AnalysisResultSet as the sole successful calculation response for static, nonlinear, modal, single-case, and multi-case analysis. | High | Each AnalysisResult is one case/state snapshot; nonlinear accepted steps are separate results rather than nested step_results. No compatibility or display-specific success schema is retained before release. |
| FR-CS-DESKTOP-UI-PARITY-1 | Reproduce the FrameWebforJS screen composition in the C# desktop client screen by screen, including shell hierarchy, navigation placement and order, route-specific input/result/print screens, visible fields and controls, grouping, default visibility, viewport/table splits, overlays, and transitions. | High | The running FrameWebforJS application is the acceptance reference. Semantic-only input parity or a generic docking/editor reinterpretation is not sufficient; every supported scenario requires source-derived mapping and side-by-side visual evidence at agreed reference sizes. |

## 非機能要件 (Non-Functional Requirements)

| Category | Requirement | Metric / Target |
|----------|-------------|-----------------|
| Reproducibility | Use the committed Python, npm, and .NET project metadata and locks from their component directories. | Canonical commands run without relying on a root-level Python or npm project. |
| Security | Decode untrusted compressed calculation input without evaluating code and keep local secrets out of tracked files. | No `eval`-style decoder; local environment files remain untracked. |
| Maintainability | Keep analysis, frontend, startup, printing, conversion, and agent-infrastructure responsibilities independently testable. | Component-specific gates report their working directory and failing command. |
| Platform | Keep the supported local workflow executable from Windows PowerShell. | Bootstrap, setup, and verification paths do not require WSL or Bash. |
| Result contract | Expose exactly one validated AnalysisResultSet success schema, with no compatibility or display-specific alternatives; keep calculation input redesign outside this refactor. | Zero alternate success schemas; zero runtime result adapters; unique case/state coordinates; atomic response; deterministic case-major order. |
| Result-set correctness v1 | Preserve deterministic input-derived case order and accepted state order, isolate mutable solver state per case, preserve case-specific support sets, validate every snapshot variant, apply no post-solve display multiplier, and fail atomically. | No fixed case-count ceiling in the initial release; zero partial responses; zero duplicate case/state coordinates; exactly one final load-step per nonlinear case; identical geometric public topology across cases. |

## アーキテクチャ (Architecture)

FrameWeb3は次の境界を持つコンポーネント指向モノレポである。

1. `FrameWebforJS/`はブラウザ/Electron操作、モデル編集、計算要求、結果表示を所有する。
2. `FrameWeb/`は入力検証、FEMモデル構築・解析、計算HTTP応答を所有する。
3. `tools/FrameWeb.Startup/`はローカルtoolchain準備、Angular/Python子プロセス、readiness、ローカル印刷を所有する。
4. `FramePrintPDF/`は既存C#印刷/PDF handlerとAzureデプロイ境界を所有する。
5. `FrameGConverter/`はWebローカル起動ライフサイクル外の独立変換utilityである。

### Result Contracts

- 計算入力は既存の検証済みschemaを継続利用し、本リファクタリングでは再設計しない。入力caseの順序を結果順序の基準とする。
- `AnalysisResultSet`: 唯一の成功response root。順序付き`cases`、一度だけ出力するcanonical `topology`、case-major順の`results`を持つ。
- `AnalysisResult`: `(case_id, state.kind, state.index)`で一意になるimmutable snapshot。staticはcaseごとに1件、material nonlinearはaccepted stepごとに1件、modalはmodeごとに1件を出力する。
- legacy入力は`load` mapの全entryを挿入順でcase化し、modern入力は現行どおり単一case `"1"`とする。支持節点はcase固有の`support_node_ids`として保持し、共有topologyはgeometryに限定する。
- legacy caseの解析種別はtop-level、case、model inferenceの順、解析parameterはdefault、case、top-level overrideの順という現行precedenceを維持する。
- `state`は`static`、`load_step`、`mode`のdiscriminated unionとする。非線形のnested `step_results`と最終状態の二重格納は行わず、caseごとに最後のload stepだけをfinalとする。
- canonical result fieldは`node_displacements`、`support_reactions`、`member_section_forces`、`shell_results`、`solid_results`、`diagnostics`とする。modal variantは`node_mode_shapes`を持ち、force-bearing fieldを持たない。
- member/shellのlocal frameとshell/solidのsampling locationをtopologyへ明示し、resultはそのID/orderを完全にcoverする。modal frequencyはHz固定ではなく宣言time unitの逆数とし、zero/degeneracy toleranceを契約で固定する。
- legacyの表示・解析後倍率`rate`は削除し、代替のrequest/result fieldは追加しない。DEFINE/COMBINE/PICKUPの係数は派生結果の概念としてbase resultを変更しない。
- 単位は既存のnormalized `model_metadata.units`をそのまま出力し、省略時は`consistent_user_defined`/`unspecified`とする。単位推定・変換は行わない。

計算成功時のschemaはこの一組だけとし、result representation negotiation、compatibility adapter、display-specific backend contractは設けない。計算と印刷は別のtransport契約であり、frontendはcanonical schemaを一度だけ検証して直接利用する。

- The Windows desktop client is composed as a `net8.0-windows` WinForms shell over UI-independent Core, Rendering, and Printing projects, plus a local Python runtime owner. Core defines typed `ProjectDocument`, `AnalysisResultSet v1` validation/indexing, result presentation, and the `IAnalysisClient`, `IPrintExporter`, and `IProjectStore` boundaries without WinForms, OpenGL, or PDF-library dependencies.

- C# desktop Step 2: `ProjectDocument v1` is the strict input-only persisted aggregate; `AnalysisResultSet v1` is parsed, semantically validated, indexed, and committed atomically; derived and moving-load values remain immutable presentation-layer objects behind typed Core interfaces.

- C# desktop Step 3: the WinForms composition root owns a creating-thread-only DockContentRegistry and DockLayoutAdapter, localized document/tool panes, serialized revision-checked document transitions, cancellable/coalesced activation, user-safe exception mapping, and bounded asynchronous close cleanup. LayoutState v1 intentionally excludes docked pane proportions and auto-hide state.

## 技術選定 (Tech Stack & Rationale)

| Area | Technology | Rationale | Alternatives Considered |
|------|------------|-----------|-------------------------|
| Analysis service | Python 3.11+; NumPy, SciPy, Flask, functions-framework; `uv` | FEM実装とHTTP境界に適合し、lockされたcomponent環境を提供する。 | ルート単一Python環境、host processへのsolver統合。 |
| Web client | Angular 15, TypeScript 4.9, npm/Node 18 | 既存のブラウザ/Electron UIと結果表示資産を維持する。 | `.agents`整備と同時のframework置換。 |
| Local orchestration | .NET 8 `FrameWeb.Startup` | Visual Studio/CLIの単一起点、readiness、child-process管理、ローカル印刷を提供する。 | 各serviceの常時手動起動。 |
| Printing | Existing .NET projects in `FramePrintPDF/` | 既存のC#印刷とAzureデプロイ境界を維持する。 | 計算transportの再利用、Pythonへの印刷移行。 |
| Developer shell | Windows PowerShell | setup script、Visual Studio workflow、現行環境と一致する。 | WSL/Bashを必須にする。 |

## 制約 (Constraints)

- Python製品・agent toolingは`uv run --project FrameWeb --locked --extra dev python ...`で実行し、bare `python`が`PATH`にあることを前提にしない。
- ローカルsetupはPython 3.12とNode 18/npm 9を対象とし、各componentが宣言するversion範囲を尊重する。
- 計算APIは成功時に`AnalysisResultSet v1`以外のresult schemaを提供せず、UI専用のbackend representationを追加しない。
- 計算encoderとC#印刷APIのwire契約が同値と証明されるまで共有しない。
- `.venv`、`node_modules`、`dist`、`bin`、`obj`、cache、vendor frontend資産はsource componentではない。
- local environment/authentication fileはmachine固有値を含み得るため、bootstrap automationで上書き・commitしない。
- Load Case Set Analysisは初期版ではケース数の固定上限を設けず、途中失敗時にpartial result setを返さない。

- The C# desktop migration replaces frontend behavior only: Python remains the FEM implementation, successful calculations use only `AnalysisResultSet v1`, calculation and printing transports stay separate, and legacy client/print compatibility is not required.

- The C# desktop first release has no login, external identity provider, account token storage, or remote authenticated calculation mode. `FrameWebforCS/` calculations use an in-process pythonnet runtime; private loopback bearer and Windows Job/listener checks apply only to components that continue to use HTTP.

## Key Decisions

| Decision | Rationale | Alternatives Considered | Date |
|----------|-----------|------------------------|------|
| Use Codex as the main repository agent and Windows PowerShell as the canonical administration path. | This matches the active runtime and the repository's supported local development environment. | Preserve copied runtime-first and Bash-first bootstrap assumptions. | 2026-09-18 |
| Keep Python, Angular, startup, printing, and conversion as explicit component boundaries in one monorepo. | Each component has a different toolchain and public contract; explicit boundaries make setup and validation reproducible. | Treat the root as one Python project or collapse services into the startup host. | 2026-09-18 |
| Keep the repository agent infrastructure Codex-focused and remove copied Claude pseudo-links, runtime-specific agents and hooks, and the inactive Antigravity workflow. | Only Codex is an active repository runtime. Removing unreachable integration surfaces prevents stale instructions and duplicate execution paths while retaining runtime-neutral skills and rules. | Maintain parallel Claude and Codex bootstrap surfaces; retain inactive integrations as examples. | 2026-09-18 |
| Default Codex to the read-only sandbox and require an explicit workspace-write opt-in for repository mutations. | Least-privilege defaults make read-only analysis safe while keeping authorized implementation work available through an explicit invocation choice. | Use workspace-write or danger-full-access as the repository default. | 2026-09-18 |
| Use AnalysisResultSet as the only public calculation root and remove FrameResultSet, legacy-cases-v1, the default flat AnalysisResult response, and all compatibility adapters before release. | A single ordered snapshot collection eliminates representation negotiation, duplicate result models, UI-specific backend fields, nonlinear final-state duplication, and compatibility maintenance. Each result is identified by case_id plus a discriminated state; shared topology is emitted once, and domain member-force aggregation becomes canonical postprocessing. | Keep separate AnalysisResult, AnalysisResultSet, and FrameResultSet wire contracts; retain legacy-cases-v1; nest nonlinear step_results inside a case-level final result. | 2026-09-18 |
| Limit the AnalysisResultSet refactor to the calculation success output; keep existing validated input schemas unchanged and remove the legacy rate display multiplier without introducing load_scale. | The objective is to establish one canonical result root. Redesigning the complete input contract adds unrelated migration risk and schema maintenance, while rate is post-solve display behavior that does not belong in the canonical analysis result. | Introduce AnalysisRequest v1 and rename rate to load_scale; keep post-solve rate behavior. | 2026-09-18 |
| Enumerate existing legacy load-map entries as ordered result cases, keep modern input single-case as case 1, store support_node_ids per ResultCase, and reproduce normalized existing unit metadata without inference. | This makes output construction deterministic without redesigning input, permits cases to select different support definitions while sharing geometric topology, and remains truthful when current inputs omit unit declarations. | Invent a new multi-case input; require identical supports across cases; put supports in shared topology; assume fixed engineering units. | 2026-09-18 |
| Build the Windows desktop client as a .NET 8 WinForms frontend replacement while retaining FrameWeb as the Python FEM service and accepting only AnalysisResultSet v1 as a successful calculation response. | This preserves the validated numerical-analysis boundary, keeps one canonical result contract, and lets the desktop migration focus on typed document, presentation, rendering, docking, and PDF workflows. | Port the FEM engine to C#; retain legacy disg/reac/fsec compatibility; support multiple successful result schemas. | 2026-09-20 |
| Keep calculation and printing as separate typed boundaries in the C# desktop architecture and require no legacy client or print-transport compatibility. | The calculation result contract and PDF composition have different responsibilities and lifecycles; separating them prevents the current untyped print dictionaries from becoming a new application contract. | Reuse the legacy print JSON/dictionaries as the desktop domain model or calculation success schema. | 2026-09-20 |
| Isolate the existing Azure/local print handlers in a non-packable PDF_Manager.LegacyPrinting bridge while the new desktop shell depends only on Core, Rendering, and typed Printing projects. | Retargeting PDF_Manager to a net8.0-windows WinExe otherwise makes FramePrintAzure(net8.0) depend on the desktop executable and fail NU1201. The bridge preserves the existing handlers without reintroducing PrintInput, PrintData, legacy dictionaries, or restricted fonts into the new desktop product boundary. | Remove FramePrintAzure and the local print host before parity; retarget Azure/Startup to Windows and reference the desktop executable; expose the legacy dictionary API from the new typed Printing project. | 2026-09-20 |
| Do not publish or redistribute the FramePrintAzure/local legacy print host until its anonymous endpoint, vulnerable PDF/image dependency graph, unbounded request/decompression/image/PDF work, and restricted embedded fonts are replaced or removed. | The Step 1 security review confirmed that the new desktop boundary is isolated, but the retained legacy host still exposes unauthenticated input to ImageSharp 1.0.4 High advisories, has no body/expansion/page/image/time/concurrency limits, and copies restricted fonts inside PDF_Manager.LegacyPrinting.dll into Startup output. IsPackable=false does not make that host safe to publish. | Publish the legacy host with documentation-only warnings; upgrade only ImageSharp while leaving anonymous unbounded work; remove FramePrintAzure immediately before replacement behavior is characterized. | 2026-09-20 |
| Persist C# desktop inputs in ProjectDocument v1 only; keep runtime AnalysisResultSet, selection, and dirty state outside the project file. | This preserves the input/result boundary, makes the project file deterministic and safely replaceable, and prevents transient UI or response state from becoming compatibility debt. | Persist calculation results and UI session state inside one document; reuse legacy print dictionaries. | 2026-09-20 |
| Implement the exact AnalysisResultSet v1 contract in package-free Core and validate shared fixtures and semantic invariants before atomically replacing current result state. | One strict immutable contract prevents partial result publication and keeps C# aligned with Python and Angular through the same canonical fixtures. | Generate a second C# result schema; accept partial or legacy disg/reac/fsec payloads. | 2026-09-20 |
| Keep DEFINE/COMBINE/PICKUP and moving-load envelopes as immutable presentation outputs: DEFINE/COMBINE use weighted sums, PICKUP selects the signed component with greatest absolute magnitude, and envelopes retain source-case provenance. | Derived behavior remains outside the wire contract and never mutates canonical base results while still supporting deterministic paging and extrema display. | Write derived values back into AnalysisResultSet; request a display-specific backend response. | 2026-09-20 |
| Identify every desktop shell content instance with a stable DocumentKey and restore layouts only through a whitelisted registry on the creating UI thread. | Stable keys allow same-key tool reuse, independent entity documents, and deterministic versioned restore without captions, CLR type names, Activator, or cross-thread DockPanelSuite access. | Persist captions or CLR types; recreate every pane on open; permit docking calls from background continuations. | 2026-09-20 |
| Persist LayoutState v1 as bounded strict UTF-8 JSON with live tab order, dock state, floating bounds, and logical active document; save atomically and restore transactionally with rollback. | A byte, character, and entry budget prevents unbounded local parsing, atomic replacement avoids partial files, and rollback keeps the live shell coherent when a layout application fails. | Persist unrestricted DockPanelSuite XML or CLR identities; apply partial layouts in place; include unsupported docked pane ratios and auto-hide state in v1. | 2026-09-20 |
| Serialize document-changing shell transitions, publish async results only against the captured document revision, and apply bounded cancellation-aware shutdown to active work, dirty saves, and layout persistence. | This prevents reentrant new/open/save/close actions and late non-cooperative completions from overwriting a newer document or hanging shutdown; dirty-save timeout aborts close, while recoverable layout-save timeout is diagnosed and permits close. | Allow overlapping commands; trust cancellation alone; wait indefinitely for external services during FormClosing; publish any completion that returns. | 2026-09-20 |
| Run desktop analysis through a Python-only FrameWeb.LocalRuntime with a per-launch CSPRNG bearer secret and require the loopback listener PID to belong to the private Windows Job before readiness and before every request. | The desktop path must not start Angular or trust a process that merely wins the loopback port race; keeping the token inside a preconfigured runtime-owned HttpClient preserves the local security boundary and deterministic child-process cleanup. | Reuse the combined legacy Startup host; expose the token to callers; trust readiness JSON without listener ownership verification. | 2026-09-20 |
| Build the first desktop viewport and PDF slice from bounded typed scene data and capture the live viewport on the UI thread; validate real OpenGL lifecycle behavior in an isolated probe process. | Stable domain IDs and explicit Z-up scene layers keep model, selection, results, and printing synchronized, while process isolation contains native WGL failure modes and makes context/resource leak acceptance observable. | Render from legacy dictionaries; reconstruct a second diagram for PDF; treat mock rendering tests as sufficient for native context lifecycle. | 2026-09-20 |
| Keep the Step 4 typed PDF exporter dependency-free and single-page with an independently parsed and rasterized approved golden; defer CJK font embedding, pagination, and full print parity to Step 8. | This proves the typed summary/result/viewport pipeline without reintroducing the vulnerable legacy PDF/image graph or unlicensed fonts, while preserving a fail-closed structural and visual regression gate. | Reuse PDF_Manager.LegacyPrinting or PdfSharpCore; embed restricted fonts now; claim full print parity from structural byte assertions alone. | 2026-09-20 |
| Implement Step 5 input authoring as one descriptor-driven 21-table WinForms editor over additive ProjectDocument v1 types, with atomic validated edit batches, dedicated model/set-management and prescribed-displacement surfaces, and shared bounded grid behavior. | A single typed editor shell keeps keyboard, clipboard, selection, undo/redo, lifecycle, and localization behavior consistent while still exposing dimension, non-default property/support/joint/spring sets, every load type, and DEFINE/COMBINE/PICKUP. Final-candidate validation gives multi-row operations one history entry and leaves document, undo, and redo unchanged on failure. | Build one custom control per input screen; keep set selectors display-only; merge force and prescribed-displacement rows under ambiguous raw IDs; validate each pasted cell as a separate edit. | 2026-09-20 |
| Provide the four built-in examples as validated typed semantic ProjectDocument builders with stable catalog IDs and pinned canonical analysis-request hashes, not as a public legacy Angular importer or row-for-row asset conversion. | Typed builders keep schema-v1 persistence and request meaning deterministic, bounded, and independently testable without importing legacy result, viewport, or version payloads into the desktop contract. Exact semantic assertions and request hashes detect geometry, material, support, load, and selector drift. | Embed the full Angular preset JSON and expose a legacy importer; assert only that four outputs differ; make legacy result/three/ver fields part of ProjectDocument. | 2026-09-20 |
| Model the desktop viewport as twelve typed stable-ID scene layers behind IViewportScene, ICameraController, and IHitTestService, with explicit node/member dependency closure, affected-layer invalidation, coalescing, and bounded materialization/compiled-work budgets. | Independent typed layers keep model, load, and result interaction deterministic while preventing stale cached geometry after topology edits and bounding valid-but-large local documents before UI/GPU amplification. | Rebuild one untyped scene on every edit; compare only each layer's direct records; allow renderer work to grow without explicit limits. | 2026-09-20 |
| Render grid, axes, labels, scale, and color legends through a bounded transparent bitmap uploaded as an OpenGL texture and composited in the same frame path used by live Paint and PNG capture; create the GL context only for a non-empty scene. | One render path gives screen and exported PNG identical decorations, keeps text rendering deterministic and bounded, and avoids main-form handle creation failures or resource growth for an empty workspace. | Draw WinForms/GDI overlays only after screen paint; maintain a separate PNG decoration path; eagerly create a GL context when the empty shell is shown. | 2026-09-20 |
| Keep generic PICKUP display as a signed greatest-absolute scalar while exporting a distinct typed engineering envelope with per-focus signed maximum/minimum source provenance and correlated force vectors; compute moving signed extrema from all sources but moving reaction absolute values from child cases with parent fallback; bound all presentation candidates and neutralize untrusted CSV text while retaining the private loopback HTTP calculation boundary. | This preserves the existing immutable presentation semantics while matching the Angular engineering export and moving-load reaction contracts, prevents valid-but-large results and spreadsheet interpretation from becoming local resource or data-injection hazards, and avoids coupling Step 7 presentation work to an unrelated calculation-transport redesign. | Collapse PICKUP display and export into one lossy scalar model; include moving parents in child reaction absolute projection; leave presentation work or CSV text unbounded; replace the approved Python loopback HTTP boundary during Step 7. | 2026-09-21 |
| Adopt official PDFsharp 6.2.4 behind the typed PDF_Manager.Printing boundary, render preview/export from one immutable plan under process-wide serialized PDF work, resolve bounded installed Windows fonts lazily per requested language without bundling font binaries, and retire FramePrintAzure during the Step 10 cutover instead of rebuilding a second print API. | One plan and one shared work budget keep preview and export consistent; serialized global font/PDF state avoids concurrency races; installed per-language fonts avoid redistributing restricted assets and keep English independent of optional CJK fonts; retiring the legacy host removes its anonymous unbounded endpoint and vulnerable PdfSharpCore/ImageSharp graph rather than preserving a second transport and presentation contract. | Keep the legacy PdfSharpCore/ImageSharp path; bundle restricted CJK fonts; allow concurrent process-global PDFsharp use; rebuild FramePrintAzure over the typed library. | 2026-09-21 |
| Ship the C# desktop first release without user authentication while retaining the per-launch private loopback bearer and Windows Job/listener ownership checks. | The user explicitly does not require login or an external identity provider. The desktop owns a local Python child process, so the existing ephemeral loopback secret and process-ownership validation remain necessary local IPC protections without creating account, token-storage, or tenant configuration work. | Add Microsoft Entra/B2C, Keycloak, another external provider, or remove the existing loopback process protections together with user authentication. | 2026-09-21 |
| Treat FrameWebforJS screen composition as the golden acceptance target for the C# desktop UI, not merely its business semantics. | The user clarified that the original request for the same major business scenarios meant the same screen composition. The current docking-centric shell materially differs in information architecture and therefore cannot be signed off as parity even though the underlying typed workflows work. | Keep the current native WinForms docking reinterpretation and validate only field meaning, validation, and workflow semantics. | 2026-09-21 |
| Make WorkspaceControl the sole visible viewport and OpenGL-context owner in the FrameWebforJS-parity shell; route panels and overlays consume typed state and commands but never create a second renderer, selection subscription, or result publisher. | A single owner preserves the completed renderer lifecycle, captured-revision publication, selection synchronization, cancellation, and bounded shutdown guarantees while the visible shell is replaced. It also makes resource-counter tests capable of detecting duplicated GL contexts and event subscriptions across route and overlay transitions. | Keep ProjectDocumentContent as a second visible viewport owner; allow each route screen to host its own renderer; retain the old docking document behind a compatibility shell. | 2026-09-21 |
| Use the proven Python (native) + Native attach and Python-breakpoint binding gate for development debugging of FrameWebforCS calls into FrameWeb/src through pythonnet; C#-to-Python F11 is not required. | The isolated PythonNetDebugProbe stopped in Python without F11 when the module was imported after attach and the source breakpoint had bound (Children > 0) before the target function was invoked. A two-stage application gate makes this order deterministic. | Rely on the ordinary C# F5 debugger, cross-language F11, a fixed delay after Attach2, or the unobserved Python LoadComplete event. | 2026-09-27 |
| Run FrameWebforCS desktop calculations in process through pythonnet, calling FrameWeb build_analysis_result_set directly; supersede the earlier private loopback HTTP desktop calculation transport for this application. | The user selected pythonnet after a working mixed-debugger attach and breakpoint-binding probe. One runtime owner supports the required attach, import, bind, and call sequence without a second calculation transport. | Retain the private loopback HTTP desktop calculation boundary or add an HTTP fallback alongside pythonnet. | 2026-09-27 |
| Do not impose a fixed analysis case-count ceiling in the initial release across the C# bridge, Python calculation/result validators, or Angular result validation; supersede the earlier 256-case rule. | The owner did not choose the inherited 256 value and prefers a simple initial version that accepts valid moving-load case fan-out without an arbitrary count rejection. Preserve per-case semantic validation and the existing ordered atomic result contract. | Keep the inherited 256-case cap; add a separate C# bridge cap or complex capacity policy before measuring a real need. | 2026-09-27 |
| Keep FrameWebforCS load-intensity Sheet2 and its bound list at exactly 100,000 global display slots, including anonymous rows with empty LoadId; express insertion and multirow deletion by shifting slot assignments across all Cases without changing row count. | The owner requires Sheet1-like fixed capacity, preserved empty positions, and predictable keyboard editing. Reject insertion when the final slot is assigned so no load is discarded; separate display slots from Case-local row identities to preserve load and viewport semantics. | Resize the sheet/list for row operations; filter or shift only one Case; discard the occupied tail; automatically assign a Case to every empty row. | 2026-09-28 |
| Persist sparse global load-intensity placement in optional versioned load_intensity_layout input metadata, while retaining per-Case input_rows for assigned empty rows and excluding global layout metadata from calculation snapshots. | Anonymous gaps and mixed-Case display order must survive save/reload without dummy loads or 100,000 empty records. Joint load/layout validation before document replacement preserves atomicity; older files without metadata retain canonical Case/row placement. | Discard blank positions on reload; encode anonymous gaps as dummy node/member loads; replace Case-local row numbers with global display indices; include desktop layout in FEM input. | 2026-09-28 |

## TODO / Open Questions

- Complete end-to-end browser verification of the sole `AnalysisResultSet` path, including first/last static cases and all accepted nonlinear steps.
- Keep production authentication and deployment configuration separate from the anonymous local-development calculation path.

- Retire the temporary `PDF_Manager.LegacyPrinting` bridge during Step 10 cutover, only after screen-parity sign-off and the signed clean-machine release candidate pass; do not maintain a compatibility adapter.

- Before any legacy print host deployment, require non-anonymous POST-only access, bounded body/decompression/page/image/time/concurrency work, cancellation, a patched PDF/image dependency graph, and removal or approved replacement of embedded CJK fonts.

- Resolved 2026-09-27: `FrameWebforCS/` calculation uses pythonnet rather than the earlier private loopback HTTP boundary. The first C# desktop release still has no production authentication provider.
