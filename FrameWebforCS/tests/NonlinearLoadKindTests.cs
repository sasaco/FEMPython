using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

// GLOSSARY: 固定荷重・増分荷重・限定増分荷重. The case rows take a kind and, for a limited
// load, the λ at which it reaches its full value; entries the engine would reject are refused.
[Collection("DisplacementSingletons")]
public sealed class NonlinearLoadKindTests
{
    [Fact]
    public void ANonPositiveLimitIsRefusedAndTheRowKeepsItsLimit()
    {
        try
        {
            Open("""{"dimension":2,"nonlinear":true,"load":{"1":{"name":"a","kind":"limited","limit":0.5}}}""");
            clsLoadNameRow row = InputLoadService.Instance.LoadNames[0];

            row.limit = 0;
            row.limit = -1;
            row.limit = double.NaN;

            Assert.Equal(0.5, row.limit);
            Assert.Equal(0.5, Save()["load"]!["1"]!["limit"]!.GetValue<double>());
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void LeavingTheLimitedKindClearsTheLimit()
    {
        try
        {
            Open("""{"dimension":2,"nonlinear":true,"load":{"1":{"name":"a","kind":"limited","limit":0.5}}}""");
            clsLoadNameRow row = InputLoadService.Instance.LoadNames[0];

            row.kind = "fixed";

            Assert.Null(row.limit);
            JsonObject saved = Save()["load"]!["1"]!.AsObject();
            Assert.Equal("fixed", saved["kind"]!.GetValue<string>());
            Assert.False(saved.ContainsKey("limit"));
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void OnlyALimitedCaseTakesALimit()
    {
        try
        {
            Open("""{"dimension":2,"nonlinear":true,"load":{"1":{"name":"a","kind":"fixed"},"2":{"name":"b"}}}""");
            clsLoadNameRow fixedRow = InputLoadService.Instance.LoadNames[0];
            clsLoadNameRow incrementalRow = InputLoadService.Instance.LoadNames[1];

            fixedRow.limit = 0.5;
            incrementalRow.limit = 0.5;

            Assert.Null(fixedRow.limit);
            Assert.Null(incrementalRow.limit);
        }
        finally { Open("{}"); }
    }

    private static JsonObject Save() =>
        JsonNode.Parse(JsonSerializer.Serialize(InputDataService.Instance.GetSaveJson()))!.AsObject();

    private static void Open(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputDataService.Instance.JsonDataOpen(document.RootElement);
    }
}
