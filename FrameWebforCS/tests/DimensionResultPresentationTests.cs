using FarPoint.Win.Spread;
using FrameWebforCS.components.result;
using FrameWebforCS.providers;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Windows.Forms;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class DimensionResultPresentationTests
{
    [Theory]
    [InlineData(2, 3)]
    [InlineData(3, 2)]
    public void LiveSwitchAndReloadKeepResultColumnsValuesAndProvenance(
        int resultDimension, int nextInputDimension)
    {
        RunSta(() =>
        {
            try
            {
                Open(ResultFile(resultDimension, 0.001));
                using var form = new Form();
                using var displacement = new ResultDisgComponent();
                using var reaction = new ResultReacComponent();
                using var section = new ResultFsecComponent();
                using var combinedDisplacement = new ResultCombineDisgComponent();
                using var combinedReaction = new ResultCombineReacComponent();
                using var combinedSection = new ResultCombineFsecComponent();
                using var pickedDisplacement = new ResultPickupDisgComponent();
                using var pickedReaction = new ResultPickupReacComponent();
                using var pickedSection = new ResultPickupFsecComponent();
                Control[] views = [displacement, reaction, section,
                    combinedDisplacement, combinedReaction, combinedSection,
                    pickedDisplacement, pickedReaction, pickedSection];
                foreach (Control view in views) form.Controls.Add(view);
                form.Show();

                PumpUntil(() => views.All(view => Spread(view).Sheets.Count == 1 &&
                    Spread(view).Sheets[0].RowCount > 0));
                AssertPresentation(views, resultDimension, mismatch: false);
                Assert.Equal("1.0000", Spread(displacement).Sheets[0].Cells[0, 1].Text);
                string combinedValue = Spread(combinedDisplacement).Sheets[0].Cells[1, 1].Text;
                string combinedSource = Spread(combinedDisplacement).Sheets[0].Cells[1,
                    resultDimension == 3 ? 7 : 4].Text;
                string pickedSource = Spread(pickedDisplacement).Sheets[0].Cells[1,
                    resultDimension == 3 ? 7 : 4].Text;
                Assert.False(string.IsNullOrWhiteSpace(combinedSource));
                Assert.False(string.IsNullOrWhiteSpace(pickedSource));
                var snapshot = ResultCombineDisgCoordinator.Instance.Snapshot;
                var reactionSnapshot = ResultCombineReacCoordinator.Instance.Snapshot;
                var sectionSnapshot = ResultCombineFsecCoordinator.Instance.Snapshot;

                InputDataService.Instance.SetDimension(nextInputDimension);
                Assert.Equal(resultDimension, InputDataService.Instance.ResultDimension);
                AssertPresentation(views, resultDimension, mismatch: true);
                Assert.Same(snapshot, ResultCombineDisgCoordinator.Instance.Snapshot);
                Assert.Same(reactionSnapshot, ResultCombineReacCoordinator.Instance.Snapshot);
                Assert.Same(sectionSnapshot, ResultCombineFsecCoordinator.Instance.Snapshot);
                Assert.Equal(combinedValue, Spread(combinedDisplacement).Sheets[0].Cells[1, 1].Text);
                Assert.Equal(combinedSource, Spread(combinedDisplacement).Sheets[0].Cells[1,
                    resultDimension == 3 ? 7 : 4].Text);
                Assert.Equal(pickedSource, Spread(pickedDisplacement).Sheets[0].Cells[1,
                    resultDimension == 3 ? 7 : 4].Text);

                string saved = JsonSerializer.Serialize(InputDataService.Instance.GetSaveJson());
                using (var savedDocument = JsonDocument.Parse(saved))
                {
                    Assert.Equal(nextInputDimension, savedDocument.RootElement
                        .GetProperty("dimension").GetInt32());
                    Assert.Equal(resultDimension, savedDocument.RootElement
                        .GetProperty("resultDimension").GetInt32());
                    InputDataService.Instance.JsonDataOpen(savedDocument.RootElement);
                }
                PumpUntil(() => views.All(view => Spread(view).Sheets.Count == 1 &&
                    Spread(view).Sheets[0].RowCount > 0));
                AssertPresentation(views, resultDimension, mismatch: true);
                Assert.Equal("1.0000", Spread(displacement).Sheets[0].Cells[0, 1].Text);
                Assert.Equal(combinedValue, Spread(combinedDisplacement).Sheets[0].Cells[1, 1].Text);
                Assert.Equal(combinedSource, Spread(combinedDisplacement).Sheets[0].Cells[1,
                    resultDimension == 3 ? 7 : 4].Text);
                Assert.Equal(pickedSource, Spread(pickedDisplacement).Sheets[0].Cells[1,
                    resultDimension == 3 ? 7 : 4].Text);

                Open(ResultFile(nextInputDimension, 0.005));
                PumpUntil(() => views.All(view => Spread(view).Sheets.Count == 1 &&
                    Spread(view).Sheets[0].RowCount > 0) &&
                    Spread(displacement).Sheets[0].Cells[0, 1].Text == "5.0000");
                AssertPresentation(views, nextInputDimension, mismatch: false);
                Assert.Equal(nextInputDimension,
                    ResultCombineDisgCoordinator.Instance.Snapshot?.Dimension);
                Assert.Equal(nextInputDimension,
                    ResultCombineReacCoordinator.Instance.Snapshot?.Dimension);
                Assert.Equal(nextInputDimension,
                    ResultCombineFsecCoordinator.Instance.Snapshot?.Dimension);

                Open("{\"dimension\":" + nextInputDimension + "}");
                Assert.Null(InputDataService.Instance.ResultDimension);
                Assert.All(views, view =>
                {
                    Assert.Empty(Spread(view).Sheets);
                    Assert.False(Notice(view).Visible);
                });
            }
            finally
            {
                using var empty = JsonDocument.Parse("""{"dimension":3}""");
                InputDataService.Instance.JsonDataOpen(empty.RootElement);
            }
        });
    }

    private static void AssertPresentation(Control[] views, int resultDimension, bool mismatch)
    {
        int[] threeDColumns = [7, 7, 9, 9, 9, 11, 9, 9, 11];
        int[] twoDColumns = [4, 4, 6, 6, 6, 8, 6, 6, 8];
        for (int index = 0; index < views.Length; index++)
        {
            Assert.Equal((resultDimension == 3 ? threeDColumns : twoDColumns)[index],
                Spread(views[index]).Sheets[0].ColumnCount);
            Assert.Equal(mismatch, Notice(views[index]).Visible);
            if (mismatch)
            {
                Assert.Contains($"結果: {resultDimension}D", Notice(views[index]).Text);
                Assert.Contains($"入力: {InputDataService.Instance.dimension}D",
                    Notice(views[index]).Text);
            }
        }
    }

    private static string ResultFile(int dimension, double dx) => """
        {"dimension":DIMENSION,
         "member":{"1":{"ni":"1","nj":"2"}},
         "load":{"1":{"name":"First"}},
         "define":{"5":{"row":1,"C1":1}},
         "combine":{"7":{"row":1,"name":"Combo","C5":2}},
         "pickup":{"9":{"row":1,"name":"Pick","C1":7}},
         "result":{"1":{"disg":{"1":{"dx":DX,
                                         "dy":0.002,"dz":0.003,"rx":0.004,"ry":0.005,"rz":0.006}},
                        "reac":{"1":{"tx":1,"ty":2,"tz":3,"mx":4,"my":5,"mz":6}},
                        "fsec":{"1":{"P1":{"fxi":1,"fyi":2,"fzi":3,
                                               "mxi":4,"myi":5,"mzi":6,
                                               "fxj":7,"fyj":8,"fzj":9,
                                               "mxj":10,"myj":11,"mzj":12,"L":1}}}}}}
        """.Replace("DIMENSION", dimension.ToString(System.Globalization.CultureInfo.InvariantCulture),
            StringComparison.Ordinal).Replace("DX", dx.ToString(System.Globalization.CultureInfo.InvariantCulture),
            StringComparison.Ordinal);

    private static void Open(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputDataService.Instance.JsonDataOpen(document.RootElement);
    }

    private static FpSpread Spread(Control view) =>
        (FpSpread)view.Controls.Find("fpSpread1", true).Single();

    private static Label Notice(Control view) =>
        (Label)view.Controls.Find("resultDimensionNotice", true).Single();

    private static void PumpUntil(Func<bool> predicate)
    {
        var timer = Stopwatch.StartNew();
        while (!predicate() && timer.Elapsed < TimeSpan.FromSeconds(10))
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        Assert.True(predicate(), "Timed out waiting for result tables to publish.");
    }

    private static void RunSta(System.Action body)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception exception) { error = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "STA result test did not complete.");
        if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}
