using System.Globalization;
using System.Numerics;
using System.Text;

namespace FrameWebforCS.calculation;

/// <summary>Exports the correlated section-force vectors selected by PICKUP.</summary>
internal static class PickupExportFormatter
{
    private static readonly string[] ForceNames = ["fx", "fy", "fz", "mx", "my", "mz"];

    internal static string Format(CalculationResultPresentation presentation)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        if (presentation.Derived is null)
            throw new InvalidOperationException("PICKUP results are not ready.");

        bool is2D = presentation.Dimension == 2;
        var output = new StringBuilder(is2D
            ? "PickUpNo,着目力,部材No,最大CaseNo,最小CaseNo,着目点,着目点距離,最大Md,最大Vd,最大Nd,最小Md,最小Vd,最小Nd\n"
            : "PickUpNo,着目断面力,部材No,最大CaseNo,最小CaseNo,着目点,着目点距離,最大Fx,最大Fy,最大Fz,最大Mx,最大My,最大Mz,最小Fx,最小Fy,最小Fz,最小Mx,最小My,最小Mz\n");

        foreach (CalculationDerivedCase pickup in presentation.Derived.Pickups)
        {
            foreach ((string symbol, string label) in is2D
                ? new[] { ("mz", "M"), ("fy", "S"), ("fx", "N") }
                : ForceNames.Select(name => (name, name)))
            {
                if (!pickup.SectionForces.TryGetValue(symbol + "_max", out var maxima) ||
                    !pickup.SectionForces.TryGetValue(symbol + "_min", out var minima))
                    throw new InvalidOperationException($"PICKUP {pickup.Id} has incomplete {symbol} results.");

                var minimumByStation = minima.ToDictionary(
                    row => (row.EntityId, row.StationId));
                var maximumByStation = maxima.ToDictionary(
                    row => (row.EntityId, row.StationId));
                int exported = 0;
                foreach (TopologyMember member in presentation.ResultSet.Topology.Members)
                {
                    for (int index = 0; index < member.Stations.Count; index++)
                    {
                        MemberStation station = member.Stations[index];
                        var key = (member.MemberId, (string?)station.StationId);
                        if (!maximumByStation.TryGetValue(key, out CalculationDerivedRow? maximum))
                            continue;
                        if (!minimumByStation.TryGetValue(key, out CalculationDerivedRow? minimum))
                            throw new InvalidOperationException($"PICKUP {pickup.Id} has no minimum at {member.MemberId}/{station.StationId}.");
                        exported++;

                        string point = index == 0 ? "ITAN" :
                            index == member.Stations.Count - 1 ? "JTAN" :
                            index.ToString(CultureInfo.InvariantCulture);
                        if (is2D)
                        {
                            foreach (string value in new[]
                            {
                                pickup.Id, label, member.MemberId, maximum.SourceCaseId,
                                minimum.SourceCaseId, point
                            }) output.Append(FixedField(value, 5));
                            output.Append(FixedField(FixedNumber(station.Position, 3), 10));
                            foreach (CalculationDerivedRow row in new[] { maximum, minimum })
                                foreach (string component in new[] { "mz", "fy", "fx" })
                                    output.Append(FixedField(FixedNumber(row.Components[component], 2), 10));
                        }
                        else
                        {
                            var cells = new List<string>
                            {
                                CsvText(pickup.Id), CsvText(label), CsvText(member.MemberId),
                                CsvText(maximum.SourceCaseId), CsvText(minimum.SourceCaseId),
                                CsvText(point), Number(station.Position)
                            };
                            foreach (CalculationDerivedRow row in new[] { maximum, minimum })
                                cells.AddRange(ForceNames.Select(component => Number(row.Components[component])));
                            output.Append(string.Join(',', cells));
                        }
                        output.Append('\n');
                    }
                }
                if (exported != maximumByStation.Count ||
                    maximumByStation.Count != minimumByStation.Count ||
                    maximumByStation.Keys.Except(minimumByStation.Keys).Any())
                    throw new InvalidOperationException($"PICKUP {pickup.Id} has inconsistent {symbol} locations.");
            }
        }
        return output.ToString();
    }

    private static string FixedField(string value, int width) => value.Length <= width
        ? value.PadLeft(width)
        : throw new InvalidOperationException($"PICKUP value exceeds its {width}-character field: {value}");

    private static string Number(double value) => double.IsFinite(value)
        ? value.ToString("G", CultureInfo.InvariantCulture)
        : throw new InvalidOperationException("PICKUP contains a non-finite number.");

    private static string CsvText(string value)
    {
        string trimmed = value.TrimStart();
        if (trimmed.Length > 0 && (trimmed[0] is '=' or '+' or '-' or '@'))
            value = "'" + value;
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : value;
    }

    // Match JavaScript Number.toFixed for values near a binary64 decimal midpoint.
    private static string FixedNumber(double value, int digits)
    {
        if (!double.IsFinite(value)) throw new InvalidOperationException("PICKUP contains a non-finite number.");
        bool negative = value < 0;
        long bits = BitConverter.DoubleToInt64Bits(Math.Abs(value));
        int exponentBits = (int)((bits >> 52) & 0x7ff);
        long fraction = bits & ((1L << 52) - 1);
        BigInteger significand = exponentBits == 0 ? fraction : (1L << 52) | fraction;
        int exponent = exponentBits == 0 ? -1074 : exponentBits - 1023 - 52;
        BigInteger factor = BigInteger.Pow(10, digits);
        BigInteger scaled = significand * factor;
        if (exponent >= 0) scaled <<= exponent;
        else
        {
            BigInteger divisor = BigInteger.One << -exponent;
            scaled = BigInteger.DivRem(scaled, divisor, out BigInteger remainder);
            if (remainder * 2 >= divisor) scaled++;
        }
        BigInteger whole = BigInteger.DivRem(scaled, factor, out BigInteger decimalPart);
        return (negative ? "-" : "") + whole.ToString(CultureInfo.InvariantCulture) + "." +
            decimalPart.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');
    }
}
