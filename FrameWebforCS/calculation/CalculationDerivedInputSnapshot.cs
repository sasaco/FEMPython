using FrameWebforCS.components.input;
using System.Globalization;

namespace FrameWebforCS.calculation;

internal sealed record CalculationDerivedDefinitionInput(
    string Id, string? Name, IReadOnlyList<int> SignedCases);
internal sealed record CalculationDerivedCombinationInput(
    string Id, string? Name, IReadOnlyList<(string Id, double Coefficient)> Terms);
internal sealed record CalculationDerivedPickupInput(
    string Id, string? Name, IReadOnlyList<string> CombinationIds);

/// <summary>Independent copy of mutable DEFINE/COMBINE/PICKUP form rows.</summary>
internal sealed class CalculationDerivedInputSnapshot
{
    private CalculationDerivedInputSnapshot(
        IEnumerable<CalculationDerivedDefinitionInput> definitions,
        IEnumerable<CalculationDerivedCombinationInput> combinations,
        IEnumerable<CalculationDerivedPickupInput> pickups)
    {
        Definitions = Frozen.List(definitions, nameof(definitions));
        Combinations = Frozen.List(combinations, nameof(combinations));
        Pickups = Frozen.List(pickups, nameof(pickups));
    }

    internal IReadOnlyList<CalculationDerivedDefinitionInput> Definitions { get; }
    internal IReadOnlyList<CalculationDerivedCombinationInput> Combinations { get; }
    internal IReadOnlyList<CalculationDerivedPickupInput> Pickups { get; }

    /// <summary>Call on the UI thread with one consistent input revision.</summary>
    internal static CalculationDerivedInputSnapshot Capture(
        IReadOnlyDictionary<int, clsCombine<int>> defineRows,
        IReadOnlyDictionary<int, clsCombine<double>> combineRows,
        IReadOnlyDictionary<int, clsCombine<int>> pickupRows)
    {
        ArgumentNullException.ThrowIfNull(defineRows);
        ArgumentNullException.ThrowIfNull(combineRows);
        ArgumentNullException.ThrowIfNull(pickupRows);
        var definitions = defineRows.OrderBy(item => item.Key)
            .Select(item => new CalculationDerivedDefinitionInput(item.Value.Id, item.Value.name,
                Frozen.List(item.Value.Coefficients.OrderBy(term => term.Key)
                    .Select(term => term.Value), nameof(defineRows))));
        var combinations = combineRows.OrderBy(item => item.Key)
            .Select(item => new CalculationDerivedCombinationInput(item.Value.Id, item.Value.name,
                Frozen.List(item.Value.Coefficients.OrderBy(term => term.Key)
                    .Select(term => (Id: term.Key.ToString(CultureInfo.InvariantCulture),
                        Coefficient: term.Value)), nameof(combineRows))));
        var pickups = pickupRows.OrderBy(item => item.Key)
            .Select(item => new CalculationDerivedPickupInput(item.Value.Id, item.Value.name,
                Frozen.List(item.Value.Coefficients.OrderBy(term => term.Key)
                    .Select(term => term.Value.ToString(CultureInfo.InvariantCulture)),
                    nameof(pickupRows))));
        return new CalculationDerivedInputSnapshot(definitions, combinations, pickups);
    }
}
