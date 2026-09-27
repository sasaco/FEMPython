namespace FrameWebforCS.components.input;

/// <summary>
/// The ordered getEnableLoad, repeat, checkIntoMemberL1, checkMember2 and
/// checkIntoMember stages from JS InputLoadService.getMemberLoadJson(0).
/// Raw row distance carry and physical member/local carry are independent.
/// </summary>
internal static class LoadLegacyMemberConversion
{
    private const int MaximumExpandedRows = 100_000;

    private sealed class LegacyRow
    {
        internal int row, m1, m2, mark;
        internal string direction = "";
        internal float L1, L2, P1, P2;
        internal bool negativeL1;

        internal LegacyRow Clone() => (LegacyRow)MemberwiseClone();
        internal LoadMemberDisplay Display() =>
            new(row, m1, m1, direction, mark, L1, L2, P1, P2);
    }

    internal static IReadOnlyList<LoadMemberDisplay> Convert(
        IReadOnlyList<LoadMemberDisplay> source, IReadOnlyDictionary<int, float> lengths,
        float? firstL1)
    {
        var enabled = new List<LegacyRow>();
        for (int index = 0; index < source.Count; index++)
        {
            // JS replaces load1[0].L1 before getEnableLoad filters rows.
            var input = index == 0 && firstL1.HasValue
                ? source[index] with { L1 = firstL1.Value } : source[index];
            if (input.Mark is not (1 or 2 or 9 or 11) ||
                input.Mark != 9 && string.IsNullOrWhiteSpace(input.Direction) ||
                !float.IsFinite(input.L1) || !float.IsFinite(input.L2) ||
                !float.IsFinite(input.P1) || !float.IsFinite(input.P2)) continue;
            var item = new LegacyRow
            {
                row = input.Row, m1 = input.MemberStart, m2 = input.MemberEnd,
                direction = input.Mark == 9 ? "x" : input.Direction.Trim().ToLowerInvariant(),
                mark = input.Mark, L1 = input.L1, L2 = input.L2,
                P1 = input.P1, P2 = input.P2, negativeL1 = input.L1 < 0
            };
            if (item.m1 == 0) item.m1 = item.m2;
            if (item.m2 == 0) item.m2 = item.m1;
            if (SafeMagnitude(item.m1) > SafeMagnitude(item.m2))
            {
                if (item.m2 < 0) (item.m1, item.m2) = (-item.m2, -item.m1);
                else (item.m1, item.m2) = (item.m2, item.m1);
            }
            enabled.Add(item);
        }
        if (enabled.Count == 0) return [];

        var repeated = RepeatMemberLoads(enabled).OrderBy(item => item.row).ToList();
        var started = CheckIntoMemberL1(repeated);
        var grouped = CheckMember2(started, lengths);
        var split = RepeatMemberLoads(grouped);
        var result = new List<LoadMemberDisplay>();
        foreach (var item in split)
        {
            CheckIntoMember(item, lengths);
            if (item.P1 == 0 && item.P2 == 0 ||
                !float.IsFinite(item.P1) || !float.IsFinite(item.P2) ||
                !float.IsFinite(item.L1) || !float.IsFinite(item.L2)) continue;
            result.Add(item.Display());
            if (result.Count >= MaximumExpandedRows) break;
        }
        return result;
    }

    private static List<LegacyRow> RepeatMemberLoads(IEnumerable<LegacyRow> rows)
    {
        var result = new List<LegacyRow>();
        foreach (var row in rows)
        {
            if (row.m1 < row.m2)
            {
                for (int id = SafeMagnitude(row.m1); id <= SafeMagnitude(row.m2); id++)
                {
                    var item = row.Clone();
                    item.m1 = item.m2 = id;
                    result.Add(item);
                    if (result.Count >= MaximumExpandedRows) return result;
                }
            }
            else result.Add(row.Clone());
            if (result.Count >= MaximumExpandedRows) return result;
        }
        return result;
    }

    // checkIntoMemberL1 carries the input row's L2 before member splitting.
    private static List<LegacyRow> CheckIntoMemberL1(IEnumerable<LegacyRow> rows)
    {
        var result = new List<LegacyRow>();
        int curPos2 = 0, oldRow = -1;
        foreach (var source in rows)
        {
            var row = source.Clone();
            int L1 = RoundMillimetres(row.L1), L2 = RoundMillimetres(row.L2);
            int curPos1 = oldRow + 1 < row.row ? L1 :
                L1 < 0 ? curPos2 + Math.Abs(L1) : L1;
            oldRow = row.row;
            curPos2 = L2 < 0 ? curPos1 + Math.Abs(L2) : L2;
            if (row.mark is 1 or 11)
            {
                if (curPos1 <= 0 && curPos2 <= 0) continue;
                if (curPos1 < 0) { row.L1 = 0; row.P1 = 0; }
                else row.L1 = curPos1 / 1000f;
                row.L2 = curPos2 / 1000f;
                row.negativeL1 = false;
            }
            else if (row.mark == 2 && curPos1 < 0)
            {
                int abs1 = Math.Abs(curPos1), abs2 = Math.Abs(curPos2);
                row.P1 += abs1 * (row.P2 - row.P1) / (abs1 + abs2);
                row.L1 = 0;
                row.L2 = -curPos2 / 1000f;
                row.negativeL1 = false;
            }
            result.Add(row);
        }
        return result;
    }

    // checkMember2 carries the physical member and local position returned by
    // getMemberGroupLoad. It resets independently of checkIntoMemberL1.
    private static List<LegacyRow> CheckMember2(IEnumerable<LegacyRow> rows,
        IReadOnlyDictionary<int, float> lengths)
    {
        var result = new List<LegacyRow>();
        int curNo = -1, oldRow = -1;
        float curPos = 0;
        foreach (var source in rows)
        {
            if (oldRow + 1 < source.row) { curNo = -1; curPos = 0; }
            oldRow = source.row;
            var row = source.Clone();
            int curPosMm = RoundMillimetres(curPos);
            int originalM1 = SafeMagnitude(row.m1);
            GetL1Position(row, lengths, ref curNo, ref curPosMm);
            GetL2Position(row, lengths, originalM1, ref curNo, ref curPosMm);
            result.AddRange(GetMemberGroupLoad(row, lengths));
            if (result.Count >= MaximumExpandedRows) break;
            curPos = curPosMm / 1000f;
        }
        return result;
    }

    private static void GetL1Position(LegacyRow row, IReadOnlyDictionary<int, float> lengths,
        ref int curNo, ref int curPos)
    {
        int m1 = SafeMagnitude(row.m1), m2 = SafeMagnitude(row.m2);
        int L1 = RoundMillimetres(Math.Abs(row.L1));
        int L2 = RoundMillimetres(Math.Abs(row.L2));
        if (row.negativeL1)
        {
            if (m1 <= curNo && curNo <= m2)
            {
                m1 = curNo;
                L1 += curPos;
                row.m1 = m1;
                row.L1 = L1 / 1000f;
            }
            if (curNo < m1 && curNo < m2)
            {
                row.m1 = m1;
                L1 = RoundMillimetres(row.L1);
            }
        }
        for (int id = m1; id <= m2; id++)
        {
            int length = LengthMm(lengths, id);
            if (L1 > length)
            {
                if (id + 1 > m2)
                {
                    row.m1 = m2; row.L1 = 0; row.P1 = 0;
                    break;
                }
                L1 -= length;
                if (row.mark is 1 or 11) L2 -= length;
                row.m1 = id + 1;
                row.L1 = L1 / 1000f;
            }
            else { row.m1 = id; break; }
        }
        curNo = row.m1;
        curPos = RoundMillimetres(row.L1);
        row.L1 = L1 / 1000f;
        if (row.L2 >= 0) row.L2 = L2 / 1000f;
    }

    private static void GetL2Position(LegacyRow row, IReadOnlyDictionary<int, float> lengths,
        int originalM1, ref int curNo, ref int curPos)
    {
        int m1 = SafeMagnitude(row.m1), m2 = SafeMagnitude(row.m2);
        int L2 = RoundMillimetres(Math.Abs(row.L2));
        float L1 = row.L1 * 1000;
        if (row.mark is 1 or 11)
        {
            if (row.L2 < 0) L2 = (int)L1 + L2;
            for (int id = m1; id <= m2; id++)
            {
                int length = LengthMm(lengths, id);
                if (L2 > length)
                {
                    if (id + 1 > m2)
                    {
                        row.m2 = m2; row.L2 = 0; row.P2 = 0; curNo = -1;
                        break;
                    }
                    L2 -= length;
                    row.m2 = id + 1; row.L2 = L2 / 1000f;
                    curNo = SafeMagnitude(row.m2);
                }
                else
                {
                    row.m2 = id; row.L2 = L2 / 1000f;
                    curNo = SafeMagnitude(row.m2);
                    break;
                }
            }
            curPos = RoundMillimetres(row.L2);
            return;
        }

        if (row.L2 < 0)
        {
            int total = 0;
            for (int id = m1; id <= m2; id++) total += LengthMm(lengths, id);
            L2 = Math.Max(0, total - (curPos + L2));
            row.m2 = m2; row.L2 = L2 / 1000f;
        }
        int lastLength = 0;
        for (int id = m2; id >= originalM1; id--)
        {
            lastLength = LengthMm(lengths, id);
            if (L2 > lastLength)
            {
                L2 -= lastLength;
                row.m2 = id - 1; row.L2 = L2 / 1000f;
            }
            else break;
        }
        if (curNo <= row.m2)
        {
            curNo = SafeMagnitude(row.m2);
            curPos = lastLength - RoundMillimetres(Math.Abs(row.L2));
        }
        else curPos = -RoundMillimetres(Math.Abs(row.L2));
    }

    private static List<LegacyRow> GetMemberGroupLoad(LegacyRow row,
        IReadOnlyDictionary<int, float> lengths)
    {
        var output = new List<LegacyRow>();
        int m1 = SafeMagnitude(row.m1), m2 = SafeMagnitude(row.m2);
        if (m1 <= 0 || m2 < m1 || m2 > MaximumExpandedRows) return output;
        int total = 0;
        for (int id = m1; id <= m2; id++) total += LengthMm(lengths, id);
        int L1 = RoundMillimetres(row.L1), L2 = RoundMillimetres(row.L2);
        if (row.mark is 1 or 11)
        {
            if (m1 == m2 && row.L1 * row.L2 >= 0)
            {
                var item = row.Clone(); item.m1 = item.m2 = m1; output.Add(item);
            }
            else
            {
                var first = row.Clone();
                first.m1 = first.m2 = m1; first.L2 = 0; first.P2 = 0;
                output.Add(first);
                var second = row.Clone();
                second.m1 = second.m2 = m2; second.L1 = row.L2;
                second.L2 = 0; second.P1 = row.P2; second.P2 = 0;
                output.Add(second);
            }
        }
        else if (row.mark == 9)
        {
            for (int id = m1; id <= m2; id++)
            {
                var item = row.Clone();
                item.m1 = item.m2 = id; item.L1 = item.L2 = 0;
                item.P2 = row.P1; output.Add(item);
            }
        }
        else
        {
            double slope = (row.P2 - row.P1) / (double)(total - L1 - L2);
            double next = row.P1;
            for (int id = m1; id <= m2; id++)
            {
                int segment = LengthMm(lengths, id);
                if (id == m1) segment -= L1;
                else if (id == m2) segment -= L2;
                var item = row.Clone();
                item.m1 = item.m2 = id; item.L1 = item.L2 = 0;
                item.P1 = (float)next;
                next += slope * segment;
                item.P2 = (float)next;
                if (id == m1) { item.L1 = row.L1; item.P1 = row.P1; }
                if (id == m2) { item.L2 = row.L2; item.P2 = row.P2; }
                output.Add(item);
            }
        }
        return output;
    }

    private static void CheckIntoMember(LegacyRow row, IReadOnlyDictionary<int, float> lengths)
    {
        int L1 = RoundMillimetres(row.L1), L2 = RoundMillimetres(row.L2);
        int length = LengthMm(lengths, row.m1);
        row.P1 = RoundCent(row.P1); row.P2 = RoundCent(row.P2);
        if (row.mark is 1 or 11)
        {
            if (L1 < 0 || L1 > length) { L1 = 0; row.P1 = 0; }
            if (L2 < 0 || L2 > length) { L2 = 0; row.P2 = 0; }
        }
        else if (row.mark is 2 or 9)
        {
            if (L1 + L2 > length)
            {
                L1 = L2 = 0; row.P1 = row.P2 = 0;
            }
            else
            {
                L1 = Math.Max(0, L1); L2 = Math.Max(0, L2);
            }
        }
        row.L1 = L1 / 1000f; row.L2 = L2 / 1000f;
    }

    private static int SafeMagnitude(int value) => value == int.MinValue ? 0 : Math.Abs(value);
    private static int LengthMm(IReadOnlyDictionary<int, float> lengths, int id) =>
        lengths.TryGetValue(id, out float value) && float.IsFinite(value) && value > 0
            ? RoundMillimetres(value) : 0;
    private static int RoundMillimetres(float value) => (int)Math.Floor((double)value * 1000 + 0.5);
    private static float RoundCent(float value) =>
        (float)(Math.Floor((double)value * 100 + 0.5) / 100);
}
