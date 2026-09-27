using FarPoint.Win.Spread;
using FrameWebforCS.calculation;
using System.Globalization;
using System.Windows.Forms;

namespace FrameWebforCS.components.result;

internal enum CalculationDerivedStage { Combine, Pickup }
internal enum CalculationDerivedQuantity { Displacement, Reaction, SectionForce }

/// <summary>Renders the detached v1 derived projection in the existing result controls.</summary>
internal sealed class CalculationDerivedViewRenderer(
    CalculationDerivedStage stage, CalculationDerivedQuantity quantity,
    FpSpread spread, ComboBox selector, Label status,
    (string Key, string Title)[] modes3D, (string Key, string Title)[] modes2D,
    Action<SheetView, int>? configureLegacy = null)
{
    private CalculationResultPresentation? _presentation;
    private CalculationDerivedPresentation? _derived;
    private IReadOnlyList<CalculationDerivedCase> _cases = [];
    private bool _building;
    private int _materialized = -1;

    internal bool IsShowing => _presentation != null;

    internal bool ShowCurrent()
    {
        var current = CalculationResultStore.Instance.Current;
        if (current is null)
        {
            _presentation = null;
            _derived = null;
            _cases = [];
            return false;
        }
        if (current.Derived is not { } derived)
        {
            _presentation = current;
            _derived = null;
            _cases = [];
            _materialized = -1;
            spread.Sheets.Clear();
            selector.Items.Clear();
            status.Text = "派生結果を更新しています。";
            return true;
        }
        if (ReferenceEquals(current, _presentation) && ReferenceEquals(derived, _derived)) return true;
        _presentation = current;
        _derived = derived;
        _cases = stage == CalculationDerivedStage.Combine ? derived.Combines : derived.Pickups;
        int dimension = current.Dimension;
        _building = true;
        try
        {
            spread.Sheets.Clear();
            selector.Items.Clear();
            _materialized = -1;
            foreach (var mode in dimension == 3 ? modes3D : modes2D)
                selector.Items.Add(new ModeChoice(mode.Key, mode.Title));
            for (int index = 0; index < _cases.Count; index++)
            {
                var item = _cases[index];
                SheetView sheet = spread.AddNewSheetView();
                string name = string.IsNullOrWhiteSpace(item.Name) ? item.Id : $"{item.Id} {item.Name}";
                sheet.SheetName = name[..Math.Min(name.Length, 31)];
                if (configureLegacy != null) configureLegacy(sheet, dimension);
                else ConfigurePickupSheet(sheet, dimension);
                UpdateUnitHeaders(sheet, current, dimension);
                sheet.RowCount = 0;
            }
            if (selector.Items.Count > 0) selector.SelectedIndex = 0;
            if (spread.Sheets.Count > 0) spread.ActiveSheetIndex = 0;
        }
        finally { _building = false; }
        status.Text = _cases.Count == 0 ? "派生結果がありません。" : $"{_cases.Count} 件の派生結果";
        Materialize();
        return true;
    }

    internal void Materialize()
    {
        if (_building || _presentation is not { } presentation ||
            selector.SelectedItem is not ModeChoice mode) return;
        int index = spread.ActiveSheetIndex;
        if (index < 0 || index >= _cases.Count) return;
        if (_materialized >= 0 && _materialized != index && _materialized < spread.Sheets.Count)
            spread.Sheets[_materialized].RowCount = 0;
        var item = _cases[index];
        var modes = quantity switch
        {
            CalculationDerivedQuantity.Displacement => item.Displacements,
            CalculationDerivedQuantity.Reaction => item.Reactions,
            _ => item.SectionForces
        };
        IReadOnlyList<CalculationDerivedRow> rows = modes.TryGetValue(mode.Key, out var selected) ? selected : [];
        SheetView sheet = spread.Sheets[index];
        sheet.RowCount = rows.Count;
        int dimension = presentation.Dimension;
        for (int row = 0; row < rows.Count; row++)
        {
            var value = rows[row];
            string[] keys = quantity == CalculationDerivedQuantity.Displacement
                ? dimension == 3 ? ["dx", "dy", "dz", "rx", "ry", "rz"] : ["dx", "dy", "rz"]
                : dimension == 3 ? ["fx", "fy", "fz", "mx", "my", "mz"] : ["fx", "fy", "mz"];
            int offset = quantity == CalculationDerivedQuantity.SectionForce ? 3 : 1;
            sheet.Cells[row, 0].Text = value.EntityId;
            if (quantity == CalculationDerivedQuantity.SectionForce)
            {
                sheet.Cells[row, 1].Text = value.StationId ?? "";
                sheet.Cells[row, 2].Text = StationPosition(presentation, value).ToString("F3", CultureInfo.InvariantCulture);
            }
            for (int component = 0; component < keys.Length; component++)
            {
                double number = value.Components[keys[component]];
                if (quantity == CalculationDerivedQuantity.Displacement && keys[component].StartsWith('d'))
                    number = presentation.DisplayLength(number);
                sheet.Cells[row, component + offset].Text = Format(number,
                    quantity == CalculationDerivedQuantity.Displacement ? 4 : 2);
            }
            sheet.Cells[row, sheet.ColumnCount - 1].Text = stage == CalculationDerivedStage.Pickup ||
                value.Provenance.Length == 0 ? value.SourceCaseId : value.Provenance;
        }
        _materialized = index;
        PublishPage();
    }

    internal void PublishPage()
    {
        if (_presentation == null || !spread.Visible || selector.SelectedItem is not ModeChoice mode) return;
        int index = spread.ActiveSheetIndex;
        if (index < 0 || index >= _cases.Count) return;
        string view = (stage, quantity) switch
        {
            (CalculationDerivedStage.Combine, CalculationDerivedQuantity.Displacement) => "comb_disg",
            (CalculationDerivedStage.Combine, CalculationDerivedQuantity.Reaction) => "comb_reac",
            (CalculationDerivedStage.Combine, CalculationDerivedQuantity.SectionForce) => "comb_fsec",
            (CalculationDerivedStage.Pickup, CalculationDerivedQuantity.Displacement) => "pik_disg",
            (CalculationDerivedStage.Pickup, CalculationDerivedQuantity.Reaction) => "pik_reac",
            _ => "pick_fsec"
        };
        FrameWebforCS.three.ThreeResultsService.PublishPage(view, _cases[index].Id, mode.Key);
    }

    private static double StationPosition(CalculationResultPresentation presentation, CalculationDerivedRow row) =>
        presentation.ResultSet.Topology.Members
            .FirstOrDefault(member => member.MemberId == row.EntityId)?.Stations
            .FirstOrDefault(station => station.StationId == row.StationId)?.Position ?? 0;

    private void UpdateUnitHeaders(SheetView sheet, CalculationResultPresentation presentation, int dimension)
    {
        if (quantity == CalculationDerivedQuantity.Displacement)
        {
            sheet.ColumnHeader.Cells[0, 1].Text = $"変位 ({presentation.DisplayLengthUnit})";
            sheet.ColumnHeader.Cells[0, dimension == 3 ? 4 : 3].Text = "回転 (rad)";
        }
        else if (quantity == CalculationDerivedQuantity.Reaction)
        {
            sheet.ColumnHeader.Cells[0, 1].Text = $"反力 ({presentation.ForceUnit})";
            sheet.ColumnHeader.Cells[0, dimension == 3 ? 4 : 3].Text =
                $"反力モーメント ({presentation.MomentUnit})";
        }
        else
        {
            sheet.ColumnHeader.Cells[1, 2].Text = $"({presentation.LengthUnit})";
            sheet.ColumnHeader.Cells[1, 3].Text = $"({presentation.ForceUnit})";
            sheet.ColumnHeader.Cells[1, dimension == 3 ? 6 : 5].Text = $"({presentation.MomentUnit})";
            if (dimension == 3)
            {
                sheet.ColumnHeader.Cells[0, 4].Text = $"せん断力 ({presentation.ForceUnit})";
                sheet.ColumnHeader.Cells[0, 7].Text = $"曲げモーメント ({presentation.MomentUnit})";
            }
            else
                sheet.ColumnHeader.Cells[1, 4].Text = $"({presentation.ForceUnit})";
        }
    }

    private void ConfigurePickupSheet(SheetView sheet, int dimension)
    {
        int columns = quantity == CalculationDerivedQuantity.SectionForce
            ? dimension == 3 ? 10 : 7 : dimension == 3 ? 8 : 5;
        sheet.ColumnCount = columns;
        sheet.ColumnHeader.RowCount = 2;
        string[] labels = quantity == CalculationDerivedQuantity.SectionForce
            ? dimension == 3 ? ["部材", "着目点", "位置", "Fx", "Fy", "Fz", "Mx", "My", "Mz", "出典"]
                : ["部材", "着目点", "位置", "Fx", "Fy", "Mz", "出典"]
            : quantity == CalculationDerivedQuantity.Displacement
                ? dimension == 3 ? ["節点", "Dx", "Dy", "Dz", "Rx", "Ry", "Rz", "出典"]
                    : ["節点", "Dx", "Dy", "Rz", "出典"]
                : dimension == 3 ? ["節点", "Fx", "Fy", "Fz", "Mx", "My", "Mz", "出典"]
                    : ["節点", "Fx", "Fy", "Mz", "出典"];
        for (int column = 0; column < columns; column++)
        {
            sheet.ColumnHeader.Cells[0, column].Text = labels[column];
            sheet.Columns[column].Width = column == columns - 1 ? 200 : 80;
            sheet.Columns[column].CellType = new FarPoint.Win.Spread.CellType.TextCellType();
        }
        sheet.Protect = true;
    }

    private static string Format(double number, int places) =>
        (Math.Floor(number * Math.Pow(10, places) + 0.5) / Math.Pow(10, places))
            .ToString($"F{places}", CultureInfo.InvariantCulture);

    private sealed record ModeChoice(string Key, string Title)
    {
        public override string ToString() => Title;
    }
}
