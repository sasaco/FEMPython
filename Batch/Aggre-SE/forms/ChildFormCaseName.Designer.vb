Imports System.Windows

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class ChildFormCaseName
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ChildFormCaseName))
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle2 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle3 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle4 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.fpCaseList = New FarPoint.Win.Spread.FpSpread(FarPoint.Win.Spread.LegacyBehaviors.None, CType(resources.GetObject("GroupBox4.Controls"), Object))
        Me.fpCaseList_Sheet1 = Me.fpCaseList.GetSheet(0)
        Me.btnSetDataFolder = New System.Windows.Forms.Button()
        Me.lblSetDataFolder = New System.Windows.Forms.Label()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.ファイル選択ボタン = New System.Windows.Forms.DataGridViewButtonColumn()
        Me.解析ケース = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.αf = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ρm = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.照査対象 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.chk1 = New System.Windows.Forms.DataGridViewCheckBoxColumn()
        Me.chk2 = New System.Windows.Forms.DataGridViewCheckBoxColumn()
        Me.chk3 = New System.Windows.Forms.DataGridViewCheckBoxColumn()
        Me.chk4 = New System.Windows.Forms.DataGridViewCheckBoxColumn()
        Me.chk5 = New System.Windows.Forms.DataGridViewCheckBoxColumn()
        Me.chk6 = New System.Windows.Forms.DataGridViewCheckBoxColumn()
        Me.chk7 = New System.Windows.Forms.DataGridViewCheckBoxColumn()
        Me.chk8 = New System.Windows.Forms.DataGridViewCheckBoxColumn()
        Me.chk9 = New System.Windows.Forms.DataGridViewCheckBoxColumn()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        CType(Me.fpCaseList, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.Label2.Location = New System.Drawing.Point(479, 3)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(93, 12)
        Me.Label2.TabIndex = 0
        Me.Label2.Text = "[ 耐震性能照査 ]"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(6, 0)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(0, 12)
        Me.Label3.TabIndex = 5
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(6, 26)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(169, 156)
        Me.Label4.TabIndex = 0
        Me.Label4.Text = "照査対象とは、" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "③損傷レベルの照査" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "④破壊形態の検討" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "⑤せん断耐力の照査" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "を照査する対象の断面" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "（部材照査タイプ）番号を" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "指定します。" &
    "" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "空白の場合は全断面照査します。"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(6, 26)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(170, 204)
        Me.Label7.TabIndex = 0
        Me.Label7.Text = "①復旧性L2地震動の照査" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "②安全性L2地震動の照査" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "③損傷レベルの照査" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "④破壊形態の検討" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "⑤せん断耐力の照査" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "⑥L1地震動の照査" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) &
    "" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "⑦基礎の安定照査" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "⑧杭の段落し図作成" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "⑨断面力ピックアップファイルの作成"
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.Label4)
        Me.GroupBox1.Controls.Add(Me.Label3)
        Me.GroupBox1.Location = New System.Drawing.Point(801, 300)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(198, 219)
        Me.GroupBox1.TabIndex = 0
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "照査対象"
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.Label7)
        Me.GroupBox3.Location = New System.Drawing.Point(801, 40)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(198, 254)
        Me.GroupBox3.TabIndex = 0
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "耐震性能照査"
        '
        'GroupBox4
        '
        Me.GroupBox4.AutoSize = True
        Me.GroupBox4.Controls.Add(Me.fpCaseList)
        Me.GroupBox4.Controls.Add(Me.Label2)
        Me.GroupBox4.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.GroupBox4.Location = New System.Drawing.Point(12, 37)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(783, 894)
        Me.GroupBox4.TabIndex = 0
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "[ 解析ケース ]"
        '
        'fpCaseList
        '
        Me.fpCaseList.AccessibleDescription = "fpCaseList, Sheet1, Row 0, Column 0"
        Me.fpCaseList.Font = New System.Drawing.Font("ＭＳ Ｐゴシック", 11.0!)
        Me.fpCaseList.Location = New System.Drawing.Point(0, 22)
        Me.fpCaseList.Margin = New System.Windows.Forms.Padding(1, 2, 1, 2)
        Me.fpCaseList.Name = "fpCaseList"
        Me.fpCaseList.Size = New System.Drawing.Size(772, 461)
        Me.fpCaseList.TabIndex = 2
        '
        'fpCaseList_Sheet1
        '
        '
        'btnSetDataFolder
        '
        Me.btnSetDataFolder.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.btnSetDataFolder.Location = New System.Drawing.Point(12, 6)
        Me.btnSetDataFolder.Name = "btnSetDataFolder"
        Me.btnSetDataFolder.Size = New System.Drawing.Size(146, 25)
        Me.btnSetDataFolder.TabIndex = 2
        Me.btnSetDataFolder.Text = "データフォルダの設定(任意)"
        Me.btnSetDataFolder.UseVisualStyleBackColor = True
        '
        'lblSetDataFolder
        '
        Me.lblSetDataFolder.AutoSize = True
        Me.lblSetDataFolder.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.lblSetDataFolder.Location = New System.Drawing.Point(180, 12)
        Me.lblSetDataFolder.Name = "lblSetDataFolder"
        Me.lblSetDataFolder.Size = New System.Drawing.Size(0, 12)
        Me.lblSetDataFolder.TabIndex = 0
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'ファイル選択ボタン
        '
        Me.ファイル選択ボタン.Frozen = True
        Me.ファイル選択ボタン.HeaderText = ""
        Me.ファイル選択ボタン.MinimumWidth = 10
        Me.ファイル選択ボタン.Name = "ファイル選択ボタン"
        Me.ファイル選択ボタン.ReadOnly = True
        Me.ファイル選択ボタン.Text = "..."
        Me.ファイル選択ボタン.UseColumnTextForButtonValue = True
        Me.ファイル選択ボタン.Width = 20
        '
        '解析ケース
        '
        DataGridViewCellStyle1.BackColor = System.Drawing.Color.Lavender
        Me.解析ケース.DefaultCellStyle = DataGridViewCellStyle1
        Me.解析ケース.Frozen = True
        Me.解析ケース.HeaderText = "ファイル名"
        Me.解析ケース.MinimumWidth = 10
        Me.解析ケース.Name = "解析ケース"
        Me.解析ケース.ReadOnly = True
        Me.解析ケース.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.解析ケース.Width = 225
        '
        'αf
        '
        DataGridViewCellStyle2.BackColor = System.Drawing.Color.Lavender
        DataGridViewCellStyle2.Format = "N2"
        DataGridViewCellStyle2.NullValue = "0.0"
        Me.αf.DefaultCellStyle = DataGridViewCellStyle2
        Me.αf.Frozen = True
        Me.αf.HeaderText = "αf"
        Me.αf.MinimumWidth = 10
        Me.αf.Name = "αf"
        Me.αf.ReadOnly = True
        Me.αf.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.αf.Width = 30
        '
        'ρm
        '
        DataGridViewCellStyle3.BackColor = System.Drawing.Color.Lavender
        DataGridViewCellStyle3.Format = "N2"
        DataGridViewCellStyle3.NullValue = "0.0"
        Me.ρm.DefaultCellStyle = DataGridViewCellStyle3
        Me.ρm.Frozen = True
        Me.ρm.HeaderText = "ρm"
        Me.ρm.MinimumWidth = 10
        Me.ρm.Name = "ρm"
        Me.ρm.ReadOnly = True
        Me.ρm.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.ρm.Width = 35
        '
        '照査対象
        '
        DataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
        Me.照査対象.DefaultCellStyle = DataGridViewCellStyle4
        Me.照査対象.HeaderText = "照査" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "対象"
        Me.照査対象.MinimumWidth = 10
        Me.照査対象.Name = "照査対象"
        Me.照査対象.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
        Me.照査対象.Width = 70
        '
        'chk1
        '
        Me.chk1.HeaderText = "①"
        Me.chk1.MinimumWidth = 10
        Me.chk1.Name = "chk1"
        Me.chk1.Width = 34
        '
        'chk2
        '
        Me.chk2.HeaderText = "②"
        Me.chk2.MinimumWidth = 10
        Me.chk2.Name = "chk2"
        Me.chk2.Width = 34
        '
        'chk3
        '
        Me.chk3.HeaderText = "③"
        Me.chk3.MinimumWidth = 10
        Me.chk3.Name = "chk3"
        Me.chk3.Width = 34
        '
        'chk4
        '
        Me.chk4.HeaderText = "④"
        Me.chk4.MinimumWidth = 10
        Me.chk4.Name = "chk4"
        Me.chk4.Width = 34
        '
        'chk5
        '
        Me.chk5.HeaderText = "⑤"
        Me.chk5.MinimumWidth = 10
        Me.chk5.Name = "chk5"
        Me.chk5.Width = 34
        '
        'chk6
        '
        Me.chk6.HeaderText = "⑥"
        Me.chk6.MinimumWidth = 10
        Me.chk6.Name = "chk6"
        Me.chk6.Width = 34
        '
        'chk7
        '
        Me.chk7.HeaderText = "⑦"
        Me.chk7.MinimumWidth = 10
        Me.chk7.Name = "chk7"
        Me.chk7.Resizable = System.Windows.Forms.DataGridViewTriState.[True]
        Me.chk7.Width = 34
        '
        'chk8
        '
        Me.chk8.HeaderText = "⑧"
        Me.chk8.MinimumWidth = 10
        Me.chk8.Name = "chk8"
        Me.chk8.Width = 34
        '
        'chk9
        '
        Me.chk9.HeaderText = "⑨"
        Me.chk9.MinimumWidth = 10
        Me.chk9.Name = "chk9"
        Me.chk9.Width = 34
        '
        'ChildFormCaseName
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.ClientSize = New System.Drawing.Size(1013, 534)
        Me.ControlBox = False
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.lblSetDataFolder)
        Me.Controls.Add(Me.btnSetDataFolder)
        Me.Controls.Add(Me.GroupBox4)
        Me.DoubleBuffered = True
        Me.Font = New System.Drawing.Font("MS UI Gothic", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "ChildFormCaseName"
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        CType(Me.fpCaseList, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label2 As Forms.Label
    Friend WithEvents Label3 As Forms.Label
    Friend WithEvents Label4 As Forms.Label
    Friend WithEvents Label7 As Forms.Label
    Friend WithEvents GroupBox1 As Forms.GroupBox
    Friend WithEvents GroupBox3 As Forms.GroupBox
    Friend WithEvents GroupBox4 As Forms.GroupBox
    Friend WithEvents btnSetDataFolder As Forms.Button
    Friend WithEvents lblSetDataFolder As Forms.Label
    Friend WithEvents OpenFileDialog1 As Forms.OpenFileDialog
    Friend WithEvents ファイル選択ボタン As Forms.DataGridViewButtonColumn
    Friend WithEvents 解析ケース As Forms.DataGridViewTextBoxColumn
    Friend WithEvents αf As Forms.DataGridViewTextBoxColumn
    Friend WithEvents ρm As Forms.DataGridViewTextBoxColumn
    Friend WithEvents 照査対象 As Forms.DataGridViewTextBoxColumn
    Friend WithEvents chk1 As Forms.DataGridViewCheckBoxColumn
    Friend WithEvents chk2 As Forms.DataGridViewCheckBoxColumn
    Friend WithEvents chk3 As Forms.DataGridViewCheckBoxColumn
    Friend WithEvents chk4 As Forms.DataGridViewCheckBoxColumn
    Friend WithEvents chk5 As Forms.DataGridViewCheckBoxColumn
    Friend WithEvents chk6 As Forms.DataGridViewCheckBoxColumn
    Friend WithEvents chk7 As Forms.DataGridViewCheckBoxColumn
    Friend WithEvents chk8 As Forms.DataGridViewCheckBoxColumn
    Friend WithEvents chk9 As Forms.DataGridViewCheckBoxColumn
    Friend WithEvents fpCaseList As FarPoint.Win.Spread.FpSpread
    Friend WithEvents fpCaseList_Sheet1 As FarPoint.Win.Spread.SheetView
End Class
