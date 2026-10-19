---
name: headless-runner
description: FrameWebforCS の Headless runner（FrameWebforCS.Headless.exe run、FrameWebforCS/Headless）を使う・直す・作り直すときに使う。引数（--pdf-sections の input・section_force・pickup_section_force・displacement・pickup_displacement・load_diagram・section_diagram・pickup_section_diagram、--load-diagram-*・--section-diagram-layout・--section-diagram-components、PIK・ノードの CSV）、成果物（result.json・pickup.pik・report.pdf・CSV）と応答の JSON、PDF の節を足す方法（RunOptions.cs と HeadlessCalculation.cs から PrintSelection へ。描画はデスクトップの PrintProjection・FramePrintPDF のまま）、テストと exe の作り直し（bin/Release、WebPier・関内EVピットと共用）、engineVersion の読み方、PIK の書式と符号を扱う。断面力図・荷重図が runner から出ない、数表しか出ない、引数がエラーになるときにも使う。
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

## テストと exe の作り直し

```powershell
# FEMPython のルートで。Headless の exe も bin/Release に作り直される（共用の exe を置き換える）
dotnet test FrameWebforCS/Headless/tests/FrameWebforCS.Headless.Tests.csproj -c Release
```

- テストは作り直した exe を子プロセスで動かす（`tests/bin/<構成>` の親の名前と同じ構成の exe）。WindowsBase の MSB3277 の警告は既知。
- 1回で約40秒（61件、2026-10-19）。
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
  外側（局所 y の反対側）引張が正、軸力は圧縮が負だった（隅角部 +、スパン中央 −、静力学の手計算と一致）。ほかの向きでは
  確かめていない。`result.json` の要素端力 mz は PIK と符号が逆の端がある（i端 −18.86 に対し PIK の ITAN +18.86）。

## 既知の見た目

- 荷重図：下側の部材で荷重強度のラベルとスパンの寸法が重なる。
- 断面力図：隅角部で数値と線が重なる。軸方向力図は剛域を枠で描き、その中に数値が並ぶ。頁にモデルの名前は入らない
  （ケース名だけ）。
