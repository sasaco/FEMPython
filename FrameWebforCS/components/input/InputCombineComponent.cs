using FarPoint.Win.Spread;
using FarPoint.Win.Spread.CellType;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace FrameWebforCS.components.input
{
    public partial class InputCombineComponent : UserControl
    {
        private readonly InputCombineService _service = InputCombineService.Instance;
        private readonly InputLoadService _load = InputLoadService.Instance;
        private readonly Label _overflowWarning = new();
        private readonly int _uiThreadId = Environment.CurrentManagedThreadId;
        private FarPoint.Win.Spread.SheetView fpSpread1_Sheet1;
        private FarPoint.Win.Spread.SheetView fpSpread1_Sheet2;
        private FarPoint.Win.Spread.SheetView fpSpread1_Sheet3;
        private readonly HashSet<int> _shownDefineRows = new();
        private readonly HashSet<int> _shownCombineRows = new();
        private readonly HashSet<int> _shownPickupRows = new();
        private bool _refreshing;
        private int _combineCoefficientCount = -1;
        private char _combinePrefix;

        public InputCombineComponent()
        {
            InitializeComponent();
            _overflowWarning.Dock = DockStyle.Top;
            _overflowWarning.Height = 24;
            _overflowWarning.BackColor = Color.MistyRose;
            _overflowWarning.Visible = false;
            Controls.Add(_overflowWarning);
            _overflowWarning.BringToFront();

            fpSpread1_Sheet1 = fpSpread1.AddNewSheetView();
            SetSheet1();

            fpSpread1_Sheet2 = fpSpread1.AddNewSheetView();
            SetSheet2();

            fpSpread1_Sheet3 = fpSpread1.AddNewSheetView();
            SetSheet3();

            fpSpread1.Change += OnChange;
            _service.RowsReplaced += OnRowsReplaced;
            _load.CasesChanged += OnLoadCasesChanged;
            HandleCreated += (_, _) => RefreshRows();
            Disposed += (_, _) =>
            {
                _service.RowsReplaced -= OnRowsReplaced;
                _load.CasesChanged -= OnLoadCasesChanged;
            };
            RefreshRows();
        }

        private void SetSheet1()
        {
            fpSpread1_Sheet1.SheetName = "DEFINE";
            ConfigureRows(fpSpread1_Sheet1);

            var column = fpSpread1_Sheet1.Columns;
            var header = fpSpread1_Sheet1.ColumnHeader;

            fpSpread1_Sheet1.ColumnCount = 50;
            for(int i =0; i< fpSpread1_Sheet1.ColumnCount; i++)
            {
                header.Cells[0, i].Text = "C" + (i + 1).ToString();
                column[i].Width = 50;
            }
        }

        private void SetSheet2()
        {
            fpSpread1_Sheet2.SheetName = "COMBINE";
            ConfigureRows(fpSpread1_Sheet2);
            RefreshCombineColumns();
        }

        private bool RefreshCombineColumns()
        {
            int defineMaximum = 0;
            foreach (var (row, item) in _service.DefineRows)
                if (item.Coefficients.Count > 0 && row > defineMaximum)
                    defineMaximum = row;

            char prefix = defineMaximum > 0 ? 'D' : 'C';
            int maximum = defineMaximum > 0 ? defineMaximum : _load.MaximumEffectiveCaseId;
            int count = Math.Min(50, Math.Max(5, maximum));
            _overflowWarning.Text = $"ケース番号が50を超えています。{prefix}51以降の列は非表示ですが、入力値は保持されます。";
            _overflowWarning.Visible = maximum > 50;
            if (_combineCoefficientCount == count && _combinePrefix == prefix) return false;

            bool wasRefreshing = _refreshing;
            _refreshing = true;
            try
            {
                var sheet = fpSpread1_Sheet2;
                sheet.ColumnCount = count + 1;
                for (int column = 0; column < count; column++)
                {
                    sheet.ColumnHeader.Cells[0, column].Text =
                        prefix + (column + 1).ToString(CultureInfo.InvariantCulture);
                    sheet.Columns[column].Width = 50;
                    sheet.Columns[column].CellType = new GeneralCellType { FormatString = "F3" };
                }
                sheet.ColumnHeader.Cells[0, count].Text = "名称";
                sheet.Columns[count].Width = 200;
                sheet.Columns[count].CellType = new GeneralCellType();
                sheet.FrozenTrailingColumnCount = 1;
                _combineCoefficientCount = count;
                _combinePrefix = prefix;
            }
            finally { _refreshing = wasRefreshing; }
            return true;
        }

        private void SetSheet3()
        {
            fpSpread1_Sheet3.SheetName = "PICKUP";
            ConfigureRows(fpSpread1_Sheet3);

            var column = fpSpread1_Sheet3.Columns;
            var header = fpSpread1_Sheet3.ColumnHeader;

            fpSpread1_Sheet3.ColumnCount = 50 + 1;
            for (int i = 0; i < fpSpread1_Sheet3.ColumnCount - 1; i++)
            {
                header.Cells[0, i].Text = "C" + (i + 1).ToString();
                column[i].Width = 50;
            }

            int j = fpSpread1_Sheet3.ColumnCount - 1;
            header.Cells[0, j].Text = "名称";
            column[j].Width = 200;
            fpSpread1_Sheet3.FrozenTrailingColumnCount = 1;
        }

        private static void ConfigureRows(SheetView sheet)
        {
            sheet.AutoGenerateColumns = false;
            sheet.DataAutoCellTypes = false;
            sheet.DataAutoHeadings = false;
            sheet.RowHeaderAutoText = HeaderAutoText.Numbers;
            sheet.StartingRowNumber = 1;
            sheet.RowCount = InputCombineService.MaxRows;
        }

        private void OnRowsReplaced(object? sender, EventArgs e)
        {
            RunOnUiThread(RefreshRows);
        }

        private void OnLoadCasesChanged(object? sender, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                if (!RefreshCombineColumns()) return;
                _refreshing = true;
                try { RefreshSheet(fpSpread1_Sheet2, _service.CombineRows, _shownCombineRows, true); }
                finally { _refreshing = false; }
            });
        }

        private void RunOnUiThread(System.Action action)
        {
            if (IsDisposed) return;
            if (Environment.CurrentManagedThreadId != _uiThreadId)
            {
                if (IsHandleCreated) BeginInvoke(action);
                return;
            }
            action();
        }

        private void RefreshRows()
        {
            _refreshing = true;
            try
            {
                RefreshCombineColumns();
                RefreshSheet(fpSpread1_Sheet1, _service.DefineRows, _shownDefineRows, false);
                RefreshSheet(fpSpread1_Sheet2, _service.CombineRows, _shownCombineRows, true);
                RefreshSheet(fpSpread1_Sheet3, _service.PickupRows, _shownPickupRows, true);
            }
            finally { _refreshing = false; }
        }

        private static void RefreshSheet<T>(SheetView sheet,
            IReadOnlyDictionary<int, clsCombine<T>> rows, HashSet<int> shownRows,
            bool hasName) where T : struct
        {
            foreach (int row in shownRows)
                RenderRow<T>(sheet, row - 1, null, hasName);
            shownRows.Clear();
            foreach (var (row, item) in rows)
            {
                RenderRow(sheet, row - 1, item, hasName);
                shownRows.Add(row);
            }
        }

        private static void RenderRow<T>(SheetView sheet, int row,
            clsCombine<T>? item, bool hasName) where T : struct
        {
            for (int column = 0; column < sheet.ColumnCount; column++)
                sheet.Cells[row, column].Value = null;
            if (item == null) return;

            int coefficientCount = sheet.ColumnCount - (hasName ? 1 : 0);
            foreach (var (index, value) in item.Coefficients)
                if (index <= coefficientCount)
                    sheet.Cells[row, index - 1].Value = value;
            if (hasName) sheet.Cells[row, coefficientCount].Value = item.name;
        }

        private void OnChange(object sender, ChangeEventArgs e)
        {
            if (_refreshing || e.Row < 0 || e.Column < 0) return;
            SheetView sheet = e.View.GetSheetView();
            int row = e.Row + 1;
            object? value = sheet.Cells[e.Row, e.Column].Value;

            if (sheet == fpSpread1_Sheet1)
            {
                if (!TryReadInt(value, out int? number)) { RejectEdit(); return; }
                _service.SetDefineCoefficient(row, e.Column + 1, number);
                _shownDefineRows.Add(row);
                if (RefreshCombineColumns())
                {
                    _refreshing = true;
                    try { RefreshSheet(fpSpread1_Sheet2, _service.CombineRows, _shownCombineRows, true); }
                    finally { _refreshing = false; }
                }
            }
            else if (sheet == fpSpread1_Sheet2)
            {
                if (e.Column == sheet.ColumnCount - 1)
                    _service.SetCombineName(row, value?.ToString());
                else
                {
                    if (!TryReadDouble(value, out double? number)) { RejectEdit(); return; }
                    _service.SetCombineCoefficient(row, e.Column + 1, number);
                }
                _shownCombineRows.Add(row);
            }
            else if (sheet == fpSpread1_Sheet3)
            {
                if (e.Column == sheet.ColumnCount - 1)
                    _service.SetPickupName(row, value?.ToString());
                else
                {
                    if (!TryReadInt(value, out int? number)) { RejectEdit(); return; }
                    _service.SetPickupCoefficient(row, e.Column + 1, number);
                }
                _shownPickupRows.Add(row);
            }
            else return;

            void RejectEdit()
            {
                RenderCurrentRow();
                MessageBox.Show(this, "係数には有効な数値を入力してください。", "入力エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            void RenderCurrentRow()
            {
                _refreshing = true;
                try
                {
                    if (sheet == fpSpread1_Sheet1)
                        RenderRow(sheet, e.Row,
                            _service.DefineRows.GetValueOrDefault(row), false);
                    else if (sheet == fpSpread1_Sheet2)
                        RenderRow(sheet, e.Row,
                            _service.CombineRows.GetValueOrDefault(row), true);
                    else
                        RenderRow(sheet, e.Row,
                            _service.PickupRows.GetValueOrDefault(row), true);
                }
                finally { _refreshing = false; }
            }
        }

        private static bool TryReadInt(object? value, out int? result)
        {
            result = null;
            if (value == null || string.IsNullOrWhiteSpace(value.ToString())) return true;
            if (!int.TryParse(value.ToString(), NumberStyles.Integer,
                CultureInfo.CurrentCulture, out int number)) return false;
            result = number;
            return true;
        }

        private static bool TryReadDouble(object? value, out double? result)
        {
            result = null;
            if (value == null || string.IsNullOrWhiteSpace(value.ToString())) return true;
            if (!double.TryParse(value.ToString(), NumberStyles.Float,
                CultureInfo.CurrentCulture, out double number) || !double.IsFinite(number))
                return false;
            result = number;
            return true;
        }
    }
}
