using FrameWebforCS.components.result;
using FarPoint.Win.Spread;
using FrameWebforCS.components.input;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Forms;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ResultPickupTests
{
    [Fact]
    public void DisplacementSelectsWholeVectorAndPreservesFirstCaseOnTie()
    {
        var combined = new ResultCombineDisgOutput(
        [
            new CombineDisgCaseResult("7", "First", new Dictionary<string, IReadOnlyList<CombineDisgNodeResult>>
            {
                ["dx_max"] = [new("11", 2, 101, 0, 0, 0, 0, "+5")],
                ["dx_min"] = [new("11", -3, 101, 0, 0, 0, 0, "+5")]
            }),
            new CombineDisgCaseResult("8", "Second", new Dictionary<string, IReadOnlyList<CombineDisgNodeResult>>
            {
                ["dx_max"] = [new("11", 2, 999, 0, 0, 0, 0, "+6")],
                ["dx_min"] = [new("11", -4, 999, 0, 0, 0, 0, "+6")]
            })
        ]);
        var pickups = ImmutableArray.Create(new PickupSelection("9", "Envelope", [7, 8]));

        PickupTableOutput result = ResultPickupDisgAggregator.SelectCombined(combined, 2, pickups);

        Assert.Single(result.Cases);
        Assert.Equal("2.0000", result.Cases[0].Rows["dx_max"][0][1]);
        Assert.Equal("101.0000", result.Cases[0].Rows["dx_max"][0][2]);
        Assert.Equal("+5", result.Cases[0].Rows["dx_max"][0][4]);
        Assert.Equal("-4.0000", result.Cases[0].Rows["dx_min"][0][1]);
        Assert.Equal("999.0000", result.Cases[0].Rows["dx_min"][0][2]);
        Assert.Equal("+6", result.Cases[0].Rows["dx_min"][0][4]);
    }

    [Fact]
    public void ReactionSelectsEachNodeIndependently()
    {
        var combined = new ResultCombineReacOutput(
        [
            new CombineReacCaseResult("7", null, new Dictionary<string, IReadOnlyList<CombineReacNodeResult>>
            {
                ["tx_max"] = [new("1", 3, 10, 0, 0, 0, 0, "+5"),
                    new("2", 1, 20, 0, 0, 0, 0, "+5")]
            }),
            new CombineReacCaseResult("8", null, new Dictionary<string, IReadOnlyList<CombineReacNodeResult>>
            {
                ["tx_max"] = [new("1", 2, 30, 0, 0, 0, 0, "+6"),
                    new("2", 4, 40, 0, 0, 0, 0, "+6")]
            })
        ]);

        PickupTableOutput result = ResultPickupReacAggregator.SelectCombined(combined, 2,
            [new PickupSelection("9", null, [7, 8])]);

        Assert.Equal(6, result.Cases[0].Rows.Count);
        Assert.Equal(["1", "3.00", "10.00", "0.00", "+5"],
            result.Cases[0].Rows["tx_max"][0]);
        Assert.Equal(["2", "4.00", "40.00", "0.00", "+6"],
            result.Cases[0].Rows["tx_max"][1]);
    }

    [Fact]
    public void MemberForceSelectsByStationAndShowsCombinationProvenance()
    {
        var combined = new ResultCombineFsecOutput(
        [
            new CombineFsecCaseResult("7", null, new Dictionary<string, IReadOnlyList<CombineFsecRowResult>>
            {
                ["fy_max"] = [new("1", "1", "11", 0, 1, 2, 0, 0, 0, 9, "+5"),
                    new("1", "", "", 2, 1, 5, 0, 0, 0, 9, "+5")]
            }),
            new CombineFsecCaseResult("8", null, new Dictionary<string, IReadOnlyList<CombineFsecRowResult>>
            {
                ["fy_max"] = [new("1", "1", "11", 0, 1, 4, 0, 0, 0, 8, "+6"),
                    new("1", "", "", 2, 1, 3, 0, 0, 0, 8, "+6")]
            })
        ]);

        PickupTableOutput result = ResultPickupFsecAggregator.SelectCombined(combined, 2,
            [new PickupSelection("9", null, [7, 8])]);

        Assert.Equal("4.00", result.Cases[0].Rows["fy_max"][0][4]);
        Assert.Equal("8:+6", result.Cases[0].Rows["fy_max"][0][6]);
        Assert.Equal("5.00", result.Cases[0].Rows["fy_max"][1][4]);
        Assert.Equal("7:+5", result.Cases[0].Rows["fy_max"][1][6]);
    }

    [Fact]
    public void InvalidOrMissingReferencesDoNotCreatePhantomPickupCases()
    {
        var combined = new ResultCombineDisgOutput([]);
        PickupTableOutput result = ResultPickupDisgAggregator.SelectCombined(combined, 2,
            [new PickupSelection("9", null, [999])]);
        Assert.Empty(result.Cases);
    }

    [Fact]
    public void LateOldCalculationCannotReplaceNewPickupView()
    {
        RunSta(() =>
        {
            var source = new FakeSource();
            source.Set(1);
            using var oldStarted = new ManualResetEventSlim();
            using var releaseOld = new ManualResetEventSlim();
            using var form = new Form();
            using var view = new FakePickupComponent(source, (snapshot, _, _) =>
            {
                if (snapshot.Id == 1)
                {
                    oldStarted.Set();
                    if (!releaseOld.Wait(TimeSpan.FromSeconds(8)))
                        throw new TimeoutException("Old PICKUP calculation was never released.");
                }
                return new PickupTableOutput(2,
                    [new PickupTableCase(snapshot.Id.ToString(), null,
                        new Dictionary<string, IReadOnlyList<string[]>>
                        {
                            ["dx_max"] = [["11", snapshot.Id.ToString()]]
                        })]);
            });
            form.Controls.Add(view);
            form.Show();
            var spread = (FpSpread)view.Controls.Find("fpSpread1", true).Single();
            PumpUntil(() => oldStarted.IsSet);
            source.Set(2);
            releaseOld.Set();
            PumpUntil(() => spread.Sheets.Count == 1 &&
                spread.Sheets[0].SheetName == "2" && spread.Sheets[0].RowCount == 1);
            Assert.Equal("2", spread.Sheets[0].Cells[0, 1].Text);
            source.Clear();
            Assert.Empty(spread.Sheets.Cast<SheetView>());
        });
    }

    [Fact]
    public void LoadedPickupRowsDriveDisplacementView()
    {
        RunSta(() =>
        {
            var coordinator = ResultCombineDisgCoordinator.Instance;
            coordinator.BeginLoad();
            using var definitions = JsonDocument.Parse("""
                {"define":{"5":{"row":1,"C1":1},"6":{"row":2,"C2":2}},
                 "combine":{"7":{"row":1,"C5":1},"8":{"row":2,"C6":1}},
                 "pickup":{"9":{"row":1,"name":"Envelope","C1":7,"C2":8}}}
                """);
            InputCombineService.Instance.setCombineJson(definitions.RootElement);
            using var displacements = JsonDocument.Parse("""
                {"result":{"1":{"disg":{"11":{"dx":1,"dy":10,"rz":0}}},
                           "2":{"disg":{"11":{"dx":3,"dy":30,"rz":0}}}}}
                """);
            ResultDisgService.Instance.setDisgJson(displacements.RootElement);
            coordinator.CompleteLoad(2);
            using var form = new Form();
            using var view = new ResultPickupDisgComponent();
            form.Controls.Add(view);
            form.Show();
            var spread = (FpSpread)view.Controls.Find("fpSpread1", true).Single();
            PumpUntil(() => spread.Sheets.Count == 1 && spread.Sheets[0].RowCount == 1);
            Assert.Equal("9 Envelope", spread.Sheets[0].SheetName);
            Assert.Equal("3000.0000", spread.Sheets[0].Cells[0, 1].Text);
            Assert.Equal("30000.0000", spread.Sheets[0].Cells[0, 2].Text);
            Assert.Equal("+6", spread.Sheets[0].Cells[0, 4].Text);
            coordinator.FailLoad();
            Assert.Equal(0, spread.Sheets.Count);
        });
    }

    private sealed record FakeSnapshot(int Id);

    private sealed class FakeSource
    {
        public FakeSnapshot? Snapshot { get; private set; }
        public long Revision { get; private set; }
        public event EventHandler? Changed;
        public void Set(int id)
        {
            Snapshot = new FakeSnapshot(id);
            Revision++;
            Changed?.Invoke(this, EventArgs.Empty);
        }
        public void Clear()
        {
            Snapshot = null;
            Revision++;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class FakePickupComponent : ResultPickupTableComponent<FakeSnapshot>
    {
        public FakePickupComponent(FakeSource source,
            Func<FakeSnapshot, ImmutableArray<PickupSelection>, CancellationToken, PickupTableOutput> calculate)
            : base(() => source.Snapshot, () => source.Revision,
                handler => source.Changed += handler,
                handler => source.Changed -= handler,
                calculate,
                [("dx_max", "X 最大")], [("dx_max", "X 最大")],
                ["節点", "値"], ["節点", "値"])
        { }
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var timer = Stopwatch.StartNew();
        while (!condition() && timer.Elapsed < TimeSpan.FromSeconds(10))
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        Assert.True(condition(), "Timed out waiting for PICKUP view.");
    }

    private static void RunSta(System.Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "PICKUP UI test did not complete.");
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
