using FrameWebforCS.three;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FrameWebforCS
{
    public partial class AppComponent : Form
    {
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
