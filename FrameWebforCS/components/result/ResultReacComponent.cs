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
    public partial class ResultReacComponent : UserControl
    {
        private readonly ResultReacService _input = ResultReacService.Instance;

        public ResultReacComponent()
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
            var cases = _input.getReac();
            if (Visible && index >= 0 && index < cases.Count)
                FrameWebforCS.three.ThreeResultsService.PublishPage("reac", cases.Keys.ElementAt(index));
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
            foreach (var result in _input.getReac())
            {
                SheetView sheet = fpSpread1.AddNewSheetView();
                sheet.SheetName = result.Key.Replace("Case", "", StringComparison.Ordinal);
                SetSheet1(sheet);
                for (int column = 0; column < sheet.ColumnCount; column++)
                    sheet.Columns[column].CellType = new FarPoint.Win.Spread.CellType.TextCellType();
                sheet.RowCount = result.Value.Count;
                int row = 0;
                foreach (var node in result.Value)
                {
                    sheet.Cells[row, 0].Text = node.Key.Replace("node", "", StringComparison.Ordinal);
                    sheet.Cells[row, 1].Text = Format(node.Value.tx);
                    sheet.Cells[row, 2].Text = Format(node.Value.ty);
                    if (sheet.ColumnCount == 7)
                    {
                        sheet.Cells[row, 3].Text = Format(node.Value.tz);
                        sheet.Cells[row, 4].Text = Format(node.Value.mx);
                        sheet.Cells[row, 5].Text = Format(node.Value.my);
                        sheet.Cells[row, 6].Text = Format(node.Value.mz);
                    }
                    else sheet.Cells[row, 3].Text = Format(node.Value.mz);
                    row++;
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
            double rounded = Math.Floor((value ?? 0) * 100 + 0.5) / 100;
            return rounded.ToString("F2", CultureInfo.InvariantCulture);
        }

        internal static void SetSheet1(SheetView _Sheet)
        {
            var header = _Sheet.ColumnHeader;
            header.RowCount = 2;

            if ((InputDataService.Instance.ResultDimension ?? InputDataService.Instance.dimension) == 3)
            {
                _Sheet.ColumnCount = 7;

                header.Cells[0, 0].Text = "節点";
                header.Cells[1, 0].Text = "No";
                header.Cells[0, 1].Text = "支点反力(KN)";
                header.Cells[1, 1].Text = "X方向";
                header.Cells[0, 2].Text = "";
                header.Cells[1, 2].Text = "Y方向";
                header.Cells[0, 3].Text = "";
                header.Cells[1, 3].Text = "Z方向";
                header.Cells[0, 4].Text = "回転反力(kN・m)";
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
                header.Cells[0, 1].Text = "支点反力(KN)";
                header.Cells[1, 1].Text = "X方向";
                header.Cells[0, 2].Text = "";
                header.Cells[1, 2].Text = "Y方向";
                header.Cells[0, 3].Text = "回転反力";
                header.Cells[1, 3].Text = "(kN・m)";

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
