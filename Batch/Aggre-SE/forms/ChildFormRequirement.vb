Imports FarPoint.Win.Spread
''' <summary>
''' 
''' </summary>
''' <remarks></remarks>
Public Class ChildFormRequirement

#Region "メンバ変数"

    ''' <summary>
    ''' シングルトン用インスタンス
    ''' </summary>
    ''' <remarks></remarks>
    Private Shared instance As ChildFormRequirement = New ChildFormRequirement()

    ''' <summary>
    ''' 初期化中かどうか
    ''' </summary>
    Private IsInitialFlg As Boolean
#End Region

#Region "プロパティ"

    ''' <summary>
    ''' インスタンス取得
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property GetInstance() As ChildFormRequirement
        Get
            '静的変数が解放されていた場合のみ、インスタンスを生成する
            If instance Is Nothing Then
                instance = New ChildFormRequirement()
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
        If FormSettings.Option_任意スペクトル = 1 Then
            rbJiban9.Visible = True
        Else
            rbJiban9.Visible = False
        End If
        Me.IsInitialFlg = True
    End Sub

#End Region

#Region "初期化処理"

    ''' <summary>
    ''' 初期処理
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataInit()
        Me.Text = "照査パラメータ"
        Me.fpEkijoCase_Sheet1.RowCount = Input.DATAROWS

        '不整形地盤の影響オプション
        If FormSettings.Option_不整形地盤 <= 0 Then
            GroupBox2.Visible = False
        End If


        Call DataLoad()
        Call MyPictureBox1_Paint(MyPictureBox1, Nothing)

        Me.IsInitialFlg = False
    End Sub

    ''' <summary>
    ''' インスタンスリフレッシュ
    ''' </summary>
    ''' <remarks></remarks>
    Public Shared Sub Reflesh()
        instance = New ChildFormRequirement()
    End Sub

#End Region

#Region "データロード"

    ''' <summary>
    ''' データロード
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataLoad()
        'グリッドの表示更新()
        Call dgEkijoCase_DataLoad()

        '着目節点の表示更新
        If Input.Data.Requirement.AtPoint = "" Then
            Try
                Dim SnapDB As CSNAPDBEx = Input.Data.SNAP.SnapDB
                If Not IsNothing(SnapDB) Then
                    Me.tbAtPoint.Text = SnapDB.Get着目点()
                End If
            Catch
            End Try
        Else
            Me.tbAtPoint.Text = Input.Data.Requirement.AtPoint
        End If

        'ラジオボタンの表示更新
        Me.rbspec2.Checked = Input.Data.Requirement.rbSpec(0)
        Me.rbspec1.Checked = Input.Data.Requirement.rbSpec(1)
        Me.rbspec3.Checked = Input.Data.Requirement.rbSpec(2)

        Me.rbChiikiA.Checked = Input.Data.Requirement.rbChiiki(0)
        Me.rbChiikiB.Checked = Input.Data.Requirement.rbChiiki(1)
        Me.rbChiikiC.Checked = Input.Data.Requirement.rbChiiki(2)

        Me.rbJiban0.Checked = Input.Data.Requirement.rbJiban(0)
        Me.rbJiban1.Checked = Input.Data.Requirement.rbJiban(1)
        Me.rbJiban2.Checked = Input.Data.Requirement.rbJiban(2)
        Me.rbJiban3.Checked = Input.Data.Requirement.rbJiban(3)
        Me.rbJiban4.Checked = Input.Data.Requirement.rbJiban(4)
        Me.rbJiban5.Checked = Input.Data.Requirement.rbJiban(5)
        Me.rbJiban9.Checked = Input.Data.Requirement.rbJiban(9)

        Me.rbEkijo5.Checked = Input.Data.Requirement.rbEkijo(0)
        Me.rbEkijo20.Checked = Input.Data.Requirement.rbEkijo(1)

        'L2地震動の設定の表示更新
        Me.cbM65Area.SelectedIndex = Input.Data.Requirement.cbM65Area
        Me.tbAlfa.Text = Input.Data.Requirement.tbAlfa

        '(5) 地盤の不整形性の影響を考慮した応答値の算定機能
        If FormSettings.Option_不整形地盤 <= 0 Then
            If Input.Data.Requirement._cbηx = True Then
                MsgBox("このデータは、オプション機能:" _
                           + vbCrLf _
                           + "不整形地盤の影響を考慮した応答値の算定" _
                           + vbCrLf _
                           + "の入力があります。" _
                           + vbCrLf _
                           + "このライセンスは、この機能を使用できません。")
            End If
            Input.Data.Requirement._cbηx = False
        Else
            Me.cbηx.Checked = Input.Data.Requirement._cbηx
            Me.tbL1ηx.Text = Input.Data.Requirement.tbηxL1
            Me.tbL2ηx.Text = Input.Data.Requirement.tbηxL2
            If Me.cbηx.Checked = False Then
                Me.tbL1ηx.Enabled = False
                Me.tbL2ηx.Enabled = False
            Else
                Me.tbL1ηx.Enabled = True
                Me.tbL2ηx.Enabled = True
            End If
        End If

        '(2) ユーザーが決めた任意の応答塑性率の直接入力機能のライセンスチェック
        If FormSettings.Option_応答塑性率の直接入力 = 1 Then
            Me.rbJiban9.Enabled = True
        Else
            Me.rbJiban9.Enabled = False
        End If


        Me.tbAtPoint.Focus()

    End Sub

    ''' <summary>
    ''' DataGridView1データロード
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub dgEkijoCase_DataLoad()

        '液状化ケースがなかったら液状化指数の入力は無効とする。
        Me.rbEkijo5.Enabled = False
        Me.rbEkijo20.Enabled = False

        Dim sh = Me.fpEkijoCase_Sheet1

        Dim RowCount = sh.RowCount

        If RowCount > Input.Data.Requirement.dgEkijoCase1.Length Then
            ReDim Preserve Input.Data.Requirement.dgEkijoCase1(RowCount)
        Else
            sh.RowCount = Input.Data.CaseName.analysisObject.Length
        End If
        If RowCount > Input.Data.Requirement.dgEkijoCase21.Length Then
            ReDim Preserve Input.Data.Requirement.dgEkijoCase21(RowCount)
        End If
        If RowCount > Input.Data.Requirement.dgEkijoCase2.Length Then
            ReDim Preserve Input.Data.Requirement.dgEkijoCase2(RowCount)
        End If


        Dim Cells = sh.Cells
        Dim rows = sh.Rows
        Dim cols = sh.Columns
        For row = 0 To sh.RowCount - 1
            Cells(row, 0).Value = Input.Data.CaseName.CaseName(row)
            Dim db = Input.Data.SNAP.SnapDB(row)
            If Not db Is Nothing AndAlso db.Get変位増分解析 = 0 Then
                rows(row).Locked = True
                Cells(row, 1).Value = 0
                Cells(row, 2).Value = 0
                Cells(row, 3).Value = 0
                rows(row).BackColor = FormSettings.ReadOnlyColor
            Else
                rows(row).Locked = False
                rows(row).BackColor = SystemColors.Window

                Dim tmpEkijoCase1 = Input.Data.Requirement.dgEkijoCase1(row)
                Dim tmpEkijoCase21 = Input.Data.Requirement.dgEkijoCase21(row)
                Dim tmpEkijoCase2 = Input.Data.Requirement.dgEkijoCase2(row)
                Cells(row, 1).Value = tmpEkijoCase1
                Cells(row, 2).Value = tmpEkijoCase21
                Cells(row, 3).Value = tmpEkijoCase2

                '液状化ケースがひとつでもあったら 液状化指数の入力を有効にする。
                If tmpEkijoCase1 <> 0 Or
                        tmpEkijoCase21 <> 0 Or
                        tmpEkijoCase2 <> 0 Then
                    Me.rbEkijo5.Enabled = True
                    Me.rbEkijo20.Enabled = True
                End If
            End If
        Next

        cols(0).BackColor = FormSettings.ReadOnlyColor

    End Sub

#End Region

#Region "データセーブ"

    ''' <summary>
    ''' データセーブ
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataSave()

        ' インプット変数に値を格納する。
        Dim Cells = Me.fpEkijoCase_Sheet1.Cells
        For row = 0 To Me.fpEkijoCase_Sheet1.RowCount - 1
            Input.Data.Requirement.dgEkijoCase1(row) = Cells(row, 1).Value
            Input.Data.Requirement.dgEkijoCase21(row) = Cells(row, 2).Value
            Input.Data.Requirement.dgEkijoCase2(row) = Cells(row, 3).Value
        Next
        Input.Data.Requirement.AtPoint = Me.tbAtPoint.Text

        Input.Data.Requirement.rbSpec(0) = Me.rbspec2.Checked
        Input.Data.Requirement.rbSpec(1) = Me.rbspec1.Checked
        Input.Data.Requirement.rbSpec(2) = Me.rbspec3.Checked

        Input.Data.Requirement.rbChiiki(0) = Me.rbChiikiA.Checked
        Input.Data.Requirement.rbChiiki(1) = Me.rbChiikiB.Checked
        Input.Data.Requirement.rbChiiki(2) = Me.rbChiikiC.Checked

        Input.Data.Requirement.rbJiban(0) = Me.rbJiban0.Checked
        Input.Data.Requirement.rbJiban(1) = Me.rbJiban1.Checked
        Input.Data.Requirement.rbJiban(2) = Me.rbJiban2.Checked
        Input.Data.Requirement.rbJiban(3) = Me.rbJiban3.Checked
        Input.Data.Requirement.rbJiban(4) = Me.rbJiban4.Checked
        Input.Data.Requirement.rbJiban(5) = Me.rbJiban5.Checked
        Input.Data.Requirement.rbJiban(9) = Me.rbJiban9.Checked

        Input.Data.Requirement.rbEkijo(0) = Me.rbEkijo5.Checked
        Input.Data.Requirement.rbEkijo(1) = Me.rbEkijo20.Checked

        'L2地震動の設定の表示更新
        Input.Data.Requirement.cbM65Area = Me.cbM65Area.SelectedIndex
        Input.Data.Requirement.tbAlfa = Me.tbAlfa.Text

        '不整形地盤の設定の表示更新
        If FormSettings.Option_不整形地盤 <= 0 Then
            Input.Data.Requirement._cbηx = False
        Else
            Input.Data.Requirement._cbηx = Me.cbηx.Checked
            Input.Data.Requirement.tbηxL1 = Me.tbL1ηx.Text
            Input.Data.Requirement.tbηxL2 = Me.tbL2ηx.Text
        End If

        '任意スペクトルモードの場合は L1地震動の照査を行わないことにする
        If Input.Data.Requirement.rbJiban(9) = True Then
            For r = 0 To Input.Data.CaseName.dgCaseList_6.Count - 1
                Input.Data.CaseName.dgCaseList_6(r) = 0
            Next
        End If
    End Sub

#End Region

#Region "イベント"

    ''' <summary>
    ''' TextBox1 KeyPressイベントハンドラ
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub TextBox1_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles tbAtPoint.KeyPress

        'リターンが押されたら DataGridView1 に フォーカスを移す
        If e.KeyChar = Microsoft.VisualBasic.ChrW(Keys.Return) Then
            e.Handled = True
            Me.fpEkijoCase.Focus()
            Return
        End If

        '押されたキーが 0～9でない場合は、イベントをキャンセルする
        If (e.KeyChar < "0"c Or e.KeyChar > "9"c) And e.KeyChar <> vbBack Then
            e.Handled = True
        End If

    End Sub

    ''' <summary>
    ''' TextBox1 TextChangeイベントハンドラ
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles tbAtPoint.TextChanged
        Me.MyPictureBox1_Paint(sender, Nothing)
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub

    ''' <summary>
    ''' MyPicutreBox1イベントハンドラ
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub MyPictureBox1_Paint(sender As Object, e As PaintEventArgs) Handles MyPictureBox1.Paint
        Try
            Dim SnapDB As CSNAPDBEx = Input.Data.SNAP.SnapDB
            If Not SnapDB Is Nothing Then
                Me.Label1.Text = "※1から" & SnapDB.GetJointCount & "までの範囲で入力"
                Try
                    Me.MyPictureBox1.PointX.Clear()
                    Me.MyPictureBox1.PointY.Clear()
                    For i = 0 To SnapDB.GetJointCount
                        Dim p = SnapDB.GetJoint(i)
                        Me.MyPictureBox1.PointX.Add(i, p.X)
                        Me.MyPictureBox1.PointY.Add(i, p.Y)
                    Next
                    '入力した節点番号を表示する
                    Dim No As Integer = IIf(IsNumeric(Me.tbAtPoint.Text), Convert.ToInt32(Me.tbAtPoint.Text), 0)
                    Me.MyPictureBox1.PrintPointNo.Clear()
                    Me.MyPictureBox1.PrintPointNo.Add(No, No.ToString())

                Catch ex As Exception
                    Me.MyPictureBox1.PrintPointNo.Clear()
                End Try
                Try
                    Me.MyPictureBox1.MemberNum = SnapDB.GetMemberCount
                    ReDim Me.MyPictureBox1.MemberNo(Me.MyPictureBox1.MemberNum + 1)
                    ReDim Me.MyPictureBox1.PrintMemberNo(Me.MyPictureBox1.MemberNum + 1)
                    ReDim Me.MyPictureBox1.IPointNo(Me.MyPictureBox1.MemberNum + 1)
                    ReDim Me.MyPictureBox1.JPointNo(Me.MyPictureBox1.MemberNum + 1)
                    For i = 1 To Me.MyPictureBox1.MemberNum
                        Me.MyPictureBox1.MemberNo(i) = i
                        Me.MyPictureBox1.PrintMemberNo(i) = ""
                        Me.MyPictureBox1.IPointNo(i) = SnapDB.GetMember(i).Itan
                        Me.MyPictureBox1.JPointNo(i) = SnapDB.GetMember(i).Jtan
                    Next
                    Dim No As Integer = IIf(IsNumeric(Me.tbAtPoint.Text), Convert.ToInt32(Me.tbAtPoint.Text), 0)
                    If No > SnapDB.GetMemberCount Then
                        Me.Label1.Text = "※範囲外の値"
                    End If
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


    ''' <summary>
    ''' fpEkijoCase 変更時イベントハンドラ
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub fpEkijoCase_ButtonClicked(sender As Object, e As EditorNotifyEventArgs) Handles fpEkijoCase.ButtonClicked
        fpEkijoCase_Sheet1_CellChanged(fpEkijoCase_Sheet1, e)
    End Sub
    Private Sub fpEkijoCase_Sheet1_CellChanged(sender As Object, e As Object) Handles fpEkijoCase_Sheet1.CellChanged

        '初期化時は動作しない
        If Me.IsInitialFlg = True Then
            Return
        End If

        If e.Column < 1 Then
            Return
        End If

        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False

        Dim sh As SheetView = sender
        Dim Cells = sh.Cells

        Me.rbEkijo5.Enabled = False
        Me.rbEkijo20.Enabled = False

        '液状化ケースがひとつでもあったら 液状化指数の入力を有効にする。
        For row = 0 To sh.RowCount - 1
            Dim tmpEkijoCase1 = Cells(row, 1).Value
            Dim tmpEkijoCase2 = Cells(row, 2).Value
            Dim tmpEkijoCase21 = Cells(row, 3).Value
            If tmpEkijoCase1 <> 0 Or
                    tmpEkijoCase21 <> 0 Or
                    tmpEkijoCase2 <> 0 Then
                Me.rbEkijo5.Enabled = True
                Me.rbEkijo20.Enabled = True
                Exit For
            End If
        Next

    End Sub

    ''' <summary>
    ''' セルを編集する前に発生するイベント
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub fpEkijoCase_EditModeOn(sender As Object, e As EventArgs) Handles fpEkijoCase.EditModeOn

        Dim fp As FpSpread = sender
        Dim sh As SheetView = fp.ActiveSheet
        Dim Cells = sh.ActiveCell

        If Cells.Column.Index < 1 Then
            fp.StopCellEditing()
        End If

    End Sub

    Private Sub rbSpec_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rbspec1.CheckedChanged, rbspec2.CheckedChanged, rbspec3.CheckedChanged
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub

    Private Sub rbJiban_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rbJiban0.CheckedChanged, rbJiban1.CheckedChanged, rbJiban2.CheckedChanged, rbJiban3.CheckedChanged, rbJiban4.CheckedChanged, rbJiban5.CheckedChanged

        Dim rb As RadioButton = CType(sender, RadioButton)
        If rb.Checked = True Then
            rbJiban9.Checked = False
        End If

        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub
    Private Sub rbJiban9_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rbJiban9.CheckedChanged

        If rbJiban9.Checked = False Then
            GroupBox4.Enabled = True '地域別係数
            GroupBox1.Enabled = True 'L2地震動の設定
            GroupBox3.Enabled = True 'スペクトル
        Else
            GroupBox4.Enabled = False '地域別係数
            GroupBox1.Enabled = False 'L2地震動の設定
            GroupBox3.Enabled = False 'スペクトル
            rbJiban0.Checked = False
            rbJiban1.Checked = False
            rbJiban2.Checked = False
            rbJiban3.Checked = False
            rbJiban4.Checked = False
            rbJiban5.Checked = False
        End If
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub

    Private Sub rbChiiki_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rbChiikiA.CheckedChanged, rbChiikiB.CheckedChanged, rbChiikiC.CheckedChanged
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub

    Private Sub rbEkijo_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rbEkijo5.CheckedChanged, rbEkijo20.CheckedChanged
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub

    Private Sub cbM65Area_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbM65Area.TextChanged
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub

    Private Sub cbM65Area_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbM65Area.SelectedIndexChanged
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub

    Private Sub tbAlfa_KeyPress(sender As Object, e As KeyPressEventArgs) Handles tbAlfa.KeyPress
        'リターンが押されたら DataGridView1 に フォーカスを移す
        If e.KeyChar = Microsoft.VisualBasic.ChrW(Keys.Return) Then
            e.Handled = True
            Me.GetNextControl(tbAlfa, True).Focus()
            Return
        End If

        '押されたキーが 0～9でない場合は、イベントをキャンセルする
        If (e.KeyChar < "0"c Or e.KeyChar > "9"c) And e.KeyChar <> "."c And e.KeyChar <> vbBack Then
            e.Handled = True
        End If
    End Sub

    Private Sub tbAlfa_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tbAlfa.TextChanged
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub





    Private Sub cbηx_CheckedChanged(sender As Object, e As EventArgs) Handles cbηx.CheckedChanged

        If Me.cbηx.Checked = False Then
            Me.tbL1ηx.Enabled = False
            Me.tbL2ηx.Enabled = False
        Else
            Me.tbL1ηx.Enabled = True
            Me.tbL2ηx.Enabled = True
        End If

        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False

    End Sub

    Private Sub tbL1ηx_KeyPress(sender As Object, e As KeyPressEventArgs) Handles tbL1ηx.KeyPress
        'リターンが押されたら DataGridView1 に フォーカスを移す
        If e.KeyChar = Microsoft.VisualBasic.ChrW(Keys.Return) Then
            e.Handled = True
            Me.GetNextControl(tbL1ηx, True).Focus()
            Return
        End If

        '押されたキーが 0～9でない場合は、イベントをキャンセルする
        If (e.KeyChar < "0"c Or e.KeyChar > "9"c) And e.KeyChar <> "."c And e.KeyChar <> vbBack Then
            e.Handled = True
        End If
    End Sub

    Private Sub tbL1ηx_TextChanged(sender As Object, e As EventArgs) Handles tbL1ηx.TextChanged
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub

    Private Sub tbL2ηx_KeyPress(sender As Object, e As KeyPressEventArgs) Handles tbL2ηx.KeyPress
        'リターンが押されたら DataGridView1 に フォーカスを移す
        If e.KeyChar = Microsoft.VisualBasic.ChrW(Keys.Return) Then
            e.Handled = True
            Me.tbAtPoint.Focus()
            Return
        End If

        '押されたキーが 0～9でない場合は、イベントをキャンセルする
        If (e.KeyChar < "0"c Or e.KeyChar > "9"c) And e.KeyChar <> "."c And e.KeyChar <> vbBack Then
            e.Handled = True
        End If
    End Sub

    Private Sub tbL2ηx_TextChanged(sender As Object, e As EventArgs) Handles tbL2ηx.TextChanged
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub


















#End Region

End Class