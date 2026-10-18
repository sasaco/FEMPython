# 材料非線形解析

[Wikiホーム](index.md) · [モデルの入力](data-structures.md) · [結果の読み方](results.md)

材料非線形解析は、荷重を増減させながら、部材や支持ばねの剛性変化と履歴を追う解析です。部材にはJR総研剛性低減RC型、節点と固定地盤の間の支持にはスリップばねとJRSNAP互換の多直線ばねを使用できます。

## スリップ支持ばね

`fix_node`の自由度欄へ `{"type":"slip","K1":1000,"K2":100,"delta_1":0.01}` を指定します。
初期・除荷・再載荷の勾配はK1、経験最大変形を超える骨格の勾配はK2です。
除荷で力がゼロになると、過去の正負最大点で決まる区間内は力・接線ともゼロになります。
`K1>0, delta_1>0, 0<=K2<=K1` の正負対称則です。

この支持があれば、すべての部材が線形でも、省略した解析種別は`material_nonlinear`になります。
明示`static`・`modal`はエラーです。全体座標の並進3方向、6DOFモデルの回転3方向へ設定できます。
支持地盤の移動、二節点間ばね、局所方向、動的時刻歴は対応範囲に含みません。

変位制御には非ゼロの基準節点荷重と`displacement_control.targets`を指定します。
ゼロ剛性区間でも拡大行列が正則なら追跡できます。荷重制御で全体が機構になる場合は未収束で停止し、
最後に収束した履歴を保持します。`run()`の再実行は初期履歴から開始します。
各ステップの`support_response`には確定した力・接線・枝・最大経験点・ゼロ力位置が入ります。

[実行可能な14点の履歴例](../examples/slip-support-history.json)・
[理論と符号規約](../理論マニュアル/08_材料非線形モデル.md#719-スリップばね)を参照してください。

## JRSNAP互換 支持ばね

`type`が`jrsnap_trilinear`・`jrsnap_bilinear`の支持ばねは、JRSNAPの地盤ばね（節点）と同じ正負非対称の骨格です。
力の制限値から折れ点を`x1=F1/K1`、`x2=x1+(F2-F1)/K2`と作り、その先は`K3`（0・負も可）です。下降して力0に達した側は以後抵抗しません。
除荷・再載荷は力の符号に応じた`K1`の直線で、反対側の第1折れ点より手前で降伏した場合は第1制限値で平坦になります。
`K3<0`で軟化した側は、経験した最大点の力を超えて回復しません（劣化記憶）。
この除荷則はJRSNAPに記載がないFrameWebの補完仕様で、JRSNAPと一致を確認したのは正側の単調載荷の骨格です。
JRSNAPは負側のばね力を変位の2倍で評価した値で報告しますが、FrameWebは正側と対称な定義を採ります。

平坦部・下降域は荷重制御では機構となって停止するので、変位制御で追跡してください。
JRSNAPの荷重増分は不釣合い力を残すため、荷重制御の変位はJRSNAPより大きくなります。比べる場合は変位制御を使います。
`support_response`には力・接線・枝（`elastic`・`plateau_*`・`skeleton_*`・`zero_*`）・ゼロ力位置`zero_position`・最大経験変位が入ります。

[実行可能な16点の履歴例](../examples/jrsnap-trilinear-support.json)・
[理論・手計算表・JRSNAPとの差](../理論マニュアル/08_材料非線形モデル.md#722-jrsnap互換-多直線支持ばね弾塑性)を参照してください。

## JR梁のモデル化の範囲

- 小変位・小回転の梁を対象に、**要素中央の1断面**へ非線形則を置きます。
- 非線形化する成分は`axial`、`moment_y`、`moment_z`、`torsion`から選びます。
- 固定骨格の各成分は独立です。成分別`laws`でNd表を曲げ軸へ指定すると、同じ試行状態の総軸力`N`から`Nd=-N`を求め、軸力と曲げを連成します。ファイバー断面やMy-Mz相互作用ではありません。
- 曲げ則を指定した面では履歴接線から有効Iと曲げ・せん断の縮約係数Cを更新し、せん断力を増分積分します。基準E・G・幾何Iは保持します。
- 線形梁、シェル、ソリッドを同じ解析に含めても、それらの材料は線形のままです。

塑性分布が一様でない部材では、要素分割による結果の変化を確認してください。有限回転、集中塑性ヒンジ、K4以外の任意形状の軟化、破断、弧長法、時刻歴の動的応答は対象外です。数学的な検証と実験・外部JR実装との較正は別で、外部との同条件較正は未実施です。[検証の適用範囲](https://github.com/sasaco/fempy/blob/02e55e59d725ceab78e89fdb03200e21c44ac732/docs/report/material-nonlinear-closure.md)も参照してください。

## 入力するのは断面の関係

2026-09-10から固定骨格の曲げ則にもI更新を適用しています。従来の固定C方式と比べ、横荷重を伴う非線形応答のたわみ・端力・履歴が変わります。折れ点を跨ぐ増分では接線が非対称になる場合があります。同じ載荷経路で荷重刻みと部材分割の影響を確認してください。

軸力Ndに依存する曲げ骨格は、旧JSONの`nonlinear.laws`、Pythonの`add_nonlinear_material_laws()`、保存用JSONの`nonlinear_materials[id].laws`で指定できます。Ndは圧縮正で、試行軸力N（引張正）から`Nd=-N`とします。表外へ出た場合は外挿せず、要素・軸・荷重段階を含む入力エラーで停止します。

`delta_1, delta_2, delta_3`という名前でも、節点の絶対変位や集中ヒンジの回転を入れる欄ではありません。

| `hysteresis_dofs` | `delta_*`の意味 | `P_*`の意味 | N・m系での単位 |
|---|---|---|---|
| `axial` | 軸ひずみε | 軸力N | 無次元、N |
| `moment_y` | 局所y軸まわりの中央曲率κy | 曲げモーメントMy | 1/m、N·m |
| `moment_z` | 局所z軸まわりの中央曲率κz | 曲げモーメントMz | 1/m、N·m |
| `torsion` | 局所x軸まわりの単位長さ当たりのねじり角 | ねじりモーメントT | rad/m、N·m |

初期勾配`P_1/delta_1`は断面剛性です。参照弾性と整合させる場合、軸は`EA`、曲げは対応する`EI`、ねじりは`GJ`と合わせます。同一材料の`hysteresis_dofs`を複数指定すると、同じ数値の骨格を各成分へ設定します。異なる単位・剛性の成分へ無条件に同じ曲線を流用しないでください。

単位換算では、軸の`delta_*`は無次元なので不変、曲げ・ねじりの`delta_*`は長さ換算率の
逆数倍です。軸の`P_*`は力換算率、曲げ・ねじりの`P_*`は力×長さ換算率を掛けます。
明示した`K_min`は`P/delta`と同じ次元なので、軸は力換算率、曲げ・ねじりは
力×長さ²の換算率を掛けます。

## 骨格曲線の設定

編集用JSONの`element`に次のように定義します。例は曲げ用の説明値で、設計用の材料推奨値ではありません。

```json
{
  "element": {
    "1": {
      "1": {
        "E": 10000.0, "nu": 0.25, "A": 1.0, "Iy": 1.0, "Iz": 1.0, "J": 2.0,
        "nonlinear": {
          "type": "jr_stiffness_reduction",
          "delta_1": 0.001, "delta_2": 0.004, "delta_3": 0.010,
          "P_1": 10.0, "P_2": 16.0, "P_3": 22.0,
          "delta_4": 0.018, "P_4": 14.0,
          "beta": 0.4, "symmetric": true,
          "hysteresis_dofs": ["moment_z"]
        }
      }
    }
  }
}
```

原点から第3点までは従来の3区間、第3点以降は第4勾配`K4`の4折線です。`delta_4, P_4`は新たな勾配切替点ではなく、`K4`を定める参照点です。

### 軸力Ndに依存する曲げ骨格

成分別`laws`のキーが適用先です。Nd表は`moment_y`または`moment_z`へ指定し、固定JR則の`axial`や`torsion`と同じ材料内で併用できます。`hysteresis_dofs`も書く場合は、`laws`のキーと完全に一致させます。

```json
{
  "nonlinear": {
    "laws": {
      "moment_z": {
        "type": "jr_stiffness_reduction",
        "symmetric": true,
        "beta": 0.0,
        "axial_force_points": [
          {"Nd": -2.0, "delta_1": 0.001, "P_1": 0.05, "delta_2": 0.003, "P_2": 0.10, "delta_3": 0.005, "P_3": 0.125},
          {"Nd": 0.0,  "delta_1": 0.001, "P_1": 0.10, "delta_2": 0.003, "P_2": 0.20, "delta_3": 0.005, "P_3": 0.250},
          {"Nd": 2.0,  "delta_1": 0.001, "P_1": 0.30, "delta_2": 0.003, "P_2": 0.60, "delta_3": 0.005, "P_3": 0.750}
        ]
      }
    },
    "hysteresis_dofs": ["moment_z"]
  }
}
```

各行で同じ番号の曲率・モーメントを線形補間してから勾配を計算します。2行以上、Ndの厳密な昇順、Nd=0を含む範囲、全補間区間で`K1 >= K2 >= K3 >= 0`が必要です。`K_min`省略時は全Nd行・正負側の最小K1の1%を固定値として使います。全行を同一曲線にすると固定骨格の履歴・内力・接線へ帰着します。

<!-- run: axial-force-dependent-beam -->
```python
from math import isclose
from fem import FemModel, BarParameter

law = {
    "type": "jr_stiffness_reduction", "symmetric": True, "beta": 0.0,
    "axial_force_points": [
        {"Nd": -2, "delta_1": .001, "P_1": .05, "delta_2": .003, "P_2": .10, "delta_3": .005, "P_3": .125},
        {"Nd": 0,  "delta_1": .001, "P_1": .10, "delta_2": .003, "P_2": .20, "delta_3": .005, "P_3": .250},
        {"Nd": 2,  "delta_1": .001, "P_1": .30, "delta_2": .003, "P_2": .60, "delta_3": .005, "P_3": .750},
    ],
}
model = FemModel()
model.add_node(1, 0, 0, 0)
model.add_node(2, 1, 0, 0)
model.add_nonlinear_material_laws(1, "Nd beam", E=1000, nu=.25, laws={"moment_z": law})
model.material.add_bar_parameter(1, BarParameter(1, 1, 1, 1))
model.add_nonlinear_bar_element(7, [1, 2], 1, 1)
model.add_restraint(1, True, True, True, True, True, True)
model.add_load(2, fx=-.5, mz=.075)
model.analysis_params.update(n_load_steps=2, max_iterations=30, tolerance=1e-10)
result = model.run("material_nonlinear")
section = result["section_response"][7]["center"]["z"]
assert isclose(section["Nd"], .5, abs_tol=1e-10)
assert isclose(section["moment"], .075, abs_tol=1e-10)
assert isclose(section["curvature"], .0005, abs_tol=1e-10)
```

`K4 = (P_4 - P_3) / (delta_4 - delta_3)`

`delta_4`到達後も勾配を変更せず、この直線を延長します。耐力ゼロでの打切りや`P_4`での下限制限は行わないため、十分遠くまで延長すると復元力の符号が反転し得ます。各側で以下を満たす必要があります。

復元力の符号が変位側と逆になった点から反転した場合も、反転点から`Kd`の直線を連続して進みます。その進行方向に復元力0の点がなければ最大変形点指向へは切り替わらず、同じ除荷直線を継続します。その直線上で再反転すると、同じ経路を反転点まで戻って骨格へ復帰します。

- `0 < delta_1 < delta_2 < delta_3`
- `0 < P_1 <= P_2 <= P_3`
- 勾配`K1 >= K2 >= K3 >= 0`
- `delta_4, P_4`を使う場合は両方を指定し、`delta_4 > delta_3`、`0 <= P_4 < P_3`
- すべて有限値。`beta >= 0`。

`delta_4, P_4`を両方省略した既存入力では、従来互換として`K4=0`、すなわち第3点以降を`P_3`一定とします。非対称入力では`delta_4_neg, P_4_neg`を指定できます。

`K_min`は**除荷剛性の下限**です。省略すると正負の初期剛性の小さい方の1%になります。指定する場合は`0 < K_min <= min(K1正, K1負)`です。骨格曲線の最終勾配を正にするパラメータではありません。

### 正負非対称の骨格

編集用JSONと`add_nonlinear_material()`では、正側は接尾辞のない`delta_1`・`P_1`など、負側は`delta_1_neg`・`P_1_neg`などを使います。負側も**正の大きさ**で指定します。

```json
{
  "nonlinear": {
    "type": "jr_stiffness_reduction",
    "symmetric": false,
    "delta_1": 0.001, "delta_2": 0.004, "delta_3": 0.010,
    "P_1": 10, "P_2": 16, "P_3": 22,
    "delta_1_neg": 0.002, "delta_2_neg": 0.008, "delta_3_neg": 0.020,
    "P_1_neg": 8, "P_2_neg": 14, "P_3_neg": 20,
    "beta": 0.4,
    "hysteresis_dofs": ["moment_z"]
  }
}
```

負側の省略項目は正側を使います。`symmetric: true`なら負側の指定値は使用しません。`delta_1_pos`などの`_pos`付き名称を使うのは、保存用JSONの`nonlinear_materials`や低水準のパラメータクラスです。入力形式を混同しないでください。

## 載荷の順序を指定する

`n_load_steps: 10`なら基準荷重の0.1倍から1倍までの10段階です。除荷・反転を含めたい場合は`load_factors`で順序を指定します。

```json
{
  "analysis_type": "material_nonlinear",
  "analysis_params": {
    "load_factors": [0, 0.5, 1, 0.5, 0, -0.5, -1, 0],
    "max_iterations": 50,
    "tolerance": 0.000001
  }
}
```

係数はその段階の**絶対倍率**です。基準荷重と基準強制変位が同じ係数で比例的に変化します。独立した複数の荷重パターンを段階ごとに切り替える機能ではありません。

0も負値も繰返し値も指定できます。通常の等間隔載荷には初期0段階を含めないため、必要なら配列の先頭へ0を入れます。`n_load_steps`と`max_iterations`は`load_factors`を使う場合も正の整数にします。

HTTP計算APIでは、1 caseあたりの非線形step数（`n_load_steps`、`load_factors`または変位制御`targets`の実効長）は最大1,000、`max_iterations`は最大1,000です。さらに全case合計で結果stateは10,000、非線形の「step数 × 最大反復回数」は500,000までです。上限は解析開始前に検査されます。Pythonで`FemModel.run()`を直接呼ぶ場合のsolver設定上限ではなく、同期HTTP requestの作業量上限です。

## 負勾配を変位制御で追跡する

`displacement_control`を指定すると、指定自由度の変位を段階ごとに固定し、基準荷重パターンの倍率`lambda`を未知数として釣合いを解きます。`load_factors`とは同時に指定できません。

```json
{
  "analysis_type": "material_nonlinear",
  "analysis_params": {
    "displacement_control": {
      "node": 30,
      "dof": "dx",
      "targets": [0.008, 0.020, 0.028, 0.036, 0.040]
    },
    "max_iterations": 50,
    "tolerance": 1e-9
  }
}
```

`dof`は`dx`, `dy`, `dz`, `rx`, `ry`, `rz`です。`targets`は絶対変位・回転の列です。等間隔なら`targets`の代わりに`target`を1値指定し、`n_load_steps`で分割できます。制御自由度は無拘束、基準荷重パターンは自由自由度上で非ゼロ、その他の強制支持変位はゼロである必要があります。

各段階の`lambda`が求まった荷重倍率です。変位が増えながら`lambda`が減れば、負勾配の耐力低下を追跡できています。通常の変位制御は単純な荷重極大点と負勾配を通過できますが、制御変位自体が戻るスナップバックには対応しません。その場合は弧長法が必要です。

## 実行例：曲げ・除荷・反転

以下は単独で実行できます。単位はN・mです。長さ2 mの梁の先端にモーメントを与えるため、曲げモーメントが一様になり、中央断面の曲率と入力した骨格を比較できます。

<!-- run: nonlinear-bending-cycle -->
```python
from math import isclose
from fem import FemModel, BarParameter

model = FemModel()
model.add_node(1, 0, 0, 0)
model.add_node(2, 2, 0, 0)
model.add_nonlinear_material(
    1, "Illustrative RC law", E=10000, nu=0.25,
    delta_1=0.001, delta_2=0.004, delta_3=0.010,
    P_1=10, P_2=16, P_3=22, beta=0.4,
)
model.material.add_bar_parameter(1, BarParameter(area=1, Iy=1, Iz=1, J=2))
model.add_nonlinear_bar_element(7, [1, 2], 1, 1, ["moment_z"], shear_correction=False)
model.add_restraint(1, True, True, True, True, True, True)
model.add_load(2, mz=12)
model.analysis_params.update(
    load_factors=[0, 0.5, 1, 0.5, 0, -0.5, -1, 0],
    max_iterations=50, tolerance=1e-9,
)
result = model.run("material_nonlinear")

for step in result["step_results"]:
    print(step["lambda"], step["curvature"][7]["z"], step["reaction_forces"][1]["mz"])

# M=12: 第2枝の勾配は(16-10)/(.004-.001)=2000
expected_curvature = 0.001 + (12 - 10) / 2000
assert isclose(result["step_results"][2]["curvature"][7]["z"], expected_curvature, rel_tol=1e-8)
assert len(result["step_results"]) == 8
assert result["converged"] is True
assert abs(result["curvature"][7]["z"]) > 1e-8  # 除荷後の残留曲率
model.save_results("nonlinear-result.json")
```

出力の各行は「載荷係数・中央曲率・固定端反力モーメント」です。係数1の段階の曲率は0.002 1/mです。最後の係数0でも、履歴を経た曲率は通常0には戻りません。曲率の符号は局所軸に基づき、材端力のi端・j端の符号とは区別します。

## 収束と履歴の扱い

曲げ則を指定した部材では、`section_response[要素ID]["center"]["y"または"z"]`に収束時の断面状態を保存します。各ステップと最終結果で利用でき、結果JSONへの保存にも対応します。

| 項目 | 内容 |
|---|---|
| `N`, `Nd` | 同じ試行状態の総軸力。Nは引張正、Nd=-Nは圧縮正 |
| `curvature`, `moment` | 中央曲率と曲げモーメント |
| `bending_tangent`, `effective_inertia` | 終端履歴枝の接線BとB/E |
| `shear_coefficient` | 終端枝の縮約係数C。増分全体の平均Cとは異なる |
| `shear_deformation`, `shear_force` | 一般化せん断変形と累積せん断力 |
| `branch`, `skeleton` | 履歴枝名と、収束Ndで補間した正負の骨格点。各点は`[曲率の大きさ, Mの大きさ]` |
| `interpolation` | Nd表使用時の`lower_Nd`、`upper_Nd`、区間内`fraction` |

`metadata.analysis.beam_formulation`は固定骨格で`jr_updated_inertia_v1`、Nd表使用時に`jr_axial_force_updated_inertia_v1`です。

K4=0の平坦枝ではBとCがともにゼロになります。回転だけの変位制御で継続できるかは横変位の拘束にも依存します。負接線で`12B+GkAL²`が相対的にゼロになる場合は、局所縮約が特異になるため要素ID・軸・ステップ付きで停止し、直前の確定状態へ戻します。

Newton反復では、直前に収束・確定した履歴から試行状態を評価します。残差が下がるように変位増分を小さくする処理を含み、収束した段階だけを確定・保存します。

収束判定はモデルの代表長さを使い、力とモーメント、並進と回転を同じ次元へそろえます。
許容差は無次元の相対残差と相対増分に適用し、入力単位の固定絶対許容差は使いません。
N–m、N–mm、kN–mの換算方法と履歴項目は[モデルの入力](data-structures.md)と
[結果の読み方](results.md)を参照してください。

未収束時は`NonlinearConvergenceError`を送出し、その段階の変位・内力・要素履歴・載荷係数を直前の確定状態へ戻します。失敗段階の結果を成功結果として返しません。HTTPでは422になります。[エラーと対処](error-handling.md)に例外の扱いを示します。

`model.run()`をもう一度呼ぶと、新しい解析として履歴を初期化します。保存した結果を読み込んで非線形状態を再開する機能はありません。続きの履歴を計算したい場合は、それまでを含む係数列を指定して最初から実行してください。

詳しい段階出力・収束履歴は[結果の読み方](results.md)、コールバックを使う低水準APIは[解析ワークフロー](workflow.md)を参照してください。
