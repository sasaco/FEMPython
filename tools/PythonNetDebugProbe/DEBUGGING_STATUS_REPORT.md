# Visual Studio 2026 + pythonnet 混合デバッグ調査レポート

作成日: 2026-09-27

## 相談したいこと

C# から pythonnet で呼び出す Python 関数へ、Visual Studio の F11 でステップインし、Python のブレークポイントとローカル変数の Watch を使いたい。手動で 3 種類のデバッグエンジンを選んでプロセスにアタッチすると成功する。一方、Visual Studio の F5 起動や自動アタッチの試作では、C# の呼び出し行から F11 を押しても Python に入らず、Python のブレークポイントに「このドキュメントのシンボルが読み込まれていません」と表示される。F5 相当の一操作で再現性よく混合デバッグを開始する方法を知りたい。

## 環境と最小再現プログラム

- Visual Studio Community 2026 18.10.2。［Python 開発］ワークロードと［Python ネイティブ開発ツール］をインストール済み。
- Windows、.NET 10、x64、NuGet `pythonnet` 3.1.0。
- `uv` 管理の CPython 3.12.12。Python DLL は `FrameWeb/.venv/pyvenv.cfg` の `home` から特定し、`Runtime.PythonDLL` に設定する。
- Python DLL は `python312.dll`。同じビルドの `python312.pdb` を DLL と同じフォルダーに配置済み。PDB の一致は PE CodeView の RSDS GUID と age で照合した。**現時点で失敗時の Visual Studio「モジュール」ウィンドウにおける `python312.dll` のシンボル状態は未採取**。
- 検証コード: [`PythonNetDebugProbe.csproj`](PythonNetDebugProbe.csproj)、[`Program.cs`](Program.cs)、[`probe_calculation.py`](probe_calculation.py)。アプリ本体 `FrameWebforCS` にはまだ接続していない。
- `Program.cs` は Python 初期化後に Enter 入力を待ち、42 行目の `calculate.Invoke(new[] { span, load })` で Python 関数を呼ぶ。`probe_calculation.py` 2 行目が Python ブレークポイント。入力は `span=7`, `load=5`、戻り値は `27`。

## 成功した手順と観測結果

1. 検証プログラムをデバッガーなしで起動し、Enter 待ちにする。
2. Visual Studio の［プロセスにアタッチ］でその PID を選び、コードの種類を **Python (native)、ネイティブ、マネージド (.NET Core/.NET 5 以降)** に指定してアタッチする。
3. 初回例外として `coreclr.dll` 内の `0xC0000005` アクセス違反が表示されたことがある。F5 で続行した後もプロセスは動作した。この例外の原因は未特定。
4. Enter を押すと `Program.cs:42` の C# ブレークポイントで停止。そこで F11 を押すと `probe_calculation.py` へ移った。
5. Python 2 行目のブレークポイントで停止し、Watch で `span = 7` を確認。F10 後に `combined = 12` も確認した。`Python result: 27` が出力され、終了コードは 0。

したがって、この PC で pythonnet を介した C# → Python のステップインと Python 変数の Watch は**手動アタッチでは実証済み**。Python コードや Python DLL の恒久的な不具合と決めつけるべきではない。

## 失敗した経路と観測結果

| 起動・アタッチ方法 | C# 42 行目 | F11 | Python 2 行目の表示 |
|---|---|---|---|
| 上記の手動 3 エンジンアタッチ | 停止 | Python ソースへ移動 | 停止、Watch 可能 |
| Visual Studio の通常の F5 起動。`launchSettings.json` の `debugEngines` は `managed-dotnet,native` | 停止 | C# 43 行目へ進む | 「このドキュメントのシンボルが読み込まれていません」 |
| F5 起動プロファイルの `debugEngines` に Python (native) エンジン GUID を追加した試行 | 停止 | C# 43 行目へ進む | 有効化できず。試行プロファイルは撤去済み |
| [`StartMixedDebug.ps1`](StartMixedDebug.ps1) で起動し、EnvDTE `Process2.Attach2` へ 3 エンジン GUID を渡して自動アタッチした直近 2 回 | 停止 | C# 43 行目へ進む | 「このドキュメントのシンボルが読み込まれていません」 |

自動アタッチの試作では、スクリプトの「3 エンジンでアタッチした」という出力は **API 呼び出しが戻ったこと**を意味する。Visual Studio 側で Python (native) エンジンの初期化とシンボル読み込みが完了した証拠にはならない。以前、同じ EnvDTE API によるアタッチで、対話的に起動したプロセスへ F11 で入れた実行も 1 回ある。成功・失敗の差がプロセスのコンソール有無、標準入出力のリダイレクト、アタッチ直後に Enter を送るタイミング、エンジン初期化、または別の条件にあるかは未検証。

`Shift+Alt+P` の再アタッチも試したが、この環境では新しい PID に自動再アタッチせず、［プロセスにアタッチ］ダイアログが開いた。コードの種類の指定は保持された。

## 調査時に区別したい点

- `probe_calculation.py` の警告文だけでは、`python312.pdb` 自体の不一致、Python (native) デバッグエンジンの未起動、ソースパス不一致、ブレークポイント未バインドのいずれかを特定できない。
- 成功した手動アタッチと失敗した自動アタッチの両方で、C# のブレークポイントは正常に機能した。C# 側の PDB が読めることと Python 側のデバッグが有効なことは別。
- Visual Studio の F5 起動プロファイル画面には Python (native) を選択する項目が見当たらず、`debugEngines` に GUID を足すだけでは改善しなかった。
- 最初に Python コードの種類を単なる `Python` にすると、存在しない旧 `C:\Users\sasai\anaconda3\python.exe` を参照してアダプター起動に失敗した。手動成功時は **Python (native)** を選んでいる。

## 追加実験: DLL 確認後の Attach2 と手動 release（2026-09-27）

`StartMixedDebug.ps1` を変更し、従来どおり CPython 初期化後のコンソール出力を読んだうえで、対象 PID のモジュール一覧に `python312.dll` が存在することを確認してから Attach2 を呼んだ。Attach2 の戻りでは Enter を送らず、Visual Studio の状態を調べてから入力待ちを解除した。固定時間の sleep は使っていない。

| 観測項目 | 結果 |
|---|---|
| Probe PID | 46168 |
| `python312.dll` | 13:15:24.994 +09:00 に検出。モジュール一覧のパスは `C:\Users\sasai\AppData\Roaming\uv\python\cpython-3.12-windows-x86_64-none\python312.dll`。親フォルダーは `cpython-3.12.12-windows-x86_64-none` への junction |
| Attach2 | 13:15:27.447 +09:00 に戻った。この時点では Enter 未送信 |
| Visual Studio のデバッグ出力 | 同 PID の `python312.dll` について「シンボルが読み込まれました」。Python モジュールの読み込みも記録 |
| EnvDTE `Engine.AttachResult` | Python (native): `262148` (`0x00040004`)、Native / Managed (.NET Core): それぞれ `262149` (`0x00040005`)。値の意味は未確定 |
| Python ブレークポイント | release 前は DTE 上で有効、`Children=0`。F11 後は `Children=1` で 2 行目に停止 |
| F11 / Python 変数 | C# `Program.cs:42` で停止後、StepInto で Python の `calculate` に移動。`span=7`、`load=5` を DTE で取得 |
| 終了 | `Python result: 27`、終了コード 0 |

この実行でも `coreclr.dll` の初回 `0xC0000005` はデバッグ出力に現れたが、Python のステップインと正常終了を妨げなかった。

**この時点の解釈:** 自動 Attach2 でも Python デバッグが成功した。ただし、旧スクリプトも `PythonEngine.Initialize()` 後の出力を読んでから Attach2 を呼んでいたため、「DLL ロード前に attach した」という仮説は旧スクリプトのコードとは合わない。この一回では DLL ロードの明示確認と Enter の遅延の効果が未分離だった。後述の A/B 実験で追加確認した。`python312.pdb` の実際のロード先と AttachResult 数値の意味は未確認。

## A/B 実験: release 時点の差（2026-09-27）

両条件とも Probe の Python 初期化完了メッセージを読んでから、同じ Visual Studio インスタンスへ 3 エンジンを Attach2 した。A は DLL の明示確認を省き、Attach2 後に人手で待って release。B は DLL を明示確認し、Attach2 が戻ると直ちに release。A/B を交互に各3回実施した。各回とも C# 行42で停止し、DTE の StepInto 後のフレームと言語、Python ブレークポイントのバインド数、終了結果を採取した。

| 試行 | PID | Attach2 後の待機 | C# 停止時の Python BP `Children` | F11 結果 | 終了 |
|---|---:|---:|---:|---|---|
| A1 | 35696 | 約27秒 | 1 | Python 行2、`span=7`、`load=5` | 27 / code 0 |
| B1 | 29496 | 0秒 | 1 | Python 行2、`span=7`、`load=5` | 27 / code 0 |
| A2 | 38012 | 約17秒 | 1 | Python 行2、`span=7`、`load=5` | 27 / code 0 |
| B2 | 1736 | 0秒 | 0 | StepInto 後も C# フレーム。Python 停止なし | 27 / code 0 |
| A3 | 4476 | 約19秒 | 1 | Python 行2、`span=7`、`load=5` | 27 / code 0 |
| B3 | 43256 | 0秒 | 1 | Python 行2、`span=7`、`load=5` | 27 / code 0 |

**結果:** A は 3/3 成功、B は 2/3 成功。DLL の明示確認なしでも成功し、DLL を確認しても即時 release では失敗することがある。B2 の失敗時だけ C# 停止時点の Python ブレークポイントが未バインドだった。これは Attach2 の戻りが Python デバッグの準備完了を保証しないという競合仮説と整合する。ただし `IDebugLoadCompleteEvent2` 自体は未採取であり、6回の試行だけでそのイベントと失敗の因果関係までは確定しない。

Microsoft の Visual Studio SDK 文書では、デバッグエンジンが attach 時に `IDebugEngineCreateEvent2`、`IDebugProgramCreateEvent2`、`IDebugLoadCompleteEvent2` を順に送るとされる。この時点では Python (native) と対象 PID の load-complete を barrier の候補としていた。下のイベント実測では、Visual Studio package のコールバックに Python の `LoadComplete` が届かないことが分かった。

## 残る確認事項

1. 旧自動失敗セッションでは `python312.dll` のシンボル状態を採取していない。再発時は Visual Studio の［モジュール］で PDB のロード先も記録する。
2. AttachResult `0x00040004` / `0x00040005` の意味を確認する。この成功セッションでは Python モジュール読み込みと Python 停止を別途確認済みだが、数値単独ではエンジンの状態を断定しない。
3. A/B 実験は待機あり 3/3、即時 release 2/3。package コールバックには Python (native) の `LoadComplete` が届かなかった。別のイベント経路、またはブレークポイント bind を使った観測方法を調べる。
4. Visual Studio 2026 の C# プロジェクトから 3 エンジンを一操作で有効化する、サポートされたワークフローを確認する。
5. `coreclr.dll` の初回 `0xC0000005` は再発するが、今回も正常終了した。必要になった時点で first/second chance とネイティブスタックを採取する。

## 参考資料

- Microsoft Learn: [Visual Studio デバッガーでのブレークポイントのトラブルシューティング](https://learn.microsoft.com/ja-jp/troubleshoot/developer/visualstudio/debuggers/troubleshooting-breakpoints)。特に「このドキュメントのシンボルが読み込まれていません」の節。
- Microsoft Learn: [Python/C++ 混合モード デバッグ](https://learn.microsoft.com/en-us/visualstudio/python/debugging-mixed-mode-c-cpp-python-in-visual-studio?view=visualstudio)。
- Microsoft Learn: [IDebugEngine2::Attach](https://learn.microsoft.com/en-us/visualstudio/extensibility/debugger/reference/idebugengine2-attach?view=visualstudio)、[IDebugLoadCompleteEvent2](https://learn.microsoft.com/en-us/visualstudio/extensibility/debugger/reference/idebugloadcompleteevent2?view=visualstudio)。

このレポートは 2026-09-27 の実機観測を記録したもの。`StartMixedDebug.ps1` は未完成の試作であり、成功手順として配布しない。

## Visual Studio デバッグイベントの実測（2026-09-27）

[`PythonNetDebugEventLogger`](../PythonNetDebugEventLogger/README.md) を実験用 Visual Studio 2026 に読み込み、`IVsDebugger.AdviseDebugEventCallback` の結果 `S_OK` を確認した。ログは `%LOCALAPPDATA%\FrameWeb3\PythonNetDebugProbe\debug-events-35848.tsv`。イベント IID、`pProgram.GetEngineInfo()` で得たエンジン GUID、PID、program GUID と UTC 時刻を記録した。コールバックの `pEngine` は観測したイベントでは null だったため、このフォールバックが必要だった。

| 条件 | Probe PID | Managed `LoadComplete` (UTC) | release (+09:00) | C# 停止時の Python BP `Children` | F11 | Python GUID で識別できたイベント |
|---|---:|---|---|---:|---|---:|
| B: 即 release | 44108 | 07:34:30.720 | 16:34:30.720 | 0 | C# に残る | 0 / 210 件 |
| A: 手動待機 | 40288 | 07:36:39.161 | 16:37:30.706 | 1 | Python `calculate` | 0 / 212 件 |

両実行とも `Python result: 27`、終了コード 0。B の F11 後も Python BP は `Children=0`。A は F11 後に Python フレームで停止した。各実行でエンジン GUID が空のイベントが 6 件あり、これらの発生元は判定できない。**記録された唯一の `IDebugLoadCompleteEvent2` は両方とも Managed (`{2E36F1D4-B23C-435D-AB41-18E608940038}`) で、Python (native) の `LoadComplete` は受信していない。** `IDebugEngineCreateEvent2` もこのコールバックでは受信していない。全 IID を記録した成功 A と失敗 B のどちらでも、Python (native) GUID `{EC1375B7-E2CE-43E8-BF75-DC638DE1F1F9}` で識別できるイベントはなかった。

したがって、この環境の `IVsDebugger.AdviseDebugEventCallback` で見える `LoadComplete` を Python 準備完了の barrier に昇格させることはできない。SDK が規定するデバッグエンジン内部のイベント送信順序と、この package コールバックに届くイベント集合は区別する必要がある。今後の候補は、Python 側のイベントが別の経路で観測可能か調べること、または Probe の Python モジュール読み込み後にブレークポイントの bind を観測すること。今回の実験では barrier 実装は行っていない。

両実行とも `coreclr.dll` の `0xC0000005` で一度停止した。B では例外アドレス `0x00007FFA55F816A2`、読み取り先 `0x50`。A はこの停止を F5 で続行してから Probe を release し、B は release 後に続行した。この順序差も残っているため、成功率の差を単に「Attach2 後の経過時間」だけへ帰属させない。

## Python ブレークポイントだけを使う一操作実験（2026-09-27）

要件を「C# から F11」から「Python のブレークポイントで自動停止」へ変更した。`StartMixedDebug.ps1 -Experiment PythonBreakpoint` は Probe をビルドし、`probe_calculation.py:2` のブレークポイントを用意して、Probe の C# ブレークポイントを一時無効化する。`Python (native) + Native` の2エンジンを Attach2 し、アタッチ後に Probe が Python モジュールを import する。ブレークポイントの `Children > 0` を確認してから `calculate()` を呼ぶ二段階ゲートである。固定 sleep、手動 Enter、F11 は使わない。

| 実行 | Probe PID | Python BP bind | 自動停止位置 | 結果 |
|---|---:|---:|---|---|
| 手動二段階ゲート | 31472 | `Children=1` | `calculate()` | F11 なしで停止 |
| 自動化初回 | 19696 | `Children=1` | `calculate()` | 停止遷移中の一時的な COM 応答拒否をスクリプトがエラー扱い |
| 自動化修正後 | 22208 | `Children=1` | `calculate()` | 続行後 `27`、終了コード 0 |
| C# BP 復元を含む確認 | 45784 | `Children=1` | `calculate()` | 続行後 `27`、終了コード 0。C# BP は復元 |

対照として、**アタッチ前**に Python モジュールを import した PID 25672 では、アタッチ後に待っても `Children=0` だった。したがって単に import 済みかどうかではなく、**アタッチ後に import し、その bind を観測すること**が今回の有効な barrier である。2エンジンの確認実行では `coreclr.dll` のアクセス違反停止は観測しなかったが、原因の確定や全環境での不発生は主張しない。これは Probe 用の PowerShell 一操作であり、通常の C# F5 起動設定や製品アプリへの統合は未実施。
