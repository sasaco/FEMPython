# JRSNAP と FrameWeb の静的非線形解析結果の比較記録

2026-10-19。ブランチ `sasa/nonli`、開始 HEAD `78e7754`。作業依頼は root の `HANDOFF.md`、計画は `.claude/docs/plans/jrsnap-frameweb-equivalence.md`。

判定は 3 値である。**一致**（記した許容差の範囲内）、**不一致**（測った差を記す）、**未確認**（欠けているものを記す）。期待値は JRSNAP の凍結結果（`tests/data/jrsnap_golden/*/golden_*.json`、`.DB1` から SNAPDBLib で取り出した倍精度値）、手計算、閉形式のいずれかで、FrameWeb の出力から作った期待値はない。製品コード（`src/fem`）は変更していない。

## 1. 対象範囲の確認

2026-10-19 に利用者へ尋ね、次の回答を得た（AskUserQuestion）。

| 項目 | 回答 |
|---|---|
| JRSNAP 互換支持ばねの未確認項目（荷重増分制御、負側、反転・繰返し、バイリニアの実計算、複数部材） | 対象 |
| 部材の材料非線形（M–φ、JR RC 剛性低減 K1–K4、軸力依存） | 対象 |
| 支持ばねと部材の材料非線形の組合せ | 対象 |
| JRSNAPHeadless（別リポジトリ）への `.ndt` 直接入力の追加 | 許可 |
| 以前ローカルにあった JR-SNAP 5.3 の beam001 類似 `.OUT`/`.FMT` | ない／不明 |

対象外（HANDOFF.md のまま）: FrameWebforJS/FrameWebforCS の UI と structural-mcp 契約、動的解析、KR2/KR3 自動算定と αf、`C:\RTRI_Program` の変更、製品挙動を JRSNAP に合わせる変更。

## 2. 環境と出典

- JRSNAP 5.2-L03（JRSNAP_Input.exe 5.2.3.1、`C:\RTRI_Program\JRSNAP(Ver5.2-L03)`）。解析は JRSNAPHeadless（`.ndt` 直接入力を追加した版。`bin\Release` を更新済み、単体試験 70 件成功）で行い、`.DB1` は `SNAPDBLib.dll`（Aggre-SE Release）で 32 ビット PowerShell から読んだ。
- マニュアルの引用は `C:\RTRI_Program\JRSNAP(Ver5.2-L03)\JRSNAP_操作説明書.pdf` の印字ページ番号（付2-xx）。
- FrameWeb は本記録のコミットのソース。既存の golden（2026-10-18、`jrsnap_golden_v1`）は無変更で、凍結済み `.ndu` を再実行して値が同一であることを確かめた（`tests/data/jrsnap_golden/README.md`「再生成」）。
- 入力・結果・再生成コマンドは `tests/data/jrsnap_golden/README.md`。各フォルダーに `.ndu`、書出し器の `.ndt`、解析した `.ndt`、`golden_*.json`、`.LOG` がある。
- 試験: `tests/integration/test_jrsnap_equivalence_linear.py`、`_support.py`、`_member.py`（`oracle` マーク、golden のみを読む）。`tests/harness/test_jrsnap_golden.py` が入力ハッシュ、`tests/harness/test_jrsnap_tools.py` が `.ndu`・`.ndt` の再生成を確かめる。

## 3. 許容差（比較前に確定）

比較を始める前に次の許容差を決め、以後は緩めていない。

| 量 | 許容差 | 根拠 |
|---|---|---|
| 線形校正（Step 1）の変位・回転・部材端力・反力 | 相対 1e-8（0 になるべき力は 1e-9 kN） | 線形解析の倍精度解。JRSNAP の剛支点は約 1e15 のばねで、1e-12 程度の痕跡が残る |
| ばね則の再生（JRSNAP の節点変位経路を FrameWeb の法則へ与える） | 相対 1e-12（絶対 1e-12） | 区分線形の閉形式を倍精度で評価する。既存試験と同じ |
| 支持ばねの全体解の力（荷重係数×荷重、ばね力、部材せん断力、反力）: 1 ばね＋剛部材 | 1e-4 kN | JRSNAP 自身のせん断力とばね力の差が 4e-5 kN（golden README）。既存試験と同じ |
| 支持ばねの全体解の力: `support_multi01`（3 ばね、剛部材 2 本） | 1e-4 kN | 部材の伸び L/(EA)·N は FrameWeb の EA=2.65e9 kN に対し N≈30 kN で 1e-8 m、K1·Δu≈1e-5 kN で、JRSNAP 内部の差 4e-5 kN が支配する |
| 制御節点の変位 | 1e-9 m | JRSNAP は目標値に 4e-11 m 以内（golden README、ケース 03） |
| FrameWeb モデルのばね節点の変位（制御節点以外） | 1e-7 m | FrameWeb の部材は E=1e8（1 ばね）または EA=2.65e9（3 ばね）で、その曲げ・伸びが最大 3e-8 m |
| 剛支点の変位 | 絶対 1e-12 m | JRSNAP の剛支点は約 1e15 のばね |
| 区分（`bane_flg`、`SCFlg`） | 完全一致 | 整数 |
| 荷重増分（精度優先）で JRSNAP の不釣合い力 \|P − ばね力\| が 1e-4 kN 以下のときの変位 | 相対 1e-6 | FrameWeb の収束 tol 1e-10、JRSNAP の倍精度。不釣合いが残るときは変位を比べず「不一致（JRSNAP の残差 X kN）」とする |
| 部材 M–φ の曲率・回転・変位（FTM=1e-8 の実行） | 相対 1e-6 | FTM が剛性低下率の収束誤差を抑える。FrameWeb の tol は 1e-10 |
| 部材 M–φ の曲率・回転・変位（FTM=0.001 の実行） | 相対 2e-3 | FTM=0.001 は剛性低下率の許容誤差 0.1%（付2-12、7-3）であり、その 2 倍を上限とする |
| 部材のモーメント（静定でモーメント一定の載荷） | 1e-6 kN·m | 荷重の釣合いだけで決まる。線形校正で JRSNAP は 1e-14 で一致 |

FTM=1.0E-08 を `.ndt` に書いた実行は、FTM=0.001 の実行と結果・`.LOG` の計算回数ともに同一だった（`member_mphi01_moment` を両方で解析して比較、§4.4）。したがって部材の判定には FTM=0.001 の行（相対 2e-3）を用いる。

## 4. 比較の記録

各行は（機能 × ケース × 量 × 段階範囲）。JRSNAP 版はすべて 5.2-L03、制御は「荷重」＝荷重増分、「変位」＝変位増分、収斂は「速度」＝列70 空白、「精度」＝列70 `1`。「FrameWeb」列の値は本記録のコミットの FrameWeb。

### 4.1 Step 1 線形校正（`linear01_cantilever`、`linear01_cantilever_moment`、`linear02_moment_disp`）

モデル: 片持ち柱 L=1 m（節点 1 (0,−1) 先端、節点 2 (0,0) 剛支点 X・Y・回転）、E=25（`.ndu`）、A=0.0007、I=1e-5。EI=250 kN·m²。全非線形フラグ off。10 段階。試験: `test_jrsnap_equivalence_linear.py`。

| ケース | 量 | 制御 | 許容差 | 最大差（段階） | 判定 | 備考 |
|---|---|---|---|---|---|---|
| linear01_cantilever（先端 X 10 kN） | 先端 dx vs PL³/3EI=0.0133333… | 荷重・速度 | 相対 1e-8 | 1.5e-12（10） | 一致 | せん断変形なし（せん断項 PL/GA≈10% は現れない）。E=25 → DB `member_info.E`=25000000 |
| 同 | 先端 rz vs PL²/2EI=0.02 | 同 | 相対 1e-8 | 5e-13 | 一致 | |
| 同 | Mi, Si, Mj, Sj vs (0, 10, 10, 10) | 同 | 相対 1e-8 / 1e-9 kN | 7e-15 | 一致 | 符号対応: Si=−i端Vy、Sj=+j端Vy、Mi=+i端Mz、Mj=−j端Mz（FrameWeb の局所端力） |
| 同 | 反力 vs FrameWeb | 同 | 相対 1e-8 | 1e-15 | 一致 | 支点行 1（節点 2 X）の `jiban_hanryoku`=+10 ＝ −（FrameWeb 反力 fx） |
| 同 | FrameWeb dx, rz | 同 | 相対 1e-8 | 1.5e-12 | 一致 | FrameWeb は G なし（せん断変形なし）で一致 |
| linear01_cantilever_moment（先端 M 10 kN·m） | 先端 dx vs ML²/2EI=0.02、rz vs ML/EI=0.04 | 荷重・速度 | 相対 1e-8 | 5e-13 | 一致 | 節点モーメント荷重（JLoadInfo 第 7 項 PM）は使える |
| 同 | Mi=Mj=10、Si=Sj=0 | 同 | 1e-9 kN | 1e-14 | 一致 | |
| linear02_moment_disp（モーメント荷重パターン + 先端 X 変位増分 0.04 m） | JRSNAP の挙動 | 変位・速度 | — | — | **比較不能（JRSNAP 固有の制御）** | 測定: 各段階で λ=`lo_step_no`/MDV の荷重パターン（1, 3, 5, 7, 7, 7, 7, 7, 7, 7 → λ 0.1〜0.7）に加え、制御節点に水平力 F（Si: 1.5, 1.5, 1.5, 1.5, 4.5, 7.5, …, 19.5 kN）が作用し、dx = 0.02λ + F/750、rz = 0.04λ + F/500 が相対 1e-6 で成り立つ。FrameWeb の変位制御はパターンのみを λ 倍する（dx=0.04 なら λ=2、せん断 0）ため同一条件にならない。荷重パターンが制御自由度と同じ方向のときは、付加力がパターンと合流するので総力で比べられる（§4.2〜4.3 はそうしている） |

Step 1 で確定した事実:

1. `.ndu` の E=25 は 2.5e7 kN/m²（DB の `member_info.E`）。
2. JRSNAP の棒要素にせん断変形はない（A=0.0007 でも PL³/3EI に 1.5e-12 で一致）。
3. 節点モーメント荷重は `JLoadInfo` の第 7 項（`.ndt` JOINT LOAD の 36〜45 桁）で与えられる。
4. 部材端力の符号対応: JRSNAP (Mi, Si, Mj, Sj) = (+Mz_i, −Vy_i, −Mz_j, +Vy_j)。N は引張正で (Ni, Nj) = (−N_i, +N_j)（§4.3 で確定）。
5. 反力: 剛支点行の `jiban_hanryoku` は復元力（FrameWeb の反力の符号を反転したもの）。`JointItem` の長さは節点数+1 なので、支点行は節点数までしか読めない（節点 2 体のモデルでは行 1・2 のみ）。
6. 変位増分では JRSNAP は荷重パターンを整数段階（`lo_step_no`/MDV）で与え、残りを制御節点の水平力で補う。

### 4.2 Step 2 支持ばね（1 ばね＋剛部材）

モデルは 2026-10-18 の golden と同じ（節点 1 (0,−1) に荷重、節点 2 (0,0) に X ばねと Y・回転の剛支点、剛な部材 E=26.5・A=I=1000）。試験: `test_jrsnap_equivalence_support.py`。

| ケース | 量 | 制御 | 許容差 | 最大差（段階） | 判定 | 原因または欠落 |
|---|---|---|---|---|---|---|
| support_trilinear01_f1 n=100（+20 kN） | JRSNAP の不釣合い力 \|P − jiban_hanryoku\| | 荷重・精度 | 1e-4 kN | 0.1428 kN（100） | 不一致 | 精度優先でも JRSNAP は不釣合い力を残す（速度優先 1.238 kN）。部材せん断力 Si は荷重に 1e-4 kN で一致し、残差はばねと部材の間にある。`.LOG`: 試行回数 最大 36 |
| 同 | 節点 2 dx vs FrameWeb 荷重制御（同じ荷重係数） | 同 | 相対 1e-6（残差なしのとき） | 2.86e-3 m、相対 1.94%（100）: JRSNAP 0.147144、FrameWeb 0.15 | 不一致 | 上の残差 0.143 kN を K3=50 で割った 2.9e-3 m と一致する。原因は JRSNAP の収斂判定（剛性低下率、力の残差を修正しない） |
| 同 | 報告ばね力 vs 正側の骨格（JRSNAP の dx を法則へ） | 同 | 相対 1e-12 | 1.9e-16（93） | 一致 | 法則は同じ。全体解の差は不釣合い力による |
| support_trilinear01_f1 n=1000 | 不釣合い力 | 荷重・精度 | 1e-4 kN | 0.1464 kN（1000） | 不一致 | 速度優先 0.380 kN。試行回数 最大 10 |
| 同 | 節点 2 dx vs FrameWeb | 同 | 相対 1e-6 | 相対 1.99%（1000）: 0.147071 vs 0.15 | 不一致 | 同上 |
| support_trilinear02_neg_f1（−10 kN） | 報告ばね力 | 荷重・精度 | — | 報告値は F_neg(2\|dx\|)（相対 1e-12 で一致）、\|荷重\| − \|報告値\| = 3.258 kN（100） | 不一致（JRSNAP の報告値、設計上再現しない） | 2 倍の報告は精度優先でも変わらない。Si は −10 kN に 1e-4 kN で一致 |
| 同 | 節点 2 dx vs FrameWeb | 同 | 相対 1e-6 | 相対 4.99%（100）: −0.17145 vs −0.18 | 不一致 | 荷重増分の残差 |
| support_trilinear04_neg_disp（−0.1 m、速度）と `_f1`（精度） | FrameWeb の負側の法則（JRSNAP の dx を与える）vs 部材せん断力 Si | 変位・速度／精度 | 1e-4 kN | 1.64e-5 kN | **一致** | 構造が受ける力は負側の骨格 F_neg(\|dx\|)。報告値 `jiban_hanryoku` は F_neg(2\|dx\|)（相対 1.9e-16）で、報告値だけが 2 倍（Open Question 3 への情報） |
| 同 | 区分 `bane_flg` vs FrameWeb の枝 | 同 | 完全一致 | 0 | 一致 | −1 は段階 11（\|dx\|=0.011 > 0.01）、−2 は段階 31（> 0.03） |
| 同 | FrameWeb 変位制御: λP と Si、ばね力と Si | 同 | 1e-4 kN | 1.58e-5 kN | 一致 | |
| 同 | 制御節点 dx | 同 | 1e-9 m | < 1e-9 | 一致 | |
| 同 | ばね節点 dx | 同 | 1e-7 m | 2.79e-8 m | 一致 | FrameWeb の部材 E=1e8 の曲げ |
| 同 | 速度優先と精度優先の結果 | — | 同一 | 全段階で同一 | 一致 | |
| support_bilinear01（通常ばね行、+0.05 m） | JRSNAP の報告ばね力 vs min(K·dx, F1) | 変位・精度 | 相対 1e-12 | 0 | 一致 | 降伏後の勾配は厳密に 0。DB の保存値 K2_S=K3_S=0、Rmit2_S≈1e15。「0 は 0.01」の置換は起きない |
| 同 | FrameWeb `jrsnap_bilinear` の法則再生 | 同 | 相対 1e-12 | 0 | **一致** | 枝 elastic→plateau_pos が `bane_flg` 0→1（段階 21）に対応 |
| 同 | FrameWeb 変位制御: λP と Si、ばね力と報告値 | 同 | 1e-4 kN | 3.31e-5 kN | 一致 | |
| 同 | 制御節点 dx／ばね節点 dx | 同 | 1e-9 / 1e-7 m | < 1e-9 / 3.3e-8 | 一致 | |

### 4.3 Step 2d 複数ばね・複数部材（`support_multi01`）

節点 1 (0,0)・2 (1,0)・3 (2,0)、剛な部材 1–2・2–3（JRSNAP E=26.5・A=I=1000、FrameWeb E=2.65e7・A=100・I=1000）、支点行 1〜3 が節点 1〜3 の X ばね（トリリニア A、+SP3=−20 のトリリニア、通常ばね）、行 4〜9 が Y・回転の剛支点。荷重 X 30 kN を節点 3、節点 3 を +0.1 m まで 200 段階。精度優先。

| 量 | 許容差 | 最大差（段階） | 判定 | 備考 |
|---|---|---|---|---|
| 各ばねの報告力 vs 法則再生（各節点の dx） | 相対 1e-12 | < 1e-12 | 一致 | 行添字は支点データ行（`lengths.joint_item`=4 で行 1〜3 が読める） |
| 各ばねの区分 | 完全一致 | 0 | 一致 | 最終 (2, 2, 1) |
| FrameWeb のばね力 vs JRSNAP 報告力 | 1e-4 kN | 9.6e-6 kN（19） | **一致** | |
| λ·30 vs Σ報告力 | 1e-4 kN | 1.6e-5 kN（19） | 一致 | |
| 軸力 N（部材 1 = F1、部材 2 = F1+F2） | 1e-4 kN | 1.6e-5 kN | 一致 | JRSNAP の Ni=Nj は引張正、FrameWeb の局所 N は i 端 −N・j 端 +N |
| ばね節点 dx（節点 1・2） | 1e-7 m | 1.2e-8 m | 一致 | |
| 制御節点 dx（節点 3） | 1e-9 m | JRSNAP が目標に 4.35e-9 m 届かない（200）。FrameWeb は目標に 1e-12 | 不一致（JRSNAP 側の目標到達誤差） | 許容差は緩めず、差をそのまま記す。ケース 03 では 4e-11 だった |

### 4.4 Step 3 部材の材料非線形

共通の骨格: M1..M4 = 10, 16, 22, 14 kN·m、φ1..φ4 = 0.001, 0.004, 0.010, 0.018（`jr_k4_displacement_control.json` と同じ点、K4=−1000）。JRSNAP は BLOCK7 固定 M–φ、A=1000、I=0.0004、E=25（EI=M1/φ1=1e4）。FrameWeb は `jr_stiffness_reduction` に同じ 4 点、`hysteresis_dofs=['moment_z']`。試験: `test_jrsnap_equivalence_member.py`。

| ケース | 量 | 制御 | 許容差 | 最大差（段階） | 判定 | 原因または欠落 |
|---|---|---|---|---|---|---|
| member_mphi01_moment（先端モーメント 21 kN·m、1 要素） | FrameWeb の断面（曲率、回転 κL、変位 κL²/2）vs 手計算の骨格 | 荷重 | 相対 1e-10 | < 1e-10 | 一致（FrameWeb と手計算） | |
| 同 | JRSNAP の `Outou_M` vs 荷重 | 荷重・精度 | 1e-6 kN·m | 4e-14 | 一致 | Mi=Mj=Outou_M=λ·21 |
| 同 | JRSNAP の曲率 `Outou_FAI_Kaiseki` vs 骨格（＝FrameWeb） | 同 | 相対 2e-3 | 0.395%（48、M=10.08）。最終段階 0.086% | **不一致** | `.LOG`: 剛性低下率の試行は 1 段階に最大 10 回で「未収斂」（段階 49〜51）。FTM を 1.0E-08 にしても結果・試行回数とも同一 |
| 同 | 先端 rz vs κL（＝FrameWeb） | 同 | 相対 2e-3 | 0.497%（48） | 不一致 | 同上 |
| 同 | `SCFlg` | 同 | 完全一致 | — | 対応表のみ | 1: M2（降伏）まで、2: M2 以降。第 1 折れ点（M1）では変わらない。FrameWeb の枝 initial→skeleton は M1 で変わる |
| member_mphi02_disp（基部 0.1 m 要素、先端 X 変位増分 0.002 m） | JRSNAP の評価位置 | 変位・精度 | — | `Outou_M` = Mj（基部側の端のモーメント、Mi=0.9Mj）、`FAI` = rz2/(0.95·0.1)（相対 3e-6） | 測定事実 | JRSNAP は要素端の最大モーメントで評価し、曲率は直線分布の端の値。FrameWeb は要素中央（M=0.95·V）で評価し曲率一様（rz2/0.1） |
| 同 | 節点 2 の回転 rz2 vs FrameWeb | 同 | 相対 2e-3 | 7.4e-6 | 一致（運動学） | 上部要素が剛なので先端変位から決まる。法則の一致ではない |
| 同 | せん断力 V（λ·10 vs Si） | 同 | 1e-4 kN | 相対 9.2%（100）、頂点付近 5%（48） | **不一致** | 評価位置の違い（上の測定事実）。K2 の区間（段階 10）では JRSNAP も Mj = 骨格(FAI)、FrameWeb も M_mid = 骨格(κ_mid) で、各自の点で法則に乗っている |
| 同 | 曲率 | 同 | 相対 2e-3 | JRSNAP は FrameWeb の 1/0.95 倍（5.3%） | 不一致 | 同上 |
| 同 | 下降域の到達 | 同 | — | 頂点 21.904 kN（48）→ 10.574 kN（100）、`SCFlg` 4 | 測定事実 | |
| member_beam001（beam001 配置、固定 M–φ 1000/3000/5000/3000、P=500 kN） | JRSNAP の `Outou_M` | 荷重・精度 | — | Mj2 = P·9.9 = 4950（100）、Mi2 = 4900 | 測定事実 | 過去の記録「4900 vs 4850」の JRSNAP 側は 4950（要素端）が該当する。FrameWeb の中央は 4925 |
| 同 | JRSNAP 曲率 vs 骨格(Outou_M) | 同 | 相対 2e-3 | 1.10%（61、M2 直後） | 不一致 | JRSNAP 自身の骨格からのずれ（`.LOG` 未収斂 20 段階） |
| 同 | 先端 dx vs FrameWeb | 同 | 相対 2e-3 | 4.5%（61）、最終 1.0%（0.00096764 vs 0.00095787） | 不一致 | 評価位置（端 vs 中央）と上のずれ |
| 軸力依存 | — | — | — | — | **未確認** | JRSNAP の固定 M–φ（BLOCK7）は軸力変動に追従しない（付2-32）。追従する BLOCK8 自動算定は対象外 |
| 除荷・履歴 | — | — | — | — | **未確認** | JRSNAP に反転・繰返しの入力がない（付2-12〜17、付2-120〜123）。`SCFlg` と FrameWeb の枝の対応だけ上に記した |

### 4.5 Step 4 組合せ（`combined01`）

member_mphi01 の基部（節点 2）に回転トリリニアばね（K1=5000 kN·m/rad、±RMiT1=8、K2=1000、±RMiT2=12、K3=100、支点行 1）を置き、先端モーメント 21 kN·m を荷重増分 100 段階、精度優先。

| 量 | 許容差 | 最大差（段階） | 判定 | 原因 |
|---|---|---|---|---|
| FrameWeb の断面曲率とばねモーメント vs 各骨格 | 相対 1e-10 | < 1e-10 | 一致（FrameWeb と手計算） | |
| JRSNAP の Mi vs 荷重 | 1e-6 kN·m | < 1e-6 | 一致 | |
| JRSNAP の断面曲率 vs FrameWeb | 相対 2e-3 | 3.1e-4（48） | **一致** | 2e-3 以内。1e-6 ではない |
| JRSNAP のばねモーメント（行 1）vs 荷重 | 1e-4 kN·m | 0.454 kN·m（100）、2.2% | 不一致 | `.LOG`: 支点状態未収斂 147 回。荷重増分の不釣合い力（§4.2 と同じ現象） |
| 基部回転 rz2 vs FrameWeb | 相対 2e-3 | 4.8%（79） | 不一致 | ばね力の遅れを骨格で読み替えた回転差（0.454/100=4.5e-3 rad） |
| ばねの区分 | 完全一致 | — | 一致 | 1 は段階 39（8.19 > 8）、2 は段階 58（12.18 > 12） |
| 両非線形の同時進行 | — | 断面 `SCFlg` 2 は段階 77、ばね 2 は段階 58 | 測定事実 | |

## 5. 未確認の項目と理由

| 項目 | 理由 | 必要なもの |
|---|---|---|
| 反転・繰返し（支持ばねの除荷則、部材の履歴則） | JRSNAP の入力に荷重の反転・繰返しの項目がない（付2-12〜17、付2-120〜123）。LK=4 と LK=1 の荷重を同じ荷重ケースに重ねる案は、書出し器が同じ節点の行を 1 行にまとめるため成立しなかった（`--analyze false` の `.ndt` で確認） | 複数荷重ケースと組合せ（SNAPConb）を持つ `.ndu` の雛形、または提供元の情報 |
| 軸力依存の M–φ | JRSNAP の固定 M–φ は軸力変動に追従しない（付2-32）。追従するのは自動算定（BLOCK8）で対象外 | — |
| FTM（剛性低下率収斂係数）の効果 | `.ndt` に 1.0E-08 を書いても結果・試行回数が変わらない。読まれていないのか上限（試行 10 回）が先に効くのかは未確認 | 提供元の情報 |
| JRSNAP の部材要素の定式化 | `Outou_M`=端のモーメント、`FAI`=直線分布の端の曲率、回転=0.95·FAI·L（基部要素）までは測定したが、剛性低下率の反復手順と「未収斂」の扱いは未確認 | 提供元の情報 |
| 負側の報告値 F_neg(2\|dx\|) と −RMiT1 の保存値 0 が仕様か不具合か | 構造が受ける力は正しい（§4.2）。報告だけが 2 倍 | 提供元（RTRI）の回答 |
| モーメント荷重パターン + X 変位増分の比較 | JRSNAP が制御節点に水平力を加えるため同一条件にならない（§4.1） | — |

## 6. 回帰試験

- JRSNAP 関連スイート（HANDOFF の 7 ファイル + 新規 4 ファイル）: 195 passed（基準 140 + 新規 55）。既存 4 本の golden の検査内容は無変更。
- 新規試験の空虚でないことの確認: 6 つの golden（linear01、trilinear04、bilinear01、multi01、mphi01、combined01）の期待値を 1 箇所ずつ摂動すると、対応する試験が失敗した（各 1〜3 件、他は成功）。摂動後は元の値に戻した。
- 既存 golden の再現性: 凍結済み `.ndu` 4 本を再実行し、旧スクリプトの凍結結果と値が同一（`engineVersion` のコミット番号のみ相違）。
- JRSNAPHeadless: 単体試験 70 件成功（Stage1・Release）。`bin\Release` を更新。
- 全件ゲート `check_test_results`: §6.1 に記す。
- `git diff --check`: §6.1 に記す。

### 6.1 全件ゲートの結果

`uv --directory FrameWeb run --locked --extra dev python -m tools.validation.check_test_results`（コミット `78e7754` + 本作業の未コミット変更、Python 3.13.11、numpy 2.4.1、scipy 1.17.0、pytest 9.0.2）。

| 実行 | 収集 | 合格 | 失敗 | エラー | 所要 | 備考 |
|---|---|---|---|---|---|---|
| 2026-10-19 1 回目（09:57〜11:16） | 3,549 | 3,535 | 10 | 4 | 4,742 s | 本記録の摂動チェック（§6 の 2 点目）をゲートと並行実行したため、09:57:54〜09:58:30 の間だけ golden が摂動済みの内容で読まれ、`test_jrsnap_equivalence_linear` 1 件・`_support` 3 件が失敗した（失敗値は摂動量そのもの: linear01 の dx ×(1+1e-7)、trilinear04 の Si +2e-4）。摂動スクリプトは実行後に元の bytes を書き戻しており、終了後に golden の値が元に戻っていることを確認した |
| 2026-10-19 2 回目（11:19〜12:50、並行作業なし） | 3,549 | 3,539 | 6 | 4 | 5,473 s | 失敗・エラーは `tests/validation` の既知 10 件（`test_mesh_convergence` 4 エラー、`test_performance` 2 件、`test_unit_invariance` 4 件）のみ。HANDOFF の基準 3,484 合格 + 新規 55 件 = 3,539 で一致。pytest 終了コード 1 は既知失敗によるもの（`tests/regression/known_failures.json` は 0 件のため `check_test_results` 自体は不一致を報告する。これは HANDOFF 時点と同じ状態） |

- 新規 55 件: `tests/harness/test_jrsnap_golden.py`（登録追加）、`tests/harness/test_jrsnap_tools.py`、`tests/integration/test_jrsnap_equivalence_{linear,support,member}.py`。
- JRSNAP 関連 11 ファイル（HANDOFF の 7 + 新規 4）の単独実行: 195 passed（2 回目ゲートの前に再確認）。
- `git diff --check`: 終了コード 0（2 回目ゲート終了後に再実行）。
- 教訓: golden を書き換える検査（摂動チェック）は、golden を読む試験と同時に走らせない。
