Imports System.Windows

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class ViewerForm
    Inherits Forms.Form

    'フォームがコンポーネントの一覧をクリーンアップするために dispose をオーバーライドします。
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Windows フォーム デザイナーで必要です。
    Private components As System.ComponentModel.IContainer

    'メモ: 以下のプロシージャは Windows フォーム デザイナーで必要です。
    'Windows フォーム デザイナーを使用して変更できます。  
    'コード エディターを使って変更しないでください。
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.ViewerControl1 = New AdvanceSoftware.VBReport8.ViewerControl()
        Me.SuspendLayout()
        '
        'ViewerControl1
        '
        Me.ViewerControl1.Dock = Forms.DockStyle.Fill
        Me.ViewerControl1.Enabled = False
        Me.ViewerControl1.Location = New System.Drawing.Point(0, 0)
        Me.ViewerControl1.MinimumSize = New System.Drawing.Size(64, 64)
        Me.ViewerControl1.Name = "ViewerControl1"
        Me.ViewerControl1.ShowReportFrame = AdvanceSoftware.VBReport8.ReportFrame.All
        Me.ViewerControl1.Size = New System.Drawing.Size(1056, 604)
        Me.ViewerControl1.TabIndex = 0
        '
        'ViewerForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1056, 604)
        Me.Controls.Add(Me.ViewerControl1)
        Me.Name = "ViewerForm"
        Me.Text = "ViewerForm"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ViewerControl1 As AdvanceSoftware.VBReport8.ViewerControl
End Class
