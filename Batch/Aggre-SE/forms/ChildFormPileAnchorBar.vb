Imports FarPoint.Win.Spread
Imports SNAPDBLib.CSNAPDB

Public Class ChildFormPileAnchorBar

#Region "メンバ変数"

    '現在表示中のデータ番号 0～
    Private _CurrentDataIndex As Integer
    Private Property CurrentDataIndex As Integer
        Get
            Return 0 '_CurrentDataIndex
        End Get
        Set(value As Integer)

            '_CurrentDataIndex = value

            'Dim maxCount As Integer = Me.dgFoundationList.RowCount - 1

            'If _CurrentDataIndex <= 0 Then
            '    _CurrentDataIndex = 0
            '    Me.btnBack.Enabled = False
            'Else
            '    Me.btnBack.Enabled = True
            'End If

            'If _CurrentDataIndex >= maxCount Then
            '    _CurrentDataIndex = maxCount
            '    Me.btnNext.Enabled = False
            'Else
            '    Me.btnNext.Enabled = True
            'End If
        End Set
    End Property

    Private isSaveFlg As Boolean

    Private MyPictureMode As Integer
    Private ActiveDLNo As Integer
    Private CurrentMemberNoFirst As Integer
    Private CurrentMemberNoLast As Integer

    ''' <summary>
    ''' シングルトン用インスタンス
    ''' </summary>
    ''' <remarks></remarks>
    Private Shared instance As ChildFormPileAnchorBar = New ChildFormPileAnchorBar()

#End Region

#Region "プロパティ"

    ''' <summary>
    ''' インスタンス取得
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property GetInstance() As ChildFormPileAnchorBar
        Get
            '静的変数が解放されていた場合のみ、インスタンスを生成する
            If instance Is Nothing Then
                instance = New ChildFormPileAnchorBar()
            End If

            Return instance
        End Get
    End Property

#End Region

#Region "コンストラクタ"

    ''' <summary>
    ''' コンストラクタ (使用不可)
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub New()
        Me.InitializeComponent()
        isSaveFlg = True
        MyPictureMode = -1
        ActiveDLNo = -1
        CurrentMemberNoFirst = -1
        CurrentMemberNoLast = -1
    End Sub

#End Region

#Region "初期化処理"

    ''' <summary>
    ''' 初期処理
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataInit()
        Me.Text = "杭の段落し"

        'データロード
        Call DataLoad()
        '位置図の更新
        Call MyPictureBox1_Paint(Me.MyPictureBox1, Nothing)
    End Sub



    ''' <summary>
    ''' インスタンスリフレッシュ
    ''' </summary>
    ''' <remarks></remarks>
    Public Shared Sub Reflesh()
        instance = New ChildFormPileAnchorBar()
    End Sub

    ''' <summary>
    ''' リセットボタンクリック
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub btnReset_Click(sender As Object, e As EventArgs) Handles btnReset.Click
        '現在のデータを破棄
        Input.Data.PileAnchorBar.段落し情報 = Nothing
        Input.Data.PileAnchorBar.段落し情報 = New List(Of clsPileAnchorBar)
        DataLoad()
        isSaveFlg = True
    End Sub

#End Region

#Region "データロード"

    ''' <summary>
    ''' データロード
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataLoad()
        '最初のデータを表示
        Me.CurrentDataIndex = 0
        'データがない場合新規追加する
        If Input.Data.PileAnchorBar.段落し情報.Count = 0 Then
            Input.Data.PileAnchorBar.段落し情報.Add(New clsPileAnchorBar())
        End If

        '部材番号データの読み込み
        Me.dgTargetMembert_DataLoad(Me.CurrentDataIndex)
        '鉄筋情報の読み込み
        Me.RCPilePropertyGrid_DataLoad(Me.CurrentDataIndex)
        '鉄筋定着長情報の読み込み
        Me.Design_DataLoad(Me.CurrentDataIndex)

    End Sub

    ''' <summary>
    ''' 定着長等 の初期化
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub Design_DataLoad(index As Integer)
        Try
            With Input.Data.PileAnchorBar.段落し情報(index)
                Me.tbPileHeadLength.Text = .PileHeadLength                                  '杭頭定着長
                Me.tbParagraphLength.Text = .ParagraphLength                                '段落し長
                Me.tbReductionCoefficient.Text = .ReductionCoefficient                      '継手低減係数
                Me.tbPileMidLength.Text = .PileMidLength                                    '段落し定着長
                Me.tbDesignMarginLength.Text = .DesignMarginLength                          '設計余裕長
                Me.tbDisabledFromPileHead.Text = .DisabledFromPileHead                      '継手を設けてはいけない範囲で、杭頭から
                Me.tbDisabledUpwardFromCutOffPoint.Text = .DisabledUpwardFromCutOffPoint    '継手を設けてはいけない範囲で、カットオフ点から上方に
                Me.tbDisabledDownwardFromCutOffPoint.Text = .DisabledDownwardFromCutOffPoint '継手を設けてはいけない範囲で、カットオフ点から下方に
            End With
        Catch ex As Exception
            isSaveFlg = True
        End Try

    End Sub


    ''' <summary>
    ''' 部材番号データの読み込み
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub dgTargetMembert_DataLoad(index As Integer)

        Dim sh = Me.fpTargetMember_Sheet1
        sh.ClearRange(0, 0, sh.RowCount, sh.ColumnCount, True)

        With Input.Data.PileAnchorBar.段落し情報(index)
            If .dgMemberFirst.Count > 0 Then
                Try
                    'sh.RowCount = .dgMemberFirst.Count + 1 '+ 1 とは、最後の行に管理行を加算
                    For row = 0 To .dgMemberFirst.Count - 1 'sh.RowCount - 1
                        sh.Cells(row, 0).Value = IIf(.dgMemberFirst(row) = 0, "", .dgMemberFirst(row))
                        sh.Cells(row, 1).Value = IIf(.dgMemberLast(row) = 0, "", .dgMemberLast(row))
                        sh.Cells(row, 2).Value = IIf(.dgMemberNumber(row) = 0, "", .dgMemberNumber(row))
                    Next
                Catch ex As Exception
                    isSaveFlg = True
                End Try
            Else '入力が無い場合
                Try
                    '部材照査タイプの入力情報が在る場合
                    '部材照査タイプの入力情報から予想して入力する。
                    Dim TargetDLNo As New List(Of Integer)
                    For i = 0 To Input.Data.Element.dgLimitLevel_typeCount - 1
                        If Val(Input.Data.Element.dgLimitLevel_type(i)) = 4 Then
                            TargetDLNo.Add(i + 1)
                        End If
                    Next
                    Dim d As CSNAPDBEx = Input.Data.SNAP.SnapDB
                    If TargetDLNo.Count > 0 AndAlso Not d Is Nothing Then
                        '部材の接続状況を調べて各杭列にばらす
                        Dim MFCount As Integer = d.GetMFCount
                        Dim TargetMemNo As New List(Of Integer)
                        For Each i In TargetDLNo
                            If i > MFCount Then
                                For Each j In d.GetMembers(i - MFCount)
                                    TargetMemNo.Add(j)
                                Next
                            Else
                                Dim j = d.GetMemberNo(i)
                                TargetMemNo.Add(j)
                            End If
                        Next

                        'i端がつながっていない(杭頭)部材を探す
                        Dim tmpMemberFirst As New List(Of Integer)
                        Dim tmpMemberLast As New List(Of Integer)
                        Dim tmpMemberNumber As New List(Of Single)

                        Dim iNo As Integer
                        Dim jNo As Integer
                        For Each i In TargetMemNo
                            Dim PileHeadFlg As Boolean = True
                            iNo = d.GetMember(i).Itan
                            For Each j In TargetMemNo
                                If i <> j Then
                                    jNo = d.GetMember(j).Jtan
                                    If iNo = jNo Then
                                        'この部材は杭頭ではない
                                        PileHeadFlg = False
                                        Exit For
                                    End If
                                End If
                            Next
                            If PileHeadFlg = True Then
                                tmpMemberFirst.Add(i)
                            End If
                        Next
                        'j端がつながっていない(杭先端)部材を探す
                        For Each i In tmpMemberFirst
                            jNo = d.GetMember(i).Jtan
                            Dim mNo As Integer = i
                            Dim PileTipFlg As Boolean = False
                            Do While PileTipFlg = False
                                PileTipFlg = True
                                For Each j In TargetMemNo
                                    If mNo <> j Then
                                        Dim m = d.GetMember(j)
                                        If jNo = m.Itan Then
                                            'この部材は先端ではない
                                            jNo = m.Jtan
                                            mNo = j
                                            PileTipFlg = False
                                            Exit For
                                        End If
                                    End If
                                Next
                            Loop
                            If PileTipFlg = True Then
                                tmpMemberLast.Add(mNo)
                            End If
                        Next

                        If tmpMemberFirst.Count = tmpMemberLast.Count Then
                            '部材の奥行き本数を調べる
                            For Each i In tmpMemberFirst
                                Dim n As Single = d.GetAp(i).ALF
                                tmpMemberNumber.Add(n)
                            Next
                            '入力グリッドに反映する
                            sh.ClearRange(0, 0, sh.RowCount, sh.ColumnCount, True)
                            Dim Cells = sh.Cells
                            For i = 0 To tmpMemberFirst.Count - 1
                                sh.Cells(i, 0).Value = tmpMemberFirst(i)
                                sh.Cells(i, 1).Value = tmpMemberLast(i)
                                sh.Cells(i, 2).Value = tmpMemberNumber(i)
                            Next
                        End If

                    Else 'SNAPの入力情報が無い場合
                        '空白の行を１行用意する。
                        sh.ClearRange(0, 0, sh.RowCount, sh.ColumnCount, True)
                    End If
                Catch ex As Exception
                    sh.ClearRange(0, 0, sh.RowCount, sh.ColumnCount, True)
                    isSaveFlg = True
                End Try
            End If
        End With

    End Sub

    ''' <summary>
    ''' 鉄筋情報の読み込み
    ''' </summary>
    ''' <param name="index"></param>
    ''' <remarks></remarks>
    Private Sub RCPilePropertyGrid_DataLoad(index As Integer)

        Dim iType As Integer

        '部材番号の情報が入力されているか調べる
        Dim tmpMemberFirst As New List(Of Integer)
        Dim tmpMemberLast As New List(Of Integer)

        Try
            Dim c = Me.fpTargetMember_Sheet1.Cells
            For row = 0 To Me.fpTargetMember_Sheet1.RowCount - 1
                Dim FirstNo As Integer = Val(c(row, 0).Value)
                Dim LastNo As Integer = Val(c(row, 1).Value)
                If FirstNo * LastNo > 0 Then
                    tmpMemberFirst.Add(FirstNo)
                    tmpMemberLast.Add(LastNo)
                End If
            Next
        Catch ex As Exception
            isSaveFlg = True
            Throw ex
        End Try


        Dim fp = Me.FpPilePropertyGrid
        Dim sh = Me.FpPilePropertyGrid_Sheet1
        sh.ClearRange(0, 0, sh.RowCount, sh.ColumnCount, True)

        Dim Cells = sh.Cells

        Try
            Dim d As CSNAPDBEx = Input.Data.SNAP.SnapDB

            Dim tmp3 As New List(Of MybarInfo)

            If tmpMemberFirst.Count > 0 AndAlso Not d Is Nothing Then
                '部材番号の情報が入力されている場合
                '鉄筋径などの情報を調べてグリッドに表示する。

                Dim tmp2 As New MyBarReg
                Dim tmp4 As New List(Of Integer)
                For i = 0 To tmpMemberFirst.Count - 1
                    For j = tmpMemberFirst(i) To tmpMemberLast(i)
                        '鉄筋の情報を取得
                        Dim tmp1 As New MybarInfo
                        tmp1.DL = d.GetDLNo(j)
                        Dim DL As IDLInfo = d.DLInfo(tmp1.DL)

                        If DL.iType = 62 Then
                            '鋼管杭
                            tmp1.D1 = DL.ShpCtrl.D
                            tmp1.n1 = DL.ShpCtrl.t
                            tmp1.D2 = DL.StlBar.FSY
                            tmp1.iType = DL.iType
                            iType = DL.iType
                        ElseIf DL.iType = 501 Then
                            '鋼管杭頭
                            tmp1.D1 = DL.ShpCtrl.R * 2
                            tmp1.n1 = DL.ShpCtrl.t
                            tmp1.D2 = DL.StlBar.FSY
                            tmp1.iType = DL.iType
                            iType = DL.iType
                        Else
                            'RC杭
                            If DL.StlBar.Item Is Nothing Then
                                Continue For
                            End If
                            tmp1.D1 = DL.StlBar.Item(0).Dia
                            For k = 0 To DL.StlBar.NS - 1
                                tmp1.n1 += DL.StlBar.Item(k).Num
                            Next
                            tmp1.D2 = DL.StlBar.DiwR
                            tmp1.Aw = DL.StlBar.HRW
                            tmp1.Ss = DL.StlBar.SW
                            tmp1.iType = DL.iType
                            iType = DL.iType
                        End If
                        '既に登録されている情報と照合し、新しい断面であれば記録
                        If tmp2.Add(tmp1) = -1 Then
                            If i > 0 Then
                                MsgBox(String.Format("杭1列目と {0}列目の鉄筋情報が異なっています。", i + 1))
                            Else
                                tmp3.Add(tmp1)
                                tmp4.Add(j)
                            End If
                        End If
                    Next j
                Next

                sh.RowCount = tmp3.Count + 1 '最後に段落しの分を＋１

            End If


            If iType = 62 OrElse iType = 501 Then
                '鋼管杭
                sh.ColumnCount = 4

                ' 列ヘッダの結合
                sh.ColumnHeader.RowCount = 2
                sh.ColumnHeader.Cells(0, 0).RowSpan = 2
                sh.ColumnHeader.Cells(0, 1).ColumnSpan = 3
                ' 列ヘッダキャプションの設定
                sh.ColumnHeader.Cells(0, 0).Value = ""
                sh.ColumnHeader.Cells(0, 1).Value = "鋼管杭"
                sh.ColumnHeader.Cells(1, 1).Value = "杭径"
                sh.ColumnHeader.Cells(1, 2).Value = "厚"
                sh.ColumnHeader.Cells(1, 3).Value = "強度"

                Me.gbRCPileProperty.Text = "鋼管断面緒元"
                Me.RCPileLengthGroup.Text = "鋼管断面 段落し長"
                Me.Label2.Text = "杭頭埋込長"
                Me.Label3.Text = "上杭長"
                Me.Label14.Text = "・施工余裕長"
                Me.Label16.Text = "低止まり"
                Me.Label17.Text = "高止まり"
                '定着長のデータをクリア
                Me.Label8.Enabled = False
                Me.tbPileMidLength.Enabled = False
                Me.Label6.Enabled = False
                For Each item In Input.Data.PileAnchorBar.段落し情報
                    item.DesignMarginLength = Nothing
                Next

                For i = 0 To tmp3.Count - 1
                    '鉄筋情報と距離をグリッドに
                    Dim tmp1 = tmp3(i)
                    Cells(i, 0).Value = tmp1.DL 'DL番号
                    Cells(i, 1).Value = tmp1.D1 '杭径
                    Cells(i, 2).Value = tmp1.n1 '厚
                    Cells(i, 3).Value = tmp1.D2 '強度
                    sh.Rows(i).BackColor = FormSettings.ReadOnlyColor
                Next

            Else
                'RC杭
                sh.ColumnCount = 6

                ' 列ヘッダの結合
                sh.ColumnHeader.RowCount = 2
                sh.ColumnHeader.Cells(0, 0).RowSpan = 2
                sh.ColumnHeader.Cells(0, 1).ColumnSpan = 2
                sh.ColumnHeader.Cells(0, 3).ColumnSpan = 3
                ' 列ヘッダキャプションの設定
                sh.ColumnHeader.Cells(0, 0).Value = ""
                sh.ColumnHeader.Cells(0, 1).Value = "軸方向鉄筋"
                sh.ColumnHeader.Cells(0, 3).Value = "帯鉄筋"
                sh.ColumnHeader.Cells(1, 1).Value = "径"
                sh.ColumnHeader.Cells(1, 2).Value = "本数"
                sh.ColumnHeader.Cells(1, 3).Value = "径"
                sh.ColumnHeader.Cells(1, 4).Value = "本数"
                sh.ColumnHeader.Cells(1, 5).Value = "間隔"
                Me.Label2.Text = "杭頭定着長"
                Me.Label3.Text = "段落し長"
                Me.Label14.Text = "・カットオフ点から"
                Me.Label16.Text = "上方に"
                Me.Label17.Text = "下方に"
                Me.Label8.Enabled = True
                Me.tbPileMidLength.Enabled = True
                Me.Label6.Enabled = True

                For i = 0 To tmp3.Count - 1
                    '鉄筋情報と距離をグリッドに
                    Dim tmp1 = tmp3(i)
                    Cells(i, 0).Value = tmp1.DL 'DL番号
                    Cells(i, 1).Value = tmp1.D1 '主筋-径
                    Cells(i, 2).Value = tmp1.n1 '主筋-本数
                    Cells(i, 3).Value = tmp1.D2 '帯筋-径
                    Cells(i, 4).Value = tmp1.Aw '帯筋-本数
                    Cells(i, 5).Value = tmp1.Ss '帯筋-間隔
                    sh.Rows(i).BackColor = FormSettings.ReadOnlyColor
                Next

            End If

        Catch ex As Exception
            isSaveFlg = True
        End Try

        Try
            '(段落し後の鉄筋情報) の初期化
            If sh.RowCount > 0 Then

                Dim row As Integer = sh.RowCount - 1

                If iType = 62 OrElse iType = 501 Then
                    With Input.Data.PileAnchorBar.段落し情報(index)
                        If Not Integer.TryParse(.D1, Cells(row, 1).Value) Then Cells(row, 1).Value = Nothing
                        If Not Integer.TryParse(.n1, Cells(row, 2).Value) Then Cells(row, 2).Value = Nothing
                        If Not Integer.TryParse(.D2, Cells(row, 3).Value) Then Cells(row, 3).Value = Nothing
                    End With
                    'シートの後端にある非スクロール行
                    sh.FrozenTrailingRowCount = 1
                    Cells(row, 3).ColumnSpan = 1
                    Cells(row, 3).BackColor = sh.NullForeColor
                Else
                    With Input.Data.PileAnchorBar.段落し情報(index)
                        If Not Integer.TryParse(.D1, Cells(row, 1).Value) Then Cells(row, 1).Value = Nothing
                        If Not Integer.TryParse(.n1, Cells(row, 2).Value) Then Cells(row, 2).Value = Nothing
                    End With
                    'シートの後端にある非スクロール行
                    sh.FrozenTrailingRowCount = 1
                    Cells(row, 3).ColumnSpan = 3
                    Cells(row, 3).BackColor = FormSettings.ReadOnlyColor
                    Cells(row, 3).Text = "←段落し後の断面"
                End If

            End If

        Catch ex As Exception

        End Try

    End Sub

    Private Class MyBarReg
        Public Bars As New List(Of MybarInfo)
        Public Function Add(tmp As MybarInfo) As Integer
            Dim id = Eqal(tmp)
            If id = -1 Then
                Bars.Add(tmp)
            End If
            Return id
        End Function
        ''' <summary>同じ情報が登録されているか調べる</summary>
        ''' <returns>-1 は未登録</returns>
        Private Function Eqal(tmp As MybarInfo) As Integer
            For i = 0 To Bars.Count - 1
                Dim t = Bars(i)
                If t.DL = tmp.DL _
                And t.D1 = tmp.D1 _
                And t.n1 = tmp.n1 _
                And t.D2 = tmp.D2 _
                And t.Aw = tmp.Aw _
                And t.Ss = tmp.Ss Then
                    Return i
                End If
            Next
            Return -1
        End Function
    End Class

    Private Class MybarInfo
        Public DL As Integer = 0   '断面番号
        Public D1 As Integer = 0   '軸方向鉄筋径
        Public n1 As Integer = 0   '軸方向鉄筋本数
        Public D2 As Integer = 0   '帯鉄筋径
        Public Aw As Single = 0    '帯鉄筋組数
        Public Ss As Single = 0    '帯鉄筋間隔
        Public iType As Integer = 52    '断面形状
    End Class

#End Region

#Region "データセーブ"

    ''' <summary>
    ''' 現在の入力データセーブ
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataSave()
        If isSaveFlg = False Then
            Return
        End If

        ' 現在表示中（カレントデータ）以外のデータは、既に保存されているため
        ' ここでは、カレントデータのみ保存対象とする。
        Dim result As New clsPileAnchorBar
        Try

            With result

                '定着長等 の保存
                .PileHeadLength = Me.tbPileHeadLength.Text                                      '杭頭定着長
                .ParagraphLength = Me.tbParagraphLength.Text                                    '段落し長
                .ReductionCoefficient = Me.tbReductionCoefficient.Text                          '継手低減係数
                .PileMidLength = Me.tbPileMidLength.Text                                        '段落し定着長
                .DesignMarginLength = Me.tbDesignMarginLength.Text                              '設計余裕長
                .DisabledFromPileHead = Me.tbDisabledFromPileHead.Text                          '継手を設けてはいけない範囲で、杭頭から
                .DisabledUpwardFromCutOffPoint = Me.tbDisabledUpwardFromCutOffPoint.Text        '継手を設けてはいけない範囲で、カットオフ点から上方に
                .DisabledDownwardFromCutOffPoint = Me.tbDisabledDownwardFromCutOffPoint.Text    '継手を設けてはいけない範囲で、カットオフ点から下方に

                'RCPilePropertyGrid2(段落し後の鉄筋情報) の保存
                Dim sh = Me.FpPilePropertyGrid_Sheet1
                Dim Cells = sh.Cells
                Dim r = sh.RowCount - 1
                Dim colCount As Integer = sh.ColumnCount
                If r > 0 Then
                    If colCount > 1 Then .D1 = Cells(r, 1).Value
                    If colCount > 2 Then .n1 = Cells(r, 2).Value
                    If colCount > 3 Then .D2 = Cells(r, 3).Value
                    If colCount > 4 Then .Aw = Cells(r, 4).Value
                    If colCount > 5 Then .Ss = Cells(r, 5).Value

                    '部材番号データの保存
                    Dim c = Me.fpTargetMember_Sheet1.Cells
                    '最終入力行を調べる
                    Dim rowCount As Integer = 0
                    For row = 0 To Me.fpTargetMember_Sheet1.RowCount - 1
                        Dim dbl As Double
                        For i = 0 To 2
                            If Double.TryParse(c(row, i).Value, dbl) Then
                                rowCount = row
                            End If
                        Next
                    Next
                    '最終入力行まで保存
                    For row = 0 To rowCount
                        Dim int As Integer
                        If Integer.TryParse(c(row, 0).Value, int) Then
                            .dgMemberFirst.Add(int)
                        Else
                            .dgMemberFirst.Add(Nothing)
                        End If
                        If Integer.TryParse(c(row, 1).Value, int) Then
                            .dgMemberLast.Add(int)
                        Else
                            .dgMemberLast.Add(Nothing)
                        End If
                        Dim sng As Single
                        If Single.TryParse(c(row, 2).Value, sng) Then
                            .dgMemberNumber.Add(sng)
                        Else
                            .dgMemberNumber.Add(Nothing)
                        End If
                    Next
                End If

            End With
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        End Try
        Try
            ' 保存した内容を登録
            If Input.Data.PileAnchorBar.段落し情報.Count < Me.CurrentDataIndex Then
                Input.Data.PileAnchorBar.段落し情報.Add(result)
            Else
                Input.Data.PileAnchorBar.段落し情報(Me.CurrentDataIndex) = result
            End If
        Catch ex As Exception

        End Try

    End Sub

#End Region

#Region "着目点 カレントデータの変更"

    ''' <summary>
    ''' 一覧表のフォーカスが変わったらMyPictureBox1を更新
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub FpPilePropertyGrid_SelectionChanged(sender As Object, e As FarPoint.Win.Spread.SelectionChangedEventArgs) Handles FpPilePropertyGrid.SelectionChanged
        Try
            Dim fp As FpSpread = sender
            Dim sh = fp.ActiveSheet
            If e.Range.Row = sh.RowCount - 1 Then
                Return
            End If
            MyPictureMode = 0
            Dim DL = sh.Cells(e.Range.Row, 0)
            Me.ActiveDLNo = DL.Value
            Call MyPictureBox1_Paint(Me.MyPictureBox1, Nothing)

        Catch ex As Exception
            MyPictureMode = -1
        End Try
    End Sub

    ''' <summary>
    ''' 部材一覧
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub fpTargetMember_CellChanged(sender As Object, e As SheetViewEventArgs) Handles fpTargetMember_Sheet1.CellChanged
        Input.ChengeDataFlg = True
    End Sub


    Private Sub fpTargetMember_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles fpTargetMember.SelectionChanged
        Try
            MyPictureMode = 1
            Dim fp As FpSpread = sender
            Dim Cells = fp.ActiveSheet.Cells
            Dim r As Integer = e.Range.Row
            Me.CurrentMemberNoFirst = Cells(r, 0).Value
            Me.CurrentMemberNoLast = Cells(r, 1).Value
            Call MyPictureBox1_Paint(Me.MyPictureBox1, Nothing)

        Catch ex As Exception
            MyPictureMode = -1
        End Try
    End Sub


    ''' <summary>
    ''' MyPictureBox1の描画
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub MyPictureBox1_Paint(sender As Object, e As PaintEventArgs) Handles MyPictureBox1.Paint
        Try
            Dim SnapDB As CSNAPDBEx = Input.Data.SNAP.SnapDB
            If Not SnapDB Is Nothing Then
                Try
                    Me.MyPictureBox1.PointX.Clear()
                    Me.MyPictureBox1.PointY.Clear()
                    For i = 0 To SnapDB.GetJointCount
                        Dim p = SnapDB.GetJoint(i)
                        Me.MyPictureBox1.PointX.Add(i, p.X)
                        Me.MyPictureBox1.PointY.Add(i, p.Y)
                    Next
                Catch ex As Exception
                End Try
                Try
                    Me.MyPictureBox1.MemberNum = SnapDB.GetMemberCount
                    ReDim Me.MyPictureBox1.MemberNo(Me.MyPictureBox1.MemberNum + 1)
                    ReDim Me.MyPictureBox1.PrintMemberNo(Me.MyPictureBox1.MemberNum + 1)
                    ReDim Me.MyPictureBox1.IPointNo(Me.MyPictureBox1.MemberNum + 1)
                    ReDim Me.MyPictureBox1.JPointNo(Me.MyPictureBox1.MemberNum + 1)

                    For i = 1 To Me.MyPictureBox1.MemberNum
                        Me.MyPictureBox1.MemberNo(i) = i
                        Select Case MyPictureMode
                            Case -1
                                Me.MyPictureBox1.PrintMemberNo(i) = i.ToString()
                            Case 0
                                If SnapDB.GetDLNo(i) = Me.ActiveDLNo Then
                                    Me.MyPictureBox1.PrintMemberNo(i) = i.ToString()
                                Else
                                    Me.MyPictureBox1.PrintMemberNo(i) = ""
                                End If
                            Case 1
                                If i >= Me.CurrentMemberNoFirst And i <= Me.CurrentMemberNoLast Then
                                    Me.MyPictureBox1.PrintMemberNo(i) = i.ToString()
                                Else
                                    Me.MyPictureBox1.PrintMemberNo(i) = ""
                                End If
                        End Select
                        Me.MyPictureBox1.IPointNo(i) = SnapDB.GetMember(i).Itan
                        Me.MyPictureBox1.JPointNo(i) = SnapDB.GetMember(i).Jtan
                    Next
                Catch ex As Exception
                    'SNAPDBLib.dll 未完成のため何もしない
                End Try
            Else
                ' 初期化
                Me.MyPictureBox1.PointX.Clear()
                Me.MyPictureBox1.PointY.Clear()
            End If
        Catch ex As Exception
            Throw ex
        End Try
        Me.MyPictureBox1.Refresh()
    End Sub

#End Region

#Region "入力動作"

    Private Sub RCPilePropertyGrid_Leave(sender As Object, e As EventArgs)
        MyPictureMode = -1
        Call MyPictureBox1_Paint(Me.MyPictureBox1, Nothing)
    End Sub

    ''' <summary>
    ''' 段落とし後の鉄筋に変更があったら
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub FpPilePropertyGrid_CellChanged(sender As Object, e As FarPoint.Win.Spread.SheetViewEventArgs)
        If e.Row = e.RowCount - 1 Then
            Input.ChengeDataFlg = True
        End If
    End Sub


    Private Sub tbParagraphLength_KeyDown(sender As Object, e As KeyEventArgs) Handles tbParagraphLength.KeyDown
        If e.KeyCode = Keys.Enter Then
            Dim dgv As TextBox = DirectCast(sender, TextBox)
            'Tabキーを送信する
            SendKeys.Send("{TAB}")
            'フォーカスが下に移動しないようにする
            e.Handled = True
        End If
    End Sub
    Private Sub tbDesignMarginLength_KeyDown(sender As Object, e As KeyEventArgs) Handles tbDesignMarginLength.KeyDown
        If e.KeyCode = Keys.Enter Then
            Dim dgv As TextBox = DirectCast(sender, TextBox)
            'Tabキーを送信する
            SendKeys.Send("{TAB}")
            'フォーカスが下に移動しないようにする
            e.Handled = True
        End If
    End Sub
    Private Sub tbDisabledDownwardFromCutOffPoint_KeyDown(sender As Object, e As KeyEventArgs) Handles tbDisabledDownwardFromCutOffPoint.KeyDown
        If e.KeyCode = Keys.Enter Then
            Dim dgv As TextBox = DirectCast(sender, TextBox)
            'Tabキーを送信する
            SendKeys.Send("{TAB}")
            'フォーカスが下に移動しないようにする
            e.Handled = True
        End If
    End Sub
    Private Sub tbDisabledFromPileHead_KeyDown(sender As Object, e As KeyEventArgs) Handles tbDisabledFromPileHead.KeyDown
        If e.KeyCode = Keys.Enter Then
            Dim dgv As TextBox = DirectCast(sender, TextBox)
            'Tabキーを送信する
            SendKeys.Send("{TAB}")
            'フォーカスが下に移動しないようにする
            e.Handled = True
        End If
    End Sub
    Private Sub tbDisabledUpwardFromCutOffPoint_KeyDown(sender As Object, e As KeyEventArgs) Handles tbDisabledUpwardFromCutOffPoint.KeyDown
        If e.KeyCode = Keys.Enter Then
            Dim dgv As TextBox = DirectCast(sender, TextBox)
            'Tabキーを送信する
            SendKeys.Send("{TAB}")
            'フォーカスが下に移動しないようにする
            e.Handled = True
        End If
    End Sub
    Private Sub tbPileHeadLength_KeyDown(sender As Object, e As KeyEventArgs) Handles tbPileHeadLength.KeyDown
        If e.KeyCode = Keys.Enter Then
            Dim dgv As TextBox = DirectCast(sender, TextBox)
            'Tabキーを送信する
            SendKeys.Send("{TAB}")
            'フォーカスが下に移動しないようにする
            e.Handled = True
        End If
    End Sub
    Private Sub tbPileMidLength_KeyDown(sender As Object, e As KeyEventArgs) Handles tbPileMidLength.KeyDown
        If e.KeyCode = Keys.Enter Then
            Dim dgv As TextBox = DirectCast(sender, TextBox)
            'Tabキーを送信する
            SendKeys.Send("{TAB}")
            'フォーカスが下に移動しないようにする
            e.Handled = True
        End If
    End Sub
    Private Sub tbReductionCoefficient_KeyDown(sender As Object, e As KeyEventArgs) Handles tbReductionCoefficient.KeyDown
        If e.KeyCode = Keys.Enter Then
            Dim dgv As TextBox = DirectCast(sender, TextBox)
            'Tabキーを送信する
            SendKeys.Send("{TAB}")
            'フォーカスが下に移動しないようにする
            e.Handled = True
        End If
    End Sub

#End Region


    Private Sub TextBox2_TextChanged(sender As Object, e As EventArgs)
        Input.ChengeDataFlg = True
    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs)
        Input.ChengeDataFlg = True
    End Sub

    Private Sub TextBox3_TextChanged(sender As Object, e As EventArgs)
        Input.ChengeDataFlg = True
    End Sub

    Private Sub tbPileHeadLength_TextChanged(sender As Object, e As EventArgs) Handles tbPileHeadLength.TextChanged
        Input.ChengeDataFlg = True
    End Sub

    Private Sub tbParagraphLength_TextChanged(sender As Object, e As EventArgs) Handles tbParagraphLength.TextChanged
        Input.ChengeDataFlg = True
    End Sub

    Private Sub tbReductionCoefficient_TextChanged(sender As Object, e As EventArgs) Handles tbReductionCoefficient.TextChanged
        Input.ChengeDataFlg = True
    End Sub

    Private Sub tbPileMidLength_TextChanged(sender As Object, e As EventArgs) Handles tbPileMidLength.TextChanged
        Input.ChengeDataFlg = True
    End Sub

    Private Sub tbDesignMarginLength_TextChanged(sender As Object, e As EventArgs) Handles tbDesignMarginLength.TextChanged
        Input.ChengeDataFlg = True
    End Sub

    Private Sub tbDisabledFromPileHead_TextChanged(sender As Object, e As EventArgs) Handles tbDisabledFromPileHead.TextChanged
        Input.ChengeDataFlg = True
    End Sub

    Private Sub tbDisabledUpwardFromCutOffPoint_TextChanged(sender As Object, e As EventArgs) Handles tbDisabledUpwardFromCutOffPoint.TextChanged
        Input.ChengeDataFlg = True
    End Sub

    Private Sub tbDisabledDownwardFromCutOffPoint_TextChanged(sender As Object, e As EventArgs) Handles tbDisabledDownwardFromCutOffPoint.TextChanged
        Input.ChengeDataFlg = True
    End Sub


End Class