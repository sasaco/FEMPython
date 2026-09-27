using FarPoint.Win.Spread;
using FrameWebforCS.calculation;
using FrameWebforCS.components.result;
using System.Text.Json.Nodes;
using System.Windows.Forms;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class CalculationRequestDerivedViewTests
{
    [Fact]
    public void CanonicalCombineAndPickupViewsRenderCommittedDerivedRows()
    {
        RunSta(() =>
        {
            var store = CalculationResultStore.Instance;
            store.Clear();
            try
            {
                using var form = new Form();
                using var combine = new ResultCombineDisgComponent();
                using var pickup = new ResultPickupDisgComponent();
                form.Controls.Add(combine);
                form.Controls.Add(pickup);
                form.Show();

                var components = new Dictionary<string, double>
                {
                    ["dx"] = 0.003, ["dy"] = -0.001, ["dz"] = 0,
                    ["rx"] = 0, ["ry"] = 0, ["rz"] = 0.02
                };
                var row = new CalculationDerivedRow("2", null, components, "D", "D");
                var modes = CalculationDerivedPresentation.ReadOnlyModes(
                    new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>
                    { ["dx_max"] = [row] });
                var derived = new CalculationDerivedPresentation(
                    [], [new CalculationDerivedCase("C1", "Combined", modes,
                        CalculationDerivedPresentation.ReadOnlyModes(new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>()),
                        CalculationDerivedPresentation.ReadOnlyModes(new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>()))],
                    [new CalculationDerivedCase("P1", "Picked", modes,
                        CalculationDerivedPresentation.ReadOnlyModes(new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>()),
                        CalculationDerivedPresentation.ReadOnlyModes(new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>()))]);
                var presentation = new CalculationResultPresentation(
                    AnalysisResultSetJson.Deserialize(Fixture()), derived, 3);
                store.Commit(presentation);

                var combineSheet = ((FpSpread)combine.Controls.Find("fpSpread1", true).Single()).Sheets[0];
                var pickupSheet = ((FpSpread)pickup.Controls.Find("fpSpread1", true).Single()).Sheets[0];
                Assert.Equal("C1 Combined", combineSheet.SheetName);
                Assert.Equal("P1 Picked", pickupSheet.SheetName);
                Assert.Equal("3.0000", combineSheet.Cells[0, 1].Text);
                Assert.Equal("-1.0000", pickupSheet.Cells[0, 2].Text);
                Assert.Equal("D", combineSheet.Cells[0, 7].Text);

                presentation.UpdateDerived(null);
                store.Commit(presentation); // A definition edit clears derived rows before recompute.
                Assert.Equal(0, ((FpSpread)combine.Controls.Find("fpSpread1", true).Single()).Sheets.Count);
                Assert.Equal(0, ((FpSpread)pickup.Controls.Find("fpSpread1", true).Single()).Sheets.Count);

                var updatedValues = new Dictionary<string, double>(components) { ["dx"] = 0.004 };
                var updatedRows = CalculationDerivedPresentation.ReadOnlyModes(
                    new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>
                    { ["dx_max"] = [new CalculationDerivedRow("2", null, updatedValues, "D", "D")] });
                presentation.UpdateDerived(new CalculationDerivedPresentation([], [
                    new CalculationDerivedCase("C1", "Combined", updatedRows,
                        CalculationDerivedPresentation.ReadOnlyModes(new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>()),
                        CalculationDerivedPresentation.ReadOnlyModes(new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>()))], []));
                store.Commit(presentation); // Same presentation object, new derived snapshot.
                Assert.Equal("4.0000", ((FpSpread)combine.Controls.Find("fpSpread1", true).Single())
                    .Sheets[0].Cells[0, 1].Text);
                store.Clear();
                Assert.Equal(0, ((FpSpread)combine.Controls.Find("fpSpread1", true).Single()).Sheets.Count);
            }
            finally { store.Clear(); }
        });
    }

    [Fact]
    public void CanonicalSectionForceHeadersUseDeclaredUnits()
    {
        RunSta(() =>
        {
            var store = CalculationResultStore.Instance;
            store.Clear();
            try
            {
                using var form = new Form();
                using var section = new ResultCombineFsecComponent();
                form.Controls.Add(section);
                form.Show();
                JsonObject fixture = JsonNode.Parse(Fixture())!.AsObject();
                fixture["units"]!["length"] = "ft";
                fixture["units"]!["force"] = "kip";
                var empty = CalculationDerivedPresentation.ReadOnlyModes(
                    new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>());
                var forceModes = CalculationDerivedPresentation.ReadOnlyModes(
                    new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>
                    {
                        ["fx_max"] = [new CalculationDerivedRow("M1", "i", new Dictionary<string, double>
                        {
                            ["fx"] = 1, ["fy"] = 2, ["fz"] = 3,
                            ["mx"] = 4, ["my"] = 5, ["mz"] = 6
                        }, "1", "source")]
                    });
                var derived = new CalculationDerivedPresentation([], [
                    new CalculationDerivedCase("C1", null, empty, empty, forceModes)], []);
                var presentation = new CalculationResultPresentation(
                    AnalysisResultSetJson.Deserialize(fixture.ToJsonString()), derived, 3);
                store.Commit(presentation);

                var sheet = ((FpSpread)section.Controls.Find("fpSpread1", true).Single()).Sheets[0];
                Assert.Contains(presentation.ForceUnit, sheet.ColumnHeader.Cells[0, 4].Text);
                Assert.Contains(presentation.MomentUnit, sheet.ColumnHeader.Cells[0, 7].Text);
                Assert.DoesNotContain("kN", sheet.ColumnHeader.Cells[0, 4].Text);
                Assert.DoesNotContain("kN", sheet.ColumnHeader.Cells[0, 7].Text);
                Assert.Equal("M1", sheet.Cells[0, 0].Text);
            }
            finally { store.Clear(); }
        });
    }

    private static string Fixture()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        return File.ReadAllText(Path.Combine(directory!.FullName, "FrameWeb", "tests", "data",
            "contracts", "positive", "single-static.json"));
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
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "STA result view test timed out.");
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
