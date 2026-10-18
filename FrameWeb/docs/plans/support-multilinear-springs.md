## Implementation Plan: JRSNAP互換 バイリニア・トリリニア支持ばね（弾塑性除荷）

作成日: 2026-10-18 / 調査時HEAD: `7488c8f`（ブランチ `sasa/calc`） / 改訂1: 独立レビュー（NEEDS_REVISION）を反映

### Purpose

FrameWebの非線形支点は、現在スリップばね（理論マニュアル7.19）の1種類だけである。JRSNAP Ver5.3の節点ばね（地盤ばね（節点）・BLOCK6）と同じ正負非対称のバイリニア／トリリニア支持ばねを追加し、材料非線形解析で地盤反力の塑性化（上限値）を扱えるようにする。除荷則は弾塑性除荷とする。

### Scope

**対応範囲（ユーザー確認済み）**: Python解析本体（FrameWeb）とJSON入出力。対象は、ばね則、旧`fix_node`・正規化JSON・Python APIの入力、Solverへの接続、結果メタデータと対応範囲、理論マニュアル・Wiki、試験。

**対象外**: FrameWebforJS／FrameWebforCSの画面、structural-mcpの`fempython-contract.ts`、`fix_member`（部材分布ばね）、動的解析。ほかに、初期ギャップ・摩擦・自由度間連成・局所座標ばね・2節点間ばねも対象外。JRSNAPの直接基礎回転ばね自動算定（KR2/KR3と基礎諸元からの算定、軸力変動）、αf割増し、応答変位法も扱わない。これらは利用者が事前に算定した値を入力する。

**JRSNAPとの対応（JRSNAP_Ver5.3操作説明書 2-126〜2-128、付2-26〜付2-32）**

| JRSNAP（BLOCK6） | FrameWebのキー | 備考 |
|---|---|---|
| 拘束方向 X／Y／回転 | `fix_node`欄 `tx`／`ty`／`rz`（6自由度では全6方向） | 全体座標。＋側はFrameWeb全体座標の正変位とする |
| ＋SP1、−SP1 | `K1_pos`、`K1_neg` | `K*_neg`はキーごとに省略でき、省略すると正側の値（JRSNAP注意⑤〜⑦と同じ） |
| ＋RMiT1、−RMiT1 | `F1_pos`、`F1_neg` | 力の大きさ（0以上）。必須で、既定値なし |
| ＋SP2、−SP2 | `K2_pos`、`K2_neg` | |
| ＋RMiT2、−RMiT2 | `F2_pos`、`F2_neg` | 必須 |
| ＋SP3、−SP3 | `K3_pos`、`K3_neg` | 負（下降勾配）と0を許可する。JRSNAPの「0.001で水平」は、FrameWebでは0を直接指定する |
| 通常の支点ばね（SP、±RMiT） | `type: "jrsnap_bilinear"`。キーはちょうど`type`、`K1`、`F1_pos`、`F1_neg` | 上限値に達した後は勾配0（完全弾塑性）。`K1`は正負共通 |

`jrsnap_trilinear`のキーは、`type`、`K1_pos`、`F1_pos`、`K2_pos`、`F2_pos`、`K3_pos`、`F1_neg`、`F2_neg`が必須で、`K1_neg`、`K2_neg`、`K3_neg`は任意とする。これ以外のキーは拒否する。保存時の正規形（`to_dict`）では、省略された`K*_neg`を解決して全キーを出力する。

入力例（旧`fix_node`のオブジェクトセル）:

```json
{"n": 2, "tx": {"type": "jrsnap_trilinear",
  "K1_pos": 1000.0, "F1_pos": 10.0, "K2_pos": 200.0, "F2_pos": 14.0, "K3_pos": -100.0,
  "K1_neg": 500.0,  "F1_neg": 5.0,  "K2_neg": 100.0, "F2_neg": 7.0,  "K3_neg": 0.0},
 "ty": 1, "rz": 1}
```

**変更するファイル（パスはすべて`FrameWeb/`配下）**

- 新規ファイル
  - `src/fem/nonlinear/hysteresis/jrsnap_support.py`: パラメータ・状態（frozen）、厳格パーサ、副作用のない`evaluate`、`to_dict`。スリップばねの`_at`による丸め誤差吸収は複製して使い、`slip.py`は変更しない。
  - `src/fem/nonlinear/support_laws.py`: 支持ばね則のレジストリ。`type`から、パラメータ型・パーサ・初期状態・評価関数・モデルID（`slip_v1`／`jrsnap_bilinear_v1`／`jrsnap_trilinear_v1`）を引く。生のdictとパラメータインスタンスの両方を受け付ける。
  - 試験（`tests/README.md`の規約に従い、各モジュールに`pytestmark`を付ける）
    - `tests/materials/test_jrsnap_support_hysteresis.py`（unit）
    - `tests/io/test_support_law_registry.py`（スリップの特性固定とレジストリ）
    - `tests/solvers/test_jrsnap_supports.py`
    - `tests/io/test_jrsnap_support_input.py`
    - `tests/integration/test_jrsnap_support_mixed.py`（混在要素）
    - `tests/regression/test_jrsnap_support_history.py`（保存した例題の履歴）
    - 共通ビルダ`tests/support/builders/jrsnap_support.py`
  - `docs/examples/jrsnap-trilinear-support.json`
  - `docs/report/jrsnap-support-implementation.md`
- 変更するファイル
  - `src/fem/nonlinear/support_springs.py`: スリップ固定の5箇所をレジストリ経由に変える（import :4、parse :8、初期状態 :31、`commit`の型検査 :51-53、snapshotの`type='slip'` :64）。文言（:44、:72）は、既存試験の正規表現`'[Ss]lip|nonlinear support'`（`tests/solvers/test_slip_supports.py:148`）に一致し続けるよう、小文字の"nonlinear support"を含める。
  - `src/fem/boundary_dofs.py:50,97`: `parse_slip_spring`をレジストリのディスパッチャへ置き換える。
  - `src/fem/file_io.py:21,713`: 保存時の正規化をレジストリの`to_dict`にする。旧`fix_node`（:486-494）と正規化入力（:172-180）は既存の`add_nonlinear_support`経由なので、ディスパッチを置き換えれば対応できる。
  - `src/fem/model.py:301-314,469,482-483`: 汎用の`add_nonlinear_spring_support(node_id, direction, definition)`を追加する。`add_slip_spring_support`の挙動は変えない。自動選択の説明文を一般化する。
  - `src/fem/solver_results.py:121-122`: `support_models`を、使っている則のモデルIDの昇順・重複なしにする。スリップだけのモデルは従来どおり`['slip_v1']`となる。
  - `src/fem/capabilities.json:13-24`、`tools/validation/render_capabilities.py:50`、生成物（`README.md`、`docs/wiki/elements.md`）。`capabilities.py`は`support_types`を読む事前検査がないため変更しない。
  - 理論マニュアル: `docs/理論マニュアル/08_材料非線形モデル.md`の末尾に7.22節として追加する。コード中の7.19〜7.21への参照がずれないよう、途中には挿入しない。
  - Wiki: `data-structures.md`、`nonlinear-analysis.md`、`python-api.md`、`file-formats.md`、`results.md`
  - 試験台帳: `docs/plans/test_inventory.md`、`docs/plans/test_items.md`
- 依存関係: なし（numpy／scipyの既存の範囲内）。`slip.py`、`equilibrium.py`、`solver.py`、`convergence.py`は変更しない方針とする。
- 不変条件
  - 旧`fix_node`の数値の意味（0は自由、1は固定、その他は`abs()`の線形ばね）を変えない。
  - 結果契約analysis-result-set-v1は閉じた契約で、JS／C#と共有している。公開結果行にフィールドを追加しない。
  - 人工剛性を入れない。機構になる場合は`StructuralMechanismError`で停止する。
  - スリップの出力・ハッシュ・`support_models`を変えない。エラー文言は既存試験の正規表現に一致し続けるようにする。
  - 既存の参照値・許容差を変更しない。
  - 正規形を全入力経路で同一にする。`solver.py:241`はパラメータを等価比較し、ハッシュは`model_to_jsonable`から作るためである。

### Implementation Steps

#### Step 1: 仕様の確定とユーザー承認（最も不確実。承認されるまでStep 2へ進まない）
- [ ] 下記の履歴則を、理論マニュアル7.22「JRSNAP互換 多直線支持ばね（弾塑性）」の草稿として書く。冒頭に「JRSNAPに記載がない除荷則はFrameWebが補った仕様である」と明記する。
- [ ] 手計算表を作る: トリリニア16点（下表）、バイリニア完全弾塑性の非対称例、軟化してゼロ力に至る経路、片側遮断（制限値0）の経路、閉ループの仕事。閉ループには、表の`δ=−.05`（p=−.036、m±=±.05）から`−.02`へ進み（`δ=−.026`で上限F1+=10に達する）、`−.05`へ戻る（`E=−10`が`L=−7`に当たる）経路を含める。この閉ループは経験最大点の内側に収まるが、両側で降伏するため仕事は正になる。
- [ ] 承認が必要な仕様決定（推奨値で草稿化済み）
  - (a) 劣化記憶D±を持つ（軟化後に耐力を回復させない）。
  - (b) 反対側の第1折れ点より手前で降伏した場合は第1制限値で平坦にする（K2を後方へ延長する移動硬化型は採らない）。
  - (c) 型名・キー名は上表のとおりとする。
  - (d) `jrsnap_bilinear`はJRSNAPの「通常の支点ばね」に限定する。
  - (e) 0の扱い（下記の入力制約）。
- [ ] 単調載荷の骨格がJRSNAPの図2-89と形状として一致することは、仕様の自己整合の確認と位置付ける。JRSNAPの実計算との照合ではない。

**入力制約（側s∈{+,−}ごと）**

- すべての値は有限の実数とする（boolは不可）。派生値`x1`、`x2`、`xz`と`K·x`も有限であることを検査する。
- `K1_s > 0`、`K2_s > 0`、`K2_s ≤ K1_s`、`K3_s ≤ K1_s`（負と0は可）とする。
- `F1_s ≥ 0`、`F2_s ≥ F1_s`とする。`F2_s = F1_s`なら`x2 = x1`（第2勾配区間なし）とする。
- 片側遮断: `F1_s = 0`は「その側は抵抗0」を意味する。トリリニアでは、`F1_s = 0`なら`F2_s = 0`かつ`K3_s ≤ 0`を要求し、`F1_s = 0 < F2_s`は拒否する。バイリニアは`F1_s = 0`だけで遮断となる。
- 両側とも遮断（全域で抵抗0）は拒否する。
- `K3_neg`を省略して正側の`K3_pos>0`を継承した結果、遮断側の`K3≤0`を満たさなくなる場合は、「`K3_neg`を明示してください」と案内するエラーにする。
- `K=0`は拒否する。JRSNAPの「0は非常に弱いばね（0.01）」は人工剛性にあたるため再現しない。代わりに上記の遮断形を案内するエラー文にする。

**履歴則（状態: ゼロ力位置p、最大変位m+／最小変位m−、直前の進行方向と接線）**

- 骨格の大きさ`Q_s(x)`（x ≥ 0）は次のとおり。`x1=F1/K1`、`x2=x1+(F2−F1)/K2`とする。
  - `[0,x1]`では`K1·x`
  - `[x1,x2]`では`F1+K2(x−x1)`
  - `x ≥ x2`では`max(0, F2+K3(x−x2))`
  - 下降勾配が力0に達する点`xz`以降は、力0・接線0とする（符号反転しない）。
  - 遮断側では`Q_s ≡ 0`とする。
- 劣化記憶: `D+ = Q+(m+)`（`K3+<0`かつ`m+>x2+`のとき）、それ以外は`+∞`とする。`D−`も同様。
- 上限と下限
  - `U(δ) = min(D+, F1+ if δ < x1+ else Q+(δ))`
  - `L(δ) = max(−D−, −F1− if δ > −x1− else −Q−(−δ))`
- 弾性部: `E(r)`は`r=δ−p`について、`r≥0`なら`K1+·r`、`r<0`なら`K1−·r`とする。除荷勾配は力の符号で選ぶ。
- 一回評価
  - 増加時は`F=min(E(δ−p), U(δ))`、減少時は`F=max(E(δ−p), L(δ))`とする。
  - 評価後に`p ← δ − E⁻¹(F)`、`m±`を更新する。
  - 接線は、上限・下限上ではその進行方向の片側微分（平坦部・力0・遮断側では0）、それ以外では力の符号に応じた`K1±`とする（`F=0`のときは進行方向側の値）。
  - `U`・`L`の傾きがそれぞれ`K1+`・`K1−`以下なので、一回評価は経路の分割に依存せず、常に`L ≤ F ≤ U`を保つ。
- 増分0かつ方向ヒント0のとき（収束時のランク検査や、無載荷の自由度での予測）
  - 確定状態とその接線をそのまま返す。
  - 未載荷の初期状態の接線は、遮断されていない側の`K1`の最小値とする。片側遮断でも、もう一方の側が支えていればランク検査を通る。
- 手計算の基準値（独立レビューで再計算済み）
  - 正側: `K1=1000, F1=10, K2=200, F2=14, K3=−100`
  - 負側: `K1=500, F1=5, K2=100, F2=7, K3=0`
  - δ経路と力: `.005→5`、`.02→12`、`.05→12`（p=.038）、`.044→6`、`.038→0`、`.03→−4`、`.02→−5`（p=.03）、`−.02→−6`、`−.05→−7`（p=−.036）、`−.04→−2`、`−.036→0`、`−.03→6`、`−.02→10`（p=−.03）、`.02→12`、`.03→12`（劣化記憶あり。なしなら14）、`.06→11`

**Verification**: 手計算表がコードから独立に導出されている。拒否条件のそれぞれに定式化上の理由がある。(a)〜(e)をユーザーが明示的に承認している。

#### Step 2: 純粋なばね則モジュール（技術リスク最大、TDD）
- [ ] 先に`tests/materials/test_jrsnap_support_hysteresis.py`を作り、失敗を確認する。
- [ ] `jrsnap_support.py`を実装する。`jrsnap_bilinear`は`x2=x1`、`K3=0`の特殊ケースとして同じ評価関数を使う。
- [ ] 厳格パーサ: Step 1の入力制約とキー集合に従い、位置付きのエラーにする。`K*_neg`は省略しても明示しても同じ正規形になる。

**Verification**:
- 手計算表の全行が一致する。単位倍率（1、1e-3、1e3）でパラメータ化して確認する。
- 単調載荷のδ格子で解析解の骨格と一致する（`x1`・`x2`・`xz`ちょうどの点を含む）。
- 乱数シードを固定したランダム経路で、`L ≤ F ≤ U`と経路分割不変性を確認する。
- 任意の閉ループで散逸`∮F dδ ≥ 0`となることを確認する。上限・下限に一度も触れない（常に`L < F < U`の）弾性閉ループでは仕事0、Step 1の閉ループでは手計算どおり正の仕事になることを確認する。「経験最大点の内側なら仕事0」はスリップでは成り立つが本則では成り立たないので、試験にしない。
- 評価後も確定状態が変わらない。増分0・ヒント0では確定状態を返し、未載荷時の接線が規則どおりになる。折れ点の片側接線は`direction_hint`で選ばれる。
- 枝の内部では有限差分で接線を確認する。
- 軟化して力0に至った側は不活性のままになる。遮断側では力0のまま`p`が移動する。
- 拒否条件の各ケースを網羅する。

#### Step 3: スリップの特性固定とレジストリ化
- [ ] 先にスリップの現状を固定する試験を書く: snapshot辞書、`model_to_jsonable`、`support_models==['slip_v1']`、例題の入力ハッシュ、エラー位置、`:44`と`:72`の文言（既存の正規表現と一致すること）。
- [ ] `support_laws.py`を追加し、`support_springs.py`、`boundary_dofs.py`、`file_io.py`をレジストリ経由に切り替える。`commit`の型検査は「各ばねの則の状態型」で行う。
- [ ] snapshotの`type`と`support_models`の一般化もこの段階で行う。後のStep 4〜5のFemModel実行で誤った`['slip_v1']`が出ないようにするためである。
- [ ] `support_response`の共通キー（`type`、`deformation`、`force`、`tangent`、`branch`）と、jrsnap則の枝名を次の7種に限定する: `elastic`、`plateau_pos`／`plateau_neg`（第1制限値の平坦部）、`skeleton_pos`／`skeleton_neg`（骨格の第2・第3勾配）、`zero_pos`／`zero_neg`（遮断側、または軟化して力0の側）。劣化記憶D±で頭打ちになった平坦部は`plateau_pos`／`plateau_neg`として報告する。

**Verification**: 特性固定試験と既存スリップ試験5ファイルが無変更で成功する。未知の`type`は位置付きのエラーになる。他の則の状態を混ぜた`commit`を拒否する。jrsnapを含むモデルの`support_models`が正しい。

#### Step 4: Solverとの結合（収束リスク）
- [ ] `BoundaryCondition`に生のdictを入れる方式（既存のスリップ試験と同じ）で、入出力の作業より先に荷重制御と変位制御を確認する。

**Verification**（`tests/solvers/test_jrsnap_supports.py`）:
- 並列梁で`P = kb·u + F(u)`を6方向・両方の制御で独立に解き、変位・反力・部材力と一致する。
- 軟化（`kb+K3>0`）が荷重制御で解ける。
- ばねだけで支えて下降し力0に至る経路を、変位制御で追跡できる。
- 平坦部・上限到達時の荷重制御の失敗は、`NonlinearConvergenceError`（スリップの既存試験`test_slip_supports.py:95-123`と同じ）であることと、`details`（節点・方向・`support_response`）まで確認する。rollbackが正しいことも確認する。
- 梁が安定させる片側遮断モデルが解ける。
- 無載荷のばね自由度（ヒント0）で、`K1+≠K1−`の場合と片側遮断の場合に、初期状態がランク検査を通る。
- 不採用のNewton候補が履歴を汚さない。
- 複数回の反転で振動しない。

収束に問題が出たら、共通反復（`equilibrium.py`）の変更は別途承認を得る。

#### Step 5: 入力経路とPython API
- [ ] 旧`fix_node`のオブジェクトセル、正規化JSON、`add_nonlinear_spring_support`を同じ正規化定義に揃える。
- [ ] 保存と再読込、FW3保存の拒否（既存の汎用処理）を確認する。

**Verification**（`tests/io/test_jrsnap_support_input.py`）:
- 3つの経路の定義が一致し、等価比較で等しい。
- 往復保存で定義が変わらない。
- `K*_neg`を省略しても明示してもハッシュが同じで、値を変えるとハッシュが変わる。
- 同一自由度の固定・線形ばね・スリップとの競合や、重複指定を拒否する。
- 旧数値`[0,1,.5,−2]`の互換性を保つ。
- 3自由度モデルの回転ばねを拒否する。
- 旧アプリ経路（`app/components/support.py:39`、`app/inputDataUtils.py:576`、`fem/models/fa_support.py:42`）の拒否試験にjrsnapのdictを加える。

#### Step 6: 対応範囲と生成文書
- [ ] `capabilities.json`に`jrsnap_bilinear`と`jrsnap_trilinear`を追加する。Step 7の完了までは`implemented`、完了後に`verified`とする。
- [ ] `render_capabilities.py:50`の説明文を一般化し、READMEとWikiを再生成する。

**Verification**: `tests/solvers/test_capabilities.py`の陳腐化検査が成功する。

#### Step 7: 混在要素と公開経路の検証
- [ ] 例題`docs/examples/jrsnap-trilinear-support.json`を作る（下降域は変位制御で追跡する）。
- [ ] `tests/integration/test_jrsnap_support_mixed.py`で、線形梁・JR梁・シェル・テトラとの混在と2次元モデル（`tx`／`ty`／`rz`）を確認する。
- [ ] `tests/regression/test_jrsnap_support_history.py`で、HTTP（平文・gzip）、ファイル再読込、ハッシュ、例題の履歴を確認する。

**Verification**: 各ステップの反力が`−F`と一致する。再読込したモデルの履歴が同一になる。例題が実行できる。

#### Step 8: 説明と全件回帰
- [ ] 理論マニュアル7.22（式、補完仕様、JRSNAPとの差異、手計算値）を書く。
- [ ] Wiki 5ファイル、試験台帳2ファイル、実装記録`docs/report/jrsnap-support-implementation.md`を書く。
- [ ] 全件回帰を実行する: `uv --directory FrameWeb run --locked --extra dev python -m tools.validation.check_test_results`（全件のpytestを含む）

**Verification**: 全件が成功する。参照値・許容差の変更がない。`git diff --check`が通る。

### Risks & Considerations

- **除荷則はJRSNAPに記載がない**: 反転を含む結果はJRSNAPと一致しない可能性がある。JRSNAPと一致すると言えるのは単調載荷の骨格だけで、反転時の挙動はFrameWebの補完仕様として理論マニュアルと対応範囲の説明に明記する。
- **JRSNAPの0は「非常に弱いばね（0.01）」**
  - FrameWebは人工剛性を入れないので、`K=0`は拒否し、遮断は`F=0`の形で表す。
  - 遮断側ではゼロ力位置が移動するため、ギャップの記憶は持たない（ギャップは対象外）。
  - JRSNAPでは大変位になる条件が、FrameWebでは機構エラーになりうる。
  - JRSNAPの0.01は単位系に依存するため、厳密な数値一致は原理的に得られない。この違いを明記する。
- **接線0・負の接線**: 荷重制御では平坦部や下降域で収束しない。これは意図した挙動で、下降域には変位制御が必要と文書化し、失敗時の診断を残す。
- **変位制御には線探索がない**（`equilibrium.py:520-534`）: 非対称なK1の折れ点で反復が振動するおそれがある。Step 4で多回反転の試験を早めに行う。
- **スリップへの回帰**: 特性固定試験を先に置き、`slip.py`は変更しない。エラー文言は既存の正規表現と一致させる。
- **折れ点での丸め誤差**: 線分を折れ点の力で定め、`_at`で丸め誤差を吸収し、`x1`・`x2`・`xz`ちょうどの点を試験する。
- **Masing型7.1／7.5との混同**: `type`名に`jrsnap_`を付け、理論マニュアルも別節（7.22）とする。
- **ワークフロー上の注意**: `.agents/`は作業ツリーで削除されていたため、この計画は本リポジトリの慣例（`FrameWeb/docs/plans/`）に置いた。Codex CLIは設定モデルがChatGPTアカウントで使えず2回失敗したため、手順の分解と妥当性検証は独立したPlanエージェントで代替した。

### Open Questions

仕様を左右する事項はStep 1の承認項目(a)〜(e)に移した。以下は実装の妨げにならない。

1. **JRSNAPとの照合データ**: 反転を含むJRSNAPの実計算結果があれば、検証記録に追加したい（任意）。
2. **対応範囲の到達状態**: 今回の納品で`verified`とする（Step 7が通れば）。
