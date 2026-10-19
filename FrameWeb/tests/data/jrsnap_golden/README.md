# JRSNAP golden（JRSNAPの実計算から凍結した基準値）

JRSNAPで実際に解析した結果を、`SNAPDBLib.dll`で結果データベース（`.DB1`）から全段階そのまま取り出し、JSONに凍結したものである。値は丸めず、JRSNAPが倍精度で保存したままとしている。

FrameWebの試験はこのJSONと比べるだけで、JRSNAPは不要である。再生成には、JRSNAPと`JRSNAPHeadless`のあるWindowsが必要になる。整合性とJRSNAPの挙動は`tests/harness/test_jrsnap_golden.py`で確認する。

## 共通のモデル

すべてJRSNAP 5.2-L03（JRSNAP_Input.exe 5.2.3.1）で2026-10-18に解析した。

| 項目 | 内容 |
|---|---|
| モデル | 2次元。剛な線形部材1本（節点1 (0,−1)〜節点2 (0,0)、`E=26.5`、`A=I=1000`はbeam001と同じ） |
| 支点 | 節点2: Y・回転は剛支点、Xはトリリニア節点ばね（BLOCK6の1行目） |
| 荷重 | 節点1のX方向 |
| 解析フラグ | 水平ばね塑性化9（考慮しない）、材料非線形化9（線形）、支点（節点ばね）非線形化0（考慮する）。剛性低下率収斂係数0.001、収斂方法は既定（列70が空白＝速度優先） |

| ケース | ばね（+SP1, +RMiT1, −RMiT1, +SP2, +RMiT2, −RMiT2, +SP3, −SP1, −SP2, −SP3） | 載荷 | ファイル |
|---|---|---|---|
| `support_trilinear01` | 1000, 10, 5, 200, 14, 7, 50, 500, 100, 0.001 | 荷重増分、X=+20 kN | `golden_n100.json`、`golden_n1000.json` |
| `support_trilinear02_neg` | 1000, 10, 5, 200, 14, 7, 50, 500, 100, 20 | 荷重増分、X=−10 kN、100段階 | `golden_n100.json` |
| `support_trilinear03_softening` | 1000, 10, 5, 200, 14, 7, **−20**, 500, 100, 0.001 | 変位増分、節点1を+0.2 mまで200段階（荷重パターンX=20） | `golden_n200.json` |

単位は m・rad・kN・kN·m。`elements`は要素ごとの`Mi`・`Ni`・`Si`・`Mj`・`Nj`・`Sj`を持つ。部材の断面力の符号はJRSNAPの定義のままである。

`joints`の項目はSNAPDBLibの`JointItem`の添字ごとにそのまま保存しているが、添字の意味は項目によって異なる。

- `dx`・`dy`・`rz`は節点番号ごとの変位である（節点2の`dy`・`rz`が0になることで確認）。
- `jiban_hanryoku`・`bane_flg`は**支点データの行番号ごと**である。添字1は第1行（節点2のXばね）にあたる。
  - `jiban_hanryoku`は、JRSNAPが報告するばね力である。
  - `bane_flg`は勾配の区分で、0・1・2が第1・第2・第3勾配、負側では−1・−2となる。

## 凍結時に確認した事実

### 正側の骨格は計画どおり

正側の`jiban_hanryoku`は、節点2の`dx`に、力の制限値から作る骨格を当てはめた値と一致する（相対誤差2e-16以下）。`bane_flg`も区分と一致する。骨格の作り方は次のとおり。

- x1 = F1/K1
- x2 = x1 + (F2 − F1)/K2
- x2より先はK3

下降勾配（+SP3<0）も同じ式で、変位増分のケース03では全段階で一致した。

### 荷重増分ではJRSNAPは不釣合い力を残す

- 部材せん断力は各段階で荷重と一致する（差は1e-5 kN程度）。ばね力はそれより小さい。
- 不釣合い力（荷重−ばね力）は最大で、ケース01のn=100で1.238 kN、n=1000で0.380 kNである。
- 収斂判定は剛性低下率で行われ、力の残差は修正されない。このため変位は骨格の逆関数より小さくなる。

| 荷重P | 骨格の逆関数 | ケース01 n=100 | ケース01 n=1000 |
|---:|---:|---:|---:|
| 10 | 0.010000 | 0.010000 | 0.010000 |
| 12 | 0.020000 | 0.019318 (−3.4%) | 0.019600 (−2.0%) |
| 14 | 0.030000 | 0.028956 (−3.5%) | 0.029600 (−1.3%) |
| 15 | 0.050000 | 0.042308 (−15.4%) | 0.042967 (−14.1%) |
| 20 | 0.150000 | 0.125241 (−16.5%) | 0.142400 (−5.1%) |

- 折れ点を過ぎた直後、接線剛性は次の勾配へ一度には切り替わらない。n=1000では、第2折れ点の後に140→105→87…と約100段階かけて50へ近づく。
- 第2折れ点は力14ではなく変位x2=0.03で検出される（n=1000では力14.08の段階）。
- 単純な「前段階の割線剛性を流用」「前段階の接線剛性で増分」のどちらのモデルでも、JRSNAPの反復手順は再現できなかった（最大差 7e-3 m）。

### 変位増分では骨格上を正確にたどる

ケース03では、全段階で次が成り立った。

- 制御節点の変位は目標値に一致する（差4e-11 m以下）。
- `jiban_hanryoku`は骨格と相対誤差2e-16で一致する。
- 部材せん断力は`jiban_hanryoku`と4e-5 kN以内で一致する。

0.03 mで14 kNに達した後、−20 kN/mで下降し、0.2 mで10.6 kNとなる。

### 負側には不整合がある（5.3でも同じ）

ケース02（負側のK1=500、F1=5、K2=100、F2=7、K3=20）で、次のことが分かった。

- `bane_flg`の切り替わりは、負側の骨格どおりの変位（|dx|=0.01、0.03）で起きる。
- 報告されるばね力`jiban_hanryoku`は、**負側の骨格を変位の2倍で評価した値**F_neg(2|dx|)と、相対誤差1e-15で一致する。勾配をすべて2倍にした曲線と同じで、最終段階では荷重10 kNに対して11.28 kNを報告する。
- 実際の変位は負側の骨格（K1=500）に近いが、弾性域でも少し硬い（荷重1 kNで0.0019867 m。骨格では0.002 m）。最終変位は0.1221 mで、骨格の逆関数0.18 mより32%小さい。
- 入力した−RMiT1=5は、DBの支点記録では`Rmit1_F=0`と読まれる（全ケースで同じ）。
- 診断のため同じ入力をJRSNAP 5.3-L01体験版2でも解析したところ、変位とばね力は5.2-L03とビット単位で一致した。版による差ではない（5.3の結果は体験版のため凍結していない）。

負側のばね力と変位は、FrameWebの基準値としては使わない。JRSNAPの仕様なのか不具合なのかは、提供元への確認が必要である。

2026-10-19 の追記: 負側の変位増分（`support_trilinear04_neg_disp`）で、部材せん断力`Si`は負側の骨格F_neg(|dx|)そのもので（FrameWebの法則と1.6e-5 kN以内）、報告値`jiban_hanryoku`だけがF_neg(2|dx|)だった。構造が受ける力は正しく、報告値が2倍である。

## FrameWebとの比べ方

比較の記録は[docs/report/jrsnap-frameweb-equivalence.md](../../../docs/report/jrsnap-frameweb-equivalence.md)にある。要点は次のとおり。

1. **ばね則**: 節点の`dx`の経路をFrameWebのばね則に与え、`jiban_hanryoku`・`bane_flg`と比べる（相対1e-12）。負側はJRSNAPの報告値がF_neg(2|dx|)なので、代わりに部材せん断力`Si`（＝構造が実際に受ける力、F_neg(|dx|)）と比べる。
2. **全体解・変位増分**: 荷重パターンが制御自由度と同じ方向なら、力（荷重係数×荷重、ばね力、せん断力）を1e-4 kNで比べられる。
3. **全体解・荷重増分**: JRSNAPは速度優先でも精度優先でも不釣合い力を残す（後述）。変位が一致しないことを差とともに記録する。

## 2026-10-19 に追加したケース（スキーマ `jrsnap_golden_v2`）

いずれもJRSNAP 5.2-L03。`.ndu`は`tools/jrsnap_golden/make_cases.py`の`MODELS`（`_f1`の3件は凍結済みの`.ndu`そのもの）、`.ndt`は書出し器の出力（`*.writer.ndt`、または`_f1`では元の`.ndt`）に`tools/jrsnap_golden/ndt_tools.py`でDAブロックを書き換えたもの（列70の収斂方法、FTM）。v2のgoldenは、`control`、`kihon`（JRSNAPが保存した基本データ）、`member_info`、`lengths`、部材の状態項目（`SCFlg`、`Outou_M`、`Outou_FAI_Kaiseki`、`M_Kaiseki_S`…）と、各段階の入力ファイルのハッシュ・後処理の内容を持つ。`.LOG`も参考に同じフォルダーへ置いた。

| フォルダー | モデル | 制御 | 列70 | 分かったこと |
|---|---|---|---|---|
| `linear01_cantilever` | 片持ち柱 L=1、E=25、A=0.0007、I=1e-5、先端X 10 kN、線形 | 荷重 10 | 空白 | E=25は2.5e7 kN/m²（`member_info.E`）。せん断変形なし（PL³/3EIに1.5e-12）。符号対応 (Mi,Si,Mj,Sj)=(+Mz_i,−Vy_i,−Mz_j,+Vy_j) |
| `linear01_cantilever_moment` | 同、先端モーメント 10 kN·m | 荷重 10 | 空白 | 節点モーメント荷重は`JLoadInfo`第7項 |
| `linear02_moment_disp` | 同、モーメント荷重パターン、先端X変位増分 0.04 m | 変位 10 | 空白 | JRSNAPは荷重パターンを整数段階（`lo_step_no`/MDV）で与え、残りを制御節点の水平力で補う |
| `support_trilinear01_f1` | 凍結ケース01（n=100、n=1000） | 荷重 | `1` | 不釣合い力は残る（最終段階で0.143／0.146 kN） |
| `support_trilinear02_neg_f1` | 凍結ケース02 | 荷重 100 | `1` | 報告値F_neg(2\|dx\|)は変わらない |
| `support_trilinear04_neg_disp` / `_f1` | ケース02のばね、節点1をX=−0.1 mまで変位増分 | 変位 100 | 空白／`1` | `Si`=F_neg(\|dx\|)（FrameWebの負側の法則と1.6e-5 kN）。速度優先と精度優先は同一結果 |
| `support_bilinear01` | 通常ばね行（SP=1000、±RmiT=10／5）、変位増分 +0.05 m | 変位 100 | `1` | 降伏後の勾配は厳密に0（`K2_S=K3_S=0`、`Rmit2_S`≈1e15）。`jrsnap_bilinear`と差0 |
| `support_multi01` | 節点1〜3のXばね（トリリニアA、+SP3=−20のトリリニア、通常ばね）、剛部材2本、節点3を+0.1 mまで変位増分 | 変位 200 | `1` | 全ばね力・区分・軸力がFrameWebと一致（1e-5 kN）。`Ni`は引張正 |
| `member_mphi01_moment` | 固定M–φ要素1本（M=10,16,22,14／φ=0.001,0.004,0.010,0.018、EI=M1/φ1）、先端モーメント 21 kN·m | 荷重 100 | `1` | `Outou_M`=Mi。曲率は骨格から最大0.395%ずれる（`.LOG`: 剛性低下未収斂、計算回数10）。`SCFlg`は降伏（M2）まで1、以後2 |
| `member_mphi02_disp` | 同じ断面を基部0.1 m要素に、先端X変位増分 0.002 m | 変位 100 | `1`、FTM=1.0E-08 | `Outou_M`は端部（基部）のモーメント、`FAI`=rz2/(0.95·0.1)。頂点せん断力21.90 kN（段階48）、下降域まで到達 |
| `member_beam001` | beam001の配置（9.8+0.1+0.1 m）、beam001.nduの固定M–φ、A=1000・I=4（EI=M1/φ1）、先端X 500 kN | 荷重 100 | `1`、FTM=1.0E-08 | `Outou_M`=Mj2=4950（P·9.9）。FrameWebの中央4925との差が変位で最大4.5% |
| `combined01` | member_mphi01の基部に回転トリリニアばね（5000/8/1000/12/100） | 荷重 100 | `1`、FTM=1.0E-08 | 断面曲率はFrameWebと3.1e-4以内。ばね力は荷重より最大0.454 kN·m遅れる（`.LOG`: 支点状態未収斂） |

FTM=1.0E-08への書換えは、結果にも`.LOG`の計算回数にも変化を与えなかった（`member_mphi01_moment`をFTM 0.001と1.0E-08で解析して比較）。効果は未確認である。

## 再生成

JRSNAP 5.2-L03とJRSNAPHeadlessの`bin\Release`（`.ndt`直接入力に対応した版）、Aggre-SEの`SNAPDBLib.dll`が必要である。解析中はJRSNAPを閉じておく（インストール先の共用ファイルを使うため）。コマンドは`FEMPython\FrameWeb`をカレントにして実行する。

```powershell
# 1. .ndu（beam001.nduから生成。凍結済みの .ndu とバイト一致する。tests/harness/test_jrsnap_tools.py が確かめる）
uv run --locked --extra dev python -m tools.jrsnap_golden.make_cases support_trilinear01 <out>\support_trilinear01.ndu --steps 100
#    ケース01のn=1000は --steps 1000、ケース03は --steps 200（出力名は golden と同じ .ndu 名にする）
#    2026-10-19 のケースは MODELS の名前で: make_cases support_bilinear01 <out>\support_bilinear01.ndu
# 2. 書出し（.ndt だけ作る。DA ブロックを書き換えないケースはこの .ndt をそのまま解析する）
..\..\JRSNAPHeadless\bin\Release\JRSNAP.Headless.exe run --input <out>\support_trilinear01.ndu --output-dir <out>\writer --analyze false
# 2'. 列70（収斂方法）や FTM を書き換える（書出し器は FTM と DSMAX を小数3桁で書くので、1E-08 などはここで入れる）
uv run --locked --extra dev python -m tools.jrsnap_golden.ndt_tools da <out>\writer\support_trilinear01.ndt <out>\support_trilinear01_f1.ndt --convergence accuracy [--ftm 1.0E-08]
# 3. JRSNAPで解析（.ndu を渡せば書出しと解析を続けて行う。.ndt を渡せばそのまま解析する）
..\..\JRSNAPHeadless\bin\Release\JRSNAP.Headless.exe run --input <out>\support_trilinear01_f1.ndt --output-dir <out>\run --analyze true > <out>\run.envelope.json
# 4. DB1から全段階を取り出す（32ビット PowerShell）
& "$env:SystemRoot\SysWOW64\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File tools\jrsnap_golden\extract_db1.ps1 -Db1 <out>\run\support_trilinear01_f1.DB1 -OutJson <out>\raw.json
# 5. 凍結（v2。--control は load か displacement。DA を書き換えたときは書出し器の .ndt と内容を記録する）
uv run --locked --extra dev python -m tools.jrsnap_golden.freeze --case support_trilinear01 --run-dir <out>\run --envelope <out>\run.envelope.json --raw <out>\raw.json --ndu <out>\support_trilinear01.ndu --steps 100 --control load --writer-ndt <out>\writer\support_trilinear01.ndt --post-processing convergence=accuracy --output <out>\golden_n100.json
```

2026-10-18 の v1 golden（`jrsnap_golden_v1`、`load_factor`を持つ）は再凍結していない。2026-10-19 に凍結済みの`.ndu`をJRSNAPで再実行し、旧スクリプトの出力が凍結 golden と値レベルで同一（`provenance.runner.engineVersion`のコミット番号のみ相違）であること、新しい`extract_db1.ps1`の従来項目が同じ値になることを確かめた。

書出しの仕様で分かったこと（2026-10-19、`--analyze false`の`.ndt`で確認）:

- `DataInfo`の第12項は`.ndt`DA行の列32、第13項は列31に書かれる（第12〜15項を1,2,2,1にすると列31〜34は`9090`。列32が材料非線形化のフラグで、`0`で部材のM–φが働く）。
- `SuppotNum`は支点拘束の総数で、行数より小さいとエラー612/613で解析が止まる。
- `JLoadInfo`は「荷重ケース, 空白, 節点, LK, PX, PY, PM, 題名」。同じ節点・同じケースの行は1行にまとめられる。
- `JibanTeikouKeisuu`は常に3行、`SesfPiIJ`と`JibanShogenInfo`は要素ごと、`MemberKUI`は全部材の後にまとめて並ぶ。
- A・I・Eは10桁、FTMとDSMAXは小数3桁で書かれる。
