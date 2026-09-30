Imports System.Drawing.Drawing2D
Imports Aggre.ChildFormElementSetting
Imports FarPoint.Win.Spread
Imports FarPoint.Win.Spread.CellType
Imports GrapeCity.Win.Spread.InputMan.CellType.Fields

''' <summary>
''' 
''' </summary>
''' <remarks></remarks>
Public Class ChildFormFoundationSetting

#Region "メンバ変数"

    '現在表示中のデータ番号 0～
    Private _CurrentDataIndex As Integer
    Private Property CurrentDataIndex As Integer
        Get
            Return _CurrentDataIndex
        End Get
        Set(value As Integer)

            _CurrentDataIndex = value

            Dim maxCount As Integer = Me.fpFoundationList_Sheet1.RowCount - 1

            If _CurrentDataIndex <= 0 Then
                _CurrentDataIndex = 0
                Me.btnBack.Enabled = False
            Else
                Me.btnBack.Enabled = True
            End If

            If _CurrentDataIndex >= maxCount Then
                _CurrentDataIndex = maxCount
                Me.btnNext.Enabled = False
            Else
                Me.btnNext.Enabled = True
            End If
        End Set
    End Property

    Private isSaveFlg As Boolean

    '現在表示中の着目点番号
    Private CurrentPointNo As Integer = 0
    Private CurrentPointDistance As Double = 0
    Private CurrentMemberNo As Integer = 0
    Private Const ReactAtPointRowCount As Integer = 30
    ''' <summary>
    ''' シングルトン用インスタンス
    ''' </summary>
    ''' <remarks></remarks>
    Private Shared instance As ChildFormFoundationSetting = New ChildFormFoundationSetting()

#End Region

#Region "プロパティ"

    ''' <summary>
    ''' インスタンス取得
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property GetInstance() As ChildFormFoundationSetting
        Get
            '静的変数が解放されていた場合のみ、インスタンスを生成する
            If instance Is Nothing Then
                instance = New ChildFormFoundationSetting()
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
    End Sub

#End Region

#Region "初期化処理"

    ''' <summary>
    ''' 初期処理
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataInit()
        Me.Text = "基礎"

        'dataGridView4 の初期化
        Me.dgLimitValue_CellFormat(0)
        Me.Button1.Visible = True

        'データロード
        Call DataLoad()
        Call MyPictureBox1_Paint(Me.MyPictureBox1, Nothing)

    End Sub

    ''' <summary>
    ''' ComboBox1が変更されたら dataGridView4 を初期化する。
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub FoundationType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbFoundationType.SelectedIndexChanged

        Dim ComboBox1 As ComboBox = TryCast(sender, ComboBox)

        Dim fp As FpSpread = Me.fpDisgReactAtPoint
        Dim sh反力照査 As SheetView = Me.fpDisgReactAtPoint_反力照査位置
        Dim sh変位照査 As SheetView = Me.fpDisgReactAtPoint_変位照査位置
        '安定レベルの制限値グリッドの初期化
        Select Case ComboBox1.SelectedIndex
            Case 0
                Me.fpDisgReactAtPoint.TabStripPolicy = False
                sh変位照査.RowCount = ChildFormFoundationSetting.ReactAtPointRowCount

                Dim num As New NumberCellType() : num.DecimalPlaces = 0
                sh変位照査.Columns(0).CellType = num
                sh変位照査.Columns(0).Width = 73
                sh変位照査.Columns(1).Visible = True
                sh反力照査.RowCount = ChildFormFoundationSetting.ReactAtPointRowCount '行数固定とする
                sh反力照査.Visible = True

                '杭基礎
                Me.Label4.Text = "[ 変位照査位置 ]"
                Me.dgLimitValue_CellFormat(ComboBox1.SelectedIndex)
                Me.Button1.Visible = True
                Me.Label2.Visible = True
                sh変位照査.Visible = True
                Me.Label2.Text = ""

            Case 1
                Me.fpDisgReactAtPoint.TabStripPolicy = True
                sh変位照査.RowCount = 1 '行数固定とする
                sh変位照査.Columns(0).Width = 150
                sh変位照査.Columns(1).Visible = False
                sh反力照査.Visible = False

                '直接基礎
                Me.Label4.Text = "[ 直接基礎照査位置 ]"
                Me.dgLimitValue_CellFormat(ComboBox1.SelectedIndex)
                Me.Button1.Visible = False
                Me.Label2.Visible = False
                Me.Label2.Visible = False
                Call SetChokuKisoAtPoint(sh変位照査.Cells(0, 0))
                Me.Label2.Text = ""
            Case Else
                Exit Sub
        End Select
    End Sub

    ''' <summary>
    ''' 制限値 dgLimitValue の初期化
    ''' </summary>
    ''' <param name="index"></param>
    ''' <remarks></remarks>
    Private Sub dgLimitValue_CellFormat(ByVal index As Integer)

        cbFoundationType.SelectedIndex = index

        Dim 直接基礎Sheets = Me.fpLimitValue.Sheets("直接基礎")
        Select Case index
            Case 0 '"杭基礎"
                Me.fpLimitValue.TabStripPolicy = False
                直接基礎Sheets.Visible = False
                Me.fpLimitValue.ActiveSheetIndex = 0
                Me.fpLimitValue.VerticalScrollBarPolicy = FarPoint.Win.Spread.ScrollBarPolicy.AsNeeded
                Me.Label6.Visible = False

            Case 1 '"直接基礎"
                Me.fpLimitValue.TabStripPolicy = True
                直接基礎Sheets.Visible = True
                Me.fpLimitValue.ActiveSheet = 直接基礎Sheets
                Me.fpLimitValue.VerticalScrollBarPolicy = FarPoint.Win.Spread.ScrollBarPolicy.Never
                Me.Label6.Visible = True

            Case Else
                Exit Sub
        End Select

        'fpFoundationList の表示更新
        Try
            Dim Cells = Me.fpFoundationList_Sheet1.Cells
            Cells(CurrentDataIndex, 0).Value = Me.cbFoundationType.SelectedItem
        Catch ex As Exception
        End Try

    End Sub

    Private Sub fpLimitValue_EditModeOn(sender As Object, e As EventArgs) Handles fpLimitValue.EditModeOn
        Dim fp As FpSpread = sender
        Dim sh As SheetView = fp.ActiveSheet
        Dim cel As Cell = sh.ActiveCell

        ' 読み取り専用セルは、編集付加とする
        If cel.BackColor.R = FormSettings.ReadOnlyColor.R And
            cel.BackColor.G = FormSettings.ReadOnlyColor.G And
            cel.BackColor.B = FormSettings.ReadOnlyColor.B Then
            fp.StopCellEditing()
        End If

    End Sub


    ''' <summary>
    ''' インスタンスリフレッシュ
    ''' </summary>
    ''' <remarks></remarks>
    Public Shared Sub Reflesh()
        instance = New ChildFormFoundationSetting()
    End Sub

#End Region

#Region "データロード"

    ''' <summary>
    ''' データロード
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataLoad()
        'データ一覧の読み込み
        Me.dgFoundationList_DataLoad()
        '最初のデータを表示
        Me.CurrentDataIndex = 0
        '安定レベルの制限値, 照査位置の読み込み
        Me.DataGridView_DataLoad(Me.CurrentDataIndex)

    End Sub

    ''' <summary>
    ''' データ一覧の読み込み
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub dgFoundationList_DataLoad()

        Dim sh As SheetView = Me.fpFoundationList_Sheet1

        Dim Cells = sh.Cells

        If Input.Data.Foundation.基礎照査.Count = 0 Then
            Input.Data.Foundation.基礎照査.Add(New clsFoundation())
            sh.RowCount = 1
            Cells(0, 0).Value = Me.cbFoundationType.SelectedItem
            Me.CurrentDataIndex = 0
        Else
            Try
                isSaveFlg = False
                Me.fpFoundationList_Sheet1.RowCount = Input.Data.Foundation.基礎照査.Count
                isSaveFlg = True
                For row = 0 To Me.fpFoundationList_Sheet1.RowCount - 1
                    '基礎形式の表示
                    Cells(row, 0).Value = Me.cbFoundationType.Items(Input.Data.Foundation.基礎照査(row).FoundationType)
                    '基礎照査位置の表示
                    Select Case Input.Data.Foundation.基礎照査(row).FoundationType
                        Case 0
                            Dim s As String = ""
                            For Each ap In Input.Data.Foundation.基礎照査(row).dgDispAtPoints
                                If IsNumeric(ap.pointNo) AndAlso ap.pointNo > 0 Then
                                    s += ap.pointNo & " "
                                End If
                            Next
                            Cells(row, 1).Value = s.Trim
                            '杭基礎照査位置の表示
                            s = ""
                            For Each ap In Input.Data.Foundation.基礎照査(row).dgReactAtPoint
                                If IsNumeric(ap.pointNo) AndAlso ap.pointNo > 0 Then
                                    s += ap.pointNo & " "
                                End If
                            Next
                            Cells(row, 2).Value = s.Trim

                        Case 1

                            Cells(row, 1).Value = Input.Data.Foundation.基礎照査(row).dgDispAtPoint.pointNo
                    End Select

                Next
            Catch ex As Exception
                isSaveFlg = True
            End Try
        End If
    End Sub

    ''' <summary>
    ''' 安定レベルの制限値, 照査位置の読み込み
    ''' </summary>
    ''' <param name="index"></param>
    ''' <remarks></remarks>
    Private Sub DataGridView_DataLoad(index As Integer)


        Dim TargetData = Input.Data.Foundation.基礎照査(index)

        '基礎形式他の読み込み
        Me.cbFoundationType.SelectedIndex = TargetData.FoundationType
        Me.Label2.Text = TargetData.sdtFilePath

        '安定レベルの制限値の読み込み
        Try
            '連携バージョンチェック機能
            Select Case TargetData.FoundationType

                Case 0 '杭基礎の制限値
                    Dim 杭_普通_Sheet = Me.fpLimitValue_普通 ' Me.fpLimitValue.Sheets("普通")
                    Dim 杭_普通_Cells = 杭_普通_Sheet.Cells
                    Dim 杭_液状化_Sheet = Me.fpLimitValue_液状化 'Me.fpLimitValue.Sheets("液状化")
                    Dim 杭_液状化_Cells = 杭_液状化_Sheet.Cells
                    Dim row As Integer = 0
                    For i = 0 To 杭_普通_Sheet.RowCount - 1
                        Single.TryParse(TargetData.dgLimitValue0(row), 杭_普通_Cells(i, 5).Value)
                        If 杭_普通_Cells(i, 5).Value = 0 Then 杭_普通_Cells(i, 5).Value = Nothing
                        row += 1
                    Next
                    For i = 0 To 杭_液状化_Sheet.RowCount - 1
                        Single.TryParse(TargetData.dgLimitValue0(row), 杭_液状化_Cells(i, 5).Value)
                        If 杭_液状化_Cells(i, 5).Value = 0 Then 杭_液状化_Cells(i, 5).Value = Nothing
                        row += 1
                    Next
                Case 1 '直接基礎の制限値
                    Dim 直接基礎_Sheet = Me.fpLimitValue_直接基礎 'Me.fpLimitValue.Sheets("直接基礎")
                    Dim 直接基礎_Cells = 直接基礎_Sheet.Cells
                    For row = 0 To 直接基礎_Sheet.RowCount - 1
                        Single.TryParse(TargetData.dgLimitValue1(row), 直接基礎_Cells(row, 5).Value)
                        If 直接基礎_Cells(row, 5).Value = 0 Then 直接基礎_Cells(row, 5).Value = Nothing
                    Next
            End Select

        Catch ex As Exception
        End Try

        '照査位置の読み込み
        Try

            Select Case TargetData.FoundationType
                Case 0
                    Dim Cells_反力照査位置 = Me.fpDisgReactAtPoint_反力照査位置.Cells
                    Dim Cells_変位照査位置 = Me.fpDisgReactAtPoint_変位照査位置.Cells
                    ' 杭反力照査位置
                    For row = 0 To Me.fpDisgReactAtPoint_反力照査位置.RowCount - 1
                        If TargetData.dgReactAtPoint.Count > row Then
                            Dim v1 As Integer
                            Integer.TryParse(TargetData.dgReactAtPoint(row).pointNo, v1)
                            Cells_反力照査位置(row, 0).Value = IIf(v1 > 0, v1, "")
                            Dim v2 As Single
                            Single.TryParse(TargetData.dgReactAtPoint(row).Direction, v2)
                            Cells_反力照査位置(row, 1).Value = IIf(v2 > 0, v2, "")
                        Else
                            Cells_反力照査位置(row, 0).Value = Nothing
                            Cells_反力照査位置(row, 1).Value = Nothing
                        End If
                    Next
                    ' 杭変位照査位置
                    For row = 0 To Me.fpDisgReactAtPoint_変位照査位置.RowCount - 1
                        If TargetData.dgDispAtPoints.Count > row Then
                            Dim v1 As Integer
                            Integer.TryParse(TargetData.dgDispAtPoints(row).pointNo, v1)
                            Cells_変位照査位置(row, 0).Value = IIf(v1 > 0, v1, "")
                            Dim v2 As Single
                            Single.TryParse(TargetData.dgDispAtPoints(row).Direction, v2)
                            Cells_変位照査位置(row, 1).Value = IIf(v1 > 0, IIf(v2 > 0, v2, 0F), "")
                        Else
                            Cells_変位照査位置(row, 0).Value = Nothing
                            Cells_変位照査位置(row, 1).Value = Nothing
                        End If
                    Next

                Case 1
                    '直接基礎照査位置
                    Dim Cells_変位照査位置 = Me.fpDisgReactAtPoint_変位照査位置.Cells

                    Call SetChokuKisoAtPoint(Cells_変位照査位置(0, 0))
                    If TargetData.dgDispAtPoint Is Nothing Then
                        TargetData.dgDispAtPoint = New clsAtPoints
                        Cells_変位照査位置(0, 0).Value = Nothing
                    Else
                        Dim v1 As Integer
                        Integer.TryParse(GetChokuKisoAtPoint(TargetData.dgDispAtPoint.pointNo), v1)
                        Cells_変位照査位置(0, 0).Value = IIf(v1 > 0, v1, "")
                    End If
            End Select

        Catch ex As Exception
        End Try

    End Sub

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
        Dim result As New clsFoundation

        ' 安定レベルの制限値の保存 **************************************************************
        Select Case Me.cbFoundationType.SelectedIndex
            Case 0
                Dim 杭_普通_Sheet = Me.fpLimitValue_普通
                Dim 杭_液状化_Sheet = Me.fpLimitValue_液状化
                Dim row As Integer = 0
                'For row = 0 To Me.dgLimitValue.RowCount - 1
                For i = 0 To 杭_普通_Sheet.RowCount - 1
                    result.dgLimitValue0(row) = 杭_普通_Sheet.Cells(i, 5).Value
                    row += 1
                Next
                For i = 0 To 杭_液状化_Sheet.RowCount - 1
                    result.dgLimitValue0(row) = 杭_液状化_Sheet.Cells(i, 5).Value
                    row += 1
                Next
            Case 1
                Dim 直接基礎_Sheet = Me.fpLimitValue_直接基礎
                Dim 直接基礎_Cells = 直接基礎_Sheet.Cells
                For row = 0 To 直接基礎_Sheet.RowCount - 1
                    result.dgLimitValue1(row) = 直接基礎_Cells(row, 5).Value
                Next
        End Select

        ' 基礎形式他の保存 ***********************************************************************
        result.FoundationType = Me.cbFoundationType.SelectedIndex
        result.sdtFilePath = Me.Label2.Text

        ' 照査位置の保存 *************************************************************************
        Select Case Me.cbFoundationType.SelectedIndex
            Case 0
                ' 杭照査位置の保存
                Dim 反力照査位置_Sheet = Me.fpDisgReactAtPoint_反力照査位置

                For row = 0 To 反力照査位置_Sheet.RowCount - 1
                    Dim d As New clsAtPoints
                    d.pointNo = 反力照査位置_Sheet.Cells(row, 0).Value
                    d.Direction = 反力照査位置_Sheet.Cells(row, 1).Value
                    Try
                        If Not Val(d.pointNo) * Val(d.Direction) = 0 Then
                            result.dgReactAtPoint.Add(d)
                        End If
                    Catch ex As Exception
                    End Try
                Next

                '変位照査位置の保存
                Dim 変位照査位置_Sheet = Me.fpDisgReactAtPoint_変位照査位置

                For row = 0 To 変位照査位置_Sheet.RowCount - 1
                    Dim AtPoint = New clsAtPoints()
                    AtPoint.pointNo = 変位照査位置_Sheet.Cells(row, 0).Value
                    AtPoint.Direction = 変位照査位置_Sheet.Cells(row, 1).Value
                    Try
                        If Val(AtPoint.pointNo) = 0 Then
                            If IsNothing(AtPoint.Direction) Then
                                Continue For
                            End If
                            If AtPoint.Direction.Trim().Length = 0 Then
                                Continue For
                            End If
                        End If
                        result.dgDispAtPoints.Add(AtPoint)
                    Catch ex As Exception
                    End Try
                Next

            Case 1
                ' 直接基礎照査位置の保存
                Dim 変位照査位置_Sheet = Me.fpDisgReactAtPoint_変位照査位置

                Dim AtPoint = New clsAtPoints()
                AtPoint.pointNo = 変位照査位置_Sheet.Cells(0, 0).Value
                result.dgDispAtPoint = AtPoint
                result.dgReactAtPoint.Add(AtPoint)
        End Select


        Try
            ' 保存した内容を登録
            If Input.Data.Foundation.基礎照査.Count < Me.CurrentDataIndex Then
                Input.Data.Foundation.基礎照査.Add(result)
            Else
                Input.Data.Foundation.基礎照査(Me.CurrentDataIndex) = result
            End If
        Catch ex As Exception

        End Try

    End Sub

#End Region

#Region "データの追加・削除, カレントデータの変更"

    Private Sub AddData()

        '現在の入力データセーブ
        Call DataSave()
        'セーブデータ に行を追加
        Input.Data.Foundation.基礎照査.Add(New clsFoundation With {.FoundationType = Me.cbFoundationType.SelectedIndex})
        'dataGridView3 の初期化
        Call dgFoundationList_DataLoad()
        '現在の入力データ番号を更新
        Me.CurrentDataIndex += 1
        'dataGridView4 の初期化
        Call FoundationType_SelectedIndexChanged(Me.cbFoundationType, Nothing)
        'dataGridView1 の初期化
        Call DataGridView_DataLoad(Me.CurrentDataIndex)

        Me.fpFoundationList_Sheet1.SetActiveCell(Me._CurrentDataIndex, 0)
    End Sub

    Private Sub ChangeData(Index As Integer)

        '現在の入力データセーブ
        Call DataSave()
        Me.CurrentDataIndex = Index
        '安定レベルの制限値, 照査位置の読み込み
        Me.DataGridView_DataLoad(Me.CurrentDataIndex)
    End Sub

    Private Sub DeleteData(Index As Integer)

        Dim targetData = Input.Data.Foundation.基礎照査(Index)
        Input.Data.Foundation.基礎照査.Remove(targetData)
        Me.CurrentDataIndex = Index - 1
        Call dgFoundationList_DataLoad()
        Call DataGridView_DataLoad(Me.CurrentDataIndex)
        Call dgFoundationList_UpDate()


        Me.fpFoundationList_Sheet1.SetActiveCell(Me._CurrentDataIndex, 0)
    End Sub

    Private Sub btnNew_Click(sender As Object, e As EventArgs) Handles btnNew.Click
        Call AddData()
    End Sub

    Private Sub btnBack_Click(sender As Object, e As EventArgs) Handles btnBack.Click
        Call ChangeData(Me.CurrentDataIndex - 1)

        Me.fpFoundationList_Sheet1.SetActiveCell(Me._CurrentDataIndex, 0)
    End Sub

    Private Sub btnNext_Click(sender As Object, e As EventArgs) Handles btnNext.Click
        Call ChangeData(Me.CurrentDataIndex + 1)

        Me.fpFoundationList_Sheet1.SetActiveCell(Me._CurrentDataIndex, 0)
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        Call DeleteData(Me.CurrentDataIndex)
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    Private Sub dgFoundationList_UpDate()
        Try
            Dim row As Integer = Me.CurrentDataIndex

            Dim Cells = Me.fpFoundationList_Sheet1.Cells

            Select Case Me.cbFoundationType.SelectedIndex
                Case 0
                    '杭基礎照査位置の表示
                    Dim s As String = ""
                    For r = 0 To Me.fpDisgReactAtPoint_変位照査位置.Rows.Count - 1
                        Dim ap As String = Me.fpDisgReactAtPoint_変位照査位置.Cells(r, 0).Value
                        If IsNumeric(ap) Then
                            s += ap.Trim & " "
                        End If
                    Next
                    Cells(row, 1).Value = s.Trim

                    s = ""
                    For r = 0 To Me.fpDisgReactAtPoint_反力照査位置.Rows.Count - 1
                        Dim ap As String = Me.fpDisgReactAtPoint_反力照査位置.Cells(r, 0).Value
                        If IsNumeric(ap) Then
                            s += ap.Trim & " "
                        End If
                    Next
                    Cells(row, 2).Value = s.Trim

                Case 1
                    '直接基礎照査位置の表示
                    Dim s As String = ""
                    For r = 0 To Me.fpDisgReactAtPoint_変位照査位置.Rows.Count - 1
                        Dim ap As String = Me.fpDisgReactAtPoint_変位照査位置.Cells(r, 0).Value
                        If IsNumeric(ap) Then
                            s += ap.Trim & " "
                        End If
                    Next
                    Cells(row, 1).Value = s.Trim
                    Cells(row, 2).Value = s.Trim
            End Select

        Catch ex As Exception

        End Try
    End Sub

    ''' <summary>
    ''' カレントデータの変更
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub fpFoundationList_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles fpFoundationList.SelectionChanged

        Dim oldChengeDataFlg = Input.ChengeDataFlg
        Dim oldReadDBFlg = Input.ReadDBFlg

        Call ChangeData(e.Range.Row)

        Input.ChengeDataFlg = oldChengeDataFlg
        Input.ReadDBFlg = oldReadDBFlg

    End Sub


    Private Sub dgReactAtPoint_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs)
        Call dgFoundationList_UpDate()
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub

    ''' <summary>
    ''' 杭の制限値を変更した場合
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub fpLimitValue_Changed(sender As Object, e As FarPoint.Win.Spread.ChangeEventArgs) Handles fpLimitValue.Change
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub


#End Region

#Region "着目点 カレントデータの変更"

    ''' <summary>
    ''' 着目点の選択セルを変えた場合時
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub fpDisgReactAtPoint_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles fpDisgReactAtPoint.SelectionChanged

        Dim fp As FpSpread = sender
        Dim sh As SheetView = fp.ActiveSheet
        Dim cel As Cell = sh.ActiveCell
        Dim Row = e.Range.Row

        If sh.Equals(Me.fpDisgReactAtPoint_反力照査位置) Then

            Try
                Dim dg1 = sh.Cells
                Dim pn = sh.Cells(Row, 0).Value
                Me.CurrentPointNo = 0
                If IsNumeric(pn) Then
                    Me.CurrentMemberNo = Convert.ToInt32(pn)
                Else
                    Me.CurrentMemberNo = 0
                End If
                Me.CurrentPointDistance = 0
                'MyPictureBox1 の更新
                Call MyPictureBox1_Paint(Me.MyPictureBox1, Nothing)
            Catch ex As Exception
            End Try

        Else
            ' 基礎着目点セルクリック時
            Try
                Dim dg2 = sh.Cells
                Dim pn = dg2(Row, 0).Value
                Dim pd = dg2(Row, 1).Value
                If IsNumeric(pn) Then
                    Me.CurrentPointNo = Convert.ToInt32(pn)
                Else
                    Me.CurrentPointNo = 0
                End If
                Me.CurrentMemberNo = 0
                If IsNumeric(pd) Then
                    Me.CurrentPointDistance = Convert.ToDouble(pd)
                Else
                    Me.CurrentPointDistance = 0
                End If
                'MyPictureBox1 の更新
                Call MyPictureBox1_Paint(Me.MyPictureBox1, Nothing)
            Catch ex As Exception
            End Try
        End If

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
                    '入力した節点番号を表示する
                    Dim No As Integer = Me.CurrentPointNo
                    Me.MyPictureBox1.PrintPointNo.Clear()
                    Me.MyPictureBox1.PrintPointNo.Add(No, No.ToString())

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
                        If i = Me.CurrentMemberNo Then
                            Me.MyPictureBox1.PrintMemberNo(i) = i.ToString()
                        Else
                            Me.MyPictureBox1.PrintMemberNo(i) = ""
                        End If
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

#Region "グリッドの入力規制および動作"

    Private Sub fpDisgReactAtPoint_反力照査位置_CellChanged(sender As Object, e As ChangeEventArgs) Handles fpDisgReactAtPoint.Change

        Dim fp As FpSpread = sender
        If fp.ActiveSheetIndex = 1 Then
            fpDisgReactAtPoint_変位照査位置_CellChanged(sender, e)
            Return
        End If
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False

        If e.Column <> 0 Then Return

        Call dgFoundationList_UpDate()

        Try
            Dim sh As SheetView = Me.fpDisgReactAtPoint_反力照査位置
            Dim Cells = sh.Cells
            '部材の奥行き本数を調べる
            Dim d As CSNAPDBEx = Input.Data.SNAP.SnapDB
            If Not d Is Nothing Then
                Dim mNo As Integer = Val(Cells(e.Row, e.Column).Value)
                Me.CurrentPointNo = mNo

                If mNo > 0 Then
                    Dim n As Single = d.GetAp(mNo).ALF
                    If n > 0 Then
                        Cells(e.Row, 1).Value = n
                    End If
                End If
            End If
            Call MyPictureBox1_Paint(Me.MyPictureBox1, Nothing)
        Catch ex As Exception

        End Try


    End Sub

    Private Sub fpDisgReactAtPoint_変位照査位置_CellChanged(sender As Object, e As ChangeEventArgs)

        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False

        If e.Column <> 0 Then Return

        Call dgFoundationList_UpDate()

        Try
            Dim sh As SheetView = Me.fpDisgReactAtPoint_変位照査位置
            Dim nNo As Integer = Val(sh.Cells(e.Row, e.Column).Value)
            Me.CurrentPointNo = nNo

            Call MyPictureBox1_Paint(Me.MyPictureBox1, Nothing)
        Catch ex As Exception

        End Try


    End Sub


    Private Sub fpLimitValue_普通_CellChanged(sender As Object, e As SheetViewEventArgs) 
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
        Me.dgLimitValue_CellValidating(sender, e)
    End Sub

    Private Sub fpLimitValue_液状化_CellChanged(sender As Object, e As SheetViewEventArgs) 
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
        Me.dgLimitValue_CellValidating(sender, e)
    End Sub

    Private Sub fpLimitValue_直接基礎_CellChanged(sender As Object, e As SheetViewEventArgs) 
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub


    ''' <summary>
    ''' dgLimitValue の入力が変わったら Me.Label2 の表示を削除する
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub dgLimitValue_CellValidating(sender As Object, e As SheetViewEventArgs)
        If e.Column = 4 Then
            Me.Label2.Text = ""
        End If
    End Sub

#End Region

#Region "Button1 クリック時の動作"

    ''' <summary>
    ''' Button1 クリック時の動作
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Try
            With OpenFileDialog1
                .FileName = ""
                .Multiselect = False
                .Filter = "SOIL-JR結果ファイル(*.sdr)|*.sdr"
            End With
            Select Case OpenFileDialog1.ShowDialog()
                Case System.Windows.Forms.DialogResult.OK
                    Using sf As New SelectFoundationDB(OpenFileDialog1.FileName)
                        Select Case sf.ShowDialog()
                            Case System.Windows.Forms.DialogResult.OK
                                '選択されたファイルの内容をDataGridView4に表示する
                                Dim 杭_普通_Sheet = Me.fpLimitValue.Sheets("普通")
                                Dim 杭_普通_Cells = 杭_普通_Sheet.Cells

                                杭_普通_Cells(0, 5).Value = sf.Lv1Rvd
                                杭_普通_Cells(1, 5).Value = sf.Lv1Rud
                                杭_普通_Cells(2, 5).Value = sf.Lv1δL
                                杭_普通_Cells(3, 5).Value = sf.Lv1θL
                                杭_普通_Cells(4, 5).Value = sf.Lv2Rvd
                                杭_普通_Cells(5, 5).Value = sf.Lv2δL
                                杭_普通_Cells(6, 5).Value = sf.Lv2θL
                                杭_普通_Cells(7, 5).Value = sf.AzenRvd
                                杭_普通_Cells(8, 5).Value = sf.AzenδL
                                杭_普通_Cells(9, 5).Value = sf.AzenθL
                                Me.Label2.Text = sf.FileName

                            Case System.Windows.Forms.DialogResult.Yes
                                '選択されたファイルの内容をDataGridView4に表示する
                                Dim 杭_液状化_Sheet = Me.fpLimitValue.Sheets("液状化")
                                Dim 杭_液状化_Cells = 杭_液状化_Sheet.Cells

                                杭_液状化_Cells(0, 5).Value = sf.Lv1Rvd
                                杭_液状化_Cells(1, 5).Value = sf.Lv1Rud
                                杭_液状化_Cells(2, 5).Value = sf.Lv1δL
                                杭_液状化_Cells(3, 5).Value = sf.Lv1θL
                                杭_液状化_Cells(4, 5).Value = sf.Lv2Rvd
                                杭_液状化_Cells(5, 5).Value = sf.Lv2δL
                                杭_液状化_Cells(6, 5).Value = sf.Lv2θL
                                Me.Label2.Text = sf.FileName
                            Case Else
                                Call MsgBox("sdrファイルの読み込みに失敗しました。")

                        End Select
                    End Using

                Case System.Windows.Forms.DialogResult.Abort
                    Call MsgBox("sdrファイルの読み込みに失敗しました。")
            End Select
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

#End Region

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="data"></param>
    ''' <returns></returns>
    Private Function GetChokuKisoAtPoint(data As String) As String

        Dim Cells_変位照査位置 = Me.fpDisgReactAtPoint_変位照査位置.Cells
        Dim cmbocell As FarPoint.Win.Spread.CellType.ComboBoxCellType = Cells_変位照査位置(0, 0).CellType

        For Each s As String In cmbocell.Items
            If s = data Then
                Return data
            End If
        Next
        Return ""
    End Function

    ''' <summary>
    ''' 直接基礎の照査位置の一覧を作成する
    ''' </summary>
    Private Sub SetChokuKisoAtPoint(targetCell As Cell)
        Try
            Dim Column As New List(Of String)

            Dim SNAPDB = Input.Data.SNAP.SnapDB

            If IsNothing(SNAPDB) = False Then
                For Each JoNo In SNAPDB.GetChokukisoJoint
                    Column.Add(JoNo)
                Next
            End If

            Dim cmbocell As New FarPoint.Win.Spread.CellType.ComboBoxCellType()
            cmbocell.Items = Column.ToArray()
            cmbocell.Editable = True '選択肢以外の入力を可能にする
            targetCell.CellType = cmbocell

        Catch ex As Exception

        End Try
    End Sub





    Private Sub dgChokuKisoAtPoint_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs)
        If e.ColumnIndex = 0 Then
            Call dgChokuKisoAtPoint_CellEnter(sender, e)
        End If
    End Sub

    Private Sub dgChokuKisoAtPoint_CellEnter(sender As Object, e As DataGridViewCellEventArgs)
        Try
            Dim dg2 As DataGridView = TryCast(sender, DataGridView)
            Dim pn = dg2(0, 0).Value
            If IsNumeric(pn) Then
                Me.CurrentPointNo = Convert.ToInt32(pn)
            Else
                Me.CurrentPointNo = 0
            End If
            Me.CurrentMemberNo = 0
            Me.CurrentPointDistance = 0
            'MyPictureBox1 の更新
            Call MyPictureBox1_Paint(Me.MyPictureBox1, Nothing)
            Call dgFoundationList_UpDate()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub dgChokuKisoAtPoint_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs)
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub

    Private Sub dgChokuKisoAtPoint_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Delete Then
            Dim dgv As DataGridView = CType(sender, DataGridView)
            Dim row As Integer = dgv.CurrentCell.OwningRow.Index
            Dim col As Integer = dgv.CurrentCell.OwningColumn.Index
            If dgv(col, row).ReadOnly = False Then
                dgv(col, row).Value = Nothing
            End If
        End If
    End Sub

End Class