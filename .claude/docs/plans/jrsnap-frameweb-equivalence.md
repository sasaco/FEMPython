## Implementation Plan: JRSNAP と FrameWeb の静的非線形解析結果の一致確認

作成 2026-10-19（改訂 1: 独立レビューの指摘を反映）。ブランチ `sasa/nonli`、HEAD `78e7754`。作業依頼は root の `HANDOFF.md`。

### Purpose

FrameWeb の静的非線形解析（JRSNAP 互換支持ばね、部材の材料非線形、その組合せ）が JRSNAP 5.2-L03 の結果と一致するかを、機能×ケース×量×段階範囲ごとに **一致／不一致／未確認** の 3 値で根拠つきに記録し、一致した行は JRSNAP なしで再実行できる pytest として凍結する。

### Scope

**利用者が確認した対象範囲（2026-10-19、AskUserQuestion の回答）**

1. 支持ばね `jrsnap_bilinear`/`jrsnap_trilinear` の未確認項目: 荷重増分制御、負側、反転・繰返し、バイリニアの実計算、複数部材モデル。
2. 部材の材料非線形（M–φ 骨格、JR RC 剛性低減 K1–K4、軸力依存）。
3. 支持ばねと部材の材料非線形の組合せ。
4. JRSNAPHeadless（別リポジトリ）への `.ndt` 直接入力の追加: **許可**。
5. 以前ローカルにあった JR-SNAP 5.3 の beam001 類似 `.OUT`/`.FMT`: **ない／不明**。新規に実計算する。

**対象外（HANDOFF.md のまま）**: FrameWebforJS/FrameWebforCS の UI と structural-mcp 契約、動的解析、KR2/KR3 自動算定と αf、`C:\RTRI_Program` の変更、製品挙動を JRSNAP に合わせる変更（利用者の承認なしには行わない）。

**調査で確定した前提（出典つき。マニュアルは `C:\RTRI_Program\JRSNAP(Ver5.2-L03)\JRSNAP_操作説明書.pdf` の印字ページ番号。5.3 体験版のマニュアルも同じ節）**

- JRSNAP は荷重増分か変位増分の単調載荷のみで、反転・繰返しの入力項目がない（付2-12〜付2-17、付2-120〜123。インストール先とリポジトリの全 `.ndu` にも反転例なし）。変位増分は節点 JNO の X 変位のみ（付2-16）。
- 収斂方法フラグ F は `.ndt` DA 行 1 の列70（付2-13）。`.ndu` からは指定できず、既存 `.ndt` の DA 行は 65 文字で列70 が空白＝速度優先。`Nrail.exe` に `=== 収斂方法は精度優先(従来)版を使用 ===` の文字列があり、`.LOG` で適用を確認できる。
- BLOCK6 支点行の `iK` は **方向**（1=X、2=Y、3=回転）であり種別ではない（`make_cases.py:17,68-69`、`test_jrsnap_golden.py:128-129`、golden README「Y・回転は剛支点」）。通常ばね（SP、±RmiT の 3 値、付表2-12）とトリリニア（付表2-15）の入力の違いは **未確認**。0 は「非常に弱いばね（0.01）」（付2-28）。
- JRSNAP の固定 M–φ（BLOCK7、`.ndu` の `NonLinFix`）は軸力変動に追従しない（付2-32）。軸力依存は自動算定 BLOCK8 のみで、対象外。
- M–φ 要素は BLOCK5 に A・I・E が必要（付2-34 注⑥）。`tests/data/snap/beam001.ndu` は A・I が空欄。JRSNAP が弾性域に EI を使うのか M1/φ1 を使うのかは未確認。
- DB1 の `IOutMember` には `SCFlg`（部材状態、>1 で降伏）、`Outou_M`、`Outou_FAI_Kaiseki`、`M_Kaiseki_S/F`、`FAI_Kaiseki_S/F`、`HisenkeiFlg` があり、`IKihonInfo` に `LastStep`・`MDV`・`DDV`・`DSMAX` がある（SNAPDBLib 5.3.1.0 のリフレクション列挙、Batch の `CalculationElement.vb:932-1126`）。現在の `tools/jrsnap_golden/extract_db1.ps1:31-33` は `Mi..Sj` しか取り出していない。`JointItem` は節点番号添字、`JibanHanryoku`/`BaneFlg` は支点行添字で、行数が節点数を超えるときの取り出し方は **未確認**。
- `freeze.py:47` の `load_factor` は `index/load_steps` で、変位増分では変位比になる。DB の `LoStepNo` は整数（ケース03 で 100 段階目が 126）。荷重係数は力から導く。
- 既存 golden の `.DB1` はリポジトリにない（`.ndu`・`.ndt`・`golden_*.json` のみ）。再抽出には JRSNAP の再実行が要る。
- `tests/harness/test_jrsnap_golden.py` は `*/golden_*.json` を glob し（`:13`）、4 本の固定リスト（`:56-62`）、`schema == jrsnap_golden_v1`（`:68`）、`CASES[case]`（`:78`）、節点 1 荷重・節点 2 ばね・要素 1 の前提（`:86-95`）を持つ。**新 golden を置いた時点で落ちる**。
- FrameWeb: 変位制御は `displacement_control.targets` に任意の段階列を取り（`solver.py:423-424`）、荷重制御は `load_factors` の任意列を取る。制御自由度は 1 つ、荷重パターンは 1 つ（`solver.py:250,428`）。荷重パターンが節点モーメントで制御自由度が `dx` の組合せは許される（`solver.py:416-419` は自由自由度上でパターンが非零であることだけを要求）。棒要素モデルは 2D でも stride 6 なので `rz` ばねを置ける（`dof.py:24-26`、`boundary_dofs.py:92-94`）。非線形部材は要素中央 1 断面で評価（`nonlinear_bar_element.py:25-31,64-68`）。`jr_stiffness_reduction` は 0 ≤ P4 < P3 のみ受け付け、K4 直線は δ4 を越えても続く（`jr_stiffness_reduction.py:57-65,140-154`）。
- FrameWeb の部材端力 → JRSNAP `Mi/Ni/Si/Mj/Nj/Sj` の符号対応は未確立（既存試験は `lambda` と `Si` しか比べていない）。
- 複数節点・複数部材・`NonLinInfo` を持つ `.ndu` の雛形: `JRSNAPHeadless\tests\JRSNAP.Headless.Tests\fixtures\線路直角方向.ndu`（8 節点、7 部材、変位増分）。

**New files**

- `FrameWeb/docs/report/jrsnap-frameweb-equivalence.md`（比較記録、日本語）
- `FrameWeb/tests/data/jrsnap_golden/<case>/`（新ケースごとに `.ndu`・writer が書いた `.ndt`・解析に使った `.ndt`・`golden_*.json`）: 候補 `linear01_cantilever`、`linear02_moment_disp`、`support_trilinear01_f1`、`support_trilinear02_neg_f1`、`support_trilinear04_neg_disp`、`support_bilinear01`、`support_multi01`、`member_mphi01_moment`、`member_mphi02_disp`、`member_beam001`、`combined01`
- `FrameWeb/tests/integration/test_jrsnap_equivalence_*.py`（一致行を固定する試験）
- `FrameWeb/tools/jrsnap_golden/templates/`（複数節点・部材非線形用の `.ndu` 雛形）
- JRSNAPHeadless: `src/JRSNAP.Headless/` の `.ndt` 入力経路とその単体試験

**Modified files**

- `FrameWeb/tools/jrsnap_golden/make_cases.py`（雛形方式、複数ばね行、M–φ、節点モーメント荷重、FTM、列70 後処理）
- `FrameWeb/tools/jrsnap_golden/extract_db1.ps1`、`freeze.py`（部材状態項目・`KihonInfo`・配列長の追加、スキーマ `jrsnap_golden_v2`、writer `.ndt` と解析 `.ndt` の両方のハッシュと後処理内容の記録）
- `FrameWeb/tests/harness/test_jrsnap_golden.py`（ケース表駆動化。既存 4 本に対する検査内容は変えない。新ケースは golden と同じ Step で表へ追加）
- `FrameWeb/tests/data/jrsnap_golden/README.md`（ケース表と再生成コマンド。列70 後処理を含む）
- `FrameWeb/docs/理論マニュアル/08_材料非線形モデル.md` §7.22「JRSNAPと比べるときの注意」（CRLF 維持）。部材側の知見が得られた場合のみ §7.11 の注記
- `FrameWeb/docs/plans/support-multilinear-springs.md` Open Questions
- JRSNAPHeadless: `RunOptions.cs`、`JrsnapRunner.cs`、`README.md`、`tests/JRSNAP.Headless.Tests/*`

**Dependencies**: JRSNAP 5.2-L03（解析中は GUI を閉じる）、`JRSNAPHeadless\bin\Release`（改修中は `bin\Stage1`。Stage1 ビルドと既存 56 試験の成功は 2026-10-19 に確認済み）、`SNAPDBLib.dll`（Aggre-SE Release）、32 ビット PowerShell、`uv`、.NET SDK（net48）。

### Implementation Steps

依存順。Step 0 → Step 1（校正と Open Question 1 の判定）→ Step 2a（Open Question 2 の判定）→ 分岐 → Step 2b〜2e・3・4 → Step 5。各ケースは `--analyze false` で `.ndt` を点検してから解析する。許容差は各ケースの比較前に記録へ書き、以後は緩めない。

**分岐（決定木）**

- OQ2「精度優先で荷重増分の不釣合い力が消えるか」: 消える（|P − ばね力| ≤ 1e-4 kN）なら 2a・3a・3c・4 を荷重増分で比較する。消えないなら、荷重増分の行はすべて「不一致（JRSNAP 側の残差 X kN）」と記録し、3a・3c・4 を変位増分に切り替える（OQ1 が可のとき）。
- OQ1「節点モーメント荷重パターン + X 変位増分を JRSNAP が受け付けるか」: 可なら 3b・4 をその形で行う。不可なら先端 X 荷重 + 基部 0.1 m 非線形要素（beam001 型）に切り替え、評価位置の違いを原因候補（仮説）として記録する。
- 両方不可のとき、部材非線形は 3b の代替案のみとなり、差が出ても 原因未特定 で終わる可能性がある。その場合もそのまま記録する。

#### Step 0: 準備と道具
- [x] ベースライン: JRSNAP 関連 7 スイート 140 passed（2026-10-19、5.28 s）。JRSNAPHeadless Stage1 ビルドと 56 試験成功。
- [ ] JRSNAPHeadless に `.ndt` 直接入力を追加する: `RunOptions.Parse` が `.ndt` を受け付け、`JrsnapRunner.Run` が writer を飛ばして入力を出力先へ複写し `Analysis.Run` へ渡す。`.ndu` 検査は `Run` 側へ移し `Figures.Run`（`Figures.cs:36`）は従来どおり。先頭行 `START   52/53` を `install.Product` と照合。`viewFlags` は null、`cases` は入力名の 1 ケース。`.LOG` の収斂方法の行（`精度優先`/`速度優先`）を envelope の messages へ通す。単体試験を追加し、既存 `Run_WritesTheNdtTheScreenWrites` のバイト一致を保つ。`-c Stage1` でビルド・試験し、Release 上書きは試験成功後。
- [ ] `test_jrsnap_golden.py` をケース表駆動にする: ケースごとにスキーマ版・制御（荷重/変位）・荷重節点・ばね節点・要素番号・期待不釣合いを表に持ち、既存 4 本の検査内容と期待値は変えない。新ケースは golden と同じ Step で表へ追加する。
- [ ] `extract_db1.ps1` に `IOutMember`（`SCFlg, HisenkeiFlg, Outou_M, Outou_FAI_Kaiseki, Outou_FAI_Shousa, M_Kaiseki_S/F, FAI_Kaiseki_S/F, BaneFlg, JibanHanryokuI/J`）、`IKihonInfo`（`KaisekiFlg, DDV, DSMAX, JNo, MDV, LastStep, LD, JALF`）、`IMemberInfo`（`A, I, E, M, K`）、配列長（`JointItem.Length`、`SupportInfo.Count`）を追加する。
- [ ] 既存 3 ケースの凍結済み `.ndu` を JRSNAP で再実行し、旧スクリプトの出力が凍結 golden とバイト一致すること（再現性の記録）と、新スクリプトの従来項目が同じ値であることを確認する。
- [ ] `freeze.py` を `jrsnap_golden_v2` にし、変位増分では `load_factor` を書かず `lo_step_no` と `control` を書く。provenance に writer が書いた `.ndt` と解析に使った `.ndt` の両方のハッシュと後処理（`convergence: accuracy`）を記録する。既存 v1 golden は無変更。
- [ ] `make_cases.py` を雛形方式に広げる（`線路直角方向.ndu` を参照して必要なキー集合を確定）: 任意節点数・部材数、`SuppotInfo` 複数行、`ElementInfo` の A・I・E と M–φ リンク（第7項）、`NonLinFix` 行、節点モーメント荷重、`DataInfo` f20（FTM）、生成後の `.ndt` の DA 行を 70 桁に延ばして列70 に `1` を書く後処理（`--convergence accuracy`）。`.ndu` は cp932・CRLF。
**Verification**: JRSNAPHeadless 全単体試験成功。既存 3 ケースの `.ndu` が `make_cases` でバイト一致再生成できる。再実行 golden が凍結 golden とバイト一致。新 `.ndt` を `--analyze false` で生成し、列70 以外が元と同じことを diff で示す。JRSNAP 関連スイートが成功。

#### Step 1: 線形校正（単位・せん断変形・符号・OQ1）
- [ ] `linear01_cantilever`: 片持ち柱 L=1 m、E=26.5、I=1e-3、A はせん断項が曲げの約 10% になる値（G の仮定を含めて手計算し記録）。全非線形フラグ 9。先端 X 荷重 P=10 kN と、別ケースで先端モーメント M=10 kN·m。荷重増分 10 段階。閉形式（PL³/3EI、ML²/2EI、ML/EI）と `dx`・`rz`・`Mi/Mj/Si/Sj/Ni/Nj`・支点反力（剛支点行の `jiban_hanryoku`）を比べ、E の単位、せん断変形の有無、符号規約、節点モーメント荷重の可否を確定する。
- [ ] `linear02_moment_disp`（OQ1 の判定）: 同じ柱に先端モーメントの荷重パターン + 先端 X の変位増分。`--analyze false` で `.ndt` の LOAD と DA を点検し、解析して λ·M と `rz`・`dx` が閉形式に合うかを見る。
- [ ] 同じモデルを FrameWeb（E=2.65e7、G なしと G ありの 2 通り）で解き、同じ量を比べる。
**Verification**: 閉形式との相対差 1e-8 以内（根拠: 線形解析の倍精度解）。せん断項の有無はこの許容差で判別できる（約 10% の差）。符号対応表と OQ1 の答えを記録に書く。

#### Step 2: 支持ばね（対象 1）
- [ ] **2a 精度優先の荷重増分（OQ2 の判定）** `support_trilinear01_f1`（n=100、n=1000）と `support_trilinear02_neg_f1`（n=100）: 既存 `.ndt` の列70 を `1` にして `.ndt` 直接入力で解析し、凍結する。JRSNAP 自身の不釣合い力 |P − jiban_hanryoku| を測る。1e-4 kN 以内なら FrameWeb の荷重制御（`load_factors`、tol 1e-10）と `dx`・ばね力・`bane_flg`・部材端力・反力を比べる。残るなら「不一致（JRSNAP の荷重増分は精度優先でも不釣合い力 X kN を残す）」と記録し、FrameWeb との変位差も記録する。
- [ ] **2b 負側の変位増分** `support_trilinear04_neg_disp`（ケース02 のばね、DSMAX=−0.1 m、100 段階、速度優先と精度優先の 2 本）: 負側の報告値が F_neg(2|dx|) か、`Rmit1_F=0` が同じかを測る。FrameWeb の負側の法則との差は「不一致（設計上再現しない、Open Question 3）」として数値を記録する。
- [ ] **2c バイリニア** `support_bilinear01`: まず通常ばね（SP=1000、+RmiT=10、−RmiT=5）の `.ndu` の書き方を確定する（マニュアル 付2-26〜32 の該当ページを引用し、`--analyze false` の `.ndt` と `figures --data-check node-springs` で JRSNAP が読んだ種別を確認）。期待法則は比較前に決める: 降伏後の勾配が 0 なら `jrsnap_bilinear` と同じ、JRSNAP が 0.01 を補うなら「不一致（0=0.01 の仕様、差 0.01·Δu）」。剛な部材、変位増分 +0.05 m・100 段階、精度優先。節点 2 の `dx` 経路を法則に与えて `jiban_hanryoku` と比べ、`bane_flg` と `plateau_pos` の対応を試験側の写像に足す（製品コードは触らない）。全体解も変位制御で比べる。
- [ ] **2d 複数ばね・複数部材** `support_multi01`: 節点 1 (0,0)・2 (1,0)・3 (2,0)、剛な部材 1–2・2–3、X ばねを支点行 1〜3（節点 1 トリリニア A＝ケース01 の値、節点 2 トリリニア B（+SP3=−20）、節点 3 通常ばね）、剛支点行（Y・回転）はその後ろ。荷重 X を節点 3、変位増分 +0.1 m・200 段階、精度優先。期待値は JRSNAP の `jiban_hanryoku` 各行、λ = Σ`jiban_hanryoku` と `Ni`。比較は (i) 法則再生（各節点の `dx` → 法則、相対 1e-12）と (ii) 全体解に分け、(ii) の力の許容差は EA と通過力から部材伸びの上限を手計算して比較前に決める（例: L/(EA)·N·K1 を見積もる）。`JointItem.Length` と `SupportInfo.Count` で行添字の到達性を確認する。
- [ ] **2e 反転・繰返し**: JRSNAP に入力項目がないため「未確認（同一条件で比較不能）」と記録し、出典（付2-12〜17、付2-120〜123）を書く。仮説: LK=4（LiMiT3 まで漸増後保持）の +X 荷重と LK=1 の −X 荷重を重ねると荷重制御で 1 回の反転ができる。比較前に、反転が起きた場合の判定規則（FrameWeb の `load_factors` 同経路、ばね力 1e-4 kN、`bane_flg` 一致。JRSNAP が荷重増分で不釣合いを残すなら OQ2 の結果に従い「不一致」）を記録に書いてから、`--analyze false` で LOAD 行を点検し 1 回だけ解析する。製品の除荷則は変更しない。
**Verification**: 法則再生は相対 1e-12（根拠: 区分線形の閉形式を倍精度で評価、既存試験と同じ）。全体解の力は、1 ばね＋剛部材では 1e-4 kN（根拠: JRSNAP 自身のせん断力とばね力の差 4e-5 kN、既存試験と同じ）、2d はケースごとに導いた上限。制御節点の変位は 1e-9 m、剛支点の変位は |v|<1e-12 m（JRSNAP の剛支点は約 1e15 のばね）、区分は完全一致。反力は剛支点行の `jiban_hanryoku` と FrameWeb `reaction_forces` を力の許容差で比べる。固定する試験は摂動で失敗することと、経路の到達（最終区分と区分変化の回数）を確かめる。

#### Step 3: 部材の材料非線形（対象 2）
共通の骨格（3a・3b・4 で同じ）: M1..M4 = 10, 16, 22, 14 kN·m、φ1..φ4 = 0.001, 0.004, 0.010, 0.018（`jr_k4_displacement_control.json` と同じ点、K4=−1000）。FrameWeb には `delta_4/P_4` を同じ値で与える。要素は A=1000、I は E·I = M1/φ1 = 1e4 kN·m² となる値（E=2.65e7 kN/m² なら I≈3.7736e-4 m⁴。E の単位は Step 1 で確定した値を使う）。FTM=1e-8、LiMiT1/2=999。
- [ ] **3a 先端モーメント・荷重増分** `member_mphi01_moment`: 片持ち柱 L=1 m、全長 1 要素を固定 M–φ。先端モーメント M=21 kN·m（M3 の手前。K1〜K3 のみ通る）、100 段階、精度優先。制御法は OQ2 の結果に従う。`Outou_FAI_Kaiseki`・`rz`・`dx`・`Mi/Mj`・`SCFlg`・反力を FrameWeb の `section_response.curvature`・区分・節点変位・`reaction_forces` と比べる。
- [ ] **3b 下降勾配・変位増分** `member_mphi02_disp`: 同じ柱、先端モーメントの荷重パターン + 先端 X 変位増分（OQ1 可のとき。DSMAX は κ=0.016 相当の κL²/2）で M4 を越えて下降域まで進める。OQ1 不可なら先端 X 荷重 + 基部 0.1 m 非線形要素に切り替える。
- [ ] **3c beam001 同一条件** `member_beam001`: `beam001.ndu` を A=1000・I（E·I = M1/φ1 = 1e8 となる値）を入れて生成し直し、FTM=1e-8、精度優先。FrameWeb は比較専用 JSON（`beam001.json` に `delta_4=0.002, P_4=3000`、φ3=0.001、Iz を同じ値）で解く（`tests/data/snap/beam001.json` と Decimal oracle は変更しない）。要素 2 の `Outou_FAI_Kaiseki`・`Mi/Mj`・先端 `dx` を比べ、差があれば評価位置（中央 y=−0.15 と JRSNAP の未確認の位置）を仮説として記録する。
- [ ] **3d 軸力依存**: 「未確認（JRSNAP の固定 M–φ は軸力変動に追従せず、追従する BLOCK8 自動算定は対象外。付2-32）」と記録する。
- [ ] **3e 除荷・履歴**: 単調載荷のみのため「未確認」。JRSNAP の `SCFlg` と FrameWeb の `branch`/区分の対応表だけ記録する。
**Verification**: 曲率・回転・変位は相対 1e-6（根拠: FTM=1e-8 が剛性低下率の収束誤差を抑え、FrameWeb の tol は 1e-10。`.LOG` に「トライアル回数制限値到達」があればその段階は「不一致（JRSNAP 未収束）」）、モーメントは 1e-6 kN·m、`SCFlg` と区分は完全一致。固定する試験は摂動で失敗し、経路の到達を確かめる。

#### Step 4: 組合せ（対象 3）
- [ ] `combined01`: 3a の柱の基部に回転トリリニアばね（方向 3、K1=5000 kN·m/rad、±RMiT1=8、K2=1000、±RMiT2=12、K3=100）を置き、先端モーメント荷重（制御法は OQ2・OQ1 に従う）。モーメントが一定なので、ばねモーメント＝断面モーメント＝荷重。先端 `dx` = κL²/2 + θ_spring·L、`rz` = κL + θ_spring。FrameWeb は `rz` ばね（stride 6）。反力の行を含める。
- [ ] 3b の結果が良ければ、先端 X 変位増分 + 基部 X トリリニアばね + 基部 0.1 m 非線形要素も 1 本加える。
**Verification**: ばね側は Step 2、部材側は Step 3 の許容差。両方の非線形が同じ段階で区分を変えることを `bane_flg`/`SCFlg` で確認する。

#### Step 5: 記録・試験の固定・回帰・文書
- [ ] `FrameWeb/docs/report/jrsnap-frameweb-equivalence.md` に、利用者の範囲確認、マニュアルの版とパス、各ケースの許容差と根拠（比較前に記入）、1 行＝（機能×ケース×量×段階範囲）の表（JRSNAP 版、FrameWeb コミット、制御、許容差、最大差と段階、判定、原因または欠落）を書く。量は変位・ばね力・区分・部材端力・反力・荷重係数。
- [ ] 「一致」行ごとに pytest を書く（golden のみ読む、`oracle` マーク、許容差超過で失敗）。期待値を 1 行摂動して失敗することを確認し、その出力を記録に残す。
- [ ] `test_jrsnap_golden.py` のケース表、golden README（ケース表・再生成コマンド・列70 後処理）。
- [ ] JRSNAP 関連スイート（既存 4 ケースの検査は無変更）、`git diff --check`、全件ゲート `check_test_results`（背景実行、約 75 分。基準 3,484 成功・`tests/validation` の既知 10 件）。
- [ ] 理論マニュアル §7.22 と計画 Open Questions を知見で更新（記録以上のことは書かない）。
- [ ] 利用者へ日本語で範囲項目ごとの判定と根拠パスを報告し、コミット（本リポジトリと JRSNAPHeadless。`GUIDE.md` を含めるかも尋ねる）の可否を尋ねる。
**Verification**: 受入基準 1〜7（HANDOFF.md）をすべて満たす。

### Risks & Considerations

- **JRSNAP の部材定式化が未記載**: 評価位置・ヒンジ長が分からないため、先端モーメント（一定モーメント）で定式化差を消す。先端 X 荷重のケースは差が出ても原因を「評価位置の違い（仮説）」としか書けず、記録では 原因未特定 と書く。
- **M–φ 要素の A・I・E の使われ方が未確認**: E·I = M1/φ1 に合わせて両解釈を一致させる。Step 1 の線形校正で E の単位とせん断変形を先に確定する。
- **JRSNAP の荷重増分の不釣合い力**: 精度優先でも残るかは未確認。決定木に従って変位増分へ切り替える。
- **変位増分は X のみ**: 回転ばねや M–φ の下降域は、モーメント荷重パターン + X 変位制御で届かせる。JRSNAP が受け付けなければ代替案へ。
- **通常ばねの入力形式が未確認**: 2c は形式の確認と期待法則の確定を比較前に行う。
- **FTM を 1e-8 にすると試行回数制限に達する可能性**: `.LOG` を確認し、達した段階は許容差を緩めずに「不一致（JRSNAP 未収束）」と記録する。
- **既存ハーネスは新 golden で落ちる**: Step 0 で表駆動にし、新ケースは golden と同じ Step で表へ入れる。既存 4 ケースの検査内容は変えない。
- **`extract_db1.ps1` の拡張で既存 golden を壊さない**: 既存 3 ケースを再実行・再抽出して従来項目の一致を確認する。新項目は v2 スキーマのみ。
- **`.ndt` 直接入力の再現性**: `.ndu` → writer `.ndt` → 列70 後処理 → 解析 `.ndt` の各段のハッシュを provenance に残し、README の再生成コマンドに後処理を含める。
- **2d の許容差**: 1 ばね＋剛部材の 1e-4 kN は転用せず、EA と通過力から比較前に導く。
- **JRSNAPHeadless の変更は別リポジトリ**: `Stage1` 構成でビルドし、既存のバイト一致試験を保つ。解析中は JRSNAP GUI を閉じる。
- **製品変更の禁止**: `plateau_*` の `bane_flg` 対応は試験側の写像に足すだけで、製品コードは触らない。FrameWeb の欠陥に見える差は再現・報告して利用者に尋ねる。
- **試験の空虚さ**: 弾性域が長いケース（3a は約半分）では、最終区分と区分変化回数の到達を試験で確かめる。
- **Codex CLI は使えない**（HTTP 400、HANDOFF.md Pitfalls）: 計画の妥当性検証は独立した Plan サブエージェントで代替した（改訂 1 はその指摘の反映）。
- **ワークフロー**: `workspace.py` は `.agents/` を作ってしまうので使わない。本計画は `.claude/docs/plans/` に置く。

### Open Questions

実施結果（2026-10-19、記録 `FrameWeb/docs/report/jrsnap-frameweb-equivalence.md`）:

1. JRSNAP が「節点モーメント荷重パターン + X 変位増分」を受け付けるか → 受け付けるが、制御節点に水平力を加えて目標変位に到達させる（λ は整数段階）ため、同一条件にならない。3b は先端 X 荷重 + 基部 0.1 m 要素に切り替えた。
2. 精度優先（列70=1）で荷重増分の不釣合い力が消えるか → 消えない（ケース01 n=100: 1.238→0.143 kN、n=1000: 0.380→0.146 kN）。荷重増分の行は不一致として記録した。
3. JRSNAP 負側の F_neg(2|dx|) と `Rmit1_F=0` → 変位増分・精度優先でも同じ。ただし部材せん断力は F_neg(|dx|) で、FrameWeb の負側の法則と一致する。仕様か不具合かは提供元待ち。
4. LK=4 と LK=1 の重ね合わせ → 同一荷重ケース内の 2 行は書出し器が 1 行にまとめるため不成立。複数荷重ケース＋組合せは未構築。未確認のまま。
5. JRSNAP の部材非線形の評価位置 → 測定で確定: `Outou_M` は要素端の最大モーメント、曲率は直線分布の端の値（rz=0.95·FAI·L）。ヒンジ長の概念はない（要素全体の等価剛性）。反復手順は未確認。
6. 通常ばねの書き方 → `SuppotInfo` の 3 値行（SP, +RmiT, −RmiT）。降伏後の勾配は厳密に 0 で、0.01 への置換は起きない。
7. 支点行数が節点数を超えるとき → `JointItem` の長さは節点数+1 で、行 1〜節点数までしか読めない。X ばねを先頭の行に置いた。
