## Implementation Plan: Rail Transverse Print Parity Verification

### Purpose

指定された元の `線路直角方向.json` を現行の FrameWebforCS 解析・印刷経路で再実行し、現在の `基本.pdf`、`PICK UP.pdf`、`変位量.pdf` と数値、対応関係、帳票構成、全ページの見た目を照合する。完全一致、差異あり、検証未完了を根拠付きで判定する。製品修正はこの検証の対象外とする。

### Scope

- New files: `.agents/docs/research/rail-transverse-print-parity-report.md` に判定、全件集計、差異例、画像確認結果と再実行手順を残す。新規の非公開 `.tmp/rail-transverse-print-parity/<run>/` に元入力・参照の固定コピー、実行ログ、生成 PDF、解析中間値、全行比較とページ画像を保存する。
- Modified files: この計画書のみ。確認段階では製品ソース、既存の検証器、K: 上の JSON/PDF、既存の `.tmp/transverse-analysis-print-verification/20261015T182603/` と `.tmp/transverse-analysis-print-corrections/20261015T193610/` は変更しない。
- Read-only production paths: `FrameWebforCS/Headless/HeadlessCalculation.cs` の要求作成→Python 計算→派生結果→投影→PDF 生成、`FrameWebforCS/components/print/PrintDialogForm.cs` の共通投影／生成呼び出し、`FrameWebforCS/providers/printing/PrintProjection.cs`、`FramePrintPDF/PDF_Manager/Printing/PrintResult/` を照合対象とする。従来の検証ハーネスは `.tmp/transverse-analysis-print-verification/20261015T182603/harness/`、比較・監査・画像化ツールは `.tmp/transverse-analysis-print-corrections/20261015T193610/` にある。
- Dependencies: リポジトリルートの PowerShell、ロック済み FrameWeb 環境、.NET Desktop 環境、日本語印刷フォントと既存 PDF 生成基盤を使用する。PDF 抽出・ラスタ化は既存の私設ツールとその実行環境を再利用し、製品依存パッケージを増やさない。
- Baseline: 2026-10-15 に実ファイルを確認した。元 JSON は 44,097 bytes、SHA-256 `c2798b3c8aa687f7b2220aa57f31dd6ad91f9f66a41685edae450386b7ae6553`。更新後の参照 PDF の SHA-256 は基本 `3f543da4a08ac47c69034e99d5364316a61a9c6fc17b6c41646ba6ddbf707d4f`、PICK UP `0dd0a1c861c0148865b6b09cfaa25ee9e9e66fc3a711e26c8a513aa0c4c75ec9`、変位量 `67871f0298b11ae2060ac368ac180573d25ef557bdb169593220b80eb52bfbb0`。これらは既存の更新後固定コピーと一致する。作業ツリーは開始時点でクリーン、HEAD は `018d0d0ee9453464b0b541a5344ce5a6c0acdfc6`。
- Prior result boundary: `.agents/docs/research/transverse-analysis-print-corrections-report.md` の final-9 は元 JSON ではなく、CASE 10 の行 ID 11 件とレイアウト参照 11 件を修正した私設コピーで比較した。数値ベクトル一致は基本 904/950、PICKUP 2470/2850、変位 173/240、PICKUP 採用元の差 215 行、部材名称欠落を記録している。この結果は既知の不一致を示すが、今回の**元 JSON**の再実行結果として転用しない。

### Implementation Steps

元入力と参照の固定、実経路の再実行、全件の内容照合、全ページの視覚照合、判定報告の順に進める。

#### Step 1: 対象と証拠を固定する

- [ ] K: の元 JSON と現在の 3 PDF の存在、長さ、SHA-256、PDF ページ数・用紙寸法を再確認し、新しい実行ディレクトリに読取専用コピーとマニフェストを作る。実行前後の外部ファイルハッシュも照合する。
- [ ] 元 JSON と修正コピーの 22 編集を別入力として識別する。今回の本判定には元 JSON だけを使い、修正コピーの final-9 と過去の 15/45/4 ページ参照を履歴比較に限定する。
- [ ] 現行 HEAD、ソースとツールのハッシュ、コマンド、.NET/Python/PDF 環境を記録する。既存ハーネスの固定入力ハッシュ・元節点数などの前提を点検する。

**Verification**: 4 ファイルが指定ハッシュと一致し、基準 3 PDF のページ数・寸法が 30/60/2 ページの A4 縦である。各コピーのハッシュが原本と一致し、過去の証拠と外部原本に変更がない。

#### Step 2: 元 JSON を製品経路で再解析・印刷する

- [ ] 新しい実行ディレクトリに複製した私設ハーネスから実際の `CalculationRequestBuilder`、Python runtime、`CalculationDerivedPresenter`、`PrintProjection.Build`、`DirectPdfGenerator.Generate` を呼び、同一の新鮮な解析結果から 3 PDF を生成する。印刷条件は **A4 縦、`ja`、題名 `線路直角方向`** に固定し、選択肢を `[SectionForce]`、`[PickupSectionForce]`、`[Displacement, CombinedDisplacement]` とする。変位は入力にある元の 12 節点を選び、断面力・PICKUP の部材と成分は全対象、その他のフィルターは無しとし、実際の選択値をマニフェストに記録する。旧ハーネス既定の横向きを使わない。Headless CLI の公開項目には `CombinedDisplacement` がないため、この 3 PDF は直接 API を呼ぶ私設ハーネスで生成する。
- [ ] 保存ケース／解析ケース、DEFINE／COMBINE／PICKUP、元節点・部材と生成点の対応、警告・省略理由、単位・軸・符号変換を中間成果物に記録する。保存済み結果や過去の PDF を代用しない。
- [ ] `FrameWebforCS/Headless/tests/FrameWebforCS.Headless.Tests.csproj` の `RealProcessRecalculatesAndEmitsRequestedArtifacts` はプロセス／成果物のスモークに限定して実行する。`FramePrintPDF/PDF_Manager.Tests` の `TransverseReportCompositionTests`、`FrameWebforCS/tests/PrintProjection.Tests` の対象回帰テストを実行し、実行件数と失敗内容を記録する。これらを外部 PDF 一致の代替にしない。

**Verification**: 実計算が成功し、同じ解析スナップショットから 3 つの読める PDF が生成される。ケース・対象範囲・設定・中間成果物・ハッシュがマニフェストから追跡でき、元 JSON と既存参照は不変である。

#### Step 3: 数値、採用元、行構成を全件照合する

- [ ] 既存の参照抽出器・生成 PDF 抽出器・比較器・独立監査器を**新しい実行ディレクトリへ複製**してから実行し、全ページの表・見出し・数値トークンをページ／列／座標つきで台帳化する。`reference-refresh/compare_updated.py` は新 run の入力を `--data`、出力を `--out`、レイアウトを `--product-template portrait`、同位置ラベル照合を `--label-associations` で明示する。監査器の固定 `RUN`／`DATA` が新 run のみを指すことを実行前に検査し、旧 run に一切書き込まない。旧版から流用する parser の仮定が現行レイアウトでも成立するか確認する。
- [ ] 帳票、ケースまたは PICKUP、焦点／最大最小、元節点・部材、距離、位置側、成分をキーに全行を対応付ける。欠落、余剰、重複、同位置の曖昧な行を隠さない。表示値の符号・丸め・単位、M/S/N の同一状態ベクトル、PICKUP の COMBINE 採用元と元ケース式、変位と COMBINE の 3 成分、名称・注記・剛域ラベルを別々に判定する。
- [ ] 印刷トークンを基準精度で厳密に比較し、独立算術監査で元の解析結果から DEFINE／COMBINE／PICKUP を再計算する。比較器の数値改変、採用元変更、行追加・削除・重複などの既存対照試験も再実行する。

**Verification**: 参照・生成側の全数値トークンと全行が一致／差異／対応不能のいずれかに計上される。3 帳票それぞれに一致数、相違成分・採用元、欠落／余剰／曖昧行、最大差と原文ページ位置があり、広い相対許容差や値を使った行合わせで一致扱いしない。

#### Step 4: PDF の見た目を全ページ照合する

- [ ] 参照と生成 PDF を同じ解像度で全ページ画像化し、ページ数、A4 縦寸法、表の並び、ケース・焦点の順序、改ページ、余白、文字種・フォント表示、数値の可読性、図、欠け、重なり、空白ページを確認する。
- [ ] 画像差分とページ対応表を作り、各帳票の代表ページおよび差異のある全ページを原寸の左右比較で確認する。ページの並びが異なる場合はまず帳票内容で対応付け、画像の全画素一致や PDF バイト一致だけを合否基準にしない。字体・文字位置の差も明示して記録する。

**Verification**: 参照 92 ページと生成全ページにレビュー記録があり、各差異をページ・座標・画像に結び付けられる。欠け、重なり、文字化け、図の欠落、帳票配置差と、実害のない描画差を区別して可視の合否を記載する。

#### Step 5: 根拠付きの結論を公開する

- [ ] 3 PDF ごとに数値、行・採用元・名称、構成・見た目の判定をまとめ、**完全一致／差異あり／検証未完了**のいずれかを明示する。元 JSON の実行と修正コピー final-9 の既知結果を並べる場合は入力を明記する。
- [ ] 差異を入力変換、FEM 算定、派生集計、印刷投影、PDF 組版、資料の履歴差、不明に分類し、確定原因と仮説を分ける。修正が必要なら再現位置と最小の次作業を提示するが、この計画では製品値や原本を変更しない。
- [ ] レポート、比較台帳、実行マニフェスト、画像証拠を相互参照し、外部ファイルの最終ハッシュ、`git status` と関連チェック結果を記録する。

**Verification**: レポートの各結論が再実行コマンドと行／ページ証拠を持つ。1 件でも未説明の数値・採用元・名称・見た目の相違、または抽出漏れがあれば完全一致と報告しない。

### Risks & Considerations

- 元 JSON は過去の参照 PDF 作成時の入力そのものと証明されていない。現在の PDF と元 JSON の差は自動的に製品欠陥と断定しない。既存 final-9 は修正コピーを使ったため、今回の判定を代替しない。
- 数値の自己整合性が通っても、参照数値と一致する証明にはならない。表示丸め、符号、単位、同位置の二重行、厳密な PICKUP の選定元を別々に照合する。
- 印刷用の 2D ベクトル図には既報のケースキー・駅点写像の疑義があり、表の一致から図の正しさを推論しない。図は実 PDF で確認する。
- 既存比較器とハーネスは私設・旧実行由来であり、現行入力・レイアウト前提と全件抽出を対照試験で確認する。既存の証拠や K: 原本は上書きしない。
- 旧比較器・監査器の出力先は過去の実行ディレクトリを既定としている。新 run に複製し、全入出力先と portrait 指定を明示・検査してから実行する。
- 日本語フォント・PDF 描画エンジン差によるラスタ差は証拠として残し、数値の一致と視覚的一致を混同しない。ユーザー指定により字体・配置の差も最終判定で報告する。

### Open Questions

- 参照 PDF 作成時の元入力、使用ソフトと設定は確定していない。追跡可能な資料が見つかるまで履歴上の同一性は未証明として扱う。これは今回の差異判定を妨げない。
- 参照 PDF にだけある部材・断面名称の権威ある出典はまだ不明。名称差を記録し、根拠なしに生成側へ補わない。
