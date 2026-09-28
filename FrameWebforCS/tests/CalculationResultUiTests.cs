using FarPoint.Win.Spread;
using FrameWebforCS.calculation;
using FrameWebforCS.components.result;
using FrameWebforCS.providers;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class CalculationResultUiTests
{
    [Fact]
    public void CanonicalTablesMaterializeOnlySelectedPage()
    {
        RunSta(() =>
        {
            using (JsonDocument empty = JsonDocument.Parse("{}"))
                InputDataService.Instance.JsonDataOpen(empty.RootElement);
            var presentation = new CalculationResultPresentation(
                AnalysisResultSetJson.Deserialize(Fixture()));
            CalculationResultStore.Instance.Commit(presentation);
            try
            {
                using var displacement = new ResultDisgComponent();
                using var reaction = new ResultReacComponent();
                using var section = new ResultFsecComponent();
                foreach (var view in new Control[] { displacement, reaction, section })
                {
                    FpSpread spread = (FpSpread)view.Controls.Find("fpSpread1", true).Single();
                    Assert.Equal(2, spread.Sheets.Count);
                    Assert.True(spread.Sheets[0].RowCount > 0);
                    Assert.Equal(0, spread.Sheets[1].RowCount);
                    spread.ActiveSheetIndex = 1;
                    Assert.Equal(0, spread.Sheets[0].RowCount);
                    Assert.True(spread.Sheets[1].RowCount > 0);
                }
            }
            finally
            {
                CalculationResultStore.Instance.Clear();
                using JsonDocument empty = JsonDocument.Parse("{}");
                InputDataService.Instance.JsonDataOpen(empty.RootElement);
            }
        });
    }

    private static string Fixture()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        if (directory is null) throw new DirectoryNotFoundException();
        JsonObject root = JsonNode.Parse(File.ReadAllText(Path.Combine(directory.FullName,
            "FrameWeb", "tests", "data", "contracts", "positive", "single-static.json")))!.AsObject();
        JsonObject originalCase = root["cases"]!.AsArray()[0]!.AsObject();
        JsonObject originalResult = root["results"]!.AsArray()[0]!.AsObject();
        var cases = new JsonArray();
        var results = new JsonArray();
        foreach (string id in new[] { "A", "B" })
        {
            var resultCase = (JsonObject)originalCase.DeepClone();
            var result = (JsonObject)originalResult.DeepClone();
            resultCase["case_id"] = id;
            result["case_id"] = id;
            cases.Add(resultCase);
            results.Add(result);
        }
        root["cases"] = cases;
        root["results"] = results;
        return root.ToJsonString();
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
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)));
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}
