---
name: headless-runner
description: FrameWebforCS の Headless runner（FrameWebforCS.Headless.exe run・export-frd、FrameWebforCS/Headless）を使う・直す・作り直すときに使う。引数（--pdf-sections の input・section_force・pickup_section_force・displacement・pickup_displacement・load_diagram・section_diagram・pickup_section_diagram、--load-diagram-*・--section-diagram-layout・--section-diagram-components、PIK・ノードの CSV）、成果物（result.json・pickup.pik・report.pdf・CSV）と応答の JSON、PDF の節を足す方法（RunOptions.cs と HeadlessCalculation.cs から PrintSelection へ。描画はデスクトップの PrintProjection・FramePrintPDF のまま）、テストと exe の作り直し（bin/Release、WebPier・関内EVピットと共用）、engineVersion の読み方、PIK の書式と符号を扱う。保存形式の JSON を FrameG の .frd に書き出す export-frd（FRD_EXPORT_UNSUPPORTED と messages の FRD_* コード）、断面力図・荷重図が runner から出ない、数表しか出ない、引数がエラーになるときにも使う。
---

# FrameWebforCS Headless runner

`FrameWebforCS/Headless/` の `FrameWebforCS.Headless.exe run` は、保存形式の FrameWeb JSON を受け取り、計算して
PIK・PDF・CSV を返す。仕様の正は `FrameWebforCS/Headless/README.md`。ここには README から読み取りにくい事実を置く。

## 使っているところ

- WebPier（`structural-mcp/WebPier/webpier/pipeline/fempython.py`）：荷重図（load_diagram）と骨組解析（PIK・変位と反力の CSV）。
- 関内EVピット（`K:\レールウェイコンサルタント\26_関内\EVピット部\src\pipeline\fempython.py`）：荷重図、断面力図
  （section_diagram・pickup_section_diagram）と PIK・result.json。
- 両方とも exe の場所を環境変数か `.env` の `STRUCTURAL_MCP_FEMPYTHON_RUNNER` に持ち、
  `FrameWebforCS/Headless/bin/Release/net10.0-windows/FrameWebforCS.Headless.exe` を指す。**exe は共用**なので、
  作り直す前にユーザーに確認する。

## PDF の節

| 節 | 中身 | PrintOption |
|---|---|---|
| `input`・`displacement`・`pickup_displacement` | 入力・変位の数表 | Input・Displacement・PickupDisplacement |
| `section_force`・`pickup_section_force` | 断面力の**数表**（MEMBER FORCES、PICK UP の MOMENT MAX. など）。図ではない | SectionForce・PickupSectionForce |
| `load_diagram` | 荷重図。これだけなら計算しない | LoadDiagram |
| `section_diagram` | 基本ケースの断面力図（曲げモーメント図・せん断力図・軸方向力図・変位図） | SectionDiagram |
| `pickup_section_diagram` | PICKUP ごとの最大・最小の包絡の断面力図（変位図は無い） | PickupSectionDiagram |

- `--section-diagram-components` は `mz`・`fy`・`fx`・`disg` の部分集合（既定は全部）。PICKUP の図では `disg` を飛ばすので、
  PICKUP だけの要求で `disg` だけなら引数エラー。`--section-diagram-layout` は `single`・`split_horizontal`（上下2段）・`split_vertical`。
- 印刷の割り付け（PrintSelection.Layout）は1つしかない。`load_diagram` と断面力図を1回で頼むなら割り付けを同じにする。
- 断面力図は2次元だけ（3次元は `SECTION_DIAGRAM_REQUIRES_2D`。3次元の図はデスクトップの画面の撮影で、runner からは出せない）。
- 頁数：上下2段で成分4つなら1ケース2頁、3つでも2頁（2頁目は1つ）。
- デスクトップの「COMBINE 断面力図」（CombinedSectionDiagram）は runner に出していない。

## 節を足すとき

描画はデスクトップと共通の `FrameWebforCS/providers/printing/PrintProjection.cs`（`AddVectorDiagrams` が図の成分と割り付けを
`diagramResult` に書く）と `FramePrintPDF/PDF_Manager/Printing/PrintDiagram/DiagramResult.cs`（`CanonicalSectionDiagram`）にある。
runner 側は次の3か所だけで足りる。

1. `RunOptions.cs`：節の名前（`SupportedSections`）、引数（`allowed`）、検証（節が無いのに引数がある、値が選択肢に無い、重複）。
2. `HeadlessCalculation.cs`：`PdfAsync` の節→PrintOption の対応と PrintSelection の項目（Layout・DiagramComponents など）、
   PICKUP が要る節なら `needsSectionForcePickup` に足す、次元の検査。
3. `tests/RunnerTests.cs` と `README.md`。

## FrameG への書き出し（export-frd）

- `export-frd --input <abs> --output-dir <abs> [--title <題名>]`。計算も Python も使わず、
  `FrameGConverter/Convert_Manager` の `FrdExporter` で `model.frd`（kind `frd`）を1つ出す。題名の既定は入力のファイル名。
- FrameG で表せない入力は `FRD_EXPORT_UNSUPPORTED` で失敗し、何も置かない。個々の理由は `messages[]` に
  `level`（error/warning）・`FRD_*` のコード・JSON の `path` で入る。表せる項目・符号・番号の振り直しの決まりは
  `.claude/docs/DESIGN.md`（2026-10-22 の行）と `FrameGConverter/README.md`。
- 出した .frd を FrameG で開いて確かめたことはまだ無い（2026-10-22 時点。確かめる項目は DESIGN.md の TODO）。

## テストと exe の作り直し

```powershell
# FEMPython のルートで。Headless の exe も bin/Release に作り直される（共用の exe を置き換える）
dotnet test FrameWebforCS/Headless/tests/FrameWebforCS.Headless.Tests.csproj -c Release
```

- テストは作り直した exe を子プロセスで動かす（`tests/bin/<構成>` の親の名前と同じ構成の exe）。WindowsBase の MSB3277 の警告は既知。
  共用の Release の exe を置き換えずに試すなら `-c Debug` で流す（`bin/Debug` の exe を使う）。
- 1回で約40秒（64件、2026-10-22）。
- `engineVersion`（例 `1.0.0+93d4ecc…`）は**ビルドしたときの HEAD**で、未コミットの変更は版に出ない。版で中身を区別したいなら
  コミットしてから作り直す。exe のソースがどのブランチかは engineVersion では分からないので、引数を exe で実際に試す
  （存在しない節名を渡すと `PDF sections must be distinct supported section names.`）。exe 内の引数名は
  `FrameWebforCS.Headless.dll` の UTF-16 文字列で探せる。

## 成果物

- 応答：標準出力に JSON 1つ（`protocolVersion` 1、`engine` "fempython"、`ok`、`executionStatus`、`artifacts` の
  `kind`・`relativePath`・`bytes`・`sha256`）。計算する要求は必ず `result.json` を返す。
- 同じ入力なら `result.json`・`pickup.pik` はバイト単位で同じ（PDF の節を変えても同じ）。
- `result.json`：`analysisResultSet.results[].member_section_forces[].segments[].i_end/j_end`（fx・fy・mz の要素端力）、
  `derived.defines/combines/pickups`。
- `pickup.pik`：1行目 `PickUpNo,着目力,部材No,最大CaseNo,最小CaseNo,着目点,着目点距離,最大Md,最大Vd,最大Nd,最小Md,最小Vd,最小Nd`、
  以下は固定幅（5桁ずつの PickUpNo・着目力 M/S/N・部材・最大と最小の COMBINE 番号、着目点 ITAN/番号/JTAN、距離、断面力 10桁ずつ）。
  着目点には `notice_points` と剛域端が入る。
- 符号（PIK・数表・図）：局所 y を全部材で内向きにした閉合骨組（関内EVピット、内向きの等分布荷重）で、曲げモーメントは
  **内側（局所 y の側）引張が正・外側引張が負**、軸力は圧縮が負（2026-10-21 に確かめ直した：計算点B の全周 36.1 kN/m で
  隅角部 −21.90・スパン中央 +7.45 = −21.90 + 36.1×2.55²/8、抽出の PIK も隅角部が負。同じ隅角部の両側の部材の M は同じ符号）。
  前に「外側引張が正」と書いたのは誤り（テストが |M| しか見ていなかった）。ほかの向きでは確かめていない。
  `result.json` の `segments` の i_end/j_end は区間の両端の断面力で、隣の区間とつながる（PIK と同じ符号）。

## 既知の見た目

- 荷重図：下側の部材で荷重強度のラベルとスパンの寸法が重なる。
- 断面力図：隅角部で数値と線が重なる。軸方向力図は剛域を枠で描き、その中に数値が並ぶ。頁にモデルの名前は入らない
  （ケース名だけ）。
