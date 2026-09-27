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
    public partial class InputFixNodeComponent : UserControl
    {
        internal event Action<int, string>? GridSelectionChanged;
        private bool _syncingSelection;
        private InputDataService _input = InputDataService.Instance;
        private const int type_count = 6;


        public InputFixNodeComponent()
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
                fpSpread1_Sheet1.DataSource = InputFixNodeService.Instance.GetRows(fpSpread1_Sheet1.SheetName);
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
            InputFixNodeService.Instance.SelectCase(fpSpread1.ActiveSheet.SheetName);

        }

        private void OnActiveSheetChanged(object? sender, EventArgs e) =>
            InputFixNodeService.Instance.SelectCase(fpSpread1.ActiveSheet.SheetName);

        private void OnEnterCell(object? sender, EnterCellEventArgs e)
        {
            if (_syncingSelection || e.Row < 0 || e.Column < 0) return;
            var sheet = fpSpread1.ActiveSheet;
            string axis = sheet.Columns[e.Column].DataField?.ToLowerInvariant() ?? "";
            // JS InputFixNodeComponent.selectEnd passes the 1-based row and field key.
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
            header.RowCount = 2;

            if (_input.dimension == 3)
            {
                fpSpread1_Sheet1.ColumnCount = 7;

                header.Cells[0, 0].Text = "節点";
                header.Cells[1, 0].Text = "No";
                header.Cells[0, 1].Text = "変位拘束";
                header.Cells[1, 1].Text = "X方向";
                header.Cells[0, 2].Text = "";
                header.Cells[1, 2].Text = "Y方向";
                header.Cells[0, 3].Text = "";
                header.Cells[1, 3].Text = "Z方向";
                header.Cells[0, 4].Text = "回転拘束";
                header.Cells[1, 4].Text = "X軸回り";
                header.Cells[0, 5].Text = "";
                header.Cells[1, 5].Text = "Y軸回り";
                header.Cells[0, 6].Text = "";
                header.Cells[1, 6].Text = "Z軸回り";

                header.Cells[0, 1].ColumnSpan = 3;
                header.Cells[0, 4].ColumnSpan = 3;

                var column = fpSpread1_Sheet1.Columns;
                string[] fields = ["N", "Tx", "Ty", "Tz", "Rx", "Ry", "Rz"];
                for (int i = 0; i < fields.Length; i++)
                    column[i].DataField = fields[i];

                column[0].Width = 50;
            }
            else
            {
                fpSpread1_Sheet1.ColumnCount = 4;

                header.Cells[0, 0].Text = "節点";
                header.Cells[1, 0].Text = "No";
                header.Cells[0, 1].Text = "変位拘束";
                header.Cells[1, 1].Text = "X方向";
                header.Cells[0, 2].Text = "";
                header.Cells[1, 2].Text = "Y方向";
                header.Cells[0, 3].Text = "回転拘束";
                header.Cells[1, 3].Text = "(kN・m/rad)";

                header.Cells[0, 1].ColumnSpan = 2;

                var column = fpSpread1_Sheet1.Columns;
                string[] fields = ["N", "Tx", "Ty", "Rz"];
                for (int i = 0; i < fields.Length; i++)
                    column[i].DataField = fields[i];

                column[0].Width = 50;
                for (var i = 1; i < column.Count; i++)
                {
                    column[i].Width = 80;
                }
            }
        }
    }
}
