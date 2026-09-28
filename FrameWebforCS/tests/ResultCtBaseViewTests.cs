using FarPoint.Win.Spread;
using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using FrameWebforCS.providers;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Windows.Forms;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ResultCtBaseViewTests
{
    [Fact]
    public void BaseViewsShowCtPresetValuesAndClearOnReload()
    {
        RunSta(() =>
        {
            try
            {
                using var file = File.OpenRead(FindCtPreset());
                using var document = JsonDocument.Parse(file);
                InputDataService.Instance.JsonDataOpen(document.RootElement);
                using var form = new Form();
                using var displacement = new ResultDisgComponent();
                using var reaction = new ResultReacComponent();
                using var section = new ResultFsecComponent();
                form.Controls.Add(displacement);
                form.Controls.Add(reaction);
                form.Controls.Add(section);
                form.Show();

                SheetView d = Sheet(displacement);
                SheetView r = Sheet(reaction);
                SheetView f = Sheet(section);
                Assert.Equal(11, Spread(displacement).Sheets.Count);
                Assert.Equal(11, Spread(reaction).Sheets.Count);
                Assert.Equal(11, Spread(section).Sheets.Count);
                Assert.Equal(63, d.RowCount);
                Assert.Equal(6, r.RowCount);
                Assert.Equal(962, f.RowCount);
                Assert.Equal("1", d.Cells[0, 0].Text);
                Assert.Equal("0.5005", d.Cells[0, 3].Text);
                Assert.Equal("-0.0373", d.Cells[0, 4].Text);
                Assert.Equal("1", r.Cells[0, 0].Text);
                Assert.Equal("-575.56", r.Cells[0, 3].Text);
                Assert.Equal("1", f.Cells[0, 0].Text);
                Assert.Equal("0.000", f.Cells[0, 2].Text);
                Assert.Equal("-489.44", f.Cells[0, 5].Text);
                Assert.Equal("0.500", f.Cells[1, 2].Text);
                Assert.Equal("-454.78", f.Cells[1, 5].Text);

                ResultDisgService.Instance.clear();
                ResultReacService.Instance.clear();
                ResultFsecService.Instance.clear();
                Assert.Empty(Spread(displacement).Sheets);
                Assert.Empty(Spread(reaction).Sheets);
                Assert.Empty(Spread(section).Sheets);
            }
            finally { ClearResults(); }
        });
    }

    [Fact]
    public void BaseViewsFilterHelpersAndShowTwoDimensionalColumnsAfterReload()
    {
        RunSta(() =>
        {
            try
            {
                InputDataService.Instance.dimension = 3;
                InputMembersService.Instance.clear();
                using var members = JsonDocument.Parse("""{"member":{"1":{"ni":"10","nj":"11"}}}""");
                InputMembersService.Instance.setMemberJson(members.RootElement);
                LoadResults("""
                    {"result":{"1":{"disg":{"node10":{"dx":0.00125,"rz":-0.0005},
                                               "10n1":{"dx":99},"10l1":{"dx":99}},
                                    "reac":{"10":{"tx":1.235,"tz":2,"mz":-1.235}},
                                    "fsec":{"1":{"P1":{"fxi":1.235,"fxj":2.345,"L":0.5},
                                                   "P2":{"fxi":99,"fxj":3.455,"L":0.25}}}}}}
                    """);
                using var form = new Form();
                using var displacement = new ResultDisgComponent();
                using var reaction = new ResultReacComponent();
                using var section = new ResultFsecComponent();
                form.Controls.Add(displacement);
                form.Controls.Add(reaction);
                form.Controls.Add(section);
                form.Show();
                Assert.Equal(1, Sheet(displacement).RowCount);
                Assert.Equal("1.2500", Sheet(displacement).Cells[0, 1].Text);
                Assert.Equal("-0.5000", Sheet(displacement).Cells[0, 6].Text);
                Assert.Equal("1.24", Sheet(reaction).Cells[0, 1].Text);
                Assert.Equal(3, Sheet(section).RowCount);
                Assert.Equal("10", Sheet(section).Cells[0, 1].Text);
                Assert.Equal("11", Sheet(section).Cells[2, 1].Text);
                Assert.Equal("0.750", Sheet(section).Cells[2, 2].Text);

                InputDataService.Instance.dimension = 2;
                LoadResults("""
                    {"result":{"2":{"disg":{"4":{"dx":0.002,"dy":0.003,"rz":0.004}},
                                    "reac":{"4":{"tx":-2,"ty":3,"mz":4}},
                                    "fsec":{"1":{"P1":{"fxi":2,"fyi":3,"mzi":4,"L":1}}}}}}
                    """);
                Assert.Single(Spread(displacement).Sheets);
                Assert.Equal("2", Sheet(displacement).SheetName);
                Assert.Equal(4, Sheet(displacement).ColumnCount);
                Assert.Equal("4.0000", Sheet(displacement).Cells[0, 3].Text);
                Assert.Equal(4, Sheet(reaction).ColumnCount);
                Assert.Equal("4.00", Sheet(reaction).Cells[0, 3].Text);
                Assert.Equal(6, Sheet(section).ColumnCount);
                Assert.Equal(2, Sheet(section).RowCount);
                Assert.Equal("4.00", Sheet(section).Cells[0, 5].Text);
            }
            finally { ClearResults(); }
        });
    }

    [Fact]
    public void InputOnlyFileReloadRemovesAllBaseResultSheets()
    {
        RunSta(() =>
        {
            try
            {
                using var result = JsonDocument.Parse("""
                    {"dimension":3,"result":{"1":{"disg":{"1":{"dx":0.001}},
                                                    "reac":{"1":{"tx":2}},
                                                    "fsec":{"1":{"P1":{"fxi":3,"L":1}}}}}}
                    """);
                InputDataService.Instance.JsonDataOpen(result.RootElement);
                using var form = new Form();
                using var displacement = new ResultDisgComponent();
                using var reaction = new ResultReacComponent();
                using var section = new ResultFsecComponent();
                form.Controls.Add(displacement);
                form.Controls.Add(reaction);
                form.Controls.Add(section);
                form.Show();
                Assert.Single(Spread(displacement).Sheets);
                Assert.Single(Spread(reaction).Sheets);
                Assert.Single(Spread(section).Sheets);

                using var inputOnly = JsonDocument.Parse("""{"dimension":3}""");
                InputDataService.Instance.JsonDataOpen(inputOnly.RootElement);
                Assert.Empty(Spread(displacement).Sheets);
                Assert.Empty(Spread(reaction).Sheets);
                Assert.Empty(Spread(section).Sheets);
            }
            finally { ClearResults(); }
        });
    }

    private static void LoadResults(string json)
    {
        using var document = JsonDocument.Parse(json);
        ResultDisgService.Instance.setDisgJson(document.RootElement);
        ResultReacService.Instance.setReacJson(document.RootElement);
        ResultFsecService.Instance.setFsecJson(document.RootElement);
    }

    private static FpSpread Spread(Control control) =>
        (FpSpread)control.Controls.Find("fpSpread1", true).Single();

    private static SheetView Sheet(Control control) => Spread(control).Sheets[0];

    private static void ClearResults()
    {
        ResultCombineDisgCoordinator.Instance.FailLoad();
        ResultCombineFsecCoordinator.Instance.FailLoad();
        ResultCombineReacCoordinator.Instance.FailLoad();
        ResultDisgService.Instance.clear();
        ResultReacService.Instance.clear();
        ResultFsecService.Instance.clear();
        InputMembersService.Instance.clear();
    }

    private static string FindCtPreset()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null;
             directory = directory.Parent)
        {
            string path = Path.Combine(directory.FullName, "FrameWebforJS", "src", "assets",
                "preset", "サンプル（Ct桁）.json");
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException("Ct preset was not found.");
    }

    private static void RunSta(System.Action body)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "STA UI test did not complete.");
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
