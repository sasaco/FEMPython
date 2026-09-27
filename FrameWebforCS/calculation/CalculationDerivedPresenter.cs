using FrameWebforCS.components.input;
using System.Collections.ObjectModel;

namespace FrameWebforCS.calculation;

/// <summary>Computes static DEFINE/COMBINE/PICKUP display modes from v1 results.</summary>
internal static class CalculationDerivedPresenter
{
    private static readonly string[] DisplacementNames = ["dx", "dy", "dz", "rx", "ry", "rz"];
    private static readonly string[] ForceNames = ["fx", "fy", "fz", "mx", "my", "mz"];

    internal static CalculationDerivedPresentation Build(
        AnalysisResultSet resultSet,
        int dimension,
        IReadOnlyDictionary<int, clsCombine<int>> defineRows,
        IReadOnlyDictionary<int, clsCombine<double>> combineRows,
        IReadOnlyDictionary<int, clsCombine<int>> pickupRows) =>
        Build(resultSet, dimension, CalculationDerivedInputSnapshot.Capture(
            defineRows, combineRows, pickupRows));

    internal static CalculationDerivedPresentation Build(
        AnalysisResultSet resultSet,
        int dimension,
        CalculationDerivedInputSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(resultSet);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (dimension is not (2 or 3))
            throw new ArgumentOutOfRangeException(nameof(dimension));
        AnalysisResultSetValidator.Validate(resultSet);

        IReadOnlyList<CalculationDerivedDefinitionInput> definitions = snapshot.Definitions;
        IReadOnlyList<CalculationDerivedCombinationInput> combinations = snapshot.Combinations;
        IReadOnlyList<CalculationDerivedPickupInput> pickups = snapshot.Pickups;

        var cases = resultSet.Cases.ToDictionary(item => item.CaseId, StringComparer.Ordinal);
        var staticResults = resultSet.Results.OfType<StaticAnalysisResult>()
            .ToDictionary(item => item.CaseId, StringComparer.Ordinal);
        var staticOrder = resultSet.Cases.Where(item => staticResults.ContainsKey(item.CaseId))
            .Select(item => item.CaseId).ToArray();
        var allIds = resultSet.Cases.Select(item => item.CaseId).ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<string> Expand(int signedCase)
        {
            if (signedCase == int.MinValue)
                throw new InvalidOperationException("DEFINE case ID is outside the supported range.");
            if (signedCase == 0) return ["0"];
            string parentId = Math.Abs(signedCase).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (allIds.Contains(parentId) && !staticResults.ContainsKey(parentId))
                throw new InvalidOperationException($"DEFINE references non-static case {parentId}.");
            if (!staticResults.ContainsKey(parentId)) return [];
            var expanded = new List<string> { signedCase < 0 ? "-" + parentId : parentId };
            if (cases[parentId].Symbol.Contains("LL", StringComparison.Ordinal))
            {
                foreach (string childId in staticOrder.Where(id =>
                    id.StartsWith(parentId + ".", StringComparison.Ordinal)))
                    expanded.Add(childId);
                foreach (string childId in allIds.Where(id =>
                    id.StartsWith(parentId + ".", StringComparison.Ordinal) &&
                    !staticResults.ContainsKey(id)))
                    throw new InvalidOperationException($"DEFINE references non-static case {childId}.");
            }
            return expanded;
        }

        var expandedDefinitions = definitions.Select(item => new ExpandedDefinition(item.Id, item.Name,
            item.SignedCases.SelectMany(Expand).ToArray())).Where(item => item.SourceIds.Length > 0).ToArray();
        if (definitions.Count == 0)
        {
            // Fallback definitions use the canonical case ID directly, including non-numeric IDs.
            expandedDefinitions = staticOrder.Select(id => new ExpandedDefinition(id, null,
                cases[id].Symbol.Contains("LL", StringComparison.Ordinal)
                    ? [id, .. staticOrder.Where(child => child.StartsWith(id + ".", StringComparison.Ordinal))]
                    : [id])).ToArray();
        }

        if (definitions.Count == 0)
            foreach (CalculationDerivedCombinationInput combination in combinations)
                foreach (var term in combination.Terms)
                    if (term.Coefficient != 0 && allIds.Contains(term.Id) &&
                        !staticResults.ContainsKey(term.Id))
                        throw new InvalidOperationException(
                            $"COMBINE {combination.Id} references non-static case {term.Id}.");
        if (definitions.Count == 0 && combinations.Count == 0)
            foreach (CalculationDerivedPickupInput pickup in pickups)
                foreach (string id in pickup.CombinationIds)
                    if (allIds.Contains(id) && !staticResults.ContainsKey(id))
                        throw new InvalidOperationException(
                            $"PICKUP {pickup.Id} references non-static case {id}.");

        var displacement = BuildQuantity(staticResults, staticOrder, expandedDefinitions,
            combinations, pickups, dimension, DisplacementNames, result =>
                result.NodeDisplacements.Select(row => new SourceRow(row.NodeId, null,
                    [row.Components.Dx, row.Components.Dy, row.Components.Dz,
                     row.Components.Rx, row.Components.Ry, row.Components.Rz])).ToArray());
        var reaction = BuildQuantity(staticResults, staticOrder, expandedDefinitions,
            combinations, pickups, dimension, ForceNames, result =>
                result.SupportReactions.Select(row => new SourceRow(row.NodeId, null,
                    [row.Components.Fx, row.Components.Fy, row.Components.Fz,
                     row.Components.Mx, row.Components.My, row.Components.Mz])).ToArray());
        var section = BuildQuantity(staticResults, staticOrder, expandedDefinitions,
            combinations, pickups, dimension, ForceNames, SectionRows);

        CalculationDerivedCase[] Compose(int stage, IEnumerable<(string Id, string? Name)> names) =>
            names.Select(item => new CalculationDerivedCase(item.Id, item.Name,
                displacement[stage].GetValueOrDefault(item.Id) ?? EmptyModes(),
                reaction[stage].GetValueOrDefault(item.Id) ?? EmptyModes(),
                section[stage].GetValueOrDefault(item.Id) ?? EmptyModes())).ToArray();

        return new CalculationDerivedPresentation(
            Compose(0, expandedDefinitions.Select(item => (item.Id, item.Name))),
            Compose(1, combinations.Select(item => (item.Id, item.Name))),
            Compose(2, pickups.Select(item => (item.Id, item.Name))));
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>> EmptyModes() =>
        CalculationDerivedPresentation.ReadOnlyModes(
            new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(StringComparer.Ordinal));

    private static Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>>>[]
        BuildQuantity(
            IReadOnlyDictionary<string, StaticAnalysisResult> staticResults,
            IReadOnlyList<string> staticOrder,
            IReadOnlyList<ExpandedDefinition> definitions,
            IReadOnlyList<CalculationDerivedCombinationInput> combinations,
            IReadOnlyList<CalculationDerivedPickupInput> pickups,
            int dimension,
            string[] componentNames,
            Func<StaticAnalysisResult, SourceRow[]> selectRows)
    {
        var baseRows = staticOrder.ToDictionary(id => id,
            id => selectRows(staticResults[id]).ToDictionary(row => row.Key, StringComparer.Ordinal),
            StringComparer.Ordinal);
        var modeIndices = dimension == 3 ? new[] { 0, 1, 2, 3, 4, 5 } : new[] { 0, 1, 5 };
        var stages = Enumerable.Range(0, 3).Select(_ =>
            new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>>>(
                StringComparer.Ordinal)).ToArray();

        foreach (ExpandedDefinition definition in definitions)
        {
            HashSet<string>? expectedKeys = null;
            foreach (string source in definition.SourceIds)
            {
                string caseId = source == "0" ? staticOrder.FirstOrDefault() ?? "" :
                    source.StartsWith("-", StringComparison.Ordinal) ? source[1..] : source;
                if (!baseRows.TryGetValue(caseId, out var sourceRows)) continue;
                if (expectedKeys is null) expectedKeys = sourceRows.Keys.ToHashSet(StringComparer.Ordinal);
                else if (!expectedKeys.SetEquals(sourceRows.Keys))
                    throw new InvalidOperationException(
                        $"DEFINE {definition.Id} references inconsistent result locations.");
            }
            var modes = new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(StringComparer.Ordinal);
            foreach (int focus in modeIndices)
                foreach (bool maximum in new[] { true, false })
                {
                    string mode = Mode(componentNames[focus], maximum);
                    var selected = new Dictionary<string, (SourceRow Row, double[] Values, string Source)>(
                        StringComparer.Ordinal);
                    foreach (string source in definition.SourceIds)
                    {
                        bool negative = source.StartsWith("-", StringComparison.Ordinal);
                        string caseId = negative ? source[1..] : source;
                        if (source == "0") caseId = staticOrder.FirstOrDefault() ?? "";
                        if (!baseRows.TryGetValue(caseId, out var rows)) continue;
                        double sign = source == "0" ? 0 : negative ? -1 : 1;
                        foreach (SourceRow row in rows.Values)
                        {
                            double[] values = row.Values.Select(value => sign * value).ToArray();
                            if (!selected.TryGetValue(row.Key, out var current) ||
                                (maximum ? values[focus] > current.Values[focus] :
                                    values[focus] < current.Values[focus]))
                                selected[row.Key] = (row, values, source);
                        }
                    }
                    modes[mode] = FreezeRows(selected.Values.Select(item =>
                        Row(item.Row, item.Values, componentNames, item.Source, item.Source)));
                }
            stages[0].Add(definition.Id, CalculationDerivedPresentation.ReadOnlyModes(modes));
        }

        foreach (CalculationDerivedCombinationInput combination in combinations)
        {
            var modes = new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(StringComparer.Ordinal);
            foreach (int focus in modeIndices)
                foreach (bool maximum in new[] { true, false })
                {
                    string mode = Mode(componentNames[focus], maximum);
                    var sums = new Dictionary<string, (CalculationDerivedRow Row, double[] Values,
                        List<string> Sources, List<string> Terms)>(StringComparer.Ordinal);
                    HashSet<string>? expectedKeys = null;
                    foreach (var term in combination.Terms)
                    {
                        if (term.Coefficient == 0) continue;
                        if (!double.IsFinite(term.Coefficient))
                            throw new InvalidOperationException("COMBINE coefficient must be finite.");
                        if (!stages[0].TryGetValue(term.Id, out var definitionModes))
                        {
                            if (staticResults.ContainsKey(term.Id)) continue;
                            continue;
                        }
                        if (!definitionModes.TryGetValue(mode, out var rows)) continue;
                        var keys = rows.Select(row => Key(row.EntityId, row.StationId));
                        if (expectedKeys is null) expectedKeys = keys.ToHashSet(StringComparer.Ordinal);
                        else if (!expectedKeys.SetEquals(keys))
                            throw new InvalidOperationException(
                                $"COMBINE {combination.Id} references inconsistent result locations.");
                        foreach (CalculationDerivedRow row in rows)
                        {
                            string key = Key(row.EntityId, row.StationId);
                            double[] values = componentNames.Select(name => row.Components[name] * term.Coefficient)
                                .ToArray();
                            string label = $"{term.Coefficient:G17}*D{term.Id}";
                            if (sums.TryGetValue(key, out var current))
                            {
                                for (int i = 0; i < values.Length; i++) values[i] += current.Values[i];
                                current.Sources.Add(row.SourceCaseId);
                                current.Terms.Add(label);
                                sums[key] = (row, values, current.Sources, current.Terms);
                            }
                            else sums.Add(key, (row, values, [row.SourceCaseId], [label]));
                        }
                    }
                    modes[mode] = FreezeRows(sums.Values.Select(item =>
                    {
                        if (item.Values.Any(value => !double.IsFinite(value)))
                            throw new InvalidOperationException("COMBINE produced a non-finite result.");
                        return Row(new SourceRow(item.Row.EntityId, item.Row.StationId, item.Values),
                            item.Values, componentNames, string.Join(",", item.Sources),
                            string.Join(" + ", item.Terms));
                    }));
                }
            stages[1].Add(combination.Id, CalculationDerivedPresentation.ReadOnlyModes(modes));
        }

        foreach (CalculationDerivedPickupInput pickup in pickups)
        {
            var modes = new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(StringComparer.Ordinal);
            foreach (int focus in modeIndices)
                foreach (bool maximum in new[] { true, false })
                {
                    string mode = Mode(componentNames[focus], maximum);
                    var selected = new Dictionary<string, CalculationDerivedRow>(StringComparer.Ordinal);
                    foreach (string combinationId in pickup.CombinationIds)
                    {
                        if (!stages[1].TryGetValue(combinationId, out var combinationModes) ||
                            !combinationModes.TryGetValue(mode, out var rows)) continue;
                        foreach (CalculationDerivedRow row in rows)
                        {
                            string key = Key(row.EntityId, row.StationId);
                            if (!selected.TryGetValue(key, out var current) ||
                                (maximum ? row.Components[componentNames[focus]] >
                                    current.Components[componentNames[focus]] :
                                    row.Components[componentNames[focus]] <
                                    current.Components[componentNames[focus]]))
                                selected[key] = row with { SourceCaseId = combinationId };
                        }
                    }
                    modes[mode] = FreezeRows(selected.Values);
                }
            stages[2].Add(pickup.Id, CalculationDerivedPresentation.ReadOnlyModes(modes));
        }

        return stages;
    }

    private static SourceRow[] SectionRows(StaticAnalysisResult result)
    {
        var rows = new Dictionary<string, SourceRow>(StringComparer.Ordinal);
        foreach (MemberSectionForces member in result.MemberSectionForces)
            foreach (MemberSegmentResult segment in member.Segments)
            {
                void Add(string station, ForceComponents value)
                {
                    var row = new SourceRow(member.MemberId, station,
                        [value.Fx, value.Fy, value.Fz, value.Mx, value.My, value.Mz]);
                    rows.TryAdd(row.Key, row);
                }
                Add(segment.StationI, segment.IEnd);
                Add(segment.StationJ, segment.JEnd);
            }
        return rows.Values.ToArray();
    }

    private static string Key(string entityId, string? stationId) =>
        entityId + "\u0000" + (stationId ?? "");

    private static string Mode(string component, bool maximum) =>
        component + (maximum ? "_max" : "_min");

    private static CalculationDerivedRow Row(SourceRow row, double[] values,
        string[] names, string source, string provenance)
    {
        var components = new Dictionary<string, double>(StringComparer.Ordinal);
        for (int i = 0; i < names.Length; i++) components.Add(names[i], values[i]);
        return new(row.EntityId, row.StationId,
            new ReadOnlyDictionary<string, double>(components), source, provenance);
    }

    private static IReadOnlyList<CalculationDerivedRow> FreezeRows(
        IEnumerable<CalculationDerivedRow> rows) => Frozen.List(rows, nameof(rows));

    private sealed record ExpandedDefinition(string Id, string? Name, string[] SourceIds);
    private sealed record SourceRow(string EntityId, string? StationId, double[] Values)
    {
        internal string Key => CalculationDerivedPresenter.Key(EntityId, StationId);
    }
}
