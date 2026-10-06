using System.Collections.Immutable;

namespace FrameWebforCS.components.result;

internal static class ResultPickupReacAggregator
{
    internal static PickupTableOutput Calculate(ResultCombineReacSnapshot snapshot,
        ImmutableArray<PickupSelection> pickups, CancellationToken cancellationToken = default)
    {
        ResultCombineReacOutput combined = ResultCombineReacAggregator.Calculate(snapshot, cancellationToken);
        return SelectCombined(combined, snapshot.Dimension, pickups, cancellationToken);
    }

    internal static PickupTableOutput SelectCombined(ResultCombineReacOutput combined, int dimension,
        ImmutableArray<PickupSelection> pickups, CancellationToken cancellationToken = default) =>
        ResultPickupAggregator.Select(dimension, combined.Cases, pickups,
            dimension == 3 ? ResultPickupAggregator.Reac3D : ResultPickupAggregator.Reac2D,
            item => item.Id, item => item.Rows, (row, _) => row.Id,
            (row, mode) => mode[..2] switch
            {
                "tx" => row.Tx, "ty" => row.Ty, "tz" => row.Tz,
                "mx" => row.Mx, "my" => row.My, "mz" => row.Mz,
                _ => throw new ArgumentException("Unknown reaction mode.")
            },
            (row, _) => dimension == 3
                ? [row.Id, Format(row.Tx), Format(row.Ty), Format(row.Tz),
                    Format(row.Mx), Format(row.My), Format(row.Mz), row.Case]
                : [row.Id, Format(row.Tx), Format(row.Ty), Format(row.Mz), row.Case],
            cancellationToken);

    internal static string Format(double value) => ResultPrintNumberFormat.Force(value);
}
