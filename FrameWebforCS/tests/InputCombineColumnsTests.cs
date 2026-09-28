using FarPoint.Win.Spread;
using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class InputCombineColumnsTests
{
    [Fact]
    public void EffectiveLoadMaximumUsesCaseIdsAndIgnoresIncompleteRows()
    {
        RunSta(() =>
        {
            var load = InputLoadService.Instance;
            try
            {
                SetLoad("""
                    {"load":{"2":{"load_node":[{"row":1,"n":"17","tx":0}]},
                             "4":{"LL_pitch":2.5},
                             "6":{"load_node":[{"row":1,"n":"18"}]},
                             "8":{"load_node":[{"row":1,"n":"bad","ty":3}]},
                             "9":{"name":"named"},
                             "12":{"load_member":[{"row":1,"m1":"3","direction":"x"}]}}}
                    """);
                Assert.Equal(12, load.MaximumEffectiveCaseId);

                SetLoad("""
                    {"load":{"2":{"load_node":[{"row":1,"n":"17","tx":0}]},
                             "4":{"LL_pitch":2.5},
                             "6":{"load_node":[{"row":1,"n":"18"}]},
                             "8":{"load_node":[{"row":1,"n":"bad","ty":3}]},
                             "9":{"name":"named"}}}
                    """);
                Assert.Equal(9, load.MaximumEffectiveCaseId);

                SetLoad("""
                    {"load":{"4":{"LL_pitch":2.5},
                             "6":{"load_node":[{"row":1,"n":"18"}]},
                             "8":{"load_node":[{"row":1,"n":"bad","ty":3}]}}}
                    """);
                Assert.Equal(0, load.MaximumEffectiveCaseId);
            }
            finally { load.clear(); }
        });
    }

    [Fact]
    public void DefineAndLoadChangesRefreshVisibleCombineColumnsAndNames()
    {
        RunSta(() =>
        {
            var combine = InputCombineService.Instance;
            var load = InputLoadService.Instance;
            try
            {
                combine.clear();
                SetLoad("""{"load":{"2":{"name":"first"},"9":{"symbol":"L"}}}""");
                SetCombine("""
                    {"define":{"7":{"row":7,"name":"label only"}},
                     "combine":{"3":{"row":1,"name":"third","C9":1.5}}}
                    """);
                using var component = new InputCombineComponent();
                var sheet = CombineSheet(component);
                AssertColumns(sheet, "C", 9);
                Assert.Equal("third", sheet.Cells[0, 9].Text);
                Assert.Equal(1.5f, Convert.ToSingle(sheet.Cells[0, 8].Value));

                EditCell(component, 0, 0, 0, 1);
                AssertColumns(sheet, "D", 5);
                Assert.Equal("third", sheet.Cells[0, 5].Text);

                EditCell(component, 0, 6, 0, 2);
                AssertColumns(sheet, "D", 7);
                Assert.Equal("third", sheet.Cells[0, 7].Text);

                EditCell(component, 0, 6, 0, null);
                AssertColumns(sheet, "D", 5);
                EditCell(component, 0, 0, 0, null);
                AssertColumns(sheet, "C", 9);
                Assert.Equal("third", sheet.Cells[0, 9].Text);
                Assert.Equal(1.5f, Convert.ToSingle(sheet.Cells[0, 8].Value));

                SetLoad("""{"load":{"12":{"name":"later"}}}""");
                AssertColumns(sheet, "C", 12);
                Assert.Equal("third", sheet.Cells[0, 12].Text);

                load.clear();
                AssertColumns(sheet, "C", 5);
                Assert.Equal("third", sheet.Cells[0, 5].Text);
            }
            finally
            {
                combine.clear();
                load.clear();
            }
        });
    }

    [Fact]
    public void FiftyColumnLimitWarnsAndRetainsHiddenCoefficientThroughSaveAndReload()
    {
        RunSta(() =>
        {
            var combine = InputCombineService.Instance;
            var load = InputLoadService.Instance;
            try
            {
                combine.clear();
                SetLoad("""{"load":{"50":{"name":"at limit"}}}""");
                using var form = new Form();
                using var component = new InputCombineComponent();
                form.Controls.Add(component);
                form.Show();
                var sheet = CombineSheet(component);
                AssertColumns(sheet, "C", 50);

                SetLoad("""{"load":{"51":{"name":"over limit"}}}""");
                AssertColumns(sheet, "C", 50);
                var warning = component.Controls.OfType<Label>().Single();
                Assert.True(warning.Visible);
                Assert.Contains("50", warning.Text);

                combine.SetCombineCoefficient(1, 51, 7.25f);
                combine.SetCombineName(1, "hidden value");
                string saved = JsonSerializer.Serialize(InputDataService.Instance.GetSaveJson());
                using var document = JsonDocument.Parse(saved);
                Assert.Equal(7.25f, document.RootElement.GetProperty("combine")
                    .GetProperty("1").GetProperty("C51").GetSingle());

                combine.clear();
                combine.setCombineJson(document.RootElement);
                Assert.Equal(7.25f, combine.CombineRows[1].Coefficients[51]);
                Assert.Equal("hidden value", sheet.Cells[0, 50].Text);

                SetLoad("""{"load":{"12":{"name":"under limit"}}}""");
                AssertColumns(sheet, "C", 12);
                Assert.False(warning.Visible);
                Assert.Equal(7.25f, combine.CombineRows[1].Coefficients[51]);
            }
            finally
            {
                combine.clear();
                load.clear();
            }
        });
    }

    [Fact]
    public void EditingFallbackColumnTwelveAndNameSurvivesSaveAndReload()
    {
        RunSta(() =>
        {
            var combine = InputCombineService.Instance;
            var load = InputLoadService.Instance;
            try
            {
                combine.clear();
                SetLoad("""{"load":{"12":{"name":"sparse"}}}""");
                using var component = new InputCombineComponent();
                var sheet = CombineSheet(component);
                AssertColumns(sheet, "C", 12);

                EditCell(component, 1, 0, 11, 3.75f);
                EditCell(component, 1, 0, 12, "display name");
                Assert.Equal(3.75f, combine.CombineRows[1].Coefficients[12]);
                Assert.Equal("display name", combine.CombineRows[1].name);

                string saved = JsonSerializer.Serialize(InputDataService.Instance.GetSaveJson());
                using var document = JsonDocument.Parse(saved);
                Assert.Equal(3.75f, document.RootElement.GetProperty("combine")
                    .GetProperty("1").GetProperty("C12").GetSingle());
                combine.clear();
                combine.setCombineJson(document.RootElement);
                Assert.Equal(3.75f, Convert.ToSingle(sheet.Cells[0, 11].Value));
                Assert.Equal("display name", sheet.Cells[0, 12].Text);

                EditCell(component, 1, 0, 11, null);
                Assert.False(combine.CombineRows[1].Coefficients.ContainsKey(12));
                Assert.Equal("display name", combine.CombineRows[1].name);
            }
            finally
            {
                combine.clear();
                load.clear();
            }
        });
    }

    [Fact]
    public void DisposedViewUnsubscribesFromDataChanges()
    {
        RunSta(() =>
        {
            var combine = InputCombineService.Instance;
            var load = InputLoadService.Instance;
            int beforeCombine = SubscriberCount(combine, "RowsReplaced");
            int beforeLoad = SubscriberCount(load, "CasesChanged");
            var component = new InputCombineComponent();
            Assert.Equal(beforeCombine + 1, SubscriberCount(combine, "RowsReplaced"));
            Assert.Equal(beforeLoad + 1, SubscriberCount(load, "CasesChanged"));
            component.Dispose();
            Assert.Equal(beforeCombine, SubscriberCount(combine, "RowsReplaced"));
            Assert.Equal(beforeLoad, SubscriberCount(load, "CasesChanged"));
        });
    }

    private static int SubscriberCount(object service, string eventName) =>
        ((MulticastDelegate?)service.GetType().GetField(eventName,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service))?
            .GetInvocationList().Length ?? 0;

    private static SheetView CombineSheet(InputCombineComponent component) =>
        ((FpSpread)component.Controls.Find("fpSpread1", true).Single()).Sheets[1];

    private static void EditCell(InputCombineComponent component, int sheetIndex,
        int row, int column, object? value)
    {
        var spread = (FpSpread)component.Controls.Find("fpSpread1", true).Single();
        spread.ActiveSheetIndex = sheetIndex;
        spread.Sheets[sheetIndex].Cells[row, column].Value = value;
        var change = new ChangeEventArgs(spread.GetRootWorkbook(), row, column);
        typeof(InputCombineComponent).GetMethod("OnChange",
            BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(component, [spread, change]);
    }

    private static void AssertColumns(SheetView sheet, string prefix, int coefficientCount)
    {
        Assert.Equal(coefficientCount + 1, sheet.ColumnCount);
        Assert.Equal(prefix + "1", sheet.ColumnHeader.Cells[0, 0].Text);
        Assert.Equal(prefix + coefficientCount, sheet.ColumnHeader.Cells[0, coefficientCount - 1].Text);
        Assert.Equal("名称", sheet.ColumnHeader.Cells[0, coefficientCount].Text);
    }

    private static void SetLoad(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputLoadService.Instance.setLoadJson(document.RootElement);
    }

    private static void SetCombine(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputCombineService.Instance.setCombineJson(document.RootElement);
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
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA COMBINE test timed out.");
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
