Imports System.ComponentModel

Public Class Main

    Shared Sub Main()
        Application.EnableVisualStyles()
        Try
            Dim cmds As String() = System.Environment.GetCommandLineArgs()

            'コマンドライン引数のチェック
            Dim key0 As String = ""
            Dim key1 As String = ""

            Dim OnLoadFlg As Boolean = False
            For Each s In cmds
                key0 = s.Trim
                key1 = Left(key0, 8)
                If key1 = "19Kw6MFa" Then
                    OnLoadFlg = True
                    Exit For
                End If
            Next

#If DEBUG Then
            OnLoadFlg = True
            key0 = "X001D10 111111119999992999991"
#End If

            If OnLoadFlg = True Then
                Dim Option1 As String = ""
                Dim Option2 As String = ""
                Dim Option3 As String = ""
                '有料オプション  '1111111 1999999 2999991
                Option1 = Mid(key0, 9, 7)  'オプション１
                Option2 = Mid(key0, 16, 7) 'オプション２
                Option3 = Mid(key0, 23, 7) 'オプション３

                '(1) 杭の段落とし図作成機能のライセンスチェック
                Dim opt1 As Integer
                If Integer.TryParse(Mid(Option1, 1, 1), opt1) Then
                    FormSettings.Option_杭の抵抗モーメント図作成機能 = IIf(opt1 = 1, 1, 0)
                End If

                '(2) ユーザーが決めた任意の応答塑性率の直接入力機能
                Dim opt2 As Integer
                If Integer.TryParse(Mid(Option1, 4, 1), opt2) Then
                    FormSettings.Option_応答塑性率の直接入力 = IIf(opt2 = 1, 1, 0)
                End If

                '(3) せん断部材照査に使われる総研設計標準以外のせん断耐力計算機能（鉄道運輸機構、JR東日本など）のライセンスチェック
                Dim opt3 As Integer
                If Integer.TryParse(Mid(Option1, 2, 1), opt3) Then
                    FormSettings.Option_カスタムせん断耐力 = IIf(opt3 = 1, 11, 0)
                End If

                '(4) 液状化による低減係数を考慮する連携機能のライセンスチェック
                Dim opt4 As Integer
                If Integer.TryParse(Mid(Option1, 3, 1), opt4) Then
                    FormSettings.Option_液状化による低減係数を考慮する連携機能 = IIf(opt4 = 1, 1, 0)
                End If

                '(5) 地盤の不整形性の影響を考慮した応答値の算定機能のライセンスチェック
                Dim opt5 As Integer
                If Integer.TryParse(Mid(Option1, 3, 1), opt5) Then
                    FormSettings.Option_不整形地盤 = IIf(opt5 = 1, 1, 0)
                End If

                '(6) Aspect-SEの連携機能のライセンスチェック
                Dim opt6 As Integer
                If Integer.TryParse(Mid(Option1, 4, 1), opt6) Then
                    FormSettings.Option_任意スペクトル = IIf(opt6 = 1, 1, 0)
                End If

                '起動
                Dim MainForm = New MDIParentForm
                MainForm.ShowDialog()
            Else
                ' 引数に指定があったら起動する
                If CheckCL(cmds) = False Then
                    MsgBox("直接起動は、できません")
                End If
            End If

        Catch ex As Exception
            MsgBox("起動に失敗しました")
        End Try


    End Sub

    ''' <summary>コマンドライン引数の処理</summary>
    Private Shared Function CheckCL(cmds() As String) As Boolean

        Dim result As Boolean = False

        For Each DataPath In cmds

            If System.IO.File.Exists(DataPath) Then

                Dim sr As New System.IO.StreamReader(DataPath,
                System.Text.Encoding.GetEncoding("shift_jis"))
                Dim s As String = sr.ReadToEnd()
                sr.Close()


                FormSettings.Option_標準総括表は１列 = 1
                FormSettings.Option_各解析ケースの結果出力 = 1

                Try
                    Input.Clear()
                    Input.SetXmData(s)

                    If Input.Data.cmd > 0 Then

                        Input.FileName = DataPath

                        For i = 0 To Input.DATAROWS - 1
                            Dim FilePath As String = Input.Data.CaseName.CaseNamePath(i)
                            If FilePath <> "" Then
                                If Input.Data.SNAP.SnapDB(i) Is Nothing Then
                                    If Input.Data.SNAP.SetSnapDB(i, FilePath, False) = False Then
                                        Input.Data.CaseName.CaseNamePath(i) = ""
                                    End If
                                End If
                            Else
                                Input.Data.SNAP.DeltSnapDB(i)
                            End If
                        Next

                        Select Case Input.Data.cmd
                            Case 1
                                Dim PDFReport As New PDFReport()
                                PDFReport.計算書作成スタート(-1)
                                result = True
                            Case 2
                                Dim PDFReport As New PDFReport()
                                PDFReport.計算書作成スタート(-2)
                                result = True
                            Case 3
                                Dim ExcelReport As New ExcelReport()
                                ExcelReport.総括表作成スタート(-1)
                                result = True
                            Case 4
                                Dim PDFReport As New PDFReportPileAnchorBar()
                                PDFReport.計算書作成スタート(-1)
                                result = True
                            Case 5
                                Dim ExcelReport As New ExcelReportPickUpFile()
                                ExcelReport.断面力ピックアップファイル作成スタート(-1)
                                result = True
                            Case 6
                                ' 大量解析ソフト用の動作
                                FormSettings.Option_各解析ケースの結果出力 = 1
                                FormSettings.Option_せん断降伏点の出力 = 1
                                FormSettings.Option_大量解析プログラム = 1
                                Dim PDFReport As New PDFReport()
                                PDFReport.計算書作成スタート(-6)
                                result = True

                        End Select
                    End If
                Catch ex As Exception

                End Try

            End If
        Next
        Return result

    End Function
End Class
