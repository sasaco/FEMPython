using FrameWebforCS.calculation;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

// GLOSSARY: 段階. The result screens show one tab per nonlinear combination with every step in it,
// opening on the final step, and a step table of λ and the control node's displacement.
public sealed class NonlinearResultPresentationTests
{
    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(NonlinearCalculationEngineTests.RepositoryRoot(), "FrameWeb", "tests", "data",
            "contracts", "positive", name + ".json"));

    [Fact]
    public void ACombinationIsOneTabOfItsStepsThatOpensOnTheFinalStep()
    {
        string json = Fixture("nonlinear-states");
        JsonArray results = JsonNode.Parse(json)!["results"]!.AsArray();
        var presentation = new CalculationResultPresentation(AnalysisResultSetJson.Deserialize(json), dimension: 2,
            controls: new Dictionary<string, NonlinearControl> { ["1"] = new("2", "dx") });

        CalculationResultTab tab = Assert.Single(presentation.Tabs);
        Assert.Equal("線路方向_Case1", tab.Label);
        Assert.Equal(Enumerable.Range(0, results.Count), tab.PageIndices);
        Assert.Equal(results.Count - 1, tab.DefaultPageIndex);
        Assert.Equal(
            results.Select(result => (
                result!["state"]!["index"]!.GetValue<int>(),
                result["state"]!["load_factor"]!.GetValue<double>(),
                (double?)result["node_displacements"]!.AsArray()
                    .Single(node => node!["node_id"]!.GetValue<string>() == "2")!["components"]!["dx"]!.GetValue<double>())),
            tab.Steps.Select(step => (step.StepIndex, step.LoadFactor, step.ControlDisplacement)));
    }

    [Fact]
    public void TheControlNodeComesFromTheCombinationRowOfTheSavedInput()
    {
        string saved = File.ReadAllText(NonlinearInputPersistenceTests.P7Path());

        var control = Assert.Single(NonlinearControl.FromSavedJson(saved));

        Assert.Equal("1", control.Key);
        Assert.Equal(new NonlinearControl("2", "dx"), control.Value);
        Assert.Empty(NonlinearControl.FromSavedJson("""{"nonlinear_combine":{"1":{"row":1,"C1":1,"n_load_steps":5}}}"""));
    }

    [Fact]
    public void WithoutAControlNodeTheStepTableHasNoDisplacement()
    {
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(Fixture("multiple-nonlinear")), dimension: 2);

        Assert.Equal(2, presentation.Tabs.Count);
        Assert.All(presentation.Tabs.SelectMany(tab => tab.Steps), step => Assert.Null(step.ControlDisplacement));
    }

    [Fact]
    public void ALinearCaseIsATabOfItsOwnWithNoSteps()
    {
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(Fixture("multiple-static")));

        Assert.Equal(presentation.Pages.Select(page => page.Label), presentation.Tabs.Select(tab => tab.Label));
        Assert.Equal(Enumerable.Range(0, presentation.Pages.Count), presentation.Tabs.Select(tab => tab.DefaultPageIndex));
        Assert.All(presentation.Tabs, tab => Assert.Empty(tab.Steps));
    }
}
