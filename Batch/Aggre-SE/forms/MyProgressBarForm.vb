Imports System.ComponentModel
Imports System.Windows.Forms

''' <summary>
''' バックグラウンド処理の進行状況を表示するフォーム
''' </summary>
Partial Public Class MyProgressBarForm
    Inherits Form

#Region "メンバ変数"

    Private workerArgument As Object = Nothing
    Private _result As Object = Nothing
    Private _error As Exception = Nothing

#End Region

#Region "プロパティ"

    ''' <summary>
    ''' DoWorkイベントハンドラで設定された結果
    ''' </summary>
    Public ReadOnly Property Result() As Object
        Get
            Return Me._result
        End Get
    End Property

    ''' <summary>
    ''' バックグラウンド処理中に発生したエラー
    ''' </summary>
    Public ReadOnly Property [Error]() As Exception
        Get
            Return Me._error
        End Get
    End Property

    ''' <summary>
    ''' 進行状況ダイアログで使用しているBackgroundWorkerクラス
    ''' </summary>
    Public ReadOnly Property BackgroundWorker() As BackgroundWorker
        Get
            Return Me.BackgroundWorker1
        End Get
    End Property

#End Region

#Region "コンストラクタ"

    ''' <summary>
    ''' ProgressDialogクラスのコンストラクタ
    ''' </summary>
    ''' <param name="caption">タイトルバーに表示するテキスト</param>
    ''' <param name="doWork">バックグラウンドで実行するメソッド</param>
    ''' <param name="argument">doWorkで取得できるパラメータ</param>
    Public Sub New(ByVal caption As String, _
                   ByVal doWork As DoWorkEventHandler, _
                   ByVal argument As Object)
        InitializeComponent()

        ' 初期設定
        Me.workerArgument = argument
        Me.lblProgress.Text = ""

        ' イベント紐付け
        AddHandler Me.BackgroundWorker1.DoWork, doWork
    End Sub

    ''' <summary>
    ''' ProgressDialogクラスのコンストラクタ
    ''' </summary>
    Public Sub New(ByVal formTitle As String, _
                   ByVal doWorkHandler As DoWorkEventHandler)
        Me.New(formTitle, doWorkHandler, Nothing)
    End Sub

#End Region

#Region "イベント"

    ''' <summary>
    ''' フォームが表示されたときにバックグラウンド処理を開始
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub MyProgressBarForm_Shown_1(sender As Object, e As EventArgs) Handles MyBase.Shown
        Me.BackgroundWorker1.RunWorkerAsync(Me.workerArgument)
    End Sub

    ''' <summary>
    ''' キャンセルボタンが押されたとき
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        btnCancel.Enabled = False
        BackgroundWorker1.CancelAsync()
    End Sub

    ''' <summary>
    ''' ReportProgressメソッドが呼び出されたとき
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub BackgroundWorker1_ProgressChanged_1(sender As Object, e As ProgressChangedEventArgs) Handles BackgroundWorker1.ProgressChanged
        ' メッセージのテキストを変更する
        Me.lblProgress.Text = DirectCast(e.UserState, String)
    End Sub

    ''' <summary>
    ''' バックグラウンド処理が終了したとき
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub BackgroundWorker1_RunWorkerCompleted_1(sender As Object, e As RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
        If e.Error IsNot Nothing Then
            MessageBox.Show(Me, "エラー", _
                            "エラーが発生しました。" & vbCrLf & vbCrLf & _
                                e.Error.Message, MessageBoxButtons.OK, _
                            MessageBoxIcon.Error)
            Me._error = e.Error
            Me.DialogResult = DialogResult.Abort
        ElseIf e.Cancelled Then
            Me.DialogResult = DialogResult.Cancel
        Else
            Me._result = e.Result
            Me.DialogResult = DialogResult.OK
        End If

        Me.Close()
    End Sub

#End Region

End Class