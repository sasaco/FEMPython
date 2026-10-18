# JRSNAP互換 支持ばねの実装記録

2026-10-18。開始HEAD: `639fcb4`（ブランチ `sasa/nonli`）。

[実装計画](../plans/support-multilinear-springs.md)（改訂2）のStep 1〜8を、TDDで実装した。
計画のStep 1で承認された仕様(a)〜(e)のとおりで、仕様の変更はない。

## 実装した挙動

- 節点と固定地盤の間に、正負非対称の力制限型ばね`jrsnap_trilinear`・`jrsnap_bilinear`を追加した。骨格はJRSNAP BLOCK6の節点ばねと同じで、除荷は力の符号に応じた`K1`の弾塑性除荷とした。反対側の第1折れ点より手前で降伏した場合は第1制限値で平坦になり、下降勾配で軟化した側は劣化記憶`D±`で耐力を回復しない。
- 片側遮断（`F1=0`）、下降して力0に達した側の不活性化、`K3=0`と負の`K3`に対応した。`K=0`と両側の遮断は拒否し、人工剛性は入れない。
- 旧`fix_node`のオブジェクトセル、正規化JSON、Pythonの`add_nonlinear_spring_support()`を、同じ厳格パーサと正規形へ接続した。`K*_neg`は省略しても明示しても同じ正規形・同じ入力hashになる。
- 結果の`support_response`に`type`・力・接線・枝・`zero_position`・最大経験変位を出力する。`metadata.analysis.support_models`は、使っている則のモデルIDを昇順・重複なしで並べる。
- 対応範囲`support_types`に2つの則を`verified`で追加し、READMEとWikiの生成表を更新した。理論マニュアル7.22とWiki 5ファイルを追加・更新した。

## 構成

| 所有先 | 責務 |
|---|---|
| `nonlinear/hysteresis/jrsnap_support.py`（新規） | 不変パラメータ・状態、厳格パーサ、確定状態からの純粋な一回評価、`to_dict` |
| `nonlinear/support_laws.py`（新規） | `type`から則（パラメータ型・状態型・パーサ・評価関数・モデルID）を引くレジストリ |
| `nonlinear/support_springs.py` | 初期状態・評価・`commit`の型検査・snapshotの`type`をレジストリ経由にした |
| `boundary_dofs.py`、`file_io.py` | 定義の解釈・保存時の正規化をレジストリ経由にした |
| `model.py` | 汎用の`add_nonlinear_spring_support()`。`add_slip_spring_support()`はこれへ委譲する |
| `solver_results.py` | `support_models`を則ごとに出す |

`slip.py`、`equilibrium.py`、`solver.py`、`convergence.py`は変更していない。結果契約analysis-result-set-v1に項目を追加していない。

## スリップへの影響

レジストリ化の前に、スリップの出力を固定する試験（`io/test_support_law_registry.py`）を書き、変更前のコードで成功することを確認した。例題の入力hash`ee070fb7…`、`support_models==['slip_v1']`、最終`support_response`、正規化JSONは変わらない。

既存のスリップ試験5ファイルは無変更で成功する。そのうち1件は`support_springs.evaluate_slip`を差し替えて局所例外を注入するので、スリップの評価関数は従来どおり`support_springs`のモジュール名から引く。

変えた文言は次の2つで、どちらも既存試験の正規表現に一致する。

- 局所評価の例外: `slip spring node … direction …` → `nonlinear support (slip) node … direction …`
- 解析種別の拒否: `Slip springs require …` → `Slip and other nonlinear support springs require …`

## TDDと独立期待値

| 段階 | 結果 |
|---|---|
| ばね則の実装前 | 未実装モジュールによる収集エラー1 |
| ばね則の実装後 | 52成功（手計算表、単位倍率、ランダム経路、閉ループ、拒否、golden照合） |
| スリップの特性固定（レジストリ化前のコード） | 6成功・レジストリ関連4失敗 |
| レジストリ化後 | 既存スリップ試験の差し替え先1件が失敗。評価関数を引く場所を戻して全168成功 |
| ソルバー・入力・混在要素・回帰 | 14・29・13・8成功 |

期待値の出どころは次の3つで、製品コードから作った値はない。

- 手計算表（理論マニュアル7.22）
- 並列梁の独立した剛性（`P=kb·u+F(u)`）
- JRSNAP 5.2-L03の凍結結果（`tests/data/jrsnap_golden`）

手計算表を試験にする途中で、計画の表に枝名のない行（2回目の`.02`）の扱いを確定した。この点は劣化記憶`D+=12`と骨格`Q+(.02)=12`の境目にあたる。その先は12で平坦なので、`plateau_pos`（接線0）とした。力の値は計画のとおりである。

## JRSNAPとの照合

| 比較 | 結果 |
|---|---|
| ケース01（n=100、n=1000）・ケース03の正側：節点2の`dx`の経路をばね則へ与える | 全段階で`jiban_hanryoku`と相対1e-12、勾配区分`bane_flg`と一致 |
| ケース02の負側 | 勾配区分の切替位置だけが一致（JRSNAPの報告値F_neg(2\|dx\|)は再現しない） |
| ケース03・ばね単独の変位制御（節点2の`dx`を制御） | 全200段階で荷重係数がばね力・部材せん断力と1e-4 kN以内 |
| ケース03・2次元の剛な部材（節点1の`dx`を制御） | 全200段階で荷重係数が部材せん断力と、ばね力が`jiban_hanryoku`と1e-4 kN以内 |

2次元の剛な部材は`E=1e8`とした。曲げたわみがばね力に与える誤差は最大5e-5 kNである。この剛性比（約3e5）では相対残差が丸め誤差の水準（約1e-10）で止まるため、この試験の収束許容差は1e-8とした。`E=1e10`では既定許容差のまま収束しなかった。これはばね則ではなく剛性比の問題で、既存の挙動である。

## 全件回帰

`uv --directory FrameWeb run --locked --extra dev python -m tools.validation.check_test_results`（2026-10-18〜19、1時間15分）の結果は、3,484成功・6失敗・4エラーだった。既知失敗の基準（0件）とは一致しない。

失敗・エラーの10件は、すべて`tests/validation`の`test_mesh_convergence.py`（4エラー）、`test_performance.py`（2失敗）、`test_unit_invariance.py`（4失敗）にある。原因は、検証ツールが`model.run()`の結果から`node_displacements`・`metadata`を読もうとするKeyErrorで、支持ばねとは関係しない。変更前のHEAD `639fcb4`のworktreeで同じ3ファイルを実行し、同じ10件が同じ理由で失敗することを確認した。Wikiの検査（`check_wiki`）の`examples.md / distributed-beam`も、HEADで同じKeyErrorにより失敗する。今回追加・変更した範囲の失敗はない。

`git diff --check`は成功した。

## 残る事項

- JRSNAPの負側の報告値F_neg(2|dx|)と、−RMiT1の保存値0が仕様か不具合か（提供元へ確認。計画のOpen Question 3）。
- 精度優先の収斂（列70の`F=1`）でJRSNAPの荷重増分の不釣合い力が消えるか。JRSNAPHeadlessに`.ndt`を直接解析する入口が必要（Open Question 4）。
- 反転を含むJRSNAPの実計算結果があれば、除荷則の比較に加える（Open Question 1）。
