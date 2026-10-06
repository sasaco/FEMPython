using System.ComponentModel;
using System.Globalization;
using FarPoint.Win.Spread;
using FarPoint.Win.Spread.CellType;
using FrameWebforCS.providers;

namespace FrameWebforCS.components.input;

public sealed class InputBridgeLoadComponent : UserControl
{
    private readonly InputBridgeLoadService _service = InputBridgeLoadService.Instance;
    private readonly myFpSpread _spread = new() { Dock = DockStyle.Fill };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 62, AutoEllipsis = true };
    private readonly Label _help = new() { Dock = DockStyle.Top, Height = 54, Padding = new Padding(5) };
    private readonly SheetView[] _sheets;
    private bool _syncing;
    private bool _refreshQueued;
    internal event Action<BridgeSelection?>? SelectionChanged;
    internal int ActiveSheetIndex => _spread.ActiveSheetIndex;

    public InputBridgeLoadComponent()
    {
        Width = 1230; Height = 480;
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false };
        AddButton(toolbar, "行追加", AddRow);
        AddButton(toolbar, "三角形編集", EditTriangles);
        AddButton(toolbar, "平面図", () => _service.RequestView("plan"));
        AddButton(toolbar, "立体図", () => _service.RequestView("iso"));
        AddCheck(toolbar, "伝達三角形", _service.ShowMesh, v => _service.ShowMesh = v);
        AddCheck(toolbar, "番号・強度", _service.ShowLabels, v => _service.ShowLabels = v);
        AddCheck(toolbar, "等価節点荷重（解析後）", _service.ShowEquivalentLoads, v => _service.ShowEquivalentLoads = v);
        AddCheck(toolbar, "詳細設定", false, SetAdvanced);
        Controls.Add(_spread); Controls.Add(_status); Controls.Add(_help); Controls.Add(toolbar);
        _sheets = [CreateSheet("荷重伝達面", _service.Panels,
                ["Id", "Name", "Nodes", "Triangles", "Elements", "Holes", "Origin", "AxisU", "AxisV", "AbsoluteTolerance", "RelativeTolerance", "LoadingNodes", "LoadingTriangles"],
                ["面No", "名称", "構造節点No", "三角形（節点No）", "既存面要素No", "孔（節点No）", "平面原点 X,Y,Z (m)", "平面U軸", "平面V軸", "絶対許容差 (m)", "相対許容差", "独立載荷節点 (JSON)", "独立載荷三角形"]),
            CreateSheet("載荷形状", _service.Paths,
                ["Id", "Name", "Order", "X", "Y", "Z"],
                ["線No", "名称", "点順", "X (m)", "Y (m)", "Z (m)"]),
            CreateSheet("橋面荷重強度", _service.Loads,
                ["CaseId", "Id", "Name", "PanelId", "Path1", "Path2", "P11", "P12", "P21", "P22", "Direction", "Vx", "Vy", "Vz", "Units"],
                ["実荷重番号", "荷重No", "名称", "面No", "線1", "線2（面荷重）", "線1始点 P11", "線1終点 P12", "線2始点 P21", "線2終点 P22", "方向", "Vx", "Vy", "Vz", "強度単位"] )];
        _sheets[2].Columns[10].CellType = new ComboBoxCellType { Items = ["global", "normal"], Editable = false };
        _sheets[2].Columns[14].Locked = true;
        _sheets[2].Protect = true;
        SetAdvanced(false);
        _spread.EnterCell += OnEnterCell;
        _spread.ActiveSheetChanged += OnActiveSheetChanged;
        _service.Changed += OnChanged;
        _service.SelectionChanged += OnSelectionChanged;
        InputDataService.Instance.DimensionChanged += OnDimensionChanged;
        RegisterRows(_sheets[0], _service.Panels);
        RegisterRows(_sheets[1], _service.Paths);
        RegisterRows(_sheets[2], _service.Loads);
        RefreshDimension(); RefreshStatus();
    }

    private void SetAdvanced(bool enabled)
    {
        for (int column = 6; column <= 12; column++) _sheets[0].Columns[column].Visible = enabled;
    }

    private SheetView CreateSheet<T>(string name, BindingList<T> rows, string[] fields, string[] headings)
    {
        var sheet = _spread.AddNewSheetView();
        sheet.SheetName = name;
        sheet.AutoGenerateColumns = false; sheet.DataAutoCellTypes = false; sheet.DataAutoHeadings = false;
        sheet.ColumnCount = fields.Length;
        sheet.RowHeaderAutoText = HeaderAutoText.Numbers; sheet.StartingRowNumber = 1;
        for (int i = 0; i < fields.Length; i++)
        {
            sheet.Columns[i].DataField = fields[i]; sheet.Columns[i].CellType = new TextCellType();
            sheet.Columns[i].Width = fields[i] is "Name" or "Nodes" or "Triangles" or "Elements" or "Holes" or "LoadingNodes" or "LoadingTriangles" ? 175 : 105;
            sheet.ColumnHeader.Cells[0, i].Text = headings[i];
        }
        sheet.DataSource = rows;
        return sheet;
    }
    private static void AddButton(Control owner, string text, System.Action action)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 27 };
        button.Click += (_, _) => action(); owner.Controls.Add(button);
    }
    private static void AddCheck(Control owner, string text, bool value, Action<bool> action)
    {
        var check = new CheckBox { Text = text, Checked = value, AutoSize = true, Margin = new Padding(8, 7, 0, 0) };
        check.CheckedChanged += (_, _) => action(check.Checked); owner.Controls.Add(check);
    }
    private void RegisterRows<T>(SheetView sheet, BindingList<T> rows) where T : new()
    {
        _spread.EnableRowOperations(sheet, (index, _) =>
        {
            if (index < 0 || index > rows.Count || rows.Count >= 100_000) return false;
            rows.Insert(index, new T()); return true;
        }, (indices, _) =>
        {
            foreach (int index in indices.Distinct().OrderDescending())
                if (index >= 0 && index < rows.Count) rows.RemoveAt(index);
            if (rows.Count == 0) rows.Add(new T());
            return indices.Count > 0;
        });
    }
    private void AddRow()
    {
        if (_spread.EditMode) _spread.StopCellEditing();
        switch (_spread.ActiveSheetIndex)
        {
            case 0: _service.Panels.Add(new()); break;
            case 1: _service.Paths.Add(new()); break;
            case 2: _service.Loads.Add(new()); break;
        }
        _spread.ActiveSheet.SetActiveCell(_spread.ActiveSheet.RowCount - 1, 0);
    }
    private void EditTriangles()
    {
        if (_spread.ActiveSheetIndex != 0) return;
        if (_spread.EditMode) _spread.StopCellEditing();
        int rowIndex = _sheets[0].ActiveRowIndex;
        if (rowIndex < 0 || rowIndex >= _service.Panels.Count) return;
        BridgePanelRow row = _service.Panels[rowIndex];
        var entries = row.Triangles.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split([',', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries)).ToArray();
        if (entries.Any(entry => entry.Length > 3))
        {
            MessageBox.Show(this, "三角形は1組につき3節点です。元のセルの区切りを確認してください。", "三角形編集");
            return;
        }
        using var dialog = new Form { Text = $"面 {row.Id} の伝達三角形", Width = 480, Height = 470,
            StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false };
        var detail = new myFpSpread { Dock = DockStyle.Fill };
        var sheet = detail.AddNewSheetView();
        sheet.ColumnCount = 3; sheet.RowCount = Math.Max(30, entries.Length + 10);
        sheet.RowHeaderAutoText = HeaderAutoText.Numbers;
        for (int c = 0; c < 3; c++)
        {
            sheet.ColumnHeader.Cells[0, c].Text = "節点 " + (c + 1);
            sheet.Columns[c].Width = 110; sheet.Columns[c].CellType = new TextCellType();
        }
        for (int r = 0; r < entries.Length; r++)
            for (int c = 0; c < entries[r].Length; c++) sheet.Cells[r, c].Text = entries[r][c];
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 36 };
        AddButton(buttons, "行追加", () => sheet.RowCount += 30);
        AddButton(buttons, "反映", () =>
        {
            if (detail.EditMode) detail.StopCellEditing();
            var triangles = new List<string>(); var nodes = new List<string>();
            for (int r = 0; r < sheet.RowCount; r++)
            {
                var corners = Enumerable.Range(0, 3).Select(c => sheet.Cells[r, c].Text.Trim()).ToArray();
                if (corners.All(string.IsNullOrWhiteSpace)) continue;
                triangles.Add(string.Join(",", corners));
                nodes.AddRange(corners.Where(value => ParseId(value).HasValue));
            }
            row.Triangles = string.Join(";", triangles);
            if (string.IsNullOrWhiteSpace(row.Nodes)) row.Nodes = string.Join(",", nodes.Distinct());
            dialog.DialogResult = DialogResult.OK;
        });
        AddButton(buttons, "キャンセル", () => dialog.DialogResult = DialogResult.Cancel);
        dialog.Controls.Add(detail); dialog.Controls.Add(buttons);
        dialog.ShowDialog(this);
    }
    private void OnEnterCell(object? sender, EnterCellEventArgs e)
    {
        if (_syncing || e.Row < 0) return;
        BridgeSelection? selection = null;
        if (_spread.ActiveSheetIndex == 0 && e.Row < _service.Panels.Count && ParseId(_service.Panels[e.Row].Id) is { } panel)
            selection = new("panel", panel);
        if (_spread.ActiveSheetIndex == 1 && e.Row < _service.Paths.Count && ParseId(_service.Paths[e.Row].Id) is { } path)
            selection = new("path", path, PointIndex: ParseId(_service.Paths[e.Row].Order) is { } order ? order - 1 : null);
        if (_spread.ActiveSheetIndex == 2 && e.Row < _service.Loads.Count && ParseId(_service.Loads[e.Row].Id) is { } load &&
            ParseId(_service.Loads[e.Row].CaseId) is { } caseId)
            selection = new("load", load, caseId.ToString(CultureInfo.InvariantCulture));
        _service.SelectEntity(selection);
    }
    private static int? ParseId(string value) => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int id) && id > 0 ? id : null;
    private void OnActiveSheetChanged(object? sender, EventArgs e)
    {
        RefreshStatus();
        if (!_syncing) _service.SelectEntity(null);
    }
    private void OnChanged()
    {
        if (IsDisposed || _refreshQueued) return;
        if (!IsHandleCreated) { RefreshStatus(); return; }
        _refreshQueued = true;
        BeginInvoke((System.Action)(() => { _refreshQueued = false; if (!IsDisposed) RefreshStatus(); }));
    }
    private void RefreshStatus()
    {
        string[] help = [
            "構造剛性を追加しない荷重伝達面です。節点Noはカンマ区切り。三角形は「三角形編集」で入力できます。三角形と既存面要素Noは一方を指定します。\n孔も節点Noの列で指定。傾斜面の原点/U軸/V軸や独立載荷メッシュは「詳細設定」で表示（UとVは直交）。",
            "同じ線Noの点を、点順1から順に入力します。座標は常に全体座標系のX,Y,Z (m)です。\n各線は2点以上。線1と線2の点順が面荷重の対応を決めます。",
            "線2が空欄なら線荷重（kN/m）、線2を指定すると面荷重（kN/m²）。各Pは対応する線の始点・終点の強度です。\nglobal＝全体座標方向（標準+Z、負の強度は下向き）。normal＝面の法線方向（節点順による）。実荷重番号は「荷重名称」と共通です。"];
        _help.Text = help[Math.Clamp(_spread.ActiveSheetIndex, 0, 2)];
        var errors = _service.Errors;
        _status.ForeColor = errors.Count == 0 ? SystemColors.ControlText : Color.DarkRed;
        _status.Text = errors.Count == 0 ? "入力を保存できます。解析時に載荷位置と伝達面の幾何条件を検証します。" :
            $"入力確認 {errors.Count}件（編集中の内容は保存されます）\n" + string.Join("\n", errors.Take(2));
    }
    internal void SelectEntity(BridgeSelection selection) => _service.SelectEntity(selection);
    private void OnSelectionChanged(BridgeSelection? selection)
    {
        if (IsDisposed) return;
        _syncing = true;
        try
        {
            if (selection != null)
            {
                int sheet = selection.Kind switch { "panel" => 0, "path" => 1, "load" => 2, _ => -1 };
                int row = sheet switch
                {
                    0 => _service.Panels.ToList().FindIndex(r => ParseId(r.Id) == selection.Id),
                    1 => _service.Paths.ToList().FindIndex(r => ParseId(r.Id) == selection.Id &&
                        (!selection.PointIndex.HasValue || ParseId(r.Order) == selection.PointIndex.Value + 1)),
                    2 => _service.Loads.ToList().FindIndex(r => ParseId(r.Id) == selection.Id && r.CaseId == selection.CaseId),
                    _ => -1
                };
                if (row >= 0) { _spread.ActiveSheetIndex = sheet; _sheets[sheet].SetActiveCell(row, 0); }
            }
        }
        finally { _syncing = false; }
        SelectionChanged?.Invoke(selection);
    }
    private void OnDimensionChanged(int _) => RefreshDimension();
    internal void RefreshDimension() => Enabled = InputDataService.Instance.dimension == 3;
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _service.Changed -= OnChanged; _service.SelectionChanged -= OnSelectionChanged;
            InputDataService.Instance.DimensionChanged -= OnDimensionChanged;
            _spread.EnterCell -= OnEnterCell; _spread.ActiveSheetChanged -= OnActiveSheetChanged;
            foreach (var sheet in _sheets) _spread.DisableRowOperations(sheet);
        }
        base.Dispose(disposing);
    }
}
