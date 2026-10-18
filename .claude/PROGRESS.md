# PROGRESS

> Auto-maintained by /checkpointing. Shows the most recent 5 checkpoints (newest first).
> Full checkpoints live in `.agents/checkpoints/` (git-ignored).

## [2026-09-18-055240](.agents/checkpoints/2026-09-18-055240.md)

### 何をしたのか

- FrameWebforJSのCt桁計算で発生していた通信エラーを、旧clientのBase64(括弧なし10進CSV)と現行Base64(JSON byte array)の両方を安全に受けるbackend decoderで修正した。`eval` / `literal_eval` は使わず、厳格なbyte検証を維持した。
- `FrameWeb/main.py`、`FrameWeb/tests/io/test_compressed_transport.py`、Node/pako fixture、HTTP endpoint文書を更新した。focused 110件、関連185件がPASSし、`main.py` coverageは84%。小規模modelでは旧/current envelopeがともにHTTP 200かつ同値だった。
- 通信修正後に「計算できたがfrontendへ表示されない」問題を旧版 `C:/Users/sasai/Documents/FrameWeb2` と比較診断した。現行responseは単一caseのflat schema、旧frontendは全case mapの各valueに `disg` / `reac` / `fsec` を要求しており、workerが不一致を空成功として黙って捨てることをdirect reproductionで確認した。
- Ct桁にはload case 1～11があるが、現行legacy loaderは先頭caseだけを選択するため、単純にcase 1で包むだけでは修復できないと確定した。
- 診断、root-cause、impact、bug reportを作成し、`STATE.md`へ構造化したbug-fix blockを追加した。表示互換adapter自体は未実装。

### どういうやり取りをユーザーと行ったのか

- ユーザーは最初にCt桁計算の通信エラー調査を依頼し、旧版backendとの差を平易に説明した後、旧版に合わせる修正方針を承認して実装を依頼した。
- 通信修正後、ユーザーから「計算はできたようだがfrontendに表示されない」と追加報告があり、旧版を参照した問題診断を依頼された。
- 今回は表示問題を診断し、次セッションへ `handoff` することが明示されたため、製品実装には進まず、原因・安全な修正境界・受入テスト・未確定事項を引き継いだ。

### どうやったのか

- 旧backendの `main.py`、`Controller`、`Result` と、現行backendの `main.py`、`file_io.py`、`legacy_beam.py`、frontendのresult providerおよびdisplacement/reaction/section-force workersをコード行単位で比較した。
- 実transport fixtureを現行backendへ直接送る再現scriptを作り、HTTP 200、flat result key、frontend互換case 0件をassertして、transport後のresponse-contract failureを切り分けた。
- git historyと既存HTTP test/wikiを調べ、現行flat schemaが公開済みcontractであることを確認した。推奨方針は、既定flatを保ち、FrameWebforJSが明示的に要求するversioned `legacy-cases-v1` compatibility pathで全caseを解析・旧schemaへ投影する方法。
- frontend側にはschema fail-fastを加え、不適合responseを `isCalculated=true` の空成功にしない計画とした。Ct 11 case、単位・符号・rate・P順序、途中失敗の原子性、実browser表示を受入gateにした。
- troubleshootのroot-cause/impact分担調査を統合し、diagnosis contractとworkspace artifact contractを検証した。

### 途中でどういう課題が起こったのか

- Codex read-only consultationはlead 2回、root-cause 1回、impact 1回がtimeoutまたは空responseで、正式な検証verdictを得られなかった。診断はdirect reproduction、旧/current/frontend code、git history、既存testsの証拠のみを根拠とした。
- troubleshootの汎用repro wrapperはWindowsで長い `-c` のquoting、cp932 decode、WSL `/bin/bash` 不在により利用できなかったため、direct Python scriptの結果だけを製品証拠とした。
- Browser backendが接続されていないため、Ct桁の実画面navigationはまだ確認できていない。
- `shell_fsec` は旧契約に存在するがCt桁にはshellがなく、現行test helperも異なるschemaである。一般shell互換を名乗るには別の旧backend oracleが必要。
- 全11 caseを順次解析するruntime/memory/timeoutと、case途中失敗時のpolicyは実装時に計測・固定が必要。

### 将来のアクション

- まず `.agents/logs/troubleshoot-framewebforjs-results-not-displayed-diagnosis.md` とroot-cause/impact reportを読み、`legacy-cases-v1` の選択方法（専用endpoint、media type、version header）を確定する。
- test-firstで「既定flat response不変」と「明示version時だけordered case map」を追加し、Ctがexactにcase 1～11を返し、全caseの `disg` / `reac` / `fsec` がnon-emptyかつ旧backend代表値と一致することを固定する。
- backendへ全case orchestrationとproduction legacy projectorを追加する。fresh model/case、入力順、単位、符号、rate、member `P1..Pn`、原子的errorを守る。
- FrameWebforJSをversioned responseへopt-inさせ、worker前schema validationとユーザー向けerrorを追加する。
- transport/HTTP/backend/frontend testに加え、実browserでCtのcase 1と11の変位・反力・断面力、DEFINE/COMBINE/PICKUP、console/worker errorなしを確認してから表示修正完了とする。
