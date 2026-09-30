Imports System.Windows

Public Class MyDataGrid
    Inherits DataGridView

#Region "初期設定"

    Public Sub New()

        MyBase.DoubleBuffered = True

        'コンテキストメニューで表示する image は MDIParentForm.resx のものを使用する
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MDIParentForm))
        '右クリック時に表示されるコンテキストメニュー
        Dim mnuContextMenu As New ContextMenuStrip
        'コピー
        Dim CopyToolStripMenuItem As New ToolStripMenuItem
        With CopyToolStripMenuItem
            .Text = "コピー(&C)"
            .Image = CType(resources.GetObject("CopyToolStripMenuItem.Image"), System.Drawing.Image)
            .ImageTransparentColor = System.Drawing.Color.Black
            .ShortcutKeys = CType((Forms.Keys.Control Or Forms.Keys.C), Forms.Keys)
            .Size = New System.Drawing.Size(177, 22)
        End With
        mnuContextMenu.Items.Add(CopyToolStripMenuItem)
        AddHandler mnuContextMenu.Items(0).Click, AddressOf CopyClick
        '貼り付け
        Dim PasteToolStripMenuItem As New ToolStripMenuItem
        With PasteToolStripMenuItem
            .Text = "貼り付け(&P)"
            .Image = CType(resources.GetObject("PasteToolStripMenuItem.Image"), System.Drawing.Image)
            .ImageTransparentColor = System.Drawing.Color.Black
            .ShortcutKeys = CType((Forms.Keys.Control Or Forms.Keys.V), Forms.Keys)
            .Size = New System.Drawing.Size(177, 22)
        End With
        mnuContextMenu.Items.Add(PasteToolStripMenuItem)
        AddHandler mnuContextMenu.Items(1).Click, AddressOf PasteClick
        '切り取り
        Dim CutToolStripMenuItem As New ToolStripMenuItem
        With CutToolStripMenuItem
            .Text = "切り取り(&T)"
            .Image = CType(resources.GetObject("CutToolStripMenuItem.Image"), System.Drawing.Image)
            .ImageTransparentColor = System.Drawing.Color.Black
            .ShortcutKeys = CType((Forms.Keys.Control Or Forms.Keys.X), Forms.Keys)
            .Size = New System.Drawing.Size(177, 22)
        End With
        mnuContextMenu.Items.Add(CutToolStripMenuItem)
        AddHandler mnuContextMenu.Items(2).Click, AddressOf CutClick

        'コンテキストメニューの配置
        Me.ContextMenuStrip = mnuContextMenu

    End Sub

#End Region

#Region "コピー, ペースト, カット"

    ''' <summary>選択されたセルをクリップボードにコピーする</summary>
    Public Sub Copy()
        Try
            'ヘッダーをコピーしないようにする
            Me.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithAutoHeaderText
            '選択されたセルをクリップボードにコピーする
            Clipboard.SetDataObject(Me.GetClipboardContent())
        Catch ex As Exception

        End Try
    End Sub

    ''' <summary>'選択されたセルをクリップボードに保存し、削除する</summary>
    Public Sub Cut()
        Try
            Call Copy()
            For Each c As DataGridViewCell In Me.SelectedCells
                Me(c.ColumnIndex, c.RowIndex).Value = Nothing
            Next c
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>'現在のセルのある行から下にペーストする</summary>
    Public Sub Paste()
        Try
            Dim s As String = Clipboard.GetText()
            Dim lines As String() = s.Split(ControlChars.Lf)
            Dim iRow As Integer = Me.CurrentCell.RowIndex
            Dim iCol As Integer = Me.CurrentCell.ColumnIndex
            Dim oCell As DataGridViewCell
            For Each line As String In lines
                If iRow < Me.RowCount AndAlso line.Length > 0 Then
                    Dim sCells As String() = line.Split(ControlChars.Tab)
                    For i As Integer = 0 To sCells.GetLength(0) - 1
                        If iCol + i < Me.ColumnCount Then
                            oCell = Me(iCol + i, iRow)
                            If Not oCell.[ReadOnly] Then
                                oCell.Value = Convert.ChangeType(sCells(i), oCell.ValueType)
                            End If
                        Else
                            Exit For
                        End If
                    Next
                    iRow += 1
                Else
                    Exit For
                End If
            Next
        Catch ex As Exception

        End Try
    End Sub

#End Region

#Region "コンテキストメニューイベント"

    Private Sub CopyClick(sender As Object, e As EventArgs)
        Me.Copy()
    End Sub

    Private Sub PasteClick(sender As Object, e As EventArgs)
        Me.Paste()
    End Sub

    Private Sub CutClick(sender As Object, e As EventArgs)
        Me.Cut()
    End Sub

#End Region

End Class
