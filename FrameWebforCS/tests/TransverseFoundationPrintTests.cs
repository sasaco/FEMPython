using FrameWebforCS.calculation;
using FrameWebforCS.components.input;
using FrameWebforCS.providers.printing;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class TransverseFoundationPrintTests
{
    [Theory]
    [InlineData((int)PrintOption.SectionForce, "fsec")]
    [InlineData((int)PrintOption.CombinedSectionForce, "fsecCombine")]
    [InlineData((int)PrintOption.PickupSectionForce, "fsecPickup")]
    public void ExplicitContinuousFoundationJoinHasTwoPrintOccurrencesWithFullVectorAndSource(
        int option, string key)
    {
        var (fixture, saved) = Input();
        var presentation = Presentation(fixture, saved);
        string rawExport = PickupExportFormatter.Format(presentation);
        string raw = fixture.ToJsonString();
        JsonObject root = Project(presentation, saved, new([(PrintOption)option]));
        foreach (JsonArray rows in ForceArrays(root[key]!))
        {
            Assert.Equal(6, rows.Count);
            Assert.Equal(new[] { "I-TAN", "1", "2", "3", "4", "J-TAN" },
                rows.Select(row => row!["stationLabel"]!.GetValue<string>()));
            Assert.Equal("S2", rows[2]!["n"]!.GetValue<string>());
            Assert.Equal("S2", rows[3]!["n"]!.GetValue<string>());
            Assert.Equal(1, rows[2]!["occurrence"]!.GetValue<int>());
            Assert.Equal(2, rows[3]!["occurrence"]!.GetValue<int>());
            JsonObject first = rows[2]!.DeepClone().AsObject();
            JsonObject second = rows[3]!.DeepClone().AsObject();
            first.Remove("stationLabel"); second.Remove("stationLabel");
            first.Remove("occurrence"); second.Remove("occurrence");
            Assert.True(JsonNode.DeepEquals(first, second));
        }
        Assert.Equal(raw, fixture.ToJsonString());
        Assert.Equal(rawExport, PickupExportFormatter.Format(presentation));
        Assert.Equal(4, ((ForceAnalysisResult)presentation.Pages[0].Result)
            .MemberSectionForces[0].Segments.Count);
    }

    [Fact]
    public void TerminalBlankFoundationIntervalRetainsItsExplicitInteriorJoin()
    {
        var (fixture, saved) = Input();
        foreach (string sheet in new[] { "1", "2" }) saved["fix_member"]![sheet]![1]!["length"] = null;
        JsonObject root = Project(Presentation(fixture, saved), saved, new([PrintOption.SectionForce]));
        Assert.All(ForceArrays(root["fsec"]!), rows => Assert.Equal(6, rows.Count));
    }

    [Theory]
    [InlineData("fx")]
    [InlineData("fy")]
    [InlineData("fz")]
    [InlineData("mx")]
    [InlineData("my")]
    [InlineData("mz")]
    public void AnyCanonicalSideJumpInAnyContributingCaseKeepsLegacyUniqueRows(string component)
    {
        var (fixture, saved) = Input();
        JsonNode right = fixture["results"]![1]!["member_section_forces"]![0]!["segments"]![2]!["i_end"]!;
        right[component] = right[component]!.GetValue<int>() + 1;
        var presentation = Presentation(fixture, saved);
        string export = PickupExportFormatter.Format(presentation);
        JsonObject root = Project(presentation, saved, new(
            [PrintOption.SectionForce, PrintOption.CombinedSectionForce, PrintOption.PickupSectionForce],
            CaseIds: ["1"], SectionForceComponents: ["mz_max"]));
        foreach (string key in new[] { "fsec", "fsecCombine", "fsecPickup" })
            Assert.All(ForceArrays(root[key]!), rows => Assert.Equal(5, rows.Count));
        Assert.Contains("unequal canonical sides", root["foundationOccurrenceAudit"]!.ToJsonString());
        Assert.Equal(export, PickupExportFormatter.Format(presentation));
    }

    [Theory]
    [InlineData("missing-sheet")]
    [InlineData("different-joins")]
    [InlineData("unmatched-position")]
    [InlineData("unknown-case")]
    [InlineData("malformed-length")]
    public void MissingOrIncompatibleGeometryKeepsUniqueRowsWithDiagnostic(string scenario)
    {
        var (fixture, saved) = Input();
        if (scenario == "missing-sheet") saved["fix_member"]!.AsObject().Remove("2");
        if (scenario == "different-joins")
        {
            saved["fix_member"]!["2"]![0]!["length"] = 0.2;
            saved["fix_member"]!["2"]![1]!["length"] = 0.8;
        }
        if (scenario == "unmatched-position")
            foreach (string sheet in new[] { "1", "2" })
            {
                saved["fix_member"]![sheet]![0]!["length"] = 0.400001;
                saved["fix_member"]![sheet]![1]!["length"] = 0.599999;
            }
        if (scenario == "unknown-case") saved["load"]!.AsObject().Remove("2");
        if (scenario == "malformed-length")
            foreach (string sheet in new[] { "1", "2" }) saved["fix_member"]![sheet]![1]!["length"] = "bad";
        JsonObject root = Project(Presentation(fixture, saved), saved, new([PrintOption.SectionForce]));
        Assert.All(ForceArrays(root["fsec"]!), rows => Assert.Equal(5, rows.Count));
        Assert.Contains("legacy", root["foundationOccurrenceAudit"]!.ToJsonString());
        if (scenario == "unmatched-position")
            Assert.Contains("no exact canonical station", root["foundationOccurrenceAudit"]!.ToJsonString());
    }

    [Theory]
    [InlineData(0.7)]
    [InlineData(0.70000000001)]
    public void ComponentAndMemberFiltersKeepOccurrencesAndNoticeRigidLabelsRemainProtected(double notice)
    {
        var (fixture, saved) = Input();
        saved["notice_points"] = JsonNode.Parse("[{\"m\":\"M1\",\"Points\":[0.7]}]");
        saved["notice_points"]![0]!["Points"]![0] = notice;
        saved["rigid"] = JsonNode.Parse("[{\"m\":\"M1\",\"Ilength\":0.2,\"Jlength\":0}]");
        JsonObject root = Project(Presentation(fixture, saved), saved, new([PrintOption.PickupSectionForce],
            MemberIds: ["M1"], SectionForceComponents: ["mz_min"]));
        Assert.Single(root["fsecPickup"]!["1"]!.AsObject());
        JsonArray rows = root["fsecPickup"]!["1"]!["mz_min"]!.AsArray();
        Assert.Equal(6, rows.Count);
        Assert.Equal("剛域", rows[1]!["stationLabel"]!.GetValue<string>());
        Assert.Equal("1", rows[4]!["stationLabel"]!.GetValue<string>());
        Assert.Equal("S2", rows[2]!["n"]!.GetValue<string>());
        Assert.Equal("S2", rows[3]!["n"]!.GetValue<string>());
    }

    [Fact]
    public void DiagramOnlyAndMovingAndThreeDimensionalRoutesKeepUniqueRows()
    {
        var (fixture, saved) = Input();
        var presentation = Presentation(fixture, saved);
        JsonObject diagram = Project(presentation, saved, new([PrintOption.SectionDiagram]));
        Assert.All(ForceArrays(diagram["fsec"]!), rows => Assert.Equal(5, rows.Count));
        Assert.Null(diagram["foundationOccurrenceAudit"]);
        JsonObject mixed = Project(presentation, saved, new([PrintOption.SectionDiagram, PrintOption.SectionForce]));
        Assert.True(JsonNode.DeepEquals(diagram["PrintDiagram"], mixed["PrintDiagram"]));
        var withoutDerived = new CalculationResultPresentation(presentation.ResultSet, dimension: 2);
        JsonObject baseOnly = Project(withoutDerived, saved, new([PrintOption.SectionForce]));
        Assert.All(ForceArrays(baseOnly["fsec"]!), rows => Assert.Equal(6, rows.Count));
        saved["dimension"] = 3;
        JsonObject three = Project(Presentation(fixture, saved, 3), saved, new([PrintOption.SectionForce]));
        Assert.All(ForceArrays(three["fsec"]!), rows => Assert.Equal(5, rows.Count));
        saved["dimension"] = 2;
        fixture["cases"]![0]!["symbol"] = "LL";
        JsonObject moving = Project(Presentation(fixture, saved), saved, new([PrintOption.PickupSectionForce]));
        Assert.All(ForceArrays(moving["fsecPickup"]!), rows => Assert.Equal(5, rows.Count));
        JsonNode childCase = fixture["cases"]![0]!.DeepClone();
        JsonNode childResult = fixture["results"]![0]!.DeepClone();
        childCase["case_id"] = "1.1"; childResult["case_id"] = "1.1";
        fixture["cases"]!.AsArray().Add(childCase);
        fixture["results"]!.AsArray().Add(childResult);
        JsonObject movingBase = Project(Presentation(fixture, saved), saved,
            new([PrintOption.SectionForce], SectionForceComponents: ["mz_max"]));
        Assert.All(ForceArrays(movingBase["fsec"]!), rows => Assert.Equal(5, rows.Count));
    }

    [Theory]
    [InlineData("modal")]
    [InlineData("nonlinear-steps")]
    public void UnsupportedCanonicalFamiliesKeepTheExistingForcePayload(string name)
    {
        var (_, saved) = Input();
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        var presentation = new CalculationResultPresentation(AnalysisResultSetJson.Deserialize(
            File.ReadAllText(Path.Combine(directory!.FullName, "FrameWeb", "tests", "data", "contracts", "positive", name + ".json"))),
            dimension: 2);
        JsonObject root = JsonNode.Parse("{\"fsec\":{\"case\":[{\"m\":\"M1\",\"n\":\"S2\",\"l\":0.4}]}}")!.AsObject();
        string before = root["fsec"]!.ToJsonString();
        PrintFoundationOccurrences.Apply(root, saved, new PrintSnapshot(
            new([PrintOption.SectionForce]), saved.ToJsonString(), 1, 1, presentation, null, []));
        Assert.Equal(before, root["fsec"]!.ToJsonString());
        Assert.Contains("unsupported", root["foundationOccurrenceAudit"]!["status"]!.GetValue<string>());
    }

    private static IEnumerable<JsonArray> ForceArrays(JsonNode node)
    {
        if (node is JsonArray rows) { yield return rows; yield break; }
        if (node is JsonObject obj)
            foreach (JsonNode child in obj.Select(pair => pair.Value).OfType<JsonNode>())
                foreach (JsonArray found in ForceArrays(child)) yield return found;
    }

    private static JsonObject Project(CalculationResultPresentation presentation, JsonObject saved,
        PrintSelection selection) => JsonNode.Parse(PrintProjection.Build(new PrintSnapshot(
            selection, saved.ToJsonString(), 1, 1, presentation, presentation.Derived, [])))!.AsObject();

    private static CalculationResultPresentation Presentation(JsonObject fixture, JsonObject saved, int dimension = 2)
    {
        AnalysisResultSet result = AnalysisResultSetJson.Deserialize(fixture.ToJsonString());
        using JsonDocument doc = JsonDocument.Parse(saved.ToJsonString());
        var parsed = InputCombineService.ParseCombineJson(doc.RootElement);
        return new(result, CalculationDerivedPresenter.Build(result, dimension,
            CalculationDerivedInputSnapshot.Capture(parsed.Define, parsed.Combine, parsed.Pickup)), dimension);
    }

    private static (JsonObject Fixture, JsonObject Saved) Input()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        JsonObject fixture = JsonNode.Parse(File.ReadAllText(Path.Combine(directory!.FullName,
            "FrameWeb", "tests", "data", "contracts", "positive", "single-static.json")))!.AsObject();
        fixture["cases"]![0]!["case_id"] = "1";
        fixture["results"]![0]!["case_id"] = "1";
        JsonNode original = fixture["results"]![0]!["member_section_forces"]![0]!["segments"]![0]!;
        double[] locations = [0, 0.2, 0.4, 0.7, 1];
        var stations = new JsonArray();
        var segments = new JsonArray();
        for (int i = 0; i < locations.Length; i++)
        {
            stations.Add(new JsonObject { ["station_id"] = "S" + i, ["position"] = locations[i] });
            if (i > 0 && i < locations.Length - 1)
            {
                // The real topology producer also exposes a solved node at every
                // interior station; diagram interpolation needs those vectors.
                string nodeId = "foundation-station-" + i;
                fixture["topology"]!["nodes"]!.AsArray().Add(new JsonObject
                {
                    ["node_id"] = nodeId, ["source_node_id"] = null, ["generated"] = true,
                    ["coordinates"] = new JsonObject { ["x"] = locations[i], ["y"] = 0, ["z"] = 0 },
                });
                JsonNode displacement = fixture["results"]![0]!["node_displacements"]![1]!.DeepClone();
                displacement["node_id"] = nodeId;
                foreach (string component in new[] { "dx", "dy", "dz", "rx", "ry", "rz" })
                {
                    double start = fixture["results"]![0]!["node_displacements"]![0]!["components"]![component]!.GetValue<double>();
                    double end = displacement["components"]![component]!.GetValue<double>();
                    displacement["components"]![component] = start + (end - start) * locations[i];
                }
                fixture["results"]![0]!["node_displacements"]!.AsArray().Add(displacement);
            }
            if (i == 0) continue;
            JsonNode segment = original.DeepClone();
            segment["segment_id"] = $"S{i - 1}-S{i}";
            segment["station_i"] = "S" + (i - 1); segment["station_j"] = "S" + i;
            segment["length"] = locations[i] - locations[i - 1];
            foreach (string end in new[] { "i_end", "j_end" })
                foreach (var (name, value) in new[] { ("fx", 10), ("fy", 20), ("fz", 30), ("mx", 40), ("my", 50), ("mz", 60) })
                    segment[end]![name] = value;
            segments.Add(segment);
        }
        fixture["topology"]!["members"]![0]!["stations"] = stations;
        fixture["results"]![0]!["member_section_forces"]![0]!["segments"] = segments;
        JsonNode secondCase = fixture["cases"]![0]!.DeepClone();
        JsonNode secondResult = fixture["results"]![0]!.DeepClone();
        secondCase["case_id"] = "2"; secondResult["case_id"] = "2";
        fixture["cases"]!.AsArray().Add(secondCase);
        fixture["results"]!.AsArray().Add(secondResult);
        JsonObject saved = JsonNode.Parse("""
            {"dimension":2,"node":{},"member":{"M1":{"ni":"1","nj":"2"}},"shell":{},"element":{},
             "load":{"1":{"fix_member":1},"2":{"fix_member":2}},"notice_points":[],"rigid":[],
             "fix_member":{"1":[{"row":1,"m":"M1","length":0.4,"ty":100},{"row":2,"m":"M1","length":0.6,"ty":100}],
                           "2":[{"row":1,"m":"M1","length":0.4,"ty":200},{"row":2,"m":"M1","length":0.6,"ty":200}]},
             "define":{"1":{"row":1,"C1":1},"2":{"row":2,"C1":2}},
             "combine":{"1":{"row":1,"C1":1,"C2":2}},"pickup":{"1":{"row":1,"C1":1}}}
            """)!.AsObject();
        return (fixture, saved);
    }
}
