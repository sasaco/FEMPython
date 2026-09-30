Imports C1.Win.C1Chart
Imports System.ComponentModel

Public Class ChildFormCalculation

#Region "メンバ変数"

    ''' <summary>
    ''' シングルトン用インスタンス
    ''' </summary>
    ''' <remarks></remarks>
    Private Shared instance As ChildFormCalculation = New ChildFormCalculation()

#End Region

#Region "プロパティ"

    ''' <summary>
    ''' インスタンス取得
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property GetInstance() As ChildFormCalculation
        Get
            '静的変数が解放されていた場合のみ、インスタンスを生成する
            If instance Is Nothing Then
                instance = New ChildFormCalculation()
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

        If FormSettings.Option_杭の抵抗モーメント図作成機能 = 1 Then
            Me.Button4.Enabled = True
        Else
            Me.Button4.Enabled = False
        End If
    End Sub

#End Region

#Region "初期化処理"

    ''' <summary>
    ''' 初期処理
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub DataInit()
        Call DataLoad()
    End Sub

    Public Sub DataLoad()
        'ラジオボタンの表示更新
        Me.sbProduct0.Checked = Input.Data.Calculation.rbProduct(0)
        Me.sbProduct1.Checked = Input.Data.Calculation.rbProduct(1)
    End Sub

    Public Sub DataSave()
        Input.Data.Calculation.rbProduct(0) = Me.sbProduct0.Checked
        Input.Data.Calculation.rbProduct(1) = Me.sbProduct1.Checked
    End Sub

#End Region

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Call DataSave()
        Call DataRead()
        Dim ExcelReport As New ExcelReport()
        ExcelReport.総括表作成スタート()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Call DataSave()
        Call DataRead()
        Dim PDFReport As New PDFReport()
        PDFReport.計算書作成スタート()
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Call DataSave()
        Call DataRead()
        Dim PDFReport As New PDFReport()
        PDFReport.計算書作成スタート(1)
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Call DataSave()

        '入力項目の不備を確認 ----------------------------------------------------
        Dim CheckFlg As Boolean = False
        For Each CaseList_8 In Input.Data.CaseName.dgCaseList_8
            If CaseList_8 = -1 Then
                CheckFlg = True
                Exit For
            End If
        Next
        If CheckFlg = False Then
            MsgBox("解析ケースの入力項目⑧にチェックが入ったケースがありません。" + vbLf _
                                            + "段落し計算書を出力するケースがありません。")
            Return
        End If
        '-------------------------------------------------------------------------
        For id = 0 To Input.Data.PileAnchorBar.段落し情報.Count - 1
            Dim pileType = Input.Data.PileAnchorBar.段落し情報(id)
            If pileType.D1.Trim.Length = 0 Then
                MsgBox(String.Format("段落し情報{0}: 鉄筋径の入力がありません。", id))
                Return
            End If
            If pileType.n1.Trim.Length = 0 Then
                MsgBox(String.Format("段落し情報{0}: 鉄筋本数の入力がありません。", id))
                Return
            End If
        Next
        '-------------------------------------------------------------------------

        Call DataRead()
        Dim PDFReport As New PDFReportPileAnchorBar()
        PDFReport.計算書作成スタート()

    End Sub

    Private Sub DataRead()

        '一回読み込んだことがある場合は 再読み込みはしない
        'If Input.ReadDBFlg = True Then
        '    Return
        'End If
        Try
            Dim DEH = New DoWorkEventHandler(AddressOf Input.Data.SNAP.ReSetSnapDB)
            Dim pb As New MyProgressBarForm("", DEH, Input.Data.CaseName)
            pb.ShowDialog()

            Input.ReadDBFlg = True

        Catch ex As Exception
            Input.ReadDBFlg = False

        End Try
    End Sub

    Private Sub sbProduct0_CheckedChanged(sender As Object, e As EventArgs) Handles sbProduct0.CheckedChanged
        Input.ChengeDataFlg = True
    End Sub

    Private Sub sbProduct1_CheckedChanged(sender As Object, e As EventArgs) Handles sbProduct1.CheckedChanged
        Input.ChengeDataFlg = True
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        Call DataSave()

        '入力項目の不備を確認 ----------------------------------------------------
        Dim CheckFlg As Boolean = False
        For Each CaseList_9 In Input.Data.CaseName.dgCaseList_9
            If CaseList_9 = -1 Then
                CheckFlg = True
                Exit For
            End If
        Next
        If CheckFlg = False Then
            MsgBox("解析ケースの入力項目⑨にチェックが入ったケースがありません。" + vbLf _
                                            + "断面力ピックアップファイルを出力するケースがありません。")
            Return
        End If
        '-------------------------------------------------------------------------
        Call DataRead()

        Dim ExcelReport As New ExcelReportPickUpFile()
        ExcelReport.断面力ピックアップファイル作成スタート()
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click

        '④:上記InputFile内の pdf ファイルを結合し、表示。 
        Dim FN As String = common.TempPath + "\PrintPreview.pdf"
        Try
            ' ファイルが存在しているかどうか確認する
            If System.IO.File.Exists(FN) Then
                Dim MyPdfViewerForm As New pdfViewerForm(FN)
                MyPdfViewerForm.ShowPdf()
            Else
                MsgBox("表示できる前回の結果がありません。")
            End If

        Catch ex As Exception
            Throw New Exception("前回の結果表示に失敗しました。" + vbLf _
                                + ex.Message)
        End Try

    End Sub

End Class