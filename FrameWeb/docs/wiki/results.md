# 結果の読み方

[Wikiホーム](index.md) · [HTTP API](endpoints.md) · [モデルの入力](data-structures.md) · [材料非線形解析](nonlinear-analysis.md)

HTTP成功結果は `AnalysisResultSet v1` だけです。Pythonの低水準 `model.run()` が返すsolver-native辞書とは異なり、HTTPではcase/state、共有topology、公開IDを持つ正規化済みsnapshotを読みます。

## ルート

| キー | 内容 |
|---|---|
| `kind` | 常に `analysis_result_set` |
| `schema_version` | 常に `1.0` |
| `units` | 入力の正規化済み単位宣言。推定・変換はしない |
| `coordinate_system` | 全体右手直交座標 `x,y,z` |
| `cases` | 入力順のcase情報とcase固有の支持節点 |
| `topology` | 全caseで共通の節点・部材・shell・solid幾何情報 |
| `results` | case-major、state index昇順のimmutable snapshot |

単位宣言がない場合は `system: consistent_user_defined`、各基本単位は `unspecified` です。回転はrad、応力はforce/length²、modal frequencyは宣言time単位の逆数です。

## ケースとstate

各resultは `(case_id, state.kind, state.index)` で一意です。

| 解析 | state | 件数 |
|---|---|---|
| 静解析 | `{"kind":"static","index":0}` | caseごとに1件 |
| 材料非線形 | `{"kind":"load_step","index":n,"load_factor":...,"is_final":...}` | 収束したstepごとに1件 |
| modal | `{"kind":"mode","index":n,"eigenvalue":...,"frequency":...,"degeneracy_group":...}` | modeごとに1件 |

非線形stepは `results` の独立した兄弟要素です。nested `step_results` や、最後のstepを重複させた外側の最終結果はありません。indexは0から連続し、最後のstepだけが `is_final: true` です。

modal resultは `node_mode_shapes` とmodal diagnosticsだけを持ちます。変位・反力・部材力・shell/solid応力を持ちません。

## 共有topology

`topology.nodes` は次を持ちます。

```json
{
  "node_id": "generated:7:S1",
  "coordinates": {"x": 0.5, "y": 0.0, "z": 0.0},
  "source_node_id": null,
  "generated": true
}
```

入力節点の `source_node_id` は元ID、荷重境界等で生成した節点はnullです。生成IDは内部solver IDではありません。

`topology.members` は元部材ID、端節点、右手局所frame、i端からの `S0..Sn` stationを持ちます。全caseの着目点・剛域・部材荷重境界の和集合を使うため、caseが違ってもstation identityは変わりません。

shell topologyは局所frameと唯一の `element_average` locationを、solid topologyは全体座標frameとformulation順の `GP0..GPn` natural coordinatesを宣言します。

## 静解析・非線形stepの物理量

すべての物理配列はtopology順で、対応するIDを過不足なくcoverします。対象topologyが空の配列は空で返します。

### 節点変位

```json
{
  "node_id": "2",
  "components": {"dx": 0.0, "dy": -0.0013, "dz": 0.0,
                 "rx": 0.0, "ry": 0.0, "rz": -0.001}
}
```

componentsは全体座標の `dx,dy,dz,rx,ry,rz` です。

### 支点反力

```json
{
  "node_id": "1",
  "components": {"fx": 0.0, "fy": 1000.0, "fz": 0.0,
                 "mx": 0.0, "my": 0.0, "mz": 2000.0}
}
```

`support_reactions` は所有caseの `support_node_ids` だけを同じ順序でcoverします。2D化のために自動追加した補助拘束はcase supportに含みません。

### 元部材の断面力

```json
{
  "member_id": "7",
  "segments": [{
    "segment_id": "S0-S1",
    "station_i": "S0",
    "station_j": "S1",
    "length": 0.5,
    "i_end": {"fx": 10, "fy": 0, "fz": 0, "mx": 0, "my": 0, "mz": 0},
    "j_end": {"fx": 10, "fy": 0, "fz": 0, "mx": 0, "my": 0, "mz": 0}
  }]
}
```

部材局所frameで、各station間を独立segmentとして返します。点荷重位置の左右端値を同じ値へ潰さないため、`i_end` と `j_end` を分離しています。`rate`による解析後乗算は行いません。

### shell

各shellは `element_average` について次を返します。

- `membrane_force`: `nx,ny,nxy`
- `bending_moment`: `mx,my,mxy`
- `transverse_shear`: `qx,qy`
- `top_stress` / `bottom_stress`: `sx,sy,txy`

値はtopologyに記録した局所frameで表す、積分測度重み付き要素平均です。

### solid

各 `GPn` locationで全体座標のstress `sx,sy,sz,txy,tyz,tzx` とstrain `ex,ey,ez,gxy,gyz,gzx` を返します。結果行のIDと順序はtopologyの `result_locations` と完全一致します。

## diagnostics

静解析は `{"warnings": []}` を持ちます。非線形stepはさらに、そのstepに属するiteration列を持ちます。

```json
{
  "warnings": [],
  "iterations": [
    {"index": 0, "residual_norm": 0.01,
     "correction_norm": 0.001, "converged": true}
  ]
}
```

iterationはstepごとに0から始まり、累積履歴を別のresultへ重複させません。

modal diagnosticsは `normalization: mass`、solverと同じpositive-eigenvalue tolerance、`degeneracy_relative_tolerance: 1e-8` を持ちます。modeは固有値昇順、質量正規化、縮退部分空間と符号の決定規則を適用済みです。モード形は実荷重による変位ではありません。

### 橋面荷重の配分確認

橋面の線荷重・面荷重がある静解析では、`diagnostics.spatial_loads`を追加します。
橋面荷重がない結果ではこのキーを省略し、非線形step・modal結果には含めません。
各値はそのケースの実際のソルバー荷重組立から取得します。

| キー | 内容 |
|---|---|
| `resultant`, `moment` | 積分した合力、全体座標原点回りのモーメント。`{x,y,z}` |
| `nodal_resultant`, `nodal_moment` | 配分後の節点力から確認した合力とモーメント。`{x,y,z}` |
| `force_error`, `moment_error` | 積分値と配分後の値の差の最大絶対成分 |
| `node_loads` | `{node_id, force:{x,y,z}}`の配列。節点番号は共有topologyの公開ID |
| `loads` | 荷重ごとの`load_id`, `panel_id`, `feature`、上記4ベクトル・2誤差、`integrated_length`, `clipped_area` |

`feature`は`spatial_line`または`spatial_area`です。長さ・面積・力・モーメントはモデルで宣言した整合単位を使います。
`node_loads`は通常荷重を含めず、生成節点も公開IDで参照します。全体座標の並進荷重で、節点偶力はありません。
これは入力へ再投入する荷重ではなく、表示・印刷のための配分確認値です。
未知キー、重複荷重ID・節点ID、存在しないtopology節点、非有限値、負の長さ・面積・誤差は不正な診断として拒否します。

## 読み取り例

```python
case = result_set["cases"][0]
static = next(
    result for result in result_set["results"]
    if result["case_id"] == case["case_id"]
    and result["state"]["kind"] == "static"
)
displacements = {
    row["node_id"]: row["components"]
    for row in static["node_displacements"]
}
print(displacements["2"]["dy"])
```

配列順が契約です。case/state順をobject keyの列挙順へ置き換えないでください。未知field、重複ID、topology coverage不足、NaN/Infinityを含む成功payloadは無効です。
