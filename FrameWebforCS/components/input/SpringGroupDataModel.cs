using FarPoint.Win.Spread.Model;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace FrameWebforCS.components.input;

internal sealed class SpringGroupDataModel(
    ISheetDataModel target,
    IReadOnlyList<clsFixMember> rows,
    Func<string, float?> memberLength) : GroupDataModel(target)
{
    public override object GetValue(int row, int column)
    {
        if (!IsGroup(row)) return base.GetValue(row, column);
        int first = row + 1 < RowCount ? GetIndex(row + 1) : -1;
        if (first < 0 || first >= rows.Count) return "";
        string? memberId = rows[first].m;
        if (string.IsNullOrWhiteSpace(memberId)) return "";
        return column switch
        {
            0 => memberId,
            1 => memberLength(memberId) is float length
                ? length.ToString("0.00", CultureInfo.InvariantCulture) : "",
            _ => "***"
        };
    }
}
