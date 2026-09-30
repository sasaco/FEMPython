using FarPoint.Win.Spread;
using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using FrameWebforCS.providers;
using System.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ResultCombineFsecTests
{
    private static ResultCombineFsecSnapshot Snapshot(int dimension = 2,
        ImmutableArray<DefineDisgSnapshot> definitions = default,
        ImmutableArray<CombineDisgSnapshot> combinations = default)
    {
        var first = new FsecCaseSnapshot("1", ImmutableArray.Create(
            new FsecRowSnapshot("10", "10", "1", 0,
                new FsecVector(2, 4, 1, 0, 0, 6)),
            new FsecRowSnapshot("10", "", "2", 2,
                new FsecVector(3, 5, 2, 0, 0, 7))));
        var second = new FsecCaseSnapshot("2", ImmutableArray.Create(
            new FsecRowSnapshot("10", "10", "1", 0,
                new FsecVector(-5, 1, 2, 0, 0, -8)),
            new FsecRowSnapshot("10", "", "2", 2,
                new FsecVector(4, -2, 2, 0, 0, 9))));
        return new ResultCombineFsecSnapshot(1, dimension,
            ImmutableArray.Create(first, second),
            definitions.IsDefault ? ImmutableArray.Create(
                new DefineDisgSnapshot("12", ImmutableArray.Create(1, -2))) : definitions,
            combinations.IsDefault ? ImmutableArray.Create(
                new CombineDisgSnapshot("40", "main",
                    ImmutableArray.Create(new CombineDisgTerm(12, 2.5)))) : combinations,
            ImmutableArray.Create(1, 2));
    }

    [Fact]
    public void DefineSelectsWholeStationVectorAndCombineKeepsLegacySourceLabels()
    {
        var result = ResultCombineFsecAggregator.Calculate(Snapshot());
        var combination = Assert.Single(result.Cases);
        Assert.Equal("40", combination.Id);
        Assert.Equal("main", combination.Name);
        Assert.Equal(6, combination.Rows.Count);
        var max = combination.Rows["fx_max"];
        Assert.Equal(2, max.Count);
        Assert.Equal(12.5, max[0].Fx);
        Assert.Equal(-2.5, max[0].Fy); // The entire -case 2 row wins.
        Assert.Equal("--2", max[0].Case); // worker1 prefixes a signed source number.
        Assert.Equal("10", max[0].MemberDisplay);
        Assert.Equal("1", max[0].NodeId);
        Assert.Equal(2, max[1].Location);
        Assert.Equal("", max[1].MemberDisplay);
        Assert.Equal("+1", combination.Rows["fx_min"][0].Case);
    }

    [Fact]
    public void NegativeCombineCoefficientAndZeroCaseFollowWorkerLabels()
    {
        var negative = Snapshot(definitions: ImmutableArray.Create(
                new DefineDisgSnapshot("12", ImmutableArray.Create(-2))),
            combinations: ImmutableArray.Create(new CombineDisgSnapshot("40", null,
                ImmutableArray.Create(new CombineDisgTerm(12, -1)))));
        var row = ResultCombineFsecAggregator.Calculate(negative)
            .Cases[0].Rows["fx_max"][0];
        Assert.Equal(-5, row.Fx);
        Assert.Equal("--1", row.Case);

        var zero = Snapshot(definitions: ImmutableArray.Create(
            new DefineDisgSnapshot("12", ImmutableArray.Create(0))));
        var zeroRow = ResultCombineFsecAggregator.Calculate(zero)
            .Cases[0].Rows["fx_max"][0];
        Assert.Equal(0, zeroRow.Fx);
        Assert.Equal("", zeroRow.Case);
    }

    [Fact]
    public void ThreeDimensionalModesAndCancellationAreObserved()
    {
        var snapshot = Snapshot(dimension: 3);
        Assert.Equal(12, ResultCombineFsecAggregator.Calculate(snapshot).Cases[0].Rows.Count);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            ResultCombineFsecAggregator.Calculate(snapshot, cancellation.Token));
    }

    [Fact]
    public void InconsistentReferencedStationsAreRejected()
    {
        var snapshot = Snapshot();
        var mismatched = snapshot with
        {
            Forces = snapshot.Forces.SetItem(1, new FsecCaseSnapshot("2",
                ImmutableArray.Create(new FsecRowSnapshot("10", "10", "1", 0,
                    new FsecVector(1, 0, 0, 0, 0, 0)))))
        };
        Assert.Throws<InvalidOperationException>(() =>
            ResultCombineFsecAggregator.Calculate(mismatched));
    }

    [Fact]
    public void StaticCaseFallbackAndBudgetsAreBounded()
    {
        var snapshot = Snapshot(definitions: ImmutableArray<DefineDisgSnapshot>.Empty,
            combinations: ImmutableArray.Create(new CombineDisgSnapshot("5", null,
                ImmutableArray.Create(new CombineDisgTerm(2, 1)))));
        Assert.Equal(-5, ResultCombineFsecAggregator.Calculate(snapshot)
            .Cases[0].Rows["fx_max"][0].Fx);
        ResultCombineFsecAggregator.ValidateBudget(ResultCombineFsecAggregator.MaxRows,
            ResultCombineFsecAggregator.MaxScalarOperations,
            ResultCombineFsecAggregator.MaxOutputCells);
        Assert.Throws<InvalidOperationException>(() =>
            ResultCombineFsecAggregator.ValidateBudget(ResultCombineFsecAggregator.MaxRows + 1L, 0, 0));
        Assert.Throws<InvalidOperationException>(() =>
            ResultCombineFsecAggregator.ValidateBudget(0,
                ResultCombineFsecAggregator.MaxScalarOperations + 1, 0));
        Assert.Throws<InvalidOperationException>(() =>
            ResultCombineFsecAggregator.ValidateBudget(0, 0,
                ResultCombineFsecAggregator.MaxOutputCells + 1));
    }

    [Fact]
    public void LegacyResultServiceSnapshotAcceptsOptionalLength()
    {
        var coordinator = ResultCombineFsecCoordinator.Instance;
        try
        {
            using var document = JsonDocument.Parse("""
                {"dimension":2,"load":{"1":{"name":"first"}},
                 "combine":{"7":{"row":1,"C1":2}},
                 "result":{"1":{"disg":{"1":{"dx":1}},
                                  "fsec":{"1":{"0":{"fxi":2,"fyi":3,"mzi":4}}},
                                  "reac":{"1":{"tx":5,"ty":6,"mz":7}}}}}
                """);
            InputDataService.Instance.JsonDataOpen(document.RootElement);
            Assert.True(coordinator.State == CombineFsecState.Valid, coordinator.Error);
            Assert.NotNull(coordinator.Snapshot);
        }
        finally { coordinator.FailLoad(); }
    }

    [Fact]
    public void ViewportProjectionRetainsBothEnvelopeSidesForSelectedComponent()
    {
        var selected = new CombineFsecCaseResult("C1", null,
            new Dictionary<string, IReadOnlyList<CombineFsecRowResult>>
            {
                ["my_max"] = [new("member7", "7", "1", 2, 0, 0, 0, 0, 11, 0, "+1")],
                ["my_min"] = [new("member7", "7", "1", 2, 0, 0, 0, 0, -4, 0, "+2")]
            });

        var samples = ResultCombineFsecComponent.ProjectViewportSamples(selected, "my_min");

        Assert.Equal(2, samples.Count);
        Assert.Contains(samples, sample => sample.MemberId == 7 && sample.Value == 11 &&
            sample.Envelope == FrameWebforCS.three.SectionForceEnvelope.Max);
        Assert.Contains(samples, sample => sample.MemberId == 7 && sample.Value == -4 &&
            sample.Envelope == FrameWebforCS.three.SectionForceEnvelope.Min);
    }

    [Fact]
    public void BackgroundViewDropsStaleResultAndDisplaysModeGroups()
    {
        RunSta(() =>
        {
            using var started = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            LoadCase("7");
            ResultCombineFsecOutput Calculate(ResultCombineFsecSnapshot snapshot, CancellationToken _)
            {
                string id = Assert.Single(snapshot.Combines).Id;
                if (id == "7")
                {
                    started.Set();
                    if (!release.Wait(TimeSpan.FromSeconds(8)))
                        throw new TimeoutException("Old calculation was never released.");
                }
                return new ResultCombineFsecOutput(
                    [new CombineFsecCaseResult(id, null,
                        new Dictionary<string, IReadOnlyList<CombineFsecRowResult>>
                        {
                            ["fx_max"] = [new CombineFsecRowResult("10", "10", "1", 0,
                                double.Parse(id), 2, 0, 0, 0, 3, "+1")],
                            ["fx_min"] = [new CombineFsecRowResult("10", "10", "1", 0,
                                -double.Parse(id), 2, 0, 0, 0, 3, "+1")]
                        })]);
            }

            using var form = new Form();
            using var component = new ResultCombineFsecComponent(Calculate);
            form.Controls.Add(component);
            form.Show();
            var spread = (FpSpread)component.Controls.Find("fpSpread1", true).Single();
            Assert.Empty(component.Controls.Find("modeSelector", true));
            PumpUntil(() => started.IsSet);
            LoadCase("8");
            Assert.Equal(0, spread.Sheets.Count);
            release.Set();
            PumpUntil(() => spread.Sheets.Count == 1 &&
                spread.Sheets[0].SheetName == "8" && spread.Sheets[0].RowCount == 4);
            var sheet = spread.Sheets[0];
            Assert.Equal(8, sheet.ColumnCount);
            Assert.False(sheet.Columns[7].Visible);
            var grouped = Assert.IsType<ResultModeGroupDataModel>(sheet.Models.Data);
            Assert.True(grouped.IsGroup(0));
            Assert.False(grouped.IsGroup(1));
            Assert.True(grouped.IsGroup(2));
            Assert.False(grouped.IsGroup(3));
            Assert.Equal("fx_max", grouped.GetModeKey(0));
            Assert.Equal("fx_max", grouped.GetModeKey(1));
            Assert.Equal("fx_min", grouped.GetModeKey(2));
            Assert.Equal("fx_min", grouped.GetModeKey(3));
            Assert.Equal("8.00", sheet.Cells[1, 3].Text);
            Assert.Equal("-8.00", sheet.Cells[3, 3].Text);
            Assert.Equal("+1", sheet.Cells[1, 6].Text);
            Assert.Equal("+1", sheet.Cells[3, 6].Text);
            ResultCombineFsecCoordinator.Instance.FailLoad();
            Assert.Equal(0, spread.Sheets.Count);
        });
    }

    private static void LoadCase(string combinationId)
    {
        var coordinator = ResultCombineFsecCoordinator.Instance;
        coordinator.BeginLoad();
        using var combinations = JsonDocument.Parse(
            "{\"define\":{\"1\":{\"row\":1,\"C1\":1}},\"combine\":{\"" +
            combinationId + "\":{\"row\":1,\"C1\":1}}}");
        InputCombineService.Instance.setCombineJson(combinations.RootElement);
        using var force = JsonDocument.Parse("""
            {"result":{"1":{"fsec":{"10":{"P1":{"fxi":2,"fyi":3,"mzi":4,"L":2}}}}}}
            """);
        ResultFsecService.Instance.setFsecJson(force.RootElement);
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
        Assert.True(predicate(), "Timed out waiting for the section-force view.");
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
