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
    public partial class InputLoadComponent : UserControl
    {
        internal event Action<int, string>? GridSelectionChanged;
        internal event Action<string>? ActiveLoadDisplayModeChanged;
        internal event Action<float>? LoadScaleChanged;
        internal string ActiveLoadDisplayMode =>
            ReferenceEquals(fpSpread1.ActiveSheet, fpSpread1_Sheet2) ? "load_values" : "load_names";
        internal float LoadScale => (float)_loadScale.Value;
        private bool _syncingSelection;
        private readonly InputLoadService _service = InputLoadService.Instance;
        private readonly ComboBox _caseSelector = new();
        private readonly NumericUpDown _loadScale = new();
        private readonly Panel _loadScalePanel = new();
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

            _caseSelector.Dock = DockStyle.Top;
            _caseSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            _caseSelector.AccessibleName = "荷重ケース";
            _caseSelector.SelectedIndexChanged += (_, _) =>
            {
                if (_caseSelector.SelectedItem is string id)
                {
                    _service.SelectCase(id);
                    fpSpread1_Sheet2.SheetName = id;
                }
            };
            Controls.Add(_caseSelector);
            // JS ThreeLoadService.guiEnable exposes LoadScale (0..400, default 100)
            // only on the load-values sheet. WinForms keeps it beside the input grid.
            _loadScale.Minimum = 0;
            _loadScale.Maximum = 400;
            _loadScale.Value = 100;
            _loadScale.Width = 75;
            _loadScale.Dock = DockStyle.Right;
            _loadScale.AccessibleName = "荷重表示倍率";
            _loadScale.ValueChanged += (_, _) => LoadScaleChanged?.Invoke(LoadScale);
            _loadScalePanel.Dock = DockStyle.Top;
            _loadScalePanel.Height = 28;
            _loadScalePanel.Controls.Add(new Label { Text = "荷重表示倍率 (%)", Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft });
            _loadScalePanel.Controls.Add(_loadScale);
            _loadScalePanel.Visible = ActiveLoadDisplayMode == "load_values";
            Controls.Add(_loadScalePanel);
            _service.CasesChanged += RefreshCaseSelector;
            RefreshCaseSelector(this, EventArgs.Empty);

            float w = 0;
            var col = fpSpread1_Sheet2.Columns;
            for (int i = 0; i < col.Count; i++)
            {
                w += col[i].Width;
            }
            w += 100;

            this.Width = (int)w;
            fpSpread1.EnterCell += OnEnterCell;
            fpSpread1.ActiveSheetChanged += OnActiveSheetChanged;
            HandleCreated += OnDisplayActivated;
            VisibleChanged += OnDisplayActivated;
            Disposed += (_, _) =>
            {
                fpSpread1.EnterCell -= OnEnterCell;
                fpSpread1.ActiveSheetChanged -= OnActiveSheetChanged;
                HandleCreated -= OnDisplayActivated;
                VisibleChanged -= OnDisplayActivated;
            };
        }

        private void OnActiveSheetChanged(object? sender, EventArgs e)
        {
            _loadScalePanel.Visible = ActiveLoadDisplayMode == "load_values";
            ActiveLoadDisplayModeChanged?.Invoke(ActiveLoadDisplayMode);
        }

        private void OnDisplayActivated(object? sender, EventArgs e)
        {
            if (Visible && !IsDisposed)
                ActiveLoadDisplayModeChanged?.Invoke(ActiveLoadDisplayMode);
        }

        private void OnEnterCell(object? sender, EnterCellEventArgs e)
        {
            if (_syncingSelection || e.Row < 0 || e.Column < 0 ||
                !ReferenceEquals(fpSpread1.ActiveSheet, fpSpread1_Sheet2)) return;
            string column = fpSpread1_Sheet2.Columns[e.Column].DataField?.ToLowerInvariant() ?? "";
            // JS InputLoadComponent.selectEnd sends its 1-based row and field key.
            GridSelectionChanged?.Invoke(e.Row + 1, column);
        }

        internal bool SelectGridRow(int row, string? column = null, string? caseId = null)
        {
            if (IsDisposed || row < 1 || row > fpSpread1_Sheet2.RowCount) return false;
            if (caseId != null && !_caseSelector.Items.Contains(caseId)) return false;
            int columnIndex = 1;
            if (column != null)
                for (int i = 0; i < fpSpread1_Sheet2.ColumnCount; i++)
                    if (string.Equals(fpSpread1_Sheet2.Columns[i].DataField, column,
                            StringComparison.OrdinalIgnoreCase)) { columnIndex = i; break; }
            _syncingSelection = true;
            try
            {
                if (caseId != null) _caseSelector.SelectedItem = caseId;
                fpSpread1.ActiveSheetIndex = 1;
                fpSpread1_Sheet2.SetActiveCell(row - 1, columnIndex);
            }
            finally { _syncingSelection = false; }
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
            var column = fpSpread1_Sheet2.Columns;

            var header = fpSpread1_Sheet2.ColumnHeader;
            header.RowCount = 3;

            fpSpread1_Sheet2.ColumnCount = 16;
            string[] fields = { "LoadId", "m1", "m2", "direction", "mark", "L1", "L2",
                "P1", "P2", "n", "tx", "ty", "tz", "rx", "ry", "rz" };
            for (int i = 0; i < fields.Length; i++)
                column[i].DataField = fields[i];

            for (int i = 0; i < column.Count; i++)
                column[i].Locked = false;
            column[0].Locked = true;
            column[0].BackColor = SystemColors.Control;
            fpSpread1_Sheet2.Protect = true;

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
            header.Cells[1, 13].Text = "RX";
            header.Cells[2, 13].Text = "(kN・m)";
            header.Cells[1, 14].Text = "RY";
            header.Cells[2, 14].Text = "(kN・m)";
            header.Cells[1, 15].Text = "RZ";
            header.Cells[2, 15].Text = "(kN・m)";

            header.Cells[0, 9].ColumnSpan = 6;

            column[9].Width = 50;
            fpSpread1_Sheet2.DataSource = _service.IntensityRows;
        }

        private static void ConfigureRows(SheetView sheet)
        {
            sheet.AutoGenerateColumns = false;
            sheet.DataAutoCellTypes = false;
            sheet.DataAutoHeadings = false;
            sheet.RowHeaderAutoText = HeaderAutoText.Numbers;
            sheet.StartingRowNumber = 1;
        }

        private void RefreshCaseSelector(object? sender, EventArgs e)
        {
            string selected = _service.SelectedCaseId;
            var caseIds = new List<string> { selected };
            foreach (string id in _service.CaseIds)
                if (!caseIds.Contains(id)) caseIds.Add(id);
            _caseSelector.BeginUpdate();
            try
            {
                _caseSelector.Items.Clear();
                foreach (string id in caseIds)
                    _caseSelector.Items.Add(id);
                _caseSelector.SelectedItem = selected;
            }
            finally
            {
                _caseSelector.EndUpdate();
            }
        }


    }
}
