using System.Globalization;
using System.Text;

namespace FrameWebforCS.calculation;

internal enum PickupNodeQuantity { Displacement, Reaction }

/// <summary>Exports node PICKUP extrema with their selected correlated vectors.</summary>
internal static class PickupNodeExportFormatter
{
    private static readonly string[] DisplacementNames = ["dx", "dy", "dz", "rx", "ry", "rz"];
    private static readonly string[] ReactionNames = ["fx", "fy", "fz", "mx", "my", "mz"];

    internal static string Format(CalculationResultPresentation presentation, PickupNodeQuantity quantity)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        CalculationDerivedPresentation derived = presentation.Derived ??
            throw new InvalidOperationException("PICKUP results are not ready.");
        if (derived.Pickups.Count == 0)
            throw new InvalidOperationException("There are no PICKUP definitions to export.");

        string[] components = quantity switch
        {
            PickupNodeQuantity.Displacement => DisplacementNames,
            PickupNodeQuantity.Reaction => ReactionNames,
            _ => throw new ArgumentOutOfRangeException(nameof(quantity)),
        };
        string[] focus = presentation.Dimension == 2
            ? [components[0], components[1], components[5]] : components;
        string length = presentation.ResultSet.Units.Length;
        string force = presentation.ResultSet.Units.Force;
        string[] units = quantity == PickupNodeQuantity.Displacement
            ? [length, length, length, "rad", "rad", "rad"]
            : [force, force, force, force + "*" + length, force + "*" + length,
                force + "*" + length];
        var header = new List<string>
        {
            "pickup_id", "focus_component", "node_id", "max_combine_id", "min_combine_id"
        };
        foreach (string prefix in new[] { "max", "min" })
            for (int index = 0; index < components.Length; index++)
                header.Add(CsvText($"{prefix}_{components[index]} ({units[index]})"));

        var output = new StringBuilder(string.Join(',', header)).Append('\n');
        string[] nodeOrder = presentation.ResultSet.Topology.Nodes.Select(node => node.NodeId).ToArray();
        var knownNodes = nodeOrder.ToHashSet(StringComparer.Ordinal);
        var eligibleNodes = quantity == PickupNodeQuantity.Reaction
            ? presentation.ResultSet.Results.OfType<StaticAnalysisResult>()
                .SelectMany(result => result.SupportReactions)
                .Select(reaction => reaction.NodeId).ToHashSet(StringComparer.Ordinal)
            : knownNodes;
        var knownCombinations = derived.Combines.Select(combination => combination.Id)
            .ToHashSet(StringComparer.Ordinal);
        foreach (CalculationDerivedCase pickup in derived.Pickups)
        {
            IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>> modes =
                quantity == PickupNodeQuantity.Displacement ? pickup.Displacements : pickup.Reactions;
            HashSet<string>? expectedReactionNodes = null;
            foreach (string component in focus)
            {
                if (!modes.TryGetValue(component + "_max", out var maximumRows) ||
                    !modes.TryGetValue(component + "_min", out var minimumRows))
                    throw new InvalidOperationException($"PICKUP {pickup.Id} has incomplete {component} modes.");

                Dictionary<string, CalculationDerivedRow> maxima = IndexRows(maximumRows, eligibleNodes,
                    knownCombinations, components, pickup.Id, component);
                Dictionary<string, CalculationDerivedRow> minima = IndexRows(minimumRows, eligibleNodes,
                    knownCombinations, components, pickup.Id, component);
                if (maxima.Count == 0 || maxima.Count != minima.Count ||
                    maxima.Keys.Except(minima.Keys, StringComparer.Ordinal).Any())
                    throw new InvalidOperationException($"PICKUP {pickup.Id} has inconsistent {component} nodes.");
                if (quantity == PickupNodeQuantity.Displacement && maxima.Count != nodeOrder.Length)
                    throw new InvalidOperationException($"PICKUP {pickup.Id} is missing {component} displacement nodes.");
                if (quantity == PickupNodeQuantity.Reaction)
                {
                    if (expectedReactionNodes is null)
                        expectedReactionNodes = maxima.Keys.ToHashSet(StringComparer.Ordinal);
                    else if (!expectedReactionNodes.SetEquals(maxima.Keys))
                        throw new InvalidOperationException($"PICKUP {pickup.Id} has inconsistent reaction nodes.");
                }

                foreach (string nodeId in nodeOrder)
                {
                    if (!maxima.TryGetValue(nodeId, out CalculationDerivedRow? maximum)) continue;
                    CalculationDerivedRow minimum = minima[nodeId];
                    var cells = new List<string>
                    {
                        CsvText(pickup.Id), CsvText(component), CsvText(nodeId),
                        CsvText(maximum.SourceCaseId), CsvText(minimum.SourceCaseId)
                    };
                    foreach (CalculationDerivedRow row in new[] { maximum, minimum })
                        cells.AddRange(components.Select(name => Number(row.Components[name])));
                    output.Append(string.Join(',', cells)).Append('\n');
                }
            }
        }
        return output.ToString();
    }

    private static Dictionary<string, CalculationDerivedRow> IndexRows(
        IReadOnlyList<CalculationDerivedRow> rows, HashSet<string> knownNodes,
        HashSet<string> knownCombinations, string[] components, string pickupId, string focus)
    {
        var byNode = new Dictionary<string, CalculationDerivedRow>(StringComparer.Ordinal);
        foreach (CalculationDerivedRow row in rows)
        {
            if (row.StationId is not null || !knownNodes.Contains(row.EntityId) ||
                !knownCombinations.Contains(row.SourceCaseId) ||
                components.Any(name => !row.Components.TryGetValue(name, out double value) ||
                    !double.IsFinite(value)) ||
                !byNode.TryAdd(row.EntityId, row))
                throw new InvalidOperationException($"PICKUP {pickupId} has invalid {focus} node data.");
        }
        return byNode;
    }

    private static string Number(double value) =>
        value.ToString("G17", CultureInfo.InvariantCulture);

    private static string CsvText(string value)
    {
        string trimmed = value.TrimStart();
        if (trimmed.Length > 0 && (trimmed[0] is '=' or '+' or '-' or '@'))
            value = "'" + value;
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : value;
    }
}
