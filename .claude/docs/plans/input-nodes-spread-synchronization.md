## Implementation Plan: Input Nodes Spread Synchronization

### Purpose

`InputNodesService` を節点座標の唯一の正本とし、既存の `_node` を `InputNodesComponent.fpSpread1_Sheet1` へ表示するとともに、Spread で確定した座標編集を直ちにサービスへ反映する。節点 ID は行ヘッダーに表示し、列 0/1/2 を X/Y/Z に対応させる。

### Scope

- New files: 実装ファイルの新規追加はなし
- Modified files:
  - `FrameWebforCS/components/input/InputNodesService.cs`
  - `FrameWebforCS/components/input/InputNodesComponent.cs`
- Unchanged files:
  - `FrameWebforCS/components/input/InputNodesComponent.Designer.cs`（生成コード）
  - `FrameWebforCS/providers/InputDataService.cs`（既存の読込・保存経路を利用）
  - `FrameWebforCS/components/menu/MenuComponent.cs`（既存の読込・保存経路を利用）
- Dependencies:
  - GrapeCity Spread WinForms 15.4.0 の `FpSpread.Change` と `ChangeEventArgs.Row/Column/View`
  - `THREE.Vector3`（X/Y/Z は `float` の公開可変フィールド）
  - 現行の `InputDataService.dimension` による 2D/3D 列構成
- Out of scope:
  - JSON スキーマ、Angular クライアント、他の入力画面の変更
  - 空行からの新規節点作成、節点 ID の編集・削除
  - 対象外の未コミット `InputMembersService.cs` の修正

### Implementation Steps

#### Step 1: サービス境界と変更通知を追加する

- [ ] `InputNodesService.cs` 内に、ID と X/Y/Z の値だけを保持する読み取り用スナップショット型を定義する。`THREE.Vector3` の参照を外部へ返さず、通知を経由しない変更を防ぐ。
- [ ] `GetNodesSnapshot()` を追加し、`_node` の各要素を値コピーとして返す。表示順は UI 側で決められるよう、サービスの辞書列挙順を UI 契約にしない。
- [ ] X/Y/Z を表す軸型と `TryUpdateCoordinate(nodeId, axis, value, out error)` 相当の API を追加する。未知 ID、未知軸、`NaN`、正負の `Infinity` を拒否し、成功時は既存ベクトルを直接書き換えず、新しい `THREE.Vector3` へ原子的に置換する。
- [ ] リセットと単一節点更新を区別できる `NodesChanged` 通知を追加する。値が同じ場合は通知せず、`clear()` と `setNodeJson()` は状態確定後に一度だけリセット通知を発行する。
- [ ] `setNodeJson()` で `node` が存在しない場合に既存状態を維持する現在の未コミット挙動は変えず、通知も発行しない。

**Verification**: サンプル JSON の読込後にスナップショットが 238 件であること、任意 ID のコピーを書き換えても `_node` に影響しないこと、有限値の単一座標更新だけが保存 JSON と通知へ反映されること、無効値・未知 ID・同値更新では状態と通知回数が変わらないことをデバッガーまたは小さな検証ハーネスで確認する。

#### Step 2: サービス状態を Spread に投影する

- [ ] `InputNodesComponent` に `InputNodesService.Instance`、行インデックスから節点 ID を引く `List<string>`、表示中の直前値、再入防止フラグを保持する。
- [ ] シート列の初期化後にサービスのスナップショットを読み、行数を実在する節点数へ合わせる。ID は行ヘッダー、列 0/1/2 は X/Y/Z とし、欠番を詰めた行番号から ID を逆算しない。
- [ ] 行は、数値として解釈できる ID を数値順、それ以外を序数文字列順に並べ、同値時も序数比較で決定する。疎な ID を含むサンプルでも行ヘッダーとデータの対応を保つ。
- [ ] 3D では X/Y/Z の3列、2D では既存どおり X/Y の2列を表示し、非表示の Z はサービス内の値を維持する。
- [ ] サービスのリセット通知ではスナップショットから全行を再構築し、単一節点更新通知では対応行だけを更新する。プログラムからセルへ値を設定する間は再入防止フラグを立てる。

**Verification**: 画面生成前にサンプルを読み込んだ場合と、画面表示中に再読込した場合の両方で、238 行、実 ID の行ヘッダー、列 0/1/2 の X/Y/Z が JSON と一致することを確認する。ID 417 が行番号 238 に誤変換されず、行ヘッダー 417 として表示されることも確認する。

#### Step 3: Spread 編集をサービスへ即時反映する

- [ ] `fpSpread1.Change` を購読し、対象ビュー、行範囲、列範囲を検証する。列 0/1/2 を UI 側で X/Y/Z 軸へ変換し、行 ID は保持した行マップから取得する。
- [ ] 編集値を現在カルチャで `float` に変換し、有限値だけを `TryUpdateCoordinate` へ渡す。2D では表示中の X/Y のみを更新し、Z を読み直したり 0 へ置換したりしない。
- [ ] UI 起点の更新中に同期的な `NodesChanged` が返ってきても全表を再構築しないようガードし、選択・編集位置と入力値を保持する。
- [ ] 空欄、非数値、範囲外、`NaN`、`Infinity`、未知 ID が入力された場合はサービスを変更せず、該当セルを直前の正本値へ戻して入力エラーを通知する。
- [ ] 有効な編集後は `InputDataService.GetSaveJson()` の既存経路が更新済みサービス値を返すことを確認し、保存処理側には新しい同期ロジックを追加しない。

**Verification**: X/Y/Z をそれぞれ変更してセル確定直後にサービスのスナップショットと保存 JSON が同じ値になることを確認する。無効入力はセルが元値へ戻り、保存 JSON と他座標が不変であること、1回の編集でサービス更新通知が1回だけ発生することを確認する。

#### Step 4: イベントのライフサイクルと UI スレッド境界を固定する

- [ ] コンストラクター内で Spread とサービスのイベントを一度だけ購読し、コンポーネントの `Disposed` 処理から両方を解除する。生成済み Designer の `Dispose` は編集しない。
- [ ] サービス通知を受けたら `IsDisposed`、`IsHandleCreated`、`InvokeRequired` を確認し、必要な場合だけ UI スレッドへマーシャリングする。ハンドル未生成中の通知は保留し、`OnHandleCreated` 後に最新スナップショットで再同期する。
- [ ] `AppRoutingModule` によるコンポーネント再利用、破棄後の再生成、メニューからのファイル再読込で多重購読や破棄済みコントロールへのコールバックが起きないようにする。

**Verification**: 節点画面を複数回開閉・再表示し、各サービス変更につき UI 更新が1回であることを確認する。コンポーネント破棄後にサービスを更新しても例外や残存コールバックがなく、再生成後は最新値が1回だけ表示されることを確認する。

#### Step 5: 回帰確認と受入確認を行う

- [ ] サンプル JSON を開き、節点画面の表示、座標編集、JSON 保存、保存ファイルの再読込を一連で確認する。
- [ ] 3D と 2D の双方で列構成と Z 値保持を確認し、未編集データの保存形式（小文字 `x/y/z`）が変わっていないことを比較する。
- [ ] `dotnet build FrameWebforCS/FrameWebforCS.csproj --no-restore` を実行する。対象外の未コミット `InputMembersService.cs` による既存16エラーと、今回変更で増えたエラーを分離して報告する。
- [ ] 現行 `FrameWebforCS` には専用テストプロジェクトがないため、上記の手動シナリオ結果を記録する。自動テスト基盤の新設は別タスクとし、この同期変更へ混在させない。

**Verification**: 有効編集が保存・再読込後も保持され、無効編集は保存されず、ロード→表示→編集→保存の往復で ID と X/Y/Z の対応が崩れないこと。今回変更した2ファイル由来の新規コンパイルエラーが0件であること。

### Risks & Considerations

- サンプルの節点 ID は 1～417 に238件だけ存在するため、`rowIndex + 1` を ID とすると誤更新する。常に明示的な行→ID マップを使う。
- `THREE.Vector3` は公開可変フィールドを持つため、内部参照を返すと通知を迂回できる。スナップショットは必ず値コピーにする。
- サービス通知と Spread の変更イベントが相互に発火すると再帰更新や選択位置消失が起きる。UI適用中とUI送信中を別フラグで管理する。
- `FpSpread.Change` はユーザー変更確定後のイベントであり、編集中の文字ごとに発火する `EditChange` は使わない。
- 数値の文字列表現は実行カルチャに依存する。現在カルチャで解釈し、変換後に有限値検証を必ず行う。
- シングルトンサービスとキャッシュされる UserControl の組合せでは、購読解除漏れがメモリ保持と多重通知につながる。
- 全行再構築はファイル読込・clear などのリセット時だけに限定し、通常の単一セル編集では該当節点だけを更新する。
- 現在の作業ツリーにはユーザー所有の変更があるため、実装時は `InputNodesService.cs` の差分を再確認し、対象外変更を上書きしない。
- 現在のベースラインビルドは対象外の `InputMembersService.cs` にある既存16エラーで失敗する。完了判定ではこの既知障害を隠さず、今回差分由来の診断と分ける。

### Open Questions

- ID の既定表示順は「数値 ID を数値順、非数値 ID を序数順」とする。JSON 記載順を必須にしたい場合のみ、実装前にこの規則を変更する。
- 無効入力時の既定 UX は「直前値へ戻してエラーを1回通知」とする。非モーダル表示など別の通知方式が必要なら実装時に差し替えるが、サービスを変更しない契約は維持する。
- 現行専用の自動テストプロジェクト新設は本変更の範囲外とする。継続的な UI 回帰試験が必要になった場合は、STA 対応の別計画として扱う。

