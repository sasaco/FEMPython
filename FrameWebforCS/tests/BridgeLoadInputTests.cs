using System.Text.Json;
using System.Text.Json.Nodes;
using FrameWebforCS.calculation;
using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class BridgeLoadInputTests
{
    internal const string Document = """
        {"dimension":3,"node":{"1":{"x":0,"y":0,"z":0},"2":{"x":4,"y":0,"z":0},"3":{"x":0,"y":4,"z":0}},
         "member":{"1":{"ni":"1","nj":"2","e":"1"},"2":{"ni":"2","nj":"3","e":"1"}},
         "element":{"1":{"1":{"E":200000000,"G":80000000,"A":1,"J":1,"Iy":1,"Iz":1}}},
         "fix_node":{"1":[{"row":1,"n":"1","tx":1,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1}]},
         "load":{"7":{"name":"橋面A"},"9":{"name":"橋面B","load_node":[{"row":1,"n":"3","tz":-2}]}},
         "bridge_loads":{"version":1,
          "panels":[{"id":1,"name":"伝達面","nodes":[1,2,3],"triangles":[[1,2,3]],
             "plane":{"origin":[0,0,0],"axis_u":[1,0,0],"axis_v":[0,1,0]}}],
          "paths":[{"id":1,"name":"線A","points":[[0.125000000000001,0,0],[2,0,0]]},
                   {"id":2,"name":"線B","points":[[0,1,0],[2,1,0]]}],
          "cases":{"7":[{"id":11,"name":"線荷重","panel_id":1,"path_ids":[1],"end_intensities":[[-3,-5]]}],
                   "9":[{"id":11,"panel_id":1,"path_ids":[1,2],"end_intensities":[[-1,-2],[-3,-4]],"direction":{"mode":"normal"}}]}}}
        """;
    internal static void Open(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputDataService.Instance.JsonDataOpen(document.RootElement);
    }
    private static string Save() => JsonSerializer.Serialize(InputDataService.Instance.GetSaveJson());

    [Fact]
    public void RoundTripPreservesDoublePrecisionOrderedPathsSignsAndDistinctCases()
    {
        try
        {
            Open(Document);
            string saved = Save(); Open("{}"); Open(saved);
            var snapshot = InputBridgeLoadService.Instance.GetSnapshot();
            Assert.Empty(snapshot.Errors);
            Assert.Equal(0.125000000000001, snapshot.Paths[0].Points[0].X);
            Assert.Equal(new BridgePoint(2, 0, 0), snapshot.Paths[0].Points[1]);
            Assert.Equal([-3d, -5d], snapshot.Cases["7"][0].EndIntensities[0]);
            Assert.Equal("normal", snapshot.Cases["9"][0].Direction.Mode);
            Assert.Equal(new BridgePoint(0, 0, 1), snapshot.Cases["7"][0].Direction.Vector);
            snapshot.Paths[0].Points[0] = new BridgePoint(999, 999, 999);
            Assert.Equal(0.125000000000001, InputBridgeLoadService.Instance.GetSnapshot().Paths[0].Points[0].X);
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void CalculationKeepsBridgeOnlyCaseAndMixedCaseWithoutDuplicateNodeLoadsOrOtherCasePaths()
    {
        try
        {
            Open(Document);
            using var request = JsonDocument.Parse(InputDataService.Instance.CreateCalculationRequest().Json);
            var cases = request.RootElement.GetProperty("load");
            Assert.Equal(2, cases.EnumerateObject().Count());
            var a = cases.GetProperty("7"); var b = cases.GetProperty("9");
            Assert.False(a.TryGetProperty("load_node", out _));
            Assert.Single(b.GetProperty("load_node").EnumerateArray());
            var sa = a.GetProperty("spatial_loads"); var sb = b.GetProperty("spatial_loads");
            Assert.Single(sa.GetProperty("paths").EnumerateArray());
            Assert.Equal(2, sb.GetProperty("paths").GetArrayLength());
            Assert.False(sa.GetProperty("loads")[0].TryGetProperty("name", out _));
            Assert.False(sa.GetProperty("panels")[0].TryGetProperty("name", out _));
            Assert.False(request.RootElement.TryGetProperty("bridge_loads", out _));
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void BridgeOnlyCaseRemainsEffectiveAfterClearingNameAndChangingOrdinaryRows()
    {
        try
        {
            Open(Document);
            InputLoadService.Instance.LoadNames[6].name = null;
            Assert.Contains("7", InputLoadService.Instance.CaseIds);
            Assert.Equal(9, InputLoadService.Instance.MaximumEffectiveCaseId);
            Assert.True(InputLoadService.Instance.getLoadJson().ContainsKey("7"));
            Open(Save());
            Assert.Single(InputBridgeLoadService.Instance.GetSnapshot().Cases["7"]);
        }
        finally { Open("{}"); }
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("missing_path")]
    [InlineData("plane")]
    [InlineData("unknown")]
    public void InvalidImportRollsBackWholeDocumentAndRevision(string failure)
    {
        try
        {
            Open(Document); string before = Save(); long revision = InputDataService.Instance.CalculationInputRevision;
            var candidate = JsonNode.Parse(Document)!;
            candidate["node"]!["1"]!["x"] = 99;
            var bridge = candidate["bridge_loads"]!;
            if (failure == "duplicate") ((JsonArray)bridge["panels"]!).Add(bridge["panels"]![0]!.DeepClone());
            if (failure == "missing_path") bridge["cases"]!["7"]![0]!["path_ids"]![0] = 99;
            if (failure == "plane") bridge["panels"]![0]!["plane"]!["axis_v"] = new JsonArray(1, 1, 0);
            if (failure == "unknown") bridge["panels"]![0]!["unsupported"] = true;
            Assert.Throws<JsonException>(() => Open(candidate.ToJsonString()));
            Assert.Equal(before, Save()); Assert.Equal(revision, InputDataService.Instance.CalculationInputRevision);
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void IncompleteEditsSurviveSaveReloadAndRejectCalculation()
    {
        try
        {
            Open(Document); var service = InputBridgeLoadService.Instance;
            long revision = InputDataService.Instance.CalculationInputRevision;
            service.Loads[0].P12 = "unfinished";
            Assert.True(InputDataService.Instance.CalculationInputRevision > revision);
            Assert.NotEmpty(service.Errors);
            string saved = Save(); Assert.Contains("drafts", saved);
            Open("{}"); Open(saved);
            Assert.Equal("unfinished", service.Loads[0].P12);
            Assert.Throws<CalculationRequestException>(() => InputDataService.Instance.CreateCalculationRequest());
            service.Loads[0].P12 = "-5";
            Assert.Empty(service.Errors); Assert.DoesNotContain("drafts", Save());
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void DimensionChangeRetainsDataAndRejectsTwoDimensionalCalculation()
    {
        try
        {
            Open(Document); var input = InputDataService.Instance;
            string before = InputBridgeLoadService.Instance.GetSaveJson().ToJsonString();
            input.SetDimension(2);
            Assert.Equal(before, InputBridgeLoadService.Instance.GetSaveJson().ToJsonString());
            Assert.Throws<CalculationRequestException>(() => input.CreateCalculationRequest());
            input.SetDimension(3);
            Assert.Contains("spatial_loads", input.CreateCalculationRequest().Json);
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void UnsupportedSolverRepresentationDoesNotSilentlyDisappearOnImport()
    {
        try
        {
            Open(Document); string before = Save();
            Assert.Throws<JsonException>(() => Open("""{"spatial_loads":{"panels":[],"paths":[],"loads":[]}}"""));
            Assert.Equal(before, Save());
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void LegacyXyPanelsAndSignedLoadsMigrateAndReachCaseLocalSolverInput()
    {
        try
        {
            var document = JsonNode.Parse(Document)!;
            document.AsObject().Remove("bridge_loads");
            document["inf_panel"] = JsonNode.Parse("""{"1":{"name":"旧橋面","nodes":[1,2,3],"triangles":[[1,2,3]]}}""");
            document["line"] = JsonNode.Parse("""{"1":{"name":"旧載荷線","position":[{"x":0,"y":0},{"x":2,"y":0}]}}""");
            document["load"]!["7"]!["inf_panel"] = 1;
            document["load"]!["7"]!["load_inf"] = JsonNode.Parse("""[{"L1":1,"P11":-3,"P12":-5}]""");
            Open(document.ToJsonString());
            var snapshot = InputBridgeLoadService.Instance.GetSnapshot();
            Assert.Equal("旧橋面", Assert.Single(snapshot.Panels).Name);
            Assert.All(Assert.Single(snapshot.Paths).Points, p => Assert.Equal(0, p.Z));
            Assert.Equal([-3d, -5d], snapshot.Cases["7"][0].EndIntensities[0]);
            string saved = Save(); Assert.Contains("bridge_loads", saved); Assert.DoesNotContain("load_inf", saved);
            using var request = JsonDocument.Parse(InputDataService.Instance.CreateCalculationRequest().Json);
            Assert.Equal(-5, request.RootElement.GetProperty("load").GetProperty("7").GetProperty("spatial_loads")
                .GetProperty("loads")[0].GetProperty("end_intensities")[0][1].GetDouble());
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void CaseLocalNormalizedDefinitionIdConflictsAreRemappedWithoutCrossCaseMixing()
    {
        try
        {
            var document = JsonNode.Parse(Document)!;
            using var canonical = JsonDocument.Parse(Document);
            var projected = InputBridgeLoadService.ProjectCases(canonical.RootElement);
            document.AsObject().Remove("bridge_loads");
            document["load"]!["7"]!["spatial_loads"] = projected["7"].DeepClone();
            var second = (JsonObject)projected["9"].DeepClone();
            second["paths"]![0]!["points"]![0]![0] = 0.25;
            document["load"]!["9"]!["spatial_loads"] = second;
            Open(document.ToJsonString());
            var snapshot = InputBridgeLoadService.Instance.GetSnapshot();
            int firstPath = snapshot.Cases["7"][0].PathIds[0];
            int secondPath = snapshot.Cases["9"][0].PathIds[0];
            Assert.NotEqual(firstPath, secondPath);
            Assert.Equal(0.125000000000001, snapshot.Paths.Single(p => p.Id == firstPath).Points[0].X);
            Assert.Equal(0.25, snapshot.Paths.Single(p => p.Id == secondPath).Points[0].X);
            Assert.Empty(snapshot.Errors);
            Assert.Equal(3, snapshot.Paths.Count);
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void ShellReferencedPanelsKeepLegacyShellIdsInCalculationProjection()
    {
        try
        {
            var document = JsonNode.Parse(Document)!;
            document["shell"] = JsonNode.Parse("""{"1":{"e":1,"nodes":[1,2,3]}}""");
            var panel = document["bridge_loads"]!["panels"]![0]!.AsObject();
            panel.Remove("triangles"); panel["elements"] = new JsonArray(1);
            Open(document.ToJsonString());
            using var request = JsonDocument.Parse(InputDataService.Instance.CreateCalculationRequest().Json);
            var value = request.RootElement.GetProperty("load").GetProperty("7").GetProperty("spatial_loads").GetProperty("panels")[0];
            Assert.Equal(1, Assert.Single(value.GetProperty("elements").EnumerateArray()).GetInt32());
            Assert.Empty(value.GetProperty("triangles").EnumerateArray());
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void IndependentLoadingMeshHoleIdsRemainInTheirOwnNamespace()
    {
        try
        {
            var document = JsonNode.Parse(Document)!;
            var panel = document["bridge_loads"]!["panels"]![0]!;
            panel["loading_nodes"] = JsonNode.Parse("""
                [{"id":101,"point":[0.25,0.25,0]},{"id":102,"point":[1.5,0.25,0]},
                 {"id":103,"point":[1.5,1.5,0]},{"id":104,"point":[0.25,1.5,0]},
                 {"id":105,"point":[0.6,0.6,0]},{"id":106,"point":[1,0.6,0]},
                 {"id":107,"point":[1,1,0]},{"id":108,"point":[0.6,1,0]}]
                """);
            panel["loading_triangles"] = JsonNode.Parse("""
                [[101,102,106],[101,106,105],[102,103,107],[102,107,106],
                 [103,104,108],[103,108,107],[104,101,105],[104,105,108]]
                """);
            panel["holes"] = JsonNode.Parse("[[105,108,107,106]]");
            document["bridge_loads"]!["paths"]![0]!["points"] = JsonNode.Parse("[[0.3,0.3,0],[1.3,0.3,0]]");
            document["bridge_loads"]!["paths"]![1]!["points"] = JsonNode.Parse("[[0.3,1.3,0],[1.3,1.3,0]]");
            Open(document.ToJsonString()); Open(Save());
            var snapshot = InputBridgeLoadService.Instance.GetSnapshot();
            Assert.Empty(snapshot.Errors);
            Assert.Equal([105,108,107,106], Assert.Single(Assert.Single(snapshot.Panels).Holes));
            using var request = JsonDocument.Parse(InputDataService.Instance.CreateCalculationRequest().Json);
            var projected = request.RootElement.GetProperty("load").GetProperty("7").GetProperty("spatial_loads").GetProperty("panels")[0];
            Assert.Equal([1,2,3], projected.GetProperty("nodes").EnumerateArray().Select(n => n.GetInt32()));
            Assert.Equal([105,108,107,106], projected.GetProperty("holes")[0].EnumerateArray().Select(n => n.GetInt32()));
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void RootNormalizedShellReferencesMigrateFromUnifiedToPublicIds()
    {
        try
        {
            var document = JsonNode.Parse(Document)!;
            using var canonical = JsonDocument.Parse(Document);
            var spatial = InputBridgeLoadService.ProjectCases(canonical.RootElement)["7"];
            document.AsObject().Remove("bridge_loads");
            document["load"]!.AsObject().Remove("9");
            document["shell"] = JsonNode.Parse("""{"1":{"e":1,"nodes":[1,2,3]}}""");
            // Members 1 and 2 reserve IDs. Public shell 1 is unified element 3.
            var panel = spatial["panels"]![0]!;
            panel["triangles"] = new JsonArray(); panel["elements"] = new JsonArray(3);
            document["spatial_loads"] = spatial;
            Open(document.ToJsonString()); Open(Save());
            Assert.Equal([1], Assert.Single(InputBridgeLoadService.Instance.GetSnapshot().Panels).Elements);
            using var request = JsonDocument.Parse(InputDataService.Instance.CreateCalculationRequest().Json);
            var solverPanel = request.RootElement.GetProperty("load").GetProperty("7").GetProperty("spatial_loads").GetProperty("panels")[0];
            Assert.Equal(1, solverPanel.GetProperty("elements")[0].GetInt32());
        }
        finally { Open("{}"); }
    }
}
