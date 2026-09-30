using FarPoint.Win.Spread;
using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using static THREE.ArcballControls;

namespace FrameWebforCS.components.input
{
    public partial class InputFixMemberComponent : UserControl
    {
        internal event Action<int, string>? GridSelectionChanged;
        private bool _syncingSelection;
        private InputDataService _input = InputDataService.Instance;

        public InputFixMemberComponent()
        {
            InitializeComponent();

            for (int i = 0; i < InputFixMemberService.TypeCount; i++)
            {
                var fpSpread1_Sheet1 = fpSpread1.AddNewSheetView();

                fpSpread1_Sheet1.SheetName = (i + 1).ToString();
                fpSpread1_Sheet1.AutoGenerateColumns = false;
                fpSpread1_Sheet1.DataAutoCellTypes = false;
                fpSpread1_Sheet1.DataAutoHeadings = false;
                fpSpread1_Sheet1.RowHeaderAutoText = HeaderAutoText.Numbers;
                fpSpread1_Sheet1.StartingRowNumber = 1;

                setColumn(fpSpread1_Sheet1);
                fpSpread1_Sheet1.DataSource = InputFixMemberService.Instance.GetRows(fpSpread1_Sheet1.SheetName);
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
            InputFixMemberService.Instance.SelectCase(fpSpread1.ActiveSheet.SheetName);

        }

        private void OnActiveSheetChanged(object? sender, EventArgs e) =>
            InputFixMemberService.Instance.SelectCase(fpSpread1.ActiveSheet.SheetName);

        private void OnEnterCell(object? sender, EnterCellEventArgs e)
        {
            if (_syncingSelection || e.Row < 0 || e.Column < 0) return;
            var sheet = fpSpread1.ActiveSheet;
            string axis = sheet.Columns[e.Column].DataField?.ToLowerInvariant() ?? "";
            // JS InputFixMemberComponent.selectEnd passes the 1-based row and field key.
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
            if (fpSpread1_Sheet1.ColumnCount > 1) header.Cells[0, 1].ColumnSpan = 1;
            header.RowCount = 2;

            if (_input.dimension == 3)
            {
                fpSpread1_Sheet1.ColumnCount = 6;

                header.Cells[0, 0].Text = "部材";
                header.Cells[1, 0].Text = "No";
                header.Cells[0, 1].Text = "部材長";
                header.Cells[1, 1].Text = "(m)";
                header.Cells[0, 2].Text = "変位拘束";
                header.Cells[1, 2].Text = "部材軸方向";
                header.Cells[0, 3].Text = "";
                header.Cells[1, 3].Text = "部材Y軸";
                header.Cells[0, 4].Text = "";
                header.Cells[1, 4].Text = "部材Z軸";
                header.Cells[0, 5].Text = "回転拘束";
                header.Cells[1, 5].Text = "(kNm/rad/m)";

                header.Cells[0, 2].ColumnSpan = 3;

                var column = fpSpread1_Sheet1.Columns;
                string[] fields = ["M", "", "Tx", "Ty", "Tz", "Tr"];
                column[0].DataField = fields[0];
                for (int i = 2; i < fields.Length; i++)
                    column[i].DataField = fields[i];

                column[0].Width = 50;
                column[1].Width = 80;
                for (var i = 2; i < column.Count; i++)
                {
                    column[i].Width = 100;
                }
            }
            else
            {
                fpSpread1_Sheet1.ColumnCount = 4;

                header.Cells[0, 0].Text = "部材";
                header.Cells[1, 0].Text = "No";
                header.Cells[0, 1].Text = "部材長";
                header.Cells[1, 1].Text = "(m)";
                header.Cells[0, 2].Text = "部材軸方向";
                header.Cells[1, 2].Text = "(kN/m/m)";
                header.Cells[0, 3].Text = "部材直角方向";
                header.Cells[1, 3].Text = "(kN/m/m)";

                var column = fpSpread1_Sheet1.Columns;
                column[0].DataField = "M";
                column[2].DataField = "Tx";
                column[3].DataField = "Ty";

                column[0].Width = 50;
                column[2].Width = 80;
                for (var i = 2; i < column.Count; i++)
                {
                    column[i].Width = 100;
                }
            }
        }

        internal void RefreshDimension()
        {
            if (fpSpread1.EditMode) fpSpread1.StopCellEditing();
            foreach (SheetView sheet in fpSpread1.Sheets) setColumn(sheet);
            float width = 100;
            foreach (Column column in fpSpread1.Sheets[0].Columns) width += column.Width;
            Width = (int)width;
        }
    }
}
