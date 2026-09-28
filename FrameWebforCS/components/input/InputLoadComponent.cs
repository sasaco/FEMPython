using FarPoint.Win.Spread;
using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FrameWebforCS.components.input
{
    public partial class InputLoadComponent : UserControl
    {
        internal event Action<int, string>? GridSelectionChanged;
        internal event Action<string>? ActiveLoadDisplayModeChanged;
        internal string ActiveLoadDisplayMode =>
            ReferenceEquals(fpSpread1.ActiveSheet, fpSpread1_Sheet2) ? "load_values" : "load_names";
        private bool _syncingSelection;
        private bool _rowHeaderSelection;
        private readonly HashSet<int> _coloredIntensityRows = new();
        private readonly HashSet<int> _pendingColorRows = new();
        private bool _pendingAllRowColors;
        private bool _colorRefreshQueued;
        private int _intensityRevision;
        private int _selectionRevision;
        private int _handleRevision;
        private readonly InputLoadService _service = InputLoadService.Instance;
        private InputDataService _input = InputDataService.Instance;
        private FarPoint.Win.Spread.SheetView fpSpread1_Sheet1;
        private FarPoint.Win.Spread.SheetView fpSpread1_Sheet2;

        public InputLoadComponent()
        {
            InitializeComponent();

            fpSpread1_Sheet1 = fpSpread1.AddNewSheetView();

            SetSheet1();

            fpSpread1_Sheet2 = fpSpread1.AddNewSheetView();

            SetSheet2();

            _service.IntensityRowMoved += OnIntensityRowMoved;
            _service.IntensityRows.ListChanged += OnIntensityRowsChanged;

            float w = 0;
            var col = fpSpread1_Sheet2.Columns;
            for (int i = 0; i < col.Count; i++)
            {
                w += col[i].Width;
            }
            w += 100;

            this.Width = (int)w;
            fpSpread1.EnterCell += OnEnterCell;
            fpSpread1.MouseDown += OnSpreadMouseDown;
            fpSpread1.KeyDown += OnSpreadKeyDown;
            fpSpread1.DeleteKeyInterceptor = DeleteSelectedIntensityRows;
            fpSpread1.ActiveSheetChanged += OnActiveSheetChanged;
            HandleCreated += OnDisplayActivated;
            HandleDestroyed += OnDisplayHandleDestroyed;
            VisibleChanged += OnDisplayActivated;
            Disposed += (_, _) =>
            {
                _service.IntensityRowMoved -= OnIntensityRowMoved;
                _service.IntensityRows.ListChanged -= OnIntensityRowsChanged;
                fpSpread1.EnterCell -= OnEnterCell;
                fpSpread1.MouseDown -= OnSpreadMouseDown;
                fpSpread1.KeyDown -= OnSpreadKeyDown;
                fpSpread1.DeleteKeyInterceptor = null;
                fpSpread1.ActiveSheetChanged -= OnActiveSheetChanged;
                HandleCreated -= OnDisplayActivated;
                HandleDestroyed -= OnDisplayHandleDestroyed;
                VisibleChanged -= OnDisplayActivated;
            };
        }

        private void OnActiveSheetChanged(object? sender, EventArgs e)
        {
            _selectionRevision++;
            _rowHeaderSelection = false;
            SelectFocusedCase();
            ActiveLoadDisplayModeChanged?.Invoke(ActiveLoadDisplayMode);
        }

        private void OnSpreadMouseDown(object? sender, MouseEventArgs e)
        {
            _selectionRevision++;
            _rowHeaderSelection = ReferenceEquals(fpSpread1.ActiveSheet, fpSpread1_Sheet2) &&
                fpSpread1.HitTest(e.X, e.Y).Type == HitTestType.RowHeader;
        }

        private void OnIntensityRowMoved(string caseId, int row)
        {
            // The service reorders the bound list while Spread commits the edit.
            // Move the active cell only after that binding transaction has ended.
            int intensityRevision = _intensityRevision;
            int selectionRevision = _selectionRevision;
            if (!IsDisposed && !Disposing && IsHandleCreated)
                BeginInvoke((System.Action)(() =>
                {
                    if (!IsDisposed && !Disposing && IsHandleCreated &&
                        intensityRevision == _intensityRevision &&
                        selectionRevision == _selectionRevision &&
                        SelectGridRow(row, "LoadId", caseId))
                        GridSelectionChanged?.Invoke(row, "loadid");
                }));
        }

        private void OnSpreadKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Modifiers != Keys.None || e.KeyCode is not (Keys.Oem5 or Keys.Oem102) ||
                fpSpread1.EditMode || !ReferenceEquals(fpSpread1.ActiveSheet, fpSpread1_Sheet2))
                return;

            int displayIndex = fpSpread1_Sheet2.ActiveRowIndex;
            int columnIndex = fpSpread1_Sheet2.ActiveColumnIndex;
            bool inserted;
            _syncingSelection = true;
            try { inserted = _service.InsertIntensityRowAt(displayIndex); }
            finally { _syncingSelection = false; }
            if (inserted)
            {
                _selectionRevision++;
                SelectIntensityDisplayRow(displayIndex, columnIndex, publishSelection: true);
            }
            e.SuppressKeyPress = true;
        }

        private bool DeleteSelectedIntensityRows(SheetView sheet)
        {
            if (!ReferenceEquals(sheet, fpSpread1_Sheet2) || !_rowHeaderSelection)
                return false;

            var selections = sheet.GetSelections();
            // Row-header selections use column -1. A cell range spanning every
            // visible column must still use the ordinary cell-value Delete path.
            if (selections.Length == 0 || selections.Any(range =>
                    range.Column != -1 || range.ColumnCount != -1))
                return false;

            var indices = new HashSet<int>();
            foreach (var range in selections)
            {
                int end = range.RowCount < 0 ? sheet.RowCount :
                    Math.Min(sheet.RowCount, range.Row + range.RowCount);
                for (int row = Math.Max(0, range.Row); row < end; row++)
                    indices.Add(row);
            }
            if (indices.Count == 0) return true;

            int firstIndex = indices.Min();
            int columnIndex = sheet.ActiveColumnIndex;
            bool deleted;
            _syncingSelection = true;
            try { deleted = _service.DeleteIntensityRowsAt(indices); }
            finally { _syncingSelection = false; }
            if (deleted)
            {
                _selectionRevision++;
                _rowHeaderSelection = false;
                int next = Math.Min(firstIndex, sheet.RowCount - 1);
                if (next >= 0)
                    SelectIntensityDisplayRow(next, columnIndex, publishSelection: true);
            }
            return true;
        }

        private void OnDisplayActivated(object? sender, EventArgs e)
        {
            if (Visible && !IsDisposed)
            {
                SelectFocusedCase();
                ActiveLoadDisplayModeChanged?.Invoke(ActiveLoadDisplayMode);
                RefreshIntensityRowColors();
                _pendingAllRowColors = false;
                _pendingColorRows.Clear();
            }
        }

        private void SelectFocusedCase()
        {
            if (_syncingSelection || !ReferenceEquals(fpSpread1.ActiveSheet, fpSpread1_Sheet2))
                return;
            clsLoadIntensityRow? row = _service.GetIntensityRowAt(fpSpread1_Sheet2.ActiveRowIndex);
            if (row?.IsAssigned == true) _service.SelectCase(row.CaseId);
        }

        private void OnEnterCell(object? sender, EnterCellEventArgs e)
        {
            if (_syncingSelection || e.Row < 0 || e.Column < 0 ||
                !ReferenceEquals(fpSpread1.ActiveSheet, fpSpread1_Sheet2)) return;
            _selectionRevision++;
            clsLoadIntensityRow? row = _service.GetIntensityRowAt(e.Row);
            if (row?.IsAssigned != true) return;
            _service.SelectCase(row.CaseId);
            string column = fpSpread1_Sheet2.Columns[e.Column].DataField?.ToLowerInvariant() ?? "";
            GridSelectionChanged?.Invoke(row.Row, column);
        }

        internal bool SelectGridRow(int row, string? column = null, string? caseId = null)
        {
            if (IsDisposed || row < 1) return false;
            string targetCase = caseId ?? _service.SelectedCaseId;
            int displayIndex = _service.FindIntensityRowIndex(targetCase, row);
            if (displayIndex < 0) return false;
            int columnIndex = 1;
            if (column != null)
                for (int i = 0; i < fpSpread1_Sheet2.ColumnCount; i++)
                    if (string.Equals(fpSpread1_Sheet2.Columns[i].DataField, column,
                            StringComparison.OrdinalIgnoreCase)) { columnIndex = i; break; }
            return SelectIntensityDisplayRow(displayIndex, columnIndex);
        }

        private void OnDisplayHandleDestroyed(object? sender, EventArgs e)
        {
            _handleRevision++;
            _selectionRevision++;
            _colorRefreshQueued = false;
        }

        private bool SelectIntensityDisplayRow(int displayIndex, int columnIndex,
            bool publishSelection = false)
        {
            clsLoadIntensityRow? row = _service.GetIntensityRowAt(displayIndex);
            if (IsDisposed || Disposing || row == null || fpSpread1_Sheet2.ColumnCount == 0)
                return false;
            _selectionRevision++;
            columnIndex = Math.Clamp(columnIndex, 0, fpSpread1_Sheet2.ColumnCount - 1);
            _syncingSelection = true;
            try
            {
                _rowHeaderSelection = false;
                if (row.IsAssigned) _service.SelectCase(row.CaseId);
                fpSpread1.ActiveSheetIndex = 1;
                fpSpread1_Sheet2.ClearSelection();
                fpSpread1_Sheet2.SetActiveCell(displayIndex, columnIndex);
            }
            finally { _syncingSelection = false; }
            if (publishSelection && row.IsAssigned)
                GridSelectionChanged?.Invoke(row.Row,
                    fpSpread1_Sheet2.Columns[columnIndex].DataField?.ToLowerInvariant() ?? "");
            return true;
        }

        private void SetSheet1()
        {
            fpSpread1_Sheet1.SheetName = "荷重名称";
            ConfigureRows(fpSpread1_Sheet1);

            var column = fpSpread1_Sheet1.Columns;

            var header = fpSpread1_Sheet1.ColumnHeader;

            fpSpread1_Sheet1.ColumnCount = 7;
            string[] fields = { "LL_pitch", "symbol", "name", "fix_node", "fix_member", "element", "joint" };
            for (int i = 0; i < fields.Length; i++)
                column[i].DataField = fields[i];

            header.Cells[0, 0].Text = "割増係数";
            header.Cells[0, 1].Text = "記号";
            header.Cells[0, 2].Text = "名称";
            header.Cells[0, 3].Text = "支点";
            header.Cells[0, 4].Text = "断面";
            header.Cells[0, 5].Text = "バネ";
            header.Cells[0, 6].Text = "結合";

            column[2].Width = 400;
            column[3].Width = 50;
            column[4].Width = 50;
            column[5].Width = 50;
            column[6].Width = 50;
            fpSpread1_Sheet1.DataSource = _service.LoadNames;
        }

        private void SetSheet2()
        {
            fpSpread1_Sheet2.SheetName = "荷重強度";
            ConfigureRows(fpSpread1_Sheet2);
            fpSpread1_Sheet2.DataAutoSizeColumns = false; // 再バインド時に Spread が列幅を自動調整しないようにする

            // データソースを割り付け
            var column = fpSpread1_Sheet2.Columns;
            fpSpread1_Sheet2.ColumnCount = 16;
            string[] fields = { "LoadId", "m1", "m2", "direction", "mark", "L1", "L2",
                "P1", "P2", "n", "tx", "ty", "tz", "rx", "ry", "rz" };
            for (int i = 0; i < fields.Length; i++)
                column[i].DataField = fields[i];
            if (_input.dimension == 2)
            {
                fpSpread1_Sheet2.ColumnCount = 13;
                column[12].DataField = "rz";
            }
            fpSpread1_Sheet2.DataSource = _service.IntensityRows;

            // 見た目の制御
            var header = fpSpread1_Sheet2.ColumnHeader;
            if (fpSpread1_Sheet2.ColumnCount > 1) header.Cells[0, 1].ColumnSpan = 1;
            if (fpSpread1_Sheet2.ColumnCount > 9) header.Cells[0, 9].ColumnSpan = 1;
            header.RowCount = 3;

            for (int i = 0; i < column.Count; i++)
                column[i].Locked = false;
            column[0].Locked = false;
            fpSpread1_Sheet2.Protect = true;
            fpSpread1_Sheet2.SelectionPolicy = FarPoint.Win.Spread.Model.SelectionPolicy.MultiRange;

            header.Cells[0, 0].Text = "実荷重番号";
            header.Cells[1, 0].Text = "";
            header.Cells[2, 0].Text = "";

            header.Cells[0, 1].Text = "要素荷重";
            header.Cells[1, 1].Text = "部材No";
            header.Cells[2, 1].Text = "1";
            header.Cells[1, 2].Text = "";
            header.Cells[2, 2].Text = "2";
            header.Cells[1, 3].Text = "方向";
            header.Cells[2, 3].Text = "(x,y,z)";
            header.Cells[1, 4].Text = "マーク";
            header.Cells[2, 4].Text = "(1,2,9,11)";
            header.Cells[1, 5].Text = "L1";
            header.Cells[2, 5].Text = "(m)";
            header.Cells[1, 6].Text = "L2";
            header.Cells[2, 6].Text = "(m)";
            header.Cells[1, 7].Text = "P1";
            header.Cells[2, 7].Text = "(kN/m)";
            header.Cells[1, 8].Text = "P2";
            header.Cells[2, 8].Text = "(kN/m)";

            header.Cells[0, 0].RowSpan = 3;
            header.Cells[0, 1].ColumnSpan = 8;
            header.Cells[1, 1].ColumnSpan = 2;

            column[0].Width = 50;
            column[1].Width = 50;
            column[2].Width = 50;

            //
            header.Cells[0, 9].Text = "節点荷重";
            header.Cells[1, 9].Text = "節点";
            header.Cells[2, 9].Text = "No";
            header.Cells[1, 10].Text = "X";
            header.Cells[2, 10].Text = "(kN)";
            header.Cells[1, 11].Text = "Y";
            header.Cells[2, 11].Text = "(kN)";
            header.Cells[1, 12].Text = "Z";
            header.Cells[2, 12].Text = "(kN)";
            if (_input.dimension != 2)
            {
                header.Cells[1, 13].Text = "RX";
                header.Cells[2, 13].Text = "(kN・m)";
                header.Cells[1, 14].Text = "RY";
                header.Cells[2, 14].Text = "(kN・m)";
                header.Cells[1, 15].Text = "RZ";
                header.Cells[2, 15].Text = "(kN・m)";
            }

            header.Cells[0, 9].ColumnSpan = _input.dimension == 2 ? 4 : 6;

            column[9].Width = 50;

            if (_input.dimension == 2)
            {
                header.Cells[1, 12].Text = "RZ";
                header.Cells[2, 12].Text = "(kN・m)";
            }

            RefreshIntensityRowColors();
        }

        internal void RefreshDimension()
        {
            if (fpSpread1.EditMode) fpSpread1.StopCellEditing();
            SetSheet2();
            float width = 100;
            foreach (Column column in fpSpread1_Sheet2.Columns) width += column.Width;
            Width = (int)width;
        }

        private static void ConfigureRows(SheetView sheet)
        {
            sheet.AutoGenerateColumns = false;
            sheet.DataAutoCellTypes = false;
            sheet.DataAutoHeadings = false;
            sheet.RowHeaderAutoText = HeaderAutoText.Numbers;
            sheet.StartingRowNumber = 1;
        }

        /// <summary>
        /// 各行のCaseId（実荷重番号）が偶数なら、その行全体を薄い青にする
        /// 奇数の行も通常色に戻すことで、番号変更後に色が残るのを防ぎます。
        /// </summary>
        private void RefreshIntensityRowColors()
        {
            var indices = _service.AssignedIntensityRowIndices.ToHashSet();
            indices.UnionWith(_coloredIntensityRows);
            foreach (int index in indices)
                RefreshIntensityRowColor(index);
        }

        private void RefreshIntensityRowColor(int index)
        {
            if (index < 0 || index >= fpSpread1_Sheet2.RowCount) return;
            var row = _service.GetIntensityRowAt(index);
            bool isEven = row?.IsAssigned == true && int.TryParse(row.CaseId, out int caseId)
                && caseId % 2 == 0;
            if (isEven)
            {
                _coloredIntensityRows.Add(index);
                fpSpread1_Sheet2.Rows[index].BackColor = Color.AliceBlue;
            }
            else if (_coloredIntensityRows.Remove(index))
                fpSpread1_Sheet2.Rows[index].BackColor = SystemColors.Window;
        }

        /// <summary>
        /// JSON読込・実荷重番号変更・行の挿入削除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnIntensityRowsChanged(object? sender, ListChangedEventArgs e)
        {
            if (IsDisposed || Disposing) return;
            if (e.ListChangedType == ListChangedType.Reset)
            {
                _intensityRevision++;
                _pendingAllRowColors = true;
                _pendingColorRows.Clear();
            }
            else if (e.ListChangedType == ListChangedType.ItemChanged && e.NewIndex >= 0)
                _pendingColorRows.Add(e.NewIndex);
            else return;
            if (!IsHandleCreated || _colorRefreshQueued) return;

            // Spreadの再バインドが終わってから色を更新します。
            _colorRefreshQueued = true;
            int handleRevision = _handleRevision;
            BeginInvoke((System.Action)(() =>
            {
                if (handleRevision != _handleRevision) return;
                _colorRefreshQueued = false;
                if (IsDisposed || Disposing || !IsHandleCreated) return;
                if (_pendingAllRowColors)
                    RefreshIntensityRowColors();
                else
                    foreach (int index in _pendingColorRows)
                        RefreshIntensityRowColor(index);
                _pendingAllRowColors = false;
                _pendingColorRows.Clear();
            }));
        }

    }
}
