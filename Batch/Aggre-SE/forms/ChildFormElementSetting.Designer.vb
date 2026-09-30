Imports System.Windows

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class ChildFormElementSetting
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ChildFormElementSetting))
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.fpLimitLevel_type = New FarPoint.Win.Spread.FpSpread(FarPoint.Win.Spread.LegacyBehaviors.None, CType(resources.GetObject("GroupBox1.Controls"), Object))
        Me.Label4 = New System.Windows.Forms.Label()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.fpLimitLevel = New FarPoint.Win.Spread.FpSpread(FarPoint.Win.Spread.LegacyBehaviors.None, resources.GetObject("resource1"))
        Me.fpLimitLevel_Sheet1 = Me.fpLimitLevel.GetSheet(0)
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.MyPictureBox1 = New Aggre.MyPictureBox()
        Me.fpLimitLevel_type_Sheet1 = Me.fpLimitLevel_type.GetSheet(0)
        Me.GroupBox1.SuspendLayout()
        CType(Me.fpLimitLevel_type, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox4.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        CType(Me.fpLimitLevel, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox3.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.fpLimitLevel_type)
        Me.GroupBox1.Controls.Add(Me.Label4)
        Me.GroupBox1.Controls.Add(Me.GroupBox4)
        Me.GroupBox1.Controls.Add(Me.Label2)
        Me.GroupBox1.Controls.Add(Me.Label1)
        Me.GroupBox1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.GroupBox1.Location = New System.Drawing.Point(12, 177)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(612, 342)
        Me.GroupBox1.TabIndex = 0
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "[ 矩形・Ｔ形 断面一覧 ]"
        '
        'fpLimitLevel_type
        '
        Me.fpLimitLevel_type.AccessibleDescription = "fpLimitLevel_type, Sheet1, Row 0, Column 0"
        Me.fpLimitLevel_type.Font = New System.Drawing.Font("ＭＳ Ｐゴシック", 11.0!)
        Me.fpLimitLevel_type.Location = New System.Drawing.Point(14, 18)
        Me.fpLimitLevel_type.Name = "fpLimitLevel_type"
        Me.fpLimitLevel_type.Size = New System.Drawing.Size(580, 245)
        Me.fpLimitLevel_type.TabIndex = 5
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label4.Location = New System.Drawing.Point(21, 321)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(178, 12)
        Me.Label4.TabIndex = 3
        Me.Label4.Text = "②逆対象曲げ【耐震照査の手引き】"
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.Label8)
        Me.GroupBox4.Controls.Add(Me.Label5)
        Me.GroupBox4.Controls.Add(Me.Label7)
        Me.GroupBox4.Location = New System.Drawing.Point(205, 269)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(389, 67)
        Me.GroupBox4.TabIndex = 4
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "各社 仕様書"
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label8.Location = New System.Drawing.Point(192, 42)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(181, 12)
        Me.Label8.TabIndex = 3
        Me.Label8.Text = "⑤整備新幹線【設計内規】　柱部材"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label5.Location = New System.Drawing.Point(6, 20)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(171, 12)
        Me.Label5.TabIndex = 3
        Me.Label5.Text = "③JR東日本【RCマニュアル7.2.3.2】"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label7.Location = New System.Drawing.Point(192, 20)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(181, 12)
        Me.Label7.TabIndex = 3
        Me.Label7.Text = "④整備新幹線【設計内規】　梁部材"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label2.Location = New System.Drawing.Point(21, 298)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(149, 12)
        Me.Label2.TabIndex = 3
        Me.Label2.Text = "①標準せん断耐力【RC標準】"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label1.Location = New System.Drawing.Point(12, 269)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(107, 12)
        Me.Label1.TabIndex = 3
        Me.Label1.Text = "せん断耐力の算定式"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.fpLimitLevel)
        Me.GroupBox2.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.GroupBox2.Location = New System.Drawing.Point(12, 12)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(612, 159)
        Me.GroupBox2.TabIndex = 0
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "[ 部材照査タイプ ]"
        '
        'fpLimitLevel
        '
        Me.fpLimitLevel.AccessibleDescription = "fpLimitLevel, Sheet1, Row 0, Column 0"
        Me.fpLimitLevel.Font = New System.Drawing.Font("ＭＳ Ｐゴシック", 11.0!)
        Me.fpLimitLevel.Location = New System.Drawing.Point(14, 18)
        Me.fpLimitLevel.Name = "fpLimitLevel"
        Me.fpLimitLevel.Size = New System.Drawing.Size(580, 135)
        Me.fpLimitLevel.TabIndex = 2
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.MyPictureBox1)
        Me.GroupBox3.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.GroupBox3.Location = New System.Drawing.Point(630, 12)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(357, 507)
        Me.GroupBox3.TabIndex = 0
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "[ 対象断面位置 ]"
        '
        'MyPictureBox1
        '
        Me.MyPictureBox1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.MyPictureBox1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.MyPictureBox1.Location = New System.Drawing.Point(3, 15)
        Me.MyPictureBox1.Name = "MyPictureBox1"
        Me.MyPictureBox1.Size = New System.Drawing.Size(351, 489)
        Me.MyPictureBox1.TabIndex = 0
        Me.MyPictureBox1.TabStop = False
        '
        'ChildFormElementSetting
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.ClientSize = New System.Drawing.Size(1011, 531)
        Me.ControlBox = False
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox1)
        Me.DoubleBuffered = True
        Me.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "ChildFormElementSetting"
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        CType(Me.fpLimitLevel_type, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        CType(Me.fpLimitLevel, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox3.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents GroupBox1 As Forms.GroupBox
    Friend WithEvents GroupBox2 As Forms.GroupBox
    Friend WithEvents GroupBox3 As Forms.GroupBox
    Friend WithEvents MyPictureBox1 As Aggre.MyPictureBox
    Friend WithEvents Label7 As Forms.Label
    Friend WithEvents Label4 As Forms.Label
    Friend WithEvents Label5 As Forms.Label
    Friend WithEvents Label2 As Forms.Label
    Friend WithEvents Label1 As Forms.Label
    Friend WithEvents Label8 As Forms.Label
    Friend WithEvents GroupBox4 As GroupBox
    Friend WithEvents fpLimitLevel As FarPoint.Win.Spread.FpSpread
    Friend WithEvents fpLimitLevel_type As FarPoint.Win.Spread.FpSpread
    Friend WithEvents fpLimitLevel_Sheet1 As FarPoint.Win.Spread.SheetView
    Friend WithEvents fpLimitLevel_type_Sheet1 As FarPoint.Win.Spread.SheetView
End Class
