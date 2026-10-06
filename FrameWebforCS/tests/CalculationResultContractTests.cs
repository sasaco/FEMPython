using FrameWebforCS.calculation;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class CalculationResultContractTests
{
    private static string Fixture(string group, string name) =>
        File.ReadAllText(Path.Combine(RepositoryRoot(), "FrameWeb", "tests", "data",
            "contracts", group, name + ".json"));

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }

    [Theory]
    [InlineData("single-static", 1)]
    [InlineData("multiple-static", 2)]
    [InlineData("nonlinear-steps", 1)]
    [InlineData("multiple-nonlinear", 2)]
    [InlineData("modal", 1)]
    [InlineData("empty-topology", 1)]
    public void SharedPositiveFixturesPreparePages(string name, int expectedCases)
    {
        AnalysisResultSet parsed = AnalysisResultSetJson.Deserialize(Fixture("positive", name));
        var presentation = new CalculationResultPresentation(parsed);
        Assert.Equal(expectedCases, parsed.Cases.Count);
        Assert.Equal(parsed.Results.Count, presentation.Pages.Count);
        Assert.Equal(parsed.Results.Select(result => result.CaseId),
            presentation.Pages.Select(page => page.Case.CaseId));
    }

    [Theory]
    [InlineData("mismatched-ids")]
    [InlineData("forbidden-fields")]
    [InlineData("final-marker-errors")]
    [InlineData("extra-properties")]
    [InlineData("duplicate-ids")]
    [InlineData("duplicate-coordinates")]
    public void SharedNegativeFixturesAreRejected(string name) =>
        Assert.ThrowsAny<Exception>(() => AnalysisResultSetJson.Deserialize(Fixture("negative", name)));

    [Fact]
    public void Valid257CasesHaveNoCountCeiling()
    {
        JsonObject root = JsonNode.Parse(Fixture("positive", "empty-topology"))!.AsObject();
        JsonObject originalCase = root["cases"]!.AsArray()[0]!.AsObject();
        JsonObject originalResult = root["results"]!.AsArray()[0]!.AsObject();
        var cases = new JsonArray();
        var results = new JsonArray();
        for (int index = 1; index <= 257; index++)
        {
            string id = "C" + index;
            var resultCase = (JsonObject)originalCase.DeepClone();
            var result = (JsonObject)originalResult.DeepClone();
            resultCase["case_id"] = id;
            result["case_id"] = id;
            cases.Add(resultCase);
            results.Add(result);
        }
        root["cases"] = cases;
        root["results"] = results;
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(root.ToJsonString()));
        Assert.Equal(257, presentation.Pages.Count);
    }

    [Fact]
    public void RejectedCandidateDoesNotReplaceCommittedResult()
    {
        var state = new AnalysisResultState();
        state.CommitJson(System.Text.Encoding.UTF8.GetBytes(Fixture("positive", "single-static")));
        AnalysisResultSet? committed = state.Current;
        Assert.ThrowsAny<Exception>(() =>
            state.CommitJson(System.Text.Encoding.UTF8.GetBytes(Fixture("negative", "mismatched-ids"))));
        Assert.Same(committed, state.Current);
    }

    [Fact]
    public void MovingLoadParentGroupsQualifiedChildrenWithoutShiftingFollowingCase()
    {
        JsonObject root = JsonNode.Parse(Fixture("positive", "single-static"))!.AsObject();
        JsonObject originalCase = root["cases"]!.AsArray()[0]!.AsObject();
        JsonObject originalResult = root["results"]!.AsArray()[0]!.AsObject();
        var cases = new JsonArray();
        var results = new JsonArray();
        foreach (var (id, symbol, displacement) in new[]
            { ("L", "LL", 0.001), ("L.1", "ll", 0.003),
              ("L.2", "SLL", -0.002), ("D", "D", 0.0) })
        {
            var itemCase = (JsonObject)originalCase.DeepClone();
            var itemResult = (JsonObject)originalResult.DeepClone();
            itemCase["case_id"] = id;
            itemCase["symbol"] = symbol;
            itemResult["case_id"] = id;
            itemResult["node_displacements"]!.AsArray()[1]!["components"]!["dx"] = displacement;
            cases.Add(itemCase);
            results.Add(itemResult);
        }
        root["cases"] = cases;
        root["results"] = results;
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(root.ToJsonString()));
        Assert.Equal(new[] { "L", "D" }, presentation.Pages.Select(page => page.Case.CaseId));
        Assert.Equal(new[] { "L.1", "L.2" },
            presentation.Pages[0].MovingChildren.Select(child => child.CaseId));

        using var sheet = new FarPoint.Win.Spread.SheetView();
        CalculationResultTableWriter.FillDisplacements(sheet, presentation, presentation.Pages[0], 3);
        Assert.Equal("Dx (mm)", sheet.Cells[6, 1].Text);
        Assert.Equal("3", sheet.Cells[6, 2].Text);
        Assert.Equal("L.1", sheet.Cells[6, 3].Text);
        Assert.Equal("-2", sheet.Cells[6, 4].Text);
        Assert.Equal("L.2", sheet.Cells[6, 5].Text);
    }

    [Fact]
    public void UnrecognizedUnitsKeepRawValuesAndShowUnspecifiedUnit()
    {
        JsonObject root = JsonNode.Parse(Fixture("positive", "single-static"))!.AsObject();
        root["units"]!["length"] = "custom_length";
        root["units"]!["force"] = "custom_force";
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(root.ToJsonString()));
        using var displacement = new FarPoint.Win.Spread.SheetView();
        CalculationResultTableWriter.FillDisplacements(displacement, presentation,
            presentation.Pages[0], 3);
        Assert.Contains("単位未指定", displacement.ColumnHeader.Cells[0, 1].Text);
        Assert.Equal("0.0010", displacement.Cells[1, 1].Text);

        using var reaction = new FarPoint.Win.Spread.SheetView();
        CalculationResultTableWriter.FillReactions(reaction, presentation,
            presentation.Pages[0], 3);
        Assert.Contains("単位未指定", reaction.ColumnHeader.Cells[0, 1].Text);
        Assert.Equal("-10.00", reaction.Cells[0, 1].Text);

        using var section = new FarPoint.Win.Spread.SheetView();
        CalculationResultTableWriter.FillSectionForces(section, presentation,
            presentation.Pages[0], 3);
        Assert.Contains("単位未指定", section.ColumnHeader.Cells[0, 4].Text);
        Assert.Contains("単位未指定", section.ColumnHeader.Cells[0, 7].Text);
        Assert.Equal("10.00", section.Cells[0, 3].Text);
        var raw = Assert.IsType<StaticAnalysisResult>(Assert.Single(presentation.ResultSet.Results));
        Assert.Equal(0.001, raw.NodeDisplacements[1].Components.Dx);
        Assert.Equal(-10, raw.SupportReactions[0].Components.Fx);
        Assert.Equal(10, raw.MemberSectionForces[0].Segments[0].IEnd.Fx);
    }

    [Fact]
    public void SectionForceTableUsesTopologyStationPositions()
    {
        JsonObject root = JsonNode.Parse(Fixture("positive", "single-static"))!.AsObject();
        JsonArray stations = root["topology"]!["members"]!.AsArray()[0]!["stations"]!.AsArray();
        stations[0]!["position"] = 0.2;
        stations[1]!["position"] = 1.2;
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(root.ToJsonString()));
        using var sheet = new FarPoint.Win.Spread.SheetView();
        CalculationResultTableWriter.FillSectionForces(sheet, presentation,
            presentation.Pages[0], 3);
        Assert.Equal("0.200", sheet.Cells[0, 2].Text);
        Assert.Equal("1.200", sheet.Cells[1, 2].Text);
    }

    [Fact]
    public void MovingChildBeforeParentIsStillGrouped()
    {
        JsonObject root = JsonNode.Parse(Fixture("positive", "single-static"))!.AsObject();
        JsonObject originalCase = root["cases"]!.AsArray()[0]!.AsObject();
        JsonObject originalResult = root["results"]!.AsArray()[0]!.AsObject();
        var cases = new JsonArray();
        var results = new JsonArray();
        foreach (string id in new[] { "L.1", "L" })
        {
            var resultCase = (JsonObject)originalCase.DeepClone();
            var result = (JsonObject)originalResult.DeepClone();
            resultCase["case_id"] = id;
            resultCase["symbol"] = "LL";
            result["case_id"] = id;
            cases.Add(resultCase);
            results.Add(result);
        }
        root["cases"] = cases;
        root["results"] = results;
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(root.ToJsonString()));
        Assert.Single(presentation.Pages);
        Assert.Equal("L", presentation.Pages[0].Case.CaseId);
        Assert.Equal("L.1", Assert.Single(presentation.Pages[0].MovingChildren).CaseId);
    }
}
