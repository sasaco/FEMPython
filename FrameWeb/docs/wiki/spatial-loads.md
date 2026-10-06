# 線荷重・面荷重

[Wikiホーム](index.md) · [Python API](python-api.md) · [結果の読み方](results.md) · [実務チェック](design-practice.md)

`static`で、指定した位置の線荷重・面荷重を構造節点へ配分して解析できます。
通常の節点荷重・部材荷重・シェル面圧と同じ荷重ケースに加算されます。
荷重別と総和の合力・全体原点回りモーメントを監査し、一回の静解析で結果を求めます。
設計利用時は、荷重領域の面積、作用方向、支持反力との釣合い、構造・載荷メッシュの感度を
[設計実務での利用手順](design-practice.md)と併せて確認してください。

## 座標・符号・単位

- `plane`省略時は全体XYに平行な平面です。パネルと経路のZ座標を一致させます。
  傾斜平面には`LocalPlane`（JSONでは`plane`）を明示します。構造節点と経路の座標は常に全体座標です。
- `direction`省略時は正の強度が全体+Z、負が全体-Z方向です。節点順を逆にしても方向は変わりません。
  全体方向ベクトルは正規化され、その長さを荷重倍率には使いません。
  `normal`方向の正負は要素・載荷三角形の節点順に従い、全セルの反転で荷重方向も反転します。
- 線荷重は力/長さ、面荷重は力/長さ²です。N・m系ではN/m、N/m²を使います。
  座標・剛性・通常荷重も同じ単位系にそろえます。自動換算はありません。
- 旧`line.position`のZ省略値は0です。強度や座標は入力時に丸めません。

局所基底の原点・二軸は積分用の平面を定めます。二軸は非零で直交する必要があり、各軸の長さは正規化します。
平面外の点を黙って投影しません。線強度は実長、面強度は実面積あたりの値です。XYへの投影長・投影面積ではありません。

一本の経路では、始終点強度を折れ線の累積長に沿って補間します。
二本の経路では正規化弧長で位置を対応させ、経路間にも強度を補間します。
対応する端点間の距離二乗和を比較して二本目の向きをそろえ、許容値内の同点は対応が曖昧として拒否します。
経路を逆向きに記述するときは、その経路の始終点強度も入れ替えます。

## パネルの指定

既存シェルを使う場合は`elements`で要素IDを指定します。
T3は重心座標、Q4は双一次形状関数を使います。平面内で歪んだQ4も求積の収束を確認して扱います。

シェルを持たない梁モデルでは`triangles`に既存構造節点の三角形接続を指定します。
載荷三角形は補間専用で、材料・板厚・剛性を追加しません。
梁は三角形からの節点荷重を受けます。梁要素への分布荷重と同じ精度になるとは限らないため、
荷重配分と変位・端力のメッシュ依存性を確認してください。

載荷領域の節点を構造節点と分ける場合は、`loading_nodes`にIDと全体座標、`loading_triangles`に
その接続を指定します。このメッシュは載荷領域だけを定め、構造自由度や剛性を追加しません。
各積分点を既存シェルまたは構造節点の`triangles`へ探索し、形状関数により総力と一次モーメントを
保存して構造自由度へ写像します。載荷メッシュが構造側の補間領域から出る場合は拒否します。

`elements`と`triangles`の非空指定は排他です。旧JSONで両方を省略すると、パネル節点に含まれる
既存シェルから接続を復元します。シェルのない節点集合から三角形を自動生成しません。
旧`elements`は`shell`名前空間のID、新形式は内部要素IDです。梁IDと旧シェルIDが同じでも区別します。

外周は指定したT3/Q4接続から復元するため、非凸でも凸包へ置換しません。穴は接続に実在する境界を
`holes`へ節点ID順で明示します。穴の順序は外周と逆向きでなければならず、面荷重積分から穴内部を除外します。
線荷重は穴を横断できませんが、決定規則により穴の境界上へ一度だけ載荷できます。

## 旧JSONの完全な例

長さ2の片持梁二本の先端間に、強度-3の線荷重を載せる数値確認モデルです。
合計荷重は-6、各先端への荷重は-3、曲げ剛性は1000なので先端変位は-0.008になります。

<!-- fixture: spatial-cantilevers.json -->
```json
{
  "analysis_type": "static",
  "node": {
    "1": {"x": 0, "y": 0, "z": 0}, "2": {"x": 2, "y": 0, "z": 0},
    "3": {"x": 2, "y": 2, "z": 0}, "4": {"x": 0, "y": 2, "z": 0}
  },
  "member": {"1": {"ni": 1, "nj": 2, "e": 1}, "2": {"ni": 4, "nj": 3, "e": 1}},
  "element": {"1": {"1": {"E": 1000, "A": 1, "Iy": 1, "Iz": 1, "J": 1}}},
  "fix_node": {"1": [
    {"n": 1, "tx": 1, "ty": 1, "tz": 1, "rx": 1, "ry": 1, "rz": 1},
    {"n": 4, "tx": 1, "ty": 1, "tz": 1, "rx": 1, "ry": 1, "rz": 1}
  ]},
  "inf_panel": {"7": {"nodes": [1, 2, 3, 4], "triangles": [[1, 2, 3], [1, 3, 4]]}},
  "line": {"1": {"position": [{"x": 2, "y": 0}, {"x": 2, "y": 2}]}},
  "load": {"1": {"inf_panel": 7, "load_inf": [{"L1": 1, "P11": -3, "P12": -3}]}}
}
```

<!-- run: spatial-file-roundtrip -->
```python
from math import isclose
from fem import FemModel

model = FemModel()
model.load_model("spatial-cantilevers.json")
result = model.run()
assert isclose(result["node_displacements"][2]["dz"], -0.008, abs_tol=1e-12)
assert isclose(result["reaction_forces"][1]["fz"], 3, abs_tol=1e-12)
assert isclose(result["reaction_forces"][1]["my"], -6, abs_tol=1e-12)
assert isclose(result["spatial_load_contribution"]["resultant"][2], -6, abs_tol=1e-12)
model.save_model("spatial-saved.json")
restored = FemModel()
restored.load_model("spatial-saved.json")
assert restored.run()["node_displacements"] == result["node_displacements"]
assert model.run()["spatial_load_contribution"] == result["spatial_load_contribution"]
model.save_results("spatial-results.json")
```

`L1/P11/P12`のみで線荷重、`L2/P21/P22`も指定すると面荷重です。
一つのケース内の`load_inf`をすべて加算します。モデル読込では選択ケース（省略時は先頭一件）を読みます。
HTTPの`AnalysisResultSet`では全ケースを独立して解析します。未選択ケースの荷重は加算しません。
選択ケースの`load_inf`と新形式`spatial_loads`の併記は拒否します。

## Python APIと面荷重

定義型は不変です。変更には新しい定義を`set_spatial_loads()`へ渡します。
次の例はPythonだけで同じ二本の梁を作り、パネル全体に強度-3の面荷重を載せます。

<!-- run: spatial-python-area -->
```python
from math import isclose
from fem import FemModel, BarParameter
from fem.spatial_loads import SpatialLoadDefinitions, SpatialLoadPanel, SpatialLoadPath, SpatialLoad

model = FemModel()
for node, (x, y) in enumerate(((0, 0), (2, 0), (2, 2), (0, 2)), 1):
    model.add_node(node, x, y, 0)
model.add_material(1, "Example", E=1000, nu=0.3)
model.material.add_bar_parameter(1, BarParameter(area=1, Iy=1, Iz=1, J=1))
model.add_element(1, "bar", [1, 2], 1, section_id=1, shear_correction=False)
model.add_element(2, "bar", [4, 3], 1, section_id=1, shear_correction=False)
for node in (1, 4):
    model.add_restraint(node, True, True, True, True, True, True)
model.set_spatial_loads(SpatialLoadDefinitions(
    panels=(SpatialLoadPanel(7, (1, 2, 3, 4), triangles=((1, 2, 3), (1, 3, 4))),),
    paths=(SpatialLoadPath(1, ((0, 0, 0), (2, 0, 0))),
           SpatialLoadPath(2, ((0, 2, 0), (2, 2, 0)))),
    loads=(SpatialLoad(1, 7, (1, 2), ((-3, -3), (-3, -3))),),
))
result = model.run("static")
audit = result["spatial_load_contribution"]
assert isclose(audit["resultant"][2], -12, abs_tol=1e-12)
assert isclose(audit["moment"][1], 12, abs_tol=1e-12)
assert isclose(sum(r["fz"] for r in result["reaction_forces"].values()), 12, abs_tol=1e-12)
assert model.run()["spatial_load_contribution"] == audit
```

傾斜平面で面法線方向を使う例です。構造節点順を反転すると面法線も反転します。

<!-- run: spatial-inclined-normal -->
```python
from math import isclose
from fem import FemModel, BarParameter
from fem.spatial_loads import (
    LoadDirection, LocalPlane, SpatialLoad, SpatialLoadDefinitions,
    SpatialLoadPanel, SpatialLoadPath,
)

model = FemModel()
for node, xyz in enumerate(((0, 0, 0), (2, 0, 0), (2, 0, 2), (0, 0, 2)), 1):
    model.add_node(node, *xyz)
model.add_material(1, "vertical", E=1000, nu=.25)
model.material.add_bar_parameter(1, BarParameter(1, 1, 1, 1))
for element, nodes in enumerate(((1, 2), (2, 3), (3, 4), (4, 1)), 1):
    model.add_element(element, "bar", nodes, 1, section_id=1, shear_correction=False)
for node in model.mesh.nodes:
    model.add_restraint(node, True, True, True, True, True, True)
panel = SpatialLoadPanel(
    7, (1, 2, 3, 4), triangles=((1, 2, 3), (1, 3, 4)),
    plane=LocalPlane((0, 0, 0), (1, 0, 0), (0, 0, 1)),
)
paths = (SpatialLoadPath(1, ((0, 0, 0), (2, 0, 0))),
         SpatialLoadPath(2, ((0, 0, 2), (2, 0, 2))))
load = SpatialLoad(1, 7, (1, 2), ((3, 3), (3, 3)), LoadDirection("normal"))
model.set_spatial_loads(SpatialLoadDefinitions((panel,), paths, (load,)))
audit = model.run()["spatial_load_contribution"]
assert isclose(audit["resultant"][1], -12, abs_tol=1e-12)
assert isclose(audit["moment"][0], 12, abs_tol=1e-12)
assert isclose(audit["moment"][2], -12, abs_tol=1e-12)
```

通常荷重を残して空間荷重だけを外すには、`model.set_spatial_loads(SpatialLoadDefinitions())`を使います。

## 正規化JSONとHTTP

橋面荷重は3Dモデル専用です。`dimension: 2`を明示した入力に空間荷重定義がある場合はエラーになります。
従来の入力との互換性のため、`dimension`を省略した3D解析入力も利用できます。

JSON保存時はトップレベル`spatial_loads`に、定義を欠落なく出力します。
次は上の面荷重の部分スキーマです。構造節点・材料・要素・支持はモデル本体へ指定します。

```json
{
  "spatial_loads": {
    "panels": [{"id": 7, "nodes": [1, 2, 3, 4], "triangles": [[1, 2, 3], [1, 3, 4]],
                "holes": [], "plane": {"origin": [0, 0, 0], "axis_u": [1, 0, 0], "axis_v": [0, 1, 0]}}],
    "paths": [
      {"id": 1, "points": [[0, 0, 0], [2, 0, 0]]},
      {"id": 2, "points": [[0, 2, 0], [2, 2, 0]]}
    ],
    "loads": [{"id": 1, "panel_id": 7, "path_ids": [1, 2], "end_intensities": [[-3, -3], [-3, -3]],
               "direction": {"mode": "global", "vector": [0, 0, 1]}}]
  }
}
```

IDは整数へ正規化し、重複ID・未知キー・欠落参照・非有限値を拒否します。
独立載荷メッシュはパネルに`loading_nodes: [{"id": 101, "point": [x, y, z]}]`と
`loading_triangles: [[101, 102, 103]]`を併記します。穴の節点IDはこの載荷メッシュ側を参照します。
`load_inf`の荷重IDは選択ケース内の順序により1から付けます。
`.fw3`には保存できません。定義を持つモデルは既存ファイルを開く前に拒否し、JSON保存を案内します。

### ケース別の橋面荷重

デスクトップからの計算要求では、上記の`panels`・`paths`・`loads`を各`load[case_id].spatial_loads`へ指定します。
定義と荷重はそのケースだけに適用され、`spatial_loads`のないケースには加算されません。
この境界の`panels[].elements`は旧入力の`shell`番号です。部材とシェルの番号が重なる場合も、内部要素番号へ変換して読み込みます。
ケース内`spatial_loads`とトップレベル`spatial_loads`、または同じケースの空でない`load_inf`を併記するとエラーになります。

トップレベルの従来の`spatial_loads`は引き続き各選択ケースへ適用され、`elements`は内部要素番号のままです。
ケース間で異なる荷重を指定するクライアントはケース内の形式を使います。共通の橋面・経路の名前などはデスクトップの保存用`bridge_loads`で保持し、計算時に必要な定義を各ケースへ展開します。

静解析結果には、実際に組み立てた橋面荷重だけの`diagnostics.spatial_loads`が含まれます。
合力・全体原点回りモーメント・配分後節点力・保存誤差を確認できます。
通常の節点荷重や部材荷重はこの診断へ含めません。診断の節点力を入力荷重へ再び加えると二重載荷になるため、結果表示専用として扱います。
詳細は[結果の読み方](results.md#橋面荷重の配分確認)を参照してください。

[HTTP API](endpoints.md)も同じ旧形式・新形式を受け付け、通常JSONと既存の圧縮形式の双方で同じ解析結果を返します。
次の例はローカルHTTPサーバー起動後に実行します。

<!-- run: spatial-http -->
```python
import json
from math import isclose
from pathlib import Path
from urllib.request import Request, urlopen

request = Request("http://localhost:5000/", method="POST",
                  data=Path("spatial-cantilevers.json").read_bytes(),
                  headers={"Content-Type": "application/json"})
with urlopen(request) as response:
    result = json.load(response)
assert isclose(result["node_displacements"]["2"]["dz"], -0.008, abs_tol=1e-12)
assert isclose(result["spatial_load_contribution"]["resultant"][2], -6, abs_tol=1e-12)
```

## 出力と失敗時の扱い

`node_displacements`、`reaction_forces`、`element_stresses`と既存のシェル結果に荷重が反映されます。
`spatial_load_contribution`は空間荷重だけの監査結果です。主なキーは次のとおりです。

| キー | 内容 |
|---|---|
| `resultant`、`moment` | 入力分布を積分した全体座標の合力と、全体原点回りモーメント |
| `nodal_resultant`、`nodal_moment` | 構造節点へ配分した荷重から再計算した合力とモーメント |
| `force_error`、`moment_error` | 入力分布側と節点側の保存誤差。許容値超過時は解析前に拒否する |
| `node_loads` | 構造節点へ配分した荷重。`stride: 3`では`[Fx, Fy, Fz]`、`stride: 6`では`[Fx, Fy, Fz, Mx, My, Mz]`。空間荷重による節点モーメントは0 |
| `shell_element_loads`、`cells` | シェルへ直接与えた要素荷重と、セルごとの配分内訳 |
| `loads` | 荷重IDごとの監査結果 |

`loads`の各項目には`feature`、`integrated_length`、`clipped_area`、合力・モーメント、保存誤差、
`estimated_nodal_error`、`estimated_resultant_error`、`estimated_moment_error`、`evaluations`、
`subdivisions`が含まれます。線荷重では`integrated_length`、面荷重では`clipped_area`を入力図と
独立計算に照合してください。複数荷重の総和は最上位の監査値で確認します。

空間荷重だけの確認ケースでは、支点位置を`r`、支点反力を`R`、支点反力モーメントを`M_R`とすると、
`sum(R) + resultant = 0`および`sum(cross(r, R) + M_R) + moment = 0`です。
他の荷重を併用する場合は、その合力とモーメントも同じ全体原点で加えて確認します。

`element_nodal_equilibrium_forces`はシェルの`K_e u_e - f_e`を全体座標で返します。
`node_ids`順の各行は`[Fx, Fy, Fz, Mx, My, Mz]`です。
直接シェル荷重には空間荷重と既存`pressure`の両方を含めます。
載荷三角形からの荷重は節点荷重なので、シェル直接荷重には含めません。
既存の構成則によるシェル応力・断面合力・辺の力は、引き続き別の出力です。

定義は読込時に既存荷重へ加算せず、解析ごとに組み立てます。保存・再読込・再解析で荷重は累積しません。
解析に失敗した場合は結果とコンパイル済み空間荷重を破棄し、修正後の再試行では最新の入力から作り直します。
空間荷重がないモデルには、この追加出力を付けません。

## 制限とエラー

- 平面の非凸外周と宣言した穴を扱います。自己交差、経路の重複点、退化要素、不連続なパネル、
  外周外や穴内への線載荷を拒否します。外挿や凸包への置換はしません。
- 曲線ライン、曲面、位置ごとに変わる面積Jacobian・法線方向は未対応です。
- `material_nonlinear`と`modal`は、強度がゼロでも荷重定義が一件あれば拒否します。
- 梁とT3/Q4シェルは公開経路を検証済みです。他の静解析対応要素の構造節点にも載荷三角形を設定できますが、
  それらへの個別の力学検証は未完了です。詳細は[対応表](elements.md)で区別しています。
- 移動荷重、最不利配置・包絡、設計活荷重モデル、旧RBF・台形則による旧値互換は未実装です。

パネル7の経路が外へ出ると、Pythonでは`ValueError`を継承する入力例外が発生し、
HTTPでは400・`error_code: "invalid_input"`・`converged: false`を返します。
メッセージにパネル・荷重・経路など原因のIDを含め、剛性行列組立前に停止します。
対象外解析は400・`unsupported_analysis`で、`details`に`analysis_type`、`features`、`panel_ids`、`load_ids`を返します。

<!-- run: spatial-unsupported-analysis -->
```python
from fem import FemModel

model = FemModel()
model.load_model("spatial-cantilevers.json")
try:
    model.run("modal")
except ValueError as error:
    assert error.error_code == "unsupported_analysis"
    assert error.details["panel_ids"] == [7]
    assert error.details["load_ids"] == [1]
else:
    raise AssertionError("modal must reject spatial loads")
assert model.results is None
assert model.run("static")["metadata"]["solver"]["converged"] is True
```
