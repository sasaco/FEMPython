Imports System.ComponentModel
Imports AdvanceSoftware.VBReport8

Class ExcelReport : Implements IDisposable

#Region "メンバ変数"

    Private CellReport1 As AdvanceSoftware.VBReport8.CellReport
    Protected CaseDB As List(Of CalculationCase)
    Protected ElementDB As List(Of CalculationElement)
    Protected FoundationDB As List(Of CalculationFoundation)
    Private ViewerForm As ViewerForm
    Private 出力モード As Integer

    Public Sub Dispose() _
            Implements IDisposable.Dispose
        ViewerForm.Dispose()
        CellReport1.Dispose()
    End Sub
#End Region

#Region "総括表作成スタート関数"
    ''' <summary>
    ''' 総括表作成スタート
    ''' </summary>
    ''' <param name="ShowMode">
    ''' 0 : Excel帳票を作成し、モーダレスダイアログで、ViewerFormを表示する
    ''' 1 : Excel帳票を作成し、モーダルダイアログ(ダイアログが閉じないと何もできない)で、ViewerFormを表示する
    ''' 2 : Excel帳票を作成し、ViewerFormを表示しない
    ''' 3 : 解析結果の集計のみで、Excel帳票は作成しない(ViewerFormを表示しない)
    ''' </param>
    ''' <remarks></remarks>
    Public Sub 総括表作成スタート(Optional ShowMode As Integer = 0)
        Dim pb As New MyProgressBarForm("", New DoWorkEventHandler(AddressOf Create総括表))
        pb.ShowDialog()
        Me.出力モード = ShowMode

        If Me.出力モード >= 0 Then

            If Me.出力モード < 2 Then
                'プレビューするコード 0, 1
                ViewerForm = New ViewerForm()
                ViewerForm.ViewerControl1.Clear()
                ViewerForm.ViewerControl1.Document = CellReport1.Document
                AddHandler ViewerForm.FormClosed, AddressOf ViewerForm_FormClosed
                Select Case ShowMode
                    Case 0
                        ViewerForm.Show()
                    Case 1
                        ViewerForm.ShowDialog()
                End Select
            End If

        Else
            Try
                Dim Filename As String = System.IO.Path.ChangeExtension(Input.FileName, ".xlsx")
                Me.SaveExcelDocument(Filename)
            Catch ex As Exception
                Throw ex
            End Try
        End If


        Call SaveFile()

    End Sub

    Private Sub ViewerForm_FormClosed(sender As Object, e As FormClosedEventArgs)
        CellReport1 = Nothing
    End Sub

    Private Sub Create総括表(ByVal sender As Object, ByVal e As DoWorkEventArgs)

        Try
            Dim bw As BackgroundWorker = DirectCast(sender, BackgroundWorker)
            bw.ReportProgress(0, "ドキュメント 生成中... 1/6")
            If Me.出力モード <= 3 Then
                'Excelドキュメントを作成する。------------------------------------------
                CellReport1 = New AdvanceSoftware.VBReport8.CellReport
                CellReport1.Report.Start()

                Dim strmExcel As System.IO.Stream
                Select Case Input.Data.Calculation.ProductStyle
                    Case 0
                        '標準スタイル
                        strmExcel = System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("Aggre.総括表type1.xlsx")
                    Case 1
                        'JRSEカスタムスタイル
                        strmExcel = System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("Aggre.総括表type2.xlsx")
                    Case Else
                        Throw New Exception("出力帳票のスタイルが選択されていません。")
                        Return
                End Select
                CellReport1.Report.Embed(strmExcel)
            End If

            '入力データを集計する ------------------------------------------------------
            bw.ReportProgress(0, "入力データ 集計中... 2/6")
            CaseDB = GetCaseList(Input.Data)
            If CaseDB.Count <= 0 Then Throw New Exception("解析データがありません")
            ElementDB = GetElementList(Input.Data, CaseDB)
            FoundationDB = GetFoundationList(Input.Data, CaseDB)

            'あらかじめ応答値を計算する。 -------------------------------------------------
            Dim RemoveDB As New List(Of CalculationCase)
            For Each DB In CaseDB
                Dim test = DB.応答値
                If test.ErrorMessage.Length > 1 Then
                    If FormSettings.Option_大量解析プログラム = 0 Then
                        MsgBox(DB.DataName + vbLf + test.ErrorMessage)
                    End If
                    RemoveDB.Add(DB)
                    DB.ErrorMessage = test.ErrorMessage
                End If

            Next

            If Me.出力モード = 3 Then Return

            '出力帳票ドキュメントの作成 -------------------------------------------------
            bw.ReportProgress(0, "応答値一覧表 作成中... 3/6")

            'エラーのあるデータを消去する。 -------------------------------------------------
            For Each DB In RemoveDB
                CaseDB.Remove(DB)
            Next

            Call 応答値一覧表(CaseDB)


            bw.ReportProgress(0, "断面照査集計表 作成中... 4/6")
            Select Case Input.Data.Calculation.ProductStyle
                Case 0
                    '標準スタイル
                    Call 断面照査集計表1(ElementDB)
                Case 1
                    'JRSEカスタムスタイル
                    Call 断面照査集計表2(ElementDB)
            End Select

            bw.ReportProgress(0, "安全率一覧表 作成中... 5/6")
            Call 安全率一覧表(ElementDB)

            bw.ReportProgress(0, "基礎安定計算 作成中... 6/6")
            Call 基礎安定計算集計表(FoundationDB)

        Catch ex As Exception
            If FormSettings.Option_大量解析プログラム = 0 Then
                MsgBox(ex.Message)
            Else

            End If
        Finally
            CellReport1.Report.End()
        End Try
    End Sub


#End Region

#Region "各ケース毎出力の場合"

    ''' <summary>
    ''' pdf ドキュメントにおいて 照査表を各ケース毎に出力する必要があったため用意した
    ''' 分岐処理の入り口
    ''' </summary>
    ''' <param name="TargetCaseDB">対象のケースデータベース</param>
    ''' <remarks></remarks>
    Public Function Create照査表(TargetCaseDB As CalculationCase) As Boolean
        Dim result As Boolean = False
        Try
            CellReport1 = New AdvanceSoftware.VBReport8.CellReport
            CellReport1.Report.Start()

            Dim strmExcel As System.IO.Stream
            strmExcel = System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("Aggre.総括表type3.xlsx") '標準スタイル
            CellReport1.Report.Embed(strmExcel)

            Dim TmpCaseDB As New List(Of CalculationCase)
            TmpCaseDB.Add(TargetCaseDB)


            '断面照査集計表 作成中.............................................
            ElementDB = GetMemberList(Input.Data, TmpCaseDB)
            If ElementDB.Count > 0 Then
                result = True
                Call 断面照査集計表1(ElementDB, 1) '標準スタイル
            End If
            '基礎安定計算 作成中.............................................
            If TargetCaseDB._解析対象(6) = True Then
                FoundationDB = GetFoundationList(Input.Data, TmpCaseDB)
                If FoundationDB.Count > 0 Then
                    result = True
                    Call 基礎安定計算集計表(FoundationDB)
                End If
            End If

            CellReport1.Report.End()
        Catch ex As Exception
            If FormSettings.Option_大量解析プログラム = 0 Then
                MsgBox(ex.Message)
            Else

            End If
        End Try
        Return result
    End Function

    Public Sub SavePdfDocument(fileName As String)
        Try
            CellReport1.Report.SavePDF(fileName)
            'ViewerForm.ViewerControl1.SavePDF(fileName)
            'ViewerForm = Nothing
        Catch ex As Exception
            Throw New Exception("部材照査表等の出力に失敗しました。" + vbLf _
                                + "ヒント:「" + fileName + "」ファイルが閉じているか確認してください。")
        End Try

    End Sub

    Public Sub SaveExcelDocument(fileName As String)
        Try
            CellReport1.Report.SaveAs(fileName, AdvanceSoftware.VBReport8.ExcelVersion.ver2013)
        Catch ex As Exception
            Throw New Exception("部材照査表等の出力に失敗しました。" + vbLf _
                                + "ヒント:「" + fileName + "」ファイルが閉じているか確認してください。")
        End Try

    End Sub
#End Region

#Region "入力データを集計する"

    ''' <summary>解析ケース毎に 入力データを集計する。</summary>
    ''' <param name="Data">入力データ</param>
    ''' <returns>解析ケース毎の CalculationCaseリストを返す</returns>
    Private Function GetCaseList(Data As Datas) As List(Of CalculationCase)
        Dim result As New List(Of CalculationCase)

        If Data.Requirement.dgEkijoCase1.Length < Input.DATAROWS Then ReDim Preserve Data.Requirement.dgEkijoCase1(0 To Input.DATAROWS - 1)
        If Data.Requirement.dgEkijoCase21.Length < Input.DATAROWS Then ReDim Preserve Data.Requirement.dgEkijoCase21(0 To Input.DATAROWS - 1)
        If Data.Requirement.dgEkijoCase2.Length < Input.DATAROWS Then ReDim Preserve Data.Requirement.dgEkijoCase2(0 To Input.DATAROWS - 1)

        For i = 0 To Input.DATAROWS - 1
            Try
                Dim SNAPDB = Data.SNAP.SnapDB(i)
                If SNAPDB Is Nothing Then
                    Continue For
                End If
                If Data.CaseName.CaseAble(i) = False Then
                    Continue For
                End If
                'SNAPDB
                Dim jc As New CalculationCase(SNAPDB)
                jc.DataName = Data.CaseName.CaseName(i)
                jc.DataID = i
                '解析ケース入力画面
                With Data.CaseName
                    jc.Set照査対象(.analysisObject(i))
                    jc._解析対象(0) = (.dgCaseList_1(i) = -1)
                    jc._解析対象(1) = (.dgCaseList_2(i) = -1)
                    jc._解析対象(2) = (.dgCaseList_3(i) = -1)
                    jc._解析対象(3) = (.dgCaseList_4(i) = -1)
                    jc._解析対象(4) = (.dgCaseList_5(i) = -1)
                    jc._解析対象(5) = (.dgCaseList_6(i) = -1)
                    jc._解析対象(6) = (.dgCaseList_7(i) = -1)
                    jc._解析対象(7) = (.dgCaseList_8(i) = -1)
                    jc._解析対象(8) = (.dgCaseList_9(i) = -1)
                End With
                '照査パラメータ入力画面
                With Data.Requirement
                    jc.着目点 = ._AtPoint
                    jc.Set地域(._rbChiiki)
                    jc.Set地盤区分(._rbJIban)
                    jc.Set液状化区分(._rbEkijo)
                    jc._M65エリアの設定 = .cbM65Area
                    jc.震度の低減係数α = .tbAlfa
                    jc.SetIs液状化ケース(.dgEkijoCase1(i), .dgEkijoCase21(i), .dgEkijoCase2(i))
                    jc.Setスペクトル種類(._rbSpec)

                    jc._不整形地盤の影響 = ._cbηx
                    jc._L1不整形地盤の係数ηx = ._tbηxL1
                    jc._L2不整形地盤の係数ηx = ._tbηxL2

                    If FormSettings.Option_不整形地盤 >= 2 Then
                        '液状化の場合は、不整形地盤の影響を考慮しない
                        If jc._Is液状化L1ケース = True OrElse jc._Is液状化L22ケース = True Then
                            jc._不整形地盤の影響 = False
                            jc._L1不整形地盤の係数ηx = 0
                            jc._L2不整形地盤の係数ηx = 0
                        End If
                    End If



                End With
                '断面入力画面
                With Data.Element
                    jc.Set部材設定(._dgLimitLevel, .dgLimitLevel_title, ._dgLimitLevel_type, ._dgLimitLevel_VydType, ._dgLimitLevel_La, ._dgLimitLevel_SIJI)
                End With
                '基礎の入力画面
                Dim tmpCase As New List(Of CalculationCase)
                tmpCase.Add(jc)
                jc.Set基礎設定(GetFoundationList(Data, tmpCase))

                '登録
                result.Add(jc)

            Catch ex As Exception
                Throw ex
            End Try
        Next
        Return result
    End Function

    ''' <summary>断面毎(DL)に 入力データを集計する。</summary>
    ''' <param name="Data">入力データ</param>
    ''' <param name="CaseDB">CalculationCase のリスト</param>
    ''' <returns>断面毎(DL)の CalculationElementリストを返す</returns>
    Private Function GetElementList(Data As Datas, CaseDB As List(Of CalculationCase)) As List(Of CalculationElement)

        Dim result As New List(Of CalculationElement)
        Dim SNAPDB = Data.SNAP.SnapDB

        Dim MFCount As Integer = SNAPDB.GetMFCount
        For i As Integer = 1 To Data.Element.dgLimitLevel_typeCount
            Try
                Dim je As New CalculationElement(CaseDB)
                With Data.Element
                    Dim row As Integer = .dgLimitLevel_type(i - 1) - 1
                    If IsNumeric(.dgLimitLevel1(row)) Then
                        If i > MFCount Then
                            Dim DLNo As Integer = i - MFCount
                            je.DLNo = DLNo
                            je.DLinfo = SNAPDB.DLInfo(DLNo)

                            je.損傷レベルの制限値1 = .dgLimitLevel1(row)
                            je.損傷レベルの制限値2 = .dgLimitLevel2(row)
                            je.照査タイプ番号 = row + 1
                            je.引張側 = 0

                            'If Is上下照査(je.DLinfo) = True Then

                            '    '上側引張 *****
                            '    je.引張側 = -1
                            '    result.Add(je)
                            '    '下側引張 *****
                            '    Dim j2 As New CalculationElement(CaseDB)
                            '    j2.DLNo = je.DLNo
                            '    j2.DLinfo = je.DLinfo
                            '    j2.損傷レベルの制限値1 = je.損傷レベルの制限値1
                            '    j2.損傷レベルの制限値2 = je.損傷レベルの制限値2
                            '    j2.照査タイプ番号 = je.照査タイプ番号
                            '    j2.引張側 = 1
                            '    result.Add(j2)
                            'Else
                            result.Add(je)
                            'End If

                        Else
                            je.DLNo = -1 * i 'MFデータは マイナス番号
                            je.DLinfo = New SNAPDBLib.CSNAPDB.IDLInfo
                            je.DLinfo.Title = "MFデータ"
                            je.DLinfo.iType = -1

                            je.損傷レベルの制限値1 = .dgLimitLevel1(row)
                            je.損傷レベルの制限値2 = .dgLimitLevel2(row)
                            je.照査タイプ番号 = row + 1
                            je.引張側 = 0
                            result.Add(je)

                        End If
                    Else
                        If FormSettings.Option_大量解析プログラム = 0 Then
                            MsgBox(String.Format("DLNo{0} は、損傷レベルの制限値が正しく入力されていません。照査を省略します。", je.DLNo))
                        Else

                        End If
                    End If
                End With
            Catch ex As Exception
            End Try
        Next
        Return result

    End Function

    ''' <summary>部材毎(Member)に 入力データを集計する。</summary>
    ''' <param name="Data">入力データ</param>
    ''' <param name="CaseDB">CalculationCase のリスト</param>
    ''' <returns>部材毎(Member)の CalculationElementリストを返す</returns>
    Private Function GetMemberList(Data As Datas, CaseDB As List(Of CalculationCase)) As List(Of CalculationElement)

        Dim result As New List(Of CalculationElement)
        Dim SNAPDB = Data.SNAP.SnapDB
        Dim MFCount As Integer = SNAPDB.GetMFCount

        For MNo As Integer = 1 To CaseDB.First._SNAPDB.GetMemberCount
            Try
                Dim ApNo As Integer = CaseDB.First._SNAPDB.GetMember(MNo).M
                If ApNo > MFCount Then
                    Dim DLNo As Integer = CaseDB.First._SNAPDB.GetDLNo(MNo)
                    If DLNo > 0 Then
                        Dim je As New CalculationElement(CaseDB)
                        je.DLNo = DLNo
                        je.MNo = MNo
                        je.DLinfo = SNAPDB.DLInfo(DLNo)
                        With Data.Element
                            Dim row As Integer = .dgLimitLevel_type(DLNo - 1 + MFCount) - 1
                            If IsNumeric(.dgLimitLevel1(row)) Then
                                je.損傷レベルの制限値1 = .dgLimitLevel1(row)
                                je.損傷レベルの制限値2 = .dgLimitLevel2(row)
                                je.照査タイプ番号 = row + 1
                                je.引張側 = 0
                                'If Is上下照査(je.DLinfo) = True Then

                                '    '上側引張 *****
                                '    je.引張側 = -1
                                '    If je.Is照査(CaseDB.First) Then result.Add(je)
                                '    '下側引張 *****
                                '    Dim j2 As New CalculationElement(CaseDB)
                                '    j2.DLNo = je.DLNo
                                '    j2.MNo = je.MNo
                                '    j2.DLinfo = je.DLinfo
                                '    j2.損傷レベルの制限値1 = je.損傷レベルの制限値1
                                '    j2.損傷レベルの制限値2 = je.損傷レベルの制限値2
                                '    j2.照査タイプ番号 = je.照査タイプ番号
                                '    j2.引張側 = 1
                                '    If j2.Is照査(CaseDB.First) Then result.Add(j2)
                                'Else
                                If je.Is照査(CaseDB.First) Then result.Add(je)
                                'End If
                            Else
                                If FormSettings.Option_大量解析プログラム = 0 Then
                                    MsgBox(String.Format("DLNo{0} は、損傷レベルの制限値が正しく入力されていません。照査を省略します。", DLNo))
                                Else

                                End If
                            End If
                        End With
                    End If
                ElseIf ApNo > 0 Then
                    Dim je As New CalculationElement(CaseDB)
                    je.DLNo = -1 * ApNo 'MFデータは マイナス番号
                    je.MNo = MNo
                    je.DLinfo = New SNAPDBLib.CSNAPDB.IDLInfo
                    je.DLinfo.Title = "MFデータ"
                    je.DLinfo.iType = -1

                    With Data.Element
                        Dim row As Integer = .dgLimitLevel_type(ApNo - 1) - 1
                        If IsNumeric(.dgLimitLevel1(row)) Then
                            je.損傷レベルの制限値1 = .dgLimitLevel1(row)
                            je.損傷レベルの制限値2 = .dgLimitLevel2(row)
                            je.照査タイプ番号 = row + 1
                            je.引張側 = 0
                            If je.Is照査(CaseDB.First) Then result.Add(je)
                        Else
                            If FormSettings.Option_大量解析プログラム = 0 Then
                                MsgBox(String.Format("DLNo{0} は、損傷レベルの制限値が正しく入力されていません。照査を省略します。", ApNo))
                            Else

                            End If
                        End If
                    End With
                End If
            Catch ex As Exception
            End Try
        Next
        Return result

    End Function

    ''' <summary>断面照査を上側引張と下側引張で分けるべきか判定する
    ''' ...2026/07/17 この機能廃止
    ''' </summary>
    ''' <param name="DLinfo">断面情報</param>
    ''' <returns>断面照査を上側引張と下側引張で分けるべき=True</returns>
    Private Function Is上下照査(DLinfo As CSNAPDBEx.IDLInfo) As Boolean

        Dim result As Boolean = False
        With DLinfo
            Select Case .iType
                Case 52, 202, 151, 300, 62, 501, 400 ' 円形 / 円環
                    result = False
                Case 54, 204 ' RC Ｔ形
                    result = True
                Case 51, 58, 55, 201 ' RC 矩形 / 中空矩形 / 小判形 / 中空小判
                    If .StlBar.DRTU <> .StlBar.DRTL Then
                        result = True
                    Else
                        result = False
                        '鉄筋位置が上下非対称の場合
                        For i As Integer = 0 To .StlBar.NS - 1
                            Dim j = .StlBar.NS - 1 - i
                            Dim Dia1 = .StlBar.Item(i).Dia
                            Dim Dia2 = .StlBar.Item(j).Dia
                            Dim Y1 = Math.Round(.StlBar.Item(i).Y, 2)
                            Dim Y2 = Math.Round(.ShpCtrl.H - .StlBar.Item(j).Y, 2)
                            Dim Num1 = .StlBar.Item(i).Num
                            Dim Num2 = .StlBar.Item(j).Num
                            If Dia1 <> Dia2 Or Y1 <> Y2 Or Num1 <> Num2 Then
                                result = True
                                Exit For
                            End If
                        Next
                    End If

                Case 61, 201 ' S鋼 矩形 / SRC 矩形
                    result = False
                    '鋼材位置が上下非対称の場合
                    For i As Integer = 0 To .StlFrame.NST - 1
                        If .StlFrame.Item(i).iDiRST = 1 Then
                            Dim Dt1 = .StlFrame.DDO
                            Dim Dt2 = .ShpCtrl.H - (.StlFrame.DDO + .StlFrame.Item(i).HS1 + .StlFrame.Item(i).HS2 + .StlFrame.Item(i).HS3)
                            Dim Bs1 = .StlFrame.Item(i).BS1
                            Dim Bs2 = .StlFrame.Item(i).BS3
                            Dim Hs1 = .StlFrame.Item(i).HS1
                            Dim Hs2 = .StlFrame.Item(i).HS3
                            If Dt1 <> Dt2 Or Bs1 <> Bs2 Or Hs1 <> Hs2 Then
                                result = True
                                Exit For
                            End If
                        End If
                    Next

            End Select
        End With
        Return result
    End Function

    ''' <summary>基礎設定毎に 入力データを集計する。</summary>
    ''' <param name="Data">入力データ</param>
    ''' <param name="CaseDB">CalculationCase のリスト</param>
    ''' <returns>基礎設定毎の CalculationFoundationリストを返す</returns>
    Private Function GetFoundationList(Data As Datas, CaseDB As List(Of CalculationCase)) As List(Of CalculationFoundation)
        Dim result As New List(Of CalculationFoundation)
        '基礎入力画面
        For Each f In Data.Foundation.基礎照査
            Try
                If f.IsEnable = True Then
                    Dim jf As New CalculationFoundation(f.FoundationType, CaseDB)
                    With jf
                        Select Case f.FoundationType
                            Case 0 '杭基礎
                                For Each dp In f.dgDispAtPoints
                                    If IsNumeric(dp.pointNo) Then
                                        .変位照査節点番号List.Add(dp.pointNo)
                                        .変位照査距離List.Add(dp._Direction)
                                End If
                                Next
                                For Each rp In f.dgReactAtPoint
                                    If IsNumeric(rp.pointNo) Then
                                        .反力照査要素番号List.Add(rp.pointNo)
                                        If rp._Direction > 0 Then
                                            .反力照査部材数List.Add(rp._Direction)
                                        Else
                                            '奥行本数が入力されていなければ 1 とする
                                            .反力照査部材数List.Add(1)
                                        End If
                                    End If
                                Next
                                '制限値
                                For i = 0 To f.dgLimitValue0.Count - 1
                                    Dim d = f.dgLimitValue0(i)
                                    If IsNumeric(d) Then .杭基礎制限値(i) = CDbl(d)
                                Next
                                ''液状化の入力がない場合液状化以外の入力で補う
                                'If Val(.杭基礎制限値(11)) = 0 Then .杭基礎制限値(11) = .杭基礎制限値(0)
                                'If Val(.杭基礎制限値(12)) = 0 Then .杭基礎制限値(12) = .杭基礎制限値(1)
                                'If Val(.杭基礎制限値(13)) = 0 Then .杭基礎制限値(13) = .杭基礎制限値(2)
                                'If Val(.杭基礎制限値(14)) = 0 Then .杭基礎制限値(14) = .杭基礎制限値(3)
                                'If Val(.杭基礎制限値(15)) = 0 Then .杭基礎制限値(15) = .杭基礎制限値(8)
                                'If Val(.杭基礎制限値(16)) = 0 Then .杭基礎制限値(16) = .杭基礎制限値(9)
                                'If Val(.杭基礎制限値(17)) = 0 Then .杭基礎制限値(17) = .杭基礎制限値(10)
                            Case 1 '直接基礎
                                If IsNumeric(f.dgDispAtPoint.pointNo) Then
                                    .変位照査節点番号List.Add(f.dgDispAtPoint.pointNo)
                                End If
                                '制限値
                                For i = 0 To f.dgLimitValue1.Count - 1
                                    Dim d = f.dgLimitValue1(i)
                                    If IsNumeric(d) Then .直接基礎制限値(i) = CDbl(d)
                                Next
                        End Select
                    End With
                    result.Add(jf)
                End If 'f.IsEnable
            Catch ex As Exception
            End Try
        Next
        Return result
    End Function

#End Region

#Region "応答値一覧表出力帳票ドキュメントの作成"

    Private Sub 応答値一覧表(InputDB As List(Of CalculationCase))

        Dim DataCount As Integer = 0 ' = InputDB.Count
        For Each DB In InputDB
            If DB.IsPrintOut = True Then DataCount += 1
        Next

        If DataCount = 0 Then Return


        CellReport1.Page.Start("応答値一覧表", "1-999")

        'あらかじめページ数を計算しておく -------------------------------------------------
        Dim iRow As New List(Of Integer)
        Dim iCol As New List(Of Integer)
        Try
            Dim maxCol As Integer = 8
            If DataCount <= maxCol Then
                CellReport1.Page.Attr.Size(PageOrientation.Portrait, System.Drawing.Printing.PaperKind.A4) 'A4縦
            Else
                CellReport1.Page.Attr.Size(PageOrientation.Landscape, System.Drawing.Printing.PaperKind.A3) 'A3横
                maxCol = 12
            End If

            '各列のアドレスを計算する
            Dim ColCount As Integer = Math.Min(Math.Ceiling(DataCount / 2), maxCol) '１つめの表の列数
            Dim tableCount = Math.Floor((DataCount - 1) / maxCol) + 1               ' テーブルの数

            Dim r As Integer = 4
            For i = 0 To tableCount - 1
                Dim c As Integer = Math.Min(DataCount - (i * maxCol), maxCol)
                For j = 0 To c - 1
                    iRow.Add(r)
                    iCol.Add(j + 4)
                Next
                r += 19
            Next

            '表をコピーする
            CellReport1.Cell("A2:C20").Copy()
            For ip = 1 To tableCount - 1
                Range(ColName(1), ip * 19 + 2).Paste()
            Next

            '行をコピーする
            CellReport1.Cell("D4:D19").Copy()
            For ip = 1 To DataCount - 1
                Range(ColName(iCol(ip)), iRow(ip), 0).Paste()
            Next
        Catch ex As Exception
            Throw ex
        End Try

        Try
            CellReport1.Cell("B1").Value = Get地盤区分Str()
            '応答値集計表の集計 ----------------------------------------------------
            Dim i As Integer = 0
            For Each DB In InputDB
                If DB.IsPrintOut = True Then
                    Dim Ad As String = ColName(iCol(i))
                    '値を代入する。
                    Call Set応答値一覧表(DB, Ad, iRow(i))
                    i += 1
                End If
            Next
        Catch ex As Exception
            Throw ex
        Finally
            CellReport1.Page.End()
        End Try
    End Sub

    Private Sub Set応答値一覧表(DB As CalculationCase, Ad As String, iRow As Integer)

        Dim oldiRow As Integer = iRow

        Try
            With DB.応答値
                Range(Ad, iRow).Value = DB.DataID + 1
                Range(Ad, iRow).Value = DB.DataName.Trim
                Range(Ad, iRow).Value = Strings.Format(DB._SNAPDB.Getαf, "0.0")
                Range(Ad, iRow).Value = Strings.Format(DB._SNAPDB.Getρm, "0.0")
                If .初期降伏震度 >= 0 Then
                    Range(Ad, iRow).Value = Strings.Format(.初期降伏震度, "0.000")
                Else
                    iRow += 1
                End If
                If .全体系折曲点震度 >= 0 Then
                    Range(Ad, iRow).Value = Strings.Format(.全体系折曲点震度, "0.000")
                Else
                    iRow += 1
                End If
                If .降伏変位 >= 0 Then
                    Range(Ad, iRow).Value = Strings.Format(.降伏変位, "0.0")
                Else
                    iRow += 1
                End If
                If IsNothing(.降伏部材情報) = False Then
                    Range(Ad, iRow).Value = .降伏部材情報.タイトル
                Else
                    iRow += 1
                End If
                If .等価固有周期 >= 0 Then
                    Range(Ad, iRow).Value = Strings.Format(.等価固有周期, "0.000")
                Else
                    iRow += 1
                End If

                'エラーのあったデータはここで抜ける
                If DB.ErrorMessage.Length > 0 Then
                    Range(Ad, iRow).Value = DB.ErrorMessage
                    iRow += 5
                    Return
                End If

                If DB._解析対象(0) = True Then
                    If .復旧性_応答塑性率 >= 0 Then
                        Range(Ad, iRow).Value = Strings.Format(.復旧性_応答塑性率, "0.00")
                    Else
                        iRow += 1
                    End If
                    Range(Ad, iRow).Value = Strings.Format(.復旧性_最大応答変位, "0.0")
                    If .最大震度step > .復旧性_最大応答step Then
                        Range(Ad, iRow).Value = Strings.Format(.復旧性_最大応答震度, "0.000")
                    Else
                        Range(Ad, iRow).Value = Strings.Format(.最大震度, "0.000")
                    End If
                Else
                    iRow += 3
                End If
                If DB._解析対象(5) = True Then
                    If .復旧性_L1震度 >= 0 Then
                        Range(Ad, iRow).Value = Strings.Format(.復旧性_L1震度, "0.000")
                    Else
                        iRow += 1
                    End If
                Else
                    iRow += 1
                End If
                If DB._解析対象(1) = True Then
                    If .安全性_応答塑性率 >= 0 Then
                        Range(Ad, iRow).Value = Strings.Format(.安全性_応答塑性率, "0.00")
                    Else
                        iRow += 1
                    End If
                    Range(Ad, iRow).Value = Strings.Format(.安全性_最大応答変位, "0.0")
                    If .最大震度step > .安全性_最大応答step Then
                        Range(Ad, iRow).Value = Strings.Format(.安全性_最大応答震度, "0.000")
                    Else
                        Range(Ad, iRow).Value = Strings.Format(.最大震度, "0.000")
                    End If
                Else
                    iRow += 3
                End If
            End With
        Catch ex As Exception
            If FormSettings.Option_大量解析プログラム = 0 Then
                MsgBox(DB.DataName + vbLf + ex.Message + vbLf + "解析を省略します。")
            Else
                Range(Ad, oldiRow + 9).Value = "エラー：" + ex.Message
                iRow = oldiRow + 16
            End If
        End Try
    End Sub

    Private Sub Set応答値エラー一覧表(DB As CalculationCase, Ad As String, iRow As Integer)
        Try
            With DB.応答値
                Range(Ad, iRow).Value = DB.DataID + 1
                Range(Ad, iRow).Value = DB.DataName.Trim
                Range(Ad, iRow).Value = Strings.Format(DB._SNAPDB.Getαf, "0.0")
                Range(Ad, iRow).Value = Strings.Format(DB._SNAPDB.Getρm, "0.0")
                Range(Ad, iRow).Value = DB.ErrorMessage
                iRow += 1
                iRow += 1
                iRow += 1
                iRow += 1
                iRow += 3
                iRow += 1
                iRow += 3
            End With
        Catch ex As Exception
            If FormSettings.Option_大量解析プログラム = 0 Then
                MsgBox(DB.DataName + vbLf + ex.Message + vbLf + "解析を省略します。")
            Else
                Throw ex
            End If
        End Try
    End Sub


    Private ReadOnly Property Get地盤区分Str As String
        Get
            Dim result As String = ""
            Try
                Dim jc As New CalculationCaseEx
                Dim is液状化 As Boolean = False
                With Input.Data.Requirement
                    jc.Set地域(._rbChiiki)
                    jc.Set地盤区分(._rbJIban)
                    jc.Set液状化区分(._rbEkijo)
                    jc._M65エリアの設定 = .cbM65Area
                    jc.震度の低減係数α = .tbAlfa
                    jc.Setスペクトル種類(._rbSpec)
                    For i = 0 To Input.DATAROWS
                        If .dgEkijoCase1(i) = -1 OrElse .dgEkijoCase2(i) = -1 Then
                            jc.SetIs液状化ケース(.dgEkijoCase1(i), .dgEkijoCase21(i), .dgEkijoCase2(i))
                            Exit For
                        End If
                    Next
                End With
                result = jc.Get地盤区分Str
            Catch ex As Exception
            End Try
            Return result
        End Get
    End Property

#End Region

#Region "断面照査集計表出力帳票ドキュメントの作成"

    Private Sub 断面照査集計表1(ElementDB As List(Of CalculationElement), Optional optionKey As Integer = 0)

        If ElementDB.Count = 0 Then Return

        'ElementDB を M-θ部材 と M-φ部材に分ける
        Dim θElementDB As New List(Of CalculationElement)
        Dim φElementDB As New List(Of CalculationElement)

        '///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        If FormSettings.Option_標準総括表は１列 = 1 Then
            Dim i As Integer = 0
            Do While i < ElementDB.Count
                Dim DB1 = ElementDB(i)
                If DB1.引張側 = 0 Then
                    i = i + 1
                Else
                    Dim DB2 = ElementDB(i + 1)
                    If DB1.MNo = DB2.MNo Then
                        If DB1.断面照査結果.せん断破壊の照査.Gami_Mdmax_Myd < DB2.断面照査結果.せん断破壊の照査.Gami_Mdmax_Myd Then
                            'DB1.断面照査結果.せん断破壊の照査.Copy曲げ照査(DB2.断面照査結果.せん断破壊の照査)
                            DB1 = DB2
                        End If
                        If DB1.断面照査結果.破壊形態の照査.安全率 < DB2.断面照査結果.破壊形態の照査.安全率 Then
                            'DB1.断面照査結果.破壊形態の照査 = DB2.断面照査結果.破壊形態の照査
                            DB1 = DB2
                        End If
                        If DB1.断面照査結果.せん断破壊の照査.Gami_Vdmax_Vyd < DB2.断面照査結果.せん断破壊の照査.Gami_Vdmax_Vyd Then
                            'DB1.断面照査結果.せん断破壊の照査.Copyせん断照査(DB2.断面照査結果.せん断破壊の照査)
                            DB1 = DB2
                        End If
                        If DB1.断面照査結果.損傷レベル.安全度 < DB2.断面照査結果.損傷レベル.安全度 Then
                            'DB1.断面照査結果.損傷レベル = DB2.断面照査結果.損傷レベル
                            DB1 = DB2
                        End If
                        ElementDB(i) = DB1
                        ElementDB.Remove(DB2)
                    End If
                    i = i + 1
                End If
            Loop
        End If
        '///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        For Each DB In ElementDB
            If DB.isθ = True Then
                θElementDB.Add(DB)
            Else
                φElementDB.Add(DB)
            End If
        Next
        Call 断面照査集計表θ(θElementDB, optionKey)
        Call 断面照査集計表φ(φElementDB, optionKey)

    End Sub

    Private Sub 断面照査集計表θ(ElementDB As List(Of CalculationElement), Optional optionKey As Integer = 0)

        Dim DataCount As Integer = ElementDB.Count
        If DataCount = 0 Then Return

        CellReport1.Page.Start("M-θ", "1-999")

        'あらかじめページ数を計算しておく -------------------------------------------------
        Dim iRow As New List(Of Integer)
        Dim iCol As New List(Of Integer)
        Try
            Dim MaxColumn As Integer = 10
            Dim pageCount As Integer = Math.Ceiling(ElementDB.Count / MaxColumn)
            '使わない表を消す
            Dim sr As Integer = 46 * pageCount
            Dim dc As Integer = 16100 - sr
            CellReport1.RowDelete(sr, dc)
            '行番号,列番号を計算しておく 
            Dim id As Integer = 1
            Dim exitflg As Boolean = False
            For ip = 1 To pageCount
                For col = 1 To MaxColumn
                    iRow.Add(46 * (ip - 1) + 2)
                    iCol.Add(col + 4)
                    If ElementDB.Count <= id Then
                        exitflg = True
                        Exit For
                    End If
                    id += 1
                Next
                If exitflg = True Then Exit For
            Next
            '行をコピーする
            CellReport1.Cell("E2:E38").Copy()
            For ip = 1 To DataCount - 1
                Range(ColName(iCol(ip)), iRow(ip), 0).Paste()
            Next
        Catch ex As Exception
            Throw ex
        End Try

        Try
            '断面照査集計表の集計 ----------------------------------------------------
            For i = 0 To ElementDB.Count - 1
                Dim DB = ElementDB(i)
                Dim Ad As String = ColName(iCol(i))
                '値を代入する。
                Call Set断面照査集計表θ(DB, Ad, iRow(i), optionKey)
            Next

        Catch ex As Exception
            Throw ex
        Finally
            CellReport1.Page.End()
        End Try

    End Sub

    Private Sub Set断面照査集計表θ(ByVal DB As CalculationElement,
                                    ByVal Ad As String,
                                    ByVal iRow As Integer,
                                    Optional optionKey As Integer = 0)

        Try
            '----------------------------------------------------------------------
            With DB
                If optionKey = 0 Then
                    Range(Ad, iRow).Value = .DLNo
                Else
                    Range(Ad, iRow).Value = String.Format("{0}({1})", .DLNo, .MNo)
                End If
                Dim s As String = .DLinfo.Title.Trim
                Range(Ad, iRow).Value = s '.DLinfo.Title.Trim
            End With
            '----------------------------------------------------------------------
            Dim is照査1 As Boolean = False
            With DB.断面照査結果
                With .破壊形態の照査
                    If .安全率 = 0 Then
                        iRow += 7
                    Else
                        Range(Ad, iRow).Value = Strings.Format(.Vdmax時の設計軸力Nd, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.設計曲げ耐力Mm, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.せん断スパンLa, "0.000")
                        Range(Ad, iRow).Value = Strings.Format(.最大設計せん断力Vdmax, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.設計せん断耐力Vud, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.安全率, "0.000")
                        Range(Ad, iRow).Value = .安全率判定
                        is照査1 = True
                    End If
                End With
                With .せん断破壊の照査
                    If .Gami_Mdmax_Myd = 0 OrElse .設計曲げ降伏耐力Myd = 0 Then
                        iRow += 5
                    Else
                        Range(Ad, iRow).Value = Strings.Format(.最大曲げモーメントMdmax, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.設計曲げ降伏耐力Myd, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.γi1, "0.00")
                        Range(Ad, iRow).Value = Strings.Format(.Gami_Mdmax_Myd, "0.000")
                        Range(Ad, iRow).Value = .MdmaxがMydに達しているか否かの判定
                        is照査1 = True
                    End If
                    If .Gami_Vdmax_Vyd = 0 OrElse .設計せん断耐力Vud = 0 Then
                        iRow += 5
                    Else
                        is照査1 = True
                        Range(Ad, iRow).Value = Strings.Format(.最大設計せん断力Vdmax, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.設計せん断耐力Vud, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.γi2, "0.00")
                        Range(Ad, iRow).Value = Strings.Format(.Gami_Vdmax_Vyd, "0.000")
                        Range(Ad, iRow).Value = .せん断耐力の照査
                    End If
                End With
                If is照査1 = False Then
                    iRow += 1
                Else
                    Range(Ad, iRow).Value = .破壊形態の推定検討結果
                End If
            End With
            '----------------------------------------------------------------------
            Dim is照査2 As Boolean = False
            With DB.断面照査結果
                With .損傷レベル
                    If Double.IsNaN(.φr) Then
                        iRow += 11
                    Else
                        is照査2 = True
                        Range(Ad, iRow).Value = Strings.Format(.Limit, "0")
                        Range(Ad, iRow).Value = Strings.Format(Math.Round(.φr, 7), "0.000000")
                        Range(Ad, iRow).Value = Strings.Format(.Nd, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(Math.Round(.φi(0), 7), "0.000000")
                        Range(Ad, iRow).Value = Strings.Format(Math.Round(.φi(1), 7), "0.000000")
                        Range(Ad, iRow).Value = Strings.Format(Math.Round(.φi(2), 7), "0.000000")
                        Range(Ad, iRow).Value = Strings.Format(.γi, "0.00")
                        Range(Ad, iRow).Value = Strings.Format(.安全率(0), "0.000")
                        Range(Ad, iRow).Value = Strings.Format(.安全率(1), "0.000")
                        Range(Ad, iRow).Value = Strings.Format(.安全率(2), "0.000")
                        Range(Ad, iRow).Value = .IsLevel
                    End If
                End With
                If is照査2 = False Then
                    iRow += 1
                Else
                    Range(Ad, iRow).Value = .損傷レベルの照査結果
                End If
            End With
            '----------------------------------------------------------------------
            Range(Ad, iRow).Value = DB.断面照査結果.総合的な照査結果

            '-備考列 -----------------------------------------------------------------
            Set備考(DB.断面照査結果, Ad, iRow)

        Catch ex As Exception
            Dim s As String = String.Format("DL番号:{0}({1}){2}{3}", DB.DLNo, DB.DLinfo.Title.Trim, vbLf, ex.Message)
            If FormSettings.Option_大量解析プログラム = 0 Then
                MsgBox(s)
            Else
                Range(Ad, iRow).Value = s
            End If
        End Try
    End Sub

    Private Sub 断面照査集計表φ(ElementDB As List(Of CalculationElement), Optional optionKey As Integer = 0)

        Dim DataCount As Integer = ElementDB.Count
        If DataCount = 0 Then Return

        CellReport1.Page.Start("M-φ", "1-999")

        'あらかじめページ数を計算しておく -------------------------------------------------
        Dim iRow As New List(Of Integer)
        Dim iCol As New List(Of Integer)
        Try
            Dim MaxColumn As Integer = 10
            Dim pageCount As Integer = Math.Ceiling(ElementDB.Count / MaxColumn)
            '使わない表を消す
            Dim sr As Integer = 46 * pageCount
            Dim dc As Integer = 16100 - sr
            CellReport1.RowDelete(sr, dc)
            '行番号,列番号を計算しておく 
            Dim id As Integer = 1
            Dim exitflg As Boolean = False
            For ip = 1 To pageCount
                For col = 1 To MaxColumn
                    iRow.Add(46 * (ip - 1) + 2)
                    iCol.Add(col + 4)
                    If ElementDB.Count <= id Then
                        exitflg = True
                        Exit For
                    End If
                    id += 1
                Next
                If exitflg = True Then Exit For
            Next
            '行をコピーする
            CellReport1.Cell("E2:E42").Copy()
            For ip = 1 To DataCount - 1
                Range(ColName(iCol(ip)), iRow(ip), 0).Paste()
            Next
        Catch ex As Exception
            Throw ex
        End Try

        Try
            '断面照査集計表の集計 ----------------------------------------------------
            For i = 0 To ElementDB.Count - 1
                Dim DB = ElementDB(i)
                Dim Ad As String = ColName(iCol(i))
                '値を代入する。
                Call Set断面照査集計表φ(DB, Ad, iRow(i), optionKey)
            Next

        Catch ex As Exception
            Throw ex
        Finally
            CellReport1.Page.End()
        End Try

    End Sub
    Private ErrorDL As Integer = -1
    Private Sub Set断面照査集計表φ(ByVal DB As CalculationElement,
                                    ByVal Ad As String,
                                    ByVal iRow As Integer,
                                    Optional optionKey As Integer = 0)

        Try
            '----------------------------------------------------------------------
            With DB
                If optionKey = 0 Then
                    Range(Ad, iRow).Value = .DLNo
                Else
                    Range(Ad, iRow).Value = String.Format("{0}({1})", .DLNo, .MNo)
                End If
                Dim s As String = .DLinfo.Title.Trim
                Range(Ad, iRow).Value = s
            End With
            '----------------------------------------------------------------------
            Dim is照査1 As Boolean = False
            With DB.断面照査結果
                With .破壊形態の照査
                    If .Mdmax_Mm = 0 Then
                        iRow += 4
                    Else
                        Range(Ad, iRow).Value = Strings.Format(.最大曲げモーメントMdmax, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.設計曲げ耐力Mm, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.Mdmax_Mm, "0.000")
                        Range(Ad, iRow).Value = .MdmaxがMmに達しているか否かの判定
                        is照査1 = True
                    End If
                    If .安全率 = 0 Then
                        iRow += 7
                    Else
                        Range(Ad, iRow).Value = Strings.Format(.最大設計せん断力Vdmax, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.Vdmax時の曲げモーメントMd, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.Vdmax時の設計軸力Nd, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.設計せん断耐力Vud, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.α, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.安全率, "0.000")
                        Range(Ad, iRow).Value = .安全率判定
                        is照査1 = True
                    End If
                End With
                With .せん断破壊の照査
                    If .Gami_Mdmax_Myd = 0 OrElse .設計曲げ降伏耐力Myd = 0 Then
                        iRow += 5
                    Else
                        Range(Ad, iRow).Value = Strings.Format(.最大曲げモーメントMdmax, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.設計曲げ降伏耐力Myd, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.γi1, "0.00")
                        Range(Ad, iRow).Value = Strings.Format(.Gami_Mdmax_Myd, "0.000")
                        Range(Ad, iRow).Value = .MdmaxがMydに達しているか否かの判定
                        is照査1 = True
                    End If
                    If .Gami_Vdmax_Vyd = 0 OrElse .設計せん断耐力Vud = 0 Then
                        iRow += 5
                    Else
                        is照査1 = True
                        Range(Ad, iRow).Value = Strings.Format(.最大設計せん断力Vdmax, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.設計せん断耐力Vud, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.γi2, "0.00")
                        Range(Ad, iRow).Value = Strings.Format(.Gami_Vdmax_Vyd, "0.000")
                        Range(Ad, iRow).Value = .せん断耐力の照査
                    End If
                End With
                If is照査1 = False Then
                    iRow += 1
                Else
                    Range(Ad, iRow).Value = .破壊形態の推定検討結果
                End If
            End With
            '----------------------------------------------------------------------
            Dim is照査2 As Boolean = False
            With DB.断面照査結果
                With .損傷レベル
                    If Double.IsNaN(.φr) Then
                        iRow += 11
                    Else
                        is照査2 = True
                        Range(Ad, iRow).Value = Strings.Format(.Limit, "0")
                        Range(Ad, iRow).Value = Strings.Format(Math.Round(.φr, 7), "0.000000")
                        Range(Ad, iRow).Value = Strings.Format(.Nd, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(Math.Round(.φi(0), 7), "0.000000")
                        Range(Ad, iRow).Value = Strings.Format(Math.Round(.φi(1), 7), "0.000000")
                        Range(Ad, iRow).Value = Strings.Format(Math.Round(.φi(2), 7), "0.000000")
                        Range(Ad, iRow).Value = Strings.Format(.γi, "0.00")
                        Range(Ad, iRow).Value = Strings.Format(.安全率(0), "0.000")
                        Range(Ad, iRow).Value = Strings.Format(.安全率(1), "0.000")
                        Range(Ad, iRow).Value = Strings.Format(.安全率(2), "0.000")
                        Range(Ad, iRow).Value = .IsLevel
                    End If
                End With
                If is照査2 = False Then
                    iRow += 1
                Else
                    Range(Ad, iRow).Value = .損傷レベルの照査結果
                End If
            End With
            '----------------------------------------------------------------------
            Dim 総合的な照査結果 As String = "OK"
            With DB.断面照査結果
                If InStr(.破壊形態の推定検討結果, "NG") Then 総合的な照査結果 = "NG"
                If InStr(.損傷レベルの照査結果, "NG") Then 総合的な照査結果 = "NG"
                Range(Ad, iRow).Value = 総合的な照査結果
            End With

            '-備考列 -----------------------------------------------------------------
            Set備考(DB.断面照査結果, Ad, iRow)


        Catch ex As Exception
            Dim s As String
            If FormSettings.Option_大量解析プログラム = 0 Then
                s = String.Format("DL番号:{0}({1}){2}{3}", DB.DLNo, DB.DLinfo.Title.Trim, vbLf, ex.Message)
            Else
                '改行しない
                s = String.Format("DL番号:{0}({1}){2}", DB.DLNo, DB.DLinfo.Title.Trim, ex.Message)
            End If
            If ErrorDL <> DB.DLNo Then
                If FormSettings.Option_大量解析プログラム = 0 Then
                    MsgBox(s)
                Else
                    Range(Ad, iRow).Value = s
                End If
                ErrorDL = DB.DLNo
            End If
        End Try
    End Sub

    Private Sub Set備考(断面照査結果 As cls断面照査, Ad As String, iRow As Integer)
        With 断面照査結果
            Dim 備考Row = New List(Of String) ' 同じコメントを印字しないようにする
            For Each s In .損傷レベル.備考
                If Len(Trim(s)) > 0 Then
                    If 備考Row.Contains(s) Then
                        Continue For
                    End If
                    Range(Ad, iRow).Value = s
                    備考Row.Add(s)
                End If
            Next
            If Len(Trim(.破壊形態の照査.備考)) > 0 Then
                If Not 備考Row.Contains(.破壊形態の照査.備考) Then
                    Range(Ad, iRow).Value = .破壊形態の照査.備考
                    備考Row.Add(.破壊形態の照査.備考)
                End If
            End If
            If Len(Trim(.せん断破壊の照査.備考)) > 0 Then
                If Not 備考Row.Contains(.せん断破壊の照査.備考) Then
                    Range(Ad, iRow).Value = .せん断破壊の照査.備考
                    備考Row.Add(.せん断破壊の照査.備考)
                End If
            End If
        End With
    End Sub

    Private Sub 断面照査集計表2(ElementDB As List(Of CalculationElement), Optional optionKey As Integer = 0)

        Dim DataCount As Integer = ElementDB.Count
        If DataCount = 0 Then Return

        CellReport1.Page.Start("設計総括表", "1-999")

        'あらかじめページ数を計算しておく -------------------------------------------------
        Dim iRow As New List(Of Integer)
        Dim iCol As New List(Of Integer)
        Try
            Dim MaxColumn As Integer = 9
            Dim pageCount As Integer = Math.Ceiling(ElementDB.Count / MaxColumn)
            '使わない表を消す
            Dim sr As Integer = 44 * pageCount
            Dim dc As Integer = 17120 - sr
            CellReport1.RowDelete(sr, dc)
            '行番号,列番号を計算しておく 
            Dim id As Integer = 1
            Dim exitflg As Boolean = False
            For ip = 1 To pageCount
                For col = 1 To MaxColumn
                    iRow.Add(44 * (ip - 1) + 2)
                    iCol.Add(col + 6)
                    If ElementDB.Count <= id Then
                        exitflg = True
                        Exit For
                    End If
                    id += 1
                Next
                If exitflg = True Then Exit For
            Next
            '行をコピーする
            CellReport1.Cell("G2:G44").Copy()
            For ip = 1 To DataCount - 1
                Range(ColName(iCol(ip)), iRow(ip), 0).Paste()
            Next
        Catch ex As Exception
            Throw ex
        End Try

        Try
            '断面照査集計表の集計 ----------------------------------------------------
            For i = 0 To ElementDB.Count - 1
                Dim DB = ElementDB(i)
                Dim Ad As String = ColName(iCol(i))
                '値を代入する。
                Call Set断面照査集計表2(DB, Ad, iRow(i), optionKey)
            Next

        Catch ex As Exception
            Throw ex
        Finally
            CellReport1.Page.End()
        End Try
    End Sub

    Private Sub Set断面照査集計表2(DB As CalculationElement, Ad As String, iRow As Integer, Optional optionKey As Integer = 0)

        Try
            With DB
                If optionKey = 0 Then
                    Range(Ad, iRow).Value = .DLNo
                Else
                    Range(Ad, iRow).Value = String.Format("{0}({1})", .DLNo, .MNo)
                End If

                Range(Ad, iRow).Value = .DLinfo.Title.Trim
                Call Set断面形状(Ad, iRow + 1, .DLinfo)
                Dim 鉄筋List = Get断面鉄筋(iRow + 4, .DLinfo, .引張側)
                For Each Ast In 鉄筋List
                    Range(Ad, Ast.Key, 0).Value = Ast.Value
                Next
            End With
            iRow += 11
            Range(Ad, iRow).Value = "1.0" 'γi
            '----------------------------------------------------------------------
            With DB.断面照査結果
                With .損傷レベル
                    If Double.IsNaN(.φr) Then
                        iRow += 7
                    Else
                        Range(Ad, iRow).Value = .決定ケース
                        Range(Ad, iRow).Value = .決定ステップ
                        Range(Ad, iRow).Value = Strings.Format(.Nd, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(Math.Round(.φr, 7), "0.000000")
                        If .φi.Count >= .Limit Then
                            Range(Ad, iRow).Value = Strings.Format(Math.Round(.φi(.Limit - 1), 7), "0.000000")
                            Range(Ad, iRow).Value = Strings.Format(.安全率(.Limit - 1), "0.000") + 符号(.安全率(.Limit - 1))
                        Else
                            '損傷レベル4
                            Range(Ad, iRow).Value = Strings.Format(Math.Round(.φi.Last, 7), "0.000000")
                            Range(Ad, iRow).Value = "0.000" + 符号(0)
                        End If

                        Range(Ad, iRow).Value = String.Format("{0} / {1}", .IsLevel, .Limit)
                    End If
                End With
            End With
            '----------------------------------------------------------------------
            With DB.断面照査結果
                With .破壊形態の照査
                    If .安全率 = 0 Then
                        iRow += 7
                    Else
                        Range(Ad, iRow).Value = .決定ケース
                        Range(Ad, iRow).Value = .決定ステップ
                        Range(Ad, iRow).Value = Strings.Format(.最大設計せん断力Vdmax, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.設計せん断耐力Vud, "0.0")
                        If DB.isθ = True Then
                            iRow += 1
                        Else
                            Range(Ad, iRow).Value = Strings.Format(.α, "0.0")
                        End If
                        Range(Ad, iRow).Value = Strings.Format(.安全率, "0.000") + 符号(.安全率)
                        Range(Ad, iRow).Value = .安全率判定
                    End If
                End With
            End With
            '----------------------------------------------------------------------
            With DB.断面照査結果
                With .せん断破壊の照査
                    If .Gami_Mdmax_Myd = 0 OrElse .設計曲げ降伏耐力Myd = 0 Then
                        iRow += 6
                    Else
                        Range(Ad, iRow).Value = .決定ケースM
                        Range(Ad, iRow).Value = .決定ステップM
                        Range(Ad, iRow).Value = Strings.Format(.最大曲げモーメントMdmax, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.応答軸力N, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.設計曲げ降伏耐力Myd, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.Gami_Mdmax_Myd, "0.000") + 符号(.Gami_Mdmax_Myd)
                    End If
                    If .Gami_Vdmax_Vyd = 0 OrElse .設計せん断耐力Vud = 0 Then
                        iRow += 5
                    Else
                        Range(Ad, iRow).Value = .決定ケースV
                        Range(Ad, iRow).Value = .決定ステップV
                        Range(Ad, iRow).Value = Strings.Format(.最大設計せん断力Vdmax, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.設計せん断耐力Vud, "0.0")
                        Range(Ad, iRow).Value = Strings.Format(.Gami_Vdmax_Vyd, "0.000") + 符号(.Gami_Vdmax_Vyd)
                    End If
                End With
                '----------------------------------------------------------------------
                With .L1地震動
                    If .安全度 = 0 Then
                        iRow += 4
                    Else
                        Range(Ad, iRow).Value = Strings.Format(.L1設計震度, "0.000")
                        Range(Ad, iRow).Value = Strings.Format(.降伏震度, "0.000")
                        Range(Ad, iRow).Value = Strings.Format(.安全度, "0.000") + 符号(.安全度)
                        Range(Ad, iRow).Value = .判定
                    End If
                End With
            End With
        Catch ex As Exception
            If ErrorDL <> DB.DLNo Then
                Dim s As String = String.Format("DL番号:{0}({1})", DB.DLNo, DB.DLinfo.Title) + vbLf + ex.Message
                If FormSettings.Option_大量解析プログラム = 0 Then
                    MsgBox(s)
                Else
                    Range(Ad, iRow).Value = s
                End If
                ErrorDL = DB.DLNo
            End If
        End Try
    End Sub

    Private Function 符号(安全率 As Double, Optional Limit As Double = 1) As String
        If 安全率 < Limit Then
            Return String.Format("＜{0:F2}", Limit)
        Else
            Return String.Format("≧{0:F2}", Limit)
        End If
    End Function

#Region "断面形状図作成関数"

    '断面形状
    Private Sub Set断面形状(col As String, ByVal row As Integer, DLINfo As CSNAPDBEx.IDLInfo)

        With DLINfo

            '形状 ------------------------------------------------------------
            Select Case .iType
                Case 51, 58 ' 矩形 / 中空矩形
                    If .ShpCtrl.B = .ShpCtrl.H Then
                        If DLINfo.ShpCtrl.T1 > 0 Then
                            Call FormSquare(col, row, .ShpCtrl.B, 1)
                        Else
                            Call FormSquare(col, row, .ShpCtrl.B, 0)
                        End If
                    ElseIf .ShpCtrl.B < .ShpCtrl.H Then
                        If DLINfo.ShpCtrl.T1 > 0 Then
                            Call FormVRect(col, row, .ShpCtrl.B, .ShpCtrl.H, 1)
                        Else
                            Call FormVRect(col, row, .ShpCtrl.B, .ShpCtrl.H, 0)
                        End If
                    Else
                        If DLINfo.ShpCtrl.T1 > 0 Then
                            Call FormHRect(col, row, .ShpCtrl.B, .ShpCtrl.H, 1)
                        Else
                            Call FormHRect(col, row, .ShpCtrl.B, .ShpCtrl.H, 0)
                        End If
                    End If
                Case 61 ' 鋼矩形
                    Dim H = .ShpCtrl.Bw + .ShpCtrl.BFT + .ShpCtrl.BFC
                    Dim B = .ShpCtrl.BFT
                    If B = H Then
                        Call FormSquare(col, row, B, 3)
                    ElseIf B < H Then
                        Call FormVRect(col, row, B, H, 3)
                    Else
                        Call FormHRect(col, row, B, H, 3)
                    End If
                Case 201 '鉄骨鉄筋
                    If .ShpCtrl.B = .ShpCtrl.H Then
                        Call FormSquare(col, row, .ShpCtrl.B, 2)
                    ElseIf .ShpCtrl.B < .ShpCtrl.H Then
                        Call FormVRect(col, row, .ShpCtrl.B, .ShpCtrl.H, 2)
                    Else
                        Call FormHRect(col, row, .ShpCtrl.B, .ShpCtrl.H, 2)
                    End If
                Case 52, 151, 501 ' 円形 / 円環
                    If DLINfo.ShpCtrl.R2 > 0 Then
                        Call FormCircle(col, row, .ShpCtrl.R * 2, 1)
                    Else
                        Call FormCircle(col, row, .ShpCtrl.R * 2, 0)
                    End If
                Case 400 ' 円形 / 円環
                    Call FormCircle(col, row, .ShpCtrl.D * 2, 1)
                Case 202 ' SRC 円形
                    Call FormCircle(col, row, .ShpCtrl.R, 2)
                Case 300 ' 鋼管
                    Call FormCircle(col, row, .ShpCtrl.R * 2, 2)
                Case 62 ' 鋼管
                    Call FormCircle(col, row, .ShpCtrl.D, 2)
                Case 54 ' Ｔ形
                    If .ShpCtrl.iD = 0 Then
                        Call FormT(col, row, .ShpCtrl.Bw, .ShpCtrl.H, 0)
                    Else
                        Call FormUDT(col, row, .ShpCtrl.Bw, .ShpCtrl.H, 0)
                    End If
                Case 204 ' Ｔ形 鉄骨鉄筋
                    If .ShpCtrl.iD = 0 Then
                        Call FormT(col, row, .ShpCtrl.Bw, .ShpCtrl.H, 1)
                    Else
                        Call FormUDT(col, row, .ShpCtrl.Bw, .ShpCtrl.H, 1)
                    End If
                Case 55 ' RC 小判形 / 中空小判
                    If .ShpCtrl.NK = 1 Then
                        If DLINfo.ShpCtrl.T3 > 0 Then
                            '横長中空
                            Call FormOval(col, row, .ShpCtrl.D2, .ShpCtrl.D1, 1)
                        Else
                            '横長
                            Call FormOval(col, row, .ShpCtrl.D2, .ShpCtrl.D1, 0)
                        End If
                    Else
                        If DLINfo.ShpCtrl.T3 > 0 Then
                            '縦長中空()
                            Call FormOval(col, row, .ShpCtrl.D1, .ShpCtrl.D2, 3)
                        Else
                            '縦長
                            Call FormOval(col, row, .ShpCtrl.D1, .ShpCtrl.D2, 2)
                        End If
                    End If
            End Select
        End With

    End Sub

    Private Function Get断面鉄筋(ByVal irow As Integer, DLINfo As CSNAPDBEx.IDLInfo, 引張側 As Integer) As Dictionary(Of Integer, String)

        Dim result As New Dictionary(Of Integer, String)
        Try
            With DLINfo
                '鉄筋 ------------------------------------------------------------
                Select Case .iType
                    Case 51, 54, 58 ' RC 矩形 / 中空矩形 / 円形 / 円環
                        '軸方向鉄筋
                        If 引張側 = 1 Then
                            '下側引張
                            result.Add(irow - 4, "下側引張")
                            Dim row1 As Integer = irow
                            Dim st As Integer = .StlBar.NS - 1
                            Dim ed As Integer = Math.Max(st - 2, .StlBar.NS - .StlBar.DRTL)
                            For i = st To ed Step -1
                                Dim Dia1 = .StlBar.Item(i).Dia
                                Dim Num1 = .StlBar.Item(i).Num
                                result.Add(row1, String.Format("D{0}-{1}本", Dia1, Num1))
                                row1 += 1
                            Next
                        Else
                            '上側引張
                            If 引張側 = -1 Then result.Add(irow - 4, "上側引張")
                            Dim row1 As Integer = irow
                            Dim st As Integer = 0
                            Dim ed As Integer = Math.Min(2, .StlBar.DRTU - 1)
                            For i = st To ed
                                Dim Dia1 = .StlBar.Item(i).Dia
                                Dim Num1 = .StlBar.Item(i).Num
                                result.Add(row1, String.Format("D{0}-{1}本", Dia1, Num1))
                                row1 += 1
                            Next
                        End If
                        '材料
                        Dim row2 As Integer = irow + 3
                        Dim SD1 = .StlBar.SDL
                        result.Add(row2, String.Format("SD{0}", SD1))
                        '帯鉄筋
                        Dim row3 As Integer = irow + 4
                        Dim SD2 = .StlBar.SDW
                        Dim Dia2 = .StlBar.DiwR
                        Dim Num2 = .StlBar.HRW
                        Dim Ss = .StlBar.SW
                        result.Add(row3, String.Format("D{0}-{1}本@{2}", Dia2, Num2, Ss))
                        row3 += 1
                        row3 += 1
                        result.Add(row3, String.Format("SD{0}", SD2))

                    Case 52 ' RC 円形 / 円環 
                        '軸方向鉄筋
                        Dim row1 As Integer = irow
                        Dim st As Integer = 0
                        Dim ed As Integer = Math.Min(2, .StlBar.NS - 1)
                        For i = st To ed
                            Dim Dia1 = .StlBar.Item(i).Dia
                            Dim Num1 = .StlBar.Item(i).Num
                            result.Add(row1, String.Format("D{0}-{1}本", Dia1, Num1))
                            row1 += 1
                        Next
                        '材料
                        Dim row2 As Integer = irow + 3
                        Dim SD1 = .StlBar.SDL
                        result.Add(row2, String.Format("SD{0}", SD1))
                        '帯鉄筋
                        Dim row3 As Integer = irow + 4
                        Dim SD2 = .StlBar.SDW
                        Dim Dia2 = .StlBar.DiwR
                        Dim Num2 = .StlBar.HRW
                        Dim Ss = .StlBar.SW
                        result.Add(row3, String.Format("D{0}-{1}本@{2}", Dia2, Num2, Ss))
                        row3 += 1
                        row3 += 1
                        result.Add(row3, String.Format("SD{0}", SD2))

                    Case 501 ' RC 円形 / 円環 / 鋼管杭接合部材
                        '軸方向鉄筋
                        Dim row1 As Integer = irow
                        Dim st As Integer = 0
                        Dim ed As Integer = Math.Min(2, .StlBar.NS - 1)
                        For i = st To ed
                            Dim Dia1 = .StlBar.Item(i).Dia
                            Dim Num1 = .StlBar.Item(i).Num
                            result.Add(row1, String.Format("D{0}-{1}本", Dia1, Num1))
                            row1 += 1
                        Next
                        '材料
                        Dim row2 As Integer = irow + 3
                        Dim SD1 = .StlBar.SDL
                        result.Add(row2, String.Format("SD{0}", SD1))
                        '帯鉄筋
                        Dim row3 As Integer = irow + 4
                        result.Add(row3, String.Format("t={0}mm", .ShpCtrl.t))
                        row3 += 1
                        row3 += 1
                        result.Add(row3, String.Format("SYK{0}", .StlBar.FSY))

                    Case 55 ' RC 小判形 / 中空小判
                        Dim row1 As Integer = irow
                        Dim Dia1 As Single = 0
                        Dim Num1 As Single = 0
                        Select Case .ShpCtrl.NK
                            Case 1
                                Dia1 = .StlBar.Item(1).Dia
                                Num1 = .StlBar.Item(1).Num
                            Case 2
                                Dia1 = .StlBar.Item(0).Dia
                                Num1 = .StlBar.Item(0).Num
                        End Select
                        result.Add(row1, String.Format("D{0}-{1}本", Dia1, Num1))
                        '材料
                        Dim row2 As Integer = irow + 3
                        Dim SD1 = .StlBar.SDL
                        result.Add(row2, String.Format("SD{0}", SD1))
                        '帯鉄筋
                        Dim row3 As Integer = irow + 4
                        Dim SD2 = .StlBar.SDW
                        Dim Dia2 = .StlBar.DiwR
                        Dim Num2 = .StlBar.HRW
                        Dim Ss = .StlBar.SW
                        result.Add(row3, String.Format("D{0}-{1}本@{2}", Dia2, Num2, Ss))
                        row3 += 1
                        row3 += 1
                        result.Add(row3, String.Format("SD{0}", SD2))

                    Case 201, 204 ' SRC 矩形 / SRC Ｔ形
                        '軸方向鉄筋
                        If 引張側 = 1 Then
                            '下側引張
                            result.Add(irow - 4, "下側引張")
                            Dim row1 As Integer = irow
                            '鋼材
                            result.Add(row1, String.Format("ﾌﾗﾝｼﾞ t={0}", .StlFrame.Item(0).HS3))
                            row1 += 1
                            result.Add(row1, String.Format("(SYK{0})", .StlFrame.SMR))
                            row1 += 1
                            '鉄筋
                            Dim st As Integer = .StlBar.NS - 1
                            Dim Dia1 = .StlBar.Item(st).Dia
                            Dim Num1 = .StlBar.Item(st).Num
                            result.Add(row1, String.Format("D{0}-{1}本", Dia1, Num1))
                            row1 += 1
                            result.Add(row1, String.Format("(SD{0})", .StlBar.SDL))
                        Else
                            '上側引張
                            If 引張側 = -1 Then result.Add(irow - 4, "上側引張")
                            Dim row1 As Integer = irow
                            '鋼材
                            result.Add(row1, String.Format("ﾌﾗﾝｼﾞ t={0}", .StlFrame.Item(0).HS1))
                            row1 += 1
                            result.Add(row1, String.Format("(SYK{0})", .StlFrame.SMR))
                            row1 += 1
                            '鉄筋
                            Dim st As Integer = 0
                            Dim Dia1 = .StlBar.Item(st).Dia
                            Dim Num1 = .StlBar.Item(st).Num
                            result.Add(row1, String.Format("D{0}-{1}本", Dia1, Num1))
                            row1 += 1
                            result.Add(row1, String.Format("(SD{0})", .StlBar.SDL))
                        End If
                        '帯鉄筋
                        Dim row3 As Integer = irow + 4
                        result.Add(row3, String.Format("ｳｪﾌﾞ t={0}", .StlFrame.Item(0).BS2))
                        row3 += 1
                        result.Add(row3, String.Format("D{0}-{1}本@{2}", .StlBar.DiwR, .StlBar.HRW, .StlBar.SW))
                        row3 += 1
                        result.Add(row3, String.Format("SD{0}", .StlBar.SDW))

                    Case 202 ' SRC 円形
                        Dim row1 As Integer = irow
                        '鋼材
                        result.Add(row1, String.Format("ﾌﾗﾝｼﾞ t={0}", .StlFrame.Item(0).HS3))
                        row1 += 1
                        result.Add(row1, String.Format("(SYK{0})", .StlFrame.SMR))
                        row1 += 1
                        '鉄筋
                        Dim st As Integer = 0
                        Dim Dia1 = .StlBar.Item(st).Dia
                        Dim Num1 = .StlBar.Item(st).Num
                        result.Add(row1, String.Format("D{0}-{1}本", Dia1, Num1))
                        row1 += 1
                        result.Add(row1, String.Format("(SD{0})", .StlBar.SDL))
                        '帯鉄筋
                        Dim row3 As Integer = irow + 4
                        result.Add(row3, String.Format("ｳｪﾌﾞ t={0}", .StlFrame.Item(0).BS2))
                        row3 += 1
                        result.Add(row3, String.Format("D{0}-{1}本@{2}", .StlBar.DiwR, .StlBar.HRW, .StlBar.SW))
                        row3 += 1
                        result.Add(row3, String.Format("SD{0}", .StlBar.SDW))


                    Case 151 ' PHC杭()
                        '軸方向鉄筋
                        Dim row1 As Integer
                        row1 = irow
                        Dim st As Integer
                        Dim ed As Integer
                        st = 0
                        ed = Math.Min(1, .PCBar.Ns - 1)
                        For i = st To ed
                            Dim Dia1 = .PCBar.item(i).Y
                            Dim Num1 = .PCBar.item(i).Num
                            result.Add(row1, String.Format("φ{0}-{1}本", Dia1, Num1))
                            row1 += 1
                        Next
                        '材料
                        Dim row2 As Integer
                        Dim SD1 As Integer
                        SD1 = .PCBar.FPYK
                        If .StlBar.NS > 0 Then
                            row2 = irow + 1
                            result.Add(row2, String.Format("(SD{0})", SD1))
                        Else
                            row2 = irow + 3
                            result.Add(row2, String.Format("SD{0}", SD1))
                        End If
                        '鉄筋
                        row1 = irow + 2
                        st = 0
                        ed = Math.Min(1, .StlBar.NS - 1)
                        For i = st To ed
                            Dim Dia1 = .StlBar.Item(i).Dia
                            Dim Num1 = .StlBar.Item(i).Num
                            result.Add(row1, String.Format("D{0}-{1}本", Dia1, Num1))
                            row1 += 1
                        Next
                        If .StlBar.NS > 0 Then
                            '材料
                            row2 = irow + 3
                            SD1 = .StlBar.SDL
                            result.Add(row2, String.Format("SD{0}", SD1))
                        End If

                        '帯鉄筋
                        Dim row3 As Integer = irow + 4
                        Dim SD2 = .PCBar.FWYK
                        Dim Dia2 = .StlBar.DiwR
                        Dim Num2 = .StlBar.HRW
                        Dim Ss = .StlBar.SW
                        result.Add(row3, String.Format("D{0}-{1}本@{2}", Dia2, Num2, Ss))
                        row3 += 1
                        row3 += 1
                        result.Add(row3, String.Format("SD{0}", SD2))


                    Case 61 ' S鋼 矩形
                        Dim row1 As Integer = irow
                        If 引張側 = 1 Then
                            '下側引張
                            result.Add(irow - 4, "下側引張")
                            result.Add(row1, String.Format("ﾌﾗﾝｼﾞ t={0}", .ShpCtrl.EBFT))
                        Else
                            '上側引張
                            If 引張側 = -1 Then result.Add(irow - 4, "上側引張")
                            result.Add(row1, String.Format("ﾌﾗﾝｼﾞ t={0}", .ShpCtrl.EBFC))
                        End If
                        row1 += 1
                        row1 += 1
                        row1 += 1
                        result.Add(row1, String.Format("SYK{0}", .StlBar.FSY))
                        row1 += 1
                        result.Add(row1, String.Format("ｳｪﾌﾞ t={0}", .ShpCtrl.TW))
                        row1 += 1
                        row1 += 1
                        result.Add(row1, String.Format("SYK{0}", .StlBar.FSY))

                    Case 62, 300, 400 'S鋼 円形 / コンクリート充填鋼管CFT / SC杭
                        result.Add(irow, String.Format("t={0}mm", .ShpCtrl.t))
                        result.Add(irow + 3, String.Format("SYK{0}", .StlBar.FSY))

                End Select
            End With
        Catch ex As Exception
        End Try
        Return result
    End Function

    'T字テンプレート
    Private Sub FormT(col As String, row As Integer, width As Integer, Height As Integer, type As Integer)

        Dim x() As Integer = {5, 9, 10, 13, 14, 21, 25}
        Dim y() As Integer = {1, 8, 12, 13, 19}

        With Range(col, row, 0).Drawing
            '形状
            .ArrowTypeS = 0
            .ArrowTypeE = 0
            .LineWeight = 1.5
            .AddLine(ShapeType.Line, x(2), y(0), x(6), y(0))
            .AddLine(ShapeType.Line, x(2), y(0), x(2), y(1))
            .AddLine(ShapeType.Line, x(6), y(0), x(6), y(1))
            .AddLine(ShapeType.Line, x(2), y(1), x(4), y(1))
            .AddLine(ShapeType.Line, x(5), y(1), x(6), y(1))
            .AddLine(ShapeType.Line, x(4), y(1), x(4), y(2))
            .AddLine(ShapeType.Line, x(5), y(1), x(5), y(2))
            .AddLine(ShapeType.Line, x(4), y(2), x(5), y(2))
            If type = 1 Then
                .LineWeight = 0.7
                .AddLine(ShapeType.Line, x(4) + 1, y(0) + 2, x(5) - 1, y(0) + 2)
                .AddLine(ShapeType.Line, x(4) + 1, y(0) + 3, x(5) - 1, y(0) + 3)
                .AddLine(ShapeType.Line, x(4) + 1, y(0) + 2, x(4) + 1, y(0) + 3)
                .AddLine(ShapeType.Line, x(5) - 1, y(0) + 2, x(5) - 1, y(0) + 3)

                .AddLine(ShapeType.Line, x(4) + 1, y(0) + 6, x(5) - 1, y(0) + 6)
                .AddLine(ShapeType.Line, x(4) + 1, y(0) + 7, x(5) - 1, y(0) + 7)
                .AddLine(ShapeType.Line, x(4) + 1, y(0) + 6, x(4) + 1, y(0) + 7)
                .AddLine(ShapeType.Line, x(5) - 1, y(0) + 6, x(5) - 1, y(0) + 7)

                .AddLine(ShapeType.Line, x(4) + 3, y(0) + 3, x(4) + 3, y(0) + 6)
                .AddLine(ShapeType.Line, x(5) - 3, y(0) + 3, x(5) - 3, y(0) + 6)
            End If
            '寸法線 高さ
            .LineWeight = 0.5
            .ArrowTypeS = 0
            .ArrowTypeE = 0
            .AddLine(ShapeType.Line, x(0), y(0), x(1), y(0))
            .AddLine(ShapeType.Line, x(0), y(2), x(3), y(2))
            .ArrowTypeS = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
            .ArrowTypeE = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
            .AddLine(ShapeType.Line, x(0), y(0), x(0), y(2))
            '寸法線 幅
            .LineWeight = 0.5
            .ArrowTypeS = 0
            .ArrowTypeE = 0
            .AddLine(ShapeType.Line, x(4), y(3), x(4), y(4))
            .AddLine(ShapeType.Line, x(5), y(3), x(5), y(4))
            .ArrowTypeS = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
            .ArrowTypeE = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
            .AddLine(ShapeType.Line, x(4), y(4), x(5), y(4))
        End With
        '寸法値
        Range(col, row).Str = Height
        '幅は中央に表示するため末尾に空白" " を入れた文字列を代入する。
        Dim wStr As String = width.ToString()
        wStr = wStr + Strings.Space(Math.Max(0, 6 - Len(wStr)))
        Range(col, row).Str = wStr

    End Sub

    '逆T字テンプレート
    Private Sub FormUDT(col As String, row As Integer, width As Integer, Height As Integer, type As Integer)
        Dim x() As Integer = {5, 9, 10, 13, 14, 21, 25}
        Dim y() As Integer = {1, 5, 12, 13, 19}

        With Range(col, row, 0).Drawing
            '形状
            .ArrowTypeS = 0
            .ArrowTypeE = 0
            .LineWeight = 1.5
            .AddLine(ShapeType.Line, x(4), y(0), x(5), y(0))
            .AddLine(ShapeType.Line, x(4), y(0), x(4), y(1))
            .AddLine(ShapeType.Line, x(5), y(0), x(5), y(1))
            .AddLine(ShapeType.Line, x(2), y(1), x(4), y(1))
            .AddLine(ShapeType.Line, x(5), y(1), x(6), y(1))
            .AddLine(ShapeType.Line, x(2), y(1), x(2), y(2))
            .AddLine(ShapeType.Line, x(6), y(1), x(6), y(2))
            .AddLine(ShapeType.Line, x(2), y(2), x(6), y(2))

            If type = 1 Then
                .LineWeight = 0.7
                .AddLine(ShapeType.Line, x(4) + 1, y(0) + 4.5, x(5) - 1, y(0) + 4.5)
                .AddLine(ShapeType.Line, x(4) + 1, y(0) + 5.5, x(5) - 1, y(0) + 5.5)
                .AddLine(ShapeType.Line, x(4) + 1, y(0) + 4.5, x(4) + 1, y(0) + 5.5)
                .AddLine(ShapeType.Line, x(5) - 1, y(0) + 4.5, x(5) - 1, y(0) + 5.5)

                .AddLine(ShapeType.Line, x(4) + 1, y(0) + 8.5, x(5) - 1, y(0) + 8.5)
                .AddLine(ShapeType.Line, x(4) + 1, y(0) + 9.5, x(5) - 1, y(0) + 9.5)
                .AddLine(ShapeType.Line, x(4) + 1, y(0) + 8.5, x(4) + 1, y(0) + 9.5)
                .AddLine(ShapeType.Line, x(5) - 1, y(0) + 8.5, x(5) - 1, y(0) + 9.5)

                .AddLine(ShapeType.Line, x(4) + 3, y(0) + 5.5, x(4) + 3, y(0) + 8.5)
                .AddLine(ShapeType.Line, x(5) - 3, y(0) + 5.5, x(5) - 3, y(0) + 8.5)
            End If

            '寸法線 高さ
            .LineWeight = 0.5
            .ArrowTypeS = 0
            .ArrowTypeE = 0
            .AddLine(ShapeType.Line, x(0), y(0), x(3), y(0))
            .AddLine(ShapeType.Line, x(0), y(2), x(1), y(2))
            .ArrowTypeS = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
            .ArrowTypeE = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
            .AddLine(ShapeType.Line, x(0), y(0), x(0), y(2))
            '寸法線 幅
            .LineWeight = 0.5
            .ArrowTypeS = 0
            .ArrowTypeE = 0
            .AddLine(ShapeType.Line, x(4), y(3), x(4), y(4))
            .AddLine(ShapeType.Line, x(5), y(3), x(5), y(4))
            .ArrowTypeS = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
            .ArrowTypeE = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
            .AddLine(ShapeType.Line, x(4), y(4), x(5), y(4))
        End With
        '寸法値
        Range(col, row).Str = Height
        '幅は中央に表示するため末尾に空白" " を入れた文字列を代入する。
        Dim wStr As String = width.ToString()
        wStr = wStr + Strings.Space(Math.Max(0, 6 - Len(wStr)))
        Range(col, row).Str = wStr

    End Sub

    '長方形縦長テンプレート
    Private Sub FormVRect(col As String, row As Integer, width As Integer, Height As Integer, type As Integer)
        '形状　type = 0 普通形状、type = 1 中空形状、type = 2 鉄骨鉄筋、type = 3 鋼矩形
        Dim x() As Integer = {5, 12, 13, 14, 17.5, 21, 22}
        Dim y() As Integer = {1, 2, 7.5, 12, 14, 15, 19}
        Call FormRect(x, y, col, row, width, Height, type)

    End Sub

    '長方形横長テンプレート
    Private Sub FormHRect(col As String, row As Integer, width As Integer, Height As Integer, type As Integer)
        '形状　type = 0 普通形状、type = 1 中空形状、type = 2 鉄骨鉄筋、type = 3 鋼矩形
        'Dim x() As Integer = {5, 12, 13, 14, 17.5, 21, 22} '縦長
        'Dim y() As Integer = {1, 2, 7.5, 12, 14, 15, 19}
        'Dim x() As Integer = {5, 9, 10, 11, 17.5, 24, 25} '横長
        'Dim y() As Integer = {1, 3, 6.5, 10, 12, 13, 19}
        Dim x() As Integer = {5, 10, 11, 12, 16, 20, 21} '正方形
        Dim y() As Integer = {1, 2, 6, 9, 11, 13, 19}
        Call FormRect(x, y, col, row, width, Height, type)

    End Sub

    '正方形テンプレート
    Private Sub FormSquare(col As String, row As Integer, val As Integer, type As Integer)
        '形状　type = 0 普通形状、type = 1 中空形状、type = 2 鉄骨鉄筋、type = 3 鋼矩形
        Dim x() As Integer = {5, 10, 11, 12, 16, 20, 21}
        Dim y() As Integer = {1, 2, 6, 9, 11, 13, 19}
        Call FormRect(x, y, col, row, val, val, type)

    End Sub

    '小判形テンプレート
    Private Sub FormOval(col As String, row As String, width As Integer, height As Integer, type As Integer)

        If type = 0 Or type = 1 Then
            '横長 type = 0,　横長中空 type = 1
            Dim x() As Integer = {5, 8, 9, 13, 17, 18, 22, 26}
            Dim y() As Integer = {3, 11, 12, 18}

            With Range(col, row, 0).Drawing
                '形状
                .LineWeight = 1.5
                .AddShape(ShapeType.Ellipse, x(2), y(0), x(4), y(1))
                .AddShape(ShapeType.Ellipse, x(5), y(0), x(7), y(1))
                If type = 1 Then
                    .LineWeight = 1.5
                    .AddShape(ShapeType.Ellipse, x(2) + 2, y(0) + 2, x(4) - 2, y(1) - 2)
                    .AddShape(ShapeType.Ellipse, x(5) + 2, y(0) + 2, x(7) - 2, y(1) - 2)
                End If
                .LineWeight = 24
                .LineColor = Color.White
                .ArrowTypeS = ArrowType.None
                .ArrowTypeE = ArrowType.None
                .AddLine(ShapeType.Line, x(4) + 0.5, y(0), x(4) + 0.5, y(1))
                .Init()
                If type = 1 Then
                    .LineWeight = 1.5
                    .AddLine(ShapeType.Line, x(3), y(0) + 2, x(3) + 1, y(0) + 2)
                    .AddLine(ShapeType.Line, x(4) - 2, y(0) + 2, x(5) + 2, y(0) + 2)
                    .AddLine(ShapeType.Line, x(6) - 1, y(0) + 2, x(6), y(0) + 2)
                    .AddLine(ShapeType.Line, x(3), y(1) - 2, x(3) + 1, y(1) - 2)
                    .AddLine(ShapeType.Line, x(4) - 2, y(1) - 2, x(5) + 2, y(1) - 2)
                    .AddLine(ShapeType.Line, x(6) - 1, y(1) - 2, x(6), y(1) - 2)
                    .AddLine(ShapeType.Line, x(3) + 1, y(0) + 2, x(3) + 1, y(1) - 2)
                    .AddLine(ShapeType.Line, x(4) - 2, y(0) + 2, x(4) - 2, y(1) - 2)
                    .AddLine(ShapeType.Line, x(5) + 2, y(0) + 2, x(5) + 2, y(1) - 2)
                    .AddLine(ShapeType.Line, x(6) - 1, y(0) + 2, x(6) - 1, y(1) - 2)
                End If
                .LineWeight = 1.5
                .AddLine(ShapeType.Line, x(3), y(0), x(6), y(0))
                .AddLine(ShapeType.Line, x(3), y(1), x(6), y(1))
                .Init()
                '寸法線 高さ
                .LineWeight = 0.5
                .ArrowTypeS = 0
                .ArrowTypeE = 0
                .AddLine(ShapeType.Line, x(0), y(0), x(2), y(0))
                .AddLine(ShapeType.Line, x(0), y(1), x(2), y(1))
                .ArrowTypeS = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
                .ArrowTypeE = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
                .AddLine(ShapeType.Line, x(0), y(0), x(0), y(1))
                ''寸法値 幅
                .LineWeight = 0.5
                .ArrowTypeS = 0
                .ArrowTypeE = 0
                .AddLine(ShapeType.Line, x(3), y(2), x(3), y(3))
                .AddLine(ShapeType.Line, x(6), y(2), x(6), y(3))
                .ArrowTypeS = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
                .ArrowTypeE = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
                .AddLine(ShapeType.Line, x(3), y(3), x(6), y(3))
            End With
            '寸法値
            Range(col, row).Str = height
            '幅は中央に表示するため末尾に空白" " を入れた文字列を代入する。
            Dim wStr As String = width.ToString()
            wStr = wStr + Strings.Space(Math.Max(0, 6 - Len(wStr)))
            Range(col, row).Str = wStr

        ElseIf type = 2 Or type = 3 Then
            '縦長 type = 2 縦長空中 type = 3
            Dim x() As Integer = {5, 8, 9, 13, 17}
            Dim y() As Integer = {0, 4, 7, 8, 9, 11, 15, 18, 23}

            With Range(col, row, 0).Drawing
                '形状
                .LineWeight = 1.5
                .AddShape(ShapeType.Ellipse, x(2), y(0), x(4), y(3))
                .AddShape(ShapeType.Ellipse, x(2), y(2), x(4), y(6))
                If type = 3 Then
                    .LineWeight = 1.5
                    .AddShape(ShapeType.Ellipse, x(2) + 2.1, y(0) + 2, x(4) - 1.4, y(3) - 2)
                    .AddShape(ShapeType.Ellipse, x(2) + 2.1, y(2) + 2, x(4) - 1.4, y(6) - 2)
                End If
                .LineWeight = 24
                .LineColor = Color.White
                .ArrowTypeS = ArrowType.None
                .ArrowTypeE = ArrowType.None
                .AddLine(ShapeType.Line, x(3), y(1), x(3), y(5))
                .Init()
                If type = 3 Then
                    .LineWeight = 1.5
                    .AddLine(ShapeType.Line, x(2) + 2.1, y(1), x(2) + 2.1, y(1) + 1)
                    .AddLine(ShapeType.Line, x(2) + 2.1, y(3) - 2, x(2) + 2.1, y(5) - 2)
                    .AddLine(ShapeType.Line, x(2) + 2.1, y(5) - 1, x(2) + 2.1, y(5))
                    .AddLine(ShapeType.Line, x(4) - 1.4, y(1), x(4) - 1.4, y(1) + 1)
                    .AddLine(ShapeType.Line, x(4) - 1.4, y(3) - 2, x(4) - 1.4, y(5) - 2)
                    .AddLine(ShapeType.Line, x(4) - 1.4, y(5) - 1, x(4) - 1.4, y(5))
                    .AddLine(ShapeType.Line, x(2) + 2.1, y(1) + 1, x(4) - 1.4, y(1) + 1)
                    .AddLine(ShapeType.Line, x(2) + 2.1, y(3) - 2, x(4) - 1.4, y(3) - 2)
                    .AddLine(ShapeType.Line, x(2) + 2.1, y(5) - 2, x(4) - 1.4, y(5) - 2)
                    .AddLine(ShapeType.Line, x(2) + 2.1, y(5) - 1, x(4) - 1.4, y(5) - 1)
                End If
                .LineWeight = 1.5
                .AddLine(ShapeType.Line, x(2), y(1), x(2), y(5))
                .AddLine(ShapeType.Line, x(4), y(1), x(4), y(5))
                .Init()
                '寸法線 高さ
                .LineWeight = 0.5
                .ArrowTypeS = 0
                .ArrowTypeE = 0
                .AddLine(ShapeType.Line, x(0), y(1), x(1), y(1))
                .AddLine(ShapeType.Line, x(0), y(5), x(1), y(5))
                .ArrowTypeS = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
                .ArrowTypeE = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
                .AddLine(ShapeType.Line, x(0), y(1), x(0), y(5))
                ''寸法値 幅
                .LineWeight = 0.5
                .ArrowTypeS = 0
                .ArrowTypeE = 0
                .AddLine(ShapeType.Line, x(2), y(6), x(2), y(7) + 1)
                .AddLine(ShapeType.Line, x(4), y(6), x(4), y(7) + 1)
                .ArrowTypeS = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
                .ArrowTypeE = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
                .AddLine(ShapeType.Line, x(2), y(7) + 1, x(4), y(7) + 1)
            End With
            '寸法値
            Range(col, row).Str = height
            '幅は中央に表示するため末尾に空白" " を入れた文字列を代入する。
            Dim wStr As String = width.ToString()
            wStr = wStr + Strings.Space(Math.Max(0, 8 - Len(wStr)))
            Range(col, row).Str = wStr
        End If

    End Sub

    Private Sub FormRect(x() As Integer, y() As Integer, col As String, row As Integer, width As Integer, Height As Integer, type As Integer)
        With Range(col, row, 0).Drawing
            .LineStyle = LineStyle.Normal
            '形状　type = 0 普通形状、type = 1 中空形状、type = 2 鉄骨鉄筋、type = 3 鋼矩形 
            .LineWeight = 1.5
            .ArrowTypeS = 0
            .ArrowTypeE = 0
            If type = 3 Then '鋼矩形
                .LineWeight = 0.7
                .AddLine(ShapeType.Line, x(2), y(0), x(6), y(0))
                .AddLine(ShapeType.Line, x(2), y(0) + 0.5, x(6), y(0) + 0.5)
                .AddLine(ShapeType.Line, x(2), y(4) - 0.5, x(6), y(4) - 0.5)
                .AddLine(ShapeType.Line, x(2), y(4), x(6), y(4))
                .AddLine(ShapeType.Line, x(2), y(0), x(2), y(0) + 0.5)
                .AddLine(ShapeType.Line, x(6), y(0), x(6), y(0) + 0.5)
                .AddLine(ShapeType.Line, x(2), y(4) - 0.5, x(2), y(4))
                .AddLine(ShapeType.Line, x(6), y(4) - 0.5, x(6), y(4))

                .AddLine(ShapeType.Line, x(2) + 0.5, y(0) + 0.5, x(2) + 0.5, y(4) - 0.5)
                .AddLine(ShapeType.Line, x(2) + 1, y(0) + 0.5, x(2) + 1, y(4) - 0.5)
                .AddLine(ShapeType.Line, x(6) - 1, y(0) + 0.5, x(6) - 1, y(4) - 0.5)
                .AddLine(ShapeType.Line, x(6) - 0.5, y(0) + 0.5, x(6) - 0.5, y(4) - 0.5)

                .AddLine(ShapeType.Line, x(4) - 0.25, y(0) + 0.5, x(4) - 0.25, y(0) + 2.5)
                .AddLine(ShapeType.Line, x(4) + 0.25, y(0) + 0.5, x(4) + 0.25, y(0) + 2.5)
                .AddLine(ShapeType.Line, x(4) - 0.25, y(0) + 2.5, x(4) + 0.25, y(0) + 2.5)

                .AddLine(ShapeType.Line, x(4) - 0.25, y(4) - 2.5, x(4) - 0.25, y(4) - 0.5)
                .AddLine(ShapeType.Line, x(4) + 0.25, y(4) - 2.5, x(4) + 0.25, y(4) - 0.5)
                .AddLine(ShapeType.Line, x(4) - 0.25, y(4) - 2.5, x(4) + 0.25, y(4) - 2.5)

                .AddLine(ShapeType.Line, x(2) + 1, y(2) - 0.25, x(2) + 3, y(2) - 0.25)
                .AddLine(ShapeType.Line, x(2) + 1, y(2) + 0.25, x(2) + 3, y(2) + 0.25)
                .AddLine(ShapeType.Line, x(2) + 3, y(2) - 0.25, x(2) + 3, y(2) + 0.25)

                .AddLine(ShapeType.Line, x(6) - 3, y(2) - 0.25, x(6) - 1, y(2) - 0.25)
                .AddLine(ShapeType.Line, x(6) - 3, y(2) + 0.25, x(6) - 1, y(2) + 0.25)
                .AddLine(ShapeType.Line, x(6) - 3, y(2) - 0.25, x(6) - 3, y(2) + 0.25)
            Else
                .LineWeight = 1.5
                .AddLine(ShapeType.Line, x(2), y(0), x(6), y(0))
                .AddLine(ShapeType.Line, x(2), y(4), x(6), y(4))
                .AddLine(ShapeType.Line, x(2), y(0), x(2), y(4))
                .AddLine(ShapeType.Line, x(6), y(0), x(6), y(4))
                If type = 1 Then '中空
                    .AddLine(ShapeType.Line, x(2) + 2, y(0) + 2, x(6) - 2, y(0) + 2)
                    .AddLine(ShapeType.Line, x(2) + 2, y(4) - 2, x(6) - 2, y(4) - 2)
                    .AddLine(ShapeType.Line, x(2) + 2, y(0) + 2, x(2) + 2, y(4) - 2)
                    .AddLine(ShapeType.Line, x(6) - 2, y(0) + 2, x(6) - 2, y(4) - 2)
                ElseIf type = 2 Then '鉄骨鉄筋
                    .LineWeight = 0.7
                    If True Then '正方
                        .AddLine(ShapeType.Line, x(3) + 1.5, y(0) + 1.5, x(5) - 1.5, y(0) + 1.5)
                        .AddLine(ShapeType.Line, x(3) + 1.5, y(0) + 2.5, x(5) - 1.5, y(0) + 2.5)
                        .AddLine(ShapeType.Line, x(3) + 1.5, y(0) + 1.5, x(3) + 1.5, y(0) + 2.5)
                        .AddLine(ShapeType.Line, x(5) - 1.5, y(0) + 1.5, x(5) - 1.5, y(0) + 2.5)

                        .AddLine(ShapeType.Line, x(3) + 1.5, y(4) - 2.5, x(5) - 1.5, y(4) - 2.5)
                        .AddLine(ShapeType.Line, x(3) + 1.5, y(4) - 1.5, x(5) - 1.5, y(4) - 1.5)
                        .AddLine(ShapeType.Line, x(3) + 1.5, y(4) - 2.5, x(3) + 1.5, y(4) - 1.5)
                        .AddLine(ShapeType.Line, x(5) - 1.5, y(4) - 2.5, x(5) - 1.5, y(4) - 1.5)

                        .AddLine(ShapeType.Line, x(3), y(0) + 2.3, x(3), y(4) - 3.2)
                        .AddLine(ShapeType.Line, x(3) + 0.9, y(0) + 2.3, x(3) + 0.9, y(4) - 3.2)
                        .AddLine(ShapeType.Line, x(3), y(0) + 2.3, x(3) + 0.9, y(0) + 2.3)
                        .AddLine(ShapeType.Line, x(3), y(4) - 3.2, x(3) + 0.9, y(4) - 3.2)

                        .AddLine(ShapeType.Line, x(5) - 0.9, y(0) + 2.3, x(5) - 0.9, y(4) - 3.2)
                        .AddLine(ShapeType.Line, x(5), y(0) + 2.3, x(5), y(4) - 3.2)
                        .AddLine(ShapeType.Line, x(5), y(0) + 2.3, x(5) - 0.9, y(0) + 2.3)
                        .AddLine(ShapeType.Line, x(5), y(4) - 3.2, x(5) - 0.9, y(4) - 3.2)

                        .AddLine(ShapeType.Line, x(4) - 0.6, y(0) + 2.5, x(4) - 0.6, y(3) - 0.3)
                        .AddLine(ShapeType.Line, x(4) + 0.3, y(0) + 2.5, x(4) + 0.3, y(3) - 0.3)

                        .AddLine(ShapeType.Line, x(3) + 0.9, y(2) - 0.8, x(4) - 0.6, y(2) - 0.8)
                        .AddLine(ShapeType.Line, x(3) + 0.9, y(2) + 0.1, x(4) - 0.6, y(2) + 0.1)
                        .AddLine(ShapeType.Line, x(4) + 0.3, y(2) - 0.8, x(5) - 0.9, y(2) - 0.8)
                        .AddLine(ShapeType.Line, x(4) + 0.3, y(2) + 0.1, x(5) - 0.9, y(2) + 0.1)

                    Else
                        .AddLine(ShapeType.Line, x(3) + 2, y(1) - 0.3, x(5) - 2, y(1) - 0.3)
                        .AddLine(ShapeType.Line, x(3) + 2, y(1) + 0.6, x(5) - 2, y(1) + 0.6)
                        .AddLine(ShapeType.Line, x(3) + 2, y(1) + 0.6, x(3) + 2, y(1) - 0.3)
                        .AddLine(ShapeType.Line, x(5) - 2, y(1) + 0.6, x(5) - 2, y(1) - 0.3)

                        .AddLine(ShapeType.Line, x(3) + 2, y(3) - 0.3, x(5) - 2, y(3) - 0.3)
                        .AddLine(ShapeType.Line, x(3) + 2, y(3) + 0.6, x(5) - 2, y(3) + 0.6)
                        .AddLine(ShapeType.Line, x(3) + 2, y(3) + 0.6, x(3) + 2, y(3) - 0.3)
                        .AddLine(ShapeType.Line, x(5) - 2, y(3) + 0.6, x(5) - 2, y(3) - 0.3)

                        .AddLine(ShapeType.Line, x(3) + 0.9, y(1) + 1, x(3) + 0.9, y(3) - 1)
                        .AddLine(ShapeType.Line, x(3) + 1.8, y(1) + 1, x(3) + 1.8, y(3) - 1)
                        .AddLine(ShapeType.Line, x(3) + 0.9, y(1) + 1, x(3) + 1.8, y(1) + 1)
                        .AddLine(ShapeType.Line, x(3) + 0.9, y(3) - 1, x(3) + 1.8, y(3) - 1)

                        .AddLine(ShapeType.Line, x(5) - 1.8, y(1) + 1, x(5) - 1.8, y(3) - 1)
                        .AddLine(ShapeType.Line, x(5) - 0.9, y(1) + 1, x(5) - 0.9, y(3) - 1)
                        .AddLine(ShapeType.Line, x(5) - 0.9, y(1) + 1, x(5) - 1.8, y(1) + 1)
                        .AddLine(ShapeType.Line, x(5) - 0.9, y(3) - 1, x(5) - 1.8, y(3) - 1)

                        .AddLine(ShapeType.Line, x(4) - 0.9, y(1) + 0.6, x(4) - 0.9, y(3) - 0.3)
                        .AddLine(ShapeType.Line, x(4), y(1) + 0.6, x(4), y(3) - 0.3)

                        .AddLine(ShapeType.Line, x(3) + 1.8, y(2), x(4) - 0.9, y(2))
                        .AddLine(ShapeType.Line, x(3) + 1.8, y(2) + 0.9, x(4) - 0.9, y(2) + 0.9)
                        .AddLine(ShapeType.Line, x(4), y(2), x(5) - 2, y(2))
                        .AddLine(ShapeType.Line, x(4), y(2) + 0.9, x(5) - 2, y(2) + 0.9)
                    End If

                End If
            End If
            '寸法線 高さ
            .LineWeight = 0.5
            .ArrowTypeS = 0
            .ArrowTypeE = 0
            .AddLine(ShapeType.Line, x(0), y(0), x(1), y(0))
            .AddLine(ShapeType.Line, x(0), y(4), x(1), y(4))
            .ArrowTypeS = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
            .ArrowTypeE = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
            .AddLine(ShapeType.Line, x(0), y(0), x(0), y(4))
            '寸法線 幅
            .LineWeight = 0.5
            .ArrowTypeS = 0
            .ArrowTypeE = 0
            .AddLine(ShapeType.Line, x(2), y(5), x(2), y(6))
            .AddLine(ShapeType.Line, x(6), y(5), x(6), y(6))
            .ArrowTypeS = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
            .ArrowTypeE = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
            .AddLine(ShapeType.Line, x(2), y(6), x(6), y(6))

        End With
        '寸法値
        Range(col, row).Str = Height
        '幅は中央に表示するため末尾に空白" " を入れた文字列を代入する。
        Dim wStr As String = width.ToString()
        wStr = wStr + Strings.Space(Math.Max(0, 6 - Len(wStr)))
        Range(col, row).Str = wStr

        '*** 寸法値は、オブジェクトとして描くとエクセルファイルに保存できない 2014/09/10 コメントアウト
        '.ArrowTypeS = 0
        '.ArrowTypeE = 0
        '.LineStyle = LineStyle.None
        ''寸法値 高
        '.TextAlign = TextAlign.Right
        '.TextAnchor = TextAnchor.Center
        '.TextVertical = TextVertical.Vert270
        '.AddTextBox(ShapeType.Line, hVal.ToString(), x(2), y(0), x(3), y(1))
        ''寸法値 幅
        '.TextAlign = TextAlign.Center
        '.TextAnchor = TextAnchor.Bottom
        '.TextVertical = TextVertical.Horz
        '.AddTextBox(ShapeType.Line, vVal.ToString(), x(0), y(2), x(1), y(3))
    End Sub

    '円形テンプレート
    Private Sub FormCircle(col As String, row As Integer, R As Integer, type As Integer)

        Dim x() As Integer = {5, 6, 8, 17, 19, 20}
        Dim y() As Integer = {0, 1, 3, 11, 12, 14, 15, 18}

        With Range(col, row, 1).Drawing
            '形状　type = 0 普通円形、type = 1 中空円形、type = 2 鋼管
            .LineWeight = 1.5
            .AddShape(ShapeType.Ellipse, x(0), y(0), x(5), y(6))
            If type = 1 Then
                .AddShape(ShapeType.Ellipse, x(2), y(2), x(3), y(4))
            ElseIf type = 2 Then
                .AddShape(ShapeType.Ellipse, x(0) + 1, y(0) + 1, x(5) - 1, y(6) - 1)
            End If
            .Init()
            '寸法線
            .LineWeight = 0.5
            .AddLine(ShapeType.Line, x(0), y(3), x(0), y(7))
            .AddLine(ShapeType.Line, x(5), y(3), x(5), y(7))
            .ArrowTypeS = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
            .ArrowTypeE = ArrowType.Short Or ArrowType.Short Or ArrowType.Arrow
            .AddLine(ShapeType.Line, x(0), y(7), x(5), y(7))
        End With

        '寸法値は中央に表示するため末尾に空白" " を入れた文字列を代入する。
        Dim wStr As String = R.ToString()
        wStr = wStr + Strings.Space(Math.Max(0, 9 - Len(wStr)))
        Range(col, row).Str = wStr

    End Sub

#End Region

#End Region

#Region "安定計算集計表出力帳票ドキュメントの作成"

    Private Sub 基礎安定計算集計表(FoundationDB As List(Of CalculationFoundation))

        If FoundationDB.Count = 0 Then Return

        'FoundationDB を 杭基礎 と 直接基礎に分ける
        Dim 杭基礎DB As New List(Of CalculationFoundation)
        Dim 直接基礎DB As New List(Of CalculationFoundation)

        For Each DB In FoundationDB
            Select Case DB._FoundationType
                Case 0
                    杭基礎DB.Add(DB)
                Case 1
                    直接基礎DB.Add(DB)
            End Select
        Next
        Call 杭基礎安定計算集計表(杭基礎DB)
        Call 直接基礎安定計算集計表(直接基礎DB)

    End Sub

    Private Sub 杭基礎安定計算集計表(FoundationDB As List(Of CalculationFoundation))

        Dim DataCount As Integer = FoundationDB.Count
        If DataCount = 0 Then Return

        CellReport1.Page.Start("杭基礎", "1-999")

        Dim iRow As New List(Of Integer)
        Try
            'あらかじめページ数を計算しておく -------------------------------------------------
            Dim r As Integer = 1
            For Each DB In FoundationDB
                '1.復旧性（性能レベル１）-----------------------------------------------
                '(1)設計鉛直支持力の照査結果
                For i = 2501 To 2504
                    CellReport1.RowCopy(i - 1)
                    CellReport1.RowPaste(r - 1)
                    r += 1
                Next
                CellReport1.RowCopy(2505 - 1)
                For Each i In DB.反力照査要素番号List
                    CellReport1.RowPaste(r - 1)
                    iRow.Add(r)
                    r += 1
                Next
                '(2)設計引抜き抵抗力の照査結果
                For i = 2506 To 2508
                    CellReport1.RowCopy(i - 1)
                    CellReport1.RowPaste(r - 1)
                    r += 1
                Next
                CellReport1.RowCopy(2509 - 1)
                For Each i In DB.反力照査要素番号List
                    CellReport1.RowPaste(r - 1)
                    iRow.Add(r)
                    r += 1
                Next
                '(3)水平変位の照査結果
                For i = 2510 To 2512
                    CellReport1.RowCopy(i - 1)
                    CellReport1.RowPaste(r - 1)
                    r += 1
                Next
                CellReport1.RowCopy(2513 - 1)
                For Each i In DB.変位照査節点番号List
                    CellReport1.RowPaste(r - 1)
                    iRow.Add(r)
                    r += 1
                Next
                '(4)回転角の照査結果
                For i = 2514 To 2516
                    CellReport1.RowCopy(i - 1)
                    CellReport1.RowPaste(r - 1)
                    r += 1
                Next
                CellReport1.RowCopy(2517 - 1)
                For Each i In DB.変位照査節点番号List
                    CellReport1.RowPaste(r - 1)
                    iRow.Add(r)
                    r += 1
                Next
                For i = 2518 To 2520
                    r += 1
                Next

                If DB.反力照査要素番号List.Count + DB.変位照査節点番号List.Count > 4 Then
                    CellReport1.Cell(r).Break = True
                End If

                '2.復旧性（性能レベル2）-----------------------------------------------
                '(1)設計鉛直支持力の照査結果
                For i = 2521 To 2523
                    CellReport1.RowCopy(i - 1)
                    CellReport1.RowPaste(r - 1)
                    r += 1
                Next
                CellReport1.RowCopy(2524 - 1)
                For Each i In DB.反力照査要素番号List
                    CellReport1.RowPaste(r - 1)
                    iRow.Add(r)
                    r += 1
                Next
                '(2)水平変位の照査結果
                For i = 2525 To 2527
                    CellReport1.RowCopy(i - 1)
                    CellReport1.RowPaste(r - 1)
                    r += 1
                Next
                CellReport1.RowCopy(2528 - 1)
                For Each i In DB.変位照査節点番号List
                    CellReport1.RowPaste(r - 1)
                    iRow.Add(r)
                    r += 1
                Next
                '(3)回転角の照査結果
                For i = 2529 To 2531
                    CellReport1.RowCopy(i - 1)
                    CellReport1.RowPaste(r - 1)
                    r += 1
                Next
                CellReport1.RowCopy(2532 - 1)
                For Each i In DB.変位照査節点番号List
                    CellReport1.RowPaste(r - 1)
                    iRow.Add(r)
                    r += 1
                Next

                For i = 2533 To 2535
                    r += 1
                Next

                CellReport1.Cell(r - 1).Break = True

                '3.安全性 -------------------------------------------------------------
                '(1)設計鉛直支持力の照査結果
                For i = 2536 To 2538
                    CellReport1.RowCopy(i - 1)
                    CellReport1.RowPaste(r - 1)
                    r += 1
                Next
                CellReport1.RowCopy(2539 - 1)
                For Each i In DB.反力照査要素番号List
                    CellReport1.RowPaste(r - 1)
                    iRow.Add(r)
                    r += 1
                Next
                '(2)水平変位の照査結果
                For i = 2540 To 2542
                    CellReport1.RowCopy(i - 1)
                    CellReport1.RowPaste(r - 1)
                    r += 1
                Next
                CellReport1.RowCopy(2543 - 1)
                For Each i In DB.変位照査節点番号List
                    CellReport1.RowPaste(r - 1)
                    iRow.Add(r)
                    r += 1
                Next
                '(3)回転角の照査結果
                For i = 2544 To 2546
                    CellReport1.RowCopy(i - 1)
                    CellReport1.RowPaste(r - 1)
                    r += 1
                Next
                CellReport1.RowCopy(2547 - 1)
                For Each i In DB.変位照査節点番号List
                    CellReport1.RowPaste(r - 1)
                    iRow.Add(r)
                r += 1
                Next
                CellReport1.Cell(r).Break = True

            Next
            '使わない表を消す
            CellReport1.RowDelete(2500, 60)

            '安定計算集計表の集計 ----------------------------------------------------
            Dim index As Integer = 0
            For i = 0 To FoundationDB.Count - 1
                Dim DB = FoundationDB(i)
                Call Set杭基礎安定計算集計表(DB, iRow, index)
            Next
        Catch ex As Exception
            Throw ex
        Finally
            CellReport1.Page.End()
        End Try

    End Sub

    Private Sub Set杭基礎安定計算集計表(ByVal DB As CalculationFoundation, ByRef iRow As List(Of Integer), ByRef index As Integer)

        Try
            With DB.安定計算結果.杭基礎
                ' エラーがあった場合の対応
                If FormSettings.Option_大量解析プログラム > 0 Then
                    If .ErrorMessage.Length > 0 Then
                        Range(ColName(1), iRow(index), 0).Value = .ErrorMessage
                        index += DB.反力照査要素番号List.Count * 4 + 6
                        Return
                    End If
                End If

                '1.復旧性（性能レベル１）-----------------------------------------------
                With .復旧性性能レベル1
                    '(1)設計鉛直支持力の照査結果
                    For i = 0 To DB.反力照査要素番号List.Count - 1
                        With .設計鉛直支持力の照査(i)
                            If .決定ケース <> -1 Then
                                Range(ColName(1), iRow(index), 0).Value = .決定ケース + 1
                                Range(ColName(2), iRow(index), 0).Value = .決定ステップ
                                Range(ColName(3), iRow(index), 0).Value = .着目番号
                                Range(ColName(4), iRow(index), 0).Value = Strings.Format(.構造解析係数γa, "0.0")
                                Range(ColName(5), iRow(index), 0).Value = Strings.Format(Math.Round(.応答値, 2), "0.0")
                                Range(ColName(6), iRow(index), 0).Value = Strings.Format(.限界値, "0.0")
                                Range(ColName(7), iRow(index), 0).Value = Strings.Format(.構造物係数γi, "0.0")
                                If .安全度 > 0 Then
                                    Range(ColName(8), iRow(index), 0).Value = Strings.Format(Math.Round(.安全度, 4), "0.000")
                                    Range(ColName(9), iRow(index), 0).Value = IIf(.安全度 < 1, "OK", "NG")
                                Else
                                    Range(ColName(8), iRow(index), 0).Value = "----"
                                    Range(ColName(9), iRow(index), 0).Value = "----"
                                End If
                            End If
                        End With
                        index += 1
                    Next
                    '(2)設計引抜き抵抗力の照査結果
                    For i = 0 To DB.反力照査要素番号List.Count - 1
                        With .設計引抜き抵抗力の照査(i)
                            If .決定ケース <> -1 Then
                                Range(ColName(1), iRow(index), 0).Value = .決定ケース + 1
                                Range(ColName(2), iRow(index), 0).Value = .決定ステップ
                                Range(ColName(3), iRow(index), 0).Value = .着目番号
                                Range(ColName(4), iRow(index), 0).Value = Strings.Format(.構造解析係数γa, "0.0")
                                Range(ColName(5), iRow(index), 0).Value = Strings.Format(Math.Round(-1 * .応答値, 2), "0.0")
                                Range(ColName(6), iRow(index), 0).Value = Strings.Format(.限界値, "0.0")
                                Range(ColName(7), iRow(index), 0).Value = Strings.Format(.構造物係数γi, "0.0")
                                If .安全度 > 0 Then
                                    Range(ColName(8), iRow(index), 0).Value = Strings.Format(Math.Round(.安全度, 4), "0.000")
                                    Range(ColName(9), iRow(index), 0).Value = IIf(.安全度 < 1, "OK", "NG")
                                Else
                                    Range(ColName(8), iRow(index), 0).Value = "----"
                                    Range(ColName(9), iRow(index), 0).Value = "----"
                                End If
                            End If
                        End With
                        index += 1
                    Next
                    '(3)水平変位の照査結果
                    For i = 0 To DB.変位照査節点番号List.Count - 1
                        With .水平変位の照査(i)
                            If .決定ケース <> -1 Then
                                Range(ColName(1), iRow(index), 0).Value = .決定ケース + 1
                                Range(ColName(2), iRow(index), 0).Value = .決定ステップ
                                Range(ColName(3), iRow(index), 0).Value = .着目番号
                                Range(ColName(4), iRow(index), 0).Value = Strings.Format(Math.Round(Math.Abs(.応答値), 4), "0.000")
                                Range(ColName(5), iRow(index), 0).Value = Strings.Format(.限界値, "0.000")
                                Range(ColName(6), iRow(index), 0).Value = Strings.Format(Math.Round(Math.Abs(.安全度), 4), "0.000")
                                Range(ColName(7), iRow(index), 0).Value = IIf(Math.Abs(.安全度) < 1, "OK", "NG")
                            End If
                        End With
                        index += 1
                    Next

                    '(4)回転角の照査結果
                    For i = 0 To DB.変位照査節点番号List.Count - 1
                        With .回転角の照査(i)
                            If .決定ケース <> -1 Then
                                Range(ColName(1), iRow(index), 0).Value = .決定ケース + 1
                                Range(ColName(2), iRow(index), 0).Value = .決定ステップ
                                Range(ColName(3), iRow(index), 0).Value = .着目番号
                                Range(ColName(4), iRow(index), 0).Value = Strings.Format(Math.Round(Math.Abs(.応答値), 4), "0.000")
                                Range(ColName(5), iRow(index), 0).Value = Strings.Format(.限界値, "0.000")
                                Range(ColName(6), iRow(index), 0).Value = Strings.Format(Math.Round(Math.Abs(.安全度), 4), "0.000")
                                Range(ColName(7), iRow(index), 0).Value = IIf(Math.Abs(.安全度) < 1, "OK", "NG")
                            End If
                        End With
                        index += 1
                    Next
                End With '.復旧性性能レベル1

                '2.復旧性（性能レベル2）-----------------------------------------------
                With .復旧性性能レベル2
                    '(1)設計鉛直支持力の照査結果
                    For i = 0 To DB.反力照査要素番号List.Count - 1
                        With .設計鉛直支持力の照査(i)
                            If .決定ケース <> -1 Then
                                Range(ColName(1), iRow(index), 0).Value = .決定ケース + 1
                                Range(ColName(2), iRow(index), 0).Value = .決定ステップ
                                Range(ColName(3), iRow(index), 0).Value = .着目番号
                                Range(ColName(4), iRow(index), 0).Value = Strings.Format(.構造解析係数γa, "0.0")
                                Range(ColName(5), iRow(index), 0).Value = Strings.Format(Math.Round(.応答値, 2), "0.0")
                                Range(ColName(6), iRow(index), 0).Value = Strings.Format(.限界値, "0.0")
                                Range(ColName(7), iRow(index), 0).Value = Strings.Format(.構造物係数γi, "0.0")
                                If .安全度 > 0 Then
                                    Range(ColName(8), iRow(index), 0).Value = Strings.Format(Math.Round(.安全度, 4), "0.000")
                                    Range(ColName(9), iRow(index), 0).Value = IIf(.安全度 < 1, "OK", "NG")
                                Else
                                    Range(ColName(8), iRow(index), 0).Value = "----"
                                    Range(ColName(9), iRow(index), 0).Value = "----"
                                End If
                            End If
                        End With
                        index += 1
                    Next
                    '(2)水平変位の照査結果
                    For i = 0 To DB.変位照査節点番号List.Count - 1
                        With .水平変位の照査(i)
                            If .決定ケース <> -1 Then
                                Range(ColName(1), iRow(index), 0).Value = .決定ケース + 1
                                Range(ColName(2), iRow(index), 0).Value = .決定ステップ
                                Range(ColName(3), iRow(index), 0).Value = .着目番号
                                Range(ColName(4), iRow(index), 0).Value = Strings.Format(Math.Round(Math.Abs(.応答値), 4), "0.000")
                                Range(ColName(5), iRow(index), 0).Value = Strings.Format(.限界値, "0.000")
                                Range(ColName(6), iRow(index), 0).Value = Strings.Format(Math.Round(Math.Abs(.安全度), 4), "0.000")
                                Range(ColName(7), iRow(index), 0).Value = IIf(Math.Abs(.安全度) < 1, "OK", "NG")
                            End If
                        End With
                        index += 1
                    Next

                    '(3)回転角の照査結果
                    For i = 0 To DB.変位照査節点番号List.Count - 1
                        With .回転角の照査(i)
                            If .決定ケース <> -1 Then
                                Range(ColName(1), iRow(index), 0).Value = .決定ケース + 1
                                Range(ColName(2), iRow(index), 0).Value = .決定ステップ
                                Range(ColName(3), iRow(index), 0).Value = .着目番号
                                Range(ColName(4), iRow(index), 0).Value = Strings.Format(Math.Round(Math.Abs(.応答値), 4), "0.000")
                                Range(ColName(5), iRow(index), 0).Value = Strings.Format(.限界値, "0.000")
                                Range(ColName(6), iRow(index), 0).Value = Strings.Format(Math.Round(Math.Abs(.安全度), 4), "0.000")
                                Range(ColName(7), iRow(index), 0).Value = IIf(Math.Abs(.安全度) < 1, "OK", "NG")
                            End If
                        End With
                        index += 1
                    Next
                End With '.復旧性性能レベル2

                '3.安全性 -------------------------------------------------------------
                With .安全性
                    '(1)設計鉛直支持力の照査結果
                    For i = 0 To DB.反力照査要素番号List.Count - 1
                        With .設計鉛直支持力の照査(i)
                            If .決定ケース <> -1 Then
                                Range(ColName(1), iRow(index), 0).Value = .決定ケース + 1
                                Range(ColName(2), iRow(index), 0).Value = .決定ステップ
                                Range(ColName(3), iRow(index), 0).Value = .着目番号
                                Range(ColName(4), iRow(index), 0).Value = Strings.Format(.構造解析係数γa, "0.0")
                                Range(ColName(5), iRow(index), 0).Value = Strings.Format(Math.Round(.応答値, 2), "0.0")
                                Range(ColName(6), iRow(index), 0).Value = Strings.Format(.限界値, "0.0")
                                Range(ColName(7), iRow(index), 0).Value = Strings.Format(.構造物係数γi, "0.0")
                                If .安全度 > 0 Then
                                    Range(ColName(8), iRow(index), 0).Value = Strings.Format(Math.Round(.安全度, 4), "0.000")
                                    Range(ColName(9), iRow(index), 0).Value = IIf(.安全度 < 1, "OK", "NG")
                                Else
                                    Range(ColName(8), iRow(index), 0).Value = "----"
                                    Range(ColName(9), iRow(index), 0).Value = "----"
                                End If
                            End If
                        End With
                        index += 1
                    Next
                    '(2)水平変位の照査結果
                    For i = 0 To DB.変位照査節点番号List.Count - 1
                        With .水平変位の照査(i)
                            If .決定ケース <> -1 Then
                                Range(ColName(1), iRow(index), 0).Value = .決定ケース + 1
                                Range(ColName(2), iRow(index), 0).Value = .決定ステップ
                                Range(ColName(3), iRow(index), 0).Value = .着目番号
                                Range(ColName(4), iRow(index), 0).Value = Strings.Format(Math.Round(Math.Abs(.応答値), 4), "0.000")
                                Range(ColName(5), iRow(index), 0).Value = Strings.Format(.限界値, "0.000")
                                Range(ColName(6), iRow(index), 0).Value = Strings.Format(Math.Round(Math.Abs(.安全度), 4), "0.000")
                                Range(ColName(7), iRow(index), 0).Value = IIf(Math.Abs(.安全度) < 1, "OK", "NG")
                            End If
                        End With
                        index += 1
                    Next

                    '(3)回転角の照査結果
                    For i = 0 To DB.変位照査節点番号List.Count - 1
                        With .回転角の照査(i)
                            If .決定ケース <> -1 Then
                                Range(ColName(1), iRow(index), 0).Value = .決定ケース + 1
                                Range(ColName(2), iRow(index), 0).Value = .決定ステップ
                                Range(ColName(3), iRow(index), 0).Value = .着目番号
                                Range(ColName(4), iRow(index), 0).Value = Strings.Format(Math.Round(Math.Abs(.応答値), 4), "0.000")
                                Range(ColName(5), iRow(index), 0).Value = Strings.Format(.限界値, "0.000")
                                Range(ColName(6), iRow(index), 0).Value = Strings.Format(Math.Round(Math.Abs(.安全度), 4), "0.000")
                                Range(ColName(7), iRow(index), 0).Value = IIf(Math.Abs(.安全度) < 1, "OK", "NG")
                            End If
                        End With
                        index += 1
                    Next
                End With '.復旧性性能レベル2

            End With 'DB.安定計算結果.杭基礎
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub 直接基礎安定計算集計表(FoundationDB As List(Of CalculationFoundation))

        Dim DataCount As Integer = FoundationDB.Count
        If DataCount = 0 Then Return

        CellReport1.Page.Start("直接基礎", "1-999")

        Try
            'あらかじめページ数を計算しておく -------------------------------------------------
            Dim iRow As New List(Of Integer)
            '行をコピーする
            CellReport1.Cell("A2501:S2534").Copy()
            For ip = 0 To DataCount - 1
                Dim r As Integer = ip * 51 + 3
                Range("A", r, 0).Paste()
                iRow.Add(r)
            Next
            '使わない表を消す
            CellReport1.RowDelete(2500, 40)

            '安定計算集計表の集計 ----------------------------------------------------
            For i = 0 To FoundationDB.Count - 1
                Dim DB = FoundationDB(i)
                '値を代入する。
                Call Set直接基礎安定計算集計表(DB, iRow(i))
            Next
        Catch ex As Exception
            Throw ex
        Finally
            CellReport1.Page.End()
        End Try
    End Sub

    Private Sub Set直接基礎安定計算集計表(DB As CalculationFoundation, iRow As Integer)
        Try

            With DB.安定計算結果.直接基礎

                Dim r As Integer = iRow + 5

                ' エラーがあった場合の対応
                If FormSettings.Option_大量解析プログラム > 0 Then
                    If .ErrorMessage.Length > 0 Then
                        Range(ColName(1), r, 0).Value = .ErrorMessage
                        Return
                    End If
                End If

                '1.復旧性（性能レベル１） -------------------------------------------------------------------
                With .復旧性性能レベル1
                    '(1)設計鉛直支持力の照査結果
                    With .設計鉛直支持力の照査
                        If .決定ケース <> -1 Then
                            Range(ColName(1), r, 0).Value = .決定ケース + 1
                            Range(ColName(2), r, 0).Value = .決定ステップ
                            Range(ColName(3), r, 0).Value = .着目番号
                            Range(ColName(4), r, 0).Value = Strings.Format(.構造解析係数γa, "0.0")
                            Range(ColName(5), r, 0).Value = Strings.Format(Math.Round(.応答値, 4), "0.000")
                            Range(ColName(6), r, 0).Value = Strings.Format(.限界値, "0.000")
                            Range(ColName(7), r, 0).Value = Strings.Format(.構造物係数γi, "0.0")
                            Range(ColName(8), r, 0).Value = Strings.Format(Math.Round(.安全度, 4), "0.000")
                            Range(ColName(9), r, 0).Value = IIf(.安全度 < 1, "OK", "NG")
                        End If
                    End With
                    r += 5
                    '(2)設計水平支持力の照査結果
                    With .設計水平支持力の照査
                        If .決定ケース <> -1 Then
                            Range(ColName(1), r, 0).Value = .決定ケース + 1
                            Range(ColName(2), r, 0).Value = .決定ステップ
                            Range(ColName(3), r, 0).Value = .着目番号
                            Range(ColName(4), r, 0).Value = Strings.Format(.構造解析係数γa, "0.0")
                            Range(ColName(5), r, 0).Value = Strings.Format(Math.Round(.応答値, 4), "0.000")
                            Range(ColName(6), r, 0).Value = Strings.Format(.限界値, "0.000")
                            Range(ColName(7), r, 0).Value = Strings.Format(.構造物係数γi, "0.0")
                            Range(ColName(8), r, 0).Value = Strings.Format(Math.Round(.安全度, 4), "0.000")
                            Range(ColName(9), r, 0).Value = IIf(.安全度 < 1, "OK", "NG")
                        End If
                    End With
                    r += 5
                    '(3)残留傾斜の照査結果
                    With .残留傾斜の照査
                        If .決定ケース <> -1 Then
                            Range(ColName(1), r, 0).Value = .決定ケース + 1
                            Range(ColName(2), r, 0).Value = .決定ステップ
                            Range(ColName(3), r, 0).Value = .着目番号
                            Range(ColName(4), r, 0).Value = Strings.Format(.構造解析係数γa, "0.0")
                            Range(ColName(5), r, 0).Value = Strings.Format(Math.Round(.応答値, 4), "0.000")
                            Range(ColName(6), r, 0).Value = Strings.Format(.限界値, "0.000")
                            Range(ColName(7), r, 0).Value = Strings.Format(.構造物係数γi, "0.0")
                            Range(ColName(8), r, 0).Value = Strings.Format(Math.Round(.安全度, 4), "0.000")
                            Range(ColName(9), r, 0).Value = IIf(.安全度 < 1, "OK", "NG")
                        End If
                    End With
                End With '.復旧性性能レベル1

                '2.復旧性（性能レベル2）） -------------------------------------------------------------------
                With .復旧性性能レベル2
                    r = iRow + 23
                    '(1)底面塑性化率の照査結果
                    With .底面塑性化率の照査
                        If .決定ケース <> -1 Then
                            Range(ColName(1), r, 0).Value = .決定ケース + 1
                            Range(ColName(2), r, 0).Value = .決定ステップ
                            Range(ColName(3), r, 0).Value = .着目番号
                            Range(ColName(4), r, 0).Value = Strings.Format(Math.Round(.応答値, 4), "0.000")
                            Range(ColName(5), r, 0).Value = Strings.Format(.限界値, "0.000")
                            Range(ColName(6), r, 0).Value = Strings.Format(Math.Round(.安全度, 4), "0.000")
                            Range(ColName(7), r, 0).Value = IIf(.安全度 < 1, "OK", "NG")
                        End If
                    End With
                    r += 5
                    '(2)設計水平支持力の照査結果
                    With .設計水平支持力の照査
                        If .決定ケース <> -1 Then
                            Range(ColName(1), r, 0).Value = .決定ケース + 1
                            Range(ColName(2), r, 0).Value = .決定ステップ
                            Range(ColName(3), r, 0).Value = .着目番号
                            Range(ColName(4), r, 0).Value = Strings.Format(.構造解析係数γa, "0.0")
                            Range(ColName(5), r, 0).Value = Strings.Format(Math.Round(.応答値, 4), "0.000")
                            Range(ColName(6), r, 0).Value = Strings.Format(.限界値, "0.000")
                            Range(ColName(7), r, 0).Value = Strings.Format(.構造物係数γi, "0.0")
                            Range(ColName(8), r, 0).Value = Strings.Format(Math.Round(.安全度, 4), "0.000")
                            Range(ColName(9), r, 0).Value = IIf(.安全度 < 1, "OK", "NG")
                        End If
                    End With
                    r += 5
                    '(3)回転角の照査結果
                    With .回転角の照査
                        If .決定ケース <> -1 Then
                            Range(ColName(1), r, 0).Value = .決定ケース + 1
                            Range(ColName(2), r, 0).Value = .決定ステップ
                            Range(ColName(3), r, 0).Value = .着目番号
                            Range(ColName(4), r, 0).Value = Strings.Format(Math.Round(.応答値, 4), "0.000")
                            Range(ColName(5), r, 0).Value = Strings.Format(.限界値, "0.000")
                            Range(ColName(6), r, 0).Value = Strings.Format(Math.Round(.安全度, 4), "0.000")
                            Range(ColName(7), r, 0).Value = IIf(.安全度 < 1, "OK", "NG")
                        End If
                    End With
                End With '.復旧性性能レベル2

                '3.安全性 ---------------------------------------------------------------------------
                With .安全性
                    r = iRow + 5
                    '(1)底面塑性化率の照査結果
                    With .底面塑性化率の照査
                        If .決定ケース <> -1 Then
                            Range(ColName(11), r, 0).Value = .決定ケース + 1
                            Range(ColName(12), r, 0).Value = .決定ステップ
                            Range(ColName(13), r, 0).Value = .着目番号
                            Range(ColName(14), r, 0).Value = Strings.Format(Math.Round(.応答値, 4), "0.000")
                            Range(ColName(15), r, 0).Value = Strings.Format(.限界値, "0.000")
                            Range(ColName(16), r, 0).Value = Strings.Format(Math.Round(.安全度, 4), "0.000")
                            Range(ColName(17), r, 0).Value = IIf(.安全度 < 1, "OK", "NG")
                        End If
                    End With
                    r += 5
                    '(2)設計水平支持力の照査結果
                    With .設計水平支持力の照査
                        If .決定ケース <> -1 Then
                            Range(ColName(11), r, 0).Value = .決定ケース + 1
                            Range(ColName(12), r, 0).Value = .決定ステップ
                            Range(ColName(13), r, 0).Value = .着目番号
                            Range(ColName(14), r, 0).Value = Strings.Format(.構造解析係数γa, "0.0")
                            Range(ColName(15), r, 0).Value = Strings.Format(Math.Round(.応答値, 4), "0.000")
                            Range(ColName(16), r, 0).Value = Strings.Format(.限界値, "0.000")
                            Range(ColName(17), r, 0).Value = Strings.Format(.構造物係数γi, "0.0")
                            Range(ColName(18), r, 0).Value = Strings.Format(Math.Round(.安全度, 4), "0.000")
                            Range(ColName(19), r, 0).Value = IIf(.安全度 < 1, "OK", "NG")
                        End If
                    End With
                    r += 5
                    '(3)回転角の照査結果
                    With .回転角の照査
                        If .決定ケース <> -1 Then
                            Range(ColName(11), r, 0).Value = .決定ケース + 1
                            Range(ColName(12), r, 0).Value = .決定ステップ
                            Range(ColName(13), r, 0).Value = .着目番号
                            Range(ColName(14), r, 0).Value = Strings.Format(Math.Round(.応答値, 4), "0.000")
                            Range(ColName(15), r, 0).Value = Strings.Format(.限界値, "0.000")
                            Range(ColName(16), r, 0).Value = Strings.Format(Math.Round(.安全度, 4), "0.000")
                            Range(ColName(17), r, 0).Value = IIf(.安全度 < 1, "OK", "NG")
                        End If
                    End With
                End With '.安全性

            End With

        Catch ex As Exception

        End Try
    End Sub

#End Region

#Region "安全率一覧表"

    Private Sub 安全率一覧表(ElementDB As List(Of CalculationElement))

        Dim DataCount As Integer = ElementDB.Count
        If DataCount = 0 Then Return

        CellReport1.Page.Start("安全率一覧表", "1-999")

        Try
            Dim NG部材List As New List(Of Integer)
            Dim isNGFlg As Boolean = False
            Dim row As Integer = 1
            Dim col As New List(Of String)
            For i = 1 To 9
                col.Add(ColName(i))
            Next
            Range(col(0), row, 2).Value = "安全率一覧表"
            Range(col(1), row, 0).Value = "No"
            Range(col(2), row, 0).Value = "断面名称"
            Range(col(3), row, 0).Value = "破壊形態"
            Range(col(4), row, 0).Value = "せん断耐力"
            Range(col(5), row, 0).Value = "曲げ耐力"
            Range(col(6), row, 0).Value = "損傷レベル"
            Range(col(7), row, 0).Value = "L1地震動"
            Range(col(8), row, 0).Value = "判定"

            For i = 0 To ElementDB.Count - 1
                With ElementDB(i)
                    row += 1
                    Range(col(1), row, 0).Value = .DLNo '"断面(DL)番号"
                    Range(col(2), row, 0).Value = .DLinfo.Title.Trim '"断面名称"
                    With .断面照査結果
                        If .破壊形態の照査.安全率 > 0 Then
                            If .破壊形態の照査.安全率 > 1 Then
                                Range(col(3), row, 0).Attr.FontColor = System.Drawing.Color.Red
                            End If
                            Range(col(3), row, 0).Value = Strings.Format(.破壊形態の照査.安全率, "0.000") '"破壊形態"
                        Else
                            Range(col(3), row, 0).Value = "----"
                        End If
                        If .せん断破壊の照査.Gami_Vdmax_Vyd > 0 And .せん断破壊の照査.設計せん断耐力Vud <> 0 Then
                            If .せん断破壊の照査.Gami_Vdmax_Vyd > 1 Then
                                Range(col(4), row, 0).Attr.FontColor = System.Drawing.Color.Red
                            End If
                            Range(col(4), row, 0).Value = Strings.Format(.せん断破壊の照査.Gami_Vdmax_Vyd, "0.000") '"せん断耐力"
                        Else
                            Range(col(4), row, 0).Value = "----"
                        End If
                        If .せん断破壊の照査.Gami_Mdmax_Myd > 0 And .せん断破壊の照査.設計曲げ降伏耐力Myd <> 0 Then
                            If .せん断破壊の照査.Gami_Mdmax_Myd > 1 Then
                                Range(col(5), row, 0).Attr.FontColor = System.Drawing.Color.Red
                            End If
                            Range(col(5), row, 0).Value = Strings.Format(.せん断破壊の照査.Gami_Mdmax_Myd, "0.000") '"曲げ耐力"
                        Else
                            Range(col(5), row, 0).Value = "----"
                        End If
                        Dim index As Integer = .損傷レベル.Limit
                        If index > 0 AndAlso .損傷レベル.安全率(index - 1) > 0 Then
                            If .損傷レベル.安全率(index - 1) > 1 Then
                                Range(col(6), row, 0).Attr.FontColor = System.Drawing.Color.Red
                            End If
                            Range(col(6), row, 0).Value = Strings.Format(.損傷レベル.安全率(index - 1), "0.000") '"損傷レベル"
                        Else
                            Range(col(6), row, 0).Value = "----"
                        End If
                        If .L1地震動.安全度 > 0 Then
                            If .L1地震動.安全度 > 1 Then
                                Range(col(7), row, 0).Attr.FontColor = System.Drawing.Color.Red
                            End If
                            Range(col(7), row, 0).Value = Strings.Format(.L1地震動.安全度, "0.000") '"L1地震動"
                        Else
                            Range(col(7), row, 0).Value = "----"
                        End If
                        If .総合的な照査結果 = cls断面照査.NG Then
                            Range(col(8), row, 0).Attr.FontColor = System.Drawing.Color.Red
                        End If
                        Range(col(8), row, 0).Value = .総合的な照査結果 '"判定"

                    End With
                    If .NG部材List.Count > 0 Then
                        For Each mNo In .NG部材List
                            NG部材List.Add(mNo)
                        Next
                        isNGFlg = True
                    End If
                End With
            Next

            If isNGFlg = False Then
                CellReport1.Page.Attr.Size(PageOrientation.Portrait, System.Drawing.Printing.PaperKind.A4) 'A4縦
            Else
                'NG部材図を表示する。
                CellReport1.Page.Attr.Size(PageOrientation.Landscape, System.Drawing.Printing.PaperKind.A3) 'A3横
                Range("K", 1, 0).Value = "NG部材一覧"
                Range("M", 3, 0).Value = "下図に番号が表示された部材はＮＧの項目があります。"

                Call PaintNG部材図(NG部材List)

            End If

        Catch ex As Exception
            Throw ex
        Finally
            CellReport1.Page.End()
        End Try
    End Sub

    Private Sub PaintNG部材図(NG部材List As List(Of Integer))

        Dim SnapDB As CSNAPDBEx = Input.Data.SNAP.SnapDB

        Dim width As Integer
        Dim height As Integer

        Dim MyPictureBox0 As New MyPictureBox
        Try
            With MyPictureBox0
                '節点の設定
                .PointX.Clear()
                .PointY.Clear()
                For i = 0 To SnapDB.GetJointCount
                    Dim p = SnapDB.GetJoint(i)
                    .PointX.Add(i, p.X)
                    .PointY.Add(i, p.Y)
                Next
                'サイズの決定
                If .MaxXDistance < .MaxYDistance Then
                    height = 1000
                    width = height * .MaxXDistance / .MaxYDistance
                Else
                    width = 800
                    height = width * .MaxYDistance / .MaxXDistance
                End If
            End With
        Catch ex As Exception
            Throw ex
        End Try

        If width = 0 Then width = height / 10
        If height = 0 Then height = width / 10

        Dim MyPictureBox1 As New MyPictureBox With {.Width = width, .Height = height}
        Try
            With MyPictureBox1
                '節点の設定
                .PointX = MyPictureBox0.PointX
                .PointY = MyPictureBox0.PointY
                .PrintPointNo.Clear()

                '部材の設定
                .MemberNum = SnapDB.GetMemberCount
                ReDim .MemberNo(.MemberNum + 1)
                ReDim .PrintMemberNo(.MemberNum + 1)
                ReDim .IPointNo(.MemberNum + 1)
                ReDim .JPointNo(.MemberNum + 1)

                For i = 1 To .MemberNum
                    .MemberNo(i) = i
                    'NGの要素番号を表示する
                    .PrintMemberNo(i) = ""
                    For Each mNo In NG部材List
                        If i = mNo Then
                            .PrintMemberNo(i) = mNo.ToString()
                            Exit For
                        End If
                    Next
                    .IPointNo(i) = SnapDB.GetMember(i).Itan
                    .JPointNo(i) = SnapDB.GetMember(i).Jtan
                Next

                Dim Image As System.Drawing.Image = .GetImage(width, height)
                CellReport1.Cell("K4").Drawing.AddImage(Image, width / 4, height / 4)

            End With

        Catch ex As Exception
            Throw ex
        End Try
    End Sub

#End Region

#Region "エクセル操作関数"

    Private Function Range(ByVal col As String, ByRef row As Integer, Optional AfterRowCount As Integer = 1) As AdvanceSoftware.VBReport8.Cell

        Dim cell As String = col + row.ToString()
        row += AfterRowCount
        Return CellReport1.Cell(cell)

    End Function

    Private Function Range(ByVal col1 As String, ByVal row1 As Integer, ByVal col2 As String, ByVal row2 As Integer) As AdvanceSoftware.VBReport8.Cell

        Dim cell As String = col1 + row1.ToString() + ":" + col2 + row2.ToString()
        Return CellReport1.Cell(cell)
    End Function

    Private Function ColName(iCol As Integer) As String
        Dim result As String = ""
        Dim iAlpha As Integer
        Dim iRemainder As Integer
        iAlpha = Int((iCol - 1) / 26)
        iRemainder = iCol - (iAlpha * 26)
        If iAlpha > 0 Then
            result = Chr(iAlpha + 64)
        End If
        If iRemainder > 0 Then
            result = result & Chr(iRemainder + 64)
        End If
        Return result
    End Function

#End Region

#Region "終了後中間ファイルを生成する"

    Private Sub SaveFile()
        Dim Filename As String = ""
        Dim Value As New List(Of String)

        Try
            '保存するファイル名の設定
            'Dim appPath As String = My.Application.Info.DirectoryPath
            ''= System.Reflection.Assembly.GetExecutingAssembly().Location
            Filename = common.TempPath + "\forDetail.txt"
        Catch ex As Exception

        End Try

        Try
            '保存するファイル内容の設定
            'ファイルパス(DB1),L1相当のステップ数, 降伏時のステップ数, 最大震度時のステップ数, 最大応答変位時のステップ数
            Dim path As String
            Dim L1Step As Integer
            Dim KhyStep As Integer
            Dim MaxKhStep As Integer
            Dim KhrStep As Integer

            For Each d In CaseDB
                For Each f In d._解析対象
                    If f = True Then
                        path = d._SNAPDB.DataPath
                        L1Step = d.応答値.復旧性_L1震度step
                        KhyStep = d.応答値.初期降伏step
                        MaxKhStep = d.応答値.最大震度step
                        KhrStep = d.応答値.安全性_最大応答step
                        Value.Add(String.Format("{0},{1},{2},{3},{4}", path, L1Step, KhyStep, MaxKhStep, KhrStep))
                        Exit For
                    End If
                Next
            Next


        Catch ex As Exception

        End Try

        Try
            '書き込むファイルが既に存在している場合は、上書きする
            Dim sw As New System.IO.StreamWriter(Filename,
                False,
                System.Text.Encoding.GetEncoding("shift_jis"))
            'resultの内容を書き込む
            sw.Write(Value.Count)
            sw.Write(vbLf)
            For Each s In Value
                sw.Write(s)
                sw.Write(vbLf)
            Next
            '閉じる
            sw.Close()
        Catch ex As Exception

        End Try

    End Sub

#End Region

End Class


