<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ChildFormRequirement
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ChildFormRequirement))
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.rbspec1 = New System.Windows.Forms.RadioButton()
        Me.rbspec3 = New System.Windows.Forms.RadioButton()
        Me.rbspec2 = New System.Windows.Forms.RadioButton()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.rbChiikiC = New System.Windows.Forms.RadioButton()
        Me.rbChiikiB = New System.Windows.Forms.RadioButton()
        Me.rbChiikiA = New System.Windows.Forms.RadioButton()
        Me.GroupBox5 = New System.Windows.Forms.GroupBox()
        Me.rbJiban5 = New System.Windows.Forms.RadioButton()
        Me.rbJiban4 = New System.Windows.Forms.RadioButton()
        Me.rbJiban3 = New System.Windows.Forms.RadioButton()
        Me.rbJiban2 = New System.Windows.Forms.RadioButton()
        Me.rbJiban1 = New System.Windows.Forms.RadioButton()
        Me.rbJiban0 = New System.Windows.Forms.RadioButton()
        Me.rbJiban9 = New System.Windows.Forms.RadioButton()
        Me.GroupBox6 = New System.Windows.Forms.GroupBox()
        Me.fpEkijoCase = New FarPoint.Win.Spread.FpSpread(FarPoint.Win.Spread.LegacyBehaviors.None, CType(resources.GetObject("GroupBox6.Controls"), Object))
        Me.fpEkijoCase_Sheet1 = Me.fpEkijoCase.GetSheet(0)
        Me.rbEkijo20 = New System.Windows.Forms.RadioButton()
        Me.rbEkijo5 = New System.Windows.Forms.RadioButton()
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.tbAtPoint = New System.Windows.Forms.TextBox()
        Me.GroupBox10 = New System.Windows.Forms.GroupBox()
        Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel()
        Me.MyPictureBox1 = New Aggre.MyPictureBox()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.cbM65Area = New System.Windows.Forms.ComboBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.tbAlfa = New System.Windows.Forms.TextBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.cbηx = New System.Windows.Forms.CheckBox()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.tbL2ηx = New System.Windows.Forms.TextBox()
        Me.tbL1ηx = New System.Windows.Forms.TextBox()
        Me.GroupBox3.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        Me.GroupBox5.SuspendLayout()
        Me.GroupBox6.SuspendLayout()
        CType(Me.fpEkijoCase, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel1.SuspendLayout()
        Me.GroupBox10.SuspendLayout()
        Me.TableLayoutPanel1.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.rbspec1)
        Me.GroupBox3.Controls.Add(Me.rbspec3)
        Me.GroupBox3.Controls.Add(Me.rbspec2)
        Me.GroupBox3.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.GroupBox3.Location = New System.Drawing.Point(352, 368)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(164, 97)
        Me.GroupBox3.TabIndex = 2
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "スペクトル"
        '
        'rbspec1
        '
        Me.rbspec1.AutoSize = True
        Me.rbspec1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbspec1.Location = New System.Drawing.Point(6, 39)
        Me.rbspec1.Name = "rbspec1"
        Me.rbspec1.Size = New System.Drawing.Size(80, 16)
        Me.rbspec1.TabIndex = 14
        Me.rbspec1.TabStop = True
        Me.rbspec1.Text = "スペクトルⅠ"
        Me.rbspec1.UseVisualStyleBackColor = True
        '
        'rbspec3
        '
        Me.rbspec3.AutoSize = True
        Me.rbspec3.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbspec3.Location = New System.Drawing.Point(6, 61)
        Me.rbspec3.Name = "rbspec3"
        Me.rbspec3.Size = New System.Drawing.Size(80, 16)
        Me.rbspec3.TabIndex = 15
        Me.rbspec3.TabStop = True
        Me.rbspec3.Text = "スペクトルⅡ"
        Me.rbspec3.UseVisualStyleBackColor = True
        '
        'rbspec2
        '
        Me.rbspec2.AutoSize = True
        Me.rbspec2.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbspec2.Location = New System.Drawing.Point(6, 17)
        Me.rbspec2.Name = "rbspec2"
        Me.rbspec2.Size = New System.Drawing.Size(122, 16)
        Me.rbspec2.TabIndex = 13
        Me.rbspec2.TabStop = True
        Me.rbspec2.Text = "スペクトルⅠ,Ⅱ 両方"
        Me.rbspec2.UseVisualStyleBackColor = True
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.rbChiikiC)
        Me.GroupBox4.Controls.Add(Me.rbChiikiB)
        Me.GroupBox4.Controls.Add(Me.rbChiikiA)
        Me.GroupBox4.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.GroupBox4.ForeColor = System.Drawing.Color.Black
        Me.GroupBox4.Location = New System.Drawing.Point(352, 21)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(164, 81)
        Me.GroupBox4.TabIndex = 0
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "[ 地域別 ]"
        '
        'rbChiikiC
        '
        Me.rbChiikiC.AutoSize = True
        Me.rbChiikiC.BackColor = System.Drawing.Color.Transparent
        Me.rbChiikiC.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbChiikiC.Location = New System.Drawing.Point(6, 57)
        Me.rbChiikiC.Name = "rbChiikiC"
        Me.rbChiikiC.Size = New System.Drawing.Size(56, 16)
        Me.rbChiikiC.TabIndex = 4
        Me.rbChiikiC.TabStop = True
        Me.rbChiikiC.Text = "Ｃ地域"
        Me.rbChiikiC.UseVisualStyleBackColor = False
        '
        'rbChiikiB
        '
        Me.rbChiikiB.AutoSize = True
        Me.rbChiikiB.BackColor = System.Drawing.Color.Transparent
        Me.rbChiikiB.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbChiikiB.Location = New System.Drawing.Point(6, 36)
        Me.rbChiikiB.Name = "rbChiikiB"
        Me.rbChiikiB.Size = New System.Drawing.Size(56, 16)
        Me.rbChiikiB.TabIndex = 3
        Me.rbChiikiB.TabStop = True
        Me.rbChiikiB.Text = "Ｂ地域"
        Me.rbChiikiB.UseVisualStyleBackColor = False
        '
        'rbChiikiA
        '
        Me.rbChiikiA.AutoSize = True
        Me.rbChiikiA.BackColor = System.Drawing.Color.Transparent
        Me.rbChiikiA.Checked = True
        Me.rbChiikiA.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbChiikiA.Location = New System.Drawing.Point(6, 16)
        Me.rbChiikiA.Name = "rbChiikiA"
        Me.rbChiikiA.Size = New System.Drawing.Size(55, 16)
        Me.rbChiikiA.TabIndex = 2
        Me.rbChiikiA.TabStop = True
        Me.rbChiikiA.Text = "A地域"
        Me.rbChiikiA.UseVisualStyleBackColor = False
        '
        'GroupBox5
        '
        Me.GroupBox5.Controls.Add(Me.rbJiban5)
        Me.GroupBox5.Controls.Add(Me.rbJiban4)
        Me.GroupBox5.Controls.Add(Me.rbJiban3)
        Me.GroupBox5.Controls.Add(Me.rbJiban2)
        Me.GroupBox5.Controls.Add(Me.rbJiban1)
        Me.GroupBox5.Controls.Add(Me.rbJiban0)
        Me.GroupBox5.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.GroupBox5.Location = New System.Drawing.Point(351, 108)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Size = New System.Drawing.Size(164, 80)
        Me.GroupBox5.TabIndex = 0
        Me.GroupBox5.TabStop = False
        Me.GroupBox5.Text = "[ 地盤区分 ]"
        '
        'rbJiban5
        '
        Me.rbJiban5.AutoSize = True
        Me.rbJiban5.BackColor = System.Drawing.Color.Transparent
        Me.rbJiban5.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbJiban5.Location = New System.Drawing.Point(85, 54)
        Me.rbJiban5.Name = "rbJiban5"
        Me.rbJiban5.Size = New System.Drawing.Size(67, 16)
        Me.rbJiban5.TabIndex = 10
        Me.rbJiban5.TabStop = True
        Me.rbJiban5.Text = "G５ 地盤"
        Me.rbJiban5.UseVisualStyleBackColor = False
        '
        'rbJiban4
        '
        Me.rbJiban4.AutoSize = True
        Me.rbJiban4.BackColor = System.Drawing.Color.Transparent
        Me.rbJiban4.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbJiban4.Location = New System.Drawing.Point(85, 34)
        Me.rbJiban4.Name = "rbJiban4"
        Me.rbJiban4.Size = New System.Drawing.Size(67, 16)
        Me.rbJiban4.TabIndex = 9
        Me.rbJiban4.TabStop = True
        Me.rbJiban4.Text = "G４ 地盤"
        Me.rbJiban4.UseVisualStyleBackColor = False
        '
        'rbJiban3
        '
        Me.rbJiban3.AutoSize = True
        Me.rbJiban3.BackColor = System.Drawing.Color.Transparent
        Me.rbJiban3.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbJiban3.Location = New System.Drawing.Point(85, 15)
        Me.rbJiban3.Name = "rbJiban3"
        Me.rbJiban3.Size = New System.Drawing.Size(67, 16)
        Me.rbJiban3.TabIndex = 8
        Me.rbJiban3.TabStop = True
        Me.rbJiban3.Text = "G３ 地盤"
        Me.rbJiban3.UseVisualStyleBackColor = False
        '
        'rbJiban2
        '
        Me.rbJiban2.AutoSize = True
        Me.rbJiban2.BackColor = System.Drawing.Color.Transparent
        Me.rbJiban2.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbJiban2.Location = New System.Drawing.Point(6, 55)
        Me.rbJiban2.Name = "rbJiban2"
        Me.rbJiban2.Size = New System.Drawing.Size(67, 16)
        Me.rbJiban2.TabIndex = 7
        Me.rbJiban2.TabStop = True
        Me.rbJiban2.Text = "G２ 地盤"
        Me.rbJiban2.UseVisualStyleBackColor = False
        '
        'rbJiban1
        '
        Me.rbJiban1.AutoSize = True
        Me.rbJiban1.BackColor = System.Drawing.Color.Transparent
        Me.rbJiban1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbJiban1.Location = New System.Drawing.Point(6, 35)
        Me.rbJiban1.Name = "rbJiban1"
        Me.rbJiban1.Size = New System.Drawing.Size(67, 16)
        Me.rbJiban1.TabIndex = 6
        Me.rbJiban1.TabStop = True
        Me.rbJiban1.Text = "G１ 地盤"
        Me.rbJiban1.UseVisualStyleBackColor = False
        '
        'rbJiban0
        '
        Me.rbJiban0.AutoSize = True
        Me.rbJiban0.BackColor = System.Drawing.Color.Transparent
        Me.rbJiban0.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbJiban0.Location = New System.Drawing.Point(6, 16)
        Me.rbJiban0.Name = "rbJiban0"
        Me.rbJiban0.Size = New System.Drawing.Size(67, 16)
        Me.rbJiban0.TabIndex = 5
        Me.rbJiban0.TabStop = True
        Me.rbJiban0.Text = "G０ 地盤"
        Me.rbJiban0.UseVisualStyleBackColor = False
        '
        'rbJiban9
        '
        Me.rbJiban9.AutoSize = True
        Me.rbJiban9.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbJiban9.Location = New System.Drawing.Point(357, 477)
        Me.rbJiban9.Name = "rbJiban9"
        Me.rbJiban9.Size = New System.Drawing.Size(141, 16)
        Me.rbJiban9.TabIndex = 10
        Me.rbJiban9.TabStop = True
        Me.rbJiban9.Text = "応答塑性率の直接入力"
        Me.rbJiban9.UseVisualStyleBackColor = True
        '
        'GroupBox6
        '
        Me.GroupBox6.Controls.Add(Me.fpEkijoCase)
        Me.GroupBox6.Controls.Add(Me.rbEkijo20)
        Me.GroupBox6.Controls.Add(Me.rbEkijo5)
        Me.GroupBox6.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.GroupBox6.ForeColor = System.Drawing.Color.Black
        Me.GroupBox6.Location = New System.Drawing.Point(538, 12)
        Me.GroupBox6.Name = "GroupBox6"
        Me.GroupBox6.Size = New System.Drawing.Size(425, 378)
        Me.GroupBox6.TabIndex = 0
        Me.GroupBox6.TabStop = False
        Me.GroupBox6.Text = "[ 液状化指数PL ]"
        '
        'fpEkijoCase
        '
        Me.fpEkijoCase.AccessibleDescription = "FpSpread1, Sheet1, Row 0, Column 0"
        Me.fpEkijoCase.Font = New System.Drawing.Font("ＭＳ Ｐゴシック", 11.0!)
        Me.fpEkijoCase.Location = New System.Drawing.Point(21, 20)
        Me.fpEkijoCase.Name = "fpEkijoCase"
        Me.fpEkijoCase.Size = New System.Drawing.Size(398, 330)
        Me.fpEkijoCase.TabIndex = 18
        '
        'fpEkijoCase_Sheet1
        '
        '
        'rbEkijo20
        '
        Me.rbEkijo20.AutoSize = True
        Me.rbEkijo20.Checked = True
        Me.rbEkijo20.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbEkijo20.Location = New System.Drawing.Point(109, 356)
        Me.rbEkijo20.Name = "rbEkijo20"
        Me.rbEkijo20.Size = New System.Drawing.Size(54, 16)
        Me.rbEkijo20.TabIndex = 16
        Me.rbEkijo20.TabStop = True
        Me.rbEkijo20.Text = "20<PL"
        Me.rbEkijo20.UseVisualStyleBackColor = True
        '
        'rbEkijo5
        '
        Me.rbEkijo5.AutoSize = True
        Me.rbEkijo5.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.rbEkijo5.Location = New System.Drawing.Point(22, 356)
        Me.rbEkijo5.Name = "rbEkijo5"
        Me.rbEkijo5.Size = New System.Drawing.Size(66, 16)
        Me.rbEkijo5.TabIndex = 15
        Me.rbEkijo5.TabStop = True
        Me.rbEkijo5.Text = "5<PL<20"
        Me.rbEkijo5.UseVisualStyleBackColor = True
        '
        'Panel1
        '
        Me.Panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel1.Controls.Add(Me.Label3)
        Me.Panel1.Controls.Add(Me.Label1)
        Me.Panel1.Controls.Add(Me.tbAtPoint)
        Me.Panel1.Location = New System.Drawing.Point(3, 3)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(315, 47)
        Me.Panel1.TabIndex = 19
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label3.Location = New System.Drawing.Point(10, 6)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(77, 12)
        Me.Label3.TabIndex = 0
        Me.Label3.Text = "着目節点番号"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.ForeColor = System.Drawing.Color.Blue
        Me.Label1.Location = New System.Drawing.Point(91, 30)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(0, 12)
        Me.Label1.TabIndex = 21
        '
        'tbAtPoint
        '
        Me.tbAtPoint.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.tbAtPoint.Location = New System.Drawing.Point(93, 3)
        Me.tbAtPoint.Name = "tbAtPoint"
        Me.tbAtPoint.Size = New System.Drawing.Size(81, 19)
        Me.tbAtPoint.TabIndex = 1
        '
        'GroupBox10
        '
        Me.GroupBox10.Controls.Add(Me.TableLayoutPanel1)
        Me.GroupBox10.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.GroupBox10.Location = New System.Drawing.Point(12, 12)
        Me.GroupBox10.Name = "GroupBox10"
        Me.GroupBox10.Size = New System.Drawing.Size(333, 481)
        Me.GroupBox10.TabIndex = 0
        Me.GroupBox10.TabStop = False
        Me.GroupBox10.Text = "[ 着目節点番号 ]"
        '
        'TableLayoutPanel1
        '
        Me.TableLayoutPanel1.ColumnCount = 1
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.Controls.Add(Me.Panel1, 0, 0)
        Me.TableLayoutPanel1.Controls.Add(Me.MyPictureBox1, 0, 1)
        Me.TableLayoutPanel1.Location = New System.Drawing.Point(6, 20)
        Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
        Me.TableLayoutPanel1.RowCount = 2
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.30769!))
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 87.69231!))
        Me.TableLayoutPanel1.Size = New System.Drawing.Size(321, 455)
        Me.TableLayoutPanel1.TabIndex = 0
        '
        'MyPictureBox1
        '
        Me.MyPictureBox1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.MyPictureBox1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.MyPictureBox1.Location = New System.Drawing.Point(3, 58)
        Me.MyPictureBox1.Name = "MyPictureBox1"
        Me.MyPictureBox1.Size = New System.Drawing.Size(315, 394)
        Me.MyPictureBox1.TabIndex = 0
        Me.MyPictureBox1.TabStop = False
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.cbM65Area)
        Me.GroupBox1.Controls.Add(Me.Label5)
        Me.GroupBox1.Controls.Add(Me.Label4)
        Me.GroupBox1.Controls.Add(Me.tbAlfa)
        Me.GroupBox1.Controls.Add(Me.Label2)
        Me.GroupBox1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.GroupBox1.Location = New System.Drawing.Point(353, 207)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(163, 155)
        Me.GroupBox1.TabIndex = 0
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "L2地震動の設定"
        '
        'cbM65Area
        '
        Me.cbM65Area.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.cbM65Area.FormattingEnabled = True
        Me.cbM65Area.Items.AddRange(New Object() {"不明", "あり", "なし"})
        Me.cbM65Area.Location = New System.Drawing.Point(65, 60)
        Me.cbM65Area.Name = "cbM65Area"
        Me.cbM65Area.Size = New System.Drawing.Size(88, 20)
        Me.cbM65Area.TabIndex = 11
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label5.Location = New System.Drawing.Point(6, 94)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(135, 12)
        Me.Label5.TabIndex = 0
        Me.Label5.Text = "設計地震動の低減係数α"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label4.Location = New System.Drawing.Point(6, 43)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(145, 12)
        Me.Label4.TabIndex = 0
        Me.Label4.Text = "M6.5以上の震源エリアの存在"
        '
        'tbAlfa
        '
        Me.tbAlfa.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.tbAlfa.Location = New System.Drawing.Point(65, 112)
        Me.tbAlfa.Name = "tbAlfa"
        Me.tbAlfa.Size = New System.Drawing.Size(88, 19)
        Me.tbAlfa.TabIndex = 12
        Me.tbAlfa.Text = "1.00"
        Me.tbAlfa.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label2.Location = New System.Drawing.Point(52, 15)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(73, 12)
        Me.Label2.TabIndex = 0
        Me.Label2.Text = "(安全性照査)"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.cbηx)
        Me.GroupBox2.Controls.Add(Me.Label7)
        Me.GroupBox2.Controls.Add(Me.Label6)
        Me.GroupBox2.Controls.Add(Me.tbL2ηx)
        Me.GroupBox2.Controls.Add(Me.tbL1ηx)
        Me.GroupBox2.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.GroupBox2.Location = New System.Drawing.Point(538, 396)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(425, 101)
        Me.GroupBox2.TabIndex = 3
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "不整形性の影響を考慮した地震動の補正係数"
        '
        'cbηx
        '
        Me.cbηx.AutoSize = True
        Me.cbηx.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.cbηx.Location = New System.Drawing.Point(21, 18)
        Me.cbηx.Name = "cbηx"
        Me.cbηx.Size = New System.Drawing.Size(158, 16)
        Me.cbηx.TabIndex = 18
        Me.cbηx.Text = "不整形性の影響を考慮する"
        Me.cbηx.UseVisualStyleBackColor = True
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label7.Location = New System.Drawing.Point(36, 69)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(253, 12)
        Me.Label7.TabIndex = 1
        Me.Label7.Text = "Ｌ２地震動　η2(x) = 1 + α1(x1) + α2(x2) ・・・ ="
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label6.Location = New System.Drawing.Point(36, 46)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(253, 12)
        Me.Label6.TabIndex = 1
        Me.Label6.Text = "Ｌ１地震動　η1(x) = 1 + α1(x1) + α2(x2) ・・・ ="
        '
        'tbL2ηx
        '
        Me.tbL2ηx.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.tbL2ηx.Location = New System.Drawing.Point(295, 66)
        Me.tbL2ηx.Name = "tbL2ηx"
        Me.tbL2ηx.Size = New System.Drawing.Size(88, 19)
        Me.tbL2ηx.TabIndex = 20
        Me.tbL2ηx.Text = "1.000"
        Me.tbL2ηx.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'tbL1ηx
        '
        Me.tbL1ηx.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.tbL1ηx.Location = New System.Drawing.Point(295, 39)
        Me.tbL1ηx.Name = "tbL1ηx"
        Me.tbL1ηx.Size = New System.Drawing.Size(88, 19)
        Me.tbL1ηx.TabIndex = 19
        Me.tbL1ηx.Text = "1.000"
        Me.tbL1ηx.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'ChildFormRequirement
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.ClientSize = New System.Drawing.Size(1011, 531)
        Me.ControlBox = False
        Me.Controls.Add(Me.rbJiban9)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.GroupBox10)
        Me.Controls.Add(Me.GroupBox6)
        Me.Controls.Add(Me.GroupBox5)
        Me.Controls.Add(Me.GroupBox4)
        Me.Controls.Add(Me.GroupBox3)
        Me.DoubleBuffered = True
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "ChildFormRequirement"
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        Me.GroupBox5.ResumeLayout(False)
        Me.GroupBox5.PerformLayout()
        Me.GroupBox6.ResumeLayout(False)
        Me.GroupBox6.PerformLayout()
        CType(Me.fpEkijoCase, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        Me.GroupBox10.ResumeLayout(False)
        Me.TableLayoutPanel1.ResumeLayout(False)
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents GroupBox3 As System.Windows.Forms.GroupBox
    Friend WithEvents rbspec1 As System.Windows.Forms.RadioButton
    Friend WithEvents rbspec2 As System.Windows.Forms.RadioButton
    Friend WithEvents GroupBox4 As System.Windows.Forms.GroupBox
    Friend WithEvents rbChiikiC As System.Windows.Forms.RadioButton
    Friend WithEvents rbChiikiB As System.Windows.Forms.RadioButton
    Friend WithEvents rbChiikiA As System.Windows.Forms.RadioButton
    Friend WithEvents GroupBox5 As System.Windows.Forms.GroupBox
    Friend WithEvents rbJiban5 As System.Windows.Forms.RadioButton
    Friend WithEvents rbJiban4 As System.Windows.Forms.RadioButton
    Friend WithEvents rbJiban3 As System.Windows.Forms.RadioButton
    Friend WithEvents rbJiban2 As System.Windows.Forms.RadioButton
    Friend WithEvents rbJiban1 As System.Windows.Forms.RadioButton
    Friend WithEvents rbJiban0 As System.Windows.Forms.RadioButton
    Friend WithEvents GroupBox6 As System.Windows.Forms.GroupBox
    Friend WithEvents rbEkijo20 As System.Windows.Forms.RadioButton
    Friend WithEvents rbEkijo5 As System.Windows.Forms.RadioButton
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents tbAtPoint As System.Windows.Forms.TextBox
    Friend WithEvents GroupBox10 As System.Windows.Forms.GroupBox
    Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents MyPictureBox1 As Aggre.MyPictureBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents tbAlfa As System.Windows.Forms.TextBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents cbM65Area As System.Windows.Forms.ComboBox
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents tbL2ηx As System.Windows.Forms.TextBox
    Friend WithEvents tbL1ηx As System.Windows.Forms.TextBox
    Friend WithEvents cbηx As System.Windows.Forms.CheckBox
    Friend WithEvents rbspec3 As RadioButton
    Friend WithEvents rbJiban9 As RadioButton
    Friend WithEvents fpEkijoCase As FarPoint.Win.Spread.FpSpread
    Friend WithEvents fpEkijoCase_Sheet1 As FarPoint.Win.Spread.SheetView
End Class
