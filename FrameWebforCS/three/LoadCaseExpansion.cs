using FrameWebforCS.components.input;

namespace FrameWebforCS.three;

/// <summary>
/// Display projection of JS InputLoadService.getMemberLoadJson(0). The input rows remain
/// unchanged for editing and saving; the returned rows refer to one member each and keep
/// the source grid row for highlighting. Distances are model units, not normalized fractions.
/// </summary>
internal static class LoadCaseExpansion
{
    private const int MaximumExpandedRows = 100_000;

    internal static IReadOnlyDictionary<string, IReadOnlyList<LoadMemberDisplay>> ExpandMemberLoads(
        string caseId, IReadOnlyList<LoadMemberDisplay> rows,
        IReadOnlyDictionary<int, float> memberLengths, string symbol = "", float? llPitch = null)
    {
        ArgumentNullException.ThrowIfNull(caseId);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(memberLengths);

        var source = rows.OrderBy(row => row.Row).ToArray();
        // JS getMemberGroupLoad/checkIntoMember measure every member in whole mm.
        var roundedLengths = memberLengths.ToDictionary(item => item.Key,
            item => RoundMm(item.Value));
        var result = new Dictionary<string, IReadOnlyList<LoadMemberDisplay>>();
        int numericCase = 0;
        bool moving = symbol == "LL" && llPitch is > 0 && float.IsFinite(llPitch.Value) &&
            source.Length > 0 && int.TryParse(caseId, out numericCase);
        if (!moving)
        {
            var projected = MemberLoadExpansion.Convert(source, roundedLengths, null);
            if (projected.Count > 0) result[caseId] = projected;
            return result;
        }

        // JS getMemberLoadJson(0) shifts the first L1 by LL_pitch and uses decimal case
        // keys (e.g. 1, 1.1, 1.2). Its get_LL_position starts before the member chain
        // when a consecutive input row uses negative L1/L2. These are display cases;
        // the editable load case stays at the integral ID.
        // Legacy clears the first L1 before get_LL_position scans negative distances.
        float negativeDistances = source.Sum(row => Math.Max(0, -row.L2)) +
            source.Skip(1).Sum(row => Math.Max(0, -row.L1));
        var first = source[0];
        int firstId = SafeMagnitude(first.MemberStart);
        int lastId = SafeMagnitude(first.MemberEnd);
        if (lastId == 0) lastId = firstId;
        float chainLength = 0;
        for (int memberId = firstId; memberId <= lastId && memberId <= 100_000; memberId++)
            if (roundedLengths.TryGetValue(memberId, out float length) && float.IsFinite(length) && length > 0)
                chainLength += length;
        float unboundedCount = (chainLength + negativeDistances) / llPitch.Value;
        int count = Math.Clamp(float.IsFinite(unboundedCount) && unboundedCount < int.MaxValue
                ? (int)JsRound(unboundedCount, 10) : MaximumExpandedRows,
            0, MaximumExpandedRows);
        int digits = count.ToString().Length;
        double divisor = Math.Pow(10, digits);
        for (int step = 0; step <= count; step++)
        {
            float shift = RoundMm(-negativeDistances + RoundMm(step * llPitch.Value));
            var projected = MemberLoadExpansion.Convert(source, roundedLengths, shift);
            if (projected.Count == 0) continue;
            string key = step == 0 ? caseId : (numericCase + step / divisor)
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
            result[key] = projected;
        }
        // Legacy promotes the first surviving fractional case to the integral key.
        if (!result.ContainsKey(caseId) && result.Count > 0)
        {
            string firstKey = result.Keys.First();
            result[caseId] = result[firstKey];
            result.Remove(firstKey);
        }
        return result;
    }

    private static int SafeMagnitude(int value) => value == int.MinValue ? 0 : Math.Abs(value);
    private static float RoundMm(float value) => JsRound(value, 1000);
    private static float JsRound(float value, int scale) =>
        float.IsFinite(value) ? (float)(Math.Floor((double)value * scale + 0.5) / scale) : value;
}
