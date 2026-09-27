## Implementation Plan: C# Desktop 2D/3D Dimension Switch

### Purpose
`FrameWebforCS` のメニューから解析次元を 2D／3D に切り替え、入力表、ビューポート、保存・読込、結果表示まで一貫して反映する。旧 `FrameWebforJS` の入力値保持を踏襲しつつ、旧版で起こり得る開いた表や結果の表示ずれを防ぐ。

### Scope
- New files: `FrameWebforCS.Tests/DimensionSwitchTests.cs`（状態遷移・保存・画面更新）、`FrameWebforCS.Tests/DimensionResultPresentationTests.cs`（結果次元と保存・再読込）。
- Modified files: `FrameWebforCS/components/menu/MenuComponent.cs`、`FrameWebforCS/components/menu/MenuComponent.Designer.cs`、`FrameWebforCS/providers/InputDataService.cs`、`FrameWebforCS/three/SceneService.cs`、`FrameWebforCS/three/ThreeService.cs`、`FrameWebforCS/AppRoutingModule.cs`。
- Modified input views: `FrameWebforCS/components/input/InputNodesComponent.cs`、`InputMembersComponent.cs`、`InputElementsComponent.cs`、`InputFixNodeComponent.cs`、`InputFixMemberComponent.cs`、`InputJointComponent.cs`、`InputLoadComponent.cs`。最後の2次元別列を持つ旧版の荷重・剛域表示については、C#版に対応する表の現状を確認して必要な表示更新のみ追加する。
- Modified result paths: `FrameWebforCS/components/result/ResultDisgComponent.cs`、`ResultReacComponent.cs`、`ResultFsecComponent.cs`、`ResultCombineDisgCoordinator.cs`、`ResultCombineFsecCoordinator.cs`、`ResultCombineReacCoordinator.cs` と、それらを表示する `ResultCombine*Component.cs`／`ResultPickup*Component.cs` のうち、入力次元を結果表示に誤用している箇所。
- Dependencies: 既存の WinForms、FarPoint Spread、THREE 系サービスと `FrameWebforCS.Tests` を再利用する。新規パッケージは追加しない。変更対象は `FrameWebforCS/` と `FrameWebforCS.Tests/`、この計画書に限る。`FrameWebforJS/` は参照のみで変更しない。`FrameWebforCS.v1/`、Python FEM、計算HTTP経路、印刷機能は対象外。

旧版の操作入口は `FrameWebforJS/src/app/components/optional-header/optional-header.component.ts:262-269` であり、次元変更後に荷重図とカメラを更新する。入力値は消さず、保存は隠れた3D値と結果を含み、2D解析要求だけ別の JSON に補正する（`FrameWebforJS/src/app/providers/input-data.service.ts:191-385`）。入力表、結果表、3D専用入口、断面力図、印刷選択肢は次元依存だが、旧版の切替入口は開いた表と既存結果を同期しない。C#版では表示ずれを解消する。ユーザー指定により初期値は現行C#版の3Dを維持し、結果データは切替後および保存・再読込後も保持する。

### Implementation Steps

各段階で前段の状態契約とテストを満たしてから次へ進む。

#### Step 1: 切替契約と回帰テストを固定する
- [ ] 2D→3D→2D、同じ次元の再選択、JSON読込、失敗時復元を対象に、`InputDataService.dimension`、メニュー、カメラ、開いた表、保存JSONが一致する期待値をテスト化する。初期値3を固定する。
- [ ] 2D中に非表示となる Z、部材コードアングル、断面特性、拘束・結合値などを編集済み入力として用意し、往復切替と保存・再読込で失われないことを確認する。
- [ ] 結果を保持する契約を固定する。入力次元変更後も結果データと結果作成時の次元を保持し、結果表を新しい入力次元の列に読み替えない。結果図のうち旧次元と不整合になる表示は明示的に停止または区別する。
- [ ] 2D結果→3D入力と3D結果→2D入力の両方で保存・再読込を試し、結果の値・作成時次元・入力値を保持する期待値を固定する。従来ファイルに結果次元メタデータがない場合は `dimension` を結果次元として読む。結果のないファイルには余分な結果メタデータを付けない。
**Verification**: 新規テストは未実装状態で期待どおり失敗し、切替・値保持・結果次元と保存再読込を個別に検出できる。

#### Step 2: 解析次元の単一変更経路を設ける
- [ ] `InputDataService` に 2／3 のみを受ける変更経路と変更通知を設ける。初期値3を維持し、同値の再選択は無変更にする。現行の公開 setter を利用する呼び出しを調べ、通知を迂回できない形にする。
- [ ] カメラ適用が失敗した場合は次元・カメラ・通知状態を戻す。成功した変更だけを一度通知し、JSON読込の `FileReplaced` と順序を揃える。入力サービスの `clear`／`Apply` は呼ばない。
- [ ] 結果の表示次元を入力次元と分離し、結果が読み込まれた時点の次元を保持する。ライブ切替では結果サービスを消去せず、派生 coordinator が新入力次元で既存結果を再集計しないようにする。
- [ ] `GetSaveJson()` に既存の `getDisgJson()`／`getReacJson()`／`getFsecJson()` をケースIDごとに統合した `result` を追加する。結果がある場合だけ結果次元を任意メタデータとして保存し、読込時は2／3を先に検証して結果と一緒に原子的に復元する。旧形式でメタデータがなければトップレベル `dimension` を結果次元とする。結果がないとき、結果次元メタデータだけがあるとき、壊れたメタデータ、複数結果種のケースID統合をテストする。
**Verification**: 状態遷移テストで初期値、同値、往復、異常値、カメラ失敗時の完全復元、単一通知、入力値・結果値の不変性を確認する。旧形式と新形式の結果保存・再読込を通す。

#### Step 3: メニューと読込状態を同期する
- [ ] 親項目の `CheckOnClick` を外し、2D／3D項目に選択ハンドラを結線する。チェックを個別トグルに任せず、現在次元から親テキストと子2件の排他的チェックを毎回設定する。
- [ ] 起動時の3D、メニューでの選択、2D／3Dファイルの読込完了で同じ表示更新を使う。読込失敗やキャンセルではメニューを変えない。
**Verification**: WinForms UIテストで初期3D、2D→3D、同じ項目の再クリック、2D/3D読込、失敗・キャンセルを検証し、常にチェックが1件だけになる。

#### Step 4: カメラと次元依存のGL表示を更新する
- [ ] 既存の `SceneService.changeCamera()` を変更通知の成功経路から呼び、2Dの正投影・回転禁止と3Dの透視投影・回転許可を維持する。GL所有スレッドへの公開は既存 `ThreeService` の保留更新経路を使う。
- [ ] ファイル差替えと同等に、現在の節点・部材を保ったまま拘束と荷重の次元依存レイヤを再構築する。選択・表示ケース・カメラ位置に必要な保存条件を定め、旧次元の結果図を新次元のデータとして描かない。
**Verification**: 既存の `ThreeCoordinatorTests` と追加テストで連続切替、次元別カメラ・回転、拘束・荷重レイヤ、ファイル読込後の表示、重複購読や遅延更新の不在を確認する。

#### Step 5: キャッシュ済み入力表と次元依存入口を再構成する
- [ ] 節点・部材・断面・節点拘束・部材拘束・結合の列数、見出し、バインド先を次元変更時に更新する。`AppRoutingModule` が再利用する開いた表にも適用し、編集中セルを確定または安全に取り消してから列を変える。
- [ ] 旧版の8入力画面との差分を、C#版の荷重表と部材表内の剛域表、3D専用入口について確認し、現在のC#画面構造で対応する次元別の列・表示条件を追加する。非表示フィールドの値はモデルから消さない。
**Verification**: 各表を開いたまま2D／3Dを往復して列・見出しが即時に更新され、キャッシュ再表示後も一致し、非表示値の再表示・保存・再読込が一致する。

#### Step 6: 保持した結果の表示次元を守る
- [ ] 基本結果3表と派生結果・PICKUP表が、保持済み結果の次元で列・成分・見出しを表示する。入力次元だけが変わっても元の結果と派生集計の値・出所を保つ。
- [ ] 入力次元と結果次元が異なる状態を画面で識別可能にし、次元不一致の結果図を誤って新しい入力図に重ねない。次のファイル読込や結果差替えでは新しい結果次元へ原子的に移る。
**Verification**: 2D結果を保持したまま入力を3Dへ、3D結果を保持したまま入力を2Dへ切り替え、基本・COMBINE・PICKUPの列と値、図の扱いを確認する。両方向の保存・再読込、旧形式の読込、結果なしとファイル差替えも確認する。

#### Step 7: 旧版対照と統合ゲートを通す
- [ ] 旧版の切替操作を同じ小型モデルで再確認し、入力値保持、保存次元、カメラ、荷重図、各入力・結果画面の列を対照表にまとめる。旧版で確認された開いた表・結果の同期不足は受入基準にしない。
- [ ] `dotnet test FrameWebforCS.Tests/FrameWebforCS.Tests.csproj`、`dotnet build FrameWebforCS/FrameWebforCS.csproj`、手動の2D/3D連続切替と入力・結果JSON往復確認を実施する。最終差分を確認し、無関係な変更を含めない。
**Verification**: 対象テストとビルドが成功し、3D初期表示、ライブ切替、開いた表、カメラ・GL、値保持、結果保持、読込・保存の対照表がすべて一致する。

### Risks & Considerations
- 旧版では2D解析要求を保存入力とは別に補正するが、現行C#版の計算メニューに実行処理がない。今回の切替では保存入力を破壊しない。将来の計算機能は専用の2D要求変換を別途必要とする。
- `InputDataService.dimension` の直接 setter、`SceneService` のカメラ適用、GL保留更新が別々に成功すると混在状態になる。単一変更経路で成功時だけ通知し、失敗時は表示を更新しない。
- `AppRoutingModule` のキャッシュにより、コンストラクターだけの列変更では既存画面が古いままになる。開いた表と非表示キャッシュの両方を更新し、イベント購読を重複させない。
- 結果を保持するユーザー指定を優先する。結果作成時の次元を失うと、COMBINE／PICKUPの成分と出所が新入力次元で再解釈されるため、結果次元を別管理し、次元不一致を表示する。
- 旧版は `result` を保存する一方、現行C#版は結果の読込だけ対応している。結果をセッション内と保存後の両方で保持するため、既存のケース別 `disg`／`reac`／`fsec` 形式を維持して書き戻し、結果次元の任意メタデータを追加する。旧形式はトップレベル次元へフォールバックし、未知のメタデータを受け付けない読込先がある場合は先に互換性を確認する。
- GL更新は所有スレッド上で行い、入力値・結果値・選択状態を消去しない。旧版の表と結果が古く残る挙動は再現しない。
- `.agents/docs/DESIGN.md` の UI parity 要件に従い、旧版の見た目・遷移を参照する。ただし初期次元3と結果保持は今回の明示的なユーザー指定を優先する。

### Open Questions
- なし。次元変更後の結果は保持し、結果作成時の次元で表示する。初期次元3、入力値保持、旧版相当の全体切替はユーザー指定済み。
