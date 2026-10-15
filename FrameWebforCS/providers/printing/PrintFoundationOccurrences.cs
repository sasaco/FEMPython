using FrameWebforCS.calculation;
using System.Globalization;
using System.Text.Json.Nodes;

namespace FrameWebforCS.providers.printing;

/// <summary>Two report occurrences only for proved continuous explicit foundation joins.</summary>
internal static class PrintFoundationOccurrences
{
    internal static void Apply(JsonObject root, JsonObject saved, PrintSnapshot snapshot)
    {
        var selected = new[]
        {
            (PrintOption.SectionForce, "fsec"),
            (PrintOption.CombinedSectionForce, "fsecCombine"),
            (PrintOption.PickupSectionForce, "fsecPickup"),
        }.Where(item => snapshot.Selection.Options.Contains(item.Item1) && root[item.Item2] is JsonObject).ToArray();
        if (selected.Length == 0) return;
        var audit = new JsonObject { ["boundaries"] = new JsonArray() };
        root["foundationOccurrenceAudit"] = audit;
        CalculationResultPresentation presentation = snapshot.Result!;
        var results = presentation.ResultSet.Results.OfType<StaticAnalysisResult>().ToArray();
        if (presentation.Dimension != 2 || results.Length == 0 ||
            results.Length != presentation.ResultSet.Results.Count ||
            presentation.ResultSet.Cases.Any(c => c.Symbol.Contains("LL", StringComparison.OrdinalIgnoreCase)))
        {
            audit["status"] = "legacy: unsupported dimension, analysis, or moving-load proof";
            return;
        }
        var sheets = new HashSet<string>(StringComparer.Ordinal);
        foreach (StaticAnalysisResult result in results)
        {
            if (saved["load"]?[result.CaseId] is not JsonObject load)
            {
                audit["status"] = "legacy: missing saved case selector";
                return;
            }
            decimal selector = Decimal(load["fix_member"]) ?? 1;
            if (selector == 0) selector = 1; // Same saved-input default as CalculationRequest.
            if (selector <= 0 || selector != decimal.Truncate(selector))
            {
                audit["status"] = "legacy: invalid foundation sheet selector";
                return;
            }
            sheets.Add(selector.ToString(CultureInfo.InvariantCulture));
        }
        var bySheet = new Dictionary<string, Dictionary<string, JsonObject[]>>();
        foreach (string sheet in sheets)
        {
            if (saved["fix_member"]?[sheet] is not JsonArray rows || rows.Any(row => row is not JsonObject))
            {
                audit["status"] = "legacy: missing or malformed active foundation sheet";
                return;
            }
            bySheet[sheet] = rows.OfType<JsonObject>().GroupBy(row => Text(row["m"]))
                .ToDictionary(group => group.Key, group => group.OrderBy(row => Decimal(row["row"]) ?? 0).ToArray());
        }
        var canonical = results.Select(result => result.MemberSectionForces.ToDictionary(member => member.MemberId)).ToArray();
        var proved = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (TopologyMember member in presentation.ResultSet.Topology.Members)
        {
            HashSet<double>? joins = null;
            bool compatible = true;
            foreach (var sheet in bySheet.Values)
            {
                HashSet<double>? boundaries = sheet.TryGetValue(member.MemberId, out var rows)
                    ? ExplicitJoins(rows, member.Stations[^1].Position) : null;
                if (boundaries is null || joins is not null && !joins.SetEquals(boundaries))
                { compatible = false; break; }
                joins = boundaries;
            }
            if (!compatible || joins is null || joins.Count == 0)
            {
                Record(member.MemberId, null, null, "legacy: absent or incompatible explicit interval joins");
                continue;
            }
            var stations = member.Stations.ToDictionary(station => station.Position);
            var sides = canonical.Select(caseMembers => caseMembers.TryGetValue(member.MemberId, out var forces)
                ? forces.Segments.Zip(forces.Segments.Skip(1)).ToDictionary(pair => pair.First.StationJ,
                    pair => (Left: pair.First.JEnd, Right: pair.Second.IEnd,
                        Adjacent: pair.First.StationJ == pair.Second.StationI)) : null).ToArray();
            foreach (double position in joins.OrderBy(value => value))
            {
                if (!stations.TryGetValue(position, out MemberStation? station))
                { Record(member.MemberId, position, null, "legacy: declared join has no exact canonical station"); continue; }
                if (sides.Any(side => side is null || !side.TryGetValue(station.StationId, out var pair) ||
                    !pair.Adjacent || pair.Left != pair.Right))
                { Record(member.MemberId, position, station.StationId, "legacy: missing or unequal canonical sides"); continue; }
                if (!proved.TryGetValue(member.MemberId, out var accepted))
                    proved[member.MemberId] = accepted = new(StringComparer.Ordinal);
                accepted.Add(station.StationId);
                Record(member.MemberId, position, station.StationId, "proved: all static cases have exactly equal six-component sides");
            }
        }
        audit["status"] = proved.Count > 0 ? "proved continuous joins only" : "legacy: no proved joins";
        var notices = saved["notice_points"] is JsonArray noticeRows ? noticeRows.OfType<JsonObject>() : [];
        var noticePositions = notices.GroupBy(row => Text(row["m"]))
            .ToDictionary(group => group.Key, group => group.SelectMany(row => row["Points"] is JsonArray points ? points : new JsonArray())
                .Select(Decimal).Where(position => position.HasValue).Select(position => (double)position!.Value)
                .Distinct().OrderBy(position => position).ToArray());
        var protectedStations = presentation.ResultSet.Topology.Members.Where(member => noticePositions.ContainsKey(member.MemberId))
            .ToDictionary(member => member.MemberId, member => member.Stations.Where(station =>
                PrintResultMetadata.LabelPositionIndex(noticePositions[member.MemberId], station.Position,
                    member.Stations[^1].Position) >= 0).Select(station => station.StationId).ToHashSet());
        foreach (var (_, key) in selected) Expand(root[key]!);

        void Record(string member, double? position, string? station, string reason) =>
            audit["boundaries"]!.AsArray().Add(new JsonObject
            { ["member"] = member, ["position"] = position, ["station"] = station, ["reason"] = reason });

        void Expand(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                foreach (JsonNode child in obj.Select(pair => pair.Value).OfType<JsonNode>().ToArray()) Expand(child);
                return;
            }
            if (node is not JsonArray rows) return;
            var counters = new Dictionary<string, int>();
            JsonNode?[] original = rows.ToArray();
            rows.Clear();
            foreach (JsonNode? value in original)
            {
                rows.Add(value);
                if (value is not JsonObject row) continue;
                string member = Text(row["m"]);
                if (!proved.TryGetValue(member, out var stationIds)) continue;
                bool duplicate = stationIds.Contains(Text(row["n"]));
                Label(row, member);
                if (duplicate)
                {
                    JsonObject copy = row.DeepClone().AsObject();
                    row["occurrence"] = 1; copy["occurrence"] = 2;
                    Label(copy, member);
                    rows.Add(copy);
                }
            }

            void Label(JsonObject row, string member)
            {
                string label = Text(row["stationLabel"]);
                if (label is "I-TAN" or "J-TAN") return;
                int number = counters.GetValueOrDefault(member) + 1;
                counters[member] = number;
                bool notice = protectedStations.TryGetValue(member, out var stations) && stations.Contains(Text(row["n"]));
                if (!notice && int.TryParse(label, NumberStyles.None, CultureInfo.InvariantCulture, out _))
                    row["stationLabel"] = number.ToString(CultureInfo.InvariantCulture);
            }
        }
    }

    private static HashSet<double>? ExplicitJoins(JsonObject[] rows, double length)
    {
        // Decimal cursor preserves declared saved intervals; unmatched canonical stations
        // are skipped explicitly, with no rounded-distance or tolerance association.
        var joins = new HashSet<double>();
        decimal cursor = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            decimal? span = Decimal(rows[i]["length"]);
            if (span is null)
            {
                if (rows[i]["length"] is not null && !string.IsNullOrWhiteSpace(Text(rows[i]["length"]))) return null;
                if (i != rows.Length - 1 || cursor <= 0 || (double)cursor >= length) return null;
                joins.Add((double)cursor);
                return joins;
            }
            if (span <= 0) return null;
            if (i > 0 && (double)cursor < length) joins.Add((double)cursor);
            cursor += span.Value;
            if ((double)cursor > length) return null;
        }
        return joins;
    }

    private static string Text(JsonNode? value) => value is JsonValue scalar &&
        scalar.TryGetValue<string>(out string? text) ? text : value?.ToJsonString() ?? "";

    private static decimal? Decimal(JsonNode? value) => decimal.TryParse(Text(value), NumberStyles.Float,
        CultureInfo.InvariantCulture, out decimal number) ? number : null;
}
