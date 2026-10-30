using FrameWebforCS.calculation;
using FrameWebforCS.providers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

// The desktop and the Headless runner both send nonlinear models through CalculationRequestBuilder.
[Collection("DisplacementSingletons")]
public sealed class NonlinearCalculationRequestTests
{
    private static JsonObject P7() =>
        JsonNode.Parse(File.ReadAllText(NonlinearInputPersistenceTests.P7Path()))!.AsObject();

    private static JsonObject Project(JsonObject saved) =>
        JsonNode.Parse(CalculationRequestBuilder.FromSavedJson(saved.ToJsonString()).Json)!.AsObject();

    [Fact]
    public void RawP7FileForwardsEveryNonlinearInput() => AssertForwarded(P7(), Project(P7()));

    [Fact]
    public void DesktopSnapshotOfP7ForwardsEveryNonlinearInput()
    {
        JsonObject source = P7();
        try
        {
            using (var document = JsonDocument.Parse(source.ToJsonString()))
                InputDataService.Instance.JsonDataOpen(document.RootElement);
            AssertForwarded(source, JsonNode.Parse(InputDataService.Instance.CreateCalculationRequest().Json)!.AsObject());
        }
        finally
        {
            using var empty = JsonDocument.Parse("{}");
            InputDataService.Instance.JsonDataOpen(empty.RootElement);
        }
    }

    private static void AssertForwarded(JsonObject source, JsonObject request)
    {
        Assert.True(request["nonlinear"]!.GetValue<bool>());
        Assert.True(JsonNode.DeepEquals(source["nonlinear_combine"], request["nonlinear_combine"]));
        foreach (var (id, sourceCase) in source["load"]!.AsObject())
        {
            Assert.Equal(sourceCase!["kind"]!.GetValue<string>(), request["load"]![id]!["kind"]!.GetValue<string>());
            Assert.True(JsonNode.DeepEquals(sourceCase["limit"], request["load"]![id]!["limit"]));
            // The P7 model writes member loads in the engine's single-member form ("m").
            var sourceMembers = (sourceCase["load_member"] as JsonArray ?? new JsonArray()).OfType<JsonObject>()
                .Select(row => (row["m"]!.ToString(), row["direction"]!.ToString(), row["P1"]!.GetValue<double>()));
            var requestMembers = (request["load"]![id]!["load_member"] as JsonArray ?? new JsonArray()).OfType<JsonObject>()
                .Select(row => (row["m"]!.ToString(), row["direction"]!.ToString(), row["P1"]!.GetValue<double>()));
            Assert.Equal(sourceMembers, requestMembers);
        }
        Assert.True(JsonNode.DeepEquals(source["element"]!["1"]!["2"]!["nonlinear"],
            request["element"]!["1"]!["2"]!["nonlinear"]));
        // The nonlinear bar's shear stiffness is G·A: without an entered G the engine derives it.
        foreach (var (_, element) in request["element"]!["1"]!.AsObject())
            Assert.False(element!.AsObject().ContainsKey("G"));
        JsonObject support = request["fix_node"]!["1"]!.AsArray().OfType<JsonObject>()
            .Single(row => row["n"]!.ToString() == "9");
        Assert.True(JsonNode.DeepEquals(source["fix_node"]!["1"]![0]!["rz"], support["rz"]));
    }

    [Fact]
    public void LinearRequestCarriesNoNonlinearKeys()
    {
        JsonObject request = Project(JsonNode.Parse("""
            {"dimension":2,"node":{"1":{"x":0,"y":0},"2":{"x":1,"y":0}},
             "member":{"1":{"ni":"1","nj":"2","e":"1"}},
             "element":{"1":{"1":{"E":10000,"A":1,"Iz":1}}},
             "fix_node":{"1":[{"n":"1","tx":1,"ty":1,"rz":1}]},
             "load":{"1":{"symbol":"DL","fix_node":1,"element":1,"load_node":[{"row":1,"n":"2","tx":4}]}}}
            """)!.AsObject());
        Assert.False(request.ContainsKey("nonlinear"));
        Assert.False(request.ContainsKey("nonlinear_combine"));
        Assert.False(request["load"]!["1"]!.AsObject().ContainsKey("kind"));
    }

    [Theory]
    [InlineData("rows", "非線形解析の組合せがありません")]
    [InlineData("limit", "限定倍率")]
    [InlineData("missing", "基本ケース 99 に荷重がありません")]
    [InlineData("sheet", "支点の番号が違います")]
    [InlineData("3d", "2Dのモデルだけ")]
    public void InvalidNonlinearModelsAreRejectedBeforeTheEngine(string defect, string message)
    {
        JsonObject saved = P7();
        switch (defect)
        {
            case "rows": saved["nonlinear_combine"] = new JsonObject(); break;
            case "limit": saved["load"]!["6"]!.AsObject().Remove("limit"); break;
            case "missing": saved["nonlinear_combine"]!["1"]!["C99"] = 1; break;
            case "sheet":
                saved["fix_node"]!["2"] = saved["fix_node"]!["1"]!.DeepClone();
                saved["load"]!["7"]!["fix_node"] = 2;
                break;
            case "3d": saved["dimension"] = 3; break;
        }
        var error = Assert.Throws<CalculationRequestException>(() => Project(saved));
        Assert.Contains(message, error.Message);
    }
}

[Collection("Python runtime")]
public sealed class NonlinearCalculationEngineTests
{
    internal static string RepositoryRoot() =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(NonlinearInputPersistenceTests.P7Path())!,
            "..", "..", "..", "..", ".."));

    internal static double PeakBaseShear(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("results").EnumerateArray().Max(state => Math.Abs(
            state.GetProperty("support_reactions").EnumerateArray()
                .Single(row => row.GetProperty("node_id").GetString() == "9")
                .GetProperty("components").GetProperty("fx").GetDouble()));
    }

    // README of JRSNAP検証: the peak base shear of 線路方向 Case1 is 10701.4 kN in FrameWeb.
    [Fact]
    public async Task P7RequestSolvesToTheRecordedPeakBaseShear()
    {
        if (await IsolatedPythonTest.RanInChildAsync(typeof(NonlinearCalculationEngineTests), TimeSpan.FromMinutes(5))) return;
        var request = CalculationRequestBuilder.FromSavedJson(File.ReadAllText(NonlinearInputPersistenceTests.P7Path()));
        await using var runtime = new PythonCalculationRuntime(RepositoryRoot());
        string json = await runtime.CalculateAsync(request.Json).WaitAsync(TimeSpan.FromMinutes(5));
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement analysis = Assert.Single(document.RootElement.GetProperty("cases").EnumerateArray());
        Assert.Equal("material_nonlinear", analysis.GetProperty("analysis_type").GetString());
        // The fixed-load state (λ = 0) and the 300 displacement-control targets.
        Assert.Equal(301, document.RootElement.GetProperty("results").GetArrayLength());
        Assert.Equal(10701.4, PeakBaseShear(json), 1);
    }
}
