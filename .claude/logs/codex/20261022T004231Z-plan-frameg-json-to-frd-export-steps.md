## Implementation Steps (ordered by dependency)

1. **LZH エンコーダを独立して実装する。** `-lh0-` / `-lh5-`、level 0 ヘッダー、CRC-16/ARC、アーカイブ終端を扱い、複数ブロックも含めて確立します。JSON 変換とは切り離します。

2. **固定長レコードの書き出し基盤と高リスクのレコード形式を固める。** Shift-JIS のバイト幅、数値書式、桁あふれを扱う共通処理を用意し、特に `Buzai_G.tmp` の位置対応、`Kajyu.tmp` の Int16、`K_JituBuzai.tmp` のレコード終端と負の m2 表現を、小さな単位で実装します。既存の読み込み側は [ConvertManager.cs](/C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameGConverter/Convert_Manager/ConvertManager.cs) と各リーダーを使います。

3. **同梱テンプレートとアーカイブ内ファイル一覧を確定する。** 複数の 4.6.3 サンプルを比較し、モデルに依存しないファイルだけを版管理されたテンプレートとして同梱します。`$11.txt` の 4 行目のタイトルは Shift-JIS のバイト長を確認して置換します。メッシュキャッシュなど、モデル依存の可能性が残るファイルはサンプルから無条件にコピーせず、生成・省略の可否を調べます。

4. **JSON の正規化、事前検査、診断契約を定める。** 保存形式を読み、参照切れ、非有限値、件数・項目幅、表現できない構造・荷重を出力前に検査します。エラーがあれば FRD を出力しない共通の診断型を定義し、判断基準を `.claude/docs/DESIGN.md` に記録します。

5. **節点・部材・断面・支持・剛域などのレコードを変換する。** `Buzai.tmp`、`Buzai_G.tmp`、ばね・継手・剛域・着目点に関するファイルを正規化モデルから生成します。既存の剛域レコードを使い、節点や部材の分割は行いません。

6. **荷重・ケース・組合せ・ピックアップを変換する。** `Kajyu.tmp`、`K_JituBuzai.tmp`、DEFINE/COMBINE、PICKUP、合成荷重のファイルを生成します。範囲指定荷重は対象部材へ展開し、負の m2 やケース終端を個別に検証します。

7. **アーカイブ組み立てと Convert_Manager の公開 API を追加する。** テンプレートと生成ファイルを既知の並び順でまとめ、LZH エンコーダで FRD 化します。API はバイト列と診断を返し、エラー時には部分成果物を返しません。

8. **Headless に `export-frd` を追加する。** [RunOptions.cs](/C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/Headless/RunOptions.cs) とプロトコル応答を拡張し、API を呼び出して出力ファイルを発行します。MCP アダプターは対象に含めません。

9. **Convert_Test に逆変換ボタンを追加する。** [Form1.cs](/C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameGConverter/Convert_Test/Form1.cs) から公開 API を呼び、診断を画面表示して FRD を保存できるようにします。

10. **往復変換と解析結果の統合確認を行う。** FRD→JSON→FRD→JSON と、FrameWeb 保存 JSON→FRD→JSON を正規化後に比較します。表現可能な入力については、FrameWeb エンジンの結果も比較します。

## Verification per Step

1. `SevenZipExtractor` でデコードし、ヘッダー、CRC、内容、終端を確認します。小さいファイル、圧縮ファイル、複数ブロック境界を含めます。
2. 各レコードを対応する既存リーダーで再読込し、値とバイト数を照合します。Shift-JIS の境界・桁あふれ、`ja-JP` / `en-US` の数値書式も確認します。
3. サンプルごとのエントリ一覧・内容差分を確認し、テンプレート部分と動的部分の分類をテストで固定します。
4. 参照切れ、不正値、3D・シェル・強制変位・移動荷重などの非対応データ、ID・固定長上限を診断テストで確認します。
5. 生成レコードを既存リーダーに通し、節点・部材・断面・支持・剛域・着目点が正規化後に一致することを確認します。
6. 生成荷重を既存リーダーに通し、符号、方向、作用範囲、ケース終端、組合せ、ピックアップを確認します。
7. Convert_Manager のテストで FRD をデコードし、全ファイルの CRC と往復後 JSON を確認します。
8. Headless のプロセステストで引数、診断エンベロープ、成果物一覧、エラー時の部分出力なしを確認します。[RunnerTests.cs](/C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/Headless/tests/RunnerTests.cs) は現在ビルド済み exe の場所を前提にするため、共有 `bin/Release` を上書きしないテスト出力先を用意します。
9. Convert_Test で保存済み JSON を選び、出力 FRD と画面の診断を確認します。
10. `FrameGConverter/Convert_Manager.Tests`、`FrameWebforCS/Headless/tests` の対象テストを実行し、正規化した往復 JSON と FrameWeb エンジン結果を照合します。FrameG 自体は自動テストに使いません。

## Risks and Mitigations

- **未読ファイルの役割が分からない。** 4.6.3 サンプル同士を比較し、固定テンプレートとモデル依存データを分けます。役割が不明なファイルをサンプルから安易にコピーしません。
- **固定長・文字コード・数値書式で意味が変わる。** 文字数ではなく Shift-JIS のバイト数で処理し、固定幅の切捨てを避けます。必要な値が幅に収まらない場合はエラーにします。
- **FrameG が表現できない入力を黙って落とす危険がある。** 参照不正、上限超過、解析結果に影響する非対応データはエラーにします。画面表示などの非工学情報の省略や ID の振り直しは警告にします。API・Headless・WinForms は同じ診断コードと対象パスを使います。
- **フロントエンドと importer の読込結果が食い違う。** `joint.cs` の片端だけピン留めされた行を落とす判定を、変更範囲を限定して修正し、専用テストを追加します。[joint.cs](/C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameGConverter/Convert_Manager/FrameWebforJS/joint.cs)
- **モデル ID と FrameG の番号制限が合わない。** 節点・部材 ID は元の数値順に連続した 1 始まりへ振り直し、参照先もすべて更新します。位置対応の `Buzai_G.tmp` は同じ部材順を使います。番号上限を超える件数はエラーにします。
- **荷重符号の逆変換を誤る。** 変換表を一箇所に集約します。y は mark 1/2 と P の符号反転、x は mark 5/6、gy/gx は mark 5/6 と V/H 指定で符号を保ち、z は mark 11 と符号反転、温度は mark 9 とします。
- **杭の区間ばねを個別に表せない。** FrameG の共有区間表に合わせ、`杭` 部材間で区間長とばね種別が一致する場合だけ出力します。不一致や対象外部材の区間ばねはエラーにし、杭先端ばねは重複計上しない形で節点支持へ対応付けます。
- **共有材料・ばね値が一意に表せない。** 共有材料は部材単位に複製し、`n=1` と、既に反映済みの A / Iz を出力します。同じ部材の材料値がシート間で異なる場合はエラーにします。固定と区別できない剛性値 1 のばね指定もエラーにします。
- **実際の FrameG での受入れは自動確認できない。** 同梱サンプルと既存 importer を自動検証に使い、出力の FrameG 起動確認はユーザーが後で行う工程として残します。

## Open Questions

- 実装着手前に回答が必要な未決事項はありません。自動検証後の実アプリ確認は、FrameG が利用できないためユーザー側の確認工程として扱います。

読み取り専用で調査し、ファイル変更やテスト実行はしていません。既存の未追跡プロンプトログもそのままです。
