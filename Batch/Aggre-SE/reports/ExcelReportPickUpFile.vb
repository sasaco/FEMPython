Imports AdvanceSoftware.VBReport8
Imports System.ComponentModel
Imports System.Windows

Class ExcelReportPickUpFile
    Inherits ExcelReport

    Private PickupListOfString As New List(Of String)

    Sub 断面力ピックアップファイル作成スタート(Optional ShowMode As Integer = 0)

        '①:応答値を計算する(プレビューなしで) -----------------------------------------------
        Try
            Call MyBase.総括表作成スタート(3)
        Catch ex As Exception
            Throw ex
        End Try
        If MyBase.CaseDB Is Nothing Then Return
        If MyBase.CaseDB.Count = 0 Then Return

        '②:断面力ピックアップファイルを出力する。 -----------------------------------------------------------------
        Dim pb As New MyProgressBarForm("", New DoWorkEventHandler(AddressOf Createピックアップファイル))
        pb.ShowDialog()


        '③:断面力ピックアップファイルを保存する。 -----------------------------------------------------------------
        Try
            If ShowMode >= 0 Then
                Dim FileName As String = Input.FileName
                Dim SaveFileDialog As New SaveFileDialog
                SaveFileDialog.RestoreDirectory = True
                If System.IO.File.Exists(FileName) Then
                    SaveFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(FileName)
                    SaveFileDialog.FileName = System.IO.Path.GetFileNameWithoutExtension(FileName)
                End If
                SaveFileDialog.Filter = "データ ファイル (*.pik)|*.pik|すべてのファイル (*.*)|*.*"

                If (SaveFileDialog.ShowDialog() = Forms.DialogResult.OK) Then
                    FileName = SaveFileDialog.FileName

                    '書き込むファイルが既に存在している場合は、上書きする
                    Dim sw As New System.IO.StreamWriter(FileName,
                        False,
                        System.Text.Encoding.GetEncoding("shift_jis"))
                    'resultの内容を書き込む

                    For Each s In PickupListOfString
                        sw.Write(s)
                        sw.Write(vbCrLf)
                    Next
                    '閉じる
                    sw.Close()

                    MsgBox("ピックアップファイルが保存されました" + vbLf +
                           FileName)
                End If
            Else
                Try
                    Dim Filename As String = System.IO.Path.ChangeExtension(Input.FileName, ".pik")
                    '書き込むファイルが既に存在している場合は、上書きする
                    Dim sw As New System.IO.StreamWriter(Filename,
                        False,
                        System.Text.Encoding.GetEncoding("shift_jis"))
                    'resultの内容を書き込む

                    For Each s In PickupListOfString
                        sw.Write(s)
                        sw.Write(vbCrLf)
                    Next
                    '閉じる
                    sw.Close()
                Catch ex As Exception
                    Throw ex
                End Try

            End If
        Catch ex As Exception
            Throw ex
        End Try


    End Sub

    Private Sub Createピックアップファイル(ByVal sender As Object, ByVal e As DoWorkEventArgs)

        If MyBase.CaseDB Is Nothing Then Return
        Dim bw As BackgroundWorker = DirectCast(sender, BackgroundWorker)

        'ピックアップファイルを集計
        bw.ReportProgress(0, "ピックアップファイルを集計中... ")
        Dim PickUp() As PickUpFile = GetPickUpFile()
        If PickUp Is Nothing Then Return

        'ピックアップファイルの形式に直す
        Try
            PickupListOfString.Add("Batch ver1.0.0")
            For caseNo As Integer = 0 To PickUp.Count - 1
                For Each s In PickUp(caseNo).GetPickupTxet(caseNo + 1)
                    Call PickupListOfString.Add(s)
                Next
            Next
        Catch ex As Exception
            Throw ex
        End Try

    End Sub


    Private Function GetPickUpFile() As PickUpFile()

        Dim caseNum As Integer = 2
        If FormSettings.Option_PickUpFileCase > 0 Then caseNum = 9

        Dim PickUp(0 To caseNum) As PickUpFile
        Dim activeFlg As Boolean = False

        '保存するファイル内容の設定
        Dim MemberCount As Integer = MyBase.CaseDB.First._SNAPDB.GetMemberCount

        Try

            For caseNo As Integer = 0 To caseNum
                PickUp(caseNo) = New PickUpFile
                ReDim PickUp(caseNo).部材(0 To MemberCount - 1)
                For i As Integer = 0 To MemberCount - 1
                    PickUp(caseNo).部材(i) = New PickUp部材
                    PickUp(caseNo).部材(i).部材No = i + 1
                    PickUp(caseNo).部材(i).部材長 = CaseDB.First._SNAPDB.GetMemberLength(i + 1)
                    For j = 0 To 1
                        PickUp(caseNo).部材(i).M着目(j) = New clsPickUp断面力
                        PickUp(caseNo).部材(i).S着目(j) = New clsPickUp断面力
                        PickUp(caseNo).部材(i).N着目(j) = New clsPickUp断面力
                    Next
                Next
            Next

            For DBindex As Integer = 0 To MyBase.CaseDB.Count() - 1
                Dim DB = MyBase.CaseDB(DBindex)
                If DB._解析対象(8) = True Then
                    activeFlg = True
                    For caseNo As Integer = 0 To caseNum
                        Dim istep As Integer
                        Select Case caseNo
                            Case 0 '全ケース L1地震動
                                istep = DB.応答値.復旧性_L1震度step
                                SetPickup(PickUp(caseNo), MemberCount, istep, DB, DBindex)

                            Case 1 '全ケース 復旧性L2地震動
                                istep = DB.応答値.復旧性_最大応答step
                                SetPickup(PickUp(caseNo), MemberCount, istep, DB, DBindex)

                            Case 2 '全ケース 安全性L2地震動
                                istep = DB.応答値.安全性_最大応答step
                                SetPickup(PickUp(caseNo), MemberCount, istep, DB, DBindex)

                            Case 3 '普通地盤 L1地震動
                                If DB._Is液状化L1ケース = False _
                                   And DB._Is液状化L22ケース = False _
                                   And DB._SNAPDB.InputInfo.KihonInfo.KaisekiFlg = 1 Then
                                    istep = DB.応答値.復旧性_L1震度step
                                    SetPickup(PickUp(caseNo), MemberCount, istep, DB, DBindex)
                                End If

                            Case 4 '普通地盤 復旧性L2地震動
                                If DB._Is液状化L1ケース = False _
                                   And DB._Is液状化L22ケース = False _
                                   And DB._SNAPDB.InputInfo.KihonInfo.KaisekiFlg = 1 Then
                                    istep = DB.応答値.復旧性_最大応答step
                                    SetPickup(PickUp(caseNo), MemberCount, istep, DB, DBindex)
                                End If

                            Case 5 '普通地盤 安全性L2地震動
                                If DB._Is液状化L1ケース = False _
                                   And DB._Is液状化L22ケース = False _
                                   And DB._SNAPDB.InputInfo.KihonInfo.KaisekiFlg = 1 Then
                                    istep = DB.応答値.安全性_最大応答step
                                    SetPickup(PickUp(caseNo), MemberCount, istep, DB, DBindex)
                                End If

                            Case 6 '液状化地盤 L1地震動
                                If DB._Is液状化L1ケース = True Then
                                    istep = DB.応答値.復旧性_L1震度step
                                    SetPickup(PickUp(caseNo), MemberCount, istep, DB, DBindex)
                                End If

                            Case 7 '液状化地盤 安全性L2地震動
                                If DB._Is液状化L22ケース = True Then
                                    istep = DB.応答値.安全性_最大応答step
                                    SetPickup(PickUp(caseNo), MemberCount, istep, DB, DBindex)
                                End If

                            Case 8 '応答変位法 L1地震動
                                If DB._SNAPDB.InputInfo.KihonInfo.KaisekiFlg <> 1 Then
                                    '荷重増分法
                                    If DB._解析対象(5) = True Then
                                        'L1地震動の照査を行っている
                                        istep = DB.応答値.maxStep
                                        SetPickup(PickUp(caseNo), MemberCount, istep, DB, DBindex)
                                    End If
                                End If

                            Case 9 '応答変位法 L2地震動
                                If DB._SNAPDB.InputInfo.KihonInfo.KaisekiFlg <> 1 Then
                                    '荷重増分法
                                    If DB._解析対象(5) = False Then
                                        'L1地震動の照査を行っていない
                                        istep = DB.応答値.maxStep
                                        SetPickup(PickUp(caseNo), MemberCount, istep, DB, DBindex)
                                    End If
                                End If

                        End Select
                    Next caseNo
                End If
            Next DBindex
        Catch ex As Exception
            Return Nothing
        End Try



        Try
            '値の入っていないケースの断面力を 0 にする
            For caseNo As Integer = 0 To caseNum
                For i As Integer = 0 To MemberCount - 1
                    With PickUp(caseNo).部材(i)
                        For j = 0 To 1 'i端 と j端
                            With .M着目(j)
                                If .MaxCase = -1 Then
                                    .MaxM = 0
                                    .MaxS = 0
                                    .MaxN = 0
                                End If
                                If .MinCase = -1 Then
                                    .MinM = 0
                                    .MinS = 0
                                    .MinN = 0
                                End If
                            End With
                            With .S着目(j)
                                If .MaxCase = -1 Then
                                    .MaxM = 0
                                    .MaxS = 0
                                    .MaxN = 0
                                End If
                                If .MinCase = -1 Then
                                    .MinM = 0
                                    .MinS = 0
                                    .MinN = 0
                                End If
                            End With
                            With .N着目(j)
                                If .MaxCase = -1 Then
                                    .MaxM = 0
                                    .MaxS = 0
                                    .MaxN = 0
                                End If
                                If .MinCase = -1 Then
                                    .MinM = 0
                                    .MinS = 0
                                    .MinN = 0
                                End If
                            End With
                        Next j
                    End With
                Next i
            Next caseNo
        Catch ex As Exception
            Return Nothing
        End Try

        If activeFlg = True Then
            Return PickUp
        Else
            Return Nothing
        End If
    End Function

    Private Sub SetPickup(ByRef PickUp As PickUpFile, _
                          ByVal MemberCount As Integer, _
                          ByVal istep As Integer, _
                          ByVal DB As CalculationCase, _
                          ByVal DBindex As Integer)

        For i As Integer = 0 To MemberCount - 1
            With PickUp.部材(i)
                Dim m As Single
                Dim s As Single
                Dim n As Single
                For j = 0 To 1 'i端 と j端
                    For stp As Integer = 0 To istep
                        With DB._SNAPDB.OutputInfo.StepCtrl(stp).MemberItem(i + 1)
                            If j = 0 Then
                                m = .Mi
                                s = .Si
                                n = .Ni
                            Else
                                m = .Mj
                                s = .Sj
                                n = .Nj
                            End If
                        End With
                        With .M着目(j)
                            If .MaxM < m Then
                                .MaxM = m
                                .MaxS = s
                                .MaxN = n
                                .MaxCase = DBindex + 1
                            End If
                            If .MinM > m Then
                                .MinM = m
                                .MinS = s
                                .MinN = n
                                .MinCase = DBindex + 1
                            End If
                        End With
                        With .S着目(j)
                            If .MaxS < s Then
                                .MaxM = m
                                .MaxS = s
                                .MaxN = n
                                .MaxCase = DBindex + 1
                            End If
                            If .MinS > s Then
                                .MinM = m
                                .MinS = s
                                .MinN = n
                                .MinCase = DBindex + 1
                            End If
                        End With
                        With .N着目(j)
                            If .MaxN < n Then
                                .MaxM = m
                                .MaxS = s
                                .MaxN = n
                                .MaxCase = DBindex + 1
                            End If
                            If .MinN > n Then
                                .MinM = m
                                .MinS = s
                                .MinN = n
                                .MinCase = DBindex + 1
                            End If
                        End With
                    Next stp
                Next j
            End With
        Next i
    End Sub



    Public Class PickUpFile
        Public 部材() As PickUp部材

        Public Function GetPickupTxet(index As Integer) As List(Of String)
            Dim result As New List(Of String)

            For i As Integer = 0 To 部材.Count - 1
                Try
                    Dim str As String = ""
                    Call SetTxtData(str, 1, 5, index)
                    Call SetTxtData(str, 6, 10, "M")
                    Call SetTxtData(str, 11, 15, 部材(i).部材No)
                    For j As Integer = 0 To 1
                        With 部材(i).M着目(j)
                            Call SetTxtData(str, 16, 20, .MaxCase)
                            Call SetTxtData(str, 21, 25, .MinCase)
                            Call SetTxtData(str, 26, 30, IIf(j = 0, "ITAN", "JTAN"))
                            Call SetTxtData(str, 31, 40, IIf(j = 0, "0.000", 部材(i).部材長.ToString("F3")))
                            Call SetTxtData(str, 41, 50, .MaxM.ToString("F2"))
                            Call SetTxtData(str, 51, 60, .MaxS.ToString("F2"))
                            Call SetTxtData(str, 61, 70, .MaxN.ToString("F2"))
                            Call SetTxtData(str, 71, 80, .MinM.ToString("F2"))
                            Call SetTxtData(str, 81, 90, .MinS.ToString("F2"))
                            Call SetTxtData(str, 91, 100, .MinN.ToString("F2"))
                        End With
                        result.Add(str)
                    Next j
                Catch ex As Exception

                End Try
            Next i
            For i As Integer = 0 To 部材.Count - 1
                Try
                    Dim str As String = ""
                    Call SetTxtData(str, 1, 5, index)
                    Call SetTxtData(str, 6, 10, "S")
                    Call SetTxtData(str, 11, 15, 部材(i).部材No)
                    For j As Integer = 0 To 1
                        With 部材(i).S着目(j)
                            Call SetTxtData(str, 16, 20, .MaxCase)
                            Call SetTxtData(str, 21, 25, .MinCase)
                            Call SetTxtData(str, 26, 30, IIf(j = 0, "ITAN", "JTAN"))
                            Call SetTxtData(str, 31, 40, IIf(j = 0, "0.000", 部材(i).部材長.ToString("F3")))
                            Call SetTxtData(str, 41, 50, .MaxM.ToString("F2"))
                            Call SetTxtData(str, 51, 60, .MaxS.ToString("F2"))
                            Call SetTxtData(str, 61, 70, .MaxN.ToString("F2"))
                            Call SetTxtData(str, 71, 80, .MinM.ToString("F2"))
                            Call SetTxtData(str, 81, 90, .MinS.ToString("F2"))
                            Call SetTxtData(str, 91, 100, .MinN.ToString("F2"))
                        End With
                        result.Add(str)
                    Next j
                Catch ex As Exception

                End Try
            Next i
            For i As Integer = 0 To 部材.Count - 1
                Try
                    Dim str As String = ""
                    Call SetTxtData(str, 1, 5, index)
                    Call SetTxtData(str, 6, 10, "N")
                    Call SetTxtData(str, 11, 15, 部材(i).部材No)
                    For j As Integer = 0 To 1
                        With 部材(i).N着目(j)
                            Call SetTxtData(str, 16, 20, .MaxCase)
                            Call SetTxtData(str, 21, 25, .MinCase)
                            Call SetTxtData(str, 26, 30, IIf(j = 0, "ITAN", "JTAN"))
                            Call SetTxtData(str, 31, 40, IIf(j = 0, "0.000", 部材(i).部材長.ToString("F3")))
                            Call SetTxtData(str, 41, 50, .MaxM.ToString("F2"))
                            Call SetTxtData(str, 51, 60, .MaxS.ToString("F2"))
                            Call SetTxtData(str, 61, 70, .MaxN.ToString("F2"))
                            Call SetTxtData(str, 71, 80, .MinM.ToString("F2"))
                            Call SetTxtData(str, 81, 90, .MinS.ToString("F2"))
                            Call SetTxtData(str, 91, 100, .MinN.ToString("F2"))
                        End With
                        result.Add(str)
                    Next j
                Catch ex As Exception

                End Try
            Next i
            Return result

        End Function

        Private Sub SetTxtData(ByRef rsDataLine As String, ByVal viCol As Integer, _
                               ByVal viEndCol As Integer, stTarget As String)

            Dim ILength As Integer
            ILength = viEndCol - viCol + 1

            Dim sTmp As String

            'データ文字列が短い場合は調整する
            If Len(rsDataLine) < viCol + ILength - 1 Then
                rsDataLine = rsDataLine.PadRight(viCol + ILength - 1)
            End If

            '埋め込むデータを文字列化する
            If Len(stTarget) < ILength Then
                sTmp = stTarget.PadLeft(ILength) '右詰で規定の文字数分切り取る
            Else
                sTmp = Left(stTarget, ILength)  '左詰で規定の文字数分切り取る
            End If

            'データ文字列の中身を置きかえる
            Mid(rsDataLine, viCol, ILength) = sTmp

        End Sub

    End Class

    Public Class PickUp部材
        Public 部材No As Integer
        Public 部材長 As Single

        Public M着目(0 To 1) As clsPickUp断面力
        Public S着目(0 To 1) As clsPickUp断面力
        Public N着目(0 To 1) As clsPickUp断面力
    End Class

    Public Class clsPickUp断面力
        Public MaxCase As Integer
        Public MaxM As Single
        Public MaxS As Single
        Public MaxN As Single

        Public MinCase As Integer
        Public MinM As Single
        Public MinS As Single
        Public MinN As Single

        Sub New()

            MaxCase = -1
            MaxM = Single.MinValue
            MaxS = Single.MinValue
            MaxN = Single.MinValue

            MinCase = -1
            MinM = Single.MaxValue
            MinS = Single.MaxValue
            MinN = Single.MaxValue

        End Sub


    End Class
End Class
