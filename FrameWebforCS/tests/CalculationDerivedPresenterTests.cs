using FrameWebforCS.calculation;
using FrameWebforCS.components.input;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class CalculationDerivedPresenterTests
{
    private static string Fixture(string name)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        string root = directory?.FullName ?? throw new DirectoryNotFoundException();
        return File.ReadAllText(Path.Combine(root, "FrameWeb", "tests", "data", "contracts",
            "positive", name + ".json"));
    }

    private static AnalysisResultSet StaticCases(string parentSymbol = "LL")
    {
        JsonObject root = JsonNode.Parse(Fixture("single-static"))!.AsObject();
        JsonObject originalCase = root["cases"]!.AsArray()[0]!.AsObject();
        JsonObject originalResult = root["results"]!.AsArray()[0]!.AsObject();
        var cases = new JsonArray();
        var results = new JsonArray();
        foreach (var item in new[]
        {
            (Id: "1", Symbol: parentSymbol, Dx: 2.0),
            (Id: "1.1", Symbol: "child", Dx: 5.0),
            (Id: "2", Symbol: "DL", Dx: 4.0),
        })
        {
            var resultCase = (JsonObject)originalCase.DeepClone();
            var result = (JsonObject)originalResult.DeepClone();
            resultCase["case_id"] = item.Id;
            resultCase["name"] = item.Id;
            resultCase["symbol"] = item.Symbol;
            result["case_id"] = item.Id;
            result["node_displacements"]!.AsArray()[1]!["components"]!["dx"] = item.Dx;
            result["support_reactions"]!.AsArray()[0]!["components"]!["fx"] = -item.Dx;
            result["member_section_forces"]!.AsArray()[0]!["segments"]!.AsArray()[0]!["i_end"]!["fx"] = item.Dx;
            cases.Add(resultCase);
            results.Add(result);
        }
        root["cases"] = cases;
        root["results"] = results;
        return AnalysisResultSetJson.Deserialize(root.ToJsonString());
    }

    private static clsCombine<T> Row<T>(string id, int row, params (int Column, T Value)[] terms)
        where T : struct
    {
        var value = new clsCombine<T> { Id = id, row = row };
        foreach (var term in terms) value.Coefficients.Add(term.Column, term.Value);
        return value;
    }

    [Fact]
    public void UppercaseLlExpandsChildrenAndSignedDefineSelectsWholeVectors()
    {
        var definitions = new Dictionary<int, clsCombine<int>>
        {
            [1] = Row("10", 1, (1, 1), (2, -2)),
        };
        CalculationDerivedPresentation derived = CalculationDerivedPresenter.Build(
            StaticCases(), 2, definitions,
            new Dictionary<int, clsCombine<double>>(), new Dictionary<int, clsCombine<int>>());

        CalculationDerivedCase definition = Assert.Single(derived.Defines);
        CalculationDerivedRow maximum = Assert.Single(definition.Displacements["dx_max"],
            row => row.EntityId == "2");
        CalculationDerivedRow minimum = Assert.Single(definition.Displacements["dx_min"],
            row => row.EntityId == "2");
        Assert.Equal(5, maximum.Components["dx"]);
        Assert.Equal("1.1", maximum.SourceCaseId);
        Assert.Equal(-4, minimum.Components["dx"]);
        Assert.Equal("-2", minimum.SourceCaseId);
        Assert.Equal(-5, Assert.Single(definition.Reactions["fx_min"]).Components["fx"]);
        Assert.Equal(5, Assert.Single(definition.SectionForces["fx_max"],
            row => row.EntityId == "M1" && row.StationId == "S0").Components["fx"]);
    }

    [Fact]
    public void LowercaseLlDoesNotExpandChildrenForDerivedDefine()
    {
        var definitions = new Dictionary<int, clsCombine<int>>
        {
            [1] = Row("10", 1, (1, 1)),
        };
        CalculationDerivedPresentation derived = CalculationDerivedPresenter.Build(
            StaticCases("ll"), 2, definitions,
            new Dictionary<int, clsCombine<double>>(), new Dictionary<int, clsCombine<int>>());

        CalculationDerivedRow maximum = Assert.Single(Assert.Single(derived.Defines)
            .Displacements["dx_max"], row => row.EntityId == "2");
        Assert.Equal(2, maximum.Components["dx"]);
        Assert.Equal("1", maximum.SourceCaseId);
    }

    [Fact]
    public void CombineAndPickupPreserveSourceAndSelectedCombination()
    {
        var definitions = new Dictionary<int, clsCombine<int>>
        {
            [1] = Row("10", 1, (1, 1), (2, -2)),
            [2] = Row("20", 2, (1, 2)),
        };
        var combinations = new Dictionary<int, clsCombine<double>>
        {
            [1] = Row("40", 1, (10, 2.0), (20, -1.0)),
            [2] = Row("41", 2, (20, 3.0)),
        };
        var pickups = new Dictionary<int, clsCombine<int>>
        {
            [1] = Row("P", 1, (1, 40), (2, 41)),
        };
        CalculationDerivedPresentation derived = CalculationDerivedPresenter.Build(
            StaticCases(), 2, definitions, combinations, pickups);

        CalculationDerivedRow combined = Assert.Single(derived.Combines[0]
            .Displacements["dx_max"], row => row.EntityId == "2");
        Assert.Equal(6, combined.Components["dx"]);
        Assert.Equal("1.1,2", combined.SourceCaseId);
        Assert.Contains("D10", combined.Provenance);

        CalculationDerivedCase pickup = Assert.Single(derived.Pickups);
        CalculationDerivedRow maximum = Assert.Single(pickup.Displacements["dx_max"],
            row => row.EntityId == "2");
        CalculationDerivedRow minimum = Assert.Single(pickup.Displacements["dx_min"],
            row => row.EntityId == "2");
        Assert.Equal(12, maximum.Components["dx"]);
        Assert.Equal("41", maximum.SourceCaseId);
        Assert.Equal(-12, minimum.Components["dx"]);
        Assert.Equal("40", minimum.SourceCaseId);
    }

    [Fact]
    public void PickupTiesKeepFirstCombinationForBothSignedExtrema()
    {
        var definitions = new Dictionary<int, clsCombine<int>>
        {
            [1] = Row("10", 1, (1, 1)),
        };
        var combinations = new Dictionary<int, clsCombine<double>>
        {
            [1] = Row("40", 1, (10, 1.0)),
            [2] = Row("41", 2, (10, 1.0)),
        };
        var pickups = new Dictionary<int, clsCombine<int>>
        {
            [1] = Row("P", 1, (1, 40), (2, 41)),
        };

        CalculationDerivedCase pickup = Assert.Single(CalculationDerivedPresenter.Build(
            StaticCases("DL"), 2, definitions, combinations, pickups).Pickups);

        Assert.Equal("40", Assert.Single(pickup.Displacements["dx_max"],
            row => row.EntityId == "2").SourceCaseId);
        Assert.Equal("40", Assert.Single(pickup.Displacements["dx_min"],
            row => row.EntityId == "2").SourceCaseId);
        Assert.Equal("40", Assert.Single(pickup.Reactions["fx_max"]).SourceCaseId);
        Assert.Equal("40", Assert.Single(pickup.Reactions["fx_min"]).SourceCaseId);
    }

    [Fact]
    public void NonStaticOperandIsRejectedForDefineAndFallbackCombine()
    {
        JsonObject root = JsonNode.Parse(Fixture("nonlinear-steps"))!.AsObject();
        root["cases"]!.AsArray()[0]!["case_id"] = "1";
        foreach (JsonNode? result in root["results"]!.AsArray()) result!["case_id"] = "1";
        AnalysisResultSet nonlinear = AnalysisResultSetJson.Deserialize(root.ToJsonString());
        var definitions = new Dictionary<int, clsCombine<int>>
        {
            [1] = Row("10", 1, (1, 1)),
        };
        Assert.Throws<InvalidOperationException>(() => CalculationDerivedPresenter.Build(
            nonlinear, 2, definitions, new Dictionary<int, clsCombine<double>>(),
            new Dictionary<int, clsCombine<int>>()));

        var combinations = new Dictionary<int, clsCombine<double>>
        {
            [1] = Row("40", 1, (1, 1.0)),
        };
        Assert.Throws<InvalidOperationException>(() => CalculationDerivedPresenter.Build(
            nonlinear, 2, new Dictionary<int, clsCombine<int>>(), combinations,
            new Dictionary<int, clsCombine<int>>()));
    }

    [Fact]
    public void CapturedInputIsIndependentOfLaterFormEdits()
    {
        var defineRow = Row("10", 1, (1, 1));
        var definitions = new Dictionary<int, clsCombine<int>> { [1] = defineRow };
        CalculationDerivedInputSnapshot snapshot = CalculationDerivedInputSnapshot.Capture(
            definitions, new Dictionary<int, clsCombine<double>>(),
            new Dictionary<int, clsCombine<int>>());
        defineRow.Coefficients[1] = 2;
        defineRow.Id = "changed";
        definitions.Clear();

        CalculationDerivedCase result = Assert.Single(CalculationDerivedPresenter.Build(
            StaticCases(), 2, snapshot).Defines);
        Assert.Equal("10", result.Id);
        CalculationDerivedRow selected = Assert.Single(result.Displacements["dx_max"],
            row => row.EntityId == "2");
        Assert.Equal(5, selected.Components["dx"]); // The captured LL parent still expands.
        Assert.Equal("1.1", selected.SourceCaseId);
    }

    [Fact]
    public void DerivedFallbackDoesNotReintroduceA256CaseLimit()
    {
        JsonObject root = JsonNode.Parse(Fixture("empty-topology"))!.AsObject();
        JsonObject originalCase = root["cases"]!.AsArray()[0]!.AsObject();
        JsonObject originalResult = root["results"]!.AsArray()[0]!.AsObject();
        var cases = new JsonArray();
        var results = new JsonArray();
        for (int index = 1; index <= 257; index++)
        {
            string id = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var resultCase = (JsonObject)originalCase.DeepClone();
            var result = (JsonObject)originalResult.DeepClone();
            resultCase["case_id"] = id;
            result["case_id"] = id;
            cases.Add(resultCase);
            results.Add(result);
        }
        root["cases"] = cases;
        root["results"] = results;

        CalculationDerivedPresentation derived = CalculationDerivedPresenter.Build(
            AnalysisResultSetJson.Deserialize(root.ToJsonString()), 2,
            new Dictionary<int, clsCombine<int>>(), new Dictionary<int, clsCombine<double>>(),
            new Dictionary<int, clsCombine<int>>());
        Assert.Equal(257, derived.Defines.Count);
        Assert.Equal("257", derived.Defines[^1].Id);
    }
}
