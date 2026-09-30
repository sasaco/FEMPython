<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class ChildFormFoundationSetting
    Inherits System.Windows.Forms.Form

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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ChildFormFoundationSetting))
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle2 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle3 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle4 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle5 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle6 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle7 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Me.a2 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.b2 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.title2 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.limit2 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.cbFoundationType = New System.Windows.Forms.ComboBox()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.GroupBox5 = New System.Windows.Forms.GroupBox()
        Me.fpFoundationList = New FarPoint.Win.Spread.FpSpread(FarPoint.Win.Spread.LegacyBehaviors.None, CType(resources.GetObject("GroupBox5.Controls"), Object))
        Me.fpFoundationList_Sheet1 = Me.fpFoundationList.GetSheet(0)
        Me.fpDisgReactAtPoint = New FarPoint.Win.Spread.FpSpread(FarPoint.Win.Spread.LegacyBehaviors.None, CType(resources.GetObject("GroupBox5.Controls1"), Object))
        Me.fpDisgReactAtPoint_反力照査位置 = Me.fpDisgReactAtPoint.GetSheet(0)
        Me.fpDisgReactAtPoint_変位照査位置 = Me.fpDisgReactAtPoint.GetSheet(1)
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.btnDelete = New System.Windows.Forms.Button()
        Me.btnNew = New System.Windows.Forms.Button()
        Me.btnNext = New System.Windows.Forms.Button()
        Me.btnBack = New System.Windows.Forms.Button()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.着目番号 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.奥行本数 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.基礎形式 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.変位照査 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.反力照査 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.a = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.b = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.title = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.limit = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.fpLimitValue = New FarPoint.Win.Spread.FpSpread(FarPoint.Win.Spread.LegacyBehaviors.None, resources.GetObject("resource1"))
        Me.fpLimitValue_普通 = Me.fpLimitValue.GetSheet(0)
        Me.fpLimitValue_液状化 = Me.fpLimitValue.GetSheet(1)
        Me.fpLimitValue_直接基礎 = Me.fpLimitValue.GetSheet(2)
        Me.MyPictureBox1 = New Aggre.MyPictureBox()
        Me.GroupBox3.SuspendLayout()
        Me.GroupBox5.SuspendLayout()
        CType(Me.fpFoundationList, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.fpDisgReactAtPoint, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel1.SuspendLayout()
        CType(Me.fpLimitValue, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'a2
        '
        Me.a2.HeaderText = ""
        Me.a2.MinimumWidth = 10
        Me.a2.Name = "a2"
        Me.a2.Width = 30
        '
        'b2
        '
        Me.b2.HeaderText = ""
        Me.b2.MinimumWidth = 10
        Me.b2.Name = "b2"
        Me.b2.Width = 50
        '
        'title2
        '
        Me.title2.HeaderText = "照査項目"
        Me.title2.MinimumWidth = 10
        Me.title2.Name = "title2"
        Me.title2.Width = 205
        '
        'limit2
        '
        Me.limit2.HeaderText = "制限値"
        Me.limit2.MinimumWidth = 10
        Me.limit2.Name = "limit2"
        Me.limit2.Width = 200
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label2.Location = New System.Drawing.Point(267, 26)
        Me.Label2.Margin = New System.Windows.Forms.Padding(6, 0, 6, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(0, 12)
        Me.Label2.TabIndex = 41
        '
        'cbFoundationType
        '
        Me.cbFoundationType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbFoundationType.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.cbFoundationType.Items.AddRange(New Object() {"杭基礎", "直接基礎"})
        Me.cbFoundationType.Location = New System.Drawing.Point(12, 23)
        Me.cbFoundationType.Margin = New System.Windows.Forms.Padding(6)
        Me.cbFoundationType.Name = "cbFoundationType"
        Me.cbFoundationType.Size = New System.Drawing.Size(95, 20)
        Me.cbFoundationType.TabIndex = 1
        '
        'Button1
        '
        Me.Button1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Button1.Location = New System.Drawing.Point(115, 18)
        Me.Button1.Margin = New System.Windows.Forms.Padding(6)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(146, 25)
        Me.Button1.TabIndex = 2
        Me.Button1.Text = "連携ファイル選択(任意)"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.MyPictureBox1)
        Me.GroupBox3.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.GroupBox3.Location = New System.Drawing.Point(6, 19)
        Me.GroupBox3.Margin = New System.Windows.Forms.Padding(6)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Padding = New System.Windows.Forms.Padding(6)
        Me.GroupBox3.Size = New System.Drawing.Size(296, 449)
        Me.GroupBox3.TabIndex = 0
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "[ 照査位置 ]"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label4.Location = New System.Drawing.Point(308, 193)
        Me.Label4.Margin = New System.Windows.Forms.Padding(6, 0, 6, 0)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(69, 12)
        Me.Label4.TabIndex = 0
        Me.Label4.Text = "[ 照査位置 ]"
        '
        'GroupBox5
        '
        Me.GroupBox5.Controls.Add(Me.fpFoundationList)
        Me.GroupBox5.Controls.Add(Me.fpDisgReactAtPoint)
        Me.GroupBox5.Controls.Add(Me.Panel1)
        Me.GroupBox5.Controls.Add(Me.Label5)
        Me.GroupBox5.Controls.Add(Me.Label4)
        Me.GroupBox5.Controls.Add(Me.GroupBox3)
        Me.GroupBox5.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.GroupBox5.Location = New System.Drawing.Point(12, 49)
        Me.GroupBox5.Margin = New System.Windows.Forms.Padding(6)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Padding = New System.Windows.Forms.Padding(6)
        Me.GroupBox5.Size = New System.Drawing.Size(617, 478)
        Me.GroupBox5.TabIndex = 0
        Me.GroupBox5.TabStop = False
        Me.GroupBox5.Text = "[ 安定レベルの照査 ]"
        '
        'fpFoundationList
        '
        Me.fpFoundationList.AccessibleDescription = "FpSpread1, Sheet1, Row 0, Column 0"
        Me.fpFoundationList.Font = New System.Drawing.Font("ＭＳ Ｐゴシック", 11.0!)
        Me.fpFoundationList.Location = New System.Drawing.Point(313, 64)
        Me.fpFoundationList.Margin = New System.Windows.Forms.Padding(2, 4, 2, 4)
        Me.fpFoundationList.Name = "fpFoundationList"
        Me.fpFoundationList.Size = New System.Drawing.Size(298, 114)
        Me.fpFoundationList.TabIndex = 11
        '
        'fpDisgReactAtPoint
        '
        Me.fpDisgReactAtPoint.AccessibleDescription = "fpDisgReactAtPoint, 反力照査位置, Row 0, Column 0"
        Me.fpDisgReactAtPoint.Font = New System.Drawing.Font("ＭＳ Ｐゴシック", 11.0!)
        Me.fpDisgReactAtPoint.Location = New System.Drawing.Point(313, 208)
        Me.fpDisgReactAtPoint.Margin = New System.Windows.Forms.Padding(6)
        Me.fpDisgReactAtPoint.Name = "fpDisgReactAtPoint"
        Me.fpDisgReactAtPoint.Size = New System.Drawing.Size(298, 255)
        Me.fpDisgReactAtPoint.TabIndex = 10
        '
        'Panel1
        '
        Me.Panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Panel1.Controls.Add(Me.btnDelete)
        Me.Panel1.Controls.Add(Me.btnNew)
        Me.Panel1.Controls.Add(Me.btnNext)
        Me.Panel1.Controls.Add(Me.btnBack)
        Me.Panel1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Panel1.Location = New System.Drawing.Point(308, 30)
        Me.Panel1.Margin = New System.Windows.Forms.Padding(6)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(281, 28)
        Me.Panel1.TabIndex = 0
        '
        'btnDelete
        '
        Me.btnDelete.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.btnDelete.Location = New System.Drawing.Point(212, 3)
        Me.btnDelete.Margin = New System.Windows.Forms.Padding(6)
        Me.btnDelete.Name = "btnDelete"
        Me.btnDelete.Size = New System.Drawing.Size(62, 23)
        Me.btnDelete.TabIndex = 6
        Me.btnDelete.Text = "Delete×"
        Me.btnDelete.UseVisualStyleBackColor = True
        '
        'btnNew
        '
        Me.btnNew.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.btnNew.Location = New System.Drawing.Point(3, 3)
        Me.btnNew.Margin = New System.Windows.Forms.Padding(6)
        Me.btnNew.Name = "btnNew"
        Me.btnNew.Size = New System.Drawing.Size(62, 23)
        Me.btnNew.TabIndex = 3
        Me.btnNew.Text = "* New"
        Me.btnNew.UseVisualStyleBackColor = True
        '
        'btnNext
        '
        Me.btnNext.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.btnNext.Location = New System.Drawing.Point(139, 3)
        Me.btnNext.Margin = New System.Windows.Forms.Padding(6)
        Me.btnNext.Name = "btnNext"
        Me.btnNext.Size = New System.Drawing.Size(62, 23)
        Me.btnNext.TabIndex = 5
        Me.btnNext.Text = "Next >"
        Me.btnNext.UseVisualStyleBackColor = True
        '
        'btnBack
        '
        Me.btnBack.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.btnBack.Location = New System.Drawing.Point(71, 3)
        Me.btnBack.Margin = New System.Windows.Forms.Padding(6)
        Me.btnBack.Name = "btnBack"
        Me.btnBack.Size = New System.Drawing.Size(62, 23)
        Me.btnBack.TabIndex = 4
        Me.btnBack.Text = "< Back"
        Me.btnBack.UseVisualStyleBackColor = True
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label5.Location = New System.Drawing.Point(308, 15)
        Me.Label5.Margin = New System.Windows.Forms.Padding(6, 0, 6, 0)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(93, 12)
        Me.Label5.TabIndex = 0
        Me.Label5.Text = "[ 照査位置一覧 ]"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label1.Location = New System.Drawing.Point(633, 53)
        Me.Label1.Margin = New System.Windows.Forms.Padding(6, 0, 6, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(57, 12)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "[ 制限値 ]"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label6.Location = New System.Drawing.Point(652, 390)
        Me.Label6.Margin = New System.Windows.Forms.Padding(6, 0, 6, 0)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(213, 12)
        Me.Label6.TabIndex = 0
        Me.Label6.Text = "注） 空白の入力項目は自動で計算します。"
        '
        '着目番号
        '
        DataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight
        Me.着目番号.DefaultCellStyle = DataGridViewCellStyle1
        Me.着目番号.HeaderText = "要素番号"
        Me.着目番号.MinimumWidth = 10
        Me.着目番号.Name = "着目番号"
        Me.着目番号.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.着目番号.Width = 120
        '
        '奥行本数
        '
        DataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight
        Me.奥行本数.DefaultCellStyle = DataGridViewCellStyle2
        Me.奥行本数.HeaderText = "奥行本数"
        Me.奥行本数.MinimumWidth = 10
        Me.奥行本数.Name = "奥行本数"
        Me.奥行本数.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.奥行本数.Width = 120
        '
        '基礎形式
        '
        DataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
        Me.基礎形式.DefaultCellStyle = DataGridViewCellStyle3
        Me.基礎形式.Frozen = True
        Me.基礎形式.HeaderText = "基礎形式"
        Me.基礎形式.MinimumWidth = 10
        Me.基礎形式.Name = "基礎形式"
        Me.基礎形式.ReadOnly = True
        Me.基礎形式.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.基礎形式.Width = 80
        '
        '変位照査
        '
        DataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
        Me.変位照査.DefaultCellStyle = DataGridViewCellStyle4
        Me.変位照査.Frozen = True
        Me.変位照査.HeaderText = "変位照査"
        Me.変位照査.MinimumWidth = 10
        Me.変位照査.Name = "変位照査"
        Me.変位照査.ReadOnly = True
        Me.変位照査.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.変位照査.Width = 80
        '
        '反力照査
        '
        DataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
        Me.反力照査.DefaultCellStyle = DataGridViewCellStyle5
        Me.反力照査.Frozen = True
        Me.反力照査.HeaderText = "反力照査"
        Me.反力照査.MinimumWidth = 10
        Me.反力照査.Name = "反力照査"
        Me.反力照査.ReadOnly = True
        Me.反力照査.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.反力照査.Width = 80
        '
        'a
        '
        DataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
        Me.a.DefaultCellStyle = DataGridViewCellStyle6
        Me.a.HeaderText = ""
        Me.a.MinimumWidth = 10
        Me.a.Name = "a"
        Me.a.ReadOnly = True
        Me.a.Width = 30
        '
        'b
        '
        Me.b.HeaderText = ""
        Me.b.MinimumWidth = 10
        Me.b.Name = "b"
        Me.b.ReadOnly = True
        Me.b.Width = 50
        '
        'title
        '
        Me.title.HeaderText = "照査項目"
        Me.title.MinimumWidth = 10
        Me.title.Name = "title"
        Me.title.ReadOnly = True
        Me.title.Width = 205
        '
        'limit
        '
        DataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight
        Me.limit.DefaultCellStyle = DataGridViewCellStyle7
        Me.limit.HeaderText = "制限値"
        Me.limit.MinimumWidth = 10
        Me.limit.Name = "limit"
        Me.limit.Width = 200
        '
        'fpLimitValue
        '
        Me.fpLimitValue.AccessibleDescription = "fpLimitValue, 普通, Row 0, Column 0"
        Me.fpLimitValue.Font = New System.Drawing.Font("ＭＳ Ｐゴシック", 11.0!)
        Me.fpLimitValue.Location = New System.Drawing.Point(636, 72)
        Me.fpLimitValue.Margin = New System.Windows.Forms.Padding(6)
        Me.fpLimitValue.Name = "fpLimitValue"
        Me.fpLimitValue.Size = New System.Drawing.Size(396, 453)
        Me.fpLimitValue.TabIndex = 47
        '
        'MyPictureBox1
        '
        Me.MyPictureBox1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.MyPictureBox1.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.MyPictureBox1.Location = New System.Drawing.Point(6, 18)
        Me.MyPictureBox1.Margin = New System.Windows.Forms.Padding(14, 12, 14, 12)
        Me.MyPictureBox1.Name = "MyPictureBox1"
        Me.MyPictureBox1.Size = New System.Drawing.Size(284, 425)
        Me.MyPictureBox1.TabIndex = 0
        Me.MyPictureBox1.TabStop = False
        '
        'ChildFormFoundationSetting
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.ClientSize = New System.Drawing.Size(1011, 531)
        Me.ControlBox = False
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.fpLimitValue)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.GroupBox5)
        Me.Controls.Add(Me.cbFoundationType)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.Label1)
        Me.DoubleBuffered = True
        Me.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Margin = New System.Windows.Forms.Padding(6)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "ChildFormFoundationSetting"
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox5.ResumeLayout(False)
        Me.GroupBox5.PerformLayout()
        CType(Me.fpFoundationList, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.fpDisgReactAtPoint, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel1.ResumeLayout(False)
        CType(Me.fpLimitValue, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents a2 As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents b2 As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents title2 As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents limit2 As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents OpenFileDialog1 As System.Windows.Forms.OpenFileDialog
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents cbFoundationType As System.Windows.Forms.ComboBox
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents GroupBox3 As System.Windows.Forms.GroupBox
    Friend WithEvents MyPictureBox1 As Aggre.MyPictureBox
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents GroupBox5 As System.Windows.Forms.GroupBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents limit As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents title As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents b As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents a As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents btnDelete As System.Windows.Forms.Button
    Friend WithEvents btnNew As System.Windows.Forms.Button
    Friend WithEvents btnNext As System.Windows.Forms.Button
    Friend WithEvents btnBack As System.Windows.Forms.Button
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents 基礎形式 As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents 変位照査 As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents 反力照査 As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents 着目番号 As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents 奥行本数 As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents fpLimitValue As FarPoint.Win.Spread.FpSpread
    Friend WithEvents fpDisgReactAtPoint As FarPoint.Win.Spread.FpSpread
    Friend WithEvents fpFoundationList As FarPoint.Win.Spread.FpSpread
    Friend WithEvents fpFoundationList_Sheet1 As FarPoint.Win.Spread.SheetView
    Friend WithEvents fpDisgReactAtPoint_反力照査位置 As FarPoint.Win.Spread.SheetView
    Friend WithEvents fpDisgReactAtPoint_変位照査位置 As FarPoint.Win.Spread.SheetView
    Friend WithEvents fpLimitValue_普通 As FarPoint.Win.Spread.SheetView
    Friend WithEvents fpLimitValue_液状化 As FarPoint.Win.Spread.SheetView
    Friend WithEvents fpLimitValue_直接基礎 As FarPoint.Win.Spread.SheetView
End Class
