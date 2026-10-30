using FrameWebforCS.providers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

// GLOSSARY: 2D非線形モード. Opening and saving a static nonlinear model must keep every
// nonlinear field value for value (the P7 JRSNAP comparison model is the acceptance fixture).
[Collection("DisplacementSingletons")]
public sealed class NonlinearInputPersistenceTests
{
    internal static string P7Path()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        string root = directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        return Path.Combine(root, "FrameWebforCS", "tests", "data", "JRSNAP検証", "models", "線路方向_Case1.json");
    }

    [Fact]
    public void P7ModelKeepsEveryNonlinearFieldThroughOpenAndSave()
    {
        JsonObject source = JsonNode.Parse(File.ReadAllText(P7Path()))!.AsObject();
        JsonObject saved = OpenAndSave(source.ToJsonString());

        Assert.True(saved["nonlinear"]!.GetValue<bool>());
        Assert.Equal(2, saved["dimension"]!.GetValue<int>());
        Assert.True(JsonNode.DeepEquals(source["nonlinear_combine"], saved["nonlinear_combine"]));
        Assert.True(JsonNode.DeepEquals(source["element"]!["1"]!["2"]!["nonlinear"],
            saved["element"]!["1"]!["2"]!["nonlinear"]));
        Assert.True(JsonNode.DeepEquals(source["fix_node"]!["1"]![0]!["rz"],
            saved["fix_node"]!["1"]![0]!["rz"]));
        foreach (var (id, sourceCase) in source["load"]!.AsObject())
        {
            Assert.Equal(sourceCase!["kind"]!.GetValue<string>(), saved["load"]![id]!["kind"]!.GetValue<string>());
            Assert.True(JsonNode.DeepEquals(sourceCase["limit"], saved["load"]![id]!["limit"]));
        }
        Assert.False(saved.ContainsKey("combine"));
        Assert.False(saved.ContainsKey("define"));
        Assert.False(saved.ContainsKey("pickup"));

        // A second round trip is stable.
        JsonObject again = OpenAndSave(saved.ToJsonString());
        Assert.True(JsonNode.DeepEquals(saved, again));
    }

    [Fact]
    public void LinearModelSavesNoNonlinearKeys()
    {
        JsonObject saved = OpenAndSave("""
            {"dimension":2,"node":{"1":{"x":0,"y":0},"2":{"x":1,"y":0}},
             "load":{"1":{"name":"a","symbol":"A"}},
             "combine":{"1":{"row":1,"name":"c","C1":1.5}}}
            """);
        Assert.False(saved.ContainsKey("nonlinear"));
        Assert.False(saved.ContainsKey("nonlinear_combine"));
        Assert.False(saved["load"]!["1"]!.AsObject().ContainsKey("kind"));
        Assert.False(saved["load"]!["1"]!.AsObject().ContainsKey("limit"));
        Assert.Equal(1.5, saved["combine"]!["1"]!["C1"]!.GetValue<double>());
    }

    [Fact]
    public void NonlinearKeyWithoutCombinationRowsOpensAsNonlinearMode()
    {
        JsonObject saved = OpenAndSave("""{"dimension":2,"nonlinear":true}""");
        Assert.True(saved["nonlinear"]!.GetValue<bool>());
        Assert.Empty(saved["nonlinear_combine"]!.AsObject());
    }

    [Theory]
    [InlineData("""{"dimension":3,"nonlinear":true}""")]
    [InlineData("""{"dimension":2,"nonlinear":1}""")]
    [InlineData("""{"dimension":2,"nonlinear":true,"combine":{"1":{"row":1,"C1":1}}}""")]
    [InlineData("""{"dimension":2,"nonlinear_combine":{"1":{"row":1,"C1":1}},"pickup":{"1":{"row":1,"C1":1}}}""")]
    [InlineData("""{"dimension":2,"load":{"1":{"kind":"constant"}},"nonlinear_combine":{}}""")]
    [InlineData("""{"dimension":2,"load":{"1":{"kind":"limited","limit":"0.1"}},"nonlinear_combine":{}}""")]
    [InlineData("""{"dimension":2,"load":{"1":{"kind":"fixed"}}}""")]
    [InlineData("""{"dimension":2,"nonlinear_combine":{"1":{"row":1,"C1":1,"speed":3}}}""")]
    public void InvalidNonlinearDocumentsAreRejectedWithoutReplacingTheOpenModel(string json)
    {
        try
        {
            Open("""{"dimension":2,"node":{"1":{"x":0,"y":0}}}""");
            Assert.ThrowsAny<JsonException>(() => Open(json));
            Assert.False(InputDataService.Instance.IsNonlinear);
            JsonObject saved = JsonNode.Parse(JsonSerializer.Serialize(InputDataService.Instance.GetSaveJson()))!.AsObject();
            Assert.Single(saved["node"]!.AsObject());
        }
        finally { Open("{}"); }
    }

    internal static JsonObject OpenAndSave(string json)
    {
        try
        {
            Open(json);
            return JsonNode.Parse(JsonSerializer.Serialize(InputDataService.Instance.GetSaveJson()))!.AsObject();
        }
        finally { Open("{}"); }
    }

    private static void Open(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputDataService.Instance.JsonDataOpen(document.RootElement);
    }
}
