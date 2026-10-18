## Implementation Plan: Result Combine Displacement Background Aggregation

### Purpose
`FrameWebforCS` の組合せ変位画面に、`ResultDisgService` の基本ケース結果と `InputCombineService` の係数から求めた `COMBINE` 変位を表示する。旧 Angular 版と同じく、重い集計は画面スレッドの外で行い、結果だけを WinForms に反映する。対象はユーザーが指定した .NET 10 の既存クライアントであり、`.NET 8`／`AnalysisResultSet v1` を使う `FrameWebforCS.v1` のリリース経路は変更しない。

### Scope
- New files: `FrameWebforCS/components/result/ResultCombineDisgAggregator.cs`（UI に依存しない集計器）、`FrameWebforCS/components/result/ResultCombineDisgCoordinator.cs`（表示 revision と immutable snapshot）、`FrameWebforCS.Tests/FrameWebforCS.Tests.csproj` と `FrameWebforCS.Tests/ResultCombineDisgAggregatorTests.cs`、必要な画面／更新テスト。
- Modified files: `FrameWebforCS/components/result/ResultCombineDisgComponent.cs` と `.Designer.cs`、`FrameWebforCS/components/result/ResultDisgService.cs`、`FrameWebforCS/components/input/InputCombineService.cs`、`FrameWebforCS/providers/InputDataService.cs`（読込完了通知を加える最小差分）、`FrameWebforCS/components/result/ResultPickupDisgComponent.cs`（既存継承の分離に必要な最小変更）、`FrameWebforCS/FrameWebforCS.csproj`（テストから内部型にアクセスする場合）、`FrameWeb.sln`（テスト登録）。
- Dependencies: 既存の .NET 10／WinForms／FarPoint Spread と `ResultDisgComponent.SetSheet1` を使用し、実行時パッケージは追加しない。テスト用パッケージは既存 .NET テストプロジェクトの版に合わせる。
- 対象は `result-combine-disg` の `COMBINE` のみ。`DEFINE` の最大・最小は内部計算に必要な中間値とし、独立した `DEFINE` 画面や `PICKUP` 集計、Python FEM、旧 Angular 製品コードは変更しない。`InputDataService.cs` の既存の staged／unstaged 変更を保持して読込境界だけを追加する。既存変更のある `FrameWebforCS/three/SceneService.cs` は触れない。

### Implementation Steps

各段階で確認した結果を次の段階の入力とする。

#### Step 0: 既存差分と基準状態を記録する
- [ ] `git status`、`git diff --cached`、`git diff` を別々に確認・保存し、`InputDataService.cs` の staged／unstaged 差分と `SceneService.cs` の変更を所有者の作業として保護する。
- [ ] 対象 C# プロジェクトの基準ビルドを実行し、既存の失敗を記録する。実装途中で同じファイルに追加変更があれば、再読込して自分の差分だけを統合する。
**Verification**: 実装前後の差分比較で既存の hunk が残り、今回の変更と基準ビルドの失敗を区別できる。

#### Step 1: 旧版の計算規則と表示契約を固定する
- [ ] `FrameWebforCS.Tests/FrameWebforCS.Tests.csproj` を `net10.0-windows` で作成し、対象プロジェクト参照と内部型のテストアクセスを設定する。サービス共有テストは全体並列実行を無効化し、非同期 message pump を持つ STA runner または fresh-process 隔離で実行する。
- [ ] `result-combine-disg1.worker.ts`、`result-combine-disg2.worker.ts`、`result-data.service.ts` に基づき、`DEFINE` の符号付き基本ケースから成分ごとの最大／最小を選び、`COMBINE` 係数で加算する規則を小さな期待値 fixture にする。`DEFINE` が無い場合は基本ケースをそのまま参照する旧版の規則も含める。
- [ ] `C<n>` は JSON の数値定義 `Id=n` を参照し、`row` は表示順にだけ使う。`Id != row` fixture を用意する。DEFINE が完全に空のときだけ、`InputLoadService.CaseIds` に存在する数値の static 基本ケースを暗黙 DEFINE とする。欠落参照と零係数は旧版同様に読み飛ばし、有効な項がない組合せは空として扱う。
- [ ] 項は係数の入力順、節点は数値 ID 昇順とそれ以外の入力順、同値時は先勝ちとする。着目成分の極値が選んだケースの6成分全体を採用する。`null` 成分は0として計算し、表示は旧 worker2 の `Math.round(10000*x)/10000`→`toFixed(4)` に合わせる。2D は `dx/dy/rz`、3D は6成分とし、シート名は組合せ `Id` と `name` から一意に構成する。非有限値と節点不一致は明示的なエラーとし、基本結果は変更しない。
- [ ] 旧 worker を制御された harness で実行し、値と元ケース欄の golden を得る。負係数時の `case` 文字列には旧実装の疑わしい式（worker1:101）があるため、このタスクでは観測された表示を互換契約として fixture に残し、修正提案は別件に分ける。`case 0`、非数値の子ケース ID、重複する組合せ名称も確認する。
- [ ] 期待値を先に `FrameWebforCS.Tests/ResultCombineDisgAggregatorTests.cs` に書き、未実装時に失敗することを確認する。既存の `FrameWebforCS.v1` テストとは混ぜない。
**Verification**: 正負の基本ケース、最大／最小の同値、負・小数係数、`Id != row`、DEFINE 省略時の load との積集合、欠落・零係数、`null` と `case 0`、項順・節点順・全6成分採用・丸めを手計算値および実行済み旧 worker の golden と照合し、対象テストが実装前に失敗する。

#### Step 2: UI に依存しない集計器を実装する
- [ ] `ResultCombineDisgAggregator` に読み取り専用入力と結果型を定義し、`DEFINE` 中間値から `COMBINE` の各モード・節点値・元ケース欄を計算する。元データを変更せず、計算途中の失敗を部分結果として公開しない。
- [ ] 定義数10,000、組合せ数1,000、節点数100,000、総スカラー演算50,000,000、生成セル10,000,000を上限とし、スナップショットの深いコピーより前に件数・推定演算量を `checked` で計算して拒否する。キャンセルをループ中にも確認する。数値の丸めと文字列化は計算から分離する。
**Verification**: 集計器の単体テストで 2D／3D、値とケース欄、順序、入力不変性、空・異常・キャンセル、各上限の exact／+1 を確認する。

#### Step 3: 変位と係数のスナップショットおよび更新通知を用意する
- [ ] `ResultDisgService` の基本結果と `InputCombineService` の `DefineRows`／`CombineRows` を、画面スレッドで深くコピーしてから背景タスクへ渡す。現在公開される可変 Dictionary や `clsCombine.Coefficients` を背景タスクから直接列挙しない。
- [ ] `ResultDisgService.setDisgJson` は `DataHelperModule` の寛容な汎用変換ではなく、`result[id].disg[node]` の6成分を有限数値または明示的 `null` として検証する candidate parse にし、成功時だけ置換する。不正型・範囲外と `null` を区別し、欠落した `result` は旧結果の再利用を禁止する。
- [ ] 結果の clear／読込、組合せの clear／読込／セル編集で成功した変更を画面へ通知する。既存の `RowsReplaced` 利用者を維持し、通知の重複や失敗した編集での通知を避ける。
- [ ] `ResultCombineDisgCoordinator` に `Loading / Valid / Invalid` の表示 revision を持たせ、`InputDataService.JsonDataOpen` の冒頭で旧表示を無効化する。全サービスと dimension の読込完了後に限り、両サービスと load ケースの immutable snapshot を一回で確定・通知する。例外または `result` 欠落なら `Invalid` にして再 throw／空表示とし、後続の編集・clear・再表示では古い結果から再集計しない。成功後の個別セル編集だけを新 revision にする。
**Verification**: スナップショット取得後に元の結果／係数を更新しても計算入力が変わらず、A→B連続読込、Bの `result` 欠落・後段例外、不正変位型、失敗後の編集・clear・再表示で A/B 混成結果を一度も公開しないテストを行う。

#### Step 4: 背景集計を画面のライフサイクルへ接続する
- [ ] `ResultCombineDisgComponent` の初回表示、読込完了、個別編集、キャッシュされた画面の再表示時に `Valid` な最新スナップショットだけを集計する `Task.Run` 経路を設ける。実行中1件＋最新保留1件に制限し、再要求時は古い計算をキャンセル／世代番号で無効化する。UI 反映の直前にも世代を確認し、破棄後や新しいデータの読込後に古い結果を表示しない。
- [ ] 完了・空・エラー状態を UI スレッドでのみ公開する。画面キャッシュ（`AppRoutingModule.cs:85-99`）からの再表示でも更新を確認し、通知購読とキャンセル資源を破棄時に解放する。
**Verification**: 遅い A の途中で B を読み込むテストでは B だけが表示され、表示切替・破棄・空データ・計算例外でもクロススレッド例外や古いシートが残らない。

#### Step 5: COMBINE の結果を Spread に表示する
- [ ] 固定20ケースのプレースホルダーから離れ、計算済みの組合せ ID／名称でシートを作る。同名でも ID を含む一意のシート名を使う。旧版の最大／最小モードを選べる UI を置き、選択中の組合せ／モードだけの節点行、2D／3D 成分、元ケース欄を遅延生成する。既存ヘッダーを再利用し、表示値を小数4桁に整える。
- [ ] 結果0件で `Sheets.First()` を呼ばない空表示を用意し、シート・モード・列幅を新しい結果へ一括で置き換える。`SidebarComponent` が COMBINE に渡す `option=1` は画面種別でありシート index として使わず、初回は先頭の組合せを選ぶ。画面内のシート切替は別に扱う。
- [ ] `ResultPickupDisgComponent` が現在 `ResultCombineDisgComponent` を継承し、`getCombineDisg()` を override する点を整理し、PICKUP 画面に COMBINE の集計やモードが流入しないよう既存表示を保つ。
**Verification**: STA 画面テストでシート数・名称・値・ケース欄・モード切替・2D／3D 列・空表示・再読込・`option=1` の初回先頭選択・PICKUP の `option=2` 継承境界を確認する。

#### Step 6: 対象プロジェクトと変更範囲を検証する
- [ ] 新テストプロジェクトを `FrameWeb.sln` に登録する。シングルトンサービスをテストごとに初期化し、非同期 message pump 対応の STA runner を使い、テスト全体の xUnit 並列実行を抑止する。初期化し切れない依存状態は fresh-process で隔離する。
- [ ] 集計器／画面テストと `FrameWebforCS` ビルドの後に、`dotnet test FrameWeb.sln -c Release` と `dotnet build FrameWeb.sln -c Release` を実行する。
- [ ] `git diff --cached` と `git diff` を Step 0 の証跡と照合し、既存の `InputDataService.cs`／`SceneService.cs` 変更、旧 Angular、`FrameWebforCS.v1`、PICKUP の演算仕様に不要な変更がないことを確認する。
**Verification**: `dotnet test FrameWebforCS.Tests/FrameWebforCS.Tests.csproj -c Release`、`dotnet build FrameWebforCS/FrameWebforCS.csproj -c Release`、上記ソリューションゲート、`git diff --check` を実行し、既存変更が保持される。リポジトリ全体の失敗があれば対象変更と切り分けて報告する。

### Risks & Considerations
- 旧 worker はモードごとの `DEFINE` 選択を先に行うため、単純な基本ケースの重み付き和では一致しない。負係数・同値・ケース欄を fixture で固定する。
- 結果サービスと係数サービスは可変状態を公開し、画面はキャッシュされる。スナップショットと世代管理を外すと再読込時に例外や古い表示が起きる。
- `JsonDataOpen` は複数サービスを順に更新する。読込完了通知より前に計算すると新しい係数と古い変位が混じる。入力結果の欠落／途中失敗も含め、表示 revision を一括確定する。
- 現行の汎用変位変換は不正型を黙って落とす。対象サービスの厳密な candidate parse で `null` と不正入力を分けるが、既存の保存形状は維持する。
- `ResultPickupDisgComponent` は対象画面を継承している。共通コンストラクターを直接 COMBINE 専用にすると PICKUP を壊すため、継承境界をテストする。
- 大きな結果では12モード×節点×組合せの計算と Spread 更新が増える。計算上限、キャンセル、必要な表示モードの描画だけを採用する。
- 設計記録の `AnalysisResultSet v1` は `FrameWebforCS.v1` 側の別系統である。今回指定された `FrameWebforCS` の既存サービスを入力とし、両者の契約を混在させない。

### Open Questions
- なし。対象はユーザー指定の `FrameWebforCS`、表示範囲は `COMBINE` のみ。`DEFINE` は内部中間値、`PICKUP` は現状維持とする。
