using System.Collections.Immutable;
using System.Globalization;

namespace FrameWebforCS.components.result;

internal sealed record PickupSelection(string Id, string? Name, ImmutableArray<int> CombinationIds);
internal sealed record PickupTableCase(string Id, string? Name,
    IReadOnlyDictionary<string, IReadOnlyList<string[]>> Rows);
internal sealed record PickupTableOutput(int Dimension, IReadOnlyList<PickupTableCase> Cases);

internal static class ResultPickupAggregator
{
    internal const int MaxPickups = 1_000;
    internal const long MaxComparisons = 50_000_000;
    internal const long MaxOutputCells = 10_000_000;

    internal static readonly (string Key, string Title)[] Disg3D =
    [
        ("dx_max", "X方向の移動量 最大"), ("dx_min", "X方向の移動量 最小"),
        ("dy_max", "Y方向の移動量 最大"), ("dy_min", "Y方向の移動量 最小"),
        ("dz_max", "Z方向の移動量 最大"), ("dz_min", "Z方向の移動量 最小"),
        ("rx_max", "X軸回りの回転角 最大"), ("rx_min", "X軸回りの回転角 最小"),
        ("ry_max", "Y軸回りの回転角 最大"), ("ry_min", "Y軸回りの回転角 最小"),
        ("rz_max", "Z軸回りの回転角 最大"), ("rz_min", "Z軸回りの回転角 最小")
    ];
    internal static readonly (string Key, string Title)[] Disg2D =
    [Disg3D[0], Disg3D[1], Disg3D[2], Disg3D[3], Disg3D[10], Disg3D[11]];
    internal static readonly (string Key, string Title)[] Reac3D =
    [
        ("tx_max", "X方向の支点反力 最大"), ("tx_min", "X方向の支点反力 最小"),
        ("ty_max", "Y方向の支点反力 最大"), ("ty_min", "Y方向の支点反力 最小"),
        ("tz_max", "Z方向の支点反力 最大"), ("tz_min", "Z方向の支点反力 最小"),
        ("mx_max", "X軸回りの回転反力 最大"), ("mx_min", "X軸回りの回転反力 最小"),
        ("my_max", "Y軸回りの回転反力 最大"), ("my_min", "Y軸回りの回転反力 最小"),
        ("mz_max", "Z軸回りの回転反力 最大"), ("mz_min", "Z軸回りの回転反力 最小")
    ];
    internal static readonly (string Key, string Title)[] Reac2D =
    [Reac3D[0], Reac3D[1], Reac3D[2], Reac3D[3], Reac3D[10], Reac3D[11]];
    internal static readonly (string Key, string Title)[] Fsec3D =
    [
        ("fx_max", "軸方向力 最大"), ("fx_min", "軸方向力 最小"),
        ("fy_max", "y方向のせん断力 最大"), ("fy_min", "y方向のせん断力 最小"),
        ("fz_max", "z方向のせん断力 最大"), ("fz_min", "z方向のせん断力 最小"),
        ("mx_max", "ねじりモーメント 最大"), ("mx_min", "ねじりモーメント 最小"),
        ("my_max", "y軸回りの曲げモーメント 最大"), ("my_min", "y軸回りの曲げモーメント 最小"),
        ("mz_max", "z軸回りの曲げモーメント 最大"), ("mz_min", "z軸回りの曲げモーメント 最小")
    ];
    internal static readonly (string Key, string Title)[] Fsec2D =
    [Fsec3D[0], Fsec3D[1], Fsec3D[2], Fsec3D[3], Fsec3D[10], Fsec3D[11]];

    internal static PickupTableOutput Select<TCase, TRow>(
        int dimension, IReadOnlyList<TCase> combinations,
        ImmutableArray<PickupSelection> pickups,
        IReadOnlyList<(string Key, string Title)> modes,
        Func<TCase, string> caseId,
        Func<TCase, IReadOnlyDictionary<string, IReadOnlyList<TRow>>> rowsOf,
        Func<TRow, int, string> rowKey,
        Func<TRow, string, double> focus,
        Func<TRow, string, string[]> display,
        CancellationToken cancellationToken = default)
    {
        if (dimension is not (2 or 3)) throw new ArgumentOutOfRangeException(nameof(dimension));
        if (pickups.Length > MaxPickups) throw new InvalidOperationException("PICKUP count exceeds the supported limit.");
        var byId = combinations.ToDictionary(caseId, StringComparer.Ordinal);
        var output = new List<PickupTableCase>(pickups.Length);
        long comparisons = 0;
        long cells = 0;
        foreach (PickupSelection pickup in pickups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var selectedCases = pickup.CombinationIds
                .Select(id => byId.GetValueOrDefault(id.ToString(CultureInfo.InvariantCulture)))
                .Where(item => item is not null).ToArray();
            if (selectedCases.Length == 0) continue;
            var resultRows = new Dictionary<string, IReadOnlyList<string[]>>(StringComparer.Ordinal);
            foreach (var mode in modes)
            {
                var selected = new Dictionary<string, (TRow Row, string CaseId, double Focus)>();
                foreach (TCase? combination in selectedCases)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (combination is null || !rowsOf(combination).TryGetValue(mode.Key, out var source))
                        continue;
                    string sourceId = caseId(combination);
                    for (int i = 0; i < source.Count; i++)
                    {
                        TRow row = source[i];
                        double value = focus(row, mode.Key);
                        if (!double.IsFinite(value)) throw new InvalidOperationException("PICKUP input is not finite.");
                        string key = rowKey(row, i);
                        if (!selected.TryGetValue(key, out var previous) ||
                            (mode.Key.EndsWith("_max", StringComparison.Ordinal) ?
                                value > previous.Focus : value < previous.Focus))
                            selected[key] = (row, sourceId, value);
                        comparisons = checked(comparisons + 1);
                        if (comparisons > MaxComparisons)
                            throw new InvalidOperationException("PICKUP work exceeds the supported limit.");
                    }
                }
                var formatted = new List<string[]>(selected.Count);
                foreach (var choice in selected.Values)
                {
                    string[] columns = display(choice.Row, choice.CaseId);
                    cells = checked(cells + columns.Length);
                    if (cells > MaxOutputCells)
                        throw new InvalidOperationException("PICKUP output exceeds the supported limit.");
                    formatted.Add(columns);
                }
                resultRows.Add(mode.Key, formatted);
            }
            output.Add(new PickupTableCase(pickup.Id, pickup.Name, resultRows));
        }
        return new PickupTableOutput(dimension, output);
    }
}

internal static class ResultPickupDisgAggregator
{
    internal static PickupTableOutput Calculate(ResultCombineDisgSnapshot snapshot,
        ImmutableArray<PickupSelection> pickups, CancellationToken cancellationToken = default)
    {
        ResultCombineDisgOutput combined = ResultCombineDisgAggregator.Calculate(snapshot, cancellationToken);
        return SelectCombined(combined, snapshot.Dimension, pickups, cancellationToken);
    }

    internal static PickupTableOutput SelectCombined(ResultCombineDisgOutput combined, int dimension,
        ImmutableArray<PickupSelection> pickups, CancellationToken cancellationToken = default) =>
        ResultPickupAggregator.Select(dimension, combined.Cases, pickups,
            dimension == 3 ? ResultPickupAggregator.Disg3D : ResultPickupAggregator.Disg2D,
            item => item.Id, item => item.Rows, (row, _) => row.Id,
            (row, mode) => mode[..2] switch
            {
                "dx" => row.Dx, "dy" => row.Dy, "dz" => row.Dz,
                "rx" => row.Rx, "ry" => row.Ry, "rz" => row.Rz,
                _ => throw new ArgumentException("Unknown displacement mode.")
            },
            (row, _) => dimension == 3
                ? [row.Id, Format(row.Dx), Format(row.Dy), Format(row.Dz),
                    Format(row.Rx), Format(row.Ry), Format(row.Rz), row.Case]
                : [row.Id, Format(row.Dx), Format(row.Dy), Format(row.Rz), row.Case],
            cancellationToken);

    internal static string Format(double value) => ResultPrintNumberFormat.Displacement(value);
}
