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

## FrameWebとの比べ方（案）

1. **ばね則（厳密に比べる）**: 正側のケース01・03で、節点2の`dx`の経路をFrameWebのばね則に与え、`jiban_hanryoku`・`bane_flg`と一致することを確認する。
2. **全体解・変位制御（厳密に比べる）**: ケース03はJRSNAPでも不釣合い力が残らないので、FrameWebの変位制御の結果と、せん断力・ばね力の許容差（1e-4 kN）で比べられる。
3. **全体解・荷重制御（許容差つき）**: FrameWebは力の残差をなくすNewton法なので、JRSNAPとは不釣合い力の分だけ変位が異なる。比べ方は未決定である。
4. **負側**: 折れ点の位置（`bane_flg`）だけを参照する。

## 再生成

JRSNAP 5.2-L03とJRSNAPHeadlessの`bin\Release`、Aggre-SEの`SNAPDBLib.dll`が必要である。解析中はJRSNAPを閉じておく（インストール先の共用ファイルを使うため）。コマンドは`FEMPython\FrameWeb`をカレントにして実行する。

```powershell
# 1. .ndu（beam001.nduから生成。凍結済みの .ndu とバイト一致する）
uv run --locked --extra dev python -m tools.jrsnap_golden.make_cases support_trilinear01 <out>\support_trilinear01.ndu --steps 100
#    ケース01のn=1000は --steps 1000、ケース03は --steps 200（出力名は golden と同じ .ndu 名にする）
# 2. JRSNAPで解析
..\..\JRSNAPHeadless\bin\Release\JRSNAP.Headless.exe run --input <out>\support_trilinear01.ndu --output-dir <out>\run --analyze true > <out>\run.envelope.json
# 3. DB1から全段階を取り出す（32ビット PowerShell）
& "$env:SystemRoot\SysWOW64\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File tools\jrsnap_golden\extract_db1.ps1 -Db1 <out>\run\support_trilinear01.DB1 -OutJson <out>\raw.json
# 4. 凍結
uv run --locked --extra dev python -m tools.jrsnap_golden.freeze --case support_trilinear01 --run-dir <out>\run --envelope <out>\run.envelope.json --raw <out>\raw.json --ndu <out>\support_trilinear01.ndu --load-steps 100 --output <out>\golden_n100.json
```

収斂方法を精度優先（列70の`F=1`）にする設定は`.ndu`からは指定できなかった（`DataInfo`の候補項目を変えても`.ndt`の列70は空白のまま）。精度優先での比較には、JRSNAPHeadlessに`.ndt`を直接解析する入口を足す必要がある。
