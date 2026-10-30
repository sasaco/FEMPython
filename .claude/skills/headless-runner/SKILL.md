---
name: headless-runner
description: FrameWebforCS の Headless runner（FrameWebforCS.Headless.exe run・export-frd、FrameWebforCS/Headless）を使う・直す・作り直すときに使う。引数（--pdf-sections の input・section_force・pickup_section_force・displacement・pickup_displacement・load_diagram・section_diagram・pickup_section_diagram・bridge_definitions・bridge_loads・bridge_audit、--load-diagram-*・--section-diagram-layout・--section-diagram-components、PIK・ノードの CSV）、成果物（result.json・pickup.pik・report.pdf・CSV）と応答の JSON、PDF の節を足す方法（RunOptions.cs と HeadlessCalculation.cs から PrintSelection へ。描画はデスクトップの PrintProjection・FramePrintPDF のまま）、テストと exe の作り直し（bin/Release、WebPier・関内EVピットと共用）、engineVersion の読み方、PIK の書式と符号を扱う。3D の橋面荷重（bridge_loads）の書き方・監査・張出しの扱い、保存形式の JSON を FrameG の .frd に書き出す export-frd（FRD_EXPORT_UNSUPPORTED と messages の FRD_* コード）、断面力図・荷重図が runner から出ない、数表しか出ない、引数がエラーになるときにも使う。
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
| `bridge_definitions` | 橋面荷重のパネル・経路（3D だけ） | BridgeDefinitions |
| `bridge_loads` | 橋面荷重のあるケースごとの入力強度と方向（3D だけ） | BridgeLoads |
| `bridge_audit` | 解析時の荷重分配確認：入力と節点の合力・原点回りモーメント、保存誤差、荷重ごとの長さ・面積、等価節点荷重（3D だけ） | BridgeAudit |

- `--section-diagram-components` は `mz`・`fy`・`fx`・`disg` の部分集合（既定は全部）。PICKUP の図では `disg` を飛ばすので、
  PICKUP だけの要求で `disg` だけなら引数エラー。`--section-diagram-layout` は `single`・`split_horizontal`（上下2段）・`split_vertical`。
- 印刷の割り付け（PrintSelection.Layout）は1つしかない。`load_diagram` と断面力図を1回で頼むなら割り付けを同じにする。
- 断面力図は2次元だけ（3次元は `SECTION_DIAGRAM_REQUIRES_2D`。3次元の図はデスクトップの画面の撮影で、runner からは出せない）。
- 頁数：上下2段で成分4つなら1ケース2頁、3つでも2頁（2頁目は1つ）。
- デスクトップの「COMBINE 断面力図」（CombinedSectionDiagram）は runner に出していない。
- 橋面荷重の3つの表（2026-10-30 に足した）：2D は `BRIDGE_LOADS_REQUIRE_3D`。`bridge_loads`・`bridge_audit` は橋面荷重のある
  ケースが、`bridge_definitions` だけならパネルか経路が要り、無ければ `BRIDGE_LOADS_REQUIRED`（どちらも計算の前に落とす）。
  橋面荷重図（BridgeLoadDiagram）はデスクトップが 3D 画面を撮影して作るので runner には出していない。
- `bridge_audit` は `PrintSnapshot` の `resultInputRevision` が入力の版と同じでないと「入力が変更されています」で落ちる。
  runner は入力そのものを解くので `PdfAsync` で 0 を渡している（足すときに一度これで落ちた）。
- 合成桁格子（90 節点・12 ケース）で `input,bridge_definitions,bridge_loads,bridge_audit` は 36 頁・12 秒。監査の単位は
  結果に単位が無いと「単位未指定」と出る。

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

## 保存形式の JSON を自分で書くとき（2026-10-27、円形立坑のリング梁 52 節点・14 ケースで本物の runner を実行して確認、engineVersion 1.0.0+7b83fa1）

- `define`（行 n の `C1` = 荷重ケース番号）→ `combine`（`C<n>` = define n の係数、`name`）→ `pickup`。**`pickup` の `C1`・`C2`・…は対象にする COMBINE の番号を並べる**もので、範囲ではない。`{"C1": 1, "C2": 6}` と書くと COMBINE 1 と 6 の 2 つだけの PICKUP になり、最大・最小の組合せが違ってくる（6 つの組合せなら `C1`〜`C6` を書く。`result.json` の `derived.pickups[].sectionForces.mz_max[].sourceCaseId` で確かめられる）。
- 荷重ケースの `fix_node`・`element` は集合の番号。`fix_node` の値は 0 が自由、1 が拘束、それ以外は絶対値が支持ばねの剛性（x・y を別の行の値で書く）。ケースごとに支持条件を変えるには集合を分ける。
- 梁の一様温度変化は `load_member` の `mark` "9"、`m1` "1"・`m2` "-52"（負の `m2` は m1 から |m2| までの範囲）、`P1` が温度変化。
- 断面力を 1 m 幅当たりで欲しいときは、モデルの断面積・断面二次モーメント・ばね・荷重をそろえて縮める（5.0 m 幅なら 1/5）。結果が比例するので、原本のように 1 m 当たりの表と図にできる。
- 断面力図の PDF：節点が多い（52）リングは、A4 の `split_horizontal` と `mz,fy,disg` では「Diagram labels cannot fit without overlap」で失敗する。A4 の `single` は成功（14 ケース × 3 成分 + PICKUP で 48 頁）、**A3 の `split_horizontal`・`mz,fy,disg` は 31 頁**（14 ケース × 2 頁 + PICKUP 3 頁、頁の用紙は A3 縦）。図は縦横の縮尺をそろえず、円のリングが横長の楕円に描かれる。
- `pickup.pik` は各部材の ITAN と JTAN の両方の行を出す。ITAN だけを照査する帳票に渡すときは、JTAN の行（着目点の列が `JTAN`）を除く。
- 閉合した円環で部材を時計回りに i→j と並べると、局所 y が外向きになり、曲げモーメントは**外側引張が正**（内向きの側圧）。上の「内側引張が正」は局所 y を内向きにした場合の話で、向き次第で変わる。

## 開削ボックスの閉合骨組で確かめたこと（2026-10-30、engineVersion 1.0.0+da2d03d、矩形の1層1径間・9 ケース。numpy の独立した骨組と照査点で 0.01 kN·m まで一致）

- **曲げモーメントの符号の規則**：文書の座標（y 下向き）で部材の局所 x（i→j）を +90° 回した向きが局所 y で、M は**局所 y の側が引張のとき正**。上床版（左→右）は下側＝内側引張が正、下床版（左→右）は下側＝外側引張が正、左側壁（上→下）は左＝外側引張が正、右側壁（上→下）は左＝内側引張が正。4 つの向きとも確かめた。文書の y を負にして（画面が y 上向きの図と同じ向きに）書き、図で見て時計回りに並べると全部材で内側引張が正。文書の座標にそのまま時計回りに書くと画面では反時計回りで、外側引張が正になる（上の円環の例はこの書き方と考えられる。推定）。
- `load_member` の `direction` は `gx`・`gy`（全体座標）も使える。`gy` は文書の y の向き（下向き）が正。`L1` は i 端からの距離、`L2` は j 端からの距離（負なら荷重幅）で、部材の一部にかける台形荷重（`mark` 2、`P1`・`P2`）が書ける。`L1`＋`L2` が部材長以上の行は黙って捨てられる。
- `load_node`：`n`・`tx`・`ty`（文書の座標、ty は下向きが正）・`rz`。
- 分布ばね `fix_member`：`{"<集合>": [{"m": "<部材>", "tx": 軸方向, "ty": 直角, "tz": null, "tr": null}]}`（kN/m/m、負の値は絶対値にされる）。荷重ケースの `fix_member` に集合の番号を書くと、**ケースごとにばねを替えられる**（例：永久作用は長期、変動作用は短期、地震は全周のばね）。`fix_node` の集合は無くてもよい（空の集合 1 が補われる）。
- 分布ばねは剛域を含む部材の全長に付く。着目点（`notice_points`）を 0.125 m ごとに置いて部材を細かく分けると、0.025 m の梁要素に節点ばねを付けた独立の骨組と 0.01 kN·m まで合った。
- 4 部材・着目点 約 150・9 ケース・組合せ 9・PICKUP 7 で、`--pdf-sections section_force,pickup_section_force` を含めて 1 回 15 秒。

## 3D の橋面荷重（bridge_loads）を書くとき（2026-10-30、合成桁の設計例の格子 90 節点・12 ケースで本物の runner を実行して確認）

記録・モデルを作るスクリプト・比べた値は `FrameWebforCS/tests/data/合成桁格子検証/README.md`（`scripts/gen.mjs` が書き方の見本）。

- 形：`"bridge_loads": {"version": 1, "panels": [...], "paths": [...], "cases": {"<荷重ケース>": [...]}}`。
  - パネル：`{"id", "name", "nodes": [構造の節点], "triangles": [[節点, 節点, 節点], ...]}`。三角形は荷重を配るためだけで剛性を足さない。節点はどれかの部材に使われていること。
  - 経路：`{"id", "name", "points": [[x, y, z], ...]}`。パネルの面の上に置く。
  - 荷重：`{"id", "name", "panel_id", "path_ids": [1本で線荷重、2本で面荷重], "end_intensities": [[始点, 終点], ...（経路ごと）], "direction": {"mode": "global", "vector": [0, 0, 1]}}`。強度の符号は `direction` の向きが正なので、鉛直下向きの荷重は `vector` を上向きにして負の強度で書いた。
  - `load` の荷重ケースは別に要る（`element`・`fix_node`・`joint` の集合を指す）。`cases` のキーがそのケースの番号。
- 計算：3D なので `--generate-pik false`（既定だと `PIK_REQUIRES_2D`）。荷重図 `load_diagram` も 2D だけ。
- 監査：`result.json` の各ケースの `diagnostics.spatial_loads`（`resultant`・`moment`・`nodal_resultant`・誤差・荷重ごとの積分）。`resultant.z` が手計算の合計と合うかで、荷重が全部配られたかを確かめられる。PDF にするなら `bridge_audit`。
- パネルの外へは載せられない（外挿は拒否）。床版の張出しの荷重は、床版の端に節点を置いて片持ちの部材で主桁に剛結し、張出しの荷重を隣の主桁へ回す伝達の部材（相手側の回転を解放）を足して、てこの原理で配った（README の「張出しの荷重をてこの原理で配る方法」）。
- 速さ（2026-10-30 に3回直した後）：三角形 1,320 個のパネルで、面荷重のあるケースは FemModel を直接動かして 1 ケース 3.1〜3.6 秒、橋面荷重の無いケースは 1.6 秒。12 ケースが runner で 40 秒（最初は 10 分の上限を超えた）。runner はケースごとにパネルの準備をやり直す（README の「見つかった問題」1）。
