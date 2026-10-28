using FrameWebforCS.calculation;
using FrameWebforCS.components.input;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FrameWebforCS.providers.printing;

/// <summary>Print-only labels and evidence for collapsing ordinary combination rows.</summary>
internal static class PrintResultMetadata
{
    internal static void Apply(JsonObject root, JsonObject saved, PrintSnapshot snapshot)
    {
        CalculationResultPresentation presentation = snapshot.Result!;
        if (presentation.Dimension == 2) root["referenceResultLayout"] = true;
        AddLabels(root, saved, presentation);
        var ordinary = new JsonArray();
        root["ordinaryDisplacementCombinations"] = ordinary;
        if (snapshot.Derived is null)
        {
            PrintFoundationOccurrences.Apply(root, saved, snapshot);
            return;
        }
        using JsonDocument input = JsonDocument.Parse(saved.ToJsonString());
        var parsed = InputCombineService.ParseCombineJson(input.RootElement);
        var definitions = CalculationDerivedInputSnapshot.Capture(
            parsed.Define, parsed.Combine, parsed.Pickup);
        AddProvenance(root, snapshot, definitions);
        var staticIds = presentation.ResultSet.Results.OfType<StaticAnalysisResult>()
            .Select(result => result.CaseId).ToHashSet(StringComparer.Ordinal);
        var cases = presentation.ResultSet.Cases.ToDictionary(item => item.CaseId, StringComparer.Ordinal);
        var sourceCounts = definitions.Definitions.ToDictionary(definition => definition.Id,
            definition => definition.SignedCases.Sum(SourceCount), StringComparer.Ordinal);
        var combinations = snapshot.Derived.Combines.ToDictionary(item => item.Id);
        foreach (CalculationDerivedCombinationInput combination in definitions.Combinations)
        {
            var terms = combination.Terms.Where(term => term.Coefficient != 0).ToArray();
            bool singleton = terms.Length > 0 && terms.All(term => definitions.Definitions.Count == 0
                ? StaticSourceCount(term.Id) == 1 : sourceCounts.GetValueOrDefault(term.Id) == 1);
            combinations.TryGetValue(combination.Id, out CalculationDerivedCase? item);
            if (singleton && item is not null && HasOneDisplacementVector(item, presentation.Dimension))
                ordinary.Add(combination.Id);
        }
        PrintFoundationOccurrences.Apply(root, saved, snapshot);

        int SourceCount(int signedCase)
        {
            if (signedCase == 0) return staticIds.Count > 0 ? 1 : 0;
            if (signedCase == int.MinValue) return 0;
            return StaticSourceCount(Math.Abs(signedCase).ToString(CultureInfo.InvariantCulture));
        }
        int StaticSourceCount(string id)
        {
            if (!staticIds.Contains(id)) return 0;
            // Mirror the actual presenter, including its case-sensitive LL expansion.
            return cases[id].Symbol.Contains("LL", StringComparison.Ordinal)
                ? 1 + staticIds.Count(child => child.StartsWith(id + ".", StringComparison.Ordinal)) : 1;
        }
    }

    private static void AddProvenance(JsonObject root, PrintSnapshot snapshot,
        CalculationDerivedInputSnapshot input)
    {
        CalculationDerivedPresentation derived = snapshot.Derived!;
        CalculationResultPresentation presentation = snapshot.Result!;
        var staticIds = presentation.ResultSet.Results.OfType<StaticAnalysisResult>()
            .Select(result => result.CaseId).ToHashSet(StringComparer.Ordinal);
        var cases = presentation.ResultSet.Cases.ToDictionary(item => item.CaseId);
        var combinations = derived.Combines.ToDictionary(item => item.Id);
        var termsByCombination = input.Combinations.ToDictionary(combination => combination.Id,
            combination => combination.Terms.Where(term => term.Coefficient != 0).ToArray());
        var allowedByDefinition = input.Definitions.ToDictionary(definition => definition.Id,
            definition => definition.SignedCases.SelectMany(signed =>
                signed == 0 ? new[] { "0" } : signed == int.MinValue ? [] :
                    Expand(Math.Abs(signed).ToString(CultureInfo.InvariantCulture), signed < 0))
                .ToHashSet(StringComparer.Ordinal));
        if (input.Definitions.Count == 0)
            foreach (string id in termsByCombination.Values.SelectMany(terms => terms.Select(term => term.Id)).Distinct())
                allowedByDefinition[id] = Expand(id, false).ToHashSet(StringComparer.Ordinal);
        var combinationRows = new Dictionary<(string Id, string Family, string Mode),
            Dictionary<(string Entity, string? Station), CalculationDerivedRow>>();
        var expressionCache = new Dictionary<(string Combination, string Sources), string?>();
        foreach (var (key, family, pickups) in new[]
        {
            ("disgCombine", "disg", false), ("disgPickup", "disg", true),
            ("reacCombine", "reac", false), ("reacPickup", "reac", true),
            ("fsecCombine", "fsec", false), ("fsecPickup", "fsec", true),
        })
        {
            if (root[key] is not JsonObject projected) continue;
            foreach (CalculationDerivedCase item in pickups ? derived.Pickups : derived.Combines)
            {
                if (projected[item.Id] is not JsonObject modes) continue;
                foreach (var (displayMode, rows) in modes)
                {
                    string mode = displayMode;
                    if (family == "reac") mode = mode.Replace("tx_", "fx_").Replace("ty_", "fy_").Replace("tz_", "fz_");
                    mode = PrintProjection.DisplayMode(mode, family, presentation.Dimension);
                    if (!Modes(item, family).TryGetValue(mode, out var original)) continue;
                    var originals = original.ToDictionary(raw => (raw.EntityId, raw.StationId));
                    foreach (JsonObject row in Records(rows))
                    {
                        string entity = Text(row[family == "fsec" ? "m" : "id"]);
                        string? station = family == "fsec" ? Text(row["n"]) : null;
                        if (!originals.TryGetValue((entity, station), out CalculationDerivedRow? raw)) continue;
                        row["rawProvenance"] = raw.Provenance;
                        string combinationId = pickups ? raw.SourceCaseId : item.Id;
                        CalculationDerivedRow? source = raw;
                        if (pickups)
                        {
                            var cacheKey = (combinationId, family, mode);
                            if (!combinationRows.TryGetValue(cacheKey, out var candidates))
                            {
                                candidates = combinations.TryGetValue(combinationId, out CalculationDerivedCase? combination) &&
                                    Modes(combination, family).TryGetValue(mode, out var values)
                                    ? values.ToDictionary(candidate => (candidate.EntityId, candidate.StationId)) : [];
                                combinationRows[cacheKey] = candidates;
                            }
                            source = candidates.TryGetValue((entity, station), out CalculationDerivedRow? candidate) &&
                                candidate.Components.Count == raw.Components.Count && raw.Components.All(pair =>
                                    candidate.Components.TryGetValue(pair.Key, out double value) && value == pair.Value)
                                ? candidate : null;
                        }
                        string? expression = null;
                        if (source is not null)
                        {
                            var cacheKey = (combinationId, source.SourceCaseId);
                            if (!expressionCache.TryGetValue(cacheKey, out expression))
                                expressionCache[cacheKey] = expression = Expression(combinationId, source);
                        }
                        row["comb"] = expression ?? raw.Provenance;
                    }
                }
            }
        }

        string? Expression(string id, CalculationDerivedRow source)
        {
            if (!termsByCombination.TryGetValue(id, out var terms)) return null;
            string[] selected = source.SourceCaseId.Split(',');
            if (selected.Length != terms.Length || terms.Length == 0) return null;
            var expression = new List<string>();
            for (int i = 0; i < terms.Length; i++)
            {
                if (!allowedByDefinition.TryGetValue(terms[i].Id, out var allowed) || !allowed.Contains(selected[i])) return null;
                bool negativeSource = selected[i].StartsWith("-", StringComparison.Ordinal);
                double coefficient = terms[i].Coefficient * (negativeSource ? -1 : 1);
                string caseId = negativeSource ? selected[i][1..] : selected[i];
                string magnitude = Math.Abs(coefficient) == 1 ? caseId :
                    Math.Abs(coefficient).ToString("G", CultureInfo.InvariantCulture) + "*" + caseId;
                expression.Add(expression.Count == 0 ? (coefficient < 0 ? "-" : "") + magnitude :
                    (coefficient < 0 ? " - " : " + ") + magnitude);
            }
            return string.Concat(expression);
        }

        string[] Expand(string id, bool negative)
        {
            if (!staticIds.Contains(id)) return [];
            return cases[id].Symbol.Contains("LL", StringComparison.Ordinal)
                ? [negative ? "-" + id : id, .. presentation.ResultSet.Cases.Select(c => c.CaseId)
                    .Where(child => staticIds.Contains(child) && child.StartsWith(id + ".", StringComparison.Ordinal))]
                : [negative ? "-" + id : id];
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>> Modes(
        CalculationDerivedCase item, string family) => family switch
        {
            "disg" => item.Displacements,
            "reac" => item.Reactions,
            _ => item.SectionForces,
        };

    private static void AddLabels(JsonObject root, JsonObject saved, CalculationResultPresentation presentation)
    {
        var pages = presentation.Pages.ToDictionary(page => page.Key);
        foreach (string family in new[] { "disg", "reac", "fsec" })
        {
            if (root[family + "Name"] is not JsonArray names) continue;
            foreach (JsonArray pair in names.OfType<JsonArray>())
            {
                string key = Text(pair[0]);
                if (!pages.TryGetValue(key, out CalculationResultPage? page)) continue;
                string name = Text(saved["load"]?[page.Case.CaseId]?["name"]);
                if (string.IsNullOrWhiteSpace(name)) continue;
                string original = page.Case.Name.Length == 0 ? page.Case.CaseId : page.Case.Name;
                string suffix = page.Label.StartsWith(original, StringComparison.Ordinal)
                    ? page.Label[original.Length..] : "";
                pair[1] = name + suffix;
            }
        }
        var rigid = Records(saved["rigid"]).GroupBy(row => Text(row["m"]))
            .ToDictionary(group => group.Key, group => group.ToArray());
        var notices = Records(saved["notice_points"]).GroupBy(row => Text(row["m"]))
            .ToDictionary(group => group.Key, group => group.ToArray());
        var members = presentation.ResultSet.Topology.Members.ToDictionary(member => member.MemberId,
            member =>
            {
                // FrameG keeps the member name on its material (sheet 1); a member name wins.
                string name = Text(saved["member"]?[member.MemberId]?["name"]);
                if (string.IsNullOrWhiteSpace(name))
                    name = Text(saved["element"]?["1"]?[Text(saved["member"]?[member.MemberId]?["e"])]?["n"]);
                string caption = $"部 材 {member.MemberId,2} ( {member.NodeI} - {member.NodeJ} )" +
                    (string.IsNullOrWhiteSpace(name) ? "" : " " + name);
                return (Caption: caption, Labels: StationLabels(member,
                    rigid.GetValueOrDefault(member.MemberId) ?? [],
                    notices.GetValueOrDefault(member.MemberId) ?? []));
            });
        foreach (string key in new[] { "fsec", "fsecCombine", "fsecPickup" })
            if (root[key] is JsonObject family)
                LabelRows(family);

        void LabelRows(JsonNode node)
        {
            if (node is JsonArray array)
            {
                foreach (JsonNode? child in array) if (child is not null) LabelRows(child);
                return;
            }
            if (node is not JsonObject obj) return;
            if (obj.ContainsKey("m") && obj.ContainsKey("n") &&
                members.TryGetValue(Text(obj["m"]), out var member))
            {
                if (!member.Labels.TryGetValue(Text(obj["n"]), out string? label)) return;
                obj["memberCaption"] = member.Caption;
                obj["stationLabel"] = label;
                return;
            }
            foreach (JsonNode child in obj.Select(pair => pair.Value).OfType<JsonNode>().ToArray()) LabelRows(child);
        }
    }

    private static Dictionary<string, string> StationLabels(TopologyMember member,
        JsonObject[] rigidRows, JsonObject[] noticeRows)
    {
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        if (member.Stations.Count == 0) return labels;
        double length = member.Stations[^1].Position;
        double[] rigid = rigidRows.SelectMany(row => new[]
            {
                Number(row["Ilength"]) is > 0 and var il ? (double?)il : null,
                Number(row["Jlength"]) is > 0 and var jl ? (double?)(length - jl) : null,
            }).Where(value => value.HasValue).Select(value => value!.Value)
            .Distinct().OrderBy(value => value).ToArray();
        double[] notices = noticeRows
            .SelectMany(row => row["Points"] is JsonArray points ? points : new JsonArray())
            .Select(Number).Where(value => value is > 0 && value < length)
            .Select(value => value!.Value).Distinct().OrderBy(value => value).ToArray();
        for (int index = 0; index < member.Stations.Count; index++)
        {
            MemberStation station = member.Stations[index];
            string label;
            if (index == 0) label = "I-TAN";
            else if (index == member.Stations.Count - 1) label = "J-TAN";
            else
            {
                int notice = LabelPositionIndex(notices, station.Position, length);
                bool isRigid = LabelPositionIndex(rigid, station.Position, length) >= 0;
                label = notice >= 0 ? (notice + 1).ToString(CultureInfo.InvariantCulture) :
                    index.ToString(CultureInfo.InvariantCulture);
                if (isRigid) label = notice >= 0 ? label + " 剛域" : "剛域";
            }
            labels.Add(station.StationId, label);
        }
        return labels;

    }

    // Label association only; foundation occurrence proof never uses this tolerance.
    internal static int LabelPositionIndex(double[] sorted, double position, double length)
    {
        int match = Array.BinarySearch(sorted, position);
        if (match >= 0) return match;
        int after = ~match;
        double tolerance = 1e-10 * Math.Max(1, length);
        if (after < sorted.Length && Math.Abs(sorted[after] - position) <= tolerance) return after;
        return after > 0 && Math.Abs(sorted[after - 1] - position) <= tolerance ? after - 1 : -1;
    }

    private static IEnumerable<JsonObject> Records(JsonNode? node) => node switch
    {
        JsonArray array => array.OfType<JsonObject>(),
        JsonObject obj => obj.Select(pair => pair.Value).OfType<JsonObject>(),
        _ => [],
    };

    private static string Text(JsonNode? value) => value is JsonValue scalar &&
        scalar.TryGetValue<string>(out string? text) ? text : value?.ToJsonString() ?? "";

    private static double? Number(JsonNode? value) => double.TryParse(Text(value),
        NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && double.IsFinite(number)
        ? number : null;

    private static bool HasOneDisplacementVector(CalculationDerivedCase item, int dimension)
    {
        string[] names = dimension == 2 ? ["dx", "dy", "rz"] : ["dx", "dy", "dz", "rx", "ry", "rz"];
        string[] expected = names.SelectMany(name => new[] { name + "_max", name + "_min" }).ToArray();
        if (expected.Any(mode => !item.Displacements.ContainsKey(mode))) return false;
        var baseline = item.Displacements[expected[0]].ToDictionary(row => (row.EntityId, row.StationId));
        return baseline.Count > 0 && expected.All(mode =>
            item.Displacements[mode].Count == baseline.Count && item.Displacements[mode].All(row =>
                baseline.TryGetValue((row.EntityId, row.StationId), out CalculationDerivedRow? other) &&
                row.Components.Count == other.Components.Count && row.Components.All(component =>
                    other.Components.TryGetValue(component.Key, out double value) && value == component.Value)));
    }
}
