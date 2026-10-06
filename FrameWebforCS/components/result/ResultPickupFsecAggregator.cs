using System.Collections.Immutable;
using System.Globalization;

namespace FrameWebforCS.components.result;

internal static class ResultPickupFsecAggregator
{
    internal static PickupTableOutput Calculate(ResultCombineFsecSnapshot snapshot,
        ImmutableArray<PickupSelection> pickups, CancellationToken cancellationToken = default)
    {
        ResultCombineFsecOutput combined = ResultCombineFsecAggregator.Calculate(snapshot, cancellationToken);
        return SelectCombined(combined, snapshot.Dimension, pickups, cancellationToken);
    }

    internal static PickupTableOutput SelectCombined(ResultCombineFsecOutput combined, int dimension,
        ImmutableArray<PickupSelection> pickups, CancellationToken cancellationToken = default) =>
        ResultPickupAggregator.Select(dimension, combined.Cases, pickups,
            dimension == 3 ? ResultPickupAggregator.Fsec3D : ResultPickupAggregator.Fsec2D,
            item => item.Id, item => item.Rows,
            (row, index) => index.ToString(CultureInfo.InvariantCulture),
            (row, mode) => mode[..2] switch
            {
                "fx" => row.Fx, "fy" => row.Fy, "fz" => row.Fz,
                "mx" => row.Mx, "my" => row.My, "mz" => row.Mz,
                _ => throw new ArgumentException("Unknown member-force mode.")
            },
            (row, caseId) => dimension == 3
                ? [row.MemberDisplay, row.NodeId, ResultPrintNumberFormat.Station(row.Location),
                    Format(row.Fx), Format(row.Fy), Format(row.Fz), Format(row.Mx),
                    Format(row.My), Format(row.Mz), caseId + ":" + row.Case]
                : [row.MemberDisplay, row.NodeId, ResultPrintNumberFormat.Station(row.Location),
                    Format(row.Fx), Format(row.Fy), Format(row.Mz), caseId + ":" + row.Case],
            cancellationToken);

    internal static string Format(double value) => double.IsFinite(value)
        ? ResultPrintNumberFormat.Force(value)
        : value.ToString("G", CultureInfo.InvariantCulture);
}
