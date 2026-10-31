using FarPoint.Win.Spread;
using FarPoint.Win.Spread.CellType;
using FrameWebforCS.providers;
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
        private FarPoint.Win.Spread.SheetView fpSpread1_Sheet4;
        private readonly HashSet<int> _shownDefineRows = new();
        private readonly HashSet<int> _shownCombineRows = new();
        private readonly HashSet<int> _shownPickupRows = new();
        private readonly HashSet<int> _shownNonlinearRows = new();
        private bool _refreshing;
        private int _combineCoefficientCount = -1;
        private char _combinePrefix;
        private int _nonlinearCoefficientCount = -1;

        // GLOSSARY: 非線形解析の組合せ. Columns: 名称, C1..Cn, then these settings.
        private static readonly string[] NonlinearSettings =
            { "方式", "制御節点", "目標変位", "分割数", "最大倍率", "最大反復回数", "許容誤差" };
        private const string DisplacementMethod = "変位増分";
        private const string LoadMethod = "荷重増分";
        private const string IndividualList = "個別指定";

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

            fpSpread1_Sheet4 = fpSpread1.AddNewSheetView();
            fpSpread1_Sheet4.SheetName = "非線形解析の組合せ";
            ConfigureRows(fpSpread1_Sheet4);
            RefreshNonlinearColumns();

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

        private bool RefreshNonlinearColumns()
        {
            int count = Math.Min(50, Math.Max(5, _load.MaximumEffectiveCaseId));
            if (_nonlinearCoefficientCount == count) return false;

            bool wasRefreshing = _refreshing;
            _refreshing = true;
            try
            {
                var sheet = fpSpread1_Sheet4;
                var header = sheet.ColumnHeader;
                sheet.ColumnCount = 1 + count + NonlinearSettings.Length;
                header.Cells[0, 0].Text = "名称";
                sheet.Columns[0].Width = 200;
                sheet.Columns[0].CellType = new GeneralCellType();
                for (int column = 1; column <= count; column++)
                {
                    header.Cells[0, column].Text = "C" + column.ToString(CultureInfo.InvariantCulture);
                    sheet.Columns[column].Width = 50;
                    sheet.Columns[column].CellType = new GeneralCellType { FormatString = "F3" };
                }
                for (int index = 0; index < NonlinearSettings.Length; index++)
                {
                    int column = 1 + count + index;
                    header.Cells[0, column].Text = NonlinearSettings[index];
                    header.Cells[0, column].Note = null;
                    sheet.Columns[column].Width = 80;
                    sheet.Columns[column].CellType = new GeneralCellType();
                }
                sheet.Columns[1 + count].CellType = new ComboBoxCellType
                {
                    Items = [DisplacementMethod, LoadMethod],
                    Editable = false
                };
                header.Cells[0, 1 + count + 2].Note = "制御節点の x 方向の変位。向きは符号で表します。";
                header.Cells[0, 1 + count + 4].Note = "荷重の倍率 λ の最大値（JRSNAP の JALF）";
                header.Cells[0, 1 + count + 5].Note = "空欄は 50";
                header.Cells[0, 1 + count + 6].Note = "空欄は 1e-6";
                _nonlinearCoefficientCount = count;
            }
            finally { _refreshing = wasRefreshing; }
            return true;
        }

        // GLOSSARY: 2D非線形モード. Only the 非線形解析の組合せ sheet is shown there.
        private void RefreshModeSheets()
        {
            bool nonlinear = InputDataService.Instance.IsNonlinear;
            fpSpread1_Sheet1.Visible = !nonlinear;
            fpSpread1_Sheet2.Visible = !nonlinear;
            fpSpread1_Sheet3.Visible = !nonlinear;
            fpSpread1_Sheet4.Visible = nonlinear;
            if (!fpSpread1.ActiveSheet.Visible)
                fpSpread1.ActiveSheet = nonlinear ? fpSpread1_Sheet4 : fpSpread1_Sheet2;
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
                _refreshing = true;
                try
                {
                    if (RefreshCombineColumns())
                        RefreshSheet(fpSpread1_Sheet2, _service.CombineRows, _shownCombineRows, true);
                    if (RefreshNonlinearColumns())
                        RefreshNonlinearSheet();
                }
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
                RefreshNonlinearColumns();
                RefreshNonlinearSheet();
                RefreshModeSheets();
            }
            finally { _refreshing = false; }
        }

        private void RefreshNonlinearSheet()
        {
            foreach (int row in _shownNonlinearRows)
                RenderNonlinearRow(row - 1, null);
            _shownNonlinearRows.Clear();
            foreach (var (row, item) in _service.NonlinearRows)
            {
                RenderNonlinearRow(row - 1, item);
                _shownNonlinearRows.Add(row);
            }
        }

        private void RenderNonlinearRow(int row, clsNonlinearCombine? item)
        {
            var sheet = fpSpread1_Sheet4;
            for (int column = 0; column < sheet.ColumnCount; column++)
                sheet.Cells[row, column].Value = null;
            if (item == null) return;

            int count = _nonlinearCoefficientCount;
            sheet.Cells[row, 0].Value = item.name;
            foreach (var (index, value) in item.Coefficients)
                if (index <= count)
                    sheet.Cells[row, index].Value = value;
            int settings = 1 + count;
            sheet.Cells[row, settings].Value = item.Method switch
            {
                NonlinearMethod.Displacement => DisplacementMethod,
                NonlinearMethod.Load => LoadMethod,
                _ => null
            };
            sheet.Cells[row, settings + 1].Value = item.ControlNode;
            sheet.Cells[row, settings + 2].Value = item.Method == NonlinearMethod.Displacement && item.IsIndividual
                ? IndividualList : item.Target;
            sheet.Cells[row, settings + 3].Value = item.Divisions;
            sheet.Cells[row, settings + 4].Value = item.Method == NonlinearMethod.Load && item.IsIndividual
                ? IndividualList : item.MaxFactor;
            sheet.Cells[row, settings + 5].Value = item.MaxIterations;
            sheet.Cells[row, settings + 6].Value = item.Tolerance;
        }

        private void EditNonlinearCell(int row, int column, object? value)
        {
            int count = _nonlinearCoefficientCount;
            if (column == 0)
            {
                _service.SetNonlinearName(row, value?.ToString());
                return;
            }
            if (column <= count)
            {
                if (!TryReadDouble(value, out double? coefficient))
                    throw new ArgumentException("係数には有効な数値を入力してください。");
                _service.SetNonlinearCoefficient(row, column, coefficient);
                return;
            }
            switch (column - count - 1)
            {
                case 0:
                    _service.SetNonlinearMethod(row, value?.ToString() switch
                    {
                        DisplacementMethod => NonlinearMethod.Displacement,
                        LoadMethod => NonlinearMethod.Load,
                        _ => null
                    });
                    break;
                case 1: _service.SetNonlinearControlNode(row, ReadInt(value)); break;
                case 2: _service.SetNonlinearTarget(row, ReadDouble(value)); break;
                case 3: _service.SetNonlinearDivisions(row, ReadInt(value)); break;
                case 4: _service.SetNonlinearMaxFactor(row, ReadDouble(value)); break;
                case 5: _service.SetNonlinearMaxIterations(row, ReadInt(value)); break;
                case 6: _service.SetNonlinearTolerance(row, ReadDouble(value)); break;
            }

            static int? ReadInt(object? value) => TryReadInt(value, out int? number)
                ? number : throw new ArgumentException("整数を入力してください。");
            static double? ReadDouble(object? value) => TryReadDouble(value, out double? number)
                ? number : throw new ArgumentException("数値を入力してください。");
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
            else if (sheet == fpSpread1_Sheet4)
            {
                string? refused = null;
                try { EditNonlinearCell(row, e.Column, value); }
                catch (ArgumentException error) { refused = error.Message; }
                // A setting can change the row's other cells (method, lists), so redraw it.
                _refreshing = true;
                try
                {
                    RenderNonlinearRow(e.Row, _service.NonlinearRows.GetValueOrDefault(row));
                    _shownNonlinearRows.Add(row);
                }
                finally { _refreshing = false; }
                if (refused != null)
                    MessageBox.Show(this, refused, "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
