Imports System.Windows

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MDIParentForm
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MDIParentForm))
        Me.MenuStrip = New System.Windows.Forms.MenuStrip()
        Me.FileMenu = New System.Windows.Forms.ToolStripMenuItem()
        Me.NewToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.OpenToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator3 = New System.Windows.Forms.ToolStripSeparator()
        Me.SaveToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SaveAsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator4 = New System.Windows.Forms.ToolStripSeparator()
        Me.ExitToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator5 = New System.Windows.Forms.ToolStripSeparator()
        Me.HelpMenu = New System.Windows.Forms.ToolStripMenuItem()
        Me.ContentsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AboutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStrip = New System.Windows.Forms.ToolStrip()
        Me.ShowCaseNameFormButton = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripSeparator2 = New System.Windows.Forms.ToolStripSeparator()
        Me.ShowRequirementFormButton = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripSeparator9 = New System.Windows.Forms.ToolStripSeparator()
        Me.ShowElementSettingFormButton = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripSeparator10 = New System.Windows.Forms.ToolStripSeparator()
        Me.ShowFoundationSettingFormButton = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator()
        Me.ShowPileAnchorBarFormButton = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripSeparator11 = New System.Windows.Forms.ToolStripSeparator()
        Me.ShowCalculationFormButton = New System.Windows.Forms.ToolStripButton()
        Me.StatusStrip = New System.Windows.Forms.StatusStrip()
        Me.ToolStripStatusLabel = New System.Windows.Forms.ToolStripStatusLabel()
        Me.MenuStrip.SuspendLayout()
        Me.ToolStrip.SuspendLayout()
        Me.StatusStrip.SuspendLayout()
        Me.SuspendLayout()
        '
        'MenuStrip
        '
        Me.MenuStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.FileMenu, Me.HelpMenu})
        Me.MenuStrip.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip.Name = "MenuStrip"
        Me.MenuStrip.Size = New System.Drawing.Size(1056, 26)
        Me.MenuStrip.TabIndex = 5
        Me.MenuStrip.Text = "MenuStrip"
        '
        'FileMenu
        '
        Me.FileMenu.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.NewToolStripMenuItem, Me.OpenToolStripMenuItem, Me.ToolStripSeparator3, Me.SaveToolStripMenuItem, Me.SaveAsToolStripMenuItem, Me.ToolStripSeparator4, Me.ExitToolStripMenuItem, Me.ToolStripSeparator5})
        Me.FileMenu.Font = New System.Drawing.Font("メイリオ", 9.0!)
        Me.FileMenu.ImageTransparentColor = System.Drawing.SystemColors.ActiveBorder
        Me.FileMenu.Name = "FileMenu"
        Me.FileMenu.Size = New System.Drawing.Size(85, 22)
        Me.FileMenu.Text = "ファイル(&F)"
        '
        'NewToolStripMenuItem
        '
        Me.NewToolStripMenuItem.Font = New System.Drawing.Font("メイリオ", 9.0!)
        Me.NewToolStripMenuItem.Image = CType(resources.GetObject("NewToolStripMenuItem.Image"), System.Drawing.Image)
        Me.NewToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Black
        Me.NewToolStripMenuItem.Name = "NewToolStripMenuItem"
        Me.NewToolStripMenuItem.ShortcutKeys = CType((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.N), System.Windows.Forms.Keys)
        Me.NewToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
        Me.NewToolStripMenuItem.Text = "新規作成(&N)"
        '
        'OpenToolStripMenuItem
        '
        Me.OpenToolStripMenuItem.Font = New System.Drawing.Font("メイリオ", 9.0!)
        Me.OpenToolStripMenuItem.Image = CType(resources.GetObject("OpenToolStripMenuItem.Image"), System.Drawing.Image)
        Me.OpenToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Black
        Me.OpenToolStripMenuItem.Name = "OpenToolStripMenuItem"
        Me.OpenToolStripMenuItem.ShortcutKeys = CType((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.O), System.Windows.Forms.Keys)
        Me.OpenToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
        Me.OpenToolStripMenuItem.Text = "開く(&O)"
        '
        'ToolStripSeparator3
        '
        Me.ToolStripSeparator3.Name = "ToolStripSeparator3"
        Me.ToolStripSeparator3.Size = New System.Drawing.Size(223, 6)
        '
        'SaveToolStripMenuItem
        '
        Me.SaveToolStripMenuItem.Font = New System.Drawing.Font("メイリオ", 9.0!)
        Me.SaveToolStripMenuItem.Image = CType(resources.GetObject("SaveToolStripMenuItem.Image"), System.Drawing.Image)
        Me.SaveToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Black
        Me.SaveToolStripMenuItem.Name = "SaveToolStripMenuItem"
        Me.SaveToolStripMenuItem.ShortcutKeys = CType((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.S), System.Windows.Forms.Keys)
        Me.SaveToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
        Me.SaveToolStripMenuItem.Text = "上書き保存(&S)"
        '
        'SaveAsToolStripMenuItem
        '
        Me.SaveAsToolStripMenuItem.Font = New System.Drawing.Font("メイリオ", 9.0!)
        Me.SaveAsToolStripMenuItem.Name = "SaveAsToolStripMenuItem"
        Me.SaveAsToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
        Me.SaveAsToolStripMenuItem.Text = "名前を付けて保存(&A)"
        '
        'ToolStripSeparator4
        '
        Me.ToolStripSeparator4.Name = "ToolStripSeparator4"
        Me.ToolStripSeparator4.Size = New System.Drawing.Size(223, 6)
        '
        'ExitToolStripMenuItem
        '
        Me.ExitToolStripMenuItem.Font = New System.Drawing.Font("メイリオ", 9.0!)
        Me.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem"
        Me.ExitToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
        Me.ExitToolStripMenuItem.Text = "アプリケーションの終了(&X)"
        '
        'ToolStripSeparator5
        '
        Me.ToolStripSeparator5.Name = "ToolStripSeparator5"
        Me.ToolStripSeparator5.Size = New System.Drawing.Size(223, 6)
        '
        'HelpMenu
        '
        Me.HelpMenu.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ContentsToolStripMenuItem, Me.AboutToolStripMenuItem})
        Me.HelpMenu.Font = New System.Drawing.Font("メイリオ", 9.0!)
        Me.HelpMenu.Name = "HelpMenu"
        Me.HelpMenu.Size = New System.Drawing.Size(75, 22)
        Me.HelpMenu.Text = "ヘルプ(&H)"
        '
        'ContentsToolStripMenuItem
        '
        Me.ContentsToolStripMenuItem.Font = New System.Drawing.Font("メイリオ", 9.0!)
        Me.ContentsToolStripMenuItem.Name = "ContentsToolStripMenuItem"
        Me.ContentsToolStripMenuItem.Size = New System.Drawing.Size(190, 22)
        Me.ContentsToolStripMenuItem.Text = "マニュアル"
        '
        'AboutToolStripMenuItem
        '
        Me.AboutToolStripMenuItem.Font = New System.Drawing.Font("メイリオ", 9.0!)
        Me.AboutToolStripMenuItem.Name = "AboutToolStripMenuItem"
        Me.AboutToolStripMenuItem.Size = New System.Drawing.Size(190, 22)
        Me.AboutToolStripMenuItem.Text = "バージョン情報(&A)..."
        '
        'ToolStrip
        '
        Me.ToolStrip.BackColor = System.Drawing.SystemColors.ButtonFace
        Me.ToolStrip.Font = New System.Drawing.Font("メイリオ", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.ToolStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ShowCaseNameFormButton, Me.ToolStripSeparator2, Me.ShowRequirementFormButton, Me.ToolStripSeparator9, Me.ShowElementSettingFormButton, Me.ToolStripSeparator10, Me.ShowFoundationSettingFormButton, Me.ToolStripSeparator1, Me.ShowPileAnchorBarFormButton, Me.ToolStripSeparator11, Me.ShowCalculationFormButton})
        Me.ToolStrip.Location = New System.Drawing.Point(0, 26)
        Me.ToolStrip.Name = "ToolStrip"
        Me.ToolStrip.Size = New System.Drawing.Size(1056, 42)
        Me.ToolStrip.TabIndex = 6
        Me.ToolStrip.Text = "ToolStrip"
        '
        'ShowCaseNameFormButton
        '
        Me.ShowCaseNameFormButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.ShowCaseNameFormButton.Font = New System.Drawing.Font("メイリオ", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.ShowCaseNameFormButton.ImageTransparentColor = System.Drawing.Color.Black
        Me.ShowCaseNameFormButton.Margin = New System.Windows.Forms.Padding(2)
        Me.ShowCaseNameFormButton.Name = "ShowCaseNameFormButton"
        Me.ShowCaseNameFormButton.Padding = New System.Windows.Forms.Padding(8)
        Me.ShowCaseNameFormButton.Size = New System.Drawing.Size(88, 38)
        Me.ShowCaseNameFormButton.Text = "解析ケース"
        '
        'ToolStripSeparator2
        '
        Me.ToolStripSeparator2.BackColor = System.Drawing.SystemColors.Control
        Me.ToolStripSeparator2.Name = "ToolStripSeparator2"
        Me.ToolStripSeparator2.Size = New System.Drawing.Size(6, 42)
        '
        'ShowRequirementFormButton
        '
        Me.ShowRequirementFormButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.ShowRequirementFormButton.Font = New System.Drawing.Font("メイリオ", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.ShowRequirementFormButton.ImageTransparentColor = System.Drawing.Color.Black
        Me.ShowRequirementFormButton.Margin = New System.Windows.Forms.Padding(2)
        Me.ShowRequirementFormButton.Name = "ShowRequirementFormButton"
        Me.ShowRequirementFormButton.Padding = New System.Windows.Forms.Padding(8)
        Me.ShowRequirementFormButton.Size = New System.Drawing.Size(112, 38)
        Me.ShowRequirementFormButton.Text = "照査パラメータ"
        '
        'ToolStripSeparator9
        '
        Me.ToolStripSeparator9.Name = "ToolStripSeparator9"
        Me.ToolStripSeparator9.Size = New System.Drawing.Size(6, 42)
        '
        'ShowElementSettingFormButton
        '
        Me.ShowElementSettingFormButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.ShowElementSettingFormButton.Font = New System.Drawing.Font("メイリオ", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.ShowElementSettingFormButton.ImageTransparentColor = System.Drawing.Color.Black
        Me.ShowElementSettingFormButton.Margin = New System.Windows.Forms.Padding(2)
        Me.ShowElementSettingFormButton.Name = "ShowElementSettingFormButton"
        Me.ShowElementSettingFormButton.Padding = New System.Windows.Forms.Padding(8)
        Me.ShowElementSettingFormButton.Size = New System.Drawing.Size(100, 38)
        Me.ShowElementSettingFormButton.Text = "　　断面　　"
        '
        'ToolStripSeparator10
        '
        Me.ToolStripSeparator10.Name = "ToolStripSeparator10"
        Me.ToolStripSeparator10.Size = New System.Drawing.Size(6, 42)
        '
        'ShowFoundationSettingFormButton
        '
        Me.ShowFoundationSettingFormButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.ShowFoundationSettingFormButton.Font = New System.Drawing.Font("メイリオ", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.ShowFoundationSettingFormButton.ImageTransparentColor = System.Drawing.Color.Black
        Me.ShowFoundationSettingFormButton.Margin = New System.Windows.Forms.Padding(2)
        Me.ShowFoundationSettingFormButton.Name = "ShowFoundationSettingFormButton"
        Me.ShowFoundationSettingFormButton.Padding = New System.Windows.Forms.Padding(8)
        Me.ShowFoundationSettingFormButton.Size = New System.Drawing.Size(100, 38)
        Me.ShowFoundationSettingFormButton.Text = "　　基礎　　"
        '
        'ToolStripSeparator1
        '
        Me.ToolStripSeparator1.Name = "ToolStripSeparator1"
        Me.ToolStripSeparator1.Size = New System.Drawing.Size(6, 42)
        '
        'ShowPileAnchorBarFormButton
        '
        Me.ShowPileAnchorBarFormButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.ShowPileAnchorBarFormButton.Font = New System.Drawing.Font("メイリオ", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.ShowPileAnchorBarFormButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ShowPileAnchorBarFormButton.Margin = New System.Windows.Forms.Padding(2)
        Me.ShowPileAnchorBarFormButton.Name = "ShowPileAnchorBarFormButton"
        Me.ShowPileAnchorBarFormButton.Padding = New System.Windows.Forms.Padding(8)
        Me.ShowPileAnchorBarFormButton.Size = New System.Drawing.Size(88, 38)
        Me.ShowPileAnchorBarFormButton.Text = "杭の段落し"
        '
        'ToolStripSeparator11
        '
        Me.ToolStripSeparator11.Name = "ToolStripSeparator11"
        Me.ToolStripSeparator11.Size = New System.Drawing.Size(6, 42)
        '
        'ShowCalculationFormButton
        '
        Me.ShowCalculationFormButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.ShowCalculationFormButton.Font = New System.Drawing.Font("メイリオ", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(128, Byte))
        Me.ShowCalculationFormButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ShowCalculationFormButton.Margin = New System.Windows.Forms.Padding(2)
        Me.ShowCalculationFormButton.Name = "ShowCalculationFormButton"
        Me.ShowCalculationFormButton.Padding = New System.Windows.Forms.Padding(8)
        Me.ShowCalculationFormButton.Size = New System.Drawing.Size(84, 38)
        Me.ShowCalculationFormButton.Text = " 計算実行 "
        Me.ShowCalculationFormButton.ToolTipText = "計算実行"
        '
        'StatusStrip
        '
        Me.StatusStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripStatusLabel})
        Me.StatusStrip.Location = New System.Drawing.Point(0, 609)
        Me.StatusStrip.Name = "StatusStrip"
        Me.StatusStrip.Size = New System.Drawing.Size(1056, 22)
        Me.StatusStrip.TabIndex = 7
        Me.StatusStrip.Text = "StatusStrip"
        '
        'ToolStripStatusLabel
        '
        Me.ToolStripStatusLabel.Name = "ToolStripStatusLabel"
        Me.ToolStripStatusLabel.Size = New System.Drawing.Size(31, 17)
        Me.ToolStripStatusLabel.Text = "状態"
        '
        'MDIParentForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.ClientSize = New System.Drawing.Size(1056, 631)
        Me.Controls.Add(Me.ToolStrip)
        Me.Controls.Add(Me.MenuStrip)
        Me.Controls.Add(Me.StatusStrip)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.IsMdiContainer = True
        Me.MainMenuStrip = Me.MenuStrip
        Me.Name = "MDIParentForm"
        Me.Text = "Batch:複数検討作業処理ツール"
        Me.MenuStrip.ResumeLayout(False)
        Me.MenuStrip.PerformLayout()
        Me.ToolStrip.ResumeLayout(False)
        Me.ToolStrip.PerformLayout()
        Me.StatusStrip.ResumeLayout(False)
        Me.StatusStrip.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents ContentsToolStripMenuItem As Forms.ToolStripMenuItem
    Friend WithEvents HelpMenu As Forms.ToolStripMenuItem
    Friend WithEvents AboutToolStripMenuItem As Forms.ToolStripMenuItem
    Friend WithEvents ToolTip As Forms.ToolTip
    Friend WithEvents ToolStripStatusLabel As Forms.ToolStripStatusLabel
    Friend WithEvents StatusStrip As Forms.StatusStrip
    Friend WithEvents ToolStrip As Forms.ToolStrip
    Friend WithEvents ToolStripSeparator4 As Forms.ToolStripSeparator
    Friend WithEvents ExitToolStripMenuItem As Forms.ToolStripMenuItem
    Friend WithEvents SaveAsToolStripMenuItem As Forms.ToolStripMenuItem
    Friend WithEvents NewToolStripMenuItem As Forms.ToolStripMenuItem
    Friend WithEvents FileMenu As Forms.ToolStripMenuItem
    Friend WithEvents OpenToolStripMenuItem As Forms.ToolStripMenuItem
    Friend WithEvents ToolStripSeparator3 As Forms.ToolStripSeparator
    Friend WithEvents SaveToolStripMenuItem As Forms.ToolStripMenuItem
    Friend WithEvents MenuStrip As Forms.MenuStrip
    Friend WithEvents ShowRequirementFormButton As Forms.ToolStripButton
    Friend WithEvents ShowElementSettingFormButton As Forms.ToolStripButton
    Friend WithEvents ShowFoundationSettingFormButton As Forms.ToolStripButton
    Friend WithEvents ShowCaseNameFormButton As Forms.ToolStripButton
    Friend WithEvents ToolStripSeparator2 As Forms.ToolStripSeparator
    Friend WithEvents ToolStripSeparator9 As Forms.ToolStripSeparator
    Friend WithEvents ToolStripSeparator10 As Forms.ToolStripSeparator
    Friend WithEvents ToolStripSeparator11 As Forms.ToolStripSeparator
    Friend WithEvents ShowCalculationFormButton As Forms.ToolStripButton
    Friend WithEvents ToolStripSeparator1 As Forms.ToolStripSeparator
    Friend WithEvents ShowPileAnchorBarFormButton As Forms.ToolStripButton
    Friend WithEvents ToolStripSeparator5 As Forms.ToolStripSeparator
End Class
