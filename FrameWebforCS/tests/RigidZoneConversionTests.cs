using FrameWebforCS.providers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class RigidZoneConversionTests
{
    [Fact]
    public void FrdRigidZonesSurviveDesktopSaveReloadAndCalculationRequest()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameGConverter")))
            directory = directory.Parent;
        string repository = directory?.FullName ?? throw new DirectoryNotFoundException();
        using var converted = InputFileLoader.Open(Path.Combine(repository, "FrameGConverter", "Convert_Test",
            "TestData", "バネ連衡あり.frd"));
        var original = converted.RootElement;
        var rigid = original.GetProperty("rigid").EnumerateArray().ToArray();
        Assert.NotEmpty(rigid);
        var input = JsonNode.Parse(original.GetRawText())!.AsObject();
        // Isolate the rigid/spring pipeline from this fixture's unsupported mark-14 loads.
        input["load"] = JsonNode.Parse("""
            {"1":{"symbol":"DL","fix_node":1,"fix_member":1,"element":1,"joint":1,
                "load_node":[{"row":1,"n":"1","tx":1}]}}
            """);
        using var document = JsonDocument.Parse(input.ToJsonString());
        try
        {
            InputDataService.Instance.JsonDataOpen(document.RootElement);
            using var saved = JsonDocument.Parse(JsonSerializer.Serialize(InputDataService.Instance.GetSaveJson()));
            AssertZones(rigid, saved.RootElement);
            Assert.Equal(original.GetProperty("member").EnumerateObject().Select(item => item.Name),
                saved.RootElement.GetProperty("member").EnumerateObject().Select(item => item.Name));
            Assert.Equal(original.GetProperty("node").EnumerateObject().Count(),
                saved.RootElement.GetProperty("node").EnumerateObject().Count());

            InputDataService.Instance.JsonDataOpen(saved.RootElement);
            using var request = JsonDocument.Parse(InputDataService.Instance.CreateCalculationRequest().Json);
            AssertZones(rigid, request.RootElement);
            Assert.Equal(original.GetProperty("member").EnumerateObject().Count(),
                request.RootElement.GetProperty("member").EnumerateObject().Count());
            Assert.Equal(40, request.RootElement.GetProperty("fix_member").GetProperty("1").GetArrayLength());
        }
        finally
        {
            using var empty = JsonDocument.Parse("{}");
            InputDataService.Instance.JsonDataOpen(empty.RootElement);
        }
    }

    private static void AssertZones(JsonElement[] expected, JsonElement actual)
    {
        var zones = actual.GetProperty("rigid").EnumerateArray().ToArray();
        Assert.Equal(expected.Length, zones.Length);
        foreach (var source in expected)
        {
            string memberId = source.GetProperty("m").GetString()!;
            var zone = Assert.Single(zones, row => row.GetProperty("m").GetString() == memberId);
            Assert.Equal(source.GetProperty("Ilength").GetDouble(), zone.GetProperty("Ilength").GetDouble(), 5);
            Assert.Equal(source.GetProperty("Jlength").GetDouble(), zone.GetProperty("Jlength").GetDouble(), 5);
            Assert.Equal(source.GetProperty("e").GetInt32(), zone.GetProperty("e").GetInt32());
            Assert.True(actual.GetProperty("member").TryGetProperty(memberId, out _));
            Assert.True(actual.GetProperty("element").GetProperty("1")
                .TryGetProperty(zone.GetProperty("e").GetInt32().ToString(), out _));
        }
    }
}
