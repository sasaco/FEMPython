Imports System.Windows
Imports System.Reflection
Imports System.ComponentModel

Public Class MDIParentForm

#Region "メンバ変数"


#End Region

#Region "初期化"

    ''' <summary>
    ''' フォームロード
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub MDIParentForm_Load(sender As Object, e As EventArgs) Handles Me.Load

        ' 各画面インスタンス生成
        Dim childFormCaseName As ChildFormCaseName = childFormCaseName.GetInstance
        Dim childFormRequirement As ChildFormRequirement = childFormRequirement.GetInstance
        Dim childFormElementSetting As ChildFormElementSetting = childFormElementSetting.GetInstance
        Dim childFormFoundationSetting As ChildFormFoundationSetting = childFormFoundationSetting.GetInstance
        Dim childFormCalculation As ChildFormCalculation = childFormCalculation.GetInstance
        Dim childFormPileAnchorBar As ChildFormPileAnchorBar = childFormPileAnchorBar.GetInstance

        ' 初期画面ボタンクリック
        'ShowCalculationFormButton.PerformClick()
        ShowCaseNameFormButton.PerformClick()

        '履歴をメニューに追加
        Call SetRecentFiles(FormSettings.ReadRecentFileList)


        'ChengeDataFlg を初期化
        Input.ChengeDataFlg = False
        Input.ReadDBFlg = False

        'ステータスバーの更新用イベントハンドラーの追加
        AddHandler Input.StatasChanged, AddressOf Input_Statas_Changed

        '(1) 杭の段落とし図作成機能のライセンスチェック
        If FormSettings.Option_杭の抵抗モーメント図作成機能 = 1 Then
            Me.ShowPileAnchorBarFormButton.Enabled = True
        Else
            Me.ShowPileAnchorBarFormButton.Enabled = False
        End If

    End Sub

    '履歴をメニューに追加
    Private Sub SetRecentFiles(tmpRecentFileList As List(Of String))
        '消去
        Try
            Dim removeList As New List(Of Forms.ToolStripMenuItem)
            For i = 0 To Me.FileMenu.DropDownItems.Count - 1
                Dim item = Me.FileMenu.DropDownItems(i)
                If InStr(item.Name, "RedentToolStripMenuItem") Then
                    removeList.Add(item)
                End If
            Next
            For Each item In removeList
                Me.FileMenu.DropDownItems.Remove(item)
            Next
        Catch ex As Exception

        End Try
        '追加
        Try
            For i = tmpRecentFileList.Count - 1 To 0 Step -1
                Dim FilePath = tmpRecentFileList(i)
                Dim FileName = FilePath 'System.IO.Path.GetFileName(FilePath)
                Dim RedentToolStripMenuItem As New Forms.ToolStripMenuItem
                RedentToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Black
                RedentToolStripMenuItem.Name = String.Format("RedentToolStripMenuItem{0}", i)
                RedentToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
                RedentToolStripMenuItem.Text = FileName
                RedentToolStripMenuItem.ToolTipText = FilePath
                AddHandler RedentToolStripMenuItem.Click, AddressOf RedentToolStripMenuItem_Click
                Me.FileMenu.DropDownItems.Add(RedentToolStripMenuItem)
            Next
        Catch ex As Exception

        End Try

        FormSettings.SaveRecentFiles()
    End Sub

    ''' <summary>
    ''' ステータスバーの更新
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub Input_Statas_Changed(sender As Object, e As EventArgs)
        Try
            Dim s As String = ""

            If Input.ChengeDataFlg = True Then
                s += "●変更あり "
            End If
            'If Input.ReadDBFlg = True Then
            '    s += "■解析データ読込済 "
            'End If
            Me.ToolStripStatusLabel.Text = s
        Catch ex As Exception

        End Try
    End Sub

#End Region

#Region "フォームクローズ処理"

    ''' <summary>
    ''' フォームクロージング
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub MDIParentForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing

        Try
            If Input.ChengeDataFlg = True Then

                Dim result As DialogResult

                If System.IO.File.Exists(Input.FileName) Then
                    result = MessageBox.Show(Input.GetFileName & "への変更を保存しますか？",
                                             "確認",
                                             MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Exclamation,
                                             MessageBoxDefaultButton.Button1)

                    If result = Forms.DialogResult.Yes Then
                        Call SaveMenuItem_Click(Nothing, Nothing)
                    End If
                Else
                    result = MessageBox.Show("データが変更されています。データを保存しますか？",
                                             "確認",
                                             MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Exclamation,
                                             MessageBoxDefaultButton.Button1)
                    If result = Forms.DialogResult.Yes Then
                        Call SaveAsMenuItem_Click(Nothing, Nothing)
                    End If
                End If
            End If
            'RemoveHandler Input.StatasChanged, AddressOf Input_Statas_Changed

            FormSettings.SaveRecentFiles()

        Catch ex As Exception

        End Try

        'アプリケーションを強制終了します
        'Environment.Exit(0)

    End Sub

    ''' <summary>
    ''' フォームクローズ
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks>閉じるボタンを押しても終了しない(原因不明の)不具合があったので強制終了処理を追加</remarks>
    Private Sub MDIParentForm_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed

    End Sub

#End Region

#Region "ChildForm の呼び出し"

    ''' <summary>
    ''' ChildForm の呼び出し
    ''' </summary>
    ''' <param name="ChildForm">表示対象子フォーム</param>
    ''' <param name="tsb">押下されたツールストリップボタン</param>
    ''' <param name="isNewOrOpen">新規作成、または開くが押されたか</param>
    ''' <remarks></remarks>
    Private Sub ShowChildForm(ChildForm As Form, tsb As ToolStripButton, isNewOrOpen As Boolean)

        '呼び出しただけで、Input.ChengeDataFlg = true になってしまうので後で戻す用に保持しておく。
        Dim oldCengeFlg As Boolean = Input.ChengeDataFlg
        Dim oldReadDBFlg As Boolean = Input.ReadDBFlg

        ' アクティブ子フォーム取得
        Dim activeMdiChild As Form = Me.ActiveMdiChild
        ' アクティブ子フォームと呼び出しフォームが同じ場合 何もしない
        Try
            If IsNothing(activeMdiChild) = False Then
                If activeMdiChild.Equals(ChildForm) Then Return
            End If
        Catch
        End Try


        ' 画面名から画面特定後、データセーブ実施
        If Not isNewOrOpen And Not activeMdiChild Is Nothing Then
            Me.CallDataSave(activeMdiChild)
        End If

        ' アクティブ子フォーム非表示
        If Not activeMdiChild Is Nothing Then
            activeMdiChild.Hide()
        End If

        ' 子フォームの新しいインスタンスを作成します
        ' 表示する前に、この MDI フォームの子に設定します
        ChildForm.MdiParent = Me
        ChildForm.WindowState = FormWindowState.Maximized
        ChildForm.Size = Size
        ChildForm.Show()

        Call Me.CallDataInit(ChildForm)

        'ツールバーのボタンのCheckStateをオフにする
        For Each tsi In Me.ToolStrip.Items
            Try
                Dim t As ToolStripButton = TryCast(tsi, ToolStripButton)
                If Not IsNothing(t) Then
                    t.CheckState = CheckState.Unchecked
                End If
            Catch ex As Exception
            End Try
        Next
        '該当するツールバーのボタンのCheckStateをオンにする
        tsb.CheckState = CheckState.Checked

        '呼び出しただけで、Input.ChengeDataFlg = true になってしまうので戻す。
        Input.ChengeDataFlg = oldCengeFlg
        Input.ReadDBFlg = oldReadDBFlg

    End Sub

    ''' <summary>
    ''' 解析ケース表示
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub ShowCaseNameForm(ByVal sender As Object, ByVal e As EventArgs) Handles ShowCaseNameFormButton.Click
        ShowChildForm(ChildFormCaseName.GetInstance, TryCast(sender, ToolStripButton), False)
    End Sub

    ''' <summary>
    ''' 照査パラメータ表示
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub ShowRequirementForm(sender As Object, e As EventArgs) Handles ShowRequirementFormButton.Click
        ShowChildForm(ChildFormRequirement.GetInstance, TryCast(sender, ToolStripButton), False)
    End Sub

    ''' <summary>
    ''' 断面表示
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub ShowElementSettingForm(ByVal sender As Object, ByVal e As EventArgs) Handles ShowElementSettingFormButton.Click
        ShowChildForm(ChildFormElementSetting.GetInstance, TryCast(sender, ToolStripButton), False)
    End Sub

    ''' <summary>
    ''' 基礎表示
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub ShowFoundationSettingForm(ByVal sender As Object, ByVal e As EventArgs) Handles ShowFoundationSettingFormButton.Click
        ShowChildForm(ChildFormFoundationSetting.GetInstance, TryCast(sender, ToolStripButton), False)
    End Sub

    ''' <summary>
    ''' 杭の段落し
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub ShowPileAnchorBarFormButton_Click(sender As Object, e As EventArgs) Handles ShowPileAnchorBarFormButton.Click
        ShowChildForm(ChildFormPileAnchorBar.GetInstance, TryCast(sender, ToolStripButton), False)
    End Sub

    ''' <summary>
    ''' 計算表示
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub CalcStart_Click(sender As Object, e As EventArgs) Handles ShowCalculationFormButton.Click
        ShowChildForm(ChildFormCalculation.GetInstance, TryCast(sender, ToolStripButton), False)
    End Sub

#End Region

#Region "メニュー操作"

    ''' <summary>
    ''' アプリケーションの終了
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub ExitMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles ExitToolStripMenuItem.Click
        Me.Close()
    End Sub

    ''' <summary>
    ''' ファイルを開く
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub OpenMenuItem_Click(sender As Object, e As EventArgs) Handles OpenToolStripMenuItem.Click
        Try
            If Input.ChengeDataFlg = True Then

                Dim result As DialogResult

                If System.IO.File.Exists(Input.FileName) Then
                    result = MessageBox.Show(Input.GetFileName & "への変更を保存しますか？",
                                             "確認",
                                             MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Exclamation,
                                             MessageBoxDefaultButton.Button1)

                    If result = Forms.DialogResult.Yes Then
                        Call SaveMenuItem_Click(Nothing, Nothing)
                    End If
                Else
                    result = MessageBox.Show("データが変更されています。データを保存しますか？",
                                             "確認",
                                             MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Exclamation,
                                             MessageBoxDefaultButton.Button1)
                    If result = Forms.DialogResult.Yes Then
                        Call SaveAsMenuItem_Click(Nothing, Nothing)
                    End If
                End If
            End If

            Dim OpenFileDialog As New OpenFileDialog

            If Not Input.FileName Is Nothing AndAlso Len(Input.FileName) > 0 Then
                Dim stParentName As String = System.IO.Path.GetDirectoryName(Input.FileName)
                OpenFileDialog.InitialDirectory = stParentName
            End If
            OpenFileDialog.RestoreDirectory = True
            OpenFileDialog.Filter = "データ ファイル (*.xml)|*.xml|すべてのファイル (*.*)|*.*"
            If (OpenFileDialog.ShowDialog(Me) = Forms.DialogResult.OK) Then
                Call OpenFile(OpenFileDialog.FileName)
                Input.ReadDBFlg = False
            End If

            ''履歴メニューを更新
            'If FormSettings.RecentFile_Add(Input.FileName) = True Then
            '    Call SetRecentFiles(FormSettings.GetRecentFileList)
            'End If
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub

    Private Sub OpenFile(FileName As String)
        Try
            If System.IO.File.Exists(FileName) = False Then
                MessageBox.Show("ファイルが見つかりません。" + vbLf +
                                FileName, "確認")
                Return
            End If

            ' インスタンスリフレッシュ
            ChildFormCaseName.Reflesh()
            ChildFormElementSetting.Reflesh()
            ChildFormFoundationSetting.Reflesh()
            ChildFormRequirement.Reflesh()

            'FileNameをShift-JISコードとして開く
            Dim sr As New System.IO.StreamReader(FileName,
                System.Text.Encoding.GetEncoding("shift_jis"))
            '内容をすべて読み込む
            Dim s As String = sr.ReadToEnd()
            'ファイルを閉じる
            sr.Close()

            'データの更新
            Input.Clear()
            Input.SetXmData(s)
            Input.FileName = FileName
            Input.applyVersion()

            'バーにファイル名の表示
            Call SetTitleBarText(FileName)

            'ケース名画面にする
            Call ShowChildForm(ChildFormCaseName.GetInstance, Me.ShowCaseNameFormButton, True)

            '履歴メニューを更新
            If FormSettings.RecentFile_Add(FileName) = True Then
                Call SetRecentFiles(FormSettings.GetRecentFileList)
            End If

            Input.ChengeDataFlg = False
            Input.ReadDBFlg = False

        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' 上書き保存
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub SaveMenuItem_Click(sender As Object, e As EventArgs) Handles SaveToolStripMenuItem.Click
        Try
            CallDataSave(Me.ActiveMdiChild)
            Input.FileName = SaveFile(Input.FileName, Input.GetXmlData())
            CallDataLoad(Me.ActiveMdiChild)
            Input.ChengeDataFlg = False

        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    ''' <summary>
    ''' 名前を付けて保存
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub SaveAsMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles SaveAsToolStripMenuItem.Click
        Try
            Dim oldFileName = Input.FileName
            Dim newFileName = SaveFileName(Input.FileName)

            Input.FileName = newFileName

            ' 現在表示中の画面を保存する
            CallDataSave(Me.ActiveMdiChild)

            ' 相対パスを更新する
            Input.Data.SNAP.ReSetSnapDB(oldFileName, newFileName)

            Input.FileName = SaveFile(Input.FileName, Input.GetXmlData())
            CallDataLoad(Me.ActiveMdiChild)

            '履歴メニューを更新
            If FormSettings.RecentFile_Add(Input.FileName) = True Then
                Call SetRecentFiles(FormSettings.GetRecentFileList)
            End If

            'バーにファイル名の表示
            Call SetTitleBarText(Input.FileName)

            Input.ChengeDataFlg = False
            Input.ReadDBFlg = False

        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    ''' <summary>
    ''' ファイル保存
    ''' </summary>
    ''' <param name="Filename"></param>
    ''' <param name="Value"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function SaveFile(Filename As String, Value As String) As String
        Try
            Dim result As String = Filename
            If result <> "" Then
                '書き込むファイルが既に存在している場合は、上書きする
                Dim sw As New System.IO.StreamWriter(result,
                    False,
                    System.Text.Encoding.GetEncoding("shift_jis"))
                'resultの内容を書き込む
                sw.Write(Value)
                '閉じる
                sw.Close()
            End If
            Return result
        Catch ex As Exception
            Throw ex
        End Try
    End Function

    Private Function SaveFileName(Filename As String) As String
        Dim result As String = Filename
        Dim SaveFileDialog As New SaveFileDialog
        SaveFileDialog.RestoreDirectory = True
        If System.IO.File.Exists(Filename) Then
            SaveFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(Filename)
            SaveFileDialog.FileName = System.IO.Path.GetFileName(Filename)
        End If
        SaveFileDialog.Filter = "データ ファイル (*.xml)|*.xml|すべてのファイル (*.*)|*.*"
        If (SaveFileDialog.ShowDialog(Me) = Forms.DialogResult.OK) Then
            Dim selectedFile As String = SaveFileDialog.FileName
            Dim ext As String = System.IO.Path.GetExtension(selectedFile)
            If String.IsNullOrEmpty(ext) OrElse String.Compare(ext, ".xml", True) <> 0 Then
                selectedFile = System.IO.Path.ChangeExtension(selectedFile, ".xml")
            End If
            result = selectedFile
        End If
        Return result
    End Function

    ''' <summary>
    ''' 新規作成
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub NewMenuItem_Click(sender As Object, e As EventArgs) Handles NewToolStripMenuItem.Click
        Try
            If Input.ChengeDataFlg = True Then

                Dim result As DialogResult

                If System.IO.File.Exists(Input.FileName) Then
                    result = MessageBox.Show(Input.GetFileName & "への変更を保存しますか？",
                                             "確認",
                                             MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Exclamation,
                                             MessageBoxDefaultButton.Button1)

                    If result = Forms.DialogResult.Yes Then
                        Call SaveMenuItem_Click(Nothing, Nothing)
                    End If
                Else
                    result = MessageBox.Show("データが変更されています。データを保存しますか？",
                                             "確認",
                                             MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Exclamation,
                                             MessageBoxDefaultButton.Button1)
                    If result = Forms.DialogResult.Yes Then
                        Call SaveAsMenuItem_Click(Nothing, Nothing)
                    End If
                End If
            End If

            ' インスタンスリフレッシュ
            ChildFormCaseName.Reflesh()
            ChildFormElementSetting.Reflesh()
            ChildFormFoundationSetting.Reflesh()
            ChildFormRequirement.Reflesh()

            'データの更新
            Input.Clear()
            Input.FileName = ""

            'バーにファイル名の表示
            Call SetTitleBarText("")

            'ケース名画面にする
            Call ShowChildForm(ChildFormCaseName.GetInstance, Me.ShowCaseNameFormButton, True)

            Input.ChengeDataFlg = False
            Input.ReadDBFlg = False
            Me.ToolStripStatusLabel.Text = ""

        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    ''' <summary>
    ''' データロード呼び出し
    ''' </summary>
    ''' <param name="childform"></param>
    ''' <remarks></remarks>
    Private Sub CallDataLoad(childform As Form)
        Try
            Dim mi As MethodInfo = childform.GetType().GetMethod("DataLoad")
            Dim param As Object = Nothing
            mi.Invoke(childform, param)

        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    ''' <summary>
    ''' データセーブ呼び出し
    ''' </summary>
    ''' <param name="childform"></param>
    ''' <remarks></remarks>
    Private Sub CallDataSave(childform As Form)
        Try
            Dim mi As MethodInfo = childform.GetType().GetMethod("DataSave")
            Dim param As Object = Nothing
            mi.Invoke(childform, param)
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    ''' <summary>
    ''' データセーブ呼び出し
    ''' </summary>
    ''' <param name="childform"></param>
    ''' <remarks></remarks>
    Private Sub CallDataInit(childform As Form)
        Try
            Dim mi As MethodInfo = childform.GetType().GetMethod("DataInit")
            Dim param As Object = Nothing
            mi.Invoke(childform, param)
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    ''' <summary>
    ''' バージョン情報
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        Dim VerSionForm = New VersionForm
        VerSionForm.ShowDialog()
    End Sub

    ' ヘルプ
    Public Declare Function ShellExecute Lib "shell32.dll" Alias "ShellExecuteA" (ByVal hWnd As Integer, ByVal lpOperation As String, ByVal lpFile As String, ByVal lpParameters As String, ByVal lpDirectory As String, ByVal nShowCmd As Integer) As Integer ' ファイルの関連付けをもとに起動するAPI													
    Public Const SW_SHOWNORMAL As Short = 1 ' 標準Window													
    Public Const SW_SHOWMAXIMIZED As Short = 3 ' 最大化													
    Public Const SW_SHOWMINIMIZED As Short = 2 ' 最小化													
    Private Sub ContentsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ContentsToolStripMenuItem.Click
        Try
            Dim ExePath = Assembly.GetEntryAssembly().Location
            Dim basePath As String = System.IO.Path.GetDirectoryName(ExePath)
            Dim filePath As String = "Docu-SE(Batch)_操作説明書.pdf"
            ''絶対パスを取得する
            Dim absolutePath = System.IO.Path.Combine(basePath, filePath)

            Dim X As Integer
            '' マニュアル表示
            X = ShellExecute(0, "Open", absolutePath, vbNullString, vbNullString, SW_SHOWNORMAL)

        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub

#End Region

    Private Sub RedentToolStripMenuItem_Click(sender As Object, e As EventArgs)
        Try

            Dim MenuItem As ToolStripMenuItem = TryCast(sender, ToolStripMenuItem)
            Dim Name As String = MenuItem.Name
            Dim id As Integer = Val(Name.Replace("RedentToolStripMenuItem", ""))
            Dim fileName As String = FormSettings.GetRecentFile(id)

            If Input.ChengeDataFlg = True Then

                Dim result As DialogResult

                If System.IO.File.Exists(Input.FileName) Then
                    result = MessageBox.Show(Input.GetFileName & "への変更を保存しますか？",
                                             "確認",
                                             MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Exclamation,
                                             MessageBoxDefaultButton.Button1)

                    If result = Forms.DialogResult.Yes Then
                        Call SaveMenuItem_Click(Nothing, Nothing)
                    End If
                Else
                    result = MessageBox.Show("データが変更されています。データを保存しますか？",
                                             "確認",
                                             MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Exclamation,
                                             MessageBoxDefaultButton.Button1)
                    If result = Forms.DialogResult.Yes Then
                        Call SaveAsMenuItem_Click(Nothing, Nothing)
                    End If
                End If
            End If

            Call OpenFile(fileName)

        Catch ex As Exception

        End Try

    End Sub


    ''' <summary>バーにファイル名の表示</summary>
    ''' <param name="Addtext"></param>
    ''' <remarks></remarks>
    Private Sub SetTitleBarText(ByVal Addtext As String)

        Me.Text = "Batch:複数検討作業処理ツール " + Addtext


    End Sub

    Private Sub MDIParentForm_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        '履歴メニューを更新
        If FormSettings.RecentFile_Add(Input.FileName) = True Then
            Call SetRecentFiles(FormSettings.GetRecentFileList)
        End If

    End Sub
End Class
