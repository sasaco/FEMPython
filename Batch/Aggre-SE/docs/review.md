# コードレビュー (2025-09-30)

## 指摘事項

1. **致命的: ライセンス判定が無条件で有効化されている**  
   配布物ではコマンドライン引数に含まれるキー `key0` の内容からオプション機能のライセンスを判定していますが、直後にすべてのオプションを `1` に書き換える処理が追加されており、ライセンスチェックが完全に無効化されています。製品環境でそのままビルドすると未購入機能が常時有効化され、契約違反や予期しない UI 表示につながります。

```76:94:Aggre-SE/Main.vb
#Region "2024/12/6 オプションの設定を全て使えるようにする"
FormSettings.Option_杭の抵抗モーメント図作成機能 = 1
FormSettings.Option_応答塑性率の直接入力 = 1
FormSettings.Option_カスタムせん断耐力 = 1
FormSettings.Option_液状化による低減係数を考慮する連携機能 = 1
FormSettings.Option_不整形地盤 = 1
FormSettings.Option_任意スペクトル = 1
#End Region
```

   *対策案*: リリースビルドではこの強制 ON ブロックを削除し、キーの解析結果をそのまま利用するように戻してください。開発中に全機能を試す必要がある場合は、`#If DEBUG` 内に限定するか、構成フラグで切り替えましょう。

2. **✅(完了) 重大: 最近使ったファイル履歴の初期化漏れで履歴が永続化されない**  
   `_RecentFiles` が `Nothing` のまま初期化されずに `RecentFile_Add` などからアクセスされるため、初回起動時に NullReferenceException が発生して履歴の追加処理が失敗します。例外は握りつぶされるのでクラッシュはしませんが、履歴がいつまでも記録されません。

   **対応済み (2025-09-30):** 下記のようにフィールド宣言を `New List(Of String)` で初期化し、初回アクセスでも正常にリストが使用されるように修正済みです。

```123:136:Aggre-SE/forms/FormSettings.vb
Private Const RecentFilesCount As Integer = 5

Private Shared _RecentFiles As New List(Of String)

Public Shared Function GetRecentFileList() As List(Of String)
    Return _RecentFiles
End Function
```

3. ✅(完了) **重大: 一時フォルダ未存在時にシステムドライブ直下へフォールバックして書き込み失敗**  
   履歴保存で利用する `TempPath` は存在しない場合でもディレクトリを作成せず `C:` などのドライブ直下を返しています。標準ユーザーはルートに書き込み権限がないため、履歴保存時に `UnauthorizedAccessException` が発生し機能しません。またコマンドライン実行時にはメッセージボックスが表示され、自動処理をブロックします。

   **対応済み (2025-09-30):** ディレクトリが存在しない場合は `CreateDirectory` で生成し、それでも失敗した場合は `LocalApplicationData\nwl\Docu-SE` → `Path.GetTempPath()` の順にフォールバックするよう更新しました。メッセージボックスは表示せず、安全に書き込み可能なパスを返す仕様に変更しています。

```6:25:Aggre-SE/common.vb
Dim strPath As String = SystemDrivePath & "\nwl\Docu-SE"

Try
    If System.IO.Directory.Exists(strPath) = False Then
        System.IO.Directory.CreateDirectory(strPath)
    End If
    Return strPath
Catch
    Try
        Dim fallbackPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "nwl",
            "Docu-SE")
        If System.IO.Directory.Exists(fallbackPath) = False Then
            System.IO.Directory.CreateDirectory(fallbackPath)
        End If
        Return fallbackPath
    Catch
        Return System.IO.Path.GetTempPath()
    End Try
End Try
```

## 残リスク

- 例外を握りつぶしている箇所が多く、他にも機能不全が隠れている可能性があります。ログ出力を追加した上で、主要ユースケース (GUI 操作・バッチ出力) の回帰テストを推奨します。
- ライセンスキー解析ロジックは固定長の `Mid` 参照に依存しており、キー仕様の変更に弱い構造です。仕様書と照合し、単体テストを整備したうえで保守性向上を検討してください。

## 追加の指摘事項 (2025-09-30)

1. **重大: 例外再スローのアンチパターン (`Throw ex`) 横行でスタックトレース喪失**  
   捕捉した例外を `Throw ex` で再スローしており、元のスタックトレースが失われます。障害解析が困難になり、根本原因の特定を阻害します。

```495:497:Aggre-SE/forms/MDIParentForm.vb
        Catch ex As Exception
            Throw ex
        End Try
```

   - **原因**: VB の `Throw ex` 構文の誤用。
   - **影響**: ログやダンプで発生元が不明瞭になる。再現困難。
   - **推奨対策**: すべての再スロー箇所を `Throw` に置換。ログ出力（例: `Trace`, `EventLog`, 任意のロガー）を併設。

2. **重大: ファイルI/Oで `Using` 未使用によるハンドルリーク/ロック**  
   例外発生時に `Close()` が実行されず、ファイルハンドルが解放されない可能性があります。レポート出力直後の再出力や削除が失敗する原因になります。

```485:493:Aggre-SE/forms/MDIParentForm.vb
                Dim sw As New System.IO.StreamWriter(result,
                    False,
                    System.Text.Encoding.GetEncoding("shift_jis"))
                sw.Write(Value)
                sw.Close()
```

```3138:3151:Aggre-SE/reports/ExcelReport.vb
            Dim sw As New System.IO.StreamWriter(Filename,
                False,
                System.Text.Encoding.GetEncoding("shift_jis"))
            sw.Write(Value.Count)
            sw.Write(vbLf)
            For Each s In Value
                sw.Write(s)
                sw.Write(vbLf)
            Next
            sw.Close()
```

```41:53:Aggre-SE/reports/ExcelReportPickUpFile.vb
'書き込むファイルが既に存在している場合は、上書きする
Dim sw As New System.IO.StreamWriter(FileName,
    False,
    System.Text.Encoding.GetEncoding("shift_jis"))
For Each s In PickupListOfString
    sw.Write(s)
    sw.Write(vbCrLf)
Next
sw.Close()
```

   - **原因**: `Using` 構文不使用。
   - **影響**: 断続的な「ファイルが使用中」エラー、メモリ/ハンドルリーク。
   - **推奨対策**: 読み書きすべてを `Using … End Using` で囲う。例外時も自動解放。

3. **重大: UI ブロッキングの `MsgBox` 多用（バッチ/大量解析と相性不良）**  
   例外や警告で同期ダイアログを多用しており、無人実行（コマンドライン/大量解析）で停止します。一部はフラグで抑制していますが、網羅的ではありません。

```1026:1032:Aggre-SE/reports/ExcelReport.vb
        Catch ex As Exception
            Dim s As String = String.Format("DL番号:{0}({1}){2}{3}", DB.DLNo, DB.DLinfo.Title.Trim, vbLf, ex.Message)
            If FormSettings.Option_大量解析プログラム = 0 Then
                MsgBox(s)
            Else
                Range(Ad, iRow).Value = s
            End If
```

   - **原因**: 実行モード（GUI/バッチ）切替の設計不足。
   - **影響**: 夜間バッチや CI で処理停止、ユーザー体験の阻害。
   - **推奨対策**: ロガー出力＋非UI通知に統一。バッチモードは非対話・戻り値で通知。GUI はステータス表示へ集約。

4. **重大: PDF ビューアが UI スレッドをブロック**  
   外部プロセス起動後に `WaitForExit()` を呼んでおり、UI が固まります。

```22:28:Aggre-SE/forms/pdfViewer.vb
    Public Sub ShowPdf()
        Try
            Dim oProc As New Process
            oProc.StartInfo.FileName = ReadedFileName
            oProc.Start()
            oProc.WaitForExit()
```

   - **原因**: 同期待機。
   - **影響**: アプリ全体が操作不能に。
   - **推奨対策**: `WaitForExit` を削除し、必要なら非同期に監視。プロセス起動失敗時のみユーザー通知。

5. **中: 行末コードが混在（`vbLf` と `vbCrLf`）し、テキスト互換性に影響**  
   Windows では CRLF が標準ですが、LF のみで出力している箇所があります。ツール連携やエディタ表示で不整合が起きる可能性があります。

```3143:3147:Aggre-SE/reports/ExcelReport.vb
            sw.Write(Value.Count)
            sw.Write(vbLf)
            For Each s In Value
                sw.Write(s)
                sw.Write(vbLf)
```

   - **推奨対策**: `Environment.NewLine` に統一。

6. **中: `Dispose` での `Nothing` チェック欠如により終了時例外の可能性**  
   出力モードや例外パスによっては `ViewerForm` / `CellReport1` が未生成で `Dispose()` 呼び出しされ、終了時に例外化する可能性があります。

```15:19:Aggre-SE/reports/ExcelReport.vb
    Public Sub Dispose() _
            Implements IDisposable.Dispose
        ViewerForm.Dispose()
        CellReport1.Dispose()
```

   - **推奨対策**: `If obj IsNot Nothing Then obj.Dispose()` の防御実装。

7. **中: BackgroundWorker キャンセル未対応**  
   進捗フォームから `CancelAsync()` を発行できますが、DoWork 側で `CancellationPending` を確認していません。

```400:430:Aggre-SE/forms/ChildFormCaseName.vb
    Private Sub SetDataFolder_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)
        Dim bw As BackgroundWorker = DirectCast(sender, BackgroundWorker)
        bw.ReportProgress(0, "データクリア中...")
        ' ... 処理 ...
        bw.ReportProgress(0, "データローディング中...")
        ' ... 処理 ...
```

   - **推奨対策**: 長処理ループで `If bw.CancellationPending Then e.Cancel = True : Exit Sub` を挿入。

8. **軽微/推測: 文字コードの固定 (Shift-JIS) による互換性低下の可能性**  
   XML/テキストの読み書きを Shift-JIS 固定で行っています。将来的な Unicode データや他ツールとの相互運用で文字化けが起こる可能性があります。（現状要件が Shift-JIS 固定であれば問題なし）

```385:391:Aggre-SE/forms/MDIParentForm.vb
            Dim sr As New System.IO.StreamReader(FileName,
                System.Text.Encoding.GetEncoding("shift_jis"))
            Dim s As String = sr.ReadToEnd()
            sr.Close()
```

   - **推奨対策**: ファイル側のエンコーディング宣言（XML の `encoding`）を尊重する読み込み、もしくは UTF-8 への段階的移行を検討。

9. **情報: 進捗ダイアログのエラー表示がモーダルで無人実行を阻害**  
   進捗フォーム完了時のエラーで `MessageBox` を表示します。大量処理や CLI 実行では停止します。

```120:133:Aggre-SE/forms/MyProgressBarForm.vb
    Private Sub BackgroundWorker1_RunWorkerCompleted_1(...)
        If e.Error IsNot Nothing Then
            MessageBox.Show(Me, "エラー", _
                            "エラーが発生しました。" & vbCrLf & vbCrLf & _
                                e.Error.Message, ...)
            Me._error = e.Error
            Me.DialogResult = DialogResult.Abort
```

   - **推奨対策**: バッチモード時はダイアログ抑止＋ログ出力のみに切替。GUI 時は非モーダル通知やステータス表示を検討。

