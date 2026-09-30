Imports System.Windows

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class ChildFormCalculation
    Inherits Forms.Form

    'フォームがコンポーネントの一覧をクリーンアップするために dispose をオーバーライドします。
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.sbProduct1 = New System.Windows.Forms.RadioButton()
        Me.sbProduct0 = New System.Windows.Forms.RadioButton()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Button4 = New System.Windows.Forms.Button()
        Me.Button5 = New System.Windows.Forms.Button()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Button6 = New System.Windows.Forms.Button()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.GroupBox1.SuspendLayout()
        Me.SuspendLayout()
        '
        'Button1
        '
        Me.Button1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Button1.Location = New System.Drawing.Point(368, 180)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(147, 50)
        Me.Button1.TabIndex = 5
        Me.Button1.Text = "総括表のみ"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.sbProduct1)
        Me.GroupBox1.Controls.Add(Me.sbProduct0)
        Me.GroupBox1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.GroupBox1.Location = New System.Drawing.Point(33, 33)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(948, 210)
        Me.GroupBox1.TabIndex = 0
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "出力形式"
        '
        'sbProduct1
        '
        Me.sbProduct1.AutoSize = True
        Me.sbProduct1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.sbProduct1.Location = New System.Drawing.Point(20, 51)
        Me.sbProduct1.Name = "sbProduct1"
        Me.sbProduct1.Size = New System.Drawing.Size(83, 16)
        Me.sbProduct1.TabIndex = 2
        Me.sbProduct1.TabStop = True
        Me.sbProduct1.Text = "カスタム仕様"
        Me.sbProduct1.UseVisualStyleBackColor = True
        '
        'sbProduct0
        '
        Me.sbProduct0.AutoSize = True
        Me.sbProduct0.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.sbProduct0.Location = New System.Drawing.Point(20, 29)
        Me.sbProduct0.Name = "sbProduct0"
        Me.sbProduct0.Size = New System.Drawing.Size(71, 16)
        Me.sbProduct0.TabIndex = 1
        Me.sbProduct0.TabStop = True
        Me.sbProduct0.Text = "標準仕様"
        Me.sbProduct0.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Button2.Location = New System.Drawing.Point(368, 45)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(147, 50)
        Me.Button2.TabIndex = 3
        Me.Button2.Text = "結果詳細"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'Button3
        '
        Me.Button3.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Button3.Location = New System.Drawing.Point(369, 111)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(146, 50)
        Me.Button3.TabIndex = 4
        Me.Button3.Text = "結果概要"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label1.Location = New System.Drawing.Point(521, 64)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(255, 12)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "荷重～変位曲線, 応力図, 各ケース照査表, 総括表"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label2.Location = New System.Drawing.Point(521, 130)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(175, 12)
        Me.Label2.TabIndex = 0
        Me.Label2.Text = "荷重～変位曲線, 応力図,, 総括表"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label3.Location = New System.Drawing.Point(521, 199)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(160, 12)
        Me.Label3.TabIndex = 0
        Me.Label3.Text = "総括表（エクセルファイル出力可）"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label4.Location = New System.Drawing.Point(522, 269)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(72, 12)
        Me.Label4.TabIndex = 0
        Me.Label4.Text = "杭の段落し図"
        '
        'Button4
        '
        Me.Button4.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Button4.Location = New System.Drawing.Point(369, 250)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(147, 50)
        Me.Button4.TabIndex = 6
        Me.Button4.Text = "杭の段落し図"
        Me.Button4.UseVisualStyleBackColor = True
        '
        'Button5
        '
        Me.Button5.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Button5.Location = New System.Drawing.Point(370, 317)
        Me.Button5.Name = "Button5"
        Me.Button5.Size = New System.Drawing.Size(147, 50)
        Me.Button5.TabIndex = 6
        Me.Button5.Text = "断面力集計"
        Me.Button5.UseVisualStyleBackColor = True
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label5.Location = New System.Drawing.Point(523, 336)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(158, 12)
        Me.Label5.TabIndex = 0
        Me.Label5.Text = "断面力ピックアップファイルの作成"
        '
        'Button6
        '
        Me.Button6.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Button6.Location = New System.Drawing.Point(370, 386)
        Me.Button6.Name = "Button6"
        Me.Button6.Size = New System.Drawing.Size(147, 50)
        Me.Button6.TabIndex = 6
        Me.Button6.Text = "再表示"
        Me.Button6.UseVisualStyleBackColor = True
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label6.Location = New System.Drawing.Point(523, 405)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(136, 12)
        Me.Label6.TabIndex = 0
        Me.Label6.Text = "前回の結果を再表示します"
        '
        'ChildFormCalculation
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.ClientSize = New System.Drawing.Size(1011, 531)
        Me.ControlBox = False
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.Button6)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Button5)
        Me.Controls.Add(Me.Button4)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.GroupBox1)
        Me.DoubleBuffered = True
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "ChildFormCalculation"
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Button1 As Forms.Button
    Friend WithEvents GroupBox1 As Forms.GroupBox
    Friend WithEvents sbProduct0 As Forms.RadioButton
    Friend WithEvents sbProduct1 As Forms.RadioButton
    Friend WithEvents Button2 As Forms.Button
    Friend WithEvents Button3 As Forms.Button
    Friend WithEvents Label1 As Forms.Label
    Friend WithEvents Label2 As Forms.Label
    Friend WithEvents Label3 As Forms.Label
    Friend WithEvents Label4 As Forms.Label
    Friend WithEvents Button4 As Forms.Button
    Friend WithEvents Button5 As Forms.Button
    Friend WithEvents Label5 As Forms.Label
    Friend WithEvents Button6 As Button
    Friend WithEvents Label6 As Label
End Class
