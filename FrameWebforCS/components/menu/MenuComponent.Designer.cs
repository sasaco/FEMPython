namespace FrameWebforCS
{
    partial class MenuComponent
    {
        /// <summary> 
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region コンポーネント デザイナーで生成されたコード

        /// <summary> 
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を 
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanel1 = new TableLayoutPanel();
            menuStrip1 = new MenuStrip();
            ファイルToolStripMenuItem = new ToolStripMenuItem();
            新規作成ToolStripMenuItem = new ToolStripMenuItem();
            ファイルを開くToolStripMenuItem = new ToolStripMenuItem();
            ファイルを保存ToolStripMenuItem = new ToolStripMenuItem();
            プリセットを開くToolStripMenuItem = new ToolStripMenuItem();
            計算ToolStripMenuItem = new ToolStripMenuItem();
            印刷ToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripMenuItem();
            dToolStripMenuItem2 = new ToolStripMenuItem();
            dToolStripMenuItem3 = new ToolStripMenuItem();
            button1 = new Button();
            openFileDialog1 = new OpenFileDialog();
            saveFileDialog1 = new SaveFileDialog();
            tableLayoutPanel1.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Controls.Add(menuStrip1, 0, 0);
            tableLayoutPanel1.Controls.Add(button1, 2, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(903, 24);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // menuStrip1
            // 
            menuStrip1.Dock = DockStyle.Fill;
            menuStrip1.Items.AddRange(new ToolStripItem[] { ファイルToolStripMenuItem, 計算ToolStripMenuItem, 印刷ToolStripMenuItem, toolStripMenuItem1 });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(180, 24);
            menuStrip1.TabIndex = 2;
            menuStrip1.Text = "menuStrip1";
            // 
            // ファイルToolStripMenuItem
            // 
            ファイルToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 新規作成ToolStripMenuItem, ファイルを開くToolStripMenuItem, ファイルを保存ToolStripMenuItem, プリセットを開くToolStripMenuItem });
            ファイルToolStripMenuItem.Name = "ファイルToolStripMenuItem";
            ファイルToolStripMenuItem.Size = new Size(53, 20);
            ファイルToolStripMenuItem.Text = "ファイル";
            // 
            // 新規作成ToolStripMenuItem
            // 
            新規作成ToolStripMenuItem.Name = "新規作成ToolStripMenuItem";
            新規作成ToolStripMenuItem.Size = new Size(180, 22);
            新規作成ToolStripMenuItem.Text = "新規作成";
            新規作成ToolStripMenuItem.Click += renewToolStripMenuItem_Click;
            // 
            // ファイルを開くToolStripMenuItem
            // 
            ファイルを開くToolStripMenuItem.Name = "ファイルを開くToolStripMenuItem";
            ファイルを開くToolStripMenuItem.Size = new Size(180, 22);
            ファイルを開くToolStripMenuItem.Text = "ファイルを開く";
            ファイルを開くToolStripMenuItem.Click += OpenToolStripMenuItem_Click;
            // 
            // ファイルを保存ToolStripMenuItem
            // 
            ファイルを保存ToolStripMenuItem.Name = "ファイルを保存ToolStripMenuItem";
            ファイルを保存ToolStripMenuItem.Size = new Size(180, 22);
            ファイルを保存ToolStripMenuItem.Text = "ファイルを保存";
            ファイルを保存ToolStripMenuItem.Click += SaveToolStripMenuItem_Click;
            // 
            // プリセットを開くToolStripMenuItem
            // 
            プリセットを開くToolStripMenuItem.Name = "プリセットを開くToolStripMenuItem";
            プリセットを開くToolStripMenuItem.Size = new Size(180, 22);
            プリセットを開くToolStripMenuItem.Text = "プリセットを開く";
            // 
            // 計算ToolStripMenuItem
            // 
            計算ToolStripMenuItem.Name = "計算ToolStripMenuItem";
            計算ToolStripMenuItem.Size = new Size(43, 20);
            計算ToolStripMenuItem.Text = "計算";
            // 
            // 印刷ToolStripMenuItem
            // 
            印刷ToolStripMenuItem.Name = "印刷ToolStripMenuItem";
            印刷ToolStripMenuItem.Size = new Size(43, 20);
            印刷ToolStripMenuItem.Text = "印刷";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { dToolStripMenuItem2, dToolStripMenuItem3 });
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(33, 20);
            toolStripMenuItem1.Text = "3D";
            // 
            // dToolStripMenuItem2
            // 
            dToolStripMenuItem2.Name = "dToolStripMenuItem2";
            dToolStripMenuItem2.Size = new Size(88, 22);
            dToolStripMenuItem2.Text = "2D";
            dToolStripMenuItem2.Click += Dimension2DToolStripMenuItem_Click;
            // 
            // dToolStripMenuItem3
            // 
            dToolStripMenuItem3.Name = "dToolStripMenuItem3";
            dToolStripMenuItem3.Size = new Size(88, 22);
            dToolStripMenuItem3.Text = "3D";
            dToolStripMenuItem3.Click += Dimension3DToolStripMenuItem_Click;
            // 
            // button1
            // 
            button1.Dock = DockStyle.Fill;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Location = new Point(806, 0);
            button1.Margin = new Padding(3, 0, 3, 0);
            button1.Name = "button1";
            button1.Size = new Size(94, 24);
            button1.TabIndex = 4;
            button1.Text = "login";
            button1.UseVisualStyleBackColor = true;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // MenuComponent
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            Controls.Add(tableLayoutPanel1);
            Name = "MenuComponent";
            Size = new Size(903, 24);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem ファイルToolStripMenuItem;
        private ToolStripMenuItem 新規作成ToolStripMenuItem;
        private ToolStripMenuItem ファイルを開くToolStripMenuItem;
        private ToolStripMenuItem ファイルを保存ToolStripMenuItem;
        private ToolStripMenuItem プリセットを開くToolStripMenuItem;
        private ToolStripMenuItem 印刷ToolStripMenuItem;
        private Button button1;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem dToolStripMenuItem2;
        private ToolStripMenuItem dToolStripMenuItem3;
        private ToolStripMenuItem 計算ToolStripMenuItem;
        private OpenFileDialog openFileDialog1;
        private SaveFileDialog saveFileDialog1;
    }
}
