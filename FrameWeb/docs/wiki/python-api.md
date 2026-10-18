# Python API

[Wikiホーム](index.md) · [実行例](examples.md) · [ファイル入出力](file-formats.md)

通常は`FemModel`を入口にします。節点・材料・断面・要素・支持・荷重を作り、`run()`で解析します。要素の追加時に材料や座標を参照するため、以下の順序で設定してください。

## スリップ支持ばね

節点と構造要素を作成したモデルへ、全体座標の方向ごとに追加します。

```python
model.add_slip_spring_support(30, "x", K1=1000, K2=100, delta_1=0.01)
model.add_load(30, fx=1.0)
model.analysis_params["displacement_control"] = {
    "node": 30, "dof": "dx", "targets": [0.005, 0.03, 0.024, 0.018, 0.01]
}
result = model.run()
force = result["support_response"][30]["x"]["force"]
```

方向は`x/y/z/rx/ry/rz`です。回転は6DOFモデルに限ります。
`K1>0, delta_1>0, 0<=K2<=K1`の有限数値を受け付け、同一自由度の他の支持・拘束と競合するとエラーになります。
線形支持は従来の`add_spring_support()`を使います。ばねを含むモデルで解析種別を省略すると
`material_nonlinear`を選び、明示`static`・`modal`は拒否します。再実行時は支持履歴も初期化します。
[完全なJSON例](../examples/slip-support-history.json)・[履歴則](nonlinear-analysis.md#スリップ支持ばね)も参照してください。

JRSNAP互換の支持ばねは、JSONと同じ定義を`add_nonlinear_spring_support()`へ渡します（スリップの定義も渡せます）。

```python
model.add_nonlinear_spring_support(30, "x", {"type":"jrsnap_trilinear","K1_pos":1000,"F1_pos":10,"K2_pos":200,"F2_pos":14,"K3_pos":-100,"F1_neg":5,"F2_neg":7})
```

定義の検査・競合・原子性は`add_slip_spring_support()`と同じです。[入力規則](data-structures.md#jrsnap互換-支持ばねの指定)を参照してください。

指定位置の線荷重・面荷重は`fem.spatial_loads`の不変定義型を作り、
`model.set_spatial_loads(definitions)`で設定して`model.run("static")`で解析します。
線の強度は力/長さ、面の強度は力/長さ²、省略時の正方向は全体+Zです。
`LocalPlane`で傾斜平面、`LoadDirection`で全体方向ベクトルまたは面法線を指定できます。
非凸外周・宣言した穴にも対応します。[入力例・結果・エラー・制限](spatial-loads.md)を参照してください。

## Pythonだけで片持ち梁を作る

この例はファイルを用意せずに実行できます。[はじめに](getting-started.md)のJSONモデルと同じ条件です。

<!-- run: python-cantilever -->
```python
from math import isclose
from fem import FemModel, BarParameter

model = FemModel()
model.add_node(1, 0, 0, 0)
model.add_node(2, 2, 0, 0)
model.add_material(1, "Steel", E=200e9, nu=0.3)
model.material.add_bar_parameter(
    1, BarParameter(area=0.01, Iy=1e-5, Iz=1e-5, J=2e-5)
)
model.add_element(1, "bar", [1, 2], 1, section_id=1, shear_correction=False)
model.add_restraint(1, True, True, True, True, True, True)
model.add_load(2, fy=-1000)
result = model.run("static")

assert isclose(result["node_displacements"][2]["dy"], -1/750, rel_tol=1e-8)
print(model.get_node_displacement(2))
print(model.get_element_stress(1))
```

Python APIには`dimension`の設定はありません。この例は3Dの梁として組み立てますが、荷重と形状によりXY平面内で変形します。厳密に平面の自由度へ制限する場合は、各節点の`dz, rx, ry`を明示的に拘束します。

## モデルを作る操作

| 操作 | 主な引数・注意点 |
|---|---|
| `add_node(node_id, x, y, z)` | 節点を追加。zも必要。 |
| `add_material(material_id, name, E, nu, density=None, ...)` | 弾性材料。Gの明示値は`shear_modulus`、線膨張係数は`alpha`。 |
| `material.add_bar_parameter(section_id, BarParameter(...))` | 梁の断面。`area, Iy, Iz, J`を明示する。 |
| `add_element(elem_id, elem_type, node_ids, material_id, ...)` | 梁なら`section_id, angle, shear_correction`、シェルなら`thickness, formulation`を指定。 |
| `add_nonlinear_material(...)` | JR材料を追加。同時に参照弾性材料も作る。[非線形ガイド](nonlinear-analysis.md)参照。 |
| `add_nonlinear_bar_element(elem_id, node_ids, material_id, section_id, hysteresis_dofs, ...)` | 非線形梁を追加。断面・適用自由度を指定。 |
| `add_restraint(node_id, dx=False, dy=False, dz=False, rx=False, ry=False, rz=False, values=None)` | Trueの成分を拘束。`values`は6成分の強制変位・回転。 |
| `add_load(node_id, fx=0, fy=0, fz=0, mx=0, my=0, mz=0)` | 全体座標の節点荷重。JSON入力とは名前が異なる。 |
| `add_spring_support(node_id, direction, stiffness)` | 全体方向`x,y,z,rx,ry,rz`の支持ばね。正の有限剛性。 |
| `add_forced_displacement(node_id, dx=0, dy=0, dz=0, rx=0, ry=0, rz=0)` | 非ゼロ成分を拘束し、既存支持に追加する。 |
| `add_distributed_load(element_id, direction, value_i, value_j)` | 要素全長に線形変化する分布荷重。 |
| `add_temperature_load(element_id, temperature)` | 線形梁の一様温度変化。材料の`alpha`も指定する。 |
| `add_distributed_spring(element_id, dx=0, dy=0, dz=0, rx=0)` | 梁局所方向の分布ばね。最後のrxは分布ねじりばね。 |
| `add_joint_condition(element_id, xi=1, yi=1, zi=1, xj=1, yj=1, zj=1)` | 局所軸の材端回転を0で解放、1で接続。省略は接続。 |
| `boundary.add_pressure(element_id, face, pressure)` | シェルのF1/F2に一様面圧。 |
| `add_notice_points([{ "m": ..., "Points": [...] }])` | 部材内の位置で分割。荷重と合わせたモデル作成には編集用JSONの入口を利用する。 |

分布荷重の方向は`local_x, local_y, local_z, local_r, global_x, global_y, global_z`です。部材途中だけの分布荷重や集中荷重は、[編集用JSONの荷重・分割指定](data-structures.md)を使うと、荷重区間と節点を一緒に処理できます。

支持・荷重の上書きと加算は区別してください。`add_load()`は同じ節点への荷重を加算します。`add_restraint()`はその節点の拘束を置き換えます。`add_forced_displacement()`の0は「既存の値を0に戻す」操作ではありません。値を0へ戻す場合は`add_restraint(..., values=[...])`で支持全体を再設定します。

拘束値の絶対値が1,000を超える場合のばね互換処理、同一自由度の拘束とばねの競合については[入力ガイド](data-structures.md)を参照してください。

## 解析・結果の操作

| 操作 | 内容 |
|---|---|
| `run(analysis_type=None)` | 解析して辞書を返す。未指定時の選択規則は[解析モード](elements.md)参照。 |
| `run_static_analysis()` | `run("static")`と同じ。 |
| `analysis_params` | 解析制御の辞書。載荷係数、反復上限、許容差、モード数を設定。 |
| `model_metadata` | 座標系と一貫単位系の宣言。既定では単位未指定。 |
| `get_results()` | 現在の結果。未解析・解析失敗後はNone。 |
| `get_node_displacement(node_id)` | 解析済み節点変位。存在しなければNone。 |
| `get_element_stress(elem_id)` | 要素結果。梁では端力。存在しなければNone。 |
| `get_model_info()` | `n_nodes, n_elements, n_materials, n_restraints, n_loads, element_types`など。 |
| `load_model(path)`、`save_model(path)` | モデルを読込・保存。 |
| `save_results(path)`、`load_results(path)` | 結果JSONを保存・読込。履歴状態の再開には使わない。 |

固有値解析のモード数は`model.analysis_params["n_modes"]`で指定して`run("modal")`を呼ぶか、`run_modal_analysis(n_modes=...)`へ直接渡します。後者は呼出し中だけ設定を上書きし、解析後は元の`analysis_params`を復元します。モード数には正の整数を指定してください。[固有値解析の例](examples.md)も参照してください。

`get_node_count()`・`get_element_count()`というメソッドはありません。`model.get_model_info()["n_nodes"]`、または`len(model.mesh.nodes)`を使います。

## ファイルを読んでから組み立てる場合

`load_model()`は変換と組立をまとめて行います。途中のモデルを扱う必要があれば、`fem.file_io.read_model(path)`が返す内部モデル辞書を`model.read_json_model(...)`へ渡せます。

`read_json_model()`は名前に反して、生のJSON辞書をそのまま受け取るメソッドではありません。`mesh, material, boundary, section`などのオブジェクトへ変換済みの辞書が必要です。`load_model_from_dict()`も存在しません。通常はJSONファイルを保存して`load_model()`を使うか、上記の構築APIで組み立ててください。

## 状態の管理

`run()`は毎回、要素を作り直して解析状態を初期化します。材料非線形の前回履歴を続行する操作ではありません。モデル読込や再解析が失敗した場合、前回の結果を今回の成功結果として残さないよう`results`を消去します。

解析失敗は`ValueError`／`RuntimeError`互換の診断例外で、`error_code`と確定できた`details`を
参照できます。分類とログ設定は[エラーと対処](error-handling.md)、成功結果の`metadata`は
[結果の読み方](results.md)を参照してください。

返値や`get_results()`はアプリで利用する結果です。独立した保管用コピーが必要なら`copy.deepcopy()`またはJSON保存を使ってください。複数ケース・複数ジョブでは、ケースごとに`FemModel`を作ると状態の混同を避けられます。

要素・ソルバーを直接組み合わせる上級用途は[解析ワークフロー](workflow.md)を参照してください。
