Imports System.ComponentModel
Imports AdvanceSoftware.VBReport8
Imports iTextSharp.text.pdf
Imports System.IO
Imports SNAPDBLib.CSNAPDB
Imports Microsoft.VisualBasic.Devices
Imports Org.BouncyCastle.Asn1.Utilities

Class PDFReportPileAnchorBar
    Inherits ExcelReport

#Region "メンバ変数"

    '出力制御に関する変数 ------------------------------------------
    Private preview As WinPDFPrint.PrintPreview
    Private InputFile As List(Of String)

    Private 出力モード As Integer

    '段落し図に関する変数 ------------------------------------------
    Private Const PrintPileHeight = 550     'A4用紙に対する杭の長さ
    Private Const thicknessYAxis = 1        'Y軸図 の線の太さ pixcel
    Private Const thicknessMd = 0.7         'モーメント線の太さ pixcel
    Private Const IntervalMdLine = 3        'モーメント線の点線の間隔
    Private Const thicknessDLine = 0.2      '寸法線の太さ pixcel
    Private Const YPosPileHead = 200        '杭頭 の Y位置
    Private Const XPosMd = 220              'モーメント図Y軸 の X位置
    Private Const XPosMaxMd = 350           '最大発生モーメント の X位置
    Private Const XPosNd = 120              '軸力図Y軸 の X位置
    Private Const XPosMaxNd = 160           '最大発生軸力 の X位置
    Private Const XPosPile1 = 435           '杭の姿 の位置
    Private Const XPosPile2 = 450           '杭の姿 の位置
    Private Const XPosDLine1 = 485          '寸法線1 の位置
    Private Const XPosDLine2 = 500          '寸法線2 の位置
    Private Const XPosDLine3 = 515          '寸法線3 の位置
    Private Const XPosDLine4 = 530          '寸法線4 の位置
    Private Const ArrowSize = 4             '寸法線 の矢印のサイズ

    Private iType As Integer '54：RC杭, 62：鋼管杭

#End Region

#Region "計算書作成スタート関数"

    Public Sub 計算書作成スタート(Optional ShowMode As Integer = 0)

        Me.出力モード = ShowMode
        InputFile = New List(Of String)

        '①:応答値を計算する(プレビューなしで) -----------------------------------------------
        Try
            Call MyBase.総括表作成スタート(3)
        Catch ex As Exception
            Throw ex
        End Try
        If MyBase.CaseDB.Count = 0 Then Return

        '②:段落し計算書を出力する。 -----------------------------------------------------------------
        Dim pb As New MyProgressBarForm("", New DoWorkEventHandler(AddressOf Create段落し計算書))
        pb.ShowDialog()

        If Me.InputFile.Count < 1 Then
            MsgBox("段落し計算書で出力できるものがありません。" + vbLf _
                                            + "ヒント:必要な入力項目に不足が無いか確認してください。")
            Return
        End If


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

            Throw New Exception("段落し計算書の出力に失敗しました。" + vbLf _
                                + ex.Message)
        End Try

    End Sub

    Private Sub deleteFiles(InputFile As List(Of String))
        On Error Resume Next
        For Each BatchPdf In InputFile
            System.IO.File.Delete(BatchPdf)
        Next
    End Sub

#End Region

#Region "段落し図作成"

    Private Sub Create段落し計算書(ByVal sender As Object, ByVal e As DoWorkEventArgs)

        If MyBase.CaseDB Is Nothing Then Return
        Dim bw As BackgroundWorker = DirectCast(sender, BackgroundWorker)
        Dim counter As Integer = 0
        Dim i As Integer
        Dim pileNo As Integer

        Try
            For i = 0 To MyBase.CaseDB.Count - 1
                counter += 1

                Dim CaseDB = MyBase.CaseDB(i)

                If CaseDB._解析対象(7) <> True Then
                    Continue For
                End If

                Dim CountString = String.Format("{0}/{1}", counter, MyBase.CaseDB.Count)
                bw.ReportProgress(0, "段落し図生成中... " + CountString)

                '段落し応力図 ------------------------------------------------------------------------------------------
                Me.preview = New WinPDFPrint.PrintPreview

                Dim firstFlg As Boolean = False
                For id = 0 To Input.Data.PileAnchorBar.段落し情報.Count - 1
                    Dim pileType = Input.Data.PileAnchorBar.段落し情報(id)
                    For pileNo = 0 To pileType.dgMemberFirst.Count - 1

                        If i = 0 And pileNo = 2 Then
                            i = i
                        End If
                        If firstFlg = False Then
                            firstFlg = True
                        Else
                            preview.NewPage()
                        End If
                        If Create段落し図(CaseDB, pileType, pileNo) = -1 Then
                            preview.DeletePage()
                        End If
                    Next
                Next
                If Me.preview.GetPageCount < 1 Then
                    Me.preview = Nothing
                    Return
                End If
                Dim FN1 As String = String.Format("{0}{1}{2}{3}", common.TempPath, "\BatchPrintPile", counter, ".pdf")
                Try
                    System.IO.File.Delete(FN1)
                    Me.preview.SavePDF(FN1)
                    InputFile.Add(FN1)
                Catch ex As Exception
                    Throw New Exception("杭の段落しの出力に失敗しました。" + vbLf _
                                        + "ヒント:「" + FN1 + "」ファイルが閉じているか確認してください。")
                Finally
                    Me.preview = Nothing
                End Try
            Next
        Catch ex As Exception
            Console.WriteLine(i)
            Throw ex
        End Try
    End Sub

    ''' <summary>
    ''' 段落し図を作成する。
    ''' </summary>
    ''' <param name="DB"></param>
    ''' <param name="pileType"></param>
    ''' <param name="pileNo"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Create段落し図(DB As CalculationCase, pileType As clsPileAnchorBar, pileNo As Integer) As Integer

        '情報の集計 ----------------------------------------------------------------------------------------------------------------------------------------------

        ''杭の形状情報 ***********
        Dim FirstMemberNo As Integer                '杭頭の要素番号
        Dim LastMemberNo As Integer                 '杭先端の要素番号
        Dim MemberNumber As Single                  '杭の奥行き本数
        Dim PileLength As Single                    '杭長
        Dim ScaleY As Single                        '縦方向のスケール
        Dim YPoint As New List(Of Single)           '各節点の高さ位置

        If Set杭の形状情報(DB, pileType, pileNo,
                           FirstMemberNo, LastMemberNo, MemberNumber,
                           YPoint, PileLength, ScaleY) = -1 Then Return -1

        If FirstMemberNo * LastMemberNo = 0 Then Return -1


        '応答値 ***********
        Dim iStep As Integer
        Dim iStepTitle As String = ""
        Dim MemberLevel As New List(Of Integer)     '各部材の損傷レベル

        Dim Mdi As New List(Of Single)        'i端の最大曲げモーメント
        Dim Mdj As New List(Of Single)        'j端の最大曲げモーメント
        Dim Ndi As New List(Of Single)        'i端の最大曲げモーメント時の軸圧縮力
        Dim Ndj As New List(Of Single)        'j端の最大曲げモーメント時の軸圧縮力
        Dim Mudi As New List(Of Single)        'i端の最大曲げモーメント時の終局耐力
        Dim Mudj As New List(Of Single)        'j端の最大曲げモーメント時の終局耐力
        Dim Mydi As New List(Of Single)        'i端の最大曲げモーメント時の降伏耐力
        Dim Mydj As New List(Of Single)        'j端の最大曲げモーメント時の降伏耐力

        Dim DiscontinuityIndex = Set応答値(DB,
                     FirstMemberNo, LastMemberNo, MemberNumber,
                     iStep, iStepTitle, MemberLevel,
                     Mdi, Mdj,
                     Ndi, Ndj,
                     Mudi, Mudj,
                     Mydi, Mydj,
                     YPoint)


        '各点の段落し後の耐力を計算する。 ***********
        Dim Mudi2 As New List(Of Single)        'i端の段落し後の終局耐力
        Dim Mudj2 As New List(Of Single)        'j端の段落し後の終局耐力
        Dim Mydi2 As New List(Of Single)        'i端の段落し後の降伏耐力
        Dim Mydj2 As New List(Of Single)        'j端の段落し後の降伏耐力
        Dim dummy As New List(Of Integer)

        Dim iType = Set段落し後の耐力(DB, pileType, dummy,
                         FirstMemberNo, LastMemberNo, MemberNumber,
                         Ndi, Ndj,
                         Mudi2, Mudj2,
                         Mydi2, Mydj2,
                         YPoint)

        If iType = -1 Then Return -1

        If iType = 62 Then
            '鋼管杭の場合 終局耐力Mud は使わないので 降伏耐力 Myd に置き換える
            Mudi = Mydi
            Mudj = Mydj

            '下杭の耐力は、段落とし位置を決めるため 継手の低減係数を考慮しておく
            Mudi2.Clear()
            For i = 0 To Mydi2.Count - 1
                Mudi2.Add(Mydi2(i) * pileType._ReductionCoefficient)
            Next
            Mudj2.Clear()
            For i = 0 To Mydj2.Count - 1
                Mudj2.Add(Mydj2(i) * pileType._ReductionCoefficient)
            Next

            pileType.MudCoefficient = 1

        End If

        '段落し位置を決定する。 ***********
        Dim Mud2CrossPoint As PointF
        Dim Myd2CrossPoint As PointF
        Dim Md2Point As PointF  '杭頭から段落し点までの位置


        If pileType._ParagraphLength > 0 Then
            '段落とし位置が入力されてた場合
            Md2Point.Y = Convert.ToSingle(pileType.ParagraphLength) / 1000

            If iType = 62 Then
                Md2Point.Y -= (pileType._PileHeadLength / 1000)
                Md2Point.Y -= (pileType._DisabledDownwardFromCutOffPoint / 1000)
            End If

            'X座標の計算
            For index = 0 To Mdi.Count - 1
                If YPoint(index + 1) >= Md2Point.Y Then
                    Dim pri As New PointF With {
                        .X = Math.Abs(Mdi(index)),
                        .Y = YPoint(index)
                    }
                    Dim prj As New PointF With {
                        .X = Math.Abs(Mdj(index)),
                        .Y = YPoint(index + 1)
                    }
                    Md2Point.X = prj.X + (pri.X - prj.X) / (pri.Y - prj.Y) * (Md2Point.Y - prj.Y)
                    Exit For
                End If
            Next



        Else
            ' Mud に関する処理
            Mud2CrossPoint = Set交差位置(YPoint,
                                     Mdi,
                                     Mdj,
                                     Mudi2,
                                     Mudj2,
                                     pileType.MudCoefficient,
                                     1, False)

            If iType = 62 Then
                '段落とし位置を決めるために低減しておいた下杭の耐力を
                '鋼管杭の場合 終局耐力Mud は使わないので 降伏耐力 Myd に置き換える
                Mudi2 = Mydi2
                Mudj2 = Mydj2
            End If

            If Mud2CrossPoint.IsEmpty Then
                Try
                    Mud2CrossPoint.Y = Convert.ToSingle(pileType._DisabledFromPileHead) / 1000
                Catch ex As Exception
                    Mud2CrossPoint.Y = 0
                End Try
            End If

            ' Myd に関する処理
            Myd2CrossPoint = Set交差位置(YPoint,
                                     Mdi,
                                     Mdj,
                                     Mydi2,
                                     Mydj2,
                                     1,
                                     1, False)

            If Myd2CrossPoint.IsEmpty Then
                Myd2CrossPoint.Y = 0
            End If

            If Mud2CrossPoint.Y > Myd2CrossPoint.Y Then
                Md2Point = Mud2CrossPoint
            Else
                Md2Point = Myd2CrossPoint
            End If
        End If


        Md2Point.Y = RoundUp(Md2Point.Y, 3)
        Md2Point.X = RoundDown(Md2Point.X, 2)

        If Md2Point.Y > PileLength Then
            Md2Point.Y = PileLength
            Md2Point.X = -1
        End If
        If Md2Point.Y <= 0 Then
            Md2Point.Y = 0
            Md2Point.X = -1
        End If


        '継ぎ手を設けない範囲を決定する。 ***********
        Dim NGArea As New DisabledArea

        Dim JointMudi As New List(Of Single)        'i端の終局耐力 × 継ぎ手低減係数
        Dim JointMudj As New List(Of Single)        'j端の終局耐力 × 継ぎ手低減係数
        Dim JointMydi As New List(Of Single)        'i端の降伏耐力
        Dim JointMydj As New List(Of Single)        'j端の降伏耐力

        Dim JointMudi2 As New List(Of Single)        'i端の段落し後の終局耐力 × 継ぎ手低減係数
        Dim JointMudj2 As New List(Of Single)        'j端の段落し後の終局耐力 × 継ぎ手低減係数

        If Set継ぎ手を設けない範囲(pileType,
                                   PileLength, YPoint, Md2Point.Y,
                                   Mdi, Mdj,
                                   Mudi, Mudj,
                                   Mydi, Mydj,
                                   Mudi2, Mudj2,
                                   NGArea,
                                   JointMudi, JointMudj,
                                   JointMydi, JointMydj,
                                   JointMudi2, JointMudj2,
                                   iType) = -1 Then Return -1

        '最大値を集計しXスケールを決定する。 ***********
        Dim MdScaleX As Single              '曲げモーメントのスケール
        Dim NdScaleX As Single              '軸力のスケール
        Dim MaxMdvale As Single             '最大曲げの値
        Dim MaxMdvale1 As PointF            '最大発生曲げモーメントの値
        Dim MaxMdvale2 As PointF            '発生曲げモーメント２番目の値

        If SetScaleX(YPoint, Md2Point.Y,
                     Mdi, Mdj,
                     Ndi, Ndj,
                     Mudi, Mudj,
                     Mydi, Mydj,
                     Mudi2, Mudj2,
                     Mydi2, Mydj2,
                     MdScaleX, NdScaleX,
                     MaxMdvale, MaxMdvale1, MaxMdvale2) = -1 Then Return -1



        '出力（印字） ----------------------------------------------------------------------------------------------------------------------------------------------
        Try
            'タイトルの印字 ***********
            If Printタイトル(DB, pileNo, FirstMemberNo, LastMemberNo, iStep, iStepTitle) = -1 Then Return -1

            'ここからフォントサイズ 9pt ***********
            Dim oldFontSize = Me.preview.FontSize
            Me.preview.FontSize = 9
            Try
                '縦軸の印字 ***********
                If Print縦軸(YPoint, MemberLevel, ScaleY, MaxMdvale, MdScaleX) = -1 Then Return -1

                '発生断面力の印字 ***********
                If print軸力(Ndi, Ndj, YPoint, ScaleY, NdScaleX) = -1 Then Return -1
                If Print曲げモーメント(Mdi, Mdj, YPoint, ScaleY, MdScaleX, pileType) = -1 Then Return -1

                '最大発生モーメントの印字 ***********
                If Print最大曲げモーメント(PileLength, NGArea, ScaleY, MdScaleX, MaxMdvale1, MaxMdvale2, iType) = -1 Then Return -1

                '段落し前の耐力の印字 ***********
                Dim MydValuePoint As New PointF '段落し位置の降伏耐力
                Dim MudValuePoint As New PointF '段落し位置の終局耐力
                If Md2Point.Y > 0 Then
                    '降伏曲げ耐力
                    If Print曲げ耐力(Mydi, Mydj, YPoint, ScaleY, MdScaleX, DiscontinuityIndex, , Md2Point.Y, , MudValuePoint.Y, MudValuePoint.X) = -1 Then Return -1
                    '終局曲げ耐力
                    If Print曲げ耐力(Mudi, Mudj, YPoint, ScaleY, MdScaleX, DiscontinuityIndex, , Md2Point.Y, , MydValuePoint.Y, MydValuePoint.X, (iType <> 62)) = -1 Then Return -1
                    '継ぎ手判定耐力
                    If Print曲げ耐力(JointMudi, JointMudj, YPoint, ScaleY, MdScaleX, DiscontinuityIndex, , Md2Point.Y, 1) = -1 Then Return -1
                End If
                '段落し後の耐力の印字 ***********
                Dim Myd2ValuePoint As New PointF '段落し位置の降伏耐力
                Dim Mud2ValuePoint As New PointF '段落し位置の終局耐力
                If Md2Point.Y <= PileLength Then
                    '降伏曲げ耐力
                    If Print曲げ耐力(Mydi2, Mydj2, YPoint, ScaleY, MdScaleX, DiscontinuityIndex, Md2Point.Y, , , Myd2ValuePoint.Y, Myd2ValuePoint.X) = -1 Then Return -1
                    '終局曲げ耐力
                    If Print曲げ耐力(Mudi2, Mudj2, YPoint, ScaleY, MdScaleX, DiscontinuityIndex, Md2Point.Y, , , Mud2ValuePoint.Y, Mud2ValuePoint.X, (iType <> 62)) = -1 Then Return -1
                    '継ぎ手判定耐力
                    If Print曲げ耐力(JointMudi2, JointMudj2, YPoint, ScaleY, MdScaleX, DiscontinuityIndex, Md2Point.Y, , 1) = -1 Then Return -1
                End If
                '段落し位置の横線と耐力の引き出し印字 ***********
                If Md2Point.Y > 0 And Md2Point.Y <= PileLength Then
                    If Print曲げ耐力Border(YPoint, Md2Point, NGArea.CutOffPoint,
                                              ScaleY, MdScaleX,
                                              Mud2ValuePoint, Myd2ValuePoint,
                                              MydValuePoint, MudValuePoint, iType) = -1 Then Return -1
                End If

                'Mud, Myd, 0.9Mud の印字 ***********
                '最大発生モーメント
                Dim pointY2 As Single = 0
                If MaxMdvale1.Y = 0 Then
                    pointY2 = MaxMdvale2.Y
                ElseIf MaxMdvale2.Y = 0 Then
                    pointY2 = MaxMdvale1.Y
                Else
                    pointY2 = MaxMdvale1.Y
                End If

                If Md2Point.Y > pointY2 / 2 Then
                    If Print曲げ耐力記号(pileType, Md2Point,
                                         YPoint, ScaleY, MdScaleX,
                                         Mudi, Mudj, Mydi, Mydj, JointMudi, JointMudj,
                                                   MaxMdvale1, MaxMdvale2, iType) = -1 Then Return -1
                Else
                    If Print曲げ耐力記号(pileType, Md2Point,
                                        YPoint, ScaleY, MdScaleX,
                                        Mudi2, Mudj2, Mydi2, Mydj2, JointMudi2, JointMudj2,
                                                  MaxMdvale1, MaxMdvale2, iType) = -1 Then Return -1
                End If



                '杭の姿の印字 ***********
                If Print杭形状(pileType, PileLength, Md2Point, NGArea, ScaleY, iType) = -1 Then Return -1

                '継ぎ手を設けない範囲の印字 ***********
                If print継ぎ手を設けない範囲(PileLength, NGArea, ScaleY, MdScaleX, iType) = -1 Then Return -1

                '鉄筋全長の印字 ***********
                If 段落し鉄筋(pileType, PileLength, NGArea, Md2Point, ScaleY) = -1 Then Return -1

            Catch ex As Exception
                Throw ex
            Finally
                Me.preview.FontSize = oldFontSize
            End Try

        Catch ex As Exception
            Throw ex
        End Try
        Return 1

    End Function


#Region "情報の集計関数群"

    ''' <summary>
    ''' 杭の形状情報
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Set杭の形状情報(ByVal DB As CalculationCase, ByVal pileType As clsPileAnchorBar, ByVal pileNo As Integer,
                                     ByRef FirstMemberNo As Integer, ByRef LastMemberNo As Integer, ByRef MemberNumber As Single,
                                     ByRef YPoint As List(Of Single), ByRef PileLength As Single, ByRef ScaleY As Single) As Integer
        Try
            FirstMemberNo = pileType.dgMemberFirst(pileNo)
            LastMemberNo = pileType.dgMemberLast(pileNo)
            MemberNumber = pileType.dgMemberNumber(pileNo)
            Dim tpl As Single = 0
            YPoint.Add(0)
            For mNo = FirstMemberNo To LastMemberNo
                tpl += DB._SNAPDB.GetMemberLength(mNo)
                YPoint.Add(tpl)
            Next
            PileLength = tpl
            ScaleY = PrintPileHeight / PileLength
        Catch ex As Exception
            Throw ex
        End Try
        Return 1
    End Function

    ''' <summary>
    ''' 応答値
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Set応答値(ByVal DB As CalculationCase,
                               ByVal FirstMemberNo As Integer, ByVal LastMemberNo As Integer, ByVal MemberNumber As Single,
                               ByRef iStep As Integer, ByRef iStepTitle As String, ByRef MemberLevel As List(Of Integer),
                               ByRef MemberMdi As List(Of Single), ByRef MemberMdj As List(Of Single),
                               ByRef MemberNdi As List(Of Single), ByRef MemberNdj As List(Of Single),
                               ByRef MemberMudi As List(Of Single), ByRef MemberMudj As List(Of Single),
                               ByRef MemberMydi As List(Of Single), ByRef MemberMydj As List(Of Single),
                               ByVal YPoint As List(Of Single)) As List(Of Integer)

        Dim DiscontinuityIndex = New List(Of Integer)

        Try
            Dim Step2 As Integer
            Dim Step2Title As String = ""
            If DB._解析対象(0) = True Then
                Step2 = DB.応答値.復旧性_最大応答step
                Step2Title = "復旧性（最大応答時）"
                If Step2 > DB.応答値.最大震度step Then
                    Step2 = DB.応答値.最大震度step
                    Step2Title = "最大震度時"
                End If
            Else
                Step2 = 0
            End If

            Dim Step3 As Integer
            Dim Step3Title As String = ""
            If DB._解析対象(1) = True Then
                Step3 = DB.応答値.安全性_最大応答step

                Step3Title = "安全性（最大応答時）"
                If Step3 > DB.応答値.最大震度step Then
                    Step3 = DB.応答値.最大震度step
                    Step3Title = "最大震度時"
                End If
            Else
                Step3 = 0
            End If
            If Step2 > Step3 Then
                iStep = Step2
                iStepTitle = Step2Title
            Else
                iStep = Step3
                iStepTitle = Step3Title
            End If
            If iStep = 0 Then Return DiscontinuityIndex

        Catch ex As Exception
            Throw ex
        End Try


        '最大発生曲げモーメントとその時の軸力を調べる
        Try
            For mNo = FirstMemberNo To LastMemberNo
                Dim Mdi As Single = 0  'i端の最大曲げモーメント
                Dim Mdj As Single = 0  'j端の最大曲げモーメント
                Dim Ndi As Single = 0  'i端の最大曲げモーメント時の軸圧縮力
                Dim Ndj As Single = 0  'j端の最大曲げモーメント時の軸圧縮力
                Dim Mudi As Single = 0  'i端の最大曲げモーメント時の終局耐力
                Dim Mudj As Single = 0  'j端の最大曲げモーメント時の終局耐力
                Dim Mydi As Single = 0  'i端の最大曲げモーメント時の降伏耐力
                Dim Mydj As Single = 0  'j端の最大曲げモーメント時の降伏耐力
                Dim tmpAzeni As Single = 0
                Dim tmpAzenj As Single = 0
                Dim si As Integer
                Dim sj As Integer
                For i = iStep To iStep 'For i = 0 To iStep

                    With DB._SNAPDB.OutputInfo.StepCtrl(i).MemberItem(mNo)
                        Dim Mud As Single
                        Dim Myd As Single
                        'i端の安全率が最大となるステップを探す 
                        If .Mi > 0 Then
                            Mud = Math.Max(Math.Max(.M_Shousa_S.M1, .M_Shousa_S.M2), Math.Max(.M_Shousa_S.M3, .M_Shousa_S.M4))
                        Else
                            Mud = Math.Min(Math.Min(.M_Shousa_F.M1, .M_Shousa_F.M2), Math.Min(.M_Shousa_F.M3, .M_Shousa_F.M4))
                        End If
                        Myd = .My
                        If Myd = 0 Then
                            If .Mi > 0 Then
                                Myd = .M_Shousa_S.M1
                            Else
                                Myd = .M_Shousa_F.M1
                            End If
                        End If
                        'Dim Ai As Single = .Mi / Mud
                        'If tmpAzeni < Ai Then
                        Mdi = .Mi
                        Ndi = .Ni
                        Mudi = Math.Abs(Mud)
                        Mydi = Math.Abs(Myd)
                        'tmpAzeni = Ai
                        si = i
                        'End If

                        'j端の安全率が最大となるステップを探す 
                        If .Mj > 0 Then
                            Mud = Math.Max(Math.Max(.M_Shousa_S.M1, .M_Shousa_S.M2), Math.Max(.M_Shousa_S.M3, .M_Shousa_S.M4))
                        Else
                            Mud = Math.Min(Math.Min(.M_Shousa_F.M1, .M_Shousa_F.M2), Math.Min(.M_Shousa_F.M3, .M_Shousa_F.M4))
                        End If
                        Myd = .My
                        If Myd = 0 Then
                            If .Mj > 0 Then
                                Myd = .M_Shousa_S.M1
                            Else
                                Myd = .M_Shousa_F.M1
                            End If
                        End If
                        'Dim Aj As Single = .Mj / Mud
                        'If tmpAzenj < Aj Then
                        Mdj = .Mj
                        Ndj = .Nj
                        Mudj = Math.Abs(Mud)
                        Mydj = Math.Abs(Myd)
                        'tmpAzenj = Aj
                        sj = i
                        'End If
                    End With
                Next
                'If si < sj Then Stop 'i端とj端のステップ数が違う場合を調べた
                '最大値を集計する。
                MemberMdi.Add(Mdi / MemberNumber)
                MemberMdj.Add(Mdj / MemberNumber)
                MemberNdi.Add(Ndi / MemberNumber)
                MemberNdj.Add(Ndj / MemberNumber)
                MemberMudi.Add(Mudi / MemberNumber)
                MemberMudj.Add(Mudj / MemberNumber)
                MemberMydi.Add(Mydi / MemberNumber)
                MemberMydj.Add(Mydj / MemberNumber)
            Next

            If FormSettings.Option_杭頭の軸力でモーメント図を描く > 0 Then
                Dim Mud = MemberMudi.First
                For i = 0 To MemberMudi.Count - 1
                    MemberMudi(i) = Mud
                Next
                For i = 0 To MemberMudj.Count - 1
                    MemberMudj(i) = Mud
                Next
                Dim Myd = MemberMydi.First
                For i = 0 To MemberMydi.Count - 1
                    MemberMydi(i) = Myd
                Next
                For i = 0 To MemberMydj.Count - 1
                    MemberMydj(i) = Myd
                Next
            End If

            If FormSettings.Option_杭の抵抗モーメント図は杭頭本数 > 0 Then
                '杭頭の鉄筋と地中部の鉄筋が異なっている場合
                '杭頭の鉄筋で段落し前の耐力線を決定する。

                'SNAPから断面情報を取得
                Dim DLNo1 = DB._SNAPDB.GetDLNo(FirstMemberNo)
                Dim DL1 As IDLInfo = DB._SNAPDB.DLInfo(DLNo1)
                Dim Dia1 = DL1.StlBar.Item(0).Dia
                Dim n1 = DL1.StlBar.Item(0).Num

                Dim i As Integer = 0
                Dim DLNo2 = DLNo1
                For mNo = FirstMemberNo To LastMemberNo
                    Dim DLNo = DB._SNAPDB.GetDLNo(mNo)
                    If DLNo2 <> DLNo Then
                        Dim DL2 As IDLInfo = DB._SNAPDB.DLInfo(DLNo)
                        Dim Dia2 = DL2.StlBar.Item(0).Dia
                        Dim n2 = DL2.StlBar.Item(0).Num
                        If Dia1 <> Dia2 OrElse n1 <> n2 Then
                            MemberMudi(i) = MemberMudj(i - 1)
                            MemberMudj(i) = MemberMudi(i - 1) + (YPoint(i) - YPoint(i - 2)) * (MemberMudj(i - 1) - MemberMudi(i - 1)) / (YPoint(i - 1) - YPoint(i - 2))
                            MemberMydi(i) = MemberMydj(i - 1)
                            MemberMydj(i) = MemberMydi(i - 1) + (YPoint(i) - YPoint(i - 2)) * (MemberMydj(i - 1) - MemberMydi(i - 1)) / (YPoint(i - 1) - YPoint(i - 2))
                        Else
                            DLNo2 = DLNo
                        End If
                    End If
                    i += 1
                Next

            End If

            '-----------------------------------------------------------------------------------------
            '鋼管杭の杭頭剛域部など、
            '耐力が計算できない場合に 0 としておいた値は、
            '１つ上の値と同じとする
            For Each target In New List(Of Single)() {MemberMudi, MemberMudj, MemberMydi, MemberMydj}
                For i = 1 To target.Count - 1
                    If target(i) = 0 Then
                        target(i) = target(i - 1)
                        If Not DiscontinuityIndex.Contains(i) Then DiscontinuityIndex.Add(i)
                    End If
                Next
            Next

            '断面力を連続した１本の曲線に修正する。*******
            Call ResetMrdPosition(MemberMudi, MemberMudj, YPoint, DiscontinuityIndex)
            Call ResetMrdPosition(MemberMydi, MemberMydj, YPoint, DiscontinuityIndex)

            '損傷レベルを調べる 
            For mNo = FirstMemberNo To LastMemberNo
                Dim 損傷レベル = DB._SNAPDB.OutputInfo.StepCtrl(iStep).MemberItem(mNo).SCFlg
                MemberLevel.Add(損傷レベル)
            Next
        Catch ex As Exception
            Throw ex
        End Try

        Return DiscontinuityIndex
    End Function

    ''' <summary>
    ''' 各点の段落し後の耐力を計算する。
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Set段落し後の耐力(ByVal DB As CalculationCase, ByVal pileType As clsPileAnchorBar, DiscontinuityIndex As List(Of Integer),
                                       ByVal FirstMemberNo As Integer, ByVal LastMemberNo As Integer, ByVal MemberNumber As Single,
                                       ByVal MemberNdi As List(Of Single), ByVal MemberNdj As List(Of Single),
                                       ByRef MemberMudi2 As List(Of Single), ByRef MemberMudj2 As List(Of Single),
                                       ByRef MemberMydi2 As List(Of Single), ByRef MemberMydj2 As List(Of Single),
                                       ByVal YPoint As List(Of Single)) As Integer
        Dim iType As Integer = 52

        Try
            Dim CalcStrength As New RCNonlinear.Strength
            Dim i As Integer = 0

            For mNo = FirstMemberNo To LastMemberNo
                Dim R As Single
                Dim Ass As New List(Of String)
                Dim Num As New List(Of Single)
                Dim dt As New List(Of Single)
                Dim γc As Single
                Dim γs As Single
                Dim fck As Single
                Dim fsy As Single
                Dim Ec As Single
                Dim Es As Single
                Dim Nd As Single
                Dim Myd As Single
                Dim Mud As Single

                'SNAPから断面情報を取得
                Dim DLNo = DB._SNAPDB.GetDLNo(mNo)
                Dim DL As IDLInfo = DB._SNAPDB.DLInfo(DLNo)

                iType = DL.iType
                '断面の情報を取得・設定
                Select Case DL.iType
                    Case 52
                        With DL
                            '直径
                            R = .ShpCtrl.R * 2
                            '鉄筋の位置と本数
                            Dim tD = Convert.ToInt32(pileType.D1)
                            If tD > 0 Then
                                Ass.Add(String.Format("D{0}", tD))
                            Else
                                Ass.Add(String.Format("D{0}", DL.StlBar.Item(0).Dia))
                            End If
                            Dim tn = Convert.ToSingle(pileType.n1)
                            If tn > 0 Then
                                Num.Add(tn)
                            Else
                                Num.Add(Convert.ToSingle(DL.StlBar.Item(0).Num / 2))
                            End If
                            dt.Add(Convert.ToSingle(DL.StlBar.Item(0).Y))
                            γc = Convert.ToSingle(.Factar.GC) 'コンクリート材料係数γc
                            γs = Convert.ToSingle(.Factar.GR) '鉄筋の材料係数γs
                            fsy = Convert.ToSingle(DL.StlBar.SDL)
                            fck = Convert.ToSingle(DL.Material.FCK)
                            Ec = Convert.ToSingle(DL.Material.EC)
                            Es = Convert.ToSingle(DL.StlBar.ES)
                        End With
                        If γc * γs * fsy * fck = 0 Then
                            Throw New Exception(String.Format("Error - γc={0}, γs={1}, fsy={2}, fck={3}", γc, γs, fsy, fck))
                        End If
                        'i端側の断面耐力の計算

                        If FormSettings.Option_杭頭の軸力でモーメント図を描く > 0 Then
                            Nd = -MemberNdi.First
                        Else
                            Nd = -MemberNdi(i)
                        End If
                        Myd = CalcStrength.Myd_R(R, Ass, Num, dt, fck, fsy, Ec, Es, Nd, γc, γs)
                        If Double.IsNaN(Myd) Then Myd = 0
                        If Myd < 0 Then Myd = 0
                        MemberMydi2.Add(Math.Max(Myd, 0))
                        Mud = CalcStrength.Mud_R(R, Ass, Num, dt, fck, fsy, Ec, Es, Nd, γc, γs)
                        If Double.IsNaN(Mud) Then Mud = 0
                        If Mud < 0 Then Mud = 0
                        MemberMudi2.Add(Math.Max(Mud, 0))
                        'j端側の断面耐力の計算
                        If FormSettings.Option_杭頭の軸力でモーメント図を描く > 0 Then
                            Nd = -MemberNdj.First
                        Else
                            Nd = -MemberNdj(i)
                        End If
                        Myd = CalcStrength.Myd_R(R, Ass, Num, dt, fck, fsy, Ec, Es, Nd, γc, γs)
                        If Double.IsNaN(Myd) Then Myd = 0
                        If Myd < 0 Then Myd = 0
                        MemberMydj2.Add(Math.Max(Myd, 0))
                        Mud = CalcStrength.Mud_R(R, Ass, Num, dt, fck, fsy, Ec, Es, Nd, γc, γs)
                        If Double.IsNaN(Mud) Then Mud = 0
                        If Mud < 0 Then Mud = 0
                        MemberMudj2.Add(Math.Max(Mud, 0))

                    Case 62, 501
                        '62:鋼管杭, 501:鋼管杭頭
                        iType = 62
                        '直径
                        If Not Single.TryParse(pileType.D1, R) Then
                            R = DL.ShpCtrl.D
                        End If
                        Dim t As Single
                        If Not Single.TryParse(pileType.n1, t) Then
                            t = DL.ShpCtrl.t
                        End If
                        If Not Single.TryParse(pileType.D2, fsy) Then
                            fsy = Convert.ToSingle(DL.StlBar.FSY)
                        End If
                        γs = Convert.ToSingle(DL.Factar.GS) '鉄筋の材料係数γs
                        Es = Convert.ToSingle(DL.Material.ES)

                        If γs * fsy = 0 Then
                            Throw New Exception(String.Format("Error - γs={0}, fsy={1}", γs, fsy))
                        End If
                        '断面積A
                        Dim A = (Math.Pow(R, 2) - Math.Pow(R - t * 2, 2)) * Math.PI / 4
                        ' 全塑性軸力 N 'y(KN)
                        γs = 1.1
                        Dim Ny = A * fsy / γs / 1000
                        ' 軸力が無い場合の降伏曲げモーメント
                        Dim My0 = Math.PI / (32 * R) * (Math.Pow(R, 4) - Math.Pow(R - 2 * t, 4)) * fsy / γs / 1000000
                        ' 軸力が無い場合の全塑性曲げモーメント
                        Dim Mp0 = Math.Pow(R, 3) / 6 * (1 - Math.Pow(1 - 2 * t / R, 3)) * fsy / γs / 1000000


                        'i端側の断面耐力の計算
                        If FormSettings.Option_杭頭の軸力でモーメント図を描く > 0 Then
                            Nd = -MemberNdi.First
                        Else
                            Nd = -MemberNdi(i)
                        End If
                        '軸力比
                        Dim NdNyRatio = Math.Max(Nd / Ny, 0)
                        '降伏ﾓｰﾒﾝﾄ My(KN･m)
                        Myd = My0 * (1 - NdNyRatio)
                        If Double.IsNaN(Myd) Then Myd = 0
                        If Myd < 0 Then Myd = 0
                        MemberMydi2.Add(Math.Max(Myd, 0))
                        '全塑性ﾓｰﾒﾝﾄ Mp(KN･m)
                        Mud = Mp0 * Math.Cos(Math.PI / 2 * NdNyRatio)
                        If Double.IsNaN(Mud) Then Mud = 0
                        If Mud < 0 Then Mud = 0
                        MemberMudi2.Add(Math.Max(Mud, 0))


                        'j端側の断面耐力の計算
                        If FormSettings.Option_杭頭の軸力でモーメント図を描く > 0 Then
                            Nd = -MemberNdj.First
                        Else
                            Nd = -MemberNdj(i)
                        End If
                        '軸力比
                        NdNyRatio = Math.Max(Nd / Ny, 0)
                        '降伏ﾓｰﾒﾝﾄ My(KN･m)
                        Myd = My0 * (1 - NdNyRatio)
                        If Double.IsNaN(Myd) Then Myd = 0
                        If Myd < 0 Then Myd = 0
                        MemberMydj2.Add(Math.Max(Myd, 0))
                        '全塑性ﾓｰﾒﾝﾄ Mp(KN･m)
                        Mud = Mp0 * Math.Cos(Math.PI / 2 * NdNyRatio)
                        If Double.IsNaN(Mud) Then Mud = 0
                        If Mud < 0 Then Mud = 0
                        MemberMudj2.Add(Math.Max(Mud, 0))

                    Case Else
                        '耐力が計算できない場合は -1 を代入しておく1
                        MemberMudi2.Add(0)
                        MemberMudj2.Add(0)
                        MemberMydi2.Add(0)
                        MemberMydj2.Add(0)
                End Select
                i = i + 1
            Next

        Catch ex As Exception
            Throw ex
        End Try

        '-----------------------------------------------------------------------------------------
        '鋼管杭の杭頭剛域部など、
        '耐力が計算できない場合に 0 としておいた値は、
        '１つ下の値と同じとする
        For Each target In New List(Of Single)() {MemberMudi2, MemberMudj2, MemberMydi2, MemberMydj2}
            For i = target.Count - 2 To 0 Step -1
                If target(i) = 0 Then
                    target(i) = target(i + 1)
                End If
            Next
        Next



        '耐力を連続した１本の曲線に修正する。*******
        Call ResetMrdPosition(MemberMudi2, MemberMudj2, YPoint, DiscontinuityIndex)
        Call ResetMrdPosition(MemberMydi2, MemberMydj2, YPoint, DiscontinuityIndex)


        Return iType
    End Function

    ''' <summary>
    ''' 耐力を連続した１本の曲線に修正する。
    ''' </summary>
    Private Sub ResetMrdPosition(ByRef MemberMri As List(Of Single),
                                 ByRef MemberMrj As List(Of Single),
                                 ByVal YPoint As List(Of Single),
                                 ByVal DiscontinuityIndex As List(Of Integer))
        Try

            '各部材の平均耐力を計算する.
            Dim Mrd As New List(Of Single)
            Dim i As Integer = 0
            For i = 0 To MemberMri.Count - 1
                Mrd.Add((MemberMri(i) + MemberMrj(i)) / 2)
            Next

            '耐力を均す
            For i = 0 To Mrd.Count - 1
                Dim p1 As Single = 0.0F
                Dim p2 As Single = 0.0F
                Dim m1 As Single = 0.0F
                Dim m2 As Single = 0.0F
                If DiscontinuityIndex.Contains(i) Or DiscontinuityIndex.Contains(i + 1) Then
                    Continue For
                End If
                If i < Mrd.Count - 1 Then
                    p1 = (YPoint(i) + YPoint(i + 1)) / 2
                    p2 = (YPoint(i + 1) + YPoint(i + 2)) / 2
                    m1 = Mrd(i)
                    m2 = Mrd(i + 1)
                    If i = 0 Then
                        '求めるi点
                        Dim pi As Single = YPoint(i)
                        Dim mi As Single = m1 - ((p1 - pi) * (m2 - m1) / (p2 - p1))
                        MemberMri(i) = mi
                    Else
                        MemberMri(i) = MemberMrj(i - 1)
                    End If
                Else
                    MemberMri(i) = MemberMrj(i - 1)
                    p1 = (YPoint(i - 1) + YPoint(i)) / 2
                    p2 = (YPoint(i) + YPoint(i + 1)) / 2
                    m1 = Mrd(i - 1)
                    m2 = Mrd(i)
                End If
                '求めるj点
                Dim pj As Single = YPoint(i + 1)
                Dim mj As Single = m1 + ((pj - p1) * (m2 - m1) / (p2 - p1))
                MemberMrj(i) = mj
            Next i

        Catch ex As Exception
            Throw ex
        End Try

    End Sub

    ''' <summary>
    ''' 耐力曲線と曲げモーメントの交点を調べる。
    ''' </summary>
    ''' <returns></returns>
    Private Function Set交差位置(ByVal YPoint As List(Of Single),
                                 ByVal Mdi As List(Of Single),
                                 ByVal Mdj As List(Of Single),
                                 ByVal Mri As List(Of Single),
                                 ByVal Mrj As List(Of Single),
                                 ByVal MudCoefficient As Single,
                                 Optional TopOrBottm As Integer = 0,
                                 Optional IsRound As Boolean = True) As PointF

        Dim Md2Point As New PointF


        '耐力曲線と曲げモーメントの交点を調べる。*******
        Try
            '全ての交点を列挙する。*******
            Dim crossPoints As List(Of PointF) = Get交差位置(YPoint,
                                                             Mdi,
                                                             Mdj,
                                                             Mri,
                                                             Mrj,
                                                             MudCoefficient)
            Dim YY As PointF
            If crossPoints.Count > 0 Then
                If TopOrBottm = 0 Then
                    YY = crossPoints.First
                Else
                    YY = crossPoints.Last
                End If
                If YY.Y > 0 Then
                    If YY.Y < YPoint.Last Then
                        If IsRound = True Then
                            Md2Point.Y = Math.Round(YY.Y, 3)
                            Md2Point.X = Math.Round(YY.X, 2)
                        Else
                            Md2Point.Y = YY.Y
                            Md2Point.X = YY.X
                        End If
                    End If
                End If
            Else
                '交点なし
                Return PointF.Empty
            End If

        Catch ex As Exception
            Return PointF.Empty
        End Try

        Return Md2Point

    End Function

    ''' <summary>
    ''' 全ての耐力曲線と曲げモーメントの交点を調べる。
    ''' </summary>
    ''' <returns></returns>
    Private Function Get交差位置(ByVal YPoint As List(Of Single),
                                 ByVal Mdi As List(Of Single),
                                 ByVal Mdj As List(Of Single),
                                 ByVal Mri As List(Of Single),
                                 ByVal Mrj As List(Of Single),
                                 ByVal MudCoefficient As Single) As List(Of PointF)

        '全ての交点を列挙する。*******
        Dim crossPoints As New List(Of PointF)
        Try
            Dim sign As Boolean = (Math.Abs(Mdi.First) > Math.Abs(Mri.First) / MudCoefficient) '大小関係
            Dim pri As New PointF '耐力上側
            Dim prj As New PointF '耐力下側
            Dim pdi As New PointF '断面力上側
            Dim pdj As New PointF '断面力下側

            For index = 0 To Mdi.Count - 1

                pri.X = Math.Abs(Mri(index)) / MudCoefficient
                prj.X = Math.Abs(Mrj(index)) / MudCoefficient
                pri.Y = YPoint(index)
                prj.Y = YPoint(index + 1)

                If Math.Sign(Mdi(index)) = Math.Sign(Mdj(index)) Then
                    'X
                    pdi.X = Math.Abs(Mdi(index))
                    pdj.X = Math.Abs(Mdj(index))
                    'Y
                    pdi.Y = pri.Y
                    pdj.Y = prj.Y
                    If sign <> (pdj.X > prj.X) Then
                        Dim YY = CrossLine(pri.X, pri.Y, prj.X, prj.Y,
                                           pdi.X, pdi.Y, pdj.X, pdj.Y)
                        crossPoints.Add(YY)
                        sign = (pdj.X > prj.X)
                    End If
                Else
                    '交点 ***
                    pdi.X = Mdi(index)
                    pdi.Y = pri.Y
                    pdj.X = Mdj(index)
                    pdj.Y = prj.Y

                    Dim ZeroPointY = CrossLine(pdi.X, pri.Y, pdj.X, prj.Y,
                                               0, pri.Y, 0, prj.Y)

                    'i端 ***
                    pdi.X = Math.Abs(Mdi(index))
                    pdi.Y = pdi.Y
                    pdj.X = 0
                    pdj.Y = ZeroPointY.Y

                    If sign <> (pdj.X > prj.X) Then
                        Dim YY = CrossLine(pri.X, pri.Y, prj.X, prj.Y,
                                           pdi.X, pdi.Y, pdj.X, pdj.Y)
                        crossPoints.Add(YY)
                        sign = (pdj.X > prj.X)
                    End If

                    'j端 ***
                    pdi.X = 0
                    pdi.Y = ZeroPointY.Y
                    pdj.X = Math.Abs(Mdj(index))
                    pdj.Y = prj.Y

                    If sign <> (pdj.X > prj.X) Then
                        Dim YY = CrossLine(pri.X, pri.Y, prj.X, prj.Y,
                                           pdi.X, pdi.Y, pdj.X, pdj.Y)
                        crossPoints.Add(YY)
                        sign = (pdj.X > prj.X)
                    End If

                End If

            Next

        Catch ex As Exception
            Throw ex
        End Try

        Return crossPoints

    End Function


    ''' <summary>
    ''' 継ぎ手を設けない範囲を調べる。
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Set継ぎ手を設けない範囲(ByVal pileType As clsPileAnchorBar,
                                             ByVal PileLength As Single, ByVal YPoint As List(Of Single), ByVal Md2Point As Single,
                                             ByVal MemberMdi As List(Of Single), ByVal MemberMdj As List(Of Single),
                                             ByVal MemberMudi As List(Of Single), ByVal MemberMudj As List(Of Single),
                                             ByVal MemberMydi As List(Of Single), ByVal MemberMydj As List(Of Single),
                                             ByVal MemberMudi2 As List(Of Single), ByVal MemberMudj2 As List(Of Single),
                                             ByRef NGArea As DisabledArea,
                                             ByRef JointMudi As List(Of Single), ByRef JointMudj As List(Of Single),
                                             ByRef JointMydi As List(Of Single), ByRef JointMydj As List(Of Single),
                                             ByRef JointMudi2 As List(Of Single), ByRef JointMudj2 As List(Of Single),
                                             ByVal iType As Integer) As Integer
        '継ぎ手低減耐力の集計 
        Try
            '段落し前の耐力集計 
            For i = 0 To MemberMudi.Count - 1
                JointMudi.Add(MemberMudi(i) * pileType._ReductionCoefficient)
                JointMydi.Add(MemberMydi(i))
                JointMudj.Add(MemberMudj(i) * pileType._ReductionCoefficient)
                JointMydj.Add(MemberMydj(i))
            Next

            '段落し後の耐力集計 
            For i = 0 To MemberMudi2.Count - 1
                JointMudi2.Add(MemberMudi2(i) * pileType._ReductionCoefficient)
                JointMudj2.Add(MemberMudj2(i) * pileType._ReductionCoefficient)
            Next

        Catch ex As Exception
            Throw ex
        End Try

        '杭頭, カットオフ点周辺の継手を設けてはいけない範囲を設定する。
        Try
            NGArea.DisabledFromPileHead = Convert.ToSingle(pileType.DisabledFromPileHead) / 1000
        Catch
            NGArea.DisabledFromPileHead = 0
        End Try
        Try
            'カットオフから上方に
            NGArea.DisabledUpwardFromCutOffPoint = Convert.ToSingle(pileType.DisabledUpwardFromCutOffPoint) / 1000
        Catch
            NGArea.DisabledUpwardFromCutOffPoint = 0
        End Try
        Try
            'カットオフから下方に
            NGArea.DisabledDownwardFromCutOffPoint = Convert.ToSingle(pileType.DisabledDownwardFromCutOffPoint) / 1000
        Catch
            NGArea.DisabledDownwardFromCutOffPoint = 0
        End Try

        ' 段落とし鉄筋を止める位置（カットオフ）
        If Md2Point = 0 Then
            NGArea.CutOffPoint = -1
        Else
            If iType <> 62 Then
                NGArea.CutOffPoint = Md2Point + (pileType._PileMidLength / 1000)
            Else
                '鋼管杭の場合
                '段落とし位置 = モーメント交差点 + 高止まり
                NGArea.CutOffPoint = Md2Point + NGArea.DisabledDownwardFromCutOffPoint
            End If
        End If


        'その他の継ぎ手を設けない範囲を探査する。
        Try
            Dim StepSt As Integer = IIf(Md2Point > 0, 1, 3)
            Dim StepEd As Integer = IIf(Md2Point <= PileLength, 3, 2)
            For i = StepSt To StepEd '下記の3ケースの継ぎ手を設けない範囲を集計する。

                '探査用の変数を用意する。---------------------------------------------------------------------------------
                Dim tYPoint = New List(Of Single)
                Dim tMemberMdi = New List(Of Single)
                Dim tMemberMdj = New List(Of Single)
                Dim tJointMri = New List(Of Single)
                Dim tJointMrj = New List(Of Single)
                Dim tPileLength As Single = 0

                Try
                    For Each a In YPoint
                        tYPoint.Add(a)
                    Next
                    For Each a In MemberMdi
                        tMemberMdi.Add(a)
                    Next
                    For Each a In MemberMdj
                        tMemberMdj.Add(a)
                    Next

                    Select Case i
                        Case 1 '段落し前の終局耐力 × 継ぎ手低減係数
                            For Each a In JointMudi
                                tJointMri.Add(a)
                            Next
                            For Each a In JointMudj
                                tJointMrj.Add(a)
                            Next
                            '段落し点より下位置の値を各配列から削除する。
                            tPileLength = Md2Point
                            DeleteListAtTargetPoint(tPileLength,
                                                    1,
                                                    tYPoint,
                                                    tMemberMdi,
                                                    tMemberMdj,
                                                    tJointMri,
                                                    tJointMrj)

                        Case 2 '段落し前の降伏耐力 
                            For Each a In JointMydi
                                tJointMri.Add(a)
                            Next
                            For Each a In JointMydj
                                tJointMrj.Add(a)
                            Next
                            '段落し点より下位置の値を各配列から削除する。
                            tPileLength = Md2Point
                            DeleteListAtTargetPoint(tPileLength,
                                                    1,
                                                    tYPoint,
                                                    tMemberMdi,
                                                    tMemberMdj,
                                                    tJointMri,
                                                    tJointMrj)

                        Case 3 '段落し後の降伏耐力 × 継ぎ手低減係数
                            For Each a In JointMudi2
                                tJointMri.Add(a)
                            Next
                            For Each a In JointMudj2
                                tJointMrj.Add(a)
                            Next
                            '段落し点より上位置の値を各配列から削除する。
                            tPileLength = Md2Point
                            DeleteListAtTargetPoint(tPileLength,
                                                    0,
                                                    tYPoint,
                                                    tMemberMdi,
                                                    tMemberMdj,
                                                    tJointMri,
                                                    tJointMrj)

                    End Select
                Catch ex As Exception
                    Throw ex
                End Try

                '交点を探査する。--------------------------------------------------------------------------------------------------------------------
                Try
                    '全ての交点を列挙する。*******
                    Dim crossPoints As List(Of PointF) = Get交差位置(tYPoint,
                                                                     tMemberMdi,
                                                                     tMemberMdj,
                                                                     tJointMri,
                                                                     tJointMrj,
                                                                     1)

                    'If iType = 62 Then
                    '    dp.Top.Y -= DisabledUpwardFromCutOffPoint
                    '    dp.Bottom.Y += DisabledDownwardFromCutOffPoint
                    '    If dp.Top.Y < 0 Then dp.Top.Y = 0
                    'End If
                    If crossPoints.Count > 1 Then
                        '交点が見つかった場合
                        If Math.Abs(tMemberMdi.First) > tJointMri.First Then
                            Dim Top As New PointF(-1, tYPoint.First)
                            Dim Bottm As PointF = crossPoints.First
                            NGArea.DisabledPoints.Add(New DisabledPoint(Top, Bottm))
                            crossPoints.RemoveAt(0)
                        End If
                        If Math.Abs(tMemberMdj.Last) > tJointMrj.Last Then
                            Dim Top As PointF = crossPoints.Last
                            Dim Bottm As New PointF(-1, tYPoint.Last)
                            NGArea.DisabledPoints.Add(New DisabledPoint(Top, Bottm))
                            crossPoints.RemoveAt(crossPoints.Count - 1)
                        End If
                        If crossPoints.Count Mod 2 > 0 Then
                            Throw New Exception("交点が奇数個存在します。")
                        Else
                            For j = 0 To crossPoints.Count - 1 Step 2
                                Dim Top As PointF = crossPoints(j)
                                Dim Bottm As PointF = crossPoints(j + 1)
                                NGArea.DisabledPoints.Add(New DisabledPoint(Top, Bottm))
                            Next
                        End If

                    ElseIf crossPoints.Count > 0 Then
                        If Math.Abs(tMemberMdi.First) > tJointMri.First Then
                            Dim Top As New PointF(-1, tYPoint.First)
                            Dim Bottm As PointF = crossPoints.Last
                            NGArea.DisabledPoints.Add(New DisabledPoint(Top, Bottm))
                        ElseIf Math.Abs(tMemberMdj.Last) > tJointMrj.Last Then
                            Dim Top As PointF = crossPoints.First
                            Dim Bottm As New PointF(-1, tYPoint.Last)
                            NGArea.DisabledPoints.Add(New DisabledPoint(Top, Bottm))
                        End If
                    End If

                Catch ex As Exception
                    Throw ex
                End Try
            Next

        Catch ex As Exception
            Throw ex
        End Try

        Try
            For Each dp In NGArea.DisabledPoints
                If dp.Top.Y < 0 Then
                    dp.Top.Y = 0
                End If
            Next

            If iType = 62 Then
                For Each dp In NGArea.DisabledPoints
                    '鋼管杭の場合, 低止まり,高止まりの距離を加算する
                    Dim tmp = New DisabledPoint(
                        New PointF(dp.Top.X, dp.Top.Y - NGArea.DisabledUpwardFromCutOffPoint),
                        New PointF(dp.Bottom.X, dp.Bottom.Y + NGArea.DisabledDownwardFromCutOffPoint))
                    If tmp.Top.Y < 0 Then
                        tmp.Top.Y = 0
                    End If
                    NGArea.DisabledPoints62.Add(tmp)
                Next
            End If

        Catch ex As Exception

        End Try

        Return 1
    End Function

    ''' <summary>
    ''' targetPoint点より 上方 もしくは 下方 の位置の値を各配列から削除する。
    ''' </summary>
    ''' <param name="targetPoint">基準点</param>
    ''' <param name="YPoint">Y座標リスト</param>
    ''' <param name="M1i">i端の値リストその１</param>
    ''' <param name="M1j">j端の値リストその１</param>
    ''' <param name="M2i">i端の値リストその２</param>
    ''' <param name="M2j">j端の値リストその２</param>
    ''' <param name="TopOrBottm">
    '''                   0 : 上方の値を各配列から削除
    '''                   1 : 下方の値を各配列から削除
    ''' </param>
    ''' <remarks></remarks>
    Private Sub DeleteListAtTargetPoint(ByVal targetPoint As Single, ByVal TopOrBottm As Integer,
                                        ByRef YPoint As List(Of Single),
                                        ByRef M1i As List(Of Single), ByRef M1j As List(Of Single),
                                        ByRef M2i As List(Of Single), ByRef M2j As List(Of Single))

        If targetPoint <= YPoint.First Then Return

        If TopOrBottm = 0 Then
            '上方の値を各配列から削除
            For i = 0 To YPoint.Count - 1
                If targetPoint <= YPoint(i) Then
                    'X座標の値を削除
                    Dim ti As New List(Of Single)
                    Dim tj As New List(Of Single)
                    For k = 1 To 2
                        Select Case k
                            Case 1 : ti = M1i : tj = M1j 'その１
                            Case 2 : ti = M2i : tj = M2j 'その２
                        End Select
                        Dim max = Math.Max(Math.Abs(ti(i - 1)), Math.Abs(tj(i - 1)))
                        Dim px = CrossLine(ti(i - 1), YPoint(i - 1), tj(i - 1), YPoint(i),
                                           -max, targetPoint, max, targetPoint)
                        ti(i - 1) = px.X
                        'tj(i - 1) = ti(i - 1) + ((targetPoint - YPoint(i)) * (tj(i - 1) - ti(i - 1)) / (YPoint(i) - YPoint(i - 1)))
                        ti.RemoveRange(0, i - 1)
                        tj.RemoveRange(0, i - 1)
                        Select Case k
                            Case 1 : M1i = ti : M1j = tj 'その１
                            Case 2 : M2i = ti : M2j = tj 'その２
                        End Select
                    Next
                    'Y座標の値を削除
                    YPoint(i - 1) = targetPoint
                    YPoint.RemoveRange(0, i - 1)
                    Exit For
                End If
            Next

        ElseIf TopOrBottm = 1 Then
            '下方の値を各配列から削除
            For i = 0 To YPoint.Count - 1
                If targetPoint <= YPoint(i) Then
                    'X座標の値を削除
                    Dim ti As New List(Of Single)
                    Dim tj As New List(Of Single)
                    For k = 1 To 2
                        Select Case k
                            Case 1 : ti = M1i : tj = M1j 'その１
                            Case 2 : ti = M2i : tj = M2j 'その２
                        End Select
                        ti.RemoveRange(i, ti.Count - i)
                        tj.RemoveRange(i, tj.Count - i)

                        Dim max = Math.Max(Math.Abs(ti(i - 1)), Math.Abs(tj(i - 1)))
                        Dim px = CrossLine(ti(i - 1), YPoint(i - 1), tj(i - 1), YPoint(i),
                                           -max, targetPoint, max, targetPoint)
                        tj(i - 1) = px.X
                        'tj(i - 1) = ti(i - 1) + ((targetPoint - YPoint(i - 1)) * (tj(i - 1) - ti(i - 1)) / (YPoint(i) - YPoint(i - 1)))
                        Select Case k
                            Case 1 : M1i = ti : M1j = tj 'その１
                            Case 2 : M2i = ti : M2j = tj 'その２
                        End Select
                    Next
                    'Y座標の値を削除
                    YPoint.RemoveRange(i + 1, YPoint.Count - 1 - i)
                    YPoint(i) = targetPoint
                    Exit For
                End If
            Next

        End If


    End Sub



    ''' <summary>
    ''' 最大値を集計しXスケールを決定する。
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function SetScaleX(ByVal YPoint As List(Of Single), ByVal Md2Point As Single,
                               ByVal MemberMdi As List(Of Single), ByVal MemberMdj As List(Of Single),
                               ByVal MemberNdi As List(Of Single), ByVal MemberNdj As List(Of Single),
                               ByVal MemberMudi As List(Of Single), ByVal MemberMudj As List(Of Single),
                               ByVal MemberMydi As List(Of Single), ByVal MemberMydj As List(Of Single),
                               ByVal MemberMudi2 As List(Of Single), ByVal MemberMudj2 As List(Of Single),
                               ByVal MemberMydi2 As List(Of Single), ByVal MemberMydj2 As List(Of Single),
                               ByRef MdScaleX As Single, ByRef NdScaleX As Single,
                               ByRef MaxMd As Single, ByRef MaxMd1 As PointF, ByRef MaxMd2 As PointF) As Integer

        Try
            MaxMd1 = New PointF(0, -1)
            MaxMd2 = New PointF(0, -1)
            MaxMd = 0
            Dim MaxNd As Single = 0
            Dim MaxMud As Single = 0
            '最大曲げモーメントの探査
            For i = 0 To MemberMdi.Count - 1
                If MaxMd1.X < Math.Abs(MemberMdi(i)) Then
                    MaxMd1.X = Math.Abs(MemberMdi(i))
                    MaxMd1.Y = YPoint(i)
                End If
                If MaxMd1.X < Math.Abs(MemberMdj(i)) Then
                    MaxMd1.X = Math.Abs(MemberMdj(i))
                    MaxMd1.Y = YPoint(i + 1)
                End If
            Next
            '２番目曲げモーメントの探査
            For i = 0 To MemberMdi.Count - 1
                If MaxMd1.Y <> YPoint(i) Then
                    If MaxMd2.X < Math.Abs(MemberMdi(i)) Then
                        MaxMd2.X = Math.Abs(MemberMdi(i))
                        MaxMd2.Y = YPoint(i)
                    End If
                End If
                If MaxMd1.Y <> YPoint(i + 1) Then
                    If MaxMd2.X < Math.Abs(MemberMdj(i)) Then
                        MaxMd2.X = Math.Abs(MemberMdj(i))
                        MaxMd2.Y = YPoint(i + 1)
                    End If
                End If
            Next
            '最大曲げ耐力の探査
            For i = 0 To MemberMudi.Count - 1
                If YPoint(i + 1) < Md2Point Then
                    If MaxMud < MemberMudi(i) Then MaxMud = MemberMudi(i)
                    If MaxMud < MemberMudj(i) Then MaxMud = MemberMudj(i)
                    If MaxMud < MemberMydi(i) Then MaxMud = MemberMydi(i)
                    If MaxMud < MemberMydj(i) Then MaxMud = MemberMydj(i)
                Else
                    Exit For
                End If
            Next
            For i = 0 To MemberMudi2.Count - 1
                If MaxMud < MemberMudi2(i) Then MaxMud = MemberMudi2(i)
                If MaxMud < MemberMudj2(i) Then MaxMud = MemberMudj2(i)
                If MaxMud < MemberMydi2(i) Then MaxMud = MemberMydi2(i)
                If MaxMud < MemberMydj2(i) Then MaxMud = MemberMydj2(i)
            Next

            '曲げモーメントのスケール決定
            MaxMd = Math.Max(MaxMd1.X, MaxMud)
            MdScaleX = (XPosMaxMd - XPosMd) / MaxMd


            '最大軸圧縮力の探査
            For i = 0 To MemberNdi.Count - 1
                If MaxNd < Math.Abs(MemberNdi(i)) Then MaxNd = Math.Abs(MemberNdi(i))
                If MaxNd < Math.Abs(MemberNdj(i)) Then MaxNd = Math.Abs(MemberNdj(i))
            Next

            '軸力のスケール決定
            NdScaleX = (XPosMaxNd - XPosNd) / MaxNd

        Catch ex As Exception
            Throw ex
        End Try
        Return 1
    End Function

#End Region

#Region "出力（印字）関数群"

    ''' <summary>
    ''' タイトル出力
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Printタイトル(DB As CalculationCase, pileNo As Integer, FirstMemberNo As Integer, LastMemberNo As Integer,
                                   iStep As Integer, iStepTitle As String) As Integer
        Try
            preview.PrtText("杭の段落し図")
            preview.enter(1.5)

            Dim cPos As PointF = preview.GetCurrentPos()
            preview.SetCurrentPos(New PointF(cPos.X + 20, cPos.Y))
            preview.PrtText(DB._SNAPDB.InputInfo.KihonInfo.Title)
            preview.enter(1.0)

            Dim dPos As PointF = preview.GetCurrentPos()
            preview.SetCurrentPos(New PointF(dPos.X + 20, dPos.Y))
            preview.PrtText(DB._SNAPDB.InputInfo.KihonInfo.Comment)
            preview.enter(1.5)

            Dim mPos As PointF = preview.GetCurrentPos()
            preview.SetCurrentPos(New PointF(mPos.X + 40, mPos.Y))
            preview.PrtText(String.Format("{0} 列目 : {1}  ～ {2}  部材", pileNo + 1, FirstMemberNo, LastMemberNo))

            preview.SetCurrentPos(New PointF(mPos.X + 300, mPos.Y))
            preview.PrtText(String.Format("{0} ステップ：{1}", iStep, iStepTitle))
            'preview.enter(1.0)

        Catch ex As Exception
            Throw ex
        End Try
        Return 1
    End Function

    ''' <summary>
    ''' 縦軸出力
    ''' </summary>
    ''' <param name="YPoint"></param>
    ''' <param name="MemberLevel"></param>
    ''' <param name="ScaleY"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Print縦軸(YPoint As List(Of Single), MemberLevel As List(Of Integer), ScaleY As Single,
                               MaxMdvale As Single, ScaleX As Single) As Integer

        'モーメント図 Y軸
        Try
            preview.DrawLine(New PointF(XPosMd, YPosPileHead), New PointF(XPosMd, YPosPileHead + PrintPileHeight), thicknessYAxis)
            'モーメント図 Y軸の目盛り
            For Each y In YPoint
                Dim pointY As Single = YPosPileHead + y * ScaleY
                preview.DrawLine(New PointF(XPosMd, pointY), New PointF(XPosMd - 2, pointY), thicknessYAxis)
                preview.SetCurrentPos(New PointF(XPosMd - 35, pointY - 7))
                preview.PrtText(y.ToString("F2"))
            Next
        Catch ex As Exception
            Throw ex
        End Try

        'モーメントの目盛り
        Try
            'タイトル
            preview.SetCurrentPos(New PointF(XPosMd + (XPosMaxMd - XPosMd) / 2, YPosPileHead - 50))
            preview.PrtText("曲げモーメント kN・m")
            '目盛りの桁数を決定する
            Dim a As Integer = Math.Ceiling(MaxMdvale)  '基準となる数値
            Dim b As String = a.ToString("F0")          '文字列に変換
            Dim c As Integer = b.Length                 '桁数
            Dim d As Integer = Left(b, 1)               '1番目の数値
            Dim e As Integer = IIf(d < 4, 5, IIf(d < 7, 10, 20)) '5 の倍数か 10 の倍数か
            Dim f As Integer = e * (10 ^ (c - 2))       '目盛りの間隔
            Dim g As Integer = Math.Ceiling(a / f)      '目盛りの個数
            Dim h As Integer = f * g                    '目盛りの最大値
            '軸を印字
            Dim pointY As Single = YPosPileHead - 23
            Dim pointX As Single = XPosMd + h * ScaleX
            preview.DrawLine(New PointF(XPosMd, pointY), New PointF(pointX, pointY), thicknessYAxis)
            '目盛りを印字
            pointX = XPosMd
            For i = 0 To g
                preview.DrawLine(New PointF(pointX, pointY - 2), New PointF(pointX, pointY), thicknessYAxis)
                preview.SetCurrentPos(New PointF(pointX, YPosPileHead - 38))
                preview.PrtText((f * i).ToString)
                pointX += f * ScaleX
            Next

        Catch ex As Exception
            Throw ex
        End Try


        '軸力図 Y軸( 損傷レベル によって色を変える) 
        Try
            For i = 0 To MemberLevel.Count - 1
                Dim LineColor As Color
                Select Case MemberLevel(i)
                    Case 1 : LineColor = Color.Green
                    Case 2 : LineColor = Color.Blue
                    Case 3 : LineColor = Color.Yellow
                    Case 4 : LineColor = Color.Red
                    Case Else : LineColor = Color.Gray
                End Select
                Dim pointY1 As Single = YPosPileHead + YPoint(i) * ScaleY
                Dim pointY2 As Single = YPosPileHead + YPoint(i + 1) * ScaleY
                preview.DrawLine(New PointF(XPosNd, pointY1), New PointF(XPosNd, pointY2), thicknessYAxis * 2, LineColor)
            Next
            'タイトル
            preview.SetCurrentPos(New PointF(XPosNd - (XPosMaxNd - XPosNd) / 2, YPosPileHead - 50))
            preview.PrtText("軸力 kN")
            preview.SetCurrentPos(New PointF(2 * XPosNd - XPosMaxNd, YPosPileHead - 38))
            preview.PrtText("(-):圧縮, (+)引張")

        Catch ex As Exception
            Throw ex
        End Try
        Return 1
    End Function

    ''' <summary>
    ''' 軸力出力
    ''' </summary>
    ''' <param name="MemberNdi"></param>
    ''' <param name="MemberNdj"></param>
    ''' <param name="YPoint"></param>
    ''' <param name="ScaleY"></param>
    ''' <param name="NdScaleX"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function print軸力(MemberNdi As List(Of Single), MemberNdj As List(Of Single),
                               YPoint As List(Of Single), ScaleY As Single, NdScaleX As Single) As Integer
        Try
            For i = 0 To MemberNdi.Count - 1
                Dim Ndi As Single = MemberNdi(i)  'i端の最大曲げモーメント時の軸圧縮力
                Dim Ndj As Single = MemberNdj(i)  'j端の最大曲げモーメント時の軸圧縮力
                Dim pointY1 As Single = YPosPileHead + YPoint(i) * ScaleY
                Dim pointY2 As Single = YPosPileHead + YPoint(i + 1) * ScaleY
                Dim pointX1 As Single = XPosNd + Ndi * NdScaleX
                Dim pointX2 As Single = XPosNd + Ndj * NdScaleX
                preview.DrawLine(New PointF(pointX1, pointY1), New PointF(pointX2, pointY2), thicknessMd)
                preview.DrawLine(New PointF(XPosNd, pointY1), New PointF(pointX1, pointY1), thicknessDLine)
                preview.DrawLine(New PointF(XPosNd, pointY2), New PointF(pointX2, pointY2), thicknessDLine)
                'i端の数値を表示
                Dim pointX3 = IIf(Ndi > 0, XPosNd - 37, XPosNd + 1)
                preview.SetCurrentPos(New PointF(pointX3, pointY1 - 7))
                preview.PrtText(Ndi.ToString("F2"))
            Next

        Catch ex As Exception
            Throw ex
        End Try
        Return 1
    End Function

    ''' <summary>
    ''' 発生曲げモーメント出力
    ''' </summary>
    ''' <param name="MemberMdi"></param>
    ''' <param name="MemberMdj"></param>
    ''' <param name="YPoint"></param>
    ''' <param name="ScaleY"></param>
    ''' <param name="MdScaleX"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Print曲げモーメント(MemberMdi As List(Of Single), MemberMdj As List(Of Single),
                                         YPoint As List(Of Single), ScaleY As Single, MdScaleX As Single,
                                         pileType As clsPileAnchorBar) As Integer
        Try
            For i = 0 To MemberMdi.Count - 1
                Dim Mdi As Single = MemberMdi(i)  'i端の最大曲げモーメント
                Dim Mdj As Single = MemberMdj(i)  'j端の最大曲げモーメント

                If Math.Sign(Mdi) <> Math.Sign(Mdj) Then
                    'もし、i端とj端の符号が異なっていたら中間値を追加する。
                    Dim pointY1 As Single = YPosPileHead + YPoint(i) * ScaleY
                    Dim pointY3 As Single = YPosPileHead + YPoint(i + 1) * ScaleY
                    Dim pointX1 As Single = XPosMd + Math.Abs(Mdi) * MdScaleX
                    Dim pointX3 As Single = XPosMd + Math.Abs(Mdj) * MdScaleX
                    '中間値Y2の計算
                    Dim pointX2 As Single = XPosMd
                    Dim pointY2 As Single = (Math.Abs(Mdj) * pointY1 + Math.Abs(Mdi) * pointY3) / (Math.Abs(Mdi) + Math.Abs(Mdj))
                    preview.DrawLine(New PointF(pointX1, pointY1), New PointF(pointX2, pointY2), thicknessMd)
                    preview.DrawLine(New PointF(pointX2, pointY2), New PointF(pointX3, pointY3), thicknessMd)
                Else
                    Dim pointY1 As Single = YPosPileHead + YPoint(i) * ScaleY
                    Dim pointY2 As Single = YPosPileHead + YPoint(i + 1) * ScaleY
                    Dim pointX1 As Single = XPosMd + Math.Abs(Mdi) * MdScaleX
                    Dim pointX2 As Single = XPosMd + Math.Abs(Mdj) * MdScaleX
                    preview.DrawLine(New PointF(pointX1, pointY1), New PointF(pointX2, pointY2), thicknessMd)
                End If
            Next
        Catch ex As Exception
            Throw ex
        End Try


        If FormSettings.Option_杭の抵抗モーメント図に15倍の線を描く > 0 Then
            ' 1.5倍の線を描く機能
            Try
                For i = 0 To MemberMdi.Count - 1
                    Dim Mdi As Single = MemberMdi(i) * pileType.MudCoefficient 'i端の最大曲げモーメント
                    Dim Mdj As Single = MemberMdj(i) * pileType.MudCoefficient 'j端の最大曲げモーメント

                    If Math.Sign(Mdi) <> Math.Sign(Mdj) Then
                        'もし、i端とj端の符号が異なっていたら中間値を追加する。
                        Dim pointY1 As Single = YPosPileHead + YPoint(i) * ScaleY
                        Dim pointY3 As Single = YPosPileHead + YPoint(i + 1) * ScaleY
                        Dim pointX1 As Single = XPosMd + Math.Abs(Mdi) * MdScaleX
                        Dim pointX3 As Single = XPosMd + Math.Abs(Mdj) * MdScaleX
                        '中間値Y2の計算
                        Dim pointX2 As Single = XPosMd
                        Dim pointY2 As Single = (Math.Abs(Mdj) * pointY1 + Math.Abs(Mdi) * pointY3) / (Math.Abs(Mdi) + Math.Abs(Mdj))
                        preview.DrawDashLine(New PointF(pointX1, pointY1), New PointF(pointX2, pointY2), thicknessMd, 0.7)
                        preview.DrawDashLine(New PointF(pointX2, pointY2), New PointF(pointX3, pointY3), thicknessMd, 0.7)
                    Else
                        Dim pointY1 As Single = YPosPileHead + YPoint(i) * ScaleY
                        Dim pointY2 As Single = YPosPileHead + YPoint(i + 1) * ScaleY
                        Dim pointX1 As Single = XPosMd + Math.Abs(Mdi) * MdScaleX
                        Dim pointX2 As Single = XPosMd + Math.Abs(Mdj) * MdScaleX
                        preview.DrawDashLine(New PointF(pointX1, pointY1), New PointF(pointX2, pointY2), thicknessMd, 0.7)
                    End If
                Next
            Catch ex As Exception
                Throw ex
            End Try
        End If
        Return 1


    End Function

    ''' <summary>
    ''' 曲げ耐力の出力
    ''' </summary>
    ''' <param name="MemberMri">i端の曲げ耐力</param>
    ''' <param name="MemberMrj">j端の曲げ耐力</param>
    ''' <param name="YPoint">各要素の節点の座標</param>
    ''' <param name="ScaleY">Y方向のスケール</param>
    ''' <param name="ScaleX">X方向のスケール</param>
    ''' <param name="DiscontinuityIndex">不連続とする部材番号</param>
    ''' <param name="LimitTop">LimitTop位置から上側の線は表示しない</param>
    ''' <param name="LimitBottom">LimitBottom位置から下側の線は表示しない</param>
    ''' <param name="LineType">0 : 実践, 1 : 破線</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Print曲げ耐力(MemberMri As List(Of Single), MemberMrj As List(Of Single),
                                   YPoint As List(Of Single), ScaleY As Single, ScaleX As Single,
                                   DiscontinuityIndex As List(Of Integer),
                                   Optional LimitTop As Single = -1, Optional LimitBottom As Single = -1,
                                   Optional LineType As Integer = 0,
                                   Optional ByRef pointY3 As Single = -1,
                                     Optional ByRef pointX3 As Single = -1,
                                     Optional isDraw As Boolean = True) As Integer
        Try
            Dim pointLimitTop As Single = YPosPileHead + LimitTop * ScaleY
            Dim pointLimitBottom As Single = YPosPileHead + LimitBottom * ScaleY

            Dim pointX0 As Single
            Dim pointY0 As Single
            For i = 0 To MemberMri.Count - 1
                Dim Mri As Single = MemberMri(i)  'i端の最大曲げモーメント時の耐力
                Dim Mrj As Single = MemberMrj(i)  'j端の最大曲げモーメント時の耐力

                Dim Flg As Boolean = False
                If DiscontinuityIndex.Contains(i - 1) Then
                    Mri = Mrj
                    Flg = True
                End If
                If pointX0 * pointY0 = 0 Then
                    Flg = False
                End If

                Dim pointY1 As Single = YPosPileHead + YPoint(i) * ScaleY
                Dim pointY2 As Single = YPosPileHead + YPoint(i + 1) * ScaleY
                Dim pointX1 = XPosMd + Math.Abs(Mri) * ScaleX
                Dim pointX2 = XPosMd + Math.Abs(Mrj) * ScaleX
                'LimitBottom位置から下側の線は表示しない
                If LimitBottom <> -1 Then
                    If pointY2 >= pointLimitBottom Then
                        pointY3 = pointLimitBottom
                        Dim dY As Single = pointY2 - pointLimitBottom
                        pointX3 = pointX2 - (((pointX2 - pointX1) / (pointY2 - pointY1)) * dY)
                        If isDraw Then
                            Dim p1 = New PointF(pointX1, pointY1)
                            Dim p3 = New PointF(pointX3, pointY3)
                            Select Case LineType
                                Case 0
                                    If Flg = True Then preview.DrawLine(p1, New PointF(pointX0, pointY0), thicknessMd)
                                    preview.DrawLine(p1, p3, thicknessMd)
                                Case 1
                                    If Flg = True Then preview.DrawDashLine(p1, New PointF(pointX0, pointY0), thicknessMd, IntervalMdLine)
                                    preview.DrawDashLine(p1, p3, thicknessMd, IntervalMdLine)
                            End Select
                            pointX0 = pointX3
                            pointY0 = pointY3
                        End If
                        Exit For
                    End If
                End If
                'LimitTop位置から上側の線は表示しない
                If LimitTop <> -1 Then
                    If pointY2 < pointLimitTop Then
                        '表示しない
                    ElseIf pointY2 >= pointLimitTop And pointY1 < pointLimitTop Then
                        pointY3 = pointLimitTop
                        Dim dY As Single = pointY2 - pointLimitTop
                        pointX3 = pointX2 - (((pointX2 - pointX1) / (pointY2 - pointY1)) * dY)
                        If isDraw Then
                            Dim p2 = New PointF(pointX2, pointY2)
                            Dim p3 = New PointF(pointX3, pointY3)
                            Select Case LineType
                                Case 0
                                    If Flg = True Then preview.DrawLine(p3, New PointF(pointX0, pointY0), thicknessMd)
                                    preview.DrawLine(p3, p2, thicknessMd)
                                Case 1
                                    If Flg = True Then preview.DrawDashLine(p3, New PointF(pointX0, pointY0), thicknessMd, IntervalMdLine)
                                    preview.DrawDashLine(p3, p2, thicknessMd, IntervalMdLine)
                            End Select
                            pointX0 = pointX2
                            pointY0 = pointY2
                        End If
                    ElseIf pointY1 >= pointLimitTop Then
                        If isDraw Then
                            Dim p1 = New PointF(pointX1, pointY1)
                            Dim p2 = New PointF(pointX2, pointY2)
                            Select Case LineType
                                Case 0
                                    If Flg = True Then preview.DrawLine(p1, New PointF(pointX0, pointY0), thicknessMd)
                                    preview.DrawLine(p1, p2, thicknessMd)
                                Case 1
                                    If Flg = True Then preview.DrawDashLine(p1, New PointF(pointX0, pointY0), thicknessMd, IntervalMdLine)
                                    preview.DrawDashLine(p1, p2, thicknessMd, IntervalMdLine)
                            End Select
                            pointX0 = pointX2
                            pointY0 = pointY2
                        End If
                    End If
                Else
                    If isDraw Then
                        Dim p1 = New PointF(pointX1, pointY1)
                        Dim p2 = New PointF(pointX2, pointY2)
                        Select Case LineType
                            Case 0
                                If Flg = True Then preview.DrawLine(p1, New PointF(pointX0, pointY0), thicknessMd)
                                preview.DrawLine(p1, p2, thicknessMd)
                            Case 1
                                If Flg = True Then preview.DrawDashLine(p1, New PointF(pointX0, pointY0), thicknessMd, IntervalMdLine)
                                preview.DrawDashLine(p1, p2, thicknessMd, IntervalMdLine)
                        End Select
                        pointX0 = pointX2
                        pointY0 = pointY2
                    End If
                End If
            Next
            pointY3 = (pointY3 - YPosPileHead) / ScaleY
            pointX3 = (pointX3 - XPosMd) / ScaleX
        Catch ex As Exception
            Throw ex
        End Try
        Return 1
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="pileType"></param>
    ''' <param name="Md2Point"></param>
    ''' <param name="YPoint"></param>
    ''' <param name="ScaleY"></param>
    ''' <param name="ScaleX"></param>
    ''' <param name="MemberMudi"></param>
    ''' <param name="MemberMudj"></param>
    ''' <param name="MemberMydi"></param>
    ''' <param name="MemberMydj"></param>
    ''' <param name="JointMudi"></param>
    ''' <param name="JointMudj"></param>
    ''' <param name="MaxMdvale1"></param>
    ''' <param name="MaxMdvale2"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Print曲げ耐力記号(ByVal pileType As clsPileAnchorBar, ByVal Md2Point As PointF,
                                       ByVal YPoint As List(Of Single), ByVal ScaleY As Single, ByVal ScaleX As Single,
                                       ByVal MemberMudi As List(Of Single), ByVal MemberMudj As List(Of Single),
                                       ByVal MemberMydi As List(Of Single), ByVal MemberMydj As List(Of Single),
                                       ByVal JointMudi As List(Of Single), ByVal JointMudj As List(Of Single),
                                         ByVal MaxMdvale1 As PointF, ByVal MaxMdvale2 As PointF,
                                         Optional iType As Integer = 52) As Integer

        '杭頭～最大発生モーメントの間に Mud と 0.9Mud を印字
        Try
            '杭頭
            Dim pointY1 As Single = 0
            '最大発生モーメント
            Dim pointY2 As Single = 0
            If MaxMdvale1.Y = 0 Then
                pointY2 = MaxMdvale2.Y
            ElseIf MaxMdvale2.Y = 0 Then
                pointY2 = MaxMdvale1.Y
            Else
                pointY2 = MaxMdvale1.Y
            End If

            '杭頭～最大発生モーメントの中間位置
            'Mud --------------------------------------------------
            Try
                Dim Str As String = ""
                Dim textAngle As Single
                Dim pt3 As New PointF
                pt3.Y = (pointY1 + pointY2) / 2
                If iType = 62 Then
                    Str = "Myd"
                Else
                    Str = "Mud"
                End If


                For i = 1 To YPoint.Count - 1

                    If YPoint(i) > pt3.Y Then
                        pt3.X = MemberMudj(i)
                        Dim idx = MemberMudi(i) * ScaleX
                        Dim idy = YPoint(i) * ScaleY
                        Dim jdx = MemberMudj(i) * ScaleX
                        Dim jdy = YPoint(i + 1) * ScaleY
                        Dim DeltaX = (jdx - idx)
                        Dim DeltaY = (jdy - idy)
                        Dim Angle As Double = -Math.Atan2(DeltaY, DeltaX)
                        textAngle = Angle * 180 / Math.PI + 180 '°
                        Exit For
                    End If
                Next

                preview.SetCurrentPos(New PointF(XPosMd + pt3.X * ScaleX, YPosPileHead + pt3.Y * ScaleY))
                preview.PrtText(Str, textAngle)
            Catch ex As Exception
                Throw ex
            End Try
            '0.9Mud --------------------------------------------------
            Try
                Dim Str As String = ""
                Dim textAngle As Single
                Dim pt3 As New PointF
                pt3.Y = (pointY1 + pointY2) / 2
                If iType = 62 Then
                    Str = String.Format("{0:F1}Myd", pileType.ReductionCoefficient)
                Else
                    Str = String.Format("{0:F1}Mud", pileType.ReductionCoefficient)
                End If
                For i = 1 To YPoint.Count - 1

                    If YPoint(i) > pt3.Y Then

                        pt3.X = JointMudj(i)
                        Dim idx = JointMudi(i) * ScaleX
                        Dim idy = YPoint(i) * ScaleY
                        Dim jdx = JointMudj(i) * ScaleX
                        Dim jdy = YPoint(i + 1) * ScaleY
                        Dim DeltaX = (jdx - idx)
                        Dim DeltaY = (jdy - idy)
                        Dim Angle As Double = -Math.Atan2(DeltaY, DeltaX)
                        textAngle = Angle * 180 / Math.PI + 180 '°
                        Exit For
                    End If
                Next
                preview.SetCurrentPos(New PointF(XPosMd + pt3.X * ScaleX, YPosPileHead + pt3.Y * ScaleY))
                preview.PrtText(Str, textAngle)

            Catch ex As Exception
                Throw ex
            End Try
            '最大発生モーメント～段落し点の間に Myd を印字
            Try
                pointY1 = Md2Point.Y
                Dim Str As String = ""
                Dim textAngle As Single
                Dim pt3 As New PointF
                pt3.Y = (pointY1 + pointY2) / 2
                Str = "Myd"

                For i = 1 To YPoint.Count - 1

                    If YPoint(i) > pt3.Y Then

                        pt3.X = MemberMydj(i)
                        Dim idx = MemberMydi(i) * ScaleX
                        Dim idy = YPoint(i) * ScaleY
                        Dim jdx = MemberMydj(i) * ScaleX
                        Dim jdy = YPoint(i + 1) * ScaleY
                        Dim DeltaX = (jdx - idx)
                        Dim DeltaY = (jdy - idy)
                        Dim Angle As Double = -Math.Atan2(DeltaY, DeltaX)
                        textAngle = Angle * 180 / Math.PI + 180 '°
                        Exit For
                    End If
                Next
                preview.SetCurrentPos(New PointF(XPosMd + pt3.X * ScaleX, YPosPileHead + pt3.Y * ScaleY))
                preview.PrtText(Str, textAngle)
            Catch ex As Exception
                Throw ex
            End Try


        Catch ex As Exception
            Throw ex
        End Try


        Return 1
    End Function


    ''' <summary>
    ''' 最大発生モーメントの印字
    ''' </summary>
    ''' <param name="ScaleY"></param>
    ''' <param name="ScaleX"></param>
    ''' <param name="MaxMdvale1"></param>
    ''' <param name="MaxMdvale2"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Print最大曲げモーメント(ByVal PileLength As Single, NGArea As DisabledArea,
                                             ByVal ScaleY As Single, ByVal ScaleX As Single,
                                             ByVal MaxMdvale1 As PointF, ByVal MaxMdvale2 As PointF,
                                             ByVal iType As Integer) As Integer

        '杭頭最大発生モーメントの印字で、最大もしくは２番目に大きい曲げが杭頭に発生していた場合のみ印字 ***********
        Try
            If MaxMdvale1.Y = 0 Then
                If PrintMdValue(MaxMdvale1, ScaleY, ScaleX) = -1 Then Return -1
            End If
            If MaxMdvale2.Y = 0 Then
                If PrintMdValue(MaxMdvale2, ScaleY, ScaleX) = -1 Then Return -1
            End If
        Catch ex As Exception
            Throw ex
        End Try

        '地中部の最大発生モーメントの印字で、継ぎ手可能範囲の場合のみ印字 ***********
        Try
            Dim L = NGArea.GetDisabledPointList(iType)
            Dim pf As Boolean
            pf = True
            For Each a In L
                If MaxMdvale1.Y > a.Top.Y And MaxMdvale1.Y < a.Bottom.Y Then
                    pf = False
                    Exit For
                End If
            Next
            If pf = True Then
                If PrintMdValue(MaxMdvale1, ScaleY, ScaleX) = -1 Then Return -1
            End If
            pf = True
            For Each a In L
                If MaxMdvale2.Y > a.Top.Y And MaxMdvale2.Y < a.Bottom.Y Then
                    pf = False
                    Exit For
                End If
            Next
            If pf = True Then
                If PrintMdValue(MaxMdvale2, ScaleY, ScaleX) = -1 Then Return -1
            End If

        Catch ex As Exception

        End Try

        Return 1
    End Function

    ''' <summary>
    ''' 曲げモーメントの値を表示
    ''' </summary>
    ''' <param name="MaxMdvale"></param>
    ''' <param name="ScaleY"></param>
    ''' <param name="ScaleX"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function PrintMdValue(MaxMdvale As PointF, ScaleY As Single, ScaleX As Single) As Integer
        Try
            Dim pointY As Single = YPosPileHead + MaxMdvale.Y * ScaleY
            Dim pointX1 As Single = XPosMd
            Dim pointX2 As Single = XPosMd + MaxMdvale.X * ScaleX
            Dim printStr As String = MaxMdvale.X.ToString("F2")

            preview.DrawLine(New PointF(pointX1, pointY), New PointF(pointX2, pointY), thicknessDLine)
            preview.SetCurrentPos(New PointF(pointX1, pointY - 10))
            preview.PrtText(printStr)

        Catch ex As Exception
            Return -1
        End Try
        Return 1
    End Function


    ''' <summary>
    ''' 段落し位置の横線と耐力の引き出し印字
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Print曲げ耐力Border(ByVal YPoint As List(Of Single), Md2Point As PointF, CutOffPoint As Single,
                                         ByVal ScaleY As Single, ByVal ScaleX As Single,
                                         ByVal Mud2ValuePoint As PointF, ByVal Myd2ValuePoint As PointF,
                                           ByVal MydValuePoint As PointF, ByVal MudValuePoint As PointF,
                                          ByVal iType As Integer) As Integer
        Try

            Dim CrossPoint As New PointF
            If iType <> 62 Then
                If Mud2ValuePoint.X < Myd2ValuePoint.X Then
                    CrossPoint = Mud2ValuePoint
                Else
                    CrossPoint = Myd2ValuePoint
                End If
            Else
                CrossPoint = Myd2ValuePoint
            End If

            Dim MaxPoint As New PointF
            If MydValuePoint.X > MudValuePoint.X Then
                MaxPoint = MydValuePoint
            Else
                MaxPoint = MudValuePoint
            End If

            preview.DrawLine(New PointF(XPosMd + CrossPoint.X * ScaleX, YPosPileHead + CrossPoint.Y * ScaleY),
                             New PointF(XPosMd + MaxPoint.X * ScaleX, YPosPileHead + CrossPoint.Y * ScaleY), thicknessMd)

            '段落し点の発生曲げモーメントの値の印字 ***********
            If PrintMdValue(Md2Point, ScaleY, ScaleX) = -1 Then Return -1

            '段落し点の耐力の値の印字 ***********
            Dim p21 As PointF = New PointF((CrossPoint.X + MaxPoint.X) / 2, CutOffPoint)
            Dim p22 As PointF = New PointF(XPosMd + p21.X * ScaleX, YPosPileHead + p21.Y * ScaleY)
            Dim p23 As PointF = New PointF(p22.X, p22.Y + 12)

            'Mud2
            If iType <> 62 Then
                Dim sMu As String = "Mud=" + Mud2ValuePoint.X.ToString("F2")
                preview.DrawLine(New PointF(XPosMd + Mud2ValuePoint.X * ScaleX, YPosPileHead + Mud2ValuePoint.Y * ScaleY), p22, thicknessDLine)
                preview.DrawLine(p22, New PointF(p22.X + (sMu.Length * 4), p22.Y), thicknessDLine)
                preview.SetCurrentPos(New PointF(p22.X, p22.Y - 10))
                preview.PrtText(sMu)
            End If

            'Myd2
            Dim sMy As String = "Myd=" + Myd2ValuePoint.X.ToString("F2")
            preview.DrawLine(New PointF(XPosMd + Myd2ValuePoint.X * ScaleX, YPosPileHead + Myd2ValuePoint.Y * ScaleY), p23, thicknessDLine)
            preview.DrawLine(p23, New PointF(p23.X + (sMy.Length * 4), p23.Y), thicknessDLine)
            preview.SetCurrentPos(New PointF(p23.X, p23.Y - 10))
            preview.PrtText(sMy)

        Catch ex As Exception
            Throw ex
        End Try
        Return 1
    End Function

    ''' <summary>
    ''' 杭の姿の印字
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function Print杭形状(ByVal pileType As clsPileAnchorBar, ByVal pileLength As Single,
                                 ByVal Md2Point As PointF, ByVal NGArea As DisabledArea,
                                   ByVal ScaleY As Single,
                                   ByVal iType As Integer) As Integer
        Try
            If Md2Point.Y > 0 Then
                If iType <> 62 Then
                    '杭頭～段落し点の印字 ***********
                    If Md2Point.Y > pileLength Then
                        preview.DrawLine(New PointF(XPosPile1, YPosPileHead), New PointF(XPosPile1, YPosPileHead + pileLength * ScaleY), thicknessYAxis)
                    Else
                        preview.DrawLine(New PointF(XPosPile1, YPosPileHead), New PointF(XPosPile1, YPosPileHead + Md2Point.Y * ScaleY), thicknessYAxis)
                        '段落し点～カットオフ点（点線）の印字 ***********
                        If NGArea.CutOffPoint >= pileLength Then
                            preview.DrawDashLine(New PointF(XPosPile1, YPosPileHead + Md2Point.Y * ScaleY), New PointF(XPosPile1, YPosPileHead + pileLength * ScaleY), thicknessYAxis, 3)
                        Else
                            preview.DrawDashLine(New PointF(XPosPile1, YPosPileHead + Md2Point.Y * ScaleY), New PointF(XPosPile1, YPosPileHead + NGArea.CutOffPoint * ScaleY), thicknessYAxis, 3)
                        End If
                    End If
                Else
                    '鋼管杭の場合
                    If Md2Point.Y < pileLength Then
                        ' 段落とし位置 = モーメント交差点 + 高止まり
                        Dim Y = Md2Point.Y + NGArea.DisabledDownwardFromCutOffPoint
                        preview.DrawLine(
                            New PointF(XPosPile1, YPosPileHead + Y * ScaleY),
                            New PointF(XPosPile2, YPosPileHead + Y * ScaleY),
                            thicknessYAxis)
                    End If
                End If
            End If

            '杭頭 および 杭先端の横線の印字 ***********
            preview.DrawLine(New PointF(XPosMd, YPosPileHead),
                             New PointF(XPosDLine4, YPosPileHead), thicknessMd)
            preview.DrawLine(New PointF(XPosMd, YPosPileHead + pileLength * ScaleY),
                             New PointF(XPosDLine4, YPosPileHead + pileLength * ScaleY), thicknessMd)

            '杭頭定着分の印字 ***********
            Dim YPileHeadPos0 = YPosPileHead - (pileType._PileHeadLength / 1000) * ScaleY
            If pileType._PileHeadLength > 0 Then
                preview.DrawLine(New PointF(XPosPile1, YPileHeadPos0),
                                 New PointF(XPosPile1, YPosPileHead), thicknessYAxis)
                preview.DrawLine(New PointF(XPosPile2, YPileHeadPos0),
                                 New PointF(XPosPile2, YPosPileHead), thicknessYAxis)
            End If

            If iType = 62 Then
                '鋼管杭の場合 杭頭に横線を追加
                preview.DrawLine(New PointF(XPosPile1, YPileHeadPos0),
                                 New PointF(XPosPile2, YPileHeadPos0), thicknessYAxis)
            End If

            If Md2Point.Y < pileLength Then
                '段落し後の鉄筋本数の印字 ***********
                Dim pt As New PointF
                Dim St As String = ""
                pt.X = XPosPile1 - 10
                Dim t1 = YPosPileHead + NGArea.CutOffPoint * ScaleY
                Dim t2 = YPosPileHead + pileLength * ScaleY
                pt.Y = (t1 + t2) / 1.9
                preview.SetCurrentPos(pt)

                If iType <> 62 Then '鋼管杭じゃなければ
                    St = String.Format("D{0}-{1}", pileType.D1, pileType.n1)
                Else
                    If pileType.D2.Trim().Length = 0 Then
                        St = String.Format("D={0}mm, t={1}mm", pileType.D1, pileType.n1)
                    Else
                        St = String.Format("D={0}mm, t={1}mm, fsy={2}N/mm2", pileType.D1, pileType.n1, pileType.D2)
                    End If
                End If
                preview.PrtText(St, 90)

            End If

        Catch ex As Exception
            Throw ex
        End Try
        Return 1
    End Function

    ''' <summary>
    ''' 継ぎ手を設けない範囲の印字
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function print継ぎ手を設けない範囲(ByVal PileLength As Single, ByVal NGArea As DisabledArea,
                                                 ByVal ScaleY As Single, ByVal ScaleX As Single,
                                                  ByVal iType As Integer) As Integer

        Dim PointList = NGArea.GetDisabledPointList(iType)
        Try
            Dim p1 As New PointF
            Dim p2 As New PointF

            Dim XPos = New Integer() {XPosPile2}
            If iType = 62 Then
                '鋼管杭の場合
                XPos = New Integer() {XPosPile1, XPosPile2}
            End If

            For Each X In XPos

                For i = 0 To PointList.Count - 1
                    Dim a = PointList(i)
                    '前の位置からTopまでの線
                    If i > 0 Then
                        p1 = p2
                        p2.X = X
                        p2.Y = YPosPileHead + a.Top.Y * ScaleY
                        preview.DrawLine(p1, p2, thicknessYAxis)
                    End If
                    '継ぎ手を設けない範囲
                    If a.Bottom.Y > PileLength Then
                        p1.X = X
                        p1.Y = YPosPileHead + a.Top.Y * ScaleY
                        p2.X = X
                        p2.Y = YPosPileHead + PileLength * ScaleY
                        preview.DrawLine(p1, p2, thicknessDLine)
                        Exit For
                    End If
                    p1.X = X
                    p1.Y = YPosPileHead + a.Top.Y * ScaleY
                    p2.X = X
                    p2.Y = YPosPileHead + a.Bottom.Y * ScaleY
                    preview.DrawLine(p1, p2, thicknessDLine)
                    '最後のBottomから杭先端まで
                    If i = PointList.Count - 1 Then
                        p1 = p2
                        p2.X = X
                        p2.Y = YPosPileHead + PileLength * ScaleY
                        preview.DrawLine(p1, p2, thicknessYAxis)
                    End If
                Next
            Next


        Catch ex As Exception
            Throw ex
        End Try
        Try
            Dim p1 As New PointF
            Dim p2 As New PointF
            '継ぎ手を設けない範囲の寸法線の印字 ***********
            Dim keyY As Single = 0
            For i = 0 To PointList.Count - 1
                Dim a = PointList(i)
                '前の位置からTopまでの線
                If i > 0 Then
                    p1 = p2
                    p2.X = XPosDLine4
                    p2.Y = YPosPileHead + a.Top.Y * ScaleY
                    Ptint寸法線(p1, p2, ((a.Top.Y - keyY) * 1000).ToString("F0"))
                    preview.DrawLine(New PointF(XPosPile2 + 5, p2.Y), p2, thicknessDLine)
                End If
                '継ぎ手を設けない範囲
                If a.Bottom.Y > PileLength Then
                    p1.X = XPosDLine4
                    p1.Y = YPosPileHead + a.Top.Y * ScaleY
                    p2.X = XPosDLine4
                    p2.Y = YPosPileHead + PileLength * ScaleY
                    keyY = a.Bottom.Y
                    Ptint寸法線(p1, p2, ((PileLength - a.Top.Y) * 1000).ToString("F0"))
                    preview.DrawLine(New PointF(XPosPile2 + 5, p2.Y), p2, thicknessDLine)
                    Exit For
                End If
                p1.X = XPosDLine4
                p1.Y = YPosPileHead + a.Top.Y * ScaleY
                p2.X = XPosDLine4
                p2.Y = YPosPileHead + a.Bottom.Y * ScaleY
                keyY = a.Bottom.Y
                Ptint寸法線(p1, p2, ((a.Bottom.Y - a.Top.Y) * 1000).ToString("F0"))
                preview.DrawLine(New PointF(XPosPile2 + 5, p2.Y), p2, thicknessDLine)

                '最後のBottomから杭先端まで
                If i = PointList.Count - 1 Then
                    p1 = p2
                    p2.X = XPosDLine4
                    p2.Y = YPosPileHead + PileLength * ScaleY
                    Ptint寸法線(p1, p2, ((PileLength - keyY) * 1000).ToString("F0"))
                End If
            Next

        Catch ex As Exception
            Throw ex
        End Try
        Try
            If iType = 62 Then
                '鋼管杭の場合 高止まり、低止まりの分が余計なので、集計し直す
                PointList = NGArea.GetDisabledPointList(52)
            End If
            '継ぎ手を設けない範囲の曲げモーメント値を表示
            For Each a In PointList
                If a.Top.X > 0 Then
                    If PrintMdValue(a.Top, ScaleY, ScaleX) = -1 Then Return -1
                End If
                If a.Bottom.X > 0 Then
                    If PrintMdValue(a.Bottom, ScaleY, ScaleX) = -1 Then Return -1
                End If
            Next
        Catch ex As Exception
            Throw ex
        End Try


        Return 1
    End Function

    ''' <summary>
    ''' 鉄筋の印字
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function 段落し鉄筋(ByVal pileType As clsPileAnchorBar, ByVal pileLength As Single,
                                ByVal NGArea As DisabledArea, ByVal Md2Point As PointF,
                                ByVal ScaleY As Single) As Integer
        Try
            Dim p1 As New PointF
            Dim p2 As New PointF
            Dim str As String = ""
            p1.X = XPosDLine3
            p1.Y = YPosPileHead - (pileType._PileHeadLength / 1000) * ScaleY
            p2.X = XPosDLine3
            p2.Y = YPosPileHead + pileLength * ScaleY
            str = (pileLength * 1000 + pileType._PileHeadLength).ToString("F0")
            Ptint寸法線(p1, p2, str)
            preview.DrawLine(New PointF(XPosPile2 + 5, p1.Y), p1, thicknessDLine)
        Catch ex As Exception
            Throw ex
        End Try

        '段落し鉄筋長の印字 ***********
        If Md2Point.Y > 0 Then
            Try
                If NGArea.CutOffPoint < pileLength Then
                    Dim p1 As New PointF
                    Dim p2 As New PointF
                    Dim str As String = ""
                    '段落し点までの鉄筋長さ
                    p1.X = XPosDLine2
                    p1.Y = YPosPileHead - (pileType._PileHeadLength / 1000) * ScaleY
                    p2.X = XPosDLine2
                    p2.Y = YPosPileHead + NGArea.CutOffPoint * ScaleY
                    str = (pileType._PileHeadLength + NGArea.CutOffPoint * 1000).ToString("F0")
                    Ptint寸法線(p1, p2, str)
                    preview.DrawLine(New PointF(XPosPile1 + 5, p2.Y), p2, thicknessDLine)
                    '段落し残り
                    p1 = p2
                    p2.Y = YPosPileHead + pileLength * ScaleY
                    str = ((pileLength - NGArea.CutOffPoint) * 1000).ToString("F0")
                    Ptint寸法線(p1, p2, str)
                End If
            Catch ex As Exception
                Throw ex
            End Try
        End If

        Try
            Dim XPos As Single
            Dim p1 As New PointF
            Dim p2 As New PointF
            Dim str As String = ""
            If Md2Point.Y > 0 And NGArea.CutOffPoint < pileLength Then
                XPos = XPosDLine1
                '杭頭定着長
                p1.X = XPos
                p1.Y = YPosPileHead - (pileType._PileHeadLength / 1000) * ScaleY
                p2.X = XPos
                p2.Y = YPosPileHead
                str = pileType._PileHeadLength.ToString("F0")
                Ptint寸法線(p1, p2, str)
                '段落し位置
                p1 = p2
                p2.Y = YPosPileHead + Md2Point.Y * ScaleY
                str = (Md2Point.Y * 1000).ToString("F0")
                Ptint寸法線(p1, p2, str)
                preview.DrawLine(New PointF(XPosPile1 + 5, p2.Y), p2, thicknessDLine)
                '段落し定着長
                p1 = p2
                p2.Y = YPosPileHead + NGArea.CutOffPoint * ScaleY
                str = ((NGArea.CutOffPoint - Md2Point.Y) * 1000).ToString("F0") 'pileType._PileMidLength.ToString("F0")
                Ptint寸法線(p1, p2, str)
            ElseIf NGArea.CutOffPoint >= pileLength Then
                XPos = XPosDLine2
                '杭頭定着長
                p1.X = XPos
                p1.Y = YPosPileHead - (pileType._PileHeadLength / 1000) * ScaleY
                p2.X = XPos
                p2.Y = YPosPileHead
                str = pileType._PileHeadLength.ToString("F0")
                Ptint寸法線(p1, p2, str)
                If Md2Point.Y >= pileLength Then
                    p1 = p2
                    p2.Y = YPosPileHead + pileLength * ScaleY
                    str = (pileLength * 1000).ToString("F0")
                    Ptint寸法線(p1, p2, str)
                    preview.DrawLine(New PointF(XPosPile1 + 5, p2.Y), p2, thicknessDLine)
                Else
                    '段落し位置
                    p1 = p2
                    p2.Y = YPosPileHead + Md2Point.Y * ScaleY
                    str = (Md2Point.Y * 1000).ToString("F0")
                    Ptint寸法線(p1, p2, str)
                    preview.DrawLine(New PointF(XPosPile1 + 5, p2.Y), p2, thicknessDLine)
                    '段落し定着長
                    p1 = p2
                    p2.Y = YPosPileHead + pileLength * ScaleY
                    str = ((pileLength - Md2Point.Y) * 1000).ToString("F0")
                    Ptint寸法線(p1, p2, str)

                End If
            Else
                XPos = XPosDLine2
                '杭頭定着長
                p1.X = XPos
                p1.Y = YPosPileHead - (pileType._PileHeadLength / 1000) * ScaleY
                p2.X = XPos
                p2.Y = YPosPileHead
                str = pileType._PileHeadLength.ToString("F0")
                Ptint寸法線(p1, p2, str)
                '杭先端位置
                p1 = p2
                p2.Y = YPosPileHead + pileLength * ScaleY
                str = (pileLength * 1000).ToString("F0")
                Ptint寸法線(p1, p2, str)
                preview.DrawLine(New PointF(XPosPile1 + 5, p2.Y), p2, thicknessDLine)
            End If
        Catch ex As Exception
            Throw ex
        End Try

        Return 1
    End Function

    ''' <summary>
    ''' 矢印付きの直線と文字を印字する
    ''' </summary>
    ''' <param name="p1"></param>
    ''' <param name="p2"></param>
    ''' <param name="Text"></param>
    ''' <remarks></remarks>
    Private Sub Ptint寸法線(p1 As PointF, p2 As PointF, Optional Text As String = Nothing)

        If p1 = p2 Then
            Return
        End If

        '寸法線の印字
        preview.DrawArrowLine(p1, p2, thicknessDLine, ArrowSize, ArrowSize)

        '寸法値の印字
        If Not Text Is Nothing Then
            '寸法線の角度を計算する。
            Dim idx = p1.X
            Dim idy = p1.Y
            Dim jdx = p2.X
            Dim jdy = p2.Y
            Dim DeltaX = (jdx - idx)
            Dim DeltaY = (jdy - idy)
            Dim Angle As Double = -Math.Atan2(DeltaY, DeltaX)
            Dim textAngle As Single = Angle * 180 / Math.PI + 180 '°

            '200以下の寸法は、
            Dim tx = IIf(Math.Abs((p2.Y - p1.Y)) < 10, 10, 0)

            '寸法値を印字する。
            Dim p3 As New PointF
            p3.X = (p1.X + p2.X) / 2 + tx
            p3.Y = (p1.Y + p2.Y) / 2
            preview.SetCurrentPos(p3)
            preview.PrtText(Text, textAngle)
        End If

    End Sub

#End Region

#Region "2直線の交差判定"

    Private Function CrossLine(X1 As Single, y1 As Single, X2 As Single, y2 As Single,
                   x3 As Single, y3 As Single, x4 As Single, y4 As Single) As PointF

        On Error GoTo errorhandler
        Dim result As New PointF

        Dim ReferenceLine As New clsLine
        ReferenceLine.xi = X1
        ReferenceLine.yi = y1
        ReferenceLine.xj = X2
        ReferenceLine.yj = y2

        Dim TargetLine As New clsLine
        TargetLine.xi = x3
        TargetLine.yi = y3
        TargetLine.xj = x4
        TargetLine.yj = y4

        Dim t As PointF
        t = ReferenceLine.CrossPoint(TargetLine)

        If t.IsEmpty Then
            Return New PointF(-1, -1)
        End If

        result.X = t.X
        result.Y = t.Y
        Return result
errorhandler:
        Stop
    End Function

    Private Class clsLine

        Public xi As Single
        Public yi As Single

        Public xj As Single
        Public yj As Single

        '初期化
        Public Sub SetPoint(p1 As PointF, p2 As PointF)
            Me.xi = p1.X
            Me.yi = p1.Y
            Me.xj = p2.X
            Me.yj = p2.Y
        End Sub

        '交点
        Public Function CrossPoint(L As clsLine) As PointF

            On Error GoTo errorhandler

            If intersect(L) = False Then GoTo errorhandler
            Dim result As New PointF
            Dim a = Me.yj - Me.yi
            Dim B = Me.xi - Me.xj
            Dim u = (Me.yj - Me.yi) * Me.xi - (Me.xj - Me.xi) * Me.yi
            Dim c = L.yj - L.yi
            Dim D = L.xi - L.xj
            Dim v = (L.yj - L.yi) * L.xi - (L.xj - L.xi) * L.yi
            result.X = (D * u - B * v) / (a * D - B * c)
            result.Y = (a * v - c * u) / (a * D - B * c)

            Return result
            Exit Function
errorhandler:
            Return PointF.Empty
        End Function

        '長さ
        Public Function Length() As Single
            Length = Math.Sqrt((Me.xj - Me.xi) ^ 2 + (Me.yj - Me.yi) ^ 2)
        End Function


        '座標 p1,p2 を通る直線と座標 p3,p4 を結ぶ線分が交差しているかを調べる
        '戻り値
        '= 0 - 直線上に線分の1点 or 2点がある
        '< 0 - 直線と線分が交差する
        '> 0 - 直線と線分は交差しない
        Public Function intersect(L As clsLine) As Boolean

            Dim p1 As New PointF, p2 As New PointF
            Dim p3 As New PointF, p4 As New PointF

            p1.X = Me.xi
            p1.Y = Me.yi
            p2.X = Me.xj
            p2.Y = Me.yj

            p3.X = L.xi
            p3.Y = L.yi
            p4.X = L.xj
            p4.Y = L.yj
            intersect = IntersectionStandard(p1, p2, p3, p4)

        End Function

        '座標 p1,p2 を結ぶ線分と座標 p3,p4 を結ぶ線分が交差しているかを調べる
        'ただし、線分が重なっている場合(4点が一直線上にある)、「交差していない」、と判定します。
        Private Function IntersectionStandard(p1 As PointF, p2 As PointF,
                                              p3 As PointF, p4 As PointF) As Boolean
            'それぞれの点と点の差
            Dim dx1 As Single, dx2 As Single, dy1 As Single, dy2 As Single
            '交点
            Dim xx As Single, yy As Single

            dx1 = p1.X - p2.X : dy1 = p1.Y - p2.Y
            dx2 = p3.X - p4.X : dy2 = p3.Y - p4.Y

            'どちらもy軸に平行な線分ではない
            If (dx1 <> 0& And dx2 <> 0&) Then
                '平行な2線分ではない
                If ((dy1 / dx1 - dy2 / dx2) <> 0&) Then
                    '交点を求める
                    xx = (p3.Y - p1.Y - p3.X * dy2 / dx2 + p1.X * dy1 / dx1) /
                         (dy1 / dx1 - dy2 / dx2)
                    'その交点が線分の範囲にあるか
                    If (((xx <= p1.X And xx >= p2.X) Or (xx >= p1.X And xx <= p2.X)) And
                        ((xx <= p3.X And xx >= p4.X) Or (xx >= p3.X And xx <= p4.X))) Then
                        IntersectionStandard = True : Exit Function
                    Else
                        If (((p1.X - p2.X) * (p3.Y - p1.Y) + (p1.Y - p2.Y) * (p1.X - p3.X)) *
                            ((p1.X - p2.X) * (p4.Y - p1.Y) + (p1.Y - p2.Y) * (p1.X - p4.X)) < 0.0#) Then
                            If (((p3.X - p4.X) * (p1.Y - p3.Y) + (p3.Y - p4.Y) * (p3.X - p1.X)) *
                                ((p3.X - p4.X) * (p2.Y - p3.Y) + (p3.Y - p4.Y) * (p3.X - p2.X)) < 0.0#) Then
                                IntersectionStandard = True : Exit Function
                            End If
                        End If
                    End If
                End If
                'p1,p2 を結ぶ線分がy軸に平行である
            ElseIf (dx1 = 0& And dx2 <> 0&) Then
                '交点を求める
                yy = p1.X * dy2 / dx2 + p3.Y - p3.X * dy2 / dx2
                'その交点が線分の範囲にあるか
                If (((yy <= p1.Y And yy >= p2.Y) Or (yy >= p1.Y And yy <= p2.Y)) And
                    ((p1.X <= p3.X And p1.X >= p4.X) Or (p1.X >= p3.X And p1.X <= p4.X))) Then
                    IntersectionStandard = True : Exit Function
                End If
                'p3,p4 を結ぶ線分がy軸に平行である
            ElseIf (dx1 <> 0& And dx2 = 0&) Then
                yy = p3.X * dy1 / dx1 + p1.Y - p1.X * dy1 / dx1
                If (((yy <= p3.Y And yy >= p4.Y) Or (yy >= p3.Y And yy <= p4.Y)) And
                    ((p3.X <= p1.X And p3.X >= p2.X) Or (p3.X >= p1.X And p3.X <= p2.X))) Then
                    IntersectionStandard = True : Exit Function
                End If
            End If
            'それ以外は交差しない
            IntersectionStandard = False : Exit Function
        End Function

    End Class

#End Region

#Region "継ぎ手を設けてはいけない範囲のヘルパークラス"

    Private Class DisabledArea

        ''' <summary>継手を設けてはいけない範囲で、杭頭から(m) </summary>
        Public DisabledFromPileHead As Single

        ''' <summary>継手を設けてはいけない範囲で、カットオフ点から上方に(m)</summary>
        Public DisabledUpwardFromCutOffPoint As Single

        ''' <summary>継手を設けてはいけない範囲で、カットオフ点から下方に(m)</summary>
        Public DisabledDownwardFromCutOffPoint As Single

        ''' <summary>カットオフ点(m)</summary>
        Public CutOffPoint As Single

        ''' <summary>杭頭やカットオフ点周辺を除く、その他の継手を設けてはいけない範囲リスト(m)</summary>
        Public DisabledPoints As New List(Of DisabledPoint)
        Public DisabledPoints62 As New List(Of DisabledPoint)

        ''' <summary>
        ''' 杭頭, カットオフ点周辺 および その他のその他の継手を設けてはいけない範囲から
        ''' 総合的な継手を設けてはいけない範囲リスト(m)を作成する
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetDisabledPointList(iType As Integer) As List(Of DisabledPoint)
            Dim result As New List(Of DisabledPoint)
            Try
                '探査用のリスト tdp を用意 ------------------------------------------------------
                '杭頭からの継手を設けてはいけない範囲を追加
                Dim tdp As New List(Of DisabledPoint) From {
                    New DisabledPoint(New PointF(-1, 0), New PointF(-1, DisabledFromPileHead))
                }
                If CutOffPoint > 0 Then
                    If iType <> 62 Then
                        'カットオフ点周辺の継手を設けてはいけない範囲を追加
                        Dim Top = New PointF(-1, CutOffPoint - DisabledUpwardFromCutOffPoint)
                        Dim Bottom = New PointF(-1, CutOffPoint + DisabledDownwardFromCutOffPoint)
                        If Top.Y < 0 Then Top.Y = 0
                        tdp.Add(New DisabledPoint(Top, Bottom))
                    Else
                        '鋼管杭
                        'カットオフ点周辺の継手を設けてはいけない範囲を追加
                        Dim Top = New PointF(-1, CutOffPoint - Math.Max(DisabledUpwardFromCutOffPoint, 2.0))
                        Dim Bottom = New PointF(-1, CutOffPoint + Math.Max(DisabledDownwardFromCutOffPoint, 2.0))
                        If Top.Y < 0 Then Top.Y = 0
                        tdp.Add(New DisabledPoint(Top, Bottom))
                    End If
                End If
                'その他の継手を設けてはいけない範囲を追加
                If iType <> 62 Then
                    For Each dp In DisabledPoints
                        tdp.Add(dp)
                    Next
                Else
                    For Each dp In DisabledPoints62
                        tdp.Add(dp)
                    Next
                End If
                'Top.Yの小さい順に並び替える
                Call QuickSort1(tdp, 0, tdp.Count - 1)

                '総合的な継手を設けてはいけない範囲リスト(m)を作成する ------------------------------------------------------
                Dim EndFlg As Boolean = False
                Dim keyTop As PointF = tdp.First.Top
                Dim keyBottom As PointF = tdp.First.Bottom
                Dim NextKeyTop As PointF = New PointF(-1, Single.MaxValue)
                Dim NextKeyBottom As PointF = NextKeyTop
                Dim counter As Long = 0
                Dim removeList As List(Of DisabledPoint)

                Do While counter < 1000
                    EndFlg = True
                    removeList = New List(Of DisabledPoint)
                    For i = 0 To tdp.Count - 1
                        Dim dp = tdp(i)
                        If dp.Top.Y <= keyBottom.Y And keyTop.Y <= dp.Bottom.Y Then
                            keyTop = If(keyTop.Y < dp.Top.Y, keyTop, dp.Top)
                            keyBottom = If(keyBottom.Y > dp.Bottom.Y, keyBottom, dp.Bottom)
                            removeList.Add(dp)
                            EndFlg = False
                        Else
                            If dp.Top.Y < NextKeyTop.Y Then
                                NextKeyTop = dp.Top
                                NextKeyBottom = dp.Bottom
                            End If
                        End If
                    Next
                    If EndFlg = False Then
                        result.Add(New DisabledPoint(keyTop, keyBottom))
                        keyTop = NextKeyTop
                        keyBottom = NextKeyBottom
                        NextKeyTop = New PointF(-1, Single.MaxValue)
                        NextKeyBottom = NextKeyTop
                    Else
                        Exit Do
                    End If
                    For Each dp In removeList
                        tdp.Remove(dp)
                    Next
                    removeList = Nothing
                    counter += 1
                Loop

            Catch ex As Exception
                Return Nothing
            End Try
            Return result
        End Function

        Private Sub QuickSort1(ByRef argAry As List(Of DisabledPoint), ByVal lngMin As Long, ByVal lngMax As Long)
            Dim i As Long
            Dim j As Long
            Dim vBase As DisabledPoint
            Dim vSwap As DisabledPoint
            vBase = argAry(Int((lngMin + lngMax) / 2))
            i = lngMin
            j = lngMax
            Do
                Do While argAry(i).Top.Y < vBase.Top.Y
                    i = i + 1
                Loop
                Do While argAry(j).Top.Y > vBase.Top.Y
                    j = j - 1
                Loop
                If i >= j Then Exit Do
                vSwap = argAry(i)
                argAry(i) = argAry(j)
                argAry(j) = vSwap
                i = i + 1
                j = j - 1
            Loop
            If (lngMin < i - 1) Then
                Call QuickSort1(argAry, lngMin, i - 1)
            End If
            If (lngMax > j + 1) Then
                Call QuickSort1(argAry, j + 1, lngMax)
            End If
        End Sub
    End Class



    ''' <summary>
    ''' 継手を設けてはいけない範囲
    ''' </summary>
    ''' <remarks></remarks>
    Private Class DisabledPoint
        Public Top As PointF
        Public Bottom As PointF
        Public Sub New()

        End Sub
        Public Sub New(Top As PointF, Bottom As PointF)
            Me.Top = Top
            Me.Bottom = Bottom
        End Sub
    End Class


#End Region

#End Region

End Class
