' このソースファイルはアドバンスソフトウェア開発ツール用のサンプルプログラムの共通処理です。
' 次の処理が含まれます。
' サンプルプログラムが正しく動作しなくなる恐れがあるため、コードエディタで変更しないでください。
' SampleDmy クラス・・・各クラスのサンプル説明用の処理をまとめた時に、デザイナが開かないようにするための便宜上の処理
' SampleWork クラス・・・サンプルの共通処理(ファイルオープン、パス取得)などを実装。
' その他、partial クラス・・・各サンプル(クラス)の説明用の処理を、partial クラスとして、このファイルに集約。
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Drawing
Imports System.Data
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports System.Drawing.Printing
#Region "サンプルの説明用の処理をまとめたソースファイル。便宜上、ユーザーコントロールで作成"
Public Class SampleDmy

	Public Sub New()

		' この呼び出しは、Windows フォーム デザイナで必要です。
		InitializeComponent()

		' InitializeComponent() 呼び出しの後で初期化を追加します。

	End Sub
End Class
#End Region

#Region "サンプルの共通処理(ファイルオープン、パス取得)などを実装。"
Public Class SampleWork
	''' <summary>
	''' Excel がインストールされている環境で、Excel ファイルをオープンします。
	''' Excel がインストールされていない場合、Excel ファイル生成のメッセージを表示します。
	''' </summary>
	Public Shared Sub ExcelCheck(ByVal fileName As String, ByVal template As Boolean)
		If (ExcelFileOpen(fileName) = False) Then
			Dim message As String
			If (template = True) Then
				message = "Excel のインストール、または Excel がインストールされている場合は、Excel 互換パックをインストールしてください。"
			Else
				message = "Excel ファイルを生成しました。" + vbCrLf + "ファイル名：[" + System.IO.Path.GetFullPath(fileName) + "]"
			End If
			MessageBox.Show(message, "アドバンスツールサンプル")
		End If

	End Sub

	''' <summary>
	''' Excel ファイルのオープン処理
	''' </summary>
	Public Shared Function ExcelFileOpen(ByVal fileName As String) As Boolean
		' 関連付けのチェック
		If ((Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(".xlsx") IsNot Nothing) Or _
		 (Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(".xls") IsNot Nothing) And fileName.EndsWith(".xls") = True) Then
			Try
				System.Diagnostics.Process.Start(fileName)
			Catch ex As Exception
				MessageBox.Show( _
				 "Excel ファイルのオープンに失敗しました。" + vbCrLf + "ファイル名：[" + fileName + "]", _
				 "エラー", _
				 MessageBoxButtons.OK, _
				 MessageBoxIcon.Error)
			End Try
			Return True
		Else
			Return False
		End If

	End Function

	''' <summary>
	''' ファイルのオープン処理
	''' </summary>
	Public Shared Function FileOpen(ByVal fileName As String) As Boolean
		' 関連付けのチェック
		If (MessageBox.Show("ファイル名：[" & _
		  fileName.Substring(fileName.LastIndexOf("\") + 1) + "]" + vbCrLf & _
		  "を出力しました。ファイルの内容を確認しますか？", "確認", MessageBoxButtons.YesNo) = DialogResult.Yes) Then
			Try
				System.Diagnostics.Process.Start(fileName)
			Catch ex As Exception
				MessageBox.Show("ファイルのオープンに失敗しました。" + vbCrLf + _
				 "ファイル名：" + vbCrLf + _
				 fileName, _
				  "エラー", _
				 MessageBoxButtons.OK, _
				 MessageBoxIcon.Error)
			End Try
			Return True
		Else
			Return False
		End If
	End Function

	''' <summary>
	''' 入力ファイルのフルパスを取得します。
	''' </summary>
	''' <param name="fileName">ファイル名</param>
	''' <returns></returns>
	Public Shared Function InputFilePath(ByVal fileName As String) As String
		Return RootFilePath("Input\" + fileName)
	End Function

	''' <summary>
	''' 出力ファイルのフルパスを取得します。
	''' </summary>
	''' <param name="fileName">ファイル名</param>
	''' <returns></returns>
	Public Shared Function OutputFilePath(ByVal fileName As String) As String
		Return RootFilePath("Output\" + fileName)
	End Function

	''' <summary>
	''' プロジェクトのルートディレクトリからのフルパスを取得します。
	''' </summary>
	''' <param name="fileName">ファイル名</param>
	''' <returns></returns>
	Public Shared Function RootFilePath(ByVal fileName) As String
		If (fileName.StartsWith("\") = True) Then fileName = fileName.Substring(1)
		Return System.IO.Path.GetFullPath(Application.StartupPath + "\..\..\" + fileName)
	End Function

	Public Shared Sub OpenSampleFolder(ByVal folderName As String)
		Dim path As String = RootFilePath("..\" + folderName)
		Try
			System.Diagnostics.Process.Start("Explorer.exe", path)
		Catch ex As Exception
			MessageBox.Show("フォルダ：" + vbCrLf + "[" + path + "]" + vbCrLf + "を開けませんでした。" + vbCrLf)
		End Try
	End Sub
End Class
#End Region

#Region "各サンプル(クラス)の説明用の処理(partial クラス)"
' WinAppClassSample クラスの作業用の処理
Partial Public Class WinAppClassSample
	Private Function CellReportWork(ByRef outputFilePath As String) As Boolean
		' PDF ファイルの出力パス
		saveFileDialog1.InitialDirectory = SampleWork.OutputFilePath("")
		saveFileDialog1.FileName = "WinAppClassSample.pdf"
		saveFileDialog1.Filter = "PDFファイル|*.pdf"
		saveFileDialog1.Title = "帳票(PDF ファイル)の保存先を指定してください。"
		If (saveFileDialog1.ShowDialog() <> DialogResult.OK) Then
			MessageBox.Show("処理を中止しました。")
			Return False
		End If
		outputFilePath = saveFileDialog1.FileName

		If (MessageBox.Show("帳票のテンプレートには Excel ファイル(デザインファイル)を使用します。" + vbCrLf + _
			  "このファイルを確認しますか？" + vbCrLf + _
			  "(開いたままでもサンプルは動作します)", "帳票テンプレートの確認", MessageBoxButtons.YesNo) = DialogResult.Yes) Then
			System.Diagnostics.Process.Start(SampleWork.InputFilePath("BasicReportSample.xls"))
		End If
		Return True
	End Function

	Private Function ViewerReportWork() As Boolean
		If (MessageBox.Show("帳票のテンプレートには Excel ファイル(デザインファイル)を使用します。" + vbCrLf + _
		   "このファイルを確認しますか？" + vbCrLf + _
		   "(開いたままでもサンプルは動作します)", "帳票テンプレートの確認", MessageBoxButtons.YesNo) = DialogResult.Yes) Then
			System.Diagnostics.Process.Start(SampleWork.InputFilePath("BasicReportSample.xls"))
		End If
		Return True
	End Function
End Class

' WebAppClassSample クラスの作業用の処理
Partial Public Class WebAppClassSample
	Private Function WebCellReportWork1(ByRef outputFilePath As String) As Boolean
		' xls ファイルの出力パス
		saveFileDialog1.InitialDirectory = SampleWork.OutputFilePath("")
		saveFileDialog1.FileName = "WebAppClassSample.xls"
		saveFileDialog1.Filter = "Excelファイル|*.xls"
		saveFileDialog1.Title = "帳票(Excel ファイル)の保存先を指定してください。"
		If (saveFileDialog1.ShowDialog() <> DialogResult.OK) Then
			MessageBox.Show("処理を中止しました。")
			Return False
		End If
		outputFilePath = saveFileDialog1.FileName

		If (MessageBox.Show("帳票のテンプレートには Excel ファイル(デザインファイル)を使用します。" + vbCrLf + _
		   "このファイルを確認しますか？" + vbCrLf + _
		   "(開いたままでもサンプルは動作します)", "帳票テンプレートの確認", MessageBoxButtons.YesNo) = DialogResult.Yes) Then
			System.Diagnostics.Process.Start(SampleWork.InputFilePath("BasicReportSample.xls"))
		End If
		Return True
	End Function
	Private Function WebCellReportWork2(ByRef outputFilePath As String) As Boolean
		' xml ファイルの出力パス
		saveFileDialog1.InitialDirectory = SampleWork.OutputFilePath("")
		saveFileDialog1.FileName = "WebAppClassSample.xml"
		saveFileDialog1.Filter = "XMLファイル|*.xml"
		saveFileDialog1.Title = "帳票(xml ファイル)の保存先を指定してください。"
		If (saveFileDialog1.ShowDialog() <> DialogResult.OK) Then
			MessageBox.Show("処理を中止しました。")
			Return False
		End If
		outputFilePath = saveFileDialog1.FileName

		Return True
	End Function
End Class

' PageStartSample クラスの作業用の処理
Partial Public Class PageStartSample
	Private Sub WorkSetSampleData(ByVal targetUserNo As String)
		Dim doc As System.Xml.XmlDocument = New System.Xml.XmlDocument()
		Dim list As System.Xml.XmlNodeList
		Dim dt As DateTime
		Dim userNo As String
		Dim userName As String
		' 受領請求リストを読み込みます。
		doc.Load(SampleWork.InputFilePath("PageStartSample.xml"))
		list = doc.SelectNodes("/受領請求リスト/顧客")

		For Each node As System.Xml.XmlNode In list
			dt = DateTime.Now
			userNo = node.SelectSingleNode("@no").InnerText
			If (userNo <> targetUserNo) Then Continue For
			userName = node.SelectSingleNode("./顧客名").InnerText

			' 日付
			cellReport1.Cell("**Date").Value = dt
			' 顧客名
			cellReport1.Cell("**Name").Value = userName
			' 番号
			cellReport1.Cell("**No").Value = userNo
			' 郵便番号
			cellReport1.Cell("**ZipCode").Value = node.SelectSingleNode("./郵便番号").InnerText
			' 住所
			cellReport1.Cell("**Address").Value = node.SelectSingleNode("./住所").InnerText
			' 商品一覧
			Dim totalSeikyu As Integer = 0
			Dim y As Integer = 0
			Dim list2 As System.Xml.XmlNodeList = doc.SelectNodes("/受領請求リスト/顧客[@no=""" + userNo + """]/商品")
			For Each node2 As System.Xml.XmlNode In list2
				' 商品名
				cellReport1.Cell("**Shouhin", 0, y).Value = node2.SelectSingleNode("./商品名").InnerText
				' 数量
				Dim unitNumber As Integer = Convert.ToInt32(node2.SelectSingleNode("./数量").InnerText)
				cellReport1.Cell("**Suu", 0, y).Value = unitNumber
				' 単位
				cellReport1.Cell("**Tani", 0, y).Value = node2.SelectSingleNode("./単位").InnerText
				' 単価
				Dim unitPrice As Integer = Convert.ToInt32(node2.SelectSingleNode("./単価").InnerText)
				cellReport1.Cell("**Tanka", 0, y).Value = unitPrice
				' 金額
				Dim totalPrice As Integer = unitNumber * unitPrice
				cellReport1.Cell("**Kingaku", 0, y).Value = totalPrice
				totalSeikyu += totalPrice
				y += 1
			Next
			' 小計
			cellReport1.Cell("**Kei").Value = totalSeikyu
			' 消費税
			cellReport1.Cell("**Zei").Value = totalSeikyu * 0.05F
			' 合計
			cellReport1.Cell("**Goukei").Value = totalSeikyu * 1.05F
			Exit For
		Next
	End Sub
End Class

' AttributePageSample クラスの作業用の処理
Partial Public Class AttributePageSample
	Private Sub WorkHeaderFooter1()

		cellReport1.Cell("A1").Attr.FontColor2 = AdvanceSoftware.VBReport8.xlColor.Red
		cellReport1.Cell("A1").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
		cellReport1.Cell("A1").Str = "シート１の１ページ目"

		cellReport1.Cell("B4:D7").Attr.Box(AdvanceSoftware.VBReport8.BoxType.Ltc, AdvanceSoftware.VBReport8.BorderStyle.Thin, AdvanceSoftware.VBReport8.xlColor.Black)
		cellReport1.Cell("B9:D13").Attr.Box(AdvanceSoftware.VBReport8.BoxType.Ltc, AdvanceSoftware.VBReport8.BorderStyle.Thin, AdvanceSoftware.VBReport8.xlColor.Black)
		cellReport1.Cell("B4:D4").Attr.Box(AdvanceSoftware.VBReport8.BoxType.Under, AdvanceSoftware.VBReport8.BorderStyle.Dashed, AdvanceSoftware.VBReport8.xlColor.Black)
		cellReport1.Cell("B9:D9").Attr.Box(AdvanceSoftware.VBReport8.BoxType.Under, AdvanceSoftware.VBReport8.BorderStyle.Dashed, AdvanceSoftware.VBReport8.xlColor.Black)
		cellReport1.Cell("B4:D13").Attr.Box(AdvanceSoftware.VBReport8.BoxType.Box, AdvanceSoftware.VBReport8.BorderStyle.Thick, AdvanceSoftware.VBReport8.xlColor.Black)
		cellReport1.Cell("B4:D4").Attr.HorizontalAlignment = AdvanceSoftware.VBReport8.HorizontalAlignment.Center
		cellReport1.Cell("B9:D9").Attr.HorizontalAlignment = AdvanceSoftware.VBReport8.HorizontalAlignment.Center
		cellReport1.Cell("B4").Value = "ヘッダ(左)"
		cellReport1.Cell("B5").Value = "・&U 下線 "
		cellReport1.Cell("B6").Value = "・&B ボールド"
		cellReport1.Cell("C4").Value = "ヘッダ(中央)"
		cellReport1.Cell("C5").Value = "・&E 二重下線 "
		cellReport1.Cell("C6").Value = "・&A シート見出し"

		cellReport1.Cell("D4").Value = "ヘッダ(右)"
		cellReport1.Cell("D5").Value = "・&I イタリック"
		cellReport1.Cell("D6").Value = "・&(アンパサンド)を表示"

		cellReport1.Cell("B9").Value = "フッタ(左)"
		cellReport1.Cell("B10").Value = "・&D 現在の日付"
		cellReport1.Cell("B11").Value = "・&T 現在の時刻"

		cellReport1.Cell("C9").Value = "フッタ(中央)"
		cellReport1.Cell("C10").Value = "・&P ページ番号"
		cellReport1.Cell("C11").Value = "・&N 全体のページ数"
		cellReport1.Cell("C12").Value = "・&""フォント"""
		cellReport1.Cell("C13").Value = "・&99 フォントサイズ(14)"

		cellReport1.Cell("D9").Value = "フッタ(右)"
		cellReport1.Cell("D10").Value = "・&S 取消線"
		cellReport1.Cell("D11").Value = "・&X 上付き"
		cellReport1.Cell("D12").Value = "・&Y 下付き"

		' ColWidth プロパティで (A～D列) の列幅の変更
		cellReport1.Cell("A:D").ColWidth = 20
		' RowHeight プロパティで (4～14行) の行高の設定
		cellReport1.Cell("4:14").RowHeight = 18
		' Break プロパティでの改ページ挿入
		cellReport1.Cell("A41").Break = True

		' AttrNo プロパティで A1 に設定された「セルの書式設定」番号を取得
		Dim attrNo As Integer = cellReport1.Cell("A1").AttrNo
		' Value2 プロパティにより  A1 の書式設定で文字列を挿入。
		cellReport1.Cell("A41").Value2("シート１の２ページ目", attrNo)
		' Copyで1ページ目の表をコピーして貼り付けます。
		cellReport1.Cell("B4:D13").Copy("B44,D53")
		' RowHeight プロパティで (44～54行) の行高の設定
		cellReport1.Cell("44:54").RowHeight = 18
		' 2ページ目の改ページ
		cellReport1.Cell("A81").Break = True
		cellReport1.Cell("A81").Value2("シート１の３ページ目", attrNo)
		' Copyで引数の指定を省略し、メモリ上にコピー
		cellReport1.Cell("B4:D13").Copy()
		' Pasteで、範囲をコピー(先頭の位置のみでよい)
		cellReport1.Cell("B84").Paste()
		' RowHeight プロパティで (84～94行) の行高の設定
		cellReport1.Cell("84:94").RowHeight = 18
	End Sub

	Private Sub WorkHeaderFooter2()
		cellReport1.Cell("A1").Attr.FontColor2 = AdvanceSoftware.VBReport8.xlColor.Red
		cellReport1.Cell("A1").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
		cellReport1.Cell("A1").Str = "※ヘッダー・フッターに全ページ数を設定した場合、Page.Start～Page.End メソッドの"
		cellReport1.Cell("A2").Attr.FontColor2 = AdvanceSoftware.VBReport8.xlColor.Red
		cellReport1.Cell("A2").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
		cellReport1.Cell("A2").Value = "　の回数に関わらず全ページ数が出力されます。"
	End Sub
End Class

' ControlCellSample クラスの作業用の処理
Partial Public Class ControlCellSample
	Private Sub ClearCellWork()
		cellReport1.Cell("E2").Value = "　← ※注意 - Clear メソッドでは結合セルを解除できない。"
		cellReport1.Cell("E2").Attr.FontColor2 = AdvanceSoftware.VBReport8.xlColor.Blue
		cellReport1.Cell("E3").Value = "　← B3:値と書式をクリア　C3:書式をクリア　D3:値をクリア。"
		cellReport1.Cell("E3").Attr.FontColor2 = AdvanceSoftware.VBReport8.xlColor.Blue
		cellReport1.Cell("E4").Value = "　← 結合セルのクリアは Attr.MergeCells プロパティで行なう。"
		cellReport1.Cell("E4").Attr.FontColor2 = AdvanceSoftware.VBReport8.xlColor.Blue

	End Sub
	Private Sub PageBreakWork()
		cellReport1.Cell("A20").Attr.BackColor2 = AdvanceSoftware.VBReport8.xlColor.Green
		cellReport1.Cell("A20").Value = "↑改ページ"
		cellReport1.Cell("K1").Attr.BackColor2 = AdvanceSoftware.VBReport8.xlColor.Green
		cellReport1.Cell("K1").Value = "←改ページ"
		cellReport1.Cell("G10").Attr.BackColor2 = AdvanceSoftware.VBReport8.xlColor.Green
		cellReport1.Cell("G10").Value = "　↑" + vbLf + "←改ページ"
	End Sub
End Class

Partial Public Class AttributeCellSample
	' SetFormatの説明用処理
	Private Sub SetFormatWork()
		cellReport1.Cell("A").ColWidth = 24
		cellReport1.Cell("B").ColWidth = 20
		cellReport1.Cell("A1").Attr.FontPoint = 14
		cellReport1.Cell("A1").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
		cellReport1.Cell("A1").Str = "表示形式の設定"
	End Sub

	' SetStringAlignmentの説明用処理
	Private Sub SetStringAlignmentWork()
		cellReport1.Page.Attr.Size(AdvanceSoftware.VBReport8.PageOrientation.Landscape, 100, PaperKind.A4)
		cellReport1.Cell("A1").Attr.FontPoint = 14
		cellReport1.Cell("A1").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
		cellReport1.Cell("A1").Str = "文字の配置の設定"
		cellReport1.Cell("B1").Str = "（x軸→縦位置、y軸→横位置）"
		cellReport1.Cell("A2:F9").RowHeight = 40
		cellReport1.Cell("A2:F9").ColWidth = 20

		cellReport1.Cell("B2:F2").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
		cellReport1.Cell("A2:A9").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
		cellReport1.Cell("A2:F9").Attr.Box(AdvanceSoftware.VBReport8.BoxType.Ltc, AdvanceSoftware.VBReport8.BorderStyle.Thin, AdvanceSoftware.VBReport8.xlColor.Black)
	End Sub

	' SetStringOrientationの説明用処理
	Private Sub SetStringOrientationWork()

		cellReport1.Page.Attr.Size(AdvanceSoftware.VBReport8.PageOrientation.Landscape, 100, PaperKind.A4)
		cellReport1.Cell("A1").Attr.FontPoint = 14
		cellReport1.Cell("A1").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
		cellReport1.Cell("A1").Str = "方向の設定"
		cellReport1.Cell("A:F").ColWidth = 16.5
		cellReport1.Cell("A2").RowHeight = 120
		cellReport1.Cell("A4").RowHeight = 80
	End Sub

	' SetStringControlの説明用処理
	Private Sub SetStringControlWork()
		cellReport1.Cell("A1").Attr.FontPoint = 14
		cellReport1.Cell("A1").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
		cellReport1.Cell("A1").Str = "文字の制御の設定"
		cellReport1.Cell("A").ColWidth = 16
	End Sub

	' SetFontの説明用処理
	Private Sub SetFontWork()
		cellReport1.Page.Attr.Size(AdvanceSoftware.VBReport8.PageOrientation.Landscape, 100, PaperKind.A4)
		cellReport1.Cell("A1").Attr.FontPoint = 14
		cellReport1.Cell("A1").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
		cellReport1.Cell("A1").Str = "フォントの設定" + GetExcelType()
		cellReport1.Cell("A").ColWidth = 18
		cellReport1.Cell("B:D").ColWidth = 33
	End Sub

	' SetRuledLineの説明用処理
	Private Sub SetRuledLineWork()
		cellReport1.Cell("A1").Attr.FontPoint = 14
		cellReport1.Cell("A1").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
		cellReport1.Cell("A1").Str = "罫線の設定" + GetExcelType()
		cellReport1.Cell("A2").Str = "※色の指定にはRGB値と色定数がありますが、RGB値の指定はExcel 2007(xlsx)形式でのみ対応します。"
		cellReport1.Cell("A2").Attr.FontColor2 = AdvanceSoftware.VBReport8.xlColor.Red
		cellReport1.Cell("B").ColWidth = 18
		cellReport1.Cell("D").ColWidth = 18
		cellReport1.Cell("F").ColWidth = 18
	End Sub

	' SetBackColorの説明用処理
	Private Sub SetBackColorWork()
		cellReport1.Page.Attr.Size(AdvanceSoftware.VBReport8.PageOrientation.Landscape, 100, PaperKind.A4)
		cellReport1.Cell("A1").Attr.FontPoint = 14
		cellReport1.Cell("A1").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
		cellReport1.Cell("A1").Str = "背景色の設定" + GetExcelType()
	End Sub

	' SetPatternの説明用処理
	Private Sub SetPatternWork()
		cellReport1.Page.Attr.Size(AdvanceSoftware.VBReport8.PageOrientation.Landscape, 100, PaperKind.A4)
		cellReport1.Cell("A1").Attr.FontPoint = 14
		cellReport1.Cell("A1").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
		cellReport1.Cell("A1").Str = "パターンの設定" + GetExcelType()
		cellReport1.Cell("A").ColWidth = 18
	End Sub
	Private Function GetExcelType() As String
		If (radioExcel2007.Checked) Then Return "(Xlsx形式)"
		Return "(Xls形式)"
	End Function
End Class

Partial Public Class CalculateCellSample
	Private Sub WorkReportCalculate()
		If (checkApplyFormula.Checked) Then
			cellReport1.Cell("A1").Value = "◆VB-Report 8.0 for .NET での計算式の扱い"
			cellReport1.Cell("A2").Value = "　　( ApplyFormula=True )"
			cellReport1.Cell("A3").Value = "　　上記設定でテンプレート設定の計算式とプログラム設定の計算式が再計算されます。"

			cellReport1.Cell("E6:G6").Value = "↑ApplyFormula=True による帳票出力時に再計算"
		Else
			cellReport1.Cell("A1").Value = "◆VB-Report 8.0 for .NET での計算式の扱い( ApplyFormula=False )"
			cellReport1.Cell("A2").Value = "　　( ApplyFormula=False )"
			cellReport1.Cell("A3").Value = "　　上記設定では計算式の再計算が行なわれません。(ExcelMode=Trueを除く)"
			cellReport1.Cell("E6").Value = "↑テンプレートの計算結果のまま"
			cellReport1.Cell("F6").Value = "↑Func メソッドで計算結果=nullを設定"
			cellReport1.Cell("G6").Value = "↑Func メソッドで計算結果=242を設定"
		End If
	End Sub
	Private Sub WorkEditCalculate()
		cellReport1.Cell("6").RowHeight = 0
		cellReport1.Cell("C8").Value = "↑計算式のセル範囲が挿入した行数分拡張されます。" + vbLf + "　B5:B6→B5:B10"
		cellReport1.Cell("E8").Value = "↑テンプレートに設定された計算式もセルの移動に対応します。" + vbLf + "　C7:D7→C11:D11"
		cellReport1.Cell("F8").Value = "↑Func メソッドで設定した計算式もセル範囲の移動・拡張に対応します。" + vbLf + "　C7:D7→C11:D11"
		cellReport1.Cell("G6").Value = "←コピー先で計算式が再調整(C5:D5→C6:D6)"
		cellReport1.Cell("G6").Attr.FontColor = Color.Olive
	End Sub
End Class

Partial Public Class InsertFigureSample
	''' <summary>
	''' 「図の挿入」用のシートを作成
	''' </summary>
	Private Sub SetFigreWork()
		cellReport1.Page.Attr.Margin(5, 5, 5, 5, 5, 5)
		cellReport1.Page.Attr.Size(AdvanceSoftware.VBReport8.PageOrientation.Landscape, PaperKind.A4)
		cellReport1.Cell("B1").Str = "(1) 図の設定方法"
		cellReport1.Cell("B1").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold

		cellReport1.Cell("A30").Break = True
		cellReport1.Cell("B30").Str = "(2) emf ファイル以外の対応する画像形式"
		cellReport1.Cell("B30").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold

		cellReport1.Cell("H34").Value = "セルの文字列が透過"
		cellReport1.Cell("M34").Value = "セルの文字列が透過"
		cellReport1.Cell("C48").Value = "セルの文字列が透過"
	End Sub

	''' <summary>
	''' 「ライン」用のシートを作成
	''' </summary>
	Private Sub SetLineWork()
		cellReport1.Page.Attr.Margin(5, 5, 5, 5, 5, 5)
		cellReport1.Page.Attr.Size(AdvanceSoftware.VBReport8.PageOrientation.Landscape, PaperKind.A4)
		cellReport1.Cell("B1").Str = "(1) 線の挿入方法(Drawing オブジェクトの使い方)"
		cellReport1.Cell("B1").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold

		cellReport1.Cell("B9").Str = "(2) 対応する線のスタイル"
		cellReport1.Cell("B9").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold

		cellReport1.Cell("B31").Str = "(3) 太さと修飾を組み合わせた設定例"
		cellReport1.Cell("B31").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold

		cellReport1.Cell("A43").Break = True
		cellReport1.Cell("B43").Str = "(4) 対応する線の矢印"
		cellReport1.Cell("B43").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold

		cellReport1.Cell("B52").Str = "(5) 矢印の大きさと太さを組み合わせた設定例"
		cellReport1.Cell("B52").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
		cellReport1.Cell("B53").Str = "※大きさ・太さ、修飾のそれぞれの設定を連結子("" Or "")で連結し、矢印のサイズを調整します。(スタイルには[Arrow]を使用)"
		cellReport1.Cell("C54").RowHeight = 60
		cellReport1.Cell("C54:E54").Attr.Box(AdvanceSoftware.VBReport8.BoxType.Box, AdvanceSoftware.VBReport8.BorderStyle.Medium, AdvanceSoftware.VBReport8.xlColor.Auto)
		cellReport1.Cell("C54:E54").Attr.MergeCells = True
		cellReport1.Cell("C54:E54").Attr.HorizontalAlignment = AdvanceSoftware.VBReport8.HorizontalAlignment.Center
		cellReport1.Cell("C54").Value = "短い" + vbLf + "(Short)" + vbLf + "[標準]"
		cellReport1.Cell("F54:H54").Attr.Box(AdvanceSoftware.VBReport8.BoxType.Box, AdvanceSoftware.VBReport8.BorderStyle.Medium, AdvanceSoftware.VBReport8.xlColor.Auto)
		cellReport1.Cell("F54:H54").Attr.MergeCells = True
		cellReport1.Cell("F54:H54").Attr.HorizontalAlignment = AdvanceSoftware.VBReport8.HorizontalAlignment.Center
		cellReport1.Cell("F54").Value = "普通" + vbLf + "(Normal)"
		cellReport1.Cell("I54:K54").Attr.Box(AdvanceSoftware.VBReport8.BoxType.Box, AdvanceSoftware.VBReport8.BorderStyle.Medium, AdvanceSoftware.VBReport8.xlColor.Auto)
		cellReport1.Cell("I54:K54").Attr.MergeCells = True
		cellReport1.Cell("I54:K54").Attr.HorizontalAlignment = AdvanceSoftware.VBReport8.HorizontalAlignment.Center
		cellReport1.Cell("I54").Value = "長い" + vbLf + "(Long)"
		cellReport1.Cell("B55:B57").Attr.Box(AdvanceSoftware.VBReport8.BoxType.Box, AdvanceSoftware.VBReport8.BorderStyle.Medium, AdvanceSoftware.VBReport8.xlColor.Auto)
		cellReport1.Cell("B55:B57").Attr.MergeCells = True
		cellReport1.Cell("B55:B57").Attr.HorizontalAlignment = AdvanceSoftware.VBReport8.HorizontalAlignment.Center
		cellReport1.Cell("B55").Value = "小さく" + vbLf + "(Small)" + vbLf + "[標準]"
		cellReport1.Cell("B58:B60").Attr.Box(AdvanceSoftware.VBReport8.BoxType.Box, AdvanceSoftware.VBReport8.BorderStyle.Medium, AdvanceSoftware.VBReport8.xlColor.Auto)
		cellReport1.Cell("B58:B60").Attr.MergeCells = True
		cellReport1.Cell("B58:B60").Attr.HorizontalAlignment = AdvanceSoftware.VBReport8.HorizontalAlignment.Center
		cellReport1.Cell("B58").Value = "普通" + vbLf + "(Medium)"
		cellReport1.Cell("B61:B63").Attr.Box(AdvanceSoftware.VBReport8.BoxType.Box, AdvanceSoftware.VBReport8.BorderStyle.Medium, AdvanceSoftware.VBReport8.xlColor.Auto)
		cellReport1.Cell("B61:B63").Attr.MergeCells = True
		cellReport1.Cell("B61:B63").Attr.HorizontalAlignment = AdvanceSoftware.VBReport8.HorizontalAlignment.Center
		cellReport1.Cell("B61").Value = "大きく" + vbLf + "(Large)"
		cellReport1.Cell("C57:K57").Attr.LineBottom(AdvanceSoftware.VBReport8.BorderStyle.Medium, AdvanceSoftware.VBReport8.xlColor.Auto)
		cellReport1.Cell("C60:K60").Attr.LineBottom(AdvanceSoftware.VBReport8.BorderStyle.Medium, AdvanceSoftware.VBReport8.xlColor.Auto)
		cellReport1.Cell("C63:K63").Attr.LineBottom(AdvanceSoftware.VBReport8.BorderStyle.Medium, AdvanceSoftware.VBReport8.xlColor.Auto)
		cellReport1.Cell("B55:B63").Attr.LineRight(AdvanceSoftware.VBReport8.BorderStyle.Medium, AdvanceSoftware.VBReport8.xlColor.Auto)
		cellReport1.Cell("E55:E63").Attr.LineRight(AdvanceSoftware.VBReport8.BorderStyle.Medium, AdvanceSoftware.VBReport8.xlColor.Auto)
		cellReport1.Cell("H55:H63").Attr.LineRight(AdvanceSoftware.VBReport8.BorderStyle.Medium, AdvanceSoftware.VBReport8.xlColor.Auto)
		cellReport1.Cell("K55:K63").Attr.LineRight(AdvanceSoftware.VBReport8.BorderStyle.Medium, AdvanceSoftware.VBReport8.xlColor.Auto)

		cellReport1.Cell("A66").Break = True
		cellReport1.Cell("B66").Str = "(6) 折れ線の描画"
		cellReport1.Cell("B66").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
	End Sub

	''' <summary>
	''' 「オートシェイプ」用のシートを作成
	''' </summary>
	Private Sub SetShapeWork()
		cellReport1.Page.Attr.Margin(5, 5, 5, 5, 5, 5)
		cellReport1.Page.Attr.Size(AdvanceSoftware.VBReport8.PageOrientation.Landscape, PaperKind.A4)
		cellReport1.Cell("B1").Str = "(1) オートシェイプの設定"
		cellReport1.Cell("B1").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
	End Sub

	''' <summary>
	''' 対応するオートシェイプの挿入
	''' </summary>
	Private Sub InsertShape(ByVal x As Integer, ByVal y As Integer, ByVal shapetype As AdvanceSoftware.VBReport8.ShapeType, ByVal message As String, ByVal color As Color)
		cellReport1.Pos(x, y).Str = "種類:" + message
		cellReport1.Pos(x, y + 1).Str = "設定 [" + shapetype.ToString() + "]  番号 [" + CType(shapetype, Integer).ToString() + "]"
		Dim Drawing As AdvanceSoftware.VBReport8.Drawing
		If ((shapetype <> AdvanceSoftware.VBReport8.ShapeType.LeftBracket) And (shapetype <> AdvanceSoftware.VBReport8.ShapeType.RightBracket)) Then
			Drawing = cellReport1.Pos(x, y + 2, x + 2, y + 5).Drawing
		Else
			Drawing = cellReport1.Pos(x, y + 2, x, y + 5).Drawing
		End If
		Drawing.FillColor = color
		Drawing.AddShape(shapetype)
		'drawing.Init()
	End Sub

	''' <summary>
	''' 「テキストボックス」用のシートを作成
	''' </summary>
	Private Sub SetTextBoxWork()
		cellReport1.Page.Attr.Margin(5, 5, 5, 5, 5, 5)
		cellReport1.Page.Attr.Size(AdvanceSoftware.VBReport8.PageOrientation.Landscape, PaperKind.A4)
		cellReport1.Cell("B2").Str = "(1) テキストボックスの設定"
		cellReport1.Cell("B2").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold

		cellReport1.Cell("B16").Str = "(2) テキストの位置"
		cellReport1.Cell("B16").Attr.FontStyle = AdvanceSoftware.VBReport8.FontStyle.Bold
	End Sub
End Class

Partial Public Class ExcelModeSample
	Private Sub WorkSaveAS1()
		If (checkExcelMode.Checked = True) Then
			cellReport1.Cell("**Description1").Value = "【Excel の機能を使用したプレビュー、PDF ファイル出力】"
		Else
			cellReport1.Cell("**Description1").Value = "【VB-Report の独自仕様によるプレビュー、PDF ファイル出力】"
		End If
		cellReport1.Cell("**Description2").Value = "ExcelMode の設定にかかわらず、印刷範囲の適用はされません。(印刷のみ)"
	End Sub
	Private Sub WorkSaveAS2()
		If (checkExcelMode.Checked = True) Then
			cellReport1.Cell("**Description1").Value = "【Excel の機能を使用したプレビュー、PDF ファイル出力】"
			cellReport1.Cell("**Description3").Value = "プレビュー、PDFファイルに計算結果(合計)が設定されます。"
		Else
			cellReport1.Cell("**Description1").Value = "【VB-Report の独自仕様によるプレビュー、PDF ファイル出力】"
			cellReport1.Cell("**Description3").Value = "合計欄に計算結果が設定されます。"
		End If
		cellReport1.Cell("**Description2").Value = "ExcelMode の設定にかかわらず、印刷タイトルの適用はされません。(印刷のみ)"
	End Sub
	Private Sub WorkSaveAS3()
		If (checkExcelMode.Checked = True) Then
			cellReport1.Cell("**Description1").Value = "【Excel の機能を使用したプレビュー、PDF ファイル出力】"
		Else
			cellReport1.Cell("**Description1").Value = "【VB-Report の独自仕様によるプレビュー、PDF ファイル出力】"
		End If
		cellReport1.Cell("**Description2").Value = "ExcelMode の設定にかかわらず、複雑なシェイプや Excel 独自の機能は出力されません。"
	End Sub
	Private Sub WorkPrintOut1()
		If (checkExcelMode.Checked = True) Then
			cellReport1.Cell("**Description1").Value = "【Excel の機能を使用した印刷】"
			cellReport1.Cell("**Description2").Value = "印刷範囲が適用されます。"
		Else
			cellReport1.Cell("**Description1").Value = "【VB-Report の独自仕様による印刷】"
			cellReport1.Cell("**Description2").Value = "印刷範囲は適用されません。"
		End If
	End Sub

	Private Sub WorkPrintOut2()
		If (checkExcelMode.Checked = True) Then
			cellReport1.Cell("**Description1").Value = "【Excel の機能を使用した印刷】"
			cellReport1.Cell("**Description2").Value = "印刷タイトルが適用されます。"
			cellReport1.Cell("**Description3").Value = "合計欄に計算結果が設定されます。"
		Else
			cellReport1.Cell("**Description1").Value = "【VB-Report の独自仕様による印刷】"
			cellReport1.Cell("**Description2").Value = "印刷タイトルは適用されません。"
			cellReport1.Cell("**Description3").Value = "合計欄の計算結果は計算されません。(ApplyFormulaプロパティにて対応可)"
		End If
	End Sub

	Private Sub WorkPrintOut3()
		If (checkExcelMode.Checked = True) Then
			cellReport1.Cell("**Description1").Value = "【Excel の機能を使用したプレビュー、PDF ファイル出力】"
			cellReport1.Cell("**Description2").Value = "複雑なシェイプや Excel 独自の機能が印刷されます。"
		Else
			cellReport1.Cell("**Description1").Value = "【VB-Report の独自仕様によるプレビュー、PDF ファイル出力】"
			cellReport1.Cell("**Description2").Value = "複雑なシェイプや Excel 独自の機能は印刷されません。"
		End If
	End Sub
End Class

Partial Public Class BarCodeReportSample
	''' <summary>
	''' コンビニバーコード付きの帳票ドキュメントを作成します。
	''' </summary>
	Public Sub WorkConviniBarCodeReport()
		Dim iraiShimei As String = "山田太朗"
		Dim iraiBangou As String = "0075078287"
		Dim kouza5keta As String = "99165"
		Dim kouza1keta As String = "2"
		Dim kouza7keta As String = "9513764"
		Dim kingaku As String = "21420"
		Dim zeigaku As String = "1020"
		Dim uketoriNin As String = "ＡＤＶシステム"
		Dim shiharaibi As String = "2014/01/21"
		Dim length As Integer = 0
		Dim su As String = ""

		cellReport1.Report.File()
		cellReport1.ScaleMode = AdvanceSoftware.VBReport8.ScaleMode.Millimeter
		cellReport1.Page.Start("払込票", "1")

		' 口座番号5桁(**口座番号5-5～**口座番号5-1)
		length = kouza5keta.Length
		For intidx As Integer = 1 To length
			su = kouza5keta.Substring(length - intidx, 1)
			cellReport1.Cell("**口座番号5-" + intidx.ToString()).Value = su
		Next
		' 口座番号5桁
		cellReport1.Cell("**口座番号5").Value = kouza5keta

		' 口座番号1桁
		cellReport1.Cell("**口座番号1").Value = kouza1keta

		' 口座番号7桁(**口座番号7-7～**口座番号7-1)
		length = kouza7keta.Length
		For intidx As Integer = 1 To length
			su = kouza7keta.Substring(length - intidx, 1)
			cellReport1.Cell("**口座番号7-" + intidx.ToString()).Value = su
		Next
		'  口座番号7桁
		cellReport1.Cell("**口座番号7").Value = kouza7keta

		' 金額8桁(**金額8～**金額1)
		length = kingaku.Length
		For intidx As Integer = 1 To length
			su = kingaku.Substring(length - intidx, 1)
			cellReport1.Cell("**金額" + intidx.ToString()).Value = su
		Next
		' 金額8桁
		cellReport1.Cell("**金額").Value = Convert.ToInt32(kingaku)
		' 税額
		Dim intZeigaku As Integer = Convert.ToInt32(zeigaku)
		cellReport1.Cell("**税額").Value = "(内消費税額 " + String.Format("{0:N0}", intZeigaku) + "円)"
		' 受取人
		cellReport1.Cell("**受取人").Value = uketoriNin
		' 依頼者氏名
		cellReport1.Cell("**依頼者氏名").Value = iraiShimei
		' 依頼者番号
		cellReport1.Cell("**依頼者番号").Value = iraiBangou
		' 支払期限
		cellReport1.Cell("**支払期限").Value = Convert.ToDateTime(shiharaibi).ToOADate()

		cellReport1.Cell("DB58:DL63").Drawing.AddImage(SampleWork.InputFilePath("mark.png"))

	End Sub
End Class
#End Region
