<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ChildFormPileAnchorBar
    Inherits System.Windows.Forms.Form

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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ChildFormPileAnchorBar))
        Me.gbRCPileProperty = New System.Windows.Forms.GroupBox()
        Me.FpPilePropertyGrid = New FarPoint.Win.Spread.FpSpread(FarPoint.Win.Spread.LegacyBehaviors.None, CType(resources.GetObject("gbRCPileProperty.Controls"), Object))
        Me.FpPilePropertyGrid_Sheet1 = Me.FpPilePropertyGrid.GetSheet(0)
        Me.Label1 = New System.Windows.Forms.Label()
        Me.RCPileLengthGroup = New System.Windows.Forms.GroupBox()
        Me.Label17 = New System.Windows.Forms.Label()
        Me.Label18 = New System.Windows.Forms.Label()
        Me.tbDisabledDownwardFromCutOffPoint = New System.Windows.Forms.TextBox()
        Me.Label16 = New System.Windows.Forms.Label()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.Label15 = New System.Windows.Forms.Label()
        Me.tbDisabledUpwardFromCutOffPoint = New System.Windows.Forms.TextBox()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.tbDisabledFromPileHead = New System.Windows.Forms.TextBox()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.tbDesignMarginLength = New System.Windows.Forms.TextBox()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.tbPileMidLength = New System.Windows.Forms.TextBox()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.tbReductionCoefficient = New System.Windows.Forms.TextBox()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.tbParagraphLength = New System.Windows.Forms.TextBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.tbPileHeadLength = New System.Windows.Forms.TextBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.gb = New System.Windows.Forms.GroupBox()
        Me.MyPictureBox1 = New Aggre.MyPictureBox()
        Me.gbMenber = New System.Windows.Forms.GroupBox()
        Me.fpTargetMember = New FarPoint.Win.Spread.FpSpread(FarPoint.Win.Spread.LegacyBehaviors.None, CType(resources.GetObject("gbMenber.Controls"), Object))
        Me.fpTargetMember_Sheet1 = Me.fpTargetMember.GetSheet(0)
        Me.btnReset = New System.Windows.Forms.Button()
        Me.gbRCPileProperty.SuspendLayout()
        CType(Me.FpPilePropertyGrid, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.RCPileLengthGroup.SuspendLayout()
        Me.gb.SuspendLayout()
        Me.gbMenber.SuspendLayout()
        CType(Me.fpTargetMember, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'gbRCPileProperty
        '
        Me.gbRCPileProperty.Controls.Add(Me.FpPilePropertyGrid)
        Me.gbRCPileProperty.Controls.Add(Me.Label1)
        Me.gbRCPileProperty.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.gbRCPileProperty.Location = New System.Drawing.Point(351, 51)
        Me.gbRCPileProperty.Name = "gbRCPileProperty"
        Me.gbRCPileProperty.Size = New System.Drawing.Size(401, 178)
        Me.gbRCPileProperty.TabIndex = 0
        Me.gbRCPileProperty.TabStop = False
        Me.gbRCPileProperty.Text = "RC円形断面緒元"
        '
        'FpPilePropertyGrid
        '
        Me.FpPilePropertyGrid.AccessibleDescription = "Book1, Sheet1, Row 0, Column 0"
        Me.FpPilePropertyGrid.Font = New System.Drawing.Font("ＭＳ Ｐゴシック", 11.0!)
        Me.FpPilePropertyGrid.Location = New System.Drawing.Point(9, 15)
        Me.FpPilePropertyGrid.Name = "FpPilePropertyGrid"
        Me.FpPilePropertyGrid.Size = New System.Drawing.Size(380, 152)
        Me.FpPilePropertyGrid.TabIndex = 3
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label1.Location = New System.Drawing.Point(292, 155)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(96, 12)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "←段落し後の断面"
        '
        'RCPileLengthGroup
        '
        Me.RCPileLengthGroup.Controls.Add(Me.Label17)
        Me.RCPileLengthGroup.Controls.Add(Me.Label18)
        Me.RCPileLengthGroup.Controls.Add(Me.tbDisabledDownwardFromCutOffPoint)
        Me.RCPileLengthGroup.Controls.Add(Me.Label16)
        Me.RCPileLengthGroup.Controls.Add(Me.Label14)
        Me.RCPileLengthGroup.Controls.Add(Me.Label15)
        Me.RCPileLengthGroup.Controls.Add(Me.tbDisabledUpwardFromCutOffPoint)
        Me.RCPileLengthGroup.Controls.Add(Me.Label13)
        Me.RCPileLengthGroup.Controls.Add(Me.Label11)
        Me.RCPileLengthGroup.Controls.Add(Me.tbDisabledFromPileHead)
        Me.RCPileLengthGroup.Controls.Add(Me.Label12)
        Me.RCPileLengthGroup.Controls.Add(Me.Label9)
        Me.RCPileLengthGroup.Controls.Add(Me.tbDesignMarginLength)
        Me.RCPileLengthGroup.Controls.Add(Me.Label10)
        Me.RCPileLengthGroup.Controls.Add(Me.Label6)
        Me.RCPileLengthGroup.Controls.Add(Me.tbPileMidLength)
        Me.RCPileLengthGroup.Controls.Add(Me.Label8)
        Me.RCPileLengthGroup.Controls.Add(Me.tbReductionCoefficient)
        Me.RCPileLengthGroup.Controls.Add(Me.Label7)
        Me.RCPileLengthGroup.Controls.Add(Me.Label5)
        Me.RCPileLengthGroup.Controls.Add(Me.Label4)
        Me.RCPileLengthGroup.Controls.Add(Me.tbParagraphLength)
        Me.RCPileLengthGroup.Controls.Add(Me.Label3)
        Me.RCPileLengthGroup.Controls.Add(Me.tbPileHeadLength)
        Me.RCPileLengthGroup.Controls.Add(Me.Label2)
        Me.RCPileLengthGroup.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.RCPileLengthGroup.Location = New System.Drawing.Point(775, 51)
        Me.RCPileLengthGroup.Name = "RCPileLengthGroup"
        Me.RCPileLengthGroup.Size = New System.Drawing.Size(224, 422)
        Me.RCPileLengthGroup.TabIndex = 0
        Me.RCPileLengthGroup.TabStop = False
        Me.RCPileLengthGroup.Text = "RC円形断面 定着長"
        '
        'Label17
        '
        Me.Label17.AutoSize = True
        Me.Label17.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label17.Location = New System.Drawing.Point(58, 305)
        Me.Label17.Name = "Label17"
        Me.Label17.Size = New System.Drawing.Size(38, 12)
        Me.Label17.TabIndex = 0
        Me.Label17.Text = "下方に"
        '
        'Label18
        '
        Me.Label18.AutoSize = True
        Me.Label18.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label18.Location = New System.Drawing.Point(163, 304)
        Me.Label18.Name = "Label18"
        Me.Label18.Size = New System.Drawing.Size(23, 12)
        Me.Label18.TabIndex = 0
        Me.Label18.Text = "mm"
        '
        'tbDisabledDownwardFromCutOffPoint
        '
        Me.tbDisabledDownwardFromCutOffPoint.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.tbDisabledDownwardFromCutOffPoint.Location = New System.Drawing.Point(107, 301)
        Me.tbDisabledDownwardFromCutOffPoint.Name = "tbDisabledDownwardFromCutOffPoint"
        Me.tbDisabledDownwardFromCutOffPoint.Size = New System.Drawing.Size(50, 19)
        Me.tbDisabledDownwardFromCutOffPoint.TabIndex = 11
        '
        'Label16
        '
        Me.Label16.AutoSize = True
        Me.Label16.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label16.Location = New System.Drawing.Point(58, 279)
        Me.Label16.Name = "Label16"
        Me.Label16.Size = New System.Drawing.Size(38, 12)
        Me.Label16.TabIndex = 0
        Me.Label16.Text = "上方に"
        '
        'Label14
        '
        Me.Label14.AutoSize = True
        Me.Label14.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label14.Location = New System.Drawing.Point(43, 251)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(82, 12)
        Me.Label14.TabIndex = 0
        Me.Label14.Text = "・カットオフ点から"
        '
        'Label15
        '
        Me.Label15.AutoSize = True
        Me.Label15.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label15.Location = New System.Drawing.Point(163, 278)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(23, 12)
        Me.Label15.TabIndex = 0
        Me.Label15.Text = "mm"
        '
        'tbDisabledUpwardFromCutOffPoint
        '
        Me.tbDisabledUpwardFromCutOffPoint.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.tbDisabledUpwardFromCutOffPoint.Location = New System.Drawing.Point(107, 275)
        Me.tbDisabledUpwardFromCutOffPoint.Name = "tbDisabledUpwardFromCutOffPoint"
        Me.tbDisabledUpwardFromCutOffPoint.Size = New System.Drawing.Size(50, 19)
        Me.tbDisabledUpwardFromCutOffPoint.TabIndex = 10
        '
        'Label13
        '
        Me.Label13.AutoSize = True
        Me.Label13.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label13.Location = New System.Drawing.Point(43, 223)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(53, 12)
        Me.Label13.TabIndex = 0
        Me.Label13.Text = "・杭頭から"
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label11.Location = New System.Drawing.Point(163, 223)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(23, 12)
        Me.Label11.TabIndex = 0
        Me.Label11.Text = "mm"
        '
        'tbDisabledFromPileHead
        '
        Me.tbDisabledFromPileHead.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.tbDisabledFromPileHead.Location = New System.Drawing.Point(107, 220)
        Me.tbDisabledFromPileHead.Name = "tbDisabledFromPileHead"
        Me.tbDisabledFromPileHead.Size = New System.Drawing.Size(50, 19)
        Me.tbDisabledFromPileHead.TabIndex = 9
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label12.Location = New System.Drawing.Point(16, 194)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(114, 12)
        Me.Label12.TabIndex = 0
        Me.Label12.Text = "継ぎ手を設けない範囲"
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Enabled = False
        Me.Label9.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label9.Location = New System.Drawing.Point(163, 162)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(23, 12)
        Me.Label9.TabIndex = 0
        Me.Label9.Text = "mm"
        '
        'tbDesignMarginLength
        '
        Me.tbDesignMarginLength.Enabled = False
        Me.tbDesignMarginLength.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.tbDesignMarginLength.Location = New System.Drawing.Point(107, 159)
        Me.tbDesignMarginLength.Name = "tbDesignMarginLength"
        Me.tbDesignMarginLength.Size = New System.Drawing.Size(50, 19)
        Me.tbDesignMarginLength.TabIndex = 8
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Enabled = False
        Me.Label10.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label10.Location = New System.Drawing.Point(16, 162)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(65, 12)
        Me.Label10.TabIndex = 0
        Me.Label10.Text = "設計余裕長"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label6.Location = New System.Drawing.Point(163, 131)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(23, 12)
        Me.Label6.TabIndex = 0
        Me.Label6.Text = "mm"
        '
        'tbPileMidLength
        '
        Me.tbPileMidLength.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.tbPileMidLength.Location = New System.Drawing.Point(107, 128)
        Me.tbPileMidLength.Name = "tbPileMidLength"
        Me.tbPileMidLength.Size = New System.Drawing.Size(50, 19)
        Me.tbPileMidLength.TabIndex = 7
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label8.Location = New System.Drawing.Point(16, 131)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(74, 12)
        Me.Label8.TabIndex = 0
        Me.Label8.Text = "段落し定着長"
        '
        'tbReductionCoefficient
        '
        Me.tbReductionCoefficient.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.tbReductionCoefficient.Location = New System.Drawing.Point(107, 96)
        Me.tbReductionCoefficient.Name = "tbReductionCoefficient"
        Me.tbReductionCoefficient.Size = New System.Drawing.Size(50, 19)
        Me.tbReductionCoefficient.TabIndex = 6
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label7.Location = New System.Drawing.Point(16, 99)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(77, 12)
        Me.Label7.TabIndex = 0
        Me.Label7.Text = "継手低減係数"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label5.Location = New System.Drawing.Point(163, 70)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(23, 12)
        Me.Label5.TabIndex = 0
        Me.Label5.Text = "mm"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label4.Location = New System.Drawing.Point(163, 39)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(23, 12)
        Me.Label4.TabIndex = 0
        Me.Label4.Text = "mm"
        '
        'tbParagraphLength
        '
        Me.tbParagraphLength.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.tbParagraphLength.Location = New System.Drawing.Point(107, 67)
        Me.tbParagraphLength.Name = "tbParagraphLength"
        Me.tbParagraphLength.Size = New System.Drawing.Size(50, 19)
        Me.tbParagraphLength.TabIndex = 5
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label3.Location = New System.Drawing.Point(16, 70)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(50, 12)
        Me.Label3.TabIndex = 0
        Me.Label3.Text = "段落し長"
        '
        'tbPileHeadLength
        '
        Me.tbPileHeadLength.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.tbPileHeadLength.Location = New System.Drawing.Point(107, 36)
        Me.tbPileHeadLength.Name = "tbPileHeadLength"
        Me.tbPileHeadLength.Size = New System.Drawing.Size(50, 19)
        Me.tbPileHeadLength.TabIndex = 4
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label2.Location = New System.Drawing.Point(16, 39)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(65, 12)
        Me.Label2.TabIndex = 0
        Me.Label2.Text = "杭頭定着長"
        '
        'gb
        '
        Me.gb.Controls.Add(Me.MyPictureBox1)
        Me.gb.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.gb.Location = New System.Drawing.Point(36, 51)
        Me.gb.Name = "gb"
        Me.gb.Size = New System.Drawing.Size(296, 422)
        Me.gb.TabIndex = 0
        Me.gb.TabStop = False
        Me.gb.Text = "[ 照査位置 ]"
        '
        'MyPictureBox1
        '
        Me.MyPictureBox1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.MyPictureBox1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.MyPictureBox1.Location = New System.Drawing.Point(3, 15)
        Me.MyPictureBox1.Name = "MyPictureBox1"
        Me.MyPictureBox1.Size = New System.Drawing.Size(290, 404)
        Me.MyPictureBox1.TabIndex = 0
        '
        'gbMenber
        '
        Me.gbMenber.Controls.Add(Me.fpTargetMember)
        Me.gbMenber.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.gbMenber.Location = New System.Drawing.Point(351, 245)
        Me.gbMenber.Name = "gbMenber"
        Me.gbMenber.Size = New System.Drawing.Size(323, 228)
        Me.gbMenber.TabIndex = 0
        Me.gbMenber.TabStop = False
        Me.gbMenber.Text = "部材番号"
        '
        'fpTargetMember
        '
        Me.fpTargetMember.AccessibleDescription = "fpTargetMember, Sheet1, Row 0, Column 0"
        Me.fpTargetMember.Font = New System.Drawing.Font("ＭＳ Ｐゴシック", 11.0!)
        Me.fpTargetMember.Location = New System.Drawing.Point(9, 18)
        Me.fpTargetMember.Name = "fpTargetMember"
        Me.fpTargetMember.Size = New System.Drawing.Size(307, 204)
        Me.fpTargetMember.TabIndex = 4
        '
        'fpTargetMember_Sheet1
        '
        '
        'btnReset
        '
        Me.btnReset.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.btnReset.Location = New System.Drawing.Point(680, 356)
        Me.btnReset.Name = "btnReset"
        Me.btnReset.Size = New System.Drawing.Size(67, 23)
        Me.btnReset.TabIndex = 0
        Me.btnReset.TabStop = False
        Me.btnReset.Text = "リセット"
        Me.btnReset.UseVisualStyleBackColor = True
        '
        'ChildFormPileAnchorBar
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.ClientSize = New System.Drawing.Size(1011, 509)
        Me.ControlBox = False
        Me.Controls.Add(Me.btnReset)
        Me.Controls.Add(Me.gbMenber)
        Me.Controls.Add(Me.gb)
        Me.Controls.Add(Me.RCPileLengthGroup)
        Me.Controls.Add(Me.gbRCPileProperty)
        Me.DoubleBuffered = True
        Me.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "ChildFormPileAnchorBar"
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        Me.gbRCPileProperty.ResumeLayout(False)
        Me.gbRCPileProperty.PerformLayout()
        CType(Me.FpPilePropertyGrid, System.ComponentModel.ISupportInitialize).EndInit()
        Me.RCPileLengthGroup.ResumeLayout(False)
        Me.RCPileLengthGroup.PerformLayout()
        Me.gb.ResumeLayout(False)
        Me.gbMenber.ResumeLayout(False)
        CType(Me.fpTargetMember, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents gbRCPileProperty As System.Windows.Forms.GroupBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents RCPileLengthGroup As System.Windows.Forms.GroupBox
    Friend WithEvents tbPileHeadLength As System.Windows.Forms.TextBox
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents tbDesignMarginLength As System.Windows.Forms.TextBox
    Friend WithEvents Label10 As System.Windows.Forms.Label
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents tbPileMidLength As System.Windows.Forms.TextBox
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents tbReductionCoefficient As System.Windows.Forms.TextBox
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents tbParagraphLength As System.Windows.Forms.TextBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Label17 As System.Windows.Forms.Label
    Friend WithEvents Label18 As System.Windows.Forms.Label
    Friend WithEvents tbDisabledDownwardFromCutOffPoint As System.Windows.Forms.TextBox
    Friend WithEvents Label16 As System.Windows.Forms.Label
    Friend WithEvents Label14 As System.Windows.Forms.Label
    Friend WithEvents Label15 As System.Windows.Forms.Label
    Friend WithEvents tbDisabledUpwardFromCutOffPoint As System.Windows.Forms.TextBox
    Friend WithEvents Label13 As System.Windows.Forms.Label
    Friend WithEvents Label11 As System.Windows.Forms.Label
    Friend WithEvents tbDisabledFromPileHead As System.Windows.Forms.TextBox
    Friend WithEvents Label12 As System.Windows.Forms.Label
    Friend WithEvents gb As System.Windows.Forms.GroupBox
    Friend WithEvents MyPictureBox1 As Aggre.MyPictureBox
    Friend WithEvents gbMenber As System.Windows.Forms.GroupBox
    Friend WithEvents btnReset As System.Windows.Forms.Button
    Friend WithEvents FpPilePropertyGrid As FarPoint.Win.Spread.FpSpread
    Friend WithEvents FpPilePropertyGrid_Sheet1 As FarPoint.Win.Spread.SheetView
    Friend WithEvents fpTargetMember As FarPoint.Win.Spread.FpSpread
    Friend WithEvents fpTargetMember_Sheet1 As FarPoint.Win.Spread.SheetView
End Class
