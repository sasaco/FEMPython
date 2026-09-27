using FrameWebforCS.providers;
using System.Drawing;
using System.Windows.Forms;

namespace FrameWebforCS.components.result;

internal static class ResultDimensionNotice
{
    internal static void Attach(Control owner)
    {
        var input = InputDataService.Instance;
        var label = new Label
        {
            Name = "resultDimensionNotice",
            AutoEllipsis = true,
            Dock = DockStyle.Top,
            Height = 24,
            Padding = new Padding(8, 2, 8, 2),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.LightGoldenrodYellow,
            Visible = false
        };
        owner.Controls.Add(label);

        void Refresh()
        {
            if (owner.IsDisposed) return;
            int? resultDimension = input.ResultDimension;
            bool mismatch = resultDimension is 2 or 3 && resultDimension != input.dimension;
            label.Text = mismatch
                ? $"保持中の結果: {resultDimension}D / 現在の入力: {input.dimension}D（次元が異なります）"
                : string.Empty;
            label.Visible = mismatch;
        }

        void ScheduleRefresh()
        {
            if (owner.IsDisposed) return;
            if (owner.InvokeRequired)
            {
                if (owner.IsHandleCreated)
                {
                    try { owner.BeginInvoke((Action)Refresh); }
                    catch (InvalidOperationException) { }
                }
                return;
            }
            Refresh();
        }

        void OnDimensionChanged(int _) => ScheduleRefresh();
        void OnFileReplaced(long _) => ScheduleRefresh();
        input.DimensionChanged += OnDimensionChanged;
        input.FileReplaced += OnFileReplaced;
        owner.HandleCreated += (_, _) => Refresh();
        owner.Disposed += (_, _) =>
        {
            input.DimensionChanged -= OnDimensionChanged;
            input.FileReplaced -= OnFileReplaced;
        };
        Refresh();
    }
}
