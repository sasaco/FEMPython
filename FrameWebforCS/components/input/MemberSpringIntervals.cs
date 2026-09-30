using System;
using System.Collections.Generic;
using System.Linq;

namespace FrameWebforCS.components.input;

internal readonly record struct MemberSpringSpan(clsFixMember Row, double Start, double End);

internal static class MemberSpringIntervals
{
    internal static IReadOnlyList<MemberSpringSpan> Resolve(
        IEnumerable<clsFixMember> memberRows, double memberLength)
    {
        if (!double.IsFinite(memberLength) || memberLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(memberLength));
        var rows = memberRows.OrderBy(row => row.row).ToArray();
        if (rows.Length == 0) return Array.Empty<MemberSpringSpan>();
        double tolerance = 1e-6 * Math.Max(1, memberLength);
        if (rows.All(row => row.length == null))
            return rows.Select(row => new MemberSpringSpan(row, 0, memberLength)).ToArray();

        var result = new List<MemberSpringSpan>(rows.Length);
        double cursor = 0;
        for (int index = 0; index < rows.Length; index++)
        {
            clsFixMember row = rows[index];
            double end;
            if (row.length is float length)
            {
                if (!float.IsFinite(length) || length <= 0)
                    throw new ArgumentException($"Invalid spring length in row {row.row}.");
                end = cursor + length;
                if (end > memberLength + tolerance)
                    throw new ArgumentException($"Spring lengths exceed the member in row {row.row}.");
                if (end > memberLength) end = memberLength;
            }
            else
            {
                if (index != rows.Length - 1)
                    throw new ArgumentException($"Only the final spring row may omit length (row {row.row}).");
                if (memberLength - cursor <= tolerance)
                    throw new ArgumentException($"No remaining member length for spring row {row.row}.");
                end = memberLength;
            }
            if (end - cursor <= tolerance)
                throw new ArgumentException($"Spring interval is empty in row {row.row}.");
            result.Add(new MemberSpringSpan(row, cursor, end));
            cursor = end;
        }
        return result;
    }
}
