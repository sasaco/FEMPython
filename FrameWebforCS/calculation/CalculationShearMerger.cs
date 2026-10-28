using System.Text.Json.Nodes;

namespace FrameWebforCS.calculation;

/// <summary>
/// Folds FrameG M/S companion analyses back into their load cases: the shear (fy) of the members an
/// S row loads comes from the companion; every other value stays from the case. The companion case
/// leaves the result set, so results, prints and exports see only the saved load cases.
/// </summary>
internal static class CalculationShearMerger
{
    internal static string Merge(string resultSetJson, IReadOnlyDictionary<string, ShearCompanion> companions)
    {
        if (companions.Count == 0) return resultSetJson;
        JsonObject root = JsonNode.Parse(resultSetJson)?.AsObject() ??
            throw new InvalidOperationException("The result set is empty.");
        JsonArray cases = root["cases"]?.AsArray() ?? throw new InvalidOperationException("The result set has no cases.");
        JsonArray results = root["results"]?.AsArray() ?? throw new InvalidOperationException("The result set has no results.");
        foreach (var (caseId, companion) in companions)
        {
            JsonObject target = Result(results, caseId);
            JsonObject source = Result(results, companion.CaseId);
            var members = companion.MemberIds.ToHashSet(StringComparer.Ordinal);
            var shears = Members(source).Where(member => members.Contains(Id(member, "member_id")))
                .ToDictionary(member => Id(member, "member_id"), StringComparer.Ordinal);
            foreach (JsonObject member in Members(target).Where(member => members.Contains(Id(member, "member_id"))))
            {
                string memberId = Id(member, "member_id");
                if (!shears.TryGetValue(memberId, out JsonObject? shear))
                    throw new InvalidOperationException($"M/S: case {companion.CaseId} has no member {memberId}.");
                var segments = Segments(shear).ToDictionary(segment => Id(segment, "segment_id"), StringComparer.Ordinal);
                foreach (JsonObject segment in Segments(member))
                {
                    string segmentId = Id(segment, "segment_id");
                    if (!segments.TryGetValue(segmentId, out JsonObject? from))
                        throw new InvalidOperationException($"M/S: member {memberId} has no segment {segmentId} in case {companion.CaseId}.");
                    foreach (string end in new[] { "i_end", "j_end" })
                        segment[end]!["fy"] = from[end]!["fy"]!.DeepClone();
                }
            }
            results.Remove(source);
            cases.Remove(cases.OfType<JsonObject>().Single(item => Id(item, "case_id") == companion.CaseId));
        }
        return root.ToJsonString();
    }

    private static JsonObject Result(JsonArray results, string caseId) =>
        results.OfType<JsonObject>().SingleOrDefault(item => Id(item, "case_id") == caseId) ??
        throw new InvalidOperationException($"M/S: the result set has no case {caseId}.");

    private static IEnumerable<JsonObject> Members(JsonObject result) =>
        result["member_section_forces"]?.AsArray().OfType<JsonObject>() ?? [];

    private static IEnumerable<JsonObject> Segments(JsonObject member) =>
        member["segments"]?.AsArray().OfType<JsonObject>() ?? [];

    private static string Id(JsonObject item, string key) => item[key]?.GetValue<string>() ?? "";
}
