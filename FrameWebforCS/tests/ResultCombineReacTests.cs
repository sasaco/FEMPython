using FarPoint.Win.Spread;
using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Forms;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ResultCombineReacTests
{
    private static ResultCombineReacSnapshot Snapshot(int dimension = 2) => new(
        1, dimension,
        [
            new ReacCaseSnapshot("1", [
                new ReacNodeSnapshot("20", new ReacVector(2, 4, 1, 3, 4, 6)),
                new ReacNodeSnapshot("10", new ReacVector(-3, 1, 1, 2, 1, -2))]),
            new ReacCaseSnapshot("2", [
                new ReacNodeSnapshot("20", new ReacVector(-5, 1, 2, 9, 8, -8)),
                new ReacNodeSnapshot("10", new ReacVector(7, -4, 2, 5, 3, 9))])
        ],
        [new DefineReacSnapshot("12", [1, -2])],
        [new CombineReacSnapshot("40", "Main", [new CombineReacTerm(12, 2.5)])],
        []);

    [Fact]
    public void DefineSelectsWholeReactionVectorAndCombinesById()
    {
        var result = ResultCombineReacAggregator.Calculate(Snapshot());
        var combination = Assert.Single(result.Cases);
        Assert.Equal("40", combination.Id);
        Assert.Equal("Main", combination.Name);
        Assert.Equal(6, combination.Rows.Count);
        Assert.Equal(new[] { "10", "20" }, combination.Rows["tx_max"].Select(row => row.Id));
        var max20 = combination.Rows["tx_max"].Single(row => row.Id == "20");
        Assert.Equal(12.5, max20.Tx);
        Assert.Equal(-2.5, max20.Ty); // Entire negated case 2 vector follows tx selection.
        Assert.Equal(-5, max20.Tz);
        Assert.Equal("-12", max20.Case);
        var min20 = combination.Rows["tx_min"].Single(row => row.Id == "20");
        Assert.Equal(5, min20.Tx);
        Assert.Equal("+12", min20.Case);
    }

    [Fact]
    public void NegativeCombineCoefficientUsesLegacyNegativeLabelAndThreeDimensionalModes()
    {
        var source = Snapshot(3);
        var snapshot = source with
        {
            Defines = [new DefineReacSnapshot("12", [-2])],
            Combines = [new CombineReacSnapshot("40", null, [new CombineReacTerm(12, -1)])]
        };
        var result = ResultCombineReacAggregator.Calculate(snapshot);
        Assert.Equal(12, result.Cases[0].Rows.Count);
        var node = result.Cases[0].Rows["tx_max"].Single(row => row.Id == "20");
        Assert.Equal(-5, node.Tx);
        Assert.Equal("-12", node.Case);
        Assert.Equal(9, node.Mx);
        Assert.Contains("my_min", result.Cases[0].Rows.Keys);
    }

    [Fact]
    public void ZeroCaseAndMissingOperandsFollowLegacyWorker()
    {
        var source = Snapshot();
        var snapshot = source with
        {
            Defines = [new DefineReacSnapshot("12", [999, 0])],
            Combines = [new CombineReacSnapshot("40", null,
                [new CombineReacTerm(99, 2), new CombineReacTerm(12, 3)])]
        };
        var rows = ResultCombineReacAggregator.Calculate(snapshot).Cases[0].Rows["tx_max"];
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => { Assert.Equal(0, row.Tx); Assert.Equal("", row.Case); });
    }

    [Fact]
    public void StaticCaseFallbackAndCancellation()
    {
        var source = Snapshot();
        var fallback = source with
        {
            Defines = [],
            Combines = [new CombineReacSnapshot("4", null, [new CombineReacTerm(2, 1)])],
            StaticCaseIds = [2]
        };
        Assert.Equal(-5, ResultCombineReacAggregator.Calculate(fallback)
            .Cases[0].Rows["tx_max"].Single(row => row.Id == "20").Tx);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            ResultCombineReacAggregator.Calculate(fallback, cancellation.Token));
    }

    [Fact]
    public void PublishedBudgetsAllowExactLimitAndRejectFirstExcess()
    {
        ResultCombineReacAggregator.ValidateBudget(
            ResultCombineReacAggregator.MaxNodes,
            ResultCombineReacAggregator.MaxScalarOperations,
            ResultCombineReacAggregator.MaxOutputCells);
        Assert.Throws<InvalidOperationException>(() => ResultCombineReacAggregator.ValidateBudget(
            ResultCombineReacAggregator.MaxNodes + 1L, 0, 0));
        Assert.Throws<InvalidOperationException>(() => ResultCombineReacAggregator.ValidateBudget(
            0, ResultCombineReacAggregator.MaxScalarOperations + 1, 0));
        Assert.Throws<InvalidOperationException>(() => ResultCombineReacAggregator.ValidateBudget(
            0, 0, ResultCombineReacAggregator.MaxOutputCells + 1));
    }

    [Fact]
    public void TwoDimensionalUiDisplaysTwoDecimalPlacesAndSelectedMode()
    {
        RunSta(() =>
        {
            LoadTwoDimensionalResults();
            using var form = new Form();
            using var component = new ResultCombineReacComponent();
            form.Controls.Add(component);
            form.Show();
            var spread = (FpSpread)component.Controls.Find("fpSpread1", true).Single();
            var modes = (ComboBox)component.Controls.Find("modeSelector", true).Single();
            PumpUntil(() => spread.Sheets.Count == 1 && spread.Sheets[0].RowCount == 1);

            Assert.Equal("7 Main", spread.Sheets[0].SheetName);
            Assert.Equal(5, spread.Sheets[0].ColumnCount);
            Assert.Equal(6, modes.Items.Count);
            Assert.Equal("2.51", spread.Sheets[0].Cells[0, 1].Text);
            Assert.Equal("+5", spread.Sheets[0].Cells[0, 4].Text);
            modes.SelectedIndex = 1;
            Assert.Equal("2.51", spread.Sheets[0].Cells[0, 1].Text);
            ResultCombineReacCoordinator.Instance.FailLoad();
            Assert.Equal(0, spread.Sheets.Count);
        });
    }

    [Fact]
    public void ThreeDimensionalUiDisplaysSixComponents()
    {
        RunSta(() =>
        {
            LoadTwoDimensionalResults(3);
            using var form = new Form();
            using var component = new ResultCombineReacComponent();
            form.Controls.Add(component);
            form.Show();
            var spread = (FpSpread)component.Controls.Find("fpSpread1", true).Single();
            var modes = (ComboBox)component.Controls.Find("modeSelector", true).Single();
            PumpUntil(() => spread.Sheets.Count == 1 && spread.Sheets[0].RowCount == 1);
            Assert.Equal(8, spread.Sheets[0].ColumnCount);
            Assert.Equal(12, modes.Items.Count);
            Assert.Equal("0.25", spread.Sheets[0].Cells[0, 3].Text);
            Assert.Equal("1.25", spread.Sheets[0].Cells[0, 6].Text);
            ResultCombineReacCoordinator.Instance.FailLoad();
        });
    }

    [Fact]
    public void OldCalculationCannotPublishOverReloadedReaction()
    {
        RunSta(() =>
        {
            using var started = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            int active = 0;
            int peak = 0;
            LoadControlledCase("7", "Old");
            ResultCombineReacOutput Calculate(ResultCombineReacSnapshot snapshot, CancellationToken _)
            {
                int count = Interlocked.Increment(ref active);
                peak = Math.Max(peak, count);
                try
                {
                    string id = snapshot.Combines.Single().Id;
                    if (id == "7")
                    {
                        started.Set();
                        if (!release.Wait(TimeSpan.FromSeconds(8)))
                            throw new TimeoutException("Old calculation was never released.");
                    }
                    return new([new CombineReacCaseResult(id, id == "7" ? "Old" : "New",
                        new Dictionary<string, IReadOnlyList<CombineReacNodeResult>>
                        {
                            ["tx_max"] = [new CombineReacNodeResult("11", int.Parse(id), 0, 0, 0, 0, 0, "+5")]
                        })]);
                }
                finally { Interlocked.Decrement(ref active); }
            }
            using var form = new Form();
            using var component = new ResultCombineReacComponent(Calculate);
            form.Controls.Add(component);
            form.Show();
            var spread = (FpSpread)component.Controls.Find("fpSpread1", true).Single();
            PumpUntil(() => started.IsSet);
            LoadControlledCase("8", "New");
            Assert.Equal(0, spread.Sheets.Count);
            release.Set();
            PumpUntil(() => spread.Sheets.Count == 1 && spread.Sheets[0].RowCount == 1 &&
                spread.Sheets[0].SheetName == "8 New");
            Assert.Equal("8.00", spread.Sheets[0].Cells[0, 1].Text);
            Assert.Equal(1, peak);
            ResultCombineReacCoordinator.Instance.FailLoad();
        });
    }

    private static void LoadTwoDimensionalResults(int dimension = 2)
    {
        var coordinator = ResultCombineReacCoordinator.Instance;
        coordinator.BeginLoad();
        using var definitions = JsonDocument.Parse("""
            {"define":{"5":{"row":1,"C1":1}},
             "combine":{"7":{"row":1,"name":"Main","C5":2}}}
            """);
        InputCombineService.Instance.setCombineJson(definitions.RootElement);
        using var reactions = JsonDocument.Parse("""
            {"result":{"1":{"reac":{"11":{"tx":1.255,"ty":-2,"tz":0.125,
                                            "mx":0.5,"my":-0.25,"mz":0.625}}}}}
            """);
        ResultReacService.Instance.setReacJson(reactions.RootElement);
        coordinator.CompleteLoad(dimension);
    }

    private static void LoadControlledCase(string id, string name)
    {
        var coordinator = ResultCombineReacCoordinator.Instance;
        coordinator.BeginLoad();
        using var definitions = JsonDocument.Parse(
            "{\"define\":{\"5\":{\"row\":1,\"C1\":1}},\"combine\":{\"" + id +
            "\":{\"row\":1,\"name\":\"" + name + "\",\"C5\":1}}}");
        InputCombineService.Instance.setCombineJson(definitions.RootElement);
        using var reactions = JsonDocument.Parse(
            """{"result":{"1":{"reac":{"11":{"tx":1,"ty":2,"mz":3}}}}}""");
        ResultReacService.Instance.setReacJson(reactions.RootElement);
        coordinator.CompleteLoad(2);
    }

    private static void PumpUntil(Func<bool> predicate)
    {
        var timer = Stopwatch.StartNew();
        while (!predicate() && timer.Elapsed < TimeSpan.FromSeconds(10))
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        Assert.True(predicate(), "Timed out waiting for reaction COMBINE UI.");
    }

    private static void RunSta(System.Action body)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "STA UI test did not complete.");
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
