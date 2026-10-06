using FarPoint.Win.Spread;
using FarPoint.Win.Spread.CellType;
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
    public partial class InputNoticePointsComponent : UserControl
    {
        internal event Action<int, string>? GridSelectionChanged;
        private bool _syncingSelection;
        private FarPoint.Win.Spread.SheetView fpSpread1_Sheet1;

        public InputNoticePointsComponent()
        {
            InitializeComponent();

            fpSpread1_Sheet1 = fpSpread1.AddNewSheetView();

            fpSpread1_Sheet1.SheetName = "着目点";
            fpSpread1_Sheet1.AutoGenerateColumns = false;
            fpSpread1_Sheet1.DataAutoCellTypes = false;
            fpSpread1_Sheet1.DataAutoHeadings = false;
            fpSpread1_Sheet1.RowHeaderAutoText = HeaderAutoText.Numbers;
            fpSpread1_Sheet1.StartingRowNumber = 1;
            fpSpread1_Sheet1.DataSource = InputNoticePointsService.Instance.NoticePoints;

            fpSpread1_Sheet1.ColumnCount = 22;

            var header = fpSpread1_Sheet1.ColumnHeader;
            header.RowCount = 2;

            header.Cells[0, 0].Text = "部材";
            header.Cells[1, 0].Text = "No";
            header.Cells[0, 1].Text = "部材長";
            header.Cells[1, 1].Text = "(m)";
            header.Cells[0, 2].Text = "i端からの距離(m)";
            header.Cells[0, 2].HorizontalAlignment = CellHorizontalAlignment.Left;

            var column = fpSpread1_Sheet1.Columns;

            column[0].DataField = "M";
            for (int i = 2; i < column.Count; i++)
                column[i].DataField = "P" + (i - 1).ToString();

            for (int i = 2; i < column.Count; i++)
            {
                header.Cells[1, i].Text = "L" + (i - 1);
                column[i].Width = 80;
                column[i].CellType = new GeneralCellType { FormatString = "F3" };
            }

            header.Cells[0, 2].ColumnSpan = column.Count - 2;

            column[0].Width = 50;
            column[1].Width = 80;

            for (int i = 0; i < column.Count; i++)
                column[i].Locked = false;
            column[1].Locked = true;
            column[1].BackColor = SystemColors.Control;
            fpSpread1_Sheet1.Protect = true;

            fpSpread1.EnterCell += OnEnterCell;
            Disposed += (_, _) => fpSpread1.EnterCell -= OnEnterCell;
        }

        private void OnEnterCell(object? sender, EnterCellEventArgs e)
        {
            if (_syncingSelection || e.Row < 0 || e.Column < 0) return;
            // JS notice-point selection identifies the row and L1..L20 point.
            string axis = e.Column >= 2 ? $"L{e.Column - 1}" : "";
            GridSelectionChanged?.Invoke(e.Row + 1, axis);
        }

        internal bool SelectGridRow(int row, string? axis = null)
        {
            if (IsDisposed || row < 1 || row > fpSpread1_Sheet1.RowCount) return false;
            int column = 0;
            if (axis != null && axis.StartsWith("L", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(axis.AsSpan(1), out int point) && point is >= 1 and <= 20)
                column = point + 1;
            _syncingSelection = true;
            try
            {
                fpSpread1_Sheet1.SetActiveCell(row - 1, column);
            }
            finally { _syncingSelection = false; }
            return true;
        }




    }
}
