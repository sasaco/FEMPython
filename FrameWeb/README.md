# FEMPython

FEMPythonは、梁・シェル・ソリッドで構造物の変位、反力、断面力、応力を計算する
Python有限要素解析ライブラリです。Python API、JSONファイル、およびリポジトリ／
コンテナから起動するHTTP APIを提供します。

現行実装は、線形静解析、JR総研剛性低減RC型の材料非線形解析、固有値解析に対応します。
要素ごとの対応範囲と未実装の組合せは[要素と解析の選び方](docs/wiki/elements.md)を確認してください。

線形静解析では、指定位置の[線荷重・面荷重](docs/wiki/spatial-loads.md)を梁・シェルへ載荷できます。
明示局所平面による傾斜パネル、全体方向ベクトル・面法線、非凸外周・穴を扱えます。省略時は全体XY／+Zです。入力例・単位・保存形式・制限はリンク先を参照してください。

## インストール

配布wheelを利用する場合:

```bash
python -m pip install FEMPython
```

リポジトリから開発・検証する場合はPython 3.11以上、uv、固有の旧JavaScript比較を行う場合は
Node.jsを用意します。

```bash
git clone https://github.com/sasaco/fempy.git
cd fempy
uv sync --locked --extra dev
```

公開import名は`fem`と`app`です。リポジトリ内部の配置名である`src.fem`は使いません。

## 最小の解析例

次はN・m・sの一貫単位系で、長さ1 m、`E=1000 N/m²`、断面積2 m²の軸材へ
100 Nを載荷します。独立解は`u=F L/(E A)=0.05 m`です。

```python
from math import isclose
from fem import BarParameter, FemModel

model = FemModel()
model.add_node(1, 0, 0, 0)
model.add_node(2, 1, 0, 0)
model.add_material(1, "test", E=1000, nu=0.25, density=2)
model.material.add_bar_parameter(
    1, BarParameter(area=2, Iy=1, Iz=1, J=1)
)
model.add_element(
    1, "bar", [1, 2], 1, section_id=1, shear_correction=False
)
model.add_restraint(1, True, True, True, True, True, True)
model.add_restraint(2, False, True, True, True, True, True)
model.add_load(2, fx=100)

result = model.run("static")
displacement = result["node_displacements"][2]["dx"]
assert isclose(displacement, 0.05, rel_tol=1e-12)
print(displacement)
```

入力値へ単位情報は保存されません。力・長さ・時間の単位をモデル全体で統一してください。
材料非線形の収束判定はモデルの代表長さで力・モーメント、並進・回転を無次元化するため、
物理的に同じN–m、N–mm、kN–mモデルでは同じ判定になります。ただし、E・断面・密度・
荷重・ばね・材料骨格を一貫して換算する必要があります。
入力形式、境界条件、結果の符号・評価位置は[ユーザーガイド](https://sasaco.github.io/fempy/)にあります。

## HTTP API

HTTP入口の`main.py`はwheelには含まれません。リポジトリまたは提供コンテナから起動します。

```bash
uv run functions-framework --target FEMPython --source main.py --port 8080
```

旧デプロイ名`FrameWeb3`は`FEMPython`の互換aliasとして残しています。通常JSONと互換圧縮形式、
成功・失敗レスポンスは[HTTP API](docs/wiki/endpoints.md)を参照してください。

## 対応範囲

<!-- capability-matrix:start -->
この表は配布物に含まれる`fem/capabilities.json`から生成します。
「実装済み」は経路が存在するものの、この組合せに対する個別の力学検証が未完了であることを示します。
荷重・結果欄にない項目は未対応です。JSON正本では未対応項目と理由も明示しています。
空間線荷重・空間面荷重は線形静解析のみです。明示局所平面・全体方向ベクトル・面法線、非凸外周・宣言した穴に対応します。省略時は全体XY／+Zです。
非線形支持ばね（スリップ、JRSNAP互換バイリニア・トリリニア）は材料非線形解析のみです。全体座標の並進3方向・6DOFモデルの回転3方向へ指定できます。支持則ごとの検証状態と組合せはJSON正本の`support_types`を参照してください。

| 要素（公開名） | 節点数 | 線形静解析 | 材料非線形解析 | 固有値解析 | 質量行列 | 利用できる荷重 | 取得できる結果 |
|---|---:|---|---|---|---|---|---|
| `bar`、`beam` | 2 | 検証済み | 検証済み | 検証済み | 検証済み | 節点荷重（検証済み）、強制変位（検証済み）、支持ばね（検証済み）、部材分布荷重（検証済み）、温度荷重（検証済み）、分布ばね（検証済み）、材端解放（検証済み）、空間線荷重（検証済み）、空間面荷重（検証済み） | 節点変位（検証済み）、支点反力（検証済み）、梁端力（検証済み）、固有値・モード（検証済み） |
| `nonlinear_bar` | 2 | 検証済み | 検証済み | 実装済み（個別検証未完了） | 実装済み（個別検証未完了） | 節点荷重（検証済み）、強制変位（検証済み）、支持ばね（検証済み）、空間線荷重（実装済み）、空間面荷重（実装済み） | 節点変位（検証済み）、支点反力（検証済み）、梁端力（検証済み）、固有値・モード（実装済み） |
| `shell` | 3、4 | 検証済み | 実装済み（個別検証未完了） | 検証済み | 検証済み | 節点荷重（検証済み）、強制変位（検証済み）、支持ばね（検証済み）、シェル面圧（検証済み）、空間線荷重（検証済み）、空間面荷重（検証済み） | 節点変位（検証済み）、支点反力（検証済み）、シェル表面量（検証済み）、固有値・モード（検証済み） |
| `tetra`、`tet` | 4 | 検証済み | 実装済み（個別検証未完了） | 検証済み | 検証済み | 節点荷重（検証済み）、強制変位（検証済み）、支持ばね（検証済み）、空間線荷重（実装済み）、空間面荷重（実装済み） | 節点変位（検証済み）、支点反力（検証済み）、積分点応力（検証済み）、固有値・モード（検証済み） |
| `wedge` | 6 | 検証済み | 実装済み（個別検証未完了） | **未対応** | **未対応** | 節点荷重（検証済み）、強制変位（検証済み）、支持ばね（検証済み）、空間線荷重（実装済み）、空間面荷重（実装済み） | 節点変位（検証済み）、支点反力（検証済み）、積分点応力（検証済み） |
| `hexa`、`hex` | 8 | 検証済み | 実装済み（個別検証未完了） | 実装済み（個別検証未完了） | 検証済み | 節点荷重（検証済み）、強制変位（検証済み）、支持ばね（検証済み）、空間線荷重（実装済み）、空間面荷重（実装済み） | 節点変位（検証済み）、支点反力（検証済み）、積分点応力（検証済み）、固有値・モード（実装済み） |
| `tetra2` | 10 | 検証済み | 実装済み（個別検証未完了） | 検証済み | 検証済み | 節点荷重（検証済み）、強制変位（検証済み）、支持ばね（検証済み）、空間線荷重（実装済み）、空間面荷重（実装済み） | 節点変位（検証済み）、支点反力（検証済み）、積分点応力（検証済み）、固有値・モード（検証済み） |
| `wedge2` | 15 | 検証済み | 実装済み（個別検証未完了） | 実装済み（個別検証未完了） | 検証済み | 節点荷重（検証済み）、強制変位（検証済み）、支持ばね（検証済み）、空間線荷重（実装済み）、空間面荷重（実装済み） | 節点変位（検証済み）、支点反力（検証済み）、積分点応力（検証済み）、固有値・モード（実装済み） |
| `hexa2` | 20 | 検証済み | 実装済み（個別検証未完了） | 実装済み（個別検証未完了） | 検証済み | 節点荷重（検証済み）、強制変位（検証済み）、支持ばね（検証済み）、空間線荷重（実装済み）、空間面荷重（実装済み） | 節点変位（検証済み）、支点反力（検証済み）、積分点応力（検証済み）、固有値・モード（実装済み） |
| `pyramid` | 5 | **未対応** | **未対応** | **未対応** | **未対応** | — | — |
| `hexa20` | 20 | **未対応** | **未対応** | **未対応** | **未対応** | — | — |

状態と全aliasを含む機械可読な定義はPythonからも取得できます。

```python
from fem import get_capability_registry

capabilities = get_capability_registry()
```
<!-- capability-matrix:end -->

- 複数の荷重ケースを同時に重ねて解く入口ではありません。編集用JSONでは選択した1ケースを解析します。
- 影響線解析、一般的な座屈解析、幾何学的非線形解析は提供していません。

「計算が終了したこと」と「対象構造に対して妥当なモデルであること」は別です。支持条件、単位、
要素選択、メッシュ依存性を確認し、重要な用途では独立解・実験・他実装との比較を行ってください。

## 検証

ロックした開発環境で全テストとWikiの実行例を検査します。

```bash
uv run --locked --extra dev pytest tests
uv run --locked --extra dev python -m tools.validation.check_wiki
```

性能は節点数だけでなく、自由度数、疎行列の非ゼロ数、荷重case数、材料非線形の履歴step数、
結果量、実行環境で変わります。固定した小／中／大workloadの再現手順と実測値は
[PQ-12性能基準・計測報告](docs/report/product-quality-pq12.md)にあります。記載した最大規模は
製品上限ではありません。通常試験とは分離した専用計測は次で実行できます。

```bash
uv run --locked --extra dev python -m tools.validation.performance --profile baseline --repeats 5 --warmups 1 --output tmp/performance.json
```

検証範囲と最新の結果は、[テスト保証範囲](docs/plans/test_items.md)、
[テスト台帳](docs/plans/test_inventory.md)、[品質ロードマップ](docs/plans/product-quality-roadmap.md)、
および[検証報告](docs/report/)を参照してください。

## ドキュメント

- [公開ユーザーガイド](https://sasaco.github.io/fempy/)
- [はじめに](docs/wiki/getting-started.md)
- [Python API](docs/wiki/python-api.md)
- [入力データ](docs/wiki/data-structures.md)
- [実行例](docs/wiki/examples.md)
- [結果の読み方](docs/wiki/results.md)
- [設計実務での利用手順](docs/wiki/design-practice.md)
- [エラーと対処](docs/wiki/error-handling.md)

## ライセンス

[MIT License](LICENSE)
