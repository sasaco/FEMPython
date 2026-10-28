using FrameWebforCS.calculation;
using FrameWebforCS.components.input;
using FrameWebforCS.providers.printing;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class TransversePrintMetadataTests
{
    [Theory]
    [InlineData("singleton", true)]
    [InlineData("multiple", false)]
    [InlineData("moving", false)]
    [InlineData("fallback", true)]
    public void OrdinaryMarkerRequiresSemanticSingletonSourcesBeforeDisplayFiltering(string scenario, bool ordinary)
    {
        JsonObject fixture = Fixture();
        JsonObject saved = Saved();
        saved["define"] = JsonNode.Parse(scenario == "fallback" ? "{}" :
            scenario == "multiple" ? "{\"1\":{\"row\":1,\"C1\":1,\"C2\":2}}" :
            "{\"1\":{\"row\":1,\"C1\":1}}");
        if (scenario == "moving")
        {
            fixture["cases"]![0]!["symbol"] = "LL";
            AddCase(fixture, "1.1", "LL", 0.001);
        }
        else AddCase(fixture, "2", "D2", 0.002);
        CalculationResultPresentation presentation = Presentation(fixture, saved);
        JsonObject root = Project(presentation, saved, new([PrintOption.CombinedDisplacement],
            DisplacementComponents: ["dy_min"]));
        Assert.NotNull(root["ordinaryDisplacementCombinations"]);
        string[] markers = root["ordinaryDisplacementCombinations"]!.AsArray()
            .Select(x => x!.GetValue<string>()).ToArray();
        Assert.Equal(ordinary, markers.Contains("1"));
        Assert.True(root["referenceResultLayout"]!.GetValue<bool>());
        Assert.Single(root["disgCombine"]!["1"]!.AsObject());
    }

    [Fact]
    public void MissingSavedCombinationProofDoesNotMarkSyntheticDerivedRowsOrdinary()
    {
        JsonObject fixture = Fixture();
        JsonObject saved = Saved();
        CalculationResultPresentation presentation = Presentation(fixture, saved);
        saved["combine"] = new JsonObject();
        JsonObject root = Project(presentation, saved, new([PrintOption.CombinedDisplacement]));
        Assert.NotNull(root["ordinaryDisplacementCombinations"]);
        Assert.Empty(root["ordinaryDisplacementCombinations"]!.AsArray());
    }

    [Fact]
    public void ForceLabelsRetainExactStationIdentityAndSavedMemberAndCaseNames()
    {
        JsonObject fixture = Fixture();
        JsonObject saved = Saved();
        saved["notice_points"] = JsonNode.Parse("[{\"m\":\"M1\",\"Points\":[0.6]}]");
        saved["rigid"] = JsonNode.Parse("[{\"m\":\"M1\",\"Ilength\":0.25,\"Jlength\":0}]");
        var stations = new JsonArray();
        var segments = new JsonArray();
        JsonNode original = fixture["results"]![0]!["member_section_forces"]![0]!["segments"]![0]!;
        double[] locations = [0, 0.25, 0.6, 0.6000001, 1];
        for (int i = 0; i < locations.Length; i++)
        {
            stations.Add(new JsonObject { ["station_id"] = "S" + i, ["position"] = locations[i] });
            if (i == 0) continue;
            JsonNode segment = original.DeepClone();
            segment["segment_id"] = $"S{i - 1}-S{i}";
            segment["station_i"] = "S" + (i - 1);
            segment["station_j"] = "S" + i;
            segment["length"] = locations[i] - locations[i - 1];
            segments.Add(segment);
        }
        fixture["topology"]!["members"]![0]!["stations"] = stations;
        fixture["results"]![0]!["member_section_forces"]![0]!["segments"] = segments;
        JsonObject root = Project(Presentation(fixture, saved), saved, new(
            [PrintOption.SectionForce, PrintOption.CombinedSectionForce, PrintOption.PickupSectionForce]));
        JsonArray rows = root["fsec"]!.AsObject().Single().Value!.AsArray();
        Assert.NotNull(rows[0]!["stationLabel"]);
        Assert.Equal(new[] { "I-TAN", "剛域", "1", "3", "J-TAN" },
            rows.Select(row => row!["stationLabel"]!.GetValue<string>()));
        Assert.Equal(new[] { "S0", "S1", "S2", "S3", "S4" },
            rows.Select(row => row!["n"]!.GetValue<string>()));
        // FrameG's member heading (原宿ボックス 0404): "部 材", the ID in two columns, spaced node pair.
        Assert.All(rows, row => Assert.Equal("部 材 M1 ( 1 - 2 ) Beam", row!["memberCaption"]!.GetValue<string>()));
        Assert.Equal("Dead load", root["fsecName"]![0]![1]!.GetValue<string>());
        Assert.Equal("1/Static/0", root["fsecName"]![0]![0]!.GetValue<string>());
        Assert.Equal("剛域", root["fsecPickup"]!["1"]!["mz_max"]![1]!["stationLabel"]!.GetValue<string>());
        Assert.Equal("1", root["fsecCombine"]!["1"]!["mz_max"]![2]!["stationLabel"]!.GetValue<string>());
    }

    [Fact]
    public void MemberHeadingFallsBackToTheSheetOneElementName()
    {
        // FrameG keeps the member name on its section (原宿ボックス 0404: 上床版, 剛域 …).
        JsonObject saved = Saved();
        saved["member"] = JsonNode.Parse("""{"M1":{"ni":"1","nj":"2","e":"7"}}""");
        saved["element"] = JsonNode.Parse("""{"1":{"7":{"E":1,"A":1,"Iz":1,"n":"上床版"}}}""");
        JsonObject root = Project(Presentation(Fixture(), saved), saved, new([PrintOption.SectionForce]));
        Assert.All(root["fsec"]!.AsObject().Single().Value!.AsArray(),
            row => Assert.Equal("部 材 M1 ( 1 - 2 ) 上床版", row!["memberCaption"]!.GetValue<string>()));
    }

    [Fact]
    public void ThreeDimensionalAndInputOnlyProjectionKeepLegacyComposition()
    {
        JsonObject fixture = Fixture();
        JsonObject saved = Saved();
        saved["dimension"] = 3;
        JsonObject root = Project(Presentation(fixture, saved, 3), saved, new([PrintOption.SectionForce]));
        Assert.Null(root["referenceResultLayout"]);
        JsonObject input = Project(Presentation(fixture, saved, 3), saved, new([PrintOption.Input]));
        Assert.Null(input["referenceResultLayout"]);
    }

    [Fact]
    public void TwoDimensionalInputOnlyProjectionWithoutResultsUsesReferencePortraitStyle()
    {
        JsonObject saved = Saved();
        var selection = new PrintSelection([PrintOption.Input],
            Orientation: PrintOrientation.Vertical, Paper: PrintPaper.A4);
        JsonObject root = JsonNode.Parse(PrintProjection.Build(new PrintSnapshot(
            selection, saved.ToJsonString(), 1, 1, null, null, [])))!.AsObject();

        Assert.True(root["referenceResultLayout"]!.GetValue<bool>());
        Assert.False(root["hasPrintCalculation"]!.GetValue<bool>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WeightedProvenanceUsesSelectedBaseCasesWithoutLosingDefineSigns(bool pickup)
    {
        JsonObject fixture = Fixture();
        AddCase(fixture, "2", "D2", 0.002);
        JsonObject saved = Saved();
        saved["define"] = JsonNode.Parse("{\"1\":{\"row\":1,\"C1\":1},\"2\":{\"row\":2,\"C1\":-2}}");
        saved["combine"] = JsonNode.Parse("{\"1\":{\"row\":1,\"C1\":1.1,\"C2\":2.5}}");
        var presentation = Presentation(fixture, saved);
        string raw = presentation.Derived!.Combines[0].Displacements["dx_max"][0].Provenance;
        JsonObject root = Project(presentation, saved, new(
            [pickup ? PrintOption.PickupDisplacement : PrintOption.CombinedDisplacement],
            DisplacementComponents: ["dx_max"]));
        JsonNode row = root[pickup ? "disgPickup" : "disgCombine"]!["1"]!["dx_max"]!["1"]!;
        Assert.Equal("1.1*1 - 2.5*2", row["comb"]!.GetValue<string>());
        Assert.Equal(pickup ? "1" : "1,-2", row["case"]!.GetValue<string>());
        Assert.Equal(raw, row["rawProvenance"]!.GetValue<string>());
        Assert.Equal(raw, presentation.Derived.Combines[0].Displacements["dx_max"][0].Provenance);
    }

    [Fact]
    public void EnvelopeProvenanceUsesEachSelectedRawModeAndLeavesAmbiguityExplicit()
    {
        JsonObject fixture = Fixture();
        AddCase(fixture, "2", "D2", 0.002);
        JsonObject saved = Saved();
        saved["define"] = JsonNode.Parse("{\"1\":{\"row\":1,\"C1\":1,\"C2\":2}}");
        saved["combine"]!["1"]!["C1"] = 2;
        var presentation = Presentation(fixture, saved);
        JsonObject root = Project(presentation, saved, new([PrintOption.CombinedDisplacement],
            DisplacementComponents: ["dx_max", "dx_min"]));
        Assert.Equal("2*2", root["disgCombine"]!["1"]!["dx_max"]!["2"]!["comb"]!.GetValue<string>());
        Assert.Equal("2*1", root["disgCombine"]!["1"]!["dx_min"]!["2"]!["comb"]!.GetValue<string>());
        Assert.Empty(root["ordinaryDisplacementCombinations"]!.AsArray());
        saved["combine"] = new JsonObject();
        JsonObject ambiguous = Project(presentation, saved, new([PrintOption.CombinedDisplacement]));
        JsonNode row = ambiguous["disgCombine"]!["1"]!["dx_max"]!["2"]!;
        Assert.Equal("2*D1", row["comb"]!.GetValue<string>());
        Assert.Equal("2*D1", row["rawProvenance"]!.GetValue<string>());
    }

    private static JsonObject Project(CalculationResultPresentation presentation, JsonObject saved,
        PrintSelection selection) => JsonNode.Parse(PrintProjection.Build(new PrintSnapshot(
            selection, saved.ToJsonString(), 1, 1, presentation, presentation.Derived, [])))!.AsObject();

    private static CalculationResultPresentation Presentation(JsonObject fixture, JsonObject saved, int dimension = 2)
    {
        AnalysisResultSet result = AnalysisResultSetJson.Deserialize(fixture.ToJsonString());
        using JsonDocument doc = JsonDocument.Parse(saved.ToJsonString());
        var parsed = InputCombineService.ParseCombineJson(doc.RootElement);
        var derived = CalculationDerivedPresenter.Build(result, dimension,
            CalculationDerivedInputSnapshot.Capture(parsed.Define, parsed.Combine, parsed.Pickup));
        return new(result, derived, dimension);
    }

    private static JsonObject Saved() => JsonNode.Parse("""
        {"dimension":2,"node":{},"member":{"M1":{"ni":"1","nj":"2","name":"Beam"}},
         "shell":{},"element":{},"load":{"1":{"name":"Dead load"}},
         "notice_points":[],"rigid":[],"define":{"1":{"row":1,"C1":1}},
         "combine":{"1":{"row":1,"name":"Combination One","C1":1}},
         "pickup":{"1":{"row":1,"C1":1}}}
        """)!.AsObject();

    private static JsonObject Fixture()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        JsonObject root = JsonNode.Parse(File.ReadAllText(Path.Combine(directory!.FullName,
            "FrameWeb", "tests", "data", "contracts", "positive", "single-static.json")))!.AsObject();
        root["cases"]![0]!["case_id"] = "1";
        root["results"]![0]!["case_id"] = "1";
        return root;
    }

    private static void AddCase(JsonObject fixture, string id, string symbol, double dx)
    {
        JsonNode c = fixture["cases"]![0]!.DeepClone();
        JsonNode r = fixture["results"]![0]!.DeepClone();
        c["case_id"] = id;
        c["symbol"] = symbol;
        r["case_id"] = id;
        r["node_displacements"]![1]!["components"]!["dx"] = dx;
        fixture["cases"]!.AsArray().Add(c);
        fixture["results"]!.AsArray().Add(r);
    }
}
