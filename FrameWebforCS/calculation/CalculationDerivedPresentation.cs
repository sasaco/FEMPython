using System.Collections.ObjectModel;

namespace FrameWebforCS.calculation;

/// <summary>A selected whole result vector for one derived mode and location.</summary>
internal sealed record CalculationDerivedRow(
    string EntityId,
    string? StationId,
    IReadOnlyDictionary<string, double> Components,
    string SourceCaseId,
    string Provenance);

internal sealed class CalculationDerivedCase
{
    internal CalculationDerivedCase(
        string id,
        string? name,
        IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>> displacements,
        IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>> reactions,
        IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>> sectionForces)
    {
        Id = id;
        Name = name;
        Displacements = displacements;
        Reactions = reactions;
        SectionForces = sectionForces;
    }

    internal string Id { get; }
    internal string? Name { get; }
    internal IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>> Displacements { get; }
    internal IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>> Reactions { get; }
    internal IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>> SectionForces { get; }
}

/// <summary>Derived display data, kept separate from the canonical AnalysisResultSet.</summary>
internal sealed class CalculationDerivedPresentation
{
    internal CalculationDerivedPresentation(
        IEnumerable<CalculationDerivedCase> defines,
        IEnumerable<CalculationDerivedCase> combines,
        IEnumerable<CalculationDerivedCase> pickups)
    {
        Defines = Frozen.List(defines, nameof(defines));
        Combines = Frozen.List(combines, nameof(combines));
        Pickups = Frozen.List(pickups, nameof(pickups));
    }

    internal IReadOnlyList<CalculationDerivedCase> Defines { get; }
    internal IReadOnlyList<CalculationDerivedCase> Combines { get; }
    internal IReadOnlyList<CalculationDerivedCase> Pickups { get; }

    internal static IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>> ReadOnlyModes(
        IDictionary<string, IReadOnlyList<CalculationDerivedRow>> modes) =>
        new ReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>>(
            new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(modes, StringComparer.Ordinal));
}
