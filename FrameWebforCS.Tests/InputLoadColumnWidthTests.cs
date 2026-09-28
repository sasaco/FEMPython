using FarPoint.Win.Spread;
using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using System.ComponentModel;
using System.Text.Json;
using System.Windows.Forms;
using Xunit;
using Xunit.Abstractions;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class InputLoadColumnWidthTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("m1", "1", false)]
    [InlineData("n", "1", false)]
    [InlineData("tx", "5", false)]
    [InlineData("LoadId", "2", false)]
    [InlineData("m1", "2", true)]
    public void CommittingAnIntensityCellPreservesConfiguredColumnWidths(
        string field, string text, bool existingRow)
    {
        RunSta(() =>
        {
            var service = InputLoadService.Instance;
            var input = InputDataService.Instance;
            int previousDimension = input.dimension;
            service.clear();
            service.SelectCase("1");
            input.SetDimension(3);
            try
            {
                if (existingRow)
                {
                    using var document = JsonDocument.Parse(
                        """{"load":{"1":{"load_member":[{"row":1,"m1":"1"}]}}}""");
                    service.setLoadJson(document.RootElement);
                }

                using var form = new Form { Width = 1200, Height = 500, ShowInTaskbar = false };
                using var component = new InputLoadComponent { Dock = DockStyle.Fill };
                form.Controls.Add(component);
                form.Show();
                var spread = Assert.Single(component.Controls.OfType<FpSpread>());
                spread.ActiveSheetIndex = 1;
                Application.DoEvents();
                var sheet = spread.ActiveSheet;
                Assert.Equal("荷重強度", sheet.SheetName);
                Assert.Equal(16, sheet.ColumnCount);
                float[] before = Widths(sheet);
                foreach (int column in new[] { 0, 1, 2, 9 })
                    Assert.Equal(50f, before[column]);
                int columnIndex = Enumerable.Range(0, sheet.ColumnCount)
                    .Single(index => sheet.Columns[index].DataField == field);
                int rowIndex = service.FindIntensityRowIndex("1", 1);
                Assert.True(rowIndex >= 0);
                int beforeRows = sheet.RowCount;
                int resets = 0;
                void OnListChanged(object? sender, ListChangedEventArgs change)
                {
                    if (change.ListChangedType == ListChangedType.Reset) resets++;
                }
                service.IntensityRows.ListChanged += OnListChanged;
                try
                {
                    sheet.SetActiveCell(rowIndex, columnIndex);
                    spread.Focus();
                    spread.StartCellEditing(EventArgs.Empty, false);
                    Assert.True(spread.EditMode, "The intensity cell did not enter edit mode.");
                    Assert.NotNull(spread.EditingControl);
                    spread.EditingControl.Text = text;
                    spread.StopCellEditing();
                    Assert.False(spread.EditMode);
                    float[] immediate = Widths(sheet);
                    Application.DoEvents();
                    float[] after = Widths(sheet);

                    string caseId = field == "LoadId" ? "2" : "1";
                    int committedIndex = service.FindIntensityRowIndex(caseId, 1);
                    var committed = Assert.IsType<clsLoadIntensityRow>(
                        service.GetIntensityRowAt(committedIndex));
                    switch (field)
                    {
                        case "m1": Assert.Equal(text, committed.m1); break;
                        case "n": Assert.Equal(text, committed.n); break;
                        case "tx": Assert.Equal(5f, committed.tx); break;
                        case "LoadId": Assert.Equal(text, committed.LoadId); break;
                    }

                    output.WriteLine($"Field={field}, existingRow={existingRow}, " +
                        $"DataAutoSizeColumns={sheet.DataAutoSizeColumns}, listResets={resets}, " +
                        $"rows={beforeRows}->{sheet.RowCount}");
                    output.WriteLine($"Before: [{string.Join(", ", before)}]");
                    output.WriteLine($"Immediately after commit: [{string.Join(", ", immediate)}]");
                    output.WriteLine($"After message processing: [{string.Join(", ", after)}]");
                    if (existingRow) Assert.Equal(0, resets);
                    else Assert.True(resets > 0, "The starter-row edit did not reset the bound list.");
                    Assert.Equal(before, after);
                }
                finally { service.IntensityRows.ListChanged -= OnListChanged; }
            }
            finally
            {
                service.clear();
                service.SelectCase("1");
                input.SetDimension(previousDimension);
            }
        });
    }

    private static float[] Widths(SheetView sheet) =>
        Enumerable.Range(0, sheet.ColumnCount).Select(index => sheet.Columns[index].Width).ToArray();

    private static void RunSta(System.Action test)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { test(); }
            catch (Exception exception) { error = exception; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Load column width STA test timed out.");
        if (error != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
