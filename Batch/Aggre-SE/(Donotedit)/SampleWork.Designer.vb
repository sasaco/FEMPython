<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class SampleDmy
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
		Me.label1 = New System.Windows.Forms.Label
		Me.SuspendLayout()
		'
		'label1
		'
		Me.label1.Dock = System.Windows.Forms.DockStyle.Fill
		Me.label1.Location = New System.Drawing.Point(0, 0)
		Me.label1.Name = "label1"
		Me.label1.Size = New System.Drawing.Size(200, 50)
		Me.label1.TabIndex = 1
		Me.label1.Text = "このファイルはサンプルの説明用のソースファイルです。サンプルの動作に問題が発生する恐れがあるため、修正を行なわないでください。"
		'
		'SampleWork
		'
		Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.Controls.Add(Me.label1)
		Me.Name = "SampleWork"
		Me.Size = New System.Drawing.Size(200, 50)
		Me.ResumeLayout(False)

	End Sub
	Private WithEvents label1 As System.Windows.Forms.Label

End Class
