# Handoff — FrameWebforJS画面構成の完全parity調査・修正計画

## Goal

次セッションでは製品コードを直ちに変更せず、`FramePrintPDF/PDF_Manager` の現行WinForms UIと、実際に動く `FrameWebforJS` の画面構成を画面・状態ごとに比較調査し、C# desktopを `FrameWebforJS` と全く同じ画面構成へ修正するための実装計画を策定する。

ユーザーの確定した意図は、`.agents/docs/plans/csharp-frameweb-client.md` の「同じ主要業務シナリオ」が単なる機能・入力意味の同等性ではなく、shell hierarchy、navigation、各入力・結果・印刷画面、表示field/control、配置・順序・grouping、default visibility、viewport/table split、overlay、画面遷移を含む **screen-composition parity** を意味する、というものである。現行の汎用Docking UIをWinForms流の別解として維持することは受入不可。

## Current Progress

- Branchは `sasa/csharp`、現在のHEADは `778c243`（Step 8 typed printing完了）。commit、stage、pushはこのhandoff作業では行っていない。
- Step 0〜8のtyped domain、ProjectDocument、AnalysisResultSet v1、21入力表、OpenGL scene、結果presentation、公式PDFsharp印刷、private loopback Python runtimeは実装済みで、直近の記録は両Release build 0 warnings/errors、両solution tests 567/567 PASS。
- 添付された現行C#画面 `C:\Users\sasai\Pictures\Screenshots\スクリーンショット 2026-09-21 125546.png` を確認した。左のproject navigation、中央のdocked model viewport/result grid、右のgeneric editor tabs、下のdiagnosticsという独自構成であり、`FrameWebforJS` の画面構成再現ではない。
- 根本原因は現行計画にある。Purposeは同じ主要業務シナリオを要求する一方、Feature-Parity Boundaryは「Angularの画面構造ではなく入力意味とvalidationを一致」、ShellはDockPanelSuite中心と明記しており、実装はその解釈に従った。
- `.agents/docs/DESIGN.md` には今回、`FR-CS-DESKTOP-UI-PARITY-1` と「FrameWebforJS screen compositionをgolden acceptance targetとする」設計決定をtyped writerで追加済み。`result=applied`、requirement 1件、decision 1件、duplicate 0件。
- DESIGNにはこのhandoff以前から未commitの「初回releaseはuser loginなし、private loopback process securityは維持」という変更がある。次セッションはこれをユーザー所有の既存変更として保持し、resetや上書きをしない。
- このhandoff以外の製品コードは変更していない。UI修正案はまだ未調査・未承認である。

## What Worked

- Core、Rendering、Printing、LocalRuntimeをUI shellから分離したため、計算契約、validation、scene生成、結果導出、PDF生成の大部分は残したまま表示層を組み替えられる。
- `FrameWebforJS/src/app/app-routing.module.ts` は入力、結果、print routeの一覧を持ち、`app.component.*`、`components/doc-layout/**`、`menu/**`、`optional-header/**`、`three/**`、各 `input-*`、`result-*`、`print/**` が画面構成の一次資料になる。
- C#側の主な比較対象は `FramePrintPDF/PDF_Manager/Shell/MainForm.cs`、`Contents/EditorContent.cs`、`Contents/ProjectDocumentContent.cs`、Docking配下、Resources配下である。
- typed editor descriptors、stable IDs、revision/cancellation boundaries、shared clipboard/undo、rendering/printingのbudgetはUI parity後も再利用すべき基盤である。

## What Didn't Work

- 「業務シナリオの同等性」を「意味・validationの同等性」と狭く解釈し、見た目と情報設計を別物にした。ユーザーの要求は最初からFrameWebforJSと同じ画面構成だった。
- Step 3のdocking shell、Step 5のdescriptor-driven generic editor、Step 9のscenario matrixは、routeごとの外観・field配置・visibility・transitionをgoldenとして固定していない。
- 567件のgreen testsはdomain、lifecycle、rendering、printingの正しさを示すが、FrameWebforJSとの視覚・構成parityを示さない。今後この件でtest件数だけをparity根拠にしない。
- 現行計画のStep 9 packaging/cutoverへそのまま進むと、誤ったUIを製品化してしまう。UI parity調査、計画修正、ユーザー承認が終わるまでcutover・legacy削除・Angular archiveを開始しない。
- generic DockPanel配置の微調整だけでは不足する。FrameWebforJSのrunning UI、HTML、SCSS、route/stateを一次資料として、screen-by-screenで再構成する必要がある。

## Next Steps

1. `context-loader`、`linksee-memory`、`plan` skillを使用する。最初に `git status` と既存diffを確認し、DESIGNの未commit変更を保全する。調査・計画セッションでは、ユーザーの別承認なしに製品コードを変更しない。
2. `FrameWebforJS` を実際に起動し、固定viewport・DPIで基準screenを撮影する。少なくともstart/preset、全input route、2D/3D model/load表示、static/nonlinear/modal、DEFINE/COMBINE/PICKUP、moving、print/preview、dialog/overlay、ja/en/zh、empty/populated/error/cancel stateを対象にする。現在のC#画面も同条件で撮影する。
3. `app.component.html/.scss`、`doc-layout`、`menu`、`optional-header`、`start-menu`、`three`、全 `input-*`、全 `result-*`、`print/**` を読み、各screenについてroute/state、親子layout、navigation、control/field、label、順序、group、default visibility/read-only、scroll/paging、modal/overlay、resize/DPI behaviorをinventory化する。
4. source inventoryとrunning screenshotを正本に、FrameWebforJS ↔ C#のscreen parity matrixを作る。各rowにreference screenshot、Angular source、現在のC# surface、差分、再利用可能service/model、必要な新WinForms control、interaction/visual acceptanceを記録する。
5. 現行 `MainForm` / `EditorContent` / `ProjectDocumentContent` / DockPanel構成を監査し、何を保持・非表示・置換・削除するか決める。内部のstable `DocumentKey`やlifecycleは再利用可能だが、FrameWebforJSにないuser-visible docking/navigationを既定UIとして残さない。
6. `.agents/docs/plans/csharp-frameweb-client.md` を修正する計画を作る。少なくともPurpose、Feature-Parity BoundaryのInput/Shell行、Step 3/5/6/7のUI受入条件、Step 9 parity/cutover順序を今回の決定へ整合させる。必要ならStep 9前に専用のUI parity remediation phaseを追加し、既完了stepを無条件にgreenのまま扱わない。
7. 実装はvertical slicesで計画する。推奨順は共通shell/navigation → start/preset → representative input screen → remaining inputs → model/load viewport integration → results/derived/moving → print/preview → languages/dialog/error states。最初のrepresentative sliceを同寸side-by-sideでユーザー承認してから横展開する。
8. acceptance gateには、source-derived field/control matrix、UI automation、固定viewport/DPI screenshot比較、resize/scroll、三言語、keyboard/clipboard、empty/populated/error/cancel state、および既存567 testsの非退行を含める。自己生成したC# screenshotだけをgoldenにせず、FrameWebforJS referenceとの比較とユーザー目視承認を必須にする。
9. 調査結果と修正版実装計画をユーザーへ提示し、承認を得てから実装用のfeature/team-executeへ進む。Step 9 packaging、Startup切替え、FramePrintAzure/LegacyPrinting削除、Angular archiveはUI parity sign-off後に行う。

次セッションの成果物は「調査報告」「screen parity matrix」「修正後の段階的実装計画」「受入基準」であり、未承認のUI実装ではない。
