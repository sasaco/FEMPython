# Impact Assessment: FrameWebforJSの計算結果が表示されない

## 結論

原因箇所だけを見れば「旧形式へ包み直す」修正に見えるが、既定のHTTP結果を旧case-mapへ置換するのは高リスクである。現行のflat結果は現行FrameWebの初期実装から続く公開契約で、後から文書・HTTPテスト・多数のPython利用箇所に固定されている。一方、フロントだけでflat結果を変換しても、バックエンドが先頭ケースしか解いていないためCt桁のケース2～11を復元できない。

最も安全なのは、`FemModel.run()`と既定HTTPのflat契約を維持し、FrameWebforJSが明示的に要求するversioned/negotiated互換表現を追加する方法である。その互換経路だけで、入力順の全荷重ケースを個別に解析し、旧`disg`/`reac`/`fsec`へ投影する。互換表現の選択を`Content-Encoding`や旧入力形式から暗黙推測してはいけない。これらは表現バージョンではなく、現行クライアントとも共有される transport/input 契約だからである。

## 導入履歴

| 日付・commit | 変更 | 判断 |
|---|---|---|
| 2024-11-25 `da44b42e99ad7097918b0471c278e0f36e5c03d4`（旧FrameWeb2初期commit） | `Controller`が全`inputData.loadCases`を順に処理し、`results[id] = make_result(...)`を構築した。各caseは`disg`/`reac`/`fsec`/`shell_fsec`/`size`を持ち、`main.py`がそのmapをそのまま返した（`C:/Users/sasai/Documents/FrameWeb2/main.py:63-78`, `app/controller.py:90-102`, `app/result.py:135-180`）。 | FrameWebforJSが前提とする旧契約。case IDと順序は入力のload-case挿入順。 |
| 2026-01-30 `12b8ab9f0d457ae52967d9d66296aeb34fac690d`（現行FrameWeb初期commit） | `main.py`が`FemModel.run("static")`の単一結果を直接JSON化するflat HTTP APIとして導入された。現在も`FrameWeb/main.py:106-118`が同じ境界である。 | flat APIは今回の通信修正で生まれた回帰ではなく、別系統として作られた現行契約。 |
| 2026-09-08 `29b9c32b04519870445038602af5c0f9dbe786db` | 旧梁入力対応で`file_io.py`に`select_case(data)`を追加し、`legacy_beam.py`を新設した。既定caseを`next(iter(data['load']))`で選び、`data['load']`をその1件へ置換した（`FrameWeb/src/fem/file_io.py:70-79`, `FrameWeb/src/fem/legacy_beam.py:6-20`）。 | 単一solverへ旧入力を渡すための意図的縮約だが、旧UIの全case契約とは非互換。 |
| 2026-09-09 `46441c5bb6df04e70519f68a19dda0e2eaa027c3` | HTTPテストがflatの`node_displacements`/`element_stresses`/`reaction_forces`と符号を固定した（`FrameWeb/tests/io/test_http.py:18-34`）。 | 既定HTTP契約を置換すると回帰する直接証拠。 |
| 2026-09-09 `d111a0277718f5df81fa3c6c74bfbecff4d423b6` | WikiがHTTPは1ケースのflat objectであり、`case1.disg`/`case1.fsec`を返さないと明記した（`FrameWeb/docs/wiki/results.md:35-43`）。 | flat形式は意図が文書化された公開契約。 |
| 2026-09-16 `be59bd48974b6fd02963ec306b327fad1293fac3` | FrameWebforJSをmonorepoへ取り込んだ時点で、旧case-mapを読む`loadResultData`と3 workerが既に存在した（`app.component.ts:249-289`, `result-data.service.ts:86-95`, `result-disg1.worker.ts:29-40`）。 | 現リポジトリではfrontend以前の履歴は追跡不能だが、統合時点でproducer/consumer契約は不一致。 |

したがって、直接の表示不具合は「最近flat化した」ことではなく、旧UIと現行エンジンを結合した際にversioned response boundaryを設けなかったことにある。今回直した圧縮通信は、この既存の次段不整合を見えるようにしただけである。

## Blast Radius

### 影響を受ける経路

- FrameWebforJSのブラウザ版・Electron版の基本結果表示。成功応答は`ResultData.loadResultData`へ無検証で渡され、displacement/reaction/section-force workerは必要fieldがない項目を全件`continue`して、空mapを`error: null`で返す（`FrameWebforJS/src/app/components/result/result-disg/result-disg1.worker.ts:29-40,105-114`, `result-reac/result-reac1.worker.ts:27-40,95-103`, `result-fsec/result-fsec1.worker.ts:41-69,244-253`）。
- 基本ケースが空なので、その後段のDEFINE・COMBINE・PICKUP結果、結果表、3D変形・反力・断面力描画、帳票用結果も連鎖的に空または利用不能になる。case一覧構築もtop-levelのflat keyをcase名として受け取る（`FrameWebforJS/src/app/providers/result-data.service.ts:87-101`）。
- Ct桁presetはload ID `1`～`11`をこの順に持ち、全caseで異なる節点荷重または部材荷重を持つ。現行`select_case`ではcase 1しか解析しないため、単純に`{"1": flat}`と包むだけでは10 caseを欠落する。
- 同じ旧`node`/`load`形式をFrameWebforJSから送る全モデルが対象であり、Ct固有ではない。複数case入力ほどデータ欠落が大きい。

### 変更時に守る必要がある現行経路

- `FemModel.run()`のflat結果は、少なくとも43個のbackend test fileと、5個の`FrameWeb/src/fem`実装file、5個のvalidation tool、`scripts/smoke-local.py`から直接参照される。solverや`FemModel.run()`自体を旧case-mapへ変更すれば最大のblast radiusになる。
- root HTTPのflat結果は`FrameWeb/tests/io/test_http.py:18-34`、`FrameWeb/tests/io/test_compressed_transport.py:59-87`、`scripts/smoke-local.py:92-99`およびWikiの公開例が利用する。endpointだけを無条件に旧形式へ替えても既存clientを破壊する。
- plain JSON、canonical compressed JSON byte array、legacy compressed CSVはtransportの違いであり、結果表現の選択信号ではない。`gzip`対`gzip,base64`で結果schemaを切り替えると、圧縮互換テストと外部clientの意味が変わる。
- `FrameWeb/src/app/result.py:113-145`には旧名称を生成する残存helperがあるが、production callerはrepository searchで0件だった。これは旧`FEMCalculation` object向けで、新`FemModel.run()`のdictへそのまま接続できない。
- `FrameWeb/tests/support/section_cut_view.py:6-107`は現行flat結果を旧比較viewへ変換し、sample/reference系6 fileから使われる。変位・反力・梁端力の変換規則を再利用できる有力な仕様証拠だが、test helperであり、全case orchestration、`rate`、正規のerror/HTTP contractを持たない。さらに旧名の`shell_fsec`ではなく`shell_results`を出し、2Dでは合成reaction key `0`を加えるため、helper全体を無検証でproductionへ移してはいけない。

### 影響しない経路

- GET/OPTIONS、request圧縮decoder、responseのBase64+gzip transportはresponse schemaより外側にあり、互換表現追加のために変更する必要がない。
- 現行Python API、結果保存/読込、nonlinear/modal/solid/shellのflat結果は、互換projectionをHTTP境界の明示的opt-inに限定すれば不変にできる。
- PDF print APIは別のC# endpointと別契約であり、今回のresponse schema変更対象ではない。

## 代替案と回帰リスク

### A. 既定backend responseを旧case-mapへ全面置換 — 高リスク

全caseを一箇所で計算できる利点はあるが、現行HTTP client・文書・testを破壊する。さらに`FemModel.run()`まで変更するとbackend内部の広いflat契約を壊す。採用しない。

### B. frontendだけでflat結果を旧形式へ変換 — 高リスクかつ不完全

受信済みのcase 1は変換できるが、解析されていないcase 2～11は復元不能である。frontendが11回requestを作る案は、caseごとの定義選択・安定した部材分割・符号変換・error集約をTypeScript側へ重複実装し、通信量と失敗点を増やす。旧動作の復元にならないため採用しない。

### C. 既定flatを維持し、明示的なversioned互換responseを追加 — 中リスク、推奨

FrameWebforJSが専用endpoint、media type、または明示的version headerで旧case-mapを要求する。backendは入力loadの挿入順でfresh modelをcaseごとに作り、各flat結果をproduction互換projectorへ渡す。既定指定なしは現在とbyte/semantic互換のflatを返す。

主な残存リスクと対策:

- **計算量:** case数だけ解析時間が増える。Ctは11 solveである。compatibility modeに限定し、case上限・request timeout・性能計測を設け、同一`FemModel`の可変stateをcase間で再利用しない。
- **case固有定義:** `element`/`fix_node`/`joint`/`fix_member`参照はcaseごとに選択する。Ctではすべて1だが、異なる参照を持つ2-case fixtureを追加する。
- **部材分割順序:** `select_case`が保存する`_all_member_loads`により全caseの荷重点を共通分割し、`original_id`と幾何i→j順で`P1..Pn`を安定化する。case間でsegment key/orderが揺れないことをassertする。
- **単位・符号:** wire上の`disg`はm/radを保持し、frontend first workerだけがmm/mradへ1000倍する（`result-disg1.worker.ts:59-70`）。`reac`は`fx/fy/fz`を`tx/ty/tz`へ改名し符号を維持する（`result-reac1.worker.ts:54-68`）。`fsec`は現行local end配列`[fx,fy,fz,mx,my,mz]`へ、i端`[-,+,+,-,-,+]`、j端`[+,-,-,+,+,-]`の係数を適用し、`L`とともに`P1..Pn`へ並べる（`FrameWeb/tests/support/section_cut_view.py:63-76`）。
- **rate:** 旧`make_result`はcaseの`rate`を`disg`/`reac`/`fsec`へ出力時に一度だけ適用した（`C:/Users/sasai/Documents/FrameWeb2/app/result.py:165-180,205-212,284-291,317-366`）。Ctは全caseで1だが、非1 fixtureで二重適用・未適用を検出する。非線形の場合にload自体をrate倍するのは旧意味と等価でない。
- **shell:** 旧mapには`shell_fsec`もある。今回の表示受入は`disg`/`reac`/`fsec`だが、旧契約を名乗るならshell mappingの仕様決定と別testが必要である。未定のまま空値を「互換」としない。
- **部分成功:** 途中caseで失敗した場合は部分mapをHTTP 200で返さず、既存diagnostic statusを保ってcase ID付きで原子的に失敗させる。
- **schema drift:** frontendはworker起動前にtop-levelが非空case mapで、各case objectに`disg`/`reac`/`fsec`が存在することを検証する。不適合flat responseを空成功にせず、`isCalculated`をfalseのまま明示errorにする。

## 最小の実行可能な受入テスト

### 1. Transport gate（表示契約と分離）

`FrameWeb/tests/io/test_compressed_transport.py`をそのまま実行し、旧CSV/canonical array、`gzip`/`gzip,base64`、plain JSONが引き続きdecodeされることを確認する。ここでのHTTP 200はtransport成功だけであり、表示成功とは扱わない。

```powershell
cd FrameWeb
uv run --locked --extra dev pytest tests/io/test_compressed_transport.py -q
```

### 2. Production projector unit tests

新しいproduction projectorを直接testし、手計算可能な小規模beam fixtureで次をexact/`pytest.approx`比較する。

- `disg`: node label、6成分、m/radのまま。frontendでのみ各成分×1000。
- `reac`: `fx/fy/fz -> tx/ty/tz`、moment名、符号維持、補助拘束node除外。
- `fsec`: original memberへの集約、幾何i→j、`P1..Pn`、区間`L`、両端6成分の上記sign vector。
- notice point、rigid境界、load分割点が混在してもsegment順と合計長が一定。
- `rate=2.5`が全3結果へ一度だけ反映される。
- 可能なら旧FrameWeb2が生成した固定goldenも比較し、同じprojector由来の自己整合testだけにしない。

### 3. Backend representation integration

新規compatibility testで以下を同時に固定する。

- version指定なしのplain/compressed requestは従来flat key/valueを返し、`test_http.py`、`test_compressed_transport.py`、`smoke-local.py`が変更なしでPASSする。
- 明示version指定時だけ外側case mapを返す。
- frontendのproduction normalizationを通したCt requestは、順序付きkeyがexactに`["1",...,"11"]`である。
- 11 caseすべてで`disg`/`reac`/`fsec`が存在し、それぞれ非空である。case 1の複製11個ではなく、異なるloadを持つcase間で選定値が異なる。
- 各caseを独立に`select_case(input, case_id)`して得たflat solveと互換projectionが一致する。
- 途中case failureは部分mapを返さず、既存の4xx/422/500分類で失敗する。

### 4. Frontend schema/fail-fast tests

`ResultData.loadResultData`へ現行flat fixtureを渡すtestは例外/errorを期待し、3 workerが空map successを返さないこと、`ResultData.isCalculated`と結果linkがfalse/disabledのままであることを確認する。正しい2-case fixtureでは全worker完了後のみcalculated状態になり、case IDを保持する。

```powershell
cd FrameWebforJS
npm test -- --watch=false --browsers=ChromeHeadless
```

### 5. Ct browser E2E（最終ユーザー受入）

local launcherで実backendとAngularを起動し、`サンプル（Ct桁）.json`を読み込んでproductionの`InputData.getInputJson(0)`から計算する。network responseの復号後にcase 1～11と非空3 fieldをassertし、画面では少なくともcase 1と11について変位・反力・断面力ページを順に開いてrow/描画が非空であることを確認する。

数値testは、選んだnode/memberについて、変位表示=`wire m/rad * 1000`、反力表示=`wire tx/ty/tz/mx/my/mz`（符号維持）、断面力表示=`P`順かつworkerの2桁丸め後の値、をbackend/旧goldenと照合する。success alert、link enable、worker completionの順もassertし、console/worker errorと通信errorがないことを確認する。

このE2EがPASSするまで、transport testやbackend HTTP 200だけで「表示修正済み」と判定してはいけない。

## External Research

不要。第三者libraryの不具合ではなく、repository内の二つの独自response contractを結合した際の不整合である。

## Codex Status

Codexはこのsessionでは利用不能。lead側の2回に続き、この調査の低effort・120秒上限のread-only consultationも有効な回答を返さず中断された。再試行していない。

- prompt: `.agents/logs/codex/prompt-framewebforjs-results-impact-risk.md`
- wrapper copy: `.agents/logs/codex/20260918T053953Z-framewebforjs-results-impact-risk.prompt.md`
- response artifact: `.agents/logs/codex/20260918T053953Z-framewebforjs-results-impact-risk.md`（0 bytes、verdictなし）

推奨C案とsafeguardはCodex verdictではなく、git history、旧/current code、既存test、実再現からのimpact investigator判断である。
