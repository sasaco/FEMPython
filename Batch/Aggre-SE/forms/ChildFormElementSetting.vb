Imports System.Security.Cryptography
Imports FarPoint.Win.Spread
Imports SNAPDBLib.CSNAPDB
''' <summary>
''' 
''' </summary>
''' <remarks></remarks>
Public Class ChildFormElementSetting

#Region "メンバ変数"

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <remarks></remarks>
    Private _ActiveDLNo As Integer
    Private Property ActiveDLNo As Integer
        Get
            If _ActiveDLNo < 1 Then
                Return 1
            Else
                Return _ActiveDLNo
            End If
        End Get
        Set(value As Integer)
            _ActiveDLNo = value
        End Set
    End Property

    ''' <summary>
    ''' シングルトン用インスタンス
    ''' </summary>
    ''' <remarks></remarks>
    Private Shared instance As ChildFormElementSetting = New ChildFormElementSetting()

#End Region

#Region "プロパティ"

    ''' <summary>
    ''' インスタンス取得
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property GetInstance() As ChildFormElementSetting
        Get
            '静的変数が解放されていた場合のみ、インスタンスを生成する
            If instance Is Nothing Then
                instance = New ChildFormElementSetting()
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
    End Sub

#End Region

#Region "初期化処理"

    ''' <summary>
    ''' 初期処理
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataInit()
        Me.Text = "断面"


        'fpLimitLevelの設定 ******************************************************************************
        Dim cols = Me.fpLimitLevel_Sheet1.Columns()
        cols(0).BackColor = FormSettings.ReadOnlyColor
        cols(1).BackColor = FormSettings.ReadOnlyColor
        cols(0).Locked = True
        cols(1).Locked = True

        Call DataLoad()

        Call MyPictureBox1_Paint(Me.MyPictureBox1, Nothing)
    End Sub

    ''' <summary>
    ''' インスタンスリフレッシュ
    ''' </summary>
    ''' <remarks></remarks>
    Public Shared Sub Reflesh()
        instance = New ChildFormElementSetting()
    End Sub

#End Region

#Region "データロード"

    ''' <summary>
    ''' データロード
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataLoad()
        'fpLimitLevel_typeの設定
        Call dgLimitLevel_type_Load()
        'fpLimitLevelの設定
        Call dgLimitLevel_Load()

    End Sub

    ''' <summary>
    ''' DataGridView1のデータロード
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub dgLimitLevel_type_Load()

        Dim sh = Me.fpLimitLevel_type_Sheet1

        Dim cols = sh.Columns()

        'カスタムせん断耐力のオプション設定 
        '連携バージョンチェック機能
        If Input.Data.Element.versionCheck Then
            sh.ColumnCount = 8
            cols(0).Width = 203
            cols(1).Visible = True
            Me.Label4.Visible = False
            Me.Label5.Text = "②JR東日本【RCマニュアル7.2.3.2】"
            Me.Label7.Text = "③整備新幹線【設計内規】　梁部材"
            Me.Label8.Text = "④整備新幹線【設計内規】　柱部材"
        Else
            cols(1).Visible = False
        End If

        Dim d As CSNAPDBEx = Input.Data.SNAP.SnapDB
        If Not d Is Nothing Then
            Try
                Dim MFCount As Integer = d.GetMFCount
                Dim DLCount As Integer = d.GetDLCount

                Dim RowCount = MFCount + DLCount
                Dim ColumnCount = sh.ColumnCount
                Dim Cells = sh.Cells

                sh.RowCount = RowCount
                Input.Data.Element.dgLimitLevel_typeCount = RowCount

                Dim iDL As Integer = 1
                For i = 0 To RowCount - 1

                    If i < MFCount Then
                        'タイトルの入力
                        Cells(i, 0).Value = "MFデータ"

                        '端部支持条件
                        Cells(i, 1).Value = Nothing

                        '部材照査タイプの入力
                        Integer.TryParse(Input.Data.Element.dgLimitLevel_type(i), Cells(i, 2).Value)
                        If Cells(i, 2).Value = 0 Then Cells(i, 2).Value = Nothing

                        ' せん断耐力式 ➀～ の背景を読み取り専用にする
                        For c = 4 To ColumnCount - 1
                            Cells(i, c).Value = Nothing
                            Cells(i, c).Locked = True
                            Cells(i, c).BackColor = FormSettings.ReadOnlyColor
                        Next
                    Else
                        With d.DLInfo(iDL)
                            'タイトルの入力
                            Cells(i, 0).Value = .Title.Trim()

                            '端部支持条件
                            If Input.Data.Element.dgLimitLevel_SIJI(i) Is Nothing Then
                                Input.Data.Element.dgLimitLevel_SIJI(i) = .SIJI.ToString()
                            End If
                            Cells(i, 1).Value = Input.Data.Element.dgLimitLevel_SIJI(i)

                            '部材照査タイプの入力
                            Integer.TryParse(Input.Data.Element.dgLimitLevel_type(i), Cells(i, 2).Value)
                            If Cells(i, 2).Value = 0 Then Cells(i, 2).Value = Nothing

                            'せん断耐力式の入力
                            Try
                                Dim VydType As Integer = Input.Data.Element.dgLimitLevel_VydType(i)

                                If RC断面耐力.isPossible(d.DLInfo(iDL)) Then
                                    ' カスタムRC断面耐力の計算が可能ば場合

                                    ' せん断耐力式 ➀～の入力情報
                                    For c = 4 To ColumnCount - 1
                                        Cells(i, c).Value = IIf(VydType = c - 3, True, Nothing)
                                    Next

                                    'せん断耐力式のオプション機能のライセンスチェック
                                    If CInt(Strings.Right(FormSettings.Option_カスタムせん断耐力.ToString("00"), 1)) > 0 Then
                                        'JR東日本【RCマニュアル】ライセンスを持っている
                                        Dim c = ColumnCount - 3
                                        Cells(i, c).Locked = False
                                        Cells(i, c).BackColor = System.Drawing.SystemColors.Window
                                        Cells(i, c).Note = ""
                                    Else
                                        Dim c = ColumnCount - 3
                                        Cells(i, c).Locked = True
                                        Cells(i, c).BackColor = FormSettings.ReadOnlyColor
                                        Cells(i, c).Note = "ライセンスをご購入ください"
                                        If Cells(i, c).Value = True Then
                                            Cells(i, c).Value = Nothing
                                            Cells(i, 4).Value = True
                                        End If
                                        Label5.Enabled = False
                                    End If
                                    If CInt(Strings.Left(FormSettings.Option_カスタムせん断耐力.ToString("00"), 1)) > 0 Then
                                        '整備新幹線【設計内規】ライセンスを持っている
                                        For c = ColumnCount - 2 To ColumnCount - 1
                                            Cells(i, c).Locked = False
                                            Cells(i, c).BackColor = System.Drawing.SystemColors.Window
                                            Cells(i, c).Note = ""
                                        Next
                                    Else
                                        For c = ColumnCount - 2 To ColumnCount - 1
                                            Cells(i, c).Locked = True
                                            Cells(i, c).BackColor = FormSettings.ReadOnlyColor
                                            Cells(i, c).Note = "ライセンスをご購入ください"
                                            If Cells(i, c).Value = True Then
                                                Cells(i, c).Value = Nothing
                                                Cells(i, 4).Value = True
                                            End If
                                        Next
                                        Label7.Enabled = False
                                        Label8.Enabled = False
                                    End If
                                    If FormSettings.Option_カスタムせん断耐力 = 0 Then
                                        Me.GroupBox4.Enabled = False
                                    End If



                                    Dim La As Single = .L
                                    'If Input.Data.Element.dgLimitLevel_SIJI(i) = "両端固定" Then
                                    '    La = .L2
                                    'End If
                                    If La > 0 Then
                                        Cells(i, 1).BackColor = FormSettings.ReadOnlyColor
                                        Cells(i, 1).Locked = True
                                        Cells(i, 1).CellType = New CellType.TextCellType()
                                        Cells(i, 3).Value = La
                                        Cells(i, 3).Locked = True
                                        Cells(i, 3).BackColor = FormSettings.ReadOnlyColor
                                        Cells(i, 3).Note = "M-θ部材は、せん断耐力の算定におけるせん断スパンは変更できません。"
                                    Else
                                        Cells(i, 3).Locked = False
                                        Single.TryParse(Input.Data.Element.dgLimitLevel_La(i), Cells(i, 3).Value)
                                        If Cells(i, 3).Value = 0 Then Cells(i, 3).Value = Nothing

                                        Cells(i, 3).BackColor = System.Drawing.SystemColors.Window
                                    End If

                                Else
                                    '上記以外の断面形状は、特殊せん断耐力には対応していない
                                    Dim toolTipStr As String = String.Format("断面形状番号{0}は、せん断耐力の算定式を選択することはできません。", .iType)
                                    For c = 5 To ColumnCount - 1
                                        Cells(i, c).Note = toolTipStr
                                    Next

                                    Dim La As Single = .L
                                    'If Input.Data.Element.dgLimitLevel_SIJI(i) = "両端固定" Then
                                    '    La = .L2
                                    'End If
                                    If IsNumeric(La) And La <> 0 Then
                                        Cells(i, 3).Value = La
                                    Else
                                        Cells(i, 3).Value = ""
                                    End If

                                    Cells(i, 1).Locked = True
                                    Cells(i, 1).BackColor = FormSettings.ReadOnlyColor
                                    Cells(i, 1).CellType = New CellType.TextCellType()
                                    Cells(i, 3).Locked = True
                                    Cells(i, 3).BackColor = FormSettings.ReadOnlyColor
                                    Cells(i, 4).Value = True
                                    Cells(i, 4).Locked = True
                                    Cells(i, 4).BackColor = FormSettings.ReadOnlyColor
                                    For c = 5 To ColumnCount - 1
                                        Cells(i, c).Value = Nothing
                                        Cells(i, c).Locked = True
                                        Cells(i, c).BackColor = FormSettings.ReadOnlyColor
                                        Cells(i, c).Note = ""
                                    Next
                                End If
                            Catch ex As Exception
                            End Try
                        End With
                        iDL += 1
                    End If
                Next

            Catch ex As Exception
            End Try

        Else
            sh.RowCount = 0
        End If
        ActiveDLNo = 1
    End Sub

    Public Class cmbItem
        Property Text() As String
        Property DisplayText() As String
    End Class
    ''' <summary>
    ''' fpLimitLevelのデータロード
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub dgLimitLevel_Load()

        Dim sh = Me.fpLimitLevel_Sheet1

        Dim RowCount = sh.RowCount
        Dim Cells = sh.Cells

        For row = 0 To RowCount - 1
            Cells(row, 2).Value = Input.Data.Element.dgLimitLevel_title(row)

            Integer.TryParse(Input.Data.Element.dgLimitLevel1(row), Cells(row, 3).Value)
            If Cells(row, 3).Value = 0 Then Cells(row, 3).Value = Nothing

            Integer.TryParse(Input.Data.Element.dgLimitLevel2(row), Cells(row, 4).Value)
            If Cells(row, 4).Value = 0 Then Cells(row, 4).Value = Nothing
        Next
    End Sub

#End Region

#Region "データセーブ"

    ''' <summary>
    ''' データセーブ
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataSave()

        ' fpLimitLevel_type をインプット変数に値を格納する。*******************
        Dim RowCount = fpLimitLevel_type_Sheet1.RowCount
        Dim Cells1 = fpLimitLevel_type_Sheet1.Cells

        ' 部材照査タイプとせん断スパン
        For row = 0 To RowCount - 1
            Input.Data.Element.dgLimitLevel_type(row) = Cells1(row, 2).Value
            Input.Data.Element.dgLimitLevel_La(row) = Cells1(row, 3).Value
            Input.Data.Element.dgLimitLevel_SIJI(row) = Cells1(row, 1).Value
        Next

        'せん断耐力式の入力
        Try
            For r = 0 To RowCount - 1
                Dim id As Integer = Input.Data.Element.dgLimitLevel_VydType(r)
                If FormSettings.Option_カスタムせん断耐力 <= 0 Then
                    Input.Data.Element.dgLimitLevel_VydType(r) = 1
                Else
                    For c = 4 To fpLimitLevel_type_Sheet1.ColumnCount - 1
                        If Cells1(r, c).Value = True Then
                            Input.Data.Element.dgLimitLevel_VydType(r) = c - 3
                            Exit For
                        End If
                    Next
                End If
            Next
        Catch ex As Exception
        End Try

        ' fpLimitLevel をインプット変数に値を格納する。*******************
        Dim Cells2 = Me.fpLimitLevel_Sheet1.Cells
        For row = 0 To Me.fpLimitLevel_Sheet1.RowCount - 1
            Input.Data.Element.dgLimitLevel_title(row) = Cells2(row, 2).Value
            Input.Data.Element.dgLimitLevel1(row) = Cells2(row, 3).Value
            Input.Data.Element.dgLimitLevel2(row) = Cells2(row, 4).Value
        Next
    End Sub

#End Region

#Region "イベント"

    ''' <summary>
    ''' 部材照査タイプに変更があった場合
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub fpLimitLevel_CellChanged(sender As Object, e As FarPoint.Win.Spread.SheetViewEventArgs)
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub

    ''' <summary>
    ''' 照査タイプの編集前のイベント
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub fpLimitLevel_EditModeOn(sender As Object, e As EventArgs) Handles fpLimitLevel.EditModeOn
        Dim fp As FpSpread = sender
        Dim sh As SheetView = fp.ActiveSheet
        Dim Cell = sh.ActiveCell
        Dim col = Cell.Column.Index
        If col < 2 Then
            fp.StopCellEditing()
        End If

    End Sub



    ''' <summary>
    ''' faLimitLevel_type をクリックした時に発生するイベント
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub fpLimitLevel_type_ButtonClicked(sender As Object, e As EditorNotifyEventArgs) Handles fpLimitLevel_type.ButtonClicked

        If e.Column < 4 Then
            Return
        End If

        Dim fp As FpSpread = sender
        Dim sh As SheetView = fp.ActiveSheet
        Dim Cells = sh.Cells
        Try
            'せん断耐力式のオプション機能のライセンスチェック
            If Cells(e.Row, e.Column).BackColor = FormSettings.ReadOnlyColor Then
                ' クリックしたセルが読み取り専用だったら
                'チェックをOFF にする
                For c As Integer = 4 To sh.ColumnCount - 1
                    Cells(e.Row, c).Value = Nothing
                Next
            Else
                '対象セルを 以外を OFF にする
                For c As Integer = 4 To sh.ColumnCount - 1
                    If c <> e.Column Then
                        Cells(e.Row, c).Value = Nothing
                    End If
                Next
            End If
        Catch ex As Exception

        Finally
            Input.ChengeDataFlg = True
            Input.ReadDBFlg = False

        End Try

    End Sub

    Private Sub dgLimitLevel_type_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs)
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
    End Sub


    ''' <summary>
    ''' SelectionChangedイベントハンドラ
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub fpLimitLevel_type_SlectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles fpLimitLevel_type.SelectionChanged

        Dim fp As FpSpread = sender
        Dim sh As SheetView = fp.ActiveSheet
        Dim Cells = sh.Cells

        ActiveDLNo = e.Range.Row + 1
        Call MyPictureBox1_Paint(Me.MyPictureBox1, Nothing)
    End Sub



    ''' <summary>
    ''' Paintイベントハンドラ
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
                    Me.MyPictureBox1.PrintPointNo.Clear()
                Catch ex As Exception
                End Try
                Try
                    Me.MyPictureBox1.MemberNum = SnapDB.GetMemberCount
                    ReDim Me.MyPictureBox1.MemberNo(Me.MyPictureBox1.MemberNum + 1)
                    ReDim Me.MyPictureBox1.PrintMemberNo(Me.MyPictureBox1.MemberNum + 1)
                    ReDim Me.MyPictureBox1.IPointNo(Me.MyPictureBox1.MemberNum + 1)
                    ReDim Me.MyPictureBox1.JPointNo(Me.MyPictureBox1.MemberNum + 1)

                    Dim MFCount As Integer = SnapDB.GetMFCount

                    For i = 1 To Me.MyPictureBox1.MemberNum
                        Me.MyPictureBox1.MemberNo(i) = i
                        '選択された断面の要素番号を表示する
                        Try
                            Me.MyPictureBox1.PrintMemberNo(i) = ""
                            If ActiveDLNo > 0 Then
                                If ActiveDLNo > MFCount Then
                                    If SnapDB.GetDLNo(i) = ActiveDLNo - MFCount Then
                                        Me.MyPictureBox1.PrintMemberNo(i) = i.ToString()
                                    End If
                                Else
                                    If SnapDB.GetMFNo(i) = ActiveDLNo Then
                                        Me.MyPictureBox1.PrintMemberNo(i) = i.ToString()
                                    End If
                                End If
                            End If
                        Catch
                        End Try
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




End Class