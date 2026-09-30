Imports System
Imports System.Threading
Imports System.ComponentModel
Imports System.Windows.Forms
Imports System.Windows
Imports FarPoint.Win.Spread

Public Class ChildFormCaseName

#Region "メンバ変数"

    ''' <summary>
    ''' シングルトン用インスタンス
    ''' </summary>
    ''' <remarks></remarks>
    Private Shared instance As ChildFormCaseName = New ChildFormCaseName()

    ''' <summary>
    ''' 
    ''' </summary>
    Private ChengeDataFlg As Boolean

#End Region

#Region "プロパティ"

    ''' <summary>
    ''' インスタンス取得
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property GetInstance() As ChildFormCaseName
        Get
            '静的変数が解放されていた場合のみ、インスタンスを生成する
            If instance Is Nothing Then
                instance = New ChildFormCaseName()
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
        Me.ChengeDataFlg = False
    End Sub

#End Region

#Region "初期化処理"

    ''' <summary>
    ''' 初期処理
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataInit()
        Me.Text = "解析ケース"
        Me.WindowState = FormWindowState.Maximized

        Call DataLoad()

    End Sub

    ''' <summary>
    ''' インスタンスリフレッシュ
    ''' </summary>
    ''' <remarks></remarks>
    Public Shared Sub Reflesh()
        instance = New ChildFormCaseName()
    End Sub

#End Region

#Region "データロード"


    ''' <summary>
    ''' Label1 データロード
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub Label1_DataLoad()

        Try
            ' DataGridView1_CaseNamePath の値がすべて同じだったら 作業フォルダをLabel1 に表示する。
            Dim key As String = ""
            Dim c As Integer
            For i = 0 To Input.DATAROWS - 1
                Dim strFilePath As String = Input.Data.CaseName.CaseNamePath(i)
                If Not strFilePath Is Nothing Then
                    ' 空白行は対象除外する。
                    strFilePath = strFilePath.Trim()
                    If strFilePath <> "" Then
                        strFilePath = System.IO.Path.GetDirectoryName(strFilePath)
                        c = c + 1
                        If c = 1 Then
                            ' 最初の有効データを基準とする。
                            key = strFilePath
                        Else
                            If key <> strFilePath Then
                                ' 違う作業フォルダのデータがある場合には、作業フォルダの表示はしない。
                                lblSetDataFolder.Text = ""
                                Return
                            End If
                        End If
                    End If
                End If
            Next
            ' 全てのデータが同じだったら 作業フォルダとして表示
            lblSetDataFolder.BeginInvoke(
                Sub()
                    lblSetDataFolder.Text = key
                End Sub
            )
        Catch ex As Exception
            lblSetDataFolder.BeginInvoke(
                Sub()
                    lblSetDataFolder.Text = ""
                End Sub
            )
        End Try
    End Sub


    ''' <summary>
    ''' dgCaseList データロード
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub dgCaseList_DataLoad(Optional CaseNameOnly As Boolean = False)

        Dim sh = Me.fpCaseList_Sheet1

        If Input.DATAROWS > Input.Data.CaseName.analysisObject.Length Then
            ReDim Preserve Input.Data.CaseName.analysisObject(0 To Input.DATAROWS - 1)
        End If
        If Input.DATAROWS > Input.Data.CaseName.dgCaseList_1.Length Then
            ReDim Preserve Input.Data.CaseName.dgCaseList_1(0 To Input.DATAROWS - 1)
        End If
        If Input.DATAROWS > Input.Data.CaseName.dgCaseList_2.Length Then
            ReDim Preserve Input.Data.CaseName.dgCaseList_2(0 To Input.DATAROWS - 1)
        End If
        If Input.DATAROWS > Input.Data.CaseName.dgCaseList_3.Length Then
            ReDim Preserve Input.Data.CaseName.dgCaseList_3(0 To Input.DATAROWS - 1)
        End If
        If Input.DATAROWS > Input.Data.CaseName.dgCaseList_4.Length Then
            ReDim Preserve Input.Data.CaseName.dgCaseList_4(0 To Input.DATAROWS - 1)
        End If
        If Input.DATAROWS > Input.Data.CaseName.dgCaseList_5.Length Then
            ReDim Preserve Input.Data.CaseName.dgCaseList_5(0 To Input.DATAROWS - 1)
        End If
        If Input.DATAROWS > Input.Data.CaseName.dgCaseList_6.Length Then
            ReDim Preserve Input.Data.CaseName.dgCaseList_6(0 To Input.DATAROWS - 1)
        End If
        If Input.DATAROWS > Input.Data.CaseName.dgCaseList_7.Length Then
            ReDim Preserve Input.Data.CaseName.dgCaseList_7(0 To Input.DATAROWS - 1)
        End If
        If Input.DATAROWS > Input.Data.CaseName.dgCaseList_8.Length Then
            ReDim Preserve Input.Data.CaseName.dgCaseList_8(0 To Input.DATAROWS - 1)
        End If
        If Input.DATAROWS > Input.Data.CaseName.dgCaseList_9.Length Then
            ReDim Preserve Input.Data.CaseName.dgCaseList_9(0 To Input.DATAROWS - 1)
        End If
        sh.RowCount = Input.Data.CaseName.analysisObject.Length

        ' グリッドの表示更新()
        Dim Cells = sh.Cells
        For row = 0 To sh.RowCount - 1
            With Input.Data.CaseName
                Cells(row, 1).Value = .CaseName(row)
                Cells(row, 2).Value = .αf(row)
                Cells(row, 3).Value = .ρm(row)
                If CaseNameOnly = False Then
                    Cells(row, 4).Value = .analysisObject(row)
                    Cells(row, 5).Value = .dgCaseList_1(row)
                    Cells(row, 6).Value = .dgCaseList_2(row)
                    Cells(row, 7).Value = .dgCaseList_3(row)
                    Cells(row, 8).Value = .dgCaseList_4(row)
                    Cells(row, 9).Value = .dgCaseList_5(row)
                    Cells(row, 10).Value = .dgCaseList_6(row)
                    Cells(row, 11).Value = .dgCaseList_7(row)
                    Cells(row, 12).Value = .dgCaseList_8(row)
                    Cells(row, 13).Value = .dgCaseList_9(row)
                End If
            End With
        Next
        '任意スペクトルモードの場合は L1地震動の照査を行わないことにする
        Dim cols = sh.Columns()
        If Input.Data.Requirement.rbJiban(9) = True Then
            For row = 0 To sh.RowCount - 1
                Cells(row, 10).Note = "応答塑性率を直接入力する場合はL1地震動の照査はできません"
            Next
            cols(10).Locked = True
            cols(10).BackColor = FormSettings.ReadOnlyColor

        Else
            For row = 0 To sh.RowCount - 1
                Cells(row, 10).Note = Nothing
            Next
            cols(10).Locked = False
            cols(10).BackColor = cols(9).BackColor
        End If

        'バージョン
        If IsNothing(Input.Data.Version) Then
            Input.Data.Version = System.Diagnostics.FileVersionInfo.GetVersionInfo(
                                 System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion
        End If

    End Sub


    ''' <summary>
    ''' データロードDoWorkイベントハンドラ
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataLoad()

        For i = 0 To Input.DATAROWS - 1

            Dim FilePath As String = Input.Data.CaseName.CaseNamePath(i)
            Dim Cells = Me.fpCaseList_Sheet1.Cells

            If FilePath <> "" Then
                If Input.Data.SNAP.SnapDB(i) Is Nothing Then
                    If Input.Data.SNAP.SetSnapDB(i, FilePath) = False Then
                        MsgBox(FilePath + vbLf + "読み込みに失敗しました。")
                        Input.Data.CaseName.CaseNamePath(i) = ""
                        Cells(i, 1).Value = ""
                    End If
                End If
            Else
                Input.Data.SNAP.DeltSnapDB(i)
            End If
        Next

        Try
            'グリッドの表示更新()
            Call dgCaseList_DataLoad()
            'ラベルの表示更新
            Call Label1_DataLoad()
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

#End Region

#Region "データセーブ"

    ''' <summary>
    ''' データセーブ
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataSave()

        If Me.ChengeDataFlg = True Then
            Call DataCheck()
        End If

        ' インプット変数に値を格納する。
        Dim Cells = Me.fpCaseList_Sheet1.Cells

        For row = 0 To Me.fpCaseList_Sheet1.RowCount - 1
            '切り取りクリックなどで, データベースが読み込まれているのに表示が無いケースはデータベースをクリアする。
            If Input.Data.CaseName.CaseNamePath(row) <> "" And Cells(row, 1).Value = "" Then
                Input.Data.CaseName.CaseNamePath(row) = ""
                Input.Data.SNAP.DeltSnapDB(row)
            End If
            Input.Data.CaseName.analysisObject(row) = Cells(row, 4).Value
            Input.Data.CaseName.dgCaseList_1(row) = Cells(row, 5).Value
            Input.Data.CaseName.dgCaseList_2(row) = Cells(row, 6).Value
            Input.Data.CaseName.dgCaseList_3(row) = Cells(row, 7).Value
            Input.Data.CaseName.dgCaseList_4(row) = Cells(row, 8).Value
            Input.Data.CaseName.dgCaseList_5(row) = Cells(row, 9).Value
            Input.Data.CaseName.dgCaseList_6(row) = Cells(row, 10).Value
            Input.Data.CaseName.dgCaseList_7(row) = Cells(row, 11).Value
            Input.Data.CaseName.dgCaseList_8(row) = Cells(row, 12).Value
            Input.Data.CaseName.dgCaseList_9(row) = Cells(row, 13).Value
        Next


        Me.ChengeDataFlg = False
    End Sub

#End Region

#Region "イベント"

    ''' <summary>
    ''' Cellボタンクリック イベントハンドラ
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub faCaseList_ButtonClicked(sender As Object, e As FarPoint.Win.Spread.EditorNotifyEventArgs) Handles fpCaseList.ButtonClicked

        If e.Column <> 0 Then
            fpCaseList_Sheet1_CellChanged(sender, Nothing)
            Return
        End If
        Try
            With OpenFileDialog1
                .FileName = ""
                .Multiselect = False
                .Filter = "解析結果ファイル(*.DB1)|*.DB1"
            End With
            If OpenFileDialog1.ShowDialog() = Forms.DialogResult.OK Then
                Call SetData_DoWork(e.Row)
            End If
        Catch ex As Exception
            Throw ex
        End Try

    End Sub

    ''' <summary>
    ''' ファイル名, αf, ρm の列の編集をブロックする
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub dgCaseList_EditModeOn(sender As Object, e As EventArgs) Handles fpCaseList.EditModeOn

        Dim fp As FpSpread = sender
        Dim targetCell As Cell = fp.ActiveSheet.ActiveCell
        Dim Column = targetCell.Column.Index
        If 0 < Column And Column < 4 Then
            fp.StopCellEditing()
            Return
        End If
    End Sub

    ''' <summary>
    ''' ファイル名を消すとデータベースから消去する
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub faCaseList_KeyDown(sender As Object, e As KeyEventArgs) Handles fpCaseList.KeyDown
        Try
            If Not (e.KeyCode = Keys.Delete OrElse e.KeyCode = Keys.Back) Then
                Return
            End If

            Dim sh As SheetView = sender.ActiveSheet
            Dim Cells As Cells = sh.Cells
            Dim targetCell As Cell = sh.ActiveCell
            Dim Column = targetCell.Column.Index
            Dim Row = targetCell.Row.Index

            If 0 < Column And Column < 4 Then
                Cells(Row, 2).Value = "" 'αf
                Cells(Row, 3).Value = "" 'ρm
                Input.Data.CaseName.CaseNamePath(Row) = ""
                Input.Data.SNAP.DeltSnapDB(Row)
            End If
        Catch ex As Exception

        End Try
    End Sub

    ''' <summary>
    ''' セルの情報を変えたら
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub fpCaseList_Sheet1_CellChanged(sender As Object, e As SheetViewEventArgs) Handles fpCaseList_Sheet1.CellChanged
        Input.ChengeDataFlg = True
        Input.ReadDBFlg = False
        Me.ChengeDataFlg = True
    End Sub

    ''' <summary>
    ''' 作業フォルダの設定ボタン クリックイベント
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub btnSetDataFolder_Click(sender As Object, e As EventArgs) Handles btnSetDataFolder.Click
        Try
            With OpenFileDialog1
                .FileName = ""
                .Multiselect = True
                .Filter = "解析結果ファイル(*.DB1)|*.DB1"
            End With

            ' ファイルダイアログでOKボタンが押下された場合
            If OpenFileDialog1.ShowDialog() = Forms.DialogResult.OK Then
                Dim pb As New MyProgressBarForm("", New DoWorkEventHandler(AddressOf SetDataFolder_DoWork))
                pb.ShowDialog()
            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub SetDataFolder_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)
        Dim bw As BackgroundWorker = DirectCast(sender, BackgroundWorker)

        ' 進行状況 [データクリア中]
        bw.ReportProgress(0, "データクリア中...")

        Dim i As Integer
        ' 現在入力されている値をクリア
        For i = 0 To Input.DATAROWS - 1
            Input.Data.CaseName.CaseNamePath(i) = ""
            Input.Data.SNAP.DeltSnapDB(i)
        Next

        ' 進行状況 [データローディング中]
        bw.ReportProgress(0, "データローディング中...")

        ' 選択されたファイルをテキストボックスに表示する
        i = 0
        For Each strFilePath As String In OpenFileDialog1.FileNames
            Dim fileName As String = IO.Path.GetFileName(strFilePath)

            Input.Data.CaseName.CaseNamePath(i) = strFilePath
            Input.Data.SNAP.SetSnapDB(i, strFilePath)

            i = i + 1
        Next

        'ケース名とラベルの表示更新
        Call dgCaseList_DataLoad(True)
        Call Label1_DataLoad()
    End Sub

    Private Sub SetData_DoWork(RowIndex As Integer)

        ' 現在入力されている値をクリア
        Input.Data.CaseName.CaseNamePath(RowIndex) = ""
        Input.Data.SNAP.DeltSnapDB(RowIndex)


        ' 選択されたファイルをテキストボックスに表示する
        Dim strFilePath = OpenFileDialog1.FileName
        Input.Data.CaseName.CaseNamePath(RowIndex) = strFilePath
        If Input.Data.SNAP.SetSnapDB(RowIndex, strFilePath) = False Then
            'ファイルの取得に失敗した
            Input.Data.CaseName.CaseNamePath(RowIndex) = ""
            Input.Data.SNAP.DeltSnapDB(RowIndex)
        End If

        'ケース名とラベルの表示更新
        Call dgCaseList_DataLoad(True)
        Call Label1_DataLoad()
    End Sub

#End Region


    Private Sub DataCheck()

        Dim DB0 = Input.Data.SNAP.SnapDB

        For i = 0 To Input.DATAROWS - 1
            Dim DBi = Input.Data.SNAP.SnapDB(i)
            If Not DBi Is Nothing Then
                '同じ内容のデータかどうか
                Try
                    If DBi.InputInfo.KihonInfo.JoNum <> DB0.InputInfo.KihonInfo.JoNum Then
                        Throw New Exception(String.Format("データ{0} は、節点数が一致していません。{1}/{2}", Input.Data.CaseName.CaseName(i), DBi.InputInfo.KihonInfo.JoNum, DB0.InputInfo.KihonInfo.JoNum))
                    End If
                    If DBi.InputInfo.KihonInfo.MeNum <> DB0.InputInfo.KihonInfo.MeNum Then
                        Throw New Exception(String.Format("データ{0} は、要素数が一致していません。{1}/{2}", Input.Data.CaseName.CaseName(i), DBi.InputInfo.KihonInfo.MeNum, DB0.InputInfo.KihonInfo.MeNum))
                    End If
                    If DBi.InputInfo.KihonInfo.APNum <> DB0.InputInfo.KihonInfo.APNum Then
                        Throw New Exception(String.Format("データ{0} は、非線形要素数が一致していません。{1}/{2}", Input.Data.CaseName.CaseName(i), DBi.InputInfo.KihonInfo.APNum, DB0.InputInfo.KihonInfo.APNum))
                    End If
                    For j = 1 To DBi.InputInfo.KihonInfo.MeNum
                        Dim mi = DBi.InputInfo.MemberInfo(j)
                        Dim m0 = DB0.InputInfo.MemberInfo(j)
                        If mi.M <> m0.M Then
                            Throw New Exception(String.Format("データ{0} は、{1}部材の非線形番号が一致していません。{2}/{3}", Input.Data.CaseName.CaseName(i), j, mi.M, m0.M))
                        End If
                    Next
                    'For j = 1 To DBi.InputInfo.DLInfo.Count - 1
                    '    Dim Di = DBi.InputInfo.DLInfo(j)
                    '    Dim D0 = DB0.InputInfo.DLInfo(j)
                    '    If Di.iType <> D0.iType Then
                    '        Throw New Exception(String.Format("データ{0} は、断面(DL)番号{1}の断面タイプが一致していません。{2}/{3}", Input.Data.CaseName.CaseName(i), j, Di.iType, D0.iType))
                    '    End If
                    'Next

                Catch ex As Exception
                    MsgBox(ex.Message)
                    Input.Data.SNAP.DeltSnapDB(i)
                    Input.Data.CaseName.CaseNamePath(i) = ""
                End Try
            End If
        Next

    End Sub


End Class