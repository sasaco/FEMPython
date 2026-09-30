using FarPoint.Win.Spread;
using FarPoint.Win.Spread.Model;
using System.Globalization;

namespace FrameWebforCS.components.result;

internal sealed class ResultModeGroupDataModel : GroupDataModel
{
    private readonly int _modeColumn;
    private readonly (string Key, string Title)[] _modes;

    private ResultModeGroupDataModel(
        ISheetDataModel target, int modeColumn, (string Key, string Title)[] modes)
        : base(target)
    {
        _modeColumn = modeColumn;
        _modes = modes;
    }

    internal static string Token(int modeIndex) =>
        modeIndex.ToString("D4", CultureInfo.InvariantCulture);

    internal static ResultModeGroupDataModel Attach(
        SheetView sheet, int modeColumn, (string Key, string Title)[] modes)
    {
        var grouped = new ResultModeGroupDataModel(sheet.Models.Data, modeColumn, modes);
        grouped.Group([new SortInfo(modeColumn, true)]);
        sheet.Models.Data = grouped;
        sheet.Columns[modeColumn].Visible = false;
        return grouped;
    }

    internal static void Clear(SheetView sheet)
    {
        if (sheet.Models.Data is ResultModeGroupDataModel grouped)
        {
            sheet.Models.Data = grouped.TargetModel;
            sheet.ColumnCount = grouped._modeColumn;
        }
        sheet.RowCount = 0;
    }

    internal string? GetModeKey(int displayRow)
    {
        if (displayRow < 0 || displayRow >= RowCount) return null;
        if (IsGroup(displayRow))
        {
            // A collapsed group has no visible child, so use its displayed title.
            string? title = GetValue(displayRow, 0)?.ToString();
            foreach (var mode in _modes)
                if (mode.Title == title) return mode.Key;
            return null;
        }

        int sourceRow = GetIndex(displayRow);
        if (sourceRow < 0) return null;
        string? token = TargetModel.GetValue(sourceRow, _modeColumn)?.ToString();
        if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out int index) ||
            index < 0 || index >= _modes.Length)
            return null;
        return _modes[index].Key;
    }

    public override object GetValue(int row, int column)
    {
        if (!IsGroup(row)) return base.GetValue(row, column);
        if (column != 0) return "";

        int first = row + 1 < RowCount ? GetIndex(row + 1) : -1;
        if (first >= 0)
        {
            string? token = TargetModel.GetValue(first, _modeColumn)?.ToString();
            if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out int index) &&
                index >= 0 && index < _modes.Length)
                return _modes[index].Title;
        }

        // GroupDataModel retains the grouping value even after its children collapse.
        string? groupToken = base.GetValue(row, _modeColumn)?.ToString();
        return int.TryParse(groupToken, NumberStyles.None, CultureInfo.InvariantCulture, out int groupIndex) &&
            groupIndex >= 0 && groupIndex < _modes.Length
            ? _modes[groupIndex].Title : "";
    }
}
