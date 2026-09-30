<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MenuControl
    Inherits System.Windows.Forms.UserControl

    'UserControl はコンポーネント一覧をクリーンアップするために dispose をオーバーライドします。
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

    'Windows フォーム デザイナで必要です。
    Private components As System.ComponentModel.IContainer

    'メモ: 以下のプロシージャは Windows フォーム デザイナで必要です。
    'Windows フォーム デザイナを使用して変更できます。  
    'コード エディタを使って変更しないでください。
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
		Me.SplitContainer1 = New System.Windows.Forms.SplitContainer
		Me.pic = New System.Windows.Forms.PictureBox
		Me.textMessage = New System.Windows.Forms.TextBox
		Me.SplitContainer1.Panel1.SuspendLayout()
		Me.SplitContainer1.Panel2.SuspendLayout()
		Me.SplitContainer1.SuspendLayout()
		CType(Me.pic, System.ComponentModel.ISupportInitialize).BeginInit()
		Me.SuspendLayout()
		'
		'SplitContainer1
		'
		Me.SplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill
		Me.SplitContainer1.Location = New System.Drawing.Point(0, 0)
		Me.SplitContainer1.Name = "SplitContainer1"
		Me.SplitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal
		'
		'SplitContainer1.Panel1
		'
		Me.SplitContainer1.Panel1.Controls.Add(Me.pic)
		'
		'SplitContainer1.Panel2
		'
		Me.SplitContainer1.Panel2.Controls.Add(Me.textMessage)
		Me.SplitContainer1.Size = New System.Drawing.Size(765, 700)
		Me.SplitContainer1.SplitterDistance = 323
		Me.SplitContainer1.TabIndex = 0
		'
		'pic
		'
		Me.pic.BackColor = System.Drawing.Color.White
		Me.pic.Dock = System.Windows.Forms.DockStyle.Fill
		Me.pic.ErrorImage = Nothing
		Me.pic.Location = New System.Drawing.Point(0, 0)
		Me.pic.Name = "pic"
		Me.pic.Size = New System.Drawing.Size(765, 323)
		Me.pic.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
		Me.pic.TabIndex = 1
		Me.pic.TabStop = False
		'
		'textMessage
		'
		Me.textMessage.Dock = System.Windows.Forms.DockStyle.Fill
		Me.textMessage.Font = New System.Drawing.Font("メイリオ", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
		Me.textMessage.Location = New System.Drawing.Point(0, 0)
		Me.textMessage.Multiline = True
		Me.textMessage.Name = "textMessage"
		Me.textMessage.ReadOnly = True
		Me.textMessage.ScrollBars = System.Windows.Forms.ScrollBars.Both
		Me.textMessage.Size = New System.Drawing.Size(765, 373)
		Me.textMessage.TabIndex = 3
		'
		'MenuControl
		'
		Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.Controls.Add(Me.SplitContainer1)
		Me.Name = "MenuControl"
		Me.Size = New System.Drawing.Size(765, 700)
		Me.SplitContainer1.Panel1.ResumeLayout(False)
		Me.SplitContainer1.Panel2.ResumeLayout(False)
		Me.SplitContainer1.Panel2.PerformLayout()
		Me.SplitContainer1.ResumeLayout(False)
		CType(Me.pic, System.ComponentModel.ISupportInitialize).EndInit()
		Me.ResumeLayout(False)

	End Sub
	Friend WithEvents SplitContainer1 As System.Windows.Forms.SplitContainer
	Private WithEvents textMessage As System.Windows.Forms.TextBox
	Private WithEvents pic As System.Windows.Forms.PictureBox

End Class
