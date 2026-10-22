## Validation Result: NEEDS_REVISION

ステップの大枠は依存順に並んでいます。ただし、入力の取りこぼし、ID再採番時の荷重参照、Headless の既存プロトコルとの不整合があり、現状のままでは実装に進めません。[計画書](</C:/Users/sasai/Documents/structural-mcp/FEMPython/.claude/docs/plans/frameg-json-to-frd-export.md:80>)

## Missing Coverage

- **`bridge_loads` の扱いがありません。** FrameWebforCS の保存 JSON には `bridge_loads` が含まれ、計算要求への変換でケース別の空間荷重になります。[InputDataService.cs](</C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/providers/InputDataService.cs:373>) [CalculationRequest.cs](</C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/calculation/CalculationRequest.cs:33>)  
  `bridge_loads` や旧形式の空間荷重データが有効な場合に、診断付きエラーで出力を止めるか、対応するかを計画に加えてください。空の項目を許容する条件も明記が必要です。

- **メンバーIDを再採番したときの荷重範囲参照が曖昧です。** Step 5 はメンバーIDを変換し、Step 6 は `m1/m2` を「そのまま保つ」としています。`m1/m2` はメンバー参照なので、旧IDから新IDへ変換し、負の `m2` の符号表現も維持する必要があります。欠番のあるIDと範囲荷重を同時に使う往復テストを明記してください。[計画書](</C:/Users/sasai/Documents/structural-mcp/FEMPython/.claude/docs/plans/frameg-json-to-frd-export.md:102>)

- **FrameWebforCS の往復テストがサンプルを参照できる手順がありません。** 新しいテストは `FrameGConverter/Convert_Test/TestData` の `.frd` を開きますが、現在の [FrameWebforCS.Tests.csproj](</C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/tests/FrameWebforCS.Tests.csproj:1>) はそのサンプルを出力先へコピーしていません。テスト用リンク設定をスコープに追加するか、別の確実な配置方法を記してください。

- **CRC検証の方法を具体化してください。** `ConvertManager` はアーカイブを展開して変換しますが、CRC値をテスト側へ返しません。[ConvertManager.cs](</C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameGConverter/Convert_Manager/ConvertManager.cs:42>) 変換結果の比較とは別に、アーカイブの各エントリーについてCRCを直接検証する手順を置いてください。

## Ordering Problems

- `gy/gx` の符号は Step 6 の前に決める必要がある、とOpen Questionsにあります。Forward converterだけではFrameG本体での荷重符号を確定できず、計画自身もリスクとして挙げています。符号を確定するか、確認までは `gy/gx` を未対応エラーにするかを計画上の決定にしてください。
- `fix_member.length` は Step 4 でエラー扱い、Step 8 でそのエラーをテストする一方、Open Questionsでは未決事項です。提案どおり対象外エラーにするなら、Open Questionsから外して確定仕様として記してください。
- 再採番マップを作る Step 5 を、Step 6 の荷重シリアライズが参照する順序自体は妥当です。上記のとおり、`m1/m2` の変換規則を明確にしてください。

## Integration Risks

- Headless の既存プロトコルは `warnings[]` ではなく `messages[]` を持ち、`RunnerError` にJSONパスや診断一覧のフィールドはありません。また現行契約では成功時の `engineeringStatus` は `not_checked`、失敗時は `not_applicable` です。[Program.cs](</C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/Headless/Program.cs:7>) [Headless README](</C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/Headless/README.md:115>)  
  診断を既存フィールドへどう対応付けるか、また成功時の `not_applicable` を採るならプロトコル変更として何を更新するかを決めてください。RunnerTestsでは警告、失敗診断、ステータス、失敗時に成果物がないことも検証対象に含める必要があります。

- `lh5.py` がリポジトリ外にあるため、独立デコーダによる照合を将来再実行できません。独立した照合手順を再現可能にするか、検証限界として明記してください。FrameGの手動確認も、現在のチェック項目ではメンバー・荷重・支持・計算が中心です。材料マークや材料シート、ばね・剛域・結合・PICKUPなど、出力対象に含める主要機能を確認項目へ加えると、外部互換性のリスクを適切に評価できます。

## Revised Steps (if NEEDS_REVISION)

実装前に計画へ次を反映してください。

1. 保存JSONの項目を棚卸しし、空間荷重を含む未対応の解析入力をエラーにする条件を追加する。
2. `gy/gx` と `fix_member.length` の扱いを確定し、診断コードと検証ケースへ反映する。
3. 再採番マップを全参照へ適用する規則を明記し、欠番IDと範囲荷重、負の `m2` を組み合わせた検証を追加する。
4. 往復テスト用サンプルの配置、アーカイブCRCの直接検証、Headlessプロトコルへの診断マッピングをスコープとテストに加える。
5. Step 11 のFrameG確認項目を、実装するレコード群に対応する形へ広げる。

レビューは読み取りのみで行い、ファイル変更やテスト実行はしていません。
