## Implementation Plan: Unified Load Intensity Sheet

### Purpose

FrameWebforCS の「荷重強度」を1枚のシートに全荷重 Case 分表示し、各行の左端に実際の Case 番号を示す。番号の編集による Case 間移動、`\` キーでの空行挿入、行ヘッダー選択中の `Del` キーによる行削除、および空行を含む保存・再読込を実現する。

### Scope

- New files: `FrameWebforCS.Tests/UnifiedLoadIntensitySheetTests.cs`（サービス、保存、UI 操作の集中テスト。既存テストへ追加する方が自然なら分割してよい）。
- Modified files: `FrameWebforCS/components/input/InputLoadService.cs`、`FrameWebforCS/components/input/InputLoadComponent.cs`、`FrameWebforCS/components/myFpSpread.cs`（荷重シート専用の Del 迂回フック）、必要に応じて `FrameWebforCS/three/ThreeService.cs`、`FrameWebforCS/three/ThreeLoadsService.cs`、既存の `FrameWebforCS.Tests/ThreeLoadsServiceTests.cs`、`InputViewportSelectionTests.cs`、`DimensionSwitchTests.cs`、`InputDocumentViewportReplacementTests.cs`。
- Dependencies: 新規パッケージなし。FarPoint Spread の BindingList/キーイベント、既存 `load` JSON、`InputLoadService` の Case 管理を再利用する。変更対象は C# 入力 UI とその統合経路。Python FEM、Angular UI、結果画面は対象外。
- Data contract: `load_node`/`load_member` の値と Case 別 `row` を維持する。空行の永続化には、荷重値のないダミー節点荷重を作らず、Case ごとに省略可能な `input_rows: number[]` を追加する。配列は1～100,000の重複しない昇順行番号で、全ての荷重行を含む。旧ファイルでは両荷重配列の `row` の和集合から復元し、疎な番号もそのまま保持する。計算向けの荷重値へ `input_rows` を混入させないことを最初に確認する。
- UI contract: Case セレクターは3D表示の対象 Case と新規行の既定 Case の選択に残すが、シート内容は絞り込まない。`荷重強度` のシート名を固定し、全 Case を入力順に、各 Case 内は `row` 順に連続表示する。空プロジェクトでも Case 1 の入力開始行を表示する。`Del` はセル選択中なら従来どおり値を消し、荷重強度シートで行ヘッダーから行全体を選択したときだけ行を削除する。

### Implementation Steps

行の保存契約を先に固め、一覧表示、編集、キー操作、3D連携の順で進める。

#### Step 1: 行と保存形式の契約を確定する

- [ ] `CaseId + row` を一意の入力行として定義し、同じ行の `load_node` と `load_member` を一体で扱う。旧ファイルの疎な番号は読込時に詰めず、挿入は指定位置以降を +1、削除は対象より後を -1 する。
- [ ] `clsLoad` に入力行の番号リストを持たせる。`DataHelperModule` は `List<int>` を自動変換しないため、`InputLoadService.ReadLoad` と `WriteLoad` で `input_rows` を明示的に検証・読書きする。省略時は節点/部材荷重行の和集合で補う。`HasData` と `LoadNames_ListChanged` の Case 生存判定にも空行を含める。
- [ ] `input_rows` を追加した保存 JSON の読込・保存・計算入力経路を小さな実データで検証する。計算側が未知のメタデータを受け付けない場合は、既存の計算入力投影で除外する。旧 JSON と既存の荷重値の保存形は変えない。
- [ ] 不正な Case 番号、重複・範囲外の `row`、`input_rows` と荷重配列の不整合は、既存の読込トランザクションを壊さず明示的に扱う。

**Verification**: 旧ファイルと新形式の読込→保存→再読込で、Case 順、疎な行番号、空行のみの Case、名称を削除して空行だけ残った Case、同一行の節点/部材荷重、荷重値が一致する。計算向けデータに空行由来の荷重が生じない。

#### Step 2: 全 Case を表示する行モデルへ切り替える

- [ ] 選択 Case の固定10万行を差し替える方式を、全 Case の実在行と入力開始用行だけを載せる `IntensityRows` に変更する。表示行番号から `CaseId + row` を取得できるようにし、`LoadId` はその行の Case を返す。
- [ ] ファイル交換・名称編集・Case 追加/削除・2D/3D 切替時に一覧を再構成し、BindingList と破棄済み Spread への通知経路を維持する。Case セレクターの変更は一覧を絞り込まず、3D対象を切り替える。
- [ ] 一覧表示と同時に、行クリック時は表示インデックスを `CaseId + row` へ変換し、選択 Case と Case 内行番号を3Dへ渡す。Case 2以降の行を選んでも Case 1 の同番号行へ渡さない。
- [ ] 行一覧の更新は変更した Case と表示に必要な行数に比例させ、Case 数×10万行の確保を避ける。

**Verification**: 複数 Case で同じ `row` を持つ荷重と空行が同じ Sheet2 に同時表示され、左端の値が各行の Case 番号となる。Case 2の行クリックでは Case 2 の荷重が選択される。セレクター変更でも他 Case の行は消えず、ファイル交換後に旧データが残らない。

#### Step 3: セル編集と Case 間移動を行単位で扱う

- [ ] 左端列のロックを解除し、他列の編集先を行の `CaseId + row` で決める。既存の名称行との参照を維持し、新しい Case 番号への移動時には必要な Case と名称行の結合を作る。
- [ ] 左端の番号を変えたとき、同じ行の節点荷重・部材荷重・空行状態をまとめて移す。移動元の後続行を詰め、移動先 Case の元の `row` 番号の位置へ挿入し、その位置以降の行を繰り下げる。移動先の最大行がそれより前なら、疎な番号を保ったまま元の番号に置く。衝突による既存荷重の置換を禁止し、範囲上限や無効な番号では状態を一切変えない。
- [ ] 移動前後で `CasesChanged`、`LoadsEdited`、選択 Case、3D表示の通知を一貫した順序で発行する。移動元に名称や別の荷重が残る場合は Case を保持し、完全に空なら既存の有効性規則に従う。

**Verification**: 節点/部材が同居する行の Case 間移動、新規 Case への移動、移動先に同じ番号の行がある場合の繰下げ、移動先の最大行が元番号より小さい場合、最後の行の移動、無効番号・10万行境界で、荷重の欠落・上書き・二重登録がない。保存再読込後も同じ Case と行番号になる。

#### Step 4: 空行挿入と行削除のキー操作を実装する

- [ ] `myFpSpread` の共通 `Del` セル消去より前に、荷重強度シートだけが行ヘッダー由来の全行選択を判定できるフックを設ける。Spread の選択モデルで全行選択とセル選択を区別し、行選択のときだけセル消去を迂回する。他のシートでの `Del` 動作は変えない。
- [ ] `\` キーで現在行の位置に空行を挿入する。`Del` キーは荷重強度シートの行ヘッダーで1行を選択したときだけ行を削除し、セルを選択したときは従来どおり値を消す。複数行選択時は誤削除を避けるため行削除もセル消去も実行せず、1行選択を求める。編集中の確定、空プロジェクトの入力開始行、Case 境界の位置を明確にして扱う。
- [ ] 挿入・削除時はその Case の後続 `row` を両荷重配列と `input_rows` で繰り下げ/詰め、他 Case の番号を変えない。操作後の選択位置と通知を更新する。

**Verification**: 実際の WinForms キー入力で、セル選択中の `Del` は値だけを消し、行ヘッダーで1行選択中の `Del` は行だけを1回削除する。複数行選択ではデータが変わらず、`\` で挿入した空行は保存後も復元する。日本語キーボード配列でも挿入キーを確認し、荷重名称など他のシートの `Del` は従来どおりセル消去となる。

#### Step 5: 行操作後の選択と3D表示を同期する

- [ ] 既存の `SelectGridRow` メソッドが Case+row を受けて正しい表示インデックスを逆引きできるようにする。荷重の3Dクリック選択は現行で無効なので、新規の逆方向クリック機能は追加しない。
- [ ] 行移動・挿入・削除・ファイル交換後の選択を有効な行へ寄せ、Case セレクターと3D対象 Case が選択行と一致するようにする。2D/3D の列切替でも表示行の識別を維持する。

**Verification**: Case 2以降の行クリックで正しい荷重だけが3Dで選択される。`SelectGridRow` メソッド、2D/3D切替、行操作、データ再読込後にも別 Case の同番号行へ飛ばない。

#### Step 6: 回帰検証と画面受入

- [ ] 集中テストと既存の荷重/表示/ファイル交換テストを実行し、荷重値・Case 順序・空行・行移動・入力イベントの整合を確認する。
- [ ] 複数 Case を含む実ファイルで、1枚のシート、左端番号、編集・移動、`\` / `Del`、保存再読込、3D選択を手動確認する。

**Verification**: `dotnet test FrameWebforCS.Tests/FrameWebforCS.Tests.csproj` と `dotnet build FrameWebforCS/FrameWebforCS.csproj` が成功し、手動操作で仕様どおりに動く。失敗が既存の別変更由来なら対象を切り分けて記録する。

### Risks & Considerations

- 空行を `load_node`/`load_member` のダミーレコードで表すと計算や他クライアントが荷重として誤処理し得る。任意の入力行メタデータを使い、計算向け投影での非混入を検証する。
- 現行 `UpdateNestedRow` は同じ `row` を置換する。Case 移動と行番号再採番を、検証後に一括適用する操作として実装する。
- `myFpSpread` が `Del` を先に消費するため、荷重シート側の後付け `KeyDown` だけでは既存セルを先に消す恐れがある。行ヘッダーの全行選択だけを先に分岐し、通常のセル選択は既存処理に渡す。
- 既存の3D連携はグリッド表示行番号を Case 内 `row` と見なす。全 Case 表示後に変換漏れがあると別行を選択する。
- 空行を大量に保存した場合の表示・保存時間、100,000 行上限、Case 番号1～100,000を境界テストに含める。
- `.agents/docs/DESIGN.md` と既存の別計画にユーザーの未コミット変更がある。これらを編集しない。

### Open Questions

- なし。Case 間移動はユーザー指定どおり、元の `row` 番号の位置へ挿入する。`Del` はセル選択で値の消去、行ヘッダーの単一行選択で行削除とする。

