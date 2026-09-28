## Implementation Plan: Load Sheet Fixed Rows

### Purpose

`FrameWebforCS/components/input/InputLoadComponent.cs` の `fpSpread1_Sheet2`（荷重強度）を、Sheet1と同様に常時 `MaxNodeId = 100,000` 行にする。`LoadId` 未割当の空行を表示し、`\` と行ヘッダー選択＋`Del` は表示行の内容を移動して表現する。シート・バインドリストの実際の行数は変えず、空行の位置も保存・再読込で復元する。

### Scope

- Modified files: `FrameWebforCS/components/input/InputLoadService.cs`（固定表示行、未割当状態、行操作、疎な配置情報）、`FrameWebforCS/components/input/InputLoadComponent.cs`（キー、選択、色・再バインド）、`FrameWebforCS/providers/InputDataService.cs`（配置の事前検証・一括適用・保存）。
- Modified tests: `FrameWebforCS.Tests/UnifiedLoadIntensitySheetTests.cs`、`InputLoadColumnWidthTests.cs`、`ThreeLoadsServiceTests.cs`。必要な回帰対象は同ディレクトリの `InputViewportSelectionTests.cs`、`DimensionSwitchTests.cs`、`InputDocumentViewportReplacementTests.cs`。
- New files: `FrameWebforCS.Tests/InputLoadFixedRowsTests.cs`（固定件数・境界・実キー操作）、`InputLoadLayoutPersistenceTests.cs`（配置・文書交換・計算入力の統合）。既存テストへ自然に集約できる場合は新設を省略する。
- Dependencies: 新規パッケージなし。FarPoint Spread 15.4、`BindingList`、`DocumentReplacementNotifications`、既存 `DeleteKeyInterceptor` を再利用する。`FrameWebforCS/components/myFpSpread.cs` の変更は既存フックでは受入条件を満たせない場合に限定する。
- Out of scope: Sheet1の仕様変更、Python・Angular・Legacy converter、結果画面、解析ケース数制限、無関係のビルド修復。先行計画 `.agents/docs/plans/unified-load-intensity-sheet.md` の全Case表示・Case移動・荷重値の契約は維持し、可変行数・自動Case開始行・表示行操作だけを本計画で置き換える。

承認済みの要件:

- 挿入位置はActiveCellの行。異なるLoadIdを含む、表示上の後続行すべてを下へ移す。挿入位置はLoadIdも空の未割当行になる。
- 最終表示行に割当やデータがある場合は挿入を拒否する。荷重の切捨て・上書きはしない。
- 行削除は選択行を取り除いた内容で後続表示行を上へ詰め、末尾を未割当の空行にする。
- 未割当の空行位置も保存する。既存のCase内行番号と荷重値を空行の代用にしない。

実装に向けた具体案（計画承認に含める）:

- 表示位置（0始まりのindex）と荷重識別 `(CaseId, row)` を分離する。Caseごとの疎な `row` は表示indexへ置換しない。
- 未割当行へのLoadId入力でCaseを割り当てる。他列から先に入力した場合は現在選択Caseを使用する。Case内番号は最大番号＋1を優先し、上限の場合は最小の未使用正整数を使用する。空いている番号がなければ編集を拒否する。未入力行への自動割当はしない。
- LoadId変更時は従来どおり節点・部材荷重を一緒にCase間移動し、移動先の同じCase内番号へ挿入する。Case内再採番は表示の参照先を更新するが、固定表示位置を勝手にCase順へ並べ替えない。
- 通常セルのDelは既存の値消去を維持する。LoadIdの空値・不正値は現在の無効ID拒否を維持し、荷重を孤立させない。行全体を消す操作は行ヘッダーDelで行う。
- `\` は現在と同様、Sheet2・修飾キーなし・非編集中に限る。編集中はエディターへ渡し、予期しない確定や行移動をしない。

現状の根拠:

- `InputLoadService.cs:171-174` はSheet1の10万行確保とSheet2の1行確保。`:81-94` は空の開始行にもCase1を返す。`:660-693` はSheet2の可変リストを毎回作り直す。
- 同ファイル `:365-417` の挿入・削除はCase内番号を対象とし、今回の表示全体のシフトとは異なる。`:426-491` のCase間移動・再採番を固定配置と整合させる必要がある。
- `InputLoadComponent.cs:97-149` にキー・行ヘッダー判定、`:164-201` にCaseと3D選択、`:349-359` に全表示行の色更新がある。未割当行の選択抑止と更新範囲の制御が必要。
- `InputLoadService.cs:235-248,697-737` の `input_rows` はCase内の空行を保存するが、未割当の全体空行位置は表せない。`InputDataService.cs:232-245,309-319,364-386` に文書読込・適用・保存の境界があり、`:125-141` の計算スナップショットは別経路になっている。

### Implementation Steps

固定行と配置の契約を最初に定義し、サービス、文書保存、キー操作、表示の順で実装・検証する。

#### Step 1: 固定行・配置契約の回帰を先に定義する

- [x] 複数行Delは選択行を一括削除して詰める仕様で確定（ユーザー回答: 2026-09-28）。
- [x] 表示行を未割当／Case内荷重行の割当として定義し、100,000件の同じ `BindingList` を一度だけ確保する。初期化完了以降、再読込やclearを含め件数を変えない。
- [x] 保存形式を入力ルートの省略可能な `load_intensity_layout` と定義する。形式は `{"version":1,"rows":[{"slot":1,"case_id":"2","row":7}]}`。slotは1始まりの表示行番号。全割当行を一度ずつ保存し、欠けたslotは未割当空行として復元する。末尾の10万空行は列挙しない。Case割当済みの値なし行は従来の `input_rows` と組み合わせる。
- [x] metadataがない旧ファイルはCaseの既存順序・Case内row順で詰めて配置し、余りを未割当にする。名称だけのCaseには自動的な強度開始行を作らない。
- [x] 配置・荷重を一体の候補として検証する。全割当数の100,000超過、未対応version、不正型、範囲外、重複slot／荷重識別、存在しない参照、割当行の欠落を拒否する。Case内番号の上限と表示容量は別々に検証する。
- [x] 固定件数、空LoadId、旧ファイル、混在Caseの配置、100,000件ちょうど／100,001件拒否を先にテストする。

**Verification**: 失敗テストが旧実装の可変件数・Case1開始行を検出する。新候補の不正入力は現在の荷重・配置・選択・revisionを変更しない。これは解析ケース数の新たな上限制限にはしない。

#### Step 2: 固定表示行と編集をサービスへ実装する

- [x] `IntensityRows.Clear/Insert/RemoveAt`、リスト交換、Spreadの行追加削除を使わず、固定slotの割当・行オブジェクトを差し替える。初期確保後のCount変更を禁止する。
- [x] 未割当行のLoadIdと全値を空にする。LoadId／値の編集時だけ識別を割り当て、無効値や上限での拒否は値・割当・通知・選択を不変にする。
- [x] 匿名空行の挿入だけでCaseや `input_rows` を作らない。割当済みの値を消した場合は既存のCase内空行保存規則を維持する。
- [x] 名称変更・SelectCase・clear・ファイル交換で固定配置を勝手に詰め直さない。Case削除で無効になる割当はそのslotを空にし、他のslotは保持する。
- [x] 通常セル更新は対象行のみ通知する。bulk更新は既存通知抑止を利用し、完了した整合状態を一度公開する。Case内識別から表示indexを検索する経路を用意する。

**Verification**: ListChangedの全通知時と各操作の前後でCount=100,000、リスト参照の同一性を確認する。LoadId先入力／値先入力、疎な番号、数値0、無効ID、エディターCancel、Case選択・名称変更で空行の自動割当や配置消失がない。

#### Step 3: 保存・文書交換を固定配置へ接続する

- [x] `InputDataService.JsonDataOpen` の事前解析で荷重と配置を両方検証し、文書全体の適用前に容量・参照整合を確定する。既存の後段エラーによる部分交換防止を維持する。
- [x] `InputLoadService` の直接setLoadJson／clearと文書経由の適用を同じ候補検証へそろえる。後続の別セクション不正でも旧状態を保つ。
- [x] 保存JSONへ疎な配置を追加する。空プロジェクトでは不要なCase・荷重・10万件metadataを出さない。配置metadataを `CaptureCalculationSnapshotJson` へ含めず、既存の計算・表示用荷重値を変えない。
- [x] Case間移動時の再採番と各slotの参照を一括更新する。無効移動やCase内上限では候補ごと拒否する。

**Verification**: 2Caseの順序混在、複数の未割当gap、割当済み空行、節点＋部材同居、最後の表示slotについて保存→再読込で値・識別・位置が一致する。不正metadataと後続不正セクションで文書交換が原子的に拒否され、配置情報は保存に存在しても計算スナップショットに存在しない。

#### Step 4: 表示indexを対象に挿入・削除する

- [x] `\` はActiveRowIndex以上の全割当を末尾側から一つ下へコピーし、ActiveRowIndexを未割当にする。最終slotが割当済み（値なしでも同様）なら変更前に拒否する。
- [x] 行ヘッダーDelは削除対象の完全な行情報を取り除き、全後続表示slotを一つ上へコピーして末尾を空にする。割当済み行の削除は既存Case内削除・再採番も一度行い、その参照を更新する。
- [x] 複数行対応を選ぶ場合、重複を除いた選択index集合を一括処理する。生存slotを元の相対順のまま上へ詰め、末尾を削除数だけ空にする。異なるCaseの荷重も各一度だけ削除・Case内再採番する。1行限定を選ぶ場合、複数行選択はイベントを消費するだけで値も変更しない。
- [x] `DeleteKeyInterceptor` を再利用し、行ヘッダー由来の選択だけを処理する。全列を覆う通常セル範囲、Sheet1、他画面のDelを区別する。既存保護セル処理を維持し、二重処理を防ぐ。
- [x] 受理後のActiveCellは挿入／最初の削除位置に保つ。拒否時は元のセル・選択・3D状態を保つ。編集中・修飾キー付き・Oem5/Oem102を検証する。

**Verification**: 先頭・途中・内部gap・最終行で、異なるLoadIdをまたいだ全列移動、節点／部材同居、空行操作、尾行クリアを確認する。末尾割当ありの拒否は保存JSONも同一。実WinFormsキー経路で行数が瞬間的にも変わらず、通常セルDel／他シートDelとの混同や二重削除がない。

#### Step 5: 選択・列幅・色と回帰を仕上げる

- [x] 未割当行ではSelectCase／3D荷重選択を発行しない。割当行は `(CaseId,row)` で選択し、シフト後も正しい表示indexを逆引きする。
- [x] 2D/3Dの列再構成、Case移動、ファイル交換、フォーム再作成後も固定件数を維持する。破棄済みフォームへのBeginInvoke、購読重複、古い選択の再適用を防ぐ。
- [x] `DataAutoSizeColumns=false` と既存日本語コメントを維持する。色更新は割当・変更範囲を対象にし、空行へ変わった行の残色を除去する。通常セル更新ごとの10万行走査・色設定を避ける。
- [x] 既存テストの可変件数・Case開始行・Reset必須という旧前提を更新し、荷重値・列幅・保護・Case移動の有効な検証を残す。
- [x] 次のチェックと実画面の受入を実施する。

```powershell
dotnet test FrameWebforCS.Tests/FrameWebforCS.Tests.csproj --filter 'FullyQualifiedName~InputLoadFixedRowsTests|FullyQualifiedName~InputLoadLayoutPersistenceTests|FullyQualifiedName~UnifiedLoadIntensitySheetTests|FullyQualifiedName~InputLoadColumnWidthTests|FullyQualifiedName~ThreeLoadsServiceTests|FullyQualifiedName~InputViewportSelectionTests|FullyQualifiedName~DimensionSwitchTests|FullyQualifiedName~InputDocumentViewportReplacementTests' -clp:ErrorsOnly
dotnet build FrameWeb.sln -clp:ErrorsOnly
git diff --check
```

**Verification**: 実STAフォームのSpreadエディターとキー入力で固定件数、幅、保護、保存復元、Case2以降の選択を確認する。空表・混在Case表・上限付近で初期表示／行シフト／再読込の時間とメモリを記録し、1セル編集が10万行更新へ拡大しないことを確認する。既存ビルド障害が残る場合は `.tmp/InputLoadFixedRows/` に実製品参照と対象テストソースをリンクした独立runnerを作り、同じ対象テストを実行する。内部型のテストアクセスを維持するためAssemblyNameを `FrameWebforCS.Tests` にし、singletonテストの `DisplacementSingletons` collection定義・必要な共通helperもリンクする。元のテスト削除・弱体化で通さず、製品buildと対象runnerの結果を別に報告する。

### Risks & Considerations

- 10万行確保と全slotシフトのUI負荷: 通常編集は局所更新、行操作は必要な後続範囲を一括更新する。list通知だけでなくSpreadの色・再バインドの仕事量も観測する。
- 表示indexをCase内rowへ流用すると解析値・3D選択を壊す。識別・表示位置の変換を一箇所に集約し、混在Caseと疎なrowで検証する。
- 末尾の値なし割当行も保存対象なので、未割当空行とは分けて容量を判定する。旧ファイルのCase合計が表示容量を超える場合は明示エラーで全体を拒否し、切捨てない。
- 荷重配列・ `input_rows` ・配置metadataは同じ候補で整合させる。配置だけ先に適用して失敗時に荷重と食い違う状態を作らない。
- 読込互換はmetadataの省略で維持する。配置情報を計算スナップショットやダミー荷重へ混入させない。全未割当の末尾部分は固定容量から復元し、ファイルを膨らませない。
- 既存未コミットの `InputLoadComponent.cs:238` のコメント、および作業中に増えた `components/menu/SidebarComponent.cs` の変更は他作業として保持する。
- 2026-09-28の事前確認で、対象test実行は既存 `LoadDisplayConversionTests.cs` の12件のCS0103により開始できなかった。本計画ではその修復を追加せず、実装時に独立runnerで対象検証を成立させる。
- 計画のチェック: plan-doc形状・workspace存在・読み取り専用Codex妥当性レビューを実行し、`.agents/check.ps1 -AgentOnly` を通す。実装開始にはこの計画へのユーザー承認が必要。
- 計画作成時のチェック結果: plan-doc・workspace存在は合格。インフラの関連テスト・契約検証は合格したが、別作業の `SidebarComponent.cs:23` の末尾空白により `git diff --check` と全体結果は失敗。対象差分は保持し、計画自身の空白チェックは合格。記録: `.agents/logs/check-20260928T022955291Z-24936.log`。

### Open Questions

- なし。計画は2026-09-28にユーザー承認済み。複数行Delは選択index集合を一括削除し、全Caseをまたぐ生存行を元の相対順で詰める。固定行数・末尾保護・全LoadIdをまたぐシフト・空行位置保存もユーザー回答で確定。
