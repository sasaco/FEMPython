## Bug Report: FrameWebforJS results not displayed after successful calculation

### Error
- Message: HTTP 200で計算完了扱いになるが、変位・反力・断面力がフロントへ表示されない。明示例外やダイアログは発生しない。
- Location: `FrameWebforJS/src/app/components/result/result-disg/result-disg1.worker.ts:29-39`（反力・断面力workerにも同型処理）。
- Stack trace: なし。workerは不適合項目を`continue`し、空結果を`error: null`で返す。

### Reproduction
- Steps:
  1. `.agents/logs/repro-framewebforjs-results-not-displayed.py`から実HTTP経路と同じ圧縮要求をFlask test clientへ送る。
  2. HTTP 200の圧縮応答を復号する。
  3. トップレベル結果と、フロントが要求する`disg/reac/fsec`を持つ荷重ケースを数える。
  4. HTTP 200、現行flat結果キー9個、互換荷重ケース0件を確認する。
- Reproducibility: always。直接実行はexit 1で`backend response has no frontend-compatible result case`。`troubleshoot/repro.py`自体はWindows上でWSL `/bin/bash`不在のため利用不能だったので、そのログは製品再現証拠に使わない。

### Immediate Context
- Failing code: 現行バックエンドは`FemModel.run()`を1回だけ実行してflat結果をそのままJSON化する（`FrameWeb/main.py:106-118`）。Angular側はその値を無検証で3 workerへ渡し、各workerが`disg/reac/fsec`のない全項目を読み飛ばす。
- Call chain: `FrameWeb/main.py:FEMPython` → `FemModel.run` → `result_to_jsonable` → Angular `post_compress` success → `InputData.getResult` → `ResultData.loadResultData` → `result-disg/reac/fsec` workers → empty success。
- Recent changes: 通信互換層の追加により旧CSV要求は通過するようになったが、結果スキーマは変更していない。flat結果は既存の現行HTTPテストで意図的に固定されている。

### Affected Area
- Files involved: `FrameWeb/main.py`、`FrameWeb/src/fem/file_io.py`、`FrameWeb/src/fem/legacy_beam.py`、`FrameWebforJS/src/app/app.component.ts`、`FrameWebforJS/src/app/providers/result-data.service.ts`、3つのresult worker、旧`FrameWeb2/main.py`・`app/controller.py`・`app/result.py`。
- Related tests: 現行flat HTTPテストと圧縮通信テストはPASSするが、旧case map、Ctの11ケース、workerの空成功、画面描画を検出するテストは存在しない。再現スクリプトは期待どおりFAILする。

### Initial Hypotheses (informed by Codex analysis)
1. レスポンス契約不一致: 現行flat結果と旧case別`disg/reac/fsec`の不一致が直接原因。直接再現とworker処理が一致する。— Codex confidence: high（2回のpartial応答で一致。ただし両consultともtimeoutのため正式検証扱いではない）
2. 複数荷重ケース実行欠落: Ctは11ケースだが現行legacy読込は先頭ケースだけへ縮約するため、単純なcase wrapperでは旧動作を復元できない。— Codex confidence: high（partial応答とコード証拠が一致）
3. 完了状態の早期設定: worker完了前の`isCalculated=true`が症状を増幅するが、待機しても入力スキーマが不一致なので主原因ではない。— Codex confidence: medium（partial応答とコード証拠が一致）

### Codex Pattern Recognition
- Error pattern: producer/consumer type-contract mismatch with silent filtering; multi-case orchestration and compatibility projection are missing at an integration boundary。
- Known similar patterns: API移行で旧consumerが未知フィールドを無視し、空データを成功として確定するsilent schema drift。
- Recommended investigation priority: 旧版の全ケース生成契約 → 現行`select_case`縮約 → 本番互換変換の配置 → workerのschema fail-fast → Ct 11ケースE2E。Codex CLIは2回とも応答ファイルを残したがtimeoutしたため、正式なPASS/validationとしては使わない。
