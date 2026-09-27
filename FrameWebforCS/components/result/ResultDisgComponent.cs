using FarPoint.Win.Spread;
using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace FrameWebforCS.components.result
{
    public partial class ResultDisgComponent : UserControl
    {

        private readonly ResultDisgService _input = ResultDisgService.Instance;

        public ResultDisgComponent()
        {
            InitializeComponent();
            ResultDimensionNotice.Attach(this);
            fpSpread1.ActiveSheetChanged += (_, _) => PublishCurrentPage();
            VisibleChanged += (_, _) => { if (Visible) PublishCurrentPage(); };
            fpSpread1.EditModeOn += fpSpread1.faSpread_EditModeOn;

            _input.Changed += OnResultsChanged;
            Disposed += (_, _) => _input.Changed -= OnResultsChanged;
            HandleCreated += (_, _) => RefreshResults();
            RefreshResults();

        }

        public void setActiveSheet(int index)
        {
            if (fpSpread1.Sheets.Count > 0 && index >= 0 && index < fpSpread1.Sheets.Count)
                fpSpread1.ActiveSheetIndex = index;
            PublishCurrentPage();
        }

        private void PublishCurrentPage()
        {
            int index = fpSpread1.ActiveSheetIndex;
            var cases = _input.getDisg();
            if (Visible && index >= 0 && index < cases.Count)
                FrameWebforCS.three.ThreeResultsService.PublishPage("disg", cases.Keys.ElementAt(index));
        }

        private void OnResultsChanged(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                if (IsHandleCreated) BeginInvoke((System.Action)RefreshResults);
                return;
            }
            RefreshResults();
        }

        private void RefreshResults()
        {
            fpSpread1.Sheets.Clear();
            foreach (var result in _input.getDisg())
            {
                SheetView sheet = fpSpread1.AddNewSheetView();
                sheet.SheetName = result.Key.Replace("Case", "", StringComparison.Ordinal);
                SetSheet1(sheet);
                for (int column = 0; column < sheet.ColumnCount; column++)
                    sheet.Columns[column].CellType = new FarPoint.Win.Spread.CellType.TextCellType();
                int row = 0;
                foreach (var node in result.Value)
                {
                    string id = node.Key.Replace("node", "", StringComparison.Ordinal);
                    if (id.Contains('n') || id.Contains('l')) continue;
                    sheet.RowCount = ++row;
                    sheet.Cells[row - 1, 0].Text = id;
                    sheet.Cells[row - 1, 1].Text = Format(node.Value.dx);
                    sheet.Cells[row - 1, 2].Text = Format(node.Value.dy);
                    if (sheet.ColumnCount == 7)
                    {
                        sheet.Cells[row - 1, 3].Text = Format(node.Value.dz);
                        sheet.Cells[row - 1, 4].Text = Format(node.Value.rx);
                        sheet.Cells[row - 1, 5].Text = Format(node.Value.ry);
                        sheet.Cells[row - 1, 6].Text = Format(node.Value.rz);
                    }
                    else sheet.Cells[row - 1, 3].Text = Format(node.Value.rz);
                }
                sheet.Protect = true;
            }
            if (fpSpread1.Sheets.Count > 0)
            {
                fpSpread1.ActiveSheetIndex = 0;
                float width = 100;
                var columns = fpSpread1.Sheets[0].Columns;
                for (int column = 0; column < columns.Count; column++) width += columns[column].Width;
                Width = (int)width;
            }
        }

        private static string Format(double? value)
        {
            double scaled = (value ?? 0) * 1000;
            double rounded = Math.Floor(scaled * 10000 + 0.5) / 10000;
            return rounded.ToString("F4", CultureInfo.InvariantCulture);
        }

        public static void SetSheet1(SheetView _Sheet)
        {
            var header = _Sheet.ColumnHeader;
            header.RowCount = 2;

            if ((InputDataService.Instance.ResultDimension ?? InputDataService.Instance.dimension) == 3)
            {
                _Sheet.ColumnCount = 7;

                header.Cells[0, 0].Text = "節点";
                header.Cells[1, 0].Text = "No";
                header.Cells[0, 1].Text = "移動量(mm)";
                header.Cells[1, 1].Text = "X方向";
                header.Cells[0, 2].Text = "";
                header.Cells[1, 2].Text = "Y方向";
                header.Cells[0, 3].Text = "";
                header.Cells[1, 3].Text = "Z方向";
                header.Cells[0, 4].Text = "回転(‰rad)";
                header.Cells[1, 4].Text = "X軸回り";
                header.Cells[0, 5].Text = "";
                header.Cells[1, 5].Text = "Y軸回り";
                header.Cells[0, 6].Text = "";
                header.Cells[1, 6].Text = "Z軸回り";

                header.Cells[0, 1].ColumnSpan = 3;
                header.Cells[0, 4].ColumnSpan = 3;

                var column = _Sheet.Columns;
                column[0].Width = 50;
            }
            else
            {
                _Sheet.ColumnCount = 4;

                header.Cells[0, 0].Text = "節点";
                header.Cells[1, 0].Text = "No";
                header.Cells[0, 1].Text = "移動量(mm)";
                header.Cells[1, 1].Text = "X方向";
                header.Cells[0, 2].Text = "";
                header.Cells[1, 2].Text = "Y方向";
                header.Cells[0, 3].Text = "回転";
                header.Cells[1, 3].Text = "(‰rad)";

                header.Cells[0, 1].ColumnSpan = 2;

                var column = _Sheet.Columns;
                column[0].Width = 50;
                for (var i = 1; i < column.Count; i++)
                {
                    column[i].Width = 80;
                }
            }
        }


    }
}
