namespace FrameWebforCS.components.input
{
    partial class InputLoadComponent
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
            fpSpread1 = new myFpSpread();
            ((System.ComponentModel.ISupportInitialize)fpSpread1).BeginInit();
            SuspendLayout();
            // 
            // fpSpread1
            // 
            fpSpread1.Dock = DockStyle.Fill;
            fpSpread1.Location = new Point(0, 0);
            fpSpread1.Name = "fpSpread1";
            fpSpread1.Size = new Size(150, 150);
            fpSpread1.TabIndex = 0;
            // 
            // InputLoadComponent
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(fpSpread1);
            Name = "InputLoadComponent";
            ((System.ComponentModel.ISupportInitialize)fpSpread1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private myFpSpread fpSpread1;
    }
}
