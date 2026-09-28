using FarPoint.Win.Spread;
using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Forms;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ResultCombineDisgUiTests
{
    [Fact]
    public void CombineDisplaysSelectedSheetAndModeOnly()
    {
        RunSta(() =>
        {
            LoadTwoDimensionalResults();
            using var form = new Form();
            using var component = new ResultCombineDisgComponent();
            form.Controls.Add(component);
            form.Show();

            var spread = (FpSpread)component.Controls.Find("fpSpread1", true).Single();
            var modes = (ComboBox)component.Controls.Find("modeSelector", true).Single();
            var status = (Label)component.Controls.Find("statusLabel", true).Single();
            PumpUntil(() => spread.Sheets.Count == 2 && spread.Sheets[0].RowCount == 1);

            Assert.Equal("7 Main", spread.Sheets[0].SheetName);
            Assert.Equal("8 Second", spread.Sheets[1].SheetName);
            Assert.Equal(0, spread.ActiveSheetIndex);
            Assert.Equal(5, spread.Sheets[0].ColumnCount);
            Assert.Equal(0, spread.Sheets[1].RowCount);
            Assert.Equal("2500.0000", spread.Sheets[0].Cells[0, 1].Text);
            Assert.Equal("+5", spread.Sheets[0].Cells[0, 4].Text);
            Assert.Equal(6, modes.Items.Count);
            Assert.Contains("2 件", status.Text);

            component.setActiveSheet(1); // Sidebar route category must not select sheet 1.
            Assert.Equal(0, spread.ActiveSheetIndex);
            spread.ActiveSheetIndex = 1;
            Assert.Equal(0, spread.Sheets[0].RowCount);
            Assert.Equal(1, spread.Sheets[1].RowCount);
            Assert.Equal("5000.0000", spread.Sheets[1].Cells[0, 1].Text);

            modes.SelectedIndex = 1;
            Assert.Equal("-4000.0000", spread.Sheets[1].Cells[0, 1].Text);
            Assert.Equal(0, spread.Sheets[0].RowCount);

            ResultCombineDisgCoordinator.Instance.FailLoad();
            Assert.Equal(0, spread.Sheets.Count);
            Assert.Contains("表示できません", status.Text);

            var coordinator = ResultCombineDisgCoordinator.Instance;
            coordinator.BeginLoad();
            using var emptyCombinations = JsonDocument.Parse("""{"define":{},"combine":{}}""");
            using var emptyResults = JsonDocument.Parse("""{"result":{}}""");
            InputCombineService.Instance.setCombineJson(emptyCombinations.RootElement);
            ResultDisgService.Instance.setDisgJson(emptyResults.RootElement);
            coordinator.CompleteLoad(2);
            PumpUntil(() => status.Text == "組合せ結果がありません。");
            Assert.Equal(0, spread.Sheets.Count);
        });
    }

    [Fact]
    public void ThreeDimensionalColumnsAndPickupRemainSeparate()
    {
        RunSta(() =>
        {
            LoadThreeDimensionalResults();
            using var form = new Form();
            using var combine = new ResultCombineDisgComponent();
            form.Controls.Add(combine);
            form.Show();
            var spread = (FpSpread)combine.Controls.Find("fpSpread1", true).Single();
            var modes = (ComboBox)combine.Controls.Find("modeSelector", true).Single();
            PumpUntil(() => spread.Sheets.Count == 1 && spread.Sheets[0].RowCount == 1);

            Assert.Equal(8, spread.Sheets[0].ColumnCount);
            Assert.Equal(12, modes.Items.Count);
            Assert.Equal("375.0000", spread.Sheets[0].Cells[0, 3].Text);
            Assert.Equal("750.0000", spread.Sheets[0].Cells[0, 6].Text);

            using var pickup = new ResultPickupDisgComponent();
            form.Controls.Add(pickup);
            var pickupSpread = (FpSpread)pickup.Controls.Find("fpSpread1", true).Single();
            var pickupModes = (ComboBox)pickup.Controls.Find("modeSelector", true).Single();
            var pickupStatus = (Label)pickup.Controls.Find("statusLabel", true).Single();
            try
            {
                PumpUntil(() => pickupSpread.Sheets.Count == 1 && pickupSpread.Sheets[0].RowCount == 1);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"PICKUP sheet count: {pickupSpread.Sheets.Count}; status: {pickupStatus.Text}", exception);
            }
            Assert.Equal(8, pickupSpread.Sheets[0].ColumnCount);
            Assert.Equal(12, pickupModes.Items.Count);
            Assert.Equal("1875.0000", pickupSpread.Sheets[0].Cells[0, 1].Text);
            pickup.setActiveSheet(2);
            Assert.Equal(0, pickupSpread.ActiveSheetIndex);
            ResultCombineDisgCoordinator.Instance.FailLoad();
            Assert.Equal(0, pickupSpread.Sheets.Count);
        });
    }

    [Fact]
    public void SlowOldCalculationCannotPublishAfterReload()
    {
        RunSta(() =>
        {
            using var oldStarted = new ManualResetEventSlim();
            using var releaseOld = new ManualResetEventSlim();
            using var newStarted = new ManualResetEventSlim();
            int activeCalculations = 0;
            int peakCalculations = 0;
            LoadControlledCase("7", "A");

            ResultCombineDisgOutput Calculate(
                ResultCombineDisgSnapshot snapshot, CancellationToken _)
            {
                int active = Interlocked.Increment(ref activeCalculations);
                peakCalculations = Math.Max(peakCalculations, active);
                try
                {
                    string id = snapshot.Combines.Single().Id;
                    if (id == "7")
                    {
                        oldStarted.Set();
                        if (!releaseOld.Wait(TimeSpan.FromSeconds(8)))
                            throw new TimeoutException("Old calculation was never released.");
                    }
                    else newStarted.Set();
                    return ControlledOutput(id);
                }
                finally { Interlocked.Decrement(ref activeCalculations); }
            }

            using var form = new Form();
            using var component = new ResultCombineDisgComponent(Calculate);
            form.Controls.Add(component);
            form.Show();
            var spread = (FpSpread)component.Controls.Find("fpSpread1", true).Single();
            PumpUntil(() => oldStarted.IsSet);

            LoadControlledCase("8", "B");
            Assert.Equal(0, spread.Sheets.Count);
            releaseOld.Set();
            PumpUntil(() => newStarted.IsSet && spread.Sheets.Count == 1 &&
                spread.Sheets[0].SheetName == "8 B" && spread.Sheets[0].RowCount == 1);

            Assert.Equal("8 B", spread.Sheets[0].SheetName);
            Assert.Equal("8.0000", spread.Sheets[0].Cells[0, 1].Text);
            Assert.Equal(1, peakCalculations);
            ResultCombineDisgCoordinator.Instance.FailLoad();
        });
    }

    [Fact]
    public void DisposingDuringCalculationSuppressesLatePublication()
    {
        RunSta(() =>
        {
            using var started = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            using var completed = new ManualResetEventSlim();
            LoadControlledCase("7", "A");

            ResultCombineDisgOutput Calculate(
                ResultCombineDisgSnapshot snapshot, CancellationToken _)
            {
                started.Set();
                try
                {
                    if (!release.Wait(TimeSpan.FromSeconds(8)))
                        throw new TimeoutException("Calculation was never released.");
                    return ControlledOutput(snapshot.Combines.Single().Id);
                }
                finally { completed.Set(); }
            }

            using var form = new Form();
            var component = new ResultCombineDisgComponent(Calculate);
            form.Controls.Add(component);
            form.Show();
            PumpUntil(() => started.IsSet);
            Task inFlight = Assert.IsAssignableFrom<Task>(component.CurrentCalculationTask);

            component.Dispose();
            LoadControlledCase("8", "B");
            release.Set();
            PumpUntil(() => completed.IsSet && inFlight.IsCompleted);
            Application.DoEvents();
            Assert.True(component.IsDisposed);
            Assert.False(inFlight.IsFaulted);
            ResultCombineDisgCoordinator.Instance.FailLoad();
        });
    }

    [Fact]
    public void RecreatedHandleWaitsForNonCooperativeOldCalculation()
    {
        RunSta(() =>
        {
            using var oldStarted = new ManualResetEventSlim();
            using var releaseOld = new ManualResetEventSlim();
            using var newStarted = new ManualResetEventSlim();
            int active = 0;
            int peak = 0;
            LoadControlledCase("7", "A");

            ResultCombineDisgOutput Calculate(
                ResultCombineDisgSnapshot snapshot, CancellationToken _)
            {
                int count = Interlocked.Increment(ref active);
                peak = Math.Max(peak, count);
                try
                {
                    string id = snapshot.Combines.Single().Id;
                    if (id == "7")
                    {
                        oldStarted.Set();
                        if (!releaseOld.Wait(TimeSpan.FromSeconds(8)))
                            throw new TimeoutException("Old calculation was never released.");
                    }
                    else newStarted.Set();
                    return ControlledOutput(id);
                }
                finally { Interlocked.Decrement(ref active); }
            }

            using var form = new Form();
            using var component = new RecreatingCombineComponent(Calculate);
            form.Controls.Add(component);
            form.Show();
            var spread = (FpSpread)component.Controls.Find("fpSpread1", true).Single();
            PumpUntil(() => oldStarted.IsSet);

            component.DestroyViewHandle();
            Assert.False(component.IsHandleCreated);
            LoadControlledCase("8", "B");
            component.CreateViewHandle();
            Assert.True(component.IsHandleCreated);
            Assert.False(newStarted.IsSet);
            Assert.True(component.IsCalculationRunning);

            releaseOld.Set();
            PumpUntil(() => newStarted.IsSet && spread.Sheets.Count == 1 &&
                spread.Sheets[0].SheetName == "8 B" && spread.Sheets[0].RowCount == 1);
            Assert.Equal("8.0000", spread.Sheets[0].Cells[0, 1].Text);
            Assert.Equal(1, peak);
            ResultCombineDisgCoordinator.Instance.FailLoad();
        });
    }

    [Fact]
    public void WorkFinishingWithoutViewHandleResumesAfterRecreation()
    {
        RunSta(() =>
        {
            using var oldStarted = new ManualResetEventSlim();
            using var releaseOld = new ManualResetEventSlim();
            using var newStarted = new ManualResetEventSlim();
            LoadControlledCase("7", "A");

            ResultCombineDisgOutput Calculate(
                ResultCombineDisgSnapshot snapshot, CancellationToken _)
            {
                string id = snapshot.Combines.Single().Id;
                if (id == "7")
                {
                    oldStarted.Set();
                    if (!releaseOld.Wait(TimeSpan.FromSeconds(8)))
                        throw new TimeoutException("Old calculation was never released.");
                }
                else newStarted.Set();
                return ControlledOutput(id);
            }

            using var form = new Form();
            using var component = new RecreatingCombineComponent(Calculate);
            form.Controls.Add(component);
            form.Show();
            var spread = (FpSpread)component.Controls.Find("fpSpread1", true).Single();
            PumpUntil(() => oldStarted.IsSet);

            component.DestroyViewHandle();
            LoadControlledCase("8", "B");
            releaseOld.Set();
            PumpUntil(() => !component.IsCalculationRunning);
            Assert.False(newStarted.IsSet);
            Assert.Equal(0, spread.Sheets.Count);

            component.CreateViewHandle();
            PumpUntil(() => newStarted.IsSet && spread.Sheets.Count == 1 &&
                spread.Sheets[0].SheetName == "8 B" && spread.Sheets[0].RowCount == 1);
            Assert.Equal("8.0000", spread.Sheets[0].Cells[0, 1].Text);
            ResultCombineDisgCoordinator.Instance.FailLoad();
        });
    }

    private static void LoadControlledCase(string id, string name)
    {
        var coordinator = ResultCombineDisgCoordinator.Instance;
        coordinator.BeginLoad();
        using var definitions = JsonDocument.Parse(
            "{\"define\":{\"5\":{\"row\":1,\"C1\":1}},\"combine\":{\"" + id +
            "\":{\"row\":1,\"name\":\"" + name + "\",\"C5\":1}}}");
        InputCombineService.Instance.setCombineJson(definitions.RootElement);
        using var displacements = JsonDocument.Parse(
            """{"result":{"1":{"disg":{"11":{"dx":1,"dy":2,"rz":3}}}}}""");
        ResultDisgService.Instance.setDisgJson(displacements.RootElement);
        coordinator.CompleteLoad(2);
    }

    private static ResultCombineDisgOutput ControlledOutput(string id) =>
        new([new CombineDisgCaseResult(id, id == "7" ? "A" : "B",
            new Dictionary<string, IReadOnlyList<CombineDisgNodeResult>>
            {
                ["dx_max"] = [new CombineDisgNodeResult("11", int.Parse(id), 0, 0, 0, 0, 0, "+5")]
            })]);

    private static void LoadTwoDimensionalResults()
    {
        var coordinator = ResultCombineDisgCoordinator.Instance;
        coordinator.BeginLoad();
        using var definitions = JsonDocument.Parse("""
            {"define":{"5":{"row":1,"C1":1,"C2":2}},
             "combine":{"7":{"row":1,"name":"Main","C5":2},
                        "8":{"row":2,"name":"Second","C5":4}}}
            """);
        InputCombineService.Instance.setCombineJson(definitions.RootElement);
        using var displacements = JsonDocument.Parse("""
            {"result":{"1":{"disg":{"11":{"dx":1.25,"dy":-2,"rz":3}}},
                       "2":{"disg":{"11":{"dx":-1,"dy":2,"rz":-3}}}}}
            """);
        ResultDisgService.Instance.setDisgJson(displacements.RootElement);
        coordinator.CompleteLoad(2);
    }

    private static void LoadThreeDimensionalResults()
    {
        var coordinator = ResultCombineDisgCoordinator.Instance;
        coordinator.BeginLoad();
        using var definitions = JsonDocument.Parse("""
            {"define":{"5":{"row":1,"C1":1}},
             "combine":{"7":{"row":1,"name":"Main","C5":1.5}},
             "pickup":{"9":{"row":1,"C1":7}}}
            """);
        InputCombineService.Instance.setCombineJson(definitions.RootElement);
        using var displacements = JsonDocument.Parse("""
            {"result":{"1":{"disg":{"11":{"dx":1.25,"dy":-2,"dz":0.25,
                                            "rx":0.125,"ry":-0.25,"rz":0.5}}}}}
            """);
        ResultDisgService.Instance.setDisgJson(displacements.RootElement);
        coordinator.CompleteLoad(3);
    }

    private static void PumpUntil(Func<bool> predicate)
    {
        var timer = Stopwatch.StartNew();
        while (!predicate() && timer.Elapsed < TimeSpan.FromSeconds(10))
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        Assert.True(predicate(), "Timed out waiting for the COMBINE UI to publish the result.");
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

    private sealed class RecreatingCombineComponent(
        Func<ResultCombineDisgSnapshot, CancellationToken, ResultCombineDisgOutput> calculate)
        : ResultCombineDisgComponent(calculate)
    {
        public void DestroyViewHandle() => DestroyHandle();
        public void CreateViewHandle() => CreateHandle();
    }
}
