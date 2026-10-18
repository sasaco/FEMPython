# テスト項目・保証範囲

2026-09-10。現在の[配置・全ケース台帳](test_inventory.md)と、整理前の[原項目表](archive/test_items-original.md)を対応させる。旧番号は追跡用に残す。旧表の大きな ✅ は継承せず、個別の保証を根拠に状態を付ける。

「検証済み」はその行に明記した振る舞いの保証。「一部」は原項目に未検証を含む。「未検証」は独立した期待値による現行試験を確認できないもの。「将来」は今回実装・試験を追加しない構想。カバレッジ率を表す区分ではない。

| 機能・旧番号 | 現在保証する範囲／所有先 | 状態・残る要求 |
|---|---|---|
| 共通静解析ソルバー | 直接解法／Newtonの明示選択、3/6DOF混在、旧API委譲、状態復元・再利用、独立スナップショット、コールバック、既知固有値。`solvers/test_unification.py` | 検証済み（21ケース）。数値方針の全面変更や一般FEM既知失敗の修復を含まない |
| 基底要素 1.1 | ID・座標・長さ・ヤコビアン・未実装 3 API。`elements/test_base.py` | 一部。未実装例外は体積／補間計算の実装完了を意味しない |
| 梁の弾性行列 1.2, 7.1, 10.1 | BE/T 梁・非線形梁の弾性極限、右手系の端力、回転対称性。`elements/beam/test_elastic.py` | 一部。異方性と任意断面計算は未検証 |
| 要素質量 1.2–1.4, 7.1 | 梁・Tri/Quad・Tetra/Hexa/Wedge のサイズと対角非負、シェル回転共変性 | 一部。一般の集中／一致質量・質量保存の保証とはしない |
| シェルの力学 1.3, 7.1, 7.8 | 膜・DKT/Mindlin 曲げ、6 剛体モード、回転、drilling、厚さ三乗、Quad 定曲げ。`elements/shell` | 一部。全形状のロッキング回避や平面ひずみを一般化しない |
| 一次ソリッド 1.4, 12.3–12.5 | 補間、導関数、固有積分点、剛性・質量、四面体体積。`elements/solid/test_linear.py` | 一部。任意歪み・ロッキングの網羅保証はない |
| 二次ソリッド 1.4, 7.8, 12.3–12.5 | Tetra/Wedge/Hexa の補間、多項式・差分、剛体・仕事・反転拒否・処方アフィン解。`elements/solid/test_quadratic.py`, `integration/test_solids.py` | 検証済み（記載範囲）。一次要素の試験を置き換えない |
| 補間と積分 1.5, 7.2, 7.8, 12.1–12.5 | 一次シェル／ソリッドと二次ソリッドの和・導関数・積分、独立パッチ。各要素配下 | 一部。三角形二次・三次、一般 p/hp 収束は将来 |
| メッシュ収束・適用範囲 4.1, 7.8 | 梁、DKT、MITC4、Hexa8の滑らかな非一様変形に対する変位・応力／モーメント・エネルギー誤差、歪み・アスペクト比・板厚・ポアソン比。`validation/test_mesh_convergence.py` | 検証済み（PQ-09記載問題）。特異点、曲面シェル、三角形Mindlin、Tetra/Wedge/二次ソリッド、Hexa8の`nu>=0.49`は保証外 |
| 単位換算と収束判定 3.2, 7.6, 7.11 | N–m、N–mm、kN–mの同一モデルについて、材料骨格、断面、密度、荷重・モーメント、並進・回転ばね、強制変位、曲率、循環載荷、荷重／変位制御、固有値を比較。`validation/test_unit_invariance.py` | 検証済み（PQ-10記載モデル）。入力値の自動換算、単位宣言からの換算、座標spanのない低水準合成モデルは保証外 |
| 材料 2.1, 7.9 | 線形構成則はパッチ・エネルギー・独立剛性で部分確認。`postprocess`, `elements` | 一部。独立した全材料 API、異方性、温度依存物性は未検証 |
| JRSNAP互換 支持ばね（理論7.22） | 手計算表、骨格、劣化記憶、遮断・軟化、経路分割不変、散逸、接線、厳格パーサ、JRSNAP 5.2-L03 goldenとの照合（正側の力1e-12、勾配区分、変位制御の全体解1e-4 kN）、6方向・両制御、機構停止とrollback、入力3経路・hash、混在要素・2次元、HTTP。`materials/test_jrsnap_support_hysteresis.py`, `solvers/test_jrsnap_supports.py`, `io/test_jrsnap_support_input.py`, `io/test_support_law_registry.py`, `integration/test_jrsnap_support_mixed.py`, `regression/test_jrsnap_support_history.py` | 検証済み（記載範囲）。反転を含む履歴とJRSNAPの一致、JRSNAPの負側報告値、荷重増分の不釣合い力は再現・保証しない |
| JR 非線形 2.2, 3.2, 7.9 | 骨格・β・正負・反転・ネスト履歴・仕事・接線・trial/commit/rollback・非有限拒否。`materials/test_jr_hysteresis.py`, `elements/beam/test_nonlinear.py` | 検証済み（JR モデル）。クリープ・緩和等の一般材料モデルは将来 |
| 線形ソルバー 3.1, 7.6, 7.7, 7.11, 10.8, 11.1–11.3 | スケーリング、特異系拒否、微小積、精度保持。`solvers/test_linear.py` | 一部。Cholesky・反復前処理・疎行列の全演算・性能は未検証 |
| Newton 3.2, 7.6, 7.11, 10.8 | 独立三次／耐力上限ばね、JR の制御・残差・収束・失敗復元。`solvers/test_nonlinear.py`, `integration/test_jr_beam_history.py` | 一部。弧長・分岐・後座屈・ラインサーチ評価は将来／未検証 |
| 微小応答の精度 7.6, 7.11, 10.8, 11.3 | 大きな剛体変位下の微小変形、ばね境界写像、構成則と端力回復の区別。`elements/beam/test_precision.py`, `integration/test_beam_precision.py`, `postprocess/test_beam_equilibrium.py` | 検証済み（明示したモデル）。一般条件数・誤差伝播の全保証ではない |
| メッシュ・節点 4.1, 10.2 | 非連続 ID、挿入順と公開 ID、部材／シェル番号衝突、注目点・荷重分割。`integration/test_model_contracts.py`, `io/test_structural_input.py` | 一部。自動メッシュ、品質・接触・一般節点結合は未検証 |
| 適応メッシュ 4.2 | 現行試験の所有先なし | 将来。誤差指標、適応再分割、一般 h/p/hp 最適化 |
| 入力形式 5.1, 7.3 | JSON・原 .fem、厳密な行番号付き拒否、G 省略、材料ケース、厚さ。`io/test_source_input.py`, `test_structural_input.py` | 検証済み（記載契約）。任意の旧データ互換性は一般 FEM 残件 |
| 保存・公開経路 5.1, 5.2, 13.1 | Python/JSON/ファイル/Flask handler の同値、保存読込、結果消去、400/422/500。`io/test_http.py`, `test_model_roundtrip.py`, `test_model_lifecycle.py`, `integration/test_input_routes.py` | 検証済み。配備後のネットワーク E2E ではない |
| 入力・数値エラー 5.2, 7.3, 13.1 | 不正節点・接続・材料、特異系、失敗再解析・後処理例外の伝播。`io`, `solvers`, `materials` | 一部。全 API のエラー分類・全メッセージは未検証 |
| 支持条件 7.4, 7.10, 10.2, 10.3 | 6/3 DOF stride、最後の節点、強制変位 RHS、ばね・反力、Newton 残り変位。並進・回転ばね付きの荷重／変位制御、ゼロ・除荷・負勾配、接線と残差の整合、補償成分。`solvers/test_boundary_conditions.py`, `solvers/test_support_equilibrium.py` | 検証済み（記載条件） |
| 材端・基礎 7.4, 7.10, 10.3 | 材端解放、分布ばね、定場パッチ・双曲関数／半無限解。`integration/test_linear_beam.py`, `test_beam_foundation.py`, `elements/beam/test_foundation.py` | 一部。任意の半剛接合・非線形接合を一般化しない |
| 梁荷重 7.4, 7.10, 10.6 | 集中・分布・モーメント・部分荷重、無効 mark、分割と剛域。`integration/test_linear_beam.py`, `io/test_structural_input.py` | 一部。一般 FEM の保存サンプルに残件あり |
| 温度荷重 7.4, 7.9, 7.10 | 自由熱膨張・拘束熱軸力。`integration/test_linear_beam.py` | 一部。物性の温度依存、一般熱ひずみテンソルは未検証 |
| 面圧 5.1, 7.4, 7.10 | F1/F2、回転、全節点値、合力／モーメント、入力保存、全解析、独立有理解。`io/test_pressure.py`, `elements/shell/test_pressure.py`, `integration/test_pressure_solution.py`, `validation/test_stored_references.py` | 検証済み（対象モデル・入力契約） |
| 断面特性 10.1, 10.4 | 与えられた A/Iy/Iz/J とせん断指定による応答を確認 | 一部。断面形状からの断面係数・ねじり定数・有効断面計算は未検証 |
| 座標変換 7.5, 10.1, 10.5 | 剛体、回転共変性、縦配置、局所／全体系の力・モーメント。`elements`, `integration/test_nonlinear_reference.py`, `postprocess` | 検証済み（明示条件） |
| 応答曲率 | 非線形梁の中央断面の全曲率、局所両曲げ軸、残留・正負履歴、要素分割、各ステップと最終値、Python/JSON/HTTP/結果保存。`integration/test_curvature_output.py`, `elements/beam/test_nonlinear_section.py` | 検証済み（現行中央断面モデル）。材端別・部材平均の曲率は出力しない |
| 応力・内力 7.5, 10.6 | せん断・両面テンソル・端力、結果量の釣合い、原演算子との照合。`postprocess` | 一部。主／相当応力・主／相当ひずみ、接触応力は独立検証なし |
| 変形 10.7 | 軸・曲げ・ねじり、片持ち梁独立解、履歴、分割収束。`integration/test_beam_solutions.py`, `test_nonlinear_reference.py`, `test_nonlinear_convergence.py` | 一部。座屈・横倒れ・一般ねじり変形の全範囲は未検証 |
| 保存参照 | 44 モデル 323 ケース、片持ち梁 101 保存段階、float/Decimal/有理数/原演算子、改変検出。`regression`, `validation`, `harness` | 一般 FEM は既知 308 ケース失敗。材料非線形の数学的検証と外部 JR 較正は別 |
| 性能・並列 6.1, 6.2, 7.7, 7.12, 8.1, 8.2, 11.1, 11.2, 14.1–14.3 | 梁・シェル・ソリッドの13 workloadで位相別時間、Python peak、RSS、同一環境回帰閾値を保存。`validation/test_performance.py` | 一部検証済み。単一process・単一threadの基準は検証。長時間memory leak、並列構築／ソルバー、HTTP同時実行、cache効率、他backendは未検証 |
| 可視化 7.13, 9.1, 9.2 | VTK 生成例と手動実行ツールを保管 | 未検証。描画・品質・境界／荷重表示、VTK の独立検証、変形／応力／モードアニメーション |
| ログ・デバッグ 13.2, 13.3 | 手動ツールは `tools/debug`、生成物は明示出力先 | 未検証。ログ書式・回転・レベル、トレース・性能監視機能 |

旧 6/8/14 と 7.12 は性能・並列へ、7.13/9 は可視化へ、1.5/7.2/7.8/12 は補間・積分へ集約した。旧 7 章の横断項目を新しい重複テストディレクトリにはしない。

未検証の現存機能は[拡充計画](test_plan.md)、既知一般 FEM 不具合は[GF 計画](general-fem-followup.md)で扱う。将来構想のために空のディレクトリや skip だけの試験を作らない。原項目表の詳細条件は原本にすべて残してあり、この表から旧番号で追跡できる。
