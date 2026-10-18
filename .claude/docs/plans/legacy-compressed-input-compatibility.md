## Implementation Plan: Legacy Compressed Input Compatibility

### Purpose
現行FrameWebバックエンドが、旧 `C:\Users\sasai\Documents\FrameWeb2\main.py` で利用できたFrameWebforJSの括弧なし10進CSV圧縮入力を、コード評価を行わない厳格な字句・型検証で受理できるようにする。旧版の実行可能な `eval` は復活させず、現行のJSON整数配列入力も維持する。本計画でいう安全性はコード実行防止と輸送形式の検証を指し、HTTP本文量・gzip展開量のDoS上限新設は既存契約を変える別課題として明示的に対象外とする。

### Scope
- New files: `FrameWeb/tests/io/test_compressed_transport.py`, `FrameWeb/tests/data/transport/legacy-browser-envelope.json`
- Modified files: `FrameWeb/main.py`, `FrameWeb/docs/wiki/endpoints.md`
- Read-only verification inputs: `FrameWebforJS/src/app/app.component.ts`, `FrameWebforJS/src/assets/preset/サンプル（Ct桁）.json`, `.agents/logs/repro-framewebforjs-calculation-communication-error.cjs`
- Dependencies: Python標準ライブラリの `base64`、`binascii`、`gzip`、`json`、`zlib` と既存の `InputValidationError`／`diagnostic_payload`。新規依存関係は追加しない。
- In scope: Base64後が `[31,139,...]` の現行JSON整数配列と、`31,139,...` の旧ブラウザー形式の両方を受理するバックエンド互換処理、入力検証、HTTP診断、文書化、回帰試験。
- Out of scope: `FrameWebforJS` の送信処理、C#印刷API、計算結果の `node_displacements/reaction_forces/element_stresses` と `disg/reac/fsec` の変換、複数荷重ケース仕様、圧縮プロトコル全体の再設計。

### Implementation Steps

テストで旧入力の互換契約と拒否境界を固定してから、バックエンドの最小実装、既存回帰、文書化、実フロント形式の受け入れ確認の順に進める。

#### Step 1: 旧形式と拒否境界をテストで固定する
- [ ] `FrameWeb/tests/io/test_compressed_transport.py` を追加し、同じgzipバイト列から現行JSON整数配列と旧括弧なしCSVを生成するテストヘルパーを用意する。
- [ ] `FrameWebforJS` の実際の `pako.gzip()` → `btoa(Uint8Array)` と同じNode式で小さな固定JSONから `FrameWeb/tests/data/transport/legacy-browser-envelope.json` を生成し、元JSON、Base64本文、生成式／Node・pako版を保存する。このNode生成fixtureをPythonの自動回帰試験で必須入力として読み、単なるPython模倣だけで済ませない。
- [ ] `Compressor.decompress()` が両形式を同じ辞書へ復元することを要求する。旧形式のテストは実装前に現在の `Extra data: line 1 column 3` で失敗することを確認する。
- [ ] Flask `test_client` で、`Content-Encoding: gzip` × JSON整数配列、`gzip` × 旧CSV、`gzip,base64` × JSON整数配列、`gzip,base64` × 旧CSVの4組を明示的にテストし、すべて同じ正常結果を返すことを要求する。最後の組は実フロントの形式として、既存Node再現スクリプトまたはそこから採取した要求本文でも照合する。
- [ ] JSON配列側について、配列以外、`bool`、浮動小数、文字列、入れ子、負数、256以上を拒否するパラメータ化テストを追加する。`bool` はPython上で `int` の派生型なので、`isinstance` ではなく厳密型判定が必要であることを固定する。
- [ ] 旧CSV側について、空入力、空トークン、先頭／末尾／二重カンマ、空白、符号、少数、小数点、指数表記、非ASCII、1トークン4桁以上の巨大数値文字列、範囲外値を拒否するテストを追加する。
- [ ] 不正Base64、gzipでないデータ、破損／途中切れgzip、展開後の不正JSONをHTTP 400・`error_code=invalid_input` とするテストを追加する。
- [ ] 展開後JSONが構文上正しくてもトップレベルが配列、文字列、数値、真偽値、`null` の場合は、`Compressor.decompress()` の戻り値契約に反するためHTTP 400として拒否するテストを追加する。
- [ ] `1+1` や `__import__(...)` などのコード風入力を拒否し、実行経路が存在しないことを回帰試験として固定する。
**Verification**: `cd FrameWeb; uv run --locked --extra dev pytest tests/io/test_compressed_transport.py -q` を実行する。実装前は `test_decompress_accepts_legacy_browser_fixture`、旧CSVの4ヘッダー組のうち2組、厳格Base64／型／範囲／トップレベルobject／安定400のテストがFAILし、`test_decompress_accepts_canonical_json_byte_array`、`test_http_accepts_canonical_json_byte_array`、既存の通常JSON経路、コード風入力の非実行はPASSすることを記録する。失敗IDがこの境界から外れた場合は実装へ進まない。

#### Step 2: `Compressor.decompress` に厳格な二形式パーサーを実装する
- [ ] `FrameWeb/main.py` の `Compressor` 内に、Base64後のバイト列をgzipバイト配列へ変換する小さな非公開ヘルパーを抽出する。`decompress()` は `base64.b64decode(data, validate=True)` による厳格Base64復号、外側形式の解析、gzip展開、内側JSON解析を明確に分ける。
- [ ] 最初に `json.loads()` で現行JSON整数配列を解析し、`JSONDecodeError` の場合だけ旧ASCII十進CSV解析へフォールバックする。JSONとして解析できたが型が不正な入力はCSVへ回さず、その場で拒否する。
- [ ] 旧CSVはASCIIの10進数字とカンマだけを字句検証し、各トークンが1～3桁かつ非空であることを確認した後だけ `int(token, 10)` で変換する。これによりPythonの整数文字列桁数制限に起因する未分類 `ValueError` も変換前に排除する。`eval`、`ast.literal_eval`、式の解釈、検証前の数値変換は使用しない。
- [ ] どちらの形式でも、コンテナが一次元であり、各値が `type(value) is int` かつ `0 <= value <= 255` であることを共通検証してから `bytes(...)` を呼ぶ。
- [ ] `binascii.Error`（Base64）、`UnicodeDecodeError`（外側ASCII／内側UTF-8）、外側・内側の `json.JSONDecodeError`、`gzip.BadGzipFile`、`EOFError`、`zlib.error` だけを各段階で `InputValidationError` に変換し、安定したHTTP 400 `invalid_input` にする。外側の `JSONDecodeError` だけがCSVフォールバックを起動し、内側JSONの失敗はフォールバックさせない。予期しない例外を一括して400へ隠さない。
- [ ] 展開後のJSONが `dict` であることを明示的に検証し、構文上正しい非object値も `InputValidationError` とする。
- [ ] 旧コメント「旧FWそのまま」を、現在は安全な互換実装であることが分かる説明へ更新する。
**Verification**: Step 1の全テストをGreenにし、`rg -n "eval\(|literal_eval" FrameWeb/main.py FrameWeb/tests/io/test_compressed_transport.py` が該当なしであることを確認する。

#### Step 3: 既存契約を壊していないことを回帰確認する
- [ ] 現行JSON整数配列を使う既存4経路を再実行し、通常JSON、圧縮HTTP、レスポンス圧縮、解析結果が変わらないことを確認する。
- [ ] `Content-Encoding: json` およびContent-Encoding省略時の通常JSON経路が従来どおり動作することを確認する。
- [ ] 旧CSVと現行JSON配列へ同一モデルを送った結果を比較し、少なくともステータス、解析種別、主要結果、エラー分類が一致することを確認する。
**Verification**: `cd FrameWeb; uv run --locked --extra dev pytest tests/io/test_compressed_transport.py tests/integration/test_input_routes.py tests/integration/test_spatial_general_plane.py tests/io/test_axial_force_input.py tests/regression/test_slip_support_history.py tests/io/test_http.py -q` を成功させる。続けて変更対象へ `uv run --locked --extra dev ruff check main.py tests/io/test_compressed_transport.py` と `uv run --locked --extra dev ruff format --check main.py tests/io/test_compressed_transport.py` を実行する。

#### Step 4: 公開HTTP契約を二形式対応として文書化する
- [ ] `FrameWeb/docs/wiki/endpoints.md` の「互換用の圧縮転送」を、現行JSON整数配列が正規形式で、旧括弧なしCSVも互換受理することが分かる記述へ更新する。
- [ ] 旧CSV対応は `eval` の復活ではなく、ASCII十進バイト列だけを安全に解析する互換層であることを明記する。
- [ ] 要求と成功応答の非対称性、エラー応答は非圧縮JSONであること、および通常JSON経路を推奨する現行説明を維持する。
**Verification**: Wiki掲載の正規JSON配列サンプルと旧CSVサンプルに対応するテストを `uv run --locked --extra dev pytest tests/io/test_compressed_transport.py -q` で実行し、両方が文書どおりに処理されることを確認する。`git diff --check -- FrameWeb/docs/wiki/endpoints.md` と `rg -n "JSON整数配列|括弧なしCSV|eval" FrameWeb/docs/wiki/endpoints.md` でMarkdown差分と必須説明を確認する。存在しない包括的ドキュメント検証コマンドはゲートにしない。

#### Step 5: 実フロント形式で輸送修復を受け入れ確認する
- [ ] リポジトリ直下で `dotnet run --project tools/FrameWeb.Startup` を実行し、`(Invoke-RestMethod http://127.0.0.1:7071/health/ready).status` が `ready`、解析が `127.0.0.1:8080`、フロントが `127.0.0.1:4200` であることを確認する。検証後は同じコンソールでCtrl+Cを送り、起動プロジェクトと子プロセスを停止する。
- [ ] `http://127.0.0.1:4200/` で `サンプル（Ct桁）.json` を開いて計算し、ブラウザーが生成した `Content-Encoding: gzip,base64` 要求を実バックエンドへ送る。自動回帰側ではNode生成fixtureを必須入力としているため、ここではUI統合と実際の要求経路を確認する。
- [ ] 従来の `HTTP 400 / Extra data: line 1 column 3` が消え、少なくともgzip展開と内側JSON解析を通過したことをサーバー応答またはテスト計測で確認する。
- [ ] 小さな既知の有効モデルでは旧CSVと現行JSON配列の両方がHTTP 200となり、同じ解析結果を返すことを確認する。
- [ ] Ct桁については、輸送層を通過しただけで「計算全体の修復完了」と報告しない。別課題である結果スキーマ／複数荷重ケース契約が未解決なら、その次のエラーまたは空結果を独立した残課題として記録する。
**Verification**: 輸送修復の完了条件は、(1)旧CSVが400 `Extra data`にならない、(2)正規JSON配列も継続して通る、(3)不正入力は安定して400、(4)コード実行経路なし、の4点。ユーザー向けの計算完了条件は、Ctの変位・反力・断面力と荷重ケース表示まで通ることとし、本計画の輸送完了条件とは分離する。

### Risks & Considerations
- 二つの外側形式を持つため入力面が増える。JSON失敗時だけ厳格CSVへフォールバックし、共通の一次元・厳密整数・0～255検証を通すことで範囲を限定する。
- `bool` は `int` として扱われ得るため、`isinstance(value, int)` では不十分。厳密型判定をテストで固定する。
- フォールバックを広い `except Exception` で行うと実装不良を旧形式として誤認する。捕捉は `JSONDecodeError` と既知の輸送例外に限定する。
- 旧版との互換性は「実際のFrameWebforJSが送る10進CSV」に対して提供する。旧 `eval` が受理できた任意のPython式との互換性は意図せず、明示的に拒否する。
- `validate=True` は改行や空白を含む非標準Base64を新たに拒否する。現行JSON配列クライアントと実フロントはいずれも改行なしの標準Base64を生成することを4組テストで固定し、未知の非標準クライアントまで互換対象とはしない。
- Base64やgzipのサイズ上限は現行にも存在しない既存のDoS課題である。本計画の「安全」はコード評価防止と厳格な輸送形式検証に限定し、DoS耐性を受け入れ条件に含めない。今回、根拠のない閾値で大型プリセットを壊さず、最大プリセットで時間・メモリを計測したうえでHTTP層の上限を別のセキュリティ課題として決定する。
- `Content-Encoding` は現行実装で `json` 以外をすべて圧縮経路へ送る。今回の互換修正でヘッダー判定まで広げず、`gzip` と実フロントの `gzip,base64` の両方を回帰試験する。
- 輸送修復後に結果スキーマの不一致が顕在化する可能性がある。HTTP 200やエラーダイアログ消失だけをCt計算全体の成功判定にしない。
- 作業ツリーには既存の `.agents` 変更があるため、実装時は `FrameWeb/main.py`、新規テスト、エンドポイント文書以外を変更しない。

### Open Questions
- 実装開始を妨げる未決事項はない。本計画では旧括弧なしCSVを、明示的な廃止判断が行われるまでバックエンドの互換契約として維持する。
- 将来FrameWebforJSを正規JSON整数配列へ移行するか、より小さいversion付きraw-gzip形式へ移行するかは別計画とする。
- HTTP要求サイズ・gzip展開サイズの上限値は、全プリセットとデプロイ環境の計測値が必要なため、今回の互換修正とは別のセキュリティ判断として記録する。
- Ct桁の最終的な複数荷重ケース出力をどの層で `disg/reac/fsec` へ変換するかは、輸送修復後の独立した仕様決定とする。
