' このソースファイルはアドバンスソフトウェア開発ツール用のサンプルプログラムの共通処理です。
' サンプルプログラムが正しく動作しなくなる恐れがあるため、コードエディタで変更しないでください。
Imports System.Collections
Imports System.Drawing

Public Class SampleControl

	Public Sub New()
		' この呼び出しは、Windows フォーム デザイナで必要です。
		InitializeComponent()

		' InitializeComponent() 呼び出しの後で初期化を追加します。
		' コードビューの文字サイズを設定する。
		If (codeViewFontSize = 1) Then
			radioFontMiddle.Checked = True
		ElseIf (codeViewFontSize = 2) Then
			radioFontLarge.Checked = True
		End If
	End Sub
	''' <summary>
	''' 【サンプル用処理】ソースコードの読み込み(コードビュー)
	''' </summary>
	Protected WriteOnly Property SouceCodePath() As String
		Set(ByVal value As String)
			code = ""
			sourceCodeCommentList = New List(Of String)
			If (String.IsNullOrEmpty(value) = True) Then Return
			Try
				code = System.IO.File.ReadAllText(value)
				code = code.Replace(vbTab, "    ")
			Catch ex As Exception
				code = "ソースコードが表示できません。" + vbCrLf + "表示したい場合、サンプルプロジェクトのファイル構成のまま、デバッグを行ってください"
			End Try
			Dim keyword As String = ".*'.*\r"
			Dim match As System.Text.RegularExpressions.Match = New System.Text.RegularExpressions.Regex(keyword).Match(code)
			While (match.Success)
				sourceCodeCommentList.Add(code.Substring(match.Index, match.Length))
				match = match.NextMatch()
			End While
		End Set
	End Property
	''' <summary>
	''' 【サンプル用処理】ソースコードの表示(コメント行の色設定)
	''' </summary>
	Protected Sub WorkReadCode()
		Dim Color As Color = Color.Green
		textCodeView.Clear()
		textCodeView.Text = code
		Dim pos As Integer = -1
		For idx As Integer = 0 To (sourceCodeCommentList.Count - 1)
			pos = textCodeView.Find(sourceCodeCommentList(idx), pos + 1, RichTextBoxFinds.MatchCase)
			If (pos >= 0) Then
				textCodeView.SelectionStart = pos
				textCodeView.SelectionLength = sourceCodeCommentList(idx).Length
				textCodeView.SelectionColor = Color
			Else
				Exit For
			End If
		Next
	End Sub
	''' <summary>
	''' 【サンプル用処理】実行した処理のソースコードにジャンプ(コードビュー)
	''' </summary>
	Protected Sub WorkJumpCord()
		WorkJumpCord("")
	End Sub
	''' <summary>
	''' 【サンプル用処理】実行した処理のソースコードにジャンプ(コードビュー)
	''' </summary>
	Protected Sub WorkJumpCord(ByVal methodName As String)
		If (methodName = "") Then
			Try
				Dim callerFrame As System.Diagnostics.StackFrame = New System.Diagnostics.StackFrame(2)
				methodName = callerFrame.GetMethod().Name
				callerFrame = Nothing
			Catch ex As Exception

			End Try
		End If
		If (String.IsNullOrEmpty(textCodeView.Text) = True) Then Return
		If (String.IsNullOrEmpty(methodName) = True) Then Return
		textCodeView.Select(textCodeView.Text.Length, 1)
		textCodeView.ScrollToCaret()
		Dim searchLength As Integer = methodName.Length
		Dim pos As Integer = textCodeView.Text.IndexOf(methodName, 0)
		If (pos = -1) Then Return '該当なし
		textCodeView.Select(pos, searchLength)
		textCodeView.ScrollToCaret()

	End Sub
	' コードビューの文字サイズを画面間で保持するためのstatic変数
	Private Shared codeViewFontSize As Integer = 0
	' サンプルコード
	Private code As String = ""
	' コメント行のリスト(色設定時に使用)
	Private sourceCodeCommentList As New List(Of String)

	''' <summary>
	''' 【サンプル用処理】コードビューの文字サイズを変更
	''' </summary>
	''' <param name="s"></param>
	''' <param name="e"></param>
	''' <remarks></remarks>
	Private Sub radioFontSizeSelected(ByVal s As System.Object, ByVal e As System.EventArgs) Handles radioFontSmall.CheckedChanged, radioFontMiddle.CheckedChanged, radioFontLarge.CheckedChanged
		If (CType(s, RadioButton).Name = "radioFontSmall") Then

			codeViewFontSize = 0
			textCodeView.Font = New Font(textCodeView.Font.Name, 9.0F)
		End If
		If (CType(s, RadioButton).Name = "radioFontMiddle") Then

			codeViewFontSize = 1
			textCodeView.Font = New Font(textCodeView.Font.Name, 11.0F)
		End If
		If (CType(s, RadioButton).Name = "radioFontLarge") Then

			codeViewFontSize = 2
			textCodeView.Font = New Font(textCodeView.Font.Name, 13.0F)
		End If
		WorkReadCode()
	End Sub

	Private Sub CopyCode(ByVal s As System.Object, ByVal e As System.EventArgs) Handles buttonCopyCode.Click
		Clipboard.SetDataObject(textCodeView.SelectedText)
	End Sub
End Class
