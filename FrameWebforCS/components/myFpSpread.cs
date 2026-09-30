using FarPoint.Win.Spread;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrameWebforCS.components
{
    internal class myFpSpread : FarPoint.Win.Spread.FpSpread
    {
        private sealed record RowOperations(
            Func<int, int, bool> Insert,
            Func<IReadOnlyList<int>, int, bool> Delete);

        private readonly Dictionary<SheetView, RowOperations> _rowOperations = new();
        private SheetView? _rowHeaderSheet;

        public myFpSpread() 
        {
            AccessibleDescription = "";
            Font = new Font("ＭＳ ゴシック", 9F);
            KeyDown += myFpSpread_KeyDown;
            MouseDown += myFpSpread_MouseDown;
            ActiveSheetChanged += myFpSpread_ActiveSheetChanged;
        }

        // Row mutations belong to each sheet's service. Sheets without a
        // registration retain only the ordinary cell-value Delete behavior.
        internal void EnableRowOperations(
            SheetView sheet,
            Func<int, int, bool> insert,
            Func<IReadOnlyList<int>, int, bool> delete)
        {
            ArgumentNullException.ThrowIfNull(sheet);
            ArgumentNullException.ThrowIfNull(insert);
            ArgumentNullException.ThrowIfNull(delete);
            _rowOperations[sheet] = new RowOperations(insert, delete);
        }

        internal void DisableRowOperations(SheetView sheet)
        {
            _rowOperations.Remove(sheet);
            if (ReferenceEquals(_rowHeaderSheet, sheet)) _rowHeaderSheet = null;
        }

        internal void ClearRowHeaderIntent() => _rowHeaderSheet = null;

        public SheetView AddNewSheetView()
        {
            var fpSpread1_Sheet1 = base.AddNewSheetView();
            fpSpread1_Sheet1.DataAutoSizeColumns = false; // 再バインド時に Spread が列幅を自動調整しないようにする

            return fpSpread1_Sheet1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _rowOperations.Clear();
                _rowHeaderSheet = null;
                foreach (SheetView sheet in Sheets)
                {
                    if (sheet.DataSource != null)
                        sheet.DataSource = null;
                }
            }

            base.Dispose(disposing);
        }

        private void myFpSpread_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Modifiers != Keys.None || EditMode)
                return;

            var sheet = ActiveSheet;
            // GroupDataModel wraps the bound target model, but Spread reports
            // DataSource as null after the wrapper is installed.
            if (sheet == null || sheet.DataSource == null && !_rowOperations.ContainsKey(sheet))
                return;

            if (e.KeyCode is Keys.Oem5 or Keys.Oem102)
            {
                if (!_rowOperations.TryGetValue(sheet, out var operations)) return;
                operations.Insert(sheet.ActiveRowIndex, sheet.ActiveColumnIndex);
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode != Keys.Delete) return;

            if (ReferenceEquals(_rowHeaderSheet, sheet) &&
                _rowOperations.TryGetValue(sheet, out var rowOperations) &&
                TryGetSelectedHeaderRows(sheet, out var selectedRows))
            {
                if (selectedRows.Count == 0 || rowOperations.Delete(selectedRows, sheet.ActiveColumnIndex))
                    _rowHeaderSheet = null;
                e.SuppressKeyPress = true;
                return;
            }

            int row = sheet.ActiveRowIndex;
            int column = sheet.ActiveColumnIndex;
            if (row < 0 || row >= sheet.RowCount || column < 0 || column >= sheet.ColumnCount)
                return;

            if (sheet.Protect && sheet.GetStyleInfo(row, column).Locked)
                return;

            sheet.Cells[row, column].Value = null;
            e.SuppressKeyPress = true;
        }

        private void myFpSpread_MouseDown(object? sender, MouseEventArgs e)
        {
            var sheet = ActiveSheet;
            _rowHeaderSheet = sheet != null && _rowOperations.ContainsKey(sheet) &&
                HitTest(e.X, e.Y).Type == HitTestType.RowHeader ? sheet : null;
        }

        private void myFpSpread_ActiveSheetChanged(object? sender, EventArgs e) =>
            _rowHeaderSheet = null;

        private static bool TryGetSelectedHeaderRows(SheetView sheet, out IReadOnlyList<int> rows)
        {
            var selections = sheet.GetSelections();
            if (selections.Length == 0 || selections.Any(range =>
                    range.Column != -1 || range.ColumnCount != -1))
            {
                rows = Array.Empty<int>();
                return false;
            }

            var indices = new HashSet<int>();
            foreach (var range in selections)
            {
                int end = range.RowCount < 0 ? sheet.RowCount :
                    Math.Min(sheet.RowCount, range.Row + range.RowCount);
                for (int row = Math.Max(0, range.Row); row < end; row++)
                    indices.Add(row);
            }
            rows = indices.OrderBy(row => row).ToArray();
            return true;
        }

        // locked 設定してるセルの編集を禁止する
        public void faSpread_EditModeOn(object sender, EventArgs e)
        {
            FpSpread? fp = sender as FpSpread;
            if (fp == null) return;

            Cell? targetCell = fp.ActiveSheet.ActiveCell;
            if (targetCell.BackColor == SystemColors.Control)
            {
                fp.StopCellEditing();
                return;
            }
        }
    }
}
