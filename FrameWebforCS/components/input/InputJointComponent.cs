using FarPoint.Win.Spread;
using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FrameWebforCS.components.input
{
    public partial class InputJointComponent : UserControl
    {
        internal event Action<int, string>? GridSelectionChanged;
        private bool _syncingSelection;
        private InputDataService _input = InputDataService.Instance;
        private const int type_count = 6;


        public InputJointComponent()
        {
            InitializeComponent();

            for (int i = 0; i < type_count; i++)
            {

                var fpSpread1_Sheet1 = fpSpread1.AddNewSheetView();

                fpSpread1_Sheet1.SheetName = (i + 1).ToString();
                fpSpread1_Sheet1.AutoGenerateColumns = false;
                fpSpread1_Sheet1.DataAutoCellTypes = false;
                fpSpread1_Sheet1.DataAutoHeadings = false;
                fpSpread1_Sheet1.RowHeaderAutoText = HeaderAutoText.Numbers;
                fpSpread1_Sheet1.StartingRowNumber = 1;

                setColumn(fpSpread1_Sheet1);
                fpSpread1_Sheet1.DataSource = InputJointService.Instance.GetRows(fpSpread1_Sheet1.SheetName);
            }

            float w = 0;
            FarPoint.Win.Spread.SheetView fs = (SheetView)fpSpread1.Sheets.First();
            var col = fs.Columns;
            for (int i = 0; i < col.Count; i++)
            {
                w += col[i].Width;
            }
            w += 100;

            this.Width = (int)w;

            fpSpread1.EnterCell += OnEnterCell;
            fpSpread1.ActiveSheetChanged += OnActiveSheetChanged;
            Disposed += (_, _) =>
            {
                fpSpread1.EnterCell -= OnEnterCell;
                fpSpread1.ActiveSheetChanged -= OnActiveSheetChanged;
            };
            InputJointService.Instance.SelectCase(fpSpread1.ActiveSheet.SheetName);

        }

        private void OnActiveSheetChanged(object? sender, EventArgs e) =>
            InputJointService.Instance.SelectCase(fpSpread1.ActiveSheet.SheetName);

        private void OnEnterCell(object? sender, EnterCellEventArgs e)
        {
            if (_syncingSelection || e.Row < 0 || e.Column < 0) return;
            var sheet = fpSpread1.ActiveSheet;
            string axis = sheet.Columns[e.Column].DataField?.ToLowerInvariant() ?? "";
            // JS InputJointComponent.selectEnd uses the row and xi..zj field key.
            GridSelectionChanged?.Invoke(e.Row + 1, axis);
        }

        internal bool SelectGridRow(int row, string? axis = null, string? caseId = null)
        {
            if (IsDisposed || row < 1) return false;
            int sheetIndex = caseId == null ? fpSpread1.ActiveSheetIndex :
                fpSpread1.Sheets.Cast<SheetView>().ToList().FindIndex(s => s.SheetName == caseId);
            if (sheetIndex < 0) return false;
            var sheet = fpSpread1.Sheets[sheetIndex];
            if (row > sheet.RowCount) return false;
            int column = 0;
            if (axis != null)
                for (int i = 0; i < sheet.ColumnCount; i++)
                    if (string.Equals(sheet.Columns[i].DataField, axis,
                            StringComparison.OrdinalIgnoreCase)) { column = i; break; }
            _syncingSelection = true;
            try
            {
                fpSpread1.ActiveSheetIndex = sheetIndex;
                sheet.SetActiveCell(row - 1, column);
            }
            finally { _syncingSelection = false; }
            return true;
        }

        private void setColumn(FarPoint.Win.Spread.SheetView fpSpread1_Sheet1)
        {
            var header = fpSpread1_Sheet1.ColumnHeader;

            if (_input.dimension == 3)
            {
                header.RowCount = 2;
                fpSpread1_Sheet1.ColumnCount = 7;

                header.Cells[0, 0].Text = "部材";
                header.Cells[1, 0].Text = "No";
                header.Cells[0, 1].Text = "i端";
                header.Cells[1, 1].Text = "X";
                header.Cells[0, 2].Text = "";
                header.Cells[1, 2].Text = "Y";
                header.Cells[0, 3].Text = "";
                header.Cells[1, 3].Text = "Z";
                header.Cells[0, 4].Text = "j端";
                header.Cells[1, 4].Text = "X";
                header.Cells[0, 5].Text = "";
                header.Cells[1, 5].Text = "Y";
                header.Cells[0, 6].Text = "";
                header.Cells[1, 6].Text = "Z";

                header.Cells[0, 1].ColumnSpan = 3;
                header.Cells[0, 4].ColumnSpan = 3;

                var column = fpSpread1_Sheet1.Columns;
                string[] fields = ["M", "Xi", "Yi", "Zi", "Xj", "Yj", "Zj"];
                for (int i = 0; i < column.Count; i++)
                {
                    column[i].DataField = fields[i];
                    column[i].Width = 50;
                }
            }
            else
            {
                header.RowCount = 1;
                fpSpread1_Sheet1.ColumnCount = 3;

                header.Cells[0, 0].Text = "部材No";
                header.Cells[0, 1].Text = "i端";
                header.Cells[0, 2].Text = "j端";

                var column = fpSpread1_Sheet1.Columns;
                string[] fields = ["M", "Zi", "Zj"];
                for (int i = 0; i < fields.Length; i++)
                    column[i].DataField = fields[i];

                column[0].Width = 80;
                column[1].Width = 50;
                column[2].Width = 50;

            }
        }

    }
}

