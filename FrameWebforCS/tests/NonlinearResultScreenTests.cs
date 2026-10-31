using FarPoint.Win.Spread;
using FrameWebforCS.calculation;
using FrameWebforCS.components.menu;
using FrameWebforCS.components.result;
using FrameWebforCS.providers;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;
using Xunit;
using Action = System.Action;

namespace FrameWebforCS.Tests;

// GLOSSARY: 段階. The displacement, section force and reaction screens show a nonlinear combination
// as one tab beside its step table; the final step is shown first and a row picks another step.
[Collection("DisplacementSingletons")]
public sealed class NonlinearResultScreenTests
{
    private static readonly (Func<Control> Create, Action<SheetView> Columns,
        Action<SheetView, CalculationResultPresentation, CalculationResultPage, int> Fill)[] Screens =
    [
        (() => new ResultDisgComponent(), ResultDisgComponent.SetSheet1, CalculationResultTableWriter.FillDisplacements),
        (() => new ResultFsecComponent(), ResultFsecComponent.SetSheet1, CalculationResultTableWriter.FillSectionForces),
        (() => new ResultReacComponent(), ResultReacComponent.SetSheet1, CalculationResultTableWriter.FillReactions),
    ];

    [Fact]
    public void ACombinationOpensOnItsFinalStepAndARowOfTheStepTableShowsThatStep()
    {
        string json = Fixture("nonlinear-states");
        JsonArray results = JsonNode.Parse(json)!["results"]!.AsArray();
        var presentation = new CalculationResultPresentation(AnalysisResultSetJson.Deserialize(json), dimension: 2,
            controls: new Dictionary<string, NonlinearControl> { ["1"] = new("2", "dx") });
        int final = results.Count - 1;
        RunSta(() => WithResult(presentation, 2, () =>
        {
            foreach (var screen in Screens)
            {
                using Control view = screen.Create();
                FpSpread spread = (FpSpread)view.Controls.Find("fpSpread1", true).Single();
                var steps = (ResultStepTable)view.Controls.Find("resultStepTable", true).Single();

                Assert.Single(spread.Sheets);
                Assert.Equal(results.Count, steps.Sheet.RowCount);
                Assert.Equal(final, steps.Sheet.ActiveRowIndex);
                Assert.Equal(final.ToString(CultureInfo.CurrentCulture), steps.Sheet.Cells[final, 0].Text);
                Assert.Equal(results[final]!["state"]!["load_factor"]!.GetValue<double>(),
                    double.Parse(steps.Sheet.Cells[final, 1].Text, CultureInfo.CurrentCulture), 4);
                double dx = results[final]!["node_displacements"]!.AsArray()
                    .Single(node => node!["node_id"]!.GetValue<string>() == "2")!["components"]!["dx"]!.GetValue<double>();
                // Shown in the unit of the other result tables (mm for results in m).
                Assert.Equal($"変位({presentation.DisplayLengthUnit})", steps.Sheet.ColumnHeader.Cells[1, 2].Text);
                Assert.Equal(presentation.DisplayLength(dx),
                    double.Parse(steps.Sheet.Cells[final, 2].Text, CultureInfo.CurrentCulture), 4);
                AssertShows(presentation.Pages[final], spread.Sheets[0]);

                steps.SelectStep(0);

                Assert.Equal(0, steps.Sheet.ActiveRowIndex);
                AssertShows(presentation.Pages[0], spread.Sheets[0]);

                void AssertShows(CalculationResultPage page, SheetView shown)
                {
                    var expected = new SheetView();
                    screen.Columns(expected);
                    screen.Fill(expected, presentation, page, 2);
                    Assert.Equal(expected.RowCount, shown.RowCount);
                    for (int row = 0; row < expected.RowCount; row++)
                        for (int column = 0; column < expected.ColumnCount; column++)
                            Assert.Equal(expected.Cells[row, column].Text, shown.Cells[row, column].Text);
                }
            }
        }));
    }

    [Fact]
    public void ALinearResultHasNoStepTable()
    {
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(Fixture("multiple-static")));
        RunSta(() => WithResult(presentation, 3, () =>
        {
            foreach (var screen in Screens)
            {
                using Control view = screen.Create();
                FpSpread spread = (FpSpread)view.Controls.Find("fpSpread1", true).Single();
                var steps = (ResultStepTable)view.Controls.Find("resultStepTable", true).Single();

                Assert.Equal(presentation.Pages.Count, spread.Sheets.Count);
                Assert.Equal(0, steps.Sheet.RowCount);
            }
        }));
    }

    // The COMBINE/PICKUP result screens belong to linear models only.
    [Fact]
    public void TheLinearCombinationResultsLeaveTheSidebarInTheNonlinearMode()
    {
        string[] linearOnly = ["combdisg", "pickdisg", "combreac", "pickreac", "combfsec", "pickfsec"];
        RunSta(() =>
        {
            try
            {
                Open("""{"dimension":2}""");
                using var sidebar = new SidebarComponent();
                TreeView tree = Assert.Single(sidebar.Controls.OfType<TreeView>());
                string[] before = Keys(tree);

                InputDataService.Instance.SetAnalysisMode(2, true);
                Assert.Equal(before.Except(linearOnly), Keys(tree));

                InputDataService.Instance.SetAnalysisMode(2, false);
                Assert.Equal(before, Keys(tree));

                Open("""{"dimension":2,"nonlinear":true}""");
                Assert.Equal(before.Except(linearOnly), Keys(tree));
            }
            finally { Open("{}"); }
        });

        static string[] Keys(TreeView tree)
        {
            var keys = new List<string>();
            void Walk(TreeNodeCollection nodes)
            {
                foreach (TreeNode node in nodes) { keys.Add(node.Name); Walk(node.Nodes); }
            }
            Walk(tree.Nodes);
            return [.. keys];
        }
    }

    private static void Open(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        InputDataService.Instance.JsonDataOpen(document.RootElement);
    }

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(NonlinearCalculationEngineTests.RepositoryRoot(), "FrameWeb", "tests", "data",
            "contracts", "positive", name + ".json"));

    private static void WithResult(CalculationResultPresentation presentation, int dimension, Action body)
    {
        using (JsonDocument input = JsonDocument.Parse($$"""{"dimension":{{dimension}}}"""))
            InputDataService.Instance.JsonDataOpen(input.RootElement);
        CalculationResultStore.Instance.Commit(presentation);
        try { body(); }
        finally
        {
            CalculationResultStore.Instance.Clear();
            using JsonDocument empty = JsonDocument.Parse("{}");
            InputDataService.Instance.JsonDataOpen(empty.RootElement);
        }
    }

    private static void RunSta(Action body)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception exception) { error = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)));
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}
