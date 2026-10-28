using FrameWebforCS.calculation;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class CalculationShearMergerTests
{
    [Fact]
    public void CompanionShearReplacesOnlyTheListedMembersAndLeavesNoCompanionCase()
    {
        JsonObject resultSet = WithCompanion();
        var companions = new Dictionary<string, ShearCompanion> { ["D"] = new("D#S", ["M1"]) };

        JsonObject merged = JsonNode.Parse(CalculationShearMerger.Merge(resultSet.ToJsonString(), companions))!.AsObject();

        Assert.Equal(new[] { "D" }, merged["cases"]!.AsArray().Select(item => item!["case_id"]!.GetValue<string>()));
        JsonNode result = Assert.Single(merged["results"]!.AsArray())!;
        JsonNode segment = result["member_section_forces"]![0]!["segments"]![0]!;
        Assert.Equal(99, segment["i_end"]!["fy"]!.GetValue<double>());
        Assert.Equal(77, segment["j_end"]!["fy"]!.GetValue<double>());
        Assert.Equal(0, segment["i_end"]!["mz"]!.GetValue<double>()); // Bending stays from the case itself.
        Assert.Equal(10, segment["i_end"]!["fx"]!.GetValue<double>());
        AnalysisResultSetJson.Deserialize(merged.ToJsonString());
    }

    [Fact]
    public void NoCompanionsLeaveTheResultUnchanged()
    {
        string source = Fixture().ToJsonString();
        Assert.Equal(source, CalculationShearMerger.Merge(source, new Dictionary<string, ShearCompanion>()));
    }

    [Fact]
    public void MissingCompanionResultIsAnError()
    {
        var companions = new Dictionary<string, ShearCompanion> { ["D"] = new("D#S", ["M1"]) };
        Assert.Throws<InvalidOperationException>(() =>
            CalculationShearMerger.Merge(Fixture().ToJsonString(), companions));
    }

    private static JsonObject WithCompanion()
    {
        JsonObject resultSet = Fixture();
        JsonNode companionCase = resultSet["cases"]![0]!.DeepClone();
        companionCase["case_id"] = "D#S";
        resultSet["cases"]!.AsArray().Add(companionCase);
        JsonNode companion = resultSet["results"]![0]!.DeepClone();
        companion["case_id"] = "D#S";
        JsonNode segment = companion["member_section_forces"]![0]!["segments"]![0]!;
        segment["i_end"]!["fy"] = 99;
        segment["j_end"]!["fy"] = 77;
        segment["i_end"]!["mz"] = 55;
        resultSet["results"]!.AsArray().Add(companion);
        return resultSet;
    }

    private static JsonObject Fixture()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        string root = directory?.FullName ?? throw new DirectoryNotFoundException();
        string path = Path.Combine(root, "FrameWeb", "tests", "data", "contracts", "positive", "single-static.json");
        return JsonNode.Parse(File.ReadAllText(path))!.AsObject();
    }
}
