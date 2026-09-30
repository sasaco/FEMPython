Imports System.ComponentModel
Imports AdvanceSoftware.VBReport8
Imports iTextSharp.text.pdf
Imports System.IO
Imports Aggre
Imports WinPDFPrint.Printing

Class PDFReport
    Inherits ExcelReport

#Region "メンバ変数"

    '断面力図に関する変数 ------------------------------------------
    Private Const Margin = 150      '余白 pixcel
    Private Const radius = 4        '節点の円 の大きさ pixcel
    Private Const thickness1 = 1    '骨組み図 の線の太さ pixcel
    Private Const thickness2 = 1    '変位図図 の線の太さ pixcel
    Private printMaxPoint As Integer = 20 '荷重変位曲線を簡易モードで描く最大引き出し数
    Private printMaxRow As Integer = 45 '震度と変位帳票の１ページ当りの行数
    Private PointOrg As Dictionary(Of Integer, PointF)
    Private MaxX As Single
    Private MinX As Single
    Private MaxY As Single
    Private MinY As Single

    Private Orientation As Boolean
    Private ScaleX As Single
    Private ScaleY As Single

    Private Const ScaleAlpha = 0.6
    '--------------------------------------------------------------

    Private preview As WinPDFPrint.PrintPreview
    Private InputFile As List(Of String)

    Private 出力モード As Integer


#End Region

#Region "計算書作成スタート関数"

    Public Sub 計算書作成スタート(Optional ShowMode As Integer = 0)

        Me.出力モード = ShowMode
        InputFile = New List(Of String)

        '①:応答値, 各照査結果総括表を作成する(プレビューなしで) -----------------------------------------------
        Try
            If ShowMode = -6 Then
                '大量解析ソフト用
                Call MyBase.総括表作成スタート(-1)
            Else
                Call MyBase.総括表作成スタート(2)
            End If
        Catch ex As Exception
            Throw ex
        End Try
        If MyBase.CaseDB Is Nothing Then Return
        If MyBase.CaseDB.Count = 0 Then Return

        '②:荷重～変位曲線, 応力図を出力する。 -----------------------------------------------------------------
        Dim pb As New MyProgressBarForm("", New DoWorkEventHandler(AddressOf Create計算書))
        pb.ShowDialog()

        ' 大量解析ソフト対応モードの時はここで終了
        If ShowMode = -6 Then Return

        '③:上記① 総括表 の pdf ファイルを保存する FN3
        Dim FN3 As String = common.TempPath + "\BatchPrintSynopticalTable.pdf"
        Try
            System.IO.File.Delete(FN3)
            MyBase.SavePdfDocument(FN3)
            If System.IO.File.Exists(FN3) Then
                InputFile.Add(FN3)
            End If
        Catch ex As Exception
            Throw New Exception("安全率総括表等の出力に失敗しました。" + vbLf _
                                + "ヒント:「" + FN3 + "」ファイルが閉じているか確認してください。")
        End Try

        '④:上記InputFile内の pdf ファイルを結合し、表示。 
        Dim FN As String = common.TempPath + "\PrintPreview.pdf"
        Try
            Dim MyPdfViewerForm As New pdfViewerForm(InputFile, FN)
            '結合前のファイルを消去
            deleteFiles(InputFile)
            If Me.出力モード >= 0 Then
                '表示
                MyPdfViewerForm.ShowPdf() '.Show() 
            Else
                Try
                    Dim Filename As String = System.IO.Path.ChangeExtension(Input.FileName, ".pdf")
                    System.IO.File.Copy(FN, Filename, True)
                Catch ex As Exception
                    Throw ex
                End Try
            End If

        Catch ex As Exception
            'Throw New Exception("計算書の出力に失敗しました。" + vbLf _
            '              + ex.Message)
        End Try


    End Sub

    Private Sub deleteFiles(InputFile As List(Of String))
        On Error Resume Next
        For Each BatchPdf In InputFile
            System.IO.File.Delete(BatchPdf)
        Next
    End Sub

    Private Sub Create計算書(ByVal sender As Object, ByVal e As DoWorkEventArgs)

        If MyBase.CaseDB Is Nothing Then Return
        Dim bw As BackgroundWorker = DirectCast(sender, BackgroundWorker)

        Dim counter As Integer = 0
        Dim firstFlg As Boolean = False


        For i = 0 To MyBase.CaseDB.Count - 1

            Dim CaseDB = MyBase.CaseDB(i)

            If CaseDB.IsPrintOut = True Then

                counter += 1
                Dim CountString = String.Format("{0}/{1}", counter, MyBase.CaseDB.Count)


                '荷重変位曲線と応力図 ------------------------------------------------------------------------------------------
                Me.preview = New WinPDFPrint.PrintPreview
                Create荷重変位曲線と応力図(sender, CountString, CaseDB)
                Dim FN1 As String = String.Format("{0}{1}{2}{3}", common.TempPath, "\BatchPrintP-δ", counter, ".pdf")
                Try
                    System.IO.File.Delete(FN1)
                    Me.preview.SavePDF(FN1)
                    InputFile.Add(FN1)
                Catch ex As Exception
                    Throw New Exception("荷重～変位曲線等の出力に失敗しました。" + vbLf _
                                        + "ヒント:「" + FN1 + "」ファイルが閉じているか確認してください。")
                Finally
                    Me.preview = Nothing
                End Try


                '部材照査表 -----------------------------------------------------------------------------------------------------
                If Me.出力モード = 0 OrElse Me.出力モード = -1 Then

                    Dim FN2 As String = String.Format("{0}{1}{2}{3}", common.TempPath, "\BatchPrintCheckList", counter, ".pdf")
                    bw.ReportProgress(0, "部材照査表生成中... " + CountString)

                    Dim TmpExcelReport As ExcelReport
                    Try
                        TmpExcelReport = New ExcelReport
                        If TmpExcelReport.Create照査表(CaseDB) = True Then
                            System.IO.File.Delete(FN2)
                            TmpExcelReport.SavePdfDocument(FN2)
                            InputFile.Add(FN2)

                            If FormSettings.Option_各解析ケースの結果出力 = 1 Then
                                Dim Filename As String = System.IO.Path.ChangeExtension(CaseDB._SNAPDB.DataPath, ".xlsx")
                                TmpExcelReport.SaveExcelDocument(Filename)
                            End If

                        End If
                    Catch ex As Exception
                    Finally
                        TmpExcelReport = Nothing
                    End Try
                End If

            End If
        Next

    End Sub

    Private Sub Create荷重変位曲線と応力図(ByVal sender As Object, CountString As String, CaseDB As CalculationCase)


        Dim bw As BackgroundWorker = DirectCast(sender, BackgroundWorker)


        '荷重変位曲線を作成する。
        bw.ReportProgress(0, "荷重変位曲線生成中... " + CountString)
        Call Create荷重変位曲線(CaseDB)
        If CaseDB._SNAPDB.InputInfo.KihonInfo.KaisekiFlg = 1 Then
            '*** 変位増分解析 ***
            '固有周期など応答値の算出過程を示す。
            preview.NewPage()
            Call print応答値の算定(CaseDB)
        End If


        '骨組み図を作成する。
        bw.ReportProgress(0, "断面力図生成中... " + CountString)

        preview.NewPage()
        Call Init断面力図(CaseDB)

        Call print骨組み図(CaseDB, True)
        Call print変位図(CaseDB)

        preview.NewPage(Orientation)
        Call print骨組み図(CaseDB)
        Call printモーメント図(CaseDB)

        preview.NewPage(Orientation)
        Call print骨組み図(CaseDB)
        Call printせん断力図(CaseDB)

        preview.NewPage(Orientation)
        Call print骨組み図(CaseDB)
        Call print軸方向力図(CaseDB)

        preview.NewPage(Orientation)
        Call Create損傷状況(CaseDB)


    End Sub

#End Region

#Region "荷重変位曲線作成"

    Private Sub Create荷重変位曲線(DB As Aggre.CalculationCase)

        Try
            'タイトル出力 ----------------------------------------------------------------------
            preview.PrtText(DB._SNAPDB.InputInfo.KihonInfo.Title)
            preview.enter(1.0)

            Dim dPos As PointF = preview.GetCurrentPos()
            preview.SetCurrentPos(New PointF(dPos.X + 20, dPos.Y))
            preview.PrtText(DB._SNAPDB.InputInfo.KihonInfo.Comment)
            preview.enter(1.5)

            Dim cPos As PointF = preview.GetCurrentPos()
            preview.SetCurrentPos(New PointF(cPos.X + 20, cPos.Y))
            preview.PrtText("荷重(P)～変位(δ)曲線")
            preview.enter(0.2)

            '荷重変位曲線出力 ----------------------------------------------------------------------
            Dim Notrice As NoticeInfoEX = 荷重変位曲線(DB)
            preview.enter(0) 'カレント X 位置をリセット

            '帳票出力 ----------------------------------------------------------------------
            Dim tmpTable1 As WinPDFPrint.Printing.Table = Notrice.SetTable(preview)
            Dim rows As Integer
            Dim Table As WinPDFPrint.Printing.Table

            '13件より多い場合は、概略表を出力し改ページ
            If tmpTable1.Rows > 13 Then
                Dim tmpTable2 As WinPDFPrint.Printing.Table = Notrice.SetSummaryTable(preview)
                preview.PrtText("水平震度と水平変位(概要)")
                preview.enter(1.2)
                rows = 2 + tmpTable2.Rows
                Table = SetNewTable(rows)
                For r = 3 To rows
                    If tmpTable2(r - 3, 1) = tmpTable2(r - 2, 1) Then
                        '同じステップにあったら
                        Table.SetHolLW(r - 1, 0)
                    Else
                        Table(r, 1) = tmpTable2(r - 2, 1)
                        Table(r, 2) = tmpTable2(r - 2, 2)
                        Table(r, 3) = tmpTable2(r - 2, 3)
                    End If
                    Table(r, 4) = " " + tmpTable2(r - 2, 4)
                    Table(r, 5) = tmpTable2(r - 2, 5)
                    Table.AlignX(r, 4) = "L"
                    Table.AlignX(r, 5) = "L"
                Next
                preview.PrintTable(Table)
                preview.NewPage()
            End If
            'テーブルの出力
            Dim page As Integer = common.RoundUp(tmpTable1.Rows / Me.printMaxRow, 0)
            For i = 1 To page
                preview.PrtText("水平震度と水平変位")
                preview.enter(1.2)

                Dim st As Integer = (i - 1) * Me.printMaxRow + 1
                Dim ed As Integer = st + Me.printMaxRow
                If ed > tmpTable1.Rows Then ed = tmpTable1.Rows

                rows = 2 + (ed - st) + 1 'tmpTable1.Rows
                Table = SetNewTable(rows)

                For r = 3 To rows
                    Dim tmp_r As Integer = st + r - 3
                    If tmpTable1.Rows < tmp_r Then Exit For
                    If Table.Rows < r Then Exit For

                    If r > 3 And tmpTable1(st + r - 4, 1) = tmpTable1(tmp_r, 1) Then
                        '同じステップにあったら
                        Table.SetHolLW(r - 1, 0)
                    Else
                        Table(r, 1) = tmpTable1(tmp_r, 1)
                        Table(r, 2) = tmpTable1(tmp_r, 2)
                        Table(r, 3) = tmpTable1(tmp_r, 3)
                    End If

                    Table(r, 4) = " " + tmpTable1(tmp_r, 4)
                    Table(r, 5) = tmpTable1(tmp_r, 5)
                    Table.AlignX(r, 4) = "L"
                    Table.AlignX(r, 5) = "L"
                Next
                preview.PrintTable(Table)

                If i < page Then preview.NewPage()
            Next

            'csv ファイルの出力 ----------------------------------------------------------------------
            If FormSettings.Option_各解析ケースの結果出力 = 1 Then
                CreateCsv(DB, tmpTable1)
            End If

        Catch ex As Exception
            Throw ex
        End Try

    End Sub

    Private Sub CreateCsv(dB As CalculationCase, tmpTable1 As Table)

        Try
            Dim Dict = New List(Of IntegerAndString)
            For row = 1 To tmpTable1.Rows
                Dim j As Integer = RoundUp(Convert.ToDouble(tmpTable1(row, 1)), 0)
                Dim t As String = tmpTable1(row, 4) + " " + tmpTable1(row, 5)
                t = t.Replace("{", "")
                t = t.Replace("}", "")
                t = t.Replace("_", "")
                t = t.Replace("(Khy+Khm)/2", "")
                Dict.Add(New IntegerAndString(j, t.Trim()))
            Next row

            Dim Filename As String = System.IO.Path.ChangeExtension(dB._SNAPDB.DataPath, ".csv")
            '保存するファイル内容の設定
            Dim Value As New List(Of String)
            Value.Add(String.Format("節点番号(変位量の着目点),{0}", dB.着目点))
            Value.Add("増分,水平震度,変位量,回転角,状態")
            Value.Add("ステップ,Kh,δ(mm),(‰rad),")
            With dB.応答値
                For i = 0 To .maxStep
                    Dim s As String = String.Format("{0},{1},{2},{3}", i, .震度(i), .変位量(i), .回転角(i))
                    Dim DelKey = New List(Of IntegerAndString)
                    For j = 0 To Dict.Count - 1
                        Dim k = Dict(j)
                        If k.key = i Then
                            s += "," + k.value
                            DelKey.Add(k)
                        End If
                    Next
                    Value.Add(s)
                    For Each k In DelKey
                        Dict.Remove(k)
                    Next
                Next i
            End With
            '書き込むファイルが既に存在している場合は、上書きする
            Dim sw As New System.IO.StreamWriter(Filename, False, System.Text.Encoding.GetEncoding("shift_jis"))
            'resultの内容を書き込む
            For Each s In Value
                sw.Write(s)
                sw.Write(vbLf)
            Next
            '閉じる
            sw.Close()
        Catch ex As Exception
        End Try

    End Sub

    Private Class IntegerAndString
        Public key As Integer
        Public value As String
        Sub New(_key As Integer, _value As String)
            Me.key = _key
            Me.value = _value
        End Sub

    End Class

    Private Function 荷重変位曲線(DB As CalculationCase) As NoticeInfoEX

        Dim result As New NoticeInfoEX

        Try
            'タイトルを書く ---------------------------------------------------------------------------
            Dim cPos As PointF = preview.GetCurrentPos()
            preview.SetCurrentPos(New PointF(cPos.X + 300, cPos.Y + 20))
            preview.PrtText(String.Format("節点番号(変位量の着目点)---- {0}", DB.着目点))
            preview.SetCurrentPos(New PointF(cPos.X, cPos.Y))
            preview.enter(1.2)

            '荷重～変位曲線を描く ---------------------------------------------------------------------
            Dim chart As New WinPDFPrint.Printing.Chart
            '荷重～変位曲線の座標データをセット
            Dim points(0 To DB.応答値.maxStep) As PointF           '荷重～変位曲線の座標データ
            With DB.応答値
                For i = 0 To .maxStep
                    points(i).Y = .震度(i)
                    points(i).X = .変位量(i)
                Next i
            End With

            '部材の降伏引き出し文字をセット ---------------------------------------------------------------------
            '部材の降伏判定
            With DB._SNAPDB
                Dim key() As Integer = Enumerable.Repeat(Of Integer)(1, .InputInfo.KihonInfo.MeNum + 1).ToArray
                For i As Integer = 0 To .maxStep
                    For j = 1 To .OutputInfo.StepCtrl(i).MemberItem.Count - 1
                        Dim m = .OutputInfo.StepCtrl(i).MemberItem(j)
                        If m.SCFlg > 5 Then Exit For
                        If m.SCFlg > key(j) Then
                            key(j) = m.SCFlg
                            result.Add(i, m.SCFlg, NoticeInfo.損傷ﾚﾍﾞﾙ, j, "部材状態の変化点", New PointF(DB.応答値.変位量(i), DB.応答値.震度(i)))
                        End If
                    Next
                Next i
            End With

            '直接基礎の安定計算引き出し文字をセット ---------------------------------------------------------------------
            With DB._SNAPDB
                Dim flg As Boolean = False
                For Each cf In DB._CFList
                    If cf._FoundationType <> 1 Then Continue For
                    If IsNothing(cf.変位照査節点番号List) Then Continue For
                    If cf.変位照査節点番号List.Count = 0 Then Continue For

                    '着目点に直接基礎の情報が入力されているか調べる
                    flg = False
                    Dim pNo = cf.変位照査節点番号List.First
                    For Each f In .OutputInfo.OutChokukiso
                        If pNo = f.JoNo Then
                            flg = True
                            Exit For
                        End If
                    Next
                    '着目点に直接基礎の情報が入力されていれば降伏震度を調べる
                    If flg = True Then
                        '0ステップから 安定ﾚﾍﾞﾙ１を超過するステップを探す
                        Dim L1Step As Integer = .maxStep
                        For istep As Integer = 0 To .maxStep
                            If cf.直接基礎L1鉛直支持力の検討(DB, istep, pNo).安全度 >= 1 Then
                                result.Add(istep, 1, NoticeInfo.直接安定ﾚﾍﾞﾙ, pNo, NoticeInfo.地盤崩壊, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                L1Step = istep
                                Exit For
                            End If
                            If cf.直接基礎L1水平支持力の検討(DB, istep, pNo).安全度 >= 1 Then
                                result.Add(istep, 1, NoticeInfo.直接安定ﾚﾍﾞﾙ, pNo, NoticeInfo.水平支持力, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                L1Step = istep
                                Exit For
                            End If
                            If cf.直接基礎L1残留傾斜の検討(DB, istep, pNo).安全度 >= 1 Then
                                result.Add(istep, 1, NoticeInfo.直接安定ﾚﾍﾞﾙ, pNo, NoticeInfo.残留傾斜, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                L1Step = istep
                                Exit For
                            End If
                        Next

                        '0ステップから安定ﾚﾍﾞﾙ２を超過するステップを探す
                        For istep As Integer = 0 To .maxStep
                            If istep >= L1Step Then
                                If cf.直接基礎復旧性2底面塑性化率の検討(DB, istep, pNo).安全度 >= 1 Then
                                    result.Add(istep, 2, NoticeInfo.直接安定ﾚﾍﾞﾙ, pNo, NoticeInfo.地盤崩壊, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                    Exit For
                                End If
                                If cf.直接基礎復旧性2回転角の検討(DB, istep, pNo).安全度 >= 1 Then
                                    result.Add(istep, 2, NoticeInfo.直接安定ﾚﾍﾞﾙ, pNo, NoticeInfo.回転安定, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                    Exit For
                                End If
                            End If
                            If cf.直接基礎復旧性2水平支持力の検討(DB, istep, pNo).安全度 >= 1 Then
                                result.Add(istep, 2, NoticeInfo.直接安定ﾚﾍﾞﾙ, pNo, NoticeInfo.水平支持力, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                Exit For
                            End If
                        Next

                        '0ステップから安定ﾚﾍﾞﾙ３を超過するステップを探す
                        For istep As Integer = 0 To .maxStep
                            If istep >= L1Step Then
                                If cf.直接基礎安全性底面塑性化率の検討(DB, istep, pNo).安全度 >= 1 Then
                                    result.Add(istep, 3, NoticeInfo.直接安定ﾚﾍﾞﾙ, pNo, NoticeInfo.地盤崩壊, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                    Exit For
                                End If
                                If cf.直接基礎安全性回転角の検討(DB, istep, pNo).安全度 >= 1 Then
                                    result.Add(istep, 3, NoticeInfo.直接安定ﾚﾍﾞﾙ, pNo, NoticeInfo.回転安定, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                    Exit For
                                End If
                            End If
                            If cf.直接基礎安全性水平支持力の検討(DB, istep, pNo).安全度 >= 1 Then
                                result.Add(istep, 3, NoticeInfo.直接安定ﾚﾍﾞﾙ, pNo, NoticeInfo.水平支持力, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                Exit For
                            End If
                        Next
                    End If
                Next cf
            End With

            '杭基礎の安定計算引き出し文字をセット ---------------------------------------------------------------------
            With DB._SNAPDB
                For Each cf In DB._CFList
                    If cf._FoundationType <> 0 Then Continue For

                    If IsNothing(cf.反力照査要素番号List) = False Then
                        '杭鉛直力に関する照査
                        '0ステップから 安定ﾚﾍﾞﾙ１を超過するステップを探す
                        For k = 0 To cf.反力照査要素番号List.Count - 1
                            Dim mNo As Integer = cf.反力照査要素番号List(k)
                            Dim ALF As Double = cf.反力照査部材数List(k)
                            If mNo < 0 Then Exit For
                            '0ステップから 安定ﾚﾍﾞﾙ１を超過するステップを探す
                            For istep As Integer = 0 To .maxStep
                                If cf.杭基礎L1鉛直支持力の検討(DB, istep, mNo, ALF).安全度 > 1 Then
                                    result.Add(istep, 1, NoticeInfo.杭安定ﾚﾍﾞﾙ, mNo, NoticeInfo.鉛直支持力, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                    Exit For
                                End If
                                If cf.杭基礎L1引抜き抵抗力の照査(DB, istep, mNo, ALF).安全度 > 1 Then
                                    result.Add(istep, 1, NoticeInfo.杭安定ﾚﾍﾞﾙ, mNo, NoticeInfo.引抜き抵抗力, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                    Exit For
                                End If
                            Next
                            '0ステップから 安定ﾚﾍﾞﾙ２を超過するステップを探す
                            For istep As Integer = 0 To .maxStep
                                If cf.杭基礎復旧性2鉛直支持の照査(DB, istep, mNo, ALF).安全度 > 1 Then
                                    result.Add(istep, 2, NoticeInfo.杭安定ﾚﾍﾞﾙ, mNo, NoticeInfo.鉛直支持力, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                    Exit For
                                End If
                            Next
                            '0ステップから 安定ﾚﾍﾞﾙ３を超過するステップを探す
                            For istep As Integer = 0 To .maxStep
                                If cf.杭基礎安全性鉛直支持の照査(DB, istep, mNo, ALF).安全度 > 1 Then
                                    result.Add(istep, 3, NoticeInfo.杭安定ﾚﾍﾞﾙ, mNo, NoticeInfo.鉛直支持力, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                    Exit For
                                End If
                            Next
                        Next '反力照査要素番号

                        '変位に関する照査
                        For k = 0 To cf.変位照査節点番号List.Count - 1
                            Dim pNo As Integer = cf.変位照査節点番号List(k)
                            Dim _Direction As Double = cf.変位照査距離List(k)
                            '0ステップから 安定ﾚﾍﾞﾙ１を超過するステップを探す
                            For istep As Integer = 0 To .maxStep
                                If pNo < 0 Then Exit For
                                If cf.杭基礎L1水平変位の照査(DB, istep, pNo, _Direction).安全度 > 1 Then
                                    result.Add(istep, 1, NoticeInfo.杭安定ﾚﾍﾞﾙ, pNo, NoticeInfo.水平変位, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                    Exit For
                                End If
                                If cf.杭基礎L1回転角の照査(DB, istep, pNo).安全度 > 1 Then
                                    result.Add(istep, 1, NoticeInfo.杭安定ﾚﾍﾞﾙ, pNo, NoticeInfo.回転安定, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                    Exit For
                                End If
                            Next
                            '0ステップから 安定ﾚﾍﾞﾙ２を超過するステップを探す
                            For istep As Integer = 0 To .maxStep
                                If pNo < 0 Then Exit For
                                If cf.杭基礎復旧性2水平変位の照査(DB, istep, pNo, _Direction).安全度 > 1 Then
                                    result.Add(istep, 2, NoticeInfo.杭安定ﾚﾍﾞﾙ, pNo, NoticeInfo.水平変位, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                    Exit For
                                End If
                                If cf.杭基礎復旧性2回転角の照査(DB, istep, pNo).安全度 > 1 Then
                                    result.Add(istep, 2, NoticeInfo.杭安定ﾚﾍﾞﾙ, pNo, NoticeInfo.回転安定, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                    Exit For
                                End If
                            Next
                            '0ステップから 安定ﾚﾍﾞﾙ３を超過するステップを探す
                            For istep As Integer = 0 To .maxStep
                                If pNo < 0 Then Exit For
                                If cf.杭基礎安全性水平変位の照査(DB, istep, pNo, _Direction).安全度 > 1 Then
                                    result.Add(istep, 3, NoticeInfo.杭安定ﾚﾍﾞﾙ, pNo, NoticeInfo.水平変位, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                    Exit For
                                End If
                                If cf.杭基礎安全性回転角の照査(DB, istep, pNo).安全度 > 1 Then
                                    result.Add(istep, 3, NoticeInfo.杭安定ﾚﾍﾞﾙ, pNo, NoticeInfo.回転安定, New PointF(DB.応答値.変位量(istep), DB.応答値.震度(istep)))
                                    Exit For
                                End If
                            Next
                        Next ' 変位照査節点番号
                    End If
                Next
            End With

            '初期降伏点, 最大点など応答値を求める際に必要なポイントを引き出しに追加する。------------------------------------------
            With DB.応答値
                If DB._SNAPDB.InputInfo.KihonInfo.KaisekiFlg = 1 Then
                    '初期降伏点
                    If result.Data.ContainsKey(.初期降伏step) Then
                        Dim d = result.Data(.初期降伏step)
                        d.s備考 = ":Kh_{y}"
                    Else
                        result.Add(.初期降伏step, 0, "Kh_{y}", Nothing, "初期降伏点", New PointF(DB.応答値.変位量(.初期降伏step), DB.応答値.震度(.初期降伏step)))
                    End If
                    '最大震度点
                    If .pKhbStep > 0 Then
                        If result.Data.ContainsKey(.最大震度step) Then
                            Dim d = result.Data(.最大震度step)
                            d.s備考 = ":Kh_{m}"
                        Else
                            result.Add(.最大震度step, 0, "Kh_{m}", Nothing, "最大震度", New PointF(DB.応答値.変位量(.最大震度step), DB.応答値.震度(.最大震度step)))
                        End If
                    End If
                End If
            End With
            Dim NoticeStr = New Dictionary(Of Integer, String)()    '引き出し文字

            For Each NL In result.Data
                NoticeStr.Add(NL.Key, NL.Value.GetComment())
            Next

            '引き出し文字のを登録する。-------------------------------------------------------------------------------------------------
            If result.Data.Count > Me.printMaxPoint Then 'データ数が非常に多い場合 簡略モードとする
                Dim NoticeStrEx = New Dictionary(Of Integer, String)()
                '引き出し文字
                Dim tmpTable2 As WinPDFPrint.Printing.Table = result.SetSummaryTable(preview)
                For r = 1 To tmpTable2.Rows
                    Dim key As Integer = Val(tmpTable2(r, 1))
                    Dim value As String = tmpTable2(r, 4)
                    If NoticeStrEx.ContainsKey(key) = True Then
                        NoticeStrEx(key) += value
                    Else
                        NoticeStrEx.Add(key, value)
                    End If
                Next
                '荷重変位曲線上の印
                For Each NL In result.Data
                    If NoticeStrEx.ContainsKey(-NL.Key) = True Then
                        '何も描かない
                    Else
                        NoticeStrEx.Add(-NL.Key, NL.Value.GetComment(True)) '荷重～変位曲線の上に描く印はステップにマイナスを付ける
                    End If
                Next
                Call chart.AddData(points, NoticeStrEx)
            Else
                Call chart.AddData(points, NoticeStr)
            End If

            'その他任意の位置の引き出し文字を登録する。------------------------------------------------------------------------------
            If FormSettings.Option_初期降伏震度を用いた応答変位 = 0 Then
                With DB.応答値
                    If .pKhbStep > 0 Then
                        '(Khy+Khm)/2 の点
                        result.Add(.pKhbStep, 0, "(Kh_{y}+Kh_{m})/2", Nothing, "", New PointF(.pKhb.x, .pKhb.y))
                        Call chart.AddDataEx(New PointF(.pKhb.x, .pKhb.y), .pKhbStep, "(Kh_{y}+Kh_{m})/2")
                    End If
                End With
            End If

            '補助線を引く ---------------------------------------------------------------------
            Dim p1 As New PointF
            Dim p2 As New PointF
            Dim strX As String = ""
            Dim strY As String = ""

            If DB._SNAPDB.InputInfo.KihonInfo.KaisekiFlg = 1 _
                AndAlso FormSettings.Option_初期降伏震度を用いた応答変位 = 0 Then

                '全体系折曲点震度
                chart.SeparateYRiseNoticeLine = DB.応答値.pKhb.y '.全体系折曲点震度
                '0～Khq
                p1.Y = DB.応答値.震度(0)
                p1.X = DB.応答値.変位量(0)
                p2.Y = DB.応答値.全体系折曲点震度
                p2.X = DB.応答値.降伏変位
                Call chart.AddLine(p1, p2, "", "", False)
                'Kheq ─
                p1.Y = DB.応答値.全体系折曲点震度
                p1.X = 0
                p2.Y = p1.Y
                p2.X = DB.応答値.降伏変位
                strY = "Kh_{eq}=" + p1.Y.ToString("0.000")
                Call chart.AddLine(p1, p2, strY, "", True)
                'δeq │
                p1.Y = DB.応答値.全体系折曲点震度
                p1.X = DB.応答値.降伏変位
                p2.Y = 0
                p2.X = p1.X
                strX = "δ_{eq}=" + p1.X.ToString("0.0")
                Call chart.AddLine(p1, p2, "", strX, False)
                If DB.応答値.pKhbStep > 0 Then
                    'khy～Khq
                    p1.Y = DB.応答値.全体系折曲点震度
                    p1.X = DB.応答値.降伏変位
                    p2.Y = DB.応答値.震度(DB.応答値.最大震度step)
                    p2.X = DB.応答値.変位量(DB.応答値.最大震度step)
                    Call chart.AddLine(p1, p2, "", "", False)

                End If
            Else
                If Not DB.応答値.pKhb Is Nothing Then
                    chart.SeparateYRiseNoticeLine = DB.応答値.pKhb.y '.全体系折曲点震度
                Else
                    chart.SeparateYRiseNoticeLine = DB.応答値.最大震度 / 2
                End If
                If DB.応答値.初期降伏震度 > 0 Then
                    'Kheq ─
                    p1.Y = DB.応答値.初期降伏震度
                    p1.X = 0
                    p2.Y = p1.Y
                    p2.X = DB.応答値.降伏変位
                    strY = "Kh_{eq}=" + p1.Y.ToString("0.000")
                    Call chart.AddLine(p1, p2, strY, "", True)
                    'δeq │
                    p1.Y = DB.応答値.初期降伏震度
                    p1.X = DB.応答値.降伏変位
                    p2.Y = 0
                    p2.X = p1.X
                    strX = "δ_{eq}=" + p1.X.ToString("0.0")
                    Call chart.AddLine(p1, p2, "", strX, False)
                End If
            End If

            If DB._解析対象(5) = True Then
                'L1震度点
                If DB.応答値.復旧性_L1震度 >= 0 Then
                    p1.Y = DB.応答値.復旧性_L1震度
                    Dim uncloss As Boolean
                    If DB.応答値.復旧性_L1震度step > 0 Then
                        p1.X = DB.応答値.変位量(DB.応答値.復旧性_L1震度step)
                        uncloss = True
                    Else
                        p1.X = DB.応答値.変位量(DB.応答値.maxStep)
                        uncloss = False
                    End If
                    p2.Y = p1.Y
                    p2.X = 0
                    strY = "Kh_{L1}=" + p1.Y.ToString("0.000")
                    Call chart.AddLine(p1, p2, strY, "", uncloss)
                    result.Add(DB.応答値.復旧性_L1震度stepF, 0, "Kh_{L1}", Nothing, "復旧性L1震度", p1)
                End If
            End If
            If DB._解析対象(0) = True Then
                '復旧性応答値
                p1.Y = DB.応答値.震度(DB.応答値.復旧性_最大応答step)
                p1.X = DB.応答値.復旧性_最大応答変位
                p2.Y = 0
                p2.X = p1.X
                strX = "δ_{r}=" + p1.X.ToString("0.0")
                strX += ":復旧性"
                Call chart.AddLine(p1, p2, "", strX, False, 10)
                result.Add(DB.応答値.復旧性_最大応答step, 0, "δ_{r}", Nothing, "復旧性の応答値", p1)
            End If
            If DB._解析対象(1) = True Then
                '安全性応答値
                p1.Y = DB.応答値.震度(DB.応答値.安全性_最大応答step)
                p1.X = DB.応答値.安全性_最大応答変位
                p2.Y = 0
                p2.X = p1.X
                strX = "δ_{r}=" + p1.X.ToString("0.0")
                strX += ":安全性"
                Call chart.AddLine(p1, p2, "", strX, False, 20)
                result.Add(DB.応答値.安全性_最大応答step, 0, "δ_{r}", Nothing, "安全性の応答値", p1)
            End If

            ' エネルギー一定測の線を追加
            If DB.応答値.安全性_線形最大震度 > 0 Then
                'Khmax ─
                p1.Y = DB.応答値.安全性_線形最大震度
                p1.X = 0
                p2.Y = p1.Y
                strY = "Kh_{max}=" + p2.Y.ToString("0.000")
                If DB.応答値.全体系折曲点震度 > DB.応答値.安全性_線形最大震度 Then
                    p2.X = Math.Max(DB.応答値.安全性_C点, DB.応答値.安全性_最大応答変位)
                    Call chart.AddLine(p1, p2, strY, "", True)
                Else
                    p2.X = DB.応答値.安全性_C点
                    Call chart.AddLine(p1, p2, strY, "", True)
                    'Khq～Khmax
                    p1.Y = DB.応答値.全体系折曲点震度
                    p1.X = DB.応答値.降伏変位
                    p2.Y = DB.応答値.安全性_線形最大震度
                    p2.X = DB.応答値.安全性_C点
                    Call chart.AddLine(p1, p2, "", "", False)
                End If
            End If
            If DB.応答値.安全性_C点 > 0 Then
                'δmax │
                p1.Y = DB.応答値.安全性_線形最大震度
                p1.X = DB.応答値.安全性_C点
                p2.Y = 0
                p2.X = p1.X
                Call chart.AddLine(p1, p2, "", "", True)
                'Kheq ─ C点
                p1.Y = DB.応答値.全体系折曲点震度
                p1.X = DB.応答値.降伏変位
                p2.Y = p1.Y
                p2.X = DB.応答値.安全性_最大応答変位
                Call chart.AddLine(p1, p2, "", "", True)
            End If

            'プリント -------------------------------------------------------------------------------------------------
            Call preview.PrintChart(chart)


            '作用軸力が適用範囲外文字をセット ------------------------------------------------------------------------------
            For Each t In DB.応答値.軸圧縮力適用範囲外List
                result.Add(t.iStep, -1, "", Nothing, String.Format("要素番号{0}の作用軸力が適用範囲外", t.ID), New PointF(DB.応答値.変位量(t.iStep), DB.応答値.震度(t.iStep)))
            Next

            '部材のせん断降伏引き出し文字をセット（オプション機能）-------------------------------------------------------------------------------------------------
            If FormSettings.Option_せん断降伏点の出力 = 1 Then

                With DB._SNAPDB
                    Dim keys1 As List(Of Integer) = Enumerable.Range(1, .InputInfo.KihonInfo.MeNum).ToList()

                    For i As Integer = 0 To .maxStep
                        Dim keys2 = New List(Of Integer)
                        For Each j In keys1
                            'For j = 1 To .OutputInfo.StepCtrl(i).MemberItem.Count - 1
                            Dim m = .OutputInfo.StepCtrl(i).MemberItem(j)
                            Select Case m.Vshousa
                                Case 1
                                    result.Add(i, -1, String.Format("●-{0}", j), Nothing, String.Format("要素番号{0}がせん断耐力を超過した", j), New PointF(DB.応答値.変位量(i), DB.応答値.震度(i)))
                                    keys2.Add(j)
                                Case Is < 0
                                    keys2.Add(j)
                            End Select
                        Next
                        For Each j In keys2
                            keys1.Remove(j)
                        Next
                    Next i
                End With
            End If


            Return result
        Catch ex As Exception
            Throw ex
        End Try
    End Function

    Private Function SetNewTable(rows As Integer) As WinPDFPrint.Printing.Table
        Dim Table As New WinPDFPrint.Printing.Table(rows, 5)
        Table(1, 1) = "増分"
        Table(2, 1) = "ステップ"
        Table(1, 2) = "水平震度"
        Table(2, 2) = "Kh"
        Table(1, 3) = "変位量"
        Table(2, 3) = "δ(mm)"
        Table(1, 4) = ""
        Table(2, 4) = ""
        Table(1, 5) = "状　　　　態"
        Table(2, 5) = ""
        Table.AlignX(1, 5) = "L"
        Table.ColWidth(1) = 50
        Table.ColWidth(2) = 50
        Table.ColWidth(3) = 50
        Table.ColWidth(4) = 70
        Table.ColWidth(5) = 260
        Table.SetHolLW(1, 0)
        Table.SetVtcLW(4, 0)
        Return Table
    End Function

    Friend Class NoticeInfoEX

        Private 印字幅Limit As Single = 65

        Public Data As New SortedList(Of Single, NoticeInfo)

        Sub Add(_istep As Integer, _i状態 As Integer, _s区分 As String, _i対象 As Integer, _s対象 As String, _pointF As PointF)
            Call Add(CSng(_istep), _i状態, _s区分, _i対象, _s対象, _pointF)
        End Sub

        Sub Add(_istep As Single, _i状態 As Integer, _s区分 As String, _i対象 As Integer, _s対象 As String, _pointF As PointF)
            Dim t As New NoticeInfo()
            '同じステップにデータがあったら
            If Data.ContainsKey(_istep) Then
                t = Data(_istep)
                t.Add(_i状態, _s区分, _i対象, _s対象, _pointF)
            Else
                t.Add(_i状態, _s区分, _i対象, _s対象, _pointF)
                Data.Add(_istep, t)
            End If
        End Sub

        Function SetTable(preview As WinPDFPrint.PrintPreview, Optional GetRows As Boolean = False) As WinPDFPrint.Printing.Table

            Dim Table As New WinPDFPrint.Printing.Table
            If GetRows = False Then
                Dim rows As Integer = SetTable(preview, True).Rows
                Table = New WinPDFPrint.Printing.Table(rows, 5)
            End If
            Dim r As Integer = 1

            For Each d In Data
                '部材の損傷状態の変化点
                For i = 0 To 2 '→ 損傷ﾚﾍﾞﾙ 2～4
                    Dim str1 As String = d.Value.Get損傷Comment(i)
                    Dim str2 As String = d.Value.Get損傷番号(i, False)
                    Dim str3 As String = str1 + d.Value.s備考

                    If str1 <> "" Then
                        If GetPrintingWidth(preview, str3) < 印字幅Limit Then
                            If GetRows = False Then
                                Table(r, 1) = d.Key.ToString()
                                Table(r, 2) = d.Value.損傷番号(i).First.pPointF.Y.ToString("0.000")
                                Table(r, 3) = d.Value.損傷番号(i).First.pPointF.X.ToString("0.0")
                                Table(r, 4) = str3 'str1 + d.Value.s備考
                                Table(r, 5) = String.Format("要素番号{0}が損傷レベル{1}に達した", str2, i + 2)
                            End If
                            r += 1
                        Else
                            If GetRows = False Then
                                Table(r, 1) = d.Key.ToString()
                                Table(r, 2) = d.Value.損傷番号(i).First.pPointF.Y.ToString("0.000")
                                Table(r, 3) = d.Value.損傷番号(i).First.pPointF.X.ToString("0.0")
                                Table(r, 4) = str3 'str1 + d.Value.s備考
                                Table(r, 5) = ""
                                Table(r + 1, 1) = Table(r, 1)
                                Table(r + 1, 4) = ""
                                Table(r + 1, 5) = String.Format("要素番号{0}が損傷レベル{1}に達した", str2, i + 2)
                            End If
                            r += 2
                        End If
                    End If
                Next
                '直接基礎の損傷状態の変化点
                For i = 0 To 2 '→ 安定ﾚﾍﾞﾙ 1～3
                    Dim str1 As String = d.Value.Get直接安定Comment(i)
                    Dim str2 As String = d.Value.Get直接安定番号(i, False)
                    Dim str3 As String = str1 + d.Value.s備考
                    If str1 <> "" Then
                        If GetPrintingWidth(preview, str3) < 印字幅Limit Then
                            If GetRows = False Then
                                Table(r, 1) = d.Key.ToString()
                                Table(r, 2) = d.Value.直接安定番号(i).First.pPointF.Y.ToString("0.000")
                                Table(r, 3) = d.Value.直接安定番号(i).First.pPointF.X.ToString("0.0")
                                Table(r, 4) = str3
                                Table(r, 5) = String.Format("節点番号{0}を有する基礎が安定レベル{1}の制限値に達した", str2, i + 1)
                            End If
                            r += 1
                        Else
                            If GetRows = False Then
                                Table(r, 1) = d.Key.ToString()
                                Table(r, 2) = d.Value.直接安定番号(i).First.pPointF.Y.ToString("0.000")
                                Table(r, 3) = d.Value.直接安定番号(i).First.pPointF.X.ToString("0.0")
                                Table(r, 4) = str3
                                Table(r, 5) = ""
                                Table(r + 1, 1) = Table(r, 1)
                                Table(r + 1, 4) = ""
                                Table(r + 1, 5) = String.Format("節点番号{0}を有する基礎が安定レベル{1}の制限値に達した", str2, i + 1)
                            End If
                            r += 2
                        End If
                    End If
                Next
                '杭基礎の損傷状態の変化点
                For i = 0 To 2 '→ 安定ﾚﾍﾞﾙ 1～3
                    Dim str1 As String = d.Value.Get杭安定Comment(i)
                    Dim str2 As String = d.Value.Get杭安定番号(i, False)
                    Dim str3 As String = str1 + d.Value.s備考
                    Dim str4 As String = ""
                    If GetRows = False Then
                        If InStr(str1, NoticeInfo.回転安定) Then
                            str4 = String.Format("節点番号{0}の傾斜角が安定レベル{1}に達した", str2, i + 1)
                        ElseIf InStr(str1, NoticeInfo.水平変位) Then
                            str4 = String.Format("節点番号{0}の水平変位が安定レベル{1}に達した", str2, i + 1)
                        ElseIf InStr(str1, NoticeInfo.引抜き抵抗力) Then
                            str4 = String.Format("要素番号{0}を有する杭が安定レベル{1}に達した", str2, i + 1)
                        ElseIf InStr(str1, NoticeInfo.鉛直支持力) Then
                            str4 = String.Format("要素番号{0}を有する杭が安定レベル{1}に達した", str2, i + 1)
                        End If
                    End If

                    If str1 <> "" Then
                        If GetPrintingWidth(preview, str3) < 印字幅Limit Then
                            If GetRows = False Then
                                Table(r, 1) = d.Key.ToString()
                                Table(r, 2) = d.Value.杭安定番号(i).First.pPointF.Y.ToString("0.000")
                                Table(r, 3) = d.Value.杭安定番号(i).First.pPointF.X.ToString("0.0")
                                Table(r, 4) = str3
                                Table(r, 5) = str4
                            End If
                            r += 1
                        Else
                            If GetRows = False Then
                                Table(r, 1) = d.Key.ToString()
                                Table(r, 2) = d.Value.杭安定番号(i).First.pPointF.Y.ToString("0.000")
                                Table(r, 3) = d.Value.杭安定番号(i).First.pPointF.X.ToString("0.0")
                                Table(r, 4) = str3
                                Table(r, 5) = ""
                                Table(r + 1, 1) = Table(r, 1)
                                Table(r + 1, 4) = ""
                                Table(r + 1, 5) = str4
                            End If
                            r += 2
                        End If
                    End If
                Next

                For Each n In d.Value.その他
                    If GetPrintingWidth(preview, n.s説明1) < 印字幅Limit Then
                        If GetRows = False Then
                            '小数点があれば 0.0 と表示する。
                            Dim num_int = Int(d.Key)
                            Dim num_dec = (d.Key - num_int) * 10
                            If num_dec > 0 Then
                                Table(r, 1) = d.Key.ToString("0.0")
                            Else
                                Table(r, 1) = d.Key.ToString("0")
                            End If
                            Table(r, 2) = n.pPointF.Y.ToString("0.000")
                            Table(r, 3) = n.pPointF.X.ToString("0.0")
                            Table(r, 4) = n.s説明1
                            Table(r, 5) = n.s説明2
                        End If
                        r += 1
                    Else
                        If GetRows = False Then
                            '小数点があれば 0.0 と表示する。
                            Dim num_int = Int(d.Key)
                            Dim num_dec = (d.Key - num_int) * 10
                            If num_dec > 0 Then
                                Table(r, 1) = d.Key.ToString("0.0")
                            Else
                                Table(r, 1) = d.Key.ToString("0")
                            End If
                            Table(r, 2) = n.pPointF.Y.ToString("0.000")
                            Table(r, 3) = n.pPointF.X.ToString("0.0")
                            Table(r, 4) = n.s説明1
                            Table(r, 5) = ""
                            Table(r + 1, 1) = Table(r, 1)
                            Table(r + 1, 4) = ""
                            Table(r + 1, 5) = n.s説明2
                        End If
                        r += 2
                    End If
                Next
            Next
            If GetRows = False Then
                Return Table
            Else
                r -= 1
                Return New WinPDFPrint.Printing.Table(r, 4)
            End If

        End Function

        ''' <summary>概要表なので、各イベントの最初だけ表示する </summary>
        Function SetSummaryTable(preview As WinPDFPrint.PrintPreview, Optional GetRows As Boolean = False) As WinPDFPrint.Printing.Table

            Dim Table As New WinPDFPrint.Printing.Table
            If GetRows = False Then
                Dim rows As Integer = SetSummaryTable(preview, True).Rows
                Table = New WinPDFPrint.Printing.Table(rows, 5)
            End If
            Dim r As Integer = 1

            '各イベントの最初だけ表示するためのindex
            Dim i部材 As Integer = 0
            Dim i直接基礎 As Integer = 0
            Dim i杭基礎 As Integer = 0

            For Each d In Data
                '部材の損傷状態の変化点
                For i = i部材 To 2 '→ 損傷ﾚﾍﾞﾙ 2～4
                    Dim str1 As String = d.Value.Get損傷Comment(i)
                    Dim str2 As String = d.Value.Get損傷番号(i, False)
                    Dim str3 As String = str1 + d.Value.s備考

                    If str1 <> "" Then
                        If GetPrintingWidth(preview, str3) < 印字幅Limit Then
                            If GetRows = False Then
                                Table(r, 1) = d.Key.ToString()
                                Table(r, 2) = d.Value.損傷番号(i).First.pPointF.Y.ToString("0.000")
                                Table(r, 3) = d.Value.損傷番号(i).First.pPointF.X.ToString("0.0")
                                Table(r, 4) = str3
                                Table(r, 5) = String.Format("要素番号{0}が損傷レベル{1}に達した", str2, i + 2)
                            End If
                            i部材 = i + 1
                            r += 1
                        Else
                            If GetRows = False Then
                                Table(r, 1) = d.Key.ToString()
                                Table(r, 2) = d.Value.損傷番号(i).First.pPointF.Y.ToString("0.000")
                                Table(r, 3) = d.Value.損傷番号(i).First.pPointF.X.ToString("0.0")
                                Table(r, 4) = str3
                                Table(r, 5) = ""
                                Table(r + 1, 1) = Table(r, 1)
                                Table(r + 1, 4) = ""
                                Table(r + 1, 5) = String.Format("要素番号{0}が損傷レベル{1}に達した", str2, i + 2)
                            End If
                            i部材 = i + 1
                            r += 2
                        End If
                    End If
                Next

                '直接基礎の損傷状態の変化点
                For i = i直接基礎 To 2 '→ 安定ﾚﾍﾞﾙ 1～3
                    Dim str1 As String = d.Value.Get直接安定Comment(i)
                    Dim str2 As String = d.Value.Get直接安定番号(i, False)
                    Dim str3 As String = str1 + d.Value.s備考
                    If str1 <> "" Then
                        If GetPrintingWidth(preview, str3) < 印字幅Limit Then
                            If GetRows = False Then
                                Table(r, 1) = d.Key.ToString()
                                Table(r, 2) = d.Value.直接安定番号(i).First.pPointF.Y.ToString("0.000")
                                Table(r, 3) = d.Value.直接安定番号(i).First.pPointF.X.ToString("0.0")
                                Table(r, 4) = str3
                                Table(r, 5) = String.Format("節点番号{0}を有する基礎が安定レベル{1}の制限値に達した", str2, i + 1)
                            End If
                            i直接基礎 = i + 1
                            r += 1
                        Else
                            If GetRows = False Then
                                Table(r, 1) = d.Key.ToString()
                                Table(r, 2) = d.Value.直接安定番号(i).First.pPointF.Y.ToString("0.000")
                                Table(r, 3) = d.Value.直接安定番号(i).First.pPointF.X.ToString("0.0")
                                Table(r, 4) = str3
                                Table(r, 5) = ""
                                Table(r + 1, 1) = Table(r, 1)
                                Table(r + 1, 4) = ""
                                Table(r + 1, 5) = String.Format("節点番号{0}を有する基礎が安定レベル{1}の制限値に達した", str2, i + 1)
                            End If
                            i直接基礎 = i + 1
                            r += 2
                        End If
                    End If

                Next
                '杭基礎の損傷状態の変化点
                For i = i杭基礎 To 2 '→ 安定ﾚﾍﾞﾙ 1～3
                    Dim str1 As String = d.Value.Get杭安定Comment(i)
                    Dim str2 As String = d.Value.Get杭安定番号(i, False)
                    Dim str3 As String = str1 + d.Value.s備考
                    Dim str4 As String = ""
                    If GetRows = False Then
                        If InStr(str1, NoticeInfo.回転安定) Then
                            str4 = String.Format("節点番号{0}の傾斜角が安定レベル{1}に達した", str2, i + 1)
                        ElseIf InStr(str1, NoticeInfo.水平変位) Then
                            str4 = String.Format("節点番号{0}の水平変位が安定レベル{1}に達した", str2, i + 1)
                        ElseIf InStr(str1, NoticeInfo.引抜き抵抗力) Then
                            str4 = String.Format("要素番号{0}を有する杭が安定レベル{1}に達した", str2, i + 1)
                        ElseIf InStr(str1, NoticeInfo.鉛直支持力) Then
                            str4 = String.Format("要素番号{0}を有する杭が安定レベル{1}に達した", str2, i + 1)
                        End If
                    End If

                    If str1 <> "" Then
                        If GetPrintingWidth(preview, str3) < 印字幅Limit Then
                            If GetRows = False Then
                                Table(r, 1) = d.Key.ToString()
                                Table(r, 2) = d.Value.杭安定番号(i).First.pPointF.Y.ToString("0.000")
                                Table(r, 3) = d.Value.杭安定番号(i).First.pPointF.X.ToString("0.0")
                                Table(r, 4) = str3
                                Table(r, 5) = str4
                            End If
                            i杭基礎 = i + 1
                            r += 1
                        Else
                            If GetRows = False Then
                                Table(r, 1) = d.Key.ToString()
                                Table(r, 2) = d.Value.杭安定番号(i).First.pPointF.Y.ToString("0.000")
                                Table(r, 3) = d.Value.杭安定番号(i).First.pPointF.X.ToString("0.0")
                                Table(r, 4) = str3
                                Table(r, 5) = ""
                                Table(r + 1, 1) = Table(r, 1)
                                Table(r + 1, 4) = ""
                                Table(r + 1, 5) = str4
                            End If
                            i杭基礎 = i + 1
                            r += 2
                        End If
                    End If
                Next
                For Each n In d.Value.その他
                    If GetPrintingWidth(preview, n.s説明1) < 印字幅Limit Then
                        If GetRows = False Then
                            '小数点があれば 0.0 と表示する。
                            Dim num_int = Int(d.Key)
                            Dim num_dec = (d.Key - num_int) * 10
                            If num_dec > 0 Then
                                Table(r, 1) = d.Key.ToString("0.0")
                            Else
                                Table(r, 1) = d.Key.ToString("0")
                            End If
                            Table(r, 2) = n.pPointF.Y.ToString("0.000")
                            Table(r, 3) = n.pPointF.X.ToString("0.0")
                            Table(r, 4) = n.s説明1
                            Table(r, 5) = n.s説明2
                        End If
                        r += 1
                    Else
                        If GetRows = False Then
                            '小数点があれば 0.0 と表示する。
                            Dim num_int = Int(d.Key)
                            Dim num_dec = (d.Key - num_int) * 10
                            If num_dec > 0 Then
                                Table(r, 1) = d.Key.ToString("0.0")
                            Else
                                Table(r, 1) = d.Key.ToString("0")
                            End If
                            Table(r, 2) = n.pPointF.Y.ToString("0.000")
                            Table(r, 3) = n.pPointF.X.ToString("0.0")
                            Table(r, 4) = n.s説明1
                            Table(r, 5) = ""
                            Table(r + 1, 1) = Table(r, 1)
                            Table(r + 1, 4) = ""
                            Table(r + 1, 5) = n.s説明2
                        End If
                        r += 2
                    End If
                Next
            Next
            If GetRows = False Then
                Return Table
            Else
                r -= 1
                Return New WinPDFPrint.Printing.Table(r, 4)
            End If

        End Function

        Private Function GetPrintingWidth(preview As WinPDFPrint.PrintPreview, printStr As String) As Single
            Dim s = preview.GetPrintingTxtSize(printStr)
            Return s.Width
        End Function

    End Class

    Friend Class NoticeInfo

        Public Const 損傷ﾚﾍﾞﾙ As String = "損傷ﾚﾍﾞﾙ"
        Public Const 直接安定ﾚﾍﾞﾙ As String = "直接基礎の安定ﾚﾍﾞﾙ"
        Public Const 杭安定ﾚﾍﾞﾙ As String = "杭の安定ﾚﾍﾞﾙ"

        '基礎の状態と荷重～変位曲線に印字される記号
        Public Const 鉛直支持力 As String = "△"
        Public Const 引抜き抵抗力 As String = "▲"
        Public Const 水平変位 As String = "□"
        Public Const 回転安定 As String = "■"

        Public Const 地盤崩壊 As String = "△"
        Public Const 水平支持力 As String = "□"
        Public Const 残留傾斜 As String = "■"

        Public 損傷番号(0 To 2) As List(Of BaseInfo) '→ 損傷ﾚﾍﾞﾙ 2～4
        Public 直接安定番号(0 To 2) As List(Of BaseInfo)  '→ 安定ﾚﾍﾞﾙ 1～3
        Public 杭安定番号(0 To 2) As List(Of BaseInfo)  '→ 安定ﾚﾍﾞﾙ 1～3
        Public その他 As List(Of ActionInfo)

        Public s備考 As String

        Sub New()
            For i = 0 To 2
                損傷番号(i) = New List(Of BaseInfo)
                直接安定番号(i) = New List(Of BaseInfo)
                杭安定番号(i) = New List(Of BaseInfo)
            Next
            その他 = New List(Of ActionInfo)
            s備考 = ""
        End Sub
        ''' <summary></summary>
        ''' <param name="_i状態">(レベル)1 とか 2 とか</param>
        ''' <param name="_s区分">"損傷ﾚﾍﾞﾙ" とか "安定ﾚﾍﾞﾙ" とか</param>
        ''' <param name="_i対象">部材番号 とか 基礎の着目点番号 とか</param>
        ''' <param name="_s対象">"部材" とか "杭基礎" とか</param>
        ''' <remarks></remarks>
        Sub Add(_i状態 As Integer, _s区分 As String, _i対象 As Integer, _s対象 As String, _pointF As PointF)
            Select Case _s区分
                Case NoticeInfo.損傷ﾚﾍﾞﾙ
                    損傷番号(_i状態 - 2).Add(New BaseInfo(_i対象, _s区分, _i対象, _s対象, _pointF))
                Case NoticeInfo.直接安定ﾚﾍﾞﾙ
                    直接安定番号(_i状態 - 1).Add(New BaseInfo(_i対象, _s区分, _i対象, _s対象, _pointF))
                Case NoticeInfo.杭安定ﾚﾍﾞﾙ
                    杭安定番号(_i状態 - 1).Add(New BaseInfo(_i対象, _s区分, _i対象, _s対象, _pointF))
                Case Else
                    その他.Add(New ActionInfo(_s区分, _s対象, _pointF))
            End Select
        End Sub

        Function GetComment(Optional mode As Boolean = False) As String
            Dim result As String = ""

            '部材の損傷状態の変化点
            For i = 0 To 2 '→ 損傷ﾚﾍﾞﾙ 2～4
                Dim L = 損傷番号(i)
                If L.Count > 0 Then
                    result += Get損傷Comment(i, mode)
                End If
            Next
            '直接基礎の損傷状態の変化点
            For i = 0 To 2 '→ 安定ﾚﾍﾞﾙ 1～3
                result += Get直接安定Comment(i, mode)
            Next
            '杭基礎の損傷状態の変化点
            For i = 0 To 2 '→ 安定ﾚﾍﾞﾙ 1～3
                result += Get杭安定Comment(i, mode)
            Next
            If mode = False Then
                For Each n In その他
                    result += n.s説明1
                Next
                result += s備考
            End If
            Return result
        End Function

        Function Get損傷Comment(i As Integer, Optional mode As Boolean = False) As String

            If mode = True Then Return "○"

            Dim result As String = ""
            If 損傷番号(i).Count > 0 Then
                result += Get損傷番号(i)
                result += String.Format("-{0}", i + 2)
            End If
            Return result
        End Function

        Function Get損傷番号(i As Integer, Optional 記号 As Boolean = True) As String
            Dim result As String = ""
            Dim c As Integer = 0
            For Each n In 損傷番号(i)
                If 記号 = True Then
                    result += "○{" + n.i対象.ToString + "}"
                Else
                    If c > 0 Then
                        result += "," + n.i対象.ToString
                    Else
                        result += n.i対象.ToString
                    End If
                    c += 1
                End If
            Next
            Return result
        End Function

        Function Get直接安定Comment(i As Integer, Optional mode As Boolean = False) As String

            Dim result As String = ""

            If 直接安定番号(i).Count > 0 Then
                If mode = True Then
                    For Each n In 直接安定番号(i)
                        result += n.s対象
                    Next
                Else
                    result += Get直接安定番号(i)
                    result += String.Format("-{0}", i + 1)
                End If
            End If
            Return result
        End Function

        Function Get直接安定番号(i As Integer, Optional 記号 As Boolean = True) As String
            Dim result As String = ""
            Dim c As Integer = 0
            For Each n In 直接安定番号(i)
                If 記号 = True Then
                    result += n.s対象 + n.i対象.ToString
                Else
                    If c > 0 Then
                        result += "," + n.i対象.ToString
                    Else
                        result += n.i対象.ToString
                    End If
                    c += 1
                End If
            Next
            Return result
        End Function

        Function Get杭安定Comment(i As Integer, Optional mode As Boolean = False) As String
            Dim result As String = ""
            If 杭安定番号(i).Count > 0 Then
                If mode = True Then
                    For Each n In 杭安定番号(i)
                        result += n.s対象
                    Next
                Else
                    result += Get杭安定番号(i)
                    result += String.Format("-{0}", i + 1)
                End If
            End If
            Return result
        End Function

        Function Get杭安定番号(i As Integer, Optional 記号 As Boolean = True) As String
            Dim result As String = ""
            Dim c As Integer = 0
            For Each n In 杭安定番号(i)
                If 記号 = True Then
                    result += n.s対象 + n.i対象.ToString
                Else
                    If c > 0 Then
                        result += "," + n.i対象.ToString
                    Else
                        result += n.i対象.ToString
                    End If
                    c += 1
                End If
            Next
            Return result
        End Function

    End Class

    Friend Class BaseInfo

        ''' <summary>(レベル)1 とか 2 とか</summary>
        Public i状態 As Integer
        ''' <summary>"損傷ﾚﾍﾞﾙ" とか "安定ﾚﾍﾞﾙ" とか</summary>
        Public s区分 As String
        ''' <summary>部材番号 とか 基礎の着目点番号 とか</summary>
        Public i対象 As Integer
        ''' <summary>"部材" とか "杭基礎" とか</summary>
        Public s対象 As String
        ''' <summary>震度Y, 変位量X</summary>
        Public pPointF As PointF

        ''' <summary></summary>
        ''' <param name="_i状態">(レベル)1 とか 2 とか</param>
        ''' <param name="_s区分">"損傷ﾚﾍﾞﾙ" とか "安定ﾚﾍﾞﾙ" とか</param>
        ''' <param name="_i対象">部材番号 とか 基礎の着目点番号 とか</param>
        ''' <param name="_s対象">"部材" とか "杭基礎" とか</param>
        ''' <remarks></remarks>
        Sub New(_i状態 As Integer, _s区分 As String, _i対象 As Integer, _s対象 As String, _pointF As PointF)
            i状態 = _i状態
            s区分 = _s区分
            i対象 = _i対象
            s対象 = _s対象
            pPointF = _pointF
        End Sub


    End Class

    Friend Class ActionInfo

        Public s説明1 As String
        Public s説明2 As String
        ''' <summary>震度Y, 変位量X</summary>
        Public pPointF As PointF

        Sub New(_s説明1 As String, _s説明2 As String, _pointF As PointF)
            s説明1 = _s説明1
            s説明2 = _s説明2
            pPointF = _pointF
        End Sub


    End Class



#End Region

#Region "固有周期など応答値の算出過程"

    Private Sub print応答値の算定(DB As Aggre.CalculationCase)

        Select Case DB._SNAPDB.InputInfo.KihonInfo.KisoType
            Case 5, 6, 7, 8 '擁壁の場合（初期降伏点を降伏点とする, エネルギー一定則で応答値を求める。）
                printエネルギー一定則(DB)
                Return
            Case Else
                ' 以下の処理を実行する
        End Select

        Dim CurPos = preview.GetCurrentPos
        Dim CurX As Single = CurPos.X
        Dim CurY As Single = CurPos.Y

        Dim 行間1 As Single = 25
        Dim 行間2 As Single = 17
        Dim 列x1 As Single = CurPos.X + 15
        Dim 列x2 As Single = CurPos.X + 30
        Dim 列x3 As Single = CurPos.X + 120
        Dim 列x4 As Single = CurPos.X + 340
        Dim s As String
        Dim MinerNo As Integer = 1
        Try
            preview.PrtText("(2)非線形応答スペクトル法による設計応答値の算定")
            With DB.応答値
                CurY += 行間1
                preview.SetCurrentPos(New PointF(列x1, CurY))
                preview.PrtText(String.Format("{0})構造物全体の降伏震度", MinerNo))
                MinerNo += 1
                If .全体系折曲点震度 >= 0 Then
                    CurY += 行間2
                    preview.SetCurrentPos(New PointF(列x2, CurY))
                    preview.PrtText("降伏震度")
                    preview.SetCurrentPos(New PointF(列x3, CurY))
                    preview.PrtText(String.Format("Ｋheq = {0:F3}", .全体系折曲点震度))
                End If
                If .降伏変位 >= 0 Then
                    CurY += 行間2
                    preview.SetCurrentPos(New PointF(列x2, CurY))
                    preview.PrtText("降伏変位")
                    preview.SetCurrentPos(New PointF(列x3, CurY))
                    preview.PrtText(String.Format("δeq  = {0:F1} mm", .降伏変位))
                End If
                If IsNothing(.降伏部材情報) = False Then
                    CurY += 行間2
                    preview.SetCurrentPos(New PointF(列x2, CurY))
                    preview.PrtText("降伏部位")
                    preview.SetCurrentPos(New PointF(列x3, CurY))
                    preview.PrtText(.降伏部材情報.タイトル)
                End If
                If .等価固有周期 >= 0 Then
                    CurY += 行間1
                    preview.SetCurrentPos(New PointF(列x2, CurY))
                    preview.PrtText("等価固有周期")
                    preview.SetCurrentPos(New PointF(列x3, CurY))
                    preview.PrtText("Teq  = 2.0  ×  #root{#frac{δeq}{Ｋheq}}")
                    s = " =  2.0  ×  #root{#frac{"
                    s = s + (.降伏変位 / 1000).ToString("0.000")
                    s = s + "}{"
                    s = s + .全体系折曲点震度.ToString("0.000")
                    s = s + "}}  =  "
                    s = s + .等価固有周期.ToString("0.000")
                    s = s + " sec"
                    preview.PrtText(s)
                End If

                '----------------------------------------------------------------------------------------------------------------------
                If DB._解析対象(1) = True Then
                    CurY += 行間1 * 1.5
                    preview.SetCurrentPos(New PointF(列x1, CurY))
                    preview.PrtText(String.Format("{0})応答塑性率および最大応答変位の算定", MinerNo))
                    MinerNo += 1

                    If .安全性_最大応答step > 0 Then
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("設計地震動")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        Select Case DB._スペクトルの種類
                            Case 1 : preview.PrtText("L2地震動 (スペクトルⅠ)")
                            Case 2 : preview.PrtText("L2地震動 (スペクトルⅡ)")
                            Case 3
                                Select Case .安全性_スペクトルの種類
                                    Case 1 : preview.PrtText("L2地震動 (スペクトルⅠ)")
                                    Case 2 : preview.PrtText("L2地震動 (スペクトルⅡ)")
                                End Select
                        End Select
                        If IsNothing(.降伏部材情報) = False Then
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("構造物種別")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            Select Case .降伏部材情報.Get構造物種類
                                Case 0 : preview.PrtText("上部構造(RC, SRC系)")
                                Case 1 : preview.PrtText("上部構造(S系)")
                                Case 2 : preview.PrtText("基礎構造物(杭ケーソン)")
                                Case 3 : preview.PrtText("基礎構造物(直接基礎)")
                                Case 4 : preview.PrtText("抗土圧構造物(RC壁体・杭基礎)")
                                Case 5 : preview.PrtText("抗土圧構造物(直接基礎)")
                            End Select
                        End If
                        If DB._Is液状化L22ケース = False AndAlso DB._Is液状化L21ケース = False Then
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("地盤種別")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            If DB._地盤区分 = 9 Then
                                preview.PrtText("任意スペクトル")
                            Else
                                preview.PrtText(String.Format("G{0}地盤", DB._地盤区分))
                            End If
                        Else
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("液状化区分")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            Select Case DB._液状化区分
                                Case 5 : preview.PrtText("液状化指数PL(5＜PL≦20)")
                                Case 20 : preview.PrtText("液状化指数PL(20＜PL)")
                            End Select
                        End If
                        If .全体系折曲点震度 >= 0 Then
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            If .全体系折曲点震度_不整形影響 = -1 Then
                                preview.PrtText("降伏震度")
                                preview.SetCurrentPos(New PointF(列x3, CurY))
                                preview.PrtText(String.Format("Ｋheq = {0:F3}", .全体系折曲点震度))
                            Else
                                preview.PrtText("不整形地盤の係数")
                                preview.SetCurrentPos(New PointF(列x3, CurY))
                                preview.PrtText("η_{2}(x) = " + String.Format("{0:F3}", DB._L2不整形地盤の係数ηx))
                                CurY += 行間2
                                preview.SetCurrentPos(New PointF(列x2, CurY))
                                preview.PrtText("降伏震度")
                                preview.SetCurrentPos(New PointF(列x3, CurY))
                                preview.PrtText("Ｋheqη_{2} = " + String.Format("{0:F3} ／ {1:F3} = {2:F3}", .全体系折曲点震度, DB._L2不整形地盤の係数ηx, .全体系折曲点震度_不整形影響))
                            End If


                        End If
                        If .等価固有周期 > 0 Then
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("等価固有周期")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            preview.PrtText(String.Format("Teq  = {0:F3} sec", .等価固有周期))
                        End If
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("応答塑性率")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText(String.Format("μ = {0:F2}", .安全性_応答塑性率))
                    End If

                    '----------------------------------------------------------------------------------------------------------------------
                    CurY += 行間1 * 1.5
                    preview.SetCurrentPos(New PointF(列x1, CurY))
                    preview.PrtText(String.Format("{0})応答値の算出", MinerNo))
                    MinerNo += 1

                    CurY += 行間2
                    preview.SetCurrentPos(New PointF(列x2, CurY))
                    preview.PrtText("最大応答変位")
                    preview.SetCurrentPos(New PointF(列x3, CurY))
                    s = "δ_{r} = μ・δeq = "
                    s = s + .安全性_応答塑性率.ToString("0.00")
                    s = s + " × "
                    s = s + .降伏変位.ToString("0.0")
                    s = s + " = "

                    If .安全性_最大応答変位_弾性応答無 = -1 Then
                        s = s + .安全性_最大応答変位.ToString("0.0")
                        s = s + " mm"
                        preview.PrtText(s)
                        preview.SetCurrentPos(New PointF(列x4, CurY))
                        preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .安全性_最大応答step, .maxStep))
                    Else
                        s = s + .安全性_最大応答変位_弾性応答無.ToString("0.0")
                        s = s + " mm"
                        preview.PrtText(s)
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText("注) 応答震度が地表面設計地震動の弾性加速度応答を上回るため、")
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText("　　応答値は下記の最大応答震度を最大値とする。")
                    End If

                    CurY += 行間2
                    preview.SetCurrentPos(New PointF(列x2, CurY))
                    preview.PrtText("最大応答震度")
                    preview.SetCurrentPos(New PointF(列x3, CurY))
                    If .最大震度step < .安全性_最大応答step Then
                        preview.PrtText(String.Format("Khr = {0:F3}", .震度(.最大震度step)))
                        preview.SetCurrentPos(New PointF(列x4, CurY))
                        preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .最大震度step, .maxStep))
                    Else
                        preview.PrtText(String.Format("Khr = {0:F3}", .安全性_最大応答震度))
                        preview.SetCurrentPos(New PointF(列x4, CurY))
                        preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .安全性_最大応答step, .maxStep))
                    End If

                End If
                If DB._解析対象(0) = True Then
                    '----------------------------------------------------------------------------------------------------------------------
                    CurY += 行間1 * 1.5
                    preview.SetCurrentPos(New PointF(列x1, CurY))
                    preview.PrtText(String.Format("{0})復旧性を検討するための地震動に対する応答値の算定", MinerNo))
                    MinerNo += 1
                    If .復旧性_最大応答step = .安全性_最大応答step Then
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("復旧性を検討するための応答値は, 同一となるため省略する。")
                    Else
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("設計地震動")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        Select Case DB._スペクトルの種類
                            Case 1 : preview.PrtText("L2地震動 (スペクトルⅠ)")
                            Case 2 : preview.PrtText("L2地震動 (スペクトルⅡ)")
                            Case 3
                                Select Case .復旧性_スペクトルの種類
                                    Case 1 : preview.PrtText("L2地震動 (スペクトルⅠ)")
                                    Case 2 : preview.PrtText("L2地震動 (スペクトルⅡ)")
                                End Select
                        End Select
                        If IsNothing(.降伏部材情報) = False Then
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("構造物種別")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            Select Case .降伏部材情報.Get構造物種類
                                Case 0 : preview.PrtText("上部構造(RC, SRC系)")
                                Case 1 : preview.PrtText("上部構造(S系)")
                                Case 2 : preview.PrtText("基礎構造物(杭ケーソン)")
                                Case 3 : preview.PrtText("基礎構造物(直接基礎)")
                                Case 4 : preview.PrtText("抗土圧構造物(RC壁体・杭基礎)")
                                Case 5 : preview.PrtText("抗土圧構造物(直接基礎)")
                            End Select
                        End If
                        If DB._Is液状化L22ケース = False AndAlso DB._Is液状化L21ケース = False Then
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("地盤種別")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            preview.PrtText(String.Format("G{0}地盤", DB._地盤区分))

                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("地域別係数")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            preview.PrtText(DB._地域別補正係数.ToString("0.00"))
                        Else
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("液状化区分")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            Select Case DB._液状化区分
                                Case 5 : preview.PrtText("液状化指数PL(5＜PL≦20)")
                                Case 20 : preview.PrtText("液状化指数PL(20＜PL)")
                            End Select
                        End If
                        If .全体系折曲点震度 >= 0 Then
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            If .全体系折曲点震度_不整形影響 = -1 Then
                                preview.PrtText("降伏震度")
                                preview.SetCurrentPos(New PointF(列x3, CurY))
                                preview.PrtText(String.Format("Ｋheq = {0:F3}", .全体系折曲点震度))
                            Else
                                preview.PrtText("不整形地盤の係数")
                                preview.SetCurrentPos(New PointF(列x3, CurY))
                                preview.PrtText("η_{2}(x) = " + String.Format("{0:F3}", DB._L2不整形地盤の係数ηx))
                                CurY += 行間2
                                preview.SetCurrentPos(New PointF(列x2, CurY))
                                preview.PrtText("降伏震度")
                                preview.SetCurrentPos(New PointF(列x3, CurY))
                                preview.PrtText(String.Format("Ｋheq = {0:F3} ／ {1:F3} = {2:F3}", .全体系折曲点震度, DB._L2不整形地盤の係数ηx, .全体系折曲点震度_不整形影響))
                            End If

                        End If
                        If .等価固有周期 > 0 Then
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("等価固有周期")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            preview.PrtText(String.Format("Teq  = {0:F3} sec", .等価固有周期))
                        End If
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("応答塑性率")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText(String.Format("μ = {0:F2}", .復旧性_応答塑性率))

                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("最大応答変位")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        s = "δ_{r} = μ・δeq = "
                        s = s + .復旧性_応答塑性率.ToString("0.00")
                        s = s + " × "
                        s = s + .降伏変位.ToString("0.0")
                        s = s + " = "

                        If .復旧性_最大応答変位_弾性応答無 = -1 Then
                            s = s + .復旧性_最大応答変位.ToString("0.0")
                            s = s + " mm"
                            preview.PrtText(s)
                            preview.SetCurrentPos(New PointF(列x4, CurY))
                            preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .復旧性_最大応答step, .maxStep))
                        Else
                            s = s + .復旧性_最大応答変位_弾性応答無.ToString("0.0")
                            s = s + " mm"
                            preview.PrtText(s)
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            preview.PrtText("注) 応答震度が地表面設計地震動の弾性加速度応答を上回るため、")
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            preview.PrtText("　　応答値は下記の最大応答震度を最大値とする。")
                        End If

                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("最大応答震度")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        If .最大震度step < .復旧性_最大応答step Then
                            preview.PrtText(String.Format("Khr = {0:F3}", .震度(.最大震度step)))
                            preview.SetCurrentPos(New PointF(列x4, CurY))
                            preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .最大震度step, .maxStep))
                        Else
                            preview.PrtText(String.Format("Khr = {0:F3}", .復旧性_最大応答震度))
                            preview.SetCurrentPos(New PointF(列x4, CurY))
                            preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .復旧性_最大応答step, .maxStep))
                        End If
                    End If
                End If

                '----------------------------------------------------------------------------------------------------------------------
                If .復旧性_L1震度 > 0 Then
                    CurY += 行間1 * 1.5
                    preview.SetCurrentPos(New PointF(列x1, CurY))
                    preview.PrtText(String.Format("{0}) L1地震動に対する応答値の算定", MinerNo))
                    MinerNo += 1
                    If DB._Is液状化L1ケース = False Then
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("地盤種別")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText(String.Format("G{0}地盤", DB._地盤区分))

                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("地域別係数")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText(DB._地域別補正係数.ToString("0.00"))
                    End If
                    If .初期降伏震度 >= 0 Then
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("降伏震度")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText(String.Format("Ｋhy = {0:F3}", .初期降伏震度))
                    End If
                    If .等価固有周期 > 0 Then
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("等価固有周期")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText(String.Format("Teq  = {0:F3} sec", .等価固有周期))
                    End If
                    CurY += 行間2
                    preview.SetCurrentPos(New PointF(列x2, CurY))
                    If .復旧性_L1震度_不整形影響無 = -1 Then
                        preview.PrtText("L1地震 設計震度")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        s = "Ｋh_{L1} = "
                        s = s + .復旧性_L1震度.ToString("0.000")
                        preview.PrtText(s)
                    Else
                        preview.PrtText("不整形地盤の係数")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText("η_{1}(x) = " + String.Format("{0:F3}", DB._L1不整形地盤の係数ηx))
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("L1地震 設計震度")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        s = "Ｋh_{L1} × η_{1} = "
                        s = s + String.Format("{0:F3} × {1:F3} = {2:F3}", .復旧性_L1震度_不整形影響無, DB._L1不整形地盤の係数ηx, .復旧性_L1震度)
                        preview.PrtText(s)
                    End If
                    preview.SetCurrentPos(New PointF(列x4, CurY))
                    preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .復旧性_L1震度step, .maxStep))
                End If
            End With
        Catch ex As Exception

        End Try
    End Sub

    Private Sub printエネルギー一定則(DB As Aggre.CalculationCase)

        Dim CurPos = preview.GetCurrentPos
        Dim CurX As Single = CurPos.X
        Dim CurY As Single = CurPos.Y

        Dim 行間1 As Single = 25
        Dim 行間2 As Single = 17
        Dim 列x1 As Single = CurPos.X + 15
        Dim 列x2 As Single = CurPos.X + 30
        Dim 列x3 As Single = CurPos.X + 120
        Dim 列x4 As Single = CurPos.X + 340
        Dim s As String
        Dim MinerNo As Integer = 1
        Try
            preview.PrtText("(2)エネルギー一定則による設計応答値の算定")
            With DB.応答値
                Dim 降伏震度 As Double = .全体系折曲点震度

                CurY += 行間1
                preview.SetCurrentPos(New PointF(列x1, CurY))
                preview.PrtText(String.Format("{0})構造物全体の降伏震度", MinerNo))
                MinerNo += 1
                If .全体系折曲点震度 >= 0 Then
                    CurY += 行間2
                    preview.SetCurrentPos(New PointF(列x2, CurY))
                    preview.PrtText("降伏震度")
                    preview.SetCurrentPos(New PointF(列x3, CurY))
                    preview.PrtText(String.Format("Ｋheq = {0:F3}", .全体系折曲点震度))
                End If
                If .降伏変位 >= 0 Then
                    CurY += 行間2
                    preview.SetCurrentPos(New PointF(列x2, CurY))
                    preview.PrtText("降伏変位")
                    preview.SetCurrentPos(New PointF(列x3, CurY))
                    preview.PrtText(String.Format("δeq  = {0:F1} mm", .降伏変位))
                End If
                If IsNothing(.降伏部材情報) = False Then
                    CurY += 行間2
                    preview.SetCurrentPos(New PointF(列x2, CurY))
                    preview.PrtText("降伏部位")
                    preview.SetCurrentPos(New PointF(列x3, CurY))
                    preview.PrtText(.降伏部材情報.タイトル)
                End If

                '----------------------------------------------------------------------------------------------------------------------
                If DB._解析対象(1) = True Then
                    CurY += 行間1 * 1.5
                    preview.SetCurrentPos(New PointF(列x1, CurY))
                    preview.PrtText(String.Format("{0})線形最大震度および最大応答変位の算定", MinerNo))
                    MinerNo += 1

                    If .安全性_最大応答step > 0 Then
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("線形最大震度")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        Select Case DB._スペクトルの種類
                            Case 1 : preview.PrtText("L2地震動 (スペクトルⅠ)")
                            Case 2 : preview.PrtText("L2地震動 (スペクトルⅡ)")
                            Case 3
                                Select Case .安全性_スペクトルの種類
                                    Case 1 : preview.PrtText("L2地震動 (スペクトルⅠ)")
                                    Case 2 : preview.PrtText("L2地震動 (スペクトルⅡ)")
                                End Select
                        End Select
                        If DB._Is液状化L22ケース = False AndAlso DB._Is液状化L21ケース = False Then
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("地盤種別")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            If DB._地盤区分 = 9 Then
                                preview.PrtText("任意スペクトル")
                            Else
                                preview.PrtText(String.Format("G{0}地盤", DB._地盤区分))
                            End If
                        Else
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("液状化区分")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            Select Case DB._液状化区分
                                Case 5 : preview.PrtText("液状化指数PL(5＜PL≦20)")
                                Case 20 : preview.PrtText("液状化指数PL(20＜PL)")
                            End Select
                        End If

                        If .全体系折曲点震度 >= 0 Then
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            If .全体系折曲点震度_不整形影響 = -1 Then
                                降伏震度 = .全体系折曲点震度
                            Else
                                降伏震度 = .全体系折曲点震度_不整形影響
                                preview.PrtText("不整形地盤の係数")
                                preview.SetCurrentPos(New PointF(列x3, CurY))
                                preview.PrtText("η_{2}(x) = " + String.Format("{0:F3}", DB._L2不整形地盤の係数ηx))
                                CurY += 行間2
                                preview.SetCurrentPos(New PointF(列x2, CurY))
                                preview.PrtText("降伏震度")
                                preview.SetCurrentPos(New PointF(列x3, CurY))
                                preview.PrtText("Ｋheqη_{2} = " + String.Format("{0:F3} ／ {1:F3} = {2:F3}", .全体系折曲点震度, DB._L2不整形地盤の係数ηx, .全体系折曲点震度_不整形影響))
                                CurY += 行間2
                            End If
                        End If
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("線形最大震度")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText(String.Format("Khmax = {0:F3}", .安全性_線形最大震度))
                    End If

                    '----------------------------------------------------------------------------------------------------------------------
                    CurY += 行間1 * 1.5
                    preview.SetCurrentPos(New PointF(列x1, CurY))
                    preview.PrtText(String.Format("{0})応答値の算出", MinerNo))
                    MinerNo += 1

                    If .安全性_線形最大震度 > 降伏震度 Then

                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("C点の変位量")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        s = "δ_{c} = "
                        s = s + .降伏変位.ToString("0.0")
                        s = s + " / "
                        s = s + 降伏震度.ToString("0.000")
                        s = s + " × "
                        s = s + .安全性_線形最大震度.ToString("0.000")
                        s = s + " = "
                        s = s + .安全性_C点.ToString("0.0")
                        s = s + " mm"
                        preview.PrtText(s)

                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("△ABCの面積")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        s = "△ABC = "
                        s = s + .安全性_線形最大震度.ToString("0.000")
                        s = s + " × "
                        s = s + .安全性_C点.ToString("0.0")
                        s = s + " × 1/2"
                        s = s + " = "
                        s = s + .安全性_ABC.ToString("0.0")
                        preview.PrtText(s)

                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("最大応答変位")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        s = "δ_{r} = "
                        s = s + .安全性_ABC.ToString("0.0")
                        s = s + " / "
                        s = s + 降伏震度.ToString("0.000")
                        s = s + " + "
                        s = s + .降伏変位.ToString("0.0")
                        s = s + " /2 = "
                        s = s + .安全性_最大応答変位.ToString("0.0")
                        s = s + " mm"
                        preview.PrtText(s)
                        preview.SetCurrentPos(New PointF(列x4, CurY))
                        preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .安全性_最大応答step, .maxStep))

                    Else
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("最大応答変位")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText("注) 降伏震度が線形最大震度を上回るため、応答値は線形最大震度とする。")
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        s = "δ_{r} = "
                        s = s + .安全性_最大応答変位.ToString("0.0")
                        s = s + " mm"
                        preview.PrtText(s)
                        preview.SetCurrentPos(New PointF(列x4, CurY))
                        preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .安全性_最大応答step, .maxStep))
                    End If

                    CurY += 行間2
                    preview.SetCurrentPos(New PointF(列x2, CurY))
                    preview.PrtText("最大応答震度")
                    preview.SetCurrentPos(New PointF(列x3, CurY))
                    If .最大震度step < .安全性_最大応答step Then
                        preview.PrtText(String.Format("Khr = {0:F3}", .震度(.最大震度step)))
                        preview.SetCurrentPos(New PointF(列x4, CurY))
                        preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .最大震度step, .maxStep))
                    Else
                        preview.PrtText(String.Format("Khr = {0:F3}", .安全性_最大応答震度))
                        preview.SetCurrentPos(New PointF(列x4, CurY))
                        preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .安全性_最大応答step, .maxStep))
                    End If

                End If




                If DB._解析対象(0) = True Then
                    '----------------------------------------------------------------------------------------------------------------------
                    CurY += 行間1 * 1.5
                    preview.SetCurrentPos(New PointF(列x1, CurY))
                    preview.PrtText(String.Format("{0})復旧性を検討するための地震動に対する応答値の算定", MinerNo))
                    MinerNo += 1
                    If .復旧性_最大応答step = .安全性_最大応答step Then
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("復旧性を検討するための応答値は, 同一となるため省略する。")
                    Else


                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("線形最大震度")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        Select Case DB._スペクトルの種類
                            Case 1 : preview.PrtText("L2地震動 (スペクトルⅠ)")
                            Case 2 : preview.PrtText("L2地震動 (スペクトルⅡ)")
                            Case 3
                                Select Case .復旧性_スペクトルの種類
                                    Case 1 : preview.PrtText("L2地震動 (スペクトルⅠ)")
                                    Case 2 : preview.PrtText("L2地震動 (スペクトルⅡ)")
                                End Select
                        End Select
                        If DB._Is液状化L22ケース = False AndAlso DB._Is液状化L21ケース = False Then
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("地盤種別")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            preview.PrtText(String.Format("G{0}地盤", DB._地盤区分))

                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("地域別係数")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            preview.PrtText(DB._地域別補正係数.ToString("0.00"))
                        Else
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("液状化区分")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            Select Case DB._液状化区分
                                Case 5 : preview.PrtText("液状化指数PL(5＜PL≦20)")
                                Case 20 : preview.PrtText("液状化指数PL(20＜PL)")
                            End Select
                        End If
                        If .全体系折曲点震度 >= 0 Then
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            If .全体系折曲点震度_不整形影響 = -1 Then
                                降伏震度 = .全体系折曲点震度
                            Else
                                降伏震度 = .全体系折曲点震度_不整形影響
                                preview.PrtText("不整形地盤の係数")
                                preview.SetCurrentPos(New PointF(列x3, CurY))
                                preview.PrtText("η_{2}(x) = " + String.Format("{0:F3}", DB._L2不整形地盤の係数ηx))
                                CurY += 行間2
                                preview.SetCurrentPos(New PointF(列x2, CurY))
                                preview.PrtText("降伏震度")
                                preview.SetCurrentPos(New PointF(列x3, CurY))
                                preview.PrtText(String.Format("Ｋheq = {0:F3} ／ {1:F3} = {2:F3}", .全体系折曲点震度, DB._L2不整形地盤の係数ηx, .全体系折曲点震度_不整形影響))
                            End If

                        End If


                        If .復旧性_線形最大震度 > 降伏震度 Then

                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("C点の変位量")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            s = "δ_{c} = "
                            s = s + .降伏変位.ToString("0.0")
                            s = s + " / "
                            s = s + 降伏震度.ToString("0.000")
                            s = s + " × "
                            s = s + .復旧性_線形最大震度.ToString("0.000")
                            s = s + " = "
                            s = s + .復旧性_C点.ToString("0.0")
                            s = s + " mm"
                            preview.PrtText(s)

                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("△ABCの面積")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            s = "△ABC = "
                            s = s + .復旧性_線形最大震度.ToString("0.000")
                            s = s + " × "
                            s = s + .復旧性_C点.ToString("0.0")
                            s = s + " × 1/2"
                            s = s + " = "
                            s = s + .復旧性_ABC.ToString("0.0")
                            preview.PrtText(s)

                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("最大応答変位")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            s = "δ_{r} = "
                            s = s + .復旧性_ABC.ToString("0.0")
                            s = s + " / "
                            s = s + 降伏震度.ToString("0.000")
                            s = s + " + "
                            s = s + .降伏変位.ToString("0.0")
                            s = s + " /2 = "
                            s = s + .復旧性_最大応答変位.ToString("0.0")
                            s = s + " mm"
                            preview.PrtText(s)
                            preview.SetCurrentPos(New PointF(列x4, CurY))
                            preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .復旧性_最大応答step, .maxStep))

                        Else
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x2, CurY))
                            preview.PrtText("最大応答変位")
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            preview.PrtText("注) 降伏震度が線形最大震度を上回るため、応答値は線形最大震度とする。")
                            CurY += 行間2
                            preview.SetCurrentPos(New PointF(列x3, CurY))
                            s = "δ_{r} = "
                            s = s + .復旧性_最大応答変位.ToString("0.0")
                            s = s + " mm"
                            preview.PrtText(s)
                            preview.SetCurrentPos(New PointF(列x4, CurY))
                            preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .復旧性_最大応答step, .maxStep))
                        End If

                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("最大応答震度")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        If .最大震度step < .復旧性_最大応答step Then
                            preview.PrtText(String.Format("Khr = {0:F3}", .震度(.最大震度step)))
                            preview.SetCurrentPos(New PointF(列x4, CurY))
                            preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .最大震度step, .maxStep))
                        Else
                            preview.PrtText(String.Format("Khr = {0:F3}", .復旧性_最大応答震度))
                            preview.SetCurrentPos(New PointF(列x4, CurY))
                            preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .復旧性_最大応答step, .maxStep))
                        End If
                    End If
                End If

                '----------------------------------------------------------------------------------------------------------------------
                If .復旧性_L1震度 > 0 Then
                    CurY += 行間1 * 1.5
                    preview.SetCurrentPos(New PointF(列x1, CurY))
                    preview.PrtText(String.Format("{0}) L1地震動に対する応答値の算定", MinerNo))
                    MinerNo += 1
                    If DB._Is液状化L1ケース = False Then
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("地盤種別")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText(String.Format("G{0}地盤", DB._地盤区分))

                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("地域別係数")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText(DB._地域別補正係数.ToString("0.00"))
                    End If
                    If .初期降伏震度 >= 0 Then
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("降伏震度")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText(String.Format("Ｋhy = {0:F3}", .初期降伏震度))
                    End If

                    CurY += 行間2
                    preview.SetCurrentPos(New PointF(列x2, CurY))
                    If .復旧性_L1震度_不整形影響無 = -1 Then
                        preview.PrtText("L1地震 設計震度")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        s = "Ｋh_{L1} = "
                        s = s + .復旧性_L1震度.ToString("0.000")
                        preview.PrtText(s)
                    Else
                        preview.PrtText("不整形地盤の係数")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        preview.PrtText("η_{1}(x) = " + String.Format("{0:F3}", DB._L1不整形地盤の係数ηx))
                        CurY += 行間2
                        preview.SetCurrentPos(New PointF(列x2, CurY))
                        preview.PrtText("L1地震 設計震度")
                        preview.SetCurrentPos(New PointF(列x3, CurY))
                        s = "Ｋh_{L1} × η_{1} = "
                        s = s + String.Format("{0:F3} × {1:F3} = {2:F3}", .復旧性_L1震度_不整形影響無, DB._L1不整形地盤の係数ηx, .復旧性_L1震度)
                        preview.PrtText(s)
                    End If
                    preview.SetCurrentPos(New PointF(列x4, CurY))
                    preview.PrtText(String.Format("( 増分ステップ {0} / {1} )", .復旧性_L1震度step, .maxStep))
                End If
            End With
        Catch ex As Exception

        End Try
    End Sub

#End Region

#Region "応力図作成"

    Private Function GetScalablePoint(p As PointF) As PointF
        Return New PointF(GetScalablePointX(p.X), GetScalablePointY(p.Y))
    End Function

    Private Function GetScalablePointX(px As Single) As Single
        If ScaleX < 0 Then
            Dim pageSize As SizeF = Me.preview.GetPageSize
            Return px + pageSize.Width / 2
        Else
            Return (px - MinX) * ScaleX + Margin
        End If
    End Function

    Private Function GetScalablePointY(py As Single) As Single
        If ScaleY < 0 Then
            Dim pageSize As SizeF = Me.preview.GetPageSize
            Return py + pageSize.Height / 2
        Else
            Return (py - MinY) * ScaleY + Margin
        End If
    End Function

    Private Sub Init断面力図(DB As CalculationCase)

        '節点座標をセット ----------------------------------------------------------
        PointOrg = New Dictionary(Of Integer, PointF)
        Try
            'キーを配列に変換する
            For i As Integer = 1 To DB._SNAPDB.GetJointCount
                Dim p = DB._SNAPDB.GetJoint(i)
                PointOrg.Add(i, New PointF(p.X, p.Y))
            Next
        Catch ex As Exception

        End Try

        '用紙の向き・縮尺の決定 ----------------------------------------------------------
        MaxX = Single.MinValue
        MinX = Single.MaxValue
        MaxY = Single.MinValue
        MinY = Single.MaxValue
        For Each p In PointOrg
            MaxX = Math.Max(MaxX, p.Value.X)
            MinX = Math.Min(MinX, p.Value.X)
            MaxY = Math.Max(MaxY, p.Value.Y)
            MinY = Math.Min(MinY, p.Value.Y)
        Next
        Dim MaxXDistance = MaxX - MinX
        Dim MaxYDistance = MaxY - MinY
        If MaxXDistance > MaxYDistance Then
            Orientation = True
        Else
            Orientation = False
        End If
        Me.preview.SetPaperOrientation(Orientation)

        '縮尺の決定 ----------------------------------------------------------

        Dim pageSize As SizeF = Me.preview.GetPageSize

        If MaxXDistance <= 0 Then
            ScaleX = -1
        Else
            ScaleX = (pageSize.Width - (Margin * 2)) / MaxXDistance
        End If
        If MaxYDistance <= 0 Then
            ScaleY = -1
        Else
            ScaleY = (pageSize.Height - (Margin * 2)) / MaxYDistance
        End If

    End Sub

    Private Sub print骨組み図(DB As CalculationCase, Optional PointRadius As Boolean = False)

        '要素を描画する。
        Try
            For m = 1 To DB._SNAPDB.GetMemberCount
                Dim iNo As Integer = DB._SNAPDB.GetMember(m).Itan
                Dim jNo As Integer = DB._SNAPDB.GetMember(m).Jtan
                Dim ix As Integer = GetScalablePointX(PointOrg(iNo).X)
                Dim iy As Integer = GetScalablePointY(PointOrg(iNo).Y)
                Dim jx As Integer = GetScalablePointX(PointOrg(jNo).X)
                Dim jy As Integer = GetScalablePointY(PointOrg(jNo).Y)
                preview.DrawLine(New PointF(ix, iy), New PointF(jx, jy), thickness1)
            Next
        Catch ex As Exception

        End Try

        If PointRadius = False Then Return

        '節点●を描画する。
        Try
            For Each p In PointOrg
                preview.DrawCircle(GetScalablePoint(p.Value), New SizeF(radius, radius), Brushes.Black)
            Next
        Catch ex As Exception

        End Try


    End Sub

    Private Sub print変位図(DB As CalculationCase)

        'タイトル出力 ----------------------------------------------------------------------
        preview.PrtText("変位図")
        preview.enter(1.5)

        '変位の最大値を計算し、変位図のスケールを決定する。 ---------------------------------
        Dim Step1 As Integer = IIf(DB._解析対象(5) = True, DB.応答値.復旧性_L1震度step, 0)
        Dim Step2 As Integer = IIf(DB._解析対象(0) = True, DB.応答値.復旧性_最大応答step, 0)
        Dim Step3 As Integer = IIf(DB._解析対象(1) = True, DB.応答値.安全性_最大応答step, 0)
        If Step1 + Step2 + Step3 = 0 Then Exit Sub
        'Dim δScale As Single
        Dim δScaleX As Single
        Dim δScaleY As Single

        Try
            Dim Maxδx As Single = 0
            Dim Maxδy As Single = 0
            For Each p In PointOrg
                Maxδx = Math.Max(Maxδx, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step1).JointItem(p.Key).DeltaX * 1000))
                Maxδx = Math.Max(Maxδx, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step2).JointItem(p.Key).DeltaX * 1000))
                Maxδx = Math.Max(Maxδx, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step3).JointItem(p.Key).DeltaX * 1000))
                Maxδy = Math.Max(Maxδy, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step1).JointItem(p.Key).DeltaY * 1000))
                Maxδy = Math.Max(Maxδy, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step2).JointItem(p.Key).DeltaY * 1000))
                Maxδy = Math.Max(Maxδy, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step3).JointItem(p.Key).DeltaY * 1000))
            Next

            If ScaleX < 0 Then
                δScaleX = Margin / Maxδx
                δScaleY = δScaleX
            ElseIf ScaleY < 0 Then
                δScaleY = Margin / Maxδy
                δScaleX = δScaleY
            Else
                δScaleX = Margin / (Maxδx * ScaleX)
                δScaleY = Margin / (Maxδy * ScaleY)

            End If

            'X と Y のスケールの小さいほうを共通のスケールとする
            δScaleX = Math.Min(δScaleX, δScaleY) * ScaleAlpha
            δScaleY = δScaleX


        Catch ex As Exception
        End Try

        '変位の図を描画する。 ------------------------------------------------------------
        Try
            For m = 1 To DB._SNAPDB.GetMemberCount
                Dim iNo As Integer = DB._SNAPDB.GetMember(m).Itan
                Dim jNo As Integer = DB._SNAPDB.GetMember(m).Jtan
                'L1地震動
                If Step1 > 0 Then
                    With DB._SNAPDB.OutputInfo.StepCtrl(Step1)
                        Dim idx = PointOrg(iNo).X + .JointItem(iNo).DeltaX * 1000 * δScaleX 'mm
                        Dim idy = PointOrg(iNo).Y + .JointItem(iNo).DeltaY * 1000 * δScaleY 'mm
                        Dim jdx = PointOrg(jNo).X + .JointItem(jNo).DeltaX * 1000 * δScaleX 'mm
                        Dim jdy = PointOrg(jNo).Y + .JointItem(jNo).DeltaY * 1000 * δScaleY 'mm
                        preview.DrawLine(GetScalablePoint(New PointF(idx, idy)),
                                         GetScalablePoint(New PointF(jdx, jdy)),
                                         thickness2, Color.Green)

                    End With
                End If
                '復旧性
                If Step2 > 0 Then
                    With DB._SNAPDB.OutputInfo.StepCtrl(Step2)
                        Dim idx = PointOrg(iNo).X + .JointItem(iNo).DeltaX * 1000 * δScaleX 'mm
                        Dim idy = PointOrg(iNo).Y + .JointItem(iNo).DeltaY * 1000 * δScaleY 'mm
                        Dim jdx = PointOrg(jNo).X + .JointItem(jNo).DeltaX * 1000 * δScaleX 'mm
                        Dim jdy = PointOrg(jNo).Y + .JointItem(jNo).DeltaY * 1000 * δScaleY 'mm
                        preview.DrawLine(GetScalablePoint(New PointF(idx, idy)),
                                         GetScalablePoint(New PointF(jdx, jdy)),
                                         thickness2, Color.Blue)

                    End With
                End If
                '安全性
                If Step3 > 0 Then
                    With DB._SNAPDB.OutputInfo.StepCtrl(Step3)
                        Dim idx = PointOrg(iNo).X + .JointItem(iNo).DeltaX * 1000 * δScaleX 'mm
                        Dim idy = PointOrg(iNo).Y + .JointItem(iNo).DeltaY * 1000 * δScaleY 'mm
                        Dim jdx = PointOrg(jNo).X + .JointItem(jNo).DeltaX * 1000 * δScaleX 'mm
                        Dim jdy = PointOrg(jNo).Y + .JointItem(jNo).DeltaY * 1000 * δScaleY 'mm
                        preview.DrawLine(GetScalablePoint(New PointF(idx, idy)),
                                         GetScalablePoint(New PointF(jdx, jdy)),
                                         thickness2, Color.Red)

                    End With
                End If
            Next
        Catch ex As Exception
        End Try
        '節点番号を表示する。 ------------------------------------------------------------
        For Each p In PointOrg
            preview.SetCurrentPos(GetScalablePoint(New PointF(p.Value.X, p.Value.Y)))
            preview.PrtText(p.Key)
        Next

        '右下に凡例を表示する。 ------------------------------------------------------------
        print凡例(Step1, Step2, Step3)


    End Sub

    Private Sub printモーメント図(DB As CalculationCase)

        'タイトル出力 ----------------------------------------------------------------------
        preview.PrtText("モーメント図")
        preview.enter(1.5)

        'モーメントの最大値を計算し、モーメント図のスケールを決定する。 ---------------------------------
        Dim Step1 As Integer = IIf(DB._解析対象(5) = True, DB.応答値.復旧性_L1震度step, 0)
        Dim Step2 As Integer = IIf(DB._解析対象(0) = True, DB.応答値.復旧性_最大応答step, 0)
        Dim Step3 As Integer = IIf(DB._解析対象(1) = True, DB.応答値.安全性_最大応答step, 0)
        If Step1 + Step2 + Step3 = 0 Then Exit Sub

        Dim printTxtCase As Integer = 3 '文字を描くケース
        If Step3 = 0 Then
            printTxtCase = 2
            If Step2 = 0 Then
                printTxtCase = 1
            End If
        End If

        Dim MdScale As Single
        Try
            Dim MaxMd As Single = 0
            For m = 1 To DB._SNAPDB.GetMemberCount
                Dim iNo As Integer = DB._SNAPDB.GetMember(m).Itan
                Dim jNo As Integer = DB._SNAPDB.GetMember(m).Jtan
                If Step1 > 0 Then
                    MaxMd = Math.Max(MaxMd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step1).MemberItem(m).Mi))
                    MaxMd = Math.Max(MaxMd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step1).MemberItem(m).Mj))
                End If
                If Step2 > 0 Then
                    MaxMd = Math.Max(MaxMd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step2).MemberItem(m).Mi))
                    MaxMd = Math.Max(MaxMd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step2).MemberItem(m).Mj))
                End If
                If Step3 > 0 Then
                    MaxMd = Math.Max(MaxMd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step3).MemberItem(m).Mi))
                    MaxMd = Math.Max(MaxMd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step3).MemberItem(m).Mj))
                End If
            Next

            Dim MdScaleX As Single
            Dim MdScaleY As Single

            If ScaleX < 0 Then
                MdScaleX = Margin / MaxMd
                MdScaleY = MdScaleX
            ElseIf ScaleY < 0 Then
                MdScaleY = Margin / MaxMd
                MdScaleX = MdScaleY
            Else
                MdScaleX = Margin / (MaxMd * ScaleX)
                MdScaleY = Margin / (MaxMd * ScaleY)
            End If


            MdScale = Math.Min(MdScaleX, MdScaleY) * ScaleAlpha
        Catch ex As Exception
        End Try

        'モーメント図を描画する。 ------------------------------------------------------------
        Dim oldFontSize = preview.FontSize
        Dim oldFontColor = preview.FontColor
        Try
            preview.FontSize = 8
            For m = 1 To DB._SNAPDB.GetMemberCount
                Dim iNo As Integer = DB._SNAPDB.GetMember(m).Itan
                Dim jNo As Integer = DB._SNAPDB.GetMember(m).Jtan

                '部材の角度を計算する。
                Dim idx = PointOrg(iNo).X
                Dim idy = PointOrg(iNo).Y
                Dim jdx = PointOrg(jNo).X
                Dim jdy = PointOrg(jNo).Y
                Dim DeltaX = (jdx - idx) * IIf(ScaleX > 0, ScaleX, 1)
                Dim DeltaY = (jdy - idy) * IIf(ScaleY > 0, ScaleY, 1)
                Dim Angle As Double = -Math.Atan2(DeltaY, DeltaX)


                Dim textAngle As Single = Angle * 180 / Math.PI + 90 '°


                'L1地震動
                If Step1 > 0 Then
                    With DB._SNAPDB.OutputInfo.StepCtrl(Step1)
                        'i端
                        Dim Xdi = idx + Math.Sin(Angle) * .MemberItem(m).Mi * MdScale
                        Dim Ydi = idy + Math.Cos(Angle) * .MemberItem(m).Mi * MdScale
                        'j端
                        Dim Xdj = jdx + Math.Sin(Angle) * .MemberItem(m).Mj * MdScale
                        Dim Ydj = jdy + Math.Cos(Angle) * .MemberItem(m).Mj * MdScale

                        preview.DrawLine(GetScalablePoint(New PointF(Xdi, Ydi)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Green)
                        preview.DrawLine(GetScalablePoint(New PointF(idx, idy)),
                                         GetScalablePoint(New PointF(Xdi, Ydi)),
                                         thickness2, Color.Green)
                        preview.DrawLine(GetScalablePoint(New PointF(jdx, jdy)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Green)

                        If printTxtCase = 1 Then
                            '文字
                            preview.FontColor = Brushes.Green

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdi, Ydi)))
                            preview.PrtText(.MemberItem(m).Mi.ToString("F2"), textAngle)

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdj, Ydj)))
                            preview.PrtText(.MemberItem(m).Mj.ToString("F2"), textAngle)

                        End If

                    End With

                End If
                '復旧性
                If Step2 > 0 Then
                    With DB._SNAPDB.OutputInfo.StepCtrl(Step2)
                        'i端
                        Dim Xdi = idx + Math.Sin(Angle) * .MemberItem(m).Mi * MdScale
                        Dim Ydi = idy + Math.Cos(Angle) * .MemberItem(m).Mi * MdScale
                        'j端
                        Dim Xdj = jdx + Math.Sin(Angle) * .MemberItem(m).Mj * MdScale
                        Dim Ydj = jdy + Math.Cos(Angle) * .MemberItem(m).Mj * MdScale

                        preview.DrawLine(GetScalablePoint(New PointF(Xdi, Ydi)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Blue)
                        preview.DrawLine(GetScalablePoint(New PointF(idx, idy)),
                                         GetScalablePoint(New PointF(Xdi, Ydi)),
                                         thickness2, Color.Blue)
                        preview.DrawLine(GetScalablePoint(New PointF(jdx, jdy)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Blue)

                        If printTxtCase = 2 Then
                            '文字
                            preview.FontColor = Brushes.Blue

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdi, Ydi)))
                            preview.PrtText(.MemberItem(m).Mi.ToString("F2"), textAngle)

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdj, Ydj)))
                            preview.PrtText(.MemberItem(m).Mj.ToString("F2"), textAngle)
                        End If
                    End With
                End If
                '安全性
                If Step3 > 0 Then
                    With DB._SNAPDB.OutputInfo.StepCtrl(Step3)
                        'i端
                        Dim Xdi = idx + Math.Sin(Angle) * .MemberItem(m).Mi * MdScale
                        Dim Ydi = idy + Math.Cos(Angle) * .MemberItem(m).Mi * MdScale
                        'j端
                        Dim Xdj = jdx + Math.Sin(Angle) * .MemberItem(m).Mj * MdScale
                        Dim Ydj = jdy + Math.Cos(Angle) * .MemberItem(m).Mj * MdScale

                        preview.DrawLine(GetScalablePoint(New PointF(Xdi, Ydi)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Red)
                        preview.DrawLine(GetScalablePoint(New PointF(idx, idy)),
                                         GetScalablePoint(New PointF(Xdi, Ydi)),
                                         thickness2, Color.Red)
                        preview.DrawLine(GetScalablePoint(New PointF(jdx, jdy)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Red)

                        If printTxtCase = 3 Then
                            '文字
                            preview.FontColor = Brushes.Red

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdi, Ydi)))
                            preview.PrtText(.MemberItem(m).Mi.ToString("F2"), textAngle)

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdj, Ydj)))
                            preview.PrtText(.MemberItem(m).Mj.ToString("F2"), textAngle)
                        End If
                    End With
                End If
                '要素番号を表示する。 ------------------------------------------------------------
                'preview.SetCurrentPos(New PointF(GetScalablePointX((idx + jdx) / 2), GetScalablePointY((idy + jdy) / 2)))
                'preview.PrtText(m)
            Next
        Catch ex As Exception
        Finally
            preview.FontSize = oldFontSize
            preview.FontColor = oldFontColor
        End Try

        '右下に凡例を表示する。 ------------------------------------------------------------
        print凡例(Step1, Step2, Step3)

    End Sub

    Private Sub printせん断力図(DB As CalculationCase)

        'タイトル出力 ----------------------------------------------------------------------
        preview.PrtText("せん断力図")
        preview.enter(1.5)

        'せん断力の最大値を計算し、せん断力図のスケールを決定する。 ---------------------------------
        Dim Step1 As Integer = IIf(DB._解析対象(5) = True, DB.応答値.復旧性_L1震度step, 0)
        Dim Step2 As Integer = IIf(DB._解析対象(0) = True, DB.応答値.復旧性_最大応答step, 0)
        Dim Step3 As Integer = IIf(DB._解析対象(1) = True, DB.応答値.安全性_最大応答step, 0)
        If Step1 + Step2 + Step3 = 0 Then Exit Sub

        Dim printTxtCase As Integer = 3 '文字を描くケース
        If Step3 = 0 Then
            printTxtCase = 2
            If Step2 = 0 Then
                printTxtCase = 1
            End If
        End If

        Dim VdScale As Single
        Try
            Dim MaxVd As Single = 0
            For m = 1 To DB._SNAPDB.GetMemberCount
                Dim iNo As Integer = DB._SNAPDB.GetMember(m).Itan
                Dim jNo As Integer = DB._SNAPDB.GetMember(m).Jtan
                If Step1 > 0 Then
                    MaxVd = Math.Max(MaxVd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step1).MemberItem(m).Si))
                    MaxVd = Math.Max(MaxVd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step1).MemberItem(m).Sj))
                End If
                If Step2 > 0 Then
                    MaxVd = Math.Max(MaxVd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step2).MemberItem(m).Si))
                    MaxVd = Math.Max(MaxVd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step2).MemberItem(m).Sj))
                End If
                If Step3 > 0 Then
                    MaxVd = Math.Max(MaxVd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step3).MemberItem(m).Si))
                    MaxVd = Math.Max(MaxVd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step3).MemberItem(m).Sj))
                End If
            Next

            Dim VdScaleX As Single
            Dim VdScaleY As Single
            If ScaleX < 0 Then
                VdScaleX = Margin / MaxVd
                VdScaleY = VdScaleX
            ElseIf ScaleY < 0 Then
                VdScaleY = Margin / MaxVd
                VdScaleX = VdScaleY
            Else
                VdScaleX = Margin / (MaxVd * ScaleX)
                VdScaleY = Margin / (MaxVd * ScaleY)
            End If

            VdScale = Math.Min(VdScaleX, VdScaleY) * ScaleAlpha
        Catch ex As Exception
        End Try

        'せん断力図を描画する。 ------------------------------------------------------------
        Dim oldFontSize = preview.FontSize
        Dim oldFontColor = preview.FontColor
        Try
            preview.FontSize = 8
            For m = 1 To DB._SNAPDB.GetMemberCount
                Dim iNo As Integer = DB._SNAPDB.GetMember(m).Itan
                Dim jNo As Integer = DB._SNAPDB.GetMember(m).Jtan

                '部材の角度を計算する。
                Dim idx = PointOrg(iNo).X
                Dim idy = PointOrg(iNo).Y
                Dim jdx = PointOrg(jNo).X
                Dim jdy = PointOrg(jNo).Y
                Dim DeltaX = (jdx - idx) * IIf(ScaleX > 0, ScaleX, 1)
                Dim DeltaY = (jdy - idy) * IIf(ScaleY > 0, ScaleY, 1)

                Dim Angle As Double = -Math.Atan2(DeltaY, DeltaX)
                Dim textAngle As Single = Angle * 180 / Math.PI + 90 '°

                'L1地震動
                If Step1 > 0 Then
                    With DB._SNAPDB.OutputInfo.StepCtrl(Step1)
                        'i端
                        Dim Xdi = idx + Math.Sin(Angle) * .MemberItem(m).Si * VdScale
                        Dim Ydi = idy + Math.Cos(Angle) * .MemberItem(m).Si * VdScale
                        'j端
                        Dim Xdj = jdx + Math.Sin(Angle) * .MemberItem(m).Sj * VdScale
                        Dim Ydj = jdy + Math.Cos(Angle) * .MemberItem(m).Sj * VdScale

                        preview.DrawLine(GetScalablePoint(New PointF(Xdi, Ydi)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Green)
                        preview.DrawLine(GetScalablePoint(New PointF(idx, idy)),
                                         GetScalablePoint(New PointF(Xdi, Ydi)),
                                         thickness2, Color.Green)
                        preview.DrawLine(GetScalablePoint(New PointF(jdx, jdy)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Green)

                        If printTxtCase = 1 Then
                            '文字
                            preview.FontColor = Brushes.Green

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdi, Ydi)))
                            preview.PrtText(.MemberItem(m).Si.ToString("F2"), textAngle)

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdj, Ydj)))
                            preview.PrtText(.MemberItem(m).Sj.ToString("F2"), textAngle)

                        End If

                    End With
                End If
                '復旧性
                If Step2 > 0 Then
                    With DB._SNAPDB.OutputInfo.StepCtrl(Step2)
                        'i端
                        Dim Xdi = idx + Math.Sin(Angle) * .MemberItem(m).Si * VdScale
                        Dim Ydi = idy + Math.Cos(Angle) * .MemberItem(m).Si * VdScale
                        'j端
                        Dim Xdj = jdx + Math.Sin(Angle) * .MemberItem(m).Sj * VdScale
                        Dim Ydj = jdy + Math.Cos(Angle) * .MemberItem(m).Sj * VdScale

                        preview.DrawLine(GetScalablePoint(New PointF(Xdi, Ydi)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Blue)
                        preview.DrawLine(GetScalablePoint(New PointF(idx, idy)),
                                         GetScalablePoint(New PointF(Xdi, Ydi)),
                                         thickness2, Color.Blue)
                        preview.DrawLine(GetScalablePoint(New PointF(jdx, jdy)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Blue)

                        If printTxtCase = 2 Then
                            '文字
                            preview.FontColor = Brushes.Blue

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdi, Ydi)))
                            preview.PrtText(.MemberItem(m).Si.ToString("F2"), textAngle)

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdj, Ydj)))
                            preview.PrtText(.MemberItem(m).Sj.ToString("F2"), textAngle)
                        End If

                    End With
                End If
                '安全性
                If Step3 > 0 Then
                    With DB._SNAPDB.OutputInfo.StepCtrl(Step3)
                        'i端
                        Dim Xdi = idx + Math.Sin(Angle) * .MemberItem(m).Si * VdScale
                        Dim Ydi = idy + Math.Cos(Angle) * .MemberItem(m).Si * VdScale
                        'j端
                        Dim Xdj = jdx + Math.Sin(Angle) * .MemberItem(m).Sj * VdScale
                        Dim Ydj = jdy + Math.Cos(Angle) * .MemberItem(m).Sj * VdScale

                        preview.DrawLine(GetScalablePoint(New PointF(Xdi, Ydi)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Red)
                        preview.DrawLine(GetScalablePoint(New PointF(idx, idy)),
                                         GetScalablePoint(New PointF(Xdi, Ydi)),
                                         thickness2, Color.Red)
                        preview.DrawLine(GetScalablePoint(New PointF(jdx, jdy)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Red)

                        If printTxtCase = 3 Then
                            '文字
                            preview.FontColor = Brushes.Red

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdi, Ydi)))
                            preview.PrtText(.MemberItem(m).Si.ToString("F2"), textAngle)

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdj, Ydj)))
                            preview.PrtText(.MemberItem(m).Sj.ToString("F2"), textAngle)
                        End If

                    End With
                End If
                '要素番号を表示する。 ------------------------------------------------------------
                'preview.SetCurrentPos(New PointF(GetScalablePointX((idx + jdx) / 2), GetScalablePointY((idy + jdy) / 2)))
                'preview.PrtText(m)
            Next
        Catch ex As Exception
        Finally
            preview.FontSize = oldFontSize
            preview.FontColor = oldFontColor
        End Try

        '右下に凡例を表示する。 ------------------------------------------------------------
        print凡例(Step1, Step2, Step3)

    End Sub

    Private Sub print軸方向力図(DB As CalculationCase)

        'タイトル出力 ----------------------------------------------------------------------
        preview.PrtText("軸方向力図")
        preview.enter(1.5)

        '軸方向力の最大値を計算し、軸方向力図のスケールを決定する。 ---------------------------------
        Dim Step1 As Integer = IIf(DB._解析対象(5) = True, DB.応答値.復旧性_L1震度step, 0)
        Dim Step2 As Integer = IIf(DB._解析対象(0) = True, DB.応答値.復旧性_最大応答step, 0)
        Dim Step3 As Integer = IIf(DB._解析対象(1) = True, DB.応答値.安全性_最大応答step, 0)
        If Step1 + Step2 + Step3 = 0 Then Exit Sub

        Dim printTxtCase As Integer = 3 '文字を描くケース
        If Step3 = 0 Then
            printTxtCase = 2
            If Step2 = 0 Then
                printTxtCase = 1
            End If
        End If

        Dim NdScale As Single
        Try
            Dim MaxNd As Single = 0
            For m = 1 To DB._SNAPDB.GetMemberCount
                Dim iNo As Integer = DB._SNAPDB.GetMember(m).Itan
                Dim jNo As Integer = DB._SNAPDB.GetMember(m).Jtan
                If Step1 > 0 Then
                    MaxNd = Math.Max(MaxNd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step1).MemberItem(m).Ni))
                    MaxNd = Math.Max(MaxNd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step1).MemberItem(m).Nj))
                End If
                If Step2 > 0 Then
                    MaxNd = Math.Max(MaxNd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step2).MemberItem(m).Ni))
                    MaxNd = Math.Max(MaxNd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step2).MemberItem(m).Nj))
                End If
                If Step3 > 0 Then
                    MaxNd = Math.Max(MaxNd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step3).MemberItem(m).Ni))
                    MaxNd = Math.Max(MaxNd, Math.Abs(DB._SNAPDB.OutputInfo.StepCtrl(Step3).MemberItem(m).Nj))
                End If
            Next

            Dim NdScaleX As Single
            Dim NdScaleY As Single
            If ScaleX < 0 Then
                NdScaleX = Margin / MaxNd * 0.6
                NdScaleY = NdScaleX
            ElseIf ScaleY < 0 Then
                NdScaleY = Margin / MaxNd * 0.6
                NdScaleX = NdScaleY
            Else
                NdScaleX = Margin / (MaxNd * ScaleX) * 0.6
                NdScaleY = Margin / (MaxNd * ScaleY) * 0.6
            End If

            NdScale = Math.Min(NdScaleX, NdScaleY) * ScaleAlpha

        Catch ex As Exception
        End Try

        '軸方向力図を描画する。 ------------------------------------------------------------
        Dim oldFontSize = preview.FontSize
        Dim oldFontColor = preview.FontColor
        Try
            preview.FontSize = 8
            For m = 1 To DB._SNAPDB.GetMemberCount
                Dim iNo As Integer = DB._SNAPDB.GetMember(m).Itan
                Dim jNo As Integer = DB._SNAPDB.GetMember(m).Jtan

                '部材の角度を計算する。
                Dim idx = PointOrg(iNo).X
                Dim idy = PointOrg(iNo).Y
                Dim jdx = PointOrg(jNo).X
                Dim jdy = PointOrg(jNo).Y
                Dim DeltaX = (jdx - idx) * IIf(ScaleX > 0, ScaleX, 1)
                Dim DeltaY = (jdy - idy) * IIf(ScaleY > 0, ScaleY, 1)
                Dim Angle As Double = -Math.Atan2(DeltaY, DeltaX)
                Dim textAngle As Single = Angle * 180 / Math.PI + 90 '°

                'L1地震動
                If Step1 > 0 Then
                    With DB._SNAPDB.OutputInfo.StepCtrl(Step1)
                        'i端
                        Dim Xdi = idx + Math.Sin(Angle) * .MemberItem(m).Ni * NdScale
                        Dim Ydi = idy + Math.Cos(Angle) * .MemberItem(m).Ni * NdScale
                        'j端
                        Dim Xdj = jdx + Math.Sin(Angle) * .MemberItem(m).Nj * NdScale
                        Dim Ydj = jdy + Math.Cos(Angle) * .MemberItem(m).Nj * NdScale

                        preview.DrawLine(GetScalablePoint(New PointF(Xdi, Ydi)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Green)
                        preview.DrawLine(GetScalablePoint(New PointF(idx, idy)),
                                         GetScalablePoint(New PointF(Xdi, Ydi)),
                                         thickness2, Color.Green)
                        preview.DrawLine(GetScalablePoint(New PointF(jdx, jdy)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Green)

                        If printTxtCase = 1 Then
                            '文字
                            preview.FontColor = Brushes.Green

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdi, Ydi)))
                            preview.PrtText(.MemberItem(m).Ni.ToString("F2"), textAngle)

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdj, Ydj)))
                            preview.PrtText(.MemberItem(m).Nj.ToString("F2"), textAngle)

                        End If

                    End With
                End If
                '復旧性
                If Step2 > 0 Then
                    With DB._SNAPDB.OutputInfo.StepCtrl(Step2)
                        'i端
                        Dim Xdi = idx + Math.Sin(Angle) * .MemberItem(m).Ni * NdScale
                        Dim Ydi = idy + Math.Cos(Angle) * .MemberItem(m).Ni * NdScale
                        'j端
                        Dim Xdj = jdx + Math.Sin(Angle) * .MemberItem(m).Nj * NdScale
                        Dim Ydj = jdy + Math.Cos(Angle) * .MemberItem(m).Nj * NdScale

                        preview.DrawLine(GetScalablePoint(New PointF(Xdi, Ydi)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Blue)
                        preview.DrawLine(GetScalablePoint(New PointF(idx, idy)),
                                         GetScalablePoint(New PointF(Xdi, Ydi)),
                                         thickness2, Color.Blue)
                        preview.DrawLine(GetScalablePoint(New PointF(jdx, jdy)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Blue)

                        If printTxtCase = 2 Then
                            '文字
                            preview.FontColor = Brushes.Blue

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdi, Ydi)))
                            preview.PrtText(.MemberItem(m).Ni.ToString("F2"), textAngle)

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdj, Ydj)))
                            preview.PrtText(.MemberItem(m).Nj.ToString("F2"), textAngle)
                        End If

                    End With
                End If
                '安全性
                If Step3 > 0 Then
                    With DB._SNAPDB.OutputInfo.StepCtrl(Step3)
                        'i端
                        Dim Xdi = idx + Math.Sin(Angle) * .MemberItem(m).Ni * NdScale
                        Dim Ydi = idy + Math.Cos(Angle) * .MemberItem(m).Ni * NdScale
                        'j端
                        Dim Xdj = jdx + Math.Sin(Angle) * .MemberItem(m).Nj * NdScale
                        Dim Ydj = jdy + Math.Cos(Angle) * .MemberItem(m).Nj * NdScale

                        preview.DrawLine(GetScalablePoint(New PointF(Xdi, Ydi)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Red)
                        preview.DrawLine(GetScalablePoint(New PointF(idx, idy)),
                                         GetScalablePoint(New PointF(Xdi, Ydi)),
                                         thickness2, Color.Red)
                        preview.DrawLine(GetScalablePoint(New PointF(jdx, jdy)),
                                         GetScalablePoint(New PointF(Xdj, Ydj)),
                                         thickness2, Color.Red)

                        If printTxtCase = 3 Then
                            '文字
                            preview.FontColor = Brushes.Red

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdi, Ydi)))
                            preview.PrtText(.MemberItem(m).Ni.ToString("F2"), textAngle)

                            preview.SetCurrentPos(GetScalablePoint(New PointF(Xdj, Ydj)))
                            preview.PrtText(.MemberItem(m).Nj.ToString("F2"), textAngle)
                        End If

                    End With
                End If
                '要素番号を表示する。 ------------------------------------------------------------
                'preview.SetCurrentPos(New PointF(GetScalablePointX((idx + jdx) / 2), GetScalablePointY((idy + jdy) / 2)))
                'preview.PrtText(m)
            Next
        Catch ex As Exception
        Finally
            preview.FontSize = oldFontSize
            preview.FontColor = oldFontColor
        End Try

        '右下に凡例を表示する。 ------------------------------------------------------------
        print凡例(Step1, Step2, Step3)

    End Sub

    Private Sub print凡例(復旧性_L1震度step As Integer, 復旧性_最大応答step As Integer, 安全性_最大応答step As Integer)
        Try
            Dim pageSize As SizeF = Me.preview.GetPageSize
            Dim x As Single = pageSize.Width
            Dim y As Single = pageSize.Height
            'L1地震動
            If 復旧性_L1震度step > 0 Then
                preview.DrawLine(New PointF(x - 170, y - 80), New PointF(x - 120, y - 80), thickness2, Color.Green)
                preview.SetCurrentPos(New PointF(x - 100, y - 85))
                preview.PrtText("復旧性L1地震動")
            End If
            '復旧性
            If 復旧性_最大応答step > 0 Then
                preview.DrawLine(New PointF(x - 170, y - 70), New PointF(x - 120, y - 70), thickness2, Color.Blue)
                preview.SetCurrentPos(New PointF(x - 100, y - 75))
                preview.PrtText("復旧性L2地震動")
            End If
            '安全性
            If 安全性_最大応答step > 0 Then
                preview.DrawLine(New PointF(x - 170, y - 60), New PointF(x - 120, y - 60), thickness2, Color.Red)
                preview.SetCurrentPos(New PointF(x - 100, y - 65))
                preview.PrtText("安全性")
            End If

        Catch ex As Exception

        End Try
    End Sub

    Private Sub Create損傷状況(DB As CalculationCase)
        'タイトル出力 ----------------------------------------------------------------------
        preview.PrtText("損傷状況図")
        preview.enter(1.5)
        Dim Step1 As Integer = IIf(DB._解析対象(5) = True, DB.応答値.復旧性_L1震度step, 0)
        Dim Step2 As Integer = IIf(DB._解析対象(0) = True, DB.応答値.復旧性_最大応答step, 0)
        Dim Step3 As Integer = IIf(DB._解析対象(1) = True, DB.応答値.安全性_最大応答step, 0)
        If Step1 + Step2 + Step3 = 0 Then Exit Sub

        Dim iStep As Integer = Math.Max(Math.Max(Step1, Step2), Step3)
        Dim MθList As New Dictionary(Of PointF, Brush)
        Try
            For m = 1 To DB._SNAPDB.GetMemberCount
                Dim iNo As Integer = DB._SNAPDB.GetMember(m).Itan
                Dim jNo As Integer = DB._SNAPDB.GetMember(m).Jtan
                Dim idx = PointOrg(iNo).X
                Dim idy = PointOrg(iNo).Y
                Dim jdx = PointOrg(jNo).X
                Dim jdy = PointOrg(jNo).Y
                Dim ix As Integer = GetScalablePointX(idx)
                Dim iy As Integer = GetScalablePointY(idy)
                Dim jx As Integer = GetScalablePointX(jdx)
                Dim jy As Integer = GetScalablePointY(jdy)
                '要素番号を表示する。 ------------------------------------------------------------
                preview.SetCurrentPos(New PointF(GetScalablePointX((idx + jdx) / 2), GetScalablePointY((idy + jdy) / 2)))

                '損傷レベルを調べる -------------------------------------------------------------------------------------------
                Dim 損傷レベル = DB._SNAPDB.OutputInfo.StepCtrl(iStep).MemberItem(m).SCFlg
                Dim LineColor As Color
                Dim BrushColor As Brush
                Select Case 損傷レベル
                    Case 1
                        LineColor = Color.Green
                        BrushColor = Brushes.Green
                    Case 2
                        LineColor = Color.Blue
                        BrushColor = Brushes.Blue
                        preview.PrtText(m)
                    Case 3
                        LineColor = Color.Yellow
                        BrushColor = Brushes.Yellow
                        preview.PrtText(m)
                    Case 4
                        LineColor = Color.Red
                        BrushColor = Brushes.Red
                        preview.PrtText(m)
                    Case Else
                        LineColor = Color.Gray
                        BrushColor = Brushes.Gray
                End Select
                preview.DrawLine(New PointF(ix, iy), New PointF(jx, jy), 3, LineColor)


                'M-θ部材なら○を描くリストに登録する -----------------------------------------------
                If DB._SNAPDB.GetApIK(DB._SNAPDB.GetDLNo(m)) = 1 Then
                    Dim x = (ix + jx) / 2
                    Dim y = (iy + jy) / 2
                    MθList.Add(New PointF(x, y), BrushColor)
                End If
            Next
            'M-θ部材の○を描く
            For Each p In MθList
                preview.DrawCircle(p.Key, New SizeF(8, 8), p.Value)
            Next
        Catch ex As Exception

        End Try

        '凡例を表示する。
        Try
            Dim pageSize As SizeF = Me.preview.GetPageSize
            Dim x As Single = pageSize.Width
            Dim y As Single = pageSize.Height

            '剛域
            preview.DrawLine(New PointF(x - 170, y - 100), New PointF(x - 120, y - 100), thickness2, Color.Gray)
            preview.SetCurrentPos(New PointF(x - 100, y - 105))
            preview.PrtText("剛域")

            '損傷レベル１
            preview.DrawLine(New PointF(x - 170, y - 90), New PointF(x - 120, y - 90), thickness2, Color.Green)
            preview.SetCurrentPos(New PointF(x - 100, y - 95))
            preview.PrtText("損傷レベル１")

            '損傷レベル２
            preview.DrawLine(New PointF(x - 170, y - 80), New PointF(x - 120, y - 80), thickness2, Color.Blue)
            preview.SetCurrentPos(New PointF(x - 100, y - 85))
            preview.PrtText("損傷レベル２")

            '損傷レベル３
            preview.DrawLine(New PointF(x - 170, y - 70), New PointF(x - 120, y - 70), thickness2, Color.Yellow)
            preview.SetCurrentPos(New PointF(x - 100, y - 75))
            preview.PrtText("損傷レベル３")

            '損傷レベル４
            preview.DrawLine(New PointF(x - 170, y - 60), New PointF(x - 120, y - 60), thickness2, Color.Red)
            preview.SetCurrentPos(New PointF(x - 100, y - 65))
            preview.PrtText("損傷レベル４")

        Catch ex As Exception

        End Try

    End Sub

#End Region



End Class



