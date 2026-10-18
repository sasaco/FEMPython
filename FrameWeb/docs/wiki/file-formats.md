# ファイル入出力・VTK

[Wikiホーム](index.md) · [モデルの入力](data-structures.md) · [Python API](python-api.md)

継続してモデルを扱う場合はJSONを使います。既存データを読み込むために `.fem` と `.fw3` の入口もありますが、対応範囲が異なります。

## スリップ支持ばねの保存

JSONでは定義を`boundary_conditions.nonlinear_spring_supports`へ保存します。
線形ばねの`spring_supports`は従来の数値辞書のままです。

```json
{"boundary_conditions":{"nonlinear_spring_supports":{"30":{"x":{"type":"slip","K1":1000,"K2":100,"delta_1":0.01}}}}}
```

モデルJSONはばね定義と解析条件を保存し、再読込・再解析で履歴を再現します。
確定履歴は結果JSONの`support_response`に保存されます。モデルを途中から再開するcheckpointではありません。
FW3へスリップばねを含むモデルを保存しようとすると、書込み前にエラーになります。
JRSNAP互換の支持ばね（`jrsnap_trilinear`・`jrsnap_bilinear`）も同じ場所・同じ規則で保存します。
トリリニアは省略した`K*_neg`を正側の値で補った全キーを保存するので、省略しても明示しても入力hashは同じです。
[履歴例](../examples/slip-support-history.json)・[結果の説明](results.md#非線形支持ばねの履歴結果)を参照してください。

## 対応形式

| 形式 | 読込 | 書込 | 用途と範囲 |
|---|---|---|---|
| 編集用JSON：`node/member/element` | 対応 | 元の編集形式では保存しない | 荷重ケース・部材荷重・着目点を含む入力 |
| 保存用JSON：`nodes/elements/materials` | 対応 | 対応 | 解析モデルを保存して再読込する |
| 結果JSON | 対応 | 対応 | 数値結果の保存・別アプリとの連携 |
| V0構造形式 `.fem` | 対応範囲あり | 非対応 | 対応するシェル・ソリッドの既存データ移行 |
| 独自形式 `.fw3` | 部分対応 | 部分対応 | 簡易な旧形式。完全なモデル保存には使わない |
| ASCII VTK | 読込APIなし | 対応 | 対応セルの形状、ID、変位・反力・要素結果の可視化 |

## 保存して再び解析する

[はじめに](getting-started.md)の`beam.json`を用意して実行します。

<!-- run: model-result-roundtrip -->
```python
from math import isclose
from fem import FemModel
from fem.file_io import read_result

model = FemModel()
model.load_model("beam.json")
before = model.run()
model.save_model("saved-model.json")
model.save_results("saved-result.json")

restored = FemModel()
restored.load_model("saved-model.json")
after = restored.run()
assert isclose(after["node_displacements"][2]["dy"], before["node_displacements"][2]["dy"], rel_tol=1e-8)
assert after["metadata"]["input_sha256"] == before["metadata"]["input_sha256"]

saved = read_result("saved-result.json")
assert "2" in saved["node_displacements"]  # JSONのIDは文字列
assert saved["metadata"]["schema_version"] == "1.0"
print(saved["node_displacements"]["2"])
```

保存するモデルは、ケース選択・分割後の解析モデルです。元の編集用JSONにあったすべての荷重ケースや編集情報を再生成する機能ではありません。元の入力も別に保管してください。

解析設定を保持したい場合は、保存前に`model.analysis_type`と`model.analysis_params`へ設定します。`run("static")`の引数は、その呼出しに使う指定であり、保存される`model.analysis_type`を書き換える操作ではありません。

非線形材料の定義と載荷係数は保存できますが、確定済みの内部履歴状態は保存しません。再読込後は最初から解析します。

## 保存用JSONの構造

| キー | 値の形 |
|---|---|
| `nodes` | ID → `[x, y, z]` |
| `elements` | ID → `type, nodes, material_id`と要素固有パラメータ |
| `materials` | ID → `name, E, nu, density, alpha, shear_modulus`など |
| `bar_parameters` | 断面ID → `area, Iy, Iz, J, kappa_y, kappa_z`など |
| `nonlinear_materials` | ID → JR材料。旧固定則は`delta_1_pos, P_1_pos`など、成分別則は`laws`内に保存し、Nd表は`axial_force_points`を保持 |
| `boundary_conditions.restraints` | 節点ID → `dof, values` |
| `boundary_conditions.loads` | 節点ID → `[fx, fy, fz, mx, my, mz]` |
| `boundary_conditions.spring_supports` | 節点ID → 方向と剛性 |
| `boundary_conditions.pressures` | `element_id, face, pressure`の配列 |
| `analysis_type, analysis_params` | 解析種別・制御値 |
| `model_metadata` | 座標系と一貫単位系の宣言。旧入力で省略した場合は単位未指定 |

保存される`auxiliary_restraint_nodes`は、2D化で自動追加した支持節点を区別する情報です。通常は利用者が手で作る必要はありません。

この形式で線形梁を手書きするときは、`elements`に`section_id`を、`bar_parameters`に対応する断面を指定します。`shear_correction`も明示してください。部材荷重は要素の`line_loads`、温度は`temperature`、分布ばねは`foundation`、回転解放は`releases`として保存されます。編集用JSONの`load_member/fix_member/joint`をそのままこの形式へ移しても処理されません。

`materials`の`k`と`c`は保存する物性欄ですが、現行の公開解析に熱伝導ソルバーがあることを意味しません。直接`BoundaryCondition`へ追加した汎用分布荷重など、保存対象に含まれない状態もあります。独自の低水準モデルは保存前後の解析条件を比較してください。

## 結果JSON

`save_results()`、`write_result()`、HTTPの結果変換はNumPy配列を配列へ、IDキーを文字列へ変換します。NaN・Infinityを含む結果は保存・送信できません。成功結果の`metadata`も通常のJSONとして保存され、`read_result()`で値を保ったまま読み戻せます。

`read_result()`や`load_results()`はJSONをそのまま読むため、文字列キーやリストを整数キー・NumPy配列へ復元しません。読込後は`model.get_results()["node_displacements"]["2"]`のようにアクセスします。数値比較が必要なら配列を`numpy.asarray()`で変換してください。

## V0の構造ファイル `.fem`

空白区切りのレコード形式です。現行リーダーは未対応行を黙って捨てず、行番号付きの入力エラーにします。

| レコード | 主な内容 |
|---|---|
| `Node` | ID、x、y、z |
| `Material` | ID、E、nu、G、密度、熱伝導率、比熱 |
| `ShellParameter` | ID、厚さ |
| `TriElement1 / QuadElement1` | ID、材料ID、厚さID、節点ID列 |
| `TetraElement1 / WedgeElement1 / HexaElement1` | ID、材料ID、一次要素の節点ID列 |
| `TetraElement2 / WedgeElement2 / HexaElement2` | ID、材料ID、二次要素の節点ID列 |
| `Restraint` | 節点ID、各自由度の「拘束フラグ・指定変位」の組。3または6成分 |
| `Load` | 節点ID、力3成分、必要ならモーメント3成分 |
| `Pressure` | シェルID、F1/F2、圧力 |

現行リーダーは梁用のV0レコード、特殊座標系、熱解析レコードなどの全機能を移植したものではありません。`docs/v0`内にサンプルがあることだけでは、現在の読込・解析対応を意味しません。詳細は[V0リーダー](https://github.com/sasaco/fempy/blob/02e55e59d725ceab78e89fdb03200e21c44ac732/src/fem/v0_io.py)と[入力テスト](https://github.com/sasaco/fempy/blob/02e55e59d725ceab78e89fdb03200e21c44ac732/tests/io/test_source_input.py)を参照してください。

## 独自形式 `.fw3`

`*NODES`、`*ELEMENTS`、`*RESTRAINTS`、`*LOADS`などのセクション形式です。現行読込は材料・断面情報を完全に復元せず、書き出した`*PRESSURES`も読込側が処理しません。これだけで解析条件を保った保存・再読込はできません。新しい作業の保存にはJSONを使ってください。

## VTKで結果を可視化する

`write_vtk()`はLegacy ASCII VTKの`UNSTRUCTURED_GRID`を書き出します。`bar`、`nonlinear_bar`、3／4節点`shell`、一次・二次の`tetra`／`wedge`／`hexa`に対応します。節点数が要素型と一致しない場合や未対応型は、セルタイプ0や空セルとして保存せず`ValueError`にします。

節点と要素はID順に出力し、元のIDを`node_id`と`element_id`へ保存します。結果辞書の挿入順には依存しません。ある量が一部の節点・要素にだけ定義される場合、対象外・欠損箇所は0ではなく`NaN`です。可視化ソフトでは`NaN`を欠損値として扱ってください。

次は梁の解析結果をそのまま書き出す例です。

<!-- run: beam-vtk -->
```python
from pathlib import Path
from fem import FemModel
from fem.file_io import write_vtk

model = FemModel()
model.load_model("beam.json")
result = model.run()
write_vtk({"mesh": model.mesh}, result, "beam.vtk")
text = Path("beam.vtk").read_text(encoding="utf-8")
assert "CELL_TYPES 1\n3\n" in text
assert "VECTORS displacement double" in text
assert "SCALARS node_id int 1" in text
assert "SCALARS element_id int 1" in text
assert "SCALARS section_force_i_mz double 1" in text
print("beam.vtk を保存しました")
```

主な配列は次のとおりです。

| VTK配列 | 位置 | 意味 |
|---|---|---|
| `node_id` / `element_id` | 点／セル | 元モデルのID |
| `displacement` | 点 | 全体座標の並進変位`dx,dy,dz` |
| `reaction_force` | 点 | 全体座標の反力`fx,fy,fz`。反力が定義されない節点は`NaN` |
| `section_force_i_*` / `section_force_j_*` | セル | 梁・非線形梁の端力`fx,fy,fz,mx,my,mz` |
| `shell_stress_surface_1/2` | セル | シェル両面の全体座標応力テンソル |
| `shell_strain_surface_1/2` | セル | シェル両面の全体座標ひずみテンソル |
| `shell_membrane_*` / `shell_moment_*` / `shell_shear_*` | セル | シェル局所座標の単位幅当たり断面合力 |
| `solid_stress_gauss_point_mean` | セル | ソリッド積分点応力の成分別単純平均テンソル |
| `solid_strain_gauss_point_mean` | セル | ソリッド積分点工学ひずみをテンソルせん断へ直した成分別単純平均 |

ソリッドの`*_gauss_point_mean`は、積分点を体積重み付けした平均、セル中心値、節点外挿値ではありません。積分点ごとの値が必要な評価では[結果JSON](results.md)の`element_stresses`を使ってください。シェルの断面合力は現行の`resultants`であり、廃止した仮想梁シェル断面力ではありません。

二次`tetra2`／`wedge2`／`hexa2`は中間節点を含むVTKセル24／26／25で保存します。`wedge`系の節点順はVTK 9.7のパラメトリック座標に合わせています。VTK 9.7より前の規約を固定したreaderでは三角形面の向きを異なる順へ正規化する場合があります。
