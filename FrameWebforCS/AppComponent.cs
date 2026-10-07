using FrameWebforCS.three;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FrameWebforCS
{
    public partial class AppComponent : Form
    {
        private const Keys ViewportCaptureShortcut = Keys.Control | Keys.Shift | Keys.F12;
        private ThreeComponent three;
        private bool _calculationClosed;
        private bool _closePending;
        public AppComponent()
        {
            InitializeComponent();

            three = new ThreeComponent(glControl1);
            // JS ThreeComponent.ngOnDestroy is empty. The native timer, GL resources,
            // and event subscriptions need an explicit owner on form close.
            FormClosing += OnFormClosing;
            Disposed += (_, _) => three.Dispose();
        }

        internal void ShowPrintDialog()
        {
            if (_closePending || IsDisposed) return;
            using var dialog = new components.printing.PrintDialogForm(requests => three.CapturePrintDiagrams(requests));
            dialog.ShowDialog(this);
            if (!IsDisposed) Activate();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == ViewportCaptureShortcut)
            {
                CaptureVisibleViewport();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void CaptureVisibleViewport()
        {
            if (WindowState == FormWindowState.Minimized || !glControl1.Visible ||
                !glControl1.IsHandleCreated || glControl1.ClientSize.Width == 0 ||
                glControl1.ClientSize.Height == 0)
            {
                MessageBox.Show(this, "表示中のビューポートがありません。", "画面キャプチャ");
                return;
            }

            var bounds = glControl1.RectangleToScreen(glControl1.ClientRectangle);
            if (!Screen.AllScreens.Any(screen => screen.Bounds.Contains(bounds)))
            {
                MessageBox.Show(this, "ビューポート全体を画面内に表示してから再実行してください。", "画面キャプチャ");
                return;
            }

            var path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, ".tmp", "glControl1-screen.png"));
            try
            {
                SaveViewportScreenshot(bounds, path);
                MessageBox.Show(this, $"画面キャプチャを保存しました。\n{path}", "画面キャプチャ");
            }
            catch (Exception error)
            {
                MessageBox.Show(this, $"画面キャプチャを保存できませんでした。\n{error.Message}", "画面キャプチャ");
            }
        }

        private static void SaveViewportScreenshot(Rectangle bounds, string path)
        {
            using var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            bitmap.Save(path, ImageFormat.Png);
        }

        private async void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_calculationClosed)
            {
                three.Dispose();
                return;
            }

            // FormClosing is synchronous. Cancel this pass, await the non-interruptible
            // Python worker without blocking the UI, then close the form a second time.
            e.Cancel = true;
            if (_closePending) return;
            _closePending = true;
            try
            {
                await menuComponent1.CloseCalculationAsync();
            }
            catch (Exception error)
            {
                System.Diagnostics.Trace.WriteLine($"Python shutdown failed: {error}");
            }
            finally
            {
                _calculationClosed = true;
                if (!IsDisposed) BeginInvoke((Action)Close);
            }
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            splitContainer1.SplitterDistance = Math.Min(splitContainer1.SplitterDistance, SidebarComponent1.Width);
        }
    }
}
