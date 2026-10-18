数値表示パリティの実装計画を、依存順・独立検証可能な8段階に分解しました。製品コード・テスト・計画文書は変更していません。

## 現状の重要な差異

| 表示経路 | 現行書式 |
|---|---|
| 節点座標 | F3 |
| 要素諸元 | E2 / F4 / F6。ただし未コミット変更 |
| legacy基本・派生結果 | 変位 F4、反力 F2、位置 F3、断面力 F2 |
| canonical基本結果 | invariant G10 |
| canonical派生結果 | F4 / F2 / F3 |
| FramePrintPDF | 呼出箇所ごとの F/E 指定＋現在カルチャ依存 |

特に、legacy固定桁とcanonical G10のどちらをPDFの正解とするかが未決定です。[CalculationResultTableWriter.cs](C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/calculation/CalculationResultTableWriter.cs:168) と [ResultDisgComponent.cs](C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/components/result/ResultDisgComponent.cs:149) は実際に異なる契約です。

## Implementation Plan

### Step 1: 列単位の表示契約を確定する

入力・基本結果・COMBINE・PICKUPごとに次を表形式で確定します。

- 数値／識別子／空欄の区別
- 表示倍率
- F/E/G形式と桁数
- midpoint丸め、負のゼロ、末尾ゼロ
- null、NaN、Infinity
- invariantかWindowsカルチャ追従か
- 2D/3D差

「legacy Spreadを正解にする」「canonical基本結果だけG10プロファイルを持つ」など、経路を暗黙推測しない明示的なプロファイルにします。

**Verification:** 全PDF列がちょうど1つのSpread列・表示プロファイルへ対応し、G10対固定桁の競合が残っていないこと。

### Step 2: 実Spreadをテストオラクルとして固定する

新規候補:

- `FrameWebforCS/tests/InputNumericDisplayFormattingTests.cs`
- `FrameWebforCS/tests/NumericDisplayFormattingContractTests.cs`

実STA Spreadの `Cell.Text` を検査し、以下を固定します。

- 節点F3
- 要素E2/F4/F6
- legacy結果F4/F2/F3
- canonical基本G10
- canonical派生F4/F2/F3
- 2D起動、3D起動、相互切替
- 1.005、-1.005、7.625、-0、桁境界、極小・極大値
- カルチャ変更後も意図した文字列になること
- 表示後もバインド元の生値が丸められないこと

既存の [InputElementsComponent.cs](C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/components/input/InputElementsComponent.cs:118) の書式変更は未コミットのユーザー作業なので、この段階では上書きしません。

**Verification:**

```powershell
dotnet test FrameWebforCS/tests/FrameWebforCS.Tests.csproj --filter "FullyQualifiedName~NumericDisplayFormatting"
```

### Step 3: FramePrintPDFの現状を失敗テストで特徴付ける

新規候補:

- `FramePrintPDF/PDF_Manager.Tests/NumericDisplayFormatterTests.cs`
- `FramePrintPDF/PDF_Manager.Tests/NumericTableParityTests.cs`

[printManager.cs](C:/Users/sasai/Documents/structural-mcp/FEMPython/FramePrintPDF/PDF_Manager/Printing/Comon/printManager.cs:58) の現在カルチャ依存、数値らしい識別子の再解釈、NaNとInfinityの差、標準.NET丸めをテストします。

PDFバイナリ比較より、`Table[row, column]` の完全一致を一次判定にします。

**Verification:** Spreadオラクルと異なるケースだけが意図した赤テストになり、差異が列名と値で特定できること。

### Step 4: 決定論的なPDF数値フォーマッタを導入する

変更候補:

- 新規 `FramePrintPDF/PDF_Manager/Printing/Comon/NumericDisplayFormatter.cs`
- [printManager.cs](C:/Users/sasai/Documents/structural-mcp/FEMPython/FramePrintPDF/PDF_Manager/Printing/Comon/printManager.cs:58)
- [dataManager.cs](C:/Users/sasai/Documents/structural-mcp/FEMPython/FramePrintPDF/PDF_Manager/Printing/Comon/dataManager.cs:57)

API上で数値とテキストを分離し、以下を明示します。

- fixed/scientific/general
- 桁数と丸め規則
- invariant culture
- null・非有限値・負のゼロ
- 末尾ゼロ
- 識別子は内容が数字でも再フォーマットしない

`PrintProjection` のraw数値JSONは丸めません。

**Verification:** `en-US`、`ja-JP`、カンマ小数カルチャで同じ期待文字列になり、数値識別子が変形されないこと。

### Step 5: 入力PDFの明示書式列を移行する

最初に、Spread側で書式が明示されている列を移行します。

- Geometry: `InputNode.cs`、`InputMember.cs`、`InputRigid.cs`、`InputNoticePoints.cs`
- Material: `InputElement.cs`
- Springs: `InputFixMember.cs`
- Loads: `InputLoad.cs`、`InputLoadName.cs`

特に確認が必要です。

- `InputRigid` は共通フォーマッタを迂回
- 3D要素表には値999以上で固定桁を外す分岐
- 欠落節点座標はゼロ、他の欠落入力は空欄
- 部材ばねはF3/E2/F2が混在

Spread既定表示に依存する列は、この段階で勝手に正規化しません。

**Verification:** 2D/3Dそれぞれについて、全対象列のPDF `Table` 文字列がStep 2のSpread文字列と完全一致すること。

### Step 6: 残る入力表を列ファミリー単位で移行する

対象候補:

- `InputFixNode.cs`
- `InputJoint.cs`
- `InputShell.cs`
- `InputDefine.cs`
- `InputCombine.cs`
- `InputPickup.cs`

Spread既定をそのまま互換仕様にするのか、新しく明示契約を設けるのかをStep 1の決定に従って処理します。

**Verification:** 各ファミリーを個別fixtureで検証し、他の入力帳票の `Table` 文字列が変わらないこと。

### Step 7: 基本・COMBINE・PICKUP結果を移行する

対象:

- `ResultDisg.cs` / `ResultDisgCombine.cs`
- `ResultReac.cs` / `ResultReacCombine.cs`
- `ResultFsec.cs` / `ResultFsecCombine.cs`

PICKUPクラスはCOMBINE実装を継承しているため、継承経路も同じテストで検証します。

注意点:

- 変位の表示倍率1000
- 旧 `floor(x × 10ⁿ + 0.5)` 丸め
- PICKUP断面力のJavaScript `toFixed` binary64互換
- 位置F3
- 出典ケースIDは数値ではなくテキスト

現在、COMBINE変位の出典ケース `"1"` が `"1.0000"` になり得る箇所があります。[ResultDisgCombine.cs](C:/Users/sasai/Documents/structural-mcp/FEMPython/FramePrintPDF/PDF_Manager/Printing/PrintResult/ResultDisgCombine.cs:538)

**Verification:** 基本・COMBINE・PICKUP、2D/3Dの全数値列と出典列がSpreadオラクルと完全一致すること。

### Step 8: 境界・統合回帰を通す

[PrintProjection.cs](C:/Users/sasai/Documents/structural-mcp/FEMPython/FrameWebforCS/providers/printing/PrintProjection.cs:367) はraw数値を保持し、表示責務をPDF境界に残します。

最終テストは次の3層にします。

1. 投影JSONが未丸めの数値
2. PDF内部 `Table` がSpreadと完全一致
3. 生成PDFがparseableで、表が欠落・クリップしていない

デスクトップpreview、保存、物理印刷、Headless出力は同じPDF生成経路なので、カルチャ差を含めて一括確認します。

**Verification:**

```powershell
dotnet test FrameWebforCS/tests/PrintProjection.Tests/PrintProjection.Tests.csproj
dotnet test FramePrintPDF/PDF_Manager.Tests/PDF_Manager.Tests.csproj
dotnet test FrameWebforCS/Headless/tests/FrameWebforCS.Headless.Tests.csproj
dotnet build FrameWeb.sln
```

既知の無関係な失敗は製品回帰と分離して報告します。

## Risks & Considerations

- 最大のブロッカーはlegacy固定桁とcanonical G10の競合です。
- `.NET`、Spread、JavaScriptでは負のmidpoint丸めが一致しない可能性があります。
- 現在のPDF数値解析はカルチャ依存です。
- numeric-looking IDを数値化してはいけません。
- 入力nullと結果欠落時のゼロ補完は別契約です。
- `InputElementsComponent.cs` の未コミット変更を実装者が上書きしないよう、所有確認が必要です。
- PDFバイナリgoldenだけでは文字列差を診断しにくいため、`Table` exact-stringテストを主判定にします。

## Open Questions

実装前に必須なのは次の1点です。

- 基本結果PDFの正解は、legacy SpreadのF4/F2/F3ですか、それともcanonical基本結果画面のG10ですか。両方なら、どの入力・画面状態がどの表示プロファイルを選ぶかを明示する必要があります。

独立レビューの判定は `NEEDS_DECISION` です。また、read-only環境のため `.agents/docs/plans/` への計画ファイル作成と計画ゲート実行はできませんでした。製品コードは未変更、テストも未実行です。
