<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class SampleControl
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
		Me.splControl = New System.Windows.Forms.SplitContainer
		Me.splMenu = New System.Windows.Forms.SplitContainer
		Me.labelDescription = New System.Windows.Forms.Label
		Me.textCodeView = New System.Windows.Forms.RichTextBox
		Me.label2 = New System.Windows.Forms.Label
		Me.buttonCopyCode = New System.Windows.Forms.Button
		Me.radioFontLarge = New System.Windows.Forms.RadioButton
		Me.radioFontMiddle = New System.Windows.Forms.RadioButton
		Me.radioFontSmall = New System.Windows.Forms.RadioButton
		Me.label1 = New System.Windows.Forms.Label
		Me.splControl.Panel1.SuspendLayout()
		Me.splControl.SuspendLayout()
		Me.splMenu.Panel1.SuspendLayout()
		Me.splMenu.Panel2.SuspendLayout()
		Me.splMenu.SuspendLayout()
		Me.SuspendLayout()
		'
		'splControl
		'
		Me.splControl.Dock = System.Windows.Forms.DockStyle.Fill
		Me.splControl.FixedPanel = System.Windows.Forms.FixedPanel.Panel1
		Me.splControl.Location = New System.Drawing.Point(0, 0)
		Me.splControl.Name = "splControl"
		Me.splControl.Orientation = System.Windows.Forms.Orientation.Horizontal
		'
		'splControl.Panel1
		'
		Me.splControl.Panel1.Controls.Add(Me.splMenu)
		Me.splControl.Size = New System.Drawing.Size(765, 770)
		Me.splControl.SplitterDistance = 396
		Me.splControl.TabIndex = 0
		'
		'splMenu
		'
		Me.splMenu.Dock = System.Windows.Forms.DockStyle.Fill
		Me.splMenu.FixedPanel = System.Windows.Forms.FixedPanel.Panel1
		Me.splMenu.IsSplitterFixed = True
		Me.splMenu.Location = New System.Drawing.Point(0, 0)
		Me.splMenu.Name = "splMenu"
		'
		'splMenu.Panel1
		'
		Me.splMenu.Panel1.BackColor = System.Drawing.Color.GhostWhite
		Me.splMenu.Panel1.Controls.Add(Me.labelDescription)
		Me.splMenu.Panel1.Font = New System.Drawing.Font("メイリオ", 9.0!)
		'
		'splMenu.Panel2
		'
		Me.splMenu.Panel2.BackColor = System.Drawing.Color.MintCream
		Me.splMenu.Panel2.Controls.Add(Me.textCodeView)
		Me.splMenu.Panel2.Controls.Add(Me.label2)
		Me.splMenu.Panel2.Controls.Add(Me.buttonCopyCode)
		Me.splMenu.Panel2.Controls.Add(Me.radioFontLarge)
		Me.splMenu.Panel2.Controls.Add(Me.radioFontMiddle)
		Me.splMenu.Panel2.Controls.Add(Me.radioFontSmall)
		Me.splMenu.Panel2.Controls.Add(Me.label1)
		Me.splMenu.Size = New System.Drawing.Size(765, 396)
		Me.splMenu.SplitterDistance = 380
		Me.splMenu.TabIndex = 0
		'
		'labelDescription
		'
		Me.labelDescription.BackColor = System.Drawing.Color.GhostWhite
		Me.labelDescription.Dock = System.Windows.Forms.DockStyle.Top
		Me.labelDescription.Font = New System.Drawing.Font("メイリオ", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
		Me.labelDescription.ForeColor = System.Drawing.Color.Red
		Me.labelDescription.Location = New System.Drawing.Point(0, 0)
		Me.labelDescription.Name = "labelDescription"
		Me.labelDescription.Size = New System.Drawing.Size(380, 22)
		Me.labelDescription.TabIndex = 1
		'
		'textCodeView
		'
		Me.textCodeView.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
					Or System.Windows.Forms.AnchorStyles.Left) _
					Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.textCodeView.BackColor = System.Drawing.Color.White
		Me.textCodeView.Font = New System.Drawing.Font("ＭＳ ゴシック", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
		Me.textCodeView.Location = New System.Drawing.Point(2, 27)
		Me.textCodeView.Name = "textCodeView"
		Me.textCodeView.ReadOnly = True
		Me.textCodeView.Size = New System.Drawing.Size(376, 349)
		Me.textCodeView.TabIndex = 13
		Me.textCodeView.Text = ""
		Me.textCodeView.WordWrap = False
		'
		'label2
		'
		Me.label2.BackColor = System.Drawing.Color.MintCream
		Me.label2.Dock = System.Windows.Forms.DockStyle.Top
		Me.label2.Font = New System.Drawing.Font("メイリオ", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
		Me.label2.ForeColor = System.Drawing.Color.Red
		Me.label2.Location = New System.Drawing.Point(0, 0)
		Me.label2.Name = "label2"
		Me.label2.Size = New System.Drawing.Size(381, 22)
		Me.label2.TabIndex = 19
		Me.label2.Text = "サンプルコード"
		Me.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
		'
		'buttonCopyCode
		'
		Me.buttonCopyCode.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.buttonCopyCode.BackColor = System.Drawing.Color.Transparent
		Me.buttonCopyCode.ForeColor = System.Drawing.Color.LightCoral
		Me.buttonCopyCode.Location = New System.Drawing.Point(70, 375)
		Me.buttonCopyCode.Name = "buttonCopyCode"
		Me.buttonCopyCode.Size = New System.Drawing.Size(104, 20)
		Me.buttonCopyCode.TabIndex = 14
		Me.buttonCopyCode.Text = "選択部分のコピー"
		Me.buttonCopyCode.UseVisualStyleBackColor = False
		'
		'radioFontLarge
		'
		Me.radioFontLarge.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.radioFontLarge.AutoSize = True
		Me.radioFontLarge.BackColor = System.Drawing.Color.Transparent
		Me.radioFontLarge.ForeColor = System.Drawing.Color.LightCoral
		Me.radioFontLarge.Location = New System.Drawing.Point(326, 377)
		Me.radioFontLarge.Name = "radioFontLarge"
		Me.radioFontLarge.Size = New System.Drawing.Size(35, 16)
		Me.radioFontLarge.TabIndex = 18
		Me.radioFontLarge.Text = "大"
		Me.radioFontLarge.UseVisualStyleBackColor = False
		'
		'radioFontMiddle
		'
		Me.radioFontMiddle.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.radioFontMiddle.AutoSize = True
		Me.radioFontMiddle.BackColor = System.Drawing.Color.Transparent
		Me.radioFontMiddle.ForeColor = System.Drawing.Color.LightCoral
		Me.radioFontMiddle.Location = New System.Drawing.Point(285, 377)
		Me.radioFontMiddle.Name = "radioFontMiddle"
		Me.radioFontMiddle.Size = New System.Drawing.Size(35, 16)
		Me.radioFontMiddle.TabIndex = 17
		Me.radioFontMiddle.Text = "中"
		Me.radioFontMiddle.UseVisualStyleBackColor = False
		'
		'radioFontSmall
		'
		Me.radioFontSmall.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.radioFontSmall.AutoSize = True
		Me.radioFontSmall.BackColor = System.Drawing.Color.Transparent
		Me.radioFontSmall.Checked = True
		Me.radioFontSmall.ForeColor = System.Drawing.Color.LightCoral
		Me.radioFontSmall.Location = New System.Drawing.Point(244, 377)
		Me.radioFontSmall.Name = "radioFontSmall"
		Me.radioFontSmall.Size = New System.Drawing.Size(35, 16)
		Me.radioFontSmall.TabIndex = 16
		Me.radioFontSmall.TabStop = True
		Me.radioFontSmall.Text = "小"
		Me.radioFontSmall.UseVisualStyleBackColor = False
		'
		'label1
		'
		Me.label1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.label1.AutoSize = True
		Me.label1.BackColor = System.Drawing.Color.Transparent
		Me.label1.ForeColor = System.Drawing.Color.LightCoral
		Me.label1.Location = New System.Drawing.Point(180, 379)
		Me.label1.Name = "label1"
		Me.label1.Size = New System.Drawing.Size(58, 12)
		Me.label1.TabIndex = 15
		Me.label1.Text = "文字サイズ"
		'
		'SampleControl
		'
		Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.Controls.Add(Me.splControl)
		Me.Name = "SampleControl"
		Me.Size = New System.Drawing.Size(765, 770)
		Me.splControl.Panel1.ResumeLayout(False)
		Me.splControl.ResumeLayout(False)
		Me.splMenu.Panel1.ResumeLayout(False)
		Me.splMenu.Panel2.ResumeLayout(False)
		Me.splMenu.Panel2.PerformLayout()
		Me.splMenu.ResumeLayout(False)
		Me.ResumeLayout(False)

	End Sub
	Protected WithEvents splControl As System.Windows.Forms.SplitContainer
	Protected WithEvents splMenu As System.Windows.Forms.SplitContainer
	Private WithEvents textCodeView As System.Windows.Forms.RichTextBox
	Protected WithEvents label2 As System.Windows.Forms.Label
	Private WithEvents buttonCopyCode As System.Windows.Forms.Button
	Private WithEvents radioFontLarge As System.Windows.Forms.RadioButton
	Private WithEvents radioFontMiddle As System.Windows.Forms.RadioButton
	Private WithEvents radioFontSmall As System.Windows.Forms.RadioButton
	Private WithEvents label1 As System.Windows.Forms.Label
	Protected WithEvents labelDescription As System.Windows.Forms.Label

End Class
